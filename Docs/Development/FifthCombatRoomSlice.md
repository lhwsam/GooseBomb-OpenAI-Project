# 다섯 번째 수제 전투방 작업 계약

- 상태: `Implemented / WebGL Verified`
- 관련: [방 저작과 검증](../Systems/RoomAuthoring.md), [GDD v0.2](../GameDesign/GDD_v0.2.md) 22·23·36장, [ADR-0007](../ADR/0007-Potential-Room-Exits.md)

## 목표

GDD 필수 범위인 수제 일반 전투방 5~7개의 최소치에 도달한다. 다섯 번째 방은 기존 적·폭탄 수치를 바꾸지 않고, 파괴 가능한 중앙 문을 열지 여부가 플레이어와 추격자의 이동 경로를 동시에 바꾸는 공간 선택을 제공한다.

## 콘텐츠 계약

- 안정 ID: `prototype-combat-gates`
- 자산: `Assets/Game/Content/Rooms/PrototypeCombatGates.asset`
- 씬: `Assets/Game/Scenes/TestSandbox/TestSandboxGates.unity`
- 격자: 11×9, 셀 크기 1, 북·동·남·서 중앙 잠재 출구 각 1개.
- spawn: 플레이어 `(0,-3)`, 추격자 `(0,3)`, 돌진형·갑옷 적 없음.
- 고정 벽: `z=-1`과 `z=1`의 `x=-2,-1,1,2` 셀. 각 가로 장벽의 중앙은 파괴 가능한 문으로 남긴다.
- 파괴 벽: `(0,-1)`, `(0,1)`. 파괴 전에도 `x=±3` 좌우 우회로가 연결되어 진행 필수가 아니다.
- 안전 셀: `(0,-3)`, `(-1,-3)`, `(1,-3)`.
- 퇴로 anchor: `(-3,-2)`, `(3,-2)`. 플레이어 spawn에서 서로 다른 첫 cardinal 이동으로 도달할 수 있어야 한다.
- 유도 loop: `x=-3..3`, `z=-2..2`의 닫힌 사각 perimeter. 고정·파괴 벽과 겹치지 않는다.
- 공간 의도: 중앙 문을 부수면 짧은 직선 경로와 적 진입로가 함께 열리고, 유지하면 좌우 우회와 외곽 유도가 남는다.

## 상태 소유와 불변식

- `PrototypeCombatRoomDefinitionAsset`과 변환된 `CombatRoomDefinition`이 논리 셀 권위다.
- 씬 Transform·Collider·재질은 논리 벽과 spawn을 표현할 뿐 판정을 소유하지 않는다.
- mutable 방문·클리어·벽 파괴 상태는 카탈로그나 ScriptableObject에 저장하지 않는다.
- 다섯 방 모두 네 방향 잠재 출구, 연결성, 안전 입장, 두 퇴로와 닫힌 유도 loop를 만족해야 한다.
- 카탈로그 입력 순서와 무관한 기존 `prototype-combat-assignment-v1` 배정 계약을 유지한다.

## 범위와 비목표

- 변경: Editor builder/validator, 방 자산·씬·전투방 카탈로그·Build Settings, 관련 PlayMode/WebGL 회귀와 문서.
- 비목표: 새 적, 적 AI 수정, 폭탄/체력/쿨타임 튜닝, 일반화된 적 spawn 목록, room-local persistence, 최종 아트·오디오.
- 자동 검증은 공정한 구조와 연결만 증명하며 재미, 파괴 선택의 의미와 두 폭탄의 실제 위치 차이는 사람 플레이테스트로 판정한다.

## 완료 조건

- Unity Editor builder가 기존 네 방을 보존하면서 다섯 번째 자산·씬·카탈로그 entry와 Build Settings를 멱등적으로 생성한다.
- validator가 다섯 정의의 ID·선택 적·네 방향 출구, 다섯 scene 매핑, 논리/시각 벽·spawn·참조와 아홉 enabled scene 순서를 검증한다.
- 신규 씬에서 플레이어·추격자·두 파괴 문·외곽 우회로가 준비되고 방 클리어까지 기존 규칙으로 진행된다.
- 전체 EditMode/PlayMode, Development WebGL build와 브라우저 smoke가 통과한다.
- 실제 WebGL 캡처로 두 장벽, 중앙 파괴 문, 좌우 우회로와 HUD 비중첩을 확인한다.

## 위험과 롤백

- 다섯 번째 catalog entry는 같은 seed의 결정론적 배정 결과를 의도적으로 바꿀 수 있으므로 seed-0 경로 문서와 browser smoke 기대값을 새 결과에 맞춰 함께 갱신한다.
- 롤백 단위는 신규 방 자산·씬과 builder/validator/catalog/Build Settings 변경 전체다. 일부만 제거해 dangling scene 또는 catalog 참조를 남기지 않는다.

## 검증 증거

- 집중 EditMode 1/1: `Artifacts/Verification/ConnectedTests/20260814-200852-552.json`.
- 집중 PlayMode 1/1: `Artifacts/Verification/ConnectedTests/20260814-200908-696.json`.
- 전체 EditMode 267/267, PlayMode 110/110: `Artifacts/Verification/ConnectedTests/20260814-200957-559.json`, `Artifacts/Verification/ConnectedTests/20260814-201015-022.json`.
- `PrototypeContentValidator` 오류 0, Unity Console Error 0, 최종 StaticOnly `Artifacts/Verification/20260815-053211-static/` 통과.
- 9씬 Development WebGL: `Artifacts/Verification/20260815-052000-fifth-combat-room-web/`, 141,882,145 bytes, warning 0, error 0.
- Edge headless smoke 27/27, browser Console/page error 0. `webgl-fifth-combat-room-gates-room.png`에서 두 장벽·중앙 파괴 문·좌우 우회와 HUD 비중첩을 확인했다.
- 자동 검증은 구조·연결·실행 가능성만 증명한다. 파괴 선택의 재미와 두 폭탄의 위치 차이는 다음 사람 플레이테스트 판정으로 남는다.
