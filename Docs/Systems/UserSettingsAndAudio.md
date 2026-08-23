# 사용자 설정, 오디오와 화면 흔들림

- 상태: 설정 저장·공통 UI·키보드 리바인딩 `Accepted`
- 상태: 로비·던전·보스 적응형 BGM 재생 `Accepted`, UI 버튼 클릭 SFX 후보 클립 `Proposed`, gameplay SFX와 폭발 화면 흔들림 연출 `Deferred`
- 기준일: 2026-08-24
- 코드 소유: `BombSwap.Unity`의 `PrototypeUserSettingsRuntime`, `PrototypeUserSettingsStorage`, `PrototypeSettingsPanelPresenter`, `PrototypeBgmPresenter`, `PrototypeBgmMixPolicy`

## 목적

로비와 일시정지에서 같은 설정을 제공하고 WebGL의 페이지 재실행 뒤에도 사용자 선택을 복원한다. 설정은 게임 규칙과 런 진행 상태가 아니며, 입력 에셋·AudioMixer·BGM 표현·향후 카메라 연출에 값을 전달하는 Unity 어댑터다. BGM은 확정된 방·전투·보스 상태를 표현하지만 게임 판정에는 영향을 주지 않는다.

## 플레이어 계약

- 로비의 `조작 방법`과 일시정지의 `설정`은 같은 조작/오디오·화면 페이지를 사용한다.
- 조작 페이지에는 키보드 배치만 표시한다. 게임패드 binding과 지원은 유지하지만 이번 설정 UI에는 표시하지 않는다.
- 변경 가능한 기본 키는 WASD 네 방향, 폭탄 설치, 폭탄 교체, 일시정지, 결과 재시작이다. 방향키 이동은 고정 fallback으로 남는다.
- 키 버튼을 누른 뒤 새 키를 입력하면 즉시 반영하고 저장한다. 이미 표시된 다른 명령이 사용하는 키는 거부하며 `Esc`는 변경만 취소한다.
- 중복 키를 입력하면 별도 상태 문구를 만들지 않는다. 선택한 키 버튼 안에 `이미 사용 중`을 잠시 표시하고 짧게 좌우로 흔든 뒤 기존 키 표시로 복원한다.
- 전체 음량, 배경음, 효과음과 화면 흔들림 강도는 0~100%이며 즉시 반영한다. 화면 흔들림 0%는 끔이다.
- 조작 페이지 최하단의 `키 설정 초기화`는 키보드 override만 제거하며 음량과 화면 흔들림 값은 유지한다.
- 오디오·화면 페이지의 `기본값 복원`은 네 수치와 키보드 override를 함께 제거한다.
- 전체 화면 전환은 현재 브라우저/플랫폼의 Unity fullscreen 요청을 사용한다.
- 일시정지 설정에서 `Esc`는 먼저 진행 중인 키 변경을 취소하고, 다음 `Esc`는 설정을 닫아 일시정지 메뉴로 돌아가며, 그 다음 `Esc`가 게임을 재개한다.

## 상태와 저장

`PrototypeUserSettingsRuntime`은 scene마다 한 개 존재하고 다음 값을 소유한다.

| 값 | 기본값 | 적용 대상 |
|---|---:|---|
| 전체 음량 | 100% | `MasterVolume` |
| 배경음 | 70% | `BgmVolume` |
| 효과음 | 100% | `SfxVolume` |
| 화면 흔들림 | 100% | 향후 카메라 shake amplitude 배율 |

- 수치는 versioned `PlayerPrefs` 키에 저장한다. 키 override는 `InputActionAsset.SaveBindingOverridesAsJson()` 결과를 한 키에 저장한다.
- 잘못되거나 이전 에셋과 호환되지 않는 override JSON은 입력을 막지 않고 폐기한다.
- 설정은 현재 브라우저 profile/site storage에만 남는다. 사이트 데이터 삭제, private browsing 정책 또는 다른 브라우저·기기 이동 뒤에는 기본값으로 돌아갈 수 있다.
- 설정 저장과 던전 run 저장은 별개다. 방문 방, 체력, 폭탄 로드아웃, 적 상태를 저장하지 않는다.

## 오디오 계약

- 권위 에셋은 `Assets/Game/Content/Audio/BombSwapAudioMixer.mixer`다.
- Mixer는 `Master` 아래 `BGM`, `SFX` 그룹을 가지며 `MasterVolume`, `BgmVolume`, `SfxVolume`을 노출한다.
- UI의 선형 0~1 값은 `20 * log10(value)`로 변환하고 0은 -80 dB로 처리한다.
- BGM `AudioSource`는 BGM 그룹, 폭탄·피격·적 공격 `AudioSource`는 SFX 그룹으로 route한다. 적응형 stem gain은 각 `AudioSource.volume`에만 적용하고 사용자 `BgmVolume` Mixer 파라미터를 덮어쓰지 않는다.
- BGM의 런타임 권위 데이터는 `Assets/Game/Content/Audio/PrototypeBgmCatalog.asset`이다. 정확히 여덟 개 clip과 BGM 출력 그룹, 1초 crossfade, 0.5 pause gain, 0.25초 pause duck 전환, 0.1초 DSP 예약 여유를 소유한다. full-mix 미리보기 세 파일은 런타임에서 참조하지 않는다.
- 로비 BGM은 `Assets/Game/Content/Audio/Music/BGM_Lobby_GooseExodus_8Bit_Loop.wav`다. D 단조, 96 BPM, 32마디·80초, 44.1 kHz stereo 16-bit PCM이며 마지막 A장조 도미넌트가 다음 재생의 첫 D 단조로 해결된다. 합성 source는 `Tools/Audio/GenerateLobbyBgm.py`이고 고정 seed와 순환 delay로 같은 파일을 재현한다.
- 던전 BGM은 D 단조, 116 BPM, 32마디·약 66.207초, 44.1 kHz stereo 16-bit PCM의 공통 timeline을 사용한다. 합성 source `Tools/Audio/GenerateDungeonCombatBgm.py`는 2,919,724 frame으로 정렬된 `BGM_Dungeon_PowderCorridor_BaseLayer_8Bit_Loop.wav`, `CombatLayer`, `DangerLayer`, `SanctuaryLayer` 네 stem을 함께 만든다.
- `BaseLayer`는 저음 drone·얇은 chord pad·희박한 D–F–A–C♯ 동기로 시작방, 이미 클리어한 방과 비전투 이동의 연속성을 소유한다. `CombatLayer`는 chord stab·8분음표 bass ostinato·drum·경고 주제를, `DangerLayer`는 off-beat fuse pulse·가속 tick·warning tritone을, `SanctuaryLayer`는 같은 화성 위의 sine pad·bell answer를 소유한다. 각 layer는 gameplay SFX가 아니라 방 상태에 적응하는 BGM stem이다.
- 기본 전투 full mix `BGM_DungeonCombat_PowderCorridor_8Bit_Loop.wav`는 `Base 100% + Combat 100% + Danger 45%` 미리보기이고, 회복 full mix `BGM_DungeonRecovery_PowderCorridor_8Bit_Loop.wav`는 `Base 75% + Sanctuary 100%` 미리보기다. 런타임 room mix는 `Start/BossAnte/Secret/Cleared = Base 100%`, `Recovery = Base 75% + Sanctuary 100%`, `BombReward = Base 85% + Sanctuary 60%`, `Combat = Base 100% + Combat 100% + Danger 45%`로 고정한다.
- 네 던전 stem은 동일 DSP 시각에 시작해 음소거 상태에서도 계속 재생한다. 같은 던전 family 안의 room·클리어 변경은 116 BPM 4박자 다음 마디에서 1초 동안 volume만 crossfade하며 clip timeline을 다시 시작하지 않는다.
- 보스 전투 BGM full mix는 `Assets/Game/Content/Audio/Music/BGM_BossBattle_OverheatedThrone_8Bit_Loop.wav`다. 같은 음형을 역순과 반음 충돌로 변형한 D 단조, 128 BPM, 32마디·60초, 44.1 kHz stereo 16-bit PCM이다. 3+3+2 accent는 제한 추격, 좌우 pulse sweep은 parity 행, 가속 tick은 자폭병 fuse, 7·15·31마디의 밀도 감소는 과열 회복을 표현한다.
- 같은 합성 source `Tools/Audio/GenerateBossBattleBgm.py`는 full mix와 sample-aligned layer 세 개를 함께 만든다. `BGM_BossBattle_OverheatedThrone_BaseLayer_8Bit_Loop.wav`는 기계 리듬·bass·주제를, `BGM_BossBattle_OverheatedThrone_GrandLayer_8Bit_Loop.wav`는 저음 organ·chip choir·octave fanfare·war drum을, `BGM_BossBattle_OverheatedThrone_DangerLayer_8Bit_Loop.wav`는 fuse tick·warning tritone·parity sweep·last-stand double-time을 소유한다. 세 런타임 stem은 2,646,000 frame으로 같고 동일 DSP 시각에 시작한다. phase mix는 `One = Base 100% + Grand 35% + Danger 25%`, `Two = Base 100% + Grand 70% + Danger 60%`, `LastStand = 세 stem 100%`이며 128 BPM 4박자 다음 마디에서 crossfade한다.
- `Lobby ↔ Dungeon ↔ Boss` family 전환은 새 family를 sample 0에서 DSP 예약 시작하고 이전 family를 1초 crossfade 뒤 정지한다. 일시정지는 timeline을 멈추지 않고 unscaled 0.25초 동안 50%로 duck하며 재개하면 100%로 복귀한다. 플레이어 사망과 보스 격파는 1초 fade-out 뒤 현재 family를 정지한다.
- `PrototypeBgmPresenter`는 `Presentation` 계층의 표현 어댑터다. 확정된 `RoomType`, room clear, `BossPhase`, pause, 사망 사건만 구독하며 Core가 DSP 시각·clip·volume을 읽거나 음악이 게임 규칙을 바꾸게 하지 않는다. 전역 singleton 접근 API나 범용 Event Bus를 추가하지 않는다.
- 로비·던전·보스·독립 TestSandbox 대상 17개 scene은 root `PrototypeBgmPresenter` 한 개와 catalog 참조만 직렬화한다. 최초 로드된 presenter가 자기 전용 GameObject를 `DontDestroyOnLoad`로 유지하고 이후 scene의 중복 root는 제거한다. 여덟 `AudioSource`는 런타임에만 만들며 scene YAML에 직렬화하지 않는다.
- WebGL autoplay 정책 때문에 첫 Input System button press 전에는 재생을 예약하지 않는다. 첫 gesture 뒤 현재 family를 시작하고 Development build에 `bgm-audio-started`를 한 번 기록한다. 이 marker는 DSP 예약 시점 도달을 뜻할 뿐 실제 가청 출력·음량 밸런스·브라우저 장치 상태를 증명하지 않는다.
- UI 버튼 클릭 SFX 후보는 `Assets/Game/Content/Audio/SFX/UI/SFX_UI_ButtonClick_GooseClack_8Bit.wav`다. 44.1 kHz mono 16-bit PCM, 6,394 frame·약 0.145초이며 짧은 부리 snap·기계식 저음 body·작은 C♯→D 확인 pulse로 구성한다. `Tools/Audio/GenerateUiButtonClickSfx.py`가 고정 seed로 같은 파일을 재현한다. 향후 연결 시 BGM이 아니라 SFX Mixer 그룹으로 route하고 hover가 아닌 interactable Button의 확정 click/Submit에만 재생한다.
- BGM clip의 처음과 끝은 digital zero이며 `AudioSource.loop`로 전체 clip을 반복한다. catalog validator는 44.1 kHz stereo와 family별 정확한 sample 수, 여덟 clip의 고유성, full-mix 미리보기 비참조를 검사한다. UI SFX도 양 끝을 digital zero로 마감하지만 반복하지 않고 click마다 한 번만 재생한다.
- BGM 연결은 구현됐지만 최종 청감·상대 stem gain·브라우저별 실제 출력 승인은 해당 화면·room과 실제 WebGL에서 사람이 판단한다. UI 버튼 클릭 SFX와 gameplay SFX의 실제 `AudioSource` 연결은 아직 없다. `audio-unlocked`와 `bgm-audio-started` marker만으로 가청 오디오 통과를 기록하지 않는다.

## 화면 흔들림 계약

- `PrototypeUserSettingsRuntime.ScaleScreenShake(authoredAmplitude)`가 저작 amplitude와 사용자 강도를 결합하는 단일 경계다.
- 향후 폭발 presenter나 카메라 shake 어댑터는 설정을 직접 읽어 별도 보정하지 말고 이 메서드의 결과만 사용한다.
- 0 이하의 저작 amplitude와 사용자 0%는 흔들림을 만들지 않는다.
- 이번 작업은 설정과 소비 계약까지만 구현한다. 실제 카메라 이동, Cinemachine impulse, 폭발 거리 감쇠와 중첩 상한은 폭발 연출 슬라이스에서 결정한다.

## UI 저작과 수명

- `PrototypeSettingsPanelFactory`는 누락 UI를 만드는 초기 저작 도구이며 TMP `DungGeunMo`와 960×600 reference Canvas 규칙을 따른다. 실제 실행에서는 로비 scene과 pause 프리팹에 저장된 `PrototypeSettingsPanelPresenter`를 사용한다.
- 로비 설정 패널은 `DungeonLobby` 씬에 직렬화해 디자이너가 RectTransform, Image, TMP와 Button을 직접 수정할 수 있다.
- 로비 조작 페이지는 ScrollRect이며 디자이너가 저작한 viewport, content 크기와 자식 배치를 presenter가 변경하지 않는다. 최하단 초기화 Button은 런타임 이름 검색 대신 presenter의 직렬화 참조로 연결한다.
- 기존 `SettingsStatusText`는 사용하지 않는다. 키 변경 대기 상태는 해당 키 값 라벨에 표시하고 중복 알림은 같은 버튼의 라벨·RectTransform에 한정한다.
- 일시정지 설정 패널은 [공유 pause 프리팹](InGameUiPrefabs.md) 안에 저작한다. 첫 pause 때 overlay 프리팹을 인스턴스화하고 현재 scene의 설정 runtime만 연결한다.
- 설정 화면은 gamepad binding 문구를 만들지 않지만 기존 Input Action의 gamepad control scheme을 제거하지 않는다.

## 이어하기 보류

저장된 런의 `이어하기` 버튼과 checkpoint 복원은 이번 프로토타입 설정 범위에서 `Deferred`다. 이를 안전하게 구현하려면 graph seed와 현재 방, 방문·클리어·공개 상태, 체력, 토큰, 폭탄 로드아웃·활성 슬롯, 회복·보상 소비 상태의 versioned snapshot과 마이그레이션/손상 복구 정책이 먼저 필요하다. 일시정지의 `게임 계속`은 저장 기능이 아니라 현재 메모리 세션 재개다.

## 검증

- PlayMode: 수치 clamp·dB 변환·화면 흔들림 배율, PlayerPrefs round-trip, binding override round-trip, 손상 JSON 복구.
- scene 통합: 로비의 Mixer 그룹/파라미터·키보드 8개·gamepad 문구 부재, 로비/일시정지의 같은 설정 panel, 설정 중 `Esc`와 실제 pause 수명.
- PlayMode: room/clear별 던전 mix, boss phase mix, DSP 다음 마디 계산, Boss room의 던전 정책 오사용 거부.
- Editor validator: AudioMixer 그룹/노출 파라미터, BGM catalog의 8개 clip·sample alignment·미리보기 비참조, 대상 17개 scene의 root presenter 정확히 한 개·catalog 참조·직렬화 `AudioSource` 부재.
- WebGL 자동: 첫 사용자 입력 뒤 `bgm-audio-started`, 기존 `audio-unlocked`, Console/page error를 확인한다. WebGL 수동: 로비→던전→보스 family 전환, 전투/안전/회복/보상/클리어 mix, 보스 phase 상승, pause duck/복귀, 사망·격파 fade-out, 설정 BGM 0/70/100%를 실제 청감으로 확인한다.

## 관련 문서

- [입력과 플레이어 명령](InputAndCommands.md)
- [ADR-0010: Presentation 소유 적응형 BGM](../ADR/0010-Presentation-Owned-Adaptive-Bgm.md)
- [BGM 통합 슬라이스](../Development/BgmIntegrationSlice.md)
- [로비와 공통 TMP UI](../Development/LobbySlice.md)
- [브라우저 테스트 매트릭스](../WebGL/BrowserTestMatrix.md)
- [런 결과와 재시작](RunCompletion.md)
