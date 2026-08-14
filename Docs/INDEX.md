# Bomb Swap 문서 인덱스

이 문서는 사람과 AI 모두의 공통 시작점이다. 게임 기획의 의도, 기술 결정, 현재 구현 상태가 서로 다른 파일에 존재하므로 작업 목적에 맞는 최소 문서만 읽는다.

## 프로젝트 한 줄 요약

정수 XZ 논리 격자 위에서 지연 폭탄 두 종류를 교대 사용해 미래의 위험 공간을 설계하는 Unity 3D WebGL 탑다운 룸 액션 로그라이트다.

## 문서 권위와 상태

| 영역 | 권위 원본 | 설명 |
|---|---|---|
| 게임 의도와 가설 | [GameDesign](GameDesign/) | 플레이어 경험, 규칙 의도, 프로토타입 검증 기준 |
| 채택한 기술 결정 | [ADR](ADR/) | 선택, 이유, 대안, 결과 |
| 시스템 계약 | [Systems](Systems/) | 책임, 상태, 데이터 흐름, 불변식, 테스트 포인트 |
| 전체 기술 구조 | [Architecture](Architecture/) | 계층, 의존 방향, 런타임 흐름 |
| 현재 진행 상태 | [CurrentState](Development/CurrentState.md) | 완료·진행·미구현, 알려진 문제, 바로 다음 작업 |
| 구현 완료 기준 | [Definition of Done](Development/DefinitionOfDone.md) | 변경 종류별 필수 검증 |
| 검증 방법 | [Testing](Testing/) | EditMode, PlayMode, 콘텐츠, WebGL 검증과 하네스 |
| 플레이테스트 운영 | [Playtesting](Playtesting/) | 관찰 프로토콜, 익명 세션 기록, 유지·변경·제거 판정 |
| WebGL 제약 | [WebGL](WebGL/) | 빌드, 호스팅, 성능, 브라우저 호환 |
| AI 작업 절차 | [AI](AI/) | 세션 시작, 작업 계약, 리뷰, 인계 |
| 실제 튜닝 값 | 검증된 ScriptableObject | 코드와 문서는 값의 의도와 범위를 설명 |

문서 상태 표기는 다음을 사용한다.

- `Accepted`: 프로토타입 기준으로 채택되어 구현이 따라야 한다.
- `Proposed`: 구현 또는 플레이테스트 전 가설이며 변경 가능하다.
- `Deferred`: 프로토타입 범위 밖이다.
- `Superseded`: 더 새 문서나 ADR로 대체되었다.

## 기본 읽기 순서

1. 저장소 루트 [AGENTS.md](../AGENTS.md)
2. 이 문서
3. [GDD v0.2](GameDesign/GDD_v0.2.md)
4. [프로토타입 검증 부록 v0.2](GameDesign/ProtoType_v0.2.md)
5. [기술 아키텍처 개요](Architecture/Overview.md)
6. [현재 프로젝트 상태](Development/CurrentState.md)
7. 변경 대상과 관련된 Systems, ADR, Testing 문서

## 작업별 최소 읽기 경로

| 작업 | 추가로 읽을 문서 |
|---|---|
| 입력·플레이어 명령 | `Systems/InputAndCommands.md`, `Architecture/RuntimeFlow.md`, `WebGL/BrowserTestMatrix.md` |
| 폭탄·폭발 규칙 | `Systems/BombAndExplosion.md`, `Systems/GridAndMovement.md`, ADR-0001~0003 |
| 두 폭탄·쿨타임 | `Systems/WeaponSlotsAndCooldown.md`, `ProtoType_v0.2.md` 가설 B·C |
| 첫 폭탄 보상·런 로드아웃 | `Development/DungeonBombRewardSlice.md`, `Systems/WeaponSlotsAndCooldown.md`, ADR-0008 |
| 플레이어 피해 | `Systems/DamageAndInvulnerability.md` |
| 적 AI | `Systems/EnemyBehavior.md`, `Systems/GridAndMovement.md` |
| 방 제작 | `Systems/RoomAuthoring.md`, `Systems/BombAndExplosion.md` |
| 던전 생성 | `Systems/DungeonGeneration.md`, ADR-0003 |
| 보스 | `Systems/BossBattle.md` |
| 플레이테스트 준비·분석 | `Playtesting/README.md`, `Playtesting/FirstCombatProtocol.md`, `Systems/Telemetry.md` |
| 입력 변경 | `Architecture/RuntimeFlow.md`, `WebGL/BrowserTestMatrix.md` |
| WebGL 빌드·성능 | `WebGL/` 전체, ADR-0004 |
| 검증 실행 | `Testing/VerificationHarness.md`, `Testing/TestMatrix.md` |
| 패키지·Unity 업그레이드 | `Development/PackageInventory.md`, `Migrations/` |
| AI 세션 인계 | `AI/SessionStart.md`, `AI/HandoffTemplate.md` |

## 디렉터리 지도

```text
Docs/
  GameDesign/     게임 경험과 검증 가설
  Architecture/   전체 구조와 의존 방향
  Systems/        시스템별 계약
  ADR/            되돌리기 어려운 기술 결정
  Development/    로드맵, 현재 상태, 완료 기준, 패키지
  Testing/        자동·수동 검증 전략
  Playtesting/    관찰 플레이테스트 운영과 결과 형식
  WebGL/          빌드, 브라우저, 성능 제약
  AI/             세션 연속성과 작업 절차
  Migrations/     버전·데이터 변경 계획과 결과
```

## 문서 갱신 규칙

- 문서와 구현이 함께 바뀌는 변경은 같은 작업에서 갱신한다.
- `CurrentState.md`는 과거 기록을 누적하지 않고 현재 사실만 남긴다.
- 되돌리기 어렵거나 여러 시스템에 영향을 주는 결정은 ADR을 추가한다.
- 확정되지 않은 수치에는 `Proposed`를 표시한다.
- 폐기된 문서는 삭제만 하지 말고 필요한 경우 `Superseded`와 대체 링크를 남긴다.
- 동일한 규칙을 여러 파일에 복제하지 말고 권위 원본으로 연결한다.
