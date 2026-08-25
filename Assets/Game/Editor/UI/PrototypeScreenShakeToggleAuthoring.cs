using System;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class PrototypeScreenShakeToggleAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/UI/Wire Screen Shake Toggle";
        private const string ButtonName = "ScreenShakeButton";

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            Apply();
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Screen-shake toggle cannot be authored in Play Mode.");
            }

            ConfigurePausePrefab();
            ConfigureLobbyScene();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Wired the screen-shake toggle in PrototypePauseCanvas and DungeonLobby.");
        }

        internal static void ConfigurePresenter(
            PrototypeSettingsPanelPresenter presenter)
        {
            if (presenter == null)
            {
                throw new ArgumentNullException(nameof(presenter));
            }
            if (presenter.AudioPage == null)
            {
                throw new InvalidOperationException(
                    $"Settings panel '{presenter.name}' is missing its AudioPage reference.");
            }

            Button[] buttons = presenter.AudioPage
                .GetComponentsInChildren<Button>(true)
                .Where(button => string.Equals(
                    button.name,
                    ButtonName,
                    StringComparison.Ordinal))
                .ToArray();
            if (buttons.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {ButtonName} under '{presenter.AudioPage.name}', " +
                    $"but found {buttons.Length}.");
            }

            TextMeshProUGUI[] labels = buttons[0]
                .GetComponentsInChildren<TextMeshProUGUI>(true);
            if (labels.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one TMP value label under {ButtonName}, " +
                    $"but found {labels.Length}.");
            }

            Undo.RecordObject(presenter, "Wire Screen Shake Toggle");
            presenter.BindScreenShakeToggle(buttons[0], labels[0]);
            EditorUtility.SetDirty(presenter);
        }

        private static void ConfigurePausePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                PrototypeInGameUiPrefabAuthoring.PausePrefabPath);
            try
            {
                PrototypeSettingsPanelPresenter[] presenters =
                    root.GetComponentsInChildren<
                        PrototypeSettingsPanelPresenter>(true);
                if (presenters.Length != 1)
                {
                    throw new InvalidOperationException(
                        "PrototypePauseCanvas requires exactly one settings panel; " +
                        $"found {presenters.Length}.");
                }

                ConfigurePresenter(presenters[0]);
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrototypeInGameUiPrefabAuthoring.PausePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.ImportAsset(
                PrototypeInGameUiPrefabAuthoring.PausePrefabPath,
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureLobbyScene()
        {
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
                PrototypeSettingsPanelPresenter[] presenters = scene
                    .GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeSettingsPanelPresenter>(true))
                    .ToArray();
                if (presenters.Length != 1)
                {
                    throw new InvalidOperationException(
                        "DungeonLobby requires exactly one settings panel; " +
                        $"found {presenters.Length}.");
                }

                ConfigurePresenter(presenters[0]);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "Unity failed to save DungeonLobby after wiring " +
                        "the screen-shake toggle.");
                }
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
