# 보스 전투

- 상태: 핵심 방향·Core·Unity 수직 슬라이스 `Accepted`, 튜닝·사람 플레이테스트 `Proposed`
- 설계 원본: `GDD_v0.2.md` 24~25, 35, 36장, `ProtoType_v0.2.md` 가설 F
- 코드 소유: 패턴 규칙은 `BombSwap.Core`, 연출은 `BombSwap.Unity`

## 목적

보스전이 별도의 탄막 게임이 아니라 격자 위험을 읽고 폭탄으로 반격하는 핵심 게임의 종합 시험이 되게 한다.

## 프로토타입 계약

- 최소 2개 패턴.
- 공격 전에 격자 기반 위험 셀을 명확히 예고.
- 회피 이후 플레이어가 폭탄을 설치하거나 유도할 반격 기회.
- 체력 구간에 따른 phase 변화.
- 일반 폭탄/연쇄/자기 위험 규칙을 그대로 사용.

## 패턴 상태

```text
Select -> Telegraph -> Execute -> Recovery -> Select
                     |              |
                     +---- Hit -----+
```

phase 변경은 현재 패턴의 안전한 전환 지점에서 일어난다. 시각 연출만 바뀌고 논리 위험 셀이 늦게 갱신되는 상태를 허용하지 않는다.

## 구현된 Core 기준선

- `BossBattleDefinition`은 안정 `EnemyDefinitionId`, 최대 체력, 2페이즈 진입 체력, 패턴 피해와 페이즈별 Telegraph·Execute·Recovery 시간을 소유한다.
- `BossBattleSimulation`은 주입된 `IGameClock`, 권위 `GridState`, 보스 `ActorId`와 보행 가능한 arena cell snapshot을 사용한다. Unity `Time`, Transform, Physics를 읽지 않는다.
- 전투는 입장 직후 `Telegraph`로 시작해 피할 시간을 먼저 제공한다. 예고에서 확정한 read-only 위험 셀 snapshot을 `Execute` 전이에도 같은 객체로 전달한다.
- 보스는 검증된 수제 방 `LureLoop`의 저작 순서를 따라 한 패턴당 한 cardinal 셀을 순환한다. 현재 route 위치의 다음 셀을 `NextBossPosition`으로 소유하고 Telegraph 위험 snapshot에 포함하며, Telegraph→Execute exact 경계에서 권위 `GridState` actor를 이동한다.
- 목적지의 폭탄은 보스 이동만 막지 않는다. `GridState.TryMoveActorAllowingBombOverlap`이 다른 actor와 비바닥은 계속 차단하면서 actor+bomb 동시 점유를 원자적으로 만들고, 이후 폭탄 제거와 보스 사망은 서로의 점유를 보존한다. 다른 actor가 목적지를 막으면 route index를 진행하지 않고 다음 패턴에 같은 목적지를 재시도한다.
- 1페이즈는 짝/홀 parity를 바꾸며 열 위험과 행 위험을 교대한다. 현재 Recovery 종료 시 체력이 임계값 이하이면 안전 전환 지점에서 2페이즈가 되고, 이후 parity가 바뀌는 체크무늬 위험을 사용한다.
- 각 패턴은 보행 가능한 arena cell만 대상으로 하며 위험 셀과 안전 셀이 모두 존재하지 않는 arena를 초기화 단계에서 거부한다.
- 보스는 `Recovery`에서만 폭탄 피해를 받는다. 같은 `BombId` 중복, 비취약 구간과 사망 뒤 피해를 구분하고, 치명 피해 시 보스 actor 점유와 위험 셀을 한 번만 제거한다.
- 큰 시계 진행에서도 각 전이의 논리 예약 시각을 보존한다. Unity 연결은 frame마다 최대 한 전이를 소비해 놓친 Recovery를 건너뛰지 않는다.

Core 테스트 fixture는 체력 4, 2 이하에서 2페이즈, 패턴 피해 1을 사용한다. 1페이즈 시간은 1.0초 예고·0.25초 실행·2.0초 회복, 2페이즈는 0.75초·0.25초·1.5초다. 이는 상태 경계 테스트를 빠르게 실행하기 위한 값이며 실제 콘텐츠 튜닝의 권위 원본이 아니다.

## 구현된 Unity 수직 슬라이스

- `PrototypeBossDefinitionAsset`이 실제 콘텐츠의 체력·phase·피해·시간·spawn과 보스/위험 셀 prefab을 소유한다. 현재 저작값은 체력 4, 체력 2 이하에서 2페이즈, 패턴 피해 1, 1페이즈 1.0초 예고·0.25초 실행·2.75초 회복, 2페이즈 0.75초·0.25초·2.75초 회복이다.
- 2.75초 Recovery는 현재 두 보상 후보를 포함한 가장 긴 2.25초 신관 뒤에도 최소 0.5초의 반격 여유를 보장한다. 패턴을 피한 뒤 폭탄을 설치하는 계약을 만족시키기 위한 실제 콘텐츠 기준선이며, 사람 플레이테스트 전까지 튜닝 값은 `Proposed`다.
- `PrototypeGameSession`은 일반 전투와 보스 전투 활성화를 분리하고 보스 `ActorId(5)`를 Core 시뮬레이션에 연결한다. Execute 위험 셀의 플레이어 피해는 기존 `PlayerHealthSimulation` 무적 시간과 공유한다.
- 폭발은 보스 위치가 영향 셀에 포함되고 상태가 Recovery일 때만 Core 피해가 된다. 치명 피해는 보스 표현 제거, 단일 `RoomCleared`, 출구 개방으로 이어진다.
- `PrototypeBossPresenter`는 collider 없는 placeholder와 pooled 위험 셀을 property block으로 표현한다. Telegraph는 노란색, Execute는 빨간색, Recovery는 위험 셀을 숨기고, 2페이즈와 사망을 별도 색/상태로 표시한다.
- 같은 presenter는 Telegraph 동안 다음 논리 목적지에 작은 청록색 boss ghost를 표시하고 `BossMoved`의 확정 `EnemyMovementStep`만 Execute 시간 동안 보간한다. pause 중에는 Core 시계와 함께 보간도 정지하며 Transform으로 목적지나 이동 성공을 다시 판정하지 않는다.
- `PrototypeHealthHud`는 보스방에서만 세션의 현재/최대 보스 체력과 phase를 상단 panel에 표시하고 피해·phase 전환·사망 사건에 맞춰 갱신한다. 취약/Recovery 여부는 표시하지 않아 반격 타이밍을 UI 정답으로 노출하지 않는다.
- `DungeonBoss`만 보스 활성 씬이며 다른 일곱 씬은 같은 presenter 참조를 가지되 보스를 생성하지 않는다. Editor builder와 validator가 이 계약과 asset 수치·참조·collider 부재를 재현·검증한다.

상세 구현·검증 범위와 다음 연결 순서는 [보스 Core 수직 슬라이스](../Development/BossCoreSlice.md)가 소유한다.

## 불변식

- 피해 가능한 기회가 플레이어에게 읽힌다.
- 피할 수 없는 입장 직후 공격을 하지 않는다.
- 예고 셀과 실제 위험 셀이 동일한 규칙 좌표를 사용한다.
- 예고한 이동 목적지와 성공한 논리 이동 셀이 일치하고 한 패턴에 최대 한 칸만 이동한다.
- 일반 actor의 폭탄 차단과 보스의 제한된 목적지 bomb overlap을 섞지 않는다.
- phase 전환 중 중복 패턴 또는 무한 무적이 발생하지 않는다.
- 보스 사망과 방 클리어는 한 번만 발생한다.

## 검증

- 패턴 상태 전이, 정확한 시간 경계와 큰 시계 진행 EditMode 테스트.
- 열·행·체크무늬의 예고/실행 셀 동일성, 위험/안전 셀 공존.
- Recovery 한정 폭탄 피해, 중복 폭발, 안전한 phase 경계와 사망 점유 단일 제거.
- 예고/실행 셀 일치 PlayMode 테스트.
- phase 경계와 사망 동시 발생.
- 실제 WebGL에서 예고 가독성, 프레임 안정성, 입력 반응.
- 플레이테스트에서 “폭탄 게임으로 느껴지는가” 인터뷰와 행동 관찰.

Core·Unity 연결·실제 WebGL 항목은 자동 검증됐다. 최신 WebGL은 초기 `(0,1)`에서 `(1,1) → (1,0) → (1,-1) → (0,-1)` 네 목적지 예고와 이동, 첫 Telegraph 선행 설치 적중, 이후 세 Recovery 반격, 차단 0, 2페이즈 전환, 사망과 방 클리어, 보스 체력·phase HUD와 완료 화면을 확인했고 브라우저 Console/page error는 0이었다. 다음 층, 최종 아트·오디오와 “목적지 예고를 읽고 선행 설치하는 폭탄 게임으로 느껴지는가” 사람 플레이테스트는 아직 남아 있다.
