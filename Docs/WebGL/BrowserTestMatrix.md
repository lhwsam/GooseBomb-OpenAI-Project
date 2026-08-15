# WebGL 브라우저 테스트 매트릭스

- 상태: `Proposed`; 실제 지원 범위는 첫 배포 전에 확정

## 우선 환경

| 우선순위 | 환경 | 목적 |
|---|---|---|
| P0 | Windows 최신 Chrome 계열 | 주 개발/배포 기준 |
| P0 | Windows 최신 Edge | Chromium 차이와 배포 사용자 확인; 현재 자동 회귀 환경 |
| P1 | macOS 최신 Safari | WebKit 호환성과 오디오/입력 확인 |
| P1 | macOS 최신 Chrome | 플랫폼과 브라우저 영향 분리 |
| P2 | Firefox 최신 | 별도 엔진 호환 확인 |

모바일 브라우저 지원은 현재 프로토타입 입력 기준 밖이며 별도 결정 전 완료 조건에 포함하지 않는다.

## smoke 항목

- cold/warm load와 진행 표시.
- canvas click/focus 전후 키보드 입력.
- WASD/방향키, 게임패드 사용 시 매핑. Input System 합성 Gamepad의 왼쪽 스틱·D-pad·South/West/Start/Select→의미 명령은 PlayMode에서 검증한다. 별도 WebGL smoke는 `navigator.getGamepads()`의 표준 가상 장치 연결부터 스틱·D-pad 해제, 유지 스틱 중 분리의 즉시 정지·300ms 위치 안정성과 동일 index 재연결 입력 복구, South 설치·자기폭발 실패, West 교체 명령, Start pause 중 유지 스틱·South 차단과 재개 뒤 유지 스틱 재적용, Select의 실패 런 재시작까지 검증한다. 실제 물리 컨트롤러 연결·장치별 버튼 표기·deadzone·브라우저/OS별 Gamepad API 차이는 수동 항목으로 남긴다. 기본 자동 smoke는 seed-0 Start 안전방에서 첫 전투방으로 이동한 뒤 겹친 직교 방향키의 최신 축 우선과 빠른 즉시 press-release 방향 교대가 각 탭마다 한 frame의 실제 motion을 만들고 이후 추가 이동 없이 멈추는지 확인한다. 이어 시작 슬롯의 `Z` 십자 폭탄을 두 번 설치해 첫 전투방을 실제 클리어한다.
- focus 상실/복귀 후 stuck input 없음. 기본 자동 smoke는 오른쪽 키를 누른 채 브라우저 `blur` lifecycle 사건을 발생시키고 `Move(None)` 뒤 셀·motion이 정지하는지 확인한다. 이어 `focus` 복귀 전 key-up이 누락된 상태에서도 이동이 되살아나지 않고 다음 `Esc` 입력이 정상 처리돼야 한다.
- 페이지 스크롤/브라우저 단축키와 충돌 없음.
- 모든 방에서 플레이어 현재/최대 체력과 bar가 좌상단에 읽히고, 현재 런의 `ROOM TOKENS`가 우상단에 보이며, 보스방에서만 보스 현재/최대 체력과 phase가 상단 중앙에 나타나 무기 HUD·전투 공간·pause/완료/실패 overlay와 충돌하지 않는지 캡처로 확인한다.
- 첫 체력 probe run의 시작방 자기 폭발로 `player-health-current-5 → 4`를 만든 뒤 첫 전투방 준비도 `4`인지 확인해 방 전환 전회복을 차단한다. 이어 추격자 접촉 실패와 페이지 reload 없는 `R` 재시작으로 새 run `5`를 확인하고, 그 새 run에서 전체 던전 회귀를 수행한다. 완료 뒤와 자기 폭발 실패 뒤 재시작도 각각 `5`여야 한다.
- 추격자 이동, 차저 예고·돌진·논리 이동, 갑옷 적 첫 피격 상태/속도 변화와 두 번째 피격 사망, 접촉 피해, 논리 이탈, 두 정의의 폭탄 설치, 실제 fuse 자기 폭발, 두 번째 방 대각선 파괴 블록의 광역 동시 파괴, 폭탄 유도 처치·방 클리어, 성공한 Core 교체.
- 안전방에서 `Esc`로 실제 pause에 진입해 `PAUSED` UI를 캡처한다. pause 중 방향키와 `Z`가 논리 셀·frame motion·폭탄 설치를 바꾸지 않는지 확인하고, 두 번째 `Esc` 뒤 같은 세션이 재개되는지 검증한다. `Time.timeScale` 기반 표현 정지가 아니라 세션 논리 시계 차단을 대상으로 한다.
- 기본 smoke에서 Start→첫 전투 클리어→금 간 서쪽 벽 폭파→10번 Secret 입장·cache `+3`·원래 입구 복귀→BombReward 왼쪽 후보 수집·슬롯 2 활성화→클리어 전투방 역방향 재입장→BombReward 재진입→회전 루프·중앙 게이트 전투방 클리어→보스 전실→2페이즈 보스 격파가 같은 브라우저 세션에서 순서대로 진행된다. 첫 전투 `combat-reward-tokens-1`, Secret `room-reward-tokens-4`, 이후 일반 전투의 합계 `combat-reward-tokens-5 → 6`을 요구하고 보스 격파는 값을 늘리지 않아야 한다. 금 간 벽과 cache를 각각 캡처하며 공개 직후 미니맵 `4방/3연결`, Secret 현재 방 10, Recovery `9방/8연결`, 보스 전실 `10방/9연결`을 확인한다. 게이트 방은 `room-ready-prototype-combat-gates`를 요구하고 진입 캡처에서 고정 장벽 2열·중앙 파괴 문 2개·좌우 우회·HUD 비중첩을 확인한다. 클리어 전투방과 이후 전투방·보스방에서는 추가 `X` 없이 `prototype-area`가 계속 설치되어 성공한 활성 슬롯과 보상 loadout이 run 동안 유지됐음을 확인한다. 갑옷 실험 씬은 별도 시작 씬 smoke에서 준비와 2회 피격을 확인한다.
- 보스방 첫 Telegraph는 현재 셀 `(0,1)`과 다음 목적지 `(1,1)` marker, 작은 청록 ghost를 함께 보여야 한다. 광역 폭탄을 Telegraph 중 목적지 영향 범위에 먼저 설치한 뒤 `boss-moved → boss-cell-x-1-z-1 → boss-pattern-recovery → bomb-exploded → boss-damaged`가 이어지고, 이후 `(1,0) → (1,-1) → (0,-1)` 세 목적지와 실제 셀이 일치해야 한다. 정상 경로에서 `boss-move-blocked`는 없어야 한다.
- 방향성 직선 폭탄 전용 smoke는 첫 전투를 클리어하고 BombReward 오른쪽 `prototype-line`을 선택한 뒤 슬롯 2에서 동쪽으로 설치한다. 설치 직후 북쪽 이동 명령을 보내도 `line-bomb-placed-east`와 `line-bomb-exploded-east`가 유지되고, 캡처에서 비대칭 설치체의 방향과 HUD 장착 상태가 읽혀야 한다.
- 보스 격파 뒤 `FLOOR CLEARED` 결과 UI가 표시되고 전투가 멈춘다. 완료 화면을 캡처한 뒤 `R`을 눌러 페이지 reload 없이 새 seed-0 시작방이 준비되고 보상·방 클리어·전투 토큰 상태가 0으로 초기화됐는지 확인한다. 이어 안전방에서 자기 폭발 5회로 실제 사망시켜 `RUN FAILED`와 `CAUSE: BOMB EXPLOSION`을 캡처하고, 다시 `R`을 눌러 세 번째 시작방 준비와 토큰 0까지 확인한다.
- 사용자 입력 뒤 오디오 재생.
- 전체 화면/창 크기 변경 시 화면과 UI. 자동 smoke는 로드 직후 1280×720→640×720, 전체 경로 뒤 1024×768→640×720을 검사한다. canvas는 각각 viewport 안에 완전히 들어오고 문서 overflow가 없으며 960×600 네이티브 상한과 16:10 비율을 유지해야 한다. 실제 fullscreen 진입과 640px 미만 텍스트 가독성은 수동 항목이다.
- 브라우저 Console error와 WebGL context loss.
- 캐시된 이전 버전에서 새 버전 갱신.

개발 빌드 자동 probe는 `probe-ready`, 방 콘텐츠 `room-ready-*`, 그래프 방문 상태 `dungeon-room-ready-<node-id>-<room-type>-<state>`로 각 런타임 준비를 동기화한 뒤 `move`, `move-direction-*`, `move-motion-direction-*`, `move-step-direction-*`, `player-cell-x-*-z-*`, `chaser-cell-x-*-z-*`, `boss-cell-x-*-z-*`, `boss-move-target-x-*-z-*`, `boss-moved`, `boss-move-blocked`, 적 상태·이동, 폭탄 설치·폭발·피해·클리어, `combat-reward-tokens-<count>`, `room-reward-tokens-<count>`, `secret-wall-revealed-room-<id>-direction-<direction>`, `secret-reward-collected-<count>`, `dungeon-transition-started`, `dungeon-room-committed`, `bomb-reward-selected-<definition-id>`, `bomb-exploded-definition-<definition-id>`, `line-bomb-placed-<direction>`, `line-bomb-exploded-<direction>`, `swap-bomb`, `pause-entered`, `pause-resumed`, `pause-resume`, `audio-unlocked`를 제공한다. pause marker는 버튼 callback이 아니라 `PrototypeGameSession.PauseStateChanged`의 확정 전이만 기록한다. `swap-bomb`은 입력 수신이고 `active-bomb-slot-1`은 쿨타임을 통과한 Core 교체다. 명령 probe는 `PlayerCommand.Move`, motion probe는 frame 연속 위치 변화, step·cell probe는 정수 셀 경계 전이와 현재 좌표를 기록한다. 빠른 단타 검사는 key down 뒤 motion을 기다렸다가 release하는 방식이 아니라 down/up을 먼저 연속 전송한 뒤 motion 한 frame과 50ms 정지 안정성을 확인한다. 기본 smoke는 Secret 왕복을 포함한 실제 graph 전환과 각 commit, 주 경로 전투 3개 클리어를 관측한다. 첫 전투의 `active → room-cleared`, 금 간 출구의 실제 폭발 공개, Secret `safe`·cache, 보상 선택, 재입장의 `cleared`와 일정 시간 적 사건 0, 후속 전투 2개의 `active → room-cleared`, 보스 전실 `safe`, 보스 `active`와 목적지 예고·확정 이동·선행 설치 적중을 서로 다른 marker로 구분한다. 갑옷 전용 smoke는 별도 시작 씬에서 `armored-moved`, 첫 폭발의 `armored-broken`, 두 번째 폭발의 `armored-died`와 최종 클리어를 확인한다. 방향성 직선 전용 smoke는 설치 직후 다른 이동 명령을 보낸 전후의 방향 marker를 순서대로 확인한다. 피해·이동·설치·폭발·상태·사망·클리어·보상 표식은 실제 Core 상태 전이만 기록한다. Core 연속 좌표와 Transform 일치는 PlayMode 통합 테스트가 확인한다. `audio-unlocked` marker는 오디오 출력 자체를 증명하지 않으므로 오디오 연결 뒤 수동 항목을 별도로 통과해야 한다.

결과·재시작 구간의 추가 marker는 `run-completed`, `player-died`, `run-failed`, `run-failed-cause-<cause>`, `run-restart-requested`, `dungeon-run-restarted`다. 현재 자동 자폭 경로는 `run-failed-cause-bomb-explosion`을 요구한다. 완료 뒤 두 번째, 실패 뒤 세 번째 `dungeon-room-ready-1-start-safe`까지 관찰해야 통과한다.

## 결과 기록

빌드 식별자, 브라우저/OS 버전, 장치, 해상도, 통과/실패, Console 발췌, 재현 단계, 스크린샷/영상 위치를 남긴다.
