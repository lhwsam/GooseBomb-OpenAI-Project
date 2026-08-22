# 사용자 설정, 오디오와 화면 흔들림

- 상태: 설정 저장·공통 UI·키보드 리바인딩 `Accepted`
- 상태: 실제 BGM/SFX 클립과 폭발 화면 흔들림 연출 `Deferred`
- 기준일: 2026-08-21
- 코드 소유: `BombSwap.Unity`의 `PrototypeUserSettingsRuntime`, `PrototypeUserSettingsStorage`, `PrototypeSettingsPanelPresenter`

## 목적

로비와 일시정지에서 같은 설정을 제공하고 WebGL의 페이지 재실행 뒤에도 사용자 선택을 복원한다. 설정은 게임 규칙과 런 진행 상태가 아니며, 입력 에셋·AudioMixer·향후 카메라 연출에 값을 전달하는 Unity 어댑터다.

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
- 향후 BGM AudioSource는 BGM 그룹, 폭탄·피격·적 공격 AudioSource는 SFX 그룹으로 route해야 한다.
- 현재 실제 clip과 AudioSource는 아직 없으므로 slider 동작과 Mixer parameter 반영만 검증한다. `audio-unlocked` 개발 marker는 실제 소리를 증명하지 않는다.

## 화면 흔들림 계약

- `PrototypeUserSettingsRuntime.ScaleScreenShake(authoredAmplitude)`가 저작 amplitude와 사용자 강도를 결합하는 단일 경계다.
- 향후 폭발 presenter나 카메라 shake 어댑터는 설정을 직접 읽어 별도 보정하지 말고 이 메서드의 결과만 사용한다.
- 0 이하의 저작 amplitude와 사용자 0%는 흔들림을 만들지 않는다.
- 이번 작업은 설정과 소비 계약까지만 구현한다. 실제 카메라 이동, Cinemachine impulse, 폭발 거리 감쇠와 중첩 상한은 폭발 연출 슬라이스에서 결정한다.

## UI 저작과 수명

- 공통 생성 경계는 `PrototypeSettingsPanelFactory`이며 TMP `DungGeunMo`와 960×600 reference Canvas 규칙을 따른다.
- 로비 설정 패널은 `DungeonLobby` 씬에 직렬화해 디자이너가 RectTransform, Image, TMP와 Button을 직접 수정할 수 있다.
- 로비 조작 페이지는 ScrollRect이며 디자이너가 저작한 viewport, content 크기와 자식 배치를 presenter가 변경하지 않는다. 최하단 초기화 Button은 런타임 이름 검색 대신 presenter의 직렬화 참조로 연결한다.
- 기존 `SettingsStatusText`는 사용하지 않는다. 키 변경 대기 상태는 해당 키 값 라벨에 표시하고 중복 알림은 같은 버튼의 라벨·RectTransform에 한정한다.
- 일시정지 설정 패널은 기존 pause overlay와 함께 런타임 생성하지만 같은 factory와 presenter를 사용한다.
- 설정 화면은 gamepad binding 문구를 만들지 않지만 기존 Input Action의 gamepad control scheme을 제거하지 않는다.

## 이어하기 보류

저장된 런의 `이어하기` 버튼과 checkpoint 복원은 이번 프로토타입 설정 범위에서 `Deferred`다. 이를 안전하게 구현하려면 graph seed와 현재 방, 방문·클리어·공개 상태, 체력, 토큰, 폭탄 로드아웃·활성 슬롯, 회복·보상 소비 상태의 versioned snapshot과 마이그레이션/손상 복구 정책이 먼저 필요하다. 일시정지의 `게임 계속`은 저장 기능이 아니라 현재 메모리 세션 재개다.

## 검증

- PlayMode: 수치 clamp·dB 변환·화면 흔들림 배율, PlayerPrefs round-trip, binding override round-trip, 손상 JSON 복구.
- scene 통합: 로비의 Mixer 그룹/파라미터·키보드 8개·gamepad 문구 부재, 로비/일시정지의 같은 설정 panel, 설정 중 `Esc`와 실제 pause 수명.
- Editor validator: AudioMixer 그룹/노출 파라미터, 로비와 모든 던전/TestSandbox scene의 설정 runtime 참조.
- WebGL: 로비와 pause에서 panel 표시, 키 변경 후 게임 입력 반영, reload 뒤 유지, slider와 fullscreen 요청, Console/page error, 실제 clip 연결 뒤 사용자 gesture 이후 가청 BGM/SFX.

## 관련 문서

- [입력과 플레이어 명령](InputAndCommands.md)
- [로비와 공통 TMP UI](../Development/LobbySlice.md)
- [브라우저 테스트 매트릭스](../WebGL/BrowserTestMatrix.md)
- [런 결과와 재시작](RunCompletion.md)
