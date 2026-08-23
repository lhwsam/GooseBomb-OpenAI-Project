# 로컬 VFX 패키지 보관함

Asset Store VFX 원본과 그 원본을 직접 참조하는 프로젝트 저작 효과 prefab의 비공개 복구 위치다. README 이외의 파일은 Git에서 제외한다.

## 공식 취득 경로

- Free Quick Effects Vol. 1: https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424
- Unity Particle Pack: https://assetstore.unity.com/packages/vfx/particles/particle-pack-127325

가격이 무료여도 공개 source 재배포 권한을 뜻하지 않는다. 각 작업자는 자신의 Unity 계정으로 package를 취득하고 프로젝트의 license·seat 조건을 확인한다.

## 복구 package

이력 재작성 전에 만든 `BombSwap-VFX-Private-Backup-*.unitypackage`는 복구 편의를 위한 비공개 보관본이다. package 자체가 사용 권한을 만들지 않으므로 승인된 저장소 밖에서만 보관하고 전달 전 수신자의 권한을 확인한다.

Import 후에는 다음을 확인한다.

1. `Assets/Arts/VFX/EffectPrefab/bomb`의 prefab이 missing material 없이 열린다.
2. `Bomb Swap > Third Party > Validate Public References`가 `Assets/Game`의 VFX 직접 참조 0건을 보고한다.
3. VFX를 공개 Git으로 옮기려면 vendor material·texture를 first-party 자산으로 교체하고 별도 검증한다.

현재 `Assets/Game`은 이 로컬 VFX 경로를 직접 참조하지 않으므로 package가 없어도 prototype 기능 검증과 WebGL 빌드가 가능해야 한다.
