# BombSwap first-party assets

이 폴더만 프로젝트 first-party 게임 코드와 콘텐츠의 기본 소유 영역으로 사용한다.

- `Core`: UnityEngine 비참조 게임 규칙.
- `Runtime`: Unity 생명주기, 입력, simulation 연결.
- `Presentation`: 3D 표현, UI, 오디오, VFX.
- `Authoring`: ScriptableObject/방 저작과 런타임 변환.
- `Content`: 폭탄, 적, 방 데이터와 프리팹.
- `Scenes`: 프로토타입과 테스트 샌드박스.
- `Editor`: 검증, 빌드, 마이그레이션 도구.
- `Tests`: EditMode/PlayMode 테스트.

상세 경계는 `Docs/Development/FolderStructure.md`와 `Docs/Architecture/DependencyRules.md`를 따른다. `Assets/Feel`, `Assets/Plugins` 등 vendor 경로는 직접 수정하지 않는다.
