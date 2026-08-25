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
    public static class LobbySettingsControlsAuthoring
    {
        private const string MenuPath =
            "Bomb Swap/UI/Wire Lobby Settings Controls";
        private const string KeyboardResetButtonName = "ResetButton";
        private const string ObsoleteStatusLabelName = "SettingsStatusText";
        private const string VersionLabelName = "VersionText";

        private static readonly AudioObjectNames[] AudioObjects =
        {
            new AudioObjectNames(
                "masterSlider", "masterValueLabel", "전체 음량", "MasterVolume"),
            new AudioObjectNames(
                "bgmSlider", "bgmValueLabel", "배경음", "BgmVolume"),
            new AudioObjectNames(
                "sfxSlider", "sfxValueLabel", "효과음", "SfxVolume"),
        };

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            Apply();
        }

        private static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Lobby settings controls cannot be authored in Play Mode.");
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
                PrototypeSettingsPanelPresenter presenter =
                    FindSettingsPanel(scene);
                PrototypeScreenShakeToggleAuthoring.ConfigurePresenter(presenter);
                PrototypeLobbyPresenter lobbyPresenter =
                    FindLobbyPresenter(scene);
                Button keyboardResetButton = FindKeyboardResetButton(presenter);
                TextMeshProUGUI versionLabel = FindVersionLabel(lobbyPresenter);

                // Name lookup is limited to this one-time authoring migration.
                // Runtime behavior uses the serialized direct reference below.
                var serializedPresenter = new SerializedObject(presenter);
                SerializedProperty resetProperty =
                    serializedPresenter.FindProperty("keyboardResetButton");
                if (resetProperty == null)
                {
                    throw new InvalidOperationException(
                        "PrototypeSettingsPanelPresenter is missing its " +
                        "keyboardResetButton serialized field.");
                }

                Undo.RecordObject(presenter, "Wire Lobby Settings Controls");
                resetProperty.objectReferenceValue = keyboardResetButton;
                serializedPresenter.ApplyModifiedProperties();
                RemoveObsoleteStatusLabel(presenter);
                NormalizeAudioObjectNames(serializedPresenter);

                Undo.RecordObject(lobbyPresenter, "Wire Lobby Version Label");
                lobbyPresenter.BindVersionLabel(versionLabel);

                EditorUtility.SetDirty(presenter);
                EditorUtility.SetDirty(lobbyPresenter);
                EditorSceneManager.MarkSceneDirty(scene);

                if (openedForAuthoring &&
                    !EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the lobby scene.");
                }

                Debug.Log(
                    "Lobby settings controls and version label wired, and " +
                    "audio hierarchy names and screen-shake toggle normalized " +
                    "without changing " +
                    "authored images or RectTransform layout.",
                    presenter);
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static PrototypeSettingsPanelPresenter FindSettingsPanel(
            Scene scene)
        {
            PrototypeSettingsPanelPresenter[] presenters =
                scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeSettingsPanelPresenter>(true))
                    .ToArray();
            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one settings panel in {scene.path}, " +
                    $"but found {presenters.Length}.");
            }

            return presenters[0];
        }

        private static PrototypeLobbyPresenter FindLobbyPresenter(Scene scene)
        {
            PrototypeLobbyPresenter[] presenters = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    PrototypeLobbyPresenter>(true))
                .ToArray();
            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one lobby presenter in {scene.path}, " +
                    $"but found {presenters.Length}.");
            }

            return presenters[0];
        }

        private static TextMeshProUGUI FindVersionLabel(
            PrototypeLobbyPresenter presenter)
        {
            TextMeshProUGUI[] candidates = presenter.LobbyCanvas
                .GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(label => string.Equals(
                    label.name,
                    VersionLabelName,
                    StringComparison.Ordinal))
                .ToArray();
            if (candidates.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {VersionLabelName} under the authored " +
                    $"LobbyCanvas, but found {candidates.Length}.");
            }

            return candidates[0];
        }

        private static Button FindKeyboardResetButton(
            PrototypeSettingsPanelPresenter presenter)
        {
            Button[] candidates = presenter.ControlsPage
                .GetComponentsInChildren<Button>(true)
                .Where(button => string.Equals(
                    button.name,
                    KeyboardResetButtonName,
                    StringComparison.Ordinal))
                .ToArray();
            if (candidates.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {KeyboardResetButtonName} under the " +
                    $"authored ControlsPage, but found {candidates.Length}.");
            }

            return candidates[0];
        }

        private static void RemoveObsoleteStatusLabel(
            PrototypeSettingsPanelPresenter presenter)
        {
            TextMeshProUGUI[] obsoleteLabels = presenter
                .GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(label => string.Equals(
                    label.name,
                    ObsoleteStatusLabelName,
                    StringComparison.Ordinal))
                .ToArray();
            if (obsoleteLabels.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Expected at most one {ObsoleteStatusLabelName}, " +
                    $"but found {obsoleteLabels.Length}.");
            }
            if (obsoleteLabels.Length == 1)
            {
                Undo.DestroyObjectImmediate(obsoleteLabels[0].gameObject);
            }
        }

        private static void NormalizeAudioObjectNames(
            SerializedObject serializedPresenter)
        {
            GameObject audioPage = GetObjectReference<GameObject>(
                serializedPresenter,
                "audioPage");
            foreach (AudioObjectNames names in AudioObjects)
            {
                Slider slider = GetObjectReference<Slider>(
                    serializedPresenter,
                    names.SliderProperty);
                TextMeshProUGUI valueLabel =
                    GetObjectReference<TextMeshProUGUI>(
                        serializedPresenter,
                        names.ValueProperty);
                TextMeshProUGUI[] labels = audioPage
                    .GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Where(label =>
                        label != valueLabel &&
                        string.Equals(
                            label.text,
                            names.DisplayText,
                            StringComparison.Ordinal))
                    .ToArray();
                if (labels.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected one '{names.DisplayText}' label under " +
                        $"{audioPage.name}, but found {labels.Length}.");
                }

                RenameChild(audioPage, labels[0].gameObject, names.Prefix + "Label");
                RenameChild(audioPage, slider.gameObject, names.Prefix + "Slider");
                RenameChild(audioPage, valueLabel.gameObject, names.Prefix + "Value");
            }
        }

        private static T GetObjectReference<T>(
            SerializedObject serializedObject,
            string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            T value = property != null
                ? property.objectReferenceValue as T
                : null;
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"PrototypeSettingsPanelPresenter is missing its " +
                    $"{propertyName} reference.");
            }

            return value;
        }

        private static void RenameChild(
            GameObject expectedParent,
            GameObject target,
            string newName)
        {
            if (!target.transform.IsChildOf(expectedParent.transform))
            {
                throw new InvalidOperationException(
                    $"{target.name} is not a child of {expectedParent.name}.");
            }

            if (string.Equals(target.name, newName, StringComparison.Ordinal))
            {
                return;
            }

            Undo.RecordObject(target, "Normalize Audio Object Name");
            target.name = newName;
            EditorUtility.SetDirty(target);
        }

        private readonly struct AudioObjectNames
        {
            public AudioObjectNames(
                string sliderProperty,
                string valueProperty,
                string displayText,
                string prefix)
            {
                SliderProperty = sliderProperty;
                ValueProperty = valueProperty;
                DisplayText = displayText;
                Prefix = prefix;
            }

            public string SliderProperty { get; }

            public string ValueProperty { get; }

            public string DisplayText { get; }

            public string Prefix { get; }
        }
    }
}
