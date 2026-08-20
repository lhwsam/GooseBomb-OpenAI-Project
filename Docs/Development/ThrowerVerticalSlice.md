# 작업: 퇴로 차단 투척병 세로 슬라이스

- 문서 상태: `Accepted`
- 구현 상태: 독립 수직 슬라이스·메인 던전 편성 `Implemented / WebGL Verified`
- 수치 상태: `Proposed`

## 목표

- 플레이어가 투척병의 경고를 보고 다음 퇴로를 바꾸거나, 착탄한 적 폭탄을 자신의 폭발로 연쇄시킬 수 있다.
- 전용 Lanes 플레이테스트 씬의 단독 회귀 경로를 유지하면서 같은 권위 room을 메인 던전에서도 검증한다.

## 근거

- `Docs/GameDesign/CombatEnemyLevelBossImprovementProposal.md`의 `퇴로 차단 투척병 + Lanes 앵커`
- `Docs/Systems/EnemyBehavior.md`
- `Docs/Systems/BombAndExplosion.md`
- `Assets/Game/Runtime/Prototype/PrototypeGameSession.cs`

## 범위

- 변경 허용: `Assets/Game/Core`, `Assets/Game/Authoring`, `Assets/Game/Runtime`, `Assets/Game/Presentation`, `Assets/Game/Editor`, 관련 테스트·문서·프로토타입 콘텐츠
- 변경 금지: `Assets/Feel`, 서드파티 패키지, Core 그래프 크기와 기존 네 전투방의 밸런스
- 비목표: 최종 아트, 드롭 테이블, 추가 패시브, 메인 던전 출현 확률 튜닝

## 계약과 불변식

- 투척병은 정수 XZ 격자와 주입된 게임 시계만으로 판단한다.
- `Track -> Telegraph -> Recover` 상태를 사용한다.
- 현재 플레이어 셀이 아니라 최소 6개의 저작된 퇴로 차단 후보를 맨해튼 거리 오름차순·저작 동률 순서로 정렬한다. 가장 가까운 압박점 1개와 현재 사격 anchor index가 순환 선택한 측면 2개를 조합하되 직전 volley 미사용 후보를 우선해 연속 공격이 같은 세 칸을 반복하지 않게 한다.
- Telegraph 진입 시 세 목표를 함께 고정·표시하며 경고 중에는 재조준하지 않는다.
- 세 폭탄은 같은 시각에 서로 다른 포물선으로 비행한 뒤 공용 `BombSimulation`에 각각 배치한다. 따라서 플레이어 폭발과 같은 고정 연쇄 지연을 사용한다.
- 투척병은 자신의 폭탄에는 피해를 받지 않지만, 다른 소유자의 폭발에는 피해를 받는다.
- 투척병은 저작된 사격 앵커를 순환하고 한 번에 3발짜리 비행/활성 volley 하나만 유지한다. 세 폭탄이 모두 해결되기 전에는 다음 volley를 만들지 않는다.
- staging spawn `(3,2)`은 사격 anchor 밖에 있고 네 잠재 출구에서 Manhattan 4칸 이상 떨어져야 한다. 첫 사격 anchor `(0,3)`까지 4칸을 Track한 뒤에만 Telegraph한다. 추격자 `(-2,2)`도 출구에서 4칸 이상 떨어지고 모든 초기 목표 폭발 footprint 밖이어야 한다. seed 0 Clockwise90 배정은 플레이어 `(4,0)`, 추격자 `(2,2)`, 투척병 `(2,-3)`, 첫 사격 anchor `(3,0)`이다.
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
- 전용 `ThrowerLanesPlaytest`는 표준 Build Settings에서 제외한다. 던전용 `TestSandboxThrower`만 기존 `TestSandboxLanes` 활성 슬롯을 대체한다.
- 편성 롤백은 카탈로그의 두 번째 entry와 enabled Build Settings를 Legacy Lanes로 되돌리고 `TestSandboxThrower`를 제외하는 단위다. 투척병 규칙과 독립 테스트 자산은 별도로 보존할 수 있다.

## 구현·검증 결과

- Core `Track → Telegraph → Recover`, 별도 staging→첫 사격 anchor 선행 이동, 거리·저작 순서 기반 압박점 1개와 사격 anchor별 측면 2개 순환, 세 목표 고정, 사격 anchor BFS·순환, 3발 volley와 발별 착탄 실패 계약을 구현했다.
- 전용 정의·폭탄·prefab·room asset과 `ThrowerLanesPlaytest.unity`를 Editor builder로 생성하고 validator로 고정했다.
- 독립 `ThrowerLanesPlaytest`는 표준 enabled Build Settings 밖에 유지했다. 메인 편성은 여섯 번째 방을 추가하지 않고 기존 Lanes entry를 `prototype-combat-thrower` / `TestSandboxThrower`로 교체해 다섯 entry와 run 길이를 유지했다. Legacy Lanes 자산·씬은 독립 테스트로 보존했다.
- 목표 후보와 volley가 모두 3개여서 연속 공격이 같은 세 칸을 반복하던 원인을 새 회귀 테스트의 `342/343` 실패로 먼저 재현했다. 후보를 6개로 확장하고 가장 가까운 압박점 1개와 직전 volley 미사용을 우선하는 측면 2개 순환으로 교정했다.
- 연결 Unity 최종 전체 EditMode `343/343`, PlayMode `134/134`가 통과했다. EditMode는 연속 volley 측면 교체와 한 발의 착탄 실패 뒤 나머지 두 발이 독립적으로 활성화·해결되는 계약을 포함한다.
- `Artifacts/Verification/20260820-063300-thrower-rotation-final-web/`의 전용 Development WebGL은 138,713,132 bytes·63.378초·오류 0·안내 경고 3건으로 성공했다. `Tools/ThrowerWebGLSmoke.mjs`의 `8/8` 검사는 첫 중앙·하단 좌우 예고 뒤 두 번째 중앙·상단·좌측 예고로의 측면 교체, 세 비행·공용 폭탄 착탄, 플레이어 폭탄의 `Chain` 기폭과 Console/page 오류 0을 확인했다.
- 첫 사람 전체 run에서 seed-0 입장 플레이어 `(4,0)`과 기존 투척병 `(3,0)`이 붙어 준비 시간이 없다는 피드백을 재현했다. AI의 0.3초 예고·이동·폭탄 수치는 유지하고 시작/사격 anchor 순서를 `(3,2)→(-3,2)→(0,3)`으로 옮겼다. 콘텐츠 validator는 모든 잠재 출구와 4칸 이상, 메인 WebGL smoke는 실제 회전 입장과 5칸 거리를 회귀한다.
- 후속 사람 플레이에서 거리를 벌렸어도 spawn이 첫 사격 anchor라 입장 직후 Telegraph·투척이 시작되고, 그 폭탄이 가까운 추격자를 자동 처치하는 문제가 확인됐다. spawn을 별도 staging `(3,2)`로 유지하되 첫 사격 anchor를 `(0,3)`으로 분리해 4칸 Track을 강제하고, 추격자를 `(-2,2)`, 목표 후보를 `(0,0)·(-3,-2)·(2,-3)·(-4,1)·(4,1)·(0,2)`로 옮겼다. 적 friendly fire 규칙은 유지하며 초기 배치만 폭발 footprint 밖으로 교정했다.
- `Artifacts/Verification/20260820-124012-connected-web/`의 표준 11씬 Development WebGL은 139,085,332 bytes·403.033초·error 0으로 성공했다. seed-0 keyboard smoke `48/48`은 최초 `(4,0)/(2,-3)`·Manhattan 5, 예고→3발 투척, 새 이동 열의 두 번째 anchor `(2,3)` 선행 설치를 포함한 십자 폭탄 2개로 추격자·투척병 처치, Secret과 이후 Pillars/Gates/Recovery/보스 전체 경로를 확인했다. 2,531사건 `summary@2`, 가상 Gamepad `14/14`, 두 브라우저 실행의 Console/page error 0도 통과했다.
- staging·추격자 재배치의 실패 기준선은 `Artifacts/Verification/ConnectedTests/20260820-041438-753.json`의 `0/2`, 최종 집중 Core는 `20260820-042017-828.json`의 `38/38`, 전체 EditMode·PlayMode는 `20260820-042653-137.json`의 `344/344`와 `20260820-042710-420.json`의 `134/134`다. 콘텐츠 validator와 Unity Console Error는 0이다. `Artifacts/Verification/20260820-133000-thrower-entry-safety-connected-web/`의 표준 11씬 Development WebGL은 139,085,389 bytes·425.102초·error 0으로 성공했고 keyboard smoke `48/48`이 seed-0 네 칸 Track→Telegraph→3 launch와 2,472사건 전체 run, Console/page error 0을 확인했다. 최초 정지 관찰에서는 추격자가 적 폭탄에 죽지 않고 플레이어를 접촉 처치했으며, 전체 자동 전투의 두 적 사망 원인은 플레이어가 의도적으로 설치한 십자 폭탄 2개였다.
- 연결 테스트 증거는 실패 기준선 `Artifacts/Verification/ConnectedTests/20260819-211852-144.json`, 최종 `Artifacts/Verification/ConnectedTests/20260819-213125-705.json`·`Artifacts/Verification/ConnectedTests/20260819-213147-343.json`, 정적 증거 `Artifacts/Verification/20260820-063451-static/summary.json`이다.

## 남은 사람 판정

- 0.3초에 동시에 표시되는 세 예고가 위협적으로 느껴지면서도 각 착탄 셀을 읽고 실제 퇴로를 변경할 수 있는가.
- 1.5초 fuse·범위 1이 회피만 강제하지 않고 의도적 연쇄 선택도 만드는가.
- 1 cell/s staging→사격 anchor 이동이 짧은 준비 신호로 읽히고, 가장 가까운 1칸은 압박을 유지하며 바뀌는 측면 2칸은 반복감을 줄이는가. 추격자 조합이 자동 friendly fire나 불공정한 입장 공격 없이 충분히 압박하는가.
- 메인 던전 편성은 기존 Lanes 교체로 완료했다. 다음 사람 판정은 전용 씬뿐 아니라 seed-0 전체 run에서 입장 압박, 세 예고 인지, 회피/연쇄 선택과 다음 Pillars 난이도 연결을 함께 본다.
