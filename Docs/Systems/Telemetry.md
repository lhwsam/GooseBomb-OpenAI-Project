# 프로토타입 계측

- 상태: 이벤트 구조 `Proposed`
- 설계 원본: `ProtoType_v0.2.md` 4~7장
- 코드 소유: Core 도메인 이벤트, Unity 세션 수집/내보내기

## 목적

자동 테스트로 판단할 수 없는 재미 가설을 행동 데이터와 인터뷰로 검토한다. 계측은 플레이를 판정하는 권위 원본이 아니라 관찰 보조 수단이다.

## 최소 세션 정보

- 익명 session ID.
- 게임/콘텐츠 버전.
- run seed.
- 플랫폼, 브라우저 계열, 해상도 등 필요한 최소 환경.
- 세션 시작/종료 이유와 플레이 시간.

개인정보나 불필요한 장치 식별자는 수집하지 않는다. 외부 분석 서비스를 추가하기 전 별도 결정과 동의를 거친다.

## 핵심 이벤트

| 이벤트 | 주요 필드 | 연결 가설 |
|---|---|---|
| BombPlaced | time, bombId, slot, cell, nearby enemies | A, B |
| BombExploded | origin, affected cells, cause, chain depth | A, B |
| BombSwapped | from, to, cooldown state | B, C |
| DamageApplied | source, target type, remaining hits | D |
| PlayerDamaged | source, selfBomb flag, cell | A |
| RoomEntered/Cleared | roomId, node type, duration | E |
| RunGraphCreated | seed, node summary, critical path | E |
| BossPattern | pattern, phase, result | F |
| RunEnded | reason, duration, progress | 전체 |

## 원칙

- 이벤트 이름과 필드는 버전 관리한다.
- Core 사건에서 생성 가능한 값과 표현 계층 로그를 구분한다.
- 동일 사건을 UI, VFX, telemetry가 각각 다시 계산하지 않는다.
- 로깅 실패가 게임 진행을 막지 않는다.
- WebGL에서는 로컬 구조화 로그로 시작하고 외부 전송은 후순위다.

## 분석 주의

- 적은 샘플을 통과/실패 수치로 과해석하지 않는다.
- 설치 횟수만으로 재미를 판단하지 않고 위치 선택, 대기, 인터뷰와 함께 본다.
- 계측 기준이 바뀌면 이전 데이터와 직접 비교 가능한지 기록한다.
