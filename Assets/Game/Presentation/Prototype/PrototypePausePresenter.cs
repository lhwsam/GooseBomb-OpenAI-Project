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
        private PrototypePauseView _viewPrefab;
        private PrototypePauseView _viewInstance;
        private PrototypeUserSettingsRuntime _settingsRuntime;
        private GameObject _canvasObject;
        private GameObject _menuObject;
        private TextMeshProUGUI _statusLabel;
        private Button _resumeButton;
        private Button _settingsButton;
        private PrototypeSettingsPanelPresenter _settingsPanel;
        private bool _isSubscribed;

        public PrototypeGameSession Session => _session;

        public PrototypePauseView ViewPrefab => _viewPrefab;

        public PrototypePauseView ViewInstance => _viewInstance;

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

        public void Configure(
            PrototypeGameSession session,
            PrototypePauseView authoredViewPrefab)
        {
            BindViewPrefab(authoredViewPrefab);
            Configure(session);
        }

        public void BindViewPrefab(PrototypePauseView authoredViewPrefab)
        {
            if (Application.isPlaying && _viewInstance != null)
            {
                throw new InvalidOperationException(
                    "Pause view prefab cannot change after the view is instantiated.");
            }

            _viewPrefab = authoredViewPrefab ??
                throw new ArgumentNullException(nameof(authoredViewPrefab));
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

            if (_viewPrefab == null || !_viewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypePausePresenter requires a configured pause view prefab.");
            }

            _viewInstance = Instantiate(_viewPrefab, transform, false);
            _viewInstance.name = _viewPrefab.name;
            if (!_viewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated pause view is missing required references.");
            }

            _canvasObject = _viewInstance.gameObject;
            _menuObject = _viewInstance.Menu;
            _statusLabel = _viewInstance.StatusLabel;
            _resumeButton = _viewInstance.ResumeButton;
            _settingsButton = _viewInstance.SettingsButton;
            _settingsPanel = _viewInstance.SettingsPanel;
            PrototypeUiFactory.EnsureEventSystem();
            _resumeButton.onClick.AddListener(ResumeGame);
            _settingsButton.interactable = _settingsRuntime != null;
            _settingsButton.onClick.AddListener(OpenSettings);

            if (_settingsRuntime != null)
            {
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

    }
}
