# 격자와 이동

- 상태: 논리 격자 `Accepted`, 세부 이동 감각 `Proposed`
- 설계 원본: `GDD_v0.2.md` 1.3, 1.4, 6.2, 22장
- 코드 소유: `BombSwap.Core`의 격자/점유 규칙, `BombSwap.Unity`의 이동·좌표 어댑터

## 목적

3D 공간의 이동 감각을 유지하면서 폭탄, 벽, 위험 셀을 재현 가능한 정수 XZ 규칙으로 판정한다.

## 플레이어에게 보이는 동작

- 플레이어는 키보드 또는 게임패드로 탑다운 공간을 이동한다.
- 통과 불가 벽과 폭탄은 이동을 막는다.
- 자신이 방금 설치한 폭탄 셀에서는 빠져나올 수 있다.
- 그 폭탄 셀을 완전히 벗어난 뒤에는 다시 통과할 수 없다.
- 입장 직후 즉시 피격되거나 탈출 경로가 없는 방을 만들지 않는다.

## 책임

- `GridState`: 바닥, 고정 벽, 파괴 가능 벽, 폭탄, actor 점유 상태.
- `GridPosition`: 정수 XZ 셀 값.
- Unity 좌표 어댑터: 월드 위치와 셀 중심/경계 변환.
- 이동 제어기: 입력 벡터를 이동 의도로 변환하고 격자 충돌 결과를 시각 이동에 반영.

카메라, 애니메이션, 발자국 VFX는 이 시스템의 권위 상태가 아니다.

`BombSwapInputReader`와 `CardinalInputInterpreter`가 키보드·게임패드 값을 `PlayerCommand.Move`의 네 방향 또는 `None`으로 변환한다. TestSandbox에서는 `PrototypeGameSession`이 공유 논리 격자의 `PlayerMovementSimulation`을 매 frame 진행하고 `PrototypePlayerController`가 Core 연속 위치를 placeholder Transform으로 표현한다. 입력의 상세 계약은 `InputAndCommands.md`가 소유한다.

같은 TestSandbox의 `ChaserEnemySimulation`은 별도 `ActorId`로 같은 격자를 점유하며 0.5초 cadence와 두 칸 방향 유지로 플레이어를 추격한다. 마지막 방의 선택적 `ChargerEnemySimulation`도 같은 격자를 점유하고 예고 뒤 잠근 방향으로 한 셀씩 돌진한다. 목적 셀의 벽·actor·폭탄 점유는 `GridState.TryMoveActor`가 플레이어와 동일한 원자적 계약으로 차단하고, 각 presenter는 확정된 step만 3D placeholder에 보간한다.

플레이어와 살아 있는 추격자의 접촉은 `GridPosition.IsCardinallyAdjacentTo`가 판정하는 Manhattan 거리 1이다. 돌진형은 다음 이동 셀이 플레이어 셀일 때 겹치지 않고 충돌을 보고한다. 두 판정 모두 Transform·Collider 거리를 규칙 입력으로 사용하지 않는다.

## 구현된 최소 Core 계약

- `GridPosition`은 부호 있는 정수 `X`, `Z`를 보존하는 불변 값이며 값 동등성과 오프셋 계산을 제공한다.
- `GridState`는 명시적으로 설정된 셀만 보관한다. 등록되지 않은 셀은 `Void` 지형과 점유 없음으로 읽힌다.
- 지형은 `Void`, `Floor`, `IndestructibleWall`, `DestructibleWall` 중 하나다.
- 동적 점유는 현재 `Actor`, `Bomb` 두 종류이며, 점유는 `Floor`에만 추가할 수 있다.
- 모든 actor는 양수 `ActorId`를 가진다. `GridState`는 `ActorId → GridPosition`과 `GridPosition → ActorId`를 점유 bit와 함께 원자적으로 유지해 다른 actor의 점유를 대신 이동시키지 못하게 한다.
- actor는 비어 있는 바닥 셀에만 새로 들어갈 수 있다. 설치 직후 상태를 표현하기 위해 actor가 있는 셀에 폭탄을 추가하는 순서만 actor와 폭탄의 동시 점유를 만든다.
- 일반 `TryMoveActor`는 목적지 bomb을 계속 차단한다. 보스의 예고된 한 칸 이동만 `TryMoveActorAllowingBombOverlap`을 호출할 수 있으며, 이 전이도 다른 actor·비바닥을 차단하고 양방향 actor 색인을 원자적으로 유지한다.
- 점유가 남은 셀을 `Floor`가 아닌 지형으로 변경하려는 요청은 상태를 바꾸지 않고 실패한다.

## 구현된 플레이어 이동 계약

- `GridState.TryMoveActor`는 `ActorId`로 현재 셀을 찾고 상하좌우로 인접한 한 셀 사이에서 해당 actor의 점유와 양방향 위치 색인을 원자적으로 옮긴다.
- 목적지가 `Floor`가 아니거나 actor/bomb 점유가 있으면 출발 셀을 바꾸지 않고 이동을 거부한다.
- `GridState`는 정책과 분리된 원자적 전이 계층이므로 출발 셀에 actor와 bomb가 함께 있으면 actor만 옮기고 bomb는 남긴다. 통과 허용 여부는 `PlayerMovementSimulation`이 먼저 판정한다.
- `GridSubcellPosition`은 셀 중심을 정수 값으로 갖는 연속 XZ 위치다. 플레이어의 셀 내부 진행도는 Core가 이 값으로 소유하며 Unity Transform은 권위 상태가 아니다.
- `PlayerMovementSimulation`은 `ActorId`, 주입된 `IGameClock`, 현재 정수 셀, 연속 위치, 유지 중인 이동 방향, 마지막 바라보기 방향과 cells/s를 소유한다. 새 세션은 북쪽을 바라보고, `Move(None)`은 이동만 멈추며 마지막 cardinal 방향을 유지한다. 막힌 방향 입력도 바라보기는 바꾼다.
- 성공한 설치 직후 소유자·현재 셀·활성 폭탄을 확인해 `ActorId`·`BombId`·설치 셀 한 쌍의 통과 권한을 부여한다. 이 권한이 없으면 출발 셀의 폭탄도 이동을 막는다.
- 권한은 설치 셀에서 처음 성공적으로 나가거나 해당 폭탄이 먼저 제거되는 순간 종료한다. 셀 이탈 후에는 목적지 폭탄 차단 규칙 때문에 원래 폭탄 셀로 재진입할 수 없다.
- 유지 방향은 다음 Unity frame에서 주입 시계의 경과 시간 × 기본 5 cells/s만큼 연속 위치를 진행한다. `Move(None)` 동안 위치는 변하지 않고 정지 중 시간도 다음 입력에 누적하지 않는다.
- 방향 변경은 별도 셀 cadence나 pending queue 없이 다음 관찰 frame의 이동 축에 적용한다. 상하좌우만 허용하므로 한 frame의 변위는 한 축에만 생긴다.
- 셀 경계를 통과할 때 `GridState.TryMoveActor`로 정수 점유를 전이하고 `PlayerMovementStep`을 발행한다. 큰 frame은 열린 각 셀을 순서대로 검사해 장애물을 건너뛰지 않는다.
- 목적 셀이 막히면 플레이어는 현재 셀 중심보다 그 목적지 쪽으로 진행하지 않는다. 반대 경계에서 들어온 진행도만 중심까지 정리할 수 있다.
- `PrototypeGameSession`은 TestSandbox의 11×9 바닥과 논리 장애물을 이동·폭탄 simulation에 공유하고 연속 위치 변경을 Unity 표현에 알린다. `PrototypePlayerController`는 독립 보간 없이 Core 위치를 직접 표시한다.
- room asset의 고정 벽은 `IndestructibleWall`, 파괴 가능 벽은 `DestructibleWall`로 초기화된다. 둘 다 이동·설치를 막고, 확정 폭발이 파괴 가능 벽을 `Floor`로 바꾼 뒤부터 같은 셀 전이 규칙으로 통과할 수 있다.

## 구현된 Unity 좌표 계약

- `GridSpace`는 논리 `(0, 0)` 셀 중심에 대응하는 3D 원점과 양수 셀 크기를 값으로 소유한다.
- `GridToWorld`는 정수 XZ를 셀 중심의 Unity `Vector3`로 바꾸고 Y는 격자 원점 높이를 사용한다.
- `GridToWorld(GridSubcellPosition)`은 같은 원점·셀 크기로 Core 연속 XZ를 Unity 월드 위치로 바꾼다.
- `WorldToGrid`는 Y를 논리 판정에서 제외하고 XZ만 변환한다.
- 각 셀은 중심 기준 반열린 구간 `[n - 0.5, n + 0.5)`을 소유한다. 따라서 정확한 양의 반 칸 경계는 다음 셀, 정확한 음의 반 칸 경계는 0번 셀에 속한다.
- NaN/무한대 원점·셀 크기·월드 위치와 `int`/Unity float 좌표 범위를 벗어나는 변환은 예외로 거부한다.
- 이 경계 규칙은 위치를 셀로 해석하는 좌표 계약이다. 연속 이동 중 점유 셀을 언제 전환할지는 이동 제어기의 별도 `Proposed` 정책이다.

## 핵심 불변식

- 하나의 정적 벽 셀은 동시에 바닥이나 다른 벽 종류가 될 수 없다.
- 폭탄 설치 가능 여부는 논리 점유로 판정한다.
- 설치 직후 통과 권한은 `설치자-폭탄` 한 쌍에만 속한다.
- 보스 목적지 bomb overlap은 설치자 탈출 권한과 별개인 명시적 한 칸 전이다. 플레이어·일반 적 이동에 전역 충돌 무시로 확장하지 않는다.
- 통과 권한은 설치자가 해당 셀 경계를 벗어나면 종료되고 다시 활성화되지 않는다.
- Transform 좌표 반올림 결과만으로 벽 통과나 폭발 피격을 확정하지 않는다.

## 미정 사항

- 기본 5 cells/s와 Core 연속 위치 직접 표시의 최종 감각.
- 플레이어 충돌 반경과 벽 모서리 코너 스냅 허용 폭.
- actor끼리의 밀기/겹침 허용 정책.
- 범용 여러 적의 ID 발급과 동일 목적 셀 경합 정책. 현재 두 적 프로토타입은 추격자 `ActorId(2)` 뒤 돌진형 `ActorId(3)` 고정 순서를 사용한다.
- 국소 Manhattan 추격이 실제 수제 방에서 막힐 때 사용할 경로 탐색 범위.

첫 기본 폭탄 수직 슬라이스에서 조작성과 폭발 회피 가독성을 비교해 확정한다.

## 자동 테스트

현재 EditMode 테스트는 다음 계약을 실행 가능하게 고정한다.

- 음수 좌표를 포함한 `GridPosition` 값 동등성, 해시, 오프셋.
- 미등록 셀 기본값과 모든 지형 종류 저장.
- 바닥 외 지형의 점유 거부, 고유 actor ID와 셀 중복 점유 거부.
- actor 위치 양방향 색인, 다른 ID의 이동 거부, actor와 폭탄의 제한된 동시 점유 및 종류별 제거.
- 점유 중 지형 변경 실패의 원자성.
- 정의되지 않은 지형과 유효하지 않은 actor ID의 거부.
- 인접 actor 점유의 원자적 전이, 벽·폭탄 차단, 출발 셀 bomb 보존.
- 보스 전용 이동의 목적지 actor 차단, bomb 동시 점유, bomb 제거 뒤 actor 색인 보존과 일반 이동의 기존 bomb 차단.
- 주입 시계 기반 frame 연속 진행, 해제 즉시 정지·시간 비누적, 빠른 방향 반복, 다중 셀 경계 순회와 막힌 셀 중심 제한.
- 설치자 권한 없는 출발 차단, 소유자 한 번 탈출, 이탈 후 재진입 차단, 비소유자 권한 거부, 폭탄 제거 시 미사용 권한 종료.

현재 PlayMode 테스트는 다음 Unity 연결 계약을 고정한다.

- 임의 원점·셀 크기의 격자↔3D XZ 왕복 변환과 Y 분리.
- 음수/양수 반 칸 경계의 반열린 구간 판정.
- 잘못된 수치와 지원 범위 밖 좌표의 거부.
- 실제 Input System 유지·해제·빠른 방향 반복에서 Core 연속 위치와 placeholder Transform의 동일 frame 반영.
- 저작된 논리 장애물이 논리 위치와 시각 위치를 함께 차단함.
- 파괴 가능 벽이 폭발 전 이동을 막고 확정 파괴 뒤 `Floor`가 되어 이동 가능한 상태로 열린다.
- 실제 `Z` 설치 뒤 소유자가 셀을 빠져나오고 반대 입력으로 폭탄 셀에 재진입하지 못함.
- 추격자가 플레이어와 공유하는 논리 격자에서 이동하고 presenter가 확정된 적 step을 보간함.
- 폭발 사망 뒤 추격자 actor 점유가 제거되고 placeholder가 짧은 사망 표시 뒤 비활성화됨.
- 마지막 방 돌진형의 예고·한 셀 이동·충돌 정지와 벽·폭탄·actor 차단, 폭발 사망 뒤 점유 제거와 적별 presenter 비활성화.
- cardinal 인접만 접촉 피해 후보가 되고 대각선·극단 좌표 계산이 overflow 없이 거부됨.

다음 항목은 방 콘텐츠 구현 이후 추가한다.

- 방 메타데이터의 출입구와 유효 셀 연결성.
