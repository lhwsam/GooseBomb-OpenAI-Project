# 작업: 체력 10 보스 페이즈 개편

- 상태: `Implemented / EditMode + PlayMode + WebGL Verified / Human Playtest Pending`
- 설계: [보스 페이즈 개선안](../GameDesign/BossPhaseImprovementProposal.md)
- 시스템 계약: [보스 전투](../Systems/BossBattle.md)
- 대체 범위: [이전 보스 Core](BossCoreSlice.md), [이전 전용 arena·실제 폭탄 최소 패턴](BossBombArenaSlice.md)의 보스 동작

## 플레이어 계약

- 1페이즈에서 두 번의 짧은 추격과 방향 고정 돌진을 피한 뒤, 세 순차 폭탄과 한 parity 파동 중 공격 폭탄을 준비한다.
- 보스는 체력 10이며 살아 있는 동안 패턴 상태와 관계없이 서로 다른 플레이어 폭탄에 피해를 받는다.
- 체력 7 이하의 첫 2페이즈에서 소환 위치를 보고 자폭병을 유도한다. 자폭병은 4.5초 안에 조기 점화하지 않으면 현재 셀에서 강제 점화되고 보스에게 1피해를 줄 수 있다.
- 자폭병이 해결된 뒤 일반 2·연쇄 2 투척과 parity 반전을 피하고 더 짧은 과열을 노린다.
- 체력 2 이하의 일회성 최후 발악에서 외곽→안쪽 연쇄와 parity 반전을 통과한다.
- 정확한 보스 이동 목적지 고스트는 보이지 않지만 돌진·착탄·소환·parity 위험은 미리 보인다.

## 변경 범위

- Core: 3 phase 정의, typed pattern tuning, 원자 시퀀스, 다중 이동 결과, 순차 투척 계획·flight 데이터, 상시 플레이어 폭탄 피해, 자폭병 직접 피해·해결 gate.
- Runtime: 예약 착탄 셀, 발사·착탄 스케줄, 착탄 시 `BombSimulation` 설치, 동적 자폭병 생성·강제 점화, 패턴 피해와 phase 사건.
- Presentation: 포물선 비행 pool, 이동 segment queue, 동적 자폭병 presenter와 보스 입장 spawn VFX 재사용, LastStand HUD·애니메이션·위험 셀, 목적지 ghost 제거. 보스 본체의 상태·phase 색 변경은 사용하지 않는다.
- Content: 체력 10/7/2 정의, 6 투척 앵커·3 소환 앵커 arena, 전용 `BossBattlePlaytest` 씬과 Open/Play/Rebuild 메뉴.
- Tests/Docs: Core·Unity 회귀, 콘텐츠 validator, 설계·시스템·현재 상태 동기화.

## 비범위

- 완성 애니메이션, 몸 기울임, 먼지·잔상, 착탄 VFX·오디오
- 2페이즈 과열 위치를 마지막 투척 반대편으로 바꾸는 후보 규칙
- 자폭병 반복 소환 또는 2기 동시 소환
- 근접 공격 2종 이상, 무작위 패턴, 새 폭탄 종류
- 최종 체력·시간·속도 밸런스 확정

## Core 완료 조건

- `BossBattleDefinition`이 최대 체력 10, Two/LastStand 임계 7/2와 보스 폭탄 두 정의를 검증한다.
- `BossPatternTuning`이 패턴별 양수 시간, 추격 횟수, 돌진 거리, 비행·간격·강제 점화 시간을 검증한다.
- One/Two/LastStand 시퀀스와 안전한 phase 예약을 결정론적으로 재현한다.
- 추격은 한 칸, 돌진·중앙 복귀는 확정된 cardinal `Movements` 목록을 반환한다.
- 투척 계획은 3/4/4개 고유 셀과 순차 offset을 소유하고 parity는 행별 snapshot을 제공한다.
- 소환 위치는 2개 이상 저작 앵커 중 플레이어에게서 먼 비점유 셀로 Telegraph에 잠긴다.
- 플레이어 폭탄과 자폭병은 살아 있는 보스에게 상태 무관 `BombId`당 한 번 피해를 주며, 같은 폭발 중복과 사망 뒤 피해는 거부한다.
- 자폭병 해결 전 강화 투척으로 진행하지 않고 LastStand는 한 번만 실행한다.

## Unity 완료 조건

- 착탄 예약 중 플레이어 설치를 거부하고 비행 중 논리 폭탄 점유가 없다.
- `BossBombLaunched`에서 포물선 시각을 시작하고 `LandsAt`에 논리 폭탄을 설치해 fuse를 시작한다.
- 동일 frame 사건도 순번 오름차순으로 처리한다.
- 소환 셀을 예고하고 Execute에서 자폭병 visual/simulation을 동적으로 만들며, 실제 생성 셀에서 보스 입장 spawn VFX를 한 번 재생한다.
- 4.5초 강제 점화는 보스 돌진 최대 위협 구간과 겹치지 않는다.
- 보스 이동은 확정 결과만 보간하고 pause 동안 멈춘다. 정확한 목적지 ghost는 표시하지 않는다.
- HUD가 체력 10과 phase 1/2/3을 표시하고 사망 뒤 방 클리어를 한 번만 연결한다.

## 콘텐츠와 테스트 진입점

- 전용 씬: `Assets/Game/Scenes/TestSandbox/BossBattlePlaytest.unity`
- Unity 메뉴:
  - `Bomb Swap > Playtest > Open Boss Battle Room`
  - `Bomb Swap > Playtest > Play Boss Battle Room`
  - `Bomb Swap > Playtest > Rebuild Boss Battle Room`
- 이 씬은 실제 던전 run host 없이 보스 session을 직접 실행한다. `DungeonBoss`를 단독 재생할 때 발생하는 run host/미니맵 초기화 오류를 피한다.
- Build Settings에는 추가하지 않는다. 정식 던전 WebGL은 기존 `DungeonBoss` 씬으로 검증한다.

## 현재 검증 증거

- 연결 Unity 6000.5.3f1 컴파일·Console Error 0.
- 최종 EditMode 전체 `329/329`, 실패·건너뜀 0: `Artifacts/Verification/ConnectedTests/20260819-143454-325.json`.
- 최종 PlayMode 전체 `133/133`, 실패·건너뜀 0: `Artifacts/Verification/ConnectedTests/20260819-143208-224.json`. Telegraph 중 실제 플레이어 폭탄 피해와 보스 돌진·parity 피해의 `BossPattern` source 회귀를 포함한다.
- StaticOnly 통과: `Artifacts/Verification/20260819-233513-static/summary.json`.
- 최종 11씬 Development WebGL 빌드 성공: 138,798,114 bytes, 74.641초, 오류 0·안내 경고 3. 증거는 `Artifacts/Verification/20260819-232513-connected-web/`이다.
- Chromium keyboard smoke `46/46`이 전체 seed-0 경로, 1페이즈 과열 2회, 일회성 자폭병, 2페이즈 과열 2회, LastStand와 처치를 통과했다. 생존 중 모든 패턴에서 플레이어 폭탄 피해를 받는 계약과 정확한 이동 목적지 ghost 비노출을 함께 확인했다.
- 플레이테스트 사건 2,512개 분석과 가상 표준 Gamepad smoke `14/14`, 두 브라우저 실행의 Console/page error 0을 확인했다.
- `BossBattlePlaytest`를 Editor에서 열어 root 1·dirty false와 네 기둥/퇴로/카메라 구조를 확인했다.
- 전용 씬 Play Mode에서 보스 visual, 행별 parity 위험 표시, HUD가 생성되고 Console Error 0임을 확인했다.
- 소환 셀 Telegraph를 포함한 최종 자동 테스트와 정적 계약까지 통과했다.

## 남은 완료 조건

1. 전용 룸과 정식 seed-0 던전에서 사람 플레이테스트를 진행한다.
2. 공격 패턴 중 폭탄 적중률, 자폭병 유도율, phase별 피격, 전투 시간, parity 안전 칸 재사용을 기록한다.
3. 관찰 근거로 투척 간격·과열 시간·예고 표현을 조정하고 같은 WebGL 경로를 재검증한다.

## 위험

- 현재 placeholder는 이동 방향 몸짓·착탄 그림자·소환 음향을 완성하지 않아 고스트 제거 뒤 가독성이 수치보다 낮을 수 있다.
- 6개 투척 앵커와 parity 행 표시가 겹치면 정보량이 과도할 수 있다.
- 체력 10은 두 폭탄 적중과 자폭병 유도가 일어나지 않으면 반복 피로를 만들 수 있다.
- 실제 브라우저 자동 경로에서는 순차 발사·착탄·fuse와 전체 phase 진행이 통과했다. 다만 사람이 비행 간격을 분리된 공격으로 읽는지는 아직 플레이테스트하지 않았다.
