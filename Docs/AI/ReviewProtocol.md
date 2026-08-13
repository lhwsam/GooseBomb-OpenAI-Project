# 자체 리뷰와 독립 리뷰 프로토콜

- 상태: `Accepted`

## 언제 독립 리뷰를 사용하는가

- Core와 Unity 경계를 함께 바꾸는 작업.
- 폭발, 쿨타임, 피해, 던전 seed 등 핵심 규칙.
- 입력, WebGL, 성능, 패키지, Unity 버전 변경.
- 마일스톤 완료와 복잡한 회귀 수정.

작고 지역적인 문서/코드 변경은 구현 담당의 자체 리뷰로 충분하다.

## 역할

- 구현/통합 담당: 유일한 기본 작성자, 요구사항 해석, 수정, 최종 검증.
- 게임 규칙 리뷰: GDD와 불변식, 경계 상황, 결정성.
- Unity/WebGL 리뷰: 생명주기, 직렬화, 입력, 빌드, 프레임/할당 위험.
- 테스트/유지보수 리뷰: 테스트 누락, API와 책임, 문서 drift.

리뷰 역할은 기본적으로 읽기 전용이다. 병렬 작성이 필요하면 파일 소유권이 겹치지 않는 독립 작업만 배정한다.

## 순서

1. 작업 계약과 예상 테스트를 확정한다.
2. 구현 담당이 테스트/구현을 완료하고 Fast 검증을 실행한다.
3. 리뷰 중인 diff를 고정한다.
4. 필요한 관점의 독립 리뷰를 병렬로 실행한다.
5. 구현 담당이 중복·오탐·우선순위를 분류하고 수정한다.
6. Full/Web 검증과 최종 자체 리뷰를 실행한다.
7. 문서, CurrentState, 인계를 갱신한다.

## finding 형식

```text
Severity: Blocker | Major | Minor | Suggestion
Location: file and tight line/asset
Problem: observable defect or contract violation
Evidence/Reproduction: exact path to verify
Impact: player, build, data, performance, maintainability
Contract: GDD/System/ADR/test being violated
Safe fix: smallest recommended correction
Regression test: how to prevent recurrence
```

취향, 일반론, 기계적 포맷은 finding으로 과도하게 만들지 않는다. CI/validator로 검사할 수 있는 항목은 하네스로 옮긴다.
