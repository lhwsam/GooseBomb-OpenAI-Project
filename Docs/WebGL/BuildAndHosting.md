# WebGL 빌드와 호스팅

- 상태: 구조 `Accepted`, 배포 서비스 `Proposed`

## 빌드 원칙

- Unity 버전은 `ProjectSettings/ProjectVersion.txt`와 일치해야 한다.
- development와 release 빌드 프로필을 분리한다.
- 빌드 전 Console error, 테스트, Build Settings 씬을 확인한다.
- 빌드 결과의 `BuildReport`, 로그, 압축 방식, 전체 다운로드 크기를 보존한다.
- 로컬 Editor 실행만으로 WebGL 완료를 선언하지 않는다.

## Development 빌드

- 디버깅 기호와 예외 정보를 충분히 유지한다.
- 브라우저 Console에서 구조화된 smoke marker와 예외를 확인할 수 있게 한다.
- 성능 수치는 development 오버헤드를 표시하고 release 수치와 섞지 않는다.

## Bomb Swap WebGL 템플릿

- 프로젝트 템플릿은 `Assets/WebGLTemplates/BombSwap/index.html`이다.
- command-line과 연결된 Editor 빌드 하네스는 빌드 범위에서만 `PROJECT:BombSwap`을 활성화한다. 완료·실패 여부와 관계없이 기존 `PlayerSettings.WebGL.template`을 복원하고 `AssetDatabase.SaveAssets()`로 디스크에도 반영한다.
- Unity canvas의 내부 기준과 first-party uGUI `CanvasScaler` reference resolution은 모두 960×600이다. uGUI 설정은 `PrototypeUiFactory`가 소유하고, hosting shell은 이보다 확대하지 않고 사용 가능한 viewport가 작을 때만 16:10 비율로 축소한다.
- gameplay world는 3DPixelCamera의 480×300 point-filter target을 960×600에서 정확히 2배로 표시하는 것이 기준이다. WebGL의 `Mobile_RPAsset`도 render scale `1.0`을 유지해 이 target을 80%로 다시 축소·업스케일하지 않는다. UI만 선명하고 3D가 더 깨지는 회귀를 막기 위해 `Tools/WebGLTemplateTests.mjs`가 WebGL→Mobile 품질 매핑과 render scale을 함께 검사한다. 브라우저 zoom·OS DPI가 100%가 아니면 backing과 point 확대 배율이 다시 달라질 수 있으므로 화질 비교와 제출 캡처는 먼저 zoom 100%(`Ctrl+0`)와 960×600 canvas를 확인한다.
- URP 17.5는 WebGL에서 FSR pass가 선택되지 않아도 `Edge Adaptive Spatial Upsampling` 셰이더 capability 경고를 초기화 시 한 번 남길 수 있다. render scale `1.0`에서는 이 pass가 실제 업스케일에 사용되지 않으므로, 이 알려진 경고와 실제 화면·page error·다른 Console error를 구분한다.
- canvas와 문서는 viewport를 넘지 않아야 하며 scrollbar를 만들지 않는다. pointer 입력은 canvas focus를 회복한다.
- 하단 footer는 핵심 키보드 조작과 fullscreen 진입점을 제공한다. 좁은 창에서는 focus 안내 일부를 숨길 수 있지만 게임 canvas를 가리면 안 된다.
- 구현·검증 근거는 [반응형 WebGL 캔버스 수직 슬라이스](../Development/ResponsiveWebGLCanvasSlice.md)를 따른다.

## Release 빌드

- 연결된 Editor의 정식 빌드는 `WebGLReleaseBuildPolicy`가 Lobby·Dungeon scene과 런타임 `PrototypeDungeonCombatRoomCatalog`가 참조하는 전투 scene을 함께 전달한다. 현재 전투 scene 4개는 과거 경로인 `Assets/Game/Scenes/TestSandbox/` 아래에 있으므로 폴더 이름만으로 제외하지 않는다. 카탈로그 밖의 `TestSandboxArmor`와 독립 playtest scene은 Release에서 제외하고 Development 검증 경로에만 남긴다.
- 기본 `Build Release WebGL Connected`는 콘텐츠 validator를 통과해야 한다. 긴급 산출물에서 기존 validator 기준선을 의도적으로 감수할 때만 이름에 `Validation Bypass`가 표시된 별도 메뉴를 사용하고, 빌드 보고서의 `contentValidationSkipped`와 남은 validator 오류를 인계한다. 우회 빌드는 전체 Web tier 통과 근거가 아니다.
- GitHub Pages 배포는 `Build GitHub Pages WebGL Connected` 프로필을 사용한다. 이 프로필은 빌드하는 동안에만 `PlayerSettings.WebGL.decompressionFallback`을 켜고 완료·실패와 관계없이 기존 프로젝트 값을 복구한다. GitHub Pages에서 `Content-Encoding` 응답 헤더를 직접 제어하지 못해도 `.unityweb` 산출물을 브라우저가 해제할 수 있게 하기 위한 호스팅 전용 차이다.
- 현재 콘텐츠 validator 기준선을 감수한 긴급 Pages 배포는 이름에 `Validation Bypass`가 표시된 GitHub Pages 메뉴를 사용하고, `webgl-build-report.json`의 `hostingProfile: GitHubPages`, `decompressionFallback: true`, `contentValidationSkipped: true`를 함께 보존한다.
- `WebGLReleaseAssetOptimizer`는 원본/PC import를 유지하면서 WebGL override만 적용한다. 현재 기준은 HUD 폭탄 아이콘 256, 캐릭터 diffuse/normal 1024, 실제 빌드에 포함되는 환경·폭탄·VFX diffuse/normal 512다. 캐릭터 기본 리그 FBX의 미사용 T-Pose clip import는 끄되 별도 동작 clip과 Animator Controller는 유지한다.
- 실제 사용 중인 한글 TMP font 2개는 각각 전체 한글 글리프를 담은 4096×4096 정적 atlas다. 글리프 누락 없이 줄이려면 Font Asset Creator에서 현재 UI 문자열·필수 fallback 범위를 확정해 새 atlas를 만든 뒤 모든 한국어 화면을 검증해야 하므로 단순 platform override 대상에 넣지 않는다.
- 적응형 BGM stem은 Vorbis quality 1.0과 현재 load type을 유지한다. 품질·sample rate·streaming 변경은 다운로드 크기보다 seamless loop와 stem DSP 동기 검증을 먼저 소유해야 한다.
- strip/link 결과에서 필요한 타입과 에셋이 보존되는지 확인한다.
- 압축 형식과 서버의 `Content-Encoding` 헤더가 일치해야 한다.
- decompression fallback 사용 여부는 호스팅 환경과 함께 결정한다.
- data caching, 초기 로딩, 새 버전 캐시 무효화를 검증한다.

## 호스팅 요구

- HTTPS에서 제공한다.
- Unity 산출물 확장자별 올바른 MIME type과 압축 헤더를 제공한다.
- 범위 요청, cache policy, 버전 경로를 배포 서비스에서 확인한다.
- threads를 켜는 결정은 COOP/COEP 헤더와 브라우저 호환 영향을 포함한 별도 ADR 없이는 하지 않는다.
- GitHub Pages에는 생성물을 `gh-pages` 브랜치 루트와 `.nojekyll`로 배포한다. `main`의 `Artifacts/`나 Unity 프로젝트에 빌드 산출물을 강제로 추가하지 않으며, Pages source는 `gh-pages` 브랜치의 `/ (root)`를 사용한다.

## 로컬 관찰 플레이테스트

`Tools/ServeWebGL.mjs`는 검증 빌드를 참가자와 같은 PC의 `127.0.0.1`에서 여는 로컬 전용 서버다. `Tools/WebGLSmoke.mjs`와 `Tools/WebGLStaticServer.mjs`를 공유해 Unity 파일의 MIME·gzip/Brotli 헤더와 경로 이탈 차단을 동일하게 적용한다. 기본 no-store 정책은 세션 중 이전 빌드 캐시 혼동을 줄인다.

이 서버는 HTTP loopback, 개발용 no-store와 최소 파일 제공만 보장한다. HTTPS, CDN cache, 범위 요청, 원격 장치 접근이나 배포 보안을 검증하지 않으므로 release 호스팅 요구를 충족했다는 근거로 사용하지 않는다. 구체적인 실행·증거 기록은 [로컬 WebGL 관찰 세션 실행](../Playtesting/ManualWebGLRun.md)을 따른다.

## 브라우저 smoke 진입점

최소 루프는 로드→canvas focus→이동→폭탄 설치→교체→폭발→피해/적 처치→pause/resume이다. 자동화 가능한 이벤트는 `HARNESS|event|json` 같은 안정된 개발 로그 형식을 검토하되 게임 규칙의 권위 API로 사용하지 않는다.

기본 `Tools/WebGLSmoke.mjs`는 다섯 전투방 카탈로그에서 seed 0 주 경로에 배정된 기둥·루프·게이트 방의 입력·전투·전환 회귀, Secret·Recovery 인접 `E` 상호작용과 게이트 방 시각 캡처를 확인한다. `Tools/ArmoredWebGLSmoke.mjs`는 갑옷 실험 씬을 첫 씬으로 만든 별도 development 빌드에서 실제 폭발 2회의 `Armored/Guard → Broken/PanicTelegraph → PanicRun → PanicRecover → Chase → Dead` 순서, 고정 방향·거리 marker, 서로 다른 두 설치 위치와 browser Console을 확인한다. 정식 11씬 Build Settings는 유지하고 전용 시작이 필요할 때만 던전 전역 adapter를 제거한 일회성 scene 사본을 사용한 뒤 삭제한다. `Tools/DirectionalLineWebGLSmoke.mjs`는 기본 던전 빌드에서 오른쪽 보상 후보를 골라 동쪽으로 설치한 직선 폭탄이 이후 북쪽 이동 명령에도 동쪽으로 폭발하는지 marker 순서와 캡처로 확인한다. `Tools/GamepadWebGLSmoke.mjs`는 표준 가상 Gamepad 연결을 브라우저 API에 주입해 스틱·D-pad·South/West/North/Start/Select가 Unity Input System과 의미 명령, 이동 중 분리 정지와 동일 장치 재연결 복구, pause 차단·유지 스틱 재개, 실패 뒤 재시작까지 도달하는지 확인하며 표준 Web tier에 포함된다. 이 스모크들은 논리 사건과 자동 입력 경로의 정확성을 증명하지만 물리 장치별 조작감이나 사람에게 상태 변화가 충분히 읽히거나 재미있다는 판정은 대신하지 않는다.

## 공식 참고

- [Unity command-line build](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)
- [Unity Web custom templates](https://docs.unity3d.com/6000.0/Documentation/Manual/web-templates-add.html)
- [Unity Web template build configuration](https://docs.unity3d.com/6000.0/Documentation/Manual/web-templates-build-configuration.html)
- [Unity Web technical limitations](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-technical-overview.html)
- [Unity Web input](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-input.html)
