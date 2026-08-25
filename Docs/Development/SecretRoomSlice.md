# 금이 간 비밀문 비밀방 수직 슬라이스

- 상태: 설계·구현·자동 검증 `Accepted`, 사람 발견성 검증 `Proposed`
- 설계 근거: [GDD v0.2](../GameDesign/GDD_v0.2.md) 20.5·21.2·22.6·37장
- 소유 계약: [던전 생성](../Systems/DungeonGeneration.md), [방 저작](../Systems/RoomAuthoring.md), [폭탄과 폭발](../Systems/BombAndExplosion.md)

## 목표와 플레이어 계약

기존 그래프가 배치된 뒤 일반 전투방 2~3개와 맞닿는 빈 좌표가 있으면 비밀방 하나를 추가한다. 연결된 일반방의 해당 출구는 일반 문과 같은 위치의 금이 간 비밀문으로 보이며, 어떤 폭탄 종류든 문 앞 저작 출구 셀까지 폭발을 닿게 하면 해당 입구가 열린다.

- 미공개 비밀방과 연결은 미니맵에 나타나지 않는다.
- 비밀문은 `GridTerrain.DestructibleWall`이 아니다. 문 앞 저작 출구 셀은 `Floor`로 유지하고, 미공개 이동 차단은 `DungeonRoomExitStatus.SecretWall` 경계 상태가 소유한다.
- 폭발의 `AffectedCells`에 문 앞 저작 출구 셀이 포함되면 문 면까지 폭발이 도달한 것으로 판정한다. 비밀문은 `DestroyedWalls`에 포함되지 않고 일반 파괴벽 전파 규칙도 바꾸지 않는다.
- 금이 간 surface는 대응 일반 door renderer와 같은 위치에 둔다. 미공개 상태에서는 일반 문을 숨기고 금 간 문만 표시하며, 공개 뒤에는 금 간 문을 숨기고 기존 열린 문을 다시 표시한다.
- 한 입구를 파괴하면 그 연결만 공개·통행 가능해진다. 다른 인접 입구는 각각 별도로 파괴해야 한다.
- 공개된 비밀방은 미니맵 frontier로 나타나며, 입장 뒤 방문방으로 표시된다.
- 비밀방은 적·클리어 잠금이 없는 안전방이다.
- 중앙 cache는 접근을 막는 논리 `Interactable`이며 cardinal 인접 셀에서 `E`/게임패드 North로 상호작용하면 한 run에서 한 번만 `ROOM TOKENS +3`을 지급하고 blocker와 월드 표현을 제거한다. 일반 전투 `+1`보다 높은 발견 보상이지만 최종 재화 가치·사용처를 확정하지 않는 `Proposed` 임시 점수다.
- 같은 seed의 새 run에서는 비밀벽 공개와 cache 소비가 모두 초기화된다.

## 생성·상태 계약

```text
기존 normal tree 생성
        │ 빈 좌표 후보 수집
        │ Combat 인접 3개 우선, 다음 2개
        │ Boss 직접 인접 제외
        ▼
Secret node + 2~3 Secret connections
        │ 각 연결별 hidden/revealed run state
        ▼
room binder exit-cell → secret-door edge map
        │ Core explosion.AffectedCells
        ▼
connection reveal → door/minimap refresh → scene travel
        ▼
DungeonSecret central cache → run token state
```

- 생성은 명시 seed와 기존 방 좌표만 읽고 시간·Unity Random을 사용하지 않는다.
- 후보 동률은 인접 일반방 수 내림차순, X 오름차순, Z 오름차순으로 고정한다.
- 후보가 없으면 비밀방을 만들지 않으며 무한 재시도하지 않는다.
- normal connection만 보면 기존 필수 던전은 여전히 tree이고 보스 필수 경로·Recovery leaf가 유지된다.
- Secret connection은 `Secret ↔ Combat`만 허용하며 Secret은 2~3개의 연결을 가진다.
- 그래프 기하의 모든 cardinal 인접은 normal 또는 Secret connection으로 명시되어야 한다.
- 공개 상태와 cache 소비는 `DungeonRunState`가 소유한다. Transform, door renderer, scene과 ScriptableObject는 mutable run 원본이 아니다.

## 범위와 비목표

- 변경 허용: `Assets/Game/Core`, first-party Runtime/Presentation/Authoring/Editor, 관련 테스트·WebGL probe·문서, Unity Editor로 생성하는 `DungeonSecret` scene과 catalog/Build Settings.
- 변경 금지: vendor assets, 패키지, ProjectSettings의 사용자 변경, 기존 폭탄 수치와 전투방 geometry.
- 비목표: 패시브 아이템, 상자 등급, 폭탄 교체 드롭, 상점·재화 소비처, 여러 비밀방, 단서 없는 랜덤 벽 검사, 최종 아트·오디오.

## 완료 조건

- EditMode: seed 재현, 후보 우선순위·없음, normal tree 보존, 2~3 Secret 연결, 개별 reveal·travel 차단/허용, minimap 숨김/공개, cache `+3` 단일 소비·terminal 거부·새 run 초기화.
- PlayMode: 실제 전투방의 문 앞 출구 셀이 `Floor`이고 접근·폭탄 설치가 가능하지만 미공개 경계 이동은 차단된다. 그 셀이 실제 폭발 영향에 포함되면 `DestroyedWalls` 없이 해당 문·미니맵만 갱신한다. `DungeonSecret`은 적 없이 입장·왕복 가능하고 중앙 cache 셀은 이동·폭탄을 막는다. 인접 `E` 상호작용 뒤 HUD를 `+3` 갱신하고 blocker를 제거하며 재입장에서 재지급하지 않는다.
- Content: special catalog에 `Secret`, 11번째 enabled scene, 네 방향 secret door root와 대응 일반 문의 위치 일치·Collider 부재·surface 1개·crack bar 3개, 중앙 cache material/reference와 단일 presenter를 validator가 확인한다.
- WebGL: seed 0에서 비밀벽이 미니맵에 숨겨지고, 폭발로 공개·입장·cache 획득·다른 입구 개방 또는 원래 입구 왕복 뒤 기존 전체 경로와 Console/page error 0을 확인한다.
- 사람 검증: 금이 간 벽이 설명 없이 폭파 가능한 단서로 읽히는지, cache가 탐색 비용에 비해 충분한지, 모든 벽을 검사하는 노동을 유발하지 않는지 관찰한다.

## 위험과 롤백

- `RoomType.Secret`과 connection kind는 Core 그래프 계약 변경이므로 생성 버전을 올리고 golden snapshot을 명시 갱신한다.
- scene·catalog·Build Settings는 builder로 생성하고 validator로 재검증한다.
- 롤백 단위는 Secret graph/connection state, 출구 셀→문 경계 adapter, door/minimap/cache 표현, `DungeonSecret` 콘텐츠와 해당 테스트·문서다. 기존 normal tree와 파괴벽 resolver는 유지한다.

## 완료 근거

- Core 생성 버전 `prototype-secret-v3`와 seed 0 golden을 갱신했고, 512개 seed에서 normal tree 보존·후보 우선순위·Secret의 Combat 2~3연결을 검증했다.
- 비밀문 경계 전환 트리에서 연결 Unity EditMode `311/311`, PlayMode `128/128`이 통과했다. PlayMode는 문 앞 `Floor` 셀 접근, 미공개 경계 이동 차단, 해당 셀 폭발 영향, `DestroyedWalls` 비포함, 공개 뒤 비밀방 왕복과 cache 단일 지급을 함께 검증한다.
- `PrototypeContentValidator`는 여섯 special catalog entry, `DungeonSecret`, 네 방향 secret door root와 대응 일반 문의 위치 일치, Collider 없는 surface 1개·crack bar 3개, `DestructibleWall.mat`, `SecretCrack.mat`, `SecretReward.mat`, 단일 cache presenter와 enabled scene 11개를 오류 0으로 확인했다.
- `Artifacts/Verification/20260818-182656-secret-door-boundary-web/`의 11씬 Development WebGL 빌드는 138,263,355 bytes·124.752초·오류 0으로 성공했다. Edge 키보드 `40/40`, 가상 Gamepad `14/14`, 1,161개 플레이테스트 사건 분석과 두 실행의 Console/page error 0이 통과했다. 실제 십자 폭탄의 서쪽 비밀문 공개·미니맵 갱신·Secret 입장·cache·양방향 복귀와 전체 경로를 확인했다.
- 캡처 `webgl-dungeon-secret-wall.png`에서 금 간 문이 서쪽 일반 문과 같은 외벽 경계 위치에 있고 플레이어가 문 앞 출구 셀까지 접근했음을 확인했다. 이전 runtime 파괴벽 정렬 증거는 `Artifacts/Verification/20260817-051928-secret-wall-alignment-web/`에 마이그레이션 근거로 보존한다.
- commit `134dd06`의 post-commit Development WebGL 빌드와 Edge 자동 플레이는 금 간 벽 파괴→`secret-wall-revealed-room-2-direction-west`→10번 비밀방→`secret-reward-collected-3`→`room-reward-tokens-4`→원래 입구 복귀 뒤 기존 보스 완료·실패·재시작까지 통과했다. 키보드 `38/38`, 가상 Gamepad `14/14`, Console/page error 0이다. 증거는 `Artifacts/Verification/20260816-060528-web-postcommit/`에 있다.
- 캡처 `webgl-dungeon-secret-wall.png`와 `webgl-dungeon-secret-room.png`에서 파괴 전 금 간 서쪽 벽, 중앙 cache, 비밀방의 아직 숨겨진 다른 출구를 확인했다. 가독성·탐색 보상·무작위 벽 검사 유발 여부는 자동 검증으로 판정하지 않는다.
