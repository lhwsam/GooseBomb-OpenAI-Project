# 의존성과 asmdef 규칙

- 상태: `Accepted`

## 실제 어셈블리

| 어셈블리 | 위치 | 참조 가능 | 금지 |
|---|---|---|---|
| `BombSwap.Core` | `Assets/Game/Core` | .NET/BCL | UnityEngine, Input System, 서드파티, Editor |
| `BombSwap.Unity` | `Assets/Game`의 Core/Editor/Tests 제외 하위 | Core, Input System, 필요한 Unity 런타임 모듈 | Editor API, 테스트 API |
| `BombSwap.Editor` | `Assets/Game/Editor` | Core, Unity, UnityEditor | 플레이어 빌드 포함 |
| `BombSwap.Core.Tests` | `Assets/Game/Tests/EditMode` | Core, Test Framework | 프로덕션 코드에서의 역참조 |
| `BombSwap.Unity.Tests` | `Assets/Game/Tests/PlayMode` | Core, Unity, Test Framework | 프로덕션 코드에서의 역참조 |

`BombSwap.Unity.asmdef`는 `Assets/Game` 루트에 있다. 더 깊은 Core, Editor, Tests asmdef가 해당 하위 트리를 분리한다.

## 허용 의존 방향

```text
BombSwap.Core
    ^
    |
BombSwap.Unity <--- BombSwap.Editor
    ^                    ^
    |                    |
BombSwap.Unity.Tests   editor tooling only

BombSwap.Core.Tests ---> BombSwap.Core
```

## 네임스페이스

- Core: `BombSwap.Core.<System>`
- Unity Runtime: `BombSwap.Unity.<System>`
- Presentation: `BombSwap.Presentation.<System>`
- Authoring: `BombSwap.Authoring.<System>`
- Editor: `BombSwap.Editor.<Tool>`
- Tests: `BombSwap.Tests.EditMode` 또는 `BombSwap.Tests.PlayMode`

폴더 이름과 네임스페이스를 기계적으로 일치시키기보다 책임과 어셈블리 경계를 우선한다.

## 경계 규칙

- Core API는 Unity 타입 대신 자체 값 객체를 사용한다. 예: `GridPosition`, `GameDuration`, `BombId`.
- Runtime은 입력, Unity 시간, Transform, Collider를 Core 명령과 값으로 변환한다.
- Presentation은 Core 상태를 직접 변경하지 않고 Runtime이 제공한 이벤트/뷰 모델만 소비한다.
- Editor 검증기는 런타임 검증과 규칙을 중복하지 말고 가능한 경우 Core validator를 호출한다.
- 서드파티 기능은 `BombSwap.Unity` 또는 Presentation의 좁은 어댑터 뒤에 둔다.
- asmdef 참조를 추가할 때 필요성, WebGL 포함 여부, 빌드 크기 영향을 검토한다.

## 새 어셈블리를 추가하는 기준

다음 중 하나가 실제로 필요할 때만 추가한다.

- 독립적으로 컴파일·테스트할 명확한 책임 경계가 있다.
- Editor/플랫폼 전용 코드가 플레이어 빌드에서 반드시 제외되어야 한다.
- 선택적 패키지 의존성을 다른 코드에서 격리해야 한다.
- 컴파일 시간 또는 모듈 소유권 문제가 측정되었다.

단순 폴더 정리나 미래 가능성만으로 asmdef를 세분화하지 않는다.
