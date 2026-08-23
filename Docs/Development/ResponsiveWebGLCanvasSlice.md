# 반응형 WebGL 캔버스 수직 슬라이스

- 상태: 자동·브라우저 검증 `Complete`, 배포 환경 검증 `Proposed`
- 결정 근거: 3D WebGL 프로토타입의 좁은 브라우저 창·인앱 브라우저 가독성
- 소유 계약: [WebGL 빌드와 호스팅](../WebGL/BuildAndHosting.md), [브라우저 테스트 매트릭스](../WebGL/BrowserTestMatrix.md), [검증 하네스](../Testing/VerificationHarness.md)

## 문제와 사용자 계약

Unity 기본 WebGL 템플릿은 960×600 canvas를 고정 CSS 크기로 배치했다. 640~752px 폭의 브라우저에서는 canvas 좌우가 viewport 밖으로 나가 우측 HUD와 미니맵이 잘리고 문서 가로 overflow가 생겼다. 게임의 내부 렌더 해상도와 16:10 화면 구도는 유지하되 다음 계약을 적용한다.

- canvas는 사용 가능한 브라우저 영역 안에 항상 완전히 들어온다.
- 960×600 네이티브 크기보다 확대하지 않고, 좁거나 낮은 창에서만 같은 비율로 축소한다.
- 페이지에는 가로·세로 scrollbar가 생기지 않는다.
- canvas click/pointer 입력은 focus를 회복한다.
- 로딩 진행률, 오류 banner, 키보드 핵심 조작 안내와 fullscreen 진입점을 제공한다.
- 레이아웃 변경은 Unity 내부 격자·카메라·HUD 좌표나 입력 규칙을 바꾸지 않는다.

## 구현과 책임

```text
Assets/WebGLTemplates/BombSwap/index.html
        │ 960×600 reference + viewport fit + focus/footer/loading
        ▼
ResponsiveWebGLTemplateScope
        │ build 동안 PROJECT:BombSwap 선택
        │ finally/Dispose에서 기존 PlayerSettings 값 저장 복원
        ▼
CommandLineVerification / ConnectedWebGLBuildHarness
        │ 동일 template 경로로 Development WebGL 생성
        ▼
WebGLTemplateTests + WebGLSmoke
        │ 정적 macro/복원 계약 + 실제 viewport/전체 게임 회귀
        ▼
사람 관찰 플레이
```

- `index.html`이 hosting shell과 canvas CSS 크기를 소유한다. canvas의 `width`·`height` 속성은 Unity 렌더 기준 960×600을 유지한다.
- Unity의 first-party uGUI도 공통 `PrototypeUiFactory`를 통해 960×600을 reference resolution으로 사용한다. 따라서 네이티브 canvas에서는 `CanvasScaler` scale이 1이고, CSS 축소와 Unity UI 좌표계가 서로 다른 기준값으로 표류하지 않는다.
- CSS 표시 크기는 `min(1, availableWidth / 960, availableHeight / 600)`으로 계산한다. `ResizeObserver`와 window resize가 같은 함수를 사용한다.
- 빌드 하네스는 프로젝트의 평상시 템플릿 설정을 영구 변경하지 않는다. build scope가 이전 값을 보존하고 종료 시 `AssetDatabase.SaveAssets()`까지 수행해 디스크 설정도 복원한다.
- `Tools/WebGLTemplateTests.mjs`는 필수 Unity macro, 반응형 식, 고정 크기 회귀 금지와 설정 저장 복원을 검사한다.
- `Tools/WebGLSmoke.mjs`는 Unity 로드 직후와 전체 던전 회귀 뒤에 1280×720, 1024×768, 640×720 viewport에서 canvas 경계·문서 overflow·네이티브 상한·16:10 비율을 검사한다.

## 검증 결과

- 변경 전 검증된 보스 이동 WebGL 빌드를 새 레이아웃 검사로 실행하면 640×720에서 960×600 canvas가 x=-160부터 배치되어 실패했다. 증거는 `Artifacts/Verification/20260816-043000-responsive-layout-baseline/browser-smoke.json`이다.
- HTML/template scope 정적 계약과 공통 정적 서버 회귀가 통과했다.
- 연결된 Unity 6000.5.3f1에서 전체 EditMode `303/303`, PlayMode `126/126`, Unity Console 오류 `0`을 확인했다.
- 10-scene Development WebGL 최초 빌드는 `137,972,718 bytes`, `32.211초`, 오류 `0`, 기존 패키지·셰이더 범주의 경고 `348`건으로 성공했다. 증거는 `Artifacts/Verification/20260816-044000-responsive-web-connected/`이다.
- Edge keyboard smoke `35/35`는 1280×720에서 960×600, 640×720에서 640×400 canvas와 overflow `0`을 확인하고 전체 던전·보스·완료·실패·재시작 회귀 및 Console/page error `0`을 통과했다.
- 같은 빌드의 가상 Gamepad smoke `14/14`와 Console/page error `0`이 통과했다.
- 인앱 브라우저 974×986 viewport에서 960×600 canvas가 중앙에 완전히 들어오고 좌우 HUD·미니맵·무기 HUD·하단 안내·fullscreen 버튼이 모두 보이는 것을 캡처로 확인했다.
- commit `7ba40e6`의 증분 post-commit WebGL 빌드는 같은 `137,972,718 bytes`, `10.581초`, warning/error `0`으로 성공했다. 빌드 전후 `ProjectSettings.asset` SHA-256 `90662C7E3115FC7E1324C38291805288D1BDD94CE9888B56A1F39CEC10D6C1A8`과 `APPLICATION:Default`가 같아 scope의 디스크 복원을 확인했다. 같은 산출물에서 keyboard `35/35`, Gamepad `14/14`, Console/page error `0`이 다시 통과했다. 최종 플레이테스트 증거는 `Artifacts/Verification/20260816-044700-responsive-postcommit-web/`이다.

## 비목표와 남은 위험

- 모바일 터치 입력, 세로 화면 전용 HUD 재배치, 사용자 선택형 UI scale·동적 내부 render resolution, 고해상도 확대, 실제 배포 CDN/HTTPS/cache 설정은 이번 범위가 아니다.
- 640px보다 더 작은 극단적 viewport에서는 전체 구도가 축소되므로 텍스트 자체의 최소 가독성은 별도 수동 검증이 필요하다.
- Safari·Firefox와 실제 fullscreen 전환은 첫 배포 지원 범위를 정할 때 브라우저 매트릭스로 확인한다.
