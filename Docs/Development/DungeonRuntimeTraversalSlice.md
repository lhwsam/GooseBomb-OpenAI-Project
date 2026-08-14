# 던전 런타임 탐색 상태 작업 계약

- 상태: `Implemented`
- 소유: Core 진행 상태 `BombSwap.Core`, 후속 Unity 표현·씬 전환 `BombSwap.Unity`
- 관련 문서: [GDD v0.2](../GameDesign/GDD_v0.2.md), [던전 생성](../Systems/DungeonGeneration.md), [방 저작](../Systems/RoomAuthoring.md)

## 플레이어 계약

- run은 생성된 `DungeonGraph`의 시작방에서 시작한다.
- 시작방·폭탄 보상방·보스 전실은 안전방이며 입장만으로 출구가 잠기지 않는다.
- 처음 입장한 일반 전투방은 클리어될 때까지 모든 퇴실을 막는다.
- 보스방도 별도 보스 구현이 연결되기 전부터 같은 잠금 계약을 사용한다.
- 현재 전투방을 클리어하면 연결된 어느 방으로든 이동할 수 있다.
- 클리어한 전투방은 다시 방문해도 잠기지 않고 자유롭게 통과할 수 있다.
- 그래프에 직접 연결되지 않은 방으로는 이동할 수 없다.

## 기술 계약

- `DungeonRunState`가 현재 방, 직전 방, 방문 여부와 전투 클리어 여부의 권위 원본이다.
- 이동은 대상 노드 ID 또는 현재 방에서 본 `RoomExitDirection`으로 요청한다.
- 방향 이동은 `DungeonGraph`의 정수 XZ 좌표와 cardinal 연결에서만 해석한다.
- 실패한 이동과 중복 클리어는 상태를 부분 변경하지 않고 명시적 상태값을 반환한다.
- Core는 씬 이름, Transform, Collider, 입력 장치와 realtime 지연을 알지 못한다.
- 방문·클리어 조회 결과는 호출자가 변경할 수 없는 snapshot이다.

## 이번 범위

- Core 탐색 상태와 방향 조회.
- 시작·안전·전투·보스방 잠금과 재방문 규칙의 EditMode 테스트.
- 그래프 전체를 방문하고 다시 시작방으로 돌아올 수 있는 결정론적 회귀 테스트.

## 이번 범위 밖

- 수제 room asset의 그래프 노드 배정과 회전.
- 실제 문, 출구 trigger, 씬 로드와 전환 연출.
- 시작방·폭탄 보상·보스 전실·보스방 placeholder 콘텐츠.
- 플레이어 체력·폭탄 슬롯·파괴 상태의 방 간 보존과 저장/재시작.

## 확인된 콘텐츠 선행 조건

현재 네 수제 전투방은 모두 서로 마주 보는 출구 두 개만 가진다. 생성 그래프에는 꺾인 2방향 노드와 3방향 분기 노드가 생길 수 있으므로 회전만으로는 모든 seed를 배정할 수 없다. 후속 배정 작업은 다음 중 하나를 명시적으로 선택하고 validator로 증명해야 한다.

1. 코너·3방향 출구를 지원하는 room variant를 추가한다.
2. 현재 room asset이 사용 가능한 출구 방향을 늘리고 실제 경계 표현·닫힌 미사용 문까지 함께 저작한다.
3. 생성기를 현재 콘텐츠 출구 집합에 맞춘 제약 생성으로 버전업한다.

그래프 방향과 물리 출구 방향이 다른 임의 매핑은 이동·미니맵 가독성을 깨므로 채택하지 않는다.

## 검증 기준

- `DungeonRunStateTests` 전체 통과.
- 기존 `DungeonGeneratorTests` 및 전체 EditMode 회귀 통과.
- Core의 UnityEngine·씬·입력·realtime 의존 없음.
- `Tools/Verify.ps1 -StaticOnly` 통과.
- Unity 연결 상태에서 컴파일 오류와 Console Error 0.

## 롤백

`DungeonRunState`, `DungeonGraph` 방향 조회, 전용 테스트와 이 문서를 한 묶음으로 되돌린다. 기존 생성 버전과 seed별 그래프 결과는 변경하지 않는다.
