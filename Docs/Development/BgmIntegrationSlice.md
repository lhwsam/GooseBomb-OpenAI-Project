# BGM 통합 슬라이스

- 상태: `Implemented`, 실제 WebGL 청감 승인 `Pending`
- 기준일: 2026-08-24
- 권위 계약: [사용자 설정, 오디오와 화면 흔들림](../Systems/UserSettingsAndAudio.md)
- 기술 결정: [ADR-0010](../ADR/0010-Presentation-Owned-Adaptive-Bgm.md)

## 플레이어 계약

- 첫 입력 뒤 로비 음악이 시작되고 로비→던전→보스 이동에서는 각 family가 처음부터 자연스럽게 교차한다.
- 던전은 곡의 위치를 유지한 채 안전·전투·회복·보상·클리어 상태에 맞춰 stem 밀도만 다음 마디에서 바뀐다.
- 보스는 One→Two→LastStand로 갈수록 Grand·Danger stem이 증가한다.
- pause는 음악을 멈추거나 다시 시작하지 않고 절반 음량으로 낮춘다. 재개하면 원래 음량으로 돌아간다.
- 사망과 보스 격파에서는 음악이 fade-out한다.
- 설정의 BGM 음량은 위 적응형 변화와 독립적으로 항상 적용된다.

## 구현 범위

| 영역 | 구현 |
|---|---|
| 데이터 | `PrototypeBgmCatalog.asset`: BGM Mixer 그룹, 런타임 clip 8개, 전환 수치 |
| 정책 | `PrototypeBgmMixPolicy`: room/clear와 boss phase별 gain, family별 마디 길이 |
| 재생 | `PrototypeBgmPresenter`: 사용자 gesture unlock, DSP 예약, stem 동기화, crossfade, pause duck, terminal fade |
| 저작 | `Bomb Swap/Prototype/Apply BGM Integration`: catalog 생성/갱신과 대상 17개 scene root 연결 |
| 검증 | catalog format/sample 수, preview 비참조, scene presenter 수·참조·직렬화 AudioSource 부재 |
| 테스트 | 던전 room/clear mix, boss phase mix, 다음 마디 계산, 잘못된 Boss room 정책 거부 |

대상 scene은 `DungeonLobby`, 정식 던전 6개, 독립 TestSandbox/플레이테스트 10개다. scene에는 `PrototypeBgmPresenter`와 catalog 참조만 저장하고 여덟 `AudioSource`는 재생 시점에 만든다.

## 콘텐츠 규칙

- 모든 런타임 clip은 44.1 kHz stereo다.
- 로비는 3,528,000 sample, 던전 네 stem은 각각 2,919,724 sample, 보스 세 stem은 각각 2,646,000 sample이어야 한다.
- `BGM_DungeonCombat_*`, `BGM_DungeonRecovery_*`, `BGM_BossBattle_OverheatedThrone_8Bit_Loop.wav` full mix는 청감 미리보기이며 runtime catalog에서 제외한다.
- source 재생 위치를 개별 보정하지 않는다. 같은 family stem은 sample 0의 동일 DSP 시각부터 계속 함께 돈다.
- 생성 source는 `Tools/Audio/GenerateLobbyBgm.py`, `GenerateDungeonCombatBgm.py`, `GenerateBossBattleBgm.py`다. clip을 교체하면 생성기 또는 명시된 원본, sample 수, digital-zero loop 경계를 함께 검증한다.

## 검증 상태

- StaticOnly: `Artifacts/Verification/20260824-044629-static/` 통과.
- 연결된 전체 PlayMode: BGM 정책 테스트는 통과했다. 전체 결과는 176개 중 163개 통과, 현재 병행 중인 이동/콘텐츠 변경의 13개 실패가 있어 Full 통과로 보고하지 않는다. 증거는 `Artifacts/Verification/ConnectedTests/20260823-194707-155.json`이다.
- 전체 실행 teardown에서 발견한 Input System one-shot 구독 해제 예외를 명시적 `InputSystem.onEvent` 구독/해제로 수정하고 Unity 재컴파일 성공을 확인했다.
- 수정 뒤 BGM 집중 재실행은 활성 Unity scene의 별도 미저장 root `fhf` 때문에 저장 확인 창에서 중단했다. 그 변경을 저장·폐기하지 않았으며 재검증이 남아 있다.
- Development WebGL 빌드, 브라우저 `bgm-audio-started`, 실제 로비/방/보스/pause/결과 청감은 아직 실행하지 않았다.

## 남은 수동 확인

1. 외부 미저장 scene 상태를 소유자가 정리한 뒤 BGM 집중 PlayMode를 다시 실행한다.
2. Web tier에서 첫 gesture 이후 `bgm-audio-started`와 Console/page error 0을 확인한다.
3. 로비→던전 전투→클리어→Recovery/BombReward→보스 3 phase→pause→격파/사망을 실제 스피커로 듣고 loop click, stem 위상, 상대 음량과 1초 전환을 기록한다.
4. 설정 BGM 0%, 기본 70%, 100%가 적응형 mix를 유지하면서 즉시 반영되는지 확인한다.
