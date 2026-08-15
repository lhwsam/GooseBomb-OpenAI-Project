# 예고 목적지 기반 보스 이동 수직 슬라이스

- 상태: 자동 검증 `Complete`, 사람 플레이 튜닝 `Proposed`
- 결정 근거: [GDD v0.2](../GameDesign/GDD_v0.2.md) 24~25장, [프로토타입 가설 F](../GameDesign/ProtoType_v0.2.md)
- 소유 계약: [보스 전투](../Systems/BossBattle.md), [격자와 이동](../Systems/GridAndMovement.md), [런타임 흐름](../Architecture/RuntimeFlow.md)

## 문제와 플레이어 계약

정지 보스는 Recovery마다 현재 위치 옆에 폭탄을 놓는 행동만 반복하게 해 GDD의 “이동과 빈틈을 예상해 미리 설치” 가설을 실행하지 못한다. 기존 Telegraph→Execute→Recovery와 두 phase의 위험 셀은 유지하면서 다음 한 칸을 먼저 보여 주는 단일 이동 패턴을 추가한다.

- 보스는 현재 수제 방의 안정 순서 `LureLoop`를 한 방향으로 한 칸씩 순환한다.
- Telegraph 동안 다음 목적지를 별도 ghost와 위험 셀로 보여 준다.
- Telegraph→Execute 경계에서 논리 보스가 목적지로 한 칸 이동한다.
- 목적지에 미리 놓인 폭탄은 이동을 막지 않는다. 보스와 폭탄의 제한된 논리 동시 점유를 허용해 이동 뒤 Recovery 폭발 적중이 가능하다.
- 다른 actor가 목적지를 점유하면 그 회차 이동만 취소한다. 목적지는 실행 위험 셀에 포함되므로 플레이어가 막아서는 행동은 기존 보스 패턴 피해 후보가 되며, 다음 회차에도 같은 목적지를 다시 예고한다.
- Execute→Recovery, Recovery→다음 Telegraph, phase 안전 전환, Recovery 한정 피해와 사망 규칙은 바꾸지 않는다.

## 상태와 책임

```text
CombatRoomDefinition.LureLoop
        │ 검증된 폐쇄 cardinal 경로
        ▼
BossBattleSimulation
        │ NextBossPosition을 Telegraph danger snapshot에 포함
        │ Execute 시작에 actor를 한 칸 원자 이동
        ▼
BossPatternTransition + EnemyMovementStep
        │ 현재 위치·다음 목적지·성공/차단 결과
        ▼
PrototypeBossPresenter
        │ 목적지 ghost + Execute 동안 한 칸 시각 이동
        ▼
WebGL marker / 캡처 / 사람 플레이
```

- Core `GridState`만 actor·bomb 점유와 이동을 확정한다. Transform과 Collider는 규칙이 아니다.
- 일반 actor 이동의 폭탄 차단은 유지한다. `TryMoveActorAllowingBombOverlap`은 보스의 명시적 이동 규칙에서만 사용하며 목적지의 다른 actor는 통과하지 않는다.
- Unity presenter는 Core 위치 사건을 보간해 표현하고 목적지를 추측하거나 경로를 다시 계산하지 않는다.
- 보스 이동 경로는 별도 새 콘텐츠 배열을 중복 저작하지 않고 이미 검증된 room `LureLoop`를 재사용한다.

## 자동 검증 계약

### EditMode

- route는 4칸 이상, 중복 없음, arena floor 포함, 시작 위치 포함, 마지막→첫 칸까지 cardinal 폐쇄 경로여야 한다.
- 초기 Telegraph는 다음 목적지를 공개하고 그 칸을 위험 snapshot에 포함한다.
- exact Telegraph 경계에서 한 칸 이동하고 graph/grid actor 위치·다음 목적지·`EnemyMovementStep`이 일치한다.
- 일반 이동은 폭탄에 막히지만 보스 전용 이동은 목적지 bomb과 동시 점유하고 bomb 제거 뒤 actor를 보존한다.
- 다른 actor가 목적지를 막으면 이동 상태를 손상하지 않고 차단 결과를 내며, 비운 뒤 같은 목적지로 재시도한다.
- 큰 시계 진행, phase 전환, Recovery 피해·중복 폭발·사망 제거의 기존 계약이 이동 위치에서도 유지된다.

### PlayMode·콘텐츠

- 실제 session이 room `LureLoop`를 Core에 전달하고 보스 위치·다음 위치·이동 사건을 노출한다.
- presenter는 Telegraph 목적지 ghost를 표시하고 Execute 구간에 보스 visual을 새 Core 셀로 이동시키며 pause 중 보간을 멈춘다.
- 실제 보스 asset spawn은 shell room의 `LureLoop`에 포함되어야 한다.
- 선행 설치한 폭탄이 이동 뒤 Recovery에서 보스에게 피해를 주고 기존 HUD·phase·클리어를 유지한다.

### WebGL·사람 검증

- 첫 보스 Telegraph에서 목적지 marker와 ghost를 확인하고, 목적지 영향 범위에 폭탄을 먼저 설치한다.
- `boss-move-target → boss-moved → boss-pattern-recovery → bomb-exploded → boss-damaged`의 논리 순서를 확인한다.
- 네 번의 이동 목적지와 2페이즈 전환, 격파·완료·재시작, Console/page error 0을 전체 경로에서 회귀한다.
- 사람 플레이에서 목적지 예고를 이해하는지, 회피 중 선행 설치를 선택하는지, 이동이 너무 잦아 적중을 방해하지 않는지 관찰한다.

## 검증 결과

- StaticOnly가 통과했다: `Artifacts/Verification/20260816-035431-static/`.
- 연결된 Unity 6000.5.3f1에서 Core 집중 EditMode `41/41`, 보스 집중 PlayMode `4/4`, 전체 EditMode `303/303`, 전체 PlayMode `126/126`이 실패·건너뜀 없이 통과했다. 전체 결과는 `Artifacts/Verification/ConnectedTests/20260815-185449-706.json`, `Artifacts/Verification/ConnectedTests/20260815-185507-252.json`에 있다.
- 콘텐츠 validator는 실제 보스 spawn의 `LureLoop` 포함과 기존 씬·prefab 계약을 오류 `0`으로 확인했고 Unity Console 오류도 `0`이었다.
- 연결된 10-scene Development WebGL 빌드는 `137,986,563 bytes`, `88.935초`, 오류 `0`, 기존 패키지·전체 셰이더 재컴파일 범주의 경고 `351`건으로 성공했다. 증거는 `Artifacts/Verification/20260816-035702-boss-movement-connected-web/`이다.
- 같은 빌드의 Edge 기본 smoke 재시도 `34/34`는 첫 Telegraph 선행 설치, 네 목적지와 네 이동, 차단 `0`, 네 피해, 2페이즈·격파·완료·실패·재시작과 browser Console/page error `0`을 확인했다. 가상 Gamepad smoke도 `14/14`, 오류 `0`으로 통과했다. 첫 실행의 `(2,0)` 고정 벽을 통과한 자동 경로 실패는 `browser-smoke.json`, 교정 뒤 성공은 `browser-smoke-retry1.json`에 함께 보존했다.
- 보스 Telegraph 캡처에서 현재 보스와 구분되는 작은 청록 ghost, 위험 셀, 보스 HUD·미니맵·무기 HUD의 비중첩을 확인했다. 이 자동·시각 증거는 사람이 예고를 즉시 이해하고 선행 설치를 재미있게 선택한다는 판정을 대신하지 않는다.

## 비목표와 롤백

- 추적 AI, 무작위 경로, 다중 칸 돌진, 플레이어 밀치기, 소환, 새 장애물, 보스별 행동 트리, 완성 VFX·오디오.
- 롤백 단위는 Core route 이동·제한된 bomb overlap, transition snapshot, Unity ghost/보간, validator·테스트·marker와 이 문서다. 기존 위험 패턴·피해·phase 규칙은 롤백 대상이 아니다.
