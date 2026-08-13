# ADR-0002: 순수 C# Core와 Unity 어댑터 분리

- 상태: `Accepted`
- 날짜: 2026-08-14

## 맥락

폭탄, 쿨타임, 피해, 던전 생성 규칙은 빠른 반복 테스트가 필요하다. Unity 생명주기와 입력 장치에 직접 결합하면 AI가 변경할 때 영향 범위가 넓어지고 WebGL 검증 전까지 오류가 늦게 발견된다.

## 결정

`BombSwap.Core`는 UnityEngine 참조 없이 규칙과 값 객체를 소유한다. `BombSwap.Unity`는 Input System, MonoBehaviour, 씬, 프리팹, 표현을 Core 명령과 이벤트에 연결한다.

## 대안

- 모든 코드를 MonoBehaviour에 구현: 초기 파일 수는 적지만 테스트와 상태 소유권이 불명확하다.
- 완전한 Clean Architecture/DI 프레임워크: 프로토타입 규모에 과하다.

## 결과

- EditMode 규칙 테스트가 빨라진다.
- 변환 코드와 bootstrap이 추가된다.
- Core와 Unity에 같은 규칙을 중복 구현하지 않아야 한다.
