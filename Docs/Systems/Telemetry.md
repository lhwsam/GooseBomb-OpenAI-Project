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

## 보스 프로토콜 상세 marker

Development WebGL probe는 기존 존재 확인 marker와 별도로 적용된 보스 피해와 보스 패턴 피격을 다음 이름에 담는다.

```text
boss-damaged-phase-<phase>-state-<state>-source-<source>-definition-<bomb-id>-health-<remaining>
boss-player-damaged-phase-<phase>-pattern-<pattern>-health-<remaining>
```

- phase는 `one`, `two`, `last-stand`, state는 `telegraph`, `execute`, `recovery`다.
- source는 `player-bomb` 또는 `self-destruct`이며 폭탄 정의는 적용된 `BombId`와 설치·폭발 사건을 연결해 기록한다.
- 분석기는 전투 시간, phase/state/source/정의별 적중, 과열과 보스 패턴 피격을 재구성한다.
- 폭탄 정의 교대는 성공 적중 순서만 뜻한다. 교체 의도, 실패한 설치, 자폭병 유도 의도와 parity 안전 칸 재사용은 관찰 기록이 소유한다.
- 상세 계약과 하위 로그 호환성은 [보스 플레이테스트 계측 수직 슬라이스](../Development/BossPlaytestTelemetrySlice.md)를 따른다.

## 투척병 전용 검증 marker

Development WebGL의 독립 투척병 씬은 다음 표식으로 규칙 연결 순서를 확인한다.

```text
thrower-cell-x-<x>-z-<z>
thrower-track-moved
thrower-telegraph-x-<x>-z-<z>
thrower-bomb-launched
thrower-bomb-armed-definition-prototype-thrower-blocker
thrower-bomb-detonated
thrower-bomb-detonated-by-chain
thrower-died
```

- `telegraph` 좌표는 매 volley에서 잠긴 서로 다른 저작 목표 3개를 각각 기록한다. 첫 volley만 기록하고 중단하지 않으므로 다음 사격 anchor에서 가장 가까운 압박점 1개가 유지되고 측면 2개가 바뀌는지도 관찰할 수 있다. `launched`와 `armed`도 성공적인 표준 volley에서는 각각 3번 발생하며, `armed`는 표현 비행이 아니라 공용 `BombSimulation` 착탄 성공을 뜻한다.
- `detonated-by-chain`은 최종 `BombExplosion.Cause == Chain`일 때만 기록한다.
- 이 표식은 자동화 동기화용이다. 플레이어가 예고를 실제로 읽었는지, 피하거나 연쇄하려는 의도가 있었는지, 압박이 재미있는지는 영상·관찰·인터뷰가 소유한다.

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
