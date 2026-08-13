# 패키지 인벤토리와 사용 정책

- 기준: `Packages/manifest.json`, 2026-08-14
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
| Feel | Asset 폴더 | 피드백/VFX | Presentation 어댑터 뒤에서만 사용, vendor 수정 금지 |

## 변경 규칙

- 새 패키지는 현재 문제와 대안, WebGL 지원, 빌드 크기, 유지보수 비용을 기록한 뒤 추가한다.
- 버전 변경은 `Docs/Migrations/` 계획을 만들고 한 번에 한 축만 이동한다.
- 단순히 설치되어 있다는 이유로 패키지 API를 Core나 콘텐츠 계약에 포함하지 않는다.
- 제거 전에는 코드, asmdef, 씬/프리팹, ProjectSettings, define symbol 사용을 모두 검색한다.
- Git URL 의존성은 재현 가능한 revision 고정 필요성을 마이그레이션/릴리스 전에 재검토한다.
