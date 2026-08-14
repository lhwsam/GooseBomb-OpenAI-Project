# 작업: 기본 전투 3방 플레이테스트 시퀀스

- 상태: `Completed`
- 시작일: 2026-08-14
- 검증 가설: `ProtoType_v0.2.md` 테스트 1, `GDD_v0.2.md` 22장

## 목표

- 한 WebGL 실행에서 서로 다른 공간 판단을 요구하는 수제 전투방 3개를 순서대로 플레이할 수 있게 한다.
- 각 방의 spawn·고정 벽·퇴로·유도 경로를 기존 검증된 room asset 계약으로 저작한다.
- 방 클리어 후 다음 플레이테스트 씬으로 한 번만 전환하고 세 번째 방에서는 현재 상태를 유지한다.

## 근거

- [프로토타입 검증 부록](../GameDesign/ProtoType_v0.2.md) 테스트 1: 기본 폭탄, 추격자, 수제 방 2~3개
- [방 저작과 검증](../Systems/RoomAuthoring.md)
- [첫 기본 전투 관찰 프로토콜](../Playtesting/FirstCombatProtocol.md)
- [런타임 흐름](../Architecture/RuntimeFlow.md)
- [테스트 전략](../Testing/TestStrategy.md)

## 범위

- 변경 허용: 전투방 ScriptableObject 2개 추가, TestSandbox scene variant 2개 추가, 기존 builder·validator·Build Settings 확장, 방 클리어 뒤 개발용 scene 전환 Unity 어댑터, PlayMode/브라우저 smoke와 관련 문서.
- 변경 금지: Core 전투 규칙·수치, 새 적, 여러 적, 절차 생성, 보상·성장, 파괴 가능 벽, 패키지·ProjectSettings 직접 변경, 서드파티 에셋 수정.
- 비목표: 방 그래프·분기, 영구 진행도, 플레이어 사망 재시작 UI, 완성 전환 연출, 세 방의 재미 자동 판정.

## 콘텐츠 계약

### 1. 중앙 루프

- 기존 `prototype-combat-loop`, 11×9.
- 중앙 십자 기둥 4개와 짧은 8셀 루프로 설치 직후 근거리 유도·퇴로 선택을 관찰한다.
- 플레이어 `(0, 0)`, 추격자 `(1, -1)`.

### 2. 평행 통로

- `prototype-combat-lanes`, 11×9.
- `x=-2`와 `x=2`에 세로 벽 3셀씩을 두어 중앙 통로와 좌·우 우회로를 만든다.
- 플레이어 `(0, -2)`, 추격자 `(0, 2)`로 마주 보게 배치한다.
- 벽 바깥을 도는 큰 닫힌 루프와 좌·우 퇴로를 제공해 좁은 통로 설치와 우회 유도를 관찰한다.

### 3. 엇갈린 기둥

- `prototype-combat-pillars`, 11×9.
- 중앙에 엇갈린 기둥 5개를 두고 서쪽 아래 플레이어와 동쪽 위 추격자를 배치한다.
- 서·동 출구와 중앙 외곽 루프를 사용해 긴 접근, 시야상 여러 통과 선택과 대각선 형태의 우회를 관찰한다.

모든 방은 기존 `CombatRoomDefinition`의 범위·중복·spawn 안전·출구 경계·전체 연결성·서로 다른 첫 이동의 퇴로 2개·닫힌 cardinal 유도 경로 불변식을 통과해야 한다.

## 런타임 계약

- `PrototypeRoomAdvanceController`는 현재 `PrototypeGameSession.RoomCleared`만 구독한다.
- 첫 번째 씬은 `TestSandboxLanes`, 두 번째는 `TestSandboxPillars`를 다음 씬으로 가진다.
- 클리어를 처음 받으면 양수의 짧은 realtime 지연 뒤 Build Settings에 포함된 다음 씬을 이름으로 로드한다.
- 중복 클리어나 활성 coroutine 중 재요청은 추가 전환을 만들지 않는다.
- 다음 씬 이름이 비어 있는 세 번째 방은 전환하지 않는다.
- 전환 상태는 MonoBehaviour가 소유하며 room asset에 mutable run state를 저장하지 않는다.
- 전환은 Unity PlayerLoop/coroutine과 `SceneManager`만 사용하고 thread·동기 대기·네트워크를 사용하지 않는다.

## 완료 조건

- builder가 세 room asset을 Unity 직렬화 경로로 생성·재구성하고 세 씬의 context, spawn, 장애물과 다음 씬 참조를 동기화한다.
- validator가 room asset 전체의 ID 중복·Core 변환, 각 씬의 기대 room 참조·표현 셀·전환 참조, Build Settings 순서를 검증한다.
- EditMode 전체 Core 회귀가 통과한다.
- PlayMode가 유효/무효 controller 설정, 단일 pending 전환과 마지막 방 무전환을 검증하고 전체 회귀가 통과한다.
- 실제 세 자산과 세 씬을 저장 후 다시 읽어 validator 오류 0과 Console 오류 0을 확인한다.
- Development WebGL 빌드가 성공하고 브라우저에서 첫 방 클리어 뒤 두 번째·세 번째 씬 준비 marker를 관측한다.
- 자동 검증 결과를 재미 판정으로 표현하지 않고 플레이테스트 문서에 세 방의 관찰 의도를 반영한다.

## 위험과 롤백

- scene 복제로 직렬화 참조가 어긋날 수 있으므로 builder가 저장 후 validator로 각 씬을 다시 연다.
- 자동 전환이 인터뷰 시점을 방해할 수 있어 전환 지연은 `Proposed`이며, 플레이테스트 증거가 생기면 수동 진행 또는 중간 화면으로 바꾼다.
- 롤백은 추가 room asset·scene variant·전환 controller와 Build Settings 항목을 제거하면 첫 `TestSandbox` 단일 방으로 돌아간다.

## 구현 및 검증 결과

- `PrototypeCombatLanes.asset`, `PrototypeCombatPillars.asset`과 대응하는 `TestSandboxLanes.unity`, `TestSandboxPillars.unity`를 Unity Editor builder로 생성했다.
- 기존 `TestSandbox.unity`를 포함한 세 씬에 `PrototypeRoomAdvanceController`를 연결하고 Build Settings의 첫 세 enabled 씬으로 고정했다.
- builder 재실행 후 세 room asset의 Core 변환·고유 ID, 세 씬의 room/spawn/장애물/다음 씬 참조와 Build Settings 순서를 validator 오류 0으로 확인했다.
- Unity Scene View 다각도 캡처에서 평행 통로의 두 세로 벽과 엇갈린 기둥의 다섯 장애물 배치를 시각 확인했다.
- 최종 문서 반영 뒤 `Tools/Verify.ps1 -StaticOnly` 통과: `Artifacts/Verification/20260814-110831-static/`.
- 연결된 Unity Test Runner에서 EditMode 152/152, PlayMode 52/52 통과, 실패·건너뜀·불확정 0, Console 오류 0을 확인했다.
- Development WebGL 빌드 성공: `Artifacts/Verification/20260814-105806-web-connected/`, 140,537,511 bytes, 69.669초, 오류 0. 기존 Sentis·vendor·TextMeshPro 셰이더 범주의 경고 359건은 남아 있다.
- Windows Edge headless smoke에서 load, canvas focus, 기존 입력·피해·폭발·처치 사건과 첫 방→평행 통로→엇갈린 기둥 전환을 한 세션에서 모두 관측했고 browser Console/page 오류는 0이었다.
- 위 결과는 직렬화·규칙·브라우저 연결의 정확성 증거이며, 세 방의 유도 재미와 1.25초 전환 지연의 적절성은 사람 플레이테스트 전까지 `Proposed`다.
