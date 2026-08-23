using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class PixelFontStyleAuthoring
    {
        public const string ShaderPath =
            "Assets/Game/Presentation/Shaders/TMP_PixelOutline.shader";
        public const string DungGeunMoFontPath =
            "Assets/TextMesh Pro/Fonts/DungGeunMo.asset";
        public const string DnfBitFontPath =
            "Assets/TextMesh Pro/Fonts/DNFBitBitv2.asset";
        public const string DungGeunMoMaterialPath =
            "Assets/Game/Content/UI/Materials/FontStyles/DungGeunMo_PixelOutline.mat";
        public const string DnfBitMaterialPath =
            "Assets/Game/Content/UI/Materials/FontStyles/DNFBitBitv2_PixelOutline.mat";
        public const string WarmGradientPath =
            "Assets/Game/Content/UI/Fonts/PixelWarmGradient.asset";
        public const string PreviewScenePath =
            "Assets/Game/Scenes/TestSandbox/PixelFontStylePreview.unity";

        private const float DefaultOutlineWidth = 1f;
        private const float DefaultMeshPadding = 2f;

        private static readonly Color DefaultOutlineColor =
            new Color(0.035f, 0.04f, 0.06f, 1f);
        private static readonly Color DefaultGradientTop =
            new Color(1f, 0.953f, 0.765f, 1f);
        private static readonly Color DefaultGradientBottom =
            new Color(1f, 0.694f, 0.235f, 1f);

        [MenuItem("Bomb Swap/UI/Rebuild Pixel Font Styles")]
        public static void RebuildPixelFontStyles()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Missing pixel font shader: {ShaderPath}");
            }

            EnsureAssetFolder("Assets/Game/Content/UI/Materials/FontStyles");
            CreateOrSynchronizeMaterial(
                DungGeunMoFontPath,
                DungGeunMoMaterialPath,
                shader);
            CreateOrSynchronizeMaterial(
                DnfBitFontPath,
                DnfBitMaterialPath,
                shader);
            CreateWarmGradientIfMissing();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Bomb Swap pixel font styles are ready. Existing outline colors, widths, and gradient colors were preserved.");
        }

        [MenuItem("Bomb Swap/UI/Rebuild Pixel Font Preview Scene")]
        public static void RebuildPixelFontPreviewScene()
        {
            TMP_FontAsset dungGeunMo = LoadRequiredAsset<TMP_FontAsset>(
                DungGeunMoFontPath);
            TMP_FontAsset dnfBit = LoadRequiredAsset<TMP_FontAsset>(DnfBitFontPath);
            Material dungGeunMoMaterial = LoadRequiredAsset<Material>(
                DungGeunMoMaterialPath);
            Material dnfBitMaterial = LoadRequiredAsset<Material>(
                DnfBitMaterialPath);
            TMP_ColorGradient gradient = LoadRequiredAsset<TMP_ColorGradient>(
                WarmGradientPath);

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene previewScene = default;
            try
            {
                previewScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                SceneManager.SetActiveScene(previewScene);
                previewScene.name = "PixelFontStylePreview";

                CreatePreviewCameraAndLight();
                RectTransform canvas = CreatePreviewCanvas();
                CreatePanel(canvas);

                CreatePreviewText(
                    "Heading",
                    canvas,
                    "픽셀 폰트 외곽선 + 그라데이션",
                    dungGeunMo,
                    dungGeunMoMaterial,
                    gradient,
                    48f,
                    new Vector2(0f, 205f),
                    new Vector2(860f, 80f));
                CreatePreviewText(
                    "DungGeunMoSample",
                    canvas,
                    "DungGeunMo  -  Bomb Goose",
                    dungGeunMo,
                    dungGeunMoMaterial,
                    gradient,
                    38f,
                    new Vector2(0f, 80f),
                    new Vector2(860f, 70f));
                CreatePreviewText(
                    "DNFBitBitv2Sample",
                    canvas,
                    "DNFBitBitv2  -  Bomb Goose",
                    dnfBit,
                    dnfBitMaterial,
                    gradient,
                    38f,
                    new Vector2(0f, -15f),
                    new Vector2(860f, 70f));
                CreateMaskPreview(
                    canvas,
                    dungGeunMo,
                    dungGeunMoMaterial,
                    gradient);
                CreatePreviewText(
                    "UsageNote",
                    canvas,
                    "Font Asset과 같은 이름의 Material Preset을 사용하세요  -  Outline 0~2 px",
                    dungGeunMo,
                    dungGeunMoMaterial,
                    null,
                    20f,
                    new Vector2(0f, -250f),
                    new Vector2(900f, 45f));

                if (!EditorSceneManager.SaveScene(previewScene, PreviewScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not save pixel font preview scene: {PreviewScenePath}");
                }
            }
            finally
            {
                if (previewScene.IsValid() && previewScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(previewScene, true);
                }
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }

            AssetDatabase.ImportAsset(PreviewScenePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Rebuilt pixel font preview scene without modifying the open scene: {PreviewScenePath}");
        }

        public static void Validate(ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                errors.Add($"Missing pixel font shader: {ShaderPath}");
                return;
            }
            if (!shader.isSupported)
            {
                errors.Add($"Pixel font shader is unsupported: {ShaderPath}");
            }

            ValidateFontMaterial(
                DungGeunMoFontPath,
                DungGeunMoMaterialPath,
                shader,
                errors);
            ValidateFontMaterial(
                DnfBitFontPath,
                DnfBitMaterialPath,
                shader,
                errors);

            if (AssetDatabase.LoadAssetAtPath<TMP_ColorGradient>(WarmGradientPath) == null)
            {
                errors.Add($"Missing TMP pixel font gradient preset: {WarmGradientPath}");
            }
        }

        private static void CreateOrSynchronizeMaterial(
            string fontPath,
            string materialPath,
            Shader shader)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                throw new InvalidOperationException($"Missing TMP font asset: {fontPath}");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool isNew = material == null;
            if (isNew)
            {
                material = new Material(font.material)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(materialPath)
                };
                material.shader = shader;
                material.SetColor("_OutlineColor", DefaultOutlineColor);
                material.SetFloat("_OutlineWidth", DefaultOutlineWidth);
                material.SetFloat("_Padding", DefaultMeshPadding);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", font.atlasTexture);
            EditorUtility.SetDirty(material);
        }

        private static void CreateWarmGradientIfMissing()
        {
            TMP_ColorGradient gradient =
                AssetDatabase.LoadAssetAtPath<TMP_ColorGradient>(WarmGradientPath);
            if (gradient != null)
            {
                return;
            }

            gradient = ScriptableObject.CreateInstance<TMP_ColorGradient>();
            gradient.name = "PixelWarmGradient";
            gradient.colorMode = ColorMode.VerticalGradient;
            gradient.topLeft = DefaultGradientTop;
            gradient.topRight = DefaultGradientTop;
            gradient.bottomLeft = DefaultGradientBottom;
            gradient.bottomRight = DefaultGradientBottom;
            AssetDatabase.CreateAsset(gradient, WarmGradientPath);
        }

        private static void ValidateFontMaterial(
            string fontPath,
            string materialPath,
            Shader shader,
            ICollection<string> errors)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                errors.Add($"Missing supported TMP pixel font asset: {fontPath}");
                return;
            }
            if (font.atlasTexture == null)
            {
                errors.Add($"TMP pixel font has no atlas texture: {fontPath}");
                return;
            }
            if (font.atlasTexture.filterMode != FilterMode.Point)
            {
                errors.Add($"TMP pixel font atlas must use Point filtering: {fontPath}");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                errors.Add($"Missing TMP pixel font material preset: {materialPath}");
                return;
            }
            if (material.shader != shader)
            {
                errors.Add($"TMP pixel font material uses the wrong shader: {materialPath}");
            }
            if (material.GetTexture("_MainTex") != font.atlasTexture)
            {
                errors.Add($"TMP pixel font material uses the wrong atlas: {materialPath}");
            }
            if (!material.HasProperty("_OutlineWidth") ||
                !material.HasProperty("_Padding"))
            {
                errors.Add($"TMP pixel font material is missing outline properties: {materialPath}");
                return;
            }

            float outlineWidth = material.GetFloat("_OutlineWidth");
            if (outlineWidth < 0f || outlineWidth > 2f)
            {
                errors.Add($"TMP pixel font outline width must be from 0 to 2: {materialPath}");
            }
            if (material.GetFloat("_Padding") < outlineWidth)
            {
                errors.Add($"TMP pixel font mesh padding is smaller than its outline: {materialPath}");
            }
        }

        private static void EnsureAssetFolder(string assetFolderPath)
        {
            string[] segments = assetFolderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Missing required asset: {assetPath}");
            }
            return asset;
        }

        private static void CreatePreviewCameraAndLight()
        {
            var cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.024f, 0.035f, 1f);
            camera.orthographic = true;

            var lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0f;
        }

        private static RectTransform CreatePreviewCanvas()
        {
            var canvasObject = new GameObject(
                "PixelFontPreviewCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            BombSwap.PrototypeUiFactory.ConfigureCanvasScaler(
                canvasObject.GetComponent<CanvasScaler>());
            return canvasObject.GetComponent<RectTransform>();
        }

        private static void CreatePanel(RectTransform canvas)
        {
            var panelObject = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(canvas, false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color =
                new Color(0.025f, 0.035f, 0.055f, 1f);
        }

        private static TextMeshProUGUI CreatePreviewText(
            string objectName,
            Transform parent,
            string value,
            TMP_FontAsset font,
            Material material,
            TMP_ColorGradient gradient,
            float fontSize,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSharedMaterial = material;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.enableVertexGradient = gradient != null;
            text.colorGradientPreset = gradient;
            return text;
        }

        private static void CreateMaskPreview(
            Transform canvas,
            TMP_FontAsset font,
            Material material,
            TMP_ColorGradient gradient)
        {
            var maskObject = new GameObject(
                "RectMaskPreview",
                typeof(RectTransform),
                typeof(RectMask2D));
            RectTransform mask = maskObject.GetComponent<RectTransform>();
            mask.SetParent(canvas, false);
            mask.anchorMin = new Vector2(0.5f, 0.5f);
            mask.anchorMax = new Vector2(0.5f, 0.5f);
            mask.pivot = new Vector2(0.5f, 0.5f);
            mask.anchoredPosition = new Vector2(0f, -120f);
            mask.sizeDelta = new Vector2(700f, 62f);

            CreatePreviewText(
                "ClippedSample",
                mask,
                "UI MASK  -  외곽선도 패널 경계에서 정확히 잘려야 합니다",
                font,
                material,
                gradient,
                30f,
                new Vector2(170f, 0f),
                new Vector2(980f, 62f));
        }
    }
}
