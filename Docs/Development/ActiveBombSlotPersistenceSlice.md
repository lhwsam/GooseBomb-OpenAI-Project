# 활성 폭탄 슬롯 run persistence 수직 슬라이스

- 상태: `Ready`
- 결함 근거: [PT-20260815-02](../Playtesting/Results/PT-20260815-02.md)
- 소유 계약: [폭탄 슬롯, 교체, 쿨타임](../Systems/WeaponSlotsAndCooldown.md), [던전 생성](../Systems/DungeonGeneration.md)

## 목표

플레이어가 2번 폭탄을 활성화한 채 문을 통과하면 다음 방에서도 같은 슬롯이 활성 상태여야 한다. 새 run만 0번 슬롯으로 시작한다.

## 관찰된 결함과 기대 동작

- 관찰: 보상으로 두 번째 폭탄을 장착하고 활성화해도 방 scene이 바뀌면 HUD와 설치 폭탄이 0번 슬롯으로 돌아간다.
- 기대: 성공한 마지막 교체 결과는 run 수명 상태이며, 방 전환은 활성 슬롯을 바꾸지 않는다.
- 심각도: 진행 차단은 아니지만 방마다 다시 교체해야 하고, 교체 쿨타임 때문에 즉시 원하는 폭탄을 쓰지 못해 두 폭탄 선택 리듬을 왜곡한다.

## 원인 증거

1. `DungeonBombLoadoutState`는 첫 슬롯, 선택된 두 번째 슬롯과 후보만 보존하며 활성 슬롯 인덱스가 없다.
2. `PrototypeDungeonRoomBinder`는 새 방의 `PrototypeGameSession`에 두 정의만 전달한다.
3. `PrototypeGameSession`은 방마다 새 `BombWeaponLoadout`을 만들고 생성자의 기본 `activeSlotIndex = 0`을 사용한다.
4. 기존 PlayMode persistence 테스트는 다음 scene의 두 번째 슬롯 정의만 확인하고 `ActiveBombSlotIndex`는 확인하지 않는다.

이 네 조건은 사용자가 관찰한 “방마다 0번으로 복귀”를 직접 설명한다. Unity Editor에서 수정 전 실패 테스트를 실행해 최종 기준선을 남긴 뒤 구현한다.

## 변경 계약

- `DungeonBombLoadoutState`가 현재 활성 슬롯 인덱스를 run 수명으로 소유한다.
- 새 run은 0번 슬롯으로 시작한다.
- 두 번째 슬롯이 비어 있을 때 1번 활성화는 거부한다.
- `PrototypeGameSession`의 성공한 교체만 run 상태를 갱신한다. 실패한 교체는 상태를 바꾸지 않는다.
- 새 방 session은 run 상태의 활성 슬롯으로 `BombWeaponLoadout`을 초기화한다.
- 완료·실패 뒤 새 run은 다시 0번 슬롯으로 시작한다.
- HUD는 복원된 Core snapshot을 표시하며 자체 선택 상태를 만들지 않는다.

## 범위와 비목표

- 변경 허용: `DungeonBombLoadoutState`, `BombWeaponLoadout`의 검증된 초기 활성 슬롯, `PrototypeGameSession` 준비 API, `PrototypeDungeonRoomBinder` 동기화, 관련 EditMode·PlayMode·WebGL 회귀와 문서.
- 변경 금지: 씬·프리팹·ScriptableObject YAML, Input Actions, vendor 에셋.
- 비목표: 슬롯별 설치 쿨타임과 교체 쿨타임의 방 전환 persistence, 플레이어 체력 persistence, 버린 폭탄, 저장/불러오기, 미니맵, 보스 이동.

## 실패 테스트와 완료 조건

### EditMode

- 새 `DungeonBombLoadoutState`의 활성 슬롯은 0이다.
- 두 번째 슬롯 선택 전 1번 활성화는 거부되고 상태는 0으로 유지된다.
- 보상 선택 뒤 1번 활성화가 성공하고 0번으로 다시 바꿀 수 있다.
- 범위 밖 인덱스는 명시적으로 거부한다.

### PlayMode

- 보상방에서 두 번째 슬롯을 장착하고 성공적으로 1번으로 교체한다.
- 다음 combat scene 로드 뒤 `ActiveBombSlotIndex == 1`이고 HUD·다음 설치 정의가 1번과 일치한다.
- 이전 방으로 왕복해도 1번이 유지된다.
- 완료 또는 실패 뒤 새 run의 첫 방은 0번이다.
- 기존 두 번째 정의 persistence, 빈 슬롯 교체 거부와 교체 쿨타임 회귀가 통과한다.

### WebGL

- 보상 선택→1번 활성화→문 전환 뒤 별도 `X` 입력 없이 후속 `Z`가 선택 폭탄 정의를 설치한다.
- 기존 전체 seed-0 경로와 browser Console/page error 0을 유지한다.

## 검증과 롤백

- Core 반복: `./Tools/Verify.ps1 -Tier Fast`
- Unity 통합: 연결된 Editor에서 대상·전체 EditMode/PlayMode와 Console 오류 0
- 입력·scene 수명 영향: `./Tools/Verify.ps1 -Tier Web` 또는 동일 범위의 기존 빌드 재생성·브라우저 스모크
- 롤백 단위는 run 활성 슬롯 필드, 방 session 초기화·동기화, 관련 테스트와 문서다. 직렬화 마이그레이션은 없다.
