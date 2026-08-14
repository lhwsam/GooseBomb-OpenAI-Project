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
- 플레이어와 필수 추격자 spawn, 선택적 돌진형·갑옷 적 spawn.
- 고정 벽 셀.
- 파괴 가능 벽 셀.
- 플레이어 입장 안전 셀.
- 최소 두 개의 퇴로 anchor.
- 순서가 있는 닫힌 폭탄 유도 순환 경로.

범용 적 spawn 목록과 제약, 일반화된 보상·전환 anchor는 아직 구현하지 않았다. 첫 폭탄 보상만 안전방 shell의 보행 가능한 `(-1,0)`·`(1,0)` 논리 셀을 고정 후보 위치로 사용하고 presenter가 초기화 때 Floor 여부를 검증한다. 첫 구현은 독립 방 prefab 대신 이 데이터와 `TestSandbox` 씬 표현을 연결한다. 향후 prefab으로 분리해도 논리 셀 데이터가 권위 원본이라는 경계는 유지한다.

## 현재 수제 전투방 세트

네 방 모두 11×9, 셀 크기 1의 `Combat` 방이며 추격자를 사용한다. 세 번째 방은 선택적 돌진형, 네 번째 방은 선택적 갑옷 적을 함께 사용한다.

| 순서 | ID / 자산 | spawn | 공간 의도 | 씬 / 다음 씬 |
|---:|---|---|---|---|
| 1 | `prototype-combat-loop` / `PrototypeCombatLoop.asset` | 플레이어 `(0, 0)`, 추격자 `(1, -1)` | 중앙 십자 고정 벽 4개, 파괴 벽 없음, 기존 전투 기준선 | `TestSandbox.unity` / `TestSandboxLanes` |
| 2 | `prototype-combat-lanes` / `PrototypeCombatLanes.asset` | 플레이어 `(0, -2)`, 추격자 `(0, 2)` | 세로 고정 벽 6개, 파괴 벽 `(-1,-1)·(1,-1)`: spawn 광역은 둘을 대각선 동시 파괴, 십자는 미도달 | `TestSandboxLanes.unity` / `TestSandboxPillars` |
| 3 | `prototype-combat-pillars` / `PrototypeCombatPillars.asset` | 플레이어 `(-3, -2)`, 추격자 `(3, 2)`, 돌진형 `(-3, 2)` | 엇갈린 고정 기둥 5개, 중앙 파괴 벽 `(0,0)`: 파괴 전 엄폐·파괴 후 공간 확장, 시작 세로 돌진 예고선 | `TestSandboxPillars.unity` / `TestSandboxArmor` |
| 4 | `prototype-combat-armor` / `PrototypeCombatArmor.asset` | 플레이어 `(0, -2)`, 추격자 `(4, 4)`, 갑옷 적 `(0, 1)` | 좌우 기둥 `(-2,-1)·(2,-1)·(-2,1)·(2,1)`, 열린 중앙 실험선, 파괴 벽 없음: 갑옷 2회 피격과 상태별 속도 비교 | `TestSandboxArmor.unity` / 없음 |

네 방은 모두 북 `(0,4)`, 동 `(5,0)`, 남 `(0,-4)`, 서 `(-5,0)` 중앙 경계의 잠재 출구를 갖는다. 잠재 출구는 항상 열린 문이 아니라 room geometry가 지원하는 후보이며, run 그래프가 필요한 방향만 활성 문으로 선택한다. 각 방은 서로 다른 첫 cardinal 이동을 쓰는 퇴로 anchor 두 개와 닫힌 cardinal 유도 순환 경로를 소유한다. 출구는 아직 실제 문 GameObject로 표현되지 않으며 [ADR-0007](../ADR/0007-Potential-Room-Exits.md)의 런 연결 메타데이터다.

유도 경로는 사람과 플레이테스트 도구가 읽는 공간 의도다. 현재 추격 AI에 waypoint를 강제하지 않으며 실제 유도 재미는 관찰 플레이테스트로 판단한다.

## 던전 런 카탈로그

`PrototypeDungeonCombatRoomCatalog.asset`은 네 전투방 ScriptableObject와 현재 대응 TestSandbox 씬 이름을 명시적으로 매핑한다. 런 시작 시 `PrototypeDungeonRunSession`이 카탈로그를 Core 정의로 변환하고 결정론적 배정 결과의 room ID를 다시 Unity asset·scene으로 해석한다. 현재 방, 방문과 클리어 같은 mutable 상태는 카탈로그에 쓰지 않는다.

`CombatRoomRotationUtility`는 run 배정의 0/90/180/270도 회전을 방 정의 전체에 적용한다. 90/270도에서는 너비와 깊이를 교환하고 플레이어·추격자·돌진형·갑옷 적 spawn, 고정·파괴 벽, 안전 셀, 퇴로 anchor, 유도 loop, 출구 셀과 방향을 같은 시계 방향으로 돌린다. 일부 목록이나 scene Transform만 따로 회전하는 것은 허용하지 않는다.

카탈로그의 entry는 null 방, 빈 씬 이름, 중복 room ID와 중복 씬 이름을 허용하지 않는다. 실제 씬 로드와 문 표현은 아직 연결되지 않았으므로 이 카탈로그는 검증된 조회 경계이며 독립적인 씬 수명 소유자는 아니다.

## 저작 불변식

- 플레이어와 모든 적은 서로 다른 셀에서 시작한다. 추격자는 시작 즉시 cardinal 접촉하지 않고, 선택적 돌진형과 갑옷 적도 플레이어와 즉시 인접하지 않는다.
- 플레이어 spawn은 안전 셀에 포함된다.
- 플레이어 spawn에서 서로 다른 첫 cardinal 이동을 사용하는 퇴로가 최소 두 개 존재한다. 경로는 spawn을 다시 통과하지 않고 퇴로 anchor에 도달해야 한다.
- 유도 경로는 최소 4개의 서로 다른 플레이 가능 셀이 닫힌 cardinal 순환을 이룬다.
- 모든 출구는 방향에 맞는 방 경계의 플레이 가능 셀이다.
- 같은 방향의 잠재 출구는 한 방 정의에 중복될 수 없다. 현재 네 프로토타입 전투방은 cardinal 네 방향을 각각 정확히 한 개 지원한다.
- 고정 벽과 파괴 가능 벽을 제외한 초기 플레이 가능 셀은 모두 플레이어 spawn에서 연결되어 있다. 따라서 파괴는 진행 필수가 아니다.
- 파괴 가능 벽은 고정 벽, spawn, 안전 셀, 퇴로 anchor, 유도 경로, 출구와 겹치지 않는다.
- 씬 spawn·장애물 Transform의 XZ 셀과 논리 메타데이터가 일치한다.
- 회전된 room 정의의 모든 셀이 교환된 경계 안에 있고, scene `GridRoot`의 같은 Y 회전 뒤 논리 셀·시각 위치가 일치한다.
- 폭발을 끊는 벽/기둥과 향후 파괴 가능 벽은 시각적으로 구분되어야 한다.
- 둘 이상의 폭탄 역할이 도입되면 서로 다른 위치 선택을 만들 공간이 있어야 한다.
- 보상 후보 visual은 Collider나 Transform 접촉을 규칙으로 사용하지 않는다. 플레이어의 확정된 논리 셀 전이만 선택을 일으킨다.

## Editor 검증기

`PrototypeContentValidator`는 명시적 검증과 빌드 검증에서 다음을 오류로 보고한다.

- 누락되거나 잘못된 room ID/type, 범위 밖·중복 셀, 고정·파괴 가능 벽 겹침.
- 중복 출구 방향 또는 현재 네 프로토타입 room asset에서 cardinal 잠재 출구가 빠진 상태.
- 누락된 출구, 안전 셀, spawn, 퇴로 anchor, 유도 경로.
- 경계 방향이 틀린 출구, 끊긴 유도 경로, 연결되지 않은 플레이 영역, 단일 퇴로.
- 네 room asset의 ID 중복 또는 각 TestSandbox 씬이 순서에 맞지 않는 room asset을 참조하는 상태.
- 던전 전투방 카탈로그 누락, 잘못된 entry 수, room asset·씬 매핑 순서 불일치 또는 Core 변환 실패.
- 필수 추격자 또는 선택적 돌진형·갑옷 적 spawn Transform이 저작 셀과 다르거나 방 전환 controller의 session·다음 씬·지연이 잘못된 상태.
- 논리 고정 벽과 `Environment/InteriorObstacles` 표현 셀의 누락·중복·추가.
- 논리 파괴 벽과 `Environment/DestructibleObstacles`의 황갈색 4분할 표현 셀·재질·Collider·presenter 참조 불일치.
- 돌진형 정의·collider 없는 prefab, 방별 선택적 spawn, session·presenter 참조 또는 적 수 구성이 권위 방 데이터와 다른 상태.
- 갑옷 적 정의·collider 없는 prefab, 방별 선택적 spawn, session·presenter 참조 또는 상태별 표현 구성이 권위 방 데이터와 다른 상태.
- 여덟 던전·TestSandbox 씬에 `PrototypeHealthHud`가 정확히 하나가 아니거나 해당 씬의 `PrototypeGameSession`을 참조하지 않는 상태.
- Build Settings의 첫 enabled 씬 여덟 개가 시작→폭탄 보상→보스 전실→보스→중앙 루프→평행 통로→엇갈린 기둥→갑옷 실험 순서가 아닌 상태.

자동 검증이 방의 재미를 보증하지는 않는다. 시각 확인과 플레이테스트를 함께 수행한다.
