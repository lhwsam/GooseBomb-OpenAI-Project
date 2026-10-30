# 패키지 인벤토리와 사용 정책

- 기준: `Packages/manifest.json`과 vendor 경로, 2026-08-23
- 상태: 설치 사실은 `Accepted`, 실제 게임 사용은 별도 표기

| 패키지 | 버전 | 현재 역할 | 프로토타입 정책 |
|---|---:|---|---|
| Universal RP | 17.5.0 | 3D 렌더 파이프라인 | 사용. WebGL Mobile 품질 기준 |
| Input System | 1.19.0 | 입력 | 사용. InputReader 어댑터 뒤에 한정 |
| Test Framework | 1.7.0 | EditMode/PlayMode 테스트 | 사용 |
| UniTask | Git dependency | async 유틸리티 | 필요 시 PlayerLoop 경로만. Core 비참조 |
| AI Navigation | 2.0.13 | 설치됨 | 실제 필요 확인 전 권위 경로 시스템으로 채택하지 않음 |
| AI Inference | 2.6.1 | 설치됨 | 프로토타입 적 런타임 AI에는 사용하지 않음 |
| Unity AI Assistant | 2.17.0-pre.1 | 개발 도구 | 게임 플레이어 빌드 의존으로 사용하지 않음 |
| uGUI | 2.5.0 | UI 가능성 | 프로토타입 HUD 구현 시 선택 |
| Visual Scripting | 1.9.11 | 설치됨 | first-party 핵심 규칙에 사용하지 않음 |
| Timeline | 1.8.12 | 설치됨 | 보스/연출 필요가 확인될 때 검토 |
| Unity Pipeline | 0.9.1-exp.1 | 개발 도구 | Unity CLI `unity command`로 열린 Editor를 제어. Editor 전용, 게임 코드·asmdef에서 참조하지 않음 |
| DOTween Core | `Assets/Plugins/Demigiant/DOTween`, `DemiLib` | UI 표현 tween | 공식 원본과 고지 파일을 유지한 무료 Core만 Git 추적 |
| DOTween Pro | 로컬 Asset Store extension | 사용하지 않음 | Git·공개 기록 제외. 각 작업자 유효 license 필요 |
| Feel | 로컬 Asset Store extension | 제거됨 | Git·공개 기록 제외. 필요 시 각 작업자가 직접 설치하고 어댑터 뒤에서만 사용 |

`Assets/ThirdParty`, `Assets/Feel`, DOTween Pro의 공급자 원본은 Unity Package Manager 패키지가 아니며 Git에서 제외한다. 무료 DOTween Core는 공급자의 재배포 조건에 맞춰 원본과 copyright/readme를 유지한다. 로컬 에셋 설치, 공개 대체 UI와 참조 검증은 [서드파티 자산과 로컬 패키지](../Systems/ThirdPartyAssets.md) 및 [ADR-0009](../ADR/0009-Local-ThirdParty-Asset-Distribution.md)를 따른다.

## Unity Pipeline 도입 기록 (2026-10-24)

- 문제: 열린 Editor 자동화가 `Artifacts/Automation/setup-request.txt` 폴링(`SetupBridge`)에 의존해, 새 Editor 스크립트를 컴파일해야 실행되고 Editor 창이 비활성이면 컴파일이 멈췄다.
- 선택: `com.unity.pipeline`을 설치해 `unity command`(C# `eval` 포함)로 열린 Editor를 직접 제어한다. `SetupBridge`는 예비 경로로 유지한다.
- 대안: 기존 파일 폴링 유지, batchmode `-executeMethod`(열린 Editor와 동시 실행 불가).
- WebGL·빌드 크기: Editor 전용 개발 도구로 플레이어 빌드 의존이 없다고 예상한다. 도입 시 WebGL 빌드로 확인한다.
- 위험: experimental(`-exp`) 버전이라 API와 명령 이름이 바뀔 수 있다.
- 롤백: `Packages/manifest.json`에서 `"com.unity.pipeline"` 한 줄을 지우고 Editor가 `packages-lock.json`을 다시 해석하게 한다.

## 변경 규칙

- 새 패키지는 현재 문제와 대안, WebGL 지원, 빌드 크기, 유지보수 비용을 기록한 뒤 추가한다.
- 버전 변경은 `Docs/Migrations/` 계획을 만들고 한 번에 한 축만 이동한다.
- 단순히 설치되어 있다는 이유로 패키지 API를 Core나 콘텐츠 계약에 포함하지 않는다.
- 제거 전에는 코드, asmdef, 씬/프리팹, ProjectSettings, define symbol 사용을 모두 검색한다.
- Git URL 의존성은 재현 가능한 revision 고정 필요성을 마이그레이션/릴리스 전에 재검토한다.
