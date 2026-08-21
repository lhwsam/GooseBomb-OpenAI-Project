using System;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class LobbyButtonFeedbackAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/UI/Apply Button Feedback To Lobby";

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Lobby button feedback cannot be authored in Play Mode.");
            }

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
                int changed = ApplyToScene(scene);
                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (openedForAuthoring &&
                        !EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException(
                            "Unity failed to save lobby button feedback.");
                    }
                }

                Debug.Log(
                    $"Lobby button feedback ready: {changed} changed, " +
                    $"{FindButtons(scene).Length} total buttons.");
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        public static int ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException(
                    "A loaded lobby scene is required.",
                    nameof(scene));
            }

            Button[] buttons = FindButtons(scene);
            int changed = 0;
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                PrototypeButtonScaleFeedback feedback =
                    button.GetComponent<PrototypeButtonScaleFeedback>();
                if (feedback == null)
                {
                    feedback = Undo.AddComponent<PrototypeButtonScaleFeedback>(
                        button.gameObject);
                    changed++;
                }

                RectTransform visualTarget = ResolveVisualTarget(
                    button,
                    feedback);

                if (feedback.HasConfiguration(visualTarget))
                {
                    continue;
                }

                Undo.RecordObject(feedback, "Configure Lobby Button Feedback");
                feedback.Configure(visualTarget);
                EditorUtility.SetDirty(feedback);
                changed++;
            }

            return changed;
        }

        private static RectTransform ResolveVisualTarget(
            Button button,
            PrototypeButtonScaleFeedback feedback)
        {
            RectTransform buttonRect = button.transform as RectTransform;
            RectTransform configuredTarget = feedback.VisualTarget;
            if (configuredTarget != null &&
                configuredTarget != buttonRect &&
                configuredTarget.IsChildOf(buttonRect))
            {
                return configuredTarget;
            }

            Transform namedVisual = button.transform.Find("Visual");
            if (namedVisual is RectTransform namedVisualRect)
            {
                return namedVisualRect;
            }

            return buttonRect;
        }

        private static Button[] FindButtons(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .OrderBy(button => GetHierarchyPath(button.transform),
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
