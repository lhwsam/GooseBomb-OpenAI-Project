# 프레임 반응형 플레이어 이동 수직 슬라이스

- 상태: 구현·자동 검증 완료, 수동 체감 재확인 대기
- 근거 세션: [PT-20260814-01](../Playtesting/Results/PT-20260814-01.md)
- 대체 계약: [방향 전환 응답성 수직 슬라이스](MovementTurnResponsivenessSlice.md)의 0.2초 step·pending turn
- 적용 계층: `BombSwap.Core` 플레이어 이동, `BombSwap.Unity` 세션·표현, WebGL 개발 검증 하네스

## 목표와 관찰 가능한 결과

- 기본 속도 5 cells/s는 유지하되 입력 반영을 0.2초 셀 cadence로 제한하지 않는다.
- 이동 키를 누르는 동안 매 Unity frame의 경과 시간만큼 Core 위치가 진행한다.
- 이동 키를 놓으면 이미 확정된 한 칸을 마저 재생하지 않고 다음 관찰 frame부터 위치가 멈춘다.
- `상 → 우 → 상 → 우 → 상 → 우`를 frame마다 바꾸면 각 방향이 같은 순서의 실제 위치 변화로 나타난다.
- 벽·폭탄·actor 점유와 설치자 한정 폭탄 셀 탈출은 기존 정수 `GridState`가 계속 판정한다.

## 수정 전 결함 증거

- 기존 `PlayerMovementSimulation`은 5 cells/s를 `1 / 5 = 0.2초` step 간격으로 바꾸고 방향도 그 예약 step에서만 소비했다.
- 기존 `PrototypePlayerController`는 논리 목적 셀이 step 시작에 확정된 뒤 그 위치까지 0.2초 선형 보간했다.
- 수정 전 PlayMode 회귀에서 키 해제 시 표시 위치는 `z=0.292967051`이었지만 0.15초 뒤 `z=1.0`까지 계속 이동해 실패했다.
- 1개 pending turn은 짧은 겹침 입력 하나를 보존했지만 빠른 반복 입력을 하나로 합치고, `Move(None)`에서 지우면 짧은 단타를 잃는 구조적 상충을 만들었다.

## 상태 소유와 불변식

- `GridSubcellPosition`은 셀 단위 `double X/Z` 연속 위치이며 `BombSwap.Core`가 권위 상태로 소유한다.
- `PlayerMovementSimulation.CurrentPosition`은 폭탄·폭발·적·점유 판정용 정수 셀이고 `Position`은 그 셀 사이의 이동 진행도를 소유한다.
- 셀 경계를 통과할 때만 `GridState.TryMoveActor`로 정수 점유를 원자적으로 전이한다. 목적 셀이 막히면 현재 셀 중심보다 벽 쪽으로 진행하지 않는다.
- 이동 거리는 주입된 게임 시계의 경과 시간과 cells/s로 계산한다. Transform, Rigidbody, Unity 물리 순서는 규칙 입력이 아니다.
- `PrototypePlayerController`는 별도 이동 타이머나 목적 셀 보간을 소유하지 않고 Core의 `GridSubcellPosition`을 월드 XZ로 변환해 표시한다.
- 큰 frame에서도 통과한 각 셀 경계를 순서대로 검사해 벽·폭탄을 건너뛰지 않는다.

## 범위와 비목표

- 변경 허용: Core 플레이어 이동·연속 위치 값, Unity 세션·표현, 관련 EditMode·PlayMode 테스트, WebGL probe/smoke, 소유 문서.
- 변경 금지: Input Actions 에셋, 씬·프리팹, 폭탄·폭발·적 cadence, 패키지·ProjectSettings, 기본 5 cells/s 튜닝 값.
- 비목표: 플레이어 충돌 반경, 코너 스냅 허용 폭, 게임패드 deadzone, 가속·감속 곡선, 대각선 이동.

## 자동 검증 계약

- EditMode: 20~50ms 진행마다 실제 연속 위치가 변하고, 해제 중 시간은 누적되지 않으며, frame별 빠른 방향 반복·큰 frame 경계 순회·벽·폭탄·설치자 탈출이 통과해야 한다.
- PlayMode: 실제 Input System에서 유지 이동, 해제 즉시 정지, 겹침 직교 전환과 6회 `North/East` 단타가 다음 frame의 실제 Transform 이동으로 관측되어야 한다.
- WebGL: 개발 probe가 입력 명령과 별도로 실제 연속 이동 `move-motion-direction-*`을 기록하고, 마지막 방에서 6회 `North/East` 단타가 모두 release 전에 실제 이동을 만들어야 한다.
- 전체 회귀: EditMode, PlayMode, 콘텐츠 validator, Unity Console 오류 0, Development WebGL 빌드, 기존 전투·3방 browser smoke가 통과해야 한다.

## 현재 검증 결과

- 수정 전 해제 회귀: 실패. 해제 위치 `z=0.292967051`에서 멈추지 않고 `z=1.0`까지 진행.
- 수정 후 Core 대상: `PlayerMovementSimulationTests` 18개 통과, 실패 0.
- 수정 후 Unity 연결 대상: `PrototypePlayerControllerTests` 19개 통과, 실패 0.
- 전체 EditMode 159개, PlayMode 64개, 콘텐츠 validator와 Unity Console 오류 0을 통과했다.
- Development WebGL 빌드 성공: 140,634,127 bytes, 266.945초, 오류 0. TextMeshPro IL2CPP 대형 메서드 분할 경고 3건은 기존 범주다.
- 실제 Edge headless에서 기존 폭탄·피해·3방 전환과 `North/East` 단타 6회의 release 전 실제 motion, 마지막 방 자기 폭발 피해, resize, browser Console/page error 0을 확인했다.
- 증거: `Artifacts/Verification/20260814-151702-continuous-movement-web-connected/` (Git 제외).
- 자동 검증은 계약 준수를 증명하지만 최종 조작감 개선 판정은 다음 수동 재플레이가 소유한다.

## 위험과 롤백

- 셀 중심선에서 벗어난 즉시 직교 전환을 허용하므로 좁은 벽 모서리의 최종 체감은 플레이테스트가 필요하다. 이번 범위에서는 막힌 목적 셀 쪽으로 현재 셀 중심을 넘지 못하게 한다.
- 플레이어와 적의 접촉·폭발 피해는 계속 정수 점유 셀 기준이다. 셀 경계 근처에서 보이는 위치와 판정 시점의 가독성을 수동 확인한다.
- 롤백 단위는 `GridSubcellPosition`, 연속 `PlayerMovementSimulation`, 세션 위치 event, 직접 표시 controller, 관련 테스트·probe/smoke·문서다. 직렬화 마이그레이션은 없다.
