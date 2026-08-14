# 한 층 던전 그래프 생성

- 상태: 결정론적 Core 그래프·탐색 상태 `Accepted`, Unity 콘텐츠 배정·씬 연결·정확한 분포 `Proposed`
- 설계 원본: `GDD_v0.2.md` 19~21장, `ProtoType_v0.2.md` 가설 E
- 코드 소유: 그래프·배정은 `BombSwap.Core`, 저작 변환은 `BombSwap.Authoring`, 실제 프리팹·문 배치는 `BombSwap.Unity`

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
- 현재 `RoomType`은 기존 직렬화 값 `Combat = 0`을 보존하고 `Start`, `BombReward`, `BossAntechamber`, `Boss`를 추가한다. 네 특수방은 실제 씬으로 저작됐으며 보상방은 첫 폭탄 선택을 제공하고 보스 전실·보스방의 전투 콘텐츠는 placeholder다.
- 조회 API는 방, 정렬된 이웃, 최단 경로와 거리를 제공한다. 반환 컬렉션은 read-only이며 호출자가 생성 결과를 변경할 수 없다.
- `DungeonGraph`는 연결된 두 노드의 XZ 좌표에서 `RoomExitDirection`을 계산하고, 현재 방과 방향으로 이웃을 조회한다. 연결되지 않은 노드나 정의되지 않은 방향은 상태를 만들지 않는다.
- `DungeonRunState`는 시작방부터 현재·직전 방, 방문과 클리어 상태를 소유한다. 일반 전투방과 보스방은 첫 입장 뒤 클리어 전까지 퇴실을 막고, 클리어한 전투방은 재방문 때 다시 잠그지 않는다. 일반 전투방 최초 클리어는 현재 런의 `CombatRewardTokenCount`를 1 올리며 중복 클리어·안전방·보스방은 토큰을 지급하지 않는다.
- 현재 방의 문 조회는 북·동·남·서 고정 순서로 연결 없음 `Inactive`, 미클리어 전투·보스방 연결 `Locked`, 이동 가능한 연결 `Open`을 반환한다. 연결 상태는 대상 방 ID를 포함하며 read-only snapshot이다.
- `DungeonCombatRoomAssigner`는 그래프 seed에서 topology와 분리된 고정 salt RNG를 만들고, 안정 ID로 정렬한 전투방 카탈로그를 `prototype-combat-assignment-v1`로 배정한다.
- `DungeonCombatRoomLayout`은 모든 전투 노드의 room definition ID, 0/90/180/270도 시계 방향 회전과 그래프가 요구하는 활성 출구를 read-only snapshot으로 소유한다.
- 호환성은 회전된 잠재 출구가 노드의 모든 연결 방향을 포함하는지로 판단한다. 후보 중 사용 횟수가 가장 적은 정의를 우선해 다섯 방을 한 번씩 쓰기 전 불필요한 중복을 막는다. 전투 노드가 다섯 개인 그래프에서는 현재 다섯 정의를 각각 정확히 한 번 사용한다.
- Unity `PrototypeDungeonRunSession`은 검증된 전투방 카탈로그를 Core 정의로 변환해 그래프·배정·탐색 상태를 조합하고, 전투 노드의 definition ID를 실제 room asset·씬 이름으로 해석한다.
- `PrototypeDungeonSpecialRoomCatalogAsset`이 시작·폭탄 보상·보스 전실·보스 타입의 서로 다른 씬 이름을 제공하며, 실제 catalog asset과 네 특수방 씬을 통해 run session이 모든 노드를 씬으로 해석한다.
- `PrototypeDungeonRunNavigator`는 씬 이름과 로드 가능성을 검증한 pending 전환을 소유하고, 기대한 씬 완료 뒤에만 `DungeonRunState.TryTravel`을 호출한다. `PrototypeDungeonRunHost`는 이 상태만 방 씬 밖에 유지한다.
- `DungeonBombLoadoutState`는 한 종류로 시작하는 run loadout과 첫 보상 후보·선택을 소유한다. Unity host와 room binder는 이 상태를 방 로컬 `PrototypeGameSession`에 주입해 scene 전환 뒤에도 선택한 2번 슬롯을 유지한다.
- Unity room binder는 Core 토큰 값의 변경 사건만 HUD에 전달한다. `PrototypeHealthHud`는 우상단 `ROOM TOKENS` snapshot을 표시하며 frame polling이나 별도 보상 상태를 만들지 않는다.
- `DungeonRunState`는 `InProgress`, 보스방 클리어의 `Completed`, 플레이어 사망의 `Failed` 결과를 소유한다. terminal 상태는 이동·추가 클리어를 거부한다. persistent host는 완료 또는 실패와 pending 전환 없음이 확인된 뒤 같은 seed·catalog에서 새 session과 navigator를 만들고 시작 씬을 다시 로드한다. 세부 계약은 `RunCompletion.md`가 소유한다.

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
7. Core `DungeonRunState`가 인접 이동, 최초 방문, 전투 잠금·클리어와 양방향 재방문을 처리한다.
8. 전투 노드별 활성 출구와 호환되는 수제 방 정의·회전을 결정적으로 배정한다.
9. Unity 런 카탈로그가 배정된 definition ID를 실제 room asset·씬 이름으로 해석한다.
10. Core 탐색 상태가 그래프 연결과 클리어 여부에서 네 방향 문의 비활성·잠금·개방 snapshot을 계산한다.
11. Unity navigator가 대상 콘텐츠·씬을 검증하고 실제 로드 완료 뒤 Core 이동을 단일 commit한다.
12. Unity room binder와 door presenter가 배정 결과와 문 상태에 맞춰 회전된 room geometry와 활성·비활성 문을 표현한다.
13. 첫 `BombReward` 진입에서 논리 셀 후보 선택을 run loadout에 기록하고 이후 방 session에 다시 주입한다.
14. 일반 전투방 최초 클리어에서 Core run 토큰을 1 지급하고 HUD에 확정 값을 전달한다.
15. 보스방 클리어를 run 완료로 판정하고 플레이어가 요청하면 토큰 0의 새 run state로 시작 씬을 다시 로드한다.

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
- 안전방은 클리어 상태를 만들지 않으며 일반 전투방과 보스방만 클리어할 수 있다.
- 전투 보상 토큰은 일반 전투방 하나당 최초 클리어에서만 정확히 1 증가하며 보스·안전방·terminal 클리어 요청은 값을 바꾸지 않는다.
- 현재 전투방을 클리어하기 전에는 연결된 방으로도 나갈 수 없고, 실패한 이동은 현재·직전·방문 상태를 바꾸지 않는다.
- 클리어한 방은 어느 연결 방향에서 재진입해도 잠기지 않는다.
- 같은 graph·catalog는 catalog 입력 배열 순서와 무관하게 같은 콘텐츠 배정을 만든다.
- 모든 전투 노드는 정확히 한 번 배정되고 모든 활성 출구는 회전된 저작 잠재 출구에 존재한다.
- 호환 가능한 room asset이 없으면 다른 방향 문에 임의 연결하거나 무한 재시도하지 않는다.
- 보스방 도착만으로 완료되지 않으며 보스방 클리어 뒤에만 새 run을 만들 수 있다.

## 자동 테스트

- 같은 정의·동일 seed 재현과 `prototype-tree-v1` seed 0 golden snapshot.
- 0, 음수, `int.MinValue`·`int.MaxValue` seed.
- 512개 연속 seed의 연결 트리, 고유 좌표, 연결/cardinal 인접 일치, 암시적 루프 없음.
- 기본 4~5 전투방 두 값 출현과 64개 초과 topology/layout signature.
- 필수 노드 한 개씩, 첫 전투 뒤 보상, 보스 주 경로 전투방 3개와 보상 포함, 보스 전실 단일 진입.
- 주 경로 밖 선택 전투 가지와 막다른 끝, 보상 이후 접근.
- 잘못된 정의, null 정의, 유효하지 않거나 범위 밖 노드 조회, read-only snapshot.
- 모든 연결의 양방향 출구가 서로 반대 방향인지, 없는 방향과 비연결 이동이 상태를 바꾸지 않는지 검증.
- 첫 전투 잠금, 안전방 비잠금, 클리어 중복, 일반 전투 최초 토큰 지급과 안전·보스·terminal 비지급, 클리어 전 퇴실 차단, 클리어 뒤 양방향 재방문과 전체 트리 왕복.
- 카탈로그 순서 무관 배정 재현, 128개 seed 다양성, 사용 횟수 균형, 5전투 노드·5정의의 각 1회 사용, 회전 방향과 활성 출구 호환, 부족한 카탈로그의 명시 실패.

실제 문 GameObject, room 회전·씬 로드·탐색과 첫 폭탄 보상은 Unity runtime/PlayMode에 연결됐다. seed-0 전체 주 경로는 선택한 loadout을 유지한 채 보스 전실·2페이즈 보스 격파·한 층 완료·새 run 재시작까지 Development WebGL 자동 검증을 통과했다. 같은 세션에서 새 안전방 자기 폭발 사망·실패 결과·두 번째 새 run 재시작도 검증했다. 다음 범위는 선택 가지, 되돌아가기 피로와 보스·결과 흐름을 포함한 사람 플레이테스트다.
