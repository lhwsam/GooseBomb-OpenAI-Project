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

## Release 빌드

- strip/link 결과에서 필요한 타입과 에셋이 보존되는지 확인한다.
- 압축 형식과 서버의 `Content-Encoding` 헤더가 일치해야 한다.
- decompression fallback 사용 여부는 호스팅 환경과 함께 결정한다.
- data caching, 초기 로딩, 새 버전 캐시 무효화를 검증한다.

## 호스팅 요구

- HTTPS에서 제공한다.
- Unity 산출물 확장자별 올바른 MIME type과 압축 헤더를 제공한다.
- 범위 요청, cache policy, 버전 경로를 배포 서비스에서 확인한다.
- threads를 켜는 결정은 COOP/COEP 헤더와 브라우저 호환 영향을 포함한 별도 ADR 없이는 하지 않는다.

## 로컬 관찰 플레이테스트

`Tools/ServeWebGL.mjs`는 검증 빌드를 참가자와 같은 PC의 `127.0.0.1`에서 여는 로컬 전용 서버다. `Tools/WebGLSmoke.mjs`와 `Tools/WebGLStaticServer.mjs`를 공유해 Unity 파일의 MIME·gzip/Brotli 헤더와 경로 이탈 차단을 동일하게 적용한다. 기본 no-store 정책은 세션 중 이전 빌드 캐시 혼동을 줄인다.

이 서버는 HTTP loopback, 개발용 no-store와 최소 파일 제공만 보장한다. HTTPS, CDN cache, 범위 요청, 원격 장치 접근이나 배포 보안을 검증하지 않으므로 release 호스팅 요구를 충족했다는 근거로 사용하지 않는다. 구체적인 실행·증거 기록은 [로컬 WebGL 관찰 세션 실행](../Playtesting/ManualWebGLRun.md)을 따른다.

## 브라우저 smoke 진입점

최소 루프는 로드→canvas focus→이동→폭탄 설치→교체→폭발→피해/적 처치→pause/resume이다. 자동화 가능한 이벤트는 `HARNESS|event|json` 같은 안정된 개발 로그 형식을 검토하되 게임 규칙의 권위 API로 사용하지 않는다.

기본 `Tools/WebGLSmoke.mjs`는 다섯 전투방 카탈로그에서 seed 0 주 경로에 배정된 기둥·루프·게이트 방의 입력·전투·전환 회귀와 게이트 방 시각 캡처를 확인한다. `Tools/ArmoredWebGLSmoke.mjs`는 갑옷 실험 씬을 첫 enabled 씬으로 만든 별도 development 빌드에서 실제 폭발 2회의 `Armored → Broken → Dead` 상태 순서와 browser Console을 확인한다. `Tools/DirectionalLineWebGLSmoke.mjs`는 기본 던전 빌드에서 오른쪽 보상 후보를 골라 동쪽으로 설치한 직선 폭탄이 이후 북쪽 이동 명령에도 동쪽으로 폭발하는지 marker 순서와 캡처로 확인한다. `Tools/GamepadWebGLSmoke.mjs`는 표준 가상 Gamepad 연결을 브라우저 API에 주입해 스틱·D-pad·South/West/Start가 Unity Input System과 의미 명령까지 도달하는지 확인하며 표준 Web tier에 포함된다. 이 스모크들은 논리 사건과 자동 입력 경로의 정확성을 증명하지만 물리 장치별 조작감이나 사람에게 상태 변화가 충분히 읽히거나 재미있다는 판정은 대신하지 않는다.

## 공식 참고

- [Unity command-line build](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)
- [Unity Web technical limitations](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-technical-overview.html)
- [Unity Web input](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-input.html)
