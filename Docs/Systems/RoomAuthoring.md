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
- 플레이어와 필수 추격자 spawn, 선택적 돌진형·갑옷 적·자폭병·퇴로 차단 투척병 spawn.
- 자폭병이 있을 때 순서가 안정적인 자폭 유도 anchor 목록. 이 목록은 AI 경로 목적지가 아니라 레벨 설계·검증·플레이테스트가 의도한 폭발 위치와 결과를 공유하는 메타데이터다.
- 투척병이 있을 때 순서가 안정적인 사격 anchor와 퇴로 차단 목표 anchor 목록. spawn은 첫 사격 전 이동하는 별도 staging 셀로서 사격 anchor에 포함되지 않고, 사격 목록은 최소 2개, 목표 목록은 반복 volley의 측면 2칸을 바꾸기 위해 정의된 발수의 최소 두 배이며 두 목록은 서로 겹치지 않는다.
- 고정 벽 셀.
- 파괴 가능 벽 셀.
- 플레이어 입장 안전 셀.
- 최소 두 개의 퇴로 anchor.
- 순서가 있는 닫힌 폭탄 유도 순환 경로.

범용 적 spawn 목록과 제약, 일반화된 보상·전환 anchor는 아직 구현하지 않았다. 첫 폭탄 보상만 안전방 shell의 보행 가능한 `(-1,0)`·`(1,0)` 논리 셀을 고정 후보 위치로 사용하고 presenter가 초기화 때 Floor 여부를 검증한다. 첫 구현은 독립 방 prefab 대신 이 데이터와 `TestSandbox` 씬 표현을 연결한다. 향후 prefab으로 분리해도 논리 셀 데이터가 권위 원본이라는 경계는 유지한다.

## 현재 수제 전투방 세트

메인 던전 카탈로그의 다섯 방은 모두 11×9, 셀 크기 1의 `Combat` 방이며 추격자를 사용한다. 두 번째 방은 선택적 투척병, 세 번째 방은 선택적 돌진형, 네 번째 방은 선택적 갑옷 적, 다섯 번째 방은 선택적 자폭병을 함께 사용한다. 기존 `prototype-combat-lanes` 자산과 `TestSandboxLanes` 씬은 삭제하지 않고 메인 편성 밖 Legacy 독립 테스트로 보존한다.

| 순서 | ID / 자산 | spawn | 공간 의도 | 씬 / 다음 씬 |
|---:|---|---|---|---|
| 1 | `prototype-combat-loop` / `PrototypeCombatLoop.asset` | 플레이어 `(0, 0)`, 추격자 `(1, -1)` | 중앙 십자 고정 벽 4개, 파괴 벽 없음, 기존 전투 기준선 | `TestSandbox.unity` / `TestSandboxThrower` |
| 2 | `prototype-combat-thrower` / `PrototypeCombatThrower.asset` | 플레이어 `(0,-2)`, 추격자 `(-2,2)`, 투척병 staging `(3,2)` | 기존 Lanes 벽과 파괴 블록, 모든 잠재 출구에서 Manhattan 4칸 이상 떨어진 두 적, staging에서 첫 사격 anchor까지 4칸 Track, 3개 사격 anchor·6개 목표 anchor: 가장 가까운 압박점 1개와 바뀌는 측면 2개를 동시에 예고하고 공용 폭탄/연쇄를 사용 | `TestSandboxThrower.unity` / `TestSandboxPillars` |
| 3 | `prototype-combat-pillars` / `PrototypeCombatPillars.asset` | 플레이어 `(-3, -2)`, 추격자 `(3, 2)`, 돌진형 `(-1, 1)` | 차선 종단 기둥 7개, 동쪽 파괴 종단 `(2,-2)`, 중앙 3×3 외곽 loop: seed-0 동쪽 입장 축과 어긋난 돌진형이 최소 한 Track 이동 뒤 짧은 차선을 예고하며 플레이어는 북/동 포켓으로 이탈 | `TestSandboxPillars.unity` / `TestSandboxArmor` |
| 4 | `prototype-combat-armor` / `PrototypeCombatArmor.asset` | 플레이어 `(0, -2)`, 추격자 `(4, 4)`, 갑옷 적 `(0, 1)` | 남쪽 통로 벽 `x=±2,z=-2..-1`, 상단 막 `x=-1..1,z=2`, 좌우 종단 `(-4,0)·(4,0)`의 T 교차점, 파괴 벽 없음: 반경 수비→첫 피격 반대편 3칸 예고·질주→두 번째 선행 설치 | `TestSandboxArmor.unity` / `TestSandboxGates` |
| 5 | `prototype-combat-gates` / `PrototypeCombatGates.asset` | 플레이어 `(0, -3)`, 추격자 `(0, 3)`, 자폭병 `(3,0)` | `z=-1·1`의 `x=-2,-1,1,2` 고정 장벽 8개와 중앙 파괴 문 `(0,-1)·(0,1)`, 자폭 유도 anchor `(0,-2)→(0,2)`: 플레이어가 추적형 자폭병을 어느 쪽으로 끄느냐에 따라 범위 2 자폭이 아래/위 문 한쪽만 먼저 연다. 첫 파괴 문이 해당 ray를 끝내며 `x=±3` 우회는 유지 | `TestSandboxGates.unity` / 없음 |

다섯 방은 모두 북 `(0,4)`, 동 `(5,0)`, 남 `(0,-4)`, 서 `(-5,0)` 중앙 경계의 잠재 출구를 갖는다. 잠재 출구는 항상 열린 문이 아니라 room geometry가 지원하는 후보이며, run 그래프가 필요한 방향만 활성 문으로 선택한다. 각 방은 서로 다른 첫 cardinal 이동을 쓰는 퇴로 anchor 두 개와 닫힌 cardinal 유도 순환 경로를 소유한다. 실제 문 GameObject는 [ADR-0007](../ADR/0007-Potential-Room-Exits.md)에 따라 run 활성 부분집합을 `Inactive`·`Locked`·`Open`·`SecretWall`로 표현한다.

Legacy `prototype-combat-lanes` / `PrototypeCombatLanes.asset`은 플레이어 `(0,-2)`, 추격자 `(0,2)`, 세로 고정 벽 6개와 파괴 벽 `(-1,-1)·(1,-1)`을 유지한다. `TestSandboxLanes.unity`는 던전 host·binder·미니맵·문·완료 presenter가 없고 다음 씬이 빈 독립 테스트 씬이며 enabled Build Settings와 메인 카탈로그에는 포함하지 않는다.

## 전용 보스 arena

- `prototype-boss-arena` / `PrototypeBossArena.asset`은 일반 전투방과 같은 검증된 `CombatRoomDefinition` 스키마를 재사용하지만 절차 배정 카탈로그에는 포함하지 않는다. `DungeonBoss.unity`만 이 shell을 참조한다.
- 크기는 11×9, 플레이어 spawn은 `(0,-3)`, 보스 spawn은 `(0,1)`이다. 스키마의 필수 추격자 spawn `(4,3)`은 보스 전용 session에서 생성하지 않는 저작 placeholder다.
- 고정 기둥은 `(-2,-1)·(2,-1)·(-2,1)·(2,1)` 네 셀이다. 파괴 가능 벽과 선택 적 spawn은 없다.
- 스키마의 `RetreatAnchors`는 보스 투척 후보 6개 `(-4,-2)·(-3,3)·(0,-3)·(0,3)·(3,3)·(4,-2)`를 소유한다. One은 가까운 서로 다른 3개, Two는 떨어진 두 일반 지점과 각 중앙 방향 인접 연쇄 셀, LastStand는 외곽→안쪽 4개 계획에 사용한다.
- 보스 전환용 자폭병 저작 spawn은 `(-4,3)`, 소환 후보는 `(-3,3)·(0,3)·(3,3)`이다. Core는 비점유 후보 중 현재 플레이어에게서 먼 셀을 안정 좌표 순서로 고르고 Telegraph에 표시한다.
- 기존 `LureLoop` 메타데이터는 `CombatRoomDefinition` 스키마 호환을 위해 남지만 현재 보스는 이를 이동 경로로 사용하지 않는다. 제한 추격·돌진·중앙 복귀가 매 step 권위 격자에서 경로를 계획하며 정확한 목적지 ghost는 표시하지 않는다.
- builder는 room asset을 먼저 갱신한 뒤 `DungeonBoss`와 `BossBattlePlaytest`의 바닥·기둥·spawn·context 참조를 Editor API로 재생성한다. validator는 정확한 room ID·크기·spawn·기둥·6 투척 anchor·3 소환 anchor와 `DungeonBoss` 단독 던전 참조를 검사한다. 전용 플레이테스트 씬은 Build Settings에 넣지 않는다.

유도 경로는 사람과 플레이테스트 도구가 읽는 공간 의도다. 현재 추격 AI에 waypoint를 강제하지 않으며 실제 유도 재미는 관찰 플레이테스트로 판단한다.

`Pillars`의 고정 기둥은 `(-4,-2)`, `(-2,1)`, `(2,1)`, `(-3,-3)`, `(-3,3)`, `(3,-3)`, `(3,3)`이다. 파괴벽 `(2,-2)`는 동쪽 차선을 열 수 있는 종단이고, 안전 셀 `(-3,-2)·(-3,-1)·(-2,-2)`와 뒤의 두 셀을 퇴로 anchor로 사용한다. 돌진형 spawn `(-1,1)`은 seed-0 회전 뒤 동쪽 입장 셀과 같은 축이 되지 않아 최소 한 번의 1 cell/s Track 이동을 보장한다. 이 정확한 셀 집합은 builder와 validator가 함께 고정하지만, 0.75초 예고·8 cells/s 돌진에서 실제 유도 선택이 읽히는지는 `Proposed`다.

## 던전 런 카탈로그

`PrototypeDungeonCombatRoomCatalog.asset`은 `Loop → Thrower → Pillars → Armor → Gates` 다섯 ScriptableObject를 각각 `TestSandbox → TestSandboxThrower → TestSandboxPillars → TestSandboxArmor → TestSandboxGates`로 명시적으로 매핑한다. 런 시작 시 `PrototypeDungeonRunSession`이 카탈로그를 Core 정의로 변환하고 결정론적 배정 결과의 room ID를 다시 Unity asset·scene으로 해석한다. 현재 방, 방문과 클리어 같은 mutable 상태는 카탈로그에 쓰지 않는다.

`CombatRoomRotationUtility`는 run 배정의 0/90/180/270도 회전을 방 정의 전체에 적용한다. 90/270도에서는 너비와 깊이를 교환하고 플레이어·추격자·돌진형·갑옷 적·자폭병·투척병 spawn, 자폭 anchor, 투척병 사격/목표 anchor, 고정·파괴 벽, 안전 셀, 퇴로 anchor, 유도 loop, 출구 셀과 방향을 같은 시계 방향으로 돌린다. 일부 목록이나 scene Transform만 따로 회전하는 것은 허용하지 않는다.

카탈로그의 entry는 null 방, 빈 씬 이름, 중복 room ID와 중복 씬 이름을 허용하지 않는다. 실제 씬 수명은 persistent run host·navigator·room binder가 소유하며, 카탈로그는 mutable 방문 상태를 갖지 않는 검증된 조회 경계다.

special catalog는 `Start`, `BombReward`, `BossAntechamber`, `Recovery`, `Secret`, `Boss` 여섯 타입을 각기 다른 scene으로 해석한다. `DungeonSecret`은 안전방 shell을 재사용하고 적 actor나 클리어 조건을 만들지 않는다.

## 회복방 저작 계약

- `DungeonRecovery.unity`는 기존 안전방 shell과 문·HUD·run binder를 재사용하며 적 actor와 클리어 조건을 만들지 않는다.
- `PrototypeRecoveryPickupPresenter`는 중앙 논리 셀 `(0,0)`을 `Interactable`로 등록한다. Collider나 Transform 접촉이 아니라 cardinal 인접 셀의 확정 플레이어 위치와 `E`/게임패드 North 명령만 회복을 요청한다.
- 직접 scene을 시작해 플레이어가 이미 pickup 셀에 겹친 과거 저작 상태에서는 actor와 blocker를 중복 등록하지 않는다. 플레이어가 그 셀을 처음 이탈한 뒤 blocker를 등록하며, 정상 던전 진입 spawn은 pickup 셀 밖에 두는 것이 권위 계약이다.
- 회복량 `2`와 1회 사용은 GDD에 없는 `Proposed` 튜닝이다. 실제 소비 여부는 scene이 아니라 Core `DungeonRunState`의 Recovery 노드 상태가 소유한다.
- 최대 체력에서는 pickup을 소비하지 않아 월드 표현과 논리 blocker가 남고, 유효한 회복이나 이미 소비한 방에서는 둘 다 제거한다. 별도 회복 안내 Canvas는 사용하지 않으며 현재 체력 변화는 공용 체력 HUD로 확인한다.
- pickup renderer는 `RecoveryPickup.mat`의 URP Lit shared material을 사용한다. 런타임 material 인스턴스를 만들지 않으며 WebGL에서 shader fallback 색으로 보이지 않아야 한다.

## 비밀방과 금 간 출구 저작 계약

- 11개 던전 scene의 네 boundary 방향에는 `Door.prefab` 일반 문과 기본 비활성 `CrackedBrickBlock.prefab` secret door root가 하나씩 있다. secret root는 대응 일반 문과 같은 위치를 사용해 동일한 방 경계 면을 표현한다.
- 일반 문의 상태는 원본 머티리얼 색상을 덮어쓰지 않고 `IsOpen` Animator bool과 SecretWall일 때의 renderer 표시 여부로만 표현한다.
- 모든 던전 scene의 secret root는 `CrackedBrickBlock.prefab`과 일치하고 Collider·Rigidbody가 없어야 한다.
- 11개 던전 scene의 `FloorVisuals`는 local Y `-1`에서 방 내부 바닥 셀에 더해 네 방향 문 위치에도 `BrickBlock.prefab` 바닥 블록을 하나씩 둔다. 문 지지는 별도 `BoundaryBaseVisuals`가 아니라 이 네 바닥 블록이 담당한다.
- 11개 던전 scene의 `InteriorObstacles` 자식은 논리 셀 XZ를 유지하면서 local Y `0`에 배치한다. 과거 primitive Cube 중심 배치에 사용한 Y `0.5`는 3D 프리팹 표현에 재사용하지 않는다.
- `DungeonRoomExitStatus.SecretWall`일 때만 해당 root가 활성화되고 바깥 장식 문 renderer는 숨긴다. 폭발로 공개되면 root도 숨겨 빈 통로를 남기며, 같은 방에 재진입해도 원래 Secret 연결에는 일반 문을 복원하지 않는다. 문·root의 활성 여부는 표현이고 연결 공개 상태는 Core run state가 소유한다.
- 비밀 연결이 실제 폭발로 처음 공개될 때는 해당 `SecretCracks` root의 world position보다 Y축으로 `0.5` 높은 위치에서 효과를 한 번 재생한다. 씬에 직접 연결한 prefab, Git에서 제외된 `Resources/BombSwapLocalVfxOverrides`의 로컬 prefab, first-party 절차형 fallback 순서로 선택한다. 로컬 패키지 prefab의 저작 회전은 유지하며, 재진입과 상태 재적용에서는 어느 경로도 다시 재생하지 않는다.
- 미공개 Secret 출구의 저작 출구 셀은 계속 `Floor`다. `PrototypeDungeonRoomBinder`가 이 셀과 Secret 연결 방향을 매핑하고, 확정 폭발의 `AffectedCells`가 셀에 닿으면 같은 run의 해당 연결만 공개한다. 공개 전 바깥 이동은 지형이 아니라 `DungeonRoomExitStatus.SecretWall` 경계 상태가 막는다.
- `DungeonSecret.unity`는 적 없는 안전방이며 중앙 `(0,0)`에 `PrototypeSecretRewardPresenter` 하나를 둔다. cache는 해당 셀을 `Interactable`로 막고 Collider 접촉이나 셀 진입이 아니라 cardinal 인접 셀의 `E`/게임패드 North 명령으로만 수집한다.
- 직접 scene을 시작해 플레이어가 이미 cache 셀에 겹친 과거 저작 상태도 같은 이탈 후 blocker 등록 호환 경로를 사용한다. 이 예외는 겹친 actor를 가두지 않기 위한 것이며 셀 진입 수집을 다시 허용하지 않는다.
- cache 보상 `ROOM TOKENS +3`은 일반 전투 `+1`보다 높은 `Proposed` 값이다. `SecretReward.mat` shared material을 사용하고 소비 상태와 합계는 Core run state가 소유한다.
- 비밀 cache는 미수집일 때 월드 표현과 논리 blocker를 유지하고 수집 뒤 둘 다 제거한다. 별도 비밀방 안내 Canvas는 사용하지 않으며 합계 변화는 공용 토큰 HUD로 확인한다.

## 장갑병 독립 플레이테스트 씬

- `ArmoredPanicPlaytest.unity`는 `prototype-combat-armor`와 일반 TestSandbox shell에서 생성하는 Editor 전용 미러다. 장갑병 전용 규칙이나 중복 방 수치를 소유하지 않는다.
- builder는 동기화할 때 권위 `PrototypeCombatArmor.asset`의 격자, spawn, 장애물과 적 정의를 적용한 뒤 던전 `RunHost`, room binder, 미니맵, 문 presenter, 완료 presenter를 제거한다.
- `PrototypeRoomAdvanceController`는 현재 session을 참조하되 다음 씬 이름을 비워 둔다. 전투를 클리어해도 다른 씬으로 이동하지 않으므로 같은 상태를 관찰할 수 있다.
- 표준 Build Settings enabled scene에는 넣지 않는다. 실제 던전 전환이나 WebGL 빌드 검증을 대신하지 않으며 빠른 장갑 상태·panic 이동·두 번째 적중 조작 확인에만 사용한다.
- Editor 메뉴 `Bomb Swap > Playtest > Play Armored Panic Room`이 동기화, 단일 씬 열기, Play 진입을 한 번에 수행한다. `Open`과 `Rebuild` 메뉴도 같은 builder 경로를 사용한다.
- 콘텐츠 validator는 Armor room 참조·spawn·장애물 표현, 필수 session/presenter, 빈 다음 씬, 던전 전용 adapter 부재, MainCamera 존재와 Build Settings 제외를 함께 검사한다.

## 자폭병·Gates 독립 플레이테스트 씬

- `SelfDestructGatesPlaytest.unity`는 `prototype-combat-gates`와 일반 TestSandbox shell에서 생성하는 Editor 전용 미러다. 자폭병 규칙, Gates 셀이나 튜닝 수치를 중복 소유하지 않는다.
- builder는 권위 `PrototypeCombatGates.asset`의 플레이어·추격자·자폭병 spawn, 고정 장벽 8셀, 중앙 파괴문 2셀과 적 정의를 동기화한 뒤 던전 `RunHost`, room binder, 미니맵, 외곽 문 presenter와 완료 presenter를 제거한다.
- 중앙 파괴문과 `PrototypeDestructibleWallPresenter`, 자폭병의 경고·Telegraph presenter, HUD와 빈 다음 씬의 `PrototypeRoomAdvanceController`는 유지한다. 따라서 방 클리어 뒤에도 다른 씬으로 전환하지 않는다.
- 표준 Build Settings enabled scene에는 넣지 않는다. 실제 던전 전환이나 WebGL 검증을 대신하지 않고 경고 점멸, 점화 전 이탈, 위·아래 anchor 유도와 한쪽 문 파괴를 빠르게 반복하는 용도다.
- Editor 메뉴 `Bomb Swap > Playtest > Play Self-Destruct Gates Room`이 동기화, 단일 씬 열기와 Play 진입을 한 번에 수행한다. `Open`과 `Rebuild` 메뉴도 같은 builder 경로를 사용한다.
- 콘텐츠 validator는 Gates 권위 room 참조·spawn·장애물·필수 session/자폭 presenter, 빈 다음 씬, 던전 adapter 부재, MainCamera와 Build Settings 제외를 검사한다.

## 투척병·Lanes 독립 플레이테스트 씬과 던전 미러

- `ThrowerLanesPlaytest.unity`는 `prototype-combat-thrower` room asset과 일반 TestSandbox shell에서 생성하는 Editor 전용 실험이며 표준 enabled Build Settings에는 포함하지 않는다.
- `TestSandboxThrower.unity`는 같은 권위 room asset을 사용하는 던전용 미러다. persistent run host·binder·미니맵·문·완료 presenter를 포함하고 메인 5개 카탈로그의 두 번째 entry와 표준 11씬 Build Settings에 들어간다.
- 권위 room은 플레이어 `(0,-2)`, 추격자 `(-2,2)`, 투척병 staging `(3,2)`, 사격 anchor `(0,3)→(-3,2)→(3,-2)`, 목표 후보 `(0,0)→(-3,-2)→(2,-3)→(-4,1)→(4,1)→(0,2)`와 기존 Lanes 벽·파괴 블록을 소유한다. 투척병은 staging에서 4칸 떨어진 첫 anchor까지 이동한 뒤에만 첫 Telegraph를 시작한다. 두 적은 네 잠재 출구에서 모두 Manhattan 4칸 이상 떨어지고 목표 후보는 추격자 시작점의 범위 1 폭발 footprint 및 출구 셀과 겹치지 않는다. seed 0의 Clockwise90 서쪽 진입에서는 플레이어 `(4,0)`, 추격자 `(2,2)`, 투척병 `(2,-3)`, 첫 사격 anchor `(3,0)`이 된다.
- builder는 room·투척병 정의·전용 폭탄·collider 없는 적/Telegraph prefab을 두 씬에 동기화한다. 독립 씬에서는 던전 `RunHost`, room binder, 미니맵, 외곽 문 presenter와 완료 presenter를 제거하고 `PrototypeRoomAdvanceController`의 다음 씬을 비운다. 던전 미러에는 반대 계약을 적용한다.
- Editor 메뉴 `Bomb Swap > Playtest > Play Thrower Lanes Room`이 동기화, 단일 씬 열기와 Play 진입을 한 번에 수행한다. `Open`과 `Rebuild` 메뉴도 같은 builder 경로를 사용한다.
- 전용 WebGL 빌드는 연결 빌드 하네스에 `ThrowerLanesPlaytest` 경로만 직접 전달하며 Build Settings 자체를 바꾸지 않는다. 표준 WebGL 빌드는 `TestSandboxThrower`를 포함한다.
- 콘텐츠 validator는 두 씬의 정확한 room/정의/폭탄 수치, anchor, session·presenter·spawn을 검사한다. 독립 씬은 빈 다음 씬·던전 adapter 부재·Build Settings 제외를, 던전 미러는 host/binder/minimap/door/completion 참조와 enabled Build Settings 포함을 각각 요구한다.

## 저작 불변식

- 플레이어와 모든 적은 서로 다른 셀에서 시작한다. 추격자는 시작 즉시 cardinal 접촉하지 않고, 선택적 돌진형·갑옷 적·자폭병·투척병도 플레이어와 즉시 인접하지 않는다.
- 플레이어 spawn은 안전 셀에 포함된다.
- 플레이어 spawn에서 서로 다른 첫 cardinal 이동을 사용하는 퇴로가 최소 두 개 존재한다. 경로는 spawn을 다시 통과하지 않고 퇴로 anchor에 도달해야 한다.
- 유도 경로는 최소 4개의 서로 다른 플레이 가능 셀이 닫힌 cardinal 순환을 이룬다.
- 모든 출구는 방향에 맞는 방 경계의 플레이 가능 셀이다.
- 같은 방향의 잠재 출구는 한 방 정의에 중복될 수 없다. 현재 다섯 프로토타입 전투방은 cardinal 네 방향을 각각 정확히 한 개 지원한다.
- 고정 벽과 파괴 가능 벽을 제외한 초기 플레이 가능 셀은 모두 플레이어 spawn에서 연결되어 있다. 따라서 파괴는 진행 필수가 아니다.
- 파괴 가능 벽은 고정 벽, spawn, 안전 셀, 퇴로 anchor, 유도 경로, 출구와 겹치지 않는다.
- 자폭병이 있으면 유도 anchor가 하나 이상 있어야 하고 모두 초기 `Floor`이며 spawn·고정 벽·파괴 가능 벽·출구와 겹치지 않는다. 각 anchor의 기대 폭발 footprint는 저작 검증에서 확인하지만 Core AI는 목록을 읽어 목적지를 선택하지 않는다. 자폭병이 없으면 anchor 목록도 비어 있어야 한다.
- 투척병이 있으면 사격 anchor가 최소 2개, 목표 anchor가 정의의 volley 발수의 두 배 이상이고 모두 초기 `Floor`여야 한다. spawn은 사격 anchor 밖의 별도 staging 셀이며 각 잠재 출구와 Manhattan 4칸 이상 떨어지고, 첫 사격 anchor까지 최소 4칸을 이동한다. 추격자도 각 잠재 출구에서 4칸 이상 떨어지고 모든 목표 anchor의 초기 범위 1 footprint 밖이어야 하며 목표 anchor는 잠재 출구 셀과 겹치지 않는다. 두 anchor 목록은 actor spawn·서로 간에 겹치지 않는다. 투척병이 없으면 두 목록도 비어 있어야 한다.
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
- 필수 추격자 또는 선택적 돌진형·갑옷 적·자폭병·투척병 spawn Transform이 저작 셀과 다르거나 방 전환 controller의 session·다음 씬·지연이 잘못된 상태.
- 논리 고정 벽과 `Environment/InteriorObstacles` 표현 셀의 누락·중복·추가.
- 논리 파괴 벽과 `Environment/DestructibleObstacles`의 황갈색 4분할 표현 셀·재질·Collider·presenter 참조 불일치.
- 돌진형 정의·collider 없는 prefab, 방별 선택적 spawn, session·presenter 참조 또는 적 수 구성이 권위 방 데이터와 다른 상태.
- 갑옷 적 정의·collider 없는 적/panic 예고 prefab, 방별 선택적 spawn, session·presenter 참조 또는 수비·panic·추격 표현 구성이 권위 방 데이터와 다른 상태.
- Armor 방의 T 교차점 고정 벽 9셀이 정확한 좌표와 다르거나 panic 좌우 종단·안전 셀·퇴로·외곽 유도 loop의 연결성이 깨진 상태.
- 자폭병 정의·범위 2 적 폭탄·collider 없는 적/Telegraph prefab, 방별 spawn·anchor, session·presenter 참조가 권위 방 데이터와 다른 상태.
- Gates 방의 자폭병 `(3,0)`, 유도 anchor `(0,-2)·(0,2)`, 중앙 파괴문 `(0,-1)·(0,1)`과 고정 장벽 8셀이 정확한 계약과 다르거나, 실제 Core 범위 2 폭발 해석에서 각 anchor가 정확히 가까운 문 하나만 파괴하지 않는 상태.
- 투척병 정의·범위 1 적 폭탄·collider 없는 적/Telegraph prefab, 전용 room의 spawn·사격/목표 anchor, session·presenter 참조가 권위 데이터와 다른 상태.
- `ThrowerLanesPlaytest`에 던전 adapter가 남아 있거나 다음 씬이 비어 있지 않거나 MainCamera가 없거나 표준 enabled Build Settings에 포함된 상태.
- 11개 던전·TestSandbox 씬에 `PrototypeHealthHud`가 정확히 하나가 아니거나 해당 씬의 `PrototypeGameSession`을 참조하지 않는 상태.
- 11개 던전·TestSandbox 씬에 `PrototypeDungeonMinimapPresenter`가 정확히 하나가 아니거나 해당 씬의 `PrototypeDungeonRoomBinder` 참조와 일치하지 않는 상태.
- Recovery special catalog entry·`DungeonRecovery` scene·pickup presenter가 누락되거나 회복량·논리 셀·session·binder·URP material 참조가 계약과 다른 상태.
- Secret special catalog entry·`DungeonSecret` scene·단일 cache presenter·`+3`·중앙 셀·URP material 또는 11개 scene의 네 방향 secret door root가 계약과 다른 상태. 각 root는 대응 `Door.prefab`과 같은 위치의 collider-free `CrackedBrickBlock.prefab` 인스턴스여야 한다.
- Build Settings의 첫 enabled 씬 11개가 시작→폭탄 보상→보스 전실→회복→비밀방→보스→중앙 루프→평행 통로→엇갈린 기둥→갑옷 실험→중앙 게이트 순서가 아닌 상태.

자동 검증이 방의 재미를 보증하지는 않는다. 시각 확인과 플레이테스트를 함께 수행한다.
