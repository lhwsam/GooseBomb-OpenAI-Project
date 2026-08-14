# ADR-0008: 던전 run 상태와 방 씬 수명 분리

- 상태: `Accepted`
- 날짜: 2026-08-14
- 관련: [런타임 흐름](../Architecture/RuntimeFlow.md), [던전 생성](../Systems/DungeonGeneration.md), [던전 문 상태 계약](../Development/DungeonDoorStateSlice.md)

## 맥락

한 층의 현재 방·방문·클리어·콘텐츠 배정은 씬을 왕복해도 유지되어야 한다. 반면 `PrototypeGameSession`, 입력 reader, 논리 전투 격자, 카메라와 presenter는 방마다 새로 만들어야 한다. 기존 `PrototypeRoomAdvanceController`처럼 클리어 직후 고정 씬을 로드하면 분기, 되돌아가기와 잠긴 문을 표현할 수 없다.

Core 이동을 `SceneManager.LoadScene`보다 먼저 확정하면 대상 씬 이름·Build Settings·로드 요청이 잘못됐을 때 논리 현재 방과 화면이 달라진다. 반대로 새 씬의 모든 `Awake` 뒤에 입장 위치를 결정하면 플레이어와 격자가 잘못된 spawn으로 초기화된다.

## 결정

- `PrototypeDungeonRunHost` 한 개가 `PrototypeDungeonRunSession`과 전환 중 상태만 소유하고 자신의 전용 GameObject만 `DontDestroyOnLoad`로 유지한다. run session에는 그래프 탐색·클리어뿐 아니라 첫 폭탄 보상 선택처럼 방을 넘어 유지되어야 하는 run loadout 상태가 포함된다.
- host는 전역 static singleton이나 Service Locator API를 제공하지 않는다. 각 방 binder는 씬 로드 초기에 활성 host를 검색하고 정확히 하나인지 검증한다.
- 모든 던전 씬은 동일한 bootstrap component를 포함한다. 먼저 생성된 host만 지속되고 이후 씬의 중복 bootstrap GameObject는 `Awake`에서 제거한다.
- `PrototypeGameSession`, 입력, Core 전투 격자, 플레이어·적·폭탄 표현, 카메라와 문 presenter는 방 씬 소유이며 지속시키지 않는다.
- 이동 요청은 현재 `Open` 문인지, 대상 노드와 콘텐츠 씬이 존재하는지, Build Settings에서 로드 가능한지를 먼저 검증한다.
- 검증에 성공하면 host는 `from`, `target`, 방향, 대상 씬과 입장 방향을 pending 전환으로 기록하고 `LoadSceneMode.Single` 로드를 요청한다. 이 시점에는 `DungeonRunState`를 아직 변경하지 않는다.
- 대상 씬 binder는 session보다 빠른 실행 순서의 `Awake`에서 pending 입장 방향과 회전된 room 정의를 읽어 플레이어 spawn과 런타임 room 구성을 준비한다.
- `SceneManager.sceneLoaded`에서 실제 씬 이름과 pending 대상이 일치한 뒤에만 같은 방향으로 Core `TryTravel`을 호출한다. 성공하면 pending을 비우고 새 room binder에 문 상태 갱신을 요청한다.
- 동기 로드 요청이 거부되거나 예상과 다른 씬이 로드되면 Core 현재 방을 변경하지 않고 명시적 오류로 중단한다. 한 번에 하나의 pending 전환만 허용한다.
- 첫 enabled 씬은 `Start` placeholder이며 새 host의 Core 시작방과 일치한다. 시작방·폭탄 보상방·보스 전실·보스방을 자동으로 건너뛰지 않는다.
- 브라우저 새로고침과 앱 재시작을 넘는 저장은 이 프로토타입 host의 책임이 아니다.

## 결과

- 방 왕복 중 방문·클리어·결정적 콘텐츠 배정과 첫 폭탄 보상 loadout이 유지된다.
- 방 로컬 전투 상태와 표현이 이전 씬 참조를 붙잡지 않는다.
- 입장 spawn은 전투 simulation 생성 전에 정해지고, 실패한 로드 요청은 Core 진행을 앞당기지 않는다.
- 실제 씬 로드는 Unity Runtime에 남지만 이동 가능 여부와 방문·클리어는 계속 Core가 소유한다.
- 모든 던전 씬에 bootstrap과 binder 참조를 저작·검증해야 한다.
- 프로토타입은 `LoadSceneMode.Single` 전환 비용을 수용한다. 로딩 화면, 비동기 preload와 additive streaming은 실제 측정 뒤 별도 결정한다.

## 대안

- 모든 방을 additive로 상주시킨다: 왕복은 빠르지만 WebGL 메모리와 중복 시스템 관리 비용이 현재 프로토타입에 과하다.
- static mutable run singleton을 둔다: 접근은 간단하지만 테스트 격리, 도메인 리로드와 수명 소유자가 불명확해진다.
- Core 이동 뒤 씬을 로드하고 실패 시 되돌린다: 방문·직전 방까지 되감아야 하며 부분 변경 위험이 있다.
- 특수방을 자동 통과한다: 구현은 작지만 GDD의 보상 선택, 전실과 탐색·되돌아가기 가설을 검증할 수 없다.

## 롤백

지속 host, 방 binder, special-room catalog와 scene authoring을 함께 제거하고 기존 선형 `PrototypeRoomAdvanceController`를 다시 활성화한다. Core 그래프·탐색·문 상태 계약은 독립적이므로 유지할 수 있다.
