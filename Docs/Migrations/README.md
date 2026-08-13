# 마이그레이션 운영

Unity 버전, 패키지, 직렬화 데이터, 공개 API, 저장 형식의 변경은 일반 기능 작업과 분리해 재현 가능하고 되돌릴 수 있게 진행한다.

## 원칙

- 한 번에 한 축만 변경한다. 예: Unity 버전과 패키지 대규모 업그레이드를 같은 단계에서 섞지 않는다.
- 변경 전 Git 상태, Unity/패키지 버전, 테스트, WebGL 빌드, 주요 화면, 성능/크기 기준선을 남긴다.
- 자동 에셋 재직렬화 전에 대상과 변경량을 예측하고 백업 가능한 Git 상태를 만든다.
- 씬/프리팹 YAML을 문자열 치환하지 않는다.
- 데이터 migrator는 가능하면 idempotent하고 dry-run/검증 결과를 제공한다.
- 호환 어댑터를 둘 경우 제거 조건과 기한을 문서화한다.
- 롤백은 “Git으로 되돌린다”보다 버전, 데이터, 캐시, generated 결과까지 구체적으로 적는다.

## 검토 관점

- API/패키지: breaking change, asmdef, define, 사용처.
- 직렬화/에셋: 필드, GUID, prefab override, import 결과.
- WebGL/성능: IL2CPP, stripping, build size, 브라우저 회귀.

최종 작성자 한 명이 변경을 통합하고 같은 Unity 프로젝트를 여러 인스턴스에서 동시에 import하지 않는다.
