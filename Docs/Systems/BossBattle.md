# 보스 전투

- 상태: 첫 플레이테스트 규칙·Core·Unity 연결 `Accepted`, 튜닝·최종 연출·재미 판정 `Proposed`
- 설계 원본: [보스 페이즈 개선안](../GameDesign/BossPhaseImprovementProposal.md), `GDD_v0.2.md` 24~25·35~36장, `ProtoType_v0.2.md` 가설 F
- 코드 소유: 규칙은 `BombSwap.Core`, 세션·저작·표현은 `BombSwap.Unity`, 생성·검증은 `BombSwap.Editor`
- 구현 상태: [보스 페이즈 개편 수직 슬라이스](../Development/BossPhaseReworkSlice.md)

## 목적

보스전은 별도 물리나 탄막 규칙이 아니라 정수 XZ 격자, 기존 폭탄·벽 차단·연쇄·자폭병을 결합한 종합 시험이다. 플레이어는 보스를 유도하고 순차 투척과 parity 파동을 피하면서도 자신의 폭탄을 계속 적중시켜 전투 시간을 줄인다.

## 권위 상태

`BossBattleSimulation`이 다음을 권위로 소유한다.

- 체력 10, 현재 phase, phase 전환 예약
- 현재 패턴과 `Telegraph → Execute → Recovery` 경계
- 제한 추격 횟수와 각 한 칸 이동의 확정 결과
- 돌진 방향·최대 3칸 경로와 위험 셀
- 순차 투척 계획, 착탄 예약 셀, parity 행·순서
- 폭발 `BombId`별 중복 피해 차단과 생존 중 상시 피해
- 2페이즈 자폭병 1회 소환·해결 대기
- 일회성 최후 발악 실행 여부

Core는 Transform, Physics, Unity `Time`, Rigidbody를 읽지 않는다. `PrototypeGameSession`은 Core 전이를 기존 폭탄·체력·표현 사건으로 연결하며 규칙을 다시 판단하지 않는다.

## phase와 시퀀스

| phase | 체력 | 시퀀스 |
|---|---:|---|
| One | 10~8 | 추격 2 → 돌진 → 중앙 복귀 → 일반 폭탄 3 → 한 parity의 행별 파동 → 과열 |
| Two 첫 회차 | 7~3 | 중앙 복귀·자폭병 소환 → 추격 3·돌진 → 자폭병 해결 대기 → 중앙 복귀 → 일반/연쇄 4 → 두 parity 반전 → 과열 |
| Two 반복 | 7~3 | 추격 3 → 돌진 → 중앙 복귀 → 일반/연쇄 4 → 직전과 반대 parity 순서 → 과열 |
| LastStand | 2~0 | 빠른 추격 2 → 돌진 → 중앙 복귀 → 외곽→안쪽 연쇄 4 → parity 반전 → 마지막 과열 |

- 체력 7/2 임계값은 피해 적용 시 예약한다.
- 현재 시퀀스가 끝나는 안전 경계에서만 phase를 바꾼다.
- LastStand는 한 번만 실행한다. 마지막 과열 뒤 생존하면 새 소환 없이 Two 반복으로 돌아간다.
- 사망은 현재 패턴·위험·점유를 제거하고 방 클리어를 한 번만 발행한다.

## 이동과 근접 공격

### 제한 추격

- 한 번의 `LimitedChase`는 플레이어 현재 셀까지의 결정론적 BFS에서 다음 한 칸만 계획한다.
- Telegraph가 시작된 뒤에는 방향을 바꾸지 않는다.
- 이동이 완료된 다음 추격 step에서만 플레이어 위치를 다시 읽는다.
- phase별 2/3/2회 뒤 반드시 `FixedCharge`로 넘어간다.

### 고정 돌진

- Telegraph 시작 시 보스→플레이어의 우세 cardinal 축을 선택하고 최대 3칸을 잠근다.
- arena 밖, 비보행 지형에서 끝난다. actor가 있는 셀은 위험 셀에는 포함하지만 보스는 그 직전까지만 이동한다.
- 실제 플레이어 피해는 Execute 전이의 위험 셀과 플레이어 셀이 일치할 때 기존 무적 규칙으로 1 적용한다.

### 중앙 복귀와 표시

- 투척 전 `ReturnToCenter`가 BFS 경로로 시작 셀까지 복귀한다.
- 정확한 다음 이동 셀 고스트는 사용하지 않는다.
- `Movements`의 확정된 여러 셀만 presenter가 패턴 실행 시간에 나눠 보간하며 pause 중 멈춘다.
- 돌진 위험 차선, 착탄·소환 셀, 현재 parity 행은 표시한다.

## 보스 폭탄

### 계획

- 4~6개의 저작 투척 앵커 중 현재 폭탄·actor가 없는 셀을 후보로 사용한다.
- 플레이어와 가까운 후보부터 안정 좌표 순서로 선택한다.
- One은 서로 다른 일반 폭탄 3개를 사용한다.
- Two는 `일반 → 인접 연쇄 → 일반 → 인접 연쇄` 4개를 사용한다.
- LastStand는 플레이어에게서 먼 외곽 두 지점과 각 중앙 방향 인접 셀에 연쇄 폭탄 4개를 사용한다.
- 모든 목표는 Telegraph 시작에 read-only `BossBombAttackPlan`으로 잠근다.

### 비행과 착탄

```text
Telegraph 예약 → Execute에서 순차 발사 → 포물선 보간 → 착탄 → BombSimulation 설치·fuse 시작
```

- 예약 셀에는 플레이어 폭탄을 설치할 수 없다.
- 비행 중에는 논리 bomb 점유가 없다.
- `BossBombFlight`는 순번·정의·시작/목표 셀·발사/착탄 시각을 소유한다.
- `PrototypeBombPresenter`는 Rigidbody 없이 고정 포물선과 회전을 표현하고, 착탄한 같은 시각 인스턴스를 일반 폭탄 표시로 넘긴다.
- 착탄 이벤트와 폭탄 생성은 순번 오름차순으로 처리한다.
- fuse, 벽 차단, 파괴 벽, 0.15초 연쇄는 기존 `BombSimulation` 계약을 그대로 사용한다.
- 보스 소유 폭탄은 보스에게 피해를 주지 않는다.

## parity 파동

- arena의 보행 가능한 셀을 행별로 나누고 `(X + Z) & 1` parity에 맞는 셀만 현재 위험으로 노출한다.
- 각 행은 별도 Telegraph/Execute/Recovery step이다. 모든 셀을 동시에 공격하지 않는다.
- One은 한 parity만 한쪽 끝에서 진행한다.
- Two와 LastStand는 첫 parity 뒤 반대 parity를 진행하며 다음 반복에서는 시작 parity를 교대한다.
- Execute에서 현재 행에 있는 플레이어만 패턴 피해 1을 받는다. 먼저 끝난 행은 이후 안전 공간으로 재사용할 수 있다.

## 피해와 과열

- 살아 있는 보스는 `Telegraph`, `Execute`, `Recovery`와 패턴 종류에 관계없이 플레이어 폭탄 피해를 받는다.
- 서로 다른 `BombId` 하나당 피해 1을 적용하며 패턴별·과열별 누적 상한은 없다.
- 같은 `BombId`의 중복 폭발과 사망 뒤 폭발은 상태를 구분해 거부한다.
- One/Two/LastStand 실제 과열 시간은 2.0/1.5/2.25초다. 과열은 공격 주기의 휴식·재정비 표현이며 피해 가능 여부를 바꾸지 않는다.
- 보스 소유 폭탄은 계속 자기 피해를 주지 않고, 자폭병 폭발은 보스 셀을 포함할 때 1피해를 준다.

## 자폭병 합동 구간

- 체력 7 이하 전환 뒤 보스는 중앙으로 복귀하고 `SummonSelfDestruct`를 한 번 실행한다.
- Core는 저작 소환 앵커 중 비점유 바닥을 고르고 플레이어와 Manhattan 거리가 먼 셀을 안정 좌표 순으로 잠근다.
- 소환 셀은 Telegraph 위험 셀로 먼저 표시되고 Execute에서 자폭병을 생성한다.
- 자폭병은 기존 BFS·WarningChase·조기 점화·0.75초 fuse를 사용한다.
- 생성 4.5초 뒤 아직 추격 중이면 현재 셀에서 강제 점화한다. 보스의 돌진 Telegraph/Execute 중에는 강제 점화를 보류해 최대 위협을 겹치지 않는다.
- 보스는 `WaitForSelfDestruct`에서 해결 사건을 받을 때까지 강화 투척으로 진행하지 않는다.
- 자폭병 폭발이 보스 셀을 포함하면 과열 여부와 무관하게 1피해를 준다. 같은 폭탄 ID는 한 번만 처리한다.

## 실제 저작값

`PrototypeBossDefinition.asset`의 현재 값:

- 체력 10, Two 임계 7, LastStand 임계 2, 패턴 피해 1
- 기존 asset의 `maxOverheatDamage: 2` 직렬화 값은 호환을 위해 남아 있지만 현재 Core 피해 규칙에는 사용하지 않는다.
- 추격 횟수 One/Two/LastStand = 2/3/2, 돌진 거리 3
- 돌진 0.7초 예고·0.3초 실행·0.5초 회복
- 중앙 복귀 0.2/0.7/0.1초
- 전환 1.1/0.1/0.1초, 소환 0.8/0.2/0.2초
- 투척 0.35/1.8/0.1초, 비행 0.45초, 투척 간격 0.4초
- parity 행 step 0.12/0.08/0.08초
- 과열 One/Two/LastStand = 2.0/1.5/2.25초
- 자폭병 강제 점화 = 소환 뒤 4.5초

보스 arena는 11×9, 투척 앵커 6개, 소환 앵커 3개, 네 고정 기둥과 두 개 이상의 퇴로를 가진다. 구체 셀은 [방 저작](RoomAuthoring.md)이 소유한다. 모든 수치는 플레이테스트 전까지 `Proposed`다.

## Unity 연결

- `PrototypeGameSession`: Core 전이, 예약 셀, 비행·착탄, 자폭병 생성/강제 점화, 보스/플레이어 피해, 방 클리어.
- `PrototypeBossPresenter`: 보스 위치 보간, 상태/phase 색, 돌진·착탄·소환·parity 위험 셀. 정확한 이동 목적지 고스트는 비활성이다.
- `PrototypeBombPresenter`: 정의별 풀, 보스 포물선 비행, 착탄 뒤 fuse 표시와 폭발 셀.
- `PrototypeSelfDestructPresenter`: 2페이즈 도중 생성 사건을 받아 동적으로 인스턴스를 만든다.
- `PrototypeHealthHud`: 체력 10과 phase 1/2/3을 사건 기반으로 표시한다.
- `BossBattlePlaytest.unity`: 던전 이동 없이 이 계약만 빠르게 확인하는 전용 씬이다.

## 불변식

- 예고한 돌진·착탄·소환·parity 셀과 실제 논리 결과가 일치한다.
- 추격은 한 칸마다 재판단하고 정해진 횟수 뒤 끝난다.
- 비행 폭탄은 착탄 전에 격자를 점유하거나 fuse를 소비하지 않는다.
- 보스 폭탄도 종류와 무관하게 같은 연쇄 스케줄러를 사용한다.
- 자폭병 합동 추격과 강화 투척·parity는 겹치지 않는다.
- 플레이어 폭탄과 자폭병은 살아 있는 보스에게 패턴 상태와 무관하게 피해를 주며 같은 `BombId`는 한 번만 처리한다.
- 생존 중 예약된 phase 전환은 현재 시퀀스 중간에 일어나지 않는다. 단, 치명 피해는 공격 상태와 관계없이 즉시 사망을 확정한다.
- 보스 사망과 방 클리어는 한 번만 발생한다.

## 검증

- EditMode: 정의·3 phase, 추격·돌진, 순차 투척, parity 행, 모든 생존 상태의 피해·중복 차단, 지연 전환, 소환 셀 잠금·해결 대기, 자폭병 피해, LastStand 단일 실행, 사망·시계·저작 앵커 검증.
- PlayMode: Telegraph 중 실제 폭탄 피해, 세션 전이, 포물선 flight→landing→fuse, 예약 셀, 자폭병 동적 생성·강제 점화, presenter pause, HUD phase, 실제 던전 보스 씬 회귀.
- Content: 보스 정의, 폭탄/prefab, 6 투척·3 소환 앵커, 전용 플레이테스트 씬과 기존 Build Settings 유지.
- 사람: 공격 패턴 중 폭탄 지속 적중, 자폭병 보스 유도, parity 안전 칸 재사용, phase 차이·최후 발악 공정성, 60~150초 목표 시간.
- WebGL: 전체 던전 경로가 모든 phase를 진행하고 살아 있는 보스에게 플레이어 폭탄 피해가 적용되는지 검증한다.
