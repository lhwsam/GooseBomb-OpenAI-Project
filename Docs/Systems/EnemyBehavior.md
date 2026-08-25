# 적 행동

- 상태: 프로토타입 5종 역할·구현 계약 `Accepted`, 세부 수치·재미 `Proposed`
- 설계 원본: `GDD_v0.2.md` 16~18장
- 코드 소유: 판단은 `BombSwap.Core`, 이동/애니메이션은 `BombSwap.Unity`

## 목적

적이 단순 체력 벽이 아니라 플레이어가 폭탄 위치와 탈출 경로를 바꾸게 만드는 공간 압력으로 작동하게 한다.

## 프로토타입 적

| 적 | 공간 역할 | 핵심 상태 |
|---|---|---|
| 추격자 | 지속 압박과 기본 유도 학습 | Acquire, Chase, Repath, Hit, Dead |
| 돌진형 | 예고 후 직선 경로를 강제 | Track, Telegraph, Charge, Recover, Dead |
| 갑옷 적 | 교차점 수비, 첫 적중 뒤 예고된 반대편 질주 | 내구 `Armored → Broken → Dead`, 행동 `Guard → PanicTelegraph → PanicRun → PanicRecover → Chase → Dead` |
| 자폭병 | 점멸하며 추적하다 인접 시 멈춰 적·문·퇴로를 함께 바꾸는 적 폭발 | Chase, WarningChase, Telegraph, Detonated |
| 퇴로 차단 투척병 | 현재 위치가 아닌 다음 퇴로를 잠그고 연쇄 가능한 적 폭탄으로 이동 선택을 바꿈 | Track, Telegraph, Recover |

AI Inference 런타임은 사용하지 않는다. 재현 가능한 상태 머신과 격자/경로 규칙을 우선한다.

## 구현된 기본 추격자

- `ChaserEnemyDefinition`은 안정적인 `EnemyDefinitionId`, 내구도, 접촉 피해, 이동 step 간격, 한 번 선택한 방향을 유지할 성공 step 수를 소유한다.
- TestSandbox의 검증된 `PrototypeChaserDefinitionAsset` 값은 내구도 1, 접촉 피해 1, 이동 2 cells/s, 방향 유지 2칸이다. 이 수치는 플레이테스트 전까지 `Proposed`다.
- `ChaserEnemySimulation`은 `ActorId(2)`로 공유 `GridState`를 점유하고 `ActorId(1)` 플레이어 위치를 논리 목표로 읽는다. 일반 ID 발급과 다중 적 수명 주기는 후속 작업이다.
- 첫 판단은 즉시 가능하고 이후 0.5초 cadence를 사용한다. 방향을 새로 고를 때 플레이어 셀에서 역방향으로 만든 BFS 거리장에서 도달 가능한 상하좌우 중 최단 거리 셀을 택한다.
- 동률이면 기존 방향을 우선하고, 새 방향끼리는 `North → East → South → West` 순서로 고정한다. 선택한 방향은 최대 두 칸 유지하되 벽·actor·폭탄에 막히거나 다음 committed step이 계획 당시 최단 거리를 늘리면 즉시 다시 판단한다.
- 플레이어와 cardinal 인접하면 같은 셀에 들어가지 않고 멈춘다. 제자리에서 이미 인접한 경우는 즉시 접촉 후보가 되지만, 한 칸 이동으로 새로 인접해진 경우에는 그 이동의 0.5초 시각 도착 경계까지 기다리고 그때도 인접해야만 접촉 피해 1을 준다. 이동 시작에 논리 점유가 먼저 확정되더라도 보이는 적보다 한 칸 앞에서 맞는 것처럼 느껴지지 않게 하는 결정론적 표현 동기화 계약이다.
- 추격자는 fuse나 폭발 위험 셀을 읽지 않는다. 설치 폭탄은 다른 논리 이동 장애물과 동일하게만 취급한다.
- `EnemyHealthSimulation`은 폭발 `BombId`별 중복 처리를 차단한다. 내구도 1 추격자가 폭발 영향 셀에 있으면 사망하고 actor 점유가 한 번 제거되며, 세션은 `EnemyDied`와 마지막 적의 `RoomCleared`를 한 번 발행한다.
- `PrototypeChaserPresenter`는 Core가 계산한 연속 위치를 표시하고 사망 색을 `MaterialPropertyBlock`으로 잠시 표시한 뒤 placeholder를 비활성화한다. 접촉 가능 시각은 Transform을 읽지 않고 Core의 판정 칸과 도착 시각으로 계산한다.

이전 국소 Manhattan 선택은 seed-0 전체 경로에서 `(1,-3) → (1,-4) → (1,-5)` 왕복을 만들었다. 현재 BFS 계약은 막힌 포켓보다 실제 도달 가능한 최단 가지를 선택하고, 두 칸 방향 유지가 계획 거리 증가를 강제하기 전에 재계획해 이 반복 원인을 제거한다. 수치와 공간 압력의 재미는 여전히 사람 플레이테스트 대상이다.

## 구현된 돌진형

- `ChargerEnemyDefinition`은 안정적인 `EnemyDefinitionId`, 내구도, 충돌 피해, 차선 획득 step 간격, 예고 시간, 한 셀 돌진 cadence와 회복 시간을 소유한다. TestSandbox의 `prototype-charger` 값은 내구도 1, 충돌 피해 1, 차선 획득 1 cell/s, 예고 0.75초, 돌진 8 cells/s, 회복 1초이며 플레이테스트 전까지 `Proposed`다.
- `ChargerEnemySimulation`은 `ActorId(3)`으로 추격자와 같은 `GridState`를 점유한다. `Track`에서 현재 플레이어와 장애물 없는 행/열이 아니면, 가장 가까우면서 플레이어까지 명확한 cardinal 차선을 만드는 도달 가능 셀을 BFS로 찾고 첫 한 칸만 이동한다.
- 차선 후보와 획득 경로 동률은 `North → East → South → West`다. 첫 판단은 즉시 가능하고 이후 1초 cadence에서만 플레이어의 현재 논리 셀을 다시 읽는다. 벽·파괴벽·폭탄·다른 actor는 경로와 정렬 차선을 막으며 유효 후보가 없으면 배회하지 않는다.
- 정렬이 확인되면 `Track → Telegraph`로 전환하면서 방향과 현재 장애물까지의 최대 돌진 거리를 잠근다. 예고 중 플레이어가 이동하거나 기존 장애물이 사라져도 방향·최대 거리를 바꾸거나 늘리지 않는다.
- `PrototypeChargerPresenter`는 고정 최대 거리의 모든 논리 셀에 collider 없는 얇은 예고 placeholder를 풀링해 표시하고 `Charge` 시작 때 회수한다. 획득 이동과 돌진 이동의 연속 위치는 각각의 Core 이동 시간으로 계산된다.
- Charger 표현은 collider와 Rigidbody가 없는 `ChargerPig` 프리팹을 사용한다. Presenter는 확정된 `Track` 이동을 Idle/Run, 상태 전이를 Telegraph/Charge/Recover, `EnemyDied`를 Die 트리거로 변환하며 Root Motion은 사용하지 않는다.
- 예고 종료 뒤 `Charge`에서 고정 방향으로 한 셀씩 이동한다. 플레이어가 다음 셀에 있으면 겹치지 않은 채 단일 충돌 피해 후보를 만들고, 고정 최대 거리를 소진하거나 고정 벽·파괴벽·새 폭탄·다른 actor가 먼저 막으면 피해 없이 `Recover`가 된다.
- 폭탄 충돌의 고정 지연 연쇄와 별도 기절은 기존 bomb scheduler·소유권 계약을 확장해야 하므로 현재는 `Deferred`다. 폭탄은 다른 논리 점유 장애물과 같이 조기 회복만 만든다.
- `PrototypeGameSession`은 같은 frame에서 추격자 다음 돌진형 순서로 이동을 확정한다. 폭발 피해는 추격자 다음 돌진형 순서로 적용하고, 모든 `EnemyDied` 뒤 살아 있는 적이 없을 때만 `RoomCleared`를 한 번 발행한다.
- 각 적 presenter는 전체 적 수가 아니라 자신의 생존 상태만 읽어 다른 적 사망 뒤 잘못 활성화되지 않는다.
- `prototype-combat-pillars`는 플레이어 `(-3,-2)`, 돌진형 `(-1,1)`의 비정렬 상태로 시작한다. seed-0에서 동쪽 문으로 회전 입장해도 돌진형이 입장 축에 바로 정렬되지 않고 최소 한 번의 Track 이동을 거친 뒤 예고한다. 남쪽 정렬 셀 획득, 서쪽 고정 돌진, 종단 기둥과 북/동 측면 탈출을 첫 관찰 경로로 사용한다.

## 구현된 갑옷 적

- `ArmoredEnemyDefinition`은 안정적인 `EnemyDefinitionId`, 접촉 피해, 장갑/추격 이동 간격, 방향 유지 step과 함께 수비 반경, panic 예고 시간·step 간격·최대 거리·회복 시간을 소유한다. `prototype-armored` 값은 접촉 피해 1, 장갑 수비 1 cell/s, 반경 1, 예고 0.6초, panic 6 cells/s·최대 3칸, 회복 0.5초, 추격 3 cells/s·방향 유지 2칸이다. 수치는 사람 플레이 전까지 `Proposed`다.
- `ArmoredEnemySimulation`은 `ActorId(4)`로 같은 `GridState`를 점유하며 내구 `Armored → Broken → Dead`와 행동 상태를 분리한다. 첫 서로 다른 폭발은 폭탄 위력과 무관하게 갑옷 한 단계만 파괴하고, 두 번째 서로 다른 폭발은 Telegraph·Run·Recover·Chase 어느 단계에서도 사망시킨다. 같은 `BombId`의 중복 셀과 사망 뒤 피해는 무시한다.
- `Guard`는 저작 spawn을 원점으로 한 Manhattan 반경 1 안에서만 플레이어와 가까워지는 한 칸을 기존 느린 cadence로 선택한다. 반경 밖 추격과 플레이어에게서 멀어지는 왕복은 하지 않는다.
- 첫 유효 폭발의 실제 `BombExplosion.Origin`을 받아 현재 셀의 네 cardinal 직선 가지를 최대 3칸 조사한다. 가장 긴 유효 가지를 먼저 고르고, 길이가 같으면 폭발 반대 방향 투영→도착점의 폭발 중심 Manhattan 거리→`North → East → South → West` 순으로 고정한다. 유효 셀이 없으면 달리기를 생략하고 회복한다.
- `PanicTelegraph`에서 고정 경로 전체를 0.6초 예고한다. 이후 `PanicRun`은 6 cells/s cadence로 그 경로만 한 칸씩 소비하며, 새 벽·폭탄·actor가 다음 셀을 막으면 재조준하지 않고 즉시 `PanicRecover`가 된다. 경로 완료 또는 조기 차단 뒤 0.5초 회복하고 `Chase`에서 기존 3 cells/s 국소 추격을 시작한다.
- 플레이어와 cardinal 인접하면 같은 셀에 들어가지 않고 접촉 피해 1 후보를 만든다. 벽·폭탄·다른 actor는 수비·panic·추격 모두 권위 논리 장애물이며, panic 계획은 선택 뒤 기존 장애물이 사라져도 늘어나지 않는다.
- `PrototypeGameSession`은 추격자→돌진형→갑옷 적 순서로 이동·폭발 피해·접촉 후보를 처리하고 `ArmoredAdvanced`로 행동 전이와 확정 이동만 표현 계층에 전달한다. 첫 피격은 `ArmoredStateChanged(Broken)` 뒤 일반 `EnemyDamaged`, 두 번째 피격은 `ArmoredStateChanged(Dead)` 뒤 `EnemyDied`를 발행하고 마지막 생존 적이면 `RoomCleared`를 한 번 발행한다.
- `PrototypeArmoredPresenter`는 공유 재질을 복제하지 않고 `MaterialPropertyBlock`과 scale로 내구 상태를 구분한다. collider 없는 얇은 셀 placeholder를 최대 3개 풀링해 고정 panic 경로를 표시하고 Run 시작 또는 사망 때 회수하며, 수비·panic·추격 각각의 논리 cadence로 확정 이동을 보간한다.
- 네 번째 `prototype-combat-armor` 방은 플레이어 `(0,-2)`, 갑옷 적 `(0,1)`을 유지하면서 상단 막과 남쪽 진입로, 좌우 3칸 가지를 가진 T 교차점으로 바뀌었다. 파괴 가능 벽과 돌진형은 제외해 첫 폭발 방향→예고된 두 번째 설치 위치 질문을 분리한다.

## 구현된 자폭병

- `SelfDestructEnemyDefinition`은 안정적인 적 정의 ID, 일반 추적 cadence, 경고 중 최소 cadence·누적 시간, 경고·점화 Manhattan 거리와 자폭 폭탄 정의를 소유한다. `prototype-self-destruct`는 일반 추적 2 cells/s, 연속 경고 1.5초 동안 최대 5 cells/s, 경고 거리 3, 조기 점화 거리 1이고 `prototype-self-destruct-blast`는 fuse 0.75초, `Cross`, 범위 2를 사용한다. 수치는 후속 사람 플레이 전까지 `Proposed`다.
- `SelfDestructEnemySimulation`은 `ActorId(6)`으로 공유 `GridState`를 점유하고 `Chase → WarningChase → Telegraph → Detonated`를 결정론적으로 진행한다. 접촉 즉발과 접촉 피해는 없으며 플레이어 셀로 들어가지 않는다.
- 매 추적 cadence에 현재 플레이어 셀에서 역방향 BFS 거리장을 만들고 최단 거리가 작아지는 한 칸을 `North → East → South → West` 동률 순서로 선택한다. actor·폭탄 점유와 비바닥은 차단하며 경로가 없으면 임의 배회하지 않고 기다린다.
- 이동 뒤 Manhattan 3칸 이내면 `WarningChase`가 되어 누적을 시작한다. 범위 안에 연속으로 머무는 동안 한 칸 cadence는 0.5초에서 0.2초까지 선형으로 줄어들고, 1.5초가 끝나면 인접 여부와 관계없이 현재 셀에서 `Telegraph`가 된다. cadence 시작 시 1칸 이내면 그보다 먼저 점화한다. 플레이어가 3칸 밖으로 벗어나면 즉시 `Chase`로 돌아가 누적을 0으로 초기화하므로 열린 공간에서 5 cells/s 플레이어가 일찍 이탈하면 취소할 수 있다.
- `WarningChase`는 누적 진행도에 따라 정상색↔주황 경고색 pulse가 3→8Hz, 최대 scale이 1.08→1.18배로 연속 상승한다. 이동 표현도 각 Core 결과의 실제 가변 cadence를 사용한다. `Telegraph`에서는 이동을 멈추고 권위 셀에 고정하며 8Hz·최대 1.18배 pulse와 실제 범위 셀을 함께 표시한다. 이는 `MaterialPropertyBlock`과 기존 인스턴스 scale만 갱신하며 material 인스턴스를 만들지 않는다.
- 자폭병 표현은 collider와 Rigidbody가 없는 `SelfDestructPig` 프리팹을 사용한다. Presenter는 Chase·WarningChase 이동을 Idle/Run, 점화를 Telegraph, 폭발 종결을 Detonate로 변환하며 별도 Die 상태와 Root Motion은 사용하지 않는다.
- `Chase` 또는 `WarningChase` 중 플레이어 폭발에 맞으면 현재 권위 셀로 표현을 맞춘 뒤 즉시 Telegraph가 된다. 이후 플레이어 이동이나 추가 폭발은 원점과 상태를 다시 선택하지 않는다.
- Telegraph 시작 시 자폭병 소유의 논리 폭탄 하나를 같은 셀에 설치한다. 플레이어 슬롯·쿨타임과 분리되지만 기존 `BombSimulation`의 ID, fuse, 0.15초 연쇄 지연, 벽 차단과 파괴벽 규칙을 그대로 사용한다.
- 자기 폭발이 확정되면 자폭병 체력과 actor 점유를 한 번 제거하고 `EnemyDied`를 발행한다. 범위에 든 플레이어·다른 적·보스는 기존 대상별 피해와 무적 계약을 사용한다.
- 적 폭탄 자체는 일반 `BombPlaced` 플레이어 사건을 만들지 않지만 폭발은 정의별 기존 VFX와 `BombExploded` 경로를 재사용한다.
- 다섯 번째 `prototype-combat-gates` 방은 자폭병 `(3,0)`과 유도 anchor `(0,-2) → (0,2)`를 사용한다. anchor는 AI 목적지가 아니라 사람이 의도와 결과를 읽는 레벨 메타데이터다. 플레이어가 추적형 자폭병을 어느 anchor 쪽으로 끄느냐에 따라 범위 2 십자 폭발이 중앙 파괴문 `(0,-1)` 또는 `(0,1)` 중 한쪽만 먼저 연다. 첫 파괴 가능 벽이 해당 방향의 폭발 전파를 끝내므로 반대쪽 문까지 관통하지 않는다.

## 구현된 퇴로 차단 투척병

- `ThrowerEnemyDefinition`은 안정적인 적 정의 ID, 이동 cadence, Telegraph·비행·회복 시간, 체력, volley당 발수와 전용 폭탄 정의를 소유한다. `prototype-thrower`는 1 cell/s, 예고 0.3초, 비행 0.45초, 회복 0.75초, 체력 1, volley 3발이며 `prototype-thrower-blocker`는 fuse 1.5초·`Cross`·범위 1을 사용한다. 수치는 사람 플레이 전까지 `Proposed`다.
- `ThrowerEnemySimulation`은 전용 테스트에서 `ActorId(7)`로 공유 `GridState`를 점유한다. 시작점은 사격 anchor와 겹치지 않는 staging 셀이며 `Track`에서 첫 저작 사격 anchor까지 BFS 이동한다. 따라서 방 초기화 직후 현재 셀에서 Telegraph하지 않는다. anchor에 도착하면 저작 퇴로 차단 anchor를 현재 플레이어 셀과의 맨해튼 거리 오름차순으로 정렬한다. 가장 가까운 셀 1개는 압박 목표로 유지하고 나머지 2개는 현재 사격 anchor index에 따라 정렬된 잔여 후보를 순환하되 직전 volley에서 쓰지 않은 셀을 우선한다. 동률은 저작 순서를 유지하며 후보는 volley 발수의 두 배 이상이어야 한다.
- `Track → Telegraph` 경계에서 세 목표 셀을 잠근다. 0.3초 예고 중 플레이어가 움직여도 다시 조준하지 않으며, 현재 플레이어 위치 자체를 직접 목표로 계산하지 않는다.
- Telegraph 종료 뒤 서로 다른 세 목표로 0.45초 표현 비행을 동시에 시작한다. 발사와 함께 목표 셀 예고를 숨기고 비행 중에는 폭발 범위를 미리 표시하지 않는다. 이때는 아직 격자를 점유하는 폭탄이 아니며, 각 착탄 순간 같은 `BombSimulation`에 `ActorId(7)` 소유 폭탄으로 설치해 fuse·벽 차단·0.15초 연쇄 지연을 기존 폭탄과 공유한다.
- 착탄 셀을 다른 폭탄이 먼저 점유하면 재조준하거나 중복 생성하지 않고 해당 발만 실패한다. 같은 volley의 나머지 발은 독립적으로 착탄한다. 비행 대기 수와 모든 활성 BombId가 해제될 때까지 다음 volley를 만들지 않으며, 이후 다음 저작 사격 anchor를 순환한다.
- 투척병은 자기 폭탄 피해를 무시하지만 플레이어·다른 적 소유 폭발에는 체력 1 규칙으로 사망한다. 사망과 actor 점유 제거는 한 번만 처리되고 마지막 적이면 기존 단일 방 클리어를 사용한다.
- `PrototypeThrowerPresenter`는 collider와 Rigidbody가 없는 `ThrowerPig` 프리팹을 Core 연속 위치에 표시한다. Track 이동은 Idle/Walk, Telegraph 진입은 Throw, Recover는 Idle, 사망은 terminal Die 표현으로 변환하고 Root Motion은 사용하지 않는다. 재사용 셀 풀은 Telegraph 중 잠긴 목표 3개를 표시하고 발사 때 숨긴다. 성공 착탄마다 현재 논리 격자에서 계산한 해당 폭탄의 Core 폭발 미리보기 범위를 다시 표시하며, 겹치는 셀은 중복하지 않고 실제 폭발한 `BombId`의 범위만 제거한다. `PrototypeBombPresenter`는 세 포물선 비행을 각각 풀링 표현한 뒤 성공 착탄 시 대응 폭탄 표현으로 넘긴다.
- `prototype-combat-thrower`는 메인 던전의 `TestSandboxThrower`와 독립 `ThrowerLanesPlaytest.unity`가 함께 사용하는 권위 room이다. 플레이어 `(0,-2)`, 추격자 `(-2,2)`, 투척병 staging `(3,2)`, 사격 anchor `(0,3)·(-3,2)·(3,-2)`, 목표 후보 `(0,0)·(-3,-2)·(2,-3)·(-4,1)·(4,1)·(0,2)`를 사용한다. 첫 공격은 중앙·하단 양측 3칸을 사용하고 다음 사격 위치에서는 중앙 압박점과 다른 측면 2칸을 조합한다. staging→첫 사격 anchor는 4칸이며 두 적은 모든 잠재 출구에서 4칸 이상 떨어진다. 추격자 시작점은 모든 초기 목표 폭발 반경 밖이므로 입장 직후 첫 적 폭탄이 일반병을 자동 처치하지 않는다.

## 불변식

- 적 판단은 보이지 않는 Transform 검색이나 프레임 순서에 의존하지 않는다.
- 공격 또는 돌진은 플레이어가 인식할 수 있는 예고를 가진다.
- 위험 셀이나 벽 통과 여부는 격자 계약을 따른다.
- 사망한 적은 점유와 방 클리어 집계에서 한 번만 제거된다.
- 상태 변경 이벤트와 애니메이션은 구분한다. 애니메이션 실패가 논리 상태를 되돌리지 않는다.

## 경로 탐색

프로토타입에서는 격자 기반의 단순하고 결정적인 규칙을 사용한다. 추격자와 자폭병은 플레이어 셀에서 역방향 BFS 거리장을 만들고, 돌진형은 현재 셀에서 가장 가까운 유효 정렬 후보를 BFS로 찾는다. 투척병은 다음 저작 사격 anchor까지 BFS로 이동하되 목표 폭탄 셀은 플레이어와 저작 퇴로 anchor의 거리만으로 잠근다. 갑옷 적의 수비·추격은 제한된 국소 Manhattan 선택을 사용하고 panic은 폭발 시점에 최대 3칸 cardinal 직선 가지를 한 번 잠근다. 모두 매 frame이 아니라 각자의 명시 cadence와 전이 경계에서만 판단하며 목표/자기 셀 외의 actor·폭탄 점유와 비바닥을 차단한다. 경로가 없으면 임의 배회하지 않는다. 탐색 저장소와 panic 배열은 simulation이 재사용하며 플레이어 미래 위치나 폭발 위험을 계산하지 않는다. AI Navigation 패키지는 Core 경로 판정의 원본이 아니다.

## 자동 테스트

- 기본 추격자 정의 ID·내구도·접촉 피해·cadence·방향 유지 값 검증.
- 즉시 첫 이동, 정확한 cadence 경계, 결정론적 동률 선택.
- 목표 셀 변경 중 방향 유지와 유지 종료 후 재판단.
- 폭탄으로 막힌 방향의 재선택과 플레이어 인접 정지.
- Manhattan 동률의 막힌 포켓 대신 BFS 최단 가지 선택, committed overshoot 중단, 목표 경로가 없을 때 대기.
- 시계 역행 거부와 잘못된 spawn·목표 거부.
- 단일 폭발 사망, 같은 폭발 중복과 사망 뒤 피해 차단.
- PlayMode의 공유 격자 이동·표현 보간·폭발 처치·점유 제거·단일 방 클리어.
- 한 칸 이동 직후 접촉 보류, 정확한 도착 경계의 접촉 허용, 도착 전 플레이어 이탈 시 취소, 제자리 인접과 공유 무적 재피해 및 같은 프레임 폭발 사망 우선순위.
- 돌진형의 네 방향 차선 획득, 결정론적 동률, 획득 cadence, 장애물 우회·경로 없음, 정렬·가시선, 방향·최대 거리 잠금, 예고·돌진·회복 경계, 플레이어 충돌과 벽·폭탄·actor 조기 차단.
- PlayMode의 획득/돌진별 보간 속도와 전체 고정 차선 예고 placeholder 생성·회수.
- PlayMode의 두 적 공유 점유, 돌진 충돌 단일 피해, 두 적 동시 폭발 사망의 `EnemyDied` 순서와 단일 방 클리어, 적별 presenter 생존 상태.
- 갑옷 적의 첫/두 번째 서로 다른 폭발, 같은 `BombId` 중복, 사망 뒤 무시와 panic 단계 중 치명 피해.
- 갑옷 적의 수비 반경·거리 감소, 네 방향·대각선·동률 panic 선택, 최대 거리 고정, Telegraph·Run·Recover·Chase의 정확한 시간 경계와 시계 역행.
- 갑옷 적 panic의 벽·폭탄·actor 초기 차단, 실행 중 새 장애물의 조기 회복, 기존 장애물 제거 뒤 경로 비확장, 유효 가지 없음과 cardinal 인접 접촉.
- PlayMode의 첫 피격 내구 표현·전체 경로 예고·상태별 이동 보간·예고 회수, 두 번째 사망·점유 제거·단일 방 클리어와 적별 presenter 생존 상태.
- 자폭병 정의 경계, 일반·경고 최소 cadence와 누적 시간, 현재 플레이어 BFS 추적·결정론적 우회·경로 없음, 경고 범위 진입/이탈·누적 초기화, 연속 노출 가속과 인접 전 1.5초 자동 점화, 인접 진입 뒤 판단 경계, 플레이어 폭발 trigger·중복 무시.
- 자폭병 소유 폭탄의 arm·연쇄·detonation ID, 자기 폭발 사망·단일 점유 제거와 Gates 한쪽 파괴문 개방.
- PlayMode의 자폭병 경고 추적 상태와 색·scale pulse, 이동 중 플레이어 폭발 trigger 시 권위 셀 고정, 기존 폭탄 스케줄러의 실제 자폭과 파괴벽 변경·단일 사망 사건.
- Development WebGL의 `self-destruct-cell-x-<x>-z-<z>`와 `self-destruct-warning-chase` marker로 실제 확정 셀·경고 진입을 관측한다. 이 marker는 자동 경로 동기화용이며 점멸의 가독성이나 행동의 재미를 통과 판정하지 않는다.
- 투척병 정의 경계, 사격 anchor 밖 staging에서 첫 anchor까지 선행 Track, 현재 셀 대신 저작 후보의 거리·동률 순서, 가장 가까운 압박점과 사격 anchor별 측면 2칸 순환, 직전 volley 측면 재사용 회피, Telegraph 다중 목표 고정, 사격 anchor BFS·순환, 정확한 시간 경계, 시계 역행, 경로 없음, 3발 대기/활성 추적, 발별 착탄 실패와 폭탄 해결 통지.
- 방 정의의 투척 spawn·사격/목표 anchor 완전성·비중첩과 방 회전 시 세 목록의 원자적 회전.
- PlayMode의 이동·예고·비행·착탄 표현, 공유 폭탄 스케줄러와 플레이어 폭탄 연쇄, 다른 소유자 폭발 사망.
- 전용 Development WebGL에서 서로 다른 `thrower-telegraph-x-*` 3개 뒤 `thrower-bomb-launched`와 `thrower-bomb-armed-definition-prototype-thrower-blocker`가 각각 3번 발생하고 하나 이상 `thrower-bomb-detonated-by-chain`으로 이어지는 순서를 검증한다. 이 표식은 규칙 연결을 증명하지만 예고 가독성·압박·재미를 판정하지 않는다.

다음 판단은 전용 Lanes 씬에서 사용자 요청으로 단축한 0.3초 예고가 위협적이면서도 다음 퇴로 선택을 바꿀 최소 가독성을 유지하는지, 1.5초 fuse·범위 1이 회피와 의도적 연쇄 둘 다 가능하게 하는지, 가장 가까운 1칸을 유지하면서 측면 2칸이 바뀌는 사격 anchor 순환이 반복 포격을 줄이는지를 사람 플레이로 확인하는 것이다. 지지될 때만 메인 던전 카탈로그 편성과 기존 Lanes 조합을 별도 수직 슬라이스로 진행한다. 범용 다중 적 ID 발급과 동일 목적 셀 경합 정책은 여러 동일 적을 실제로 추가할 때까지 보류한다.

## 공통 이동 표현 계약

- `EnemyLocomotionState.Moving`은 Core의 커밋된 한 칸 이동이 진행 중임을 나타낸다.
- 이동을 허용하는 행동 phase에서 실제 이동이 성공하면 `Moving`을 저장하고 다음 cadence까지 유지한다. 경로·점유·목표 부재로 이동 시도가 실패하거나 목적지 대기 및 명시적 비이동 phase에 들어가면 `Idle`로 전환한다. Charger의 Charge 진입은 즉시 첫 charge step을 예약하므로 `Moving` 의도이며, Presenter는 전용 Charge 애니메이션을 우선한다.
- Presenter는 Core의 연속 위치와 locomotion 상태를 읽을 뿐, 별도 위치 보간이나 이동 타이머를 소유하지 않는다.
- Telegraph·Recover·Detonated는 진행 중인 한 칸 이동이 100% 완료된 뒤 진입한다. 사망은 예약을 해제하고 전용 terminal 표현을 우선한다.
- 각 적 simulation은 호환용 이동 사건의 `From`, `To`, `StartedAt`, `EndsAt`과 별개로 실제 연속 위치와 50% 판정 칸을 Core에서 소유한다.
- transition은 논리 점유를 지연하지 않는다. Core의 정수 격자 위치는 이동 판단 시 즉시 `To`로 확정되고, transition은 그 결과를 프레임 독립적으로 표현하기 위한 읽기 전용 구간이다.
