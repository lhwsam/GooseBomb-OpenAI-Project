# 현재 프로젝트 상태

- 기준일: 2026-08-14
- 단계: 프로토타입 개발 준비 / 기술 기반 구성
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

## 현재 저장소 사실

- 게임플레이 first-party C# 구현은 아직 없다.
- 자동 테스트 구현은 아직 없다.
- Build Settings에는 `Assets/Scenes/SampleScene.unity`만 등록되어 있다.
- 기존 Input Actions는 일반 템플릿 액션 중심이며 게임 전용 `Move`, `PlaceBomb`, `SwapBomb`, `Pause` 계약으로 정리되지 않았다.
- URP 17.5.0과 Input System 1.19.0이 설치되어 있다.
- WebGL platform quality는 Mobile 프로필을 사용한다.
- WebGL threads support는 꺼져 있고 data caching은 켜져 있다.
- Feel 등 vendor 에셋이 있으나 Core/first-party 구현과 아직 연결되지 않았다.

## 진행 중

- 없음. 다음 작업을 시작할 때 이 섹션을 갱신한다.

## 바로 다음 권장 작업

1. 빠른/전체/WebGL 검증을 실행하는 개발 하네스와 프로젝트 스킬을 설계·구현한다.
2. `GridPosition`, `GridState`, 주입 시계의 최소 Core 계약과 EditMode 테스트를 만든다.
3. 기본 십자 폭탄 단독 수직 슬라이스를 Core→Unity→3D 표현 순서로 연결한다.
4. Prototype/TestSandbox 씬과 게임 전용 Input Actions를 Unity Editor에서 안전하게 구성한다.

## 알려진 위험과 미정

- asmdef JSON과 참조 구조는 정적 검증을 통과했지만 Unity Editor import/compile은 아직 확인하지 못했다.
- 정확한 simulation step, 연속 이동 감각, 셀 경계 정책은 미정이다.
- WebGL 성능/다운로드 예산은 빈 기준 빌드와 첫 수직 슬라이스 측정 후 확정해야 한다.
- AI Navigation, AI Inference, Visual Scripting 등 설치 패키지의 실제 사용 여부는 결정되지 않았다.
- 프로토타입 씬, 게임 전용 ScriptableObject 스키마, 콘텐츠 검증기는 아직 없다.

## 최근 검증

- Git 작업 트리 기준선 확인: 작업 시작 전 clean.
- 전체 Markdown 42개(이번 작업 신규 40개) 내부 링크 검사: 통과.
- 신규 asmdef 5개 JSON 파싱, 이름/참조 구조 정적 검사: 통과.
- 루트 AGENTS 크기: 약 9 KB로 Codex 기본 합산 제한 32 KiB 이내.
- Unity import/compile: 실행 중인 Editor가 MCP에 연결되지 않았고 동일 프로젝트의 두 번째 Unity 인스턴스를 띄우지 않기 위해 미실행. 다음 Editor refresh 후 확인 필요.
- EditMode/PlayMode 테스트: 테스트 코드가 없어 미실행.
- WebGL 빌드: 이 구성 작업에서는 미실행.
