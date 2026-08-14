# 작업: 일반 전투방 클리어 토큰 보상

## 목표

- 플레이어가 일반 전투방을 처음 클리어하면 즉시 확인 가능한 작은 보상을 얻는다.
- 보상은 현재 런 동안 방을 이동해도 유지되고 새 런에서는 초기화된다.
- 이번 슬라이스는 보상 루프의 존재를 검증하는 최소 점수이며 최종 재화 경제를 미리 만들지 않는다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 5.2, 21.1: 일반 전투방 클리어 시 작은 재화를 지급한다.
- `Docs/Systems/DungeonGeneration.md`: `DungeonRunState`가 방문·클리어·런 결과의 권위 상태를 소유한다.
- `Docs/Systems/RunCompletion.md`: 재시작은 방문·클리어·보상 선택을 포함한 런 상태를 초기화한다.

## 범위

- 변경 허용 경로:
  - `Assets/Game/Core/Dungeon/DungeonRunState.cs`
  - 기존 Unity 런 세션, room binder, HUD와 관련 EditMode/PlayMode 테스트
  - WebGL smoke, 관련 Systems/Testing/CurrentState 문서
- 변경 금지 경로:
  - `Assets/Feel`, `Assets/Plugins`, 패키지와 ProjectSettings
- 명시적 비목표:
  - 상점, 소비, 아이템 드롭, 메타 재화, 저장, 보상 배율, 무작위 보상
  - 최종 재화 명칭·밸런스 확정 또는 재미 검증 통과 선언

## 계약과 불변식

- 입력: 진행 중인 런의 현재 일반 `Combat` 방이 처음 클리어된다.
- 출력/관찰 가능한 결과:
  - Core `CombatRewardTokenCount`가 정확히 1 증가한다.
  - HUD의 `ROOM TOKENS` 숫자가 같은 클리어 처리에서 갱신된다.
  - 방 전환 후 새 HUD도 현재 런의 누적 값을 표시한다.
  - WebGL 하네스는 `combat-reward-tokens-N` marker로 0, 1, 2, 3과 재시작 뒤 0을 관찰할 수 있다.
- 상태 소유자:
  - `DungeonRunState`가 토큰 수의 유일한 권위 원본이다.
  - Unity run session과 binder는 조회·사건 전달만 하고 HUD는 표시 snapshot만 소유한다.
- 실패/경계 동작:
  - 같은 방의 중복 클리어, 안전방, 보스방, terminal 런의 클리어 요청은 토큰을 지급하지 않는다.
  - 완료 또는 실패 뒤 재시작한 새 `DungeonRunState`는 0에서 시작한다.
- WebGL 제약:
  - 프레임 폴링, 런타임 할당 루프, 스레드와 동기 대기를 추가하지 않는다.
  - 기존 이벤트 연결과 동적 HUD UI를 재사용한다.

## 완료 조건

- 구현:
  - 일반 전투방 최초 클리어 +1, 중복 방지, 런 수명 유지와 새 런 초기화가 구현된다.
  - 모든 던전 씬에서 HUD 값이 표시되고 즉시 갱신된다.
- EditMode:
  - 초기값, 일반 전투 최초/중복 지급, 안전방·보스·terminal 비지급을 검증한다.
- PlayMode:
  - run session 노출과 실제 던전 씬 전환 뒤 HUD 누적 표시를 검증한다.
- WebGL/브라우저:
  - 주 경로의 세 일반 전투 클리어가 1→2→3 marker를 만들고 완료·실패 재시작 뒤 0을 확인한다.
  - browser Console과 page error가 0이다.
- 문서:
  - `DungeonGeneration.md`, `VerificationHarness.md`, `CurrentState.md`가 실제 계약과 증거를 반영한다.

## 검증 명령과 증거

- 명령/도구:
  - 연결된 Unity의 EditMode·PlayMode 테스트와 콘텐츠 검증
  - 연결된 Unity WebGL development build
  - `node Tools/WebGLSmoke.mjs ...`
  - `./Tools/Verify.ps1 -StaticOnly` post-commit 점검
- 실제 산출물:
  - `Artifacts/Verification/20260815-060000-combat-clear-reward-web/`
  - 9씬 WebGL build report, browser smoke JSON과 완료·실패·pause·게이트·보스 예고 screenshot
- 실제 결과:
  - EditMode 267/267, PlayMode 110/110, 콘텐츠 validator·Unity Console 오류 0
  - Development WebGL 137,745,798 bytes, 오류 0, 기존 패키지·셰이더 범주의 경고 351건
  - Edge headless 27/27, `combat-reward-tokens-1 → 2 → 3`, 완료·실패 재시작 `0`, browser Console/page error 0
- 기준선:
  - 시작 commit `0bdd94b`, EditMode 267/267, PlayMode 110/110, 콘텐츠 검증·Console 오류 0

## 위험과 롤백

- 직접적 패키지/성능 위험: 낮음. Core 정수 하나와 클리어 사건 기반 HUD 갱신만 추가한다.
- 의미 위험: `+1`과 `ROOM TOKENS`는 소비처 없는 프로토타입 임시 표현이며 최종 경제가 아니다.
- 롤백 단위: 이 슬라이스의 Core 상태, binder/HUD 표시, 테스트·하네스·문서를 함께 되돌린다.
