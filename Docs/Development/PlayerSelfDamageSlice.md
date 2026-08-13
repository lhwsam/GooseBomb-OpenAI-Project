# 작업: 플레이어 자기 폭발 피해와 무적 시간

- 상태: `Implemented`
- 시작일: 2026-08-14
- 권장 개발 순서: `PrototypeRoadmap.md` 1단계

## 목표

- 플레이어가 폭발 시점에 영향 셀 안에 있으면 체력 1을 잃는다.
- 피해 직후 짧은 논리 무적 시간 동안 다른 폭발 사건의 피해를 받지 않는다.
- 체력 0에서 사망을 한 번만 확정하고 이후 플레이어 명령을 소비하지 않는다.
- TestSandbox에서 남은 체력과 피격·사망을 식별할 수 있는 placeholder 피드백을 제공한다.

## 근거

- [GDD v0.2](../GameDesign/GDD_v0.2.md) 6.1, 6.3, 17장
- [피해와 무적 시간](../Systems/DamageAndInvulnerability.md)
- [폭탄과 폭발](../Systems/BombAndExplosion.md)
- [런타임 흐름](../Architecture/RuntimeFlow.md)
- [ADR-0002](../ADR/0002-Core-Unity-Separation.md), [ADR-0003](../ADR/0003-Manual-Clock-And-Seed.md)

## 범위

- 변경 허용: Core 플레이어 체력·폭발 피해 규칙, 플레이어 수치 ScriptableObject, TestSandbox 세션·placeholder 표현, builder·validator, 테스트·WebGL probe·문서.
- 변경 금지: Input Actions, vendor 에셋, 물리 기반 피해 판정, 일반 Unity/URP 설정.
- 비목표: 적 접촉 피해, 넉백, 부활·재시작 UI, 적 체력, 폭탄별 위력 데이터, 완성 HUD·VFX·audio.

## 채택할 최소 계약

- 플레이어 최대 체력은 5, 폭발 한 건의 피해는 1이다.
- 무적 시간은 게임 시계를 사용하는 0.75초 `Proposed` 값이며 정확한 경계 `now == invulnerableUntil`부터 다시 피해를 받는다.
- `PrototypePlayerVitalsAsset`이 최대 체력과 무적 시간을 소유하고 유효한 Core 정의로 변환한다.
- 폭발 피해 후보는 `BombExplosion.AffectedCells`와 피해 적용 직전 플레이어의 논리 셀로 판정한다. Transform·Collider는 판정에 사용하지 않는다.
- 각 `BombId` 폭발 사건은 플레이어에게 최대 한 번만 처리된다. 같은 사건을 다시 전달하면 피해나 무적 갱신을 반복하지 않는다.
- 무적 중 도달한 별도 폭발은 즉시 무시하고 나중에 지연 적용하지 않는다.
- 적용된 피해만 `PlayerDamaged`를 발행하며 체력 0이 된 같은 결과에서 `PlayerDied`를 한 번 발행한다.
- 사망 시 유지 이동을 `None`으로 지우고 이후 `PlayerCommand`를 무시한다. 이미 설치된 폭탄과 표현은 정상적으로 마무리한다.
- placeholder 표현은 공유 material을 복제하지 않고 `MaterialPropertyBlock`으로 피격 pulse와 사망 색을 표현한다.

## 완료 조건

- EditMode: 단일 피해, 같은 폭발 중복, 별도 폭발 무적 차단, 무적 종료 직전·정확한 경계, 체력 하한과 사망 단일 발생, 시계 역행 거부를 검증한다.
- PlayMode: 실제 `Z` 폭발이 체력 5→4와 피격 이벤트·placeholder pulse를 만들고 무적 중 다음 폭발 피해를 막는 흐름을 검증한다.
- 콘텐츠: player vitals asset, session·presenter 씬 참조를 builder로 생성·업그레이드하고 validator로 검증한다.
- WebGL: 빌드 성공, browser probe에서 실제 `player-damaged` 사건과 기존 입력·폭탄 사건, Console/page 오류 0을 확인한다.
- 문서: DamageAndInvulnerability, BombAndExplosion, RuntimeFlow, CurrentState가 실제 계약과 일치한다.

## 위험과 롤백

- 폭발 처리 순서는 현재 이동 전이 뒤, 폭탄 폭발 계산 뒤, 도메인 이벤트 전달 전으로 고정한다. 이 순서 변경은 향후 별도 계약 변경이다.
- 직렬화 변경은 신규 asset·신규 component·신규 참조 추가뿐이다. builder가 idempotent하게 업그레이드하고 validator가 누락을 차단한다.
- 수치가 재미를 보장하지 않으므로 자동 테스트 통과와 플레이테스트 판정을 구분한다.

## 구현 및 검증 결과

- Core에 `PlayerHealthDefinition`, `PlayerHealthSimulation`, `PlayerDamageResult`를 추가하고 동일 폭발 중복, 무적 중 별도 폭발, 정확한 종료 경계, 체력 하한, 시계 역행을 포함한 EditMode 테스트를 연결했다.
- Unity에는 `PrototypePlayerVitalsAsset`, 세션 피해·사망 이벤트, `PrototypePlayerHealthPresenter`를 추가하고 Editor builder·validator가 TestSandbox 참조를 생성·검증하도록 했다.
- EditMode `BombSwap.Core.Tests` 107/107, PlayMode `BombSwap.Unity.Tests` 42/42, 콘텐츠 validator 오류 0으로 통과했다.
- `Tools/Verify.ps1 -StaticOnly`과 `node --check Tools/WebGLSmoke.mjs`를 통과했다.
- Development WebGL 빌드는 140,321,275 bytes, 282.492초, 오류 0으로 성공했다. 기존 AI Inference·Feel·TextMeshPro 경로에서 경고 359개가 남아 있다.
- 실제 Edge headless smoke에서 `player-damaged`를 포함한 필수 사건을 모두 관측했고 browser Console/page 오류는 0이었다.
- 검증 증거는 `Artifacts/Verification/20260814-085326-static/`과 `Artifacts/Verification/20260814-084456-web-connected/`에 있으며 Git에서 제외된다.
