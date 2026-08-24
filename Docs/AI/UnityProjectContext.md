# Unity 프로젝트 컨텍스트

<!-- unity-onboarding:generated:start -->

## 프로젝트 요약

- 프로젝트 루트: `D:\UnityProject\OpenAI Project Clean`
- 한 줄 설명: 정수 XZ 논리 격자에서 두 종류의 지연 폭탄을 교대 사용해 미래 위험 공간을 설계하는 3D WebGL 탑다운 룸 액션 로그라이트 프로토타입.
- 현재 단계: 로비와 인게임 UI, 한 층 던전, 5개 전투방, 폭탄 3종, 적 5종과 3페이즈 보스까지 연결된 기능 프로토타입. 자동 계약은 넓지만 핵심 전투 수치와 재미 판정은 여전히 사람 플레이테스트 대상이다.
- 마지막 분석: 2026-08-24 (Asia/Seoul)
- 마지막 분석 커밋: `de3f3a2d77e5deef81e1a0ba979b0368a4547be2` (`fix: restore responsive continuous player movement`)
- Git 분석 기준 branch: `codex/ui-design-continued`. 작업 중인 변경은 `git status`로 다시 확인하고 사용자 변경과 분리한다.

## 확인된 환경

- Unity: `6000.5.3f1 (c2eb47b3a2a9)`.
- 렌더링: URP `17.5.0`. WebGL은 `Mobile` 품질 프로필과 `Mobile_RPAsset`을 사용하며 render scale은 `0.8`, 추가 광원 그림자는 비활성화되어 있다.
- 입력: Input System `1.19.0` 전용(`activeInputHandler: 1`). 게임 전용 `BombSwapInputActions.inputactions`를 의미 명령으로 변환한다.
- 목표 플랫폼: 3D WebGL. 기준 해상도는 `960x600`, WebGL threads는 꺼져 있고 data caching은 켜져 있다.
- 실행 상태: Unity 주 Editor 한 개와 그 하위 Asset Import Worker 두 개가 프로젝트를 열고 있다. 독립 Editor가 세 개 열린 상태는 아니다.
- 현재 씬: `Library/LastSceneManagerSetup.txt` 기준 `DungeonLobby`가 활성 상태로 보이지만, Unity MCP 연결이 없어 Editor API로 재확인하지 못했다.

## 주요 패키지와 프레임워크

| 영역 | 결과 | 신뢰도 | 근거 |
| --- | --- | --- | --- |
| 렌더링 | URP 17.5.0, Mobile/PC 두 품질 자산 | 확인 | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset` |
| 입력 | Input System 1.19.0과 first-party 의미 명령 어댑터 | 확인 | `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset`, `Assets/Game/Runtime/Input/` |
| UI | uGUI 2.5.0, TextMeshPro, Raster 픽셀 폰트, 공유 HUD/미니맵/pause 프리팹 | 확인 | `Packages/manifest.json`, `Assets/Game/Presentation/UI/`, `Docs/Development/CurrentState.md` |
| 비동기 | UniTask Git 패키지가 설치되어 있으나 `Assets/Game` C# 사용처는 없음 | 확인 | `Packages/packages-lock.json`, first-party 코드 검색 |
| AI/내비게이션 | AI Assistant, AI Inference, AI Navigation 패키지가 설치되어 있으나 first-party C# 사용처는 없음 | 확인 | 패키지 설정과 first-party 코드 검색 |
| 멀티플레이 | Multiplayer Center만 설치되어 있고 런타임 네트워킹 구현은 없음 | 확인 | 패키지 설정과 first-party 코드 검색 |
| 서드파티 | 무료 DOTween Core는 Git에 포함. FEEL, DOTween Pro, `Assets/ThirdParty`, `Assets/Arts/VFX` 원본은 로컬 패키지 경계 | 확인 | `Docs/ADR/0009-Local-ThirdParty-Asset-Distribution.md` |

Addressables, ECS/DOTS, DI 컨테이너, 서비스 로케이터, 범용 이벤트 버스, 런타임 네트워킹은 현재 구조에 없다.

## 디렉터리 구조

| 경로 | 책임 | 신뢰도 |
| --- | --- | --- |
| `Assets/Game/Core` | UnityEngine 비참조 결정론적 격자·폭탄·이동·피해·적·보스·던전 규칙 | 확인 |
| `Assets/Game/Runtime` | Unity 생명주기, 입력, run/room 세션, 씬 전환, 설정, WebGL probe | 확인 |
| `Assets/Game/Presentation` | Transform, presenter, HUD, 미니맵, pause, 로비, 시각 표현 | 확인 |
| `Assets/Game/Authoring` | ScriptableObject 정의와 검증된 Core 불변 데이터 변환 | 확인 |
| `Assets/Game/Content` | 28 ScriptableObject, 39 prefab, 36 material, 12 wav, Input Actions와 UI 리소스 | 확인 |
| `Assets/Game/Editor` | 콘텐츠 builder/validator, UI 저작 도구, 플레이테스트 메뉴, 빌드 자동화 | 확인 |
| `Assets/Game/Tests` | 29 EditMode 테스트 파일, 13 PlayMode 테스트 파일, 연결 테스트 리포터 | 확인 |
| `Tools` | Static/Fast/Full/Web 검증, WebGL 서버·Playwright smoke·로그 분석 | 확인 |

first-party C#은 Core 97, Runtime 16, Presentation 33, Authoring 15, Editor 15, Tests 43개 파일이다.

## 어셈블리 경계

| 어셈블리 | 책임 | 주요 참조 | 비고 |
| --- | --- | --- | --- |
| `BombSwap.Core` | 결정론적 규칙과 값 객체 | 없음 | `noEngineReferences: true`, unsafe 비활성 |
| `BombSwap.Unity` | Runtime/Presentation/Authoring/Content 연결 | Core, Input System, uGUI, TMP | Unity 런타임 어셈블리 |
| `BombSwap.Editor` | 검증·빌드·저작 도구 | Core, Unity, Input, UI, TMP | Editor 전용 |
| `BombSwap.Core.Tests` | 빠른 규칙 계약 | Core, Test Framework | Editor/EditMode 전용 |
| `BombSwap.Unity.Tests` | 씬·생명주기·입력·표현 연결 | Core, Unity, Input Test Framework, TMP | PlayMode |
| `BombSwap.ConnectedTestHarness` | 연결 Editor 테스트 실행 결과 수집 | Test Framework | Editor 전용 |

허용 의존 방향은 `Core <- Unity <- Editor/PlayMode Tests`다. Core의 Unity API, 전역 시간/랜덤, 스레드와 WebGL 비호환 API 사용은 정적 검증에서 차단한다.

## 씬과 시작 흐름

- enabled Build Settings 12씬 순서: `DungeonLobby`, `DungeonStart`, `DungeonReward`, `DungeonBossAnte`, `DungeonRecovery`, `DungeonSecret`, `DungeonBoss`, `TestSandbox`, `TestSandboxThrower`, `TestSandboxPillars`, `TestSandboxArmor`, `TestSandboxGates`.
- `TestSandboxLanes`와 기본 `SampleScene`은 비활성화되어 있다. 추가 독립 플레이테스트 씬은 저장소에 있으나 표준 build 목록에는 없다.
- 시작 흐름: `DungeonLobby`의 `PrototypeLobbyPresenter`가 Start를 받으면 `DungeonStart`를 Single load한다.
- 던전 흐름: `PrototypeDungeonRunHost`만 `DontDestroyOnLoad`로 run graph, 방문·클리어, loadout, 체력, 회복/Secret 소비 상태를 보존한다. 방의 `PrototypeGameSession`, 격자, 입력과 presenter는 매 씬에서 새로 만들어진다.
- 씬 전환은 대상 씬 로드 가능 여부를 먼저 검사하고 pending transition을 만든 뒤, 실제 로드된 씬 이름이 일치할 때만 Core 이동을 commit한다.
- terminal 상태는 같은 seed의 새 run 즉시 재시작 또는 host 제거 후 로비 복귀를 지원한다. 브라우저 새로고침을 넘는 저장/불러오기는 `Deferred`다.

## 아키텍처

| 패턴 | 결과 | 신뢰도 | 근거 |
| --- | --- | --- | --- |
| 논리 XZ 격자 | 셀 지형·actor/bomb 점유·폭발 전파·벽 차단의 권위 원본 | 확인 | ADR-0001, `GridState` |
| 순수 Core + Unity 어댑터 | Core는 Unity를 모르고 Runtime이 입력/시간/씬/표현을 연결 | 확인 | ADR-0002, asmdef |
| 결정론적 시간/랜덤 | `ManualGameClock`, 10ms simulation step, 명시적 dungeon seed | 확인 | ADR-0003, `PrototypeGameSession`, `DungeonGenerator` |
| ScriptableObject authoring | 튜닝/콘텐츠 에셋을 시작 시 검증해 Core 정의로 변환 | 확인 | `Assets/Game/Authoring/` |
| 이벤트 기반 표현 | presenter는 확정된 Core 사건과 snapshot을 표현하고 규칙을 재계산하지 않음 | 확인 | `PrototypeGameSession` events, `Docs/Architecture/RuntimeFlow.md` |
| 메모리 run 상태 + room-local simulation | persistent host와 Single scene 수명 분리 | 확인 | ADR-0008 |
| 프로토타입 조정자 | `PrototypeGameSession`이 2,044줄이며 고정 ActorId 1~7과 명시적 적 처리 순서를 가진다 | 확인 | `PrototypeGameSession.cs`, `CurrentState.md` |

마지막 항목은 현재 Accepted 프로토타입 모델에는 부합하지만, 적 수·종류가 더 늘어날 때 가장 먼저 커질 결합 지점이다. 범용 scheduler나 프레임워크로 선제 추상화하지 말고 실제 다음 기능에서 분리 필요성을 판단한다.

## 코딩 규약

- 네임스페이스는 Core `BombSwap.Core.<System>`, Unity 쪽은 주로 `BombSwap`/문서화된 책임별 네임스페이스를 사용한다.
- `[SerializeField]` 필드는 camelCase, 런타임 private 상태는 `_camelCase`, public 멤버는 PascalCase를 사용한다.
- Allman brace, 명시적 가드/예외, gameplay 거절에는 `Try*` 결과 타입을 주로 사용한다.
- 사건 이름은 `BombPlaced`, `PlayerDied`, `RoomCommitted`처럼 확정된 과거형 결과를 표현한다.
- nullable reference type은 프로젝트 표본에서 사용하지 않으며, first-party 런타임에 `async`/`await`, `Task.Run`, 스레드 사용이 없다.
- 게임 규칙 수치는 코드 상수보다 검증된 ScriptableObject가 권위 원본이며, 아직 플레이테스트 전인 수치는 `Proposed`로 취급한다.

## 테스트와 검증

- 진입점: `Tools/Verify.ps1`.
- `StaticOnly`: 문서 링크/UTF-8/asmdef/Core 금지 API/필수 도구 구조만 검사하며 compile/test 통과를 의미하지 않는다.
- `Fast`: 정적 계약, Unity compile/validator, Core EditMode.
- `Full`: Fast + first-party PlayMode.
- `Web`: Full + Development WebGL build + template/server/analyzer 테스트 + Edge/Chrome keyboard·가상 Gamepad smoke.
- 최신 통합 기준 증거: EditMode `363/363`, 전체 PlayMode `184/192` 통과. 플레이어 이동·입력 응답성, 보스 fuse 순서와 폭탄 준비 VFX timing 실패는 0이며 남은 8건은 run host·추격자·자폭병·폭발·방 클리어 기준선이다.
- 남은 PlayMode 8개 실패는 run host, 추격자, 자폭병, 폭발·방 클리어 기준선 범주이며 이동과 폭탄 fuse 계약에서 분리해 다룬다.
- 플레이어는 기본 5 cells/s의 10ms step 연속 이동, 적은 시작한 한 칸을 완료하는 committed 이동을 사용한다. 입력 해제·직교 전환·짧은 탭의 자동 계약은 복구됐고 최종 조작감은 수동 재확인이 남아 있다.
- 최신 Development WebGL은 로비 제목·투척병 Animator·현재 UI의 private vendor 직접 참조를 콘텐츠 validator가 빌드 전에 차단했다. 보스 fuse와 폭탄 준비 animation validator 오류는 0이지만 WebGL과 browser smoke 통과로 보고하지 않는다.

## 사용 가능한 Unity 도구

| 기능 | 상태 | 근거 |
| --- | --- | --- |
| Unity Editor 프로세스 | 사용 가능 | Unity 6000.5.3f1 주 Editor 1개 + Asset Import Worker 2개 |
| MCP 연결 상태 | 현재 사용 불가 | `mcpforunity://instances`가 instance count 0 반환 |
| Editor 버전/프로젝트 정보 | 저장소로 확인, MCP 미확인 | `ProjectVersion.txt`; MCP project info는 instance 없음 |
| Console 읽기 | 사용 불가 | MCP Editor instance 없음 |
| Build Settings/씬 목록 | 저장소로 확인, MCP 미확인 | `ProjectSettings/EditorBuildSettings.asset` |
| Asset/Hierarchy 조회 | 현재 사용 불가 | MCP Editor instance 없음 |
| Test 목록/실행 | 현재 사용 불가 | MCP Editor instance 없음; 저장된 연결 테스트 산출물은 읽기 가능 |
| Profiler/Play Mode 상태 | 현재 사용 불가 | MCP Editor instance 없음 |

Unity MCP 서버와 리소스 정의 자체는 설치되어 있다. Editor bridge 연결만 복구되면 Console, 씬, 테스트와 profiler를 읽을 수 있다. 연결 복구 전에는 열린 Editor가 프로젝트 lock을 소유하므로 별도 batchmode 검증을 병렬 실행하지 않는다.

## 중요한 제약

- Transform/Physics/Input System/Unity 시간과 랜덤을 Core 권위 상태로 만들지 않는다.
- 파괴 불가 벽은 폭발 전파를 효과 없이 끝내고, 파괴 가능 벽은 해당 셀에 효과·파괴를 적용한 뒤 전파를 끝낸다.
- 모든 폭탄 종류의 연쇄는 같은 scheduler에서 양수 고정 지연을 거쳐야 한다.
- 설치자 폭탄 통과는 해당 actor/bomb 쌍의 셀 이탈 전 제한 상태이며 전역 충돌 무시가 아니다.
- 씬, prefab, ScriptableObject, Input Actions와 ProjectSettings YAML을 텍스트 치환으로 수정하지 않는다.
- `Assets/Feel`, `Assets/Plugins`와 private third-party 원본을 직접 수정하지 않는다.
- WebGL 메인 스레드 경로에 동기 대기, 스레드 전제, 반복 할당을 추가하지 않는다.
- 자동 테스트는 재미를 판정하지 않는다. 사람 플레이테스트와 자동/브라우저 증거를 함께 사용한다.

## 미확인 사항과 현재 위험

1. **현재 기준선 실패:** 최신 커밋은 Full/Web green 상태가 아니다. 남은 PlayMode 8개 실패를 run host·추격자·자폭병·폭발·방 클리어 원인별로 분리하고 회귀 기준선을 복구하는 것이 우선이다.
2. **플레이어 이동 수동 확인:** 자동 이동 계약은 복구됐다. 키 해제 즉시 정지, 셀 중간 직교 전환, 빠른 `상→우` 반복과 벽·폭탄 경계의 예약 안전성을 실제 키보드로 재확인해야 한다.
3. **Console 미확인:** Editor는 실행 중이지만 MCP bridge가 연결되지 않아 현재 Console error/warning을 직접 읽지 못했다.
4. **WebGL 검증 차단:** 최신 Development WebGL은 콘텐츠 validator에서 중단됐고 browser smoke는 실행되지 않았다.
5. **프로토타입 확장성:** `PrototypeGameSession`과 고정 ActorId/처리 순서는 현재 범위에는 명시적이지만 다음 적/시스템 추가 시 결합 비용이 높다.
6. **사용하지 않는 패키지:** AI Inference/Navigation/Visual Scripting/UniTask 등의 설치 필요성과 WebGL 크기 영향은 결정되지 않았다.
7. **사람 검증 잔여:** 폭탄별 실제 위치 선택, 투척병 가독성, 보스 10체력 피로도, Secret/Recovery 가치, 물리 게임패드 조작감은 자동 통과로 확정할 수 없다.
8. **미연결 표현:** BGM/SFX runtime route, room/phase crossfade, 폭발 화면 흔들림 소비자와 완성 VFX/오디오는 아직 연결·검증되지 않았다.
9. **private asset 경계:** 공개 clone은 기능 폴백으로 동작하지만 원래 UI/VFX 외형과 서드파티 사용 권한은 별도 로컬 패키지와 라이선스 확인이 필요하다.

## 검사한 주요 근거

- `AGENTS.md`
- `Docs/INDEX.md`
- `Docs/GameDesign/GDD_v0.2.md`
- `Docs/GameDesign/ProtoType_v0.2.md`
- `Docs/Architecture/Overview.md`
- `Docs/Architecture/RuntimeFlow.md`
- `Docs/Architecture/DependencyRules.md`
- `Docs/ADR/0001-Logical-XZ-Grid.md` ~ `0009-Local-ThirdParty-Asset-Distribution.md` 중 관련 Accepted 결정
- `Docs/Development/CurrentState.md`
- `Docs/Development/PlayerMovementResponsivenessRegression.md`와 병행 갱신된 이동 계약 문서
- `Docs/Development/DefinitionOfDone.md`
- `Docs/Testing/VerificationHarness.md`
- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`, `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`, `Packages/packages-lock.json`
- first-party asmdef 6개와 `Assets/Game` 대표 Core/Runtime/Presentation/Authoring 코드
- `Assets/Game/Core/Grid/GridState.cs`, `Assets/Game/Core/Bombs/BombSimulation.cs`
- `Assets/Game/Runtime/Prototype/PrototypeGameSession.cs`, `PrototypeDungeonRunHost.cs`, `PrototypeDungeonRunSession.cs`
- `Assets/Game/Runtime/Input/BombSwapInputReader.cs`, `Assets/Game/Presentation/Prototype/PrototypeLobbyPresenter.cs`
- `Tools/Verify.ps1`
- `Artifacts/Verification/20260824-032025-static/summary.json`
- `Artifacts/Verification/ConnectedTests/20260823-181535-063.json`, `20260823-181634-617.json` 및 후속 집중 PlayMode 결과
- Unity MCP resources: `mcpforunity://instances`, `mcpforunity://editor/state`, `mcpforunity://project/info`, `mcpforunity://tests`

<!-- unity-onboarding:generated:end -->
