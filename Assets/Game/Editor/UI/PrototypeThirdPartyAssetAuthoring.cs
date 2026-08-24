using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class PrototypeThirdPartyAssetAuthoring
    {
        public const string ThirdPartyRoot = "Assets/ThirdParty";
        public const string LocalSkinAssetPath =
            "Assets/ThirdParty/BombSwap/Resources/BombSwap/ThirdPartyUiSkin.asset";

        private static readonly string[] PrivateVendorAssetPrefixes =
        {
            ThirdPartyRoot + "/",
            "Assets/Feel/",
            "Assets/Plugins/Demigiant/DOTweenPro/",
            "Assets/Arts/VFX/"
        };

        private static readonly string[] PrivateVendorAssetPaths =
        {
            "Assets/Feel.meta",
            "Assets/Plugins/Demigiant/DOTweenPro.meta",
            "Assets/Plugins/Demigiant/readme_DOTweenPro.txt",
            "Assets/Plugins/Demigiant/readme_DOTweenPro.txt.meta",
            "Assets/Arts/VFX.meta"
        };

        private const string ObsoleteFeelDefine =
            "MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED";

        private static readonly BuildTargetGroup[] SupportedBuildTargetGroups =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.Standalone,
            BuildTargetGroup.WebGL
        };

        private const string BlackAndWhiteTexturePath =
            "Assets/ThirdParty/UI/BlackandWhiteUI.png/BlackandWhiteUI.png";
        private const string PixelariumBannerTexturePath =
            "Assets/ThirdParty/UI/Pixelarium - Interfaces Bundle - Full version/" +
            "Pixelarium - Interfaces Bundle - Full version/Pack Content/" +
            "Zelda-Like Interface/GameplayHud/spr_banner_hud_zeldalike.png";
        private const string LobbyBackgroundTexturePath =
            "Assets/ThirdParty/UI/CreateAI/Lobby/Lobby_BackGround.png";

        private const string LobbyBackgroundSpriteName = "Lobby_BackGround";
        private const string NavigationArrowLeftSpriteName =
            "BlackandWhiteUI_271";
        private const string NavigationArrowRightSpriteName =
            "BlackandWhiteUI_270";
        private const string SettingsPanelSpriteName = "BlackandWhiteUI_117";
        private const string SettingsButtonSpriteName =
            "spr_banner_hud_zeldalike";
        private const string SettingsSliderBackgroundSpriteName =
            "BlackandWhiteUI_280";
        private const string SettingsSliderFillSpriteName =
            "BlackandWhiteUI_276";

        internal static readonly Vector2 SettingsPanelSpritePixelSize =
            new Vector2(87f, 77f);

        [MenuItem("Bomb Swap/Third Party/Legacy/Create or Update Local UI Skin")]
        private static void CreateOrUpdateLocalSkinFromMenu()
        {
            EnsureNotPlaying();
            PrototypeOptionalUiSkin skin = CreateOrUpdateLocalSkin();
            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
            Debug.Log(
                $"Local optional UI skin ready at '{LocalSkinAssetPath}'. " +
                "This asset remains inside the ignored third-party package.");
        }

        [MenuItem("Bomb Swap/Third Party/Legacy/Configure Public UI Fallbacks")]
        private static void ConfigurePublicFallbacksFromMenu()
        {
            EnsureNotPlaying();
            ConfigureLobbyPublicFallback();
            ConfigurePausePublicFallback();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Public UI fallbacks configured without changing authored " +
                "RectTransforms, colors, or Image types.");
        }

        [MenuItem("Bomb Swap/Third Party/Validate Public References")]
        private static void ValidatePublicReferencesFromMenu()
        {
            var errors = new List<string>();
            ValidatePublicDependencies(errors);
            ValidateOptionalUiBindings(errors);
            ValidateNoObsoleteVendorDefines(errors);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            Debug.Log(
                "Private source files remain outside Git, direct UI Sprite " +
                "references use fallback components, and unsupported vendor " +
                "dependencies were not found.");
        }

        [MenuItem(
            "Tools/Bomb Swap/Third Party/Remove Obsolete Vendor Defines")]
        public static void RemoveObsoleteVendorDefines()
        {
            EnsureNotPlaying();
            int changedTargetCount = 0;
            for (int index = 0;
                 index < SupportedBuildTargetGroups.Length;
                 index++)
            {
                NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(
                    SupportedBuildTargetGroups[index]);
                string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
                string updated = RemoveDefine(symbols, ObsoleteFeelDefine);
                if (string.Equals(
                        symbols,
                        updated,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                PlayerSettings.SetScriptingDefineSymbols(target, updated);
                changedTargetCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Removed obsolete private-vendor defines from " +
                $"{changedTargetCount} build target(s).");
        }

        [MenuItem("Bomb Swap/Third Party/Export Local Assets Package")]
        private static void ExportLocalAssetsPackageFromMenu()
        {
            EnsureNotPlaying();
            if (!AssetDatabase.IsValidFolder(ThirdPartyRoot))
            {
                throw new InvalidOperationException(
                    $"Third-party source folder is missing: {ThirdPartyRoot}");
            }

            string outputDirectory = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "ExternalAssets",
                    "UI-Packages"));
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(
                outputDirectory,
                $"BombSwap-ThirdParty-{DateTime.UtcNow:yyyyMMdd-HHmm}.unitypackage");
            AssetDatabase.ExportPackage(
                ThirdPartyRoot,
                outputPath,
                ExportPackageOptions.Recurse);
            Debug.Log($"Exported local third-party package: {outputPath}");
        }

        internal static void ValidatePublicDependencies(
            ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            PrototypeDirectThirdPartyUiAuthoring
                .ValidateSupportedDependencies(errors);
        }

        internal static void ValidateNoObsoleteVendorDefines(
            ICollection<string> errors)
        {
            for (int index = 0;
                 index < SupportedBuildTargetGroups.Length;
                 index++)
            {
                BuildTargetGroup group = SupportedBuildTargetGroups[index];
                NamedBuildTarget target =
                    NamedBuildTarget.FromBuildTargetGroup(group);
                string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
                if (symbols.Split(';').Any(symbol => string.Equals(
                        symbol.Trim(),
                        ObsoleteFeelDefine,
                        StringComparison.Ordinal)))
                {
                    errors.Add(
                        $"Build target '{group}' still declares removed vendor " +
                        $"define '{ObsoleteFeelDefine}'.");
                }
            }
        }

        private static bool IsPrivateVendorAsset(string assetPath)
        {
            for (int index = 0;
                 index < PrivateVendorAssetPrefixes.Length;
                 index++)
            {
                if (assetPath.StartsWith(
                        PrivateVendorAssetPrefixes[index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            for (int index = 0;
                 index < PrivateVendorAssetPaths.Length;
                 index++)
            {
                if (string.Equals(
                        assetPath,
                        PrivateVendorAssetPaths[index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RemoveDefine(string symbols, string define)
        {
            return string.Join(
                ";",
                symbols.Split(';')
                    .Select(symbol => symbol.Trim())
                    .Where(symbol =>
                        !string.IsNullOrEmpty(symbol) &&
                        !string.Equals(
                            symbol,
                            define,
                            StringComparison.Ordinal)));
        }

        internal static void ValidateOptionalUiBindings(
            ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            PrototypeDirectThirdPartyUiAuthoring
                .ValidateDirectSpriteSlots(errors);
        }

        internal static bool HasPublicSettingsPanelConfiguration(
            Image image)
        {
            return PrototypeDirectThirdPartyUiAuthoring
                .HasSettingsPanelConfiguration(image);
        }

        internal static bool HasExpectedBindings(
            PrototypeOptionalUiSkinApplicator applicator,
            bool includesLobbyBackground)
        {
            if (applicator == null ||
                !applicator.HasValidBindings ||
                !applicator.UsesPublicFallback)
            {
                return false;
            }

            var expectedCounts = new Dictionary<PrototypeUiSpriteRole, int>
            {
                [PrototypeUiSpriteRole.NavigationArrowLeft] = 2,
                [PrototypeUiSpriteRole.NavigationArrowRight] = 2,
                [PrototypeUiSpriteRole.SettingsPanelFrame] = 1,
                [PrototypeUiSpriteRole.SettingsButtonFrame] = 3,
                [PrototypeUiSpriteRole.SettingsSliderBackground] = 4,
                [PrototypeUiSpriteRole.SettingsSliderFill] = 4
            };
            if (includesLobbyBackground)
            {
                expectedCounts[PrototypeUiSpriteRole.LobbyBackground] = 1;
            }

            var actualCounts = new Dictionary<PrototypeUiSpriteRole, int>();
            for (int index = 0;
                 index < applicator.BindingCount;
                 index++)
            {
                PrototypeUiSpriteRole role =
                    applicator.GetBinding(index).Role;
                actualCounts.TryGetValue(role, out int count);
                actualCounts[role] = count + 1;
            }

            return actualCounts.Count == expectedCounts.Count &&
                expectedCounts.All(pair =>
                    actualCounts.TryGetValue(pair.Key, out int count) &&
                    count == pair.Value);
        }

        private static PrototypeOptionalUiSkin CreateOrUpdateLocalSkin()
        {
            EnsureAssetFolder(
                "Assets/ThirdParty/BombSwap/Resources/BombSwap");
            PrototypeOptionalUiSkin skin =
                AssetDatabase.LoadAssetAtPath<PrototypeOptionalUiSkin>(
                    LocalSkinAssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<PrototypeOptionalUiSkin>();
                AssetDatabase.CreateAsset(skin, LocalSkinAssetPath);
            }

            skin.Configure(new[]
            {
                CreateEntry(
                    PrototypeUiSpriteRole.LobbyBackground,
                    LobbyBackgroundTexturePath,
                    LobbyBackgroundSpriteName),
                CreateEntry(
                    PrototypeUiSpriteRole.NavigationArrowLeft,
                    BlackAndWhiteTexturePath,
                    NavigationArrowLeftSpriteName),
                CreateEntry(
                    PrototypeUiSpriteRole.NavigationArrowRight,
                    BlackAndWhiteTexturePath,
                    NavigationArrowRightSpriteName),
                CreateEntry(
                    PrototypeUiSpriteRole.SettingsPanelFrame,
                    BlackAndWhiteTexturePath,
                    SettingsPanelSpriteName),
                CreateEntry(
                    PrototypeUiSpriteRole.SettingsButtonFrame,
                    PixelariumBannerTexturePath,
                    SettingsButtonSpriteName),
                CreateEntry(
                    PrototypeUiSpriteRole.SettingsSliderBackground,
                    BlackAndWhiteTexturePath,
                    SettingsSliderBackgroundSpriteName),
                CreateEntry(
                    PrototypeUiSpriteRole.SettingsSliderFill,
                    BlackAndWhiteTexturePath,
                    SettingsSliderFillSpriteName)
            });
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        private static PrototypeOptionalUiSkin.SpriteEntry CreateEntry(
            PrototypeUiSpriteRole role,
            string texturePath,
            string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .SingleOrDefault(candidate => string.Equals(
                    candidate.name,
                    spriteName,
                    StringComparison.Ordinal));
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Sprite '{spriteName}' was not found at " +
                    $"'{texturePath}'. Import the approved local package first.");
            }

            return new PrototypeOptionalUiSkin.SpriteEntry(role, sprite);
        }

        private static void ConfigureLobbyPublicFallback()
        {
            Scene scene = SceneManager.GetSceneByPath(
                PrototypeContentValidator.LobbyScenePath);
            bool openedForAuthoring = !scene.IsValid() || !scene.isLoaded;
            int loadedSceneCount = SceneManager.sceneCount;
            if (openedForAuthoring)
            {
                scene = EditorSceneManager.OpenScene(
                    PrototypeContentValidator.LobbyScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                PrototypeLobbyPresenter[] presenters = scene
                    .GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeLobbyPresenter>(true))
                    .ToArray();
                if (presenters.Length != 1 ||
                    presenters[0].LobbyCanvas == null)
                {
                    throw new InvalidOperationException(
                        "Lobby scene requires one presenter with an authored Canvas.");
                }

                ConfigureApplicator(
                    presenters[0].LobbyCanvas.gameObject,
                    true);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the lobby public fallback.");
                }
            }
            finally
            {
                if (openedForAuthoring &&
                    loadedSceneCount > 0 &&
                    scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ConfigurePausePublicFallback()
        {
            string path = PrototypeInGameUiPrefabAuthoring.PausePrefabPath;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PrototypePauseView view =
                    root.GetComponent<PrototypePauseView>();
                if (view == null)
                {
                    throw new InvalidOperationException(
                        $"Pause prefab view is missing at '{path}'.");
                }

                ConfigureApplicator(view.gameObject, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureApplicator(
            GameObject owner,
            bool includesLobbyBackground)
        {
            PrototypeOptionalUiSkinApplicator applicator =
                owner.GetComponent<PrototypeOptionalUiSkinApplicator>();
            if (applicator == null)
            {
                applicator =
                    owner.AddComponent<PrototypeOptionalUiSkinApplicator>();
            }

            var bindings = new List<
                PrototypeOptionalUiSkinApplicator.SpriteBinding>();
            if (applicator.HasValidBindings)
            {
                for (int index = 0;
                     index < applicator.BindingCount;
                     index++)
                {
                    bindings.Add(applicator.GetBinding(index));
                }
            }
            else
            {
                Image[] images = owner.GetComponentsInChildren<Image>(true);
                for (int index = 0; index < images.Length; index++)
                {
                    Image image = images[index];
                    if (image.sprite == null ||
                        !TryResolveRole(
                            image.sprite,
                            out PrototypeUiSpriteRole role))
                    {
                        continue;
                    }

                    if (!includesLobbyBackground &&
                        role == PrototypeUiSpriteRole.LobbyBackground)
                    {
                        continue;
                    }

                    bool hideWhenMissing =
                        role == PrototypeUiSpriteRole.NavigationArrowLeft ||
                        role == PrototypeUiSpriteRole.NavigationArrowRight;
                    bindings.Add(
                        new PrototypeOptionalUiSkinApplicator.SpriteBinding(
                            role,
                            image,
                            hideWhenMissing));
                }
            }

            applicator.ConfigureBindings(bindings.ToArray());
            applicator.ApplyPublicFallback();
            EditorUtility.SetDirty(applicator);
            for (int index = 0; index < bindings.Count; index++)
            {
                EditorUtility.SetDirty(bindings[index].Target);
            }

            if (!HasExpectedBindings(applicator, includesLobbyBackground))
            {
                throw new InvalidOperationException(
                    $"Optional UI bindings on '{owner.name}' do not match " +
                    "the approved public fallback contract.");
            }
        }

        private static bool TryResolveRole(
            Sprite sprite,
            out PrototypeUiSpriteRole role)
        {
            switch (sprite.name)
            {
                case LobbyBackgroundSpriteName:
                    role = PrototypeUiSpriteRole.LobbyBackground;
                    return true;
                case NavigationArrowLeftSpriteName:
                    role = PrototypeUiSpriteRole.NavigationArrowLeft;
                    return true;
                case NavigationArrowRightSpriteName:
                    role = PrototypeUiSpriteRole.NavigationArrowRight;
                    return true;
                case SettingsPanelSpriteName:
                    role = PrototypeUiSpriteRole.SettingsPanelFrame;
                    return true;
                case SettingsButtonSpriteName:
                    role = PrototypeUiSpriteRole.SettingsButtonFrame;
                    return true;
                case SettingsSliderBackgroundSpriteName:
                    role = PrototypeUiSpriteRole.SettingsSliderBackground;
                    return true;
                case SettingsSliderFillSpriteName:
                    role = PrototypeUiSpriteRole.SettingsSliderFill;
                    return true;
                default:
                    role = default;
                    return false;
            }
        }

        private static void ValidateApplicator(
            PrototypeOptionalUiSkinApplicator applicator,
            bool includesLobbyBackground,
            string ownerPath,
            ICollection<string> errors)
        {
            if (!HasExpectedBindings(applicator, includesLobbyBackground))
            {
                errors.Add(
                    $"Optional UI skin bindings or public fallbacks are invalid: " +
                    ownerPath);
            }
        }

        internal static bool TryGetIntegerSettingsPanelScale(
            RectTransform rect,
            out int displayScale)
        {
            displayScale = 0;
            if (rect == null)
            {
                return false;
            }

            Vector2 displayedSize = rect.rect.size;
            float widthScale =
                displayedSize.x / SettingsPanelSpritePixelSize.x;
            float heightScale =
                displayedSize.y / SettingsPanelSpritePixelSize.y;
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

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static void EnsureNotPlaying()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Third-party asset authoring is unavailable in Play Mode.");
            }
        }
    }
}
