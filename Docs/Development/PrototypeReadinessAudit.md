# 프로토타입 준비도 감사

- 감사 기준 코드: `772dc78` (`docs: record playtest analyzer verification`)
- 고정 사람 플레이 빌드 코드: `923a9b5` (`feat: export WebGL playtest logs`)
- 고정 WebGL 산출물: `Artifacts/Verification/20260816-062845-playtest-log-web-postcommit/WebGLBuild`
- 판정: **GDD 필수 구현 범위 충족 / 프로토타입 재미 완료 판정 보류**
- 권위 원본: [GDD v0.2 36~38장](../GameDesign/GDD_v0.2.md), [프로토타입 완료 판정](../GameDesign/ProtoType_v0.2.md), [Definition of Done](DefinitionOfDone.md)

## 목적

“기능이 코드에 존재한다”, “자동 회귀가 통과한다”, “사람 플레이에서 가설이 지지된다”를 분리한다. 다음 세션이 자동화 성공을 재미 검증으로 오인하거나, 반대로 이미 구현된 GDD 필수 기능을 다시 만드는 일을 막는다.

이 문서는 GDD나 시스템 계약을 재정의하지 않는 현재 스냅샷이다. 게임 의도는 GameDesign, 실제 시스템 동작은 Systems, 최신 작업 상태는 [CurrentState](CurrentState.md)가 계속 소유한다.

## 증거 표기

| 표기 | 증거 |
|---|---|
| `E305` | 연결 Unity EditMode `305/305`, `Artifacts/Verification/ConnectedTests/20260815-215241-030.json` |
| `P127` | 연결 Unity PlayMode `127/127`, `Artifacts/Verification/ConnectedTests/20260815-215305-947.json` |
| `W39` | 11씬 Development WebGL keyboard smoke `39/39`, `Artifacts/Verification/20260816-064439-playtest-analyzer-postcommit-browser/browser-smoke.json` |
| `G14` | 같은 빌드의 독립 Gamepad 재시도 `14/14`, `gamepad-smoke-retry.json` |
| `V0` | `923a9b5` 연결 빌드 전 콘텐츠 validator, BuildReport warning/error `0`, Unity Console Error/Warning `0` |

Gamepad 첫 post-commit 실행의 pause 재개 step timeout은 같은 폴더의 `gamepad-smoke.json`에 보존되어 있다. `G14`는 독립 재시도 성공을 뜻하며 최초 실패를 없던 일로 만들지 않는다.

## GDD 36 필수 구현 감사

### 플레이어

| GDD 요구 | 구현 근거 | 자동 증거 | 구현 판정 |
|---|---|---|---|
| 기본 캐릭터 1명 | [격자와 이동](../Systems/GridAndMovement.md), `PrototypePlayerController` | `E305`, `P127`, `W39` | 충족 |
| 체력 5칸 | [피해와 무적](../Systems/DamageAndInvulnerability.md), `PrototypePlayerVitalsAsset` | `E305`, `P127`, `W39` | 충족. 수치는 `Proposed` |
| 4방향 연속 격자 이동 | [프레임 연속 이동](ContinuousPlayerMovementSlice.md) | `E305`, `P127`, `W39`, `G14` | 충족 |
| 적 접촉 피해 | [추격자 접촉 피해](ChaserContactDamageSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 자기 폭발 피해 | [플레이어 자기 피해](PlayerSelfDamageSlice.md) | `E305`, `P127`, `W39`, `G14` | 충족 |
| 피격 후 짧은 무적 | [피해와 무적](../Systems/DamageAndInvulnerability.md) | `E305`, `P127`, `W39` | 충족. `0.75초`는 `Proposed` |

### 폭탄

| GDD 요구 | 구현 근거 | 자동 증거 | 구현 판정 |
|---|---|---|---|
| 기본 폭탄 1종 | [기본 십자 폭탄](BasicCrossBombSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 선택 가능한 추가 폭탄 2종 이상 | [광역 폭탄](AreaBombSlice.md), [방향성 직선 폭탄](DirectionalLineBombSlice.md) | `E305`, `P127`, `W39` | 충족. 후보는 `prototype-area`, `prototype-line` |
| 폭탄 무기 슬롯 2개 | [무기 슬롯과 쿨타임](../Systems/WeaponSlotsAndCooldown.md) | `E305`, `P127`, `W39`, `G14` | 충족 |
| 무기별 독립 설치 쿨타임 | [두 슬롯 쿨타임](TwoSlotCooldownSlice.md) | `E305`, `P127`, `W39` | 충족. 재미 가설 C는 별도 |
| 무기 교체 쿨타임 | [무기 슬롯과 쿨타임](../Systems/WeaponSlotsAndCooldown.md) | `E305`, `P127`, `W39` | 충족. 수치는 `Proposed` |
| 설치된 폭탄 유지 | [폭탄과 폭발](../Systems/BombAndExplosion.md) | `E305`, `P127`, `W39` | 충족 |
| 모든 폭탄 간 연쇄 | [폭탄과 폭발](../Systems/BombAndExplosion.md) | `E305`, `P127`, `W39` | 충족 |
| 짧은 연쇄 지연 | [폭탄과 폭발](../Systems/BombAndExplosion.md) | `E305` | 충족 |
| 폭탄 칸 통과 제한 | [설치 직후 통과](BombPassThroughSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 고정 벽 폭발 차단 | [폭탄과 폭발](../Systems/BombAndExplosion.md) | `E305`, `P127`, `W39` | 충족 |
| 파괴 벽 폭발 차단·해당 셀 파괴 | [파괴 가능 벽](DestructibleWallSlice.md) | `E305`, `P127`, `W39` | 충족 |

### 적

| GDD 요구 | 구현 근거 | 자동 증거 | 구현 판정 |
|---|---|---|---|
| 기본 추격자 | [기본 추격자](BasicChaserSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 돌진형 적 | [돌진형 적](ChargerEnemySlice.md) | `E305`, `P127`, `W39` | 충족 |
| 갑옷 적 | [갑옷 적](ArmoredEnemySlice.md) | `E305`, `P127`, 전용 WebGL 증거는 [CurrentState](CurrentState.md) | 충족 |
| 횟수제 내구도와 첫 피격 후 행동 변화 | [적 행동](../Systems/EnemyBehavior.md), [갑옷 적](ArmoredEnemySlice.md) | `E305`, `P127`, 전용 갑옷 smoke | 충족. 가설 D는 사람 검증 필요 |

### 던전

| GDD 요구 | 구현 근거 | 자동 증거 | 구현 판정 |
|---|---|---|---|
| 수제 일반방 프리팹 5~7개 | [다섯 번째 전투방](FifthCombatRoomSlice.md), [방 저작](../Systems/RoomAuthoring.md) | `E305`, `P127`, `V0`, `W39` | 최소치 5개로 충족 |
| 아이작형 한 층 그래프 | [던전 생성](../Systems/DungeonGeneration.md) | `E305`, `P127`, `W39` | 충족. seed 결정론·제한 정보 탐색 포함 |
| 한 런 일반방 3~5개 | [던전 그래프 Core](DungeonGraphCoreSlice.md) | `E305`, `W39` | 4~5개로 충족 |
| 첫 전투 후 폭탄 선택 | [던전 폭탄 보상](DungeonBombRewardSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 보스 전실 | [던전 scene lifetime](DungeonSceneLifetimeSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 보스방 | [보스 Core](BossCoreSlice.md) | `E305`, `P127`, `W39` | 충족 |

### 보스

| GDD 요구 | 구현 근거 | 자동 증거 | 구현 판정 |
|---|---|---|---|
| 별도 체력 | [보스 전투](../Systems/BossBattle.md) | `E305`, `P127`, `W39` | 충족 |
| 격자 기반 패턴 | [보스 Core](BossCoreSlice.md) | `E305`, `P127`, `W39` | 충족 |
| 최소 2개의 공격 패턴 | [보스 Core](BossCoreSlice.md)의 행·열·체크무늬 | `E305`, `P127`, `W39` | 3개로 충족 |
| 체력 구간 변화 또는 강화 | [보스 전투](../Systems/BossBattle.md) | `E305`, `P127`, `W39` | 2페이즈로 충족 |
| 패턴 후 반격 기회 | [예고 목적지 보스 이동](BossMovementSlice.md) | `E305`, `P127`, `W39` | Recovery 2.75초로 충족. 체감은 사람 검증 필요 |

## GDD 37 선택 범위 감사

| 선택 범위 | 현재 상태 | 결정 |
|---|---|---|
| 보물방 | 미구현 | 아래 패시브 gate와 함께 보류 |
| 패시브 아이템 선택 1회 | 미구현 | [프로토타입 6.4](../GameDesign/ProtoType_v0.2.md)의 선행 조건 미충족으로 `Deferred` |
| 회복방 | 구현·자동 검증 완료 | [GDD 기반 회복방](RecoveryRoomSlice.md), 사람 가치 검증 대기 |
| 일반방 재화 보상 | 구현·자동 검증 완료 | [전투 클리어 보상](CombatClearRewardSlice.md), 소비처 없는 임시 점수 |
| 금이 간 벽 비밀방 | 구현·자동 검증 완료 | [비밀방](SecretRoomSlice.md), 발견성 검증 대기 |
| 추가 폭탄 1종 | 기본 외 광역·직선 2종 구현 | 필수 추가 후보 2종 요구까지 함께 충족 |

패시브 아이템 추가 조건인 “아이템 없이도 기본 전투가 재미있음”, “다른 빌드를 시도하려는 욕구”, “행동을 명확히 바꾸는 아이템”은 아직 사람 증거가 없다. 콘텐츠 양으로 핵심 가설의 미판정을 덮지 않는다.

## 프로토타입 완료 판정 감사

| 완료 조건 | 현재 사람 증거 | 판정 |
|---|---|---|
| 기본 폭탄으로 적을 유도한다 | `PT-20260814-01`은 설치 뒤 행동 타임라인 미수집 | `Insufficient evidence` |
| 두 폭탄의 설치 위치와 목적이 다르다 | `PT-20260815-02`는 폭탄별 설치 이유 미수집 | `Insufficient evidence` |
| 교체가 단순 쿨타임 로테이션만이 아니다 | 공간·상황별 교체 이유 미수집 | `Insufficient evidence` |
| 약한 적 한 번 처치와 중형 적 변화가 모두 재미있다 | 갑옷 자동 상태 전이는 검증했지만 첫 피격 가독성·반복감 미관찰 | `Insufficient evidence` |
| 최소 세 방에서 다른 전투 구도가 발생한다 | 다섯 방을 구현했지만 방별 유도·퇴로 행동 비교 미수집 | `Insufficient evidence` |
| 보스 퓨즈와 이동을 함께 계산한다 | 이전 정지 보스는 `Not supported`; 이동 보스의 자발적 선행 설치는 미관찰 | `Retest required` |
| 플레이어가 사망 원인을 설명한다 | UI와 source marker는 자동 검증했지만 직후 설명 응답 미수집 | `Insufficient evidence` |
| 성장 없이 즉시 재도전한다 | `R` 재시작 기능은 자동 검증했지만 자발적 재도전 의사 미수집 | `Insufficient evidence` |

방향키 해제·빠른 직교 반복·벽 모서리 전환은 `PT-20260815-02`에서 `Supported`지만, 이는 위 여덟 재미 완료 조건의 대체 증거가 아니다.

## 마일스톤 판정

- GDD 36 필수 **구현 범위**: 충족.
- 현재 코드의 연결 Unity 회귀: EditMode `305/305`, PlayMode `127/127`, Console Error/Warning `0`.
- 고정 Development WebGL: 11씬 빌드 성공, keyboard `39/39`, Gamepad 독립 재시도 `14/14`, browser Console/page error `0`.
- 엄격한 Definition of Done 마일스톤: **미충족**.
  - 사람 플레이에서 완료 조건 8개가 판정되지 않았다.
  - 열린 Editor의 연결 검증은 현재 코드를 강하게 확인하지만, 같은 HEAD의 독립 batchmode `Fast → Full → Web` 단일 연속 실행을 대신하지 않는다.
  - Gamepad pause 재개 step의 간헐 timeout 1건이 보존되어 있다.

## 다음 결정

지금 추가해야 할 누락 GDD 필수 기능은 없다. 다음 단계는 [비밀방·탐색·보스 전체 경로 프로토콜](../Playtesting/SecretExplorationRouteProtocol.md)의 자연 플레이 증거 수집이다.

관찰 뒤 가장 강한 반복 문제 하나만 다음 수직 슬라이스로 선택한다.

| 관찰된 가장 강한 문제 | 다음 단일 변경 후보 |
|---|---|
| 금 간 벽을 장식으로 오해하거나 무작위 벽 검사가 반복됨 | Secret crack 대비·공개 피드백 한 변수 |
| 미니맵을 못 보거나 `C/V/?`를 반복 오해함 | 크기·범례·현재 방 강조 중 한 변수 |
| 보스 목적지는 읽지만 선행 설치가 발생하지 않음 | ghost timing·Recovery·이동 빈도 중 한 변수 |
| 광역·직선 폭탄을 같은 위치와 목적으로 사용함 | 폭탄 수치 또는 한 전투방 공간 계약 한 변수 |
| 갑옷 첫 피격이 성공으로 읽히지 않거나 두 번째 적중이 노동임 | 첫 피격 표현 또는 행동 변화 한 변수 |
| 전체 경로가 보스 직행/전부 청소 중 하나로 고정됨 | 가지 보상·방 수·왕복 편의 중 한 변수 |

보물방·패시브 아이템은 기본 전투와 두 폭탄 역할의 사람 증거가 지지된 뒤에만 다시 검토한다.
