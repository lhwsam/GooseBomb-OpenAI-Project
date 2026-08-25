# 작업: 추적형 자폭병과 Gates 비대칭 상호작용

- 상태: Core·Unity·Development WebGL 정확성 `Complete / Accepted`, 튜닝·사람 재미 검증 `Proposed`
- 기준일: 2026-08-26
- 변경 이유: 기존 anchor 자동 이동형 자폭병이 플레이어를 따라오는 위협으로 읽히지 않고 움직임도 부자연스럽다는 사람 플레이 피드백

## 플레이어 계약

- 자폭병은 플레이어를 계속 따라온다.
- 가까워지면 본체가 홀로그램으로 점멸해 위험 단계가 바뀌었음을 미리 보여준다. 본체 scale은 바꾸지 않는다.
- 경고 범위 안에 계속 있으면 추격이 빨라지고, 1.5초 안에 인접하지 못해도 현재 셀에서 멈춰 더 빠른 홀로그램 점멸과 실제 폭발 범위를 표시한다.
- 플레이어가 그 전에 경고 범위 밖으로 확실히 벗어나면 누적이 초기화되고 자폭병은 일반 속도로 다시 따라온다.
- 점화가 시작된 뒤에는 취소되지 않는다. 플레이어는 0.75초 안에 범위 밖으로 피해야 한다.
- 플레이어 폭발로 추적 중인 자폭병을 맞히면 즉사시키는 대신 그 자리에서 점화할 수 있다.
- Gates에서는 플레이어가 자폭병을 아래 또는 위 중앙문 근처로 유인해 어느 문을 먼저 열지 선택할 수 있다.

## 상태와 전이

| 상태 | 논리 동작 | 표현 | 전이 |
|---|---|---|---|
| `Chase` | 0.5초마다 현재 플레이어 셀로 BFS 한 칸 추적 | 저작 material 색 유지, 이동 보간 | 이동 뒤 Manhattan 3칸 이내면 `WarningChase`; cadence 시작 시 1칸 이내면 `Telegraph`; 플레이어 폭발 피격 시 `Telegraph` |
| `WarningChase` | 연속 노출 1.5초 동안 cadence가 0.5→0.2초로 줄어들며 계속 추적 | 저작 scale 유지, 0.14초 간격 선택적 홀로그램 점멸, 실제 cadence 이동 보간 | 3칸 밖이면 누적 초기화 후 `Chase`; 1칸 이내 또는 누적 1.5초면 `Telegraph`; 플레이어 폭발 피격 시 `Telegraph` |
| `Telegraph` | 현재 셀에 적 소유 논리 폭탄 하나를 설치하고 이동 중단 | 권위 셀 고정, Telegraph 애니메이션, 0.065초 간격 선택적 홀로그램 점멸, 범위 셀 표시 | 일치하는 무장 폭탄이 폭발하면 `Detonated` |
| `Detonated` | actor 점유와 체력을 한 번 제거 | Detonate 애니메이션 뒤 짧게 숨김 | 종결 |

`Chase → Telegraph` 직접 전이는 플레이어가 두 판단 사이에 스스로 인접한 경우 허용한다. `WarningChase`를 반드시 한 frame 이상 거쳐야 한다는 규칙은 없다. `WarningChase` 중에는 매 frame 현재 거리를 확인해 이탈을 먼저 반영하고, 누적 만료는 가변 이동 cadence와 독립적으로 판정한다.

## Core 규칙

### 정의

`SelfDestructEnemyDefinition`이 다음 검증된 값을 소유한다.

- 안정적인 `EnemyDefinitionId`
- 양수 `ChaseStepInterval`
- 양수이며 일반 추적 cadence 이하인 `WarningMinimumStepInterval`
- 양수 `WarningEscalationDuration`
- 양수 `WarningDistance`
- 양수이며 경고 거리보다 작은 `PrimeDistance`
- 양수 범위의 `Cross` 자폭 폭탄 정의

현재 `prototype-self-destruct` 값은 일반 추적 2 cells/s, 경고 최대 5 cells/s, 연속 경고 1.5초, 경고 거리 3, 조기 점화 거리 1이다. 자폭 폭탄 `prototype-self-destruct-blast`는 fuse 0.75초, `Cross`, 범위 2다. 모두 후속 사람 플레이 확정 전 `Proposed`다.

### 추적

- 현재 플레이어 셀에서 역방향 BFS 거리장을 만든다.
- 한 칸 후보는 `North → East → South → West` 순서로 평가하고 가장 작은 BFS 거리를 선택한다. 같은 거리는 앞선 방향을 유지한다.
- 플레이어 목표 셀과 자폭병 현재 셀만 경로 계산 예외로 허용한다. 다른 actor·폭탄 점유, `Void`, 고정 벽과 파괴 가능 벽은 통과하지 않는다.
- 플레이어 셀은 경로 목표일 뿐 실제 이동 목적지가 아니다. cadence 시작 시 Manhattan 거리가 점화 거리 이하면 먼저 `Telegraph`로 전환하므로 두 actor는 겹치지 않는다.
- 경로가 없으면 임의 배회하거나 Transform 방향으로 밀지 않고 현재 셀에서 기다린다.
- BFS `Dictionary`와 `Queue`는 simulation이 재사용한다. frame 반복 경로에서 LINQ나 임시 컬렉션을 만들지 않는다.

### 점화 타이밍

- 한 이동으로 플레이어 인접 셀에 들어간 결과는 `WarningChase` 이동 사건만 발행한다.
- 다음 가변 cadence 시작 때도 플레이어가 1칸 이내면 그때 `Telegraph`가 된다. 이 규칙이 논리 위치와 해당 이동의 시각 보간 불일치를 막는다.
- 경고 범위 안의 누적 진행도는 주입 시계로 계산한다. 진행도 0→1에 맞춰 다음 이동 간격을 0.5→0.2초로 선형 보간하고, 1.5초 만료는 다음 이동을 기다리지 않고 현재 셀에서 `Telegraph`를 시작한다.
- 그 사이 플레이어가 3칸 밖으로 벗어나면 같은 frame에 `Chase`로 돌아가 누적을 초기화한다. 다시 경고 범위에 들어오면 0부터 새로 시작한다.
- 플레이어 폭발 피격은 cadence를 기다리지 않고 현재 논리 셀에서 `Telegraph`를 시작한다. presenter는 진행 중 보간을 끝내고 그 권위 셀로 즉시 맞춘다.
- `Telegraph` 이후에는 플레이어 거리나 추가 폭발로 상태를 되돌리지 않는다. 이미 `BombSimulation`에 실제 폭탄이 존재하기 때문이다.

### 폭발과 사망

- `Telegraph` 시작 시 `ActorId(6)` 소유의 자폭 폭탄을 자폭병 현재 셀에 설치한다.
- 이 폭탄은 플레이어 슬롯·설치 쿨타임·`BombPlaced` 입력 성공 사건을 사용하지 않는다.
- 활성 폭탄 목록, `BombId`, fuse, 0.15초 고정 연쇄 지연, 벽 차단, 파괴 가능 벽 규칙과 `BombExploded` 결과는 기존 `BombSimulation`을 그대로 사용한다.
- 일치하는 무장 폭탄의 폭발만 `CompleteDetonation`을 호출할 수 있다.
- 자기 폭발이 확정될 때 체력 1과 actor 점유를 한 번 제거하고 `EnemyDied`를 발행한다. 플레이어 폭발은 자폭병을 즉시 제거하지 않는다.

## Unity 저작과 표현

- `PrototypeSelfDestructDefinitionAsset`의 기존 `approachCellsPerSecond` 직렬화 데이터는 `FormerlySerializedAs`로 `chaseCellsPerSecond`에 이관한다.
- 정의 asset은 `ChaseCellsPerSecond`, `WarningMaxCellsPerSecond`, `WarningEscalationSeconds`, `WarningDistance`, `PrimeDistance`, 자폭 폭탄, 적 prefab, Telegraph 셀 prefab과 표현 높이·사망 시간을 제공한다.
- `PrototypeSelfDestructPresenter`는 논리 상태를 소유하지 않는다. `SelfDestructAdvanced`와 `EnemyDied` 사건만 소비한다.
- 경고·점화 표현은 Transform scale을 바꾸지 않는다. Git 제외 로컬 Holograms 설정이 있으면 캐시한 Scanline shared material과 `MaterialPropertyBlock`으로 점멸하고, 없으면 Animator·범위 셀만 유지한다.
- `WarningChase` 중에는 각 `SelfDestructEnemyAdvanceResult.MovementDuration`으로 이동 보간 시간을 갱신해 논리 가속과 표현 속도를 일치시킨다. `Telegraph` 시작 시 보간을 중단하고 `CurrentSelfDestructGridPosition`으로 맞춘 뒤 점멸 간격을 0.065초로 바꾸고 폭발 셀을 표시한다.
- 사용자 정의 pause 중에는 홀로그램 점멸 시간도 진행하지 않는다.

## Gates 저작 계약

- 플레이어 spawn `(0,-3)`, 추격자 `(0,3)`, 자폭병 `(3,0)`을 유지한다.
- 고정 장벽은 `z=-1·1`의 `x=-2,-1,1,2`, 중앙 파괴문은 `(0,-1)·(0,1)`, 좌우 우회로는 `x=±3`이다.
- 자폭 유도 anchor `(0,-2)·(0,2)`는 AI가 읽는 waypoint가 아니다. 다음 세 목적을 가진 레벨 메타데이터다.
  - 사람이 의도한 대표 유인 위치를 문서와 scene에서 공유한다.
  - Content Validator가 실제 Core 범위 2 십자 폭발 해석으로 각 anchor가 정확히 가까운 중앙문 하나만 파괴하는지 확인한다. 첫 파괴 가능 벽은 해당 ray 전파를 끝낸다.
  - 자동·사람 플레이테스트가 같은 셀을 관찰 기준으로 사용한다.
- 아래 유도 예시는 플레이어가 `(0,-3)`에 머물면 자폭병이 `(3,0) → (3,-1) → (3,-2) → (2,-2) → (1,-2) → (0,-2)`로 최단 추적한 뒤 다음 cadence에 점화하는 흐름이다. 장애물이나 다른 actor 점유가 바뀌면 같은 목적지로 다른 최단 경로를 선택할 수 있다.
- 두 문을 모두 파괴하지 않아도 초기 플레이 가능 셀은 좌우 우회로로 연결되어야 한다.

## 세션 처리 순서와 관찰 사건

- `PrototypeGameSession.Update`는 기존 적 순서대로 자폭병 `Advance`를 처리하고 `ShouldArm`이면 논리 폭탄을 설치한 뒤 `SelfDestructAdvanced`를 발행한다.
- 폭탄 fuse와 폭발은 적 이동 처리 뒤 기존 폭탄 단계에서 확정한다.
- 플레이어 폭발은 `Chase`와 `WarningChase`에서만 강제 점화를 시작한다.
- Development WebGL probe는 다음을 제공한다.
  - `self-destruct-cell-x-<x>-z-<z>`: 초기 셀과 모든 확정 이동 셀
  - `self-destruct-moved`: 첫 이동 확인
  - `self-destruct-warning-chase`: 첫 경고 진입
  - `self-destruct-armed`, `self-destruct-telegraph`
  - `self-destruct-detonated`, `self-destruct-died`

## 불변식

- Core는 UnityEngine, Transform, Collider, frame 시간과 전역 Random을 참조하지 않는다.
- 논리 격자와 주입 시계가 이동·거리·점화·폭발 원점의 권위다.
- 자폭병은 플레이어와 같은 셀을 점유하거나 접촉 즉발·접촉 피해를 주지 않는다.
- 경고 표현 실패가 상태를 바꾸지 않고, 상태 전이가 표현에서 역으로 Core를 조작하지 않는다.
- 실제 폭탄 설치 뒤 취소·원점 변경·즉시 재귀 폭발을 허용하지 않는다.
- Gates anchor 데이터와 AI 추적 목표를 다시 결합하지 않는다.

## 비목표

- 같은 종류 여러 마리와 범용 적 `ActorId` 발급
- 여러 자폭병의 동일 목적 셀 경합 정책
- 최종 모델·애니메이션·사운드와 홀로그램 점멸 강도 확정
- 플레이어와 거리가 벌어졌을 때 이미 시작한 fuse 취소
- 장애물 파괴를 예측하는 경로 계획, 폭발 위험 회피, 미래 플레이어 위치 예측
- 투척병과 범용 적 폭탄 UI

## 검증 계약

- EditMode는 정의 경계, 0.5초 일반 cadence·0.2초 경고 최소 cadence·1.5초 누적, 현재 플레이어 BFS 추적, 결정론적 우회와 경로 없음, 경고 진입·이탈과 재진입 초기화, 연속 노출 가속·인접 전 자동 점화, 인접 뒤 판단 경계, 두 추적 상태의 플레이어 폭발 trigger, arm·detonation ID와 범위 2 예고 셀을 검증한다.
- PlayMode는 `WarningChase` 진입, 저작 scale 유지·선택적 홀로그램 점멸, 이동 중 피격 시 권위 셀 고정, 자폭 폭탄 arm, Telegraph 셀, 파괴문 변경, 자기 사망과 단일 사망 사건을 검증한다.
- Content Validator는 일반 2 cells/s·경고 최대 5 cells/s·누적 1.5초·거리 3/1·0.75초 범위 2 십자와 기존 asset/prefab 참조를 검증한다. Gates에서는 두 anchor 각각의 실제 Core 폭발이 가까운 문 하나만 파괴하는지도 실행해 확인한다.
- WebGL smoke는 graph 경계 입장 직후 오른쪽 아래 `(3,-3)`으로 Z축 우선 이동해 자폭병을 오른쪽 우회로 `(3,-1)`까지 끌어낸다. 그 뒤 플레이어가 왼쪽 아래 `(-1,-2)`로 X축 우선 이동하고, 자폭병 `(2,-2)` 시점에 `(-1,-3)` 한 칸 sidestep·복귀로 뒤따르는 추격자 접촉만 피하면서 아래 anchor `(0,-2)` 유도를 유지한다. 경고·정지·점화, 안전 셀 `(-1,-3)` 이탈, 아래 문 파괴와 자기 사망을 확인한 뒤 같은 셀에서 인접한 추격자에게 광역 폭탄을 설치하고 `(-3,-4)`로 이탈해 처치한다.
- 사람 플레이는 홀로그램 점멸·애니메이션 가독성, 정지 시점, 0.75초 회피 여유와 위/아래 문 유인 의도가 자연스러운지를 판정한다. 자동 테스트 결과를 재미 판정으로 사용하지 않는다.

## 현재 증거와 남은 검증

- 연결된 Unity 6000.5.3f1에서 ScriptableObject를 전용 `Bomb Swap/Prototype/Refresh Self-Destruct Content` 메뉴로 갱신했다. 실제 asset은 일반 2 cells/s·경고 최대 5 cells/s·누적 1.5초와 fuse 0.75초·범위 2 십자를 저장하고 기존 프리팹·폭탄 참조를 유지한다.
- 실제 Core 폭발 해석을 사용하는 Gates validator를 포함해 `PrototypeContentValidator` 0 errors, 전체 EditMode `330/330`, 전체 PlayMode `131/131`이 통과했다. 연결 테스트 증거는 `Artifacts/Verification/ConnectedTests/20260818-215122-265.json`, `Artifacts/Verification/ConnectedTests/20260818-215143-014.json`이다.
- `Artifacts/Verification/20260819-065352-self-destruct-threat-web/`의 연결 Unity 6000.5.3f1 11씬 Development WebGL은 138,375,867 bytes·62.194초·오류 0으로 성공했다. 경고 351건은 기존 패키지·셰이더 빌드 범주이고 BuildReport 오류 요약은 비어 있다.
- Edge keyboard smoke `41/41`은 2단계 유인, 가속하는 `self-destruct-warning-chase`, `(0,-2)` 정지·점화, 범위 2 `prototype-self-destruct-blast`, 아래 문 한쪽 파괴, 자기 사망과 전체 던전·비밀방·보스·실패/재시작을 Console/page error 0으로 통과했다.
- 같은 빌드의 가상 Gamepad smoke `14/14`, 템플릿·정적 서버·분석기 테스트와 1,260개 playtest 사건 분석이 통과했다.
- 최종 `Tools/Verify.ps1 -StaticOnly`는 `Artifacts/Verification/20260819-065922-static/`에 통과 결과를 기록했다. 빌드 뒤 Unity Console Error는 0이며 현재 Warning 6건은 MMFeedbacks 선택 연동 asset 3건과 WebGL IL2CPP 큰 메서드 분리 안내 3건이다.
- `browser-smoke-failed-cleanup-route.json`, `browser-smoke-failed-lure-contact.json`, `browser-smoke-failed-cleanup-footprint.json`, `browser-smoke-failed-sidestep-hold.json`, `browser-smoke-failed-cleanup-crossing.json`은 모두 새 자폭병의 유인·가속·폭발·문 파괴 뒤 낮은 체력의 플레이어가 남은 추격자와 교차한 자동화 동선 실패다. 게임 규칙을 테스트에 맞춰 바꾸지 않고 자폭 후 현재 위치에서 설치·이탈하는 최종 동선의 근거로 보존한다.
- 남은 완료 조건은 새 가속과 홀로그램 경고·점화 전환이 실제 플레이에서 카운트다운으로 읽히는지, 장애물 없이 늦게 반응하면 맞되 정확히 이탈하면 피할 수 있는지, 문 선택 여유가 적절한지에 대한 사람 플레이 판정이다.

## 위험과 롤백

- 일반 2·경고 최대 5 cells/s, 연속 경고 1.5초, 거리 3/1, 홀로그램 점멸 0.14초/0.065초, 0.75초 fuse와 범위 2는 모두 `Proposed`다. 사람 플레이에서 지나치게 빠르거나 위험 면적이 크면 최대 속도와 폭발 범위를 다시 분리해 한 변수씩 조정한다.
- 경고 거리는 Manhattan이므로 벽 너머 가까운 경우에도 홀로그램 경고가 시작될 수 있다. 현재는 위험 근접 신호로 허용하며, 실제 오독이 관찰될 때만 BFS 경로 거리 기반 경고를 별도 결정한다.
- 롤백 단위는 자폭 Core 정의·상태·simulation, Unity asset/session/presenter, probe·WebGL smoke, builder·validator·테스트와 관련 문서를 한 묶음으로 한다. Gates room 셀 배치와 anchor 스키마는 그대로 유지할 수 있다.
