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

`Tools/WebGLSmoke.mjs`는 정적 서버와 Playwright를 사용한다. 게임플레이 probe는 먼저 `probe-ready`를 보내 Unity 런타임과 입력 구독이 준비됐음을 알리고, 이후 `move`, `chaser-moved`, `place-bomb`, `player-contact-damaged`, `contact-escape-moved`, `bomb-exploded`, `player-damaged`, `player-explosion-damaged`, `enemy-died`, `room-cleared`, `swap-bomb`, `pause-resume`, `audio-unlocked` 사건을 문자열 또는 `{ name: string }` 형태로 제공해야 한다.

현재 TestSandbox 개발 빌드는 `PrototypeInputHarnessProbe`와 `BombSwapHarness.jslib`로 이 배열을 제공한다. `probe-ready`는 `PrototypeGameSession`이 InputReader 구독까지 끝낸 뒤에만 기록한다. Playwright는 canvas focus와 이 준비 표식을 확인하고, `move`까지 `W` 유지 → `Z` 설치 → 접촉 피해까지 대기 → `contact-escape-moved`까지 `A` 유지 → 자기 폭발 확인 → 두 번째 `Z` 재유도 → `X`, `Esc` 두 번 순서로 진행한다. 고정 지연 대신 Core 사건으로 동기화해 느린 headless WebGL 프레임의 입력 오탐을 피한다. `player-contact-damaged`와 `player-explosion-damaged`는 적용된 피해 원인이 각각 적 `ActorId`와 `BombId`로 확정됐을 때만 기록된다. 나머지 이동·설치·폭발·사망·클리어 표식도 실제 Core 상태 전이에서만 발생한다. `audio-unlocked`는 사용자 입력 수신 marker일 뿐 실제 오디오 출력 검증을 대체하지 않는다.

Playwright를 찾지 못하면 Node의 `CODEX_NODE_MODULES`가 Playwright 모듈을 포함한 경로를 가리키게 하거나 프로젝트 개발 의존성 도입을 별도 승인받는다. 브라우저는 설치된 Edge/Chrome을 자동 탐색하며, 별도 위치는 `BOMBSWAP_BROWSER_PATH`로 지정한다.

## 공식 참고

- [Unity command-line build](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)
- [Unity Test Framework command-line arguments](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-command-line.html)
- [Unity BuildReport](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Reporting.BuildReport.html)
