# 인게임 UI 프리팹 저작 계약

- 상태: `Accepted`
- 기준일: 2026-08-24
- 대상: 무기 HUD, 체력 HUD, 던전 미니맵, 일시정지, 방 안내, 런 결과 화면
- 관련: [로비와 공통 TMP UI](../Development/LobbySlice.md), [사용자 설정](UserSettingsAndAudio.md), [서드파티 자산](ThirdPartyAssets.md), [최소 미니맵](../Development/MinimalMinimapSlice.md)

## 목적

인게임 UI의 계층과 기본 시각 요소를 C#이 매번 조립하지 않는다. 디자이너는 공유 프리팹을 Prefab Mode에서 직접 수정하고, presenter는 프리팹 인스턴스를 한 번 만든 뒤 확정된 게임 상태를 표시하는 역할만 맡는다.

이 전환은 Canvas를 매 scene에 복제하기 위한 것이 아니다. 각 gameplay scene은 presenter와 공유 프리팹 참조만 저장하며, 실행 중 해당 scene의 presenter가 프리팹 인스턴스를 하나 생성한다. 따라서 한 프리팹 수정이 모든 던전방과 독립 플레이테스트방에 적용된다.

## 권위 프리팹

| UI | 프리팹 | 런타임 소유 |
|---|---|---|
| 무기 슬롯·쿨타임 | `Assets/Game/Content/Resources/UI/PrototypeWeaponHudCanvas.prefab` | `PrototypeWeaponHud` |
| 플레이어·보스 체력·방 토큰 | `Assets/Game/Content/Resources/UI/PrototypeHealthHudCanvas.prefab` | `PrototypeHealthHud` |
| 플레이어 체력 한 칸 | `Assets/Game/Content/Resources/UI/PrototypePlayerHealthHeart.prefab` | `PrototypeHealthHud` |
| 제한 정보 미니맵 | `Assets/Game/Content/Resources/UI/PrototypeDungeonMinimapCanvas.prefab` | `PrototypeDungeonMinimapPresenter` |
| 미니맵 방 노드 | `Assets/Game/Content/Resources/UI/PrototypeDungeonMinimapRoom.prefab` | `PrototypeDungeonMinimapPresenter` |
| 미니맵 연결선 | `Assets/Game/Content/Resources/UI/PrototypeDungeonMinimapConnection.prefab` | `PrototypeDungeonMinimapPresenter` |
| 일시정지·공통 설정 | `Assets/Game/Content/Resources/UI/PrototypePauseCanvas.prefab` | `PrototypeGameSession`과 `PrototypePausePresenter` |
| 런 완료·실패 결과 | `Assets/Game/Content/Resources/UI/PrototypeRunCompletionCanvas.prefab` | `PrototypeRunCompletionPresenter` |
| 폭탄 보상방 안내 | `Assets/Game/Content/Resources/UI/PrototypeBombRewardCanvas.prefab` | `PrototypeBombRewardPresenter` |
| 회복방 안내 | `Assets/Game/Content/Resources/UI/PrototypeRecoveryPickupCanvas.prefab` | `PrototypeRecoveryPickupPresenter` |
| 비밀방 보상 안내 | `Assets/Game/Content/Resources/UI/PrototypeSecretRewardCanvas.prefab` | `PrototypeSecretRewardPresenter` |

production scene은 위 에셋을 직렬화 참조한다. `Resources/UI` 경로는 scene 없이 만드는 합성 PlayMode fixture도 같은 프리팹을 불러 검증하기 위한 보조 진입점이며, 런타임 이름·태그·계층 검색에는 사용하지 않는다.

## View와 Presenter 경계

- 프리팹 root의 `Prototype*View`는 Canvas와 presenter가 바꿔야 하는 TMP, Image, Button, Map root를 명시적 직렬화 참조로 가진다.
- 계층 이름은 사람이 읽기 위한 것이며 기능 연결의 권위가 아니다. 이름이나 배치 순서를 바꿔도 View 참조가 유지되면 동작해야 한다.
- presenter는 프리팹 인스턴스의 Text, fill amount, 활성 상태와 정의된 상태 색만 갱신한다. 플레이어 체력은 최대 체력만큼 공용 하트 프리팹을 준비하고 현재 체력만큼 `Full`, 나머지는 `Empty` 표현을 보인다. 미니맵처럼 개수가 상태에 따라 달라지는 자식도 C#에서 Image·TMP 계층을 조립하지 않고 공용 자식 프리팹을 필요한 수만큼 인스턴스화한다. RectTransform 배치, sprite, font, material, outline과 장식 계층은 바꾸지 않는다.
- View root 컴포넌트와 필수 참조를 삭제하거나 비우면 validator와 PlayMode가 실패해야 한다.
- 모든 Canvas 프리팹은 공통 960×600 `CanvasScaler` 기준을 유지한다. 기본 sorting order는 무기 100, 미니맵 109, 체력·방 안내 110, pause 250, 런 결과 300이다.

### 무기 HUD

두 슬롯의 배경, 준비 fill, 정의·쿨타임 TMP와 교체 상태 TMP는 프리팹 참조다. 활성/비활성 슬롯과 준비/냉각 색은 `PrototypeWeaponHudView` Inspector에서 수정한다. 표시 문구와 fill amount는 실제 loadout snapshot이 덮어쓴다.

### 체력 HUD

플레이어 체력은 `PlayerHeartContainer`로 참조한 panel/container와 `PrototypePlayerHealthHeart.prefab`을 사용한다. 하트 프리팹은 디자이너가 저작한 `Full`·`Empty` Image를 각각 보존하고 presenter는 상태에 따라 둘 중 하나만 표시한다. HUD 프리팹에 배치된 기본 5개 `PlayerHeart01..05` 인스턴스를 먼저 재사용하고 최대 체력이 더 크면 같은 프리팹을 추가 생성하며, 최대 체력이 줄면 남는 인스턴스는 비활성화한다. `PLAYER HP` 문구와 플레이어 fill bar는 사용하지 않는다.

방 토큰은 아이콘 옆 TMP에 접두 문구 없이 현재 숫자만 표시한다. 보스 panel은 `BossNameLabel`, `BossPhaseLabel`, `BossHealthValueLabel`과 `BossHealthFill`을 각각 명시적으로 참조한다. 보스 이름은 `BossNameLabel`의 저작 문자열로 따로 설정하며 presenter가 전투 상태 갱신으로 덮어쓰지 않는다. presenter는 phase 라벨, 공백을 포함한 `현재 / 최대` 수치 라벨과 fill amount만 갱신하고 디자이너가 저작한 각 라벨의 font·색·material과 fill sprite를 보존한다.

### 미니맵

패널과 `Map` RectTransform은 Canvas 프리팹 저작이다. 현재 run마다 달라지는 방 노드와 연결선은 각각 `PrototypeDungeonMinimapRoom.prefab`, `PrototypeDungeonMinimapConnection.prefab`을 `Map` 아래에 snapshot 갱신 시 필요한 수만큼 인스턴스화한다. presenter는 방 프리팹의 `RoomImage`와 `Icon` RectTransform·색을 유지하고 현재/비현재 배경과 공개 아이콘만 바꾼다. 현재 방 배경은 `BlackandWhiteUI_16`, 나머지는 `BlackandWhiteUI_3`이며, 방문 전은 interrogation, 방문 뒤에는 flag/skull/ring/heart/chest/door 아이콘을 방 종류에 따라 사용하고 보스 전실은 아이콘을 숨긴다. 연결 sprite와 기본 계층은 연결 프리팹에서, 노드 크기·연결 두께와 최대 간격은 `PrototypeDungeonMinimapView` Inspector에서 수정한다. 배치 계산은 고정 패널 수치가 아니라 저작한 `Map` 영역의 실제 크기를 사용한다.

### 일시정지

배경, 메뉴, `PAUSED` TMP, 계속·설정 Button과 `SettingsPanel` 전체를 프리팹에서 수정한다. 별도 `ESC - 게임 계속` 안내 문구는 사용하지 않으며 `PrototypePauseView.statusLabel`은 이전 프리팹 호환을 위한 선택 참조다. `PAUSED` TMP의 선택 가능한 `PrototypePauseTitleWave`는 DOTween의 unscaled 단일 phase와 즉시 TMP 메시 갱신으로 보이는 글자를 차례로 올렸다가 원위치시킨다. 컴포넌트 기본 전체 주기는 1초이고 현재 pause 프리팹의 디자이너 저작값은 느린 연출을 위한 2초다. 마지막 글자 뒤 한 글자 간격만 쉰다. pause 중 여섯 글자만 갱신하며 별도 GameObject·material 인스턴스나 frame 반복 할당을 만들지 않는다. 컴포넌트를 비활성화하면 tween과 callback을 정리하고 원래 정점을 복원하므로 정지 제목으로 되돌릴 때 프리팹 계층이나 글자를 나눌 필요가 없다. pause Canvas는 첫 일시정지 때 지연 생성하며 세션과 같은 scene 수명을 가진다. presenter는 Button listener, 선택 상태, 표시 전환과 설정 runtime 연결만 담당한다.

pause 프리팹에는 외부 Sprite를 사용하는 16개 `Image` 슬롯이 직접 직렬화되어 있다. 각 슬롯은 같은 GameObject의 `PrototypeOptionalSpriteFallback`으로 package 부재를 처리하며 legacy role applicator는 사용하지 않는다. package가 있으면 Prefab Mode에서 Sprite를 바로 보고 교체할 수 있고, package가 없으면 기능 Image는 기본 표현을 유지하며 장식 화살표만 숨긴다. package 유무가 pause 입력·설정 기능에 영향을 주면 안 된다.

### 방 안내와 런 결과

폭탄 보상·회복·비밀방의 화면 상단 안내는 각 방의 공유 Canvas 프리팹을 사용한다. presenter는 실제 수집 상태에 맞춰 안내 문구만 바꾸며 Canvas, TMP 또는 RectTransform을 생성하지 않는다. 보상 폭탄·회복 캡슐·비밀 cache의 3D 표현은 UI가 아니므로 각 gameplay presenter의 기존 표현 책임을 유지한다.

완료·실패 화면의 배경, 제목, 사망 원인, 상태 TMP와 두 Button은 `PrototypeRunCompletionCanvas.prefab`에서 저작한다. presenter는 Core 결과에 따라 제목·상태·사망 원인 활성 상태와 제목 상태 색을 갱신하고 Button listener와 기본 선택만 연결한다. 결과가 확정되기 전에는 프리팹을 만들지 않으며 확정 시 한 번만 인스턴스화한다.

## 디자이너 작업 절차

1. Play Mode를 끈다.
2. 위 권위 프리팹 중 수정할 Canvas 또는 재사용 자식 프리팹을 Prefab Mode로 연다.
3. RectTransform, Image, TMP, Button, sprite, material과 장식 자식을 수정한다.
4. `Assets/ThirdParty` Sprite를 직접 넣었다면 같은 Image에 `PrototypeOptionalSpriteFallback`을 추가하고 기능 Image는 유지, 순수 장식은 숨김 정책을 선택한다.
5. root View Inspector에서 필수 참조가 유지됐는지 확인한다.
6. 960×600 Game View에서 HUD 겹침과 픽셀 선명도를 확인한다.
7. gameplay scene을 재생해 실제 값 변화, 동적 개수 변화, 결과 Button과 pause 설정 이동을 확인한다.

`Bomb Swap > UI > Create Missing In-Game UI Prefabs and Wire Scenes`는 누락 프리팹만 기본 형태로 만들고 scene 참조를 복구한다. 이미 존재하는 프리팹의 계층이나 시각값은 다시 생성하거나 덮어쓰지 않는다.

## 검증 계약

- Editor validator는 여덟 Canvas 프리팹과 하트·미니맵 방·연결의 세 재사용 자식 프리팹에 있는 View·필수 참조, Canvas와 자식 프리팹의 조합, pause의 직접 Sprite·per-Image 폴백과 모든 gameplay/playtest scene의 정확한 공유 프리팹 참조를 확인한다.
- PlayMode는 프리팹 인스턴스 생성, 무기·체력 표시, 프리팹 자식 기반 미니맵 snapshot 갱신, 보상·회복·비밀방 안내, 결과 프리팹 참조, pause 열기·설정 이동·닫기를 확인한다.
- 실제 WebGL에서는 960×600 Canvas, 브라우저 축소, 키보드·마우스 포커스, pause 중 unscaled UI 입력과 Console/page error를 확인한다.

## 비목표

- 방 노드나 플레이어 하트처럼 개수가 실제 상태에 따라 달라지는 UI를 모두 scene에 고정 배치하지 않는다. 단, 동적으로 늘어나는 한 단위의 시각 계층은 공용 프리팹이 소유한다.
- 최종 아트, HUD 애니메이션, 로컬라이징, UI Toolkit 전환과 addressable UI 로딩은 이번 계약에 포함하지 않는다.
