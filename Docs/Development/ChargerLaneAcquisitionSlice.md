# 작업: 돌진형 차선 획득과 Pillars 재구성

- 상태: 구현·자동/WebGL 검증 완료, 수치·공간 재미 `Proposed`
- 기준일: 2026-08-18

## 목표

- 돌진형이 플레이어와 우연히 일직선이 되기를 제자리에서 기다리지 않고, 논리 격자에서 가장 가까운 유효 정렬 셀을 찾아 천천히 이동해야 한다.
- 플레이어는 정렬 뒤 표시되는 전체 돌진 차선과 고정 방향을 읽고 옆 칸으로 피하거나, 폭탄·기둥·파괴벽 쪽으로 돌진형을 유도할 수 있어야 한다.
- `prototype-combat-pillars`는 이 규칙을 한 방에서 관찰할 수 있도록 짧은 차선과 측면 탈출 공간을 제공해야 한다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 16장, 18.2장, 22.4장
- `Docs/GameDesign/ProtoType_v0.2.md` 테스트 2와 적 관찰 질문
- `Docs/GameDesign/CombatEnemyLevelBossImprovementProposal.md` 순서 2
- `Docs/Systems/EnemyBehavior.md`, `Docs/Systems/GridAndMovement.md`, `Docs/Systems/RoomAuthoring.md`
- 기존 코드 진입점: `ChargerEnemySimulation`, `PrototypeChargerPresenter`, `PrototypeContentBuilder`

## 범위

- 변경 허용: Charger Core 정의·상태 머신·결과, Unity 저작 정의·presenter, 관련 테스트·하네스, Pillars room asset·scene을 만드는 Editor builder·validator, 관련 문서.
- 직렬화 변경: Charger 차선 획득 속도와 차선 예고 prefab 참조, Pillars spawn·벽·안전 셀·퇴로·유도 loop. 저장은 Unity Editor builder를 통해서만 수행한다.
- 변경 금지: `Assets/Feel`, `Assets/Plugins`, 패키지, Input Actions, WebGL/렌더 파이프라인 설정, 다른 네 전투방의 설계.
- 명시적 비목표: 폭탄 충돌 즉시 연쇄·별도 기절, 돌진 벽 파괴, 여러 돌진형, 범용 적 목록/ID 발급, NavMesh, 최종 아트·VFX·오디오, 최종 튜닝 확정.

## 계약과 불변식

- `Track`은 주입 시계의 차선 획득 cadence에서만 BFS를 다시 계산한다. 매 frame 완전 재탐색하지 않는다.
- 현재 셀에서 플레이어까지 장애물 없는 행/열이 있으면 이동하지 않고 즉시 `Telegraph`로 전환한다.
- 정렬되지 않았으면 현재 플레이어 셀을 기준으로, 도달 가능하면서 플레이어까지 장애물 없는 cardinal 차선을 만드는 가장 가까운 셀을 BFS로 찾고 첫 한 칸만 이동한다.
- 경로와 정렬 후보의 동률은 `North → East → South → West`로 고정한다. 벽·파괴벽·폭탄·다른 actor는 획득 경로와 차선을 막고, 경로가 없으면 다음 cadence까지 제자리에서 기다린다.
- 프로토타입 차선 획득은 1 cell/s다. 첫 판단은 즉시 가능하고 이후 한 칸 시각 이동이 끝나는 1초 경계에서만 다시 판단한다.
- `Telegraph` 시작 순간 방향과 현재 장애물까지의 최대 이동 칸 수를 잠근다. 플레이어 이동이나 예고 중 벽 파괴로 이를 늘리지 않는다.
- 예고 셀은 고정된 최대 이동 범위 전체를 논리 셀 중심에 표시한다. `Charge` 진입 시 숨기며 collider나 물리 판정은 갖지 않는다.
- `Charge` 중 새 폭탄·벽·다른 actor가 차선을 막으면 고정 최대 거리보다 먼저 `Recover`로 전환한다. 예고 때 있던 장애물이 사라져도 예고 범위를 넘어가지 않는다.
- 플레이어가 다음 셀에 남아 있으면 기존처럼 겹치지 않고 피해 후보 하나만 만든 뒤 회복한다. 회복은 현재 프로토타입에서 모든 충돌 원인에 1초를 공유한다.
- 폭탄 충돌의 기존 고정 지연 연쇄와 별도 기절은 폭탄 스케줄러 계약을 확장해야 하므로 이 슬라이스에서는 의도적으로 보류한다.
- Core는 UnityEngine, Transform, Collider, NavMesh를 참조하지 않는다. 탐색 컬렉션은 simulation이 재사용한다.

## Pillars 저작 계약

- 플레이어 `(-3,-2)`, 추격자 `(3,2)`, 돌진형 `(0,1)`에서 시작한다. 돌진형은 즉시 정렬되지 않고 중앙 아래쪽 정렬 셀을 획득해야 한다.
- 고정 충돌 셀은 서쪽 시작 차선 종단 `(-4,-2)`, 좌우 짧은 차선 종단 `(-2,1)·(2,1)`, 두 세로 차선 종단 `(-3,-3)·(-3,3)·(3,-3)·(3,3)`이다.
- 파괴벽 `(2,-2)`는 동쪽으로 열 수 있는 차선 종단이다. 파괴 전후 모두 방 전체 진행은 가능해야 한다.
- 플레이어 안전 셀 `(-3,-2)·(-3,-1)·(-2,-2)`와 퇴로 anchor `(-3,-1)·(-2,-2)`는 북쪽/동쪽의 서로 다른 첫 이동을 보장한다.
- 중앙 `x=-1..1, z=-1..1` 직사각 loop는 벽과 겹치지 않는 닫힌 cardinal 유도 경로다.

## 완료 조건

- Core: 차선 획득 cadence·결정론적 후보 선택·경로 없음 대기·정렬 뒤 방향/최대 거리 잠금·동적 조기 충돌을 구현한다.
- EditMode: 정의 값, 네 방향 획득, 장애물 우회, 동률, cadence, 경로 없음, 예고 범위 고정과 기존 충돌·회복 회귀를 검증한다.
- PlayMode: Track 이동이 획득 속도로 보간되고, 전체 차선 예고가 생성·회수되며 기존 피해·사망·방 클리어를 보존한다.
- Content/Visual: Editor builder로 Charger 정의/prefab과 Pillars asset/scene을 저장하고 validator 오류 0, 실제 Scene/Game 캡처에서 차선·탈출 포켓을 확인한다.
- WebGL/브라우저: Pillars 진입 뒤 `Track 이동 → Telegraph → Charge/충돌 → Recover`와 기존 키보드·게임패드·던전 경로, Console/page error 0을 확인한다.
- 문서: `EnemyBehavior`, `RoomAuthoring`, `CurrentState`, 테스트 문서와 이 작업 계약을 실제 구현·검증 결과로 동기화한다.

## 완료 증거

- Core·Unity: 결정론적 BFS 차선 획득, 1 cell/s cadence, 방향·최대 거리 잠금, 동적 장애물 조기 회복, collider 없는 전체 차선 예고와 `Pillars` 재구성을 연결했다.
- 연결된 Unity 6000.5.3f1에서 EditMode `311/311`, PlayMode `128/128`이 실패·건너뜀 0으로 통과했다. 증거는 `Artifacts/Verification/ConnectedTests/20260818-055634-459.json`, `Artifacts/Verification/ConnectedTests/20260818-060838-695.json`이다.
- `PrototypeContentValidator`와 Unity Console 오류는 0이다. Development WebGL은 enabled scene 11개, 138,265,302 bytes, 379.618초, 오류 0으로 성공했다. 전체 셰이더 재컴파일에서 기존 패키지·셰이더 범주의 경고 351건이 기록됐다. 증거는 `Artifacts/Verification/20260818-061000-charger-lane-connected-web/`이다.
- 같은 빌드의 Edge keyboard smoke `40/40`과 가상 Gamepad smoke `14/14`가 통과했고 두 실행의 Console/page error는 0이다. keyboard는 `charger-track-moved → charger-telegraph-<direction>-distance-<cells> → charger-charge-moved → charger-recover`와 전체 던전 회귀를 확인했다. 최종 보고서는 `browser-smoke-final3.json`, `gamepad-smoke.json`이다.
- 첫 PlayMode 실패 두 건은 background Game view의 focus 상실로 합성 입력이 무시된 경우였고, 세 번째 실패는 긴 frame에서 한 셀 관찰을 건너뛴 이동 helper 문제였다. 합성 fixture가 입력 직전에 focus를 명시하고 첫 권위 셀 변화 뒤 해제·재계획하도록 교정했으며 실패 보고서는 진단 증거로 보존했다.
- 첫 browser smoke 세 건은 새 `Pillars`의 돌진형 접촉 압력과 변경된 벽 배치에 기존 고정 경로가 맞지 않은 하네스 실패였다. 돌진형 상태 순서를 먼저 확정하고 폭탄 설치 직후 측면으로 이탈하는 실제 플레이 경로로 교정했다. 자동 검증은 규칙과 회귀만 증명하며 1/0.75/8/1초 수치와 공간 재미의 `Accepted` 판정은 사람 플레이가 맡는다.

## 위험과 롤백

- 플레이어가 계속 이동하면 목표 정렬 셀이 cadence마다 바뀔 수 있다. 이는 예고 전 약한 이동이고, 예고 뒤에는 절대 재조준하지 않는 것으로 공정성 경계를 둔다.
- 1 cell/s 획득, 0.75초 예고, 8 cells/s 돌진, 1초 회복은 자동 검증용 `Proposed` 값이다. 압박감과 회피 여유는 고정 WebGL 사람 플레이로 판정한다.
- 새 예고 prefab은 풀링하며 runtime material/Collider를 만들지 않는다. 예상 방 크기를 넘으면 표현만 확장하고 규칙은 누락하지 않는다.
- 롤백 단위는 Charger 정의·상태 머신·presenter·예고 prefab, Pillars 저작 데이터·scene, 테스트·문서를 한 묶음으로 한다.
