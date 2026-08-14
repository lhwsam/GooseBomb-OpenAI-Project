# 던전 씬 수명·입장 작업 계약

- 상태: `Implemented / WebGL Traversal Verified`
- 수명 소유: `BombSwap.Unity`
- 직렬화 소유: `BombSwap.Authoring`
- 선행 결정: [ADR-0008](../ADR/0008-Dungeon-Scene-Lifetime.md)

## 플레이어 계약

- 새 run은 적이 없는 시작방에서 시작한다.
- 열린 문으로 나가면 그래프의 해당 방향 이웃 방으로 이동한다.
- 전투방 입장 시 문이 잠기고 모든 적 처치 후 열린다.
- 클리어한 전투방은 다시 입장해도 적과 문 잠금을 재생성하지 않는다.
- 이전 방으로 돌아갈 수 있고 방의 그래프 방향과 화면의 문 방향은 일치한다.
- 새 방에서는 들어온 문 경계 셀에서 시작하며, 이동키를 계속 누르고 있어도 방 안쪽으로 진행한다.

## 구현 단위

1. 전투가 없는 placeholder에서도 플레이어 이동·폭탄·체력을 재사용할 수 있도록 `PrototypeGameSession`의 적 활성 여부를 명시적으로 구성한다.
2. room assignment 회전을 모든 저작 셀과 scene `GridRoot`에 같은 방식으로 적용하고 session 초기화 전에 입장 spawn을 덮어쓴다.
3. 전투방 catalog와 별도로 필수 특수방 타입→씬 이름 catalog를 저작·검증한다.
4. persistent run host와 room-local binder를 구현해 pending scene transition을 [ADR-0008](../ADR/0008-Dungeon-Scene-Lifetime.md) 순서로 처리한다.
5. 네 방향 외곽 벽을 문 폭만큼 분할하고 `Inactive`·`Locked`·`Open` presenter와 출구 감지를 연결한다.
6. 시작방·폭탄 보상방·보스 전실·보스방 placeholder 씬을 Unity Editor builder로 만들고 Build Settings에 포함한다.
7. room binder가 Core의 클리어 상태에서 방문별 전투 활성 여부를 파생해, 첫 입장만 적을 생성하고 클리어 뒤 재입장에서는 적 simulation·표현과 문 잠금을 모두 생략한다.

1~7번 구현은 완료됐다. 실제 special catalog asset과 네 placeholder 씬, persistent host, room-local binder, 회전 문 presenter, 출구 감지, Build Settings 첫 Start 씬을 Editor builder·validator가 소유한다. 전투 가능 여부는 scene 저작 설정이고, 방문별 활성 여부는 `DungeonRunState.IsCleared`에서 파생하므로 별도의 Unity 전역 상태를 만들지 않는다.

## 특수방 범위

- 시작방은 `prototype-cross` 한 슬롯으로 시작하고 빈 2번 슬롯을 표시한다.
- 폭탄 보상방은 보행 가능한 두 논리 셀에 `prototype-area`와 `prototype-long-cross` 후보를 표시하며, 플레이어가 올라선 후보를 빈 2번 슬롯에 한 번 장착한다. 상세 계약은 [첫 폭탄 보상 수직 슬라이스](DungeonBombRewardSlice.md)가 소유한다.
- 보스 전실은 안전 이동과 보스 방향 표시만 제공한다.
- 보스방은 전환·잠금 검증용 placeholder이며 실제 보스 규칙과 승리는 후속 [보스 전투 문서](../Systems/BossBattle.md)가 소유한다.
- placeholder geometry는 검증된 네 방향 방 shell을 재사용할 수 있지만 전투방 콘텐츠로 배정하지 않는다.

## 검증

- 적 비활성 room session의 초기화, 이동·폭탄과 적/클리어 상태.
- 0/90/180/270도에서 wall·spawn·exit 회전과 입력의 세계 방향 일치.
- host 중복 제거, pending 단일성, 로드 전 Core 미변경, 로드 뒤 단일 commit, 잘못된 씬 실패.
- 시작→첫 전투 잠금→클리어→보상 선택→다음 전투와 이전 방 왕복.
- 비활성·잠금·개방 문 시각/논리 일치와 입장 직후 역방향 재전환 방지.
- 전체 EditMode·PlayMode, content validator, 실제 WebGL build와 browser 방향키/focus/Console smoke.

현재 증거:

- 첫 보상 연결 전 기준 전체 EditMode 244/244, PlayMode 91/91 통과. 최신 전체 수치는 [현재 상태](CurrentState.md)를 따른다.
- content validator 오류 0과 builder 2회차 멱등 동기화 통과.
- Editor Play Mode에서 `DungeonStart` primary host·안전 session·열린 문을 확인하고, 서쪽 출구→`TestSandboxPillars` 로드·Core commit·90도 회전 입장 셀·잠긴 문을 확인.
- 실제 `DungeonStart`와 seed-0 첫 전투 씬을 로드하는 PlayMode 회귀에서 첫 입장의 적 생성·문 잠금, Core 클리어 뒤 시작방 왕복, 재입장의 적 0·미생성 presenter·열린 문을 확인.
- Development WebGL 8개 씬 빌드 오류 0. Playwright 한 세션에서 canvas focus, Start 안전방 경로, 시작 십자 폭탄만으로 첫 전투 클리어, 보상방 후보 수집, 클리어 전투방 역방향 재입장 시 적 미생성, 나머지 주 경로 전투방 2개 클리어, 보스 전실과 보스 placeholder 진입을 확인했다.
- WebGL build, 8번의 graph transition/commit, 주 경로 전투 3개 클리어, 보상 선택과 보스방까지의 loadout 유지, pause/resume, resize와 browser Console/page error 0 증거는 `Artifacts/Verification/20260815-003200-full-boss-path-web/`에 남겼다.

## 비목표

- 저장·불러오기, 비동기 로딩 화면, additive streaming.
- 두 슬롯이 모두 찬 뒤 교체할 무기 선택과 버린 무기의 room-local persistence.
- 실제 보스 AI·승리·다음 층.
- 플레이어 체력, 파괴 가능 벽과 적의 부분 체력 같은 room-local 세부 상태의 방 전환 persistence. 선택한 두 슬롯 loadout의 run persistence는 구현됐다.
- 완성 미니맵.
