using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeBgmAuthoring
    {
        public const string CatalogAssetPath =
            "Assets/Game/Content/Audio/PrototypeBgmCatalog.asset";
        public const string LobbyClipPath =
            "Assets/Game/Content/Audio/Music/BGM_Lobby_GooseExodus_8Bit_Loop.wav";
        public const string DungeonBaseClipPath =
            "Assets/Game/Content/Audio/Music/BGM_Dungeon_PowderCorridor_BaseLayer_8Bit_Loop.wav";
        public const string DungeonCombatClipPath =
            "Assets/Game/Content/Audio/Music/BGM_Dungeon_PowderCorridor_CombatLayer_8Bit_Loop.wav";
        public const string DungeonDangerClipPath =
            "Assets/Game/Content/Audio/Music/BGM_Dungeon_PowderCorridor_DangerLayer_8Bit_Loop.wav";
        public const string DungeonSanctuaryClipPath =
            "Assets/Game/Content/Audio/Music/BGM_Dungeon_PowderCorridor_SanctuaryLayer_8Bit_Loop.wav";
        public const string BossBaseClipPath =
            "Assets/Game/Content/Audio/Music/BGM_BossBattle_OverheatedThrone_BaseLayer_8Bit_Loop.wav";
        public const string BossGrandClipPath =
            "Assets/Game/Content/Audio/Music/BGM_BossBattle_OverheatedThrone_GrandLayer_8Bit_Loop.wav";
        public const string BossDangerClipPath =
            "Assets/Game/Content/Audio/Music/BGM_BossBattle_OverheatedThrone_DangerLayer_8Bit_Loop.wav";

        [MenuItem("Bomb Swap/Prototype/Apply BGM Integration")]
        public static void ApplyBgmIntegration()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "BGM integration cannot be authored while Unity is in Play Mode.");
            }

            EnsureLoadedTargetScenesAreClean();
            PrototypeBgmCatalogAsset catalog = EnsureCatalog();
            int changedSceneCount = 0;
            string[] scenePaths = PrototypeContentValidator.BgmScenePaths;
            for (int index = 0; index < scenePaths.Length; index++)
            {
                if (EnsureScenePresenter(scenePaths[index], catalog))
                {
                    changedSceneCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Bomb Swap BGM integration applied. Catalog: {CatalogAssetPath}; changed scenes: {changedSceneCount}.");
        }

        private static PrototypeBgmCatalogAsset EnsureCatalog()
        {
            AudioMixer mixer = RequireAsset<AudioMixer>(
                PrototypeContentValidator.AudioMixerPath);
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("BGM")
                .Where(group => string.Equals(group.name, "BGM", StringComparison.Ordinal))
                .ToArray();
            if (groups.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Prototype AudioMixer must expose exactly one BGM group, found {groups.Length}.");
            }

            PrototypeBgmCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeBgmCatalogAsset>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PrototypeBgmCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            Undo.RecordObject(catalog, "Configure Bomb Swap BGM Catalog");
            catalog.Configure(
                groups[0],
                RequireAsset<AudioClip>(LobbyClipPath),
                RequireAsset<AudioClip>(DungeonBaseClipPath),
                RequireAsset<AudioClip>(DungeonCombatClipPath),
                RequireAsset<AudioClip>(DungeonDangerClipPath),
                RequireAsset<AudioClip>(DungeonSanctuaryClipPath),
                RequireAsset<AudioClip>(BossBaseClipPath),
                RequireAsset<AudioClip>(BossGrandClipPath),
                RequireAsset<AudioClip>(BossDangerClipPath));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static bool EnsureScenePresenter(
            string scenePath,
            PrototypeBgmCatalogAsset catalog)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException($"Missing BGM target scene '{scenePath}'.");
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForAuthoring = !scene.IsValid() || !scene.isLoaded;
            if (openedForAuthoring)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                PrototypeBgmPresenter[] presenters = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<PrototypeBgmPresenter>(true))
                    .ToArray();
                if (presenters.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scenePath}' contains {presenters.Length} BGM presenters; expected at most one before authoring.");
                }

                bool changed = false;
                PrototypeBgmPresenter presenter;
                if (presenters.Length == 0)
                {
                    var root = new GameObject("PrototypeBgm");
                    SceneManager.MoveGameObjectToScene(root, scene);
                    presenter = root.AddComponent<PrototypeBgmPresenter>();
                    Undo.RegisterCreatedObjectUndo(root, "Create Bomb Swap BGM Presenter");
                    changed = true;
                }
                else
                {
                    presenter = presenters[0];
                }

                if (presenter.transform.parent != null)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scenePath}' BGM presenter must be a scene root.");
                }
                if (presenter.Catalog != catalog)
                {
                    Undo.RecordObject(presenter, "Configure Bomb Swap BGM Presenter");
                    presenter.Configure(catalog);
                    EditorUtility.SetDirty(presenter);
                    changed = true;
                }

                if (!changed)
                {
                    return false;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save BGM target scene '{scenePath}'.");
                }
                return true;
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void EnsureLoadedTargetScenesAreClean()
        {
            string[] targets = PrototypeContentValidator.BgmScenePaths;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty && targets.Contains(scene.path, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Save or discard existing changes in '{scene.path}' before applying BGM integration.");
                }
            }
        }

        private static T RequireAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Required BGM asset '{assetPath}' is missing or has the wrong type.");
            }
            return asset;
        }
    }
}
