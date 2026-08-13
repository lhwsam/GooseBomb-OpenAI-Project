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

## 브라우저 smoke 진입점

최소 루프는 로드→canvas focus→이동→폭탄 설치→교체→폭발→피해/적 처치→pause/resume이다. 자동화 가능한 이벤트는 `HARNESS|event|json` 같은 안정된 개발 로그 형식을 검토하되 게임 규칙의 권위 API로 사용하지 않는다.

## 공식 참고

- [Unity command-line build](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)
- [Unity Web technical limitations](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-technical-overview.html)
- [Unity Web input](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-input.html)
