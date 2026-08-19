# 작업: 퇴로 차단 투척병 세로 슬라이스

- 문서 상태: `Accepted`
- 구현 상태: 독립 수직 슬라이스 `Implemented`, 메인 던전 편성 `Proposed`
- 수치 상태: `Proposed`

## 목표

- 플레이어가 투척병의 경고를 보고 다음 퇴로를 바꾸거나, 착탄한 적 폭탄을 자신의 폭발로 연쇄시킬 수 있다.
- 투척병을 메인 던전 카탈로그에 넣기 전에 전용 Lanes 플레이테스트 씬에서 단독 회귀 검증할 수 있다.

## 근거

- `Docs/GameDesign/CombatEnemyLevelBossImprovementProposal.md`의 `퇴로 차단 투척병 + Lanes 앵커`
- `Docs/Systems/EnemyBehavior.md`
- `Docs/Systems/BombAndExplosion.md`
- `Assets/Game/Runtime/Prototype/PrototypeGameSession.cs`

## 범위

- 변경 허용: `Assets/Game/Core`, `Assets/Game/Authoring`, `Assets/Game/Runtime`, `Assets/Game/Presentation`, `Assets/Game/Editor`, 관련 테스트·문서·프로토타입 콘텐츠
- 변경 금지: `Assets/Feel`, 서드파티 패키지, 현재 5개 던전 전투방의 밸런스와 카탈로그 편성
- 비목표: 최종 아트, 드롭 테이블, 추가 패시브, 메인 던전 출현 확률 튜닝

## 계약과 불변식

- 투척병은 정수 XZ 격자와 주입된 게임 시계만으로 판단한다.
- `Track -> Telegraph -> Recover` 상태를 사용한다.
- 현재 플레이어 셀이 아니라 최소 6개의 저작된 퇴로 차단 후보를 맨해튼 거리 오름차순·저작 동률 순서로 정렬한다. 가장 가까운 압박점 1개와 현재 사격 anchor index가 순환 선택한 측면 2개를 조합하되 직전 volley 미사용 후보를 우선해 연속 공격이 같은 세 칸을 반복하지 않게 한다.
- Telegraph 진입 시 세 목표를 함께 고정·표시하며 경고 중에는 재조준하지 않는다.
- 세 폭탄은 같은 시각에 서로 다른 포물선으로 비행한 뒤 공용 `BombSimulation`에 각각 배치한다. 따라서 플레이어 폭발과 같은 고정 연쇄 지연을 사용한다.
- 투척병은 자신의 폭탄에는 피해를 받지 않지만, 다른 소유자의 폭발에는 피해를 받는다.
- 투척병은 저작된 사격 앵커를 순환하고 한 번에 3발짜리 비행/활성 volley 하나만 유지한다. 세 폭탄이 모두 해결되기 전에는 다음 volley를 만들지 않는다.
- 착탄 셀이 이미 폭탄으로 점유되면 해당 발만 조용히 실패한다. 다른 두 발은 독립적으로 착탄하며 임의 재조준이나 대체 폭탄은 만들지 않는다.
- Core는 UnityEngine, Transform, Physics, Input System을 참조하지 않는다.

## 초기 Proposed 수치

- 이동: 1.0 cells/s
- 경고: 0.3초
- 비행: 0.45초
- 회복: 0.75초
- 폭탄: `prototype-thrower-blocker`, 퓨즈 1.5초, 십자 범위 1
- 공격당 폭탄: 서로 다른 목표 3발 동시 투척
- 체력: 1

## 완료 조건

- Core: 상태 전이, 세 목표 고정, 결정론적 이동, 앵커 순환, 단일 3발 volley, 발별 착탄 실패/해제 계약
- EditMode: 정상·동률·시계 역행·막힌 경로·중복 앵커·3발 대기/활성 추적 테스트
- Unity: 전용 정의 에셋, 적/경고 프리팹, 룸 정의, `ThrowerLanesPlaytest` 씬, 이동·경고·비행·착탄 표현
- PlayMode: 세션이 공용 폭탄 시뮬레이션으로 착탄/연쇄/피해/사망을 처리하는 통합 테스트
- WebGL: 전용 씬 스모크와 기존 11개 씬 회귀 스모크
- 문서: EnemyBehavior, RoomAuthoring, CurrentState, PrototypeRoadmap 갱신

## 위험과 롤백

- 직렬화 에셋은 `PrototypeContentBuilder`를 Unity Editor에서 실행해 생성·동기화한다.
- 전용 씬은 표준 Build Settings에서 제외하므로 기존 던전 진행에는 영향을 주지 않는다.
- 롤백 단위는 투척병 Core/Unity 코드, 전용 콘텐츠, 전용 씬, 문서로 한정한다.

## 구현·검증 결과

- Core `Track → Telegraph → Recover`, 거리·저작 순서 기반 압박점 1개와 사격 anchor별 측면 2개 순환, 세 목표 고정, 사격 anchor BFS·순환, 3발 volley와 발별 착탄 실패 계약을 구현했다.
- 전용 정의·폭탄·prefab·room asset과 `ThrowerLanesPlaytest.unity`를 Editor builder로 생성하고 validator로 고정했다.
- 전용 씬은 표준 enabled Build Settings 11개에 포함하지 않았고 메인 던전의 기존 다섯 전투방·밸런스·카탈로그도 바꾸지 않았다.
- 목표 후보와 volley가 모두 3개여서 연속 공격이 같은 세 칸을 반복하던 원인을 새 회귀 테스트의 `342/343` 실패로 먼저 재현했다. 후보를 6개로 확장하고 가장 가까운 압박점 1개와 직전 volley 미사용을 우선하는 측면 2개 순환으로 교정했다.
- 연결 Unity 최종 전체 EditMode `343/343`, PlayMode `134/134`가 통과했다. EditMode는 연속 volley 측면 교체와 한 발의 착탄 실패 뒤 나머지 두 발이 독립적으로 활성화·해결되는 계약을 포함한다.
- `Artifacts/Verification/20260820-063300-thrower-rotation-final-web/`의 전용 Development WebGL은 138,713,132 bytes·63.378초·오류 0·안내 경고 3건으로 성공했다. `Tools/ThrowerWebGLSmoke.mjs`의 `8/8` 검사는 첫 중앙·하단 좌우 예고 뒤 두 번째 중앙·상단·좌측 예고로의 측면 교체, 세 비행·공용 폭탄 착탄, 플레이어 폭탄의 `Chain` 기폭과 Console/page 오류 0을 확인했다.
- 전용 씬은 표준 Build Settings와 메인 던전 카탈로그 밖이므로 이번 반복 수정 뒤 표준 11씬 빌드는 다시 실행하지 않았다. 공유 런타임의 WebGL 컴파일은 전용 빌드가, 기존 던전 연결 회귀는 전체 PlayMode가 담당한다.
- 연결 테스트 증거는 실패 기준선 `Artifacts/Verification/ConnectedTests/20260819-211852-144.json`, 최종 `Artifacts/Verification/ConnectedTests/20260819-213125-705.json`·`Artifacts/Verification/ConnectedTests/20260819-213147-343.json`, 정적 증거 `Artifacts/Verification/20260820-063451-static/summary.json`이다.

## 남은 사람 판정

- 0.3초에 동시에 표시되는 세 예고가 위협적으로 느껴지면서도 각 착탄 셀을 읽고 실제 퇴로를 변경할 수 있는가.
- 1.5초 fuse·범위 1이 회피만 강제하지 않고 의도적 연쇄 선택도 만드는가.
- 1 cell/s 사격 anchor 순환에서 가장 가까운 1칸은 압박을 유지하고 바뀌는 측면 2칸은 반복감을 줄이는가. 추격자 조합이 불공정한 입장 공격 없이 충분히 압박하는가.
- 지지될 때만 기존 Lanes 또는 새 여섯 번째 전투방으로 메인 던전에 편성한다.
