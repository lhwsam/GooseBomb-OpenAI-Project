# 프레임 반응형 플레이어 이동 수직 슬라이스

- 상태: 구현·자동 검증 완료, 수동 체감 재확인 대기
- 근거 세션: [PT-20260814-01](../Playtesting/Results/PT-20260814-01.md)
- 적용 계층: `BombSwap.Core` 플레이어 이동, `BombSwap.Unity` 세션·표현, WebGL 개발 검증 하네스

> 이 문서의 현재 계약과 불변식이 구현 기준이다. 과거 결함과 폐기안은 재구현 지침이 아니며 [플레이어 이동 응답성 회귀 기록](PlayerMovementResponsivenessRegression.md)에서만 추적한다.

## 현재 계약과 관찰 가능한 결과

- 기본 속도는 5 cells/s이고, 입력 반영에 별도 셀 cadence를 두지 않는다.
- 이동 키를 누르는 동안 매 10ms simulation step의 경과 시간 × cells/s만큼 Core 위치가 진행한다.
- 이동 키를 놓으면 이미 확정된 한 칸을 마저 재생하지 않고 다음 10ms simulation step부터 위치가 멈춘다.
- `상 → 우 → 상 → 우 → 상 → 우`의 빠른 입력 각각이 관찰 가능한 10ms step에 도달하면 같은 순서의 실제 위치 변화로 나타난다.
- 세션은 이동 계산 직전에 Input Action의 최신 값을 다시 샘플링한다. 같은 대각선 입력이 유지되는 동안에는 마지막으로 전환한 축을 고정하고, 입력 벡터가 바뀔 때만 축 우선순위를 다시 계산한다.
- 벽·폭탄·actor 점유와 설치자 한정 폭탄 셀 탈출은 기존 정수 `GridState`가 계속 판정한다.

## 결정 경계

- 플레이어는 입력 해제·전환 때 사용하지 않는 예약을 즉시 풀고, 셀 경계에서만 점유를 원자적으로 전이한 뒤 다음 접근 셀을 다시 예약한다.
- 10ms simulation step이 없는 Unity frame에서는 이동 의도를 확정하지 않고, 실제 step 직전에 최신 frame intent와 같은 frame에서 끝난 마지막 짧은 탭 하나를 관찰한다.
- 적은 시작한 한 칸을 목적지 중심까지 완료하는 committed 이동을 유지한다. 플레이어 정책을 적에게 적용하거나 적 정책을 플레이어에게 적용하지 않는다.

## 상태 소유와 불변식

- `GridSubcellPosition`은 셀 단위 `double X/Z` 연속 위치이며 `BombSwap.Core`가 권위 상태로 소유한다.
- `PlayerMovementSimulation.CurrentPosition`은 폭탄·폭발·적·점유 판정용 정수 셀이고 `Position`은 그 셀 사이의 이동 진행도를 소유한다.
- 접근 중인 다음 셀만 예약하고, 셀 경계를 통과할 때만 `GridState.TryCommitReservedActorMove`로 정수 점유를 원자적으로 전이한 뒤 예약을 완료한다. 목적 셀이 막히면 현재 셀 중심보다 벽 쪽으로 진행하지 않는다.
- 이동 거리는 주입된 게임 시계의 경과 시간과 cells/s로 계산한다. Transform, Rigidbody, Unity 물리 순서는 규칙 입력이 아니다.
- `PrototypePlayerController`는 별도 이동 타이머나 목적 셀 보간을 소유하지 않고 Core의 `GridSubcellPosition`을 월드 XZ로 변환해 표시한다.
- 큰 frame에서도 통과한 각 셀 경계를 순서대로 검사해 벽·폭탄을 건너뛰지 않는다.

## 범위와 비목표

- 변경 허용: Core 플레이어 이동·연속 위치 값, Unity 세션·표현, 관련 EditMode·PlayMode 테스트, WebGL probe/smoke, 소유 문서와 플레이 가능한 16개 씬의 `cellsPerSecond=5` 복구.
- 변경 금지: Input Actions 에셋, 그 밖의 씬·프리팹 저작, 폭탄·폭발·적 cadence, 패키지·ProjectSettings, 기본 5 cells/s 이외 튜닝 값.
- 비목표: 플레이어 충돌 반경, 코너 스냅 허용 폭, 게임패드 deadzone, 가속·감속 곡선, 대각선 이동.

## 자동 검증 계약

- EditMode: 20~50ms 진행마다 실제 연속 위치가 변하고, 해제 중 시간은 누적되지 않으며, 연속 step의 빠른 방향 반복·큰 frame 경계 순회·벽·폭탄·설치자 탈출이 통과해야 한다.
- PlayMode: 실제 Input System에서 유지 이동, 해제 즉시 정지, 겹침 직교 전환과 6회 `North/East` 단타가 다음 10ms 관찰 step의 실제 Transform 이동으로 관측되어야 한다.
- WebGL: 개발 probe가 입력 명령과 별도로 실제 연속 이동 `move-motion-direction-*`을 기록하고, 마지막 방에서 6회 `North/East` 단타가 모두 release 전에 실제 이동을 만들어야 한다.
- 전체 회귀: EditMode, PlayMode, 콘텐츠 validator, Unity Console 오류 0, Development WebGL 빌드, 기존 전투·3방 browser smoke가 통과해야 한다.

## 현재 검증 결과

- 2026-08-24 최종 복구: 플레이 가능한 16개 씬을 Unity Editor로 `cellsPerSecond=5`에 맞춘 뒤 Unity 컴파일에 성공했고, 기존 기대값을 바꾸지 않은 전체 EditMode `363/363`이 통과했다. 증거 `Artifacts/Verification/ConnectedTests/20260823-223646-232.json`.
- 전체 PlayMode는 `172/186` 통과했다. 플레이어 이동·입력 응답성 실패는 0이고 남은 14개는 별도 보스 fuse·scene binder/run host·적/폭발/방 클리어 회귀다. 증거 `Artifacts/Verification/ConnectedTests/20260823-223924-646.json`.
- 최종 문서 반영 뒤 StaticOnly가 통과했다. 증거 `Artifacts/Verification/20260824-074531-static/summary.json`.
- 최신 Development WebGL 재시도는 기존 로비 제목, 투척병 Animator, 보스 chain fuse와 공개 에셋의 private vendor 직접 참조 validator 오류로 빌드 전에 차단됐다. 증거 `Artifacts/Verification/20260824-074450-connected-web/webgl-build-status.txt`; WebGL과 browser smoke는 미실행이다.
- 자동 검증은 계약 준수를 증명하지만 최종 조작감 개선 판정은 다음 수동 재플레이가 소유한다.

## 위험과 롤백

- 셀 중심선에서 벗어난 즉시 직교 전환을 허용하므로 좁은 벽 모서리의 최종 체감은 플레이테스트가 필요하다. 이번 범위에서는 막힌 목적 셀 쪽으로 현재 셀 중심을 넘지 못하게 한다.
- 플레이어와 적의 접촉·폭발 피해는 계속 정수 점유 셀 기준이다. 셀 경계 근처에서 보이는 위치와 판정 시점의 가독성을 수동 확인한다.
- 롤백 단위는 `GridSubcellPosition`, 연속 `PlayerMovementSimulation`, 세션 위치 event, 직접 표시 controller, 관련 테스트·probe/smoke·문서다. 직렬화 마이그레이션은 없다.
