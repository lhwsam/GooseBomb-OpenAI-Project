# 보스 전투

- 상태: 핵심 방향과 Core 상태 계약 `Accepted`, Unity 콘텐츠·수치 `Proposed`
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
- 1페이즈는 짝/홀 parity를 바꾸며 열 위험과 행 위험을 교대한다. 현재 Recovery 종료 시 체력이 임계값 이하이면 안전 전환 지점에서 2페이즈가 되고, 이후 parity가 바뀌는 체크무늬 위험을 사용한다.
- 각 패턴은 보행 가능한 arena cell만 대상으로 하며 위험 셀과 안전 셀이 모두 존재하지 않는 arena를 초기화 단계에서 거부한다.
- 보스는 `Recovery`에서만 폭탄 피해를 받는다. 같은 `BombId` 중복, 비취약 구간과 사망 뒤 피해를 구분하고, 치명 피해 시 보스 actor 점유와 위험 셀을 한 번만 제거한다.
- 큰 시계 진행에서도 각 전이의 논리 예약 시각을 보존한다. Unity 연결은 frame마다 최대 한 전이를 소비해 놓친 Recovery를 건너뛰지 않도록 구성할 예정이다.

Core 테스트 fixture는 체력 4, 2 이하에서 2페이즈, 패턴 피해 1을 사용한다. 1페이즈 시간은 1.0초 예고·0.25초 실행·2.0초 회복, 2페이즈는 0.75초·0.25초·1.5초다. 이는 ScriptableObject로 채택된 실제 콘텐츠 값이 아니라 Unity 수직 슬라이스를 연결하기 전의 `Proposed` 기준선이다.

상세 구현·검증 범위와 다음 연결 순서는 [보스 Core 수직 슬라이스](../Development/BossCoreSlice.md)가 소유한다.

## 불변식

- 피해 가능한 기회가 플레이어에게 읽힌다.
- 피할 수 없는 입장 직후 공격을 하지 않는다.
- 예고 셀과 실제 위험 셀이 동일한 규칙 좌표를 사용한다.
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

현재 앞의 세 Core 항목만 자동 검증됐다. Unity 보스 정의 asset, 보스방 session/presenter, 플레이어 패턴 피해, HUD·승리와 WebGL 가독성은 아직 구현되지 않았다.
