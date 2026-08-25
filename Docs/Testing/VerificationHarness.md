# 검증 하네스

- 상태: `Accepted`
- 진입점: `Tools/Verify.ps1`

## 목적

사람과 AI가 같은 명령으로 정적 규칙, Unity 컴파일, 테스트, WebGL 빌드, 브라우저 스모크를 실행하고 동일한 구조의 증거를 남긴다. 낮은 검증 단계를 높은 단계로 잘못 보고하지 않는다.

## 사용법

Windows PowerShell 5.1 또는 PowerShell 7 이상에서 저장소 루트를 기준으로 실행한다.

```powershell
./Tools/Verify.ps1 -StaticOnly
./Tools/Verify.ps1 -Tier Fast
./Tools/Verify.ps1 -Tier Full
./Tools/Verify.ps1 -Tier Web
```

Unity가 기본 Hub 경로에 없다면 `-UnityPath` 또는 `UNITY_EDITOR_PATH`를 사용한다.

```powershell
./Tools/Verify.ps1 -Tier Fast -UnityPath 'D:/Tools/Unity/Editor/Unity.exe'
```

## 단계 계약

| 실행 | 포함 항목 | 성공 의미 |
|---|---|---|
| `-StaticOnly` | UTF-8/공백, Markdown 링크, 스킬 구조, asmdef 참조, Core 금지 API | 저장소 구조만 통과. Unity 검증 아님 |
| `Fast` | StaticOnly + Unity import/compile + Editor validator + `BombSwap.Core.Tests` | Core 반복 작업의 최소 검증 |
| `Full` | Fast + `BombSwap.Unity.Tests` PlayMode | Unity 통합 기능 완료의 최소 검증 |
| `Web` | Full + development WebGL build + browser smoke | 개발용 WebGL 기능 검증 |

`Web -SkipBrowserSmoke`는 빌드만 확인하는 부분 검증이다. summary status는 `Partial`, 프로세스 종료 코드는 `2`이며 Web 통과로 보고하면 안 된다.

## 종료 코드

| 코드 | 의미 |
|---:|---|
| 0 | 요청한 단계 전체 통과 또는 명시적 StaticOnly 통과 |
| 1 | 검사, 컴파일, 테스트, 빌드 또는 브라우저 실패 |
| 2 | 명시적 부분 검증. 현재는 browser smoke 생략 |
| 3 | 같은 프로젝트가 Unity Editor에서 열려 있어 batchmode 실행 차단 |

## 산출물

각 실행은 `Artifacts/Verification/<timestamp>-<tier>/`를 만들며 `.gitignore` 대상이다.

- `summary.json`: 요청 단계, 최종 상태, 종료 코드, 각 step 결과.
- `unity-compile.log`, `editor-validation.json`.
- `editmode-results.xml`, `playmode-results.xml`과 Unity 로그.
- Web 실행 시 `WebGLBuild/`, `webgl-build-report.json`, build 로그.
- browser 실행 시 `browser-smoke.json`, `playtest-events.json`, `playtest-log-summary.json`, `playtest-log-summary.md`, `gamepad-smoke.json`, `gamepad-paused.png`, `browser-smoke.log`.

실패한 실행의 산출물을 진단 전에 삭제하지 않는다.

## Unity 동시 실행 규칙

하네스는 `Temp/UnityLockfile`이 있으면 Unity 실행을 거부한다. 열려 있는 Editor를 닫고 실행하거나, 연결된 Unity 도구에서 컴파일/테스트를 수행한 뒤 정확한 증거를 별도로 보고한다. 같은 프로젝트에 두 번째 Unity 인스턴스를 띄우지 않는다.

연결된 Editor에서 공식 Unity MCP로 테스트를 시작하면 실행을 요청한 동적 콜백이 종료되거나 PlayMode 도메인 리로드로 사라질 수 있다. 각 테스트 어셈블리 내부의 `ConnectedEditModeResultReporter`와 `ConnectedPlayModeResultReporter`가 완료 여부를 독립적으로 Console에 남긴다.

`Assets/Game/Tests/EditorHarness/ConnectedTestHarness.cs`는 `[InitializeOnLoad]`로 Test Runner callback을 도메인 리로드 뒤 다시 등록하고 실행 ID, 모드, 상태, 시작·종료 시각, 통과·실패·건너뜀 수와 실패 목록을 다음 두 위치에 기록한다.

- 최신 상태: `Artifacts/Verification/connected-test-status.json`.
- 실행별 증거: `Artifacts/Verification/ConnectedTests/<runId>.json`.

메뉴 또는 공식 Unity MCP `Unity_RunCommand`에서 `BombSwap.Tests.Harness.ConnectedTestHarness.RunEditMode`, `BombSwap.Tests.Harness.ConnectedTestHarness.RunPlayMode`를 실행할 수 있다. 상태 파일의 `runId`가 요청과 같고 `state`가 더 이상 `Scheduled`/`Running`이 아니며 합계가 0보다 큰지 확인한다. 성공은 `state: Passed`, `failed: 0`으로 판정한다. 이 JSON은 연결 세션의 종료 증거를 안정화하지만, batchmode `Full`의 독립 import와 XML 결과를 대신하지 않는다.

합성 Input System 상태를 queue하는 PlayMode fixture는 background Game view에서도 결정론적으로 동작하도록 각 상태 전송 직전에 reader의 입력 focus를 명시적으로 활성화한다. 이동 helper는 긴 frame에서 정확히 한 셀 snapshot을 관찰할 수 있다고 가정하지 않고, 첫 권위 셀 변화가 확인되면 입력을 해제한 뒤 현재 위치에서 경로를 다시 계산한다. 이는 테스트 환경 보정이며 실제 runtime의 focus 상실 즉시 정지 계약을 우회하지 않는다.

Editor가 열려 있어 batchmode Web tier를 실행할 수 없을 때는 `Assets/Game/Editor/BuildAutomation/ConnectedWebGLBuildHarness.cs`를 사용한다. 일반 연결 작업은 `Bomb Swap/Verification/Build Development WebGL Connected` 메뉴가 시각이 포함된 artifact 경로를 만들고 예약한다. 자동화에서 경로를 직접 제어해야 하면 공식 Unity MCP `Unity_RunCommand`로 짧은 빌드에는 `BuildDevelopment(artifactsDirectory, buildPath)`, MCP 제한 시간을 넘길 수 있는 빌드에는 `ScheduleDevelopment(artifactsDirectory, buildPath)`를 호출한다. 표준 Build Settings 밖의 독립 플레이테스트 씬은 대응하는 `BuildDevelopmentScene(..., scenePath)` 또는 `ScheduleDevelopmentScene(..., scenePath)`를 사용해 씬 목록을 직렬화 변경하지 않고 한 씬만 빌드한다. 예약 경로는 컴파일된 `EditorApplication.update` callback으로 도구 응답과 분리해 실행을 이어가고 `webgl-build-status.txt`에 `Scheduled → Running → Passed | Failed`를 기록한다. 예약 뒤 스크립트 재컴파일이나 domain reload를 일으키지 말고 status가 종료 상태인지 확인한다. 모든 경로는 content validator를 먼저 실행하고 Development WebGL을 빌드한 뒤 `webgl-build-report.json`과 `WebGLBuild/`을 남기며 Editor를 종료하지 않는다. 최종 status, 성공 JSON과 `index.html`을 확인한 뒤 해당 browser smoke를 별도로 실행해야 하며, 연결 빌드만으로 Web tier 전체 통과를 주장하지 않는다.

EditMode 표식:

- `BOMBSWAP_EDITMODE_RESULT STARTED`: 발견된 테스트 수.
- `BOMBSWAP_EDITMODE_RESULT FINISHED`: passed/failed/skipped/inconclusive 요약.
- `BOMBSWAP_EDITMODE_RESULT FAILED`: 실패한 개별 테스트와 stack trace.

PlayMode 표식:

- `BOMBSWAP_PLAYMODE_RESULT STARTED`: 발견된 테스트 수.
- `BOMBSWAP_PLAYMODE_RESULT FINISHED`: passed/failed/skipped/inconclusive 요약.
- `BOMBSWAP_PLAYMODE_RESULT FAILED`: 실패한 개별 테스트와 stack trace.

연결된 Editor 검증은 실행 직전 Console을 비우고, 완료 뒤 이 표식과 일반 Error 항목을 함께 읽는다. 이 표식은 연결 검증의 증거이며 `Tools/Verify.ps1 -Tier Full`의 XML·로그 산출물을 대체하지 않는다.

## 브라우저 스모크 준비 상태

Web tier는 브라우저를 열기 전에 `Tools/WebGLTemplateTests.mjs`, `Tools/WebGLStaticServerTests.mjs`, `Tools/PlaytestLogAnalyzerTests.mjs`를 실행한다. template 검사는 Unity 필수 macro, 960×600 네이티브 상한을 둔 viewport fit 식, window/`ResizeObserver` 갱신, 고정 CSS 크기 회귀 금지와 빌드 뒤 `PlayerSettings.WebGL.template` 저장 복원을 확인한다. 기본 browser smoke는 로드 직후와 전체 게임 회귀 뒤 desktop·640px viewport에서 canvas 경계, 문서 overflow, aspect ratio와 네이티브 상한을 실제 DOM geometry로 검증한다.

표준 development build의 첫 씬은 `DungeonLobby`다. 기본·방향성 smoke는 `lobby-ready` 뒤 키보드 Enter로, 가상 Gamepad smoke는 실제 연결한 표준 South 버튼으로 `게임 시작`을 Submit하고 `lobby-start-requested`를 관찰한 뒤 기존 던전 probe를 기다린다. 기본 smoke는 최초 로비 screenshot을 남기고 보스 완료 화면의 선택을 `로비로 돌아가기`로 옮겨 terminal host 제거→두 번째 `lobby-ready`→새 시작방을 한 페이지에서 검증한다. 실패 결과의 `R`·Select 즉시 재시작 회귀는 그대로 유지한다.

`Tools/WebGLSmoke.mjs`는 `Tools/WebGLStaticServer.mjs`의 정적 서버와 Playwright를 사용한다. 같은 서버 모듈은 수동 관찰용 `Tools/ServeWebGL.mjs`에서도 사용하므로 자동·수동 실행의 MIME·압축·경로 경계가 갈라지지 않는다. 게임플레이 probe는 먼저 `probe-ready`, 현재 방별 `room-ready-<room-id>`, 그래프 방과 방문 상태를 나타내는 `dungeon-room-ready-<node-id>-<room-type>-<active|cleared|safe>`를 보내 Unity 런타임, 방 권위와 입력 구독이 준비됐음을 알린다. 이후 `move`, 입력 명령 `move-direction-*`, `interact`, 실제 frame 위치 변화 `move-motion-direction-*`, 논리 셀 경계 `move-step-direction-*`, 현재 셀 `player-cell-x-<x>-z-<z>`, 추격자 확정 셀 `chaser-cell-x-<x>-z-<z>`, 돌진형 `charger-track-moved`, `charger-telegraph-<direction>-distance-<cells>`, `charger-charge-moved`, `charger-recover`, 자폭병 확정 셀 `self-destruct-cell-x-<x>-z-<z>`, `self-destruct-moved`, `self-destruct-warning-chase`, `self-destruct-telegraph`, `self-destruct-armed`, `self-destruct-detonated`, `self-destruct-died`, 투척병 `thrower-cell-*`, `thrower-telegraph-*`, `thrower-bomb-launched`, `thrower-bomb-armed-*`, `thrower-bomb-detonated[-by-chain]`, 보스 입장 `boss-intro-started`·`boss-intro-completed`, `dungeon-transition-started`, `dungeon-room-committed`, 적 이동·상태, 폭탄 설치·폭발·피해·클리어, Core 전투 보상 snapshot `combat-reward-tokens-<count>`, 회복 `player-health-recovered-<amount>`·`recovery-consumed-room-<node-id>`, `swap-bomb`, `pause-entered`, `pause-resumed`, `pause-resume`, `audio-unlocked`, `bgm-audio-started` 사건을 문자열 또는 `{ name: string }` 형태로 제공해야 한다. pause 사건은 입력 callback이 아니라 세션의 확정 상태 전이를 뜻한다. `audio-unlocked`는 기존 입력 수신, `bgm-audio-started`는 BGM DSP 예약 시작 경계만 뜻하며 둘 다 실제 가청 출력을 증명하지 않는다. 방·셀 상세 marker는 Development WebGL build에서만 생성해 release와 Editor의 frame 반복 경로에 문자열 할당을 추가하지 않는다.

seed-0의 회전된 `prototype-combat-loop`는 BFS 추격자 회귀를 함께 검증한다. 보상으로 선택한 광역 폭탄을 `(0,-5)`에 설치하고 `(0,-4)`에서 추격자가 cardinal 인접할 때까지 기다린 뒤 `(3,-4)`로 이탈한다. 추격자가 첫 폭발 범위에 남아 방이 클리어되어야 한다. 과거 국소 Manhattan 왕복을 깨기 위해 플레이어 위치를 옮기거나 두 번째 폭탄 쿨다운을 기다리는 경로를 다시 도입하지 않는다.

현재 던전 개발 빌드는 `PrototypeInputHarnessProbe`, `PrototypeDungeonRunHost`, `BombSwapHarness.jslib`로 이 배열을 제공한다. Playwright는 `DungeonStart` canvas focus와 `dungeon-room-ready-1-start-safe`를 확인한 뒤 오른쪽 키 유지 중 브라우저 `blur`를 발생시켜 `move-direction-none`과 300ms 셀·motion 정지를 확인한다. 누락 key-up 상태로 `focus`를 복구해도 이동이 되살아나지 않아야 하며, 이어지는 `Esc` pause 성공이 입력 map 복구까지 증명한다. 그 뒤 `PAUSED` 화면을 캡처하고 방향키와 `Z`가 현재 셀·frame motion·폭탄 설치 수를 바꾸지 않는지 400ms 동안 확인한 뒤 재개한다. 첫 체력 probe run에서는 시작방 자기 폭발로 `5→4`, 첫 전투방 준비도 `4`임을 확인한다. seed-0 회전 `Pillars`의 방 준비 뒤 첫 돌진형 관련 행동이 `charger-telegraph`가 아니라 `charger-track-moved`인지 검사하고, 추격자 접촉 실패와 `R` 재시작으로 새 run `5`를 증명한다. 새 run의 첫 `Pillars` 전투방에서는 최신 직교축 우선과 빠른 방향 교대, 실제 폭탄 전투 클리어를 확인한다. 돌진형의 Telegraph·Charge 이동/목표 충돌·Recover 전체 상태·시간 분기는 Core/EditMode·Unity PlayMode가 소유하며, 브라우저 run에서 다른 적 점유나 선행 폭탄 처치 때문에 나타나지 않은 분기를 강제로 만들지 않는다. 첫 십자 폭탄은 설치 직후 `(3,2)` 측면 포켓으로 이탈하고, 두 번째는 중앙 아래쪽에서 배치·유도한다. 새 압력 때문에 클리어가 남았을 때만 중앙 유도 폭탄을 한 번 더 허용하며 고정 횟수보다 방 클리어 사건을 권위로 삼는다. 이어 서쪽 금 간 출구를 실제 십자 폭탄으로 파괴해 `secret-wall-revealed-room-2-direction-west`, 미니맵 `4방/3연결`, `dungeon-room-ready-10-secret-safe`를 확인한다. 중앙 cache 인접 셀에서 `E`의 `interact` 뒤 `secret-reward-collected-3`·`room-reward-tokens-4`를 확인하고 같은 입구로 복귀하며, 북쪽 `BombReward` 왼쪽 후보를 수집해 슬롯 2를 활성화한다. 클리어 방 왕복과 주 경로 4·5번 전투를 유지된 광역 폭탄으로 클리어하면 합계 marker는 `combat-reward-tokens-5 → 6`이다. 8번 Recovery에서 입장 체력을 유지하고 pickup 인접 셀의 `E`로 중앙 `+2`를 한 번 소비한 뒤 6번 보스 전실과 7번 보스방에 진입한다. 보스방에서는 `boss-intro-started → boss-intro-completed`를 기다린 뒤 첫 Telegraph 목적지에 광역 폭탄을 선행 설치하고 네 이동 `(1,1) → (1,0) → (1,-1) → (0,-1)`, 피해 4회, 2페이즈·격파·`run-completed`를 확인한다. 완료 뒤와 자기 폭발 실패 뒤 각 `R` 재시작은 Secret 공개·cache·토큰을 포함한 새 run 상태를 0으로 되돌려야 한다. 마지막으로 viewport resize와 Console/page error 0을 확인하고 금 간 벽·비밀방·pause·게이트·회복·보스 예고·완료·실패 화면을 캡처한다. 방 준비·이동·전환은 고정 지연이 아니라 현재 논리 셀, 적 셀과 scene commit 사건으로 동기화하며 fuse와 쿨타임 경계에만 명시적 시간을 사용한다. `swap-bomb`은 입력 수신만 뜻하고 `active-bomb-slot-1`은 Core 교체 성공을 뜻한다. `audio-unlocked`는 사용자 입력 수신 marker일 뿐 실제 오디오 출력 검증을 대체하지 않는다.

Gates 구간은 graph 연결 방향에 따라 저작 player spawn 대신 경계 입장 셀에서 시작할 수 있다. seed-0 smoke는 `room-ready-prototype-combat-gates` 직후 실제 현재 셀에서 오른쪽 아래 `(3,-3)`으로 Z축 우선 이동해 자폭병을 오른쪽 우회로 `(3,-1)`까지 끌어낸다. 그 셀 사건 뒤 플레이어가 왼쪽 아래 `(-1,-2)`로 X축 우선 이동하면 자폭병의 최단 추적이 아래 유도 anchor `(0,-2)`를 통과한다. 저체력 전체-run 자동화는 자폭병이 `(2,-2)`에 왔을 때 `(-1,-3)`으로 한 칸 sidestep했다가 다음 판단 전에 `(-1,-2)`로 복귀해 뒤따르는 추격자의 접촉만 피하고 유도 목표는 유지한다. 이어 `self-destruct-warning-chase`·`self-destruct-armed`·`self-destruct-telegraph`를 확인하고, 한 칸 떨어진 안전 셀 `(-1,-3)`으로 이탈해 0.75초 fuse를 실제로 기다린다. `self-destruct-detonated → bomb-exploded-definition-prototype-self-destruct-blast → self-destruct-died`와 진입 시점보다 정확히 하나 이상 증가한 `destructible-wall-destroyed`를 확인한 뒤, 같은 `(-1,-3)`에서 접근 중인 추격자가 인접할 때 광역 폭탄을 설치하고 `(-3,-4)`로 이탈해 `room-cleared`를 확인한다. 이 순서는 저체력 플레이어가 방을 가로질러 추격자와 교차하지 않으면서 자폭병의 2단계 유인·회피·경고·정지·환경 파괴를 각각 관찰한다.

개발 빌드에서 첫 harness 사건이 도착하면 WebGL footer의 `SAVE TEST LOG` 버튼이 활성화된다. 버튼은 외부 서버로 전송하지 않고 현재 페이지의 사건 배열을 `bombswap/playtest-log@1` JSON으로 로컬 다운로드한다. 파일에는 생성 시각, product 이름·버전, 사건 수와 `{ name, timestamp }` 사건만 포함하며 commit·빌드 보고서·관찰 메모는 세션 기록에서 별도로 고정한다. 기본 browser smoke는 전체 회귀 뒤 버튼을 실제 클릭해 `playtest-events.json`을 받고, schema·build identity·사건 수와 내려받은 사건 전체가 클릭 직전 메모리 snapshot과 같은지 검증한다. Release WebGL에서는 C# reporter가 호출되지 않으므로 버튼도 계속 숨겨져 있어야 한다.

`Tools/PlaytestLogAnalyzerTests.mjs`는 Web tier의 browser 준비 단계에서 schema 불일치, 사건 수 불일치, 잘못된 사건 형식과 시간 역행을 거부하는 계약을 검사한다. 키보드 smoke가 실제 `playtest-events.json`을 만든 뒤 `Tools/AnalyzePlaytestLog.mjs`가 같은 파일을 다시 검증하고 결정론적인 `bombswap/playtest-summary@2` JSON·Markdown을 생성해야 Gamepad smoke로 진행한다. 요약은 런 시작·완료·실패, 방 방문 순서, Secret·미니맵·Recovery와 보스 encounter 시간·phase/state/source/폭탄 정의별 적중·보스 패턴 피격·과열·자폭병 적중을 정리한다. 폭탄 정의 교대는 성공 적중 순서일 뿐이며 플레이어 의도, parity 안전 칸 재사용, 가독성과 재미를 판정하지 않는다. 상세 marker가 없는 이전 `playtest-log@1` 입력은 거부하지 않고 보스 피해를 미분류로 남긴다.

체력 persistence 회귀는 각 session 준비와 적용 피해에서 `player-health-current-<count>` marker를 기록한다. 기본 smoke는 첫 run 시작방의 `5`, 자기 폭발 뒤 `4`, 다음 전투방 준비의 `4`를 순서대로 요구해 scene 전환 자동 회복이 없음을 증명한다. 이어 첫 run을 추격자 접촉으로 실패시키고 페이지 reload 없이 `R`로 재시작해 다시 `5`가 되는지 확인한 뒤, 새 run으로 전체 던전 회귀를 수행한다. 완료 뒤와 의도적 자기 폭발 실패 뒤의 각 `R`도 새 run `5`를 요구한다. 무적 종료 시각과 처리한 폭발 ID는 방 로컬이므로 marker는 run 현재 체력만 나타낸다.

Recovery 회귀는 5번 전투방 클리어 시점의 손상됐지만 살아 있는 체력을 읽어 회복방 입장 직후에도 동일한지 확인한다. 중앙 pickup의 cardinal 인접 셀에서 `E`를 누른 뒤 기대값 `min(최대 체력, 진입 체력 + 2)`와 실제 회복량 marker를 요구하고, pickup 소비 뒤 추가 회복이 없는지 확인한다. 현재 seed-0 WebGL 경로의 관찰값은 `1→3`이며 튜닝 변경 시 하네스는 고정 시작 체력이 아니라 계산된 기대값을 사용한다. 최대 체력 미소비와 재입장 단일 소비는 실제 scene PlayMode 테스트가 소유한다.

제한 정보 미니맵 회귀는 `minimap-current-room-<id>`, `minimap-visible-rooms-<count>`, `minimap-visible-connections-<count>`를 실제 Core snapshot 재구성 뒤 기록한다. 기본 smoke는 시작 `1/2/1`, Secret 공개와 입장 `2 또는 10/4/3`, Recovery `8/9/8`, 보스 전실 `6/10/9`를 요구하고 완료·실패 뒤 두 번의 페이지 reload 없는 재시작마다 다시 `1/2/1`이 증가했는지 확인한다. 이 marker는 공개 개수와 현재 방을 증명하며 글자·연결선·토큰 HUD 비중첩은 Secret·Recovery·보스 예고·pause 캡처로 확인한다. 미방문 방 종류, frontier 너머 연결과 아직 열지 않은 다른 Secret 입구 비공개는 EditMode snapshot 테스트가 소유한다.

방 보상 회귀는 첫 일반 전투 클리어 `combat-reward-tokens-1`, Secret cache `room-reward-tokens-4`, 이후 두 일반 전투 클리어 `combat-reward-tokens-5 → 6`을 순서대로 기다린다. 보스 격파는 값을 늘리지 않으며 완료·실패 뒤 각 `R` 재시작은 새 시작방의 추가 `combat-reward-tokens-0`과 `room-reward-tokens-0` marker로 새 Core run 상태를 증명한다.

`Tools/ArmoredWebGLSmoke.mjs`는 `TestSandboxArmor`를 첫 씬으로 사용하는 별도 development 빌드에서 장갑병 가설만 검증한다. 정식 Build Settings와 콘텐츠 검증 순서는 바꾸지 않고, 필요하면 던전 host·binder·minimap·완료 presenter를 제거한 일회성 검증 scene 사본을 빌드한 뒤 삭제한다. canvas focus와 `room-ready-prototype-combat-armor` 뒤 시작 십자 폭탄으로 `armored-broken → armored-panic-telegraph-east-distance-3`을 기다리고, 플레이어를 T 교차점 남쪽 포켓과 예고 도착점 옆으로 이동시켜 다른 셀에 두 번째 폭탄을 설치한다. `armored-panic-run-moved → armored-panic-recover → armored-chase → armored-died → enemy-died` 순서, 예고·최종 screenshot, browser Console/page error 0을 구조화된 JSON으로 남긴다. 필수 추격자가 남을 수 있으므로 이 전용 smoke는 `room-cleared`를 요구하지 않으며 다중 적 최종 클리어는 PlayMode와 기본 던전 smoke가 소유한다.

`Tools/ThrowerWebGLSmoke.mjs`는 `ThrowerLanesPlaytest` 한 씬만 사용하는 별도 development 빌드에서 투척병 연결을 검증한다. staging `(3,2)`에서 첫 사격 anchor `(0,3)`까지 `thrower-cell-x-*`가 staging 포함 최소 5개 고유 셀을 기록하고 1회성 최초 이동 표식 `thrower-track-moved`가 첫 Telegraph보다 앞서는지 확인한다. 시작 플레이어 `(0,-2)`와 다른 중앙 `(0,0)`·측면 `(-3,-2)/(2,-3)` 첫 목표 3개가 논리적으로 함께 잠기는지 telemetry marker로 확인하고 시작 셀에 즉시 범위 2 십자 폭탄을 설치해 중앙 착탄보다 먼저 fuse를 예약한다. 이 `thrower-telegraph-x-*` marker는 월드 착지 셀 표시가 아니며 presenter는 Telegraph와 비행 중 목표 셀을 표시하지 않는다. 세 marker 뒤 `thrower-bomb-launched`와 `thrower-bomb-armed-definition-prototype-thrower-blocker`가 각각 3번 발생하고 `thrower-bomb-detonated-by-chain`으로 이어져야 한다. 첫 폭탄들이 해결되고 다음 사격 anchor로 이동한 뒤에는 중앙 `(0,0)`을 유지하면서 측면이 `(0,2)/(-4,1)`로 교체되는 두 번째 잠금을 추가로 기다린다. 전체 순서와 browser Console/page error 0을 구조화된 JSON으로 남긴다. 이 검사는 입장 선행 이동·세 저작 목표 잠금·세 비행→논리 착탄·공용 연쇄·연속 volley 비반복을 증명하지만 Throw/비행 가독성, 착탄 뒤 회피 공정성과 위협도는 판정하지 않는다.

메인 `Tools/WebGLSmoke.mjs`의 seed-0 첫 전투는 `TestSandboxThrower`의 Clockwise90 배정이다. 안전 Start 방에서 빠른 직교 입력을 먼저 검증하고, 던전 전환 뒤 `room-ready-prototype-combat-thrower` 이후 최초 플레이어 `(4,0)`·투척병 `(2,-3)` marker와 Manhattan 5칸 입장 여유를 요구한다. 이어 `thrower-cell-x-*` 고유 셀 5개로 투척병이 첫 사격 anchor `(3,0)`까지 네 칸 Track한 뒤에만 Telegraph하고 세 launch를 만드는 순서를 요구한다. 실제 십자 폭탄으로 두 적 사망과 `room-cleared`를 확인하며 같은 회전 방에서 Secret 서쪽 문 공개·왕복·북쪽 보상방 진입을 확인한다. 다음 `TestSandboxPillars`에서는 돌격병의 첫 행동 Track과 추격자/돌격병 순차 처치를 검증한다.

`Tools/DirectionalLineWebGLSmoke.mjs`는 기본 던전 development 빌드에서 첫 전투를 실제로 클리어한 뒤 `BombReward`의 오른쪽 `prototype-line` 후보를 수집한다. 슬롯 2로 교체하고 동쪽을 바라본 상태에서 설치한 뒤 북쪽 이동 명령을 보내도 `bomb-reward-selected-prototype-line → active-bomb-slot-1 → line-bomb-placed-east → move-direction-north → line-bomb-exploded-east` 순서가 유지돼야 한다. 전용 screenshot과 `directional-line-smoke.json`에 browser Console/page error 0을 함께 기록한다. 이 smoke는 설치 방향 고정을 증명하지만 네 방향 전체 셀 집합은 EditMode 테스트가 소유한다.

`Tools/GamepadWebGLSmoke.mjs`는 페이지 script보다 먼저 표준 매핑 가상 Gamepad를 `navigator.getGamepads()`에 주입하고 Unity 입력 초기화 뒤 실제 `gamepadconnected` 사건을 발생시킨다. 왼쪽 스틱 East와 중립, D-pad North와 해제가 각각 `Move`와 `Move(None)`을 만들고 West가 교체, North가 상호작용 명령을 만드는지 확인한다. 동쪽 스틱을 유지한 채 `gamepaddisconnected`를 발생시키면 `Move(None)` 뒤 300ms 동안 논리 셀·동쪽 step이 불변이어야 하며, 같은 index 장치를 재연결하면 서쪽 입력의 실제 한 셀 이동과 중립 정지가 복구돼야 한다. 이어 동쪽 스틱을 유지한 채 Start로 pause하면 500ms 동안 논리 셀·동쪽 step·폭탄 설치 수가 불변이어야 하며, Start 재개 뒤 같은 스틱 방향이 다시 명령·실제 한 셀 이동으로 적용되고 중립에서 멈춰야 한다. pause 메뉴에서 South는 선택된 `게임 계속` 같은 UI 버튼의 Submit이므로 정지 gameplay 검사 입력으로 사용하지 않는다. South로 시작 폭탄을 다섯 번 실제 설치·폭발시켜 bomb-explosion run 실패를 만든 뒤 Select가 페이지 reload 없이 새 start-safe 런을 준비해야 한다. 결과는 `gamepad-smoke.json`과 `gamepad-paused.png`에 browser Console/page error와 함께 기록한다. 이 검사는 브라우저 Gamepad API→Emscripten→Unity Input System→`PlayerCommand`·Core 결과 경로를 증명하지만, 물리 컨트롤러 연결·장치별 버튼 표기·deadzone·조작감을 대체하지 않는다.

Playwright를 찾지 못하면 Node의 `CODEX_NODE_MODULES`가 Playwright 모듈을 포함한 경로를 가리키게 하거나 프로젝트 개발 의존성 도입을 별도 승인받는다. 브라우저는 설치된 Edge/Chrome을 자동 탐색하며, 별도 위치는 `BOMBSWAP_BROWSER_PATH`로 지정한다.

정적 서버만 빠르게 회귀 검사하려면 `node Tools/WebGLStaticServerTests.mjs`를 실행한다. 표준 `-Tier Web`은 이 테스트와 기본 키보드 smoke, 가상 표준 게임패드 smoke를 순서대로 자동 실행하고 같은 `browser-smoke.log`에 결과를 남긴다. StaticOnly는 필수 서버·CLI·테스트·전용 smoke 파일의 존재와 일반 텍스트 계약만 확인하며 Node 런타임 테스트를 통과했다는 의미가 아니다. 수동 서버에서 게임이 열리는 사실도 Web 검증 통과가 아니며, 기본 Start→메인 투척병→Pillars→Gates→Recovery→Boss graph 자동 입력 회귀와 별도 갑옷·투척병 시작 씬·방향성 직선 폭탄·가상 게임패드·Console 검증은 각 browser smoke 결과로 증명해야 한다. 사람 관찰 실행법은 [로컬 WebGL 관찰 세션](../Playtesting/ManualWebGLRun.md)을 따른다.

## 공식 참고

- [Unity command-line build](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)
- [Unity Test Framework command-line arguments](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-command-line.html)
- [Unity BuildReport](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Reporting.BuildReport.html)
