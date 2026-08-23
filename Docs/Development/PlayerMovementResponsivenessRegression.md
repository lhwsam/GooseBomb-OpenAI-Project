# 플레이어 4방향 이동 응답성 회귀 인계

- 상태: `Resolved` — `10 cells/s + 0.2초 반복 대기` 칸 이동 후보는 `Rejected`, 플레이어 연속 이동 복구
- 최초 회귀 commit: `9f4cf04 refactor: unify actor tile movement`
- 영향 계층: `BombSwap.Core` 플레이어 이동, Unity 입력→세션 연결, EditMode·PlayMode·WebGL 이동 회귀
- 권위 계약: [입력과 플레이어 명령](../Systems/InputAndCommands.md), [격자와 이동](../Systems/GridAndMovement.md), [프레임 반응형 플레이어 이동](ContinuousPlayerMovementSlice.md)
- 사람 검증 근거: [PT-20260815-02](../Playtesting/Results/PT-20260815-02.md)

## 2026-08-24 칸 이동 후보 판정: Rejected

사람 플레이에서 임시 `10 cells/s + 0.2초 반복 대기` 칸 이동 후보가 너무 빠르고 한 번 입력에 두 칸이 진행되는 느낌을 만들었다. 이 후보는 `Rejected`로 판정하고 작업 트리에서 제거했다.

- `9f4cf04` 전체를 되돌리지 않았다. 적 이동의 한 칸 확정 완료, `GridState`의 목적지 예약·원자적 점유 전이와 Unity의 10ms 고정 simulation은 유지한다.
- 플레이어만 기본 `5 cells/s`의 4방향 연속 이동 정책으로 분리했다. 매 simulation step의 이동 거리는 `elapsed × cellsPerSecond`다.
- 키 해제와 방향 변경은 다음 10ms step부터 적용하며, 셀 중심이나 `movementEndsAt`까지 이동을 완료하지 않는다.
- `InitialHeldRepeatDelay`, 고정 `0.2초` 반복 대기, 플레이어용 셀 중심 대기와 방향 FIFO는 제거했다.
- 플레이어는 접근할 다음 셀만 예약한다. 입력 해제·방향 변경으로 사용하지 않는 예약은 즉시 취소하고, 셀 경계를 통과할 때만 정수 점유를 원자적으로 전이한다.
- 입력 어댑터의 현재 유지 방향과 같은 frame에서 끝난 마지막 짧은 탭 하나 보존 계약은 유지한다. 새 직교 키를 우선하고 이를 놓으면 계속 눌린 기존 키로 다음 step에 복귀한다.
- `PrototypeGameSession` 기본값과 플레이 가능한 16개 씬의 직렬화 값은 Unity Editor를 통해 `5`로 복구했다.

기존 연속 이동 테스트의 기대값은 변경하지 않았다. 후보에서 실패하던 EditMode 8건을 포함해 전체 EditMode `363/363`이 복구 상태에서 통과했다.

## 이전 연속 이동 회귀 복구의 결론

플레이어 이동은 **대각선 이동을 허용하지 않는 4방향 이동**과 **입력을 즉시 반영하는 연속 이동**을 동시에 만족해야 한다. 두 요구는 서로 반대되는 것이 아니다.

- 한 simulation 관찰 구간에서는 `North/East/South/West` 중 하나만 적용한다.
- 한 구간에서 X와 Z가 동시에 변하는 45도 대각선 속도는 허용하지 않는다.
- `W+D`처럼 두 키를 함께 누르면 입력 계층이 하나의 cardinal 방향만 선택한다.
- 선택된 cardinal 방향이 바뀌면 한 칸 이동 완료를 기다리지 않고 다음 관찰 frame 또는 고정 simulation step부터 새 축을 적용한다.
- `North → East`가 연속 frame에 적용되어 꺾인 경로가 되는 것은 허용한다. 이것은 한 frame에서 X/Z가 함께 변하는 대각선 이동이 아니다.

## 복구한 플레이어 이동 계약

1. **4방향 제한**
   - 실제 변위는 매 관찰 구간 한 축에만 생긴다.
   - 대각선 입력 벡터는 `CardinalInputInterpreter`가 단일 cardinal 의도로 축소한다.
2. **키 해제 즉시 정지**
   - `Move(None)`을 관찰한 뒤 이미 예약한 한 칸의 중심까지 계속 이동하지 않는다.
   - 정지 중 경과 시간은 다음 입력에 누적하지 않는다.
3. **방향 전환 즉시 반영**
   - 이동 중 새 직교 방향을 누르면 현재 칸 완료나 셀 중심 도착을 기다리지 않는다.
   - 다음 frame 또는 10ms 고정 simulation step에서 새 cardinal 축으로 이동한다.
4. **빠른 반복 입력 보존**
   - `상→우→상→우→상→우`가 서로 다른 관찰 frame에 들어오면 같은 순서의 실제 이동 방향이 관측되어야 한다.
   - 여러 방향을 셀 단위 FIFO에 쌓아 키를 놓은 뒤 실행하는 backlog는 만들지 않는다.
5. **겹침 입력 처리**
   - 기존 방향을 유지한 채 새 직교 키를 누르면 새 직교 방향을 우선한다.
   - 새 키를 놓고 기존 키가 계속 눌려 있으면 다음 관찰 구간부터 기존 방향으로 복귀한다.
   - 같은 대각선 두 키를 변화 없이 유지하는 동안 선택 축을 frame마다 번갈아 바꾸지 않는다.
6. **논리 격자 권위 유지**
   - Transform, Rigidbody와 Collider는 이동 규칙의 권위가 아니다.
   - 정수 셀 점유, 벽·폭탄·actor 차단과 설치자 폭탄 셀 탈출은 `GridState`가 판정한다.
   - 큰 frame에서도 통과하는 셀 경계를 순서대로 검사해 장애물을 건너뛰지 않는다.

## 회귀 원인과 수정 전 상태

`9f4cf04`는 적과 플레이어의 이동을 공통 예약형 칸 이동으로 통합했다. 적에게 필요한 “시작한 한 칸 이동을 목적지 중심까지 완료” 정책이 플레이어에게도 적용되면서 기존 입력 응답 계약을 덮어썼다.

수정 전 `PlayerMovementSimulation`은 다음 상태를 소유했다.

- `StepDuration = 1 / CellsPerSecond`: 기본 5 cells/s에서 정확히 0.2초
- `movementDirection`: 이동 시작 시 잠긴 현재 셀 이동 방향
- `movementEndsAt`: 목적지 중심 도착 시각
- `MoveDirection`: 이동 중 바뀌어도 다음 셀 시작 전까지 소비되지 않는 최신 입력

따라서 수정 전에는 이동 중 `Move(None)`이나 새 직교 방향이 들어와도 `movementEndsAt`까지 기존 방향을 계속 진행했다. 최악의 경우 키 해제·방향 전환 반영이 약 0.2초 늦고, 그 안의 빠른 반복 입력은 실제 이동으로 모두 나타나지 않았다.

이것은 Input System callback이나 WebGL focus 문제가 아니다. 입력 어댑터는 cardinal 의도를 즉시 만들지만 Core 플레이어 이동 정책이 적용을 다음 셀로 미룬다.

## 2026-08-24 최종 연속 이동 복구 결과

- `PlayerMovementSimulation`을 플레이어 전용 연속 이동 정책으로 복구했다. 현재 관찰 구간의 cardinal 입력 한 방향만 `elapsed × cells/s` 거리로 적용하며 셀 중심 완료를 강제하지 않는다.
- 플레이어가 셀 경계에 접근하는 동안만 다음 셀을 `GridState.TryReserveActorMove`로 예약한다. `Move(None)`·직교 전환·취소에서는 즉시 예약을 해제한다.
- 50% 셀 경계를 통과할 때 `TryCommitReservedActorMove`로 정수 점유를 원자적으로 전이하고, 플레이어 예약은 즉시 완료한다. 계속 이동하면 다음 인접 셀만 새로 예약한다.
- 경계 전후 방향 변경에서도 현재 판정 셀의 actor 점유 하나만 남고 이전 목적지 예약이 누수되지 않는다. 적의 `CommittedActorMovement`와 목적지 중심 완료 계약은 변경하지 않았다.
- `PrototypeGameSession`은 실제 10ms simulation step이 있는 frame에서만, 그 step 직전에 `RefreshMoveIntent`를 호출한다. simulation이 관찰하지 않는 짧은 Editor frame이 한-frame 탭을 미리 소비하는 문제를 제거했으며 입력 FIFO나 0.2초 backlog는 추가하지 않았다.
- 반대 기대값으로 바뀌었던 Core·PlayMode 테스트는 키 해제 즉시 정지, `North→East→North`, 빠른 6회 교대와 frame 내 press-release 계약으로 복구했다. PlayMode는 각 방향이 최소 한 번의 10ms 고정 step에 관찰되도록 20ms 이내 제한 대기를 사용한다.

## 위반된 기존 계약과 증거

- `ContinuousPlayerMovementSlice.md`: 0.2초 cadence를 제거하고 frame별 이동, 해제 즉시 정지, 빠른 `North/East` 반복을 요구한다.
- `InputAndCommands.md`: 별도 0.2초 입력 cadence나 다중 방향 queue 없이 다음 관찰 frame에 방향을 적용한다.
- `PT-20260815-02`: 키 해제 즉시 정지, 빠른 상/우 반복, 벽 모서리 직교 전환을 사람이 재확인했고 해당 정책을 `Keep`으로 판정했다.
- 기존 PlayMode 테스트는 위 계약을 검증했지만 `9f4cf04`에서 다음과 같이 반대 기대값으로 변경됐다.
  - 해제 즉시 정지 → 현재 셀 이동 완료 뒤 정지
  - 겹침 직교 방향의 연속 frame 적용 → 다음 셀에서 적용
  - 빠른 교대 방향 각각 반영 → committed cell 안에서는 전환하지 않음

현재 테스트가 통과하더라도 사용자 승인 계약 준수를 뜻하지 않는 이유다.

## 수정 원칙

- `9f4cf04` 전체를 되돌리지 않는다. 적 이동 예약, actor 점유 안전성과 10ms 고정 simulation은 별도 가치가 있다.
- 적의 committed tile 정책과 플레이어의 frame-responsive 정책을 분리한다.
- 플레이어는 현재 cardinal 입력을 다음 고정 step에 적용하고, 한 step에서는 한 축만 이동한다.
- 키 해제·방향 변경 시 이전 목적지 예약을 누수 없이 해제하거나 현재 판정 셀에 맞게 정리한다.
- 셀 경계를 통과할 때만 정수 점유를 원자적으로 전이하고 폭탄 통과 권한을 갱신한다.
- `CardinalInputInterpreter`의 단일 축 선택과 최신 직교 축 우선 정책은 유지한다.
- Transform 보간으로 반응을 숨기지 않고 Core 연속 위치를 그대로 표시한다.

구현 중 가장 먼저 확정해야 할 부분은 **셀 중간에서 정지·직교 전환할 때 예약을 안전하게 정리하는 방법**이다. 이를 위해 플레이어 전용 이동 정책을 두되 `GridState`의 원자적 예약/점유 API를 재사용하거나 필요한 최소 취소 API만 추가한다. 적의 committed movement 계약을 플레이어 편의를 위해 약화하지 않는다.

## 복구한 회귀 테스트

### EditMode

- 20~50ms 진행마다 유지 방향의 연속 위치가 변한다.
- `Move(None)` 뒤 0.1~0.25초를 더 진행해도 위치가 변하지 않는다.
- `North/East`를 frame 또는 10ms step마다 교대하면 각 구간에서 해당 축만 변한다.
- 한 구간에서 X와 Z가 동시에 변하지 않는다.
- `North` 유지 중 짧은 `East` 탭이 East 이동을 만들고, 탭 해제 뒤 North로 복귀한다.
- 방향 변경·해제를 셀 경계 50% 전후에 반복해도 actor 예약과 점유가 누수·중복되지 않는다.
- 벽·폭탄·다른 actor 차단, 큰 delta 경계 순회와 설치자 폭탄 셀 단일 탈출이 유지된다.

### PlayMode

- 실제 Keyboard의 키 해제가 다음 frame부터 Transform을 정지시킨다.
- 실제 `W/D` 또는 방향키의 빠른 6회 교대가 같은 순서의 motion 사건을 만든다.
- `W` 유지→`D` 누름→`D` 해제에서 `North→East→North`가 관측된다.
- 유지 대각선 입력은 선택한 한 축을 유지하며 frame별로 축이 교번하지 않는다.
- pause, focus 상실, 장치 분리에서 즉시 `Move(None)`이 적용되고 복귀 시 stale 이동이 재개되지 않는다.

### WebGL·사람 검증

1. 열린 방에서 키를 짧게 누르고 떼어 추가 미끄러짐이 없는지 확인한다.
2. `상→우`를 빠르게 여섯 번 교대해 모든 방향 변화가 보이는지 확인한다.
3. 위쪽 키 유지 중 오른쪽을 짧게 눌렀다 떼어 `위→오른쪽→위`로 반응하는지 확인한다.
4. 벽 모서리에서 직교 방향 전환이 자연스럽고 벽을 통과하지 않는지 확인한다.
5. 한 frame의 움직임이 45도 대각선이 되지 않는지 확인한다.

## 자동 검증 결과

- Unity 6000.5.3f1에서 최종 플레이어 연속 이동 코드와 임시 씬 저작 도구 제거 뒤 `BombSwap.Core`, `BombSwap.Unity`, `BombSwap.Editor` 컴파일과 domain reload가 성공했다.
- 플레이 가능한 16개 씬의 `PrototypeGameSession.cellsPerSecond`가 모두 `5`임을 Unity 직렬화 경로로 저장·재확인했으며 임시 Editor 도구는 제거했다.
- 기존 기대값을 변경하지 않은 전체 EditMode는 `363/363` 통과, 실패·건너뜀 0이다. 후보에서 실패하던 키 해제·직교 전환·빠른 반복·backlog 금지 8건도 모두 통과했다. 증거 `Artifacts/Verification/ConnectedTests/20260823-223646-232.json`.
- 전체 PlayMode는 `172/186` 통과, 14개 실패, 건너뜀 0이다. 플레이어 이동·입력 응답성 테스트 실패는 0이며, 남은 실패는 기존 보스 fuse·scene binder/run host·추격자/돌진형/자폭병·폭발/방 클리어 회귀에 한정된다. 증거 `Artifacts/Verification/ConnectedTests/20260823-223924-646.json`.
- 실패한 `10 cells/s + 0.2초 반복 대기` 후보의 과거 증거는 `Artifacts/Verification/ConnectedTests/20260823-220605-117.json`, `20260823-220725-410.json`에 보존한다. 이 결과를 현재 계약의 통과 근거로 사용하지 않는다.
- 최종 문서 반영 뒤 StaticOnly가 통과했다. 증거 `Artifacts/Verification/20260824-074531-static/summary.json`.
- Development WebGL 재시도는 실행 전 콘텐츠 validator가 기존 로비 제목, 투척병 Animator, 보스 연쇄 폭탄 fuse와 공개 에셋의 private vendor 직접 참조를 차단해 빌드되지 않았다. 증거 `Artifacts/Verification/20260824-074450-connected-web/webgl-build-status.txt`. WebGL·browser smoke는 미실행이며 통과로 보고하지 않는다.

## 연속 이동 복구의 범위 밖 기록

- 8방향 또는 아날로그 대각선 이동
- `5 cells/s` 이외의 이동 속도 튜닝
- 가속·감속, 관성, 코너 스냅 폭 튜닝
- 적 AI의 committed tile 이동 폐기
- Rigidbody/CharacterController 기반 권위 이동 전환

## 남은 검증과 다음 순서

1. 열린 방에서 짧은 입력 해제 즉시 정지, 셀 중간 직교 전환, 빠른 `상→우` 6회와 `위 유지→오른쪽 짧은 탭→위 복귀`를 사람 플레이로 재확인한다.
2. 벽 모서리와 폭탄 옆, 셀 경계 전후에서 정수 점유와 목적지 예약 누수가 없는지 개발 probe와 실제 플레이로 확인한다.
3. 이동과 별개인 콘텐츠 validator 회귀를 해결한 고정 build에서 WebGL keyboard/gamepad smoke를 다시 실행한다.

폭탄 퓨즈/보스 생성 회귀는 같은 통합에서 드러났지만 별도 원인이다. 이동 수정과 한 변경으로 섞지 않는다.
