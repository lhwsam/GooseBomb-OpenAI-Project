# 한 층 던전 그래프 생성

- 상태: 결정론적 Core 그래프·탐색·Secret 공개 상태 `Accepted`, Unity 콘텐츠 배정·씬 연결·정확한 분포와 보상 수치 `Proposed`
- 설계 원본: `GDD_v0.2.md` 19~21장, `ProtoType_v0.2.md` 가설 E
- 코드 소유: 그래프·배정은 `BombSwap.Core`, 저작 변환은 `BombSwap.Authoring`, 실제 프리팹·문 배치는 `BombSwap.Unity`

## 목적

적은 수의 수제 방으로도 매 run 탐색 순서와 선택이 달라지게 하되, 보스까지의 최소 흐름과 첫 폭탄 보상을 보장한다.

## 프로토타입 계약

- run은 명시적 seed를 가진다.
- Unity host는 Editor와 Development build에서는 저작 seed를 그대로 사용해 자동화와 버그 재현을 보존한다. 정식 non-Development build에서는 새 run을 시작하거나 terminal run을 재시작할 때마다 현재 시각·플랫폼 tick·host-local sequence를 혼합한 0이 아닌 seed를 만들고, 그 정수 하나를 Core에 명시적으로 전달한다.
- 한 층의 normal progression은 트리형 방 그래프로 생성한다. 이후 비밀방 하나가 일반 전투방 2~3개를 잇는 명시적 Secret 연결을 추가할 수 있다.
- GDD의 전투방 범위는 run당 3~5개다. 현재 테스트 4 기본 정의는 보스 주 경로 전투방 3개와 선택 가지를 동시에 보장하기 위해 4~5개를 생성한다. 이 수치는 플레이테스트 전 `Proposed`다.
- 첫 전투 진행에서 두 번째 폭탄 획득을 보장한다.
- 보스 전실을 거쳐 보스방에 들어간다.
- 보스까지 최소 약 3개의 전투방을 거치는 경로를 초기 가설로 둔다.
- 실제 방 geometry는 수제 프리팹에서 선택한다.

## 구현된 Core 원본

- `DungeonGenerationDefinition`은 최소/최대 전투방 수와 보스 주 경로의 전투방 수를 소유한다. `CreatePrototype()`은 `4~5 / 주 경로 3`을 반환하며 현재 프로토타입 상한 5를 넘는 정의를 거부한다.
- `DungeonGenerator.Generate(seed, definition)`은 모든 `int` seed를 받고 `prototype-secret-v3` 결과를 만든다. normal tree를 완성한 뒤 현재 좌표에서 Secret 후보를 결정론적으로 선택하며 시스템 시간, 호출 횟수, `System.Random` 또는 `UnityEngine.Random`을 읽지 않는다.
- `DeterministicSeedRandom`은 고정 32비트 seed 혼합, LCG 진행과 상위 비트 곱셈 범위 변환을 사용한다. 연속 seed의 낮은 비트 상관이 방향 선택을 몇 패턴으로 고정하지 않게 하면서 플랫폼별 부동소수점 연산을 피한다.
- `DungeonGraph`는 seed, 생성 버전, 정의, 순서가 고정된 `DungeonRoomNode`·`DungeonRoomConnection` snapshot을 소유한다. 연결은 `Normal` 또는 `Secret` kind를 가지며 기존 생성 흐름과 최단 경로는 normal subgraph를 기준으로 보존된다. 각 노드는 양수 `DungeonRoomNodeId`, `RoomType`, 고유 `RoomGraphPosition`을 가진다.
- 현재 `RoomType`은 기존 직렬화 값 `Combat = 0`, `Start = 1`, `BombReward = 2`, `BossAntechamber = 3`, `Boss = 4`, `Recovery = 5`를 보존하고 끝에 `Secret = 6`을 추가한다. 여섯 특수방 타입은 실제 씬으로 저작됐으며 보상방은 첫 폭탄 선택을, 회복방은 일회성 `+2` 회복을, 비밀방은 일회성 `ROOM TOKENS +3` cache를 제공한다.
- 조회 API는 방, 정렬된 이웃, 최단 경로와 거리를 제공한다. 반환 컬렉션은 read-only이며 호출자가 생성 결과를 변경할 수 없다.
- `DungeonGraph`는 연결된 두 노드의 XZ 좌표에서 `RoomExitDirection`을 계산하고, 현재 방과 방향으로 이웃을 조회한다. 연결되지 않은 노드나 정의되지 않은 방향은 상태를 만들지 않는다.
- `DungeonRunState`는 시작방부터 현재·직전 방, 방문·클리어, Secret 연결별 공개와 Secret cache 소비를 소유한다. 일반 전투방과 보스방은 첫 입장 뒤 클리어 전까지 퇴실을 막고, 클리어한 전투방은 재방문 때 다시 잠그지 않는다. 일반 전투방 최초 클리어는 현재 런의 `RoomRewardTokenCount`를 1 올리고 Secret cache는 한 번만 3을 올린다. `CombatRewardTokenCount`는 기존 호출부를 위한 동일 합계 alias다.
- `DungeonRunState.CreateMinimapSnapshot()`은 방문 방, 방문 방의 직접 인접 방과 적어도 한쪽 끝을 방문한 공개 연결만 read-only로 복사한다. 미공개 Secret 방·연결은 숨기고, 한 Secret 연결을 공개하면 해당 방과 그 연결만 보인다. 현재·방문 방에는 첫 입장부터 알려진 `RoomType`을 제공하지만 발견만 한 방에는 종류를 제공하지 않으며 그 너머 연결도 숨긴다. 반환 순서는 graph 순서를 따른다.
- 현재 방의 문 조회는 북·동·남·서 고정 순서로 연결 없음 `Inactive`, 미공개 Secret 연결 `SecretWall`, 미클리어 전투·보스방 연결 `Locked`, 이동 가능한 연결 `Open`을 반환한다. Secret 공개는 전투방 클리어 잠금을 우회하지 않는다. 연결 상태는 대상 방 ID를 포함하며 read-only snapshot이다.
- `DungeonCombatRoomAssigner`는 그래프 seed에서 topology와 분리된 고정 salt RNG를 만들고, 안정 ID로 정렬한 전투방 카탈로그를 `prototype-combat-assignment-v1`로 배정한다.
- `DungeonCombatRoomLayout`은 모든 전투 노드의 room definition ID, 0/90/180/270도 시계 방향 회전과 그래프가 요구하는 활성 출구를 read-only snapshot으로 소유한다.
- 호환성은 회전된 잠재 출구가 노드의 모든 연결 방향을 포함하는지로 판단한다. 후보 중 사용 횟수가 가장 적은 정의를 우선해 다섯 방을 한 번씩 쓰기 전 불필요한 중복을 막는다. 전투 노드가 다섯 개인 그래프에서는 현재 다섯 정의를 각각 정확히 한 번 사용한다.
- Unity `PrototypeDungeonRunSession`은 검증된 전투방 카탈로그를 Core 정의로 변환해 그래프·배정·탐색 상태를 조합하고, 전투 노드의 definition ID를 실제 room asset·씬 이름으로 해석한다.
- 현재 메인 카탈로그는 `Loop`, `Thrower`, `Pillars`, `Armor`, `Gates` 다섯 정의다. 기존 `Lanes`는 삭제하지 않고 독립 테스트 자산으로 보존하지만 배정 후보와 enabled Build Settings에서는 제외한다. 따라서 run당 전투방 4~5개와 카탈로그 크기 5 계약은 변하지 않는다.
- `PrototypeDungeonSpecialRoomCatalogAsset`이 시작·폭탄 보상·보스 전실·회복·비밀·보스 타입의 서로 다른 씬 이름을 제공하며, 실제 catalog asset과 여섯 특수방 씬을 통해 run session이 모든 노드를 씬으로 해석한다.
- `PrototypeDungeonRunNavigator`는 씬 이름과 로드 가능성을 검증한 pending 전환을 소유하고, 기대한 씬 완료 뒤에만 `DungeonRunState.TryTravel`을 호출한다. `PrototypeDungeonRunHost`는 이 상태만 방 씬 밖에 유지한다.
- `DungeonBombLoadoutState`는 한 종류로 시작하는 run loadout, 첫 보상 후보·선택과 현재 활성 슬롯을 소유한다. Unity host와 room binder는 이 상태를 방 로컬 `PrototypeGameSession`에 주입하고 성공한 교체 사건을 다시 Core 상태에 반영해 scene 전환 뒤에도 장착 정의와 활성 슬롯을 유지한다.
- `DungeonPlayerHealthState`는 run의 최대·현재 체력을 소유한다. persistent host가 검증된 player-vitals 데이터로 새 상태를 만들고 room binder가 새 방 session을 현재 체력으로 초기화하며, 적용된 피해를 즉시 되돌려 기록한다. 이동과 scene load는 체력을 바꾸지 않고 새 run만 최대 체력으로 시작한다.
- `DungeonRunState`는 정확히 한 개인 Recovery 노드의 소비 여부를 run 수명으로 소유한다. 현재 Recovery 노드에서 최대 체력이 아닐 때만 회복과 소비를 함께 확정하며 재입장이나 scene 재로드로 다시 생성하지 않는다.
- `DungeonRunState`는 Secret 연결별 공개와 Secret 노드의 cache 소비를 run 수명으로 소유한다. Unity binder가 매핑한 문 앞 출구 셀이 실제 폭발 `AffectedCells`에 포함됐을 때만 해당 연결을 공개하고, 현재 Secret 방에서만 양수 cache 토큰을 한 번 지급한다.
- Unity room binder는 Core 토큰 값의 변경 사건만 HUD에 전달한다. `PrototypeHealthHud`는 토큰 아이콘 옆에 접두 문구 없는 숫자 snapshot을 표시하며 frame polling이나 별도 보상 상태를 만들지 않는다.
- `DungeonRunState`는 `InProgress`, 보스방 클리어의 `Completed`, 플레이어 사망의 `Failed` 결과를 소유한다. terminal 상태는 이동·추가 클리어를 거부한다. persistent host는 완료 또는 실패와 pending 전환 없음이 확인된 뒤 Editor/Development에서는 같은 저작 seed, 정식 build에서는 새 runtime seed와 같은 catalog로 새 session·navigator를 만들고 시작 씬을 다시 로드한다. 세부 계약은 `RunCompletion.md`가 소유한다.

현재 필수 주 경로는 다음과 같다.

```text
Main:              Start → Combat → BombReward → Combat → Final Combat → BossAntechamber → Boss
Optional combat:                              └─ Combat (→ 선택적 두 번째 Combat)
Recovery detour:                                               Final Combat → Recovery (leaf)
Secret post-pass:             Combat ─┐
                                    Secret (Combat 이웃 2~3개, 입구별 독립 공개)
                               Combat ─┘
```

선택 전투 가지의 접속 지점과 4/5 전투방 수, 정수 방 좌표는 seed로 달라진다. 선택 전투 가지는 폭탄 보상 이후에만 도달하며 보스 주 경로에는 포함되지 않는다. Recovery는 seed와 무관하게 보스 주 경로의 마지막 일반 전투방에 붙는 단일 막다른 leaf이며, 보스 전실로 가기 전 우회 선택을 만든다. Secret은 normal 그래프 배치 뒤 Combat 2~3개와 맞닿는 빈 좌표가 있을 때 하나만 추가되고 후보가 없으면 생략된다.

## 생성 단계

1. seed를 고정 정수 혼합기로 분산하고 결정적 RNG를 만든다.
2. 정의 범위에서 목표 전투방 수와 선택 가지 접속 지점을 고른다.
3. 시작→첫 전투→보상→나머지 주 경로 전투→보스 전실→보스의 부모 관계를 만든다.
4. 보상 이후 주 경로 노드에 남은 전투방 1~2개의 단일 선택 가지를 붙인다.
5. 보스 주 경로의 마지막 일반 전투방에 Recovery leaf를 붙인다.
6. 노드별 고정 방향 후보 순서로 정수 XZ 좌표를 유한 backtracking 배치한다. normal 연결이 없는 방의 cardinal 인접은 거부해 암시적 문과 루프를 막는다.
7. normal 배치 뒤 비어 있는 좌표 중 다른 비-Combat 방과 맞닿지 않고 Combat 2~3개와 맞닿는 후보를 수집한다. 3개 인접을 먼저, 그다음 X·Z 오름차순으로 하나를 선택해 Secret 노드와 명시적 Secret 연결을 추가한다. 후보가 없으면 추가하지 않는다.
8. normal tree, 연결 수, 좌표, 필수 노드, 첫 보상, 보스 경로, 선택 전투 가지, Recovery leaf와 Secret의 Combat 2~3연결을 `DungeonGraph` 생성 시 다시 검증한다.
9. Core `DungeonRunState`가 인접 이동, 최초 방문, 전투 잠금·클리어, Secret 공개와 양방향 재방문을 처리한다.
10. 전투 노드별 활성 출구와 호환되는 수제 방 정의·회전을 결정적으로 배정한다.
11. Unity 런 카탈로그가 배정된 definition ID와 특수 타입을 실제 room asset·씬 이름으로 해석한다.
12. Core 탐색 상태가 그래프 연결·클리어·Secret 공개 여부에서 네 방향 문의 `Inactive`·`SecretWall`·`Locked`·`Open` snapshot을 계산한다.
13. Unity binder가 미공개 Secret 연결의 문 앞 출구 `Floor` 셀을 방향에 매핑하고 실제 `BombExplosion.AffectedCells`가 그 셀에 닿으면 해당 연결만 공개한다. `DestroyedWalls`와 일반 파괴벽 전파 규칙은 사용하지 않는다.
14. Unity navigator가 대상 콘텐츠·씬을 검증하고 실제 로드 완료 뒤 Core 이동을 단일 commit한다.
15. room commit 또는 Secret 공개 뒤 미니맵 presenter가 Core snapshot을 읽어 현재·방문·직접 인접 공개 방과 확인된 연결만 우측 상단에 다시 그린다. 방문 방 종류는 아이콘으로 표시하고 미방문 frontier는 물음표 아이콘으로 유지한다.
16. Unity room binder와 door presenter가 배정 결과와 문 상태에 맞춰 회전된 room geometry, 금 간 벽과 활성·비활성 문을 표현한다.
17. 첫 `BombReward` 진입에서 논리 셀 후보 선택을 run loadout에 기록하고, 성공한 슬롯 교체와 함께 이후 방 session에 다시 주입한다.
18. 방 session은 run 현재 체력으로 시작하고 적용된 피해 결과를 같은 run 상태에 즉시 기록한다.
19. Recovery 중앙 셀은 유효할 때만 `+2`, Secret 중앙 cache는 최초 한 번만 토큰 `+3`과 소비를 함께 확정한다.
20. 일반 전투방 최초 클리어에서 run token을 1 지급하고 HUD에 `RoomRewardTokenCount` 확정 값을 전달한다.
21. 보스방 클리어를 run 완료로 판정하고 플레이어가 요청하면 빌드 종류에 맞는 다음 명시 seed로 토큰 0·최대 체력·미소비 Recovery/Secret cache·미공개 Secret 연결·초기 미니맵 범위의 새 run state를 만들고 시작 씬을 다시 로드한다.

## 불변식

- 모든 필수 노드는 시작방에서 도달 가능하다.
- 보스방은 보스 전실을 통해서만 진입한다.
- 첫 폭탄 보상 보장이 누락되지 않는다.
- 동일한 콘텐츠 버전과 seed는 동일한 논리 그래프를 만든다.
- 생성 실패를 무한 재시도로 숨기지 않는다.
- 보스는 보스 전실 한 곳하고만 연결되고 보스 전실은 이전 주 경로와 보스를 잇는 두 연결만 가진다.
- 첫 폭탄 보상은 시작에서 정확히 `Start → Combat → BombReward` 두 edge 뒤이며 모든 보스 경로에 포함된다.
- 보스 주 경로 밖 전투방이 최소 하나 존재하고 그 경로도 폭탄 보상을 지난다.
- 모든 cardinal 좌표 인접은 `Normal` 또는 `Secret` 연결로 명시된다. normal 연결만 보면 기존 progression은 tree이고 Secret만 2~3개의 Combat을 잇는다.
- 노드 ID는 1부터 연속이고 연결·이웃 순서는 안정적이다.
- 미니맵에는 방문 방과 그 직접 이웃 중 공개된 연결만 보인다. 미공개 Secret 방·연결은 보이지 않고 한 입구를 공개해도 다른 Secret 입구는 독립적으로 숨는다. 현재 방은 정확히 하나이고 새 run은 시작방과 첫 이웃·한 normal 연결만 공개한다. 방문 방만 종류를 노출하며 발견 상태의 방은 실제 종류와 무관하게 종류 정보가 없다.
- 생성 알고리즘 변경 시 버전을 함께 바꾸며 seed 0 golden snapshot을 조용히 변경하지 않는다.
- 안전방은 클리어 상태를 만들지 않으며 일반 전투방과 보스방만 클리어할 수 있다.
- 전투 보상 토큰은 일반 전투방 하나당 최초 클리어에서만 정확히 1 증가하며 보스·안전방·terminal 클리어 요청은 값을 바꾸지 않는다.
- 현재 전투방을 클리어하기 전에는 연결된 방으로도 나갈 수 없고, 실패한 이동은 현재·직전·방문 상태를 바꾸지 않는다.
- 클리어한 방은 어느 연결 방향에서 재진입해도 잠기지 않는다.
- 같은 graph·catalog는 catalog 입력 배열 순서와 무관하게 같은 콘텐츠 배정을 만든다.
- 모든 전투 노드는 정확히 한 번 배정되고 모든 활성 출구는 회전된 저작 잠재 출구에 존재한다.
- 호환 가능한 room asset이 없으면 다른 방향 문에 임의 연결하거나 무한 재시도하지 않는다.
- 보스방 도착만으로 완료되지 않으며 보스방 클리어 뒤에만 새 run을 만들 수 있다.
- 방 이동과 재입장은 플레이어 현재 체력을 회복하거나 감소시키지 않는다. 새 run 생성만 검증된 최대 체력을 다시 적용한다.
- Recovery는 정확히 하나이며 보스 주 경로 마지막 일반 전투방의 leaf다. 안전방이므로 잠금·클리어·전투 보상 토큰을 만들지 않는다.
- Recovery 소비는 실제 체력이 증가한 한 번에만 기록되고 최대 체력·사망·terminal·다른 방의 요청은 상태를 바꾸지 않는다.
- Secret은 존재하면 정확히 하나이고 Combat 2~3개에만 연결되며 보스·안전방과 직접 연결되지 않는다. 안전방이므로 전투 잠금과 클리어를 만들지 않는다.
- Secret 연결은 실제 폭발 footprint가 방별 문 앞 출구 셀에 도달한 결과로 하나씩만 공개되고 미공개 연결 이동은 `BlockedBySecretWall`이다. 출구 셀은 `Floor`로 유지되며 비밀문은 파괴벽 결과에 포함되지 않는다. cache `+3`은 현재 Secret 방에서 한 번만 지급되며 terminal·다른 방·재입장 요청은 합계를 바꾸지 않는다.

## 자동 테스트

- 같은 정의·동일 seed 재현과 `prototype-secret-v3` seed 0 golden snapshot.
- 0, 음수, `int.MinValue`·`int.MaxValue` seed.
- 512개 연속 seed의 normal 연결 트리, 고유 좌표, 모든 cardinal 인접의 명시 연결, Secret 후보 우선순위와 Combat 2~3연결.
- 기본 4~5 전투방 두 값 출현과 64개 초과 topology/layout signature.
- 필수 노드 한 개씩, 첫 전투 뒤 보상, 보스 주 경로 전투방 3개와 보상 포함, 보스 전실 단일 진입.
- 주 경로 밖 선택 전투 가지와 막다른 끝, 보상 이후 접근.
- 모든 seed의 단일 Recovery leaf, 마지막 주 경로 Combat 부모, 보스 필수 경로 제외와 안전방 문 상태.
- 잘못된 정의, null 정의, 유효하지 않거나 범위 밖 노드 조회, read-only snapshot.
- 모든 연결의 양방향 출구가 서로 반대 방향인지, 없는 방향과 비연결 이동이 상태를 바꾸지 않는지 검증.
- 첫 전투 잠금, 안전방 비잠금, 클리어 중복, 일반 전투 최초 토큰 지급과 안전·보스·terminal 비지급, 클리어 전 퇴실 차단, 클리어 뒤 양방향 재방문과 전체 트리 왕복.
- 카탈로그 순서 무관 배정 재현, 128개 seed 다양성, 사용 횟수 균형, 5전투 노드·5정의의 각 1회 사용, 회전 방향과 활성 출구 호환, 부족한 카탈로그의 명시 실패.
- Recovery의 상한 회복·최대 체력 비소비·단일 소비·재입장 유지와 다른 방·사망·terminal 거부.
- Secret 연결별 숨김·공개·이동 차단/허용, 문 앞 `Floor` 접근, 실제 폭발 영향과 파괴벽 비포함, minimap 제외/추가, cache `+3` 단일 소비·재입장 유지·다른 방·terminal 거부와 새 run 초기화.

실제 문 GameObject, room 회전·씬 로드·탐색, 첫 폭탄 보상, Recovery, 비밀방과 제한 정보 미니맵은 Unity runtime/PlayMode에 연결됐다. seed-0 WebGL 경로는 2번 전투방의 금 간 서쪽 벽을 실제 폭발로 열고 미니맵 `4방/3연결`, 10번 비밀방 cache `ROOM TOKENS +3`, 양방향 복귀를 확인했다. 이후 선택한 loadout과 체력을 유지한 채 Recovery에서 `1→3` 회복하고, 미니맵이 보스 전실 `10방/9연결`까지 확장된 뒤 2페이즈 보스 격파·한 층 완료·새 run 초기 지도로 재시작했다. 다음 범위는 금 간 단서가 무작위 벽 폭파를 유발하지 않는지, cache 가치와 기존 탐색 피로를 함께 보는 사람 플레이테스트다.
