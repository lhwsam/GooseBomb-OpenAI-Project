# 작업: 장갑병 수비와 panic run

- 상태: 구현·자동/Unity/WebGL 검증 `Accepted`, 수치·공간 재미 `Proposed`
- 기준일: 2026-08-18

## 목표

- 장갑병은 장갑이 남아 있을 때 저작 spawn 주변의 작은 교차점을 지키고, 플레이어를 방 끝까지 단순 추격하지 않아야 한다.
- 첫 폭발로 장갑이 파괴되면 폭발 중심 반대편의 유효한 직선 가지와 최대 3칸 도착점을 고정·예고한 뒤 빠르게 달아나야 한다.
- 플레이어는 첫 폭탄의 방향으로 장갑병의 두 번째 피격 위치를 만들고, 예고된 도착점에 다음 폭탄을 선행 설치할 수 있어야 한다.
- `prototype-combat-armor`는 이 질문을 읽을 수 있는 T 갈림길과 양쪽 panic run 가지를 제공해야 한다.

## 근거

- [전투·적 AI·레벨·보스 개선 제안](../GameDesign/CombatEnemyLevelBossImprovementProposal.md) 순서 3과 장갑병 제안
- [적 행동](../Systems/EnemyBehavior.md), [격자와 이동](../Systems/GridAndMovement.md), [방 저작과 검증](../Systems/RoomAuthoring.md)
- 기존 코드 진입점: `ArmoredEnemySimulation`, `PrototypeArmoredPresenter`, `PrototypeContentBuilder`, `Tools/ArmoredWebGLSmoke.mjs`

## 범위

- 변경 허용: 장갑병 Core 정의·상태 머신·결과, Unity 저작 정의·presenter·진단 marker, 관련 테스트·하네스, Armor room asset·scene을 만드는 Editor builder·validator, 관련 문서.
- 직렬화 변경: 수비 반경, panic 예고·속도·거리·회복 수치와 예고 셀 prefab 참조, Armor 방 고정 벽·안전 셀·퇴로·유도 loop. 저장은 Unity Editor builder를 통해서만 수행한다.
- 변경 금지: `Assets/Feel`, `Assets/Plugins`, 패키지, Input Actions, WebGL/렌더 파이프라인 설정, 다른 네 전투방 설계.
- 명시적 비목표: 범용 다중 적 ID·동일 목적 셀 경합, NavMesh, 폭발 위험 완전 회피, 곡선·BFS panic 경로, 벽 파괴 돌진, 최종 아트·VFX·오디오, 최종 튜닝 확정.

## 계약과 불변식

- 내구도 상태 `Armored → Broken → Dead`와 행동 상태 `Guard → PanicTelegraph → PanicRun → PanicRecover → Chase`를 분리한다. 두 번째 서로 다른 폭발은 panic 단계와 무관하게 사망시킨다.
- `Guard`는 spawn을 중심으로 Manhattan 반경 1칸 안에서만 플레이어와 가까워지는 cardinal 한 칸을 기존 느린 cadence로 이동한다. 반경 밖으로 나가거나 거리를 늘리는 배회는 하지 않는다.
- 첫 유효 폭발은 실제 `BombExplosion.Origin`을 panic 선택에 전달한다. 현재 셀에서 네 cardinal 직선 가지를 최대 3칸까지 조사하고 가장 긴 유효 가지를 먼저 선택해 짧은 벽 앞 도주보다 읽을 수 있는 약 3칸 달리기를 우선한다. 길이가 같으면 폭발→장갑병 벡터의 반대 방향 투영, 도착점의 폭발 중심 Manhattan 거리, `North → East → South → West` 순으로 결정한다.
- 가지는 선택 순간의 바닥·점유 상태로 길이와 도착점을 한 번 고정한다. 최소 한 칸도 이동할 수 없으면 달리기를 생략하고 회복 뒤 공격 추격으로 전환한다.
- `PanicTelegraph`는 고정된 경로 전체를 collider 없는 셀 placeholder로 표시한다. 예고 뒤 플레이어나 기존 장애물이 이동해도 방향·도착점을 바꾸거나 경로를 늘리지 않는다.
- `PanicRun`은 고정 경로를 빠른 cadence로 한 셀씩 이동한다. 새 폭탄·벽·actor가 다음 셀을 막으면 재조준하지 않고 즉시 `PanicRecover`로 전환한다.
- 고정 경로를 소진하거나 조기 차단되면 짧은 회복 뒤 `Chase`가 된다. `Chase`는 기존 Broken 3 cells/s 국소 추격과 접촉 피해 계약을 사용한다.
- Core는 UnityEngine, Transform, Collider, NavMesh를 참조하지 않는다. 매 frame 새 컬렉션을 만들지 않고 최대 3칸 panic 경로 저장소를 simulation이 재사용한다.

## Armor 방 저작 계약

- 플레이어 `(0,-2)`, 추격자 `(4,4)`, 장갑병 `(0,1)`을 유지한다. 첫 십자 폭탄 fuse 동안 장갑병이 수비 반경 안의 `(0,0)`으로 접근하면 폭발 범위에 들어온다.
- `z=2`의 `x=-1..1` 상단 막이 정면 도주를 제한하고, `z=-2..-1`의 `x=±2`가 남쪽 접근 통로를 만든다.
- `(-4,0)·(4,0)`은 좌우 panic 가지의 3칸 종단이다. 폭발 방향과 점유에 따라 좌우 중 유효한 반대편 도착점이 선택되어야 한다.
- 플레이어 안전 셀 `(0,-2)·(-1,-2)·(1,-2)`, 퇴로 anchor `(-3,-2)·(3,-2)`, 외곽 유도 loop는 초기 비파괴 상태에서도 모두 연결되어야 한다.

## 완료 조건

- 구현: 수비 반경, 폭발 중심 기반 고정 panic 가지, 예고·달리기·조기 차단·회복·공격 추격을 Core와 Unity에 연결한다.
- EditMode: 정의 값·거부 경계, 수비 반경, 네 방향·대각선·동률 선택, 고정 거리, 정확한 시간 경계, 동적 차단, 두 폭발·점유·시계 회귀를 검증한다.
- PlayMode: 첫 피격의 scale 축소·본체 material override 부재와 전체 panic 경로 표시, 단계별 이동 보간·예고 회수, 두 번째 사망·점유 제거·단일 방 클리어를 검증한다.
- Content/Visual: Editor builder로 정의/prefab과 Armor asset/scene을 저장하고 validator 오류 0, 대표 캡처에서 T 갈림길과 예고 경로를 확인한다.
- WebGL/브라우저: Armor 전용 빌드에서 `Guard → Broken/Telegraph → PanicRun → Recover/Chase → Dead`와 두 폭탄 위치 변경, 기존 전체 던전 키보드·Gamepad 회귀, Console/page error 0을 확인한다.
- 문서: `EnemyBehavior`, `RoomAuthoring`, `CurrentState`, Testing 문서와 이 계약을 실제 결과로 동기화한다.

## 검증 명령과 증거

- Core 반복: 연결 Unity EditMode `BombSwap.Core.Tests`; Editor가 닫힌 경우 `./Tools/Verify.ps1 -Tier Fast`.
- Unity 통합: 연결 Unity PlayMode `BombSwap.Unity.Tests`, `PrototypeContentValidator`, Console 오류 확인.
- Web: 연결 `ConnectedWebGLBuildHarness` 또는 Editor가 닫힌 경우 `./Tools/Verify.ps1 -Tier Web`; 기본 `WebGLSmoke.mjs`, `GamepadWebGLSmoke.mjs`, 전용 `ArmoredWebGLSmoke.mjs`.
- 실제 산출물: EditMode `314/314`는 `Artifacts/Verification/ConnectedTests/20260818-101541-974.json`, PlayMode `129/129`는 `Artifacts/Verification/ConnectedTests/20260818-101558-137.json`에 보존했다.
- 정식 11씬 Development WebGL은 `Artifacts/Verification/20260818-191733-armored-panic-web/`에서 138,299,311 bytes·185.367초·오류 0으로 성공했다. Edge keyboard `40/40`, Gamepad `14/14`, 두 실행의 Console/page error 0이 통과했다.
- Armor 시작 전용 Development WebGL은 `Artifacts/Verification/20260818-armored-panic-standalone-web-v3/`에서 138,299,652 bytes·17.237초 incremental·오류 0으로 성공했다. 전용 smoke 7개 검사가 `Broken/Telegraph east 3 → PanicRun → Recover → Chase → Dead`와 서로 다른 위치의 두 폭탄, 55개 Core marker, Console/page error 0을 확인했고 예고·최종 screenshot을 남겼다.
- `PrototypeContentValidator`는 정의 수치·collider 없는 예고 prefab·Armor T 좌표·scene 참조를 오류 0으로 확인했다. 전용 시작을 위해 사용한 임시 scene은 검증 후 삭제했으며 정식 Build Settings 11개 순서는 변경하지 않았다.
- 기준선: 비밀문 경계 완료 트리 EditMode `311/311`, PlayMode `128/128`, Edge keyboard `40/40`, Gamepad `14/14`, Console/page error 0.

## 위험과 롤백

- 반경 1, 예고 0.6초, 최대 3칸, panic 6 cells/s, 추격 3 cells/s, 회복 0.5초는 자동 검증용 `Proposed` 값이다. 첫 폭발 방향과 두 번째 설치 위치가 실제로 읽히는지는 사람 플레이가 판정한다.
- 새 예고 prefab은 collider와 runtime material을 만들지 않고 presenter가 최대 3개를 풀링한다.
- 롤백 단위는 장갑병 정의·simulation·presenter·예고 prefab, Armor 방 저작 데이터·scene, 테스트·하네스·문서를 한 묶음으로 한다.
