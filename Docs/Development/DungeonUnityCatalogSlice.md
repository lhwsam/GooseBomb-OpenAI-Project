# 던전 Unity 런 카탈로그 작업 계약

- 상태: `Implemented`
- 소유: 저작 데이터 `BombSwap.Authoring`, 런 조합 `BombSwap.Unity`, 논리 상태 `BombSwap.Core`
- 선행 결정: [ADR-0007](../ADR/0007-Potential-Room-Exits.md)

## 목표

Core `DungeonCombatRoomLayout`의 room definition ID를 Unity의 실제 `PrototypeCombatRoomDefinitionAsset`과 Build Settings 씬 이름으로 안전하게 해석한다. 현재 방·방문·클리어 상태는 콘텐츠 에셋이 아니라 세션 인스턴스에 유지한다.

## 저작 계약

- `PrototypeDungeonCombatRoomCatalogAsset`은 전투방 asset과 씬 이름의 명시적 entry 목록을 소유한다.
- 현재 프로토타입 카탈로그는 중앙 루프→평행 통로→엇갈린 기둥→갑옷 실험선→중앙 게이트 자산을 각각 `TestSandbox`, `TestSandboxLanes`, `TestSandboxPillars`, `TestSandboxArmor`, `TestSandboxGates`에 매핑한다.
- null 방, 빈 씬 이름, 중복 room ID와 중복 씬 이름을 저장·런 생성 경계에서 거부한다.
- `Configure`는 호출자 배열을 복사하고 Core 정의 생성으로 모든 entry를 즉시 검증한다.

## 런 계약

- `PrototypeDungeonRunSession(seed, catalog)`이 Core 그래프, 전투방 배정과 탐색 상태를 한 번 생성한다.
- 특수방에서는 전투방 선택 조회가 `false`를 반환한다.
- 전투방에서는 선택된 방 asset, 씬 이름, 회전과 활성 출구를 `PrototypeDungeonCombatRoomSelection`으로 반환한다.
- 이동·클리어 요청은 별도 Unity 규칙을 만들지 않고 `DungeonRunState`에 위임한다.
- 세션은 전역 singleton이나 `DontDestroyOnLoad`를 사용하지 않는다. 실제 Unity 수명 소유자는 후속 bootstrap 작업에서 명시한다.

## Editor 계약

- `PrototypeContentBuilder`가 실제 카탈로그 ScriptableObject를 Unity 직렬화 경로로 만들고 다섯 entry를 동기화한다.
- `PrototypeContentValidator`가 에셋 존재, 5개 entry, 정확한 room asset·씬 순서와 Core 변환 성공을 확인한다.

## 범위 밖

- 실제 `SceneManager.LoadScene` 호출과 지속 bootstrap.
- 활성·비활성 문 GameObject, trigger, 방 회전 표현과 입장 spawn.
- 특수방 scene·placeholder와 보상 처리.
- 플레이어 체력·폭탄 슬롯·파괴 상태의 방 간 보존.

## 검증 기준

- 런 세션 대상 PlayMode: 모든 전투 노드 선택 해석, 특수방 경계, 잠금·클리어·왕복 위임.
- 카탈로그 대상 PlayMode: 배열 복사, 안정 ID 조회, null·빈·중복 entry 거부.
- 실제 카탈로그 Editor validator 오류 0.
- 전체 EditMode·PlayMode 회귀와 Console Error 0.
- `Tools/Verify.ps1 -StaticOnly` 통과.

## 롤백

카탈로그 저작 타입·실제 에셋, 런 세션, builder·validator·PlayMode 테스트와 이 문서를 한 묶음으로 되돌린다. Core 그래프·배정·탐색 상태는 영향을 받지 않는다.
