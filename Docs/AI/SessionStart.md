# AI 세션 시작 절차

새 세션은 이전 대화 기억이 아니라 저장소 근거로 현재 상태를 재구성한다.

## 1. 공통 문맥

1. 루트 `AGENTS.md`를 읽는다.
2. `Docs/INDEX.md`에서 작업별 읽기 경로를 찾는다.
3. GDD와 프로토타입 부록에서 관련 게임 의도와 가설을 읽는다.
4. `Docs/Architecture/Overview.md`와 관련 ADR을 읽는다.
5. `Docs/Development/CurrentState.md`를 읽는다.

## 2. 저장소 사실

- `git status --short --branch`로 사용자 변경을 확인한다.
- `ProjectSettings/ProjectVersion.txt`에서 Unity 버전을 확인한다.
- `Packages/manifest.json`과 관련 asmdef를 확인한다.
- 변경 대상 파일의 기존 패턴과 사용처를 검색한다.
- Unity가 열려 있다면 대상 프로젝트, compile 상태, Console 기준선을 확인한다.

## 3. 작업 계약

`TaskContract.md` 형식으로 다음을 내부적으로 확정한다.

- 사용자/플레이어 관점 목표.
- 바꾸는 파일과 바꾸지 않는 파일.
- 관련 게임 가설과 기술 불변식.
- 완료 조건과 검증 증거.
- 미정 사항 중 보수적으로 가정할 수 있는 것과 질문이 필요한 것.

## 4. 작업 중

- 작은 단위로 구현하고 관련 검증을 즉시 실행한다.
- 직렬화/패키지/ProjectSettings 변경 전 영향과 롤백을 확인한다.
- 문서와 구현의 계약이 달라지면 같은 변경에서 갱신한다.
- 장시간 작업은 현재 가정, 완료/남은 항목, 마지막 검증 결과를 commentary에 남긴다.

## 5. 종료

- 최종 diff를 자체 리뷰한다.
- 변경 위험에 맞는 테스트를 실행한다.
- `CurrentState.md`를 현재 스냅샷으로 갱신한다.
- `HandoffTemplate.md` 구조로 결과를 남긴다.
