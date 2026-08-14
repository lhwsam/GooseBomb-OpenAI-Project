# 현재 프로젝트 상태

- 기준일: 2026-08-14
- 단계: 기본 전투 3방 관찰 플레이테스트 준비
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
- 장치 입력을 Core `PlayerCommand`로 변환하고 focus 상실 시 이동을 해제하는 `BombSwapInputReader` 구현.
- 11×9 격자, 경계 벽, 내부 장애물, 플레이어 placeholder, 탑다운 카메라를 가진 `TestSandbox` 씬 구현.
- Input Actions·TestSandbox·Build Settings를 재생성/검증하는 Editor builder와 validator 구현.
- 개발 WebGL에서 입력 사건을 브라우저 smoke에 전달하는 제한된 harness probe 구현.
- 주입 시계 cadence, 원자적 actor 점유 전이, 벽·폭탄 차단을 소유하는 `PlayerMovementSimulation` 구현.
- TestSandbox 유지 입력을 기본 5 cells/s 논리 이동과 placeholder Transform 보간에 연결.
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

## 현재 저장소 사실

- `BombSwap.Core`에는 UnityEngine 비참조 논리 격자와 주입식 수동 시계가 구현되어 있다.
- `GridState`는 미등록 셀을 `Void`로 취급하고 지형, actor/bomb 점유, `ActorId` 양방향 위치 색인을 소유한다. 점유는 바닥에만 존재하며 actor가 있는 셀에 폭탄을 설치하는 제한된 동시 점유를 허용한다.
- `BombSimulation`은 활성 폭탄, 세션 내 고유 ID, 설치자 ID, fuse와 종류 독립적인 양수 지연 연쇄를 소유하고 읽기 전용 snapshot·폭발 결과에 설치자 ID를 보존한다.
- 기본 십자 폭발은 `Void`·고정 벽에서 효과 없이 멈추고 파괴 벽은 해당 셀에 효과를 남긴 뒤 바닥으로 바꾸고 멈춘다.
- `PrototypeGameSession`은 공유 `GridState`·`ManualGameClock`으로 이동 후 fuse 폭발 순서를 조정하고 성공한 설치·폭발 결과만 표현 계층에 전달한다.
- `PlayerHealthSimulation`은 폭발 ID별 처리 여부, 체력 하한, 논리 무적 종료 시각과 단일 치명 결과를 소유한다. 폭발과 적 접촉은 원본 ID를 구분해 보존하면서 같은 무적을 공유하고, `PrototypeGameSession`은 적용된 피해와 사망만 표현 이벤트로 발행한다.
- `ChaserEnemySimulation`은 현재 단일 `ActorId(2)`로 플레이어 `ActorId(1)`을 추격하고, 2 cells/s·두 칸 방향 유지·결정론적 동률 규칙을 사용한다. 폭탄의 위험 정보는 읽지 않고 점유 장애물로만 취급한다.
- 기본 추격자는 내구도 1·접촉 피해 1이며 영향 셀 폭발 한 번에 사망한다. 세션은 마지막 적 사망을 단일 방 클리어로 집계하지만 문 개방·보상은 아직 없다.
- 세 room asset은 모두 11×9이며 중앙 십자, 평행 통로, 엇갈린 기둥의 서로 다른 고정 벽·spawn·퇴로·유도 순환 경로를 소유한다. 정확한 셀 계약은 `Docs/Systems/RoomAuthoring.md`가 소유한다.
- 각 TestSandbox 씬의 `TestSandboxContext`는 격자 크기·셀 크기·blocked cell을 대응 방 자산에서 읽는다. spawn과 내부 장애물 Transform은 표현이며 validator가 저작 셀과 일치하는지 확인한다.
- TestSandbox의 `prototype-cross` ScriptableObject는 현재 fuse 2초, 범위 2와 bomb/explosion-cell prefab을 소유한다.
- EditMode 테스트 152개가 하네스 발견성, 좌표·격자·시계와 cardinal 인접, actor 식별, 폭탄 설치·폭발·벽·연쇄, 플레이어 명령과 이동 cadence·점유 전이·설치자 한정 통과, 폭발/접촉 피해 원인·공유 무적 경계, 추격자 결정론·cadence·방향 유지·폭탄 차단·단일 피격, 방 경계·연결성·퇴로·유도 경로 계약을 검증한다.
- `GridSpace`는 임의 원점·양수 셀 크기의 격자↔3D XZ 변환을 제공하고 Y를 표현 높이로 분리한다.
- PlayMode 전체 52개가 `GridSpace`, room asset→격자·spawn·blocked cell 연결, cardinal 입력 해석, 실제 Input System 키→명령→공유 격자 플레이어·추격자 이동, 접촉 피해·공유 무적·같은 프레임 폭발 사망 우선순위, 폭탄 설치·한 번 탈출·재진입 차단·fuse 폭발·적 사망·방 클리어, 방 전환 pending·마지막 방 무전환, Transform 보간, pooled 표현과 property block 생명주기, 저작 장애물 차단, probe 초기화 순서, focus reset, 재구독 계약과 하네스 발견성을 검증한다.
- 세 TestSandbox 씬의 내부 장애물은 Transform/Collider가 아니라 대응 방 ScriptableObject의 명시적 논리 blocked cell로 저작되어 있다.
- Build Settings의 첫 enabled 씬 세 개는 `TestSandbox`, `TestSandboxLanes`, `TestSandboxPillars` 순서이며 기존 SampleScene은 보존하되 비활성화했다.
- BombSwap 런타임은 기존 일반 템플릿을 수정하지 않고 게임 전용 `BombSwapInputActions.inputactions`를 사용한다.
- URP 17.5.0과 Input System 1.19.0이 설치되어 있다.
- WebGL platform quality는 Mobile 프로필을 사용한다.
- WebGL threads support는 꺼져 있고 data caching은 켜져 있다.
- `Tools/ServeWebGL.mjs`는 검증 빌드를 `127.0.0.1`에서 수동 관찰용으로 제공하고 자동 스모크와 동일한 `WebGLStaticServer.mjs`를 사용한다. 외부 배포 서버는 아니다.
- Feel 등 vendor 에셋이 있으나 Core/first-party 구현과 아직 연결되지 않았다.

## 진행 중

- 없음. 다음 작업을 시작할 때 이 섹션을 갱신한다.

## 바로 다음 권장 작업

1. `Docs/Playtesting/FirstCombatProtocol.md`에 따라 세 수제 전투방을 한 시퀀스로 사람 관찰 플레이테스트하고 익명 증거를 기록한다.
2. 플레이테스트 증거와 프로토타입 권장 순서를 바탕으로 두 슬롯·독립 설치 쿨타임 수직 슬라이스의 최소 계약을 확정한다.
3. 다중 적이 실제 검증에 필요해질 때 고정 `ActorId(2)`를 컬렉션과 결정론적 이동·접촉 순서로 확장한다.

## 알려진 위험과 미정

- 이동은 현재 기본 5 cells/s, step 시작 시 목적 셀 점유, 선형 보간을 사용한다. 최종 속도·곡선·셀 경계 감각은 플레이테스트 전까지 `Proposed`다.
- 프로토타입은 플레이어 `ActorId(1)`과 단일 추격자 `ActorId(2)`를 고정 생성한다. 여러 적의 ID 발급, 이동 순서와 동일 목적 셀 경합 정책은 아직 없다.
- 현재 폭탄 정의와 Unity 저작 데이터는 기본 십자 모양만 지원하며 쿨타임, 폭탄별 위력, 적 피해, 직선·광역 폭탄은 아직 없다.
- 최대 체력 5, 자기 폭발/추격자 접촉 피해 1, 무적 0.75초와 피격 색 pulse는 자동 계약을 통과했지만 재미·가독성은 플레이테스트 전까지 `Proposed`다. 지속 인접 시 무적 종료마다 반복 피해가 가능하며 부활·재시작, 완성 HUD·오디오는 아직 없다.
- 추격자 2 cells/s·두 칸 방향 유지·국소 Manhattan 선택은 복잡한 미로 최단 경로를 보장하지 않는 `Proposed` 정책이다. 접촉 압력은 연결됐지만 실제 공정성과 유도 재미는 아직 플레이테스트하지 않았다.
- 개발 WebGL 기준 빌드는 약 140.0 MB이며 현재 설치된 AI Inference·vendor 패키지와 셰이더가 빌드 크기, 전체 재빌드 시간과 경고 수를 크게 차지한다. 실제 배포 예산과 패키지 정리는 피해·적 수직 슬라이스 이후 별도 결정이 필요하다.
- AI Navigation, AI Inference, Visual Scripting 등 설치 패키지의 실제 사용 여부는 결정되지 않았다.
- TestSandbox의 설치 명령은 실제 게임 상태를 바꾸지만 교체·pause 명령은 아직 probe 외 실제 규칙 소비자가 없다.
- 프로토타입 전투방 스키마는 단일 추격자와 고정 벽만 지원한다. 파괴 가능 벽, 여러 적 spawn 후보, 보상·전환 anchor, 방 prefab과 런 그래프는 아직 없다.
- 현재 3방 전환은 씬 이름과 realtime 1.25초 지연을 쓰는 플레이테스트 어댑터다. 실제 던전 그래프, 보상, 저장·재시작과 방 전환 연출을 대신하지 않는다.
- 개발 browser probe의 `audio-unlocked`는 입력 수신 marker이며 실제 오디오 재생은 아직 검증하지 않았다.
- 게임패드 binding은 구조 검증만 완료했고 목표 기기 수동 플레이가 남아 있다.

## 최근 검증

- Git 작업 트리 기준선 확인: 작업 시작 전 clean.
- `Tools/Verify.ps1 -StaticOnly`: 통과. Markdown 링크, 스킬 4종, asmdef 5종, Core 금지 API 검사. 최신 기록 산출물 `Artifacts/Verification/20260814-112132-static/`.
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
- EditMode: 연결된 Unity Test Runner에서 `BombSwap.Core.Tests` 152개 통과, 실패/건너뜀/불확정 0. 기존 전투 규칙과 방 ID·범위·중복·spawn 안전·출구 경계·전체 연결성·두 퇴로·닫힌 유도 경로 테스트 포함.
- `Tools/Verify.ps1 -Tier Fast`: 실행 중인 동일 프로젝트 Editor 잠금 때문에 별도 batchmode로는 미실행. Unity 컴파일과 EditMode 테스트는 연결된 MCP로 수행.
- PlayMode: 공식 Unity MCP로 `GridSpaceTests` 18개 통과, 실패/건너뜀/불확정 0. 테스트 어셈블리 내부 리포터로 도메인 리로드 후 결과 확인.
- PlayMode 전체 회귀: `BombSwap.Unity.Tests` 52개 통과, 실패/건너뜀/불확정 0. room asset 연결, 기존 입력·전투·표현 생명주기와 방 전환 설정·단일 pending·마지막 방 무전환 포함.
- `PrototypeContentValidator`: 세 전투방의 Core 변환·고유 ID, 각 씬의 대응 room/spawn/장애물/전환 참조, Input Actions·폭탄·vitals·추격자·session·카메라·조명과 Build Settings 3방 순서 검증 오류 0.
- Scene View 다각도 시각 확인: 평행 통로의 두 세로 벽과 엇갈린 기둥의 다섯 장애물, 각 플레이어 spawn 표현을 식별.
- Development WebGL 3방 빌드 성공: 140,537,511 bytes, 69.669초, 오류 0. 설치된 Sentis·vendor·TextMeshPro 관련 기존 범주의 경고 359개가 보고됐다.
- 실제 Edge headless browser smoke: load, canvas focus, 기존 `W`·`Z`·`A`·`X`·`Esc` 입력과 접촉/폭발 source·적 사망·방 클리어를 관측하고 중앙 루프→평행 통로→엇갈린 기둥을 한 세션에서 전환, browser Console/page error 0.
- 최신 WebGL 검증 증거: `Artifacts/Verification/20260814-105806-web-connected/` (Git 제외). 빌드 후 자동 생성된 URP/ProjectSettings/Burst 부산물은 작업 diff에서 제거했다.
- 공통 정적 서버 리팩터링 뒤 기존 빌드 Edge headless 회귀: load, canvas focus, keyboard, 3방 시퀀스, resize, gameplay probe, browser Console 모두 통과. 증거 `Artifacts/Verification/20260814-111845-shared-server-browser/`.
