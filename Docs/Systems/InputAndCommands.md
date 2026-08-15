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
| 일시정지·재개 | `Esc` | Start 버튼 |
| 완료·실패한 런 재시작 | `R` | Select 버튼 |

이동은 상하좌우 네 방향만 Core에 전달한다. 아날로그·복합 입력은 절댓값이 큰 축을 선택한다. 두 축의 크기가 같고 현재 방향도 여전히 눌려 있으면 현재 축에 직교하는 새 전환 축을 우선해 짧은 키 겹침에서도 방향 전환을 즉시 명령으로 만든다. 유지 중인 방향이 벡터에 없거나 `None`이면 세로축을 우선하는 결정론적 규칙을 사용한다.

## 책임과 비책임

- `BombSwapInputActions.inputactions`: 장치 경로, 액션 타입, control scheme의 권위 에셋.
- `BombSwapInputReader`: 액션 callback을 구독하고 frame 경계에서 `PlayerCommand`를 발행하며 focus/생명주기를 정리한다. 세션의 이동 계산 직전에는 현재 Move 값을 다시 읽고 같은 frame 안에 끝난 마지막 짧은 방향 탭을 한 frame만 보존한다.
- `CardinalInputInterpreter`: `Vector2`를 네 방향 이동 의도로 축소한다.
- `PlayerCommand`: `Move`, `PlaceBomb`, `SwapBomb`, `Pause`, `RestartRun` 의미와 이동 방향을 보존하는 Core 값이다.
- `PrototypeGameSession`: TestSandbox에서 공유 시계·격자와 실제 pause 상태를 소유한다. 활성 상태에서는 `Move`를 `PlayerMovementSimulation`, `PlaceBomb`과 `SwapBomb`을 `BombWeaponLoadout`에 전달하고, pause 상태에서는 `Pause` 외 명령과 simulation 진행을 차단한다.
- `PrototypePlayerController`: Core 연속 위치 변경을 받아 placeholder Transform에 직접 표시한다.
- `PrototypePausePresenter`: 세션의 확정된 pause 상태만 구독해 `PAUSED` 오버레이와 재개 키를 표시한다. 입력을 직접 읽거나 상태를 판정하지 않는다.
- `PrototypeRunCompletionPresenter`: 완료 또는 실패 결과 화면이 보일 때만 `RestartRun`을 persistent run host에 전달한다.

입력 계층은 이동 가능 여부, 폭탄 설치·교체 성공, 쿨타임, 실제 pause 상태를 판정하지 않는다. 현재 TestSandbox에서 이동, 활성 슬롯, 설치와 교체 성공은 공유 Core simulation이 판정하고, pause 전이는 세션이 판정하며, Transform/prefab/HUD와 pause presenter가 그 결과를 표현한다.

## 상태와 전이

- `Move` performed/canceled callback은 방향 변화를 기록한다. `PrototypeGameSession`이 매 `Update`의 이동 계산 전에 `RefreshMoveIntent`를 호출하면 입력 어댑터가 최신 값을 다시 샘플링하고 그 frame의 의미 명령을 한 번 확정한다.
- press와 release가 한 Unity frame 안에 모두 처리돼 최종 장치 상태가 `None`이더라도 마지막 짧은 cardinal 탭은 정확히 한 frame의 `Move`로 보존한다. 다음 frame에는 실제 유지 상태로 복귀하므로 0.2초 시간 버퍼나 명령 backlog를 만들지 않는다.
- 입력 벡터가 실제로 바뀐 경우에만 방향 규칙을 다시 평가하므로, 같은 대각선 두 키를 유지해도 선택 축이 frame마다 번갈아 바뀌거나 중복 명령이 발생하지 않는다.
- 서로 직교하는 두 cardinal 키가 겹치면 이전 키를 놓기 전에 새 전환 방향을 발행하고, 이전 키 해제만으로 같은 명령을 중복 발행하지 않는다.
- 입력 어댑터가 발행하는 `Move`는 현재 유지 방향 또는 같은 frame 안에 끝난 마지막 짧은 탭이다. Core 이동은 별도 0.2초 입력 cadence나 다중 방향 queue 없이 다음 관찰 frame의 연속 위치에 이 방향을 적용한다.
- 이동 해제는 `Move(None)`으로 표현한다.
- 설치·교체·pause·재시작은 버튼의 performed 시점에 한 번 발행한다.
- 살아 있는 활성 세션에서 `Pause`를 받으면 `PrototypeGameSession.IsPaused`를 toggle한다. 진입 시 Core 이동 의도와 입력 어댑터의 유지·같은 frame 짧은 탭을 모두 해제한다.
- pause 중에는 세션 `Update`가 입력 재샘플링과 `ManualGameClock.Advance` 전에 반환한다. 따라서 이동, 폭탄 설치·교체, fuse·쿨타임, 적·보스 상태와 피해가 함께 멈추며 `Time.timeScale`은 변경하지 않는다.
- pause 중 `Move`, `PlaceBomb`, `SwapBomb`, `RestartRun`은 소비하지 않는다. 다시 `Pause`를 받으면 현재 유지 중인 Move 값을 즉시 재샘플링하고 다음 `Update`부터 같은 논리 시계를 진행한다.
- `PauseStateChanged`는 세션이 실제 상태를 바꾼 뒤 발행한다. UI와 개발 probe는 입력 버튼 자체가 아니라 이 사건을 구독한다.
- 컴포넌트 비활성화 또는 application focus/pause 상실 시 활성 이동을 즉시 `None`으로 해제한다.
- focus를 잃은 동안 Gameplay map을 비활성화하고 map에 바인딩된 장치 상태를 reset한 뒤 복귀 시 다시 활성화한다.
- enable/disable마다 callback을 대칭으로 구독·해제해 씬 재진입에서도 중복 명령을 만들지 않는다.

## Unity 저작 계약

- 에셋 경로: `Assets/Game/Content/Input/BombSwapInputActions.inputactions`
- action map: `Gameplay`
- actions: `Move`(`Value/Vector2`), `PlaceBomb`, `SwapBomb`, `Pause`, `RestartRun`(`Button`)
- control schemes: 필수 Keyboard 한 개, 필수 Gamepad 한 개
- 생성/복구 도구: `Bomb Swap/Prototype/Create Missing Prototype Content`
- `PrototypeContentValidator`가 액션, 필수 binding, 중복 binding, control scheme, 아홉 던전·TestSandbox 씬 참조와 Build Settings 순서를 검사한다.

기존 Unity 템플릿 `Assets/InputSystem_Actions.inputactions`는 수정하지 않는다. BombSwap 런타임은 게임 전용 에셋만 참조한다.

## WebGL 고려사항

- canvas focus 상실 중 key-up이 누락되어도 `SetInputFocus(false)` 경계에서 이동을 해제한다.
- 개발 WebGL 빌드의 `PrototypeInputHarnessProbe`는 게임 세션이 입력 구독을 완료한 뒤 `probe-ready`와 `room-ready-<room-id>`를 보내고, 입력 방향 `move-direction-*`, 실제 frame 이동 `move-motion-direction-*`, 논리 셀 경계 전이 `move-step-direction-*`, 폭탄·전투와 swap 사건을 브라우저 검증 배열에 기록한다. 세션의 실제 pause 전이는 `pause-entered`, `pause-resumed`로 기록하고 최초 정상 왕복을 `pause-resume`으로 요약한다.
- `audio-unlocked` probe는 사용자 입력을 게임이 수신한 시점을 표시할 뿐 실제 오디오 클립 재생을 증명하지 않는다. 실제 오디오 연결 후 브라우저에서 별도로 확인해야 한다.
- probe와 `.jslib` 브리지는 개발 빌드 검증용이며 게임 규칙의 권위 API가 아니다.

## 자동 테스트

- EditMode: 명령 factory, 유효성, 방향 보존, 값 동등성.
- PlayMode: cardinal 축 선택과 새 직교 축 tie-break, 실제 방향키 겹침·빠른 단타·동일 frame press-release, 유지 대각선의 최신 축 고정, Input System Keyboard 상태와 합성 Gamepad의 왼쪽 스틱·D-pad·South/West/Start/Select 상태→이동·폭탄·pause·재시작 명령 변환, focus 상실 해제와 누락 key-up reset, 재활성화 후 중복 callback 방지, 유지·해제 입력→Core 연속 위치→Transform 직접 표시, pause 중 이동·설치·교체와 fuse 정지·UI 표시 및 재개 뒤 유지 키 재적용.
- Editor validator: Input Actions 구조, 세 TestSandbox 씬의 필수 참조·카메라·조명·방 전환 계약, 첫 enabled Build Settings 씬 세 개의 순서.
- WebGL smoke: canvas focus 뒤 오른쪽 키를 누른 채 브라우저 `blur`를 발생시켜 `Move(None)`과 셀·motion 정지를 확인하고, 누락 key-up 상태의 `focus` 복귀가 이동을 되살리지 않는지 검증한다. 이어 안전방에서 `Esc`로 실제 pause에 진입하고 `PAUSED` 화면을 캡처한다. pause 중 방향키 유지와 `Z`를 보내도 논리 셀·frame motion·폭탄 설치 수가 변하지 않아야 하며, 두 번째 `Esc`의 `pause-resumed` 뒤 진행을 재개한다. 첫 전투방에서는 `ArrowLeft/ArrowUp`의 즉시 press-release 단타를 여섯 번 교대하고 각 탭이 한 frame의 대응 `move-motion-direction-*`을 만든 뒤 추가 이동 없이 멈춰야 한다. 별도 가상 표준 게임패드 smoke는 브라우저 API 연결부터 스틱·D-pad의 방향/해제, 이동 중 분리의 `Move(None)`·300ms 위치 안정성, 동일 index 재연결의 실제 이동 복구, South 설치·자기폭발 실패, West 교체 명령, Start pause 중 유지 스틱·South 차단과 재개 뒤 유지 스틱 재적용, Select의 실패 런 재시작을 확인한다. 보스 격파 뒤 완료 화면과 `R` 재시작을 확인하고, 새 안전방에서 자기 폭발로 사망시킨 뒤 실패 화면과 두 번째 새 run 시작까지 확인한다.

## 미정 사항과 종료 조건

- 동일 크기 두 축에서 새 직교 방향을 우선하는 정책과 frame 연속 이동은 PT-20260814-01 결함 수정으로 채택했다. 기본 5 cells/s, 코너 보정 폭과 셀 경계 판정 가독성은 후속 조작감 비교 전까지 `Proposed`다.
- 사용자 리바인딩과 UI 전용 action map은 프로토타입 코어 전투 이후 결정한다.
- 게임패드 binding 구조, 합성 Input System 장치 상태→의미 명령, WebGL 표준 가상 장치의 브라우저 API→Emscripten→Unity Input System→명령·분리 정지·재연결 복구·Core 설치·실패·Select 재시작 경로는 자동 검증했다. 실제 목표 물리 컨트롤러의 연결·장치별 버튼 표기·deadzone·대각선 값·브라우저/OS 차이·조작감 수동 플레이가 남아 있다.
- application focus 상실 시 입력은 해제하지만 자동 pause는 하지 않는다. 설정 메뉴, UI 전용 action map과 사용자 리바인딩은 현재 범위 밖이다.

## 관련 문서

- `../Architecture/RuntimeFlow.md`
- `GridAndMovement.md`
- `../WebGL/BrowserTestMatrix.md`
- `../Testing/VerificationHarness.md`
