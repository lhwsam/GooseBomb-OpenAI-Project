using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class UiButtonAudioAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/UI/Apply Button Audio";

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            ApplyAll(saveLoadedLobbyScene: true);
        }

        public static void ApplyAllFromCommandLine()
        {
            ApplyAll(saveLoadedLobbyScene: true);
        }

        public static int ApplyToButtons(IEnumerable<Button> authoredButtons)
        {
            if (authoredButtons == null)
            {
                throw new ArgumentNullException(nameof(authoredButtons));
            }

            AudioClip hoverClip = LoadRequiredAsset<AudioClip>(
                PrototypeContentValidator.UiButtonHoverClipPath);
            AudioClip clickClip = LoadRequiredAsset<AudioClip>(
                PrototypeContentValidator.UiButtonClickClipPath);
            AudioMixer mixer = LoadRequiredAsset<AudioMixer>(
                PrototypeContentValidator.AudioMixerPath);
            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            if (sfxGroups.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one SFX group in {PrototypeContentValidator.AudioMixerPath}; " +
                    $"found {sfxGroups.Length}.");
            }

            Button[] buttons = authoredButtons
                .Where(button => button != null)
                .Distinct()
                .OrderBy(
                    button => GetHierarchyPath(button.transform),
                    StringComparer.Ordinal)
                .ToArray();
            var buttonsByCanvas = new Dictionary<Canvas, List<Button>>();
            for (int index = 0; index < buttons.Length; index++)
            {
                Canvas canvas = FindOwningCanvas(buttons[index]);
                if (canvas == null)
                {
                    throw new InvalidOperationException(
                        $"Button '{GetHierarchyPath(buttons[index].transform)}' " +
                        "must be below an authored Canvas.");
                }

                if (!buttonsByCanvas.TryGetValue(canvas, out List<Button> group))
                {
                    group = new List<Button>();
                    buttonsByCanvas.Add(canvas, group);
                }
                group.Add(buttons[index]);
            }

            int changed = 0;
            foreach (KeyValuePair<Canvas, List<Button>> pair in buttonsByCanvas)
            {
                changed += ConfigureCanvas(
                    pair.Key,
                    pair.Value,
                    sfxGroups[0],
                    hoverClip,
                    clickClip);
            }

            return changed;
        }

        public static int ApplyToPrefabAsset(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                throw new ArgumentException(
                    "A prefab asset path is required.",
                    nameof(prefabPath));
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Missing UI prefab: {prefabPath}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int changed = ApplyToButtons(
                    root.GetComponentsInChildren<Button>(true));
                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    AssetDatabase.ImportAsset(
                        prefabPath,
                        ImportAssetOptions.ForceUpdate);
                }

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int ApplyAll(bool saveLoadedLobbyScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before authoring UI button audio.");
            }

            AssetDatabase.ImportAsset(
                PrototypeContentValidator.UiButtonHoverClipPath,
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                PrototypeContentValidator.UiButtonClickClipPath,
                ImportAssetOptions.ForceUpdate);

            int lobbyChanges = ApplyToLobbySceneAsset(
                saveLoadedLobbyScene);
            int pauseChanges = ApplyToPrefabAsset(
                PrototypeInGameUiPrefabAuthoring.PausePrefabPath);
            int completionChanges = ApplyToPrefabAsset(
                PrototypeInGameUiPrefabAuthoring.RunCompletionPrefabPath);
            AssetDatabase.SaveAssets();

            int totalChanges = lobbyChanges +
                pauseChanges +
                completionChanges;
            Debug.Log(
                "UI button audio ready: " +
                $"lobby={lobbyChanges}, pause={pauseChanges}, " +
                $"completion={completionChanges}, total={totalChanges} changes.");
            return totalChanges;
        }

        private static int ApplyToLobbySceneAsset(bool saveLoadedScene)
        {
            string scenePath = PrototypeContentValidator.LobbyScenePath;
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForAuthoring = !scene.IsValid() || !scene.isLoaded;
            if (openedForAuthoring)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                int changed = LobbyButtonFeedbackAuthoring.ApplyToScene(scene);
                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                if ((openedForAuthoring || saveLoadedScene) &&
                    scene.isDirty &&
                    !EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save UI button audio in '{scenePath}'.");
                }

                return changed;
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static int ConfigureCanvas(
            Canvas canvas,
            IReadOnlyList<Button> buttons,
            AudioMixerGroup sfxGroup,
            AudioClip hoverClip,
            AudioClip clickClip)
        {
            PrototypeUiButtonAudioPlayer[] existingPlayers =
                canvas.GetComponents<PrototypeUiButtonAudioPlayer>();
            if (existingPlayers.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Canvas '{GetHierarchyPath(canvas.transform)}' contains " +
                    $"{existingPlayers.Length} UI button audio players.");
            }

            int changed = 0;
            PrototypeUiButtonAudioPlayer player = existingPlayers.Length == 1
                ? existingPlayers[0]
                : null;
            AudioSource source = player != null
                ? player.AudioSource
                : null;
            if (source == null || source.gameObject != canvas.gameObject)
            {
                AudioSource[] canvasSources = canvas.GetComponents<AudioSource>();
                if (player != null && canvasSources.Length == 1)
                {
                    source = canvasSources[0];
                }
                else
                {
                    source = Undo.AddComponent<AudioSource>(canvas.gameObject);
                    changed++;
                }
            }
            if (player == null)
            {
                player = Undo.AddComponent<PrototypeUiButtonAudioPlayer>(
                    canvas.gameObject);
                changed++;
            }

            bool sourceNeedsConfiguration = source.playOnAwake ||
                source.loop ||
                source.clip != null ||
                !Mathf.Approximately(source.volume, 1f) ||
                !Mathf.Approximately(source.pitch, 1f) ||
                !Mathf.Approximately(source.spatialBlend, 0f) ||
                !Mathf.Approximately(source.dopplerLevel, 0f) ||
                source.outputAudioMixerGroup != sfxGroup;
            if (sourceNeedsConfiguration)
            {
                Undo.RecordObject(source, "Configure UI Button Audio Source");
                source.playOnAwake = false;
                source.loop = false;
                source.clip = null;
                source.volume = 1f;
                source.pitch = 1f;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                source.outputAudioMixerGroup = sfxGroup;
                EditorUtility.SetDirty(source);
                changed++;
            }

            if (!player.HasConfiguration(source, hoverClip, clickClip))
            {
                Undo.RecordObject(player, "Configure UI Button Audio Player");
                player.Configure(source, hoverClip, clickClip);
                EditorUtility.SetDirty(player);
                changed++;
            }

            for (int index = 0; index < buttons.Count; index++)
            {
                Button button = buttons[index];
                PrototypeButtonScaleFeedback[] feedbacks =
                    button.GetComponents<PrototypeButtonScaleFeedback>();
                if (feedbacks.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Button '{GetHierarchyPath(button.transform)}' contains " +
                        $"{feedbacks.Length} scale feedback components.");
                }

                PrototypeButtonScaleFeedback feedback =
                    feedbacks.Length == 1 ? feedbacks[0] : null;
                if (feedback == null)
                {
                    feedback = Undo.AddComponent<PrototypeButtonScaleFeedback>(
                        button.gameObject);
                    feedback.Configure(button.transform as RectTransform);
                    changed++;
                }

                if (feedback.AudioPlayer == player)
                {
                    changed += EnsureClickAudioListener(button, player);
                    continue;
                }

                Undo.RecordObject(feedback, "Bind UI Button Audio Player");
                feedback.ConfigureAudio(player);
                EditorUtility.SetDirty(feedback);
                changed++;
                changed += EnsureClickAudioListener(button, player);
            }

            return changed;
        }

        private static int EnsureClickAudioListener(
            Button button,
            PrototypeUiButtonAudioPlayer player)
        {
            int persistentCount = button.onClick.GetPersistentEventCount();
            bool hasExpectedListener = persistentCount == 1 &&
                button.onClick.GetPersistentTarget(0) == player &&
                string.Equals(
                    button.onClick.GetPersistentMethodName(0),
                    nameof(PrototypeUiButtonAudioPlayer.PlayClick),
                    StringComparison.Ordinal);
            if (hasExpectedListener)
            {
                return 0;
            }

            Undo.RecordObject(button, "Bind UI Button Click Audio");
            for (int index = persistentCount - 1; index >= 0; index--)
            {
                if (button.onClick.GetPersistentTarget(index) is
                        PrototypeUiButtonAudioPlayer &&
                    string.Equals(
                        button.onClick.GetPersistentMethodName(index),
                        nameof(PrototypeUiButtonAudioPlayer.PlayClick),
                        StringComparison.Ordinal))
                {
                    UnityEventTools.RemovePersistentListener(
                        button.onClick,
                        index);
                }
            }

            if (button.onClick.GetPersistentEventCount() != 0)
            {
                throw new InvalidOperationException(
                    $"Button '{GetHierarchyPath(button.transform)}' already has " +
                    "a persistent action. UI click audio must be the only persistent " +
                    "listener so it runs before runtime navigation actions.");
            }

            UnityEventTools.AddPersistentListener(
                button.onClick,
                player.PlayClick);
            EditorUtility.SetDirty(button);
            return 1;
        }

        private static Canvas FindOwningCanvas(Button button)
        {
            Transform current = button.transform;
            while (current != null)
            {
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null)
                {
                    return canvas;
                }
                current = current.parent;
            }

            return null;
        }

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Missing required {typeof(T).Name}: {assetPath}");
            }

            return asset;
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
