# 방 저작과 검증

- 상태: 수제 방 원칙·프로토타입 전투방 스키마 `Accepted`, 확장 스키마 `Proposed`
- 설계 원본: `GDD_v0.2.md` 19, 21~23장
- 코드 소유: `BombSwap.Core`, `BombSwap.Authoring`, `BombSwap.Editor`

## 목적

전투 공간은 사람이 설계하고, 런 구조만 절차적으로 조합해 공정성과 폭탄별 공간 차이를 함께 확보한다. 정수 XZ 논리 셀 데이터가 권위 원본이고 씬 Transform·Collider는 이를 표현한다.

## 권장 프로토타입 범위

- 전투방 프리팹 5~7개.
- 시작방, 일반 전투방, 첫 폭탄 보상, 보스 전실, 선택형 회복방, 금이 간 벽 비밀방, 보스방.
- 보물방과 추가 비밀방 변형은 시간이 남을 때 또는 프로토타입 이후.

## 구현된 프로토타입 전투방 스키마

`PrototypeCombatRoomDefinitionAsset`이 Unity 저작 원본이며 `CombatRoomDefinition`으로 변환될 때 실행 불가능한 데이터를 거부한다. 현재 스키마는 다음을 소유한다.

- 안정적인 room ID와 room type.
- 홀수 격자 너비·깊이와 양수 셀 크기.
- 출입구 위치와 방향.
- 플레이어와 필수 추격자 spawn, 선택적 돌진형·갑옷 적·자폭병 spawn.
- 자폭병이 있을 때 순서가 안정적인 자폭 유도 anchor 목록. 이 목록은 AI 경로 목적지가 아니라 레벨 설계·검증·플레이테스트가 의도한 폭발 위치와 결과를 공유하는 메타데이터다.
- 고정 벽 셀.
- 파괴 가능 벽 셀.
- 플레이어 입장 안전 셀.
- 최소 두 개의 퇴로 anchor.
- 순서가 있는 닫힌 폭탄 유도 순환 경로.

범용 적 spawn 목록과 제약, 일반화된 보상·전환 anchor는 아직 구현하지 않았다. 첫 폭탄 보상만 안전방 shell의 보행 가능한 `(-1,0)`·`(1,0)` 논리 셀을 고정 후보 위치로 사용하고 presenter가 초기화 때 Floor 여부를 검증한다. 첫 구현은 독립 방 prefab 대신 이 데이터와 `TestSandbox` 씬 표현을 연결한다. 향후 prefab으로 분리해도 논리 셀 데이터가 권위 원본이라는 경계는 유지한다.

## 현재 수제 전투방 세트

다섯 방 모두 11×9, 셀 크기 1의 `Combat` 방이며 추격자를 사용한다. 세 번째 방은 선택적 돌진형, 네 번째 방은 선택적 갑옷 적, 다섯 번째 방은 선택적 자폭병을 함께 사용한다. 네 번째 방은 장갑병의 반경 수비와 좌우 panic run을 읽기 위한 T 교차점이고, 다섯 번째 방은 자폭 위치에 따라 중앙 문 중 한쪽만 먼저 열리는 비대칭 경로 선택을 검증한다.

| 순서 | ID / 자산 | spawn | 공간 의도 | 씬 / 다음 씬 |
|---:|---|---|---|---|
| 1 | `prototype-combat-loop` / `PrototypeCombatLoop.asset` | 플레이어 `(0, 0)`, 추격자 `(1, -1)` | 중앙 십자 고정 벽 4개, 파괴 벽 없음, 기존 전투 기준선 | `TestSandbox.unity` / `TestSandboxLanes` |
| 2 | `prototype-combat-lanes` / `PrototypeCombatLanes.asset` | 플레이어 `(0, -2)`, 추격자 `(0, 2)` | 세로 고정 벽 6개, 파괴 벽 `(-1,-1)·(1,-1)`: spawn 광역은 둘을 대각선 동시 파괴, 십자는 미도달 | `TestSandboxLanes.unity` / `TestSandboxPillars` |
| 3 | `prototype-combat-pillars` / `PrototypeCombatPillars.asset` | 플레이어 `(-3, -2)`, 추격자 `(3, 2)`, 돌진형 `(0, 1)` | 차선 종단 기둥 7개, 동쪽 파괴 종단 `(2,-2)`, 중앙 3×3 외곽 loop: 돌진형이 남쪽 정렬 셀을 획득한 뒤 서쪽 짧은 차선을 예고하며 플레이어는 북/동 포켓으로 이탈 | `TestSandboxPillars.unity` / `TestSandboxArmor` |
| 4 | `prototype-combat-armor` / `PrototypeCombatArmor.asset` | 플레이어 `(0, -2)`, 추격자 `(4, 4)`, 갑옷 적 `(0, 1)` | 남쪽 통로 벽 `x=±2,z=-2..-1`, 상단 막 `x=-1..1,z=2`, 좌우 종단 `(-4,0)·(4,0)`의 T 교차점, 파괴 벽 없음: 반경 수비→첫 피격 반대편 3칸 예고·질주→두 번째 선행 설치 | `TestSandboxArmor.unity` / `TestSandboxGates` |
| 5 | `prototype-combat-gates` / `PrototypeCombatGates.asset` | 플레이어 `(0, -3)`, 추격자 `(0, 3)`, 자폭병 `(3,0)` | `z=-1·1`의 `x=-2,-1,1,2` 고정 장벽 8개와 중앙 파괴 문 `(0,-1)·(0,1)`, 자폭 유도 anchor `(0,-2)→(0,2)`: 플레이어가 추적형 자폭병을 어느 쪽으로 끄느냐에 따라 범위 1 자폭이 아래/위 문 한쪽만 먼저 열고 `x=±3` 우회는 유지 | `TestSandboxGates.unity` / 없음 |

다섯 방은 모두 북 `(0,4)`, 동 `(5,0)`, 남 `(0,-4)`, 서 `(-5,0)` 중앙 경계의 잠재 출구를 갖는다. 잠재 출구는 항상 열린 문이 아니라 room geometry가 지원하는 후보이며, run 그래프가 필요한 방향만 활성 문으로 선택한다. 각 방은 서로 다른 첫 cardinal 이동을 쓰는 퇴로 anchor 두 개와 닫힌 cardinal 유도 순환 경로를 소유한다. 실제 문 GameObject는 [ADR-0007](../ADR/0007-Potential-Room-Exits.md)에 따라 run 활성 부분집합을 `Inactive`·`Locked`·`Open`·`SecretWall`로 표현한다.

유도 경로는 사람과 플레이테스트 도구가 읽는 공간 의도다. 현재 추격 AI에 waypoint를 강제하지 않으며 실제 유도 재미는 관찰 플레이테스트로 판단한다.

`Pillars`의 고정 기둥은 `(-4,-2)`, `(-2,1)`, `(2,1)`, `(-3,-3)`, `(-3,3)`, `(3,-3)`, `(3,3)`이다. 파괴벽 `(2,-2)`는 동쪽 차선을 열 수 있는 종단이고, 안전 셀 `(-3,-2)·(-3,-1)·(-2,-2)`와 뒤의 두 셀을 퇴로 anchor로 사용한다. 이 정확한 셀 집합은 builder와 validator가 함께 고정하지만, 1 cell/s 획득·0.75초 예고·8 cells/s 돌진에서 실제 유도 선택이 읽히는지는 `Proposed`다.

## 던전 런 카탈로그

`PrototypeDungeonCombatRoomCatalog.asset`은 다섯 전투방 ScriptableObject와 대응 TestSandbox 씬 이름을 명시적으로 매핑한다. 런 시작 시 `PrototypeDungeonRunSession`이 카탈로그를 Core 정의로 변환하고 결정론적 배정 결과의 room ID를 다시 Unity asset·scene으로 해석한다. 현재 방, 방문과 클리어 같은 mutable 상태는 카탈로그에 쓰지 않는다.

`CombatRoomRotationUtility`는 run 배정의 0/90/180/270도 회전을 방 정의 전체에 적용한다. 90/270도에서는 너비와 깊이를 교환하고 플레이어·추격자·돌진형·갑옷 적·자폭병 spawn, 자폭 anchor, 고정·파괴 벽, 안전 셀, 퇴로 anchor, 유도 loop, 출구 셀과 방향을 같은 시계 방향으로 돌린다. 일부 목록이나 scene Transform만 따로 회전하는 것은 허용하지 않는다.

카탈로그의 entry는 null 방, 빈 씬 이름, 중복 room ID와 중복 씬 이름을 허용하지 않는다. 실제 씬 수명은 persistent run host·navigator·room binder가 소유하며, 카탈로그는 mutable 방문 상태를 갖지 않는 검증된 조회 경계다.

special catalog는 `Start`, `BombReward`, `BossAntechamber`, `Recovery`, `Secret`, `Boss` 여섯 타입을 각기 다른 scene으로 해석한다. `DungeonSecret`은 안전방 shell을 재사용하고 적 actor나 클리어 조건을 만들지 않는다.

## 회복방 저작 계약

- `DungeonRecovery.unity`는 기존 안전방 shell과 문·HUD·run binder를 재사용하며 적 actor와 클리어 조건을 만들지 않는다.
- `PrototypeRecoveryPickupPresenter`는 중앙 논리 셀 `(0,0)`을 감시한다. Collider나 Transform 접촉이 아니라 `PlayerMovementStep`의 확정 논리 셀만 획득을 일으킨다.
- 회복량 `2`와 1회 사용은 GDD에 없는 `Proposed` 튜닝이다. 실제 소비 여부는 scene이 아니라 Core `DungeonRunState`의 Recovery 노드 상태가 소유한다.
- 최대 체력에서는 `HEALTH FULL`로 남고 소비하지 않는다. 유효한 회복 뒤에는 `RECOVERY USED`, 미소비 상태에서는 `RECOVERY +2`를 표시해 색만으로 상태를 구분하지 않는다.
- pickup renderer는 `RecoveryPickup.mat`의 URP Lit shared material을 사용한다. 런타임 material 인스턴스를 만들지 않으며 WebGL에서 shader fallback 색으로 보이지 않아야 한다.

## 비밀방과 금 간 출구 저작 계약

- 11개 던전·TestSandbox scene의 네 boundary 방향에는 기본 비활성 secret door root가 하나씩 있다. root는 대응하는 일반 door renderer와 같은 위치·회전을 사용해 동일한 방 경계 면을 표현한다.
- 각 root는 Collider 없는 `SecretWallSurface` 하나와 균열 막대 3개를 소유한다. surface는 `DestructibleWall.mat`, 막대는 `SecretCrack.mat` URP Lit shared material을 사용해 폭발 가능한 벽을 표현한다.
- `DungeonRoomExitStatus.SecretWall`일 때만 해당 root가 활성화되고 바깥 장식 문 renderer는 숨긴다. 폭발로 공개되면 root를 숨기고 기존 문 renderer를 `Open` 상태로 복원한다. 문·root의 활성 여부는 표현이고 연결 공개 상태는 Core run state가 소유한다.
- 미공개 Secret 출구의 저작 출구 셀은 계속 `Floor`다. `PrototypeDungeonRoomBinder`가 이 셀과 Secret 연결 방향을 매핑하고, 확정 폭발의 `AffectedCells`가 셀에 닿으면 같은 run의 해당 연결만 공개한다. 공개 전 바깥 이동은 지형이 아니라 `DungeonRoomExitStatus.SecretWall` 경계 상태가 막는다.
- `DungeonSecret.unity`는 적 없는 안전방이며 중앙 `(0,0)`에 `PrototypeSecretRewardPresenter` 하나를 둔다. cache는 Collider 접촉이 아니라 확정 `PlayerMovementStep`으로만 수집한다.
- cache 보상 `ROOM TOKENS +3`은 일반 전투 `+1`보다 높은 `Proposed` 값이다. `SecretReward.mat` shared material을 사용하고 소비 상태와 합계는 Core run state가 소유한다.

## 장갑병 독립 플레이테스트 씬

- `ArmoredPanicPlaytest.unity`는 `prototype-combat-armor`와 일반 TestSandbox shell에서 생성하는 Editor 전용 미러다. 장갑병 전용 규칙이나 중복 방 수치를 소유하지 않는다.
- builder는 동기화할 때 권위 `PrototypeCombatArmor.asset`의 격자, spawn, 장애물과 적 정의를 적용한 뒤 던전 `RunHost`, room binder, 미니맵, 문 presenter, 완료 presenter를 제거한다.
- `PrototypeRoomAdvanceController`는 현재 session을 참조하되 다음 씬 이름을 비워 둔다. 전투를 클리어해도 다른 씬으로 이동하지 않으므로 같은 상태를 관찰할 수 있다.
- 표준 Build Settings enabled scene에는 넣지 않는다. 실제 던전 전환이나 WebGL 빌드 검증을 대신하지 않으며 빠른 장갑 상태·panic 이동·두 번째 적중 조작 확인에만 사용한다.
- Editor 메뉴 `Bomb Swap > Playtest > Play Armored Panic Room`이 동기화, 단일 씬 열기, Play 진입을 한 번에 수행한다. `Open`과 `Rebuild` 메뉴도 같은 builder 경로를 사용한다.
- 콘텐츠 validator는 Armor room 참조·spawn·장애물 표현, 필수 session/presenter, 빈 다음 씬, 던전 전용 adapter 부재, MainCamera 존재와 Build Settings 제외를 함께 검사한다.

## 저작 불변식

- 플레이어와 모든 적은 서로 다른 셀에서 시작한다. 추격자는 시작 즉시 cardinal 접촉하지 않고, 선택적 돌진형·갑옷 적·자폭병도 플레이어와 즉시 인접하지 않는다.
- 플레이어 spawn은 안전 셀에 포함된다.
- 플레이어 spawn에서 서로 다른 첫 cardinal 이동을 사용하는 퇴로가 최소 두 개 존재한다. 경로는 spawn을 다시 통과하지 않고 퇴로 anchor에 도달해야 한다.
- 유도 경로는 최소 4개의 서로 다른 플레이 가능 셀이 닫힌 cardinal 순환을 이룬다.
- 모든 출구는 방향에 맞는 방 경계의 플레이 가능 셀이다.
- 같은 방향의 잠재 출구는 한 방 정의에 중복될 수 없다. 현재 다섯 프로토타입 전투방은 cardinal 네 방향을 각각 정확히 한 개 지원한다.
- 고정 벽과 파괴 가능 벽을 제외한 초기 플레이 가능 셀은 모두 플레이어 spawn에서 연결되어 있다. 따라서 파괴는 진행 필수가 아니다.
- 파괴 가능 벽은 고정 벽, spawn, 안전 셀, 퇴로 anchor, 유도 경로, 출구와 겹치지 않는다.
- 자폭병이 있으면 유도 anchor가 하나 이상 있어야 하고 모두 초기 `Floor`이며 spawn·고정 벽·파괴 가능 벽·출구와 겹치지 않는다. 각 anchor의 기대 폭발 footprint는 저작 검증에서 확인하지만 Core AI는 목록을 읽어 목적지를 선택하지 않는다. 자폭병이 없으면 anchor 목록도 비어 있어야 한다.
- 씬 spawn·장애물 Transform의 XZ 셀과 논리 메타데이터가 일치한다.
- 회전된 room 정의의 모든 셀이 교환된 경계 안에 있고, scene `GridRoot`의 같은 Y 회전 뒤 논리 셀·시각 위치가 일치한다.
- 폭발을 끊는 벽/기둥과 향후 파괴 가능 벽은 시각적으로 구분되어야 한다.
- 둘 이상의 폭탄 역할이 도입되면 서로 다른 위치 선택을 만들 공간이 있어야 한다.
- 보상 후보 visual은 Collider나 Transform 접촉을 규칙으로 사용하지 않는다. 플레이어의 확정된 논리 셀 전이만 선택을 일으킨다.
- secret door surface·crack visual·cache primitive에는 Collider가 없다. 보이는 secret door는 대응 일반 문과 같은 위치이고, 폭발 판정은 별도 저작 출구 `Floor` 셀과 `AffectedCells`의 교집합으로만 수행한다.

## Editor 검증기

`PrototypeContentValidator`는 명시적 검증과 빌드 검증에서 다음을 오류로 보고한다.

- 누락되거나 잘못된 room ID/type, 범위 밖·중복 셀, 고정·파괴 가능 벽 겹침.
- 중복 출구 방향 또는 현재 다섯 프로토타입 room asset에서 cardinal 잠재 출구가 빠진 상태.
- 누락된 출구, 안전 셀, spawn, 퇴로 anchor, 유도 경로.
- 경계 방향이 틀린 출구, 끊긴 유도 경로, 연결되지 않은 플레이 영역, 단일 퇴로.
- 다섯 room asset의 ID 중복 또는 각 TestSandbox 씬이 순서에 맞지 않는 room asset을 참조하는 상태.
- 던전 전투방 카탈로그 누락, 잘못된 entry 수, room asset·씬 매핑 순서 불일치 또는 Core 변환 실패.
- 필수 추격자 또는 선택적 돌진형·갑옷 적·자폭병 spawn Transform이 저작 셀과 다르거나 방 전환 controller의 session·다음 씬·지연이 잘못된 상태.
- 논리 고정 벽과 `Environment/InteriorObstacles` 표현 셀의 누락·중복·추가.
- 논리 파괴 벽과 `Environment/DestructibleObstacles`의 황갈색 4분할 표현 셀·재질·Collider·presenter 참조 불일치.
- 돌진형 정의·collider 없는 prefab, 방별 선택적 spawn, session·presenter 참조 또는 적 수 구성이 권위 방 데이터와 다른 상태.
- 갑옷 적 정의·collider 없는 적/panic 예고 prefab, 방별 선택적 spawn, session·presenter 참조 또는 수비·panic·추격 표현 구성이 권위 방 데이터와 다른 상태.
- Armor 방의 T 교차점 고정 벽 9셀이 정확한 좌표와 다르거나 panic 좌우 종단·안전 셀·퇴로·외곽 유도 loop의 연결성이 깨진 상태.
- 자폭병 정의·범위 1 적 폭탄·collider 없는 적/Telegraph prefab, 방별 spawn·anchor, session·presenter 참조가 권위 방 데이터와 다른 상태.
- Gates 방의 자폭병 `(3,0)`, 유도 anchor `(0,-2)·(0,2)`, 중앙 파괴문 `(0,-1)·(0,1)`과 고정 장벽 8셀이 정확한 계약과 다르거나 각 anchor 폭발이 정확히 한쪽 문에만 닿지 않는 상태.
- 11개 던전·TestSandbox 씬에 `PrototypeHealthHud`가 정확히 하나가 아니거나 해당 씬의 `PrototypeGameSession`을 참조하지 않는 상태.
- 11개 던전·TestSandbox 씬에 `PrototypeDungeonMinimapPresenter`가 정확히 하나가 아니거나 해당 씬의 `PrototypeDungeonRoomBinder` 참조와 일치하지 않는 상태.
- Recovery special catalog entry·`DungeonRecovery` scene·pickup presenter가 누락되거나 회복량·논리 셀·session·binder·URP material 참조가 계약과 다른 상태.
- Secret special catalog entry·`DungeonSecret` scene·단일 cache presenter·`+3`·중앙 셀·URP material 또는 11개 scene의 네 방향 secret door root가 계약과 다른 상태. 각 root는 대응 일반 문과 같은 위치에 있고 Collider 없이 파괴벽 surface 1개·crack bar 3개를 정확히 가져야 한다.
- Build Settings의 첫 enabled 씬 11개가 시작→폭탄 보상→보스 전실→회복→비밀방→보스→중앙 루프→평행 통로→엇갈린 기둥→갑옷 실험→중앙 게이트 순서가 아닌 상태.

자동 검증이 방의 재미를 보증하지는 않는다. 시각 확인과 플레이테스트를 함께 수행한다.
