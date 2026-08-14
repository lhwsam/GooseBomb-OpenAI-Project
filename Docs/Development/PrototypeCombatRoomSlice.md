# 작업: 첫 수제 전투방 저작 수직 슬라이스

- 상태: `Implemented`
- 시작일: 2026-08-14
- 검증 가설: `GDD_v0.2.md` 19·21~23장, `ProtoType_v0.2.md` 테스트 1

## 목표

- 기본 폭탄과 단일 추격자의 유도·회피·퇴로 판단을 관찰할 수 있는 첫 수제 전투방을 만든다.
- 씬 Transform과 런타임 하드코딩이 아니라 검증된 방 데이터 하나를 논리 구조의 권위 원본으로 사용한다.
- 사람이 방의 의도를 읽을 수 있고 AI가 다른 세션에서 안전하게 확장할 수 있는 저작 계약을 확정한다.

## 근거

- [GDD v0.2](../GameDesign/GDD_v0.2.md) 19, 21~23장
- [프로토타입 검증 부록](../GameDesign/ProtoType_v0.2.md) 테스트 1
- [방 저작과 검증](../Systems/RoomAuthoring.md)
- [격자와 이동](../Systems/GridAndMovement.md)
- [테스트 전략](../Testing/TestStrategy.md)

## 범위

- 변경 허용: Core 방 정의와 불변식, 방 ScriptableObject, TestSandbox의 방 참조, builder·validator, 첫 방 데이터, EditMode·PlayMode·WebGL 검증, 관련 문서.
- 변경 금지: 절차 생성, 여러 적, 문 개폐, 보상, 파괴 가능 벽, 완성 아트·조명, Collider/Transform을 논리 권위로 사용, 기존 서드파티 에셋 수정.

## 채택할 최소 계약

- 방 정의는 안정적인 room ID, room type, 홀수 격자 크기, 셀 크기, 플레이어·추격자 spawn, 고정 벽, 출구, 입장 안전 셀, 퇴로 anchor, 순서가 있는 폭탄 유도 순환 경로를 소유한다.
- 정수 XZ 셀이 논리 권위다. TestSandbox의 spawn Transform과 장애물 표현은 방 데이터를 표시하며 저장·검증 시 같은 셀과 일치해야 한다.
- 모든 저작 셀은 방 범위 안에 있고 고정 벽과 spawn·출구·anchor·유도 경로가 겹치지 않아야 한다.
- 플레이어와 추격자는 서로 다른 셀에서 시작하고 시작 즉시 cardinal 접촉하지 않는다.
- 출구는 최소 2개이며 경계 셀과 방향이 일치하고 플레이 가능한 영역에 연결된다.
- 고정 벽을 제외한 전체 플레이 가능 셀은 하나의 연결 성분이어야 한다.
- 플레이어 spawn에서는 서로 다른 첫 cardinal 이동을 사용하는 퇴로가 최소 2개 존재해야 한다. 각 경로는 spawn 재통과 없이 하나 이상의 퇴로 anchor에 도달해야 한다.
- 유도 경로는 최소 4개의 서로 다른 플레이 가능 셀로 이루어진 닫힌 cardinal 순환 경로다. 이는 관찰 의도이며 적 AI의 강제 waypoint가 아니다.
- 첫 방 `prototype-combat-loop`는 11×9, 셀 크기 1, 중앙 십자형 고정 벽 4개, 남·북 출구, 좌·우 퇴로 anchor, 중앙을 도는 짧은 유도 경로를 사용한다.

## 완료 조건

- EditMode에서 유효한 방과 ID 값 동등성, 범위·중복·겹침, 출구 경계, 연결성, 두 퇴로, 닫힌 유도 경로를 검증한다.
- 방 ScriptableObject를 다시 읽어 Core 정의로 변환할 수 있고 콘텐츠 validator가 오류 없이 통과한다.
- TestSandbox는 격자 크기·셀 크기·고정 벽·spawn을 방 데이터에서 읽고, 씬 Transform과 장애물 표현이 데이터 셀과 일치한다.
- PlayMode 회귀 테스트가 임시 방 데이터에서도 같은 권위 경계를 사용한다.
- 실제 TestSandbox 재생에서 추격·접촉·폭탄 유도 처치와 두 방향 이탈이 유지된다.
- WebGL 빌드와 브라우저 smoke에서 기존 필수 gameplay marker, 입력 focus, resize, Console/page 오류 0을 확인한다.
- RoomAuthoring, CurrentState와 검증 문서가 구현된 계약과 일치한다.

## 위험과 롤백

- 첫 방 하나의 규칙을 모든 방에 과도하게 일반화할 위험이 있다. 이번 스키마는 전투방의 실행 불가능 상태만 오류로 막고 재미·난이도는 플레이테스트 판단으로 남긴다.
- 유도 순환 경로는 저작 의도 표기이며 현재 추격 AI가 직접 따라가지 않는다. 경로의 실제 유효성은 플레이테스트에서 확인한다.
- 문제가 생기면 TestSandbox의 방 참조와 새 자산을 제거해 기존 씬 직렬화 값으로 되돌릴 수 있지만, 이후 방 확장은 검증된 단일 원본 경계를 유지해야 한다.

## 구현 및 검증 결과

- `RoomDefinitionId`, `RoomExit`, `CombatRoomDefinition`과 Core 불변식 검증을 구현했다.
- `PrototypeCombatRoomDefinitionAsset`과 `prototype-combat-loop` 자산을 만들고 TestSandbox 격자·고정 벽·spawn의 단일 저작 원본으로 연결했다. 씬에는 중복된 격자 수치와 blocked cell 배열을 남기지 않는다.
- builder가 방 자산을 생성·갱신하고 spawn을 저작 셀에 배치하며, validator가 자산 유효성·씬 참조·spawn/placeholder·장애물 표현 셀을 다시 읽어 비교한다.
- EditMode `BombSwap.Core.Tests` 152/152, PlayMode `BombSwap.Unity.Tests` 47/47, 콘텐츠 validator 오류 0, Unity Console 오류 0을 확인했다.
- Development WebGL 전체 빌드는 140,521,752 bytes, 291.473초, 오류 0으로 성공했다. 설치된 Sentis·vendor 셰이더와 기존 TextMeshPro를 포함한 경고 359개는 이번 방 변경과 무관한 패키지/빌드 경고로 분류했다.
- 실제 Edge headless smoke에서 load, canvas focus, 기존 입력·접촉·이탈·자기 폭발·재유도 처치·방 클리어, resize, browser Console/page 오류 0을 확인했다.
- 검증 증거는 `Artifacts/Verification/20260814-103449-static/`과 `Artifacts/Verification/20260814-102330-web-connected/`에 있으며 Git에서 제외된다.
