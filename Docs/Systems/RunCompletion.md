# 런 결과, 재시작과 로비 복귀

- 상태: 한 층 완료·플레이어 사망 결과, 즉시 재시작과 로비 복귀 `Accepted`, 다층 진행 `Deferred`
- 설계 원본: `GDD_v0.2.md` 19~21장, `ProtoType_v0.2.md` 가설 E
- 코드 소유: 결과 상태는 Core `DungeonRunState`, 방 사건 연결은 `PrototypeDungeonRoomBinder`, 씬 수명과 초기화는 `PrototypeDungeonRunHost`, 결과 UI와 입력 소비는 `PrototypeRunCompletionPresenter`

## 목적

현재 프로토타입의 한 층 주 경로가 보스 격파 또는 플레이어 사망으로 끝났음을 명확히 알리고, 브라우저 페이지를 새로 고치지 않아도 같은 seed의 초기 상태로 즉시 다시 플레이할 수 있게 한다. 여러 층 메타 진행이나 저장 시스템은 미리 만들지 않는다.

## 플레이어 계약

- 보스방 진입만으로 런이 끝나지 않는다. 보스방의 Core 클리어가 확정되면 `FLOOR CLEARED`를 표시한다.
- 어느 방에서든 플레이어 체력이 0이 되면 런이 실패하고 `RUN FAILED`와 사망 원인을 표시한다. 현재 원인 문구는 `BOMB EXPLOSION`, `CHASER CONTACT`, `CHARGER CHARGE`, `ARMORED ENEMY CONTACT`, 일반 `ENEMY CONTACT`, `BOSS ATTACK`이다.
- 결과가 확정되면 현재 방 simulation을 멈춘다.
- 키보드 `R` 또는 게임패드 Select를 한 번 누르면 같은 브라우저 페이지에서 새 런을 시작한다.
- 결과 화면의 `다시 시작`은 같은 즉시 재시작을 수행하고, `로비로 돌아가기`는 현재 run host를 제거한 뒤 `DungeonLobby`로 이동한다.
- 재시작은 같은 저작 seed를 사용하므로 그래프와 방 배정은 재현되지만 방문·클리어·방 보상 토큰·Secret 연결 공개·Recovery/Secret 보상 소비·보상 선택·두 번째 폭탄은 초기 상태이고, 새 `DungeonPlayerHealthState`는 검증된 최대 체력으로 시작한다.
- 완료 또는 실패 전의 `RestartRun` 명령은 게임 상태를 바꾸지 않는다.

## 책임과 상태 전이

1. Core `DungeonRunState`가 `InProgress → Completed | Failed` 단방향 결과를 소유한다.
2. `PrototypeGameSession`이 치명 피해에서 `PlayerDied`, 보스 사망에서 `RoomCleared`를 각각 한 번 발행한다.
3. `PrototypeDungeonRoomBinder`가 `PlayerDied`의 정확한 치명 `PlayerDamageResult`를 run 실패와 `FailureDamage` snapshot으로, `RoomCleared`를 현재 노드 클리어로 기록한다.
4. 현재 simulation 사건 순서는 `PlayerDied`가 `RoomCleared`보다 앞선다. 같은 frame에 플레이어와 보스가 함께 죽으면 먼저 기록된 `Failed`가 유지되고 뒤의 클리어 요청은 terminal 상태로 거부된다.
5. `PrototypeRunCompletionPresenter`는 구독 순서에 의존하지 않도록 다음 `LateUpdate`에서 Core run 결과를 읽는다. 완료는 보스방의 로컬 클리어도 함께 확인하고, 실패는 어느 방에서든 표시한다. 실패 원인은 Transform이나 Collider가 아니라 `FailureDamage.SourceKind`와 프로토타입 적 `ActorId(2~4)`만으로 표시 문구를 선택한다.
6. presenter는 공유 `PrototypeRunCompletionCanvas.prefab`을 한 번 인스턴스화하고 방 로컬 `PrototypeGameSession`을 비활성화한다. InputReader와 persistent run host는 계속 살아 있다.
7. `RestartRun`을 받으면 presenter가 중복 요청을 잠그고 `PrototypeDungeonRunHost.RestartFinishedRun()`을 호출한다.
8. host는 pending 전환이 없고 기존 run이 terminal인지 확인한 뒤 같은 seed, 검증된 세 catalog와 player-vitals 데이터로 새 run session·navigator를 만든다.
9. 시작 씬의 로드 가능성을 먼저 확인하고 navigator를 교체한 뒤 `DungeonStart`를 단일 로드한다. 로드 호출이 실패하면 이전 navigator를 복구한다.
10. 새 씬의 중복 bootstrap은 기존 primary host를 발견하고 제거되며, 새 room binder는 토큰 0의 새 run state와 시작 폭탄 한 종류를 주입한다.
11. 로비 복귀는 terminal·pending 없음·씬 로드 가능성을 확인하고 `DungeonLobby`를 단일 로드한 뒤 persistent host를 제거한다. 로비에는 대체 host가 없으며 다음 `게임 시작`이 새 run을 만든다.

## 불변식

- 결과의 권위 원본은 UI 가시성, 플레이어·보스 Transform 또는 씬 이름이 아니라 Core `DungeonRunOutcome`이다.
- 결과는 `InProgress`, `Completed`, `Failed` 중 하나이며 terminal 결과는 다른 결과로 바뀌지 않는다.
- 보스방 클리어만 `Completed`를 만들고, `PlayerDied`만 `Failed`를 만든다.
- `Failed` 전이는 적용된 치명 피해 결과를 반드시 보존한다. 치명적이지 않은 피해로 실패를 요청하면 기존 run 상태를 바꾸지 않고 거부한다.
- terminal run은 방 이동과 추가 방 클리어를 거부하며 연결 문 snapshot은 잠김으로 보인다.
- 결과 UI와 재시작 요청은 한 번만 발생한다.
- pending 씬 전환 중이거나 진행 중인 run은 재시작할 수 없다.
- pending 씬 전환 중이거나 진행 중인 run은 로비로 나갈 수 없다.
- 재시작은 기존 `DungeonRunState` 또는 `DungeonBombLoadoutState`를 재사용하거나 부분 초기화하지 않으며 방 보상 토큰과 모든 Secret 연결·cache 상태는 새 상태에서 0·미공개·미소비로 시작한다.
- 재시작은 기존 `DungeonPlayerHealthState`도 재사용하지 않으며 새 상태는 최대 체력이다.
- 페이지 reload, 전역 mutable singleton, 별도 스레드나 동기 대기를 사용하지 않는다.
- 결과 UI는 규칙을 판정하지 않고 Core 결과 snapshot을 표현하며 입력을 host 명령으로 전달한다.
- 결과 Canvas·TMP·Button 계층과 기본 배치는 공유 프리팹이 소유한다. presenter는 런타임에 UI 계층을 조립하지 않는다.

## 저작과 검증

- 11개 던전·TestSandbox 씬 모두 Systems 오브젝트에 `PrototypeRunCompletionPresenter` 한 개를 가지며 같은 씬의 room binder와 input reader를 참조한다.
- presenter는 보스방 완료 또는 어느 방에서든 실패했을 때만 UI를 표시한다.
- Input Actions의 `Gameplay/RestartRun`은 Button이며 `<Keyboard>/r`, `<Gamepad>/select` binding을 가진다.
- Editor builder와 validator는 action·binding·컴포넌트 수, 공유 결과 프리팹의 View 참조와 모든 해당 scene의 정확한 프리팹 연결을 검사한다.
- EditMode는 결과 단방향 전이, terminal 이동·클리어 거부와 사망 우선 순서를 검증한다.
- PlayMode는 session 위임, 치명 피해 snapshot 보존, source와 고정 적 ID의 사망 원인 매핑, 완료·실패 상태의 새 run 재시작과 terminal host 제거→로비→새 run 수명을 검증한다.
- Development WebGL smoke는 실제 보스 격파 뒤 결과 UI로 로비에 복귀해 다시 시작한다. 이어 안전방 자기 폭발 5회로 `player-died → run-failed → run-failed-cause-bomb-explosion`을 관찰하고, `CAUSE: BOMB EXPLOSION` 실패 화면을 캡처한 뒤 `R`로 새 시작방까지 확인한다.

## 범위 밖

- 다음 층 생성, seed 변경 정책, 메타 성장과 저장/불러오기.
- 부활, 체크포인트, 자동 재시작과 공격 이름·방·시간을 포함한 상세 사망 타임라인.
- 완료·실패 통계, 점수, 플레이 시간, 보상 요약.
- 완성된 UI 아트·애니메이션·오디오와 게임패드 실기 수동 검증.

## 관련 문서

- `DungeonGeneration.md`
- `DamageAndInvulnerability.md`
- `InputAndCommands.md`
- `../Architecture/RuntimeFlow.md`
- `../WebGL/BrowserTestMatrix.md`
