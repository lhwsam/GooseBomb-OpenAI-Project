# 로컬 VFX 패키지 보관함

Asset Store VFX 원본과 그 원본을 직접 참조하는 프로젝트 저작 효과 prefab의 비공개 복구 위치다. README 이외의 파일은 Git에서 제외한다.

## 공식 취득 경로

- Free Quick Effects Vol. 1: https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424
- Unity Particle Pack: https://assetstore.unity.com/packages/vfx/particles/particle-pack-127325

가격이 무료여도 공개 source 재배포 권한을 뜻하지 않는다. 각 작업자는 자신의 Unity 계정으로 package를 취득하고 프로젝트의 license·seat 조건을 확인한다.

## 복구 package

이력 재작성 전에 만든 `BombSwap-VFX-Private-Backup-*.unitypackage`는 복구 편의를 위한 비공개 보관본이다. package 자체가 사용 권한을 만들지 않으므로 승인된 저장소 밖에서만 보관하고 전달 전 수신자의 권한을 확인한다.

Import 후에는 Unity Editor에서 `Bomb Swap/Local Setup/Connect Licensed VFX`를 한 번 실행한다. 이 메뉴는 아래 두 경로를 검사하고 `Assets/Arts/VFX/Resources/BombSwapLocalVfxOverrides.asset`을 생성한다.

- `Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/DustExplosion.prefab`
- `Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/Legacy Particles/Prefabs/SparksEffect.prefab`

연결 결과는 `Bomb Swap/Local Setup/Validate Licensed VFX`로 다시 검사할 수 있다. 메뉴는 플레이어 폭탄 3종의 `SparksEffect` 앵커 아래에 남은 정상·Missing 자식을 먼저 제거하고, 현재 로컬 패키지의 실제 VFX prefab을 `Particle` 자식으로 즉시 다시 저장한다. 생성되는 설정 asset과 VFX 전체는 `.gitignore` 대상이다. 메뉴를 반복 실행해도 현재 로컬 prefab 하나로 교체되므로 중복되지 않는다.

Import 후에는 다음도 확인한다.

1. `Assets/Arts/VFX/EffectPrefab/bomb`의 prefab이 missing material 없이 열린다.
2. 로컬 관찰을 위해 씬이나 prefab에 VFX를 임시 연결했다면 해당 변경은 커밋하지 않는다. 정상 연결은 위 로컬 설정만 사용한다.
3. `PrototypeContentValidator`가 플레이어 폭탄 3종→정확한 `SparksEffect.prefab` 예외 외의 private vendor 직접 참조 0건을 보고하는지 확인한다.
4. VFX를 공개 Git으로 옮기려면 vendor material·texture를 first-party 자산으로 교체하고 별도 검증한다.

현재 플레이어 폭탄 3종은 준비 파티클의 vendor GUID를 참조할 수 있고, package가 없는 clone에서는 Missing으로 보일 수 있다. package를 임포트하고 연결 메뉴를 누르면 현재 로컬 GUID로 교체된다. 비밀문 파괴는 로컬 Resources 설정이 없으면 first-party 절차형 particle fallback을 사용한다.
