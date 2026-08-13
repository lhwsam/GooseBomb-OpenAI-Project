# 테스트 전략

- 상태: `Accepted`

## 목표

빠른 순수 규칙 검증에서 실제 브라우저까지 단계적으로 신뢰를 쌓는다. 낮은 단계 통과가 높은 단계 검증을 대체하지 않는다.

## 검증 계층

1. **Static**: asmdef/namespace/금지 API/사용처/문서 링크/직렬화 호환 검토.
2. **Compile**: Unity import와 first-party 어셈블리 컴파일, 새 Console error 확인.
3. **EditMode**: 격자, 폭탄, 쿨타임, 피해, 상태 머신, seed 그래프.
4. **PlayMode**: MonoBehaviour 생명주기, 입력 어댑터, 씬/프리팹, VFX 요청, UI.
5. **Content**: room/definition ID, 셀 연결성, 참조 누락, Build Settings.
6. **WebGL Build**: IL2CPP/API/strip/link/build 포함 문제.
7. **Browser Smoke**: 로딩, focus, 입력, 오디오, Console, 기본 루프.
8. **Visual/Playtest**: 가독성, 감각, 재미 가설, 계측/인터뷰.

## 실행 단계

- `Fast`: Static + Compile + Core EditMode. 작은 Core 반복에 사용.
- `Full`: Fast + first-party PlayMode + Editor validation. 기능 완료와 통합 전 사용.
- `Web`: Full + development WebGL build + browser smoke. 입력/렌더/패키지/마일스톤에 사용.

실행 규약과 종료 코드는 [VerificationHarness.md](VerificationHarness.md)를 따른다. `Tools/Verify.ps1`이 XML, 로그, JSON 산출물을 생성하는 권위 실행 경로다.

## Core 테스트 원칙

- Arrange에서 명시적 초기 상태, 시계, seed를 사용한다.
- 한 테스트는 관찰 가능한 계약 하나를 설명한다.
- 정상뿐 아니라 경계, 중복 사건, 같은 step 순서 충돌을 다룬다.
- private field를 반사로 검사하는 대신 공개 결과, 상태 snapshot, domain event를 확인한다.
- 여러 seed 속성을 검증하되 실패 seed를 출력해 재현 가능하게 한다.

## Unity 테스트 원칙

- 씬 전체보다 작은 fixture/prefab을 우선한다.
- 프레임 수와 임의 대기 시간에 덜 의존하고 명확한 완료 조건을 기다린다.
- 생성한 오브젝트와 정적 상태를 정리한다.
- Editor에서 통과한 입력/렌더 기능도 WebGL 브라우저에서 별도 확인한다.

## 기준선과 보고

- 테스트 전 기존 Console error와 실패를 기록한다.
- 새 실패와 기존 실패를 구분한다.
- 통과 수, 실패 수, Unity 버전, target, 로그 경로를 남긴다.
- 실행하지 못한 단계는 “통과”로 표현하지 않는다.
