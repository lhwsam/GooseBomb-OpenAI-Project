using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeCameraShakeAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/Prototype/Apply Player Bomb Camera Shake";

        [MenuItem(MenuPath)]
        public static void ApplyPlayerBombCameraShakeToPlayableScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before applying player-bomb camera shake.");
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
                     index < PrototypeContentValidator.CameraShakeScenePaths.Length;
                     index++)
                {
                    string scenePath =
                        PrototypeContentValidator.CameraShakeScenePaths[index];
                    Scene scene = EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Single);
                    ConfigureScene(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException(
                            $"Unity failed to save camera-shake scene '{scenePath}'.");
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
                $"Applied player-bomb camera shake to {updatedCount} playable scenes.");
        }

        public static void ConfigureScene(Scene scene)
        {
            PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
            PrototypeUserSettingsRuntime settings =
                FindExactlyOne<PrototypeUserSettingsRuntime>(scene);
            Camera camera = FindExactlyOne<Camera>(scene);
            if (!camera.enabled || !camera.CompareTag("MainCamera"))
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' camera must be enabled and tagged MainCamera.");
            }

            GameObject owner = session.gameObject;
            PrototypeCameraShake shake = owner.GetComponent<PrototypeCameraShake>();
            if (shake == null)
            {
                shake = owner.AddComponent<PrototypeCameraShake>();
            }
            PrototypePlayerBombCameraShakePresenter presenter =
                owner.GetComponent<PrototypePlayerBombCameraShakePresenter>();
            if (presenter == null)
            {
                presenter = owner.AddComponent<PrototypePlayerBombCameraShakePresenter>();
            }

            shake.Configure(camera.transform);
            presenter.Configure(session, settings, shake);
            EditorUtility.SetDirty(shake);
            EditorUtility.SetDirty(presenter);
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
