using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace BombSwap.Editor.Verification
{
    public static class WebGLReleaseBuildPolicy
    {
        private const string TestSceneDirectory = "/Scenes/TestSandbox/";
        private const string CombatRoomCatalogPath =
            "Assets/Game/Content/Rooms/PrototypeDungeonCombatRoomCatalog.asset";

        public static string[] GetEnabledReleaseScenePaths()
        {
            PrototypeDungeonCombatRoomCatalogAsset combatCatalog =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonCombatRoomCatalogAsset>(
                    CombatRoomCatalogPath);
            if (combatCatalog == null)
            {
                throw new InvalidOperationException(
                    $"Release build requires the combat room catalog at " +
                    $"'{CombatRoomCatalogPath}'.");
            }

            var requiredCombatSceneNames = new HashSet<string>(
                combatCatalog.Entries.Select(entry => entry.SceneName),
                StringComparer.Ordinal);
            string[] releaseScenePaths = EditorBuildSettings.scenes
                .Where(scene =>
                    scene.enabled &&
                    File.Exists(scene.path) &&
                    IsReleaseScenePath(scene.path, requiredCombatSceneNames))
                .Select(scene => scene.path)
                .ToArray();

            var includedSceneNames = new HashSet<string>(
                releaseScenePaths.Select(Path.GetFileNameWithoutExtension),
                StringComparer.Ordinal);
            string[] missingCombatScenes = requiredCombatSceneNames
                .Where(sceneName => !includedSceneNames.Contains(sceneName))
                .OrderBy(sceneName => sceneName, StringComparer.Ordinal)
                .ToArray();
            if (missingCombatScenes.Length != 0)
            {
                throw new InvalidOperationException(
                    "Release build is missing runtime combat scenes: " +
                    string.Join(", ", missingCombatScenes));
            }

            return releaseScenePaths;
        }

        private static bool IsReleaseScenePath(
            string scenePath,
            ISet<string> requiredCombatSceneNames)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            string normalizedPath = scenePath.Replace('\\', '/');
            if (normalizedPath.IndexOf(
                    TestSceneDirectory,
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                return true;
            }

            return requiredCombatSceneNames.Contains(
                Path.GetFileNameWithoutExtension(normalizedPath));
        }
    }
}
