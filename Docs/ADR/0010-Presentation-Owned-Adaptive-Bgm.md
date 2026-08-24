# ADR-0010: Presentation 소유 적응형 BGM

- 상태: `Accepted`
- 날짜: 2026-08-24
- 관련: [사용자 설정, 오디오와 화면 흔들림](../Systems/UserSettingsAndAudio.md), [BGM 통합 슬라이스](../Development/BgmIntegrationSlice.md)

## 맥락

로비, 던전 안전·전투·회복·보상 상태와 보스 phase마다 음악 밀도를 바꿔야 하지만 방 전환 때마다 곡을 다시 시작하면 연속성이 깨진다. 던전과 보스 음악은 서로 sample-aligned stem으로 저작됐으며 WebGL은 사용자 gesture 이전 자동 재생을 제한할 수 있다. 동시에 음악 재생 시각과 음량이 결정론적 Core 규칙에 들어가면 Unity 오디오 장치, frame과 브라우저 상태가 게임 판정에 섞인다.

## 결정

- 적응형 BGM은 `BombSwap.Unity` Presentation 계층이 소유한다. Core는 clip, DSP 시각, BPM, stem gain과 사용자 음량을 알지 않으며 방 종류·클리어·보스 phase·pause·사망 같은 기존 확정 상태만 제공한다.
- `PrototypeBgmCatalogAsset`이 BGM Mixer 그룹과 여덟 런타임 clip, crossfade·pause duck·DSP 예약 수치를 소유한다. full-mix 세 파일은 청감 미리보기일 뿐 런타임 catalog에 넣지 않는다.
- 각 대상 scene은 root `PrototypeBgmPresenter` 한 개와 catalog 참조를 저작한다. 최초 presenter의 전용 GameObject만 scene 전환을 넘어 유지하고 이후 중복 presenter는 스스로 제거한다. 전역 접근자나 Service Locator는 제공하지 않는다.
- presenter는 첫 Input System button gesture 뒤 현재 family를 DSP 예약 시작한다. `Lobby`, `Dungeon`, `Boss` family 변경은 sample 0에서 새 family를 시작해 1초 crossfade하고 이전 family를 정지한다.
- 같은 던전 또는 보스 family의 모든 stem은 동일 DSP 시각에 시작하고 gain 0이어도 timeline을 유지한다. room·clear·boss phase 변경은 다음 4박자 마디 경계부터 한 마디 동안 stem volume만 smoothstep crossfade한다. family 전환과 terminal fade의 저작 시간은 catalog 값을 유지한다.
- 일시정지는 BGM timeline을 멈추지 않고 50%로 duck한다. 사망과 보스 격파는 fade-out 뒤 정지한다.
- 사용자 `BgmVolume`은 AudioMixer에만 적용한다. 적응형 mix와 pause duck은 `AudioSource.volume`에서 합성해 사용자 설정 값을 덮어쓰지 않는다.
- Development WebGL의 `bgm-audio-started`는 예약 시작 경계 도달만 나타낸다. 실제 가청 출력과 밸런스는 브라우저 수동 검증을 별도로 요구한다.

## 대안

- 방마다 완성된 full-mix 한 곡을 새로 재생한다: 구현은 단순하지만 방 왕복마다 음악 phase가 초기화되고 전투·클리어 전환이 거칠다.
- AudioMixer snapshot으로 모든 적응형 상태를 표현한다: 사용자 `BgmVolume`과 상태 gain의 소유 경계가 흐려지고 sample-aligned source 시작·정지를 별도로 관리해야 한다.
- persistent 전역 audio singleton API를 둔다: 어느 코드에서도 음악을 제어할 수 있어 수명과 상태 권위가 불명확해진다.
- Core에서 음악 상태 머신을 소유한다: 재생이 게임 규칙에 영향을 주지 않으므로 결정론적 Core 복잡도만 늘어난다.

## 결과

- 던전과 보스 stem의 시간축을 유지하면서 상태 변화만 음악 밀도로 표현할 수 있다.
- scene 직접 실행과 `LoadSceneMode.Single` 전환 모두 같은 presenter 계약을 사용한다.
- BGM 설정, 적응형 mix와 pause duck이 독립된 gain 단계로 결합된다.
- target scene, catalog와 clip 길이가 늘어날 때 Editor authoring/validator를 함께 갱신해야 한다.
- 실제 WebGL 출력은 자동 marker만으로 완료 판정할 수 없으며 브라우저·장치별 청감 검증이 남는다.

## 롤백

17개 scene의 `PrototypeBgm` root와 catalog를 제거하고 presenter를 비활성화한다. AudioMixer와 원본·미리보기 clip은 독립 자산으로 유지할 수 있으며 Core와 gameplay 상태에는 롤백 변경이 없다.
