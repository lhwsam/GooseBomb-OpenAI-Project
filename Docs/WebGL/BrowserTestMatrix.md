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
- WASD/방향키, 게임패드 사용 시 매핑. 자동 smoke는 Core 사건을 기준으로 `W` 이동, 첫 `Z` 기본 폭탄 설치, 접촉 확인, `A` 이탈, 첫 폭발, `X` 성공, 두 번째 `Z` 광역 폭탄 설치, `Esc` 두 번과 두 번째 방의 광역 `Z` 유도를 순서대로 전송한다. 마지막 방에서는 `ArrowUp/ArrowRight` 단타를 여섯 번 교대하고 각 release 전에 실제 frame 이동이 `North/East` 같은 순서로 발생하는지 확인한다. 이어 차저의 `Telegraph → Charge → logical move` 사건 순서를 확인하고 서쪽으로 두 셀 이탈한 뒤 정지 `Z`로 차저 접촉 무적과 분리된 자기 폭발 피해를 확인한다.
- focus 상실/복귀 후 stuck input 없음.
- 페이지 스크롤/브라우저 단축키와 충돌 없음.
- 추격자 이동, 차저 예고·돌진·논리 이동, 갑옷 적 첫 피격 상태/속도 변화와 두 번째 피격 사망, 접촉 피해, 논리 이탈, 두 정의의 폭탄 설치, 실제 fuse 자기 폭발, 두 번째 방 대각선 파괴 블록의 광역 동시 파괴, 폭탄 유도 처치·방 클리어, 성공한 Core 교체, pause/resume.
- 기본 smoke에서 중앙 루프→평행 통로→엇갈린 기둥이 같은 브라우저 세션에서 순서대로 준비됨. 갑옷 실험 씬은 별도 시작 씬 smoke에서 준비와 2회 피격을 확인한다.
- 사용자 입력 뒤 오디오 재생.
- 전체 화면/창 크기 변경 시 화면과 UI.
- 브라우저 Console error와 WebGL context loss.
- 캐시된 이전 버전에서 새 버전 갱신.

개발 빌드 자동 probe는 `probe-ready`와 `room-ready-prototype-combat-loop`로 첫 런타임 준비를 동기화한 뒤 `move`, `move-direction-north/east`, `move-motion-direction-north/east`, `move-step-direction-north`, `chaser-moved`, `charger-telegraph`, `charger-charge`, `charger-moved`, `armored-moved`, `armored-broken`, `armored-died`, `place-bomb`, `place-bomb-definition-prototype-cross`, `active-bomb-slot-1`, `place-bomb-definition-prototype-area`, `destructible-wall-destroyed`, `player-contact-damaged`, `contact-escape-moved`, `bomb-exploded`, `player-damaged`, `player-explosion-damaged`, `enemy-died`, `room-cleared`, `room-transition-started`, 방별 `room-ready-*`, `swap-bomb`, `pause-resume`, `audio-unlocked`를 제공한다. `swap-bomb`은 입력 수신이고 `active-bomb-slot-1`은 쿨타임을 통과한 Core 교체다. 명령 probe는 `PlayerCommand.Move`, motion probe는 frame 연속 위치 변화, step probe는 정수 셀 경계 전이를 기록한다. 빠른 단타 검사는 여섯 방향 각각에서 command뿐 아니라 실제 motion이 release 전에 와야 통과한다. 기본 smoke는 `room-transition-started` 두 번과 두 후속 방 준비 표식으로 기존 3방 시퀀스를 검증하고, 차저 표식은 전체 사건 배열에서 `charger-telegraph → charger-charge → charger-moved` 순서를 만족해야 한다. 갑옷 전용 smoke는 별도 시작 씬에서 `armored-moved`, 첫 폭발의 `armored-broken`, 두 번째 폭발의 `armored-died`와 최종 클리어를 확인한다. `destructible-wall-destroyed`와 피해·이동·설치·폭발·상태·사망·클리어 표식은 실제 Core 상태 전이만 기록한다. Core 연속 좌표와 Transform 일치는 PlayMode 통합 테스트가 확인한다. `audio-unlocked` marker는 오디오 출력 자체를 증명하지 않으므로 오디오 연결 뒤 수동 항목을 별도로 통과해야 한다.

## 결과 기록

빌드 식별자, 브라우저/OS 버전, 장치, 해상도, 통과/실패, Console 발췌, 재현 단계, 스크린샷/영상 위치를 남긴다.
