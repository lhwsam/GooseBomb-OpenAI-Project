# 현재 프로젝트 상태

- 기준일: 2026-08-14
- 단계: 논리 플레이어 이동과 3D placeholder 보간 수직 슬라이스
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
- `GridPosition`, `GridState`, `IGameClock`, `ManualGameClock` 최소 Core 계약 구현.
- 논리 좌표, 지형·점유 불변식, 수동 시계 계약을 검증하는 EditMode 테스트 구현.
- 기본 십자 폭탄 정의, 설치, fuse, 폭발 셀, 벽 차단·파괴, 지연 연쇄를 소유하는 `BombSimulation` 구현.
- 동일 시각 폭발과 큰 시계 진행에서도 결정론적인 폭탄 사건 순서 구현.
- 정수 XZ 논리 격자와 Unity 3D 셀 중심을 변환하는 `GridSpace` 구현.
- 공식 Unity MCP PlayMode 실행 결과를 도메인 리로드 뒤에도 Console에서 확인하는 테스트 전용 리포터 구현.
- 게임 전용 `Gameplay/Move·PlaceBomb·SwapBomb·Pause` Input Actions와 Keyboard/Gamepad control scheme 구현.
- 장치 입력을 Core `PlayerCommand`로 변환하고 focus 상실 시 이동을 해제하는 `BombSwapInputReader` 구현.
- 11×9 격자, 경계 벽, 내부 장애물, 플레이어 placeholder, 탑다운 카메라를 가진 `TestSandbox` 씬 구현.
- Input Actions·TestSandbox·Build Settings를 재생성/검증하는 Editor builder와 validator 구현.
- 개발 WebGL에서 입력 사건을 브라우저 smoke에 전달하는 제한된 harness probe 구현.
- 주입 시계 cadence, 원자적 actor 점유 전이, 벽·폭탄 차단을 소유하는 `PlayerMovementSimulation` 구현.
- TestSandbox 유지 입력을 기본 5 cells/s 논리 이동과 placeholder Transform 보간에 연결.

## 현재 저장소 사실

- `BombSwap.Core`에는 UnityEngine 비참조 논리 격자와 주입식 수동 시계가 구현되어 있다.
- `GridState`는 미등록 셀을 `Void`로 취급하고 지형과 actor/bomb 점유를 소유한다. 점유는 바닥에만 존재하며 actor와 bomb의 설치 직후 동시 점유를 허용한다.
- `BombSimulation`은 활성 폭탄, 세션 내 고유 ID, fuse와 종류 독립적인 양수 지연 연쇄를 소유하고 읽기 전용 폭발 결과를 반환한다.
- 기본 십자 폭발은 `Void`·고정 벽에서 효과 없이 멈추고 파괴 벽은 해당 셀에 효과를 남긴 뒤 바닥으로 바꾸고 멈춘다.
- EditMode 테스트 89개가 하네스 발견성, 좌표·격자·시계, 폭탄 설치·폭발·벽·연쇄, 플레이어 명령과 이동 cadence·점유 전이 계약을 검증한다.
- `GridSpace`는 임의 원점·양수 셀 크기의 격자↔3D XZ 변환을 제공하고 Y를 표현 높이로 분리한다.
- PlayMode 전체 36개가 `GridSpace`, cardinal 입력 해석, 실제 Input System 키→명령→논리 셀→Transform 보간, 저작 장애물 차단, probe 초기화 순서, focus reset, 재구독 계약과 하네스 발견성을 검증한다.
- TestSandbox의 네 내부 장애물은 Transform/Collider와 별개인 명시적 논리 blocked cell로 저작되어 있다.
- Build Settings의 첫 enabled 씬은 `Assets/Game/Scenes/TestSandbox/TestSandbox.unity`이며 기존 SampleScene은 보존하되 비활성화했다.
- BombSwap 런타임은 기존 일반 템플릿을 수정하지 않고 게임 전용 `BombSwapInputActions.inputactions`를 사용한다.
- URP 17.5.0과 Input System 1.19.0이 설치되어 있다.
- WebGL platform quality는 Mobile 프로필을 사용한다.
- WebGL threads support는 꺼져 있고 data caching은 켜져 있다.
- Feel 등 vendor 에셋이 있으나 Core/first-party 구현과 아직 연결되지 않았다.

## 진행 중

- 없음. 다음 작업을 시작할 때 이 섹션을 갱신한다.

## 바로 다음 권장 작업

1. 폭탄 Core 설치·fuse·폭발 결과를 3D 표현에 연결해 첫 단독 전투 수직 슬라이스를 만든다.
2. 플레이어 설치자 식별과 폭탄 셀 이탈 후 재진입 차단 계약을 연결한다.
3. 기본 추격자와 자기 폭발 피해 후보를 연결해 프로토타입 가설 A의 첫 플레이 루프를 만든다.
4. 5 cells/s와 선형 보간의 조작감을 실제 플레이로 평가하고 튜닝 데이터를 분리한다.

## 알려진 위험과 미정

- 이동은 현재 기본 5 cells/s, step 시작 시 목적 셀 점유, 선형 보간을 사용한다. 최종 속도·곡선·셀 경계 감각은 플레이테스트 전까지 `Proposed`다.
- 현재 `GridState` 점유는 actor/bomb 종류만 표현하며 개체 식별과 설치자 한정 통과 권한은 아직 없다.
- 현재 폭탄 정의는 기본 십자 모양만 지원하며 쿨타임, 피해 후보, 직선·광역 폭탄은 아직 없다.
- 개발 WebGL 기준 빌드는 약 139.5 MB이며 현재 설치된 AI Inference·vendor 패키지와 셰이더가 빌드 크기와 시간을 크게 차지한다. 실제 배포 예산과 패키지 정리는 첫 수직 슬라이스 뒤 별도 결정이 필요하다.
- AI Navigation, AI Inference, Visual Scripting 등 설치 패키지의 실제 사용 여부는 결정되지 않았다.
- TestSandbox 플레이어는 이동하지만 폭탄 설치·교체·pause 명령은 아직 실제 게임 상태를 바꾸지 않는다.
- 게임 전용 폭탄 ScriptableObject 스키마와 일반 방 콘텐츠 검증기는 아직 없다.
- 개발 browser probe의 `audio-unlocked`는 입력 수신 marker이며 실제 오디오 재생은 아직 검증하지 않았다.
- 게임패드 binding은 구조 검증만 완료했고 목표 기기 수동 플레이가 남아 있다.

## 최근 검증

- Git 작업 트리 기준선 확인: 작업 시작 전 clean.
- `Tools/Verify.ps1 -StaticOnly`: 통과. Markdown 링크, 스킬 4종, asmdef 5종, Core 금지 API 검사.
- `skill-creator` 공식 `quick_validate.py`: 프로젝트 스킬 4종 모두 통과.
- PowerShell AST parse와 `node --check Tools/WebGLSmoke.mjs`: 통과.
- 하네스 C# 3개를 Unity 6000.5.3f1 설치 어셈블리 기준으로 외부 컴파일: 경고 0, 오류 0.
- WebGL smoke의 입력 반응·지연 이벤트 fixture를 설치된 Edge headless에서 실행: load, canvas focus, keyboard, resize, gameplay probe, Console 모두 통과.
- Fast 잠금 보호: 실행 중 Editor의 `Temp/UnityLockfile`을 감지해 종료 코드 3과 summary를 기록하는 동작 확인.
- Unity Editor refresh로 신규 first-party 폴더, asmdef, C#의 `.meta` 생성 확인.
- 전체 Markdown 내부 링크와 신규 asmdef JSON 정적 검사: 통과.
- 신규 asmdef 5개 JSON 파싱, 이름/참조 구조 정적 검사: 통과.
- 루트 AGENTS 크기: 약 9 KB로 Codex 기본 합산 제한 32 KiB 이내.
- 공식 Unity MCP 연결과 활성 씬 `Assets/Scenes/SampleScene.unity` 확인.
- Unity Editor import/compile: 격자·시계·폭탄 Core, Unity 좌표 어댑터와 테스트 스크립트 임포트 후 Console 오류 0.
- EditMode: 연결된 Unity Test Runner에서 `BombSwap.Core.Tests` 89개 통과, 실패/건너뜀/불확정 0.
- `Tools/Verify.ps1 -Tier Fast`: 실행 중인 동일 프로젝트 Editor 잠금 때문에 별도 batchmode로는 미실행. Unity 컴파일과 EditMode 테스트는 연결된 MCP로 수행.
- PlayMode: 공식 Unity MCP로 `GridSpaceTests` 18개 통과, 실패/건너뜀/불확정 0. 테스트 어셈블리 내부 리포터로 도메인 리로드 후 결과 확인.
- PlayMode 전체 회귀: `BombSwap.Unity.Tests` 36개 통과, 실패/건너뜀/불확정 0. 실제 입력 이동·장애물 차단, WebGL probe 초기화 순서와 기존 하네스 smoke 포함.
- `PrototypeContentValidator`: 게임 전용 Input Actions, TestSandbox 이동 controller·blocked cells·probe 참조, 카메라·조명, Build Settings 검증 통과.
- TestSandbox Scene View 시각 확인: 11×9 격자, 경계 벽, 네 장애물, 플레이어 placeholder가 탑다운 구도에서 식별 가능.
- Development WebGL 이동 빌드: `Assets/Game/Scenes/TestSandbox/TestSandbox.unity` 단일 씬으로 성공. 최종 증분 빌드 139,607,212 bytes, 42.17초, 오류 0, 경고 3개. 경고는 TextMeshPro 대형 메서드 분할 안내다.
- 실제 Edge headless browser smoke: load, canvas focus, 입력 구독 완료 `probe-ready`, Core 이동까지 W 유지, Z/X/Esc×2, resize, `move/place-bomb/swap-bomb/pause-resume/audio-unlocked` 관측, browser Console 모두 통과.
- 검증 증거: `Artifacts/Verification/20260814-070658-web-connected/` (Git 제외). 같은 작업의 실패 산출물은 probe 초기화 순서와 고정 입력 지연 문제의 진단 근거로 보존했다.
