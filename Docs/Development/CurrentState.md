# 현재 프로젝트 상태

- 기준일: 2026-08-14
- 단계: 프로토타입 Core 기반 구현
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

## 현재 저장소 사실

- `BombSwap.Core`에는 UnityEngine 비참조 논리 격자와 주입식 수동 시계가 구현되어 있다.
- `GridState`는 미등록 셀을 `Void`로 취급하고 지형과 actor/bomb 점유를 소유한다. 점유는 바닥에만 존재하며 actor와 bomb의 설치 직후 동시 점유를 허용한다.
- EditMode 테스트 30개가 하네스 발견성, 좌표 값 계약, 격자 지형·점유 불변식, 수동 시계 단조 증가를 검증한다.
- Build Settings에는 `Assets/Scenes/SampleScene.unity`만 등록되어 있다.
- 기존 Input Actions는 일반 템플릿 액션 중심이며 게임 전용 `Move`, `PlaceBomb`, `SwapBomb`, `Pause` 계약으로 정리되지 않았다.
- URP 17.5.0과 Input System 1.19.0이 설치되어 있다.
- WebGL platform quality는 Mobile 프로필을 사용한다.
- WebGL threads support는 꺼져 있고 data caching은 켜져 있다.
- Feel 등 vendor 에셋이 있으나 Core/first-party 구현과 아직 연결되지 않았다.

## 진행 중

- 없음. 다음 작업을 시작할 때 이 섹션을 갱신한다.

## 바로 다음 권장 작업

1. 기본 십자 폭탄의 설치, fuse, 벽 차단, 폭발 셀 계산을 Core 규칙과 EditMode 테스트로 구현한다.
2. 논리 격자와 3D XZ 월드 좌표를 연결하는 Unity 어댑터를 구현하고 경계값을 검증한다.
3. Prototype/TestSandbox 씬과 게임 전용 Input Actions를 Unity Editor에서 안전하게 구성한다.
4. 폭탄 Core 결과를 Unity Runtime과 3D 표현에 연결해 첫 단독 수직 슬라이스를 만든다.

## 알려진 위험과 미정

- 정확한 simulation step, 연속 이동 감각, 셀 경계 정책은 미정이다.
- 현재 `GridState` 점유는 actor/bomb 종류만 표현하며 개체 식별과 설치자 한정 통과 권한은 아직 없다.
- WebGL 성능/다운로드 예산은 빈 기준 빌드와 첫 수직 슬라이스 측정 후 확정해야 한다.
- AI Navigation, AI Inference, Visual Scripting 등 설치 패키지의 실제 사용 여부는 결정되지 않았다.
- 프로토타입 씬, 게임 전용 ScriptableObject 스키마, 콘텐츠 검증기는 아직 없다.
- 첫 플레이 가능한 수직 슬라이스 전에는 브라우저 gameplay probe가 없으므로 실제 `Web` tier는 통과할 수 없다.

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
- Unity Editor import/compile: 신규 Core와 테스트 스크립트 임포트 후 Console 오류 0.
- EditMode: 연결된 Unity Test Runner에서 `BombSwap.Core.Tests` 30개 통과, 실패/건너뜀/불확정 0.
- `Tools/Verify.ps1 -Tier Fast`: 실행 중인 동일 프로젝트 Editor 잠금 때문에 별도 batchmode로는 미실행. Unity 컴파일과 EditMode 테스트는 연결된 MCP로 수행.
- PlayMode: Unity 생명주기 또는 씬 연결 변경이 없어 이번 Core 작업에서는 미실행.
- WebGL 빌드: 이 구성 작업에서는 미실행.
