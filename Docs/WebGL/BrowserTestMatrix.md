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
- WASD/방향키, 게임패드 사용 시 매핑. 자동 smoke는 현재 `W`, `Z`, `X`, `Esc` 두 번을 전송한다.
- focus 상실/복귀 후 stuck input 없음.
- 페이지 스크롤/브라우저 단축키와 충돌 없음.
- 폭탄 설치, 교체, pause/resume.
- 사용자 입력 뒤 오디오 재생.
- 전체 화면/창 크기 변경 시 화면과 UI.
- 브라우저 Console error와 WebGL context loss.
- 캐시된 이전 버전에서 새 버전 갱신.

개발 빌드 자동 probe는 `probe-ready`로 런타임 준비를 동기화한 뒤 `move`, `place-bomb`, `swap-bomb`, `pause-resume`, `audio-unlocked`를 확인한다. 현재 입력 기반 `move`와 `audio-unlocked` marker는 실제 Transform 이동 또는 오디오 출력 자체를 증명하지 않으므로 첫 수직 슬라이스와 오디오 연결 뒤 수동 항목을 별도로 통과해야 한다.

## 결과 기록

빌드 식별자, 브라우저/OS 버전, 장치, 해상도, 통과/실패, Console 발췌, 재현 단계, 스크린샷/영상 위치를 남긴다.
