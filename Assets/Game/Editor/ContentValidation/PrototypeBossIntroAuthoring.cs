using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeBossIntroAuthoring
    {
        public const string BossClearTransitionPrefabPath =
            "Assets/Game/Content/Resources/UI/PrototypeBossClearCanvas.prefab";

        private const string MenuPath =
            "Bomb Swap/Prototype/Apply Boss Intro Presentation";

        [MenuItem(MenuPath)]
        public static void ApplyBossIntroPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before applying boss intro presentation.");
            }
            int updatedCount = 0;
            for (int index = 0;
                 index < PrototypeContentValidator.BossIntroScenePaths.Length;
                 index++)
            {
                string scenePath =
                    PrototypeContentValidator.BossIntroScenePaths[index];
                Scene scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
                try
                {
                    ConfigureScene(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException(
                            $"Unity failed to save boss-intro scene '{scenePath}'.");
                    }
                    updatedCount++;
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Applied boss intro presentation to {updatedCount} boss scenes.");
        }

        public static void ConfigureScene(Scene scene)
        {
            PrototypeCameraShakeAuthoring.ConfigureScene(scene);

            PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
            PrototypeBossPresenter bossPresenter =
                FindExactlyOne<PrototypeBossPresenter>(scene);
            PrototypeBombPresenter bombPresenter =
                FindExactlyOne<PrototypeBombPresenter>(scene);
            PrototypeHealthHud healthHud = FindExactlyOne<PrototypeHealthHud>(scene);
            PrototypeUserSettingsRuntime settings =
                FindExactlyOne<PrototypeUserSettingsRuntime>(scene);
            PrototypeCameraShake cameraShake =
                FindExactlyOne<PrototypeCameraShake>(scene);
            Camera camera = FindExactlyOne<Camera>(scene);
            PrototypeBossClearTransitionView transitionViewPrefab =
                AssetDatabase.LoadAssetAtPath<PrototypeBossClearTransitionView>(
                    BossClearTransitionPrefabPath);
            if (transitionViewPrefab == null ||
                !transitionViewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    $"Boss presentation requires a valid transition prefab at '{BossClearTransitionPrefabPath}'.");
            }
            if (!session.IsBossEnabledByDefault ||
                !camera.enabled || !camera.CompareTag("MainCamera") ||
                !camera.orthographic)
            {
                throw new InvalidOperationException(
                    $"Boss intro scene '{scene.path}' requires an authored boss and an " +
                    "enabled orthographic MainCamera.");
            }

            PrototypeBossIntroPresenter intro =
                session.GetComponent<PrototypeBossIntroPresenter>();
            if (intro == null)
            {
                intro = session.gameObject.AddComponent<PrototypeBossIntroPresenter>();
            }
            intro.Configure(
                session,
                bossPresenter,
                healthHud,
                settings,
                camera,
                cameraShake);
            PrototypeBossClearPresenter clearPresenter =
                session.GetComponent<PrototypeBossClearPresenter>();
            if (clearPresenter == null)
            {
                clearPresenter =
                    session.gameObject.AddComponent<PrototypeBossClearPresenter>();
            }
            clearPresenter.Configure(
                session,
                bossPresenter,
                bombPresenter,
                settings,
                camera,
                cameraShake,
                transitionViewPrefab);
            bossPresenter.ConfigureAttackFeedback(settings, cameraShake);

            PrototypeRunCompletionPresenter[] completionPresenters =
                scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeRunCompletionPresenter>(true))
                    .ToArray();
            if (completionPresenters.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Boss scene '{scene.path}' has multiple run-completion presenters.");
            }
            if (completionPresenters.Length == 1)
            {
                completionPresenters[0].BindBossClearPresenter(clearPresenter);
                EditorUtility.SetDirty(completionPresenters[0]);
            }

            EditorUtility.SetDirty(intro);
            EditorUtility.SetDirty(clearPresenter);
            EditorUtility.SetDirty(bossPresenter);
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
