# 한 층 완료와 재시작

- 상태: 한 층 완료 판정과 즉시 재시작 `Accepted`, 다층 진행·사망 재시작 `Deferred`
- 설계 원본: `GDD_v0.2.md` 19~21장, `ProtoType_v0.2.md` 가설 E
- 코드 소유: 완료 상태는 `PrototypeDungeonRunSession`, 씬 수명과 초기화는 `PrototypeDungeonRunHost`, 결과 UI와 입력 소비는 `PrototypeRunCompletionPresenter`

## 목적

현재 프로토타입의 한 층 주 경로를 보스 격파까지 플레이한 뒤 명확한 종료 피드백을 제공하고, 브라우저 페이지를 새로 고치지 않아도 같은 seed의 초기 상태로 곧바로 다시 플레이할 수 있게 한다. 이 흐름은 여러 층 메타 진행이나 저장 시스템을 미리 만들지 않는다.

## 플레이어 계약

- 보스방 진입만으로 런이 끝나지 않는다.
- 현재 방이 보스방이고 그 보스방의 Core 클리어 상태가 기록된 때에만 한 층이 완료된다.
- 완료 시 전투 simulation을 멈추고 `FLOOR CLEARED`와 재시작 안내를 표시한다.
- 키보드 `R` 또는 게임패드 Select를 한 번 누르면 같은 브라우저 페이지에서 새 런을 시작한다.
- 재시작은 같은 저작 seed를 다시 사용하므로 그래프와 방 배정은 재현되지만 방문·클리어·보상 선택·두 번째 폭탄은 모두 초기 상태다.
- 완료 전의 `RestartRun` 명령은 게임 상태를 바꾸지 않는다.

## 책임과 상태 전이

1. `PrototypeGameSession`이 보스 사망 뒤 `RoomCleared`를 한 번 발행한다.
2. `PrototypeDungeonRoomBinder`가 현재 보스 노드의 Core 클리어를 `PrototypeDungeonRunSession`에 기록한다.
3. `PrototypeRunCompletionPresenter`는 같은 frame의 구독 순서에 의존하지 않도록 다음 `LateUpdate`에서 방 로컬 클리어와 run 완료를 함께 확인한다.
4. 완료 UI를 한 번 만들고 방 로컬 `PrototypeGameSession`을 비활성화해 이동·보스·폭탄 규칙 진행을 멈춘다. InputReader와 persistent run host는 계속 살아 있다.
5. `RestartRun`을 받으면 presenter가 중복 요청을 잠그고 `PrototypeDungeonRunHost.RestartCompletedRun()`을 호출한다.
6. host는 pending 전환이 없고 기존 run이 완료됐는지 확인한 뒤, 같은 seed와 검증된 세 catalog로 새 run session·navigator를 만든다.
7. 시작 씬의 로드 가능성을 먼저 확인하고 navigator를 교체한 뒤 `DungeonStart`를 단일 로드한다. 로드 호출이 실패하면 이전 navigator를 복구한다.
8. 새 씬의 중복 bootstrap은 기존 primary host를 발견하고 제거되며, 새 room binder는 초기 run state와 시작 폭탄 한 종류를 주입한다.

## 불변식

- 완료의 권위 원본은 UI 가시성이나 보스 Transform이 아니라 `현재 노드 == BossRoomId && BossRoomId가 cleared`인 run 논리 상태다.
- 보스방에 도착했지만 보스가 살아 있으면 완료가 아니다.
- 완료 UI와 재시작 요청은 한 번만 발생한다.
- pending 씬 전환 중이거나 미완료 run은 재시작할 수 없다.
- 재시작은 기존 `DungeonRunState` 또는 `DungeonBombLoadoutState`를 재사용하거나 부분 초기화하지 않는다.
- 페이지 reload, 전역 mutable singleton, 별도 스레드나 동기 대기를 사용하지 않는다.
- 결과 UI는 규칙을 판정하지 않고 완료 snapshot을 표현하며 입력을 host 명령으로 전달한다.

## 저작과 검증

- 여덟 던전 씬 모두 Systems 오브젝트에 `PrototypeRunCompletionPresenter` 한 개를 가지며 같은 씬의 room binder와 input reader를 참조한다.
- presenter는 어느 방에도 존재할 수 있지만 `RoomType.Boss`의 실제 완료에서만 UI를 표시한다.
- Input Actions의 `Gameplay/RestartRun`은 Button이며 `<Keyboard>/r`, `<Gamepad>/select` binding을 가진다.
- Editor builder가 기존 Input Actions와 여덟 씬을 마이그레이션하고 validator가 action·binding·컴포넌트 수·참조를 검사한다.
- EditMode는 `RestartRun` 명령 값을, PlayMode는 Input System 변환·보스 클리어 전후 완료 상태·새 run과 시작 씬 로드를 검증한다.
- Development WebGL smoke는 실제 보스 격파 뒤 `run-completed`를 관찰하고 완료 화면을 캡처한 다음 `R`로 `run-restart-requested → dungeon-run-restarted → dungeon-room-ready-1-start-safe`를 확인한다.

## 범위 밖

- 플레이어 사망 결과 화면과 재시작.
- 다음 층 생성, seed 변경 정책, 메타 성장과 저장/불러오기.
- 완료 통계, 점수, 플레이 시간, 보상 요약.
- 완성된 UI 아트·애니메이션·오디오와 게임패드 실기 수동 검증.

## 관련 문서

- `DungeonGeneration.md`
- `InputAndCommands.md`
- `../Architecture/RuntimeFlow.md`
- `../WebGL/BrowserTestMatrix.md`
