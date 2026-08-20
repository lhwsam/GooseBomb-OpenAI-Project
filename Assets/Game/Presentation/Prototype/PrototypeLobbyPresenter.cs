using System;
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
        public const string GameTitle = "폭탄을 낳는 거위";

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
        private Button startButton;

        [SerializeField]
        private Button controlsButton;

        [SerializeField]
        private Button backButton;

        private bool _isStarting;

        public string StartSceneName => startSceneName;

        public Canvas LobbyCanvas => lobbyCanvas;

        public EventSystem LobbyEventSystem => eventSystem;

        public GameObject ControlsPanel => controlsPanel;

        public TextMeshProUGUI TitleLabel => titleLabel;

        public TextMeshProUGUI StatusLabel => statusLabel;

        public string TitleText => titleLabel != null ? titleLabel.text : string.Empty;

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;

        public Button StartButton => startButton;

        public Button ControlsButton => controlsButton;

        public Button BackButton => backButton;

        public bool HasAuthoredViewReferences =>
            lobbyCanvas != null &&
            eventSystem != null &&
            controlsPanel != null &&
            titleLabel != null &&
            statusLabel != null &&
            startButton != null &&
            controlsButton != null &&
            backButton != null;

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
            Button authoredBackButton)
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
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ValidateConfiguration();
            controlsPanel.SetActive(false);
            startButton.onClick.AddListener(StartNewRun);
            controlsButton.onClick.AddListener(ShowControls);
            backButton.onClick.AddListener(HideControls);
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
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HideControls);
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            eventSystem.SetSelectedGameObject(startButton.gameObject);
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
            statusLabel.text = "던전을 준비하는 중...";
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
                statusLabel.text = "시작하지 못했습니다. 다시 시도해 주세요.";
                throw;
            }
        }

        public void ShowControls()
        {
            if (_isStarting || controlsPanel == null)
            {
                return;
            }

            controlsPanel.SetActive(true);
            eventSystem.SetSelectedGameObject(backButton.gameObject);
            WebGlHarnessReporter.Report("lobby-controls-opened");
        }

        public void HideControls()
        {
            if (controlsPanel == null)
            {
                return;
            }

            controlsPanel.SetActive(false);
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
                    "PrototypeLobbyPresenter requires scene-authored Canvas, EventSystem, labels, panels, and buttons.");
            }
            if (lobbyCanvas.gameObject.scene != gameObject.scene ||
                eventSystem.gameObject.scene != gameObject.scene ||
                controlsPanel.scene != gameObject.scene)
            {
                throw new InvalidOperationException(
                    "PrototypeLobbyPresenter view references must belong to the lobby scene.");
            }

            TMP_FontAsset font = PrototypeUiFactory.RequireGameFont();
            TextMeshProUGUI[] labels = lobbyCanvas.GetComponentsInChildren<
                TextMeshProUGUI>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                if (labels[index].font != font)
                {
                    throw new InvalidOperationException(
                        $"Lobby label '{labels[index].name}' must use {PrototypeUiFactory.GameFontAssetName}.");
                }
            }
        }
    }
}
