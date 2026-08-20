# 작업: 갑옷 적 횟수제 수직 슬라이스

> 이 문서는 최초 `Armored → Broken → Dead` 수직 슬라이스의 역사적 계약이다. 현재 장갑병 행동과 Armor 방 계약은 [장갑병 수비와 panic run](ArmoredPanicRunSlice.md)이 대체한다.

- 상태: `Implemented`; 자동 검증 완료, 사람 플레이테스트 판정 대기
- 기준일: 2026-08-14
- 기준선 commit: `c831a02`

## 목표

- GDD 테스트 3의 “첫 폭발 뒤 행동이 바뀌는 2회 피격 적”을 독립된 3D WebGL 전투방에서 관찰할 수 있게 한다.
- 단순히 체력 2인 느린 적이 아니라, 첫 폭발이 갑옷을 파괴하고 이동 속도를 높여 두 번째 폭탄의 설치 위치와 유도 계획을 바꾸게 한다.
- 자동 검증은 두 개의 서로 다른 폭발, 상태·속도 변화와 방 클리어를 증명하고, 반복 노동인지 전략 변화인지는 사람 플레이테스트로 남긴다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 17.2장과 18.3장
- `Docs/GameDesign/ProtoType_v0.2.md` 테스트 3 “횟수제 적”
- `Docs/Systems/EnemyBehavior.md`
- `Docs/Systems/DamageAndInvulnerability.md`
- `Docs/ADR/0001-Logical-XZ-Grid.md`

## 범위

- 변경 허용: `Assets/Game`, 관련 `Docs`, WebGL 갑옷 적 전용 probe·smoke와 빌드 진입점.
- 직렬화 변경: 갑옷 적 정의·material·collider 없는 prefab, 선택적 room spawn, 네 번째 TestSandbox 씬과 Build Settings. Unity Editor builder와 validator로만 저장한다.
- 변경 금지: `Assets/Feel`, `Assets/Plugins`, 패키지, Input Actions, 기존 폭탄·플레이어 수치.
- 비목표: 방패 방향 판정, 넉백·기절, 분열, 범용 적 팩토리/가변 목록, 갑옷 수리, 여러 갑옷 적, 최종 애니메이션·VFX·오디오.

## 플레이어 계약

- 갑옷 적 상태는 `Armored → Broken → Dead`다.
- 첫 번째 유효한 폭발은 항상 갑옷 한 단계만 파괴한다. 폭탄 종류나 향후 위력 수치가 한 번에 두 단계를 건너뛰게 하지 않는다.
- 같은 `BombId`는 한 번만 처리한다. 첫 폭발의 중복 셀이 두 번째 피격으로 계산되지 않는다.
- 두 번째 서로 다른 유효한 폭발이 적을 처치하고 논리 actor 점유를 한 번 제거한다.
- `Armored`에서는 1 cell/s, `Broken`에서는 3 cells/s의 한 셀 cadence로 플레이어를 추격한다. 첫 피격 직후 기존 방향 약속과 대기 시간을 버리고 다음 frame부터 빠른 상태로 재판단한다.
- 방향 선택은 기본 추격자와 같은 결정론적 국소 Manhattan 규칙과 `North → East → South → West` 동률 순서를 사용한다. 벽·폭탄·actor는 동일한 논리 장애물이다.
- 플레이어와 cardinal 인접하면 같은 셀에 들어가지 않고 접촉 피해 1 후보를 만든다. 두 상태가 같은 플레이어 무적 계약을 사용한다.
- Transform·Collider·재질은 상태나 피해를 결정하지 않는다. 정수 XZ 격자, `ActorId`, `BombId`와 주입 시계가 권위 원본이다.

## 프로토타입 수치와 고정 순서

- 정의 ID `prototype-armored`, 고정 내구 단계 2, 접촉 피해 1.
- 갑옷 상태 1 cell/s, 파괴 상태 3 cells/s, 방향 유지 2칸. 수치와 속도 증가 선택은 플레이테스트 전 `Proposed`다.
- 플레이어 `ActorId(1)`, 추격자 `ActorId(2)`, 선택적 돌진형 `ActorId(3)`, 선택적 갑옷 적 `ActorId(4)` 고정 구성을 사용한다.
- 같은 frame 적 처리 순서는 추격자 → 돌진형(있을 때) → 갑옷 적(있을 때)이다. 폭발 피해와 사망 사건도 같은 ID 순서를 사용한다.

## 독립 네 번째 방

- 신규 `prototype-combat-armor` 방과 `TestSandboxArmor` 씬을 만든다.
- 플레이어 spawn `(0,-2)`, 기본 추격자 `(4,4)`, 갑옷 적 `(0,1)`, 돌진형 없음.
- 중앙 세로선은 갑옷 적 두 단계 실험을 위해 비워 둔다. 시작 위치 폭탄은 느린 갑옷 적의 첫 폭발 피격을 재현할 수 있어야 한다.
- 고정 기둥은 좌우 `(-2,-1)`, `(2,-1)`, `(-2,1)`, `(2,1)`에 두어 중앙 실험선을 가리지 않으면서 측면 퇴로를 만든다. 파괴 가능 벽은 두지 않아 횟수제 가설과 블록 가설을 섞지 않는다.
- 기존 세 방과 `cf06286` WebGL 플레이테스트 산출물은 변경하지 않는다. 새 기본 시퀀스는 네 번째 방을 포함하되 갑옷 전용 WebGL 검증은 이 씬을 시작 씬으로 재정렬한 별도 빌드에서 수행한다.

## 완료 조건

- Core: 정의 검증, `Armored → Broken → Dead`, 폭발 ID 중복 차단, 상태별 cadence와 재판단을 구현한다.
- EditMode: 첫/두 번째 폭발, 같은 ID 중복, 사망 뒤 무시, 느림→빠름 경계, 벽·폭탄·actor 차단, 접촉 인접, 시계 역행과 생성 오류를 검증한다.
- Runtime: 선택적 갑옷 적이 공유 격자·시계·폭발·접촉·방 클리어 집계에 참여하고 고정 ID 순서를 지킨다.
- Presentation: 첫 피격 뒤 갑옷 외형을 즉시 제거/축소하고 파괴 상태 색을 표시하며, 재질 인스턴스를 매번 만들지 않는다.
- Content: 정의·prefab·room asset·네 번째 씬·Build Settings·전환 참조를 builder로 저장하고 validator 오류 0을 확인한다.
- WebGL: 갑옷 전용 시작 씬에서 첫 실제 폭발의 `armored-broken`, 두 번째 실제 폭발의 `armored-died`, 상태별 이동과 기존 입력·Console 회귀를 확인한다.
- 문서: 적 행동, 피해, 방 저작, 런타임 흐름, 테스트, WebGL과 현재 상태를 구현에 맞춘다.

## 검증과 증거

- `ArmoredEnemySimulationTests`와 방 정의 EditMode 대상 회귀.
- `PrototypePlayerControllerTests`의 첫 피격 표현·속도 변화·두 번째 사망·방 클리어 PlayMode 회귀.
- 공식 Unity MCP 전체 EditMode/PlayMode, `PrototypeContentValidator`, Console 오류 확인.
- `./Tools/Verify.ps1 -StaticOnly`.
- Development WebGL 갑옷 시작 씬 빌드와 `Tools/ArmoredWebGLSmoke.mjs`.
- 연결된 Editor 전체 회귀: EditMode 206/206, PlayMode 72/72, 실패·건너뜀·불확정 0.
- `PrototypeContentValidator`: 갑옷 정의·collider 없는 prefab·선택적 spawn·네 번째 씬·Build Settings 4방 순서까지 오류 0.
- 갑옷 시작 Development WebGL: 141,100,454 bytes, 오류 0. `armored-moved → armored-broken → armored-died → enemy-died → room-cleared` 실제 Core 사건과 browser Console/page error 0을 확인했다.
- 기본 4방 Development WebGL: 141,100,958 bytes, 오류 0. 기존 입력·빠른 방향 전환·두 폭탄·파괴 블록·돌진형·피해·전환 회귀와 browser Console/page error 0을 확인했다.
- 갑옷 전용 증거: `Artifacts/Verification/20260814-191400-armored-web-connected/`.
- 기본 4방 회귀 증거: `Artifacts/Verification/20260814-192200-default-web-connected/`.
- 연결 테스트 증거: `Artifacts/Verification/ConnectedTests/20260814-101213-997.json`, `Artifacts/Verification/ConnectedTests/20260814-101121-209.json`.

자동 검증은 두 폭발이 서로 다른 `BombId`이고 첫 적중 뒤 상태·이동 주기가 바뀌며 두 번째 적중 뒤 방이 클리어된다는 사실만 보장한다. 1→3 cells/s 변화가 충분히 읽히고 반복 노동 대신 새 설치 계획을 만드는지는 후속 사람 플레이테스트에서 판정한다.

## 위험과 롤백

- 첫 피격 직후 즉시 빨라지는 적이 플레이어와 이미 인접하면 불공정할 수 있다. 논리 무적이나 이동 규칙을 예외 처리하지 않고 spawn·느린/빠른 cadence를 먼저 조정한다.
- 기본 추격자와 같은 국소 경로 선택을 복제하므로 후속 범용 AI 추상화 전까지 두 구현의 계약 회귀를 함께 유지한다.
- 첫 폭발은 정확히 한 갑옷 단계만 제거하므로 향후 폭탄별 위력이 도입되면 별도 관통/단계 피해 결정을 해야 한다.
- 롤백 단위는 Core 상태·이동, 세션 선택적 연결, 네 번째 방·씬, 정의·prefab·presenter, 전용 WebGL 하네스와 문서를 한 묶음으로 한다.
