using System;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class LobbySettingsPanelSpriteAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/UI/Configure Lobby Settings Panel Sprite";
        internal const string TexturePath =
            "Assets/ThirdParty/UI/BlackandWhiteUI.png/BlackandWhiteUI.png";
        internal const string SpriteName = "BlackandWhiteUI_117";
        private const float PixelsPerUnitMultiplier = 1f;
        internal static readonly Vector2 SpritePixelSize =
            new Vector2(87f, 77f);

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Lobby settings panel cannot be authored in Play Mode.");
            }

            ConfigureTextureImporter();

            Scene scene = SceneManager.GetSceneByPath(
                PrototypeContentValidator.LobbyScenePath);
            bool openedForAuthoring = !scene.IsValid() || !scene.isLoaded;
            if (openedForAuthoring)
            {
                scene = EditorSceneManager.OpenScene(
                    PrototypeContentValidator.LobbyScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                Image panelImage = FindPanelImage(scene);
                Sprite panelSprite = LoadPanelSprite();
                RectTransform panelRect = panelImage.rectTransform;
                if (panelSprite.rect.size != SpritePixelSize)
                {
                    throw new InvalidOperationException(
                        $"Expected {SpriteName} to be {SpritePixelSize}, " +
                        $"found {panelSprite.rect.size}.");
                }

                Undo.RecordObjects(
                    new UnityEngine.Object[] { panelImage },
                    "Configure Settings Panel Pixel Art");

                panelImage.sprite = panelSprite;
                panelImage.type = Image.Type.Simple;
                panelImage.preserveAspect = false;
                panelImage.fillCenter = true;
                panelImage.pixelsPerUnitMultiplier = PixelsPerUnitMultiplier;
                EditorUtility.SetDirty(panelImage);
                EditorSceneManager.MarkSceneDirty(scene);

                if (openedForAuthoring &&
                    !EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the lobby scene.");
                }

                string scaleDescription = TryGetIntegerDisplayScale(
                    panelRect,
                    out int displayScale)
                    ? $"integer scale {displayScale}"
                    : $"designer size {panelRect.rect.size}";
                Debug.Log(
                    $"Lobby settings panel ready: {SpriteName}, " +
                    $"simple, {scaleDescription}, " +
                    $"PPU multiplier {PixelsPerUnitMultiplier}. " +
                    "The authored RectTransform was preserved.");
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ConfigureTextureImporter()
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Texture importer was not found at {TexturePath}.");
            }

            bool requiresReimport =
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled ||
                importer.textureCompression !=
                    TextureImporterCompression.Uncompressed ||
                importer.wrapMode != TextureWrapMode.Clamp;
            if (!requiresReimport)
            {
                return;
            }

            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static Sprite LoadPanelSprite()
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(TexturePath)
                .OfType<Sprite>()
                .SingleOrDefault(candidate => string.Equals(
                    candidate.name,
                    SpriteName,
                    StringComparison.Ordinal));
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Sprite {SpriteName} was not loaded from {TexturePath}.");
            }

            return sprite;
        }

        internal static bool HasPixelPerfectConfiguration(Image image)
        {
            if (image == null || image.sprite == null ||
                !string.Equals(
                    image.sprite.name,
                    SpriteName,
                    StringComparison.Ordinal) ||
                image.type != Image.Type.Simple ||
                image.preserveAspect ||
                !Mathf.Approximately(
                    image.pixelsPerUnitMultiplier,
                    PixelsPerUnitMultiplier))
            {
                return false;
            }

            RectTransform rect = image.rectTransform;
            return TryGetIntegerDisplayScale(rect, out _);
        }

        internal static bool TryGetIntegerDisplayScale(
            RectTransform rect,
            out int displayScale)
        {
            displayScale = 0;
            if (rect == null)
            {
                return false;
            }

            Vector2 displayedSize = rect.rect.size;
            float widthScale = displayedSize.x / SpritePixelSize.x;
            float heightScale = displayedSize.y / SpritePixelSize.y;
            int roundedScale = Mathf.RoundToInt(widthScale);
            if (roundedScale < 1 ||
                !Mathf.Approximately(widthScale, roundedScale) ||
                !Mathf.Approximately(heightScale, roundedScale))
            {
                return false;
            }

            displayScale = roundedScale;
            return true;
        }

        internal static bool HasPixelPerfectImporterConfiguration()
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            return importer != null &&
                importer.filterMode == FilterMode.Point &&
                !importer.mipmapEnabled &&
                importer.textureCompression ==
                    TextureImporterCompression.Uncompressed &&
                importer.wrapMode == TextureWrapMode.Clamp;
        }

        private static Image FindPanelImage(Scene scene)
        {
            PrototypeLobbyPresenter[] presenters = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<PrototypeLobbyPresenter>(true))
                .ToArray();

            if (presenters.Length != 1 || presenters[0].SettingsPanel == null)
            {
                throw new InvalidOperationException(
                    $"Expected one configured PrototypeLobbyPresenter in " +
                    $"{scene.path}, but found {presenters.Length}.");
            }

            Image image = presenters[0].SettingsPanel.GetComponent<Image>();
            if (image == null)
            {
                throw new InvalidOperationException(
                    "The authored settings panel requires an Image component.");
            }

            return image;
        }
    }
}
