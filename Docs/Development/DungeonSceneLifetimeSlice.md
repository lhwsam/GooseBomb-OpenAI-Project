# 던전 씬 수명·입장 작업 계약

- 상태: `In Progress`
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

현재 1~2번, 3번의 catalog 스키마와 4번의 host·pending 전환은 구현됐다. 실제 special catalog asset 생성, room-local binder, 문 presenter와 씬 저작은 3~6번의 남은 Editor 연결 범위다.

## placeholder 범위

- 시작방은 현재 두 슬롯 loadout을 임시로 유지해 이동·설치·자기 폭발을 확인할 수 있게 한다. 첫 보상에서 실제 두 번째 슬롯 선택으로 바꾸는 작업은 후속이다.
- 폭탄 보상방은 안전 이동과 그래프 왕복만 제공하고 보상 선택 UI는 후속이다.
- 보스 전실은 안전 이동과 보스 방향 표시만 제공한다.
- 보스방은 전환·잠금 검증용 placeholder이며 실제 보스 규칙과 승리는 후속 [보스 전투 문서](../Systems/BossBattle.md)가 소유한다.
- placeholder geometry는 검증된 네 방향 방 shell을 재사용할 수 있지만 전투방 콘텐츠로 배정하지 않는다.

## 검증

- 적 비활성 room session의 초기화, 이동·폭탄과 적/클리어 상태.
- 0/90/180/270도에서 wall·spawn·exit 회전과 입력의 세계 방향 일치.
- host 중복 제거, pending 단일성, 로드 전 Core 미변경, 로드 뒤 단일 commit, 잘못된 씬 실패.
- 시작→첫 전투 잠금→클리어→보상 placeholder→다음 전투와 이전 방 왕복.
- 비활성·잠금·개방 문 시각/논리 일치와 입장 직후 역방향 재전환 방지.
- 전체 EditMode·PlayMode, content validator, 실제 WebGL build와 browser 방향키/focus/Console smoke.

## 비목표

- 저장·불러오기, 비동기 로딩 화면, additive streaming.
- 실제 폭탄 보상 선택·버린 무기 persistence.
- 실제 보스 AI·승리·다음 층.
- 완성 미니맵과 방별 전투 상태 저장.
