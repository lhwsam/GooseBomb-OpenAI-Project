using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class PrototypeDirectThirdPartyUiAuthoring
    {
        private const int MinimumLobbySlotCount = 17;
        private const int MinimumPauseSlotCount = 16;

        private static readonly string[] UnsupportedPrivatePrefixes =
        {
            "Assets/Feel/",
            "Assets/Plugins/Demigiant/DOTweenPro/",
            "Assets/Arts/VFX/"
        };

        private static readonly string[] UnsupportedPrivatePaths =
        {
            "Assets/Feel.meta",
            "Assets/Plugins/Demigiant/DOTweenPro.meta",
            "Assets/Plugins/Demigiant/readme_DOTweenPro.txt",
            "Assets/Plugins/Demigiant/readme_DOTweenPro.txt.meta",
            "Assets/Arts/VFX.meta"
        };

        [MenuItem(
            "Bomb Swap/Third Party/Migrate Lobby and Pause to Direct Sprite References")]
        private static void MigrateLegacyOptionalUiFromMenu()
        {
            EnsureNotPlaying();
            PrototypeOptionalUiSkin skin =
                AssetDatabase.LoadAssetAtPath<PrototypeOptionalUiSkin>(
                    PrototypeThirdPartyAssetAuthoring.LocalSkinAssetPath);
            if (skin == null || !skin.HasValidEntries)
            {
                throw new InvalidOperationException(
                    "The approved local package and its legacy UI skin are " +
                    "required for this one-time migration.");
            }

            int migratedCount = MigrateLobby(skin) + MigratePause(skin);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Migrated {migratedCount} optional UI Sprite slot(s) to " +
                "direct references with per-Image runtime fallbacks. Authored " +
                "hierarchy, RectTransforms, colors, and Image types were preserved.");
        }

        internal static void ValidateSupportedDependencies(
            ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string ownerPath in assetPaths)
            {
                if (!ownerPath.StartsWith(
                        "Assets/Game/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] dependencies =
                    AssetDatabase.GetDependencies(ownerPath, false);
                for (int index = 0; index < dependencies.Length; index++)
                {
                    string dependency = dependencies[index];
                    if (IsUnsupportedPrivateAsset(dependency))
                    {
                        errors.Add(
                            $"Public asset '{ownerPath}' directly references " +
                            $"unsupported private vendor asset '{dependency}'.");
                        continue;
                    }

                    if (IsThirdPartyAsset(dependency) &&
                        !ContainsSprite(dependency))
                    {
                        errors.Add(
                            $"Public asset '{ownerPath}' directly references " +
                            $"non-Sprite third-party asset '{dependency}'. " +
                            "Only optional UI Sprite textures are allowed.");
                    }
                }
            }
        }

        internal static void ValidateDirectSpriteSlots(
            ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            PrototypePauseView pause =
                AssetDatabase.LoadAssetAtPath<PrototypePauseView>(
                    PrototypeInGameUiPrefabAuthoring.PausePrefabPath);
            if (pause == null ||
                !HasExpectedDirectSpriteSlots(
                    pause.gameObject,
                    MinimumPauseSlotCount))
            {
                errors.Add(
                    "Pause UI direct Sprite slots or runtime fallbacks are " +
                    $"invalid: {PrototypeInGameUiPrefabAuthoring.PausePrefabPath}");
            }

            ValidateLobbyDirectSpriteSlots(errors);
        }

        internal static bool HasSettingsPanelConfiguration(Image image)
        {
            if (image == null ||
                image.type != Image.Type.Simple ||
                image.preserveAspect)
            {
                return false;
            }

            return PrototypeThirdPartyAssetAuthoring
                .TryGetIntegerSettingsPanelScale(
                    image.rectTransform,
                    out _);
        }

        internal static bool HasExpectedDirectSpriteSlots(
            GameObject owner,
            int expectedMinimumCount)
        {
            if (owner == null || expectedMinimumCount < 1 ||
                owner.GetComponent<PrototypeOptionalUiSkinApplicator>() != null)
            {
                return false;
            }

            PrototypeOptionalSpriteFallback[] slots =
                owner.GetComponentsInChildren<PrototypeOptionalSpriteFallback>(
                    true);
            if (slots.Length < expectedMinimumCount)
            {
                return false;
            }

            var targets = new HashSet<Image>();
            for (int index = 0; index < slots.Length; index++)
            {
                PrototypeOptionalSpriteFallback slot = slots[index];
                Image target = slot != null ? slot.TargetImage : null;
                if (target == null ||
                    !target.transform.IsChildOf(owner.transform) ||
                    !targets.Add(target))
                {
                    return false;
                }
            }

            Image[] images = owner.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                if (IsThirdPartyAsset(spritePath) &&
                    image.GetComponent<PrototypeOptionalSpriteFallback>() == null)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int MinimumLobbySlots => MinimumLobbySlotCount;

        private static void ValidateLobbyDirectSpriteSlots(
            ICollection<string> errors)
        {
            Scene scene = SceneManager.GetSceneByPath(
                PrototypeContentValidator.LobbyScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (openedForValidation)
                {
                    scene = EditorSceneManager.OpenScene(
                        PrototypeContentValidator.LobbyScenePath,
                        OpenSceneMode.Additive);
                }

                PrototypeLobbyPresenter[] presenters = scene
                    .GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeLobbyPresenter>(true))
                    .ToArray();
                if (presenters.Length != 1 ||
                    presenters[0].LobbyCanvas == null ||
                    !HasExpectedDirectSpriteSlots(
                        presenters[0].LobbyCanvas.gameObject,
                        MinimumLobbySlotCount))
                {
                    errors.Add(
                        "Lobby UI direct Sprite slots or runtime fallbacks " +
                        $"are invalid: {PrototypeContentValidator.LobbyScenePath}");
                }
            }
            catch (Exception exception)
            {
                errors.Add(
                    "Lobby UI direct Sprite validation failed: " +
                    exception.Message);
            }
            finally
            {
                if (openedForValidation && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static int MigrateLobby(PrototypeOptionalUiSkin skin)
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

                int migratedCount = MigrateOwner(
                    presenters[0].LobbyCanvas.gameObject,
                    skin);
                if (migratedCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException(
                            "Unity failed to save the migrated lobby scene.");
                    }
                }

                return migratedCount;
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

        private static int MigratePause(PrototypeOptionalUiSkin skin)
        {
            string path = PrototypeInGameUiPrefabAuthoring.PausePrefabPath;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PrototypePauseView view = root.GetComponent<PrototypePauseView>();
                if (view == null)
                {
                    throw new InvalidOperationException(
                        $"Pause prefab view is missing at '{path}'.");
                }

                int migratedCount = MigrateOwner(view.gameObject, skin);
                if (migratedCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                return migratedCount;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int MigrateOwner(
            GameObject owner,
            PrototypeOptionalUiSkin skin)
        {
            PrototypeOptionalUiSkinApplicator applicator =
                owner.GetComponent<PrototypeOptionalUiSkinApplicator>();
            if (applicator == null)
            {
                return 0;
            }
            if (!applicator.HasValidBindings)
            {
                throw new InvalidOperationException(
                    $"Legacy optional UI bindings are invalid on '{owner.name}'.");
            }

            int migratedCount = 0;
            for (int index = 0; index < applicator.BindingCount; index++)
            {
                PrototypeOptionalUiSkinApplicator.SpriteBinding binding =
                    applicator.GetBinding(index);
                if (!skin.TryGetSprite(binding.Role, out Sprite sprite))
                {
                    throw new InvalidOperationException(
                        $"Local skin is missing Sprite role '{binding.Role}'.");
                }

                Image target = binding.Target;
                target.sprite = sprite;
                target.enabled = true;
                PrototypeOptionalSpriteFallback fallback =
                    target.GetComponent<PrototypeOptionalSpriteFallback>();
                if (fallback == null)
                {
                    fallback = target.gameObject.AddComponent<
                        PrototypeOptionalSpriteFallback>();
                }
                fallback.Configure(null, binding.HideWhenMissing);
                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(fallback);
                migratedCount++;
            }

            UnityEngine.Object.DestroyImmediate(applicator);
            return migratedCount;
        }

        private static bool ContainsSprite(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .Any();
        }

        private static bool IsThirdPartyAsset(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                assetPath.StartsWith(
                    PrototypeThirdPartyAssetAuthoring.ThirdPartyRoot + "/",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnsupportedPrivateAsset(string assetPath)
        {
            for (int index = 0;
                 index < UnsupportedPrivatePrefixes.Length;
                 index++)
            {
                if (assetPath.StartsWith(
                        UnsupportedPrivatePrefixes[index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            for (int index = 0;
                 index < UnsupportedPrivatePaths.Length;
                 index++)
            {
                if (string.Equals(
                        assetPath,
                        UnsupportedPrivatePaths[index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureNotPlaying()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Third-party UI migration is unavailable in Play Mode.");
            }
        }
    }
}
