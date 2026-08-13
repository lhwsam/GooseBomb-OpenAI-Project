# 프로젝트 폴더와 asmdef 구조

- 상태: `Accepted`

## Unity first-party 구조

```text
Assets/Game/
  BombSwap.Unity.asmdef
  Core/
    BombSwap.Core.asmdef
    Grid/
    Bombs/
    Input/
    Combat/
    Enemies/
    Dungeon/
    Common/
  Runtime/
    Bootstrap/
    Input/
    Simulation/
    World/
    Diagnostics/
    WebGL/
  Presentation/
    Camera/
    Characters/
    Bombs/
    UI/
    Audio/
    VFX/
  Authoring/
    Prototype/
    Definitions/
    Rooms/
    Validation/
  Content/
    Input/
    Materials/
    Bombs/
    Enemies/
    Rooms/
  Scenes/
    Prototype/
    TestSandbox/
  Editor/
    BombSwap.Editor.asmdef
    BuildAutomation/
    ContentValidation/
    Migration/
  Tests/
    EditMode/
      BombSwap.Core.Tests.asmdef
    PlayMode/
      BombSwap.Unity.Tests.asmdef
```

기능 구현 시 실제 코드가 생기는 세부 폴더만 추가한다. 현재 `Core/Input`, `Runtime/Input`, 개발 WebGL probe, TestSandbox authoring과 프로토타입 재료·입력 에셋이 존재한다.

## asmdef 선택 이유

- `BombSwap.Core`는 `noEngineReferences`로 UnityEngine 의존을 컴파일 단계에서 차단한다.
- `BombSwap.Unity`는 `Assets/Game` 루트에 두어 Runtime, Presentation, Authoring, Content의 Unity 코드를 하나의 프로토타입 어셈블리로 유지한다.
- Core, Editor, Tests는 더 깊은 asmdef가 루트 어셈블리 범위에서 분리한다.
- Editor 전용 코드는 `includePlatforms: Editor`로 플레이어 빌드에서 제외한다.
- EditMode와 PlayMode 테스트를 분리해 빠른 Core 검증과 Unity 통합 검증을 독립 실행한다.

## 파일 배치 판단

| 질문 | 배치 |
|---|---|
| Unity 없이 입력과 출력이 설명 가능한 규칙인가? | Core |
| MonoBehaviour, Input System, Transform, Physics가 필요한가? | Runtime |
| 게임 상태를 보이거나 들리게만 하는가? | Presentation |
| 디자이너가 에셋으로 정의하는 데이터/바인딩인가? | Authoring 또는 Content |
| UnityEditor API 또는 에셋 검증/마이그레이션인가? | Editor |
| 규칙의 실행 가능한 계약인가? | EditMode Tests |
| 씬/프리팹/생명주기 연결을 검증하는가? | PlayMode Tests |

## 서드파티 경계

`Assets/Feel`, `Assets/Plugins` 등 vendor 경로는 first-party 구조에 포함하지 않는다. 패키지 연동 코드는 필요 시 `Presentation` 또는 `Runtime` 아래의 좁은 Adapter 폴더를 만들고 해당 패키지 참조를 그 경계에 한정한다. 참조가 커지면 그때 별도 asmdef 분리를 검토한다.
