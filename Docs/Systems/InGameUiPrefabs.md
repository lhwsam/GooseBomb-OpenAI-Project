# 인게임 UI 프리팹 저작 계약

- 상태: `Accepted`
- 기준일: 2026-08-23
- 대상: 무기 HUD, 체력 HUD, 던전 미니맵, 일시정지 화면
- 관련: [로비와 공통 TMP UI](../Development/LobbySlice.md), [사용자 설정](UserSettingsAndAudio.md), [서드파티 자산](ThirdPartyAssets.md), [최소 미니맵](../Development/MinimalMinimapSlice.md)

## 목적

인게임 UI의 계층과 기본 시각 요소를 C#이 매번 조립하지 않는다. 디자이너는 공유 프리팹을 Prefab Mode에서 직접 수정하고, presenter는 프리팹 인스턴스를 한 번 만든 뒤 확정된 게임 상태를 표시하는 역할만 맡는다.

이 전환은 Canvas를 매 scene에 복제하기 위한 것이 아니다. 각 gameplay scene은 presenter와 공유 프리팹 참조만 저장하며, 실행 중 해당 scene의 presenter가 프리팹 인스턴스를 하나 생성한다. 따라서 한 프리팹 수정이 모든 던전방과 독립 플레이테스트방에 적용된다.

## 권위 프리팹

| UI | 프리팹 | 런타임 소유 |
|---|---|---|
| 무기 슬롯·쿨타임 | `Assets/Game/Content/Resources/UI/PrototypeWeaponHudCanvas.prefab` | `PrototypeWeaponHud` |
| 플레이어·보스 체력·방 토큰 | `Assets/Game/Content/Resources/UI/PrototypeHealthHudCanvas.prefab` | `PrototypeHealthHud` |
| 제한 정보 미니맵 | `Assets/Game/Content/Resources/UI/PrototypeDungeonMinimapCanvas.prefab` | `PrototypeDungeonMinimapPresenter` |
| 일시정지·공통 설정 | `Assets/Game/Content/Resources/UI/PrototypePauseCanvas.prefab` | `PrototypeGameSession`과 `PrototypePausePresenter` |

production scene은 위 에셋을 직렬화 참조한다. `Resources/UI` 경로는 scene 없이 만드는 합성 PlayMode fixture도 같은 프리팹을 불러 검증하기 위한 보조 진입점이며, 런타임 이름·태그·계층 검색에는 사용하지 않는다.

## View와 Presenter 경계

- 프리팹 root의 `Prototype*View`는 Canvas와 presenter가 바꿔야 하는 TMP, Image, Button, Map root를 명시적 직렬화 참조로 가진다.
- 계층 이름은 사람이 읽기 위한 것이며 기능 연결의 권위가 아니다. 이름이나 배치 순서를 바꿔도 View 참조가 유지되면 동작해야 한다.
- presenter는 프리팹 인스턴스의 Text, fill amount, 활성 상태와 정의된 상태 색만 갱신한다. RectTransform 배치, sprite, font, material, outline과 장식 계층은 바꾸지 않는다.
- View root 컴포넌트와 필수 참조를 삭제하거나 비우면 validator와 PlayMode가 실패해야 한다.
- 모든 프리팹은 공통 960×600 `CanvasScaler` 기준을 유지한다. 기본 sorting order는 무기 100, 미니맵 109, 체력 110, pause 250이다.

### 무기 HUD

두 슬롯의 배경, 준비 fill, 정의·쿨타임 TMP와 교체 상태 TMP는 프리팹 참조다. 활성/비활성 슬롯과 준비/냉각 색은 `PrototypeWeaponHudView` Inspector에서 수정한다. 표시 문구와 fill amount는 실제 loadout snapshot이 덮어쓴다.

### 체력 HUD

플레이어 fill·TMP, 보스 panel/fill/TMP, 방 토큰 TMP를 프리팹에서 수정한다. presenter는 체력 비율, 문구와 보스 panel 표시 여부만 갱신하므로 배경 sprite, 색, 위치와 폰트는 프리팹 저작값을 유지한다.

### 미니맵

패널, 제목, 범례와 `Map` RectTransform은 프리팹 저작이다. 현재 run마다 달라지는 방 노드와 연결선만 `Map` 아래에 snapshot 갱신 시 다시 만든다. 노드·연결 sprite, 상태 색, 노드 크기, 연결 두께와 최대 간격은 `PrototypeDungeonMinimapView` Inspector에서 수정한다. 배치 계산은 고정 패널 수치가 아니라 저작한 `Map` 영역의 실제 크기를 사용한다.

### 일시정지

배경, 메뉴, `PAUSED` TMP, 계속·설정 Button과 `SettingsPanel` 전체를 프리팹에서 수정한다. 별도 `ESC - 게임 계속` 안내 문구는 사용하지 않으며 `PrototypePauseView.statusLabel`은 이전 프리팹 호환을 위한 선택 참조다. `PAUSED` TMP의 선택 가능한 `PrototypePauseTitleWave`는 DOTween의 unscaled 단일 phase와 즉시 TMP 메시 갱신으로 보이는 글자를 차례로 올렸다가 원위치시킨다. 컴포넌트 기본 전체 주기는 1초이고 현재 pause 프리팹의 디자이너 저작값은 느린 연출을 위한 2초다. 마지막 글자 뒤 한 글자 간격만 쉰다. pause 중 여섯 글자만 갱신하며 별도 GameObject·material 인스턴스나 frame 반복 할당을 만들지 않는다. 컴포넌트를 비활성화하면 tween과 callback을 정리하고 원래 정점을 복원하므로 정지 제목으로 되돌릴 때 프리팹 계층이나 글자를 나눌 필요가 없다. pause Canvas는 첫 일시정지 때 지연 생성하며 세션과 같은 scene 수명을 가진다. presenter는 Button listener, 선택 상태, 표시 전환과 설정 runtime 연결만 담당한다.

pause 프리팹의 외부 Sprite 슬롯은 `PrototypeOptionalUiSkinApplicator`의 16개 명시적 `Image` 바인딩이다. Git에는 Sprite가 없는 공개 대체 상태를 저장하고, 로컬 package가 있으면 인스턴스 생성 시 role profile을 한 번 적용한다. package 유무가 pause 입력·설정 기능에 영향을 주면 안 된다.

## 디자이너 작업 절차

1. Play Mode를 끈다.
2. 위 네 프리팹 중 하나를 Prefab Mode로 연다.
3. RectTransform, Image, TMP, Button, sprite, material과 장식 자식을 수정한다.
4. root View Inspector에서 필수 참조가 유지됐는지 확인한다.
5. 960×600 Game View에서 HUD 겹침과 픽셀 선명도를 확인한다.
6. gameplay scene을 재생해 실제 값 변화와 pause 설정 이동을 확인한다.

`Bomb Swap > UI > Create Missing In-Game UI Prefabs and Wire Scenes`는 누락 프리팹만 기본 형태로 만들고 scene 참조를 복구한다. 이미 존재하는 프리팹의 계층이나 시각값은 다시 생성하거나 덮어쓰지 않는다.

## 검증 계약

- Editor validator는 네 프리팹의 View·필수 참조, pause의 공개 대체 Sprite 바인딩과 모든 gameplay/playtest scene의 정확한 공유 프리팹 참조를 확인한다.
- PlayMode는 프리팹 인스턴스 생성, 무기·체력 표시, 미니맵 snapshot 갱신, pause 열기·설정 이동·닫기를 확인한다.
- 실제 WebGL에서는 960×600 Canvas, 브라우저 축소, 키보드·마우스 포커스, pause 중 unscaled UI 입력과 Console/page error를 확인한다.

## 비목표

- 방 노드나 폭발처럼 개수가 실제 상태에 따라 달라지는 시각 오브젝트를 모두 scene에 고정 배치하지 않는다.
- 최종 아트, HUD 애니메이션, 로컬라이징, UI Toolkit 전환과 addressable UI 로딩은 이번 계약에 포함하지 않는다.
