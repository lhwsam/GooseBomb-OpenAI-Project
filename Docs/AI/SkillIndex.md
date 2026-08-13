# 프로젝트 스킬 인덱스

- 상태: 스킬 설계 전 `Proposed`

프로젝트 스킬은 반복 작업의 절차를 캡슐화한다. 저장소의 지속 규칙은 루트 `AGENTS.md`, 시스템 사실은 `Docs/`에 남기고 스킬은 특정 작업을 수행하는 순서와 도구 사용법에 집중한다.

## 저장 위치

공유 프로젝트 스킬은 향후 `.agents/skills/<skill-name>/SKILL.md`에 둔다. 현재 이 저장소에는 프로젝트 전용 스킬이 아직 없다.

## 계획 후보

| 스킬 | 책임 | 상태 |
|---|---|---|
| `bombswap-gameplay-change` | GDD→Core test→Unity 연결→문서 갱신의 기능 변경 절차 | 미구현 |
| `bombswap-content-authoring` | 방/폭탄/적 정의 저작과 콘텐츠 검증 | 미구현 |
| `bombswap-webgl-verify` | Full 검증, WebGL build, 브라우저 smoke, 증거 수집 | 미구현 |
| `bombswap-playtest-review` | 계측 요약, 관찰/인터뷰, 가설 판정 보조 | 미구현 |

## 설계 원칙

- 스킬 하나는 명확한 작업 하나만 수행한다.
- trigger가 되는 description은 사용 시점과 비사용 시점을 구체적으로 적는다.
- 긴 배경 지식은 Docs 권위 문서로 연결하고 SKILL 본문을 중복 저장소로 만들지 않는다.
- 반복 가능한 검증과 외부 도구 호출은 script로 만들고 명확한 exit code와 산출물을 사용한다.
- Unity 직렬화 에셋 변경은 Editor/MCP 연결 확인과 후속 Console 검증을 절차에 포함한다.
- 스킬이 AGENTS 또는 시스템 계약을 우회하지 못한다.

구현 시 현재 Codex 스킬 작성 지침을 다시 확인하고, 각 스킬에 정상/오탐/비관련 요청의 trigger 검증을 수행한다.
