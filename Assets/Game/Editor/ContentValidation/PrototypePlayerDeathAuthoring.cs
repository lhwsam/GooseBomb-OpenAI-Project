using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypePlayerDeathAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/Prototype/Apply Player Death Presentation";

        [MenuItem(MenuPath)]
        public static void ApplyPlayerDeathPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before applying player death presentation.");
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            int updatedCount = 0;
            try
            {
                for (int index = 0;
                     index < PrototypeContentValidator.PlayerDeathScenePaths.Length;
                     index++)
                {
                    string scenePath =
                        PrototypeContentValidator.PlayerDeathScenePaths[index];
                    Scene scene = EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Single);
                    ConfigureScene(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException(
                            $"Unity failed to save player-death scene '{scenePath}'.");
                    }
                    updatedCount++;
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Applied player death presentation to {updatedCount} dungeon scenes.");
        }

        public static void ConfigureScene(Scene scene)
        {
            PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
            PrototypePlayerController playerController =
                FindExactlyOne<PrototypePlayerController>(scene);
            PrototypePlayerAnimationPresenter playerAnimation =
                FindExactlyOne<PrototypePlayerAnimationPresenter>(scene);
            PrototypeCameraShake cameraShake =
                FindExactlyOne<PrototypeCameraShake>(scene);
            PrototypeRunCompletionPresenter completionPresenter =
                FindExactlyOne<PrototypeRunCompletionPresenter>(scene);
            Camera camera = FindExactlyOne<Camera>(scene);
            PrototypeBossClearTransitionView transitionViewPrefab =
                AssetDatabase.LoadAssetAtPath<PrototypeBossClearTransitionView>(
                    PrototypeBossIntroAuthoring.BossClearTransitionPrefabPath);
            if (transitionViewPrefab == null ||
                !transitionViewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    $"Player death presentation requires a valid transition prefab at '{PrototypeBossIntroAuthoring.BossClearTransitionPrefabPath}'.");
            }
            if (!camera.enabled || !camera.CompareTag("MainCamera") ||
                !camera.orthographic)
            {
                throw new InvalidOperationException(
                    $"Player death scene '{scene.path}' requires an enabled orthographic MainCamera.");
            }

            PrototypePlayerDeathPresenter presenter =
                session.GetComponent<PrototypePlayerDeathPresenter>();
            if (presenter == null)
            {
                presenter =
                    session.gameObject.AddComponent<PrototypePlayerDeathPresenter>();
            }
            presenter.Configure(
                session,
                playerController,
                playerAnimation,
                camera,
                cameraShake,
                transitionViewPrefab);
            completionPresenter.BindPlayerDeathPresenter(presenter);

            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(completionPresenter);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static T FindExactlyOne<T>(Scene scene)
            where T : Component
        {
            T[] components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' requires exactly one {typeof(T).Name}; " +
                    $"found {components.Length}.");
            }

            return components[0];
        }
    }
}
