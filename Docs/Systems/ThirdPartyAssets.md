# 서드파티 자산과 로컬 패키지 계약

- 상태: `Accepted`
- 기준일: 2026-08-23
- 관련: [ADR-0005](../ADR/0005-ThirdParty-Adapter.md), [ADR-0009](../ADR/0009-Local-ThirdParty-Asset-Distribution.md), [인게임 UI 프리팹](InGameUiPrefabs.md)

## 저장 경계

| 위치 | Git | 책임 |
|---|---:|---|
| `Assets/Game` | 추적 | 프로젝트 소유 코드·콘텐츠·공개 대체 UI |
| `Assets/ThirdParty`와 `Assets/ThirdParty.meta` | 제외 | 공급자 원본과 로컬 통합 profile |
| `Assets/Feel`과 `Assets/Feel.meta` | 제외 | 유료 FEEL extension. 현재 프로젝트에서는 제거 상태 |
| `Assets/Plugins/Demigiant/DOTweenPro`와 Pro README | 제외 | 유료 DOTween Pro extension. 현재 first-party 직접 사용 없음 |
| `Assets/Arts/VFX`와 `Assets/Arts/VFX.meta` | 제외 | Asset Store VFX 원본과 그 원본을 직접 참조하는 로컬 효과 prefab |
| `Assets/Plugins/Demigiant/DOTween`, `DemiLib` | 추적 | 재배포 조건을 보존한 무료 DOTween Core 원본 |
| `ExternalAssets/UI-Packages`의 package 파일 | 제외 | 팀 내부 전달용 `.unitypackage`와 checksum |
| `ExternalAssets/VFX-Packages`의 package 파일 | 제외 | 권한 확인을 마친 팀원용 VFX 복구 package와 checksum |
| `ExternalAssets/UI-Packages/README.md` | 추적 | Import·Export 절차 |
| `ExternalAssets/VFX-Packages/README.md` | 추적 | VFX 취득·복구·공개 대체 절차 |

`Assets/Game`의 scene, prefab, material, ScriptableObject는 `Assets/ThirdParty`, FEEL, DOTween Pro, `Assets/Arts/VFX`를 직접 직렬화 참조하지 않는다. 외부 에셋을 사용하지 않는 clone도 기능 검증을 수행할 수 있어야 한다.

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

## 선택 UI 스킨

로비와 pause의 외부 UI 이미지는 다음 역할로만 연결한다.

- 로비 배경
- 좌·우 선택 화살표
- 설정 panel 프레임
- 설정 tab·초기화 button 프레임
- 설정 slider 배경·fill

Git에 저장하는 scene과 prefab은 Sprite가 없는 공개 대체 상태다. 일반 Image는 현재 색을 사용하는 단색 사각형으로 남고, Sprite가 없으면 의미가 없는 선택 화살표만 `Image.enabled = false`다. RectTransform, 색상, Image 타입과 버튼 기능은 유지한다.

`PrototypeOptionalUiSkinApplicator`는 Inspector에 저장된 `Image` 참조와 역할만 사용한다. `Awake`에서 `Resources/BombSwap/ThirdPartyUiSkin`을 한 번 읽고 존재하는 Sprite를 런타임 인스턴스에 적용한다. 계층 이름을 바꾸거나 UI를 이동해도 바인딩이 유지되면 동작한다. package가 없거나 역할이 누락되면 해당 Image만 공개 대체 상태를 사용한다.

로컬 profile의 권위 경로는 다음이다.

`Assets/ThirdParty/BombSwap/Resources/BombSwap/ThirdPartyUiSkin.asset`

이 profile은 공개 Git이 아니라 내부 `.unitypackage`에 포함한다. Inspector에서 Sprite 역할을 교체할 수 있으며 first-party scene과 prefab에는 외부 GUID가 저장되지 않는다.

## Editor 절차

### 최초 Import 또는 매핑 갱신

1. 수신자와 프로젝트가 해당 에셋을 사용할 권한이 있는지 확인한다.
2. Unity에서 전달받은 `.unitypackage`를 Import한다.
3. profile이 없거나 기본 역할을 복구해야 하면 `Bomb Swap > Third Party > Create or Update Local UI Skin`을 실행한다.
4. `ThirdPartyUiSkin.asset`의 역할별 Sprite를 Inspector에서 확인한다.
5. `Bomb Swap > Third Party > Validate Public References`를 실행한다.
6. 로비와 pause를 Play Mode에서 확인한다.

### 내부 패키지 내보내기

1. Play Mode를 끈다.
2. 공개 대체 참조 검증을 통과시킨다.
3. local profile이 완전한지 확인한다.
4. `Bomb Swap > Third Party > Export Local Assets Package`를 실행한다.
5. 생성된 `ExternalAssets/UI-Packages/BombSwap-ThirdParty-*.unitypackage`의 SHA-256을 전달 기록에 남긴다.
6. 저장소나 공개 파일 서버가 아닌 승인된 비공개 경로로 전달한다.

`.unitypackage`는 편의를 위한 전달 형식일 뿐 사용 권한을 만들지 않는다. 공급자별 seat, entity, redistribution 조건은 팀이 별도로 확인한다.

## 검증 계약

- `PrototypeContentValidator`는 모든 `Assets/Game` 직접 의존성에 `Assets/ThirdParty`, FEEL, DOTween Pro, `Assets/Arts/VFX`가 없는지 검사한다.
- 로비는 17개, pause는 16개의 명시적 Sprite 바인딩과 공개 대체 상태를 검사한다.
- PlayMode는 profile 부재 시 화살표 숨김·기능 Image 유지와 profile 존재 시 역할별 Sprite 적용을 검사한다.
- 외부 package가 없는 공개 clone에서 Full 검증이 통과해야 한다.
- package 포함 최종 렌더링은 960×600 Game View와 실제 WebGL에서 별도로 확인한다.
- 무료 DOTween Core만 있는 clean checkout에서 compile·PlayMode·WebGL을 통과해야 한다.
