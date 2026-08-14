# 방향성 직선 폭탄 수직 슬라이스

- 상태: 동작 계약·구현 `Accepted`, 수치·재미 판정 `Proposed`
- 게임 의도: [GDD v0.2 3.2, 12.2](../GameDesign/GDD_v0.2.md), [프로토타입 가설 B](../GameDesign/ProtoType_v0.2.md)
- 시스템 계약: [폭탄과 폭발](../Systems/BombAndExplosion.md), [두 슬롯과 쿨타임](../Systems/WeaponSlotsAndCooldown.md)

## 목표

첫 폭탄 보상의 오른쪽 후보를 단순 범위 3 십자가 아니라 플레이어가 바라보는 앞쪽으로만 길게 폭발하는 `prototype-line`으로 교체한다. 광역 폭탄과 직선 폭탄이 서로 다른 방 구조와 적 정렬에서 다른 설치 위치를 만들 수 있는 상태까지 구현한다.

이 슬라이스는 폭발 모양과 방향 계약의 정확성을 구현한다. 두 후보 중 어느 쪽이 더 재미있거나 균형 잡혔는지는 사람 플레이테스트 전까지 판정하지 않는다.

## 플레이어에게 보이는 계약

- 마지막으로 입력한 유효한 상하좌우 방향이 플레이어의 바라보는 방향이다.
- 방향키를 떼면 이동은 즉시 멈추지만 바라보는 방향은 유지된다.
- 막힌 셀을 향해 방향키를 눌러도 바라보는 방향은 해당 입력으로 바뀐다.
- 새 런에서 이동 입력 전 기본 바라보기는 북쪽이다.
- `prototype-line`을 설치하면 설치 순간의 바라보는 방향이 폭탄에 고정된다.
- 설치 후 플레이어가 이동하거나 방향을 바꿔도 이미 설치한 폭탄의 방향은 변하지 않는다.
- fuse가 끝나면 원점과 고정된 앞쪽 한 ray만 범위까지 폭발한다. 옆과 뒤에는 영향이 없다.
- `Void`와 파괴 불가 벽은 효과 없이 ray를 끝낸다. 파괴 가능 벽은 해당 셀에 효과와 파괴를 적용한 뒤 ray를 끝낸다.
- 다른 폭탄과의 연쇄 지연, 동일 시각 순서, 피해와 표현 사건은 기존 폭탄과 같은 계약을 사용한다.

## 상태 소유와 데이터 흐름

```text
Input System
  -> PlayerCommand.Move(cardinal)
  -> PlayerMovementSimulation.FacingDirection
  -> PrototypeGameSession 설치 요청
  -> BombWeaponLoadout
  -> BombSimulation.ActiveBomb.PlacementDirection
  -> ForwardLineExplosionResolver
  -> BombExplosion
  -> Unity presenter / damage / chain scheduler
```

- `PlayerMovementSimulation`이 현재 이동 방향과 마지막 바라보기 방향을 구분해 소유한다.
- `BombSimulation`이 설치 방향을 폭탄별 불변 상태로 소유한다. Transform 회전이나 Animator는 권위가 아니다.
- 방향이 필요한 폭발 모양은 `CardinalDirection.None`으로 설치할 수 없다.
- 십자와 광역 폭탄은 방향과 무관하며 기존 방향 없는 Core 호출도 유지한다.
- `BombSnapshot`과 `BombExplosion`은 검증·표현·하네스가 설치 방향을 관찰할 수 있게 방향을 노출한다.

## 콘텐츠 마이그레이션

현재 `prototype-long-cross` 정의와 청록색 placeholder의 GUID를 보존한 채 Unity `AssetDatabase`로 다음 이름에 이동한다.

- 정의: `PrototypeLineBomb.asset`, 안정 ID `prototype-line`, shape `ForwardLine`
- 폭탄 prefab: `LineBombPlaceholder.prefab`
- 폭발 prefab: `LineExplosionCellPlaceholder.prefab`
- material: `LineBomb.mat`, `LineExplosion.mat`

보상 catalog의 두 후보 순서는 `prototype-area`, `prototype-line`이다. 기존 수치인 fuse 2.25초, 범위 3, 설치 쿨타임 2.25초와 표시 시간 0.25초는 유지한다. 이는 공간 패턴만 먼저 비교하기 위한 `Proposed` 값이며, 직선 폭탄을 더 빠르게 만들지는 플레이테스트 뒤 별도 조정한다.

## 명시적 비목표

- 조준 전용 입력, 마우스 조준, 대각선 또는 자유 각도 폭발
- 설치 뒤 방향 회전, 원격 기폭, 폭탄 투척
- 폭탄별 공격력, 동시 설치 수, 상태 이상
- 캐릭터 mesh·Animator의 시각적 회전과 신규 완성형 VFX/audio
- 기존 저장 데이터 호환 계층. 현재 프로토타입에는 영속 run save가 없다.

## 완료 조건

- Core EditMode 테스트가 네 방향 셀 집합, 설치 방향 고정, 벽 차단·파괴, 방향 누락 거부와 기존 십자·광역 회귀를 검증한다.
- PlayMode 테스트가 이동 입력 해제 뒤 바라보기 유지와 세션 설치 방향 전달을 검증한다.
- 콘텐츠 builder가 기존 long-cross 에셋을 GUID 보존 이동하고 validator가 새 경로·ID·shape·catalog 순서를 확인한다.
- 전체 EditMode·PlayMode 테스트와 콘텐츠 validator가 통과하고 Unity Console 오류가 없다.
- Development WebGL 빌드에서 보상 선택, 네 방향 중 대표 방향 설치·폭발 marker, 기존 입력·폭탄·던전 흐름과 browser Console 오류 0을 확인한다.
- [폭탄과 폭발](../Systems/BombAndExplosion.md), [두 슬롯과 쿨타임](../Systems/WeaponSlotsAndCooldown.md), [런타임 흐름](../Architecture/RuntimeFlow.md), [현재 상태](CurrentState.md)를 구현 사실에 맞게 갱신한다.

## 위험과 롤백

- `BombExplosionShape` 직렬화 enum에는 기존 값 0·1을 유지하고 `ForwardLine`을 새 값으로 추가한다.
- 에셋 이동은 `.meta`와 GUID를 보존해야 한다. 새 경로 검증과 legacy 경로 부재 검증을 함께 수행한다.
- 직선 폭탄은 같은 수치에서 기존 긴 십자보다 영향 셀이 적다. 자동 검증은 정확성만 보장하며 선택률·체감 위력은 사람 플레이테스트 위험으로 남긴다.
- 롤백 단위는 Core shape/방향 전달, Unity 콘텐츠 마이그레이션, 문서 갱신을 포함한 한 커밋이다.
