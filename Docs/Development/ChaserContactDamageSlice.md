# 작업: 추격자 접촉 피해 수직 슬라이스

- 상태: `Implemented`
- 시작일: 2026-08-14
- 검증 가설: `GDD_v0.2.md` 6.1·18.1, `ProtoType_v0.2.md` 4.4

## 목표

- 기본 추격자의 접근이 실제 공간 압력과 회피 판단으로 이어지게 한다.
- 접촉 피해와 자기 폭발 피해가 하나의 플레이어 체력·무적 계약을 공유하게 한다.
- 피해 원인을 자동 테스트와 WebGL probe에서 구분해 이후 플레이테스트의 억울한 피해 분석 기반을 만든다.

## 근거

- [GDD v0.2](../GameDesign/GDD_v0.2.md) 6.1, 18.1, 최소 프로토타입 범위
- [프로토타입 검증 부록](../GameDesign/ProtoType_v0.2.md) 4.4
- [피해와 무적](../Systems/DamageAndInvulnerability.md)
- [적 행동](../Systems/EnemyBehavior.md)
- [격자와 이동](../Systems/GridAndMovement.md)
- [런타임 흐름](../Architecture/RuntimeFlow.md)

## 범위

- 변경 허용: Core 격자 인접 판정·플레이어 피해 원인·접촉 피해 API, 추격자 정의의 접촉 피해 수치, TestSandbox 세션 순서, probe, builder·validator, EditMode·PlayMode·WebGL 검증과 관련 문서.
- 변경 금지: Collider/Physics 접촉을 권위 상태로 사용, 넉백, 밀기, 적과 플레이어의 같은 셀 점유, 접촉별 별도 무적 시간, 부활·재시작, HUD·오디오 완성, 다중 적 전환.

## 채택할 최소 계약

- 접촉은 플레이어와 살아 있는 추격자의 논리 XZ 셀이 Manhattan 거리 1인 cardinal 인접 상태일 때만 성립한다. 대각선, 같은 셀, Transform·Collider 겹침은 접촉이 아니다.
- 접촉 피해 수치 1은 `PrototypeChaserDefinitionAsset`이 소유하고 양수로 검증한다. 플레이테스트 전까지 `Proposed`다.
- 접촉 피해는 기존 `PlayerHealthSimulation`의 체력·주입 시계·0.75초 무적·사망 계약을 그대로 사용한다.
- 인접 상태가 유지되는 동안 매 simulation update에 접촉 후보를 평가한다. 무적 중 후보는 즉시 무시하고 저장하거나 지연하지 않으며, 무적 종료 경계부터 같은 적의 지속 접촉도 다시 피해를 줄 수 있다.
- 피해 결과는 `Explosion` 또는 `EnemyContact` 원인과 해당 `BombId` 또는 적 `ActorId`를 보존한다. 잘못된 원본 ID와 플레이어 자신의 actor ID를 접촉 원인으로 사용하는 요청은 거부한다.
- 프레임 순서는 플레이어·추격자 이동, 만료 폭탄과 폭발 피해·적 사망, 살아 있는 추격자 접촉 피해, 표현 이벤트 발행 순이다.
- 같은 프레임 폭발로 추격자가 사망하면 actor 점유를 제거한 뒤 접촉을 평가하므로 접촉 피해를 주지 않는다.
- 같은 프레임 플레이어가 폭발 피해를 먼저 받으면 공유 무적 때문에 뒤의 접촉 피해는 적용되지 않는다.
- 적용된 접촉 피해만 기존 `PlayerDamaged`·`PlayerDied`와 health presenter에 전달한다. 별도 물리 이벤트나 별도 체력 소유자를 만들지 않는다.

## 완료 조건

- EditMode에서 cardinal/대각선/극단 좌표 인접 판정, 피해 원인 보존, 지속 접촉의 무적 경계 재피해, 폭발과 접촉의 공유 무적, 잘못된 source 거부를 검증한다.
- PlayMode에서 추격자 접근이 접촉 피해 1을 주고, 무적 중 중복 피해를 막으며, 이탈 뒤 무적 종료 후 재접촉이 다시 피해를 주는 흐름을 검증한다.
- PlayMode에서 같은 프레임 폭발 사망 추격자가 접촉 피해를 남기지 않는 순서를 검증한다.
- health presenter는 기존 property block 피격 표시를 접촉 피해에도 재사용하고 shared material을 변경하지 않는다.
- 콘텐츠 validator가 접촉 피해가 양수인 추격자 정의를 다시 읽어 검증한다.
- WebGL smoke가 기존 사건과 함께 `player-contact-damaged`, `player-explosion-damaged`를 관측하고 browser Console/page 오류 0을 확인한다.
- 관련 Systems, RuntimeFlow, Testing, WebGL, CurrentState 문서가 실제 계약과 일치한다.

## 위험과 롤백

- 지속 인접 시 0.75초마다 반복 피해가 발생하므로 체감상 과도할 수 있다. 자동 계약은 결정론만 보장하며 최종 수치와 반복 정책은 플레이테스트에서 유지·완화·제거한다.
- 현재 단일 추격자이므로 source actor 하나만 검증한다. 다중 적에서는 같은 프레임 여러 접촉 후보의 결정론적 순서와 합산/단일 피해 정책을 별도로 정해야 한다.
- 문제가 생기면 접촉 후보 평가와 source-kind 확장을 제거하면 기존 폭발 피해·추격 이동·폭발 처치 수직 슬라이스로 돌아갈 수 있다.

## 구현 및 검증 결과

- `GridPosition.IsCardinallyAdjacentTo`, `PlayerDamageSourceKind`, `ApplyContactDamage`와 추격자 저작 `contactDamage`를 추가했다.
- 세션은 폭발 피해·적 사망을 먼저 확정한 뒤 살아 있는 추격자의 접촉 피해를 평가하고, 기존 health presenter와 `PlayerDamaged`/`PlayerDied` 경로를 재사용한다.
- EditMode `BombSwap.Core.Tests` 139/139, PlayMode `BombSwap.Unity.Tests` 46/46, 콘텐츠 validator 오류 0, Unity Console 오류 0을 확인했다.
- Development WebGL 증분 빌드는 140,471,653 bytes, 47.113초, 오류 0으로 성공했다. 기존 TextMeshPro 경고 3개가 남았다.
- 실제 Edge headless smoke에서 접촉 피해→논리 이탈→자기 폭발 피해→두 번째 폭탄 유도 처치→방 클리어와 browser Console/page 오류 0을 확인했다.
- 검증 증거는 `Artifacts/Verification/20260814-095831-static/`과 `Artifacts/Verification/20260814-095200-web-connected/`에 있으며 Git에서 제외된다.
