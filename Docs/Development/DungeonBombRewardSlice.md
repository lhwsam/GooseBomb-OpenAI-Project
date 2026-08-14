# 던전 첫 폭탄 보상 작업 계약

- 상태: `Implemented / WebGL Verified`
- 규칙 소유: `BombSwap.Core`
- 런 수명 소유: `BombSwap.Unity`
- 직렬화 소유: `BombSwap.Authoring`
- 관련: [폭탄 슬롯과 쿨타임](../Systems/WeaponSlotsAndCooldown.md), [던전 씬 수명](../ADR/0008-Dungeon-Scene-Lifetime.md)

## 플레이어 계약

- 새 던전 run은 `prototype-cross` 한 종류만 1번 슬롯에 장착하고 2번 슬롯은 비운 채 시작한다.
- 빈 2번 슬롯에서는 `X` 교체가 실패하고 HUD는 `EMPTY — FIND A BOMB`과 `SWAP LOCKED`를 표시한다.
- 첫 전투를 클리어해 `BombReward` 방에 들어가면 두 후보가 보행 가능한 논리 셀에 나타난다.
- 왼쪽 `(-1, 0)`의 `prototype-area` 또는 오른쪽 `(1, 0)`의 `prototype-long-cross` 위로 이동하면 해당 후보가 빈 2번 슬롯에 장착된다.
- 선택은 한 run에서 한 번만 성공한다. 선택 결과는 방 로컬 씬이 아니라 지속 run 상태에 저장되어 전투방 왕복과 `LoadSceneMode.Single` 뒤에도 유지된다.
- 선택 뒤에는 기존 `X` 교체와 슬롯별 독립 설치 쿨타임 계약을 그대로 사용한다.

## 구현 경계

1. `BombWeaponLoadout`은 비어 있는 2번 슬롯 snapshot, 빈 슬롯 교체 차단, 한 번의 `TryEquipSecondSlot`을 소유한다.
2. `DungeonBombLoadoutState`는 첫 폭탄, 복사된 2~3개의 고유 후보 ID와 단일 선택 결과를 Unity 참조 없이 소유한다.
3. `PrototypeBombRewardCatalogAsset`은 첫 폭탄·후보 asset·교체 쿨타임을 검증하고 Core run 상태로 변환한다.
4. `PrototypeDungeonRunSession`은 `BombReward` 방에서만 후보 선택을 허용하고 결과를 씬 전환보다 긴 run 수명으로 보존한다.
5. 각 `PrototypeDungeonRoomBinder`는 pending 대상 방을 준비할 때 run loadout을 방 로컬 `PrototypeGameSession`에 주입한다. scene commit 전에는 host의 이전 현재 방이 아니라 binder가 준비한 대상 방 ID·타입을 사용한다.
6. `PrototypeBombRewardPresenter`는 장치 입력을 직접 읽지 않는다. 기존 Core 이동의 `PlayerMoved` 셀 사건으로 후보 접촉을 판정하고, 선택 성공 뒤에만 다른 후보를 숨기고 HUD가 갱신된다.
7. `PrototypeBombPresenter`는 빈 슬롯을 미리 풀링하지 않고 선택된 정의가 실제 설치될 때 필요한 visual pool을 지연 생성한다.

## 현재 저작값

| 역할 | ID | 모양 | fuse | 범위 | 설치 쿨타임 | 표현 |
|---|---|---|---:|---:|---:|---|
| 시작 폭탄 | `prototype-cross` | `Cross` | 2.0초 | 2 | 1.5초 | 검정 구체·주황 폭발 |
| 왼쪽 후보 | `prototype-area` | `SquareArea` | 1.75초 | 1(3×3) | 2.5초 | 보라 원통·자홍 폭발 |
| 오른쪽 후보 | `prototype-long-cross` | `Cross` | 2.25초 | 3 | 2.25초 | 청록 캡슐·청록 폭발 |

교체 쿨타임은 2초다. 모든 수치는 `Proposed`다. `prototype-long-cross`는 선택 UI와 장거리 위험 비교를 위한 현재 후보이며, GDD의 방향성 직선 폭탄을 완성한 것으로 간주하지 않는다.

## 검증과 증거

- Core 대상 EditMode 14/14: 빈 snapshot, 교체 차단, 단일 장착, 후보 수·중복·복사·단일 선택.
- 던전 대상 PlayMode 17/17: catalog, 보상방 한정 선택, 실제 `DungeonReward` 씬의 빈 슬롯→수집→다음 scene 유지, 기존 host·문·재입장 회귀.
- 최종 전체 EditMode 251/251과 PlayMode 95/95가 통과했다. 증거는 `Artifacts/Verification/ConnectedTests/20260814-151258-803.json`, `Artifacts/Verification/ConnectedTests/20260814-151310-363.json`에 있다.
- Development WebGL 8개 씬 빌드와 Edge headless에서 시작 폭탄만으로 첫 전투 클리어, 왼쪽 광역 후보 수집, 클리어 방 왕복, 다음 전투방 `X` 교체·광역 설치, Console/page error 0을 확인했다. 증거는 `Artifacts/Verification/20260815-000500-bomb-reward-web/`에 있다.

## 비목표와 후속

- 이미 찬 슬롯을 새 폭탄으로 교체하거나 버린 폭탄을 방 바닥에 남기는 일반 획득 규칙.
- 후보 재굴림, 확률·seed 기반 후보 구성, 여러 보상방.
- 다음 층, 저장·불러오기, 브라우저 새로고침을 넘는 loadout persistence.
- 방향을 저장하는 실제 전방 직선 폭탄과 추가 폭발 resolver.
- 후보의 재미·가독성 자동 판정. 두 후보가 실제로 다른 선택을 만드는지는 사람 플레이테스트가 필요하다.
