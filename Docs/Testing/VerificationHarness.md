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
- browser 실행 시 `browser-smoke.json`, `browser-smoke.log`.

실패한 실행의 산출물을 진단 전에 삭제하지 않는다.

## Unity 동시 실행 규칙

하네스는 `Temp/UnityLockfile`이 있으면 Unity 실행을 거부한다. 열려 있는 Editor를 닫고 실행하거나, 연결된 Unity 도구에서 컴파일/테스트를 수행한 뒤 정확한 증거를 별도로 보고한다. 같은 프로젝트에 두 번째 Unity 인스턴스를 띄우지 않는다.

연결된 Editor에서 공식 Unity MCP로 테스트를 시작하면 실행을 요청한 동적 콜백이 종료되거나 PlayMode 도메인 리로드로 사라질 수 있다. 각 테스트 어셈블리 내부의 `ConnectedEditModeResultReporter`와 `ConnectedPlayModeResultReporter`가 완료 여부를 독립적으로 Console에 남긴다.

`Assets/Game/Tests/EditorHarness/ConnectedTestHarness.cs`는 `[InitializeOnLoad]`로 Test Runner callback을 도메인 리로드 뒤 다시 등록하고 실행 ID, 모드, 상태, 시작·종료 시각, 통과·실패·건너뜀 수와 실패 목록을 다음 두 위치에 기록한다.

- 최신 상태: `Artifacts/Verification/connected-test-status.json`.
- 실행별 증거: `Artifacts/Verification/ConnectedTests/<runId>.json`.

메뉴 또는 공식 Unity MCP `Unity_RunCommand`에서 `BombSwap.ConnectedTestHarness.RunEditMode`, `BombSwap.ConnectedTestHarness.RunPlayMode`를 실행할 수 있다. 상태 파일의 `runId`가 요청과 같고 `state`가 더 이상 `Scheduled`/`Running`이 아니며 합계가 0보다 큰지 확인한다. 성공은 `state: Passed`, `failed: 0`으로 판정한다. 이 JSON은 연결 세션의 종료 증거를 안정화하지만, batchmode `Full`의 독립 import와 XML 결과를 대신하지 않는다.

Editor가 열려 있어 batchmode Web tier를 실행할 수 없을 때는 `Assets/Game/Editor/BuildAutomation/ConnectedWebGLBuildHarness.cs`의 `BuildDevelopment(artifactsDirectory, buildPath)`를 공식 Unity MCP `Unity_RunCommand`에서 호출할 수 있다. 이 경로는 content validator를 먼저 실행하고 enabled scene 전체로 Development WebGL을 빌드한 뒤 `webgl-build-report.json`과 `WebGLBuild/`를 남기며 Editor를 종료하지 않는다. 성공 JSON과 `index.html`을 확인한 뒤 같은 `Tools/WebGLSmoke.mjs`를 별도로 실행해야 하며, 연결 빌드만으로 Web tier 전체 통과를 주장하지 않는다.

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

`Tools/WebGLSmoke.mjs`는 `Tools/WebGLStaticServer.mjs`의 정적 서버와 Playwright를 사용한다. 같은 서버 모듈은 수동 관찰용 `Tools/ServeWebGL.mjs`에서도 사용하므로 자동·수동 실행의 MIME·압축·경로 경계가 갈라지지 않는다. 게임플레이 probe는 먼저 `probe-ready`, 현재 방별 `room-ready-<room-id>`, 그래프 방과 방문 상태를 나타내는 `dungeon-room-ready-<node-id>-<room-type>-<active|cleared|safe>`를 보내 Unity 런타임, 방 권위와 입력 구독이 준비됐음을 알린다. 이후 `move`, 입력 명령 `move-direction-*`, 실제 frame 위치 변화 `move-motion-direction-*`, 논리 셀 경계 `move-step-direction-*`, 현재 셀 `player-cell-x-<x>-z-<z>`, 추격자 확정 셀 `chaser-cell-x-<x>-z-<z>`, `dungeon-transition-started`, `dungeon-room-committed`, 적 이동·상태, 폭탄 설치·폭발·피해·클리어, `swap-bomb`, `pause-resume`, `audio-unlocked` 사건을 문자열 또는 `{ name: string }` 형태로 제공해야 한다. 방·셀 상세 marker는 Development WebGL build에서만 생성해 release와 Editor의 frame 반복 경로에 문자열 할당을 추가하지 않는다.

현재 던전 개발 빌드는 `PrototypeInputHarnessProbe`, `PrototypeDungeonRunHost`, `BombSwapHarness.jslib`로 이 배열을 제공한다. Playwright는 `DungeonStart` canvas focus와 `dungeon-room-ready-1-start-safe`를 확인하고 seed-0 서쪽 출구까지 저작 장애물을 우회한다. 첫 전투방의 `dungeon-room-ready-2-combat-active` 뒤 겹친 두 키의 최신 직교축 우선과 빠른 방향 교대를 frame motion으로 검증하고, 시작 슬롯의 십자 폭탄을 두 번 설치해 두 적을 처치하고 `room-cleared`를 만든다. 이어 북쪽 `BombReward`의 `dungeon-room-ready-3-bomb-reward-safe`에서 왼쪽 후보를 수집해 `bomb-reward-selected-prototype-area`를 확인하고, 남쪽으로 돌아온 첫 전투방의 `dungeon-room-ready-2-combat-cleared`와 적 사건 미발생, 보상방 재진입을 확인한다. 이후 주 경로의 4·5번 전투방을 선택한 광역 폭탄과 추격자 셀 marker로 클리어하고 6번 보스 전실과 7번 보스 placeholder에 진입한다. 보스방에서 다시 광역 폭탄을 설치해 선택 loadout이 전체 run 동안 유지됐음을 증명한다. 한 세션의 graph 전환과 commit은 각각 8회, 전투 클리어는 3회다. 마지막으로 pause/resume, viewport resize와 Console/page error 0을 확인하고 `webgl-boss-path.png` 또는 `--screenshotPath`에 화면을 남긴다. 방 준비·이동·전환은 고정 지연이 아니라 가장 최근 전투방 이후의 현재 논리 셀, 적 셀과 scene commit 사건으로 동기화하며 fuse와 쿨타임 경계에만 명시적 시간을 사용한다. `swap-bomb`은 입력 수신만 뜻하고 `active-bomb-slot-1`은 Core 교체 성공을 뜻한다. `audio-unlocked`는 사용자 입력 수신 marker일 뿐 실제 오디오 출력 검증을 대체하지 않는다.

`Tools/ArmoredWebGLSmoke.mjs`는 `TestSandboxArmor`를 첫 enabled 씬으로 재정렬한 별도 development 빌드에서 갑옷 가설만 검증한다. canvas focus와 `room-ready-prototype-combat-armor` 뒤 실제 두 폭탄을 설치하고, 첫 폭발의 `armored-broken`과 그 전후 `armored-moved`, 두 번째 폭발의 `armored-died`, 일반 `enemy-died`, `room-cleared` 순서를 확인한다. 전용 screenshot, browser Console/page error 0과 함께 구조화된 JSON 결과를 남긴다.

Playwright를 찾지 못하면 Node의 `CODEX_NODE_MODULES`가 Playwright 모듈을 포함한 경로를 가리키게 하거나 프로젝트 개발 의존성 도입을 별도 승인받는다. 브라우저는 설치된 Edge/Chrome을 자동 탐색하며, 별도 위치는 `BOMBSWAP_BROWSER_PATH`로 지정한다.

정적 서버만 빠르게 회귀 검사하려면 `node Tools/WebGLStaticServerTests.mjs`를 실행한다. 표준 `-Tier Web`은 이 테스트를 browser smoke 직전에 자동 실행하고 같은 `browser-smoke.log`에 결과를 남긴다. StaticOnly는 필수 서버·CLI·테스트·갑옷 smoke 파일의 존재와 일반 텍스트 계약만 확인하며 Node 런타임 테스트를 통과했다는 의미가 아니다. 수동 서버에서 게임이 열리는 사실도 Web 검증 통과가 아니며, 기본 Start→배정 전투방 graph 자동 입력 회귀와 별도 갑옷 시작 씬·Console 검증은 각 browser smoke 결과로 증명해야 한다. 사람 관찰 실행법은 [로컬 WebGL 관찰 세션](../Playtesting/ManualWebGLRun.md)을 따른다.

## 공식 참고

- [Unity command-line build](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)
- [Unity Test Framework command-line arguments](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-command-line.html)
- [Unity BuildReport](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Reporting.BuildReport.html)
