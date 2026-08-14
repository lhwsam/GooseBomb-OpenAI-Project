# 작업: 두 폭탄 슬롯과 독립 설치 쿨타임

- 상태: `Completed`
- 기준일: 2026-08-14

## 목표

- 플레이어가 `X`로 두 폭탄 슬롯을 교체하고 `Z`로 활성 슬롯의 폭탄을 설치한다.
- 성공한 설치만 해당 슬롯의 독립 설치 쿨타임을 시작한다.
- 비활성 슬롯도 같은 게임 시계로 회복하고, 교체는 별도 쿨타임을 사용한다.
- 현재 슬롯과 두 설치 쿨타임 및 교체 쿨타임을 WebGL 화면에서 읽을 수 있다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 8~9장, 12장
- `Docs/GameDesign/ProtoType_v0.2.md` 가설 B·C와 테스트 2
- `Docs/Systems/WeaponSlotsAndCooldown.md`
- `Docs/Architecture/RuntimeFlow.md`

## 범위

- 변경 허용 경로: `Assets/Game`, 관련 `Docs`, 검증 하네스의 슬롯 관찰 항목
- 변경 금지 경로: `Assets/Feel`, `Assets/Plugins`, 패키지 버전, 기존 Input Actions binding
- 명시적 비목표: 직선·광역 폭발 resolver, 동시 설치 수 제한, 공통 설치 간격, 패시브 빌드, 완성형 HUD

## 계약과 불변식

- 입력은 기존 `PlayerCommand.PlaceBomb`과 `PlayerCommand.SwapBomb`을 사용한다.
- Core 로드아웃이 활성 슬롯, 두 슬롯의 다음 설치 가능 시각, 다음 교체 가능 시각을 소유한다.
- 설치 성공 시에만 활성 슬롯의 설치 쿨타임을 소비한다. 막힌 셀·중복 폭탄 등 실패는 소비하지 않는다.
- 한 슬롯의 설치가 다른 슬롯의 설치 가능 시각을 바꾸지 않는다.
- 설치된 폭탄은 교체 후에도 원래 정의 ID와 fuse를 유지한다.
- 비활성 슬롯은 별도 업데이트나 타이머 감소 없이 주입된 `IGameClock.Now`를 기준으로 회복한다.
- 거부된 교체는 활성 슬롯과 기존 교체 종료 시각을 바꾸지 않는다.
- UI는 Core snapshot을 표시하고 자체 쿨타임 권위 상태를 만들지 않는다.
- 프로토타입 초기값은 1번 기본 십자 폭탄 1.5초, 2번 빠른 십자 placeholder 0.75초, 교체 2초로 두며 모두 `Proposed`다.

## 완료 조건

- Core: 독립 진행, 비활성 회복, 실패 미소비, 교체 경계, 주입 시계 정지를 EditMode에서 검증한다.
- PlayMode: 실제 `X` 입력이 활성 슬롯을 바꾸고 다음 `Z`가 선택한 정의를 설치한다.
- Presentation: 두 슬롯과 설치·교체 준비 상태가 화면에 표시되고 서로 다른 설치 폭탄을 구분할 수 있다.
- Content: 두 폭탄 정의, 로드아웃 참조, 세 TestSandbox 씬 연결과 validator가 통과한다.
- WebGL: canvas focus 후 `X`/`Z` 입력으로 실제 슬롯 변경과 슬롯별 설치를 smoke에서 관찰한다.
- 문서: Systems, BrowserTestMatrix, CurrentState를 실제 구현과 일치시킨다.

## 위험과 롤백

- 현재 두 번째 폭탄은 슬롯·리듬 검증용 빠른 십자 placeholder라서 가설 B의 공간 역할 차이를 판정하지 않는다.
- ScriptableObject와 세 씬 참조는 Unity Editor builder로만 생성·저장한다.
- 롤백 단위는 Core 로드아웃, Unity 세션·표현, 저작 asset·scene upgrade를 한 묶음으로 한다.

## 완료 증거

- EditMode 전체 166개 통과, 실패/건너뜀/불확정 0. 이 중 `BombWeaponLoadoutTests` 7개가 독립 진행, 비활성 회복, 실패 미소비, 교체 경계와 주입 시계 정지를 검증한다.
- PlayMode 전체 66개 통과, 실패/건너뜀/불확정 0. 실제 `X` 교체 뒤 두 번째 정의 설치와 HUD snapshot 표시 테스트를 포함한다.
- `PrototypeContentValidator` 오류 0. 두 폭탄 정의·로드아웃, 세 TestSandbox 씬의 session/HUD 참조와 정의별 prefab을 확인했다.
- Development WebGL 빌드 성공: 140,815,078 bytes, 오류 0. 증거는 `Artifacts/Verification/20260814-161438-two-slot-web-connected/`에 기록했다.
- Edge headless smoke에서 첫 정의 설치 → 두 번째 슬롯 교체 → 빠른 정의 설치와 기존 3방·빠른 방향 전환 회귀를 통과했고 browser Console/page error는 0이었다.
- 실제 WebGL 화면에서 두 슬롯, 활성 슬롯 강조, 슬롯별 설치 준비 막대와 교체 준비 문구가 식별되는지 확인했다.
