# 서드파티 자산과 로컬 패키지 계약

- 상태: `Accepted`
- 기준일: 2026-08-24
- 관련: [ADR-0005](../ADR/0005-ThirdParty-Adapter.md), [ADR-0009](../ADR/0009-Local-ThirdParty-Asset-Distribution.md), [인게임 UI 프리팹](InGameUiPrefabs.md)

## 저장 경계

| 위치 | Git | 책임 |
|---|---:|---|
| `Assets/Game` | 추적 | 프로젝트 소유 코드·콘텐츠·직접 UI Sprite 참조와 공개 대체 UI |
| `Assets/ThirdParty`와 `Assets/ThirdParty.meta` | 제외 | 공급자 원본과 로컬 통합 profile |
| `Assets/Feel`과 `Assets/Feel.meta` | 제외 | 유료 FEEL extension. 현재 프로젝트에서는 제거 상태 |
| `Assets/Plugins/Demigiant/DOTweenPro`와 Pro README | 제외 | 유료 DOTween Pro extension. 현재 first-party 직접 사용 없음 |
| `Assets/Arts/VFX`와 `Assets/Arts/VFX.meta` | 제외 | Asset Store VFX 원본과 그 원본을 직접 참조하는 로컬 효과 prefab |
| `Assets/Plugins/Demigiant/DOTween`, `DemiLib` | 추적 | 재배포 조건을 보존한 무료 DOTween Core 원본 |
| `ExternalAssets/UI-Packages`의 package 파일 | 제외 | 팀 내부 전달용 `.unitypackage`와 checksum |
| `ExternalAssets/VFX-Packages`의 package 파일 | 제외 | 권한 확인을 마친 팀원용 VFX 복구 package와 checksum |
| `ExternalAssets/UI-Packages/README.md` | 추적 | Import·Export 절차 |
| `ExternalAssets/VFX-Packages/README.md` | 추적 | VFX 취득·복구·공개 대체 절차 |

`Assets/Game`의 scene과 prefab은 선택 UI에 사용하는 `Sprite`만 `Assets/ThirdParty`로 직접 직렬화 참조할 수 있다. 해당 `Image`에는 반드시 `PrototypeOptionalSpriteFallback`을 함께 둔다. material, prefab, MonoBehaviour, AudioClip 등 다른 서드파티 타입과 FEEL, DOTween Pro, `Assets/Arts/VFX`의 직접 참조는 허용하지 않는다. 외부 에셋이 없는 clone도 컴파일·기능 검증·WebGL 빌드를 수행할 수 있어야 한다.

## 코드 extension 분리

- 무료 DOTween Core는 공식 원본, copyright와 readme를 함께 유지하는 조건으로 Git에서 재현한다. first-party 코드는 Presentation 계층에서만 Core API를 사용한다.
- DOTween Pro는 무료 Core와 다른 유료 extension이다. Pro 폴더, meta와 전용 readme는 Git에 넣지 않으며 Pro 컴포넌트를 scene·prefab에 저장하지 않는다.
- FEEL은 공급자 원본과 비컴파일 콘텐츠를 재배포하지 않는다. 현재 사용처가 없어 프로젝트에서 제거했고 `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` define도 사용하지 않는다.
- 유료 extension은 비공개 `.unitypackage` 전달을 기본 설치 경로로 사용하지 않는다. 필요한 작업자가 공급자/Asset Store의 유효한 seat를 확보한 뒤 직접 설치한다.
- 빌드된 게임에 포함하는 권리와 Unity 프로젝트 원본을 Git으로 배포하는 권리를 같은 것으로 취급하지 않는다.

## 선택 VFX 원본

- Free Quick Effects Vol. 1과 Unity Particle Pack은 가격이 무료여도 Unity Asset Store 원본이다. 공개 source 저장소에는 넣지 않고 각 작업자가 자신의 Unity 계정으로 공식 package를 취득한다.
- 현재 `Assets/Arts/VFX/EffectPrefab/bomb`의 프로젝트 저작 prefab은 공급자 material을 직접 참조하므로 같은 로컬 경계에 둔다. 공개 Git에 넣으려면 first-party material·texture로 교체하고 직접 의존성 검증을 통과시킨 뒤 별도 변경으로 이동한다.
- 현재 `Assets/Game`에는 이 VFX prefab의 직접 참조가 없다. package가 없어도 prototype 기능과 공개 대체 표현은 유지된다.
- 이력 재작성 전 복구 package는 저장소 밖에만 보관한다. `.unitypackage`는 사용 권한을 만들지 않으며 수신자별 license·seat 조건을 먼저 확인한다.

## 선택 UI Sprite 직접 연결

로비와 pause의 외부 이미지는 각 `Image.sprite`에 직접 연결한다. scene·prefab YAML에는 외부 Sprite의 GUID와 file ID만 저장되며, 원본 texture나 `.meta` 파일 자체는 공개 Git에 포함하지 않는다. 권한 있는 작업자가 동일 GUID를 가진 내부 `.unitypackage`를 Import하면 Edit Mode에서도 참조가 자동 복구된다. 패키지가 없는 clone의 Inspector에서는 `Missing` 또는 빈 Sprite로 보일 수 있으며 이는 허용된 상태다.

외부 Sprite를 연결한 모든 `Image`에는 `PrototypeOptionalSpriteFallback`을 붙인다.

- 기능 배경·panel·button은 `hideWhenMissing = false`를 사용한다. 외부 Sprite가 없으면 선택적인 first-party `fallbackSprite`를 적용하거나, fallback이 비어 있으면 기존 Image 색과 기본 quad를 유지한다.
- Sprite 자체가 의미인 화살표 같은 장식은 `hideWhenMissing = true`를 사용해 런타임에서 숨긴다.
- 컴포넌트는 RectTransform, 색상, Image Type, 버튼 listener와 계층을 바꾸지 않는다.
- 이름·태그·계층 검색과 역할 enum은 사용하지 않는다.

이 구조는 새 UI 타입마다 코드를 수정하지 않는다. 디자이너가 Image를 추가하고 Sprite와 폴백 정책을 Inspector에서 정하면 된다. 이미 배포한 package 안의 Sprite를 새 위치에 사용하는 변경은 scene·prefab만 커밋한다. 새 원본 파일을 추가하거나 slicing·pivot·border처럼 `.meta`가 바뀌면 그때만 내부 package를 갱신하고 팀원에게 다시 전달한다.

`PrototypeOptionalUiSkinApplicator`, `PrototypeOptionalUiSkin`과 `ThirdPartyUiSkin.asset`은 2026-08-24 이전 role 기반 연결을 직접 참조 방식으로 옮기기 위한 legacy migration 호환물이다. 새 UI 저작에는 사용하지 않는다.

## Editor 절차

### 최초 Import 또는 Sprite 변경

1. 수신자와 프로젝트가 해당 에셋을 사용할 권한이 있는지 확인한다.
2. Unity에서 전달받은 `.unitypackage`를 Import한다.
3. 로비 scene 또는 pause prefab을 열어 외부 Sprite가 자동 복구됐는지 확인한다.
4. 이미지를 바꿀 때는 해당 `Image.sprite`에 원하는 Sprite를 직접 드래그한다.
5. 같은 GameObject에 `PrototypeOptionalSpriteFallback`이 있고 `hideWhenMissing` 정책이 맞는지 확인한다.
6. `Bomb Swap > Third Party > Validate Public References`를 실행한다.
7. 960×600 Edit Mode와 Play Mode에서 로비와 pause를 확인한다.

과거 role 기반 자산을 한 번만 전환해야 할 때는 package가 설치된 상태에서 `Bomb Swap > Third Party > Migrate Lobby and Pause to Direct Sprite References`를 사용한다. `Legacy` 하위 skin 메뉴는 새 작업에 사용하지 않는다.

### 내부 패키지 내보내기

1. Play Mode를 끈다.
2. 직접 Sprite·폴백 참조 검증을 통과시킨다.
3. package에 포함할 원본과 `.meta`가 완전한지 확인한다.
4. 새 원본이나 import 설정이 변경된 경우에만 `Bomb Swap > Third Party > Export Local Assets Package`를 실행한다.
5. 생성된 `ExternalAssets/UI-Packages/BombSwap-ThirdParty-*.unitypackage`의 SHA-256을 전달 기록에 남긴다.
6. 저장소나 공개 파일 서버가 아닌 승인된 비공개 경로로 전달한다.

`.unitypackage`는 편의를 위한 전달 형식일 뿐 사용 권한을 만들지 않는다. 공급자별 seat, entity, redistribution 조건은 팀이 별도로 확인한다.

## 검증 계약

- `PrototypeContentValidator`는 `Assets/Game`에서 허용된 외부 UI Sprite를 제외한 `Assets/ThirdParty` 타입, FEEL, DOTween Pro, `Assets/Arts/VFX` 직접 의존성이 없는지 검사한다.
- 로비는 17개, pause는 16개 이상의 직접 Sprite 슬롯, 각 Image의 폴백 컴포넌트와 legacy applicator 부재를 검사한다.
- package가 설치된 상태에서 외부 Sprite를 직접 참조하는 Image에 폴백 컴포넌트가 빠지면 검증이 실패한다.
- PlayMode는 Sprite 부재 시 장식 숨김·기능 Image 유지·first-party fallback 적용과 Sprite 존재 시 원본 유지 동작을 검사한다.
- 외부 package가 없는 공개 clone에서 Full 검증이 통과해야 한다.
- package 포함 최종 렌더링은 960×600 Game View와 실제 WebGL에서 별도로 확인한다.
- 무료 DOTween Core만 있는 clean checkout에서 compile·PlayMode·WebGL을 통과해야 한다.
