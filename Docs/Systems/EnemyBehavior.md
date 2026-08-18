# 적 행동

- 상태: 프로토타입 3종 역할 `Accepted`, 세부 상태/수치 `Proposed`
- 설계 원본: `GDD_v0.2.md` 16~18장
- 코드 소유: 판단은 `BombSwap.Core`, 이동/애니메이션은 `BombSwap.Unity`

## 목적

적이 단순 체력 벽이 아니라 플레이어가 폭탄 위치와 탈출 경로를 바꾸게 만드는 공간 압력으로 작동하게 한다.

## 프로토타입 적

| 적 | 공간 역할 | 핵심 상태 |
|---|---|---|
| 추격자 | 지속 압박과 기본 유도 학습 | Acquire, Chase, Repath, Hit, Dead |
| 돌진형 | 예고 후 직선 경로를 강제 | Track, Telegraph, Charge, Recover, Dead |
| 갑옷 적 | 첫 적중 뒤 행동/외형 변화 | Armored, Broken, Hit, Dead |

AI Inference 런타임은 사용하지 않는다. 재현 가능한 상태 머신과 격자/경로 규칙을 우선한다.

## 구현된 기본 추격자

- `ChaserEnemyDefinition`은 안정적인 `EnemyDefinitionId`, 내구도, 접촉 피해, 이동 step 간격, 한 번 선택한 방향을 유지할 성공 step 수를 소유한다.
- TestSandbox의 검증된 `PrototypeChaserDefinitionAsset` 값은 내구도 1, 접촉 피해 1, 이동 2 cells/s, 방향 유지 2칸이다. 이 수치는 플레이테스트 전까지 `Proposed`다.
- `ChaserEnemySimulation`은 `ActorId(2)`로 공유 `GridState`를 점유하고 `ActorId(1)` 플레이어 위치를 논리 목표로 읽는다. 일반 ID 발급과 다중 적 수명 주기는 후속 작업이다.
- 첫 판단은 즉시 가능하고 이후 0.5초 cadence를 사용한다. 방향을 새로 고를 때 플레이어 셀에서 역방향으로 만든 BFS 거리장에서 도달 가능한 상하좌우 중 최단 거리 셀을 택한다.
- 동률이면 기존 방향을 우선하고, 새 방향끼리는 `North → East → South → West` 순서로 고정한다. 선택한 방향은 최대 두 칸 유지하되 벽·actor·폭탄에 막히거나 다음 committed step이 계획 당시 최단 거리를 늘리면 즉시 다시 판단한다.
- 플레이어와 cardinal 인접하면 같은 셀에 들어가지 않고 멈춘다. 살아 있는 동안 기존 플레이어 체력·공유 무적 계약으로 접촉 피해 1을 준다.
- 추격자는 fuse나 폭발 위험 셀을 읽지 않는다. 설치 폭탄은 다른 논리 이동 장애물과 동일하게만 취급한다.
- `EnemyHealthSimulation`은 폭발 `BombId`별 중복 처리를 차단한다. 내구도 1 추격자가 폭발 영향 셀에 있으면 사망하고 actor 점유가 한 번 제거되며, 세션은 `EnemyDied`와 마지막 적의 `RoomCleared`를 한 번 발행한다.
- `PrototypeChaserPresenter`는 Core 이동 결과를 선형 보간하고 사망 색을 `MaterialPropertyBlock`으로 잠시 표시한 뒤 placeholder를 비활성화한다.

이전 국소 Manhattan 선택은 seed-0 전체 경로에서 `(1,-3) → (1,-4) → (1,-5)` 왕복을 만들었다. 현재 BFS 계약은 막힌 포켓보다 실제 도달 가능한 최단 가지를 선택하고, 두 칸 방향 유지가 계획 거리 증가를 강제하기 전에 재계획해 이 반복 원인을 제거한다. 수치와 공간 압력의 재미는 여전히 사람 플레이테스트 대상이다.

## 구현된 돌진형

- `ChargerEnemyDefinition`은 안정적인 `EnemyDefinitionId`, 내구도, 충돌 피해, 차선 획득 step 간격, 예고 시간, 한 셀 돌진 cadence와 회복 시간을 소유한다. TestSandbox의 `prototype-charger` 값은 내구도 1, 충돌 피해 1, 차선 획득 1 cell/s, 예고 0.75초, 돌진 8 cells/s, 회복 1초이며 플레이테스트 전까지 `Proposed`다.
- `ChargerEnemySimulation`은 `ActorId(3)`으로 추격자와 같은 `GridState`를 점유한다. `Track`에서 현재 플레이어와 장애물 없는 행/열이 아니면, 가장 가까우면서 플레이어까지 명확한 cardinal 차선을 만드는 도달 가능 셀을 BFS로 찾고 첫 한 칸만 이동한다.
- 차선 후보와 획득 경로 동률은 `North → East → South → West`다. 첫 판단은 즉시 가능하고 이후 1초 cadence에서만 플레이어의 현재 논리 셀을 다시 읽는다. 벽·파괴벽·폭탄·다른 actor는 경로와 정렬 차선을 막으며 유효 후보가 없으면 배회하지 않는다.
- 정렬이 확인되면 `Track → Telegraph`로 전환하면서 방향과 현재 장애물까지의 최대 돌진 거리를 잠근다. 예고 중 플레이어가 이동하거나 기존 장애물이 사라져도 방향·최대 거리를 바꾸거나 늘리지 않는다.
- `PrototypeChargerPresenter`는 고정 최대 거리의 모든 논리 셀에 collider 없는 얇은 예고 placeholder를 풀링해 표시하고 `Charge` 시작 때 회수한다. 획득 이동은 1 cell/s, 돌진 이동은 8 cells/s로 각각 확정 Core step을 보간한다.
- 예고 종료 뒤 `Charge`에서 고정 방향으로 한 셀씩 이동한다. 플레이어가 다음 셀에 있으면 겹치지 않은 채 단일 충돌 피해 후보를 만들고, 고정 최대 거리를 소진하거나 고정 벽·파괴벽·새 폭탄·다른 actor가 먼저 막으면 피해 없이 `Recover`가 된다.
- 폭탄 충돌의 고정 지연 연쇄와 별도 기절은 기존 bomb scheduler·소유권 계약을 확장해야 하므로 현재는 `Deferred`다. 폭탄은 다른 논리 점유 장애물과 같이 조기 회복만 만든다.
- `PrototypeGameSession`은 같은 frame에서 추격자 다음 돌진형 순서로 이동을 확정한다. 폭발 피해는 추격자 다음 돌진형 순서로 적용하고, 모든 `EnemyDied` 뒤 살아 있는 적이 없을 때만 `RoomCleared`를 한 번 발행한다.
- 각 적 presenter는 전체 적 수가 아니라 자신의 생존 상태만 읽어 다른 적 사망 뒤 잘못 활성화되지 않는다.
- `prototype-combat-pillars`는 플레이어 `(-3,-2)`, 돌진형 `(0,1)`의 비정렬 상태로 시작한다. 남쪽 정렬 셀 획득, 서쪽 고정 돌진, 종단 기둥과 북/동 측면 탈출을 첫 관찰 경로로 사용한다.

## 구현된 갑옷 적

- `ArmoredEnemyDefinition`은 안정적인 `EnemyDefinitionId`, 갑옷/파괴 상태별 이동 간격, 접촉 피해와 방향 유지 성공 step 수를 소유한다. TestSandbox의 `prototype-armored` 값은 접촉 피해 1, 갑옷 상태 1 cell/s, 파괴 상태 3 cells/s, 방향 유지 2칸이며 플레이테스트 전까지 `Proposed`다.
- `ArmoredEnemySimulation`은 `ActorId(4)`로 같은 `GridState`를 점유하고 `Armored → Broken → Dead` 상태를 소유한다. 첫 서로 다른 폭발은 폭탄 위력과 무관하게 갑옷 한 단계만 파괴하고, 두 번째 서로 다른 폭발이 사망시킨다. 같은 `BombId`의 중복 셀과 사망 뒤 피해는 무시한다.
- 첫 피격은 기존 방향 유지와 다음 이동 대기를 버린다. 다음 frame부터 3 cells/s cadence로 기본 추격자와 같은 국소 Manhattan 선택과 `North → East → South → West` 동률 규칙을 다시 평가한다.
- 플레이어와 cardinal 인접하면 같은 셀에 들어가지 않고 접촉 피해 1 후보를 만든다. 벽·폭탄·다른 actor는 두 상태 모두 같은 논리 장애물로 취급한다.
- `PrototypeGameSession`은 추격자→돌진형→갑옷 적 순서로 이동, 폭발 피해, 접촉 후보를 처리한다. 첫 피격은 `ArmoredStateChanged(Broken)` 뒤 일반 `EnemyDamaged`, 두 번째 피격은 `ArmoredStateChanged(Dead)` 뒤 `EnemyDied`를 발행하고 마지막 생존 적이면 `RoomCleared`를 한 번 발행한다.
- `PrototypeArmoredPresenter`는 공유 재질을 복제하지 않고 `MaterialPropertyBlock`과 scale로 갑옷 상태, 파괴 상태, 사망을 구분하고 상태별 논리 cadence에 맞춰 확정 이동을 보간한다.
- 네 번째 `prototype-combat-armor` 방만 플레이어 `(0,-2)`, 갑옷 적 `(0,1)`의 열린 중앙 실험선으로 시작한다. 파괴 가능 벽과 돌진형을 제외해 2회 피격 가설을 다른 가설과 섞지 않는다.

## 불변식

- 적 판단은 보이지 않는 Transform 검색이나 프레임 순서에 의존하지 않는다.
- 공격 또는 돌진은 플레이어가 인식할 수 있는 예고를 가진다.
- 위험 셀이나 벽 통과 여부는 격자 계약을 따른다.
- 사망한 적은 점유와 방 클리어 집계에서 한 번만 제거된다.
- 상태 변경 이벤트와 애니메이션은 구분한다. 애니메이션 실패가 논리 상태를 되돌리지 않는다.

## 경로 탐색

프로토타입에서는 격자 기반의 단순하고 결정적인 BFS를 사용한다. 추격자는 플레이어 셀에서 역방향 거리장을 만들고, 돌진형은 현재 셀에서 가장 가까운 유효 정렬 후보를 찾는다. 둘 다 매 frame이 아니라 각자의 명시 cadence와 재계획 경계에서만 탐색하며, 목표/자기 셀 외의 actor·폭탄 점유와 비바닥을 차단한다. 경로가 없으면 임의 배회하지 않고 다음 cadence까지 기다린다. 탐색용 `Dictionary`·`HashSet`·`Queue`는 각 simulation이 재사용하며 플레이어 미래 위치나 폭발 위험을 계산하지 않는다. AI Navigation 패키지는 Core 경로 판정의 원본이 아니다.

## 자동 테스트

- 기본 추격자 정의 ID·내구도·접촉 피해·cadence·방향 유지 값 검증.
- 즉시 첫 이동, 정확한 cadence 경계, 결정론적 동률 선택.
- 목표 셀 변경 중 방향 유지와 유지 종료 후 재판단.
- 폭탄으로 막힌 방향의 재선택과 플레이어 인접 정지.
- Manhattan 동률의 막힌 포켓 대신 BFS 최단 가지 선택, committed overshoot 중단, 목표 경로가 없을 때 대기.
- 시계 역행 거부와 잘못된 spawn·목표 거부.
- 단일 폭발 사망, 같은 폭발 중복과 사망 뒤 피해 차단.
- PlayMode의 공유 격자 이동·표현 보간·폭발 처치·점유 제거·단일 방 클리어.
- PlayMode의 cardinal 접촉 피해·공유 무적 재피해와 같은 프레임 폭발 사망 우선순위.
- 돌진형의 네 방향 차선 획득, 결정론적 동률, 획득 cadence, 장애물 우회·경로 없음, 정렬·가시선, 방향·최대 거리 잠금, 예고·돌진·회복 경계, 플레이어 충돌과 벽·폭탄·actor 조기 차단.
- PlayMode의 획득/돌진별 보간 속도와 전체 고정 차선 예고 placeholder 생성·회수.
- PlayMode의 두 적 공유 점유, 돌진 충돌 단일 피해, 두 적 동시 폭발 사망의 `EnemyDied` 순서와 단일 방 클리어, 적별 presenter 생존 상태.
- 갑옷 적의 첫/두 번째 서로 다른 폭발, 같은 `BombId` 중복, 사망 뒤 무시, 1→3 cells/s cadence 경계와 첫 피격 재판단.
- 갑옷 적의 벽·폭탄·actor 차단, cardinal 인접 접촉, 시계 역행과 잘못된 정의·spawn 거부.
- PlayMode의 첫 피격 표현·상태 사건·빠른 이동, 두 번째 사망·점유 제거·단일 방 클리어와 적별 presenter 생존 상태.
- Development WebGL의 `chaser-cell-x-<x>-z-<z>` marker로 실제 확정 셀 이동과 플레이어 cardinal 인접 상태를 관측한다. 이 marker는 자동 경로 동기화와 순환 진단용이며 행동의 재미를 통과 판정하지 않는다.

다음 적 단계에서는 범용 다중 적 ID 발급과 동일 목적 셀 경합 정책을 추가한다.
