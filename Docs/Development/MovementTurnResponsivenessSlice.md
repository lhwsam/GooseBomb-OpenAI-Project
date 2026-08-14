# 방향 전환 응답성 수직 슬라이스

- 상태: `Superseded` — 후속 체감에서 0.2초 cadence 자체가 원인으로 확인되어 [프레임 반응형 플레이어 이동](ContinuousPlayerMovementSlice.md)으로 대체
- 근거 세션: [PT-20260814-01](../Playtesting/Results/PT-20260814-01.md)
- 적용 계층: `BombSwap.Core` 이동 simulation, `BombSwap.Unity` 입력 어댑터와 WebGL 개발 검증 하네스

## 결함 계약

- 관찰: 방향키로 수직 방향을 바꿀 때 현재 누른 키보다 이전 방향으로 한 step 더 이동하는 듯한 한 박자 지연이 느껴졌다.
- 기대: 이전 cardinal 키를 아직 놓지 않은 짧은 겹침 구간에도 새로 누른 직교 키가 즉시 현재 `PlayerCommand.Move` 의도가 되어야 한다.
- 환경: Unity 6000.5.3f1 Development WebGL, 게임 commit `09bbe8b`, 세 TestSandbox 시퀀스.
- 빈도: 참가자 P01이 플레이 세션에서 보고. 동일 벡터와 이전 방향을 사용한 Unity 직접 실행에서 결정론적으로 재현.
- 심각도: 진행 차단은 아니지만 기본 전투의 조작 신뢰를 해치는 주요 조작성 결함.
- 1차 수정 후 관찰: 문제는 줄었지만, 위쪽을 계속 누른 상태에서 오른쪽을 짧게 눌렀다 떼면 오른쪽 한 칸 뒤 위쪽으로 복귀하지 않고 계속 위쪽으로만 이동하는 경우가 남았다.
- 2차 기대: 현재 셀 이동 사이에 들어온 최신 직교 방향을 한 번 기억해 다음 셀에서 소비하고, 이후에는 여전히 눌린 방향으로 복귀해야 한다.

## 가설 기록

| 순위 | 가설 | 지지 증거 | 반대 증거 | 판별 실험 | 상태 |
|---:|---|---|---|---|---|
| 1 | 같은 크기의 두 축에서 이전 방향을 유지하는 tie-break가 새 방향 명령을 늦춘다. | `(1,1)+North → North`, `(1,1)+East → East`; 1차 실패 테스트와 일치 | 1차 수정 뒤에도 실제 이동 유실이 남음 | 새 직교 방향을 기대하는 interpreter·실제 키 겹침 테스트 | `Confirmed`, 1차 원인 |
| 2 | 0.2초 이동 cadence 사이의 짧은 직교 입력이 원본 없이 현재 유지 방향에 덮어써진다. | `North step → 50ms East → 50ms North → 100ms` 뒤 실제 두 번째 step이 `North`; 참가자 2차 표현과 일치 | East를 다음 step까지 계속 누르면 기존 코드도 East로 이동 | East를 다음 step 전에 해제하는 Core·Input System 회귀 테스트 | `Confirmed`, 잔여 원인 |
| 3 | WebGL focus나 Input System callback 누락이 키 상태를 지연한다. | WebGL에서 보고됨 | Console 오류 0, focus/reset 테스트 통과, 동일 interpreter 입력으로 Editor에서 재현 | 실제 Input System 키 겹침 PlayMode 테스트 | `Contradicted` |

## 일반적인 4방향 이동 방식 검토

- 입력 계층은 `마지막으로 누른 방향 우선`으로 현재 유지 방향을 정한다. [Unity Input System의 `Vector2Composite`](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.Composites.Vector2Composite.html)는 현재 버튼 상태를 벡터로 합칠 뿐 짧은 입력의 순서를 이동 주기까지 보존하지 않으므로 게임 규칙이 별도 의도를 소유해야 한다.
- 고정 간격 격자 이동은 이동 주기 사이에 들어온 입력을 짧게 버퍼링하고 다음 타일 이동에서 소비하는 방식이 일반적이다. [MonoGame의 입력 버퍼 지침](https://docs.monogame.net/articles/tutorials/building_2d_games/10_handling_input/)과 [공식 격자 이동 예제](https://docs.monogame.net/articles/tutorials/building_2d_games/23_completing_the_game/index.html)도 방향 입력을 큐에 넣고 다음 이동 주기에 꺼내 반응성과 코너 조작을 보완한다.
- 여러 입력을 FIFO로 길게 쌓으면 플레이어가 손을 뗀 뒤에도 캐릭터가 명령을 수행하는 느낌이 생긴다. Bomb Swap은 이동 주기가 0.2초이고 액션 전투가 목적이므로 `최신 방향 1개`만 보존한다.
- 중간 셀에서 즉시 꺾거나 Transform을 방향키 쪽으로 스냅하는 보정은 논리 점유와 표현의 불일치를 만들 수 있어 이번 범위에서 제외한다. 실제 꺾임은 셀 경계에 유지한다.

## 구현 계약

- 두 축 중 하나만 actuated면 해당 방향을 유지한다.
- 두 축 크기가 다르면 기존처럼 dominant axis를 선택한다.
- 두 축 크기가 같고 이전 cardinal 방향이 아직 입력 벡터에 포함되면, 이전 축에 직교하는 축을 새 전환 의도로 선택한다.
- 이전 방향이 현재 벡터와 맞지 않거나 `None`이면 기존 세로축 tie-break를 유지한다.
- `BombSwapInputReader`는 이전 키를 놓기 전에 새 방향 `PlayerCommand`를 한 번 발행하고, 이전 키 해제만으로 같은 명령을 중복 발행하지 않는다.
- `PlayerMovementSimulation.MoveDirection`은 현재 유지 방향을 소유하고, 이동 cadence 사이에 들어온 최신 방향 1개를 별도 pending turn으로 보존한다.
- 새 방향을 다음 step 전에 놓아 이전 유지 방향으로 돌아와도 pending turn은 한 번 소비될 때까지 유지한다. 소비 뒤에는 현재 유지 방향이 다음 step부터 다시 적용된다.
- 다음 step 전에 다른 방향이 들어오면 더 오래된 pending turn을 교체한다. `Move(None)`은 focus 상실과 정지에서 pending turn도 함께 지운다.
- pending turn의 목적지가 막혔으면 같은 step에서 현재 유지 방향을 시도하고 pending turn은 폐기한다. 막힌 입력이 나중에 예기치 않게 실행되지 않게 한다.
- Core의 0.2초 cadence, step 시작 점유, Transform 선형 보간은 바꾸지 않는다.

## 범위와 비목표

- 변경 허용: `PlayerMovementSimulation`, 관련 EditMode·PlayMode 테스트, 개발 WebGL 방향 probe와 smoke, 관련 문서.
- 변경 금지: Input Actions 직렬화 에셋, Core 이동 cadence, 씬·프리팹, 폭탄·적 규칙, 패키지·ProjectSettings.
- 비목표: 아날로그 deadzone 튜닝, 중간 셀에서 즉시 꺾기, 이동 속도 변경, 보간 곡선 변경.

## 검증 계약

- 수정 전 1차: 새 직교 방향 명령 기대 테스트가 `Expected: East`로 실패해야 한다.
- 수정 전 2차: `North` 유지 중 `East`를 다음 cadence 전에 눌렀다 떼면 실제 step 기대가 `East`인데 `North`로 실패해야 한다.
- EditMode: 짧은 직교 탭의 1회 소비→유지 방향 복귀, 최신 pending 교체, 막힌 pending에서 유지 방향 fallback과 기존 cadence·점유 회귀가 통과해야 한다.
- PlayMode: interpreter 8개 사분면, 실제 방향키 `위 유지 → 오른쪽 누름 → 오른쪽 해제` 뒤 실제 이동 `North → East → North`, 기존 focus·버튼·단축키 회귀가 통과해야 한다.
- 전체 Unity: EditMode, PlayMode, 콘텐츠 validator, Console 오류 0.
- WebGL: 마지막 방의 열린 셀에서 `ArrowUp` step 직후 `ArrowRight`를 짧게 눌렀다 떼고, 명령이 다시 North로 돌아온 뒤에도 실제 이동이 `East → North` 순서로 관측되어야 한다. 기존 3방 smoke도 통과해야 한다.
- 수동: 참가자가 수정 build에서 방향 전환 감각과 남은 셀 경계 지연을 다시 구분해 평가한다.

## 검증 결과

- 수정 전 회귀 테스트: 새 직교 방향을 기대한 테스트가 `Expected: East`로 실패해 결함을 포착했다.
- 수정 후 대상 PlayMode: 입력 해석기와 실제 방향키 겹침 fixture 22개 통과, 실패 0.
- 1차 수정 전체 Unity: EditMode 152개, PlayMode 60개 통과. 실패·건너뜀·불확정 0.
- `PrototypeContentValidator`: 오류 0. Unity Console 컴파일 오류 0.
- Development WebGL: 140,538,771 bytes, 72.070초, 오류 0. TextMeshPro IL2CPP 대형 메서드 분할 경고 3건.
- Edge headless: load, canvas focus, 기존 전투와 3방 전환, `ArrowUp` 유지 중 `ArrowRight` 전환, resize, browser Console/page error 0 모두 통과.
- 증거: `Artifacts/Verification/20260814-132248-direction-turn-web-connected/` (Git 제외).
- 남은 검증: 참가자가 같은 키 겹침 순서를 반복해 체감 개선과 남은 셀 경계 대기감을 분리해 확인한다.

2차 수정 검증 결과:

- Unity 직접 재현: `North step → 50ms East → 50ms North → 100ms` 뒤 수정 전 실제 두 번째 step이 `North`, 위치 `(0, 2)`.
- 수정 전 새 Core 회귀 테스트: `Expected: East`, `But was: North`로 실패.
- 수정 후 `PlayerMovementSimulationTests` 14개 통과, 실패 0.
- 실제 Input System 짧은 탭 PlayMode 대상 테스트 1개 통과: 실제 이동 `North → East → North`.
- 전체 Unity: EditMode 155개, PlayMode 61개 통과. 실패·건너뜀·불확정 0.
- `PrototypeContentValidator`: 오류 0. Unity Console 오류 0.
- Development WebGL: 140,540,123 bytes, 45.333초, 오류 0, TextMeshPro IL2CPP 대형 메서드 분할 경고 3건.
- Edge headless: 오른쪽 키 해제와 North 복귀 명령 뒤 실제 `East → North` step, 기존 전투·3방 전환, canvas focus, resize, browser Console/page error 0 모두 통과.
- 증거: `Artifacts/Verification/20260814-140913-buffered-turn-web-connected/` (Git 제외).
- 남은 검증: 참가자가 수정 build에서 짧은 오른쪽 탭의 실제 한 칸 전환과 전체 조작감을 다시 평가한다.

## 위험과 롤백

- 정확한 대각선 값을 만드는 게임패드·D-pad에서도 새 직교 축 우선 정책이 적용된다. 게임패드 수동 검증은 아직 남는다.
- 수정 뒤에도 지연이 느껴지면 tie-break를 되돌리지 않고 cadence와 보간을 별도 변수로 조사한다.
- 입력 버퍼는 최대 한 방향, 최대 다음 step까지라서 긴 명령 backlog는 만들지 않는다. 다만 blocked fallback과 세 방향 이상 동시 입력의 체감은 후속 수동 검증 대상이다.
- 롤백 단위는 interpreter tie-break, Core pending turn, 관련 EditMode·PlayMode 회귀 테스트, 방향 probe/smoke와 문서다. 직렬화 데이터 마이그레이션은 없다.
