# 작업: 기본 추격자 유도·폭발 처치 수직 슬라이스

- 상태: `Implemented`
- 시작일: 2026-08-14
- 검증 가설: `ProtoType_v0.2.md` 가설 A와 테스트 1

## 목표

- 한 마리의 기본 추격자가 공유 논리 격자에서 플레이어를 결정론적으로 따라간다.
- 적은 짧은 이동 구간 동안 선택한 방향을 유지하고 설치 폭탄을 위험으로 예측해 회피하지 않는다.
- 기본 폭탄 영향 셀에 들어간 추격자는 한 번의 적중으로 사망하고 점유에서 한 번만 제거된다.
- TestSandbox와 WebGL에서 추격, 폭발 처치, 단일 방 클리어를 관찰할 수 있다.

## 근거

- [GDD v0.2](../GameDesign/GDD_v0.2.md) 16장, 17.1, 18.1
- [프로토타입 검증 부록](../GameDesign/ProtoType_v0.2.md) 가설 A, 테스트 1
- [적 행동](../Systems/EnemyBehavior.md)
- [격자와 이동](../Systems/GridAndMovement.md)
- [폭탄과 폭발](../Systems/BombAndExplosion.md)
- [런타임 흐름](../Architecture/RuntimeFlow.md)

## 범위

- 변경 허용: Core 추격 이동·약한 적 체력, 적 정의 ScriptableObject, TestSandbox 적 spawn·placeholder, 세션 적 사건과 방 클리어 집계, builder·validator, 테스트·WebGL probe·문서.
- 변경 금지: NavMesh/AI Navigation을 권위 상태로 사용, AI Inference, Collider·Transform 기반 추격/피격, vendor 에셋 직접 수정.
- 비목표: 적 접촉 피해, 여러 적, 돌진형·갑옷 적, NavMesh/A*·폭발 위험 예측, 문 개방, 보상, 완성 애니메이션·VFX·audio.

## 채택할 최소 계약

- `PrototypeChaserDefinitionAsset`이 안정 ID, 내구도 1, 이동 2 cells/s, 방향 유지 2칸, prefab과 사망 표시 시간을 소유한다. 수치는 플레이테스트 전까지 `Proposed`다.
- 플레이어는 `ActorId(1)`, 첫 추격자는 `ActorId(2)`를 사용한다. 일반 ID 발급과 다중 적 수명 주기는 후속 작업이다.
- 추격자는 주입된 게임 시계로 0.5초 cadence를 사용한다. 첫 판단은 즉시 가능하고 정지·재개로 cadence를 우회하지 않는다.
- 방향을 새로 고를 때 플레이어 셀에서 만든 결정론적 BFS 거리장의 도달 가능한 최단 이웃을 선택한다. 동률은 현재 방향을 먼저 유지하고, 그렇지 않으면 `North → East → South → West` 순서로 고정한다.
- 새 방향은 성공한 이동을 포함해 최대 두 칸 유지한다. 벽·actor·폭탄으로 막히거나 다음 칸이 계획 당시 최단 거리를 늘리면 즉시 다시 선택한다. 경로가 없으면 임의 방향으로 배회하지 않는다.
- 플레이어와 같은 셀에는 들어가지 않으며 cardinal 인접 상태에서는 멈춘다. 접촉 피해는 이번 수직 슬라이스에서 발생하지 않는다.
- 추격자는 폭탄 fuse·영향 범위를 읽지 않는다. 폭탄 셀은 일반 이동 장애물로만 취급한다.
- `BombExplosion.AffectedCells`에 현재 추격자 논리 셀이 포함되면 해당 `BombId`를 최대 한 번 처리한다. 내구도 0에서 actor 점유를 한 번 제거하고 `EnemyDied`, 마지막 적이면 `RoomCleared`를 한 번 발행한다.
- 표현은 Core 이동 결과를 선형 보간하고, 사망 시 짧은 색 표시 뒤 비활성화한다. 공유 material은 변경하지 않는다.

## 완료 조건

- EditMode: 정의 검증, 즉시 첫 이동과 cadence, 결정론적 동률, 방향 유지, BFS 우회·overshoot 중단·경로 없음 대기, 장애물 재선택, 인접 정지, 시계 역행, 단일 폭발 사망·중복 방지를 검증한다.
- PlayMode: 실제 세션에서 추격 이동·placeholder 보간과 `Z` 폭발 처치·점유 제거·단일 방 클리어를 검증한다.
- 콘텐츠: 적 정의·prefab과 spawn, session·presenter 참조를 builder로 생성·업그레이드하고 validator로 검증한다.
- WebGL: `chaser-moved`, `enemy-died`, `room-cleared`와 기존 입력·폭탄·플레이어 피해 사건을 관측하고 Console/page 오류 0을 확인한다.
- 문서: EnemyBehavior, GridAndMovement, BombAndExplosion, RuntimeFlow, CurrentState가 실제 계약과 일치한다.

## 위험과 롤백

- BFS는 현재 11×9 수제 방의 전체 등록 바닥을 재계획 시점에 탐색한다. 더 큰 방·다수의 경로 탐색 적을 도입할 때만 거리장 공유나 탐색 상한을 별도 결정하며, 현재는 단일 추격자 simulation의 재사용 컬렉션으로 범위를 제한한다.
- 단일 추격자 고정 ID와 단일 방 클리어는 프로토타입 어댑터다. Core 값 객체와 사건 계약을 유지한 채 다중 적 컬렉션으로 교체할 수 있어야 한다.
- 접촉 피해가 없으므로 이번 자동 검증은 추격·유도·처치 연결만 증명한다. 공간 압력과 피해 공정성은 다음 접촉 피해 작업 및 플레이테스트에서 판단한다.

## 구현 및 검증 결과

- Core에 `EnemyDefinitionId`, `ChaserEnemyDefinition`, `ChaserEnemySimulation`, `EnemyHealthSimulation`과 이동·피해 결과 값을 추가했다.
- Unity에는 `PrototypeChaserDefinitionAsset`, collider 없는 prefab, `ChaserSpawn`, 세션 적 사건·방 클리어 집계, `PrototypeChaserPresenter`를 추가했다.
- Editor builder가 신규 에셋과 씬 참조를 생성·업그레이드하고 validator가 정의·prefab·spawn·session·presenter 참조를 다시 읽어 오류 0으로 검증했다.
- EditMode `BombSwap.Core.Tests` 126/126, PlayMode `BombSwap.Unity.Tests` 44/44를 통과했다.
- Development WebGL 빌드는 140,461,366 bytes, 278.370초, 오류 0으로 성공했다. 기존 AI Inference·Feel·TextMeshPro 경로에서 경고 359개가 남아 있다.
- 실제 Edge headless smoke에서 `chaser-moved`, `enemy-died`, `room-cleared`를 포함한 필수 사건을 모두 관측했고 browser Console/page 오류는 0이었다.
- WebGL 증거는 `Artifacts/Verification/20260814-091812-web-connected/`, 최신 StaticOnly 증거는 `Artifacts/Verification/20260814-092702-static/`에 있으며 둘 다 Git에서 제외된다.
- 2026-08-17 BFS 개선은 기존 cadence·동률·방향 유지 회귀를 포함한 추격자 EditMode `18/18`, 전체 EditMode `307/307`, PlayMode `127/127`을 통과했다. 막힌 북쪽 포켓과 도달 가능한 남쪽 우회가 Manhattan 동률인 fixture에서 `South → West → West`로 인접까지 진행하고, 목표가 완전히 막히면 배회하지 않는 것을 검증했다.
- `Artifacts/Verification/20260817-053808-chaser-bfs-web/`의 11씬 Development WebGL 빌드는 138,189,741 bytes·265.500초·오류 0·기존 TextMeshPro 안내 경고 3건으로 성공했다. 첫 keyboard smoke는 제거된 Manhattan 왕복을 전제로 두 번째 폭탄을 기다리다 접촉 사망해 실패했고 `browser-smoke-bfs-old-route-failure.json`으로 보존했다. 첫 광역 폭탄 범위에 BFS 추격자를 유도한 뒤 동쪽으로 이탈하는 경로로 고쳐 keyboard `39/39`, 가상 Gamepad `14/14`, 1,127개 사건 분석과 두 실행의 Console/page error 0이 통과했다. 최종 StaticOnly는 `Artifacts/Verification/20260817-054819-static/`이다.
