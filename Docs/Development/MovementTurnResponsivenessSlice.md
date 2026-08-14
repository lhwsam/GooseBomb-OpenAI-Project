# 방향 전환 응답성 수직 슬라이스

- 상태: 구현 및 자동 검증 완료, 참가자 수동 재확인 대기
- 근거 세션: [PT-20260814-01](../Playtesting/Results/PT-20260814-01.md)
- 적용 계층: `BombSwap.Unity` 입력 어댑터와 WebGL 개발 검증 하네스

## 결함 계약

- 관찰: 방향키로 수직 방향을 바꿀 때 현재 누른 키보다 이전 방향으로 한 step 더 이동하는 듯한 한 박자 지연이 느껴졌다.
- 기대: 이전 cardinal 키를 아직 놓지 않은 짧은 겹침 구간에도 새로 누른 직교 키가 즉시 현재 `PlayerCommand.Move` 의도가 되어야 한다.
- 환경: Unity 6000.5.3f1 Development WebGL, 게임 commit `09bbe8b`, 세 TestSandbox 시퀀스.
- 빈도: 참가자 P01이 플레이 세션에서 보고. 동일 벡터와 이전 방향을 사용한 Unity 직접 실행에서 결정론적으로 재현.
- 심각도: 진행 차단은 아니지만 기본 전투의 조작 신뢰를 해치는 주요 조작성 결함.

## 가설 기록

| 순위 | 가설 | 지지 증거 | 반대 증거 | 판별 실험 | 상태 |
|---:|---|---|---|---|---|
| 1 | 같은 크기의 두 축에서 이전 방향을 유지하는 tie-break가 새 방향을 늦춘다. | `(1,1)+North → North`, `(1,1)+East → East`; 참가자 표현과 일치 | 없음 | 새 직교 방향을 기대하는 interpreter·실제 키 겹침 테스트 | `Confirmed` |
| 2 | 0.2초 이동 cadence가 이전 방향을 한 step 더 적용한다. | 셀 경계까지 최대 0.2초 대기 가능 | 기존 Core 테스트는 방향 변경 뒤 다음 예약 step에 새 방향을 적용 | cadence를 바꾸지 않고 tie-break만 수정한 build 재플레이 | `Weakened` |
| 3 | WebGL focus나 Input System callback 누락이 키 상태를 지연한다. | WebGL에서 보고됨 | Console 오류 0, focus/reset 테스트 통과, 동일 interpreter 입력으로 Editor에서 재현 | 실제 Input System 키 겹침 PlayMode 테스트 | `Contradicted` |

## 구현 계약

- 두 축 중 하나만 actuated면 해당 방향을 유지한다.
- 두 축 크기가 다르면 기존처럼 dominant axis를 선택한다.
- 두 축 크기가 같고 이전 cardinal 방향이 아직 입력 벡터에 포함되면, 이전 축에 직교하는 축을 새 전환 의도로 선택한다.
- 이전 방향이 현재 벡터와 맞지 않거나 `None`이면 기존 세로축 tie-break를 유지한다.
- `BombSwapInputReader`는 이전 키를 놓기 전에 새 방향 `PlayerCommand`를 한 번 발행하고, 이전 키 해제만으로 같은 명령을 중복 발행하지 않는다.
- Core의 0.2초 cadence, step 시작 점유, Transform 선형 보간은 바꾸지 않는다.

## 범위와 비목표

- 변경 허용: `CardinalInputInterpreter`, 입력 PlayMode 테스트, 개발 WebGL 방향 probe와 smoke, 관련 문서.
- 변경 금지: Input Actions 직렬화 에셋, Core 이동 cadence, 씬·프리팹, 폭탄·적 규칙, 패키지·ProjectSettings.
- 비목표: 아날로그 deadzone 튜닝, 중간 셀에서 즉시 꺾기, 이동 속도 변경, 보간 곡선 변경.

## 검증 계약

- 수정 전: 새 직교 방향 기대 테스트가 `Expected: East`로 실패해야 한다.
- PlayMode: interpreter 8개 사분면, 실제 방향키 `위 → 위+오른쪽 → 오른쪽 → 해제`, 기존 focus·버튼·단축키 회귀가 통과해야 한다.
- 전체 Unity: EditMode, PlayMode, 콘텐츠 validator, Console 오류 0.
- WebGL: 실제 Development build에서 `ArrowUp`을 놓기 전에 `ArrowRight`의 `move-direction-east`가 관측되고 기존 3방 smoke가 통과해야 한다.
- 수동: 참가자가 수정 build에서 방향 전환 감각과 남은 셀 경계 지연을 다시 구분해 평가한다.

## 검증 결과

- 수정 전 회귀 테스트: 새 직교 방향을 기대한 테스트가 `Expected: East`로 실패해 결함을 포착했다.
- 수정 후 대상 PlayMode: 입력 해석기와 실제 방향키 겹침 fixture 22개 통과, 실패 0.
- 전체 Unity: EditMode 152개, PlayMode 60개 통과. 실패·건너뜀·불확정 0.
- `PrototypeContentValidator`: 오류 0. Unity Console 컴파일 오류 0.
- Development WebGL: 140,538,771 bytes, 72.070초, 오류 0. TextMeshPro IL2CPP 대형 메서드 분할 경고 3건.
- Edge headless: load, canvas focus, 기존 전투와 3방 전환, `ArrowUp` 유지 중 `ArrowRight` 전환, resize, browser Console/page error 0 모두 통과.
- 증거: `Artifacts/Verification/20260814-132248-direction-turn-web-connected/` (Git 제외).
- 남은 검증: 참가자가 같은 키 겹침 순서를 반복해 체감 개선과 남은 셀 경계 대기감을 분리해 확인한다.

## 위험과 롤백

- 정확한 대각선 값을 만드는 게임패드·D-pad에서도 새 직교 축 우선 정책이 적용된다. 게임패드 수동 검증은 아직 남는다.
- 수정 뒤에도 지연이 느껴지면 tie-break를 되돌리지 않고 cadence와 보간을 별도 변수로 조사한다.
- 롤백 단위는 interpreter tie-break, 두 회귀 테스트, 방향 probe/smoke와 관련 문서다. 직렬화 데이터 마이그레이션은 없다.
