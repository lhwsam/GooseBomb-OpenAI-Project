# 로비와 공통 TMP UI 수직 슬라이스

- 상태: `Accepted`
- 기준일: 2026-08-20
- 관련: [런 결과와 재시작](../Systems/RunCompletion.md), [런타임 흐름](../Architecture/RuntimeFlow.md), [ADR-0008](../ADR/0008-Dungeon-Scene-Lifetime.md)

## 목적

WebGL을 열자마자 던전 simulation을 시작하지 않고 게임 이름과 시작 의도를 먼저 보여 준다. 로비는 런 상태를 소유하지 않으며, 플레이어가 시작을 확정했을 때만 기존 `DungeonStart` bootstrap이 새 seed-0 런을 만든다.

## 플레이어 계약

- 첫 enabled 씬은 `DungeonLobby`이며 제목은 **폭탄을 낳는 거위**다.
- `게임 시작`은 새 `DungeonStart` 런을 시작한다. 중복 제출은 첫 요청 뒤 잠긴다.
- `조작 방법`은 키보드·게임패드 조작과 폭탄 유도 목표를 같은 화면에서 보여 주며 `돌아가기`로 닫는다.
- 완료·실패 결과 화면은 `다시 시작`과 `로비로 돌아가기`를 별도 선택지로 제공한다. 기존 `R`·게임패드 Select 즉시 재시작은 유지한다.
- 로비 복귀는 terminal run에서만 허용하며 기존 persistent host를 파기한다. 로비에서 다시 시작하면 방문·체력·로드아웃·토큰을 재사용하지 않는 새 런이다.

## UI와 폰트 계약

- `Assets/Game`의 모든 first-party 런타임 문자는 `TextMeshProUGUI`다. vendor 자산과 Unity 패키지는 이 마이그레이션 범위가 아니다.
- 기본 폰트와 로비의 씬 배치 문자 및 다른 런타임 생성 문자의 명시적 폰트는 `DungGeunMo SDF`다.
- `TMP Settings`의 default font도 DungGeunMo로 고정해 새 TMP UI의 누락을 줄이고, 공통 `PrototypeUiFactory`는 다른 기본 폰트나 미설정을 즉시 거부한다.
- 로비의 초기 Canvas 계층은 공통 factory로 한 번 저작해 씬에 저장한다. HUD·미니맵·보상·회복·비밀방·pause·결과 화면은 기존 런타임 생성 경계를 유지한다.
- 로비는 화면 크기 변화에 `CanvasScaler.ScaleWithScreenSize`로 대응하며 키보드, 게임패드 UI Submit, 마우스 클릭을 받는다.

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
- `LobbyCanvas`, `LobbyEventSystem`, 메뉴·조작 패널과 모든 TMP·Button은 `DungeonLobby` 씬에 저작한다. `PrototypeLobbyPresenter`는 직렬화 참조를 검증하고 버튼 listener와 씬 전환만 담당하며 UI 오브젝트를 생성하지 않는다.
- 디자이너는 Play Mode를 끈 상태에서 `DungeonLobby` 씬의 `LobbyCanvas` 아래 TMP, Image, Button, RectTransform을 직접 수정한다. 제목·필수 참조·DungGeunMo 폰트·비활성 조작 패널 계약은 유지한다.
- Editor builder는 씬이 없거나 구 런타임 생성형 presenter에 씬 참조가 없을 때만 기본 UI를 마이그레이션한다. 직렬화 참조가 완성된 뒤에는 정상적인 디자이너 수정 내용을 재생성으로 덮어쓰지 않는다.

## 검증

- Editor validator는 DungGeunMo 글리프, TMP 기본 폰트, 로비 컴포넌트 배제/필수 수, 첫 Build Settings 순서를 검사한다.
- PlayMode는 terminal host 제거→씬 배치 로비 표시→모든 문자의 DungGeunMo 사용→조작 패널 토글→새 시작방 런 생성을 한 흐름으로 검증한다.
- 기본·방향성·가상 게임패드 WebGL smoke는 `lobby-ready → lobby-start-requested` 뒤 기존 던전 검증을 시작한다.
- 기본 WebGL smoke는 보스 완료 결과에서 로비로 복귀한 뒤 다시 시작해 페이지 reload 없는 전체 수명 왕복을 검사하고 로비 screenshot을 남긴다.

## 범위 밖

- 설정, 세이브 슬롯, 언어 선택, 계정, 온라인 기능, 메타 성장, 완성된 로비 아트·애니메이션·음악.
- DungGeunMo 이외 대체 폰트와 다국어 fallback 정책. 실제 로컬라이징을 시작할 때 별도 결정한다.
