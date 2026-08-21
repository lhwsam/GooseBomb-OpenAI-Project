# 픽셀 폰트 렌더링

- 상태: `Accepted`
- 기준일: 2026-08-21
- 관련: [로비와 공통 TMP UI](../Development/LobbySlice.md), [WebGL 성능 예산](../WebGL/PerformanceBudget.md)

## 목적

`DungGeunMo`와 `DNFBitBitv2`의 Raster TMP atlas를 Point 필터로 유지하면서, SDF 변환 없이 선명한 정수 두께 외곽선과 TMP vertex gradient를 제공한다. 폰트 원본과 TextMesh Pro vendor shader는 수정하지 않는다.

## 권위 에셋

| 역할 | 경로 |
|---|---|
| 공용 셰이더 | `Assets/Game/Presentation/Shaders/TMP_PixelOutline.shader` |
| DungGeunMo 머티리얼 | `Assets/Game/Content/UI/Materials/FontStyles/DungGeunMo_PixelOutline.mat` |
| DNFBitBitv2 머티리얼 | `Assets/Game/Content/UI/Materials/FontStyles/DNFBitBitv2_PixelOutline.mat` |
| 예시 그라데이션 | `Assets/Game/Content/UI/Fonts/PixelWarmGradient.asset` |
| 생성·동기화 도구 | `Assets/Game/Editor/UI/PixelFontStyleAuthoring.cs` |
| 머티리얼 Inspector | `Assets/Game/Editor/UI/PixelFontShaderGui.cs` |

`DungGeunMo`는 TMP 기본 폰트다. `DNFBitBitv2`는 제목이나 강조 문구에 선택할 수 있는 지원 폰트이며, 기본 폰트를 대체하지 않는다. 두 원본 TMP Font Asset은 기존 참조 보호를 위해 `Assets/TextMesh Pro/Fonts`에서 이동하거나 수정하지 않는다.

## 렌더링 계약

- 셰이더는 TMP Bitmap의 uGUI stencil, clip rect, alpha clip과 pixel snap 동작을 유지한다.
- 외곽선은 atlas의 주변 texel을 샘플링한다. 두께는 `0`, `1`, `2` atlas pixel만 사용하고 기본값은 `1`이다.
- 두 font atlas는 Point filter와 5px atlas padding을 사용한다. 머티리얼의 `TMP Mesh Padding`은 외곽선 두께 이상이어야 하며 기본값은 `2`다.
- 글자 면은 TMP vertex color를 곱하므로 `Vertex Color Gradient`와 호환된다. 외곽선 색은 그라데이션과 분리된 단색이다.
- 그라데이션 프리셋의 색을 그대로 보려면 TMP 컴포넌트의 기본 Color를 흰색으로 둔다. 다른 Color는 프리셋에 곱해지는 tint로 사용한다.
- 폰트와 머티리얼 atlas는 반드시 짝이 맞아야 한다. DungGeunMo 텍스트에는 DungGeunMo 프리셋, DNFBitBitv2 텍스트에는 DNFBitBitv2 프리셋을 지정한다.

## 디자이너 사용 순서

1. `TextMeshProUGUI`의 Font Asset에 `DungGeunMo` 또는 `DNFBitBitv2` Raster asset을 지정한다.
2. Material Preset에 같은 이름의 `*_PixelOutline` 머티리얼을 지정한다.
3. 그라데이션이 필요하면 `Vertex Color Gradient`를 켜고 `Color Preset`에 `PixelWarmGradient`를 지정하거나 네 꼭짓점 색을 직접 편집한다.
4. 외곽선 색과 두께는 머티리얼 Inspector에서 조절한다. 공유 프리셋 수정은 그 프리셋을 쓰는 모든 문자에 적용된다.
5. 화면별로 다른 스타일이 필요하면 해당 머티리얼을 `Assets/Game/Content/UI/Materials/FontStyles` 안에서 복제하고 shader와 font atlas 짝을 유지한다.

`Bomb Swap/UI/Rebuild Pixel Font Styles` 메뉴는 누락된 기본 프리셋을 만들고 각 머티리얼의 atlas와 shader를 다시 연결한다. 이미 존재하는 외곽선 색·두께와 그라데이션 색은 덮어쓰지 않는다.

## WebGL 성능과 검증

- 기본 1px 외곽선은 글자 fragment당 중심 포함 최대 9회, 2px는 최대 17회 atlas sample을 사용한다. 2px는 큰 제목처럼 면적이 제한된 문자에 우선 사용한다.
- 반복 경로에서 `text.fontMaterial` 접근으로 머티리얼 인스턴스를 만들지 않는다. 저작한 shared material preset을 사용한다.
- `PrototypeContentValidator`는 shader, 두 폰트의 Point filter, 머티리얼과 atlas 짝, outline/padding 범위, gradient preset 존재를 검사한다.
- 렌더링 변경 완료 기준에는 960×600 WebGL에서 한글 외곽선 끊김·이웃 glyph 번짐·그라데이션 banding·UI mask 누락이 없는지 확인하는 브라우저 캡처가 포함된다.
