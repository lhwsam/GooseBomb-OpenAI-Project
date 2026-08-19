# 작업: 전용 보스방과 실제 보스 폭탄 최소 패턴

- 상태: `Superseded` — 자동 정확성 검증 기록은 보존하며 현재 보스 동작은 [체력 10 보스 페이즈 개편](BossPhaseReworkSlice.md)이 대체한다.
- 근거: [전투·적 AI·레벨·보스 개선 제안](../GameDesign/CombatEnemyLevelBossImprovementProposal.md) 권장 수직 슬라이스 5
- 소유 계약: [보스 전투](../Systems/BossBattle.md), [폭탄과 폭발](../Systems/BombAndExplosion.md), [방 저작](../Systems/RoomAuthoring.md)

## 목표

> 아래 내용은 체력 4·`RetreatThrow/CrossChain` 기준의 과거 수직 슬라이스 기록이다. 현재 구현 계약으로 사용하지 않는다.

- `DungeonBoss`가 일반 `prototype-combat-loop`가 아닌 전용 `prototype-boss-arena`를 사용한다.
- 보스 공격은 추상 위험 셀 즉시 피해 대신 기존 `BombSimulation`에 실제 적 소유 폭탄을 설치한다.
- 플레이어는 Telegraph 시작 시 어느 퇴로가 표적이 됐는지 보고 피하면서, 다음 보스 목적지에 자신의 공격 폭탄을 선행 설치할 수 있다.
- 2페이즈는 두 번째 적 폭탄이 첫 폭발에 맞아 기존 고정 연쇄 지연 뒤 순차 폭발한다.

## 범위

- 변경 허용: `Assets/Game/Core/Bosses`, 보스 authoring/session/presenter, prototype builder·validator, 보스 관련 EditMode·PlayMode·WebGL probe, `DungeonBoss`와 전용 room/bomb/prefab 콘텐츠, 관련 문서.
- 변경 금지: `Assets/Feel`, `Assets/Plugins`, URP·ProjectSettings, 일반 전투방 5개와 던전 그래프 생성 규칙.
- 비목표: 투사체 비행 물리, 완성 아트·오디오, 무작위 패턴, 소환, 퇴로 차단 투척병, 새 피해 시스템, 보스 체력 증가.

## 계약과 불변식

- 상태 소유자는 `BossBattleSimulation`이다. `GridState`에서 현재 플레이어 셀과 저작 퇴로 anchor를 읽어 Telegraph 시작 순간 계획을 한 번 확정한다.
- 1페이즈 `RetreatThrow`는 플레이어에게 가장 가까운 사용 가능한 anchor에 `prototype-boss-throw` 하나를 계획한다. 동률은 안정적인 좌표 순서로 결정한다.
- 2페이즈 `CrossChain`은 같은 표적과 arena 중앙 쪽 cardinal 인접 셀에 `prototype-boss-chain`을 함께 계획한다. 첫 폭발 footprint가 두 번째 폭탄 셀을 포함해야 한다.
- 계획 시 이미 폭탄이 있는 후보는 건너뛴다. 모든 후보가 막히면 해당 회차 공격 계획은 비며 임의 재표적하지 않는다.
- 계획 뒤 플레이어 이동으로 표적을 바꾸지 않는다. session은 같은 frame에 계획된 모든 폭탄을 설치하며 부분 설치가 발생하면 일관성 오류로 취급한다.
- 적 폭탄은 기존 셀 점유, fuse, 벽 차단, 파괴 가능 벽, 연쇄 스케줄러와 `BombId`를 그대로 사용한다.
- 보스 폭탄은 플레이어·일반 적·파괴 가능 벽에 기존 폭발 피해를 주지만 소유자인 보스 자신에게 피해를 주지 않는다. 자폭병 등 다른 소유자의 폭발은 기존처럼 Recovery의 보스에게 피해를 줄 수 있다.
- 보스 이동 목적지 ghost, 한 패턴당 한 칸 이동, Recovery 한정 플레이어 폭탄 피해, phase 안전 전환과 단일 사망/클리어는 유지한다.
- `CurrentDangerCells`는 계획된 실제 십자 폭발 footprint의 합집합이며 Telegraph/Execute 표현만 소유한다. Execute 즉시 추상 피해는 제거한다.

## 콘텐츠 계약

- `prototype-boss-arena`: 11×9, 플레이어 `(0,-3)`, 보스 `(0,1)`, 고정 기둥 `(-2,-1)·(2,-1)·(-2,1)·(2,1)`.
- 퇴로/투척 anchor는 `(-3,-2)·(3,-2)`, 2페이즈 연쇄 셀은 각각 `(-2,-2)·(2,-2)`다.
- 보스 이동은 중앙 3×3 외곽의 기존 8셀 cardinal 폐쇄 loop를 사용한다.
- `prototype-boss-throw`: `Cross`, fuse 1.25초, 범위 2.
- `prototype-boss-chain`: `Cross`, fuse 2.25초, 범위 2. 첫 폭발로 예약 시 전역 0.15초 연쇄 지연을 사용한다.
- 보스 폭탄 두 종류는 플레이어 폭탄과 구분되는 전용 placeholder를 사용하며 collider를 소유하지 않는다.
- 모든 수치는 고정 사람 플레이 전까지 `Proposed`다.

## 완료 조건

- EditMode: 계획 잠금·동률·기존 폭탄 회피·1/2페이즈 배치·실제 footprint·연쇄 가능성·기존 이동/Recovery/사망 회귀.
- PlayMode: 실제 적 소유 폭탄 설치와 시각화, 추상 패턴 피해 제거, 폭발 플레이어 피해, 보스 자기 면역, 플레이어 선행 설치 적중과 phase 전환.
- 콘텐츠: 전용 room·폭탄·prefab 참조, 정확한 셀·anchor, `DungeonBoss` 단독 사용, Build Settings 유지와 validator 오류 0.
- WebGL: 전체 seed-0 경로에서 보스 폭탄 arm→폭발→2페이즈 연쇄→격파, 키보드·Gamepad·Console/page error 회귀.
- 사람 판정: 미끼 anchor와 보스 목적지 선행 설치가 동시에 읽히는지는 자동 완료와 분리한다.

## 위험과 롤백

- 위험: 실제 폭탄이 Recovery 초반을 점유해 반격 창이 지나치게 짧거나 시각 정보가 겹칠 수 있다.
- 완화: 기존 2.75초 Recovery와 보스 체력 4를 유지하고, 첫 폭발 뒤에도 1초 이상 남는 fuse 기준선을 사용한다.
- 롤백 단위: Core 계획·두 보스 bomb asset·session/presenter·전용 arena·builder/validator/test/probe와 이 문서를 한 묶음으로 한다. 기존 보스 이동과 일반 폭탄 규칙은 롤백 대상이 아니다.

## 검증 증거

- StaticOnly: `Artifacts/Verification/20260819-075201-static/summary.json` 통과.
- 연결 Unity 6000.5.3f1: EditMode `327/327`, PlayMode `131/131`, 실패·건너뜀 0. 증거는 `Artifacts/Verification/ConnectedTests/20260818-221653-809.json`, `Artifacts/Verification/ConnectedTests/20260818-222312-449.json`이다.
- 콘텐츠: `PrototypeContentValidator` 오류 0. 전용 두 폭탄 asset/prefab, `prototype-boss-arena`의 셀·기둥·anchor·loop, `DungeonBoss` 단독 참조와 Build Settings 11씬을 확인했다.
- WebGL: `Artifacts/Verification/20260819-073000-boss-bombs-connected-web/`의 Development 빌드가 138,462,515 bytes·296.119초·오류 0으로 성공했다. 경고 352건은 기존 전체 shader/package 범주다.
- 키보드 browser smoke `41/41`: 1,283개 사건에서 throw arm/폭발 각 4회, chain arm/폭발·throw 연쇄 기폭 각 2회, 보스 격파·run 완료 각 1회와 Console/page error 0을 확인했다. 가상 Gamepad smoke도 `14/14`, Console/page error 0으로 통과했다.
- 시각 확인: `webgl-dungeon-boss-telegraph.png`에서 전용 11×9 방의 네 기둥, 보스·목적지 ghost, 외곽 실제 폭탄과 노란 십자 footprint, HUD·미니맵 비중첩을 확인했다.
- 미완료 판정: 퇴로 폭탄과 목적지 ghost를 사람이 동시에 읽는지, 회피 압력과 2.75초 Recovery가 재미있는지는 고정 빌드 사람 플레이테스트로 판정한다.
