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
- WASD/방향키, 게임패드 사용 시 매핑. 기본 자동 smoke는 seed-0 Start 안전방에서 첫 전투방으로 이동한 뒤 겹친 직교 방향키의 최신 축 우선과 빠른 즉시 press-release 방향 교대가 각 탭마다 한 frame의 실제 motion을 만들고 이후 추가 이동 없이 멈추는지 확인한다. 이어 시작 슬롯의 `Z` 십자 폭탄을 두 번 설치해 첫 전투방을 실제 클리어한다.
- focus 상실/복귀 후 stuck input 없음.
- 페이지 스크롤/브라우저 단축키와 충돌 없음.
- 추격자 이동, 차저 예고·돌진·논리 이동, 갑옷 적 첫 피격 상태/속도 변화와 두 번째 피격 사망, 접촉 피해, 논리 이탈, 두 정의의 폭탄 설치, 실제 fuse 자기 폭발, 두 번째 방 대각선 파괴 블록의 광역 동시 파괴, 폭탄 유도 처치·방 클리어, 성공한 Core 교체, pause/resume.
- 기본 smoke에서 Start→첫 전투 클리어→BombReward 왼쪽 후보 수집→클리어 전투방 역방향 재입장→BombReward 재진입→나머지 주 경로 전투방 2개 클리어→보스 전실→2페이즈 보스 격파가 같은 브라우저 세션에서 순서대로 진행된다. 클리어 전투방에서는 적 사건이 다시 발생하지 않아야 하며, 이후 전투방과 보스방에서는 `X` 교체와 `prototype-area` 설치로 보상 loadout이 유지됐음을 확인한다. 갑옷 실험 씬은 별도 시작 씬 smoke에서 준비와 2회 피격을 확인한다.
- 보스 격파 뒤 `FLOOR CLEARED` 결과 UI가 표시되고 전투가 멈춘다. 완료 화면을 캡처한 뒤 `R`을 눌러 페이지 reload 없이 새 seed-0 시작방이 준비되고 보상·방 클리어 상태가 초기화됐는지 확인한다. 이어 안전방에서 자기 폭발 5회로 실제 사망시켜 `RUN FAILED`와 `CAUSE: BOMB EXPLOSION`을 캡처하고, 다시 `R`을 눌러 세 번째 시작방 준비까지 확인한다.
- 사용자 입력 뒤 오디오 재생.
- 전체 화면/창 크기 변경 시 화면과 UI.
- 브라우저 Console error와 WebGL context loss.
- 캐시된 이전 버전에서 새 버전 갱신.

개발 빌드 자동 probe는 `probe-ready`, 방 콘텐츠 `room-ready-*`, 그래프 방문 상태 `dungeon-room-ready-<node-id>-<room-type>-<state>`로 각 런타임 준비를 동기화한 뒤 `move`, `move-direction-*`, `move-motion-direction-*`, `move-step-direction-*`, `player-cell-x-*-z-*`, `chaser-cell-x-*-z-*`, 적 상태·이동, 폭탄 설치·폭발·피해·클리어, `dungeon-transition-started`, `dungeon-room-committed`, `bomb-reward-selected-<definition-id>`, `swap-bomb`, `pause-resume`, `audio-unlocked`를 제공한다. `swap-bomb`은 입력 수신이고 `active-bomb-slot-1`은 쿨타임을 통과한 Core 교체다. 명령 probe는 `PlayerCommand.Move`, motion probe는 frame 연속 위치 변화, step·cell probe는 정수 셀 경계 전이와 현재 좌표를 기록한다. 빠른 단타 검사는 key down 뒤 motion을 기다렸다가 release하는 방식이 아니라 down/up을 먼저 연속 전송한 뒤 motion 한 frame과 50ms 정지 안정성을 확인한다. 기본 smoke는 8번의 실제 graph 전환과 각 commit, 주 경로 전투 3개 클리어를 관측한다. 첫 전투의 `active → room-cleared`, 보상 선택, 재입장의 `cleared`와 일정 시간 적 사건 0, 후속 전투 2개의 `active → room-cleared`, 보스 전실 `safe`, 보스 placeholder `active`와 선택 폭탄 설치를 서로 다른 marker로 구분한다. 갑옷 전용 smoke는 별도 시작 씬에서 `armored-moved`, 첫 폭발의 `armored-broken`, 두 번째 폭발의 `armored-died`와 최종 클리어를 확인한다. 피해·이동·설치·폭발·상태·사망·클리어·보상 표식은 실제 Core 상태 전이만 기록한다. Core 연속 좌표와 Transform 일치는 PlayMode 통합 테스트가 확인한다. `audio-unlocked` marker는 오디오 출력 자체를 증명하지 않으므로 오디오 연결 뒤 수동 항목을 별도로 통과해야 한다.

결과·재시작 구간의 추가 marker는 `run-completed`, `player-died`, `run-failed`, `run-failed-cause-<cause>`, `run-restart-requested`, `dungeon-run-restarted`다. 현재 자동 자폭 경로는 `run-failed-cause-bomb-explosion`을 요구한다. 완료 뒤 두 번째, 실패 뒤 세 번째 `dungeon-room-ready-1-start-safe`까지 관찰해야 통과한다.

## 결과 기록

빌드 식별자, 브라우저/OS 버전, 장치, 해상도, 통과/실패, Console 발췌, 재현 단계, 스크린샷/영상 위치를 남긴다.
