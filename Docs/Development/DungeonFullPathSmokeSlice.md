# seed-0 전체 던전 WebGL 스모크 작업 계약

- 상태: `Implemented / WebGL Verified`
- 대상: Development WebGL 자동 회귀
- 관련: [던전 생성](../Systems/DungeonGeneration.md), [던전 씬 수명](DungeonSceneLifetimeSlice.md), [검증 하네스](../Testing/VerificationHarness.md)

## 목적

첫 전투와 보상 획득만 검증하던 브라우저 경로를 실제 seed-0 보스 주 경로 끝까지 확장한다. 한 브라우저 세션 안에서 씬 전환, 전투 잠금과 클리어, 클리어 방 재입장, 보상 loadout의 run 수명, 보스 전실과 보스 placeholder 연결이 함께 유지되는지를 증명한다.

## 고정 경로

`prototype-tree-v1`, seed 0의 주 경로와 현재 콘텐츠 배정은 다음과 같다.

| 노드 | 방 타입 | 좌표 | 콘텐츠 | 회전 |
|---:|---|---|---|---|
| 1 | Start | `(0,0)` | `DungeonStart` | - |
| 2 | Combat | `(-1,0)` | `prototype-combat-pillars` / `TestSandboxPillars` | Clockwise90 |
| 3 | BombReward | `(-1,1)` | `DungeonReward` | - |
| 4 | Combat | `(-1,2)` | `prototype-combat-loop` / `TestSandbox` | Clockwise90 |
| 5 | Combat | `(0,2)` | `prototype-combat-gates` / `TestSandboxGates` | None |
| 6 | BossAntechamber | `(1,2)` | `DungeonBossAnte` | - |
| 7 | Boss | `(1,1)` | `DungeonBoss` | - |

3번 보상방에서 갈라지는 8번 선택 전투방은 `prototype-combat-lanes` / `TestSandboxLanes`, None 회전으로 배정되며 이 주 경로 smoke의 대상이 아니다. 현재 seed 0에서는 `prototype-combat-armor`가 배정되지 않는다.

## 자동 계약

1. Start에서 첫 전투방으로 이동하고 빠른 직교 방향 전환의 frame motion을 기존 방식으로 회귀한다.
2. 시작 십자 폭탄 두 번으로 첫 전투를 클리어한다.
3. 보상방 왼쪽 후보 `prototype-area`를 수집한다.
4. 첫 전투방으로 되돌아가 `cleared` 상태와 적 사건 미발생을 확인한 뒤 보상방에 재진입한다.
5. 4번 회전 루프와 5번 중앙 게이트 전투방을 광역 폭탄으로 클리어한다. `player-cell-*`와 `chaser-cell-*`을 이용해 실제 논리 인접 상태를 기준으로 폭탄 설치와 퇴로를 동기화하며, 게이트 방에서는 서쪽 우회로를 사용한다.
6. 보스 전실의 `safe`, 보스 placeholder의 `active` 상태를 확인한다.
7. 보스방에서 2번 슬롯을 활성화하고 광역 폭탄을 설치해 선택 결과가 전체 경로 동안 유지됐음을 확인한다.
8. 전환 시작 8회와 commit 8회, 전투 클리어 3회, Console/page error 0, pause/resume, viewport resize와 최종 screenshot을 요구한다.

## 개발 전용 진단 marker

`PrototypeInputHarnessProbe`는 추격자의 실제 Core 확정 이동마다 `chaser-cell-x-<x>-z-<z>`를 보고한다. `WebGlHarnessReporter`의 컴파일 조건은 `UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD`이므로 release와 Editor frame 반복 경로에는 marker 문자열 할당을 추가하지 않는다.

이 marker는 자동 입력을 Transform 추정이나 고정 대기 시간에서 분리하기 위한 관측 수단이다. Core 상태의 권위나 게임 규칙을 바꾸지 않는다.

## 발견한 AI 위험

4번 전투방에서 플레이어가 `(-3,-4)`에 있고 추격자가 x=1 열에 있을 때, 국소 Manhattan 거리와 기존 방향/`North → East → South → West` 동률 규칙 때문에 추격자가 `(1,-3)`, `(1,-4)`, `(1,-5)` 사이를 반복했다. 자동 smoke는 플레이어를 `(-3,-5)`로 한 칸 이동해 동률을 깨고 계속 진행한다.

이 경로 조정은 AI 결함 수정이 아니다. 반복 상태 감지, BFS 거리장 또는 동률 정책 변경은 사람 플레이에서 막힘의 빈도와 공정성 영향을 확인한 뒤 별도 gameplay change로 다룬다.

## 검증 증거

- 전체 EditMode 267/267: `Artifacts/Verification/ConnectedTests/20260814-200957-559.json`.
- 전체 PlayMode 110/110: `Artifacts/Verification/ConnectedTests/20260814-201015-022.json`.
- `PrototypeContentValidator` 오류 0, Unity Console Error 0.
- Development WebGL 9개 씬 증분 완료 빌드: `Artifacts/Verification/20260815-052000-fifth-combat-room-web/`, 141,882,145 bytes, 8.155초, warning 0, error 0.
- Edge headless: 27/27, transition/commit 각 8회, room clear 3회, reward selection 1회, 게이트 방 캡처와 Console/page error 0.
- 게이트 화면: 같은 폴더의 `webgl-fifth-combat-room-gates-room.png`에서 회색 장벽 2열, 중앙 파괴 문 2개, 좌우 우회와 체력·무기 HUD 비중첩을 확인했다.
- 최종 StaticOnly와 WebGL 정적 서버 회귀: `Artifacts/Verification/20260815-053211-static/`, 통과.

## 비목표

- 다음 층 전환.
- 8번 선택 가지 자동 탐색.
- 추격자의 동률 순환 정책 수정.
- 탐색 피로, 폭탄 선택과 전투 감각에 대한 사람 판정.
