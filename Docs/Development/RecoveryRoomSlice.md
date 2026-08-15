# GDD 기반 회복방 수직 슬라이스

- 상태: `Implemented` — 자동 검증 완료, 회복량·발견성·우회 가치는 사람 플레이테스트 대기
- 결정 근거: [GDD v0.2](../GameDesign/GDD_v0.2.md) 4.1, 20.4, 21.2, 37장과 [PT-20260815-02](../Playtesting/Results/PT-20260815-02.md)
- 소유 계약: [피해와 무적 시간](../Systems/DamageAndInvulnerability.md), [던전 생성](../Systems/DungeonGeneration.md), [방 저작](../Systems/RoomAuthoring.md)
- 선행 작업: [활성 폭탄 슬롯 run persistence](ActiveBombSlotPersistenceSlice.md)

## 목표

방을 이동할 때 플레이어 체력을 자동으로 전부 회복하지 않는다. 현재 체력을 한 run 동안 유지하고, 플레이어가 보스 근처의 선택형 막다른 가지를 우회해 회복 자원을 얻을지 결정하게 한다.

## 채택한 GDD 계약

- 회복방은 체력을 회복하는 선택형 우회 방이다.
- 후반부 가지 또는 보스와 가까운 막다른 길에 배치한다.
- 보스방으로 가는 필수 경로에는 배치하지 않는다.
- 플레이어는 회복을 위해 우회할지 바로 보스에게 갈지 선택한다.
- 프로토타입에서는 회복방을 `시간이 남을 때` 범위로 분류하지만, 사용자 플레이 피드백에 따라 현재 후속 수직 슬라이스로 승격한다.
- 적 처치 확률 체력 드롭은 이번 범위에 넣지 않는다. 회복 수단을 동시에 둘 이상 추가해 선택의 원인을 흐리지 않는다.

## GDD에 없는 수치와 잠정안

GDD는 회복량, 사용 횟수, 이미 최대 체력일 때의 동작을 정하지 않는다. 이 값은 확정 계약으로 위장하지 않고 저작 데이터의 `Proposed` 튜닝 값으로 둔다.

- 현재 구현: 한 번만 획득 가능한 회복 아이템, 현재 체력을 최대 체력 이하에서 `+2` 회복.
- 최대 체력이면 소비하지 않아 피해를 받은 뒤 다시 방문할 수 있다.
- `+2`와 1회 사용은 `Proposed`다. 이후 플레이테스트에서 우회 비용과 보스 준비 가치로 조정한다.

## 상태 소유와 데이터 흐름

```text
PlayerHealthSimulation (현재 방의 피해·무적 판정)
        │ 적용된 피해/회복 snapshot
        ▼
run 수명 PlayerHealthState (현재 HP·최대 HP)
        │ 다음 방 초기 HP
        ▼
새 PrototypeGameSession + HUD

Recovery room item interaction
        │ Core 회복 명령
        └──────────────► 두 상태와 HUD를 같은 확정 결과로 갱신
```

- run 수명 Core 상태가 현재/최대 체력을 권위 원본으로 소유한다.
- 방 로컬 `PlayerHealthSimulation`은 run snapshot의 현재 체력으로 시작한다. 무적 종료 시각과 처리한 폭발 ID는 방을 넘어 보존하지 않는다.
- 적용된 피해와 회복만 run 상태에 즉시 반영한다. 표현과 HUD는 별도 체력을 만들지 않는다.
- 새 run은 검증된 `PrototypePlayerVitalsAsset`의 최대 체력으로 시작한다.
- 회복 아이템의 사용 여부는 해당 회복방 노드의 run 수명 상태다. 방 재진입이나 scene 재로드로 다시 생성되지 않는다.
- 체력이 0인 terminal run은 회복할 수 없다.

## 던전 그래프 계약

- 기존 `RoomType` 직렬화 값을 보존하고 새 `Recovery` 값을 끝에 추가한다.
- 프로토타입 그래프에는 회복방 하나를 보스 주 경로의 마지막 일반 전투방에 연결된 막다른 leaf로 추가하는 안을 사용한다. 이 위치는 보스 전실 직전의 선택형 우회이며 필수 보스 경로에는 포함되지 않는다.
- 생성 알고리즘과 seed 결과가 바뀌므로 generation version을 올리고 seed-0 golden snapshot을 명시적으로 갱신한다.
- 회복방은 안전방이며 클리어·전투 보상 토큰·문 잠금의 대상이 아니다.
- 특수방 카탈로그가 `Recovery` scene을 정확히 하나 해석하고, 모든 seed에서 로드 가능한 콘텐츠가 있음을 validator가 확인한다.

## 단계별 변경 범위

### 1. 체력 persistence

- `Complete`: 초기 현재 체력을 받을 수 있도록 Core 플레이어 체력 생성 계약을 확장했다.
- `Complete`: `DungeonPlayerHealthState`에 현재 체력을 저장하고 적용된 피해 뒤 즉시 동기화한다.
- `Complete`: 다음 방과 이전 방 재방문에서 같은 체력을 session·HUD에 복원한다. 동일 binder 경로를 사용하는 보스방도 같은 계약을 따른다.
- `Complete`: 완료·실패 뒤 새 run만 최대 체력으로 초기화한다.

### 2. 회복 규칙과 그래프

- `Complete`: Core에 상한을 지키는 회복 결과와 회복방 노드별 일회성 소비 상태를 추가했다.
- `Complete`: 기존 직렬화 값을 유지한 `RoomType.Recovery = 5`와 선택형 leaf 배치를 추가하고 generation version을 `prototype-tree-v2`로 올렸다.
- `Complete`: 최대 체력·이미 사용·다른 방·사망·terminal 상태의 거부는 체력과 소비 상태를 바꾸지 않는다.

### 3. Unity 콘텐츠와 표현

- `Complete`: Unity Editor builder로 `DungeonRecovery` scene, special catalog entry, 중앙 논리 셀 `(0,0)`의 회복 pickup과 URP shared material을 저작했다.
- `Complete`: 회복 가능·최대 체력·사용 완료 상태를 pickup 형태와 `RECOVERY +2`·`HEALTH FULL`·`RECOVERY USED` 문구로 구분한다.
- `Complete`: 논리 셀 진입만 획득을 일으키며 기존 체력 HUD를 즉시 갱신하고, room 재진입에서는 Core 소비 상태를 복원한다.

## 비목표

- 적 처치 확률 체력 드롭, 상점 회복, 최대 체력 증가, 층 사이 저장/불러오기.
- 회복 아이템의 범용 인벤토리화 또는 여러 종류의 회복 아이템.
- 미니맵, 보스 이동, 슬롯별 쿨타임 persistence.
- 기존 자기 폭발·접촉·보스 피해량과 무적 시간 재조정.

## 실패 테스트와 완료 조건

### EditMode

- 새 run은 최대 체력으로 시작하고 유효 범위 밖 초기 체력을 거부한다.
- 피해 뒤 현재 체력이 run 상태에 유지되며 방 전환 자체는 체력을 바꾸지 않는다.
- 회복은 최대 체력을 넘지 않고 실제 회복량을 결과로 반환한다.
- 최대 체력, 이미 사용한 회복방, 사망/terminal run의 회복 요청은 상태를 바꾸지 않는다.
- 동일 seed는 동일한 회복방 leaf를 만들고 회복방은 필수 보스 경로에 포함되지 않는다.
- generation version과 seed-0 golden snapshot 변경이 명시적이다.

### PlayMode

- 피해를 받은 뒤 다음 방과 이전 방으로 이동해도 같은 체력이 HUD와 session에 표시된다.
- 회복방 입장과 재입장은 자동 회복하지 않는다.
- 유효한 상호작용 한 번만 체력과 HUD를 갱신하고, 재입장해도 아이템이 복원되지 않는다.
- 최대 체력에서의 비소비 동작과 회복 뒤 보스방 진입을 검증한다.
- 회복방은 적 actor 없이 열려 있고 전투 보상 토큰을 지급하지 않는다.

### WebGL와 사람 검증

- `Complete`: seed-0 전체 경로에서 피해→방 전환 체력 유지→8번 회복방 우회→HP `1→3` 회복→5번 전투방 복귀→보스 진입을 검증했다.
- `Complete`: 키보드 전체 경로와 가상 표준 게임패드 회귀가 통과했고 두 실행 모두 Console/page error가 0이다.
- 사람 플레이에서 회복방 위치를 발견할 수 있는지, 우회 비용이 과하지 않은지, 회복 뒤 바로 보스로 갈지 탐색을 계속할지 기록한다.

## 검증 근거

- StaticOnly: `Artifacts/Verification/20260816-020042-static/summary.json`.
- 연결된 Unity EditMode 297/297: `Artifacts/Verification/ConnectedTests/20260815-173821-473.json`.
- 연결된 Unity PlayMode 123/123: `Artifacts/Verification/ConnectedTests/20260815-173843-980.json`.
- Content Validator: 회복 scene·catalog·presenter·material·Build Settings 포함 오류 0.
- 10씬 Development WebGL: `Artifacts/Verification/20260816-023939-connected-web/webgl-build-report.json`, 137,804,630 bytes, 113.821초, 오류 0.
- 키보드 전체 경로와 회복 `1→3`: `Artifacts/Verification/20260816-023939-connected-web/browser-smoke.json`.
- 가상 표준 게임패드 13개 회귀: `Artifacts/Verification/20260816-023939-connected-web/gamepad-smoke.json`.
- 최종 회복방 시각 증거: `Artifacts/Verification/20260816-023939-connected-web/webgl-dungeon-recovery-room.png`.

## 문서·마이그레이션·롤백

- 동작 완료 시 `DamageAndInvulnerability.md`, `DungeonGeneration.md`, `RoomAuthoring.md`, `CurrentState.md`를 함께 갱신한다.
- `RoomType`은 기존 숫자를 유지하고 끝에만 추가한다. 특수방 카탈로그 schema 변경은 Unity 직렬화 호환성을 검토한다.
- scene·ScriptableObject 변경은 Unity Editor로만 수행하고 저장 뒤 validator로 다시 읽는다.
- 롤백 단위는 run 체력 상태, 회복 규칙, `Recovery` graph node와 콘텐츠, 관련 테스트·문서다.
