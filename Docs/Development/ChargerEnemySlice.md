# 작업: 돌진형 적 수직 슬라이스

- 상태: `Accepted` 구현 완료, 수치·가독성은 플레이테스트 전 `Proposed`
- 기준일: 2026-08-14

## 목표

- GDD 테스트 2의 `추격자와 돌진형 적` 구성을 실제 3D WebGL 전투방에서 함께 관찰할 수 있게 한다.
- 돌진형 적이 속도만 빠른 추격자가 아니라, 예고된 직선 경로와 정지 위치를 보고 폭탄 설치 위치를 미리 고르게 만드는 공간 압력이 되게 한다.
- 자동 테스트는 상태 순서와 충돌 규칙을 검증하고, 재미·공정성·가독성 판정은 사람 플레이테스트로 남긴다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 16장과 18.2장
- `Docs/GameDesign/ProtoType_v0.2.md` 가설 B와 테스트 2
- `Docs/Systems/EnemyBehavior.md`
- `Docs/Systems/GridAndMovement.md`
- `Docs/ADR/0001-Logical-XZ-Grid.md`

## 범위

- 변경 허용 경로: `Assets/Game`, 관련 `Docs`, WebGL gameplay probe와 smoke 기대값.
- 직렬화 변경: 돌진형 정의 자산, 선택적 방 spawn, 대응 씬 spawn·presenter 참조. Unity Editor builder와 validator로만 저장한다.
- 변경 금지 경로: `Assets/Feel`, `Assets/Plugins`, 패키지·ProjectSettings, Input Actions.
- 명시적 비목표: 범용 적 팩토리·무제한 적 목록, 경로 탐색, 넉백, 벽 파괴 돌진, 여러 돌진형, 갑옷 적, 완성 애니메이션·VFX·오디오.

## 플레이어 계약

- 돌진형은 플레이어와 같은 행 또는 열이며 둘 사이 모든 셀이 비어 있을 때만 `Track → Telegraph`로 전환한다.
- 예고가 시작된 순간 cardinal 방향을 잠근다. 예고 중 플레이어가 이동해도 취소하거나 방향을 바꾸지 않는다.
- 예고 시간이 끝나면 `Charge`가 되고, 고정된 방향으로 설정 cadence마다 한 셀씩 이동한다.
- 돌진 중 플레이어가 다음 셀에 있으면 그 셀로 겹쳐 들어가지 않고 단일 충돌 피해 후보를 만든 뒤 `Recover`가 된다.
- 벽, 파괴 가능 벽, 폭탄 또는 다른 actor가 다음 셀을 막아도 `Recover`가 되며 플레이어 피해는 만들지 않는다.
- 회복 시간이 끝나야 다시 정렬을 탐지한다. 상태 순서는 `Track → Telegraph → Charge → Recover → Track`이다.
- Transform·Collider·애니메이션은 상태를 결정하지 않는다. 정수 XZ 격자, actor ID와 주입 시계가 권위 원본이다.

## 프로토타입 수치와 배치

- 정의 ID `prototype-charger`, 내구도 1, 충돌 피해 1.
- 예고 0.75초, 돌진 8 cells/s, 회복 0.75초. 모두 플레이테스트 전 `Proposed`다.
- 플레이어 `ActorId(1)`, 추격자 `ActorId(2)`, 돌진형 `ActorId(3)`의 고정 프로토타입 구성을 사용한다. 범용 ID 발급은 비목표다.
- 첫 두 방은 기존 기준선을 보존한다. 마지막 `prototype-combat-pillars` 방에 추격자와 함께 돌진형을 `(-3, 2)`에 배치해 플레이어 spawn `(-3, -2)`과 장애물 없는 세로 예고선을 만든다.
- 동일 frame 적 이동 순서는 고정 actor ID 순서인 추격자 후 돌진형이다. 먼저 확정된 점유를 뒤 적이 통과하지 못한다.

## 완료 조건

- Core: 정의 값 검증과 `Track·Telegraph·Charge·Recover` 결정론적 상태 머신을 구현한다.
- EditMode: 정렬·가시선, 방향 잠금, cadence, 플레이어 충돌, 벽·폭탄·다른 actor 충돌, 회복 경계, 시계 역행과 잘못된 ID/spawn을 검증한다.
- Runtime: 추격자와 돌진형이 같은 `GridState`를 점유하고 폭발 피해·사망·방 클리어 집계에 모두 참여한다.
- Presentation: 예고·돌진·회복을 재질 인스턴스 생성 없이 색과 이동으로 구분하고 확정 상태만 표현한다.
- Content: 정의·prefab·마지막 방 spawn·세 씬 참조를 Editor builder로 저장하고 validator 오류 0을 확인한다.
- WebGL: 마지막 방에서 예고, 실제 돌진 이동 또는 충돌, 기존 입력·폭탄·3방 전환 회귀와 Console/page error 0을 확인한다.
- 문서: 적 행동, 방 저작, 런타임 흐름, 테스트, 현재 상태를 실제 구현에 맞춘다.

## 검증 명령과 증거

- 대상 EditMode: `ChargerEnemySimulationTests`와 적·격자 회귀.
- 대상 PlayMode: `PrototypePlayerControllerTests`의 두 적 점유·피해·사망·presenter 계약.
- 전체: 연결 Unity Editor EditMode/PlayMode와 `PrototypeContentValidator`.
- 정적: `./Tools/Verify.ps1 -StaticOnly`.
- WebGL: 연결 Editor Development build와 `Tools/WebGLSmoke.mjs`.
- 산출물: `Artifacts/Verification/20260814-181400-charger-web-connected/`.

## 완료 증거

- 공식 Unity MCP EditMode `BombSwap.Core.Tests` 193개 통과, 실패·건너뜀·불확정 0.
- 공식 Unity MCP PlayMode `BombSwap.Unity.Tests` 71개 통과, 실패·건너뜀·불확정 0.
- `PrototypeContentValidator` 오류 0. 세 방의 선택적 spawn, 정의·prefab, session·presenter 참조가 권위 데이터와 일치한다.
- `Tools/Verify.ps1 -StaticOnly`, `node --check Tools/WebGLSmoke.mjs`, `node Tools/WebGLStaticServerTests.mjs` 통과. 정적 산출물은 `Artifacts/Verification/20260814-183106-static/`에 기록했다.
- Development WebGL 빌드 140,947,504 bytes, 오류 0. 기존 패키지·셰이더 범주의 경고 359개는 별도 정리 대상이다.
- Edge headless browser smoke 통과. `charger-telegraph → charger-charge → charger-moved`, 입력·폭탄·파괴 블록·3방 전환 회귀, 돌진선 이탈 뒤 자기 폭발 피해와 Console/page error 0을 확인했다.

## 위험과 롤백

- 추격자와 돌진형이 같은 좁은 길에서 서로 막는 것은 논리 점유 결과다. 재미가 나쁘면 actor 충돌을 우회하지 않고 방 배치나 cadence를 조정한다.
- 마지막 방의 파괴 블록 가설과 적 압력이 동시에 바뀔 수 있으므로 관찰표에서 블록 선택과 돌진 회피를 별도 질문으로 기록한다.
- 롤백 단위는 Core 상태 머신, 고정 두 적 세션 연결, 선택적 방 spawn, 정의·prefab·presenter, probe·문서를 한 묶음으로 한다.
