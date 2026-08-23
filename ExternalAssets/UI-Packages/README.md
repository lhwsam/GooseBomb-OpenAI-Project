# 로컬 서드파티 패키지 보관함

다운로드한 공급자 파일과 팀 내부 전달용 `.unitypackage`를 저장소 밖에서 보관하는 위치다.

README 이외의 파일은 Git에서 제외한다. `.unitypackage`를 받았다는 사실만으로 사용 권한이 생기지는 않으므로 수신자와 프로젝트의 라이선스 조건을 먼저 확인한다.

## 받는 사람

1. Unity에서 `Assets > Import Package > Custom Package`를 선택한다.
2. 전달받은 `BombSwap-ThirdParty-*.unitypackage`를 Import한다.
3. `Assets/ThirdParty/BombSwap/Resources/BombSwap/ThirdPartyUiSkin.asset`이 있는지 확인한다.
4. `Bomb Swap > Third Party > Validate Public References`를 실행한다.
5. 로비와 pause를 Play Mode에서 확인한다.

## 내보내는 사람

1. Play Mode를 끈다.
2. 필요하면 `Bomb Swap > Third Party > Create or Update Local UI Skin`으로 기본 매핑을 복구한다.
3. `ThirdPartyUiSkin.asset`의 역할별 Sprite를 확인한다.
4. `Bomb Swap > Third Party > Export Local Assets Package`를 실행한다.
5. 생성된 package의 SHA-256과 에셋 버전을 전달 기록에 남긴다.
6. package는 승인된 비공개 경로로만 전달하고 Git에 추가하지 않는다.

first-party scene과 prefab은 외부 원본을 직접 참조하지 않는다. package Import 후 런타임에만 선택 UI 스킨이 적용되므로 package가 없는 공개 clone도 기능 검증이 가능하다. 자세한 계약은 `Docs/Systems/ThirdPartyAssets.md`를 따른다.
