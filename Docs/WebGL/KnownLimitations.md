# WebGL 알려진 제약

- 상태: `Accepted` 플랫폼 전제

## 런타임

- 브라우저 메인 스레드 제약을 기본으로 설계한다.
- 일반적인 멀티스레드/소켓/동적 코드 생성 전제를 사용하지 않는다.
- IL2CPP stripping과 AOT 때문에 reflection 기반 생성/직렬화는 별도 검증이 필요하다.
- 파일 시스템과 프로세스 API를 런타임 저장/도구 경로로 사용하지 않는다.
- 원격 통신은 브라우저 보안 정책, CORS, HTTPS의 영향을 받는다.

## 입력과 오디오

- canvas focus가 없으면 키 입력을 받지 못할 수 있다.
- 브라우저가 방향키/단축키를 먼저 처리할 수 있다.
- focus 상실 중 key-up이 누락될 수 있어 복귀 시 입력 상태를 초기화해야 한다.
- 오디오는 사용자 상호작용 전 자동 재생이 제한될 수 있다.

## 메모리와 성능

- 브라우저 탭의 실제 메모리 한계는 환경마다 다르다.
- 큰 단일 프레임 작업, GC spike, shader variant 준비가 눈에 띄는 멈춤을 만든다.
- development build의 성능을 release 기준으로 오해하지 않는다.
- Asset Store/vendor 폴더 크기와 최종 build 포함 크기는 다르므로 BuildReport로 확인한다.

## 프로젝트 현재 설정 메모

현재 조사 기준 threads support는 꺼져 있고 data caching은 켜져 있다. 설정 변경 시 `CurrentState.md`와 관련 ADR을 갱신하고 실제 호스팅에서 재검증한다.
