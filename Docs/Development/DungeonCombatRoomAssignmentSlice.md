# 던전 전투방 콘텐츠 배정 작업 계약

- 상태: `Implemented`
- 소유: 호환·배정 규칙 `BombSwap.Core`, 저작 원본 `BombSwap.Authoring`, 검증·동기화 `BombSwap.Editor`
- 결정: [ADR-0007](../ADR/0007-Potential-Room-Exits.md)

## 목표

`DungeonGraph`의 모든 전투 노드에 현재 다섯 수제 전투방 중 하나와 회전을 결정적으로 배정한다. 그래프 연결 방향과 실제 잠재 출구 방향은 일치해야 하고, 같은 seed·그래프·카탈로그에서 재현 가능해야 한다.

## 입력과 출력

- 입력: 불변 `DungeonGraph`, 검증된 `CombatRoomDefinition` 카탈로그.
- 출력: `prototype-combat-assignment-v1`의 read-only `DungeonCombatRoomLayout`.
- 각 `DungeonCombatRoomAssignment`은 그래프 노드 ID, 안정 room definition ID, 시계 방향 회전과 활성 출구 방향 snapshot을 소유한다.

## 배정 규칙

1. 카탈로그를 room ID ordinal 순서로 정렬하고 null·빈 목록·중복 ID를 거부한다.
2. 그래프의 전투 노드를 안정 ID 순서로 처리한다.
3. 각 노드의 이웃 XZ 좌표를 cardinal 활성 출구 집합으로 만든다.
4. 0/90/180/270도 회전 뒤 모든 활성 출구를 지원하는 정의만 후보로 둔다.
5. 호환 후보 중 현재 사용 횟수가 최소인 정의를 seed 기반으로 선택하고, 호환 회전도 같은 분리 RNG로 선택한다.
6. 후보가 없으면 실패 노드와 필요한 방향을 포함한 오류로 중단한다.

## 콘텐츠 변경

- `prototype-combat-loop`, `lanes`, `pillars`, `armor`, `gates`는 중앙 북 `(0,4)`, 동 `(5,0)`, 남 `(0,-4)`, 서 `(-5,0)` 잠재 출구를 가진다.
- 같은 방향의 출구 중복은 `CombatRoomDefinition` 생성 시 거부한다.
- Editor builder가 다섯 자산을 Unity 직렬화로 동기화하고 validator가 cardinal 네 방향을 재확인한다.

## 범위 밖

- 실제 문·portal GameObject, 닫힘/열림 표현과 trigger.
- 씬 또는 prefab의 회전·로드·언로드.
- 특수방 콘텐츠 배정.
- 체력·폭탄 슬롯·파괴 상태의 방 간 보존.

## 검증 기준

- 배정기와 방 정의 대상 EditMode 테스트.
- 128개 seed 배정 다양성, 카탈로그 순서 무관 재현, 정의별 사용 균형, 5노드·5정의 각 1회 사용과 실패 경계.
- 모든 배정의 활성 출구가 그래프 연결과 회전된 저작 출구에 일치.
- 실제 다섯 room asset의 Editor content validator 오류 0.
- 전체 EditMode·PlayMode 회귀와 Console Error 0.
- `Tools/Verify.ps1 -StaticOnly` 통과. 실행 중 Editor 잠금 시 Full 명령 대신 연결된 동일 구성요소 증거를 남긴다.

## 롤백

배정 Core 파일, 회전 값, 방 정의의 방향 중복 불변식, 다섯 room asset의 cardinal 출구, builder·validator·테스트와 이 문서를 함께 되돌린다. `DungeonGenerator`의 `prototype-tree-v1` 출력은 변경하지 않는다.
