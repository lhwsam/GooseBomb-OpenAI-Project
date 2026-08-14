# 로컬 WebGL 관찰 세션 실행

- 상태: 로컬 플레이테스트 절차 `Accepted`
- 실행 도구: `Tools/ServeWebGL.mjs`
- 현재 관찰 계약: [직선·광역 폭탄 선택 플레이테스트](DirectionalBombChoiceProtocol.md)
- 비교 기준선: [두 적·폭탄 선택 비교 플레이테스트](TwoEnemyBombChoiceProtocol.md)
- 기본 전투 기준선: [첫 기본 전투 관찰 플레이테스트](FirstCombatProtocol.md)

## 목적

검증된 WebGL 빌드를 `file://`로 직접 열지 않고 Unity 산출물의 MIME·압축 헤더를 처리하는 로컬 HTTP 서버에서 실행한다. 이 도구는 참가자와 같은 PC에서 진행하는 관찰 세션용이며 배포 호스팅이나 자동 브라우저 검증을 대신하지 않는다.

## 사전 조건

- 세션에 사용할 commit과 그 commit에서 만든 WebGL 빌드를 고정한다.
- 빌드 폴더 바로 아래에 `index.html`과 `Build/` 또는 Unity가 생성한 빌드 파일이 있어야 한다.
- Node.js 18 이상을 사용할 수 있어야 한다. 프로젝트에 npm 의존성을 설치할 필요는 없다.
- 세션 전에 자동 검증 결과와 Unity Console 오류 여부를 확인한다.

## 서버 시작

저장소 루트의 PowerShell에서 실행한다. `--port 0`은 사용 가능한 loopback 포트를 자동 선택한다.

```powershell
node ./Tools/ServeWebGL.mjs `
  --buildPath './Artifacts/Verification/<web-run>/WebGLBuild' `
  --port 0
```

성공하면 다음 형식의 실제 URL을 출력한다.

```text
BOMBSWAP_WEBGL_SERVER|ready|url=http://127.0.0.1:<port>/|root=<absolute-build-path>
```

해당 URL을 관찰 대상 브라우저에서 열고 canvas를 한 번 클릭한 뒤 세션을 시작한다. 종료할 때 서버 터미널에서 `Ctrl+C`를 누른다.

고정 포트가 필요하면 `--port 8000`처럼 지정할 수 있다. 서버는 의도하지 않은 LAN 공개를 막기 위해 항상 `127.0.0.1`에만 바인딩한다. 다른 장치나 원격 참가자에게 제공할 때는 이 도구를 외부 공개하지 말고 [빌드와 호스팅](../WebGL/BuildAndHosting.md)의 HTTPS 배포 요구를 따른다.

## 세션 준비 기록

`Artifacts/Playtests/<session-id>/build-reference.txt`에 최소한 다음을 기록한다.

```text
session-id:
commit:
unity-version:
build-artifact-path:
webgl-build-report-path:
server-url:
os:
browser-and-version:
resolution:
started-at:
```

- 원본 영상·브라우저 로그·개인 식별 가능 정보는 `Artifacts/Playtests/<session-id>/`에만 둔다.
- 익명 관찰 요약은 [세션 기록 템플릿](SessionTemplate.md)을 복사해 `Docs/Playtesting/Results/`에 작성한다.
- 서로 다른 commit이나 빌드는 같은 조건의 결과로 합치지 않는다.

## 관찰 세션 순서

1. 브라우저 DevTools Console을 열어 시작 시 오류가 없는지 확인한다.
2. 고정 build에 대응하는 프로토콜의 시작 안내만 읽고 전략 힌트는 주지 않는다.
3. 고정 build에 대응하는 프로토콜의 대상 방과 시도 수를 따른다. 현재 전체 경로 빌드는 첫 run에서 보상 후보를 자유 선택하고 다음 run에서 반대 후보를 비교한다.
4. 보상 선택, 방별 설치 셀·바라보기·퇴로, 폭탄 교체 이유와 피해 원인 설명을 기록한다.
5. 세션이 끝나면 Console 오류를 `browser-console.txt`로 보존하고 서버를 종료한다.
6. 자동 probe 사건과 관찰 사실·발언·해석을 분리한다.

## 도구 자체 검증

정적 서버의 MIME, gzip/Brotli 헤더, HEAD 응답, 누락 파일과 경로 이탈 차단은 다음 명령으로 빠르게 검사한다.

```powershell
node ./Tools/WebGLStaticServerTests.mjs
```

성공 표식은 `BOMBSWAP_WEBGL_STATIC_SERVER_TEST|passed`다. 실제 게임 입력과 전투 상태는 이 테스트가 아니라 `Tools/WebGLSmoke.mjs`, `Tools/DirectionalLineWebGLSmoke.mjs`, `Tools/ArmoredWebGLSmoke.mjs` 또는 사람 세션에서 별도로 확인한다.

## 문제 해결

- `WebGL index.html was not found`: `--buildPath`가 `index.html`을 직접 포함한 빌드 루트를 가리키는지 확인한다.
- `EADDRINUSE`: `--port 0`을 사용하거나 다른 고정 포트를 선택한다.
- WASM MIME 오류: Python의 단순 서버나 `file://` 대신 이 도구로 다시 실행한다.
- 키 입력 없음: canvas를 클릭하고 브라우저 focus를 확인한다. focus 복귀 뒤에도 지속되면 Console과 재현 순서를 남긴다.
- 로딩·Console 오류: 빌드 파일을 수정하지 말고 고정 commit, BuildReport와 브라우저 로그를 함께 보존한다.
