# 작업: 결정론적 한 층 그래프 Core 수직 슬라이스

- 상태: `Implemented`; Core 자동 검증 완료, Unity 탐색 연결 대기
- 기준일: 2026-08-14
- 기준선 commit: `0a81789`

## 목표

- 개발자가 명시적 정수 seed 하나로 시작방, 첫 전투 뒤 폭탄 보상, 선택 전투 가지, 보스 전실과 보스방을 포함하는 한 층 논리 그래프를 재현할 수 있게 한다.
- 동일 생성 버전·정의·seed는 Unity Editor와 WebGL/IL2CPP에서 같은 노드 타입, 방 좌표와 연결을 만들 수 있어야 한다.
- 이 단계는 탐색 가설을 실행할 수 있는 Core 원본을 만들며, 실제 씬 전환·문·미니맵과 탐색 재미 판정은 후속 Unity 수직 슬라이스로 남긴다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 4~5장, 19~23장, 36장 던전 범위.
- `Docs/GameDesign/ProtoType_v0.2.md` 가설 E와 테스트 4.
- `Docs/Systems/DungeonGeneration.md`.
- `Docs/ADR/0003-Manual-Clock-And-Seed.md`.

## 범위

- 변경 허용: `Assets/Game/Core/Dungeon`, 관련 EditMode 테스트와 `Docs`.
- 변경 금지: 현재 네 TestSandbox 씬·room asset·Build Settings, 고정 WebGL 플레이테스트 산출물, 폭탄·적·이동 수치, vendor와 패키지.
- 비목표: 실제 씬 로드와 문 잠금/개방, 방 prefab 선택·회전, 적 조합, 두 번째 폭탄 선택 UI, 되돌아가기, 미니맵, 보물/회복/비밀방, 보스 규칙, 저장/재개.

## 계약과 불변식

- 입력은 `DungeonGenerationDefinition`과 모든 `int` 값을 허용하는 명시적 seed다. 전역 랜덤, `UnityEngine.Random`, 현재 시각과 호출 순서를 읽지 않는다.
- 기본 프로토타입 정의는 일반 전투방 4~5개, 보스 주 경로 일반 전투방 3개다. GDD의 전체 3~5 범위 중 선택 가지와 보스 경로 3개를 동시에 보장하기 위해 실제 테스트 4 기본값은 4~5를 사용한다. 정확한 분포는 `Proposed`다.
- 필수 주 경로는 `Start → Combat → BombReward → Combat… → BossAntechamber → Boss`다. 첫 전투 뒤 보상은 모든 보스 경로에 포함된다.
- 보스는 보스 전실 하나하고만 연결되고, 보스 전실을 통하지 않은 진입은 없다.
- 전체 그래프는 연결된 트리다. 노드 수가 `N`이면 연결은 `N-1`개이며 중복·자기 연결·사이클이 없다.
- 보스 주 경로에 포함되지 않는 전투방을 최소 하나 두고, 보상 이후의 주 경로 노드에서 뻗는 단일 선택 가지로 배치한다.
- 방 그래프 좌표는 고유한 정수 XZ 좌표다. 연결된 방은 cardinal 인접하고, 연결되지 않은 방이 우연히 cardinal 인접해 암시적 문/루프를 만들지 않는다.
- 노드 ID, 노드 배열과 연결 배열은 안정된 순서를 가진 불변 snapshot이다. 호출자가 내부 컬렉션을 변경할 수 없다.
- 레이아웃 탐색은 유한한 후보를 결정적 순서로 검사한다. 생성 실패를 무한 재시도하거나 다른 seed로 조용히 대체하지 않고 원래 seed를 포함한 예외를 낸다.
- 생성 알고리즘 버전은 결과에 보존한다. 향후 알고리즘을 바꾸면 같은 seed 호환 여부를 명시적으로 결정한다.

## 완료 조건

- 구현: 고정 알고리즘 RNG, 방 그래프 좌표·노드 ID·노드·연결·그래프 snapshot, 생성 정의와 생성기를 `BombSwap.Core`에 구현한다.
- EditMode: 동일 seed 재현, 여러 seed 다양성, 음수/0/극단 seed, 4~5 전투방 범위, 필수 노드, 첫 전투 보상, 보스 경로 3개, 선택 가지, 연결 트리, 좌표/연결 불변식, 잘못된 정의와 조회 경계를 검증한다.
- Unity: 기존 Unity 프로젝트가 신규 Core와 테스트를 오류 없이 import/compile하고 전체 EditMode 회귀를 통과한다.
- WebGL: Core-only 단계에서는 새 빌드를 요구하지 않는다. Unity Runtime 탐색·씬/입력을 연결하는 후속 마일스톤에서 실제 WebGL을 검증한다.
- 문서: `DungeonGeneration.md`, `CurrentState.md`, 테스트 문서를 실제 구현과 검증 수치에 맞춘다.

## 검증 명령과 증거

- `./Tools/Verify.ps1 -StaticOnly`.
- `./Tools/Verify.ps1 -Tier Fast` 또는 실행 중 Editor가 있으면 연결된 전체 EditMode와 Console 오류 0.
- 신규 `DungeonGeneratorTests` 대상 실행과 전체 `BombSwap.Core.Tests`.
- 산출물은 `Artifacts/Verification/`에 보존하고 Git에 포함하지 않는다.

## 완료 증거

- Unity 6000.5.3f1에서 `BombSwap.Core`와 `BombSwap.Core.Tests` 재컴파일 성공.
- `DungeonGeneratorTests`: 15/15 통과. seed 0 golden snapshot, 음수·0·정수 극단, 512개 연속 seed의 4/5방 분포·64개 초과 signature 다양성과 전체 그래프 불변식을 포함한다.
- 최종 전체 EditMode: 221/221 통과, 실패·건너뜀 0. 증거 `Artifacts/Verification/ConnectedTests/20260814-110501-629.json`.
- `PrototypeContentValidator`: `RoomType.Combat = 0` 직렬화 호환과 기존 네 방·씬·Build Settings 회귀 오류 0.
- Unity Console 오류 0, `./Tools/Verify.ps1 -StaticOnly` 통과.
- 실행 중인 같은 프로젝트 Editor 잠금 때문에 별도 batchmode `-Tier Fast`는 실행하지 않았다. 연결된 Unity 컴파일·전체 EditMode·validator·Console로 해당 단계의 개별 증거를 수집했으며, 커밋 뒤 StaticOnly를 다시 실행한다.

## 위험과 롤백

- 초기 생성기가 단일 전투 가지 모양만 지원하므로 탐색 재미나 장기 콘텐츠 확장성을 증명하지 않는다. 테스트 4에 필요한 최소 선택만 구현하고 범용 그래프 프레임워크를 선행하지 않는다.
- seed 재현 계약 때문에 RNG와 후보 순서 변경은 저장 호환 결정이 된다. 생성 버전을 함께 바꾸고 이전 결과와 섞지 않는다.
- 4~5 전투방 기본값은 플레이테스트 전 `Proposed`다. 되돌아가기 피로가 관찰되면 그래프 규칙이 아니라 정의 수치를 먼저 조정한다.
- 롤백 단위는 `Assets/Game/Core/Dungeon`, 신규 EditMode 테스트와 이 작업 문서를 한 묶음으로 한다. 기존 전투 코드·씬·WebGL 빌드는 영향을 받지 않는다.
