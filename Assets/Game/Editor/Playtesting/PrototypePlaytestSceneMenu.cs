using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.Playtesting
{
    public static class PrototypePlaytestSceneMenu
    {
        private const string OpenMenuPath =
            "Bomb Swap/Playtest/Open Armored Panic Room";
        private const string PlayMenuPath =
            "Bomb Swap/Playtest/Play Armored Panic Room";
        private const string RebuildMenuPath =
            "Bomb Swap/Playtest/Rebuild Armored Panic Room";

        [MenuItem(OpenMenuPath, false, 100)]
        public static void OpenArmoredPanicRoom()
        {
            TryPrepareAndOpenArmoredPanicRoom();
        }

        [MenuItem(OpenMenuPath, true)]
        private static bool CanOpenArmoredPanicRoom()
        {
            return !EditorApplication.isCompiling &&
                   !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(PlayMenuPath, false, 101)]
        public static void PlayArmoredPanicRoom()
        {
            if (TryPrepareAndOpenArmoredPanicRoom())
            {
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem(PlayMenuPath, true)]
        private static bool CanPlayArmoredPanicRoom()
        {
            return CanOpenArmoredPanicRoom();
        }

        [MenuItem(RebuildMenuPath, false, 102)]
        public static void RebuildArmoredPanicRoom()
        {
            Debug.Log(PrototypeContentBuilder.CreateOrUpdateArmoredPanicPlaytestScene());
        }

        [MenuItem(RebuildMenuPath, true)]
        private static bool CanRebuildArmoredPanicRoom()
        {
            return CanOpenArmoredPanicRoom();
        }

        private static bool TryPrepareAndOpenArmoredPanicRoom()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            Debug.Log(PrototypeContentBuilder.CreateOrUpdateArmoredPanicPlaytestScene());
            Scene scene = EditorSceneManager.OpenScene(
                PrototypeContentValidator.ArmoredPanicPlaytestScenePath,
                OpenSceneMode.Single);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
