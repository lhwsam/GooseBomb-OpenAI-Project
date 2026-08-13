# 작업: 기본 십자 폭탄 3D 수직 슬라이스

- 상태: `Implemented`; 플레이테스트 가설 평가는 아직 남음
- 시작일: 2026-08-14
- 권장 개발 순서: `PrototypeRoadmap.md` 1단계

## 목표

- 플레이어가 TestSandbox에서 `Z`로 현재 셀에 기본 십자 폭탄을 설치할 수 있다.
- 폭탄은 공유 논리 시계의 fuse가 끝나면 벽 차단 규칙에 따라 폭발한다.
- 설치 폭탄과 영향 셀이 3D placeholder로 표시되고 WebGL 브라우저에서 실제 결과가
  관측된다.

## 근거

- [GDD v0.2](../GameDesign/GDD_v0.2.md) 6.2, 7장, 12.1
- [폭탄과 폭발](../Systems/BombAndExplosion.md)
- [격자와 이동](../Systems/GridAndMovement.md)
- [ADR-0001](../ADR/0001-Logical-XZ-Grid.md), [ADR-0002](../ADR/0002-Core-Unity-Separation.md), [ADR-0003](../ADR/0003-Manual-Clock-And-Seed.md), [ADR-0006](../ADR/0006-Shared-Prototype-Game-Session.md)
- 기존 `BombSimulation`, `PlayerMovementSimulation`, `TestSandbox`

## 범위

- 변경 허용: `Assets/Game`, `Docs`, `Tools/WebGLSmoke.mjs`, TestSandbox first-party 콘텐츠.
- 변경 금지: `Assets/Feel`, `Assets/Plugins`, 외부 패키지와 일반 Unity 템플릿 에셋.
- 비목표: 피해·체력, 두 슬롯·교체 규칙, 설치 쿨타임, 적 AI, 완성 VFX/audio.

## 계약과 불변식

- 입력: 의미 명령 `PlaceBomb`; 키 경로는 Input Actions가 소유한다.
- 출력: 성공한 `BombPlaced`, fuse/chain으로 확정된 `BombExploded`, 3D placeholder.
- 상태 소유자: `PrototypeGameSession`이 공유 Core 격자·시계·이동·폭탄 simulation을
  조정한다. 조정 수치와 prefab은 검증된 ScriptableObject가 소유한다.
- 설치 실패는 폭탄 ID, 점유, 표현을 남기지 않는다.
- Transform·Collider·VFX는 설치 가능 여부와 폭발 셀의 권위 원본이 아니다.
- WebGL 런타임 경로는 스레드·동기 대기 없이 PlayerLoop에서 진행하고 반복 표현은
  풀링한다.

## 완료 조건

- 구현: 실제 입력→Core 설치→fuse→폭발→3D 표현 흐름이 연결된다.
- EditMode: 기존 폭탄 규칙 전체 회귀가 통과한다.
- PlayMode: 실제 Input System 설치, 공유 점유, fuse 폭발, pooled 표현 생명주기를
  검증한다.
- 콘텐츠: ScriptableObject·prefab·scene 참조 validator가 통과한다.
- WebGL: 실제 빌드에서 `move`, 성공한 `place-bomb`, `bomb-exploded`, swap/pause 입력,
  focus, resize, browser Console을 확인한다.
- 문서: RuntimeFlow, BombAndExplosion, CurrentState를 실제 구현과 맞춘다.

## 검증 명령과 증거

- 구조: `./Tools/Verify.ps1 -StaticOnly`
- 연결 Editor: Core EditMode, first-party PlayMode, 콘텐츠 validator, Console error 0.
- 마일스톤: Development WebGL build와 `Tools/WebGLSmoke.mjs` 보고서.
- 기준선: Core 89개, PlayMode 36개, 직전 WebGL smoke 통과.

## 위험과 롤백

- 직렬화 위험: TestSandbox와 신규 ScriptableObject/prefab은 Unity Editor API로만 저장하고
  validator로 재읽는다.
- 성능 위험: 폭탄·폭발 placeholder는 사전 풀을 사용하되 풀 초과 시 표현 누락 대신
  제한적으로 확장한다.
- 롤백 단위: 공유 세션/표현 코드, 생성 콘텐츠, TestSandbox 연결, 문서와 smoke 계약을
  하나의 큰 커밋으로 되돌릴 수 있게 유지한다.
