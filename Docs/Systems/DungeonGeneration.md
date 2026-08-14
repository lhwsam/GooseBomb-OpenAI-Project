# 한 층 던전 그래프 생성

- 상태: 결정론적 Core 그래프 `Accepted`, Unity 탐색 연결·정확한 분포 `Proposed`
- 설계 원본: `GDD_v0.2.md` 19~21장, `ProtoType_v0.2.md` 가설 E
- 코드 소유: 그래프는 `BombSwap.Core`, 프리팹 배치는 `BombSwap.Unity`

## 목적

적은 수의 수제 방으로도 매 run 탐색 순서와 선택이 달라지게 하되, 보스까지의 최소 흐름과 첫 폭탄 보상을 보장한다.

## 프로토타입 계약

- run은 명시적 seed를 가진다.
- 한 층은 트리형 방 그래프로 생성한다.
- GDD의 전투방 범위는 run당 3~5개다. 현재 테스트 4 기본 정의는 보스 주 경로 전투방 3개와 선택 가지를 동시에 보장하기 위해 4~5개를 생성한다. 이 수치는 플레이테스트 전 `Proposed`다.
- 첫 전투 진행에서 두 번째 폭탄 획득을 보장한다.
- 보스 전실을 거쳐 보스방에 들어간다.
- 보스까지 최소 약 3개의 전투방을 거치는 경로를 초기 가설로 둔다.
- 실제 방 geometry는 수제 프리팹에서 선택한다.

## 구현된 Core 원본

- `DungeonGenerationDefinition`은 최소/최대 전투방 수와 보스 주 경로의 전투방 수를 소유한다. `CreatePrototype()`은 `4~5 / 주 경로 3`을 반환하며 현재 프로토타입 상한 5를 넘는 정의를 거부한다.
- `DungeonGenerator.Generate(seed, definition)`은 모든 `int` seed를 받고 `prototype-tree-v1` 결과를 만든다. 시스템 시간, 호출 횟수, `System.Random` 또는 `UnityEngine.Random`을 읽지 않는다.
- `DeterministicSeedRandom`은 고정 32비트 seed 혼합, LCG 진행과 상위 비트 곱셈 범위 변환을 사용한다. 연속 seed의 낮은 비트 상관이 방향 선택을 몇 패턴으로 고정하지 않게 하면서 플랫폼별 부동소수점 연산을 피한다.
- `DungeonGraph`는 seed, 생성 버전, 정의, 순서가 고정된 `DungeonRoomNode`·`DungeonRoomConnection` snapshot을 소유한다. 각 노드는 양수 `DungeonRoomNodeId`, `RoomType`, 고유 `RoomGraphPosition`을 가진다.
- 현재 `RoomType`은 기존 직렬화 값 `Combat = 0`을 보존하고 `Start`, `BombReward`, `BossAntechamber`, `Boss`를 추가한다. 특수방의 실제 씬·prefab은 아직 없다.
- 조회 API는 방, 정렬된 이웃, 최단 경로와 거리를 제공한다. 반환 컬렉션은 read-only이며 호출자가 생성 결과를 변경할 수 없다.

현재 필수 주 경로는 다음과 같다.

```text
Start → Combat → BombReward → Combat → Combat → BossAntechamber → Boss
                         └─ Combat (→ 선택적 두 번째 Combat)
```

선택 전투 가지의 접속 지점과 4/5 전투방 수, 정수 방 좌표는 seed로 달라진다. 선택 가지는 폭탄 보상 이후에만 도달하며 보스 주 경로에는 포함되지 않는다.

## 생성 단계

1. seed를 고정 정수 혼합기로 분산하고 결정적 RNG를 만든다.
2. 정의 범위에서 목표 전투방 수와 선택 가지 접속 지점을 고른다.
3. 시작→첫 전투→보상→나머지 주 경로 전투→보스 전실→보스의 부모 관계를 만든다.
4. 보상 이후 주 경로 노드에 남은 전투방 1~2개의 단일 선택 가지를 붙인다.
5. 노드별 고정 방향 후보 순서로 정수 XZ 좌표를 유한 backtracking 배치한다. 연결되지 않은 방의 cardinal 인접은 거부해 암시적 문과 루프를 막는다.
6. 연결 수, 좌표, 필수 노드, 첫 보상, 보스 경로, 선택 가지를 `DungeonGraph` 생성 시 다시 검증한다.
7. 후속 Unity 단계에서 출입구와 호환되는 수제 방 정의를 선택한다. 이 단계는 아직 구현되지 않았다.

## 불변식

- 모든 필수 노드는 시작방에서 도달 가능하다.
- 보스방은 보스 전실을 통해서만 진입한다.
- 첫 폭탄 보상 보장이 누락되지 않는다.
- 동일한 콘텐츠 버전과 seed는 동일한 논리 그래프를 만든다.
- 생성 실패를 무한 재시도로 숨기지 않는다.
- 보스는 보스 전실 한 곳하고만 연결되고 보스 전실은 이전 주 경로와 보스를 잇는 두 연결만 가진다.
- 첫 폭탄 보상은 시작에서 정확히 `Start → Combat → BombReward` 두 edge 뒤이며 모든 보스 경로에 포함된다.
- 보스 주 경로 밖 전투방이 최소 하나 존재하고 그 경로도 폭탄 보상을 지난다.
- 연결된 방만 cardinal 인접하며 연결되지 않은 좌표 인접은 허용하지 않는다.
- 노드 ID는 1부터 연속이고 연결·이웃 순서는 안정적이다.
- 생성 알고리즘 변경 시 버전을 함께 바꾸며 seed 0 golden snapshot을 조용히 변경하지 않는다.

## 자동 테스트

- 같은 정의·동일 seed 재현과 `prototype-tree-v1` seed 0 golden snapshot.
- 0, 음수, `int.MinValue`·`int.MaxValue` seed.
- 512개 연속 seed의 연결 트리, 고유 좌표, 연결/cardinal 인접 일치, 암시적 루프 없음.
- 기본 4~5 전투방 두 값 출현과 64개 초과 topology/layout signature.
- 필수 노드 한 개씩, 첫 전투 뒤 보상, 보스 주 경로 전투방 3개와 보상 포함, 보스 전실 단일 진입.
- 주 경로 밖 선택 전투 가지와 막다른 끝, 보상 이후 접근.
- 잘못된 정의, null 정의, 유효하지 않거나 범위 밖 노드 조회, read-only snapshot.

호환 room asset 부족, 출입구 회전, prefab 중복 선택 분포와 실제 탐색은 후속 Unity authoring/runtime 테스트에서 추가한다.
