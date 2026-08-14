# 작업: 파괴 가능 블록 수직 슬라이스

- 상태: `Completed`
- 기준일: 2026-08-14

## 목표

- 이미 검증된 Core 파괴 벽 규칙을 방 ScriptableObject, 런타임 격자, 3D 표현과 WebGL 관찰 흐름에 연결한다.
- 파괴 가능 블록은 단순 진행세가 아니라 폭탄 모양에 따라 다른 설치 위치를 만드는 선택이어야 한다.
- 첫 방은 기존 전투 기준선을 보존하고, 두 번째·세 번째 방에서만 공간 변화와 두 폭탄의 차이를 관찰한다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 7.2장과 22.6장
- `Docs/GameDesign/ProtoType_v0.2.md` 테스트 2
- `Docs/Systems/BombAndExplosion.md`
- `Docs/Systems/RoomAuthoring.md`
- `Docs/ADR/0001-Logical-XZ-Grid.md`

## 범위

- 변경 허용 경로: `Assets/Game`, 관련 `Docs`, WebGL gameplay probe와 smoke 기대값
- 변경 금지 경로: `Assets/Feel`, `Assets/Plugins`, 패키지·ProjectSettings, 입력 binding
- 명시적 비목표: 드롭·보상, 비밀방, 내구도 여러 단계, 복구·재생성, 완성 파괴 VFX/audio, 여러 적, 절차 배치

## 플레이어 계약

- 파괴 가능 블록은 처음에는 이동·폭탄 설치를 막고 폭발 셀에는 포함된다.
- 기본 십자 폭발은 블록 셀을 파괴한 뒤 그 방향에서 멈춘다. 같은 폭발이 벽 너머 셀에 닿지 않는다.
- 3×3 광역 폭발은 영역 안 파괴 가능 블록을 포함·파괴하지만, ray가 아니므로 다른 영역 셀의 판정을 가리지 않는다.
- 같은 논리 시각 폭발 묶음이 모두 계산된 뒤 셀은 `Floor`가 된다. 이후 이동과 폭탄 설치가 가능하다.
- 3D 블록은 Core의 확정된 `BombExplosion.DestroyedWalls` 결과를 받은 뒤에만 사라진다. Transform·Collider 파괴가 논리 상태를 먼저 바꾸지 않는다.
- 고정 벽은 회색 단일 덩어리, 파괴 가능 블록은 황갈색 분할 블록으로 명확히 구분한다.

## 방 배치 계약

- `prototype-combat-loop`: 파괴 가능 블록 없음. 기존 이동·전투 기준선 유지.
- `prototype-combat-lanes`: `(-1, -1)`, `(1, -1)` 두 개. spawn `(0, -2)`의 광역 폭탄은 두 블록을 대각선으로 동시에 파괴하지만 같은 위치의 십자 폭탄은 닿지 않는다.
- `prototype-combat-pillars`: `(0, 0)` 한 개. 파괴 전에는 중앙 엄폐, 파괴 후에는 중앙 공간 확장으로 사용한다.
- 파괴 가능 블록을 제거하지 않아도 각 방의 전체 초기 플레이 가능 영역은 연결되고 spawn·안전 셀·퇴로·유도 경로·출구와 겹치지 않는다.

## 완료 조건

- Core/Authoring: 파괴 가능 셀의 범위·중복·고정 벽 겹침·중요 셀 겹침·초기 연결성을 실행 가능한 테스트로 고정한다.
- Runtime: room asset의 파괴 가능 셀이 `GridTerrain.DestructibleWall`로 생성되고 폭발 뒤 `Floor`가 된다.
- Presentation: 전용 presenter가 저작 셀과 시각 블록을 일대일로 검증하고 확정 파괴 결과에서만 해당 시각을 비활성화한다.
- Content: 세 room asset·세 씬·재질·presenter 참조를 Unity Editor builder로 저장하고 validator 오류 0을 확인한다.
- WebGL: 두 번째 방 광역 폭탄에서 실제 `destructible-wall-destroyed` 사건, 기존 3방·입력·피해 회귀, Console/page error 0을 확인한다.
- 문서: 방 저작·격자·폭발·런타임·검증·현재 상태를 실제 구현에 맞춘다.

## 검증

- 대상 EditMode: `CombatRoomDefinitionTests`, 기존 `BombExplosionTests`
- 대상 PlayMode: `PrototypePlayerControllerTests`
- 전체: 연결 Unity Editor EditMode/PlayMode, `PrototypeContentValidator`
- 정적: `./Tools/Verify.ps1 -StaticOnly`
- WebGL: 연결 Editor Development build와 `Tools/WebGLSmoke.mjs`
- 산출물: `Artifacts/Verification/20260814-171929-destructible-wall-web-connected/`

### 완료 증거

- EditMode 173개, PlayMode 68개 전체 통과. 실패·건너뜀·불확정 0.
- `PrototypeContentValidator` 오류 0.
- Development WebGL 빌드 성공: 140,883,086 bytes, 317.216초, 오류 0. 기존 범주의 경고 359개.
- Edge headless smoke 12개 검사 통과. 두 번째 방에서 면적 폭탄이 `destructible-wall-destroyed`를 실제 발생시켰고 3방 전환, 빠른 직교 방향 반복, 자기 폭발 피해, resize를 함께 회귀 검증했다.
- Browser Console 오류 0, page error 0. `webgl-destructible-walls.png`에서 황갈색 분할 블록 두 개와 기존 고정 벽의 시각 구분을 확인했다.
- 최종 정적 검증: `Artifacts/Verification/20260814-173048-static/`.

## 위험과 롤백

- 새 블록이 기존 자동 유도 동선을 막으면 테스트가 재미가 아니라 배치 회귀를 드러낸 것이다. 블록을 논리 권위에서 우회시키지 않고 배치를 조정한다.
- 파괴 직후 단순 비활성화는 완성 VFX가 아니다. 현재 목적은 상태 변화와 공간 역할을 읽는 것까지다.
- 롤백 단위는 방 스키마, 런타임 지형, presenter, 세 room/scene 콘텐츠, probe·문서를 한 묶음으로 한다.
