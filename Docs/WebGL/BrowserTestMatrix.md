# WebGL 브라우저 테스트 매트릭스

- 상태: `Proposed`; 실제 지원 범위는 첫 배포 전에 확정

## 우선 환경

| 우선순위 | 환경 | 목적 |
|---|---|---|
| P0 | Windows 최신 Chrome 계열 | 주 개발/배포 기준 |
| P0 | Windows 최신 Edge | Chromium 차이와 배포 사용자 확인 |
| P1 | macOS 최신 Safari | WebKit 호환성과 오디오/입력 확인 |
| P1 | macOS 최신 Chrome | 플랫폼과 브라우저 영향 분리 |
| P2 | Firefox 최신 | 별도 엔진 호환 확인 |

모바일 브라우저 지원은 현재 프로토타입 입력 기준 밖이며 별도 결정 전 완료 조건에 포함하지 않는다.

## smoke 항목

- cold/warm load와 진행 표시.
- canvas click/focus 전후 키보드 입력.
- WASD/방향키, 게임패드 사용 시 매핑. 자동 smoke는 Core 사건을 기준으로 `W` 이동, `Z` 설치, 접촉 확인, `A` 이탈, 자기 폭발 확인, 두 번째 `Z` 재유도, `X`, `Esc` 두 번을 순서대로 전송한다. 마지막 방에서는 `ArrowUp` 유지 중 `ArrowRight`를 누르고 `ArrowUp`을 놓기 전에 새 동쪽 방향 명령이 들어오는지 확인한다.
- focus 상실/복귀 후 stuck input 없음.
- 페이지 스크롤/브라우저 단축키와 충돌 없음.
- 추격자 이동, 접촉 피해, 논리 이탈, 폭탄 설치, 실제 fuse 자기 폭발, 두 번째 폭탄 유도 처치·방 클리어, 교체, pause/resume.
- 중앙 루프 클리어 뒤 평행 통로, 평행 통로 클리어 뒤 마지막 엇갈린 기둥 씬이 같은 브라우저 세션에서 준비됨.
- 사용자 입력 뒤 오디오 재생.
- 전체 화면/창 크기 변경 시 화면과 UI.
- 브라우저 Console error와 WebGL context loss.
- 캐시된 이전 버전에서 새 버전 갱신.

개발 빌드 자동 probe는 `probe-ready`와 `room-ready-prototype-combat-loop`로 첫 런타임 준비를 동기화한 뒤 `move`, `move-direction-north`, `move-direction-east`, `chaser-moved`, `place-bomb`, `player-contact-damaged`, `contact-escape-moved`, `bomb-exploded`, `player-damaged`, `player-explosion-damaged`, `enemy-died`, `room-cleared`, `room-transition-started`, `room-ready-prototype-combat-lanes`, `room-ready-prototype-combat-pillars`, `swap-bomb`, `pause-resume`, `audio-unlocked`를 확인한다. 방향 probe는 `PlayerCommand.Move`가 바뀔 때마다 `move-direction-<direction>`을 기록하며, overlap 검사는 `north` 뒤 `east`가 이전 키 해제 전에 도착해야 통과한다. `room-transition-started`가 두 번 발생하고 두 후속 방 준비 표식이 순서대로 관측되어야 3방 시퀀스가 통과한다. 두 피해 표식은 적용된 `PlayerDamageResult.SourceKind`가 각각 `EnemyContact`, `Explosion`일 때만 발생한다. 이동·설치·폭발·사망·클리어 표식도 실제 Core 상태 전이만 기록한다. 실제 보간 좌표, pooled 표현 생명주기, 피격 material property block은 PlayMode 통합 테스트가 확인한다. `audio-unlocked` marker는 오디오 출력 자체를 증명하지 않으므로 오디오 연결 뒤 수동 항목을 별도로 통과해야 한다.

## 결과 기록

빌드 식별자, 브라우저/OS 버전, 장치, 해상도, 통과/실패, Console 발췌, 재현 단계, 스크린샷/영상 위치를 남긴다.
