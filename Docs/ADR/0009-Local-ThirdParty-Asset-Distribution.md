# ADR-0009: 서드파티 원본은 로컬 패키지로 배포

- 상태: `Accepted`
- 날짜: 2026-08-23 (2026-08-24 UI 직접 참조 결정으로 개정)
- 결정자: 프로젝트 팀

## 맥락

`Assets/ThirdParty`의 UI 원본, FEEL과 DOTween Pro 같은 유료 Asset Store extension은 게임에서 사용할 권리가 있더라도 공개 Git 저장소에 원본을 재배포할 권리와는 다를 수 있다. 가격이 무료인 Free Quick Effects Vol. 1과 Unity Particle Pack도 Asset Store 원본이라는 경계는 같다. 반대로 단순히 폴더만 삭제하면 로비 UI Sprite나 Pro 컴포넌트 참조가 깨져 외부 에셋이 없는 개발자와 자동 검증 환경이 프로젝트를 정상적으로 열 수 없다. 무료 DOTween Core는 Pro와 별도 라이선스로 원본 재배포가 허용되므로 같은 경로 전체를 일괄 제외하면 재현 가능한 빌드를 잃는다.

## 결정

- `Assets/ThirdParty` 전체와 폴더 meta는 Git에서 추적하지 않는다.
- `Assets/Feel` 전체와 DOTween Pro 폴더·meta·전용 readme는 Git에서 추적하지 않는다. FEEL은 현재 프로젝트에서 제거하고 유료 extension은 각 작업자가 유효한 license로 직접 설치한다.
- `Assets/Arts/VFX` 전체와 폴더 meta는 Git에서 추적하지 않는다. 여기에는 Asset Store 원본과 그 material을 직접 참조하는 프로젝트 저작 bomb VFX prefab이 함께 포함된다.
- `Assets/Plugins/Demigiant/DOTween`과 `DemiLib`의 무료 Core 원본은 copyright와 readme를 보존한 채 Git에서 재현한다. vendor 원본은 프로젝트에서 직접 수정하지 않는다.
- 승인된 팀원에게는 저장소 밖의 비공개 경로로 `.unitypackage`를 전달한다. 전달 전 각 수신자의 사용 권한을 확인한다.
- Git에 들어가는 `Assets/Game` 씬·프리팹은 선택 UI의 `Sprite`에 한해 `Assets/ThirdParty` GUID를 직접 참조할 수 있다. material, prefab, script, audio 등 다른 외부 타입의 직접 참조는 금지한다.
- 직접 외부 Sprite를 저장한 모든 `Image`에는 `PrototypeOptionalSpriteFallback`을 둔다. 외부 Sprite가 없으면 기능 Image는 first-party fallback 또는 기본 Image 표현을 유지하고, Sprite 자체가 의미인 장식은 숨긴다.
- 외부 바이너리와 `.meta`는 계속 Git에서 제외한다. scene·prefab에 저장한 GUID는 원본 재배포가 아니며, 동일 GUID를 보존한 승인된 내부 package를 Import하면 Edit Mode 참조가 자동 복구된다.
- 패키지가 없는 공개 clone에서 Inspector의 Sprite가 `Missing`으로 보이는 것은 허용한다. 컴파일·PlayMode 기능·WebGL 빌드는 폴백으로 유지되어야 한다.
- 새 UI 종류는 역할 enum이나 profile을 추가하지 않고 Inspector에서 Sprite와 폴백 정책을 직접 저작한다. 이름·태그·계층 경로 검색은 하지 않는다.
- 기존 `PrototypeOptionalUiSkinApplicator`와 `ThirdPartyUiSkin.asset`은 직접 참조로 옮기는 일회성 legacy migration 용도로만 남긴다.
- Editor validator는 허용된 UI Sprite 이외의 private vendor 직접 의존성과 각 직접 Sprite 슬롯의 폴백을 검사한다.
- VFX를 공개 소유 콘텐츠로 전환하려면 공급자 material·texture 의존성을 first-party 자산으로 교체한 뒤 `Assets/Game` 또는 별도의 공개 경로로 이동한다.

## 대안

- 원본을 Git 또는 Git LFS에 유지: 파일 배포 경로가 공개 저장소와 같아져 라이선스 위험을 해결하지 못한다.
- 외부 참조가 남은 씬을 폴백 없이 커밋하고 Import로 GUID만 복구: Import 전 기능과 검증이 불완전해지므로 채택하지 않는다. 직접 참조는 per-Image 폴백이 있을 때만 허용한다.
- 역할별 runtime skin profile: 공개 clone에는 Missing이 없지만 새 UI 타입마다 enum·profile·적용 코드를 수정해야 하고 결과를 Play Mode에서만 확인하기 쉬워 저작 비용이 과도하다. 2026-08-24 직접 Sprite+폴백 방식으로 대체했다.
- 외부 이미지를 first-party 복사본으로 커밋: 파생본 재배포 권한을 별도로 확인해야 하며 현재 요구를 해결하지 못한다.
- 모든 외부 UI를 즉시 다시 제작: 안전하지만 현재 디자이너 작업을 불필요하게 폐기한다.

## 결과

- 공개 저장소 clone은 외부 패키지 없이도 컴파일·테스트·WebGL 빌드가 가능해야 한다. 외형은 단색 대체 상태다.
- 권한이 있는 팀원은 패키지를 Import한 뒤 Edit Mode와 Play Mode에서 원래 UI Sprite를 확인하고 직접 교체할 수 있다.
- 이미 package에 포함된 Sprite를 새 Image에 사용하는 변경은 scene·prefab만 커밋한다. 새 원본 또는 `.meta` import 설정이 바뀔 때만 `.unitypackage`를 다시 내보낸다.
- `Assets/ThirdParty`, FEEL, DOTween Pro, `Assets/Arts/VFX`는 모든 브랜치·태그의 Git 기록에서도 제거한다. 강제 갱신 뒤 팀원은 새 clone을 사용하고 old clone·fork·PR cache가 남지 않았는지 별도로 확인한다.
- 패키지 Import·제거 양쪽에서 Full 검증을 수행하고, 렌더링이나 빌드 포함이 바뀌면 Web tier까지 실행한다.

## 검증 및 철회 조건

- `Bomb Swap > Third Party > Validate Public References`가 허용되지 않은 private 의존성 0건과 직접 UI Sprite 폴백 완성을 보고해야 한다.
- 로컬 패키지 유무 각각에서 로비·pause 기능, Console, PlayMode를 확인한다.
- Unity의 패키지 배포 방식이나 에셋 라이선스가 바뀌면 이 결정을 재검토한다.

## 관련 문서

- [서드파티 자산과 로컬 패키지](../Systems/ThirdPartyAssets.md)
- [서드파티 어댑터](0005-ThirdParty-Adapter.md)
- [인게임 UI 프리팹](../Systems/InGameUiPrefabs.md)
