# 탐색 정보 제한형 최소 미니맵 수직 슬라이스

- 상태: `Implemented` (`Proposed` 정보 공개·시각 정책은 사람 검증 대기)
- 결정 근거: [GDD v0.2](../GameDesign/GDD_v0.2.md) 4.1, 19~20장, [프로토타입 가설 E](../GameDesign/ProtoType_v0.2.md), [PT-20260815-02](../Playtesting/Results/PT-20260815-02.md)
- 소유 계약: [던전 생성](../Systems/DungeonGeneration.md), [런타임 흐름](../Architecture/RuntimeFlow.md), [테스트 매트릭스](../Testing/TestMatrix.md)

## 문제와 목표

방 그래프와 되돌아가기는 동작하지만 현재 위치와 이미 확인한 가지를 기억하기 어려워 탐색보다 길 찾기 피로가 커진다. 우측 상단 미니맵으로 현재 위치와 이미 획득한 공간 정보만 보여 주되, 미탐색 전체 그래프를 공개해 탐색 선택을 없애지 않는다.

GDD는 미니맵 세부 규칙을 정하지 않는다. 따라서 아래 공개 범위와 시각 수치는 플레이 피드백을 검증하기 위한 `Proposed` 계약이다.

## 정보 공개 계약

- 현재 방은 항상 방문 상태이며 `BlackandWhiteUI_16` 배경으로 표시한다.
- 현재 방이 아닌 방은 `BlackandWhiteUI_3` 배경으로 표시한다.
- 방문한 방은 첫 입장부터 방 종류 아이콘을 공개한다. 시작 `icon_flag`, 전투 `icon_skull`, 폭탄 보상 `icon_ring`, 회복 `icon_heart`, Secret `icon_chest`, 보스 `icon_door`를 사용하고 보스 전실은 종류 아이콘을 표시하지 않는다.
- 방문한 방에 직접 연결됐지만 아직 들어가지 않은 방은 종류를 숨기고 `icon_interrogation`으로 표시한다.
- 확인된 연결은 적어도 한쪽 끝 방을 방문했을 때만 표시한다.
- 미방문 방의 종류, 그 방 너머 연결, 보상·회복·보스 여부는 공개하지 않는다.
- X는 화면 오른쪽, Z는 화면 위쪽이다. 실제 북·동·남·서 문과 그래프 방향을 바꾸어 그리지 않는다.
- 미니맵은 탐색 정보를 표현할 뿐 이동, 문 잠금, 방 클리어, fast travel을 소유하지 않는다.

## 상태 소유와 갱신

```text
DungeonGraph + DungeonRunState
        │ CreateMinimapSnapshot()
        │ 현재/방문/확인된 인접 방·연결만 복사
        ▼
PrototypeDungeonMinimapPresenter
        │ scene 진입 commit 시 1회 재구성
        ▼
우측 상단 room/connection UI
```

- Core snapshot은 read-only이며 호출자가 run 상태나 그래프를 바꿀 수 없다.
- Unity presenter는 frame polling을 하지 않는다. 시작방에서는 초기화 때, 방 전환에서는 실제 Core commit 뒤 갱신한다.
- 각 방 scene은 presenter 하나를 가지며 persistent 별도 지도 상태를 만들지 않는다. 새 scene도 같은 run snapshot에서 화면을 재구성한다.
- 완료·실패 뒤 새 run은 시작방과 그 인접 연결만 보이는 초기 지도로 돌아간다.

## 시각 범위

- 기준 UI 해상도 960×600, 우측 상단 토큰 표시 패널 아래에 배치한다.
- 고정 패널 안에서 알려진 방 좌표 범위를 중앙 정렬하고 작은 그래프는 확대하지 않는다.
- 연결선을 먼저 그리고 방을 나중에 그려 교차점과 방 중심을 구분한다.
- 현재/비현재는 서로 다른 배경 sprite로, 미방문/방 종류는 전경 아이콘으로 구분한다. 배경은 현재 위치만 표현하고 아이콘은 공개된 방 종류만 표현한다.
- 마우스 입력, 확대/축소, 전체 지도 화면, 애니메이션과 완성 아트는 이번 범위에 넣지 않는다.

## 실패 테스트와 완료 조건

### EditMode

- 새 run snapshot은 시작방, 시작방의 직접 이웃과 한 연결만 포함한다.
- 이동 뒤 이전·현재 방은 방문, 새 현재 방의 미방문 이웃은 `?`로 추가된다.
- 미방문 이웃 너머 방과 연결은 포함하지 않는다.
- snapshot 방·연결 순서는 안정적이고 반환 컬렉션은 수정할 수 없다.
- X/Z 좌표와 현재/방문/발견 상태가 Core graph·run state와 일치한다.
- 방문한 현재·이전 방 snapshot만 `RoomType`을 포함하고 미방문 frontier는 `RoomType`을 포함하지 않는다.

### PlayMode와 콘텐츠

- 시작방에서 presenter 하나가 현재 방 1과 알려진 방/연결 수를 표시한다.
- 실제 다음 scene 진입 뒤 현재 강조가 새 방으로 바뀌고 이전 방은 방문 상태로 남는다.
- 시작방·방문한 전투방·미방문 보상방이 각각 flag·skull·interrogation 아이콘과 올바른 현재/비현재 배경을 사용한다.
- 열 던전·TestSandbox scene의 presenter·room binder 참조와 단일 개수를 validator가 확인한다.
- 미니맵 패널이 우측 상단 토큰 HUD, 보스 HUD와 겹치지 않는다.

### WebGL와 사람 검증

- seed-0 전체 경로에서 시작 `2방/1연결`, 첫 전투 이후 `3방/2연결`, 회복방 진입 시점 `8방/7연결`, 보스 전실에서 전체 `9방/8연결` 공개를 확인한다.
- browser Console/page error 0과 실제 캡처로 아이콘·연결·현재 방 배경 강조를 확인한다.
- 사람 플레이에서 이전보다 길을 덜 잃는지, 선택 가지를 스스로 발견하는지, `?`가 과도한 스포일러인지 관찰한다.

## 구현과 검증 근거

- Core `DungeonRunState.CreateMinimapSnapshot()`이 현재·방문·직접 인접 방과 확인된 연결만 안정 순서의 read-only snapshot으로 만든다. 현재·방문 방에는 알려진 종류를 포함하지만 미방문 방 종류와 미방문 방 너머 연결은 노출하지 않는다.
- `PrototypeDungeonMinimapPresenter`는 persistent 별도 상태나 frame polling 없이 시작 scene 초기화와 실제 `RoomCommitted` 뒤 Core snapshot을 다시 그린다. 패널과 Map 영역은 공유 Canvas 프리팹이, 방 배경·아이콘과 연결의 시각 계층은 각각 [공유 자식 프리팹](../Systems/InGameUiPrefabs.md)이 소유한다. presenter는 snapshot에 따라 그 프리팹 인스턴스의 개수·위치·배경·아이콘만 갱신하고 저작된 RectTransform·색은 덮어쓰지 않는다. Core X는 화면 오른쪽, Z는 화면 위쪽으로 유지한다.
- Editor builder와 validator가 Build Settings의 던전·TestSandbox 열 scene에 presenter를 하나씩 저작하고 해당 scene의 room binder 참조가 같은지 검증한다.
- 연결된 Unity 6000.5.3f1에서 EditMode 전체 `363/363`과 미니맵 집중 PlayMode `2/2`를 통과했다. 집중 검증은 모든 방 종류의 아이콘 매핑, 보스 전실 아이콘 비표시, 현재/비현재 배경, 실제 시작방에서 전투방으로 이동한 뒤의 방문 공개 전이를 포함한다. PlayMode 전체는 기존 콘텐츠 계약 실패와 그 뒤의 host/scene 상태 연쇄 실패 때문에 `178/191`이며 이번 미니맵 집중 검증과는 분리해 기록한다.
- Development WebGL 연결 빌드는 콘텐츠 validator가 로비 제목, Thrower Animator, 보스 chain/throw fuse와 기존 UI 전반의 private vendor sprite 직접 참조를 차단해 빌드·브라우저 smoke까지 진행하지 못했다. 따라서 WebGL 통과로 판정하지 않는다. 사용자가 저작한 미니맵 배경 sprite와 레이아웃은 이 작업에서 교체하거나 덮어쓰지 않았다.
- 현재 증거: `Artifacts/Verification/ConnectedTests/20260824-120103-512.json`, `Artifacts/Verification/ConnectedTests/20260824-120222-607.json`, `Artifacts/Verification/ConnectedTests/20260824-120638-209.json`, `Artifacts/Verification/20260824-210819-connected-web/`.

## 비목표와 롤백

- 미탐색 전체 그래프·방 종류 공개, 방문한 보스 전실 아이콘, 보상 소비/클리어 overlay, 적 위치, fog animation, fast travel, 저장/불러오기.
- 롤백 단위는 Core minimap snapshot, Unity presenter, scene component, validator·테스트·WebGL marker와 이 문서다. 던전 생성이나 실제 이동 상태는 롤백 대상이 아니다.
