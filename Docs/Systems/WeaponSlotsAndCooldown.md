# 폭탄 슬롯, 교체, 쿨타임

- 상태: 두 슬롯·독립 설치 쿨타임 `Accepted`, 정확한 수치 `Proposed`
- 설계 원본: `GDD_v0.2.md` 8~9장, `ProtoType_v0.2.md` 가설 B·C
- 코드 소유: `BombSwap.Core`, 입력 연결은 `BombSwap.Unity`

## 목적

두 폭탄의 공간 역할을 상황에 따라 바꾸고, 각각의 재사용 가능 시간을 읽어 다음 행동을 계획하게 한다.

## 플레이어에게 보이는 동작

- 플레이어는 두 개의 폭탄 슬롯을 가진다.
- 새 던전 run은 첫 슬롯만 채운다. 첫 전투 뒤 보상방에서 후보 하나를 골라 빈 두 번째 슬롯을 채운다.
- 한 슬롯이 활성이고 다른 슬롯이 비활성이다.
- 설치는 활성 슬롯의 폭탄과 해당 슬롯의 설치 쿨타임을 사용한다.
- 두 슬롯의 설치 쿨타임은 서로 독립적이며 비활성 중에도 회복한다.
- 교체는 별도 교체 쿨타임의 영향을 받는다.
- 교체 예상 초기값 약 2초는 가설이며 플레이테스트로 확정한다.

## 상태

각 슬롯은 점유 여부와, 점유했다면 폭탄 정의 ID, 남은 설치 쿨타임, 사용 가능 여부를 가진다. loadout은 활성 슬롯 인덱스와 남은 교체 쿨타임을 가진다. 빈 슬롯은 준비된 폭탄이 아니며 교체 대상으로 사용할 수 없다.

설치 성공 시에만 활성 슬롯의 설치 쿨타임이 시작된다. 잘못된 셀이나 입력 차단으로 실패한 설치는 쿨타임을 소비하지 않는다.

## 구현된 경계

- `BombWeaponDefinition`: 폭발 규칙 `BombDefinition`과 양수 설치 쿨타임을 묶는 Core 불변 정의다.
- `BombWeaponLoadout`: 주입된 `IGameClock`을 읽어 활성 슬롯, 슬롯별 다음 설치 가능 시각과 다음 교체 가능 시각을 소유한다.
- `BombWeaponSlotSnapshot`: UI와 검증 계층에 정의 ID, 전체/남은 쿨타임, 준비 비율을 읽기 전용으로 제공한다.
- `DungeonBombLoadoutState`: 첫 폭탄, 2~3개의 보상 후보 ID와 한 번 선택된 두 번째 폭탄을 run 수명으로 소유한다.
- `PrototypeBombDefinitionAsset`: 폭발 데이터와 설치 쿨타임을 검증해 Core 무기 정의로 변환한다.
- `PrototypeBombLoadoutAsset`: 서로 다른 두 폭탄 정의와 교체 쿨타임을 검증하고 Core loadout을 만든다.
- `PrototypeBombRewardCatalogAsset`: 던전 시작 폭탄과 보상 후보 asset을 검증하고 `DungeonBombLoadoutState`를 만든다.
- `PrototypeGameSession`: `PlaceBomb`을 활성 슬롯에, `SwapBomb`을 Core loadout에 전달하고 성공한 교체만 `ActiveBombSlotChanged`로 발행한다.
- `PrototypeWeaponHud`: Core snapshot을 표시한다. 왼쪽 아래 두 슬롯의 활성 상태, 설치 준비 bar/시간과 교체 준비 시간을 보여준다.
- `PrototypeBombRewardPresenter`: 보상방의 기존 논리 이동 셀 사건으로 후보 접촉을 판정한다. 장치 상태를 직접 읽지 않는다.

standalone 검증 씬의 고정 조합은 1번 `prototype-cross`(`Cross`, fuse 2초, 범위 2, 설치 1.5초)와 2번 `prototype-area`(`SquareArea`, fuse 1.75초, 범위 1, 설치 2.5초)다. 던전은 `prototype-cross` 하나로 시작하고 첫 보상에서 `prototype-area` 또는 `prototype-line`(`ForwardLine`, fuse 2.25초, 범위 3, 설치 2.25초)를 고른다. 교체 2초와 모든 폭탄 수치는 `Proposed`다. 직선 후보는 설치 순간의 마지막 바라보기 방향 한 ray만 공격하며 비대칭 청록 설치체가 방향을 표시한다. 실제 재미 가설은 자동 테스트만으로 통과시키지 않는다.

## 불변식

- 두 슬롯은 서로 다른 상태 소유자이며 한쪽 설치가 다른 쪽 쿨타임을 덮어쓰지 않는다.
- 비활성 슬롯도 같은 게임 시간 기준으로 회복한다.
- 교체 실패는 활성 슬롯을 바꾸지 않는다.
- 빈 두 번째 슬롯은 교체·설치할 수 없고 유효한 보상 후보로 정확히 한 번만 채울 수 있다.
- 첫 보상은 현재 `BombReward` 방에서만 선택하며, 선택 결과는 방 scene이 아니라 run session에 남는다.
- 슬롯 교체와 폭탄 설치는 세션이 받은 `PlayerCommand` 순서대로 원자적으로 처리하며 각 명령의 성공/실패가 다음 명령에 보인다.
- UI는 Core 상태를 표시할 뿐 쿨타임을 계산하지 않는다.

## 핵심 리스크

독립 쿨타임이 항상 A 설치→B 교체→B 설치→A 교체의 고정 로테이션으로 굳을 수 있다. 다음 신호를 계측한다.

- 슬롯별 설치 횟수와 위치 차이.
- 교체 직후 즉시 설치 비율.
- 한 슬롯을 의도적으로 기다린 시간.
- 같은 공간 상황에서 반복되는 고정 순서.

문제가 확인되기 전 교체 제한, 공유 자원, 강제 순서를 추가하지 않는다.

## 자동 테스트

- 두 슬롯 쿨타임 독립 진행.
- 비활성 슬롯 회복.
- 설치 실패 시 쿨타임 미소비.
- 교체 쿨타임 경계와 일시정지.
- 같은 step의 설치/교체 명령 순서.

현재 EditMode는 독립 쿨타임, 비활성 회복, 실패 미소비, 교체 경계·시계 정지와 빈 슬롯·단일 보상 장착을 검증한다. PlayMode는 고정 두 슬롯 씬의 기존 `X`/`Z`, 던전 보상방의 빈 슬롯 수집과 다음 scene persistence, 해제 뒤 바라보기와 직선 폭탄 설치 방향 고정을 검증한다. 기본 WebGL smoke는 시작 십자 2회, `bomb-reward-selected-prototype-area`, 후속 전투방과 실제 보스 반격에서 `active-bomb-slot-1`과 `place-bomb-definition-prototype-area` 성공 사건을 요구한다. `Tools/DirectionalLineWebGLSmoke.mjs`는 오른쪽 `prototype-line` 후보를 고른 뒤 동쪽 설치, 북쪽 이동 명령, 동쪽 폭발 순서를 별도로 검증한다.
