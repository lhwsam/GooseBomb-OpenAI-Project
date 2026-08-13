# 작업: 설치자 한정 폭탄 셀 통과

- 상태: `Implemented`
- 시작일: 2026-08-14
- 권장 개발 순서: `PrototypeRoadmap.md` 1단계

## 목표

- 폭탄을 설치한 actor만 해당 폭탄과 함께 있는 셀에서 한 번 빠져나올 수 있다.
- 설치자가 셀을 벗어난 뒤에는 폭탄이 제거될 때까지 해당 셀에 다시 들어갈 수 없다.
- 다른 actor는 설치자의 통과 권한을 공유하거나 다른 actor의 점유를 이동시킬 수 없다.

## 근거

- [GDD v0.2](../GameDesign/GDD_v0.2.md) 6.2
- [격자와 이동](../Systems/GridAndMovement.md)
- [폭탄과 폭발](../Systems/BombAndExplosion.md)
- [ADR-0001](../ADR/0001-Logical-XZ-Grid.md), [ADR-0002](../ADR/0002-Core-Unity-Separation.md)

## 범위

- 변경 허용: Core actor/격자/폭탄/이동 계약, TestSandbox 세션 연결, 관련 테스트·문서.
- 변경 금지: 직렬화 콘텐츠 스키마, Input Actions, vendor 에셋, ProjectSettings.
- 비목표: 밀기, 순간이동, 여러 플레이어, 적 AI, 피해, 설치 쿨타임.

## 계약과 불변식

- 모든 actor는 세션에서 유효하고 고유한 `ActorId`를 가진다.
- `GridState`는 actor ID와 현재 셀의 양방향 대응을 점유 bit와 함께 원자적으로 유지한다.
- 폭탄 결과는 설치자 `ActorId`를 보존한다.
- 통과 권한은 `ActorId`·`BombId`·설치 셀 한 쌍이며 설치 직후에만 부여된다.
- 설치 셀에서 처음 성공적으로 나가거나 폭탄이 먼저 제거되면 권한은 종료되고 복구되지 않는다.
- 권한 없는 actor가 폭탄과 같은 셀에 있더라도 그 폭탄을 통과 근거로 이동할 수 없다.
- 목적지의 폭탄은 모든 actor의 진입을 막는다.

## 완료 조건

- EditMode: actor 고유 점유, 다른 actor 이동 거부, 소유자 권한 부여·이탈 종료·재진입 차단·폭발 전 종료를 검증한다.
- PlayMode: 실제 `Z` 설치 뒤 이동으로 탈출하고 반대 입력으로 재진입하지 못하는 흐름을 검증한다.
- 회귀: 콘텐츠 validator, 전체 Core/PlayMode, Console 오류 0, StaticOnly 통과.
- 문서: GridAndMovement, BombAndExplosion, CurrentState가 실제 계약과 일치한다.

## 위험과 롤백

- 기존 종류 기반 actor 점유 API를 ID 기반 API로 바꾸므로 모든 호출자와 테스트를 같은 변경에서 전환한다.
- 직렬화 에셋 변경은 없으며 Core/Runtime/테스트/문서를 하나의 커밋 단위로 되돌릴 수 있다.

## 검증 결과

- Unity import/compile: Console 컴파일 오류 0.
- EditMode: `BombSwap.Core.Tests` 93/93 통과.
- PlayMode: `BombSwap.Unity.Tests` 39/39 통과. 실제 Input System의 `Z → W → S` 흐름으로 설치 셀 탈출과 재진입 차단을 검증했다.
- `PrototypeContentValidator`: 통과.
- `Tools/Verify.ps1 -StaticOnly`: 통과. 기록 산출물 `Artifacts/Verification/20260814-081856-static/`.
- Development WebGL 빌드와 Edge headless browser smoke: 통과. 빌드 오류 0, 브라우저 Console/page 오류 0. 통합 산출물 `Artifacts/Verification/20260814-080915-web-connected/`.
