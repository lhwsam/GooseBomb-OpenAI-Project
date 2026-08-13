# 프로젝트 스킬 인덱스

- 상태: `Accepted`

프로젝트 스킬은 반복 작업의 절차를 캡슐화한다. 저장소의 지속 규칙은 루트 `AGENTS.md`, 시스템 사실은 `Docs/`에 남기고 스킬은 특정 작업을 수행하는 순서와 도구 사용법에 집중한다.

## 저장 위치

공유 프로젝트 스킬은 `.agents/skills/<skill-name>/SKILL.md`에 둔다. Codex는 저장소 안에서 작업할 때 이 위치의 name/description을 발견하고 명시적 또는 암시적으로 스킬을 선택한다.

## 구현된 스킬

| 스킬 | 책임 | 상태 |
|---|---|---|
| [`bombswap-gameplay-change`](../../.agents/skills/bombswap-gameplay-change/SKILL.md) | GDD→Core test→Unity 연결→문서 갱신의 기능 변경 절차 | 구현됨 |
| [`bombswap-content-authoring`](../../.agents/skills/bombswap-content-authoring/SKILL.md) | 방/폭탄/적 정의 저작과 콘텐츠 검증 | 구현됨 |
| [`bombswap-webgl-verify`](../../.agents/skills/bombswap-webgl-verify/SKILL.md) | Fast/Full/Web 실행, WebGL build, 브라우저 smoke, 증거 수집 | 구현됨 |
| [`bombswap-playtest-review`](../../.agents/skills/bombswap-playtest-review/SKILL.md) | 계측 요약, 관찰/인터뷰, 가설 판정 보조 | 구현됨 |

명시적으로 사용할 때는 `$bombswap-gameplay-change`처럼 스킬 이름을 호출한다. description과 요청이 명확히 일치하면 Codex가 암시적으로 선택할 수 있다.

## 설계 원칙

- 스킬 하나는 명확한 작업 하나만 수행한다.
- trigger가 되는 description은 사용 시점과 비사용 시점을 구체적으로 적는다.
- 긴 배경 지식은 Docs 권위 문서로 연결하고 SKILL 본문을 중복 저장소로 만들지 않는다.
- 반복 가능한 검증과 외부 도구 호출은 script로 만들고 명확한 exit code와 산출물을 사용한다.
- Unity 직렬화 에셋 변경은 Editor/MCP 연결 확인과 후속 Console 검증을 절차에 포함한다.
- 스킬이 AGENTS 또는 시스템 계약을 우회하지 못한다.

스킬을 변경할 때는 현재 Codex 스킬 작성 지침을 다시 확인하고 `skill-creator`의 `quick_validate.py`를 실행한다. 정상 요청, 간접 요청, 불완전 요청, 비관련 요청, 안전 경계 요청으로 trigger와 결과를 검토한다.
