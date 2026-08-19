using System;
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
        private const string OpenSelfDestructMenuPath =
            "Bomb Swap/Playtest/Open Self-Destruct Gates Room";
        private const string PlaySelfDestructMenuPath =
            "Bomb Swap/Playtest/Play Self-Destruct Gates Room";
        private const string RebuildSelfDestructMenuPath =
            "Bomb Swap/Playtest/Rebuild Self-Destruct Gates Room";
        private const string OpenBossMenuPath =
            "Bomb Swap/Playtest/Open Boss Battle Room";
        private const string PlayBossMenuPath =
            "Bomb Swap/Playtest/Play Boss Battle Room";
        private const string RebuildBossMenuPath =
            "Bomb Swap/Playtest/Rebuild Boss Battle Room";

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

        [MenuItem(OpenSelfDestructMenuPath, false, 110)]
        public static void OpenSelfDestructGatesRoom()
        {
            TryPrepareAndOpen(
                PrototypeContentValidator.SelfDestructGatesPlaytestScenePath,
                PrototypeContentBuilder.CreateOrUpdateSelfDestructGatesPlaytestScene);
        }

        [MenuItem(OpenSelfDestructMenuPath, true)]
        private static bool CanOpenSelfDestructGatesRoom()
        {
            return CanPreparePlaytestRoom();
        }

        [MenuItem(PlaySelfDestructMenuPath, false, 111)]
        public static void PlaySelfDestructGatesRoom()
        {
            if (TryPrepareAndOpen(
                    PrototypeContentValidator.SelfDestructGatesPlaytestScenePath,
                    PrototypeContentBuilder.CreateOrUpdateSelfDestructGatesPlaytestScene))
            {
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem(PlaySelfDestructMenuPath, true)]
        private static bool CanPlaySelfDestructGatesRoom()
        {
            return CanPreparePlaytestRoom();
        }

        [MenuItem(RebuildSelfDestructMenuPath, false, 112)]
        public static void RebuildSelfDestructGatesRoom()
        {
            Debug.Log(PrototypeContentBuilder.CreateOrUpdateSelfDestructGatesPlaytestScene());
        }

        [MenuItem(RebuildSelfDestructMenuPath, true)]
        private static bool CanRebuildSelfDestructGatesRoom()
        {
            return CanPreparePlaytestRoom();
        }

        [MenuItem(OpenBossMenuPath, false, 120)]
        public static void OpenBossBattleRoom()
        {
            TryPrepareAndOpen(
                PrototypeContentValidator.BossBattlePlaytestScenePath,
                PrototypeContentBuilder.CreateOrUpdateBossBattlePlaytestScene);
        }

        [MenuItem(OpenBossMenuPath, true)]
        private static bool CanOpenBossBattleRoom()
        {
            return CanPreparePlaytestRoom();
        }

        [MenuItem(PlayBossMenuPath, false, 121)]
        public static void PlayBossBattleRoom()
        {
            if (TryPrepareAndOpen(
                    PrototypeContentValidator.BossBattlePlaytestScenePath,
                    PrototypeContentBuilder.CreateOrUpdateBossBattlePlaytestScene))
            {
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem(PlayBossMenuPath, true)]
        private static bool CanPlayBossBattleRoom()
        {
            return CanPreparePlaytestRoom();
        }

        [MenuItem(RebuildBossMenuPath, false, 122)]
        public static void RebuildBossBattleRoom()
        {
            Debug.Log(PrototypeContentBuilder.CreateOrUpdateBossBattlePlaytestScene());
        }

        [MenuItem(RebuildBossMenuPath, true)]
        private static bool CanRebuildBossBattleRoom()
        {
            return CanPreparePlaytestRoom();
        }

        private static bool TryPrepareAndOpenArmoredPanicRoom()
        {
            return TryPrepareAndOpen(
                PrototypeContentValidator.ArmoredPanicPlaytestScenePath,
                PrototypeContentBuilder.CreateOrUpdateArmoredPanicPlaytestScene);
        }

        private static bool TryPrepareAndOpen(
            string scenePath,
            Func<string> synchronizeScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            Debug.Log(synchronizeScene());
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool CanPreparePlaytestRoom()
        {
            return !EditorApplication.isCompiling &&
                   !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
