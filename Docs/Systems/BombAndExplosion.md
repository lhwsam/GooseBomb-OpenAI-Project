# 폭탄과 폭발

- 상태: 핵심 규칙 `Accepted`, 개별 폭탄 수치 `Proposed`
- 설계 원본: `GDD_v0.2.md` 6.2, 6.3, 7장, 10~12장
- 코드 소유: `BombSwap.Core`

## 목적

몇 초 뒤 위험 공간을 설계하고 적을 유도하면서 자기 폭발을 피하는 핵심 전투를 제공한다.

## 플레이어에게 보이는 동작

- 현재 선택된 폭탄을 유효한 셀에 설치한다.
- fuse가 끝나면 폭탄 종류가 정의한 셀 패턴으로 폭발한다.
- 폭발은 플레이어, 적, 다른 폭탄, 파괴 가능 벽과 상호작용한다.
- 다른 폭탄에 닿으면 폭탄 종류와 관계없이 짧은 고정 지연 뒤 연쇄 폭발한다.
- 플레이어도 자기 폭발에 피해를 받을 수 있다.

프로토타입 폭탄 후보는 기본 십자, 방향성 직선, 3x3 광역이다. 상태 이상 폭탄은 후순위다.

## 데이터

폭탄 정의는 최소한 다음 의도를 가진다.

- 안정적인 `BombDefinitionId`
- 공간 패턴과 방향 규칙
- fuse 시간
- 범위 또는 패턴 크기
- 설치 쿨타임
- 피해 의미
- 표현용 prefab/VFX/audio 참조는 Unity 저작 데이터에만 존재

중복 스탯을 늘리지 않는다. 공간 역할 차이를 수치 차이보다 먼저 만든다.

현재 Core의 최소 `BombDefinition`은 안정적인 ID, 폭발 모양, 양수 fuse, 0 이상의 범위를 가진다. `BombWeaponDefinition`이 이 폭발 정의와 설치 쿨타임을 묶어 슬롯 시스템에 제공한다. 정확한 값은 호출자가 주입하며 코드 기본값으로 고정하지 않는다. 구현된 모양은 cardinal 네 방향으로 전파하는 `Cross`, 원점을 포함한 Chebyshev 정사각 영역을 평가하는 `SquareArea`, 설치 순간의 cardinal 방향 한 ray만 전파하는 `ForwardLine`이다. 폭탄별 위력은 아직 없다. 플레이어 자기 피해는 폭탄 정의에 중복 저장하지 않고 폭발 사건을 소비하는 체력 시스템이 현재 고정 피해 1로 적용한다.

TestSandbox는 검증된 `PrototypeBombDefinitionAsset`에서 안정 ID, 모양, fuse, 범위, 설치 쿨타임과 bomb/explosion-cell prefab을 읽는다. 현재 플레이어용 `prototype-cross`·`prototype-area`·`prototype-line`은 모두 fuse 2초를 사용하며, 각각 `Cross`·범위 2·설치 1.5초, `SquareArea`·범위 1·설치 2.5초, `ForwardLine`·범위 3·설치 2.25초다. 자폭병 전용 `prototype-self-destruct-blast`는 `Cross`·fuse 0.75초·범위 2이고, 투척병 전용 `prototype-thrower-blocker`와 보스 전용 `prototype-boss-throw`·`prototype-boss-chain`은 모두 `Cross`·fuse 2초이며 범위는 각각 1·2·2다. 네 적 폭탄 정의는 플레이어 무기 슬롯·설치 쿨타임을 사용하지 않는다. 폭발 데이터와 쿨타임은 Core 정의로 변환되고 표현 참조는 Core에 전달되지 않는다. 모든 수치 집합은 플레이테스트 전까지 `Proposed`다.

플레이어 설치 폭탄의 표현 프리팹은 `Assets/Game/Content/Prefabs/Bomb/Player`가 권위 경로다. `prototype-cross`는 `NormalBomb.prefab`, `prototype-area`는 `RangeBomb.prefab`, `prototype-line`은 `StraightBomb.prefab`을 사용한다. 세 프리팹의 Animator·모델 Transform은 표현 전용이며 논리 셀 점유와 fuse는 각 `PrototypeBombDefinitionAsset`이 계속 소유한다.

## 폭발 전파 규칙

각 방향은 원점에서 가까운 셀부터 평가한다.

1. 범위 밖이면 종료한다.
2. 파괴 불가 벽이면 그 셀에 폭발 효과를 만들지 않고 전파를 종료한다.
3. 파괴 가능 벽이면 벽 파괴/피격 효과를 적용하고 그 뒤 전파를 종료한다.
4. 일반 바닥이면 폭발 셀을 추가한다.
5. 폭탄이 있으면 연쇄 스케줄러에 등록한다. 동일 폭탄을 두 번 예약하지 않는다.

`SquareArea`는 원점을 먼저 포함한 뒤 `deltaZ`, `deltaX` 오름차순으로 정사각 영역의 각 셀을 독립 평가한다. `Void`와 파괴 불가 벽은 영향 셀에서 제외하고, 파괴 가능 벽은 영향·파괴 목록에 포함한다. ray 전파가 아니므로 한 셀의 벽이 다른 영역 셀을 가리지 않는다. 같은 시각 폭발 묶음의 벽 파괴 지연과 연쇄 예약 규칙은 `Cross`와 동일하다.

`ForwardLine`은 원점 뒤 설치 순간에 고정된 `North`·`East`·`South`·`West` 한 방향만 가까운 셀부터 평가한다. 방향은 플레이어가 나중에 이동하거나 키를 떼어도 바뀌지 않는다. `CardinalDirection.None`으로는 이 모양을 설치할 수 없고, 벽 차단·파괴와 연쇄 예약은 `Cross`의 각 ray와 같은 규칙을 사용한다.

## 상태와 전이

```text
Requested -> Placed -> Armed -> DetonationQueued -> Exploded -> Removed
                         ^             |
                         +-- chain ----+
```

- 설치 실패는 상태를 부분 변경하지 않는다.
- fuse 만료와 연쇄 예약이 같은 폭탄에 동시에 도달해도 한 번만 폭발한다.
- 연쇄는 즉시 재귀가 아니라 스케줄 사건으로 처리한다.

## 구현된 최소 Core 계약

- `BombSimulation`이 세션 내 증가하는 `BombId`, 설치자 `ActorId`, 설치 순간 방향, 활성 폭탄, 위치별 폭탄 점유, fuse와 연쇄 예약을 소유한다.
- 폭탄은 `Floor`에만 설치할 수 있고 같은 셀에 두 개를 설치할 수 없다. 설치자 actor와 새 폭탄의 동시 점유는 허용한다.
- 설치 실패는 ID를 소비하거나 격자 점유를 남기지 않는다.
- `ProcessDueBombs`는 주입된 `IGameClock`의 현재 시각까지 도달한 사건을 예약된 논리 시각 순서로 처리한다.
- 같은 논리 시각의 폭탄은 `BombId` 순서로 보고한다. 해당 그룹의 모든 폭발 범위를 먼저 계산한 후 파괴 가능 벽을 바닥으로 바꾸므로, 같은 시각의 다른 폭발이 방금 파괴된 벽을 통과하지 않는다.
- `Void`와 파괴 불가 벽은 효과 없이 해당 방향을 끝낸다. 파괴 가능 벽은 폭발 셀과 파괴 목록에 포함한 후 해당 방향을 끝낸다.
- 폭발 셀의 다른 활성 폭탄은 폭탄 정의와 관계없이 주입된 양수 고정 지연으로 한 번만 앞당겨 예약한다.
- 시계가 여러 사건 시각을 한 번에 지나가도 각 폭발은 원래 예약된 논리 시각으로 처리되어 프레임 호출 빈도에 따라 결과가 달라지지 않는다.
- 설치 직후 snapshot과 폭발 결과는 폭탄/정의 ID, 설치자 ID, 설치 방향, 원점, 논리 시각, fuse/chain 원인, 영향 셀, 파괴 벽을 읽기 전용으로 제공한다.

## 폭발 영향 오브젝트 분류

`BombExplosion`은 모든 반응 대상을 직접 소유하는 범용 목록이 아니라 두 종류의 확정 결과를 제공한다.

- `AffectedCells`는 폭발 footprint다. 플레이어·적·활성 폭탄과 전파를 바꾸지 않는 방 기믹이 이 셀 집합을 소비한다. `Affects(position)`은 이 read-only 결과의 포함 여부를 조회하는 편의 API다.
- `DestroyedWalls`는 `GridTerrain.DestructibleWall`만을 위한 지형 변경 결과다. 해당 셀을 `Floor`로 바꾸고 ray를 끝내는 전파 계약과 결합되어 있으므로 문·스위치 같은 다른 반응물을 여기에 넣지 않는다.
- 전파를 막거나 범위 계산을 바꾸는 새 환경 오브젝트는 Unity Collider callback이 아니라 Core grid/resolver 규칙으로 추가한다.
- 전파를 바꾸지 않는 방 기믹은 저작된 논리 셀 또는 경계를 `AffectedCells`에 매핑하고, 확정된 결과만 전용 Core 상태와 presenter에 전달한다.

현재 비밀문은 마지막 경우다. 문 앞 출구 셀은 `Floor`이며 binder가 `출구 셀 → Secret 연결 방향`을 방 단위로 보관한다. 그 셀이 `AffectedCells`에 포함되면 `DungeonRunState`에 공개를 요청한다. 비밀문은 지형이 아니므로 `DestroyedWalls`에 나타나지 않고 일반 파괴벽의 차단·파괴 규칙도 바꾸지 않는다.

## 구현된 Unity 수직 슬라이스

- `PrototypeGameSession`은 이동·폭탄·두 슬롯이 공유하는 하나의 논리 격자·시계를 소유하고 `PlaceBomb` 명령을 활성 슬롯, 현재 플레이어 셀과 Core의 마지막 바라보기 방향에 적용한다.
- 설치가 성공할 때만 `BombPlaced`, fuse 또는 연쇄 처리 결과가 확정될 때만 `BombExploded`를 발행한다.
- 보스 계획 설치는 플레이어 입력 성공 사건과 분리한 `BossBombPlaced`를 발행한다. snapshot과 최종 폭발은 같은 `BombSimulation`의 `BombId`·소유자·정의 ID를 사용한다.
- `PrototypeGameSession`은 설치 snapshot의 소유자가 현재 셀의 플레이어임을 근거로 한 번의 탈출 권한을 부여하고, 폭발로 폭탄이 제거되면 남은 권한을 종료한다.
- 확정된 폭발 셀에 현재 플레이어 논리 셀이 포함되면 체력 시스템에 해당 `BombId`의 피해를 한 번 전달하고, 무적 계약을 통과한 결과만 `PlayerDamaged`로 발행한다.
- 확정된 폭발 셀에 살아 있는 기본 추격자 또는 선택적 돌진형의 논리 셀이 포함되면 각 적 체력 시스템에 해당 `BombId`의 피해를 한 번 전달한다. 두 적은 모두 내구도 1이며 같은 결과에서 사망하면 각 논리 점유가 제거된다.
- `PrototypeBombPresenter`는 정의 ID별 설치 폭탄과 영향 셀 placeholder 풀을 사용하고, 직선 폭탄의 비대칭 설치체를 확정된 방향으로 회전한다. 공개 플레이어 폭탄 prefab 3종은 빈 `SparksEffect` 앵커를 fallback으로 유지하므로 로컬 VFX 패키지가 없는 clone에서도 Missing 참조 없이 동작한다. 권한 있는 작업자는 `Bomb Swap/Local Setup/Connect Licensed VFX`로 현재 패키지의 준비 파티클 prefab을 앵커 아래 `Particle` 자식으로 연결할 수 있다. 공개 변경을 준비할 때는 `Bomb Swap/Local Setup/Reset Player Bomb VFX to Public Fallback`으로 로컬 자식을 제거하며, 두 메뉴 모두 앵커의 저작 위치·회전을 보존한다. 설치 때 앵커를 Animator와 함께 활성화하고 폭발 때 Animator를 비활성화한다. 폭발 셀은 해당 정의의 표시 시간이 끝나면 같은 풀에 반환한다. 풀을 초과하면 규칙을 누락하지 않고 표현 인스턴스만 확장한다.
- `PrototypeDestructibleWallPresenter`는 room asset과 일치하는 정적 시각 셀을 검증하고 `BombExplosion.DestroyedWalls`가 확정된 뒤에만 대응 황갈색 4분할 블록을 비활성화한다. authored 시각이 없는 파괴 결과는 오류다.
- `PrototypeDungeonRoomBinder`는 현재 방의 미공개 Secret 연결을 문 앞 출구 셀에 매핑한다. `BombExplosion.AffectedCells`가 그 셀에 닿으면 해당 연결만 공개하고 door/minimap 표현을 갱신한다.
- 자폭병은 Telegraph 시작 시 `ActorId(6)` 소유의 `prototype-self-destruct-blast`를 자기 셀에 직접 설치한다. 이 적 폭탄은 `BombSimulation`의 활성 폭탄과 연쇄 스케줄러에는 포함되지만 플레이어 loadout·설치 쿨타임·`BombPlaced` 입력 성공 사건에는 포함되지 않는다.
- 자폭 폭발은 다른 정의와 같은 `BombExploded`·`AffectedCells`·`DestroyedWalls` 결과를 제공한다. 자기 폭발이 자폭병의 단일 사망을 확정하고, 범위 안 플레이어·다른 적·보스와 Gates 파괴문은 기존 소비 경로로 반응한다.
- 투척병은 Telegraph에서 서로 다른 목표 셀 3개를 잠근 뒤 세 방향으로 0.45초 표현 비행을 동시에 시작한다. 비행 중에는 폭탄 점유가 없고 각 착탄 순간에만 `ActorId(7)` 소유 `prototype-thrower-blocker`를 같은 `BombSimulation`에 설치한다. 이미 폭탄이 있는 셀의 발만 조용히 실패하며 다른 셀로 재조준하지 않는다.
- 성공 착탄한 세 투척 폭탄은 플레이어 폭탄과 같은 fuse·벽 차단·파괴벽·연쇄 스케줄러를 각각 사용한다. 투척병은 자기 소유 폭발만 무시하고 다른 소유자의 폭발에는 정상 피해를 받는다.
- 보스는 Telegraph 시작 시 잠근 퇴로 anchor에 `ActorId(5)` 소유 throw 폭탄을 설치하고, 2페이즈에는 그 안쪽 cardinal 셀에 chain 폭탄도 설치한다. throw footprint가 chain 셀을 포함하므로 전역 0.15초 연쇄 지연을 그대로 사용한다. 보스 폭탄은 플레이어·일반 적·벽에 정상 반응하지만 소유자인 보스는 자기 폭발 피해를 무시한다.
- TestSandbox의 폭탄/폭발 prefab은 collider 없이 시각 표현만 담당한다. 설치·차단·범위는 계속 Core 격자가 판정한다.
- 현재 수직 슬라이스는 플레이어 자기 피해, 기본 추격자·돌진형·갑옷 적·자폭병·투척병·보스 피해, 두 슬롯과 독립 설치·교체 쿨타임, 기본 십자·3×3 광역·앞쪽 직선·적 소유 십자 폭발을 포함한다. 추격자→돌진형→갑옷 적→자폭병→투척병→보스 고정 순서로 피해와 사망을 확정하고 마지막 적 뒤 단일 방 클리어를 발행한다. 투척병은 아직 메인 던전 카탈로그 밖 전용 씬에서만 활성화한다. 범용 다중 적 목록과 일반화된 적 폭탄 소유권 UI는 아직 없다.

## 불변식

- 폭탄 ID는 한 게임 세션에서 유일하다.
- 폭발한 폭탄은 다시 피해나 연쇄를 발생시키지 않는다.
- 같은 폭발 사건이 같은 대상에 중복 피해를 주지 않는다. 다중 폭발 사건은 피해/무적 규칙에 따라 별도 처리한다.
- 시각 VFX가 끝나기 전에도 논리 폭발 결과는 확정되어 있다.
- 모든 폭탄 종류는 동일한 연쇄 계약을 따른다.

## 자동 테스트

현재 EditMode 테스트는 다음 계약을 실행 가능하게 고정한다.

- 정의 ID 값 동등성, 양수 fuse, 0 이상 범위와 지원 모양 검증.
- 바닥 설치, 설치자 ID 보존, actor 동시 점유, 중복/비바닥 설치 실패의 원자성, 세션 ID 증가.
- 십자 범위 0과 2의 셀 집합.
- 광역 범위 1의 결정론적 3×3 셀 순서, `Void`·고정 벽 제외, 파괴 벽 포함과 비차폐 평가.
- 직선 범위 3의 네 cardinal 셀 순서, 옆·뒤 제외, 설치 방향 고정과 방향 누락 거부.
- `Void`·고정 벽 앞 종료와 파괴 벽 셀 포함 후 종료 및 실제 지형 변경.
- 같은 시각 폭발의 ID 순서와 벽 파괴 지연 적용.
- 서로 다른 정의 간 연쇄, 양수 고정 지연, 중복 예약 방지.
- fuse와 chain 동시 도달 시 단일 폭발.
- 큰 시계 진행에서도 예약 시각 순서가 보존되는 연쇄 처리.
- 적 소유 폭탄도 플레이어 폭탄과 같은 양수 고정 연쇄 지연·벽 차단·파괴벽 지연 적용을 사용하고 소유자와 정의 ID를 보존한다.
- 투척병 비행 3개는 착탄 전 점유를 만들지 않고, 성공 착탄마다 서로 다른 활성 폭탄 ID를 소유한다. 플레이어 폭발은 영향 셀에 닿은 각 `prototype-thrower-blocker`만 전역 연쇄 지연으로 한 번씩 앞당겨 기폭한다.

다음 항목은 후속 폭탄·피해 시스템에서 추가한다.

- 검증된 콘텐츠가 정한 최대 범위 경계.
- 범용 다중 적 목록의 피해 후보 수집과 대상별 중복 제거.

## 플레이테스트 관찰

- 설치 후 기다리기만 하지 않고 이동·유도가 이어지는가.
- 폭발 예고와 벽 차단이 읽히는가.
- 서로 다른 폭탄이 다른 위치 선택을 만드는가.
- 연쇄 지연이 의도와 결과를 이해할 만큼 충분한가.
