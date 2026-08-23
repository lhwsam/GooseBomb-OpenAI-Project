using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeLobbyPresenter : MonoBehaviour
    {
        public const string DefaultLobbySceneName = "DungeonLobby";
        public const string DefaultStartSceneName = "DungeonStart";
        public const string GameTitle = "Bomb Goose";

        [SerializeField]
        private string startSceneName = DefaultStartSceneName;

        [Header("Scene-authored view")]
        [SerializeField]
        private Canvas lobbyCanvas;

        [SerializeField]
        private EventSystem eventSystem;

        [SerializeField]
        private GameObject controlsPanel;

        [SerializeField]
        private TextMeshProUGUI titleLabel;

        [SerializeField]
        private TextMeshProUGUI statusLabel;

        [SerializeField]
        private TextMeshProUGUI versionLabel;

        [SerializeField]
        private Button startButton;

        [SerializeField]
        private Button controlsButton;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private PrototypeUserSettingsRuntime settingsRuntime;

        [SerializeField]
        private PrototypeSettingsPanelPresenter settingsPanel;

        private bool _isStarting;

        public string StartSceneName => startSceneName;

        public Canvas LobbyCanvas => lobbyCanvas;

        public EventSystem LobbyEventSystem => eventSystem;

        public GameObject ControlsPanel => controlsPanel;

        public TextMeshProUGUI TitleLabel => titleLabel;

        public TextMeshProUGUI StatusLabel => statusLabel;

        public TextMeshProUGUI VersionLabel => versionLabel;

        public string TitleText => ComposeTitleText(titleLabel);

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;

        public string VersionText =>
            versionLabel != null ? versionLabel.text : string.Empty;

        public Button StartButton => startButton;

        public Button ControlsButton => controlsButton;

        public Button BackButton => backButton;

        public PrototypeUserSettingsRuntime SettingsRuntime => settingsRuntime;

        public PrototypeSettingsPanelPresenter SettingsPanel => settingsPanel;

        public bool HasBaseAuthoredViewReferences =>
            lobbyCanvas != null &&
            eventSystem != null &&
            controlsPanel != null &&
            titleLabel != null &&
            startButton != null &&
            controlsButton != null &&
            backButton != null;

        public bool HasAuthoredViewReferences =>
            HasBaseAuthoredViewReferences &&
            settingsRuntime != null &&
            settingsPanel != null &&
            settingsRuntime.HasRequiredReferences &&
            settingsPanel.HasAuthoredViewReferences;

        public bool HasVersionLabelReference => versionLabel != null;

        public bool IsControlsVisible =>
            controlsPanel != null && controlsPanel.activeSelf;

        public bool IsStarting => _isStarting;

        public int StartRequestCount { get; private set; }

        public void Configure(string authoredStartSceneName)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeLobbyPresenter before changing its configuration.");
            }
            if (string.IsNullOrWhiteSpace(authoredStartSceneName))
            {
                throw new ArgumentException(
                    "Lobby start scene name cannot be empty.",
                    nameof(authoredStartSceneName));
            }

            startSceneName = authoredStartSceneName;
        }

        public void BindAuthoredView(
            Canvas authoredLobbyCanvas,
            EventSystem authoredEventSystem,
            GameObject authoredControlsPanel,
            TextMeshProUGUI authoredTitleLabel,
            TextMeshProUGUI authoredStatusLabel,
            Button authoredStartButton,
            Button authoredControlsButton,
            Button authoredBackButton,
            PrototypeUserSettingsRuntime authoredSettingsRuntime,
            PrototypeSettingsPanelPresenter authoredSettingsPanel)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Lobby view references can only be authored outside Play Mode.");
            }

            lobbyCanvas = authoredLobbyCanvas;
            eventSystem = authoredEventSystem;
            controlsPanel = authoredControlsPanel;
            titleLabel = authoredTitleLabel;
            statusLabel = authoredStatusLabel;
            startButton = authoredStartButton;
            controlsButton = authoredControlsButton;
            backButton = authoredBackButton;
            settingsRuntime = authoredSettingsRuntime;
            settingsPanel = authoredSettingsPanel;
        }

        public void BindVersionLabel(TextMeshProUGUI authoredVersionLabel)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Lobby version label can only be authored outside Play Mode.");
            }

            versionLabel = authoredVersionLabel ??
                throw new ArgumentNullException(nameof(authoredVersionLabel));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ValidateConfiguration();
            versionLabel.text = FormatVersionText(Application.version);
            controlsPanel.SetActive(false);
            settingsPanel.Configure(settingsRuntime, HideControls);
            EnsureTextColorTarget(startButton);
            EnsureTextColorTarget(controlsButton);
            startButton.onClick.AddListener(StartNewRun);
            controlsButton.onClick.AddListener(ShowControls);
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartNewRun);
            }
            if (controlsButton != null)
            {
                controlsButton.onClick.RemoveListener(ShowControls);
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            eventSystem.SetSelectedGameObject(startButton.gameObject);
            startButton
                .GetComponent<PrototypeButtonScaleFeedback>()
                .SuppressSelectionVisualUntilInteraction();
            WebGlHarnessReporter.Report("lobby-ready");
        }

        public void StartNewRun()
        {
            if (_isStarting)
            {
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(startSceneName))
            {
                throw new InvalidOperationException(
                    $"Lobby start scene '{startSceneName}' is not loadable.");
            }

            _isStarting = true;
            StartRequestCount++;
            startButton.interactable = false;
            controlsButton.interactable = false;
            SetStatus("던전을 준비하는 중...");
            WebGlHarnessReporter.Report("lobby-start-requested");
            try
            {
                SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
            }
            catch
            {
                _isStarting = false;
                startButton.interactable = true;
                controlsButton.interactable = true;
                SetStatus("시작하지 못했습니다. 다시 시도해 주세요.");
                throw;
            }
        }

        public void ShowControls()
        {
            if (_isStarting || controlsPanel == null)
            {
                return;
            }

            settingsPanel.Open();
            WebGlHarnessReporter.Report("lobby-settings-opened");
        }

        public void HideControls()
        {
            if (controlsPanel == null)
            {
                return;
            }

            settingsPanel.HideImmediately();
            eventSystem.SetSelectedGameObject(controlsButton.gameObject);
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(startSceneName))
            {
                throw new InvalidOperationException(
                    "PrototypeLobbyPresenter requires a start scene name.");
            }
            if (!HasAuthoredViewReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeLobbyPresenter requires a scene-authored Canvas, EventSystem, title, panels, and buttons.");
            }
            if (!HasVersionLabelReference)
            {
                throw new InvalidOperationException(
                    "PrototypeLobbyPresenter requires a scene-authored version label.");
            }
            if (!HasTextColorFeedback(startButton) ||
                !HasTextColorFeedback(controlsButton))
            {
                throw new InvalidOperationException(
                    "Lobby main menu buttons require TMP labels and PrototypeButtonScaleFeedback components.");
            }
            if (lobbyCanvas.gameObject.scene != gameObject.scene ||
                eventSystem.gameObject.scene != gameObject.scene ||
                controlsPanel.scene != gameObject.scene ||
                versionLabel.gameObject.scene != gameObject.scene ||
                settingsRuntime.gameObject.scene != gameObject.scene ||
                settingsPanel.gameObject.scene != gameObject.scene)
            {
                throw new InvalidOperationException(
                    "PrototypeLobbyPresenter view references must belong to the lobby scene.");
            }

            PrototypeUiFactory.RequireGameFont();
            TextMeshProUGUI[] labels = lobbyCanvas.GetComponentsInChildren<
                TextMeshProUGUI>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                if (!PrototypeUiFactory.IsSupportedGameFont(labels[index].font))
                {
                    throw new InvalidOperationException(
                        $"Lobby label '{labels[index].name}' must use {PrototypeUiFactory.GameFontAssetName} or {PrototypeUiFactory.AlternateGameFontAssetName}.");
                }
            }
        }

        public static string FormatVersionText(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException(
                    "Application version cannot be empty.",
                    nameof(version));
            }

            return $"v.{version.Trim()}";
        }

        private static bool HasTextColorFeedback(Button button)
        {
            return button != null &&
                   button.GetComponent<PrototypeButtonScaleFeedback>() != null &&
                   button.GetComponentInChildren<TextMeshProUGUI>(true) != null;
        }

        private static void EnsureTextColorTarget(Button button)
        {
            button
                .GetComponent<PrototypeButtonScaleFeedback>()
                .EnsureColorTarget(
                    button.GetComponentInChildren<TextMeshProUGUI>(true));
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private static string ComposeTitleText(TextMeshProUGUI primaryLabel)
        {
            if (primaryLabel == null)
            {
                return string.Empty;
            }

            string primaryText = (primaryLabel.text ?? string.Empty).Trim();
            if (string.Equals(primaryText, GameTitle, StringComparison.Ordinal) ||
                primaryLabel.transform.parent == null)
            {
                return primaryText;
            }

            TextMeshProUGUI[] titleParts = primaryLabel.transform.parent
                .GetComponentsInChildren<TextMeshProUGUI>(true);
            if (titleParts.Length <= 1)
            {
                return primaryText;
            }

            var composedTitle = new StringBuilder();
            for (int index = 0; index < titleParts.Length; index++)
            {
                string part = (titleParts[index].text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                if (composedTitle.Length > 0)
                {
                    composedTitle.Append(' ');
                }
                composedTitle.Append(part);
            }

            return composedTitle.ToString();
        }
    }
}
