# 현재 프로젝트 상태

- 기준일: 2026-08-14
- 단계: 결정론적 던전 Core와 실제 Start→전투방 WebGL 수직 슬라이스 완료, 전투 클리어 뒤 보상방·왕복 탐색 확장 전
- Unity: `ProjectSettings/ProjectVersion.txt` 기준 6000.5.3f1
- 목표 플랫폼: 3D WebGL

이 파일은 현재 스냅샷이다. 과거 작업 일지를 누적하지 않는다.

## 완료

- GDD v0.2와 프로토타입 검증 부록 v0.2 작성.
- 프로젝트 Unity 버전, 패키지, 렌더 파이프라인, 입력, Build Settings, WebGL 기본 설정 조사.
- 루트 `AGENTS.md`와 문서 권위/세션 시작 규칙 정의.
- Architecture, Systems, ADR, Development, Testing, WebGL, AI, Migrations 문서 뼈대 생성.
- `BombSwap.Core`, `BombSwap.Unity`, `BombSwap.Editor`, EditMode/PlayMode test asmdef 경계 구성.
- first-party `Assets/Game` 책임 폴더 구성.
- 프로젝트 전용 스킬 4종 구현: gameplay change, content authoring, WebGL verify, playtest review.
- `Tools/Verify.ps1` 기반 StaticOnly/Fast/Full/Web 검증 하네스와 구조화된 산출물 구현.
- Unity command-line Editor validator/WebGL build 도구와 EditMode/PlayMode 하네스 smoke test 구현.
- Playwright 기반 WebGL 정적 서버·canvas/input/console/gameplay probe 스모크 구현.
- 자동 스모크와 수동 관찰 세션이 공유하는 loopback WebGL 정적 서버, 수동 실행 CLI와 MIME·압축·경로 보안 회귀 테스트 구현.
- `GridPosition`, `GridState`, `IGameClock`, `ManualGameClock` 최소 Core 계약 구현.
- 논리 좌표, 지형·점유 불변식, 수동 시계 계약을 검증하는 EditMode 테스트 구현.
- 기본 십자 폭탄 정의, 설치, fuse, 폭발 셀, 벽 차단·파괴, 지연 연쇄를 소유하는 `BombSimulation` 구현.
- 동일 시각 폭발과 큰 시계 진행에서도 결정론적인 폭탄 사건 순서 구현.
- 정수 XZ 논리 격자와 Unity 3D 셀 중심을 변환하는 `GridSpace` 구현.
- 공식 Unity MCP EditMode/PlayMode 실행 결과를 실행 요청 수명·도메인 리로드와 분리해 Console에서 확인하는 테스트 전용 리포터 구현.
- 게임 전용 `Gameplay/Move·PlaceBomb·SwapBomb·Pause` Input Actions와 Keyboard/Gamepad control scheme 구현.
- 장치 입력을 Core `PlayerCommand`로 변환하고, 세션 이동 계산 직전 최신 Move 값을 frame 단위로 재확인하며, focus 상실 시 이동을 해제하는 `BombSwapInputReader` 구현.
- 11×9 격자, 경계 벽, 내부 장애물, 플레이어 placeholder, 탑다운 카메라를 가진 `TestSandbox` 씬 구현.
- Input Actions·TestSandbox·Build Settings를 재생성/검증하는 Editor builder와 validator 구현.
- 개발 WebGL에서 입력 사건을 브라우저 smoke에 전달하는 제한된 harness probe 구현.
- 주입 시계의 frame 경과 시간, Core 연속 `GridSubcellPosition`, 셀 경계의 원자적 actor 점유 전이, 벽·폭탄 차단을 소유하는 `PlayerMovementSimulation` 구현.
- TestSandbox 유지 입력을 기본 5 cells/s 연속 논리 이동과 같은 frame의 placeholder Transform 직접 표시에 연결. 변하지 않은 대각선 입력은 마지막 전환 축을 유지해 frame별 재샘플링에서도 방향이 교번하지 않는다.
- `PrototypeGameSession`이 하나의 논리 격자·수동 시계를 이동과 폭탄 simulation에 공유하도록 런타임 상태 소유를 통합.
- 검증된 `PrototypeBombDefinitionAsset`과 기본 십자 폭탄/폭발 셀 prefab을 만들고 TestSandbox `Z` 입력을 실제 Core 설치·fuse·폭발에 연결.
- `PrototypeBombPresenter`가 설치 폭탄과 영향 셀 placeholder를 풀링해 3D로 표현하도록 연결.
- 고유 `ActorId`와 양방향 actor 위치 색인을 도입하고, 설치자에게만 `ActorId`·`BombId`·설치 셀 기반의 한 번 탈출 권한을 부여해 셀 이탈 후 재진입을 차단.
- 주입 시계 기반 `PlayerHealthSimulation`과 최대 체력 5·무적 0.75초의 검증된 `PrototypePlayerVitalsAsset`을 구현.
- 폭발 영향 셀과 현재 플레이어 논리 셀을 비교해 자기 폭발 피해 1을 적용하고, 같은 폭발 중복·무적 중 별도 폭발·사망 뒤 명령을 차단.
- `PrototypePlayerHealthPresenter`가 공유 material을 복제하지 않고 피격 pulse와 사망 색을 표시하도록 TestSandbox에 연결.
- 안정 `EnemyDefinitionId`, 주입 시계 cadence, 결정론적 국소 Manhattan 선택, 두 칸 방향 유지를 소유하는 `ChaserEnemySimulation` 구현.
- 내구도 1, 폭발 ID별 중복 차단과 단일 치명 결과를 소유하는 `EnemyHealthSimulation` 구현.
- 검증된 `PrototypeChaserDefinitionAsset`, collider 없는 chaser prefab, 논리 spawn과 `PrototypeChaserPresenter`를 TestSandbox에 연결.
- 추격자 폭발 사망 시 논리 actor 점유를 한 번 제거하고 `EnemyDied`와 단일 `RoomCleared`를 발행하는 첫 유도→처치 루프 구현.
- cardinal 논리 인접만 추격자 접촉으로 판정하고 접촉 피해 1을 기존 플레이어 체력·0.75초 무적·피격 표현에 연결.
- `PlayerDamageResult`가 폭발 `BombId`와 적 접촉 `ActorId` 원인을 구분하고, 같은 프레임 폭발 사망 적이 접촉 피해를 남기지 않는 처리 순서 구현.
- 안정 `RoomDefinitionId`, 경계 출구, 플레이어/추격자 spawn, 고정 벽, 안전 셀, 퇴로 anchor와 닫힌 유도 순환 경로를 소유하는 `CombatRoomDefinition` 구현.
- `prototype-combat-loop`, `prototype-combat-lanes`, `prototype-combat-pillars` 세 ScriptableObject를 대응 TestSandbox 씬의 격자·고정 벽·spawn 단일 저작 원본으로 연결하고 씬 중복 수치 제거.
- 세 방의 연결성, 서로 다른 첫 이동의 퇴로 2개, 출구 경계, 닫힌 cardinal 유도 경로, 씬 장애물 표현과 Build Settings 순서를 builder·validator·테스트로 검증.
- 방 클리어 뒤 1.25초 realtime 지연으로 중앙 루프→평행 통로→엇갈린 기둥을 단일 로드하고 마지막 방에 머무는 플레이테스트 전용 전환 어댑터 구현.
- 첫 내부 관찰 세션의 고정 3방 build, 비유도 시작 안내, 방별 관찰표, 직후 인터뷰, 유지·변경·제거 후보 기준과 익명 결과 템플릿 정의.
- 첫 사람 플레이 세션 `PT-20260814-01`을 완료하고 방향 전환 지연 관찰, 현재 기반 유지 후보, 폭탄 상호작용 블록 추가 뒤 재검증 조건을 분리해 기록.
- 서로 직교하는 두 방향키가 잠깐 겹칠 때 이전 cardinal 방향 대신 새 방향을 우선하도록 입력 해석을 수정하고 실제 키 겹침·WebGL 브라우저 회귀를 추가.
- 짧은 탭 pending turn 뒤에도 남은 키 해제 후 이동과 빠른 반복 입력 유실을 재현하고, 플레이어 전용 0.2초 step·pending turn·목적 셀 보간을 frame 연속 이동으로 대체.
- `BombWeaponLoadout`이 두 폭탄 정의, 활성 슬롯, 슬롯별 설치 쿨타임과 별도 교체 쿨타임을 주입 시계 기준으로 소유하도록 구현.
- 성공한 설치만 활성 슬롯 쿨타임을 소비하고, 실패한 설치·거부된 교체가 기존 상태를 바꾸지 않으며 비활성 슬롯도 별도 업데이트 없이 회복하는 Core 계약을 구현.
- 기본 `prototype-cross`와 두 슬롯 로드아웃 ScriptableObject, 정의별 bomb/explosion prefab을 Unity Editor builder로 생성하고 세 TestSandbox 씬에 연결.
- `PrototypeWeaponHud`가 Core snapshot을 바탕으로 활성 슬롯, 두 설치 준비 상태, 교체 준비 상태를 표시하고 `PrototypeBombPresenter`가 정의별 풀과 색을 사용하도록 연결.
- 실제 `X` 교체 뒤 `Z`가 선택된 정의를 설치하도록 세션을 연결하고, 성공한 슬롯 변경·정의별 설치를 WebGL harness 사건으로 검증.
- `BombExplosionShape.SquareArea`와 결정론적 영역 resolver를 구현해 원점 포함 3×3 바닥, 영역별 `Void`·고정 벽 제외, 파괴 벽 포함·비차폐, 대각선 지연 연쇄를 Core 규칙으로 고정.
- 빠른 십자 placeholder를 `prototype-area` 광역 폭탄과 보라색 설치체·자홍색 폭발 셀 prefab으로 교체하고 로드아웃·validator·WebGL 성공 사건을 마이그레이션.
- 전투방 스키마에 파괴 가능 셀을 추가하고 초기 연결성·고정 벽/중요 셀 비중첩을 Core 불변식으로 검증.
- 두 번째 방의 대각선 파괴 블록 2개와 세 번째 방 중앙 블록 1개를 논리 `DestructibleWall`, 황갈색 4분할 3D 표현과 확정 파괴 presenter로 연결.
- `ChargerEnemySimulation`의 `Track → Telegraph → Charge → Recover` 결정론적 상태 머신, 예고 방향 잠금, 한 셀 cadence와 벽·폭탄·actor 차단을 구현.
- 선택적 돌진형 spawn을 방 스키마에 추가하고 세 번째 방 `(-3,2)`에 `prototype-charger` 정의·collider 없는 placeholder·상태별 presenter를 연결.
- `PrototypeGameSession`에 추격자 `ActorId(2)` 뒤 돌진형 `ActorId(3)` 고정 이동·피해 순서, 적별 생존 상태와 두 적 사망 뒤 단일 `RoomCleared` 집계를 구현.
- WebGL gameplay probe와 browser smoke가 돌진형의 예고→돌진→논리 이동 순서, 이동 입력 회귀와 차저 접촉 무적에 가려지지 않는 자기 폭발 피해를 검증하도록 확장.
- `ArmoredEnemySimulation`의 `Armored → Broken → Dead` 2회 피격, 폭발 ID 중복 차단, 첫 피격 뒤 1→3 cells/s 상태별 cadence와 즉시 재판단을 구현.
- 선택적 갑옷 적 `ActorId(4)`을 추격자·돌진형 뒤 고정 순서로 공유 격자·폭발·접촉·방 클리어에 연결하고 상태별 property block·scale 표현을 구현.
- 열린 중앙 실험선과 좌우 기둥을 가진 네 번째 `prototype-combat-armor` 방·씬·정의·collider 없는 prefab을 Editor builder로 저작하고 4방 전환·Build Settings를 validator로 고정.
- 연결된 Unity Test Runner의 도메인 리로드 뒤에도 실행 ID와 최종 수치를 JSON으로 보존하는 `ConnectedTestHarness`를 구현.
- 갑옷 전용 WebGL smoke가 첫 실제 폭발의 상태 파괴·빠른 이동과 두 번째 폭발의 사망·방 클리어를 확인하고, 네 번째 씬을 포함한 기본 빌드의 기존 3방 smoke가 입력·폭탄·파괴 블록·돌진형 회귀를 함께 검증하도록 유지.
- 갑옷 첫 피격 가독성, 가속 인지, 두 번째 설치 계획과 반복 노동감을 분리해 관찰하는 고정 WebGL 프로토콜과 익명 기록 템플릿을 준비.
- `prototype-tree-v1` 결정론적 한 층 Core 그래프를 구현해 명시 seed에서 시작→첫 전투→폭탄 보상→주 경로 전투 3개→보스 전실→보스와 선택 전투 가지를 생성.
- 고정 seed 혼합·LCG·상위 비트 범위 변환과 유한 정수 XZ backtracking으로 연결된 트리, 고유 좌표, 연결되지 않은 방의 암시적 cardinal 인접 금지를 보장.
- `DungeonGraph`가 생성 버전, 정의, 안정 ID·노드·연결의 read-only snapshot과 이웃·최단 경로 조회를 소유하고 seed 0 golden snapshot과 512개 seed 회귀를 추가.
- `DungeonRunState`가 시작방부터 현재·직전 방, 방문·클리어 상태를 소유하고 첫 전투·보스방 클리어 전 퇴실 차단, 클리어 방 양방향 재방문을 결정론적으로 처리.
- `DungeonGraph`의 정수 XZ 연결을 `RoomExitDirection`으로 조회해 Unity 씬 이름이나 Transform 없이 방향별 이동을 판정하도록 확장.
- `prototype-combat-assignment-v1` 배정기가 안정 room ID·분리 seed 흐름·사용 횟수 균형으로 모든 전투 노드에 수제 방 정의, 0/90/180/270도 회전과 활성 출구 snapshot을 결정.
- ADR-0007에 따라 네 수제 전투방의 북·동·남·서 중앙 경계 셀을 잠재 출구로 저작하고, 중복 방향과 누락 cardinal 출구를 Core·Editor validator로 거부.
- `PrototypeDungeonCombatRoomCatalog.asset`이 네 전투방 ScriptableObject와 현재 네 TestSandbox 씬 이름을 명시적으로 매핑하고 builder·validator가 누락·중복·순서를 검증.
- `PrototypeDungeonRunSession`이 명시 seed와 카탈로그에서 Core 그래프·배정·탐색 상태를 조합하고 전투 노드의 실제 방 asset·씬·회전·활성 출구를 조회하도록 구현.
- `DungeonRunState`가 북·동·남·서 네 방향을 `Inactive`·`Locked`·`Open`과 대상 방 ID로 제공하고, Unity 런 세션이 전투방 배정과 일치하는 read-only 문 상태 snapshot을 노출하도록 구현.
- `CombatRoomRotationUtility`가 0/90/180/270도에서 방 크기, 모든 spawn·벽·안전/퇴로/유도 셀과 출구 셀·방향을 한 번에 회전하도록 구현.
- `PrototypeGameSession`이 적 비활성 placeholder에서 기존 이동·폭탄·체력을 재사용하고 적 actor 없이 안전방으로 시작하며, 회전된 room 정의와 입장 spawn을 `Awake` 전에만 준비하도록 확장.
- `PrototypeDungeonSpecialRoomCatalogAsset`이 시작·폭탄 보상·보스 전실·보스의 정확한 네 타입과 고유 씬 이름을 검증하고 run session이 모든 그래프 노드를 씬으로 해석하도록 확장.
- `PrototypeDungeonRunNavigator`가 열린 문·대상 콘텐츠·로드 가능성을 검증한 pending 전환을 만들고 기대한 씬 로드 뒤에만 Core 이동을 단일 commit하도록 구현.
- `PrototypeDungeonRunHost`가 run session·navigator만 전용 root에서 지속하고 중복 bootstrap 중 primary 한 개만 남기도록 구현.
- `PrototypeDungeonRoomBinder`가 pending 입장 방향과 전투방 배정 회전을 session `Awake` 전에 적용하고, 논리 출구 셀의 바깥 방향 입력을 graph travel 요청으로 연결하도록 구현.
- `PrototypeDungeonDoorPresenter`가 회전된 그래프 방향을 저작된 네 문으로 역매핑하고 `Inactive`·`Locked`·`Open` 상태를 material property block으로 표시하도록 구현.
- 기존 네 전투방을 선형 `PrototypeRoomAdvanceController`에서 graph binder로 마이그레이션하고, 중앙 문 틈을 가진 8개 분할 외벽·collider 없는 네 문 패널을 Editor builder로 저작.
- 실제 `PrototypeDungeonSpecialRoomCatalog.asset`과 `DungeonStart`·`DungeonReward`·`DungeonBossAnte`·`DungeonBoss` placeholder 씬을 생성하고 Build Settings의 첫 enabled 씬을 `DungeonStart`로 고정.
- 연결된 Editor에서 콘텐츠 validator와 Development WebGL BuildReport를 남기는 `ConnectedWebGLBuildHarness`를 구현하고, Playwright가 안전 시작방 이동→실제 graph scene commit→회전 전투방 입력·폭탄을 검증하도록 smoke를 갱신.

## 현재 저장소 사실

- `BombSwap.Core`에는 UnityEngine 비참조 논리 격자와 주입식 수동 시계가 구현되어 있다.
- `GridState`는 미등록 셀을 `Void`로 취급하고 지형, actor/bomb 점유, `ActorId` 양방향 위치 색인을 소유한다. 점유는 바닥에만 존재하며 actor가 있는 셀에 폭탄을 설치하는 제한된 동시 점유를 허용한다.
- `BombSimulation`은 활성 폭탄, 세션 내 고유 ID, 설치자 ID, fuse와 종류 독립적인 양수 지연 연쇄를 소유하고 읽기 전용 snapshot·폭발 결과에 설치자 ID를 보존한다.
- `BombWeaponLoadout`은 정확히 두 개의 서로 다른 정의와 각 슬롯의 다음 설치 가능 시각, 다음 교체 가능 시각을 소유한다. 쿨타임은 매 frame 감소시키지 않고 `IGameClock.Now`와 종료 시각의 차이로 계산한다.
- `DungeonGenerator`는 모든 `int` seed와 불변 정의에서 `prototype-tree-v1` 그래프를 만든다. 기본 정의는 전투방 4~5개, 보스 주 경로 3개와 보상 이후 선택 가지 1~2개이며 `System.Random`, `UnityEngine.Random`, 시간과 호출 순서를 읽지 않는다.
- `DungeonGraph`는 `Start`, `Combat`, `BombReward`, `BossAntechamber`, `Boss` 노드, 고유 정수 방 좌표와 연결 트리를 검증하고 read-only 이웃·최단 경로를 제공한다. 실제 여덟 Unity 씬의 binder와 host가 이 그래프를 소비한다.
- `DungeonRunState`는 시작방을 최초 방문으로 시작하고, 연결된 방만 이동하며 전투·보스방 클리어 전 퇴실을 막는다. 안전방은 잠기지 않고 클리어한 전투방은 다시 잠기지 않으며, 네 방향 문 snapshot이 실제 문 presenter와 scene travel의 권위다.
- `DungeonCombatRoomLayout`은 모든 전투 노드의 room definition ID, 회전과 그래프 연결 방향인 활성 출구를 read-only로 소유한다. 같은 그래프·카탈로그는 입력 배열 순서와 무관하게 같은 배정을 만들며 호환 출구가 없으면 명시적으로 실패한다.
- `PrototypeDungeonRunSession`은 combat/special catalog asset을 mutable run 상태와 분리하고 모든 그래프 노드를 실제 scene으로 해석하며 `DungeonRunState`에 이동·클리어를 위임한다.
- `PrototypeGameSession`은 `combatEnabled=false`일 때 추격자·돌진형·갑옷 적을 생성하지 않고 `EnemyActiveCount=0`, `IsRoomCleared=true`로 시작하지만 플레이어 이동, 두 폭탄 슬롯, 자기 피해와 표현 사건은 유지한다. Start·보상·보스 전실 placeholder가 이 구성을 사용한다.
- 각 던전 씬은 동일한 seed-0 bootstrap을 포함하지만 scene load 뒤 persistent primary host가 중복 bootstrap을 제거한다. Core 이동은 기대한 대상 씬이 실제 로드된 뒤 한 번만 commit된다.
- 기본 십자 폭발은 `Void`·고정 벽에서 효과 없이 멈추고 파괴 벽은 해당 셀에 효과를 남긴 뒤 바닥으로 바꾸고 멈춘다. 광역 폭발은 반경 내 각 셀을 독립 평가해 원점을 포함한 최대 3×3을 만들며 한 셀의 벽이 다른 영역 셀을 가리지 않는다.
- `PrototypeGameSession`은 공유 `GridState`·`ManualGameClock`으로 이동 후 fuse 폭발 순서를 조정하고 성공한 설치·폭발 결과만 표현 계층에 전달한다.
- 플레이어 연속 위치와 방향은 매 Unity frame Core에서 갱신된다. `CurrentGridPosition`은 폭탄·폭발·적·점유 판정의 정수 셀 권위를 유지하고, 셀 경계를 통과할 때만 `GridState.TryMoveActor`와 `PlayerMovementStep`이 발생한다.
- `PlayerHealthSimulation`은 폭발 ID별 처리 여부, 체력 하한, 논리 무적 종료 시각과 단일 치명 결과를 소유한다. 폭발과 적 접촉은 원본 ID를 구분해 보존하면서 같은 무적을 공유하고, `PrototypeGameSession`은 적용된 피해와 사망만 표현 이벤트로 발행한다.
- `ChaserEnemySimulation`은 `ActorId(2)`로 플레이어 `ActorId(1)`을 추격하고, 2 cells/s·두 칸 방향 유지·결정론적 동률 규칙을 사용한다. 폭탄의 위험 정보는 읽지 않고 점유 장애물로만 취급한다.
- 선택적 `ChargerEnemySimulation`은 `ActorId(3)`으로 같은 격자를 점유하며 같은 행/열의 빈 가시선에서 0.75초 예고 뒤 8 cells/s cadence로 잠근 방향을 돌진하고 0.75초 회복한다. 수치는 `Proposed`다.
- 선택적 `ArmoredEnemySimulation`은 `ActorId(4)`로 같은 격자를 점유하며 첫 서로 다른 폭발에 갑옷만 파괴하고 1→3 cells/s로 빨라지며, 두 번째 서로 다른 폭발에 사망한다. 같은 `BombId`는 중복 단계로 계산하지 않는다. 수치는 `Proposed`다.
- 기본 추격자와 돌진형은 내구도 1·접촉 피해 1이며 영향 셀 폭발 한 번에 사망한다. 갑옷 적은 내구 단계 2·접촉 피해 1이다. 세션은 추격자→돌진형→갑옷 적 고정 순서로 처리하고 마지막 적 사망 뒤 단일 방 클리어를 발행하며 binder가 연결문을 개방한다. 실제 보상 선택은 아직 없다.
- 네 room asset은 모두 11×9이며 cardinal 네 방향의 중앙 잠재 출구와 중앙 십자, 평행 통로, 엇갈린 기둥, 갑옷 실험선의 서로 다른 고정 벽·spawn·퇴로·유도 순환 경로를 소유한다. 첫 방은 파괴 벽이 없고, 두 번째는 `(-1,-1)·(1,-1)`, 세 번째는 `(0,0)` 파괴 벽과 돌진형 spawn `(-3,2)`, 네 번째는 갑옷 적 spawn `(0,1)`을 소유한다. 정확한 셀 계약은 `Docs/Systems/RoomAuthoring.md`가 소유한다.
- 각 TestSandbox 씬의 `TestSandboxContext`는 격자 크기·셀 크기·blocked cell과 선택적 돌진형·갑옷 적 spawn을 대응 방 자산에서 읽는다. spawn과 내부 장애물 Transform은 표현이며 validator가 저작 셀과 일치하는지 확인한다.
- TestSandbox 로드아웃은 `prototype-cross`(`Cross`, fuse 2초, 범위 2, 설치 쿨타임 1.5초)와 `prototype-area`(`SquareArea`, fuse 1.75초, 범위 1, 설치 쿨타임 2.5초), 교체 쿨타임 2초를 소유한다. 수치는 모두 `Proposed`다.
- EditMode 테스트 244개가 하네스 발견성, 좌표·격자·시계와 cardinal 인접, actor 식별, 십자·광역 폭탄 설치·폭발·벽·연쇄, 두 슬롯 독립 쿨타임·실패 미소비·교체 경계·주입 시계 정지, 플레이어 명령과 frame 연속 진행·해제 즉시 정지·빠른 방향 반복·다중 셀 경계·점유 전이·설치자 한정 통과, 폭발/접촉 피해 원인·공유 무적 경계, 추격자·돌진형·갑옷 적의 결정론·cadence·상태 전이·충돌 차단·피격 단계, 전투방 저작 불변식과 전체 셀 회전, 던전 동일 seed·필수 경로·선택 가지·연결 트리·좌표 배치·실패 경계, 전투 잠금·클리어·양방향 재방문, 안정된 네 방향 문 상태와 콘텐츠 배정 재현·회전·호환·균형을 검증한다.
- `GridSpace`는 임의 원점·양수 셀 크기의 격자↔3D XZ 변환을 제공하고 Y를 표현 높이로 분리한다.
- PlayMode 전체 86개가 `GridSpace`의 정수·연속 좌표 변환, room asset→격자·spawn·고정/파괴 cell 연결과 `Awake` 전 runtime spawn, cardinal 입력과 새 직교 방향 우선의 키 겹침, 실제 Input System 유지·해제·빠른 방향 단타의 같은 frame 반영, 폭탄·파괴 벽·HUD, 안전방과 세 적 유형, 전투 클리어, combat/special catalog, 회전된 저작 문↔graph 문 상태, 로드 전 불변·기대 씬 뒤 단일 Core commit과 persistent host primary 단일성, 표현 생명주기와 하네스 발견성을 검증한다.
- 네 TestSandbox 씬의 내부 장애물은 Transform/Collider가 아니라 대응 방 ScriptableObject의 명시적 논리 blocked cell로 저작되어 있다.
- Build Settings의 첫 enabled 씬 여덟 개는 `DungeonStart`, `DungeonReward`, `DungeonBossAnte`, `DungeonBoss`, `TestSandbox`, `TestSandboxLanes`, `TestSandboxPillars`, `TestSandboxArmor` 순서이며 기존 SampleScene은 보존하되 비활성화했다.
- BombSwap 런타임은 기존 일반 템플릿을 수정하지 않고 게임 전용 `BombSwapInputActions.inputactions`를 사용한다.
- URP 17.5.0과 Input System 1.19.0이 설치되어 있다.
- WebGL platform quality는 Mobile 프로필을 사용한다.
- WebGL threads support는 꺼져 있고 data caching은 켜져 있다.
- `Tools/ServeWebGL.mjs`는 검증 빌드를 `127.0.0.1`에서 수동 관찰용으로 제공하고 자동 스모크와 동일한 `WebGLStaticServer.mjs`를 사용한다. 외부 배포 서버는 아니다.
- Feel 등 vendor 에셋이 있으나 Core/first-party 구현과 아직 연결되지 않았다.

## 진행 중

- 최신 4방 WebGL에서 파괴 블록·돌진형·갑옷 적이 폭탄별 설치 위치, 퇴로와 다음 폭발 계획을 실제로 다르게 만드는지 사람 플레이테스트로 비교한다.
- 첫 전투방 클리어→보상 placeholder→다음 전투와 이전 방 왕복을 한 세션에서 검증하고 special room의 실제 역할을 점진적으로 교체한다.

## 바로 다음 권장 작업

1. [갑옷 적 2회 피격 플레이테스트](../Playtesting/ArmoredEnemyProtocol.md)로 첫 피격의 외형 축소·색 변화와 1→3 cells/s 변화가 즉시 읽히는지, 두 번째 폭탄 위치를 다시 계획하게 만드는지 관찰한다.
2. seed-0 Start→첫 전투 자동 스모크를 전투 클리어→보상방→다음 전투·이전 방 왕복까지 확장하고, 재입장 시 전투 상태 정책을 명시한다.
3. `DungeonReward`에 첫 실제 폭탄 선택을 연결하고 선택 결과를 두 슬롯 loadout persistence로 넘긴다.

## 알려진 위험과 미정

- 이동은 현재 기본 5 cells/s의 Core frame 연속 위치와 셀 경계 정수 점유 전이를 사용한다. 키 해제 즉시 정지와 빠른 `North/East` 반복은 자동 검증됐지만 최종 속도, 벽 모서리 코너 보정과 셀 경계 판정 가독성은 수동 재확인 전까지 `Proposed`다.
- 프로토타입은 플레이어 `ActorId(1)`, 추격자 `ActorId(2)`, 선택적 돌진형 `ActorId(3)`, 선택적 갑옷 적 `ActorId(4)`을 고정 생성하고 ID 순서를 사용한다. 범용 적 ID 발급, 가변 목록과 동일 목적 셀 경합 정책은 아직 없다.
- 두 폭탄 슬롯은 십자와 3×3 광역으로 구조적 공간 역할이 달라졌지만 실제 플레이에서 다른 위치 선택을 만드는지 아직 판정하지 않았다. 광역의 넓은 자기 위험과 긴 설치 쿨타임이 선택을 만들지 답답함만 만드는지 관찰해야 한다. 폭탄별 위력과 동시 설치 수 제한은 아직 없다.
- 최대 체력 5, 자기 폭발/추격자 접촉/돌진 충돌 피해 1, 무적 0.75초와 피격 색 pulse는 자동 계약을 통과했지만 재미·가독성은 플레이테스트 전까지 `Proposed`다. 지속 인접 시 무적 종료마다 반복 피해가 가능하며 부활·재시작, 완성 HUD·오디오는 아직 없다.
- 추격자 2 cells/s·두 칸 방향 유지·국소 Manhattan 선택은 복잡한 미로 최단 경로를 보장하지 않는 `Proposed` 정책이다. 접촉 압력은 연결됐지만 실제 공정성과 유도 재미는 아직 플레이테스트하지 않았다.
- 돌진형의 예고·돌진·회복 수치와 세 번째 방 시작 직선 배치는 `Proposed`다. 자동 검증은 상태와 충돌의 정확성만 보장하며, 색만으로 예고를 읽는 가독성·두 적의 동시 압력·파괴 블록과의 선택은 사람 플레이테스트가 필요하다.
- 갑옷 적의 1→3 cells/s 변화, 외형 축소와 색 변화는 `Proposed`다. 자동 검증은 두 서로 다른 폭발과 상태·속도·점유·클리어 순서만 보장하며, 첫 피격이 충분히 읽히고 두 번째 설치 계획을 바꾸는지는 사람 플레이테스트가 필요하다.
- 던전 8개 씬 Development WebGL 빌드는 약 141.6 MB이며 최종 연결 빌드에서 오류 0, TextMeshPro 대형 메서드 분할 안내 3건이 기록됐다. 실제 배포 예산과 미사용 AI Inference·vendor 패키지 정리는 수직 슬라이스 이후 별도 결정이 필요하다.
- AI Navigation, AI Inference, Visual Scripting 등 설치 패키지의 실제 사용 여부는 결정되지 않았다.
- TestSandbox의 설치·교체 명령은 실제 게임 상태를 바꾸지만 pause 명령은 아직 probe 외 실제 규칙 소비자가 없다.
- 프로토타입 전투방 스키마는 필수 추격자와 선택적 돌진형·갑옷 적 각 한 개, 고정 벽과 1회 파괴 벽만 지원한다. 범용 여러 적 spawn 후보, 파괴 보상·비밀방, 보상·전환 anchor와 room prefab 선택은 아직 없다.
- 네 room asset의 cardinal 잠재 출구와 Core 배정은 구현됐지만 TestSandbox의 외곽 벽은 아직 실제 문으로 분할되지 않았다. 미사용 잠재 출구의 닫힘, 활성 문의 열림·입장 안전 spawn과 회전된 방 geometry 표현은 Unity 어댑터 검증 전까지 미구현이다.
- Core 그래프의 기본 4~5 전투방과 단일 선택 가지는 `Proposed`다. 자동 검증은 재현성과 구조만 보장하며 탐색 동기, 되돌아가기 피로와 방 반복 체감은 Unity 탐색 loop와 사람 플레이테스트 전에는 판정할 수 없다.
- 현재 4방 전환은 씬 이름과 realtime 1.25초 지연을 쓰는 플레이테스트 어댑터로 새 `PrototypeDungeonRunSession`을 소비하지 않는다. Core 방문/클리어와 그래프 노드별 전투방 asset·scene 선택은 구현됐지만 실제 문, 보상, Unity 수명 연결, 저장·재시작과 그래프 기반 방 전환 연출은 아직 없다.
- 개발 browser probe의 `audio-unlocked`는 입력 수신 marker이며 실제 오디오 재생은 아직 검증하지 않았다.
- 게임패드 binding은 구조 검증만 완료했다. 정확한 대각선 값을 만드는 게임패드·D-pad에도 새 직교 축 우선 정책이 적용되므로 목표 기기 수동 플레이가 남아 있다.

## 최근 검증

- Git 작업 트리 기준선 확인: 작업 시작 전 clean.
- `Tools/Verify.ps1 -StaticOnly`: 통과. Markdown 링크, 스킬 4종, asmdef 5종, Core 금지 API 검사. 최신 기록 산출물 `Artifacts/Verification/20260814-204239-static/`.
- `skill-creator` 공식 `quick_validate.py`: 프로젝트 스킬 4종 모두 통과.
- PowerShell AST parse와 `node --check`로 `WebGLSmoke.mjs`, `WebGLStaticServer.mjs`, `ServeWebGL.mjs`, `WebGLStaticServerTests.mjs`: 통과.
- `node Tools/WebGLStaticServerTests.mjs`: HTML/WASM/data/symbols MIME, gzip/Brotli `Content-Encoding`, GET/HEAD 제한, 404와 경로 이탈 403 계약 통과.
- 수동 CLI를 실제 3방 WebGL 빌드에 loopback 기동: index 200 `text/html`, WASM HEAD 200 `application/wasm`, `Cache-Control: no-store`, 경로 이탈 403 확인.
- 하네스 C# 3개를 Unity 6000.5.3f1 설치 어셈블리 기준으로 외부 컴파일: 경고 0, 오류 0.
- WebGL smoke의 입력 반응·지연 이벤트 fixture를 설치된 Edge headless에서 실행: load, canvas focus, keyboard, resize, gameplay probe, Console 모두 통과.
- Fast 잠금 보호: 실행 중 Editor의 `Temp/UnityLockfile`을 감지해 종료 코드 3과 summary를 기록하는 동작 확인.
- Unity Editor refresh로 신규 first-party 폴더, asmdef, C#의 `.meta` 생성 확인.
- 전체 Markdown 내부 링크와 신규 asmdef JSON 정적 검사: 통과.
- 신규 asmdef 5개 JSON 파싱, 이름/참조 구조 정적 검사: 통과.
- 루트 AGENTS 크기: 약 9 KB로 Codex 기본 합산 제한 32 KiB 이내.
- 공식 Unity MCP 연결과 활성 씬 `Assets/Scenes/SampleScene.unity` 확인.
- Unity Editor import/compile: 격자·시계·폭탄 Core, Unity 좌표 어댑터와 테스트 스크립트 임포트 후 Console 오류 0.
- EditMode: 연결된 Unity Test Runner에서 `BombSwap.Core.Tests` 159개 통과, 실패/건너뜀/불확정 0. frame 연속 이동·해제 즉시 정지·빠른 방향 반복·다중 셀 경계·막힌 중심 제한과 기존 전투 규칙, 방 ID·범위·중복·spawn 안전·출구 경계·전체 연결성·두 퇴로·닫힌 유도 경로 테스트 포함.
- `Tools/Verify.ps1 -Tier Fast`: 실행 중인 동일 프로젝트 Editor 잠금 때문에 별도 batchmode로는 미실행. Unity 컴파일과 EditMode 테스트는 연결된 MCP로 수행.
- PlayMode 대상 회귀: 공식 Unity MCP로 `PrototypePlayerControllerTests` 19개 통과, 실패/건너뜀/불확정 0. 테스트 어셈블리 내부 리포터로 도메인 리로드 후 결과 확인.
- PlayMode 전체 회귀: `BombSwap.Unity.Tests` 64개 통과, 실패/건너뜀/불확정 0. 새 직교 방향 우선 8개 사분면, 실제 입력 유지·해제·6회 `North/East` 단타의 Core 연속 위치와 Transform 직접 표시, room asset 연결, 기존 입력·전투·표현 생명주기와 방 전환 설정·마지막 방 무전환 포함.
- `PrototypeContentValidator`: 세 전투방의 Core 변환·고유 ID, 각 씬의 대응 room/spawn/장애물/전환 참조, Input Actions·폭탄·vitals·추격자·session·카메라·조명과 Build Settings 3방 순서 검증 오류 0.
- Scene View 다각도 시각 확인: 평행 통로의 두 세로 벽과 엇갈린 기둥의 다섯 장애물, 각 플레이어 spawn 표현을 식별.
- Development WebGL 3방 빌드 성공: 140,537,511 bytes, 69.669초, 오류 0. 설치된 Sentis·vendor·TextMeshPro 관련 기존 범주의 경고 359개가 보고됐다.
- 실제 Edge headless browser smoke: load, canvas focus, 기존 `W`·`Z`·`A`·`X`·`Esc` 입력과 접촉/폭발 source·적 사망·방 클리어를 관측하고 중앙 루프→평행 통로→엇갈린 기둥을 한 세션에서 전환, browser Console/page error 0.
- 프레임 연속 이동 Development WebGL 빌드: 140,634,127 bytes, 266.945초, 오류 0, TextMeshPro IL2CPP 대형 메서드 분할 경고 3건. Edge headless에서 6회 `North/East` 단타가 각각 release 전에 실제 motion을 만들었고, 기존 전투·3방 전환·마지막 방 자기 폭발·resize·browser Console/page error 0을 확인했다.
- 최신 WebGL 검증 증거: `Artifacts/Verification/20260814-151702-continuous-movement-web-connected/` (Git 제외). 빌드 후 자동 생성된 URP/ProjectSettings/Burst 부산물은 작업 diff에서 제거했다.
- 공통 정적 서버 리팩터링 뒤 기존 빌드 Edge headless 회귀: load, canvas focus, keyboard, 3방 시퀀스, resize, gameplay probe, browser Console 모두 통과. 증거 `Artifacts/Verification/20260814-111845-shared-server-browser/`.
- 파괴 가능 블록 연결 후 EditMode 173개와 PlayMode 68개 전체 통과, 실패/건너뜀/불확정 0. 대상 `CombatRoomDefinitionTests` 16개와 파괴 블록 PlayMode 테스트도 별도로 통과했으며 `PrototypeContentValidator`는 방 데이터·씬 시각·재질·presenter 참조를 오류 0으로 검증.
- Development WebGL 빌드 성공: 140,883,086 bytes, 317.216초, 오류 0, 설치된 패키지·셰이더 기존 범주의 경고 359개. 산출물은 `Artifacts/Verification/20260814-171929-destructible-wall-web-connected/`에 기록.
- Edge headless smoke 12개 검사 통과. 두 번째 방에서 슬롯 초기화를 거쳐 면적 폭탄을 명시 선택한 뒤 `destructible-wall-destroyed`를 관측했고, 기존 입력·3방 전환·6회 `North/East` 단타·마지막 방 자기 폭발·resize도 함께 통과했다. Browser Console/page error 0.
- 실제 WebGL 캡처 `webgl-destructible-walls.png`에서 두 번째 방의 황갈색 분할 블록 두 개, 회색 고정 벽, 두 폭탄 슬롯의 시각 구분을 확인했다.
- 파괴 블록 시점 정적 검증 `Artifacts/Verification/20260814-173048-static/`, `node --check Tools/WebGLSmoke.mjs`, `WebGLStaticServerTests.mjs` 통과.
- 돌진형 연결 후 공식 Unity MCP EditMode 193개와 PlayMode 71개 전체 통과, 실패/건너뜀/불확정 0. `PrototypeContentValidator`는 정의·collider 없는 prefab·방별 선택적 spawn·session/presenter 참조를 오류 0으로 검증했다.
- 돌진형 Development WebGL 빌드 성공: 140,947,504 bytes, 241.080초, 오류 0, 설치된 패키지·셰이더 기존 범주의 경고 359개. 산출물은 `Artifacts/Verification/20260814-181400-charger-web-connected/`에 기록했다.
- Edge headless smoke 전체 통과. 마지막 방에서 `charger-telegraph → charger-charge → charger-moved`, 빠른 `North/East` 단타 회귀, 돌진선 이탈 뒤 자기 폭발 피해와 기존 3방·파괴 블록·Console/page error 0을 확인했다.
- 돌진형 문서 갱신 뒤 최신 정적 검증 `Artifacts/Verification/20260814-183106-static/`, `node --check Tools/WebGLSmoke.mjs`, `WebGLStaticServerTests.mjs` 통과.
- 갑옷 적 연결 후 공식 Unity MCP 전체 EditMode 206개와 PlayMode 72개 통과, 실패·건너뜀·불확정 0. 연결 하네스 JSON은 `Artifacts/Verification/ConnectedTests/20260814-101213-997.json`, `Artifacts/Verification/ConnectedTests/20260814-101121-209.json`에 보존했다.
- `PrototypeContentValidator`: 갑옷 정의·collider 없는 prefab·선택적 spawn·네 번째 방/씬·presenter와 Build Settings 4방 순서까지 오류 0.
- 갑옷 시작 Development WebGL 성공: 141,100,454 bytes, 339.23초, 오류 0. Edge headless에서 첫 실제 폭발의 `armored-broken`과 상태별 이동, 두 번째 폭발의 `armored-died`, 최종 `room-cleared`, screenshot, browser Console/page error 0을 확인했다. 증거 `Artifacts/Verification/20260814-191400-armored-web-connected/`.
- 네 번째 씬을 포함한 기본 Development WebGL 성공: 141,100,958 bytes, 25.47초 incremental, 오류 0. Edge headless 기본 smoke에서 입력·빠른 방향 전환·두 폭탄·파괴 블록·돌진형·피해·기존 3방 전환·resize·browser Console/page error 0을 확인했다. 네 번째 갑옷 씬은 위 전용 smoke로 별도 검증했다. 증거 `Artifacts/Verification/20260814-192200-default-web-connected/`.
- 결정론적 던전 Core 대상 `DungeonGeneratorTests` 15/15 통과. seed 0 golden snapshot, 음수·0·정수 극단과 512개 연속 seed의 4/5방 분포·64개 초과 signature, 필수 보상/보스 경로·선택 가지·연결/좌표 불변식·read-only 조회를 포함한다. 증거 `Artifacts/Verification/ConnectedTests/20260814-105857-417.json`.
- 던전 Core 최종 연결 뒤 전체 EditMode 221/221 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-110501-629.json`.
- Unity 6000.5.3f1 `BombSwap.Core`·tests 컴파일 성공, `PrototypeContentValidator` 기존 네 방·씬·Build Settings 오류 0, Console 오류 0. 실행 중 Editor 잠금 때문에 별도 batchmode `-Tier Fast`는 미실행하고 연결된 개별 증거로 검증했다.
- 던전 탐색 상태 대상 `DungeonRunStateTests` 9/9 통과. 첫 전투·보스 잠금, 안전방, 클리어 전 퇴실 차단, 방향별 이동, read-only 방문/클리어 snapshot과 전체 트리 왕복을 포함한다. 증거 `Artifacts/Verification/ConnectedTests/20260814-111508-768.json`.
- 탐색 상태 최종 연결 뒤 전체 EditMode 230/230 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-111727-876.json`. Unity 컴파일과 Console 오류 0, 정적 검증은 `Artifacts/Verification/20260814-201824-static/`에 기록했다.
- 전투방 배정기·방 정의 대상 EditMode 30/30 통과. catalog 순서 무관 재현, 활성 출구·회전 호환, 128 seed 다양성, 사용 균형, 호환 콘텐츠 부족 실패와 중복 출구 방향 거부를 포함한다. 증거 `Artifacts/Verification/ConnectedTests/20260814-112553-378.json`.
- 배정·네 방향 잠재 출구 최종 연결 뒤 전체 EditMode 240/240, PlayMode 72/72 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-113106-251.json`, `Artifacts/Verification/ConnectedTests/20260814-113119-678.json`.
- `PrototypeContentValidator`가 네 실제 room asset의 cardinal 잠재 출구, 기존 room ID·spawn·장애물·씬·Build Settings를 오류 0으로 검증했고 Unity Console Error 0이다. 정적 검증은 `Artifacts/Verification/20260814-203304-static/`에 기록했다.
- Unity 런 카탈로그 대상 `PrototypeDungeonRunSessionTests` 5/5 통과. 실제 asset·scene 선택 해석, 특수방 경계, Core 잠금·클리어·왕복 위임, 카탈로그 배열 복사와 null·빈·중복 entry 거부를 포함한다. 증거 `Artifacts/Verification/ConnectedTests/20260814-113823-164.json`.
- 런 카탈로그 최종 연결 뒤 전체 EditMode 240/240, PlayMode 77/77 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-114054-717.json`, `Artifacts/Verification/ConnectedTests/20260814-114114-309.json`.
- 실제 `PrototypeDungeonCombatRoomCatalog.asset`의 네 room asset·씬 매핑과 기존 전체 콘텐츠를 `PrototypeContentValidator`가 오류 0으로 재검증했고 Unity Console Error 0이다. 정적 검증은 `Artifacts/Verification/20260814-204239-static/`에 기록했다.
- 던전 문 상태 대상 `DungeonRunStateTests` 11/11, Unity 런 세션 대상 `PrototypeDungeonRunSessionTests` 6/6 통과. 네 방향 안정 순서, 비연결·잠금·개방, 대상 방 보존과 전투방 활성 출구 일치를 포함한다. 증거 `Artifacts/Verification/ConnectedTests/20260814-115245-379.json`, `Artifacts/Verification/ConnectedTests/20260814-115301-842.json`.
- 문 상태 연결 뒤 전체 EditMode 242/242, PlayMode 78/78 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-115349-825.json`, `Artifacts/Verification/ConnectedTests/20260814-115407-511.json`. Unity Console Error 0, 정적 검증 `Artifacts/Verification/20260814-205647-static/` 통과. 실제 씬 문 표현·전환을 변경하지 않아 이번 슬라이스에서는 WebGL 재빌드를 요구하지 않았다.
- 방 전체 회전 대상 `CombatRoomDefinitionTests` 23/23, 안전 placeholder·runtime spawn 대상 `PrototypePlayerControllerTests` 29/29 통과. 최종 overlap 방어 재검증까지 포함한 증거 `Artifacts/Verification/ConnectedTests/20260814-120832-507.json`, `Artifacts/Verification/ConnectedTests/20260814-121408-188.json`.
- 회전·안전 session 기반 연결 뒤 전체 EditMode 244/244, PlayMode 80/80 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-120956-813.json`, `Artifacts/Verification/ConnectedTests/20260814-121013-190.json`. Unity Console Error 0, 정적 검증 `Artifacts/Verification/20260814-211516-static/` 통과. 씬·입력·빌드 산출물을 아직 변경하지 않아 WebGL 재빌드는 다음 실제 문/전환 슬라이스에서 수행한다.
- 특수방 catalog·navigator·host 대상 `PrototypeDungeonRunSessionTests` 10/10 통과. 직렬화 catalog 시작 검증, 필수 타입과 고유 씬, 로드 불가·중복·씬 불일치의 Core 불변, 기대 씬 단일 commit과 host primary 단일성을 포함한다. 최종 증거 `Artifacts/Verification/ConnectedTests/20260814-122518-725.json`.
- 전환 host 연결 뒤 전체 EditMode 244/244, PlayMode 84/84 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-122150-723.json`, `Artifacts/Verification/ConnectedTests/20260814-122207-191.json`. Unity Console Error 0, 정적 검증 `Artifacts/Verification/20260814-212627-static/` 통과. 실제 씬·Build Settings는 아직 변경하지 않아 WebGL 재빌드는 문/씬 authoring 슬라이스에 남겼다.
