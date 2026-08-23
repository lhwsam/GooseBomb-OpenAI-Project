# 로비와 공통 TMP UI 수직 슬라이스

- 상태: `Accepted`
- 기준일: 2026-08-22
- 관련: [런 결과와 재시작](../Systems/RunCompletion.md), [픽셀 폰트 렌더링](../Systems/PixelFontRendering.md), [서드파티 자산](../Systems/ThirdPartyAssets.md), [런타임 흐름](../Architecture/RuntimeFlow.md), [ADR-0008](../ADR/0008-Dungeon-Scene-Lifetime.md)

## 목적

WebGL을 열자마자 던전 simulation을 시작하지 않고 게임 이름과 시작 의도를 먼저 보여 준다. 로비는 런 상태를 소유하지 않으며, 플레이어가 시작을 확정했을 때만 기존 `DungeonStart` bootstrap이 새 seed-0 런을 만든다.

## 플레이어 계약

- 첫 enabled 씬은 `DungeonLobby`이며 제목은 **폭탄을 낳는 거위**다.
- `게임 시작`은 새 `DungeonStart` 런을 시작한다. 중복 제출은 첫 요청 뒤 잠긴다.
- 씬에 저작한 `VersionText`는 현재 Player Settings의 `Application.version`을 `v.{version}` 형식으로 표시한다. 버전 문자열의 권위 원본은 UI 문구가 아니라 Player Settings다.
- `조작 방법`은 공통 설정 패널을 열어 키보드 조작과 키 변경, 키 설정 초기화, 음량·화면 흔들림·전체 화면 설정을 제공하며 `돌아가기`로 닫는다. 게임패드 지원은 유지하지만 이 패널에는 표기하지 않는다. 조작 목록은 ScrollRect로 탐색한다.
- 완료·실패 결과 화면은 `다시 시작`과 `로비로 돌아가기`를 별도 선택지로 제공한다. 기존 `R`·게임패드 Select 즉시 재시작은 유지한다.
- 로비 복귀는 terminal run에서만 허용하며 기존 persistent host를 파기한다. 로비에서 다시 시작하면 방문·체력·로드아웃·토큰을 재사용하지 않는 새 런이다.

## UI와 폰트 계약

- `Assets/Game`의 모든 first-party 런타임 문자는 `TextMeshProUGUI`다. vendor 자산과 Unity 패키지는 이 마이그레이션 범위가 아니다.
- 기본 폰트와 런타임 생성 문자의 명시적 폰트는 Raster atlas를 사용하는 `DungGeunMo`다. `DNFBitBitv2` Raster asset은 제목·강조 문구에 선택할 수 있는 지원 폰트다. 둘 다 960×600 네이티브 크기에서 SDF 보간 없이 픽셀 형태를 유지한다.
- `TMP Settings`의 default font는 DungGeunMo로 고정해 새 TMP UI의 누락을 줄인다. 공통 `PrototypeUiFactory`는 기본 폰트가 다르거나 누락되면 즉시 거부하지만, 씬 저작 TMP에는 DungGeunMo와 DNFBitBitv2를 허용한다.
- 외곽선과 그라데이션의 자산·사용·성능 계약은 [픽셀 폰트 렌더링](../Systems/PixelFontRendering.md)을 따른다.
- 로비의 초기 Canvas 계층은 공통 factory로 한 번 저작해 씬에 저장한다. 무기 HUD·체력 HUD·미니맵·pause는 [공유 인게임 UI 프리팹](../Systems/InGameUiPrefabs.md)에서 사람이 직접 저작하고 presenter가 scene 수명에 맞춰 한 번 인스턴스화한다. 보상·회복·비밀방·결과 화면은 기존 런타임 표현 경계를 유지한다.
- 모든 first-party `CanvasScaler`는 `PrototypeUiFactory`의 960×600 공통 기준, `ScaleWithScreenSize`, `MatchWidthOrHeight = 0.5`를 사용한다. 네이티브 WebGL 크기에서는 UI scale이 1이고, 브라우저 표시 축소는 hosting shell이 담당한다.
- 로비는 키보드, 게임패드 UI Submit, 마우스 클릭을 받는다.
- 권한이 있는 개발자가 로컬 package를 Import하면 씬에 저장된 GUID로 `BlackandWhiteUI_117` 등 외부 Sprite가 Edit Mode에서 직접 복구된다. 각 외부 Sprite Image의 `PrototypeOptionalSpriteFallback`은 package가 없는 공개 clone에서 기능 Image를 유지하고 순수 장식만 숨긴다. 설정 panel은 87×77 원본의 디자이너 저작 정수 6배 522×462 `Simple` Image 크기를 유지하며, Sprite 교체와 폴백은 RectTransform·색상·Image 타입을 변경하지 않는다. Unity Game View를 `0.8x`처럼 비정수 배율로 축소한 미리보기는 픽셀 샘플을 다시 보간하므로 선명도 판정은 `1x` 또는 실제 960×600 WebGL canvas에서 한다.
- 로비의 `ControlsPage`는 씬 저작 ScrollRect와 최하단 `ResetButton`을 유지한다. presenter는 이 Button을 명시적 직렬화 참조로 사용해 키보드 override만 초기화하며 이미지·RectTransform·스크롤 content를 런타임에 재배치하지 않는다. `SettingsStatusText`는 제거하고 중복 키는 해당 키 Button 안의 `이미 사용 중` 경고색과 짧은 좌우 흔들림으로 알린다.
- 로비의 비활성 설정 패널을 포함한 모든 Button은 [UI 상호작용 피드백](../Systems/UiInteractionFeedback.md)의 공통 DOTween 컴포넌트를 가진다. 기본 hover/키보드 선택은 `1.06`, 누름은 `0.96`이며 pause 영향 없이 전환한다. 현재 씬은 기존 계층을 보존해 버튼 root를 확대하지만, 디자이너는 hit 영역을 고정해야 하는 버튼에 별도 `Visual` 자식을 지정할 수 있다. 게임 시작·설정 버튼은 각각 지정된 TMP 라벨만 `startColor`에서 `targetColor`로 전환하며 버튼 배경은 변경하지 않는다. 두 버튼은 좌우 화살표를 Inspector 직렬화 참조로 소유하고 hover·키보드 선택·누름에 함께 표시하며 이름 기반 검색은 사용하지 않는다. 최초 로비에서는 키보드·게임패드 Submit 대상을 시작 버튼으로 유지하되 실제 입력 전까지 선택 시각 효과를 숨긴다.
- 로비와 pause는 [사용자 설정 계약](../Systems/UserSettingsAndAudio.md)의 같은 settings presenter를 사용한다. 로비 패널은 씬에 배치하고 pause 패널은 공유 pause 프리팹 안에 저작해 첫 pause 때 인스턴스화한다.

## 씬과 수명

```text
DungeonLobby (run 없음)
  └─ 게임 시작 → DungeonStart (새 persistent run host)
       └─ 방 전환 … → Completed | Failed
            ├─ 다시 시작 → DungeonStart (같은 seed의 새 run)
            └─ 로비로 돌아가기 → host 제거 → DungeonLobby
```

- 로비 씬에는 `PrototypeDungeonRunHost`, `PrototypeDungeonRoomBinder`, `PrototypeGameSession`, `BombSwapInputReader`가 없어야 한다.
- `PrototypeLobbyPresenter` 한 개, MainCamera 한 개, AudioListener 한 개와 최소 한 개 Light를 저작한다.
- 3D 거위·폭탄 placeholder는 로비 분위기를 전달하는 표현이며 규칙 상태가 아니다. 최종 에셋으로 교체할 수 있다.
- `LobbyCanvas`, `LobbyEventSystem`, 메뉴·조작 패널과 모든 TMP·Button은 `DungeonLobby` 씬에 저작한다. `PrototypeLobbyPresenter`는 직렬화 참조를 검증하고 버튼 listener, 버전 표시와 씬 전환만 담당하며 UI 오브젝트를 생성하지 않는다.
- 제목은 하나의 TMP 또는 같은 제목 container 아래의 여러 TMP 조각으로 저작할 수 있다. hierarchy 순서와 공백으로 조합한 결과에는 게임 이름 **폭탄을 낳는 거위**가 포함되어야 하며, 앞뒤의 영문 로고·장식 문구는 허용한다. main menu `StatusLabel`은 선택 사항이며, 디자이너가 배치한 경우에만 시작·실패 상태 문구를 표시한다. 설정 패널 내부 상태 문구는 별도 필수 참조다.
- 디자이너는 Play Mode를 끈 상태에서 `DungeonLobby` 씬의 `LobbyCanvas` 아래 TMP, Image, Button, RectTransform을 직접 수정한다. 제목·필수 참조·지원 Raster 폰트·960×600 CanvasScaler·비활성 조작 패널 계약은 유지한다.
- Editor builder는 씬이 없거나 구 런타임 생성형 presenter에 필수 씬 참조가 없을 때만 기본 UI를 마이그레이션한다. 선택 사항인 main menu 상태 라벨의 삭제는 디자이너 의도로 보존하고, 직렬화 참조가 완성된 뒤에는 정상적인 디자이너 수정 내용을 재생성으로 덮어쓰지 않는다.

## 검증

- Editor validator는 DungGeunMo 글리프와 TMP 기본 폰트, 두 Raster 폰트 스타일 자산, 960×600 공통 CanvasScaler, 로비의 모든 Button 피드백, 로비 컴포넌트 배제/필수 수, 첫 Build Settings 순서를 검사한다.
- PlayMode는 terminal host 제거→씬 배치 로비 표시→공통 CanvasScaler와 모든 문자의 지원 Raster 폰트 사용→조작 패널 토글→새 시작방 런 생성을 한 흐름으로 검증한다.
- 기본·방향성·가상 게임패드 WebGL smoke는 `lobby-ready → lobby-start-requested` 뒤 기존 던전 검증을 시작한다.
- 기본 WebGL smoke는 보스 완료 결과에서 로비로 복귀한 뒤 다시 시작해 페이지 reload 없는 전체 수명 왕복을 검사하고 로비 screenshot을 남긴다.

## 범위 밖

- 해상도·render scale·UI scale 설정, 저장된 런 이어하기/세이브 슬롯, 언어 선택, 계정, 온라인 기능, 메타 성장, 완성된 로비 아트·애니메이션과 BGM 최종 청감 승인·AudioSource 연결. WebGL의 CSS 표시 크기는 hosting shell이 반응형으로 처리하고 설정의 fullscreen은 Unity 요청만 제공한다. 로비 BGM 후보 clip의 계약은 [사용자 설정과 오디오](../Systems/UserSettingsAndAudio.md)를 따른다.
- `DungGeunMo`·`DNFBitBitv2` 이외 대체 폰트와 다국어 fallback 정책. 실제 로컬라이징을 시작할 때 별도 결정한다.
