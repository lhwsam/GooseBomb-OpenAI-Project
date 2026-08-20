# Bomb Swap UI 에셋 배치 규칙

이 폴더는 게임이 직접 소유하고 수정하는 UI 에셋의 권위 위치다.

## 폴더 용도

- `Sprites/Common`: 여러 화면에서 공유하는 패널, 버튼, 프레임, 커서 이미지
- `Sprites/Lobby`: 로비 전용 배경과 장식 이미지
- `Sprites/HUD`: 체력, 폭탄 슬롯, 미니맵 등 인게임 HUD 이미지
- `Prefabs`: 프로젝트가 직접 소유하는 재사용 UI prefab과 외부 패키지 prefab variant
- `Materials`: UI 전용 Material과 Shader 설정 자산
- `Animations`: UI Animator Controller와 AnimationClip
- `Atlases`: 프로젝트가 직접 구성한 SpriteAtlas
- `Fonts`: 새로 도입하는 프로젝트 전용 폰트 원본과 TMP Font Asset

## 사용 원칙

1. 직접 만든 이미지와 수정본은 이 폴더에 둔다.
2. 외부 패키지 원본은 `Assets/ThirdParty/UI/<PackageName>`에 보존한다.
3. 외부 prefab을 수정할 때는 원본을 직접 바꾸지 말고 이 폴더의 `Prefabs`에 Prefab Variant를 만든다.
4. 외부 이미지를 가공해야 하면 라이선스를 확인한 뒤 복사본을 이 폴더의 해당 `Sprites` 하위에 둔다.
5. 현재 게임 기본 폰트 `DungGeunMo SDF`는 기존 참조 보호를 위해 `Assets/TextMesh Pro/Fonts`에 유지한다. 이동하지 않는다.
6. WebGL용 UI 이미지는 가능한 한 2의 거듭제곱 크기, 적절한 Max Size, 불필요한 Read/Write 비활성화와 Atlas 묶음을 검토한다.

외부 패키지에서 실제 게임에 채택한 자산만 프로젝트 UI 경계로 연결한다. 테스트용 샘플 씬과 데모 스크립트는 빌드 씬이나 게임 어셈블리에 연결하지 않는다.
