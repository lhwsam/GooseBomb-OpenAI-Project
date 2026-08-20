# seed-0 전체 던전 WebGL 스모크 작업 계약

- 상태: `Implemented / WebGL Verified`
- 대상: Development WebGL 자동 회귀
- 관련: [던전 생성](../Systems/DungeonGeneration.md), [던전 씬 수명](DungeonSceneLifetimeSlice.md), [검증 하네스](../Testing/VerificationHarness.md)

## 목적

첫 전투와 보상 획득만 검증하던 브라우저 경로를 실제 seed-0 보스 주 경로 끝까지 확장한다. 한 브라우저 세션 안에서 씬 전환, 전투 잠금과 클리어, 클리어 방 재입장, 보상 loadout의 run 수명, 보스 전실과 보스 placeholder 연결이 함께 유지되는지를 증명한다.

## 고정 경로

`prototype-secret-v3`, seed 0의 전체 그래프와 현재 콘텐츠 배정은 다음과 같다.

| 노드 | 방 타입 | 좌표 | 콘텐츠 | 회전 |
|---:|---|---|---|---|
| 1 | Start | `(0,0)` | `DungeonStart` | - |
| 2 | Combat | `(-1,0)` | `prototype-combat-thrower` / `TestSandboxThrower` | Clockwise90 |
| 3 | BombReward | `(-1,1)` | `DungeonReward` | - |
| 4 | Combat | `(-1,2)` | `prototype-combat-pillars` / `TestSandboxPillars` | Clockwise90 |
| 5 | Combat | `(0,2)` | `prototype-combat-gates` / `TestSandboxGates` | None |
| 6 | BossAntechamber | `(1,2)` | `DungeonBossAnte` | - |
| 7 | Boss | `(1,1)` | `DungeonBoss` | - |
| 8 | Recovery | `(0,3)` | `DungeonRecovery` | - |
| 9 | Combat | `(-2,1)` | `prototype-combat-loop` / `TestSandbox` | None |
| 10 | Secret | `(-2,0)` | `DungeonSecret` | - |

9번 선택 전투방은 주 경로 smoke의 자동 전투 대상이 아니며, 10번 Secret은 2번 전투방의 금 간 서쪽 문으로 공개하고 왕복한다. 현재 seed 0에서는 `prototype-combat-armor`가 배정되지 않는다. Legacy `prototype-combat-lanes`는 메인 카탈로그 밖이므로 어떤 노드에도 배정되지 않는다.

## 자동 계약

1. 안전 Start 방에서 빠른 직교 방향 전환의 frame motion과 즉시 정지를 회귀한 뒤 첫 전투방으로 이동한다.
2. Clockwise90 투척병 방에서 플레이어 `(4,0)`과 투척병 `(2,-3)`의 Manhattan 5칸 입장 여유를 확인하고, 단일 Telegraph 뒤 서로 다른 세 폭탄 launch와 시작 십자 폭탄 2개로 추격자와 투척병을 실제 처치한다.
3. 보상방 왼쪽 후보 `prototype-area`를 수집한다.
4. 첫 전투방으로 되돌아가 `cleared` 상태와 적 사건 미발생을 확인한 뒤 보상방에 재진입한다.
5. 4번 Pillars에서 돌격병의 첫 행동이 Telegraph가 아닌 Track인지 확인하고 광역/십자 폭탄으로 추격자와 돌격병을 순차 처치한다. 5번 Gates는 광역 폭탄과 자폭병 유도로 클리어한다.
6. 8번 Recovery 우회에서 현재 체력만 회복되고 1회 소비가 유지되는지 확인한 뒤 6번 보스 전실과 7번 보스방으로 진행한다.
7. 보스 패턴·상시 피해·최후 발악·클리어, run 완료와 같은 페이지의 새 run 재시작을 확인한다.
8. 전체 경로, Secret·미니맵·보상·체력·활성 슬롯 persistence, Console/page error 0, pause/resume, viewport resize, 플레이테스트 로그 다운로드/분석과 최종 screenshot을 요구한다.

## 개발 전용 진단 marker

`PrototypeInputHarnessProbe`는 추격자의 실제 Core 확정 이동마다 `chaser-cell-x-<x>-z-<z>`를 보고한다. `WebGlHarnessReporter`의 컴파일 조건은 `UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD`이므로 release와 Editor frame 반복 경로에는 marker 문자열 할당을 추가하지 않는다.

이 marker는 자동 입력을 Transform 추정이나 고정 대기 시간에서 분리하기 위한 관측 수단이다. Core 상태의 권위나 게임 규칙을 바꾸지 않는다.

## 발견한 경로 위험과 해소

과거 4번 전투방에서 플레이어가 `(-3,-4)`에 있고 추격자가 x=1 열에 있을 때, 국소 Manhattan 거리와 기존 방향/`North → East → South → West` 동률 규칙 때문에 추격자가 `(1,-3)`, `(1,-4)`, `(1,-5)` 사이를 반복했다. 후속 BFS 거리장 변경으로 이 원인을 차단했다.

메인 투척병 편성 뒤 2번 방은 서쪽 진입에 의해 Clockwise90으로 회전해 플레이어가 `(4,0)`에서 시작한다. 최초 투척병 `(3,0)`은 바로 인접해 준비 여유가 없다는 사람 피드백을 받아 staging을 회전 후 `(2,-3)`의 5칸 거리로 옮겼다. 후속 플레이에서 staging이 첫 사격 anchor이기도 해 즉시 투척하고 가까운 추격자를 자동 처치하는 문제가 확인되어, 추격자를 `(2,2)`, 첫 사격 anchor를 `(3,0)`으로 분리했다. 자동 smoke는 최초 셀 쌍과 첫 Telegraph 전 최소 네 Track 이동을 명시적으로 확인한다. 내부 벽을 가로지르는 Secret 복귀·북쪽 출구 경로는 외곽 통로를 사용한다. 4번 Pillars는 광역 폭탄 설치 뒤 추격자 반대쪽으로 이탈하고, 필요하면 돌격 예고 지점에 십자 폭탄을 추가해 두 적의 실제 클리어를 요구한다.

## 검증 증거

- staging·추격자 재배치 뒤 연결 Unity 전체 EditMode `344/344`, PlayMode `134/134`, 콘텐츠 validator와 Console Error 0이 통과했다. `Artifacts/Verification/20260820-133000-thrower-entry-safety-connected-web/`의 표준 11씬 Development WebGL은 139,085,389 bytes·425.102초·error 0으로 성공했고 Edge keyboard `48/48`이 seed-0 투척병의 네 칸 선행 Track, 전체 전투·Secret·보스·재시작과 2,472개 사건, Console/page error 0을 확인했다.
- 연결 Unity 6000.5.3f1 전체 EditMode `343/343`, PlayMode `134/134`, `PrototypeContentValidator`, Unity Console Error 0. 연결 증거는 `Artifacts/Verification/ConnectedTests/20260820-033842-313.json`, `Artifacts/Verification/ConnectedTests/20260820-033855-309.json`이다.
- `Artifacts/Verification/20260820-124012-connected-web/`의 표준 11씬 Development WebGL은 139,085,332 bytes·403.033초·error 0으로 성공했고 build report에 `TestSandboxThrower`가 포함됐다. clean 전체 셰이더 재컴파일의 기존 Sentis·vendor·TextMeshPro 범주 warning은 351건이다.
- Edge headless 키보드 `48/48`, 2,531개 플레이테스트 사건과 `summary@2` 분석, Console/page error 0이 통과했다. 실제 5칸 입장 여유, 투척병 Telegraph/3 launch/실제 사망, Secret 왕복, Pillars 두 적, Gates, Recovery, 보스와 새 run을 포함한다.
- 같은 빌드의 가상 표준 Gamepad `14/14`, Console/page error 0이 통과했다. WebGL template·정적 서버·playtest analyzer Node 회귀도 통과했다.
- 최종 StaticOnly은 `Artifacts/Verification/20260820-125842-static/summary.json`에서 통과했다.

## 비목표

- 다음 층 전환.
- 9번 선택 전투 가지 자동 탐색.
- 추격자의 동률 순환 정책 수정.
- 탐색 피로, 폭탄 선택과 전투 감각에 대한 사람 판정.
