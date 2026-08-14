# 작업: 3×3 광역 폭탄 수직 슬라이스

- 상태: `Completed`
- 기준일: 2026-08-14

## 목표

- 두 번째 폭탄 슬롯의 빠른 십자 placeholder를 GDD 후보인 3×3 광역 폭탄으로 교체한다.
- 기본 십자 폭탄은 긴 통로와 cardinal 정렬에, 광역 폭탄은 주변에 모인 적과 넓은 공간에 서로 다른 설치 판단을 요구한다.
- 설치된 광역 폭탄의 정의·폭발 모양·표현이 슬롯 교체 뒤에도 보존되고 WebGL에서 구분된다.

## 근거

- `Docs/GameDesign/GDD_v0.2.md` 11.1, 12.3장
- `Docs/GameDesign/ProtoType_v0.2.md` 가설 B와 테스트 2
- `Docs/Systems/BombAndExplosion.md`
- `Docs/Systems/WeaponSlotsAndCooldown.md`
- `Docs/ADR/0001-Logical-XZ-Grid.md`

## 범위

- 변경 허용 경로: `Assets/Game`, 관련 `Docs`, 정의별 WebGL probe와 smoke 기대값
- 변경 금지 경로: `Assets/Feel`, `Assets/Plugins`, 패키지·ProjectSettings, 기존 입력 binding
- 명시적 비목표: 방향성 직선 폭탄, 폭탄별 피해량, 동시 설치 수, 파괴 가능 벽 콘텐츠, 완성 VFX/audio, 재미 통과 판정

## 계약과 불변식

- Core `BombExplosionShape.SquareArea`는 저작된 반경의 Chebyshev 정사각형 셀을 대상으로 한다. 프로토타입 광역 폭탄은 반경 1이라 원점을 포함한 최대 3×3이다.
- 원점은 계속 `Floor`여야 하며 항상 영향 셀에 포함된다.
- 영역 안 각 셀은 독립 판정한다. `Void`와 파괴 불가 벽은 영향 셀에서 제외하고, 파괴 가능 벽은 영향·파괴 목록에 포함한 뒤 같은 시각 폭발 묶음 계산이 끝나면 파괴한다.
- 광역형은 ray를 사용하지 않으므로 한 셀의 벽이 다른 영역 셀을 가리지 않는다.
- 영역 안 다른 종류의 폭탄도 기존과 같은 고정 양수 지연으로 한 번만 연쇄 예약한다.
- 동일 초기 상태의 영향 셀·파괴 셀·연쇄 순서는 결정론적이다.
- 형태·범위·fuse·쿨타임은 검증된 ScriptableObject에서 Core 불변 정의로 변환한다. prefab과 material은 Core에 전달하지 않는다.
- 광역 폭탄 초기 제안값은 ID `prototype-area`, fuse 1.75초, 반경 1, 설치 쿨타임 2.5초다. 정확한 수치는 `Proposed`다.

## 완료 조건

- Core: 3×3 바닥, `Void`·고정 벽 제외, 파괴 벽 처리, 대각선 연쇄와 결정론적 순서를 EditMode에서 검증한다.
- PlayMode: 실제 `X` 뒤 `Z`가 광역 정의를 설치하고 최대 3×3 폭발 결과를 만든다.
- Content: 광역 정의·material·prefab, 로드아웃 참조와 세 씬이 Unity Editor에서 저장되고 validator가 통과한다.
- Presentation: 광역 폭탄과 폭발 셀이 기본 십자 폭탄과 형태·색으로 구분된다.
- WebGL: 성공한 `prototype-area` 설치 사건, 기존 입력·3방·피해·빠른 방향 전환 회귀와 browser Console 오류 0을 확인한다.
- 문서: 폭발·슬롯·런타임·브라우저 계약과 `CurrentState.md`를 실제 구현에 맞춘다.

## 검증 명령과 증거

- Core·Unity: 연결된 Unity Editor의 전체 EditMode/PlayMode와 `PrototypeContentValidator`
- 정적: `./Tools/Verify.ps1 -StaticOnly`
- WebGL: 연결 Editor Development WebGL build와 `Tools/WebGLSmoke.mjs`
- 산출물: `Artifacts/Verification/20260814-165049-area-bomb-web-connected/`

## 완료 증거

- 대상 Core 회귀 29개와 `PrototypePlayerControllerTests` 22개 통과.
- 전체 EditMode 170개, PlayMode 67개 통과. 실패·건너뜀·불확정 0.
- `PrototypeContentValidator` 오류 0, 레거시 quick-cross 자산 없음, 광역 정의·prefab·로드아웃 참조 확인.
- Development WebGL 빌드 성공: 140,817,654 bytes, 78.902초, 오류 0. 패키지·셰이더 기존 범주의 경고 359개.
- Edge headless에서 `prototype-area` 성공 설치, 3방 전환, 빠른 `North/East` 여섯 번, 마지막 방 자기 피해, resize와 gameplay probe 통과. Console/page error 0.
- `webgl-gameplay.png`에서 활성 2번 슬롯·`prototype-area` HUD, 보라색 광역 폭탄, 기존 십자 폭발과의 표현 구분을 확인.
- `Tools/Verify.ps1 -StaticOnly`, `node --check Tools/WebGLSmoke.mjs`, 정적 서버 회귀 통과.

## 위험과 롤백

- 3×3은 자기 위험과 근거리 처치력을 동시에 키우므로 실제 유용성·공정성은 수동 플레이테스트가 필요하다.
- 기존 `prototype-quick-cross`는 저장 시스템이 없는 프로토타입 placeholder다. Unity Editor builder가 새 광역 자산으로 로드아웃을 교체한 뒤 해당 legacy 자산을 제거한다.
- 롤백 단위는 Core shape/resolver, Unity 저작 shape, 광역 콘텐츠, 로드아웃·probe·문서를 한 묶음으로 한다.
