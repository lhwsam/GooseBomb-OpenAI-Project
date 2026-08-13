# Bomb Swap 프로젝트 작업 지침

이 파일은 저장소 전체에 적용되는 지속 규칙이다. 세부 설계와 현재 진행 상태를 이 파일에 중복해서 적지 말고 `Docs/`의 권위 문서를 따른다.

## 세션 시작 순서

작업을 시작하기 전에 다음 순서로 문맥을 확인한다.

1. 이 `AGENTS.md`
2. `Docs/INDEX.md`
3. `Docs/GameDesign/GDD_v0.2.md`와 `Docs/GameDesign/ProtoType_v0.2.md`
4. `Docs/Architecture/Overview.md`와 관련 ADR
5. `Docs/Development/CurrentState.md`
6. 변경 대상과 연결된 시스템 문서 및 테스트
7. `git status`, Unity 버전, Console 상태

전체 문서를 매번 모두 읽지 않는다. `Docs/INDEX.md`의 작업별 읽기 경로를 사용한다.

## 문서와 코드의 권위

- 사용자의 현재 요청이 항상 가장 우선한다.
- 게임의 의도와 검증 가설은 GameDesign 문서가 권위 원본이다.
- 채택한 기술 결정과 변경 이유는 `Docs/ADR/`이 권위 원본이다.
- 시스템 계약과 책임은 `Docs/Systems/`가 설명한다.
- 실제 튜닝 수치는 코드 상수보다 검증된 ScriptableObject 데이터가 권위 원본이다.
- 테스트는 실행 가능한 계약이다. 문서·코드·테스트가 충돌하면 임의로 한쪽을 맞다고 가정하지 말고 소유 문서와 함께 수정하거나 충돌을 보고한다.
- `Docs/Development/CurrentState.md`는 현재 스냅샷만 유지한다. 누적 작업 일지로 사용하지 않는다.

## 변하지 않는 프로젝트 전제

- 이 게임은 Unity 기반 3D WebGL 탑다운 룸 액션 로그라이트다.
- 게임 규칙의 공간 기준은 정수 XZ 논리 격자다. Y축은 높이와 표현에 사용한다.
- 셀 점유, 폭탄, 폭발, 벽 차단, 연쇄 판정은 Transform이나 3D 물리 순서가 아니라 논리 상태가 권위 원본이다.
- 3D Transform, Collider, Animator, VFX는 논리 상태를 표현하거나 입력을 전달하는 어댑터다.
- 입력 장치 상태를 Core에서 직접 읽지 않는다. Input System 입력은 의미 있는 `PlayerCommand`로 변환한 뒤 규칙 계층에 전달한다.
- 시간 의존 규칙은 주입 가능한 게임 시계로, 절차 생성은 명시적 seed로 재현 가능해야 한다.
- 프로토타입의 재미는 자동 테스트만으로 통과 판정하지 않는다. 자동 검증, 계측, 관찰 플레이테스트를 함께 사용한다.

## 코드와 어셈블리 경계

- `Assets/Game/Core` (`BombSwap.Core`): UnityEngine을 참조하지 않는 결정론적 규칙과 값 객체.
- `Assets/Game/Runtime`, `Presentation`, `Authoring`, `Content` (`BombSwap.Unity`): Unity 생명주기, 입력 어댑터, 씬 연결, 표현, 저작 데이터.
- `Assets/Game/Editor` (`BombSwap.Editor`): 검증기, 빌드 자동화, 마이그레이션 등 Editor 전용 코드.
- `Assets/Game/Tests/EditMode` (`BombSwap.Core.Tests`): 빠른 규칙 테스트.
- `Assets/Game/Tests/PlayMode` (`BombSwap.Unity.Tests`): Unity 연결과 생명주기 테스트.
- 의존 방향은 `Core <- Unity <- Editor/PlayMode Tests`다. Core는 Unity, Input System, Feel, 에디터 코드에 의존하면 안 된다.
- 순환 참조, 전역 mutable singleton, Service Locator, 범용 Event Bus를 추가하지 않는다.
- 하나의 구현만 있는 추상화나 프로토타입에 필요하지 않은 프레임워크를 미리 만들지 않는다.

## Unity 에셋 안전 규칙

- 씬, 프리팹, ScriptableObject, Input Actions, ProjectSettings의 YAML을 텍스트 치환으로 수정하지 않는다.
- 직렬화 에셋 변경은 가능하면 Unity Editor 또는 검증된 Editor 도구로 수행하고 저장 후 다시 읽어 확인한다.
- 직렬화 필드 이름을 바꿀 때는 데이터 호환성을 검토하고 필요한 경우 `FormerlySerializedAs`와 마이그레이션을 사용한다.
- `Assets/Feel`, `Assets/Plugins`와 기타 서드파티 파일은 명시적 요청 없이 수정하지 않는다. 프로젝트 코드는 `Assets/Game`의 어댑터를 통해 연동한다.
- `Packages/manifest.json`, 렌더 파이프라인 설정, Build Settings 변경은 영향과 롤백 방법을 먼저 기록한다.
- 같은 프로젝트에서 둘 이상의 Unity 인스턴스가 동시에 import, test, build하지 않게 한다.

## WebGL 규칙

- WebGL/IL2CPP에서 지원 여부가 확인되지 않은 API를 핵심 경로에 추가하지 않는다.
- `Thread`, `Task.Run`, `System.Net.Sockets`, `Reflection.Emit`, 동적 코드 생성을 런타임 설계에 사용하지 않는다.
- 비동기는 Unity PlayerLoop/Coroutine/지원되는 UniTask 경로로 한정하고 브라우저 메인 스레드 예산을 보호한다.
- 네트워크가 필요하면 `UnityWebRequest` 기반으로 설계한다.
- 반복 생성되는 폭탄·폭발·피격 VFX는 풀링 대상으로 본다. 프레임 반복 경로의 LINQ, boxing, 임시 컬렉션, material 인스턴스 생성을 피한다.
- WebGL 품질 기준은 Mobile URP 프로필이다. 실시간 광원·그림자·후처리는 성능 예산 문서에 근거해 추가한다.
- 기능 완료 전 실제 WebGL 빌드와 브라우저 입력 포커스, 키 입력, 오디오 시작, 로딩 실패를 확인한다.

## 구현 절차

1. 요청을 플레이어 또는 개발자 관점의 계약으로 바꾼다.
2. 관련 GDD, 시스템 문서, ADR, 현재 상태, 기존 테스트를 확인한다.
3. 가장 작은 일관된 변경 범위와 검증 방법을 정한다.
4. 규칙은 가능하면 EditMode 테스트와 함께 Core에 먼저 구현한다.
5. Unity 어댑터와 표현을 연결하고 PlayMode 또는 샌드박스 씬에서 검증한다.
6. 변경 diff를 자체 리뷰하고 필요한 경우 독립 리뷰를 수행한다.
7. 관련 문서와 `CurrentState.md`를 갱신한 뒤 검증 근거와 미실행 항목을 인계한다.

## 검증 기준

- 문서 변경: 내부 링크, 권위 원본 중복, 상태 표기를 확인한다.
- Core 변경: 정적 경계 검사, Unity 컴파일, 관련 EditMode 테스트가 최소 기준이다.
- Unity 연결 변경: 위 기준에 PlayMode 테스트와 Console 오류 확인을 추가한다.
- 씬·프리팹·콘텐츠 변경: 참조 누락 검증, 실제 재생 확인, 필요한 시각 증거를 추가한다.
- 입력·렌더링·패키지·빌드 변경: WebGL 빌드와 브라우저 스모크 테스트까지 수행한다.
- 실행하지 못한 검증은 통과로 표현하지 않고 이유와 남은 위험을 기록한다.
- 구체적인 테스트 매트릭스는 `Docs/Testing/`을 따른다.

## 멀티 에이전트와 코드 리뷰

- 기본은 단일 작성자다. 구현 담당만 파일을 수정하고 최종 통합한다.
- 멀티 에이전트는 독립적으로 나눌 수 있는 읽기, 조사, 규칙 리뷰, WebGL 리뷰, 테스트 누락 리뷰에 선택적으로 사용한다.
- 같은 씬, 프리팹, ScriptableObject, ProjectSettings 또는 같은 코드 파일을 병렬 수정하지 않는다.
- 리뷰 에이전트는 수정 요청이 명시되지 않은 한 읽기 전용이다.
- 마일스톤, Core 규칙, 입력, WebGL, 패키지/Unity 버전 마이그레이션은 독립 리뷰 대상이다.
- 리뷰 결과는 `심각도 / 파일·위치 / 문제 / 재현 또는 근거 / 영향 / 위반 계약 / 권장 수정 / 회귀 테스트` 형식으로 남긴다.
- 테스트와 명시된 계약이 리뷰어의 취향보다 우선한다. 구현 담당이 중복·오탐을 분류하고 최종 검증을 다시 실행한다.

## Code Review Rules

- Core에서 Transform, Physics, Input System, UnityEngine 시간/랜덤을 권위 상태로 사용하는 변경은 차단한다. 안전한 경로는 논리 격자, 주입 시계, 명시 seed, Unity 어댑터다.
- 폭발이 파괴 불가 벽을 통과하거나 파괴 가능 벽 너머까지 계속되는 변경은 차단한다. 파괴 불가 벽은 효과 없이 전파를 끝내고, 파괴 가능 벽은 해당 셀에 효과와 파괴를 적용한 뒤 끝낸다.
- 연쇄 폭발이 폭탄 종류에 따라 제외되거나 즉시 재귀 실행되는 변경은 차단한다. 모든 폭탄 종류가 짧은 고정 지연을 거쳐 동일한 스케줄러에서 처리되어야 한다.
- 폭탄 설치 직후 통과 규칙을 전역 충돌 무시로 구현하는 변경은 차단한다. 설치자와 해당 폭탄 사이의 제한된 상태로 소유하고 셀 이탈 시 종료한다.
- 브라우저 메인 스레드를 막는 동기 대기, 스레드 전제, 반복 할당을 WebGL 런타임 경로에 추가하는 변경은 차단한다.
- 서드파티 패키지를 직접 수정하는 변경은 차단한다. `Assets/Game`의 어댑터나 설정 확장점을 사용한다.

## 완료와 인계

- 완료 조건은 `Docs/Development/DefinitionOfDone.md`를 따른다.
- 새 결정은 ADR, 동작 계약 변경은 Systems, 빌드/검증 변경은 Testing 또는 WebGL 문서에 반영한다.
- 작업 종료 시 변경 내용, 결정 이유, 실행한 검증과 결과, 미실행 검증, 알려진 위험, 다음 작업 순서를 남긴다.
