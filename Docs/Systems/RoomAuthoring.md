# 방 저작과 검증

- 상태: 수제 방 원칙·프로토타입 전투방 스키마 `Accepted`, 확장 스키마 `Proposed`
- 설계 원본: `GDD_v0.2.md` 19, 21~23장
- 코드 소유: `BombSwap.Core`, `BombSwap.Authoring`, `BombSwap.Editor`

## 목적

전투 공간은 사람이 설계하고, 런 구조만 절차적으로 조합해 공정성과 폭탄별 공간 차이를 함께 확보한다. 정수 XZ 논리 셀 데이터가 권위 원본이고 씬 Transform·Collider는 이를 표현한다.

## 권장 프로토타입 범위

- 전투방 프리팹 5~7개.
- 시작방, 일반 전투방, 첫 폭탄 보상, 보스 전실, 보스방.
- 보물/회복/비밀방은 시간이 남을 때 또는 프로토타입 이후.

## 구현된 프로토타입 전투방 스키마

`PrototypeCombatRoomDefinitionAsset`이 Unity 저작 원본이며 `CombatRoomDefinition`으로 변환될 때 실행 불가능한 데이터를 거부한다. 현재 스키마는 다음을 소유한다.

- 안정적인 room ID와 room type.
- 홀수 격자 너비·깊이와 양수 셀 크기.
- 출입구 위치와 방향.
- 플레이어와 필수 추격자 spawn, 선택적 돌진형 spawn.
- 고정 벽 셀.
- 파괴 가능 벽 셀.
- 플레이어 입장 안전 셀.
- 최소 두 개의 퇴로 anchor.
- 순서가 있는 닫힌 폭탄 유도 순환 경로.

범용 적 spawn 목록과 제약, 보상·전환 anchor는 아직 구현하지 않았으며 방 그래프 작업에서 확장한다. 첫 구현은 독립 방 prefab 대신 이 데이터와 `TestSandbox` 씬 표현을 연결한다. 향후 prefab으로 분리해도 논리 셀 데이터가 권위 원본이라는 경계는 유지한다.

## 현재 수제 전투방 세트

세 방 모두 11×9, 셀 크기 1의 `Combat` 방이며 추격자를 사용한다. 마지막 방만 선택적 돌진형을 함께 사용한다.

| 순서 | ID / 자산 | spawn | 공간 의도 | 씬 / 다음 씬 |
|---:|---|---|---|---|
| 1 | `prototype-combat-loop` / `PrototypeCombatLoop.asset` | 플레이어 `(0, 0)`, 추격자 `(1, -1)` | 중앙 십자 고정 벽 4개, 파괴 벽 없음, 기존 전투 기준선 | `TestSandbox.unity` / `TestSandboxLanes` |
| 2 | `prototype-combat-lanes` / `PrototypeCombatLanes.asset` | 플레이어 `(0, -2)`, 추격자 `(0, 2)` | 세로 고정 벽 6개, 파괴 벽 `(-1,-1)·(1,-1)`: spawn 광역은 둘을 대각선 동시 파괴, 십자는 미도달 | `TestSandboxLanes.unity` / `TestSandboxPillars` |
| 3 | `prototype-combat-pillars` / `PrototypeCombatPillars.asset` | 플레이어 `(-3, -2)`, 추격자 `(3, 2)`, 돌진형 `(-3, 2)` | 엇갈린 고정 기둥 5개, 중앙 파괴 벽 `(0,0)`: 파괴 전 엄폐·파괴 후 공간 확장, 시작 세로 돌진 예고선 | `TestSandboxPillars.unity` / 없음 |

첫 방은 북·남, 두 번째는 북·남, 세 번째는 서·동 경계 출구를 갖는다. 각 방은 서로 다른 첫 cardinal 이동을 쓰는 퇴로 anchor 두 개와 닫힌 cardinal 유도 순환 경로를 소유한다. 출구는 아직 실제 문이나 런 그래프로 연결되지 않으며 공간 저작 검증용 메타데이터다.

유도 경로는 사람과 플레이테스트 도구가 읽는 공간 의도다. 현재 추격 AI에 waypoint를 강제하지 않으며 실제 유도 재미는 관찰 플레이테스트로 판단한다.

## 저작 불변식

- 플레이어와 모든 적은 서로 다른 셀에서 시작한다. 추격자는 시작 즉시 cardinal 접촉하지 않고, 선택적 돌진형도 플레이어와 즉시 인접하지 않는다.
- 플레이어 spawn은 안전 셀에 포함된다.
- 플레이어 spawn에서 서로 다른 첫 cardinal 이동을 사용하는 퇴로가 최소 두 개 존재한다. 경로는 spawn을 다시 통과하지 않고 퇴로 anchor에 도달해야 한다.
- 유도 경로는 최소 4개의 서로 다른 플레이 가능 셀이 닫힌 cardinal 순환을 이룬다.
- 모든 출구는 방향에 맞는 방 경계의 플레이 가능 셀이다.
- 고정 벽과 파괴 가능 벽을 제외한 초기 플레이 가능 셀은 모두 플레이어 spawn에서 연결되어 있다. 따라서 파괴는 진행 필수가 아니다.
- 파괴 가능 벽은 고정 벽, spawn, 안전 셀, 퇴로 anchor, 유도 경로, 출구와 겹치지 않는다.
- 씬 spawn·장애물 Transform의 XZ 셀과 논리 메타데이터가 일치한다.
- 폭발을 끊는 벽/기둥과 향후 파괴 가능 벽은 시각적으로 구분되어야 한다.
- 둘 이상의 폭탄 역할이 도입되면 서로 다른 위치 선택을 만들 공간이 있어야 한다.

## Editor 검증기

`PrototypeContentValidator`는 명시적 검증과 빌드 검증에서 다음을 오류로 보고한다.

- 누락되거나 잘못된 room ID/type, 범위 밖·중복 셀, 고정·파괴 가능 벽 겹침.
- 누락된 출구, 안전 셀, spawn, 퇴로 anchor, 유도 경로.
- 경계 방향이 틀린 출구, 끊긴 유도 경로, 연결되지 않은 플레이 영역, 단일 퇴로.
- 세 room asset의 ID 중복 또는 각 TestSandbox 씬이 순서에 맞지 않는 room asset을 참조하는 상태.
- 필수 추격자 또는 선택적 돌진형 spawn Transform이 저작 셀과 다르거나 방 전환 controller의 session·다음 씬·지연이 잘못된 상태.
- 논리 고정 벽과 `Environment/InteriorObstacles` 표현 셀의 누락·중복·추가.
- 논리 파괴 벽과 `Environment/DestructibleObstacles`의 황갈색 4분할 표현 셀·재질·Collider·presenter 참조 불일치.
- 돌진형 정의·collider 없는 prefab, 방별 선택적 spawn, session·presenter 참조 또는 적 수 구성이 권위 방 데이터와 다른 상태.
- Build Settings의 첫 enabled 씬 세 개가 중앙 루프→평행 통로→엇갈린 기둥 순서가 아닌 상태.

자동 검증이 방의 재미를 보증하지는 않는다. 시각 확인과 플레이테스트를 함께 수행한다.
