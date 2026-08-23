using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePausePresenter : MonoBehaviour
    {
        private PrototypeGameSession _session;
        private PrototypeUserSettingsRuntime _settingsRuntime;
        private GameObject _canvasObject;
        private GameObject _menuObject;
        private TextMeshProUGUI _statusLabel;
        private Button _resumeButton;
        private Button _settingsButton;
        private PrototypeSettingsPanelPresenter _settingsPanel;
        private bool _isSubscribed;

        public PrototypeGameSession Session => _session;

        public bool IsVisible { get; private set; }

        public bool IsSettingsOpen =>
            _settingsPanel != null && _settingsPanel.IsOpen;

        public PrototypeSettingsPanelPresenter SettingsPanel => _settingsPanel;

        public int ShowCount { get; private set; }

        public int HideCount { get; private set; }

        public string StatusText =>
            _statusLabel != null ? _statusLabel.text : string.Empty;

        public void Configure(PrototypeGameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (_session == session)
            {
                if (isActiveAndEnabled)
                {
                    Subscribe();
                    SetVisible(session.IsPaused);
                }
                return;
            }

            Unsubscribe();
            _session = session;
            _settingsRuntime = session.GetComponent<PrototypeUserSettingsRuntime>();
            if (isActiveAndEnabled)
            {
                Subscribe();
                SetVisible(session.IsPaused);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _session == null)
            {
                return;
            }
            Subscribe();
            SetVisible(_session.IsPaused);
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetVisible(false);
        }

        private void Subscribe()
        {
            if (_isSubscribed || _session == null)
            {
                return;
            }
            _session.PauseStateChanged += OnPauseStateChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _session == null)
            {
                return;
            }
            _session.PauseStateChanged -= OnPauseStateChanged;
            _isSubscribed = false;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            SetVisible(isPaused);
        }

        public bool TryHandlePauseCommand()
        {
            if (!IsSettingsOpen)
            {
                return false;
            }
            if (_settingsPanel.ConsumeCancelCommand())
            {
                return true;
            }
            _settingsPanel.Close();
            return true;
        }

        private void SetVisible(bool visible)
        {
            if (visible)
            {
                EnsureUi();
                ShowPauseMenu();
            }
            if (_canvasObject != null)
            {
                _canvasObject.SetActive(visible);
            }
            if (IsVisible == visible)
            {
                return;
            }

            IsVisible = visible;
            if (visible)
            {
                ShowCount++;
            }
            else
            {
                HideCount++;
            }
        }

        private void EnsureUi()
        {
            if (_canvasObject != null)
            {
                return;
            }

            _canvasObject = new GameObject(
                "PrototypePauseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            PrototypeUiFactory.ConfigureCanvasScaler(
                _canvasObject.GetComponent<CanvasScaler>());
            PrototypeUiFactory.EnsureEventSystem();

            RectTransform backdrop = PrototypeUiFactory.CreateRect(
                "Backdrop",
                _canvasObject.transform);
            SetAnchors(backdrop, Vector2.zero, Vector2.one);
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.015f, 0.02f, 0.04f, 0.76f);

            _menuObject = PrototypeUiFactory.CreateRect("PauseMenu", backdrop).gameObject;
            SetAnchors(
                _menuObject.GetComponent<RectTransform>(),
                new Vector2(0.22f, 0.2f),
                new Vector2(0.78f, 0.8f));

            TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                "Title",
                _menuObject.transform,
                56f,
                TextAlignmentOptions.Center,
                FontStyles.Bold,
                TextWrappingModes.Normal);
            SetAnchors(title.rectTransform, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.92f));
            title.text = "PAUSED";
            title.color = new Color(0.35f, 0.82f, 1f, 1f);

            _resumeButton = CreateButton(
                "ResumeButton", _menuObject.transform, "게임 계속", 27f,
                new Vector2(0.18f, 0.42f), new Vector2(0.82f, 0.58f));
            _resumeButton.onClick.AddListener(ResumeGame);

            _settingsButton = CreateButton(
                "SettingsButton", _menuObject.transform, "설정", 27f,
                new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.38f));
            _settingsButton.interactable = _settingsRuntime != null;
            _settingsButton.onClick.AddListener(OpenSettings);

            _statusLabel = PrototypeUiFactory.CreateText(
                "ResumeHint",
                _menuObject.transform,
                19f,
                TextAlignmentOptions.Center,
                FontStyles.Normal,
                TextWrappingModes.Normal);
            SetAnchors(_statusLabel.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.17f));
            _statusLabel.text = "ESC - 게임 계속";
            _statusLabel.color = Color.white;

            if (_settingsRuntime != null)
            {
                _settingsPanel = PrototypeSettingsPanelFactory.Create(
                    backdrop,
                    "PauseSettingsPanel");
                _settingsPanel.Configure(_settingsRuntime, ShowPauseMenu);
            }
        }

        private void ResumeGame()
        {
            _session.ResumeFromPause();
        }

        private void OpenSettings()
        {
            if (_settingsPanel == null)
            {
                return;
            }
            _menuObject.SetActive(false);
            _settingsPanel.Open();
        }

        private void ShowPauseMenu()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.HideImmediately();
            }
            if (_menuObject != null)
            {
                _menuObject.SetActive(true);
            }
            if (_resumeButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
            }
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            float fontSize,
            Vector2 min,
            Vector2 max)
        {
            Button button = PrototypeUiFactory.CreateButton(
                objectName,
                parent,
                label,
                fontSize,
                new Color(0.1f, 0.17f, 0.25f, 1f),
                new Color(0.2f, 0.46f, 0.65f, 1f));
            SetAnchors(button.GetComponent<RectTransform>(), min, max);
            return button;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
