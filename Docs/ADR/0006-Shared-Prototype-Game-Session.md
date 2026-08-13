# ADR-0006: 프로토타입 규칙 상태를 공유 게임 세션이 조정한다

- 상태: `Accepted`
- 날짜: 2026-08-14

## 맥락

첫 이동 수직 슬라이스에서는 `PrototypePlayerController`가 자체 `GridState`와
`ManualGameClock`을 만들었다. 폭탄을 연결할 때 이동과 폭탄이 서로 다른 격자나 시계를
소유하면 설치 셀 점유, 설치 직후 이탈, fuse와 이동 순서를 일관되게 판정할 수 없다.

## 결정

Unity Runtime의 `PrototypeGameSession`이 한 플레이 세션에 하나인 `GridState`,
`ManualGameClock`, `PlayerMovementSimulation`, `BombSimulation`을 만들고 조정한다.
Input System의 의미 명령은 세션이 받아 Core 규칙에 전달한다. 플레이어 Transform,
폭탄 mesh와 폭발 표시는 세션의 확정된 결과 이벤트만 소비한다.

폭탄의 조정 가능한 수치와 표현 prefab은 검증된
`PrototypeBombDefinitionAsset`이 소유한다. Core에는 이 에셋을 불변
`BombDefinition`으로 변환한 값만 전달한다.

## 처리 순서

한 Unity frame에서 세션은 다음 순서를 유지한다.

1. Unity가 제공한 유효한 경과 시간을 공유 수동 시계에 반영한다.
2. 현재 이동 의도로 최대 한 번의 논리 셀 전이를 처리한다.
3. 현재 논리 시각까지 만료된 폭탄을 예약 시각 순서로 처리한다.
4. 확정된 이동·설치·폭발 결과를 표현 어댑터에 알린다.

입력 callback에서 설치 요청은 현재 논리 셀과 현재 논리 시각을 사용한다. 표현 실패는
Core 상태를 되돌리지 않는다.

## 대안

- 이동 controller와 폭탄 controller가 각자 격자/시계를 소유: 초기 파일은 적지만
  점유와 시간의 권위 원본이 둘이 된다.
- 모든 규칙을 하나의 MonoBehaviour에 재구현: Unity 의존성이 Core 규칙으로 침투하고
  EditMode 테스트 계약과 중복된다.
- 범용 DI·이벤트 프레임워크 도입: 현재 단일 플레이어 프로토타입에 과하다.

## 결과

- 이동과 폭탄이 동일한 점유 상태와 시간을 본다.
- 플레이어·폭탄 표현은 규칙 상태를 직접 변경하지 않는다.
- 이후 슬롯, 피해, 적 simulation은 같은 세션 처리 순서에 추가할 수 있다.
- 현재 세션은 프로토타입 조정자이며 저장·네트워크·멀티플레이 API를 약속하지 않는다.

## 철회 조건

프로토타입 이후 다른 simulation 소유 모델이 필요해지면 동일한 단일 권위 상태와
결정론적 처리 순서를 보존하는 대안 ADR로 대체한다.
