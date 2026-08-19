# 보스 Core 수직 슬라이스 작업 계약

- 상태: `Core + Unity Implemented / WebGL Verified / Playtest Pending`
- 후속 상태: 이 기준선과 [전용 보스방·실제 폭탄 수직 슬라이스](BossBombArenaSlice.md)의 보스 동작은 [체력 10 보스 페이즈 개편](BossPhaseReworkSlice.md)으로 대체됐다. 과거 검증 근거만 유지한다.
- 규칙 소유: `BombSwap.Core`
- 후속 연결: `BombSwap.Authoring`, `BombSwap.Unity`
- 관련: [보스 전투](../Systems/BossBattle.md), [피해와 무적](../Systems/DamageAndInvulnerability.md), [완료 정의](DefinitionOfDone.md)

## 목적

보스방 placeholder를 실제 전투로 연결하기 전에 격자 예고, 실행, 반격 기회, phase 전환과 폭탄 피해의 결정론적 규칙을 Unity 비참조 테스트로 고정한다. 이 단계는 보스의 재미나 최종 수치를 통과 판정하지 않는다.

## 플레이어 계약

- 입장 직후 공격하지 않고 먼저 위험 셀을 예고한다.
- 예고한 셀과 실제 실행 셀은 같은 snapshot이다.
- 1페이즈는 열과 행 위험을 번갈아 보여준다.
- 패턴 실행 뒤 Recovery에서만 보스에게 폭탄 피해를 줄 수 있다.
- 체력 임계값을 넘은 즉시 패턴을 끊지 않고 현재 Recovery 종료 뒤 2페이즈 체크무늬로 전환한다.
- 보스 사망과 격자 점유 제거는 한 번만 일어난다.

## 코드 경계

- `BossBattleDefinition`: 체력, phase 임계값, 패턴 피해와 페이즈별 시간.
- `BossBattleSimulation`: 시계 관측, 패턴 선택, 위험 셀 snapshot, 상태 전이, Recovery 취약성, 폭발 피해와 사망.
- `BossPatternTransition`: 전이 예약 시각, 이전/현재 상태, phase, 패턴, sequence와 위험 셀.
- `BossDamageResult`: 적용, 비취약, 중복 폭발, 사망 뒤 무시를 구분하는 결과.
- 기존 `EnemyHealthSimulation`을 내부 합성해 `BombId` 중복과 체력 하한을 재사용한다.

패턴 선택은 현재 프로토타입에 필요한 열·행·체크무늬 세 종류만 명시한다. 임의 패턴 그래프나 행동 트리 프레임워크는 만들지 않는다.

## 자동 검증

- 보스 정의 ID·체력·임계값·피해·양수 timing 검증.
- 시작 Telegraph와 보스 권위 격자 점유.
- exact boundary의 `Telegraph → Execute → Recovery`와 예고/실행 snapshot 동일성.
- 비취약 구간 무시, Recovery 적용, 같은 `BombId` 중복 차단.
- 1페이즈 열→행 교대와 Recovery 종료 시점의 2페이즈 체크무늬 전환.
- 큰 시계 진행에서도 원래 예약 시각 순서 보존.
- 치명 피해 뒤 `Defeated`, 위험 셀·actor 점유 단일 제거와 추가 전이/피해 차단.
- 중복/비보행 arena cell, 잘못된 actor와 시계 역행 거부.

증거:

- 보스 대상 EditMode 11/11: `Artifacts/Verification/ConnectedTests/20260814-160136-925.json`.
- 전체 EditMode 262/262: `Artifacts/Verification/ConnectedTests/20260814-160210-645.json`.
- 최종 전체 EditMode 264/264: `Artifacts/Verification/ConnectedTests/20260814-162458-961.json`.
- 최종 전체 PlayMode 97/97: `Artifacts/Verification/ConnectedTests/20260814-163107-198.json`.
- Unity import/compile, `PrototypeContentValidator` 및 Console Error 0.
- 최종 Development WebGL 빌드 성공: 137,676,341 bytes, 100.410초, 오류 0, 기존 TextMeshPro IL2CPP 경고 3. `Artifacts/Verification/20260815-015934-boss-battle-web-final/webgl-build-report.json`.
- Edge headless browser smoke 22/22: 전체 seed-0 주 경로, 보스 Telegraph·Execute·Recovery 각 4회, Recovery 피해 4회, 2페이즈·격파·클리어, canvas focus·pause·resize와 Console/page error 0. `Artifacts/Verification/20260815-015934-boss-battle-web-final/browser-smoke.json`.
- 시각 증거: 같은 폴더의 `webgl-boss-battle-boss-telegraph.png`, `webgl-boss-battle.png`.

## Unity 연결 결과

1. `PrototypeBossDefinitionAsset`과 boss spawn·arena cell 저작 계약을 추가했다.
2. `PrototypeGameSession`의 일반 적 활성과 보스 활성 구성을 분리하고 보스 `ActorId(5)`를 연결했다.
3. 매 frame 보스 전이를 한 단계 처리하고 Execute 셀의 플레이어 피해를 기존 무적 계약에 전달했다.
4. 폭발 영향 셀에 보스 위치가 포함되고 상태가 Recovery일 때만 Core 피해를 적용했다.
5. 위험 셀 telegraph/execute, 보스 phase·피격·사망 presenter를 pooling/property block 기반으로 구현했다.
6. 보스 사망을 단일 `RoomCleared`와 문 개방에 연결했다.
7. PlayMode, content validator와 실제 WebGL 가독성 자동 검증을 완료했다. 사람 가독성·재미 플레이테스트는 별도 단계로 남겼다.

## 비목표와 남은 위험

- Core 테스트 시간 fixture는 실제 저작값이 아니다. 실제 `PrototypeBossDefinitionAsset`은 두 페이즈 Recovery를 2.75초로 두어 2.25초 신관 뒤 최소 0.5초 반격 여유를 제공한다.
- 보스 패턴 플레이어 피해는 기존 무적 계약과 통합됐고 진단 probe가 source를 구분한다. 기본 보스 체력·phase HUD와 완료 화면까지 연결됐으며 최종 아트·오디오와 사람 가독성 검증은 남아 있다.
- 열·행·체크무늬가 두 폭탄 모두의 다른 설치 판단을 만드는지는 자동 테스트로 판정하지 않는다.
- 보스 이동, 소환, 장애물 생성, 완성 VFX·audio와 다음 층은 이 수직 슬라이스에 포함하지 않는다.
