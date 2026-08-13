# Architecture Decision Records

ADR은 되돌리기 어렵거나 여러 시스템에 영향을 주는 기술 결정을 기록한다. 시스템 사용법은 `Docs/Systems/`에, 현재 진행 상황은 `Docs/Development/CurrentState.md`에 둔다.

## 상태

- `Proposed`: 검토 중
- `Accepted`: 현재 구현이 따라야 함
- `Superseded`: 다른 ADR로 대체됨
- `Rejected`: 검토했으나 채택하지 않음

## 파일 규칙

`NNNN-short-title.md` 형식을 사용한다. 기존 번호를 재사용하지 않는다.

## 템플릿

```markdown
# ADR-NNNN: 제목

- 상태: Proposed
- 날짜: YYYY-MM-DD
- 결정자: 프로젝트 팀

## 맥락
## 결정
## 대안
## 결과
## 검증 및 철회 조건
## 관련 문서
```
