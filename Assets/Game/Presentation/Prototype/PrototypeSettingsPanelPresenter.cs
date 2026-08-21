using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSettingsPanelPresenter : MonoBehaviour
    {
        [Serializable]
        public sealed class KeyboardBindingView
        {
            [SerializeField]
            private string actionName;

            [SerializeField]
            private string bindingId;

            [SerializeField]
            private Button button;

            [SerializeField]
            private TextMeshProUGUI valueLabel;

            public string ActionName => actionName;

            public string BindingId => bindingId;

            public Button Button => button;

            public TextMeshProUGUI ValueLabel => valueLabel;

            public void Configure(
                string authoredActionName,
                string authoredBindingId,
                Button authoredButton,
                TextMeshProUGUI authoredValueLabel)
            {
                actionName = authoredActionName;
                bindingId = authoredBindingId;
                button = authoredButton;
                valueLabel = authoredValueLabel;
            }
        }

        [SerializeField]
        private GameObject controlsPage;

        [SerializeField]
        private GameObject audioPage;

        [SerializeField]
        private Button controlsTabButton;

        [SerializeField]
        private Button audioTabButton;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button fullscreenButton;

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private Slider masterSlider;

        [SerializeField]
        private Slider bgmSlider;

        [SerializeField]
        private Slider sfxSlider;

        [SerializeField]
        private Slider screenShakeSlider;

        [SerializeField]
        private TextMeshProUGUI masterValueLabel;

        [SerializeField]
        private TextMeshProUGUI bgmValueLabel;

        [SerializeField]
        private TextMeshProUGUI sfxValueLabel;

        [SerializeField]
        private TextMeshProUGUI screenShakeValueLabel;

        [SerializeField]
        private TextMeshProUGUI statusLabel;

        [SerializeField]
        private KeyboardBindingView[] keyboardBindings = Array.Empty<KeyboardBindingView>();

        private PrototypeUserSettingsRuntime _settings;
        private Action _closeRequested;
        private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
        private InputAction _rebindingAction;
        private bool _rebindingActionWasEnabled;
        private string _previousOverridePath;
        private KeyboardBindingView _activeBinding;
        private bool _listenersBound;
        private bool _synchronizing;
        private UnityAction[] _bindingButtonListeners;
        private int _rebindCanceledFrame = -1;

        public bool HasAuthoredViewReferences =>
            controlsPage != null &&
            audioPage != null &&
            controlsTabButton != null &&
            audioTabButton != null &&
            backButton != null &&
            fullscreenButton != null &&
            resetButton != null &&
            masterSlider != null &&
            bgmSlider != null &&
            sfxSlider != null &&
            screenShakeSlider != null &&
            masterValueLabel != null &&
            bgmValueLabel != null &&
            sfxValueLabel != null &&
            screenShakeValueLabel != null &&
            statusLabel != null &&
            keyboardBindings != null &&
            keyboardBindings.Length == PrototypeSettingsPanelFactory.KeyboardBindingCount;

        public bool IsOpen => gameObject.activeSelf;

        public bool IsControlsPageVisible => controlsPage != null && controlsPage.activeSelf;

        public bool IsRebinding => _rebindingOperation != null;

        public int KeyboardBindingCount =>
            keyboardBindings != null ? keyboardBindings.Length : 0;

        public PrototypeUserSettingsRuntime Settings => _settings;

        public Button BackButton => backButton;

        public void BindAuthoredView(
            GameObject authoredControlsPage,
            GameObject authoredAudioPage,
            Button authoredControlsTabButton,
            Button authoredAudioTabButton,
            Button authoredBackButton,
            Button authoredFullscreenButton,
            Button authoredResetButton,
            Slider authoredMasterSlider,
            Slider authoredBgmSlider,
            Slider authoredSfxSlider,
            Slider authoredScreenShakeSlider,
            TextMeshProUGUI authoredMasterValueLabel,
            TextMeshProUGUI authoredBgmValueLabel,
            TextMeshProUGUI authoredSfxValueLabel,
            TextMeshProUGUI authoredScreenShakeValueLabel,
            TextMeshProUGUI authoredStatusLabel,
            KeyboardBindingView[] authoredKeyboardBindings)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable the settings panel before changing authored references.");
            }

            controlsPage = authoredControlsPage;
            audioPage = authoredAudioPage;
            controlsTabButton = authoredControlsTabButton;
            audioTabButton = authoredAudioTabButton;
            backButton = authoredBackButton;
            fullscreenButton = authoredFullscreenButton;
            resetButton = authoredResetButton;
            masterSlider = authoredMasterSlider;
            bgmSlider = authoredBgmSlider;
            sfxSlider = authoredSfxSlider;
            screenShakeSlider = authoredScreenShakeSlider;
            masterValueLabel = authoredMasterValueLabel;
            bgmValueLabel = authoredBgmValueLabel;
            sfxValueLabel = authoredSfxValueLabel;
            screenShakeValueLabel = authoredScreenShakeValueLabel;
            statusLabel = authoredStatusLabel;
            keyboardBindings = authoredKeyboardBindings ??
                throw new ArgumentNullException(nameof(authoredKeyboardBindings));
        }

        public void Configure(
            PrototypeUserSettingsRuntime settings,
            Action closeRequested)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _closeRequested = closeRequested;
            if (isActiveAndEnabled)
            {
                BindListeners();
                RefreshAll();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (_settings == null || !HasAuthoredViewReferences)
            {
                throw new InvalidOperationException(
                    "Settings panel requires a configured runtime and authored view references.");
            }

            BindListeners();
            ShowControlsPage();
            RefreshAll();
        }

        private void OnDisable()
        {
            CancelRebind();
            UnbindListeners();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            ShowControlsPage();
            RefreshAll();
            Select(controlsTabButton);
            WebGlHarnessReporter.Report("settings-opened");
        }

        public void Close()
        {
            if (_settings != null)
            {
                _settings.Persist();
            }
            CancelRebind();
            WebGlHarnessReporter.Report("settings-closed");
            _closeRequested?.Invoke();
        }

        public void HideImmediately()
        {
            CancelRebind();
            gameObject.SetActive(false);
        }

        public void ShowControlsPage()
        {
            controlsPage.SetActive(true);
            audioPage.SetActive(false);
            SetStatus("변경할 키를 선택하세요. ESC는 변경 취소입니다.");
        }

        public void ShowAudioPage()
        {
            controlsPage.SetActive(false);
            audioPage.SetActive(true);
            SetStatus("음량과 화면 흔들림은 즉시 적용됩니다.");
            WebGlHarnessReporter.Report("settings-audio-page-opened");
        }

        private void BindListeners()
        {
            if (_listenersBound)
            {
                return;
            }

            controlsTabButton.onClick.AddListener(ShowControlsPage);
            audioTabButton.onClick.AddListener(ShowAudioPage);
            backButton.onClick.AddListener(Close);
            fullscreenButton.onClick.AddListener(ToggleFullscreen);
            resetButton.onClick.AddListener(ResetDefaults);
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            screenShakeSlider.onValueChanged.AddListener(OnScreenShakeChanged);
            for (int index = 0; index < keyboardBindings.Length; index++)
            {
                KeyboardBindingView binding = keyboardBindings[index];
                _bindingButtonListeners ??= new UnityAction[keyboardBindings.Length];
                UnityAction listener = () => BeginRebind(binding);
                _bindingButtonListeners[index] = listener;
                binding.Button.onClick.AddListener(listener);
            }
            _settings.Changed += OnSettingsChanged;
            _listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!_listenersBound)
            {
                return;
            }
            controlsTabButton.onClick.RemoveListener(ShowControlsPage);
            audioTabButton.onClick.RemoveListener(ShowAudioPage);
            backButton.onClick.RemoveListener(Close);
            fullscreenButton.onClick.RemoveListener(ToggleFullscreen);
            resetButton.onClick.RemoveListener(ResetDefaults);
            masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            screenShakeSlider.onValueChanged.RemoveListener(OnScreenShakeChanged);
            for (int index = 0; index < keyboardBindings.Length; index++)
            {
                if (_bindingButtonListeners != null &&
                    index < _bindingButtonListeners.Length &&
                    _bindingButtonListeners[index] != null)
                {
                    keyboardBindings[index].Button.onClick.RemoveListener(
                        _bindingButtonListeners[index]);
                    _bindingButtonListeners[index] = null;
                }
            }
            if (_settings != null)
            {
                _settings.Changed -= OnSettingsChanged;
            }
            _listenersBound = false;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (_synchronizing)
            {
                return;
            }
            _settings.SetMasterVolume(value);
            WebGlHarnessReporter.Report("settings-master-volume-changed");
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (_synchronizing)
            {
                return;
            }
            _settings.SetBgmVolume(value);
            WebGlHarnessReporter.Report("settings-bgm-volume-changed");
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (_synchronizing)
            {
                return;
            }
            _settings.SetSfxVolume(value);
            WebGlHarnessReporter.Report("settings-sfx-volume-changed");
        }

        private void OnScreenShakeChanged(float value)
        {
            if (_synchronizing)
            {
                return;
            }
            _settings.SetScreenShakeIntensity(value);
            WebGlHarnessReporter.Report("settings-screen-shake-changed");
        }

        private void OnSettingsChanged(PrototypeUserSettings settings)
        {
            RefreshValues(settings);
        }

        private void RefreshAll()
        {
            RefreshValues(_settings.Current);
            RefreshBindingLabels();
        }

        private void RefreshValues(PrototypeUserSettings settings)
        {
            _synchronizing = true;
            masterSlider.SetValueWithoutNotify(settings.MasterVolume);
            bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
            sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
            screenShakeSlider.SetValueWithoutNotify(settings.ScreenShakeIntensity);
            masterValueLabel.text = FormatPercent(settings.MasterVolume);
            bgmValueLabel.text = FormatPercent(settings.BgmVolume);
            sfxValueLabel.text = FormatPercent(settings.SfxVolume);
            screenShakeValueLabel.text = settings.ScreenShakeIntensity <= 0.001f
                ? "끔"
                : FormatPercent(settings.ScreenShakeIntensity);
            _synchronizing = false;
        }

        private void RefreshBindingLabels()
        {
            for (int index = 0; index < keyboardBindings.Length; index++)
            {
                KeyboardBindingView view = keyboardBindings[index];
                if (!TryResolveBinding(view, out InputAction action, out int bindingIndex))
                {
                    view.ValueLabel.text = "누락";
                    continue;
                }
                view.ValueLabel.text = action.GetBindingDisplayString(bindingIndex);
            }
        }

        private void BeginRebind(KeyboardBindingView view)
        {
            if (_rebindingOperation != null ||
                !TryResolveBinding(view, out InputAction action, out int bindingIndex))
            {
                return;
            }

            _activeBinding = view;
            _rebindingAction = action;
            _rebindingActionWasEnabled = action.enabled;
            _previousOverridePath = action.bindings[bindingIndex].overridePath;
            action.Disable();
            view.ValueLabel.text = "키 입력...";
            SetStatus("새 키를 누르세요. ESC를 누르면 취소됩니다.");

            _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ => FinishRebind(false))
                .OnComplete(_ => FinishRebind(true));
            _rebindingOperation.Start();
        }

        private void FinishRebind(bool completed)
        {
            KeyboardBindingView completedView = _activeBinding;
            InputAction completedAction = _rebindingAction;
            bool actionWasEnabled = _rebindingActionWasEnabled;
            string previousOverridePath = _previousOverridePath;

            _rebindingOperation?.Dispose();
            _rebindingOperation = null;
            _activeBinding = null;
            _rebindingAction = null;
            _previousOverridePath = null;

            if (completed && completedView != null &&
                TryResolveBinding(completedView, out InputAction action, out int bindingIndex))
            {
                string candidatePath = action.bindings[bindingIndex].effectivePath;
                if (HasDuplicateKeyboardBinding(completedView, candidatePath))
                {
                    if (string.IsNullOrEmpty(previousOverridePath))
                    {
                        action.RemoveBindingOverride(bindingIndex);
                    }
                    else
                    {
                        action.ApplyBindingOverride(bindingIndex, previousOverridePath);
                    }
                    SetStatus("이미 다른 조작에서 사용하는 키입니다.");
                }
                else
                {
                    _settings.SaveInputOverrides();
                    SetStatus("키 변경을 저장했습니다.");
                    WebGlHarnessReporter.Report("settings-key-rebound");
                }
            }
            else
            {
                _rebindCanceledFrame = Time.frameCount;
                SetStatus("키 변경을 취소했습니다.");
            }

            if (completedAction != null && actionWasEnabled)
            {
                completedAction.Enable();
            }
            RefreshBindingLabels();
            if (completedView != null)
            {
                Select(completedView.Button);
            }
        }

        private void CancelRebind()
        {
            if (_rebindingOperation != null)
            {
                _rebindingOperation.Cancel();
            }
        }

        public bool ConsumeCancelCommand()
        {
            if (_rebindingOperation != null)
            {
                _rebindingOperation.Cancel();
                return true;
            }
            return _rebindCanceledFrame == Time.frameCount;
        }

        private bool HasDuplicateKeyboardBinding(
            KeyboardBindingView selected,
            string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                return true;
            }
            for (int index = 0; index < keyboardBindings.Length; index++)
            {
                KeyboardBindingView other = keyboardBindings[index];
                if (other == selected ||
                    !TryResolveBinding(other, out InputAction action, out int bindingIndex))
                {
                    continue;
                }
                if (string.Equals(
                        action.bindings[bindingIndex].effectivePath,
                        candidatePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryResolveBinding(
            KeyboardBindingView view,
            out InputAction action,
            out int bindingIndex)
        {
            action = _settings.InputActions.FindAction(view.ActionName, false);
            bindingIndex = -1;
            if (action == null || !Guid.TryParse(view.BindingId, out Guid bindingId))
            {
                return false;
            }
            for (int index = 0; index < action.bindings.Count; index++)
            {
                if (action.bindings[index].id == bindingId)
                {
                    bindingIndex = index;
                    return true;
                }
            }
            return false;
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            SetStatus(Screen.fullScreen ? "전체 화면을 요청했습니다." : "창 모드로 전환했습니다.");
            WebGlHarnessReporter.Report("settings-fullscreen-toggled");
        }

        private void ResetDefaults()
        {
            CancelRebind();
            _settings.ResetToDefaults();
            RefreshAll();
            SetStatus("설정을 기본값으로 복원했습니다.");
            WebGlHarnessReporter.Report("settings-defaults-restored");
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private static string FormatPercent(float value) =>
            $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }
}
