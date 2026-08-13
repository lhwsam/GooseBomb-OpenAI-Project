# 입력과 플레이어 명령

- 상태: 의미 명령 경계 `Accepted`, 키 배치와 대각선 처리 감각 `Proposed`
- 설계 원본: `GDD_v0.2.md` 1.3장, `Architecture/RuntimeFlow.md`
- 코드 소유: `BombSwap.Core`의 명령 값, `BombSwap.Unity`의 Input System 어댑터

## 목적

키보드·게임패드의 장치 상태를 게임 규칙에 직접 노출하지 않고, 재현 가능한 플레이 의도인 `PlayerCommand`로 변환한다. WebGL canvas가 focus를 잃었다가 돌아와도 이동 입력이 눌린 상태로 남지 않아야 한다.

## 플레이어에게 보이는 동작

현재 프로토타입 키 배치는 다음과 같다. 키 배치는 플레이테스트 전까지 가설이며 명령 의미는 유지한다.

| 명령 | 키보드 | 게임패드 |
|---|---|---|
| 이동 | WASD 또는 방향키 | 왼쪽 스틱 또는 D-pad |
| 폭탄 설치 | `Z` | South 버튼 |
| 폭탄 교체 | `X` | West 버튼 |
| 일시정지 요청 | `Esc` | Start 버튼 |

이동은 상하좌우 네 방향만 Core에 전달한다. 아날로그 입력은 절댓값이 큰 축을 선택한다. 두 축의 크기가 같으면 아직 눌린 이전 방향을 유지하고, 유지할 방향이 없으면 세로축을 우선하는 결정론적 규칙을 사용한다.

## 책임과 비책임

- `BombSwapInputActions.inputactions`: 장치 경로, 액션 타입, control scheme의 권위 에셋.
- `BombSwapInputReader`: 액션 callback을 구독하고 `PlayerCommand`를 발행하며 focus/생명주기를 정리한다.
- `CardinalInputInterpreter`: `Vector2`를 네 방향 이동 의도로 축소한다.
- `PlayerCommand`: `Move`, `PlaceBomb`, `SwapBomb`, `Pause` 의미와 이동 방향을 보존하는 Core 값이다.
- 향후 `GameSession`: 명령을 논리 시간과 함께 simulation에 전달한다.

입력 계층은 이동 가능 여부, 폭탄 설치 성공, 쿨타임, 실제 pause 상태를 판정하지 않는다. 현재 TestSandbox에는 아직 `GameSession` 소비자가 없으므로 키 입력이 플레이어 Transform이나 폭탄 상태를 바꾸지 않는다.

## 상태와 전이

- `Move` performed/canceled에서 방향이 바뀔 때만 새 이동 명령을 발행한다.
- 이동 해제는 `Move(None)`으로 표현한다.
- 설치·교체·pause는 버튼의 performed 시점에 한 번 발행한다.
- 컴포넌트 비활성화 또는 application focus/pause 상실 시 활성 이동을 즉시 `None`으로 해제한다.
- focus를 잃은 동안 Gameplay map을 비활성화하고 map에 바인딩된 장치 상태를 reset한 뒤 복귀 시 다시 활성화한다.
- enable/disable마다 callback을 대칭으로 구독·해제해 씬 재진입에서도 중복 명령을 만들지 않는다.

## Unity 저작 계약

- 에셋 경로: `Assets/Game/Content/Input/BombSwapInputActions.inputactions`
- action map: `Gameplay`
- actions: `Move`(`Value/Vector2`), `PlaceBomb`, `SwapBomb`, `Pause`(`Button`)
- control schemes: 필수 Keyboard 한 개, 필수 Gamepad 한 개
- 생성/복구 도구: `Bomb Swap/Prototype/Create Missing Prototype Content`
- `PrototypeContentValidator`가 액션, 필수 binding, 중복 binding, control scheme, 씬 참조와 Build Settings를 검사한다.

기존 Unity 템플릿 `Assets/InputSystem_Actions.inputactions`는 수정하지 않는다. BombSwap 런타임은 게임 전용 에셋만 참조한다.

## WebGL 고려사항

- canvas focus 상실 중 key-up이 누락되어도 `SetInputFocus(false)` 경계에서 이동을 해제한다.
- 개발 WebGL 빌드의 `PrototypeInputHarnessProbe`는 입력 구독 직후 `probe-ready`를 보내고, 최초로 관찰한 move/place/swap과 pause→resume 한 쌍을 브라우저 검증 배열에 기록한다.
- `audio-unlocked` probe는 사용자 입력을 게임이 수신한 시점을 표시할 뿐 실제 오디오 클립 재생을 증명하지 않는다. 실제 오디오 연결 후 브라우저에서 별도로 확인해야 한다.
- probe와 `.jslib` 브리지는 개발 빌드 검증용이며 게임 규칙의 권위 API가 아니다.

## 자동 테스트

- EditMode: 명령 factory, 유효성, 방향 보존, 값 동등성.
- PlayMode: cardinal 축 선택과 tie-break, 실제 Input System 키 상태→명령 변환, focus 상실 해제와 누락 key-up reset, 재활성화 후 중복 callback 방지.
- Editor validator: Input Actions 구조, TestSandbox 필수 참조, 카메라·조명, 첫 enabled Build Settings 씬.
- WebGL smoke: canvas focus 후 `W`, `Z`, `X`, `Esc` 두 번을 보내고 개발 probe 사건을 확인한다.

## 미정 사항과 종료 조건

- 실제 이동 감각, 입력 버퍼와 simulation step 연결은 첫 수직 슬라이스에서 확정한다.
- 사용자 리바인딩과 UI 전용 action map은 프로토타입 코어 전투 이후 결정한다.
- 게임패드 binding은 자동 구조 검증만 완료했으며 목표 기기 수동 플레이가 남아 있다.
- pause 명령이 논리 시계와 UI를 실제로 멈추는 연결은 아직 없다.

## 관련 문서

- `../Architecture/RuntimeFlow.md`
- `GridAndMovement.md`
- `../WebGL/BrowserTestMatrix.md`
- `../Testing/VerificationHarness.md`
