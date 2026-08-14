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

## 첫 기본 전투 세션

- 첫 1~3명 내부 세션은 [첫 기본 전투 관찰 프로토콜](../Playtesting/FirstCombatProtocol.md)의 영상·관찰표·직후 인터뷰를 주 증거로 사용한다.
- 현재 WebGL harness probe는 자동 smoke의 사건 존재 확인용이며 플레이어 의도, 전체 행동 횟수 또는 재미를 판정하지 않는다.
- 원본 증거는 `Artifacts/Playtests/`에 두고 익명화한 판단 요약만 `Docs/Playtesting/Results/`에 보존한다.
- 반복 수기 기록이 실제 분석을 방해한다는 증거가 생길 때 전체 사건 수·셀·논리 시각을 가진 구조화 recorder를 별도 작업으로 검토한다.

## 분석 주의

- 적은 샘플을 통과/실패 수치로 과해석하지 않는다.
- 설치 횟수만으로 재미를 판단하지 않고 위치 선택, 대기, 인터뷰와 함께 본다.
- 계측 기준이 바뀌면 이전 데이터와 직접 비교 가능한지 기록한다.
