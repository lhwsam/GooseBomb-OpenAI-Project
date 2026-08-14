# 던전 문 상태 작업 계약

- 상태: `Implemented`
- 규칙 소유: `BombSwap.Core`
- Unity 조합 경계: `BombSwap.Unity`
- 선행 결정: [ADR-0007](../ADR/0007-Potential-Room-Exits.md)

## 목표

그래프 연결과 현재 방의 클리어 상태를 실제 문 표현과 씬 전환이 안전하게 소비할 수 있는 읽기 전용 계약으로 만든다. 문 GameObject, Collider 또는 애니메이션은 이 상태를 표현할 뿐 이동 가능 여부의 권위가 아니다.

## 상태 계약

- `DungeonRunState.GetCurrentExitStates()`는 북·동·남·서 순서의 네 상태를 반환한다.
- 그래프 연결이 없는 방향은 `Inactive`이며 대상 방 ID가 없다.
- 연결은 있지만 현재 전투방 또는 보스방이 미클리어면 `Locked`다.
- 안전방, 클리어한 전투방과 클리어한 보스방의 연결은 `Open`이다.
- 연결된 상태는 대상 `DungeonRoomNodeId`를 포함한다. 전환 요청은 별도 추측 없이 같은 방향으로 `TryTravel`을 호출해야 한다.
- 반환 목록은 호출자가 변경할 수 없는 snapshot이다. 문 presenter는 매 frame polling하지 않고 방 입장·클리어처럼 상태가 바뀌는 시점에만 갱신한다.

## Unity 경계

- `PrototypeDungeonRunSession.GetCurrentExitStates()`는 Core snapshot을 그대로 노출한다.
- 전투방에서는 연결 방향 집합이 `DungeonCombatRoomAssignment.ActiveExitDirections`와 같아야 한다.
- 후속 문 presenter는 `Inactive`를 닫힌 외곽 벽, `Locked`를 전투 잠금 표시, `Open`을 상호작용 가능한 출구로 표현한다.
- 후속 전환 어댑터는 `Open`을 확인한 뒤 Core `TryTravel(direction)` 성공을 먼저 확정하고 대상 콘텐츠를 로드한다.
- 씬 로드 실패 정책, run 수명, 입장 spawn, room 회전과 특수방 placeholder는 이 슬라이스에서 구현하지 않는다.

## 검증

- 시작방의 유일 연결과 안정된 네 방향 순서.
- 미클리어 전투방의 모든 연결 잠금과 클리어 직후 동일 대상의 개방.
- read-only snapshot과 정의되지 않은 방향 거부.
- Unity 런 세션의 활성 전투방 출구와 Core 문 상태 방향 일치.

## 롤백

`DungeonRoomExitState`, `DungeonRunState` 조회 API, Unity 세션 위임, 관련 테스트와 이 문서를 한 묶음으로 되돌린다. 기존 그래프·탐색·콘텐츠 배정 상태는 변경하지 않는다.
