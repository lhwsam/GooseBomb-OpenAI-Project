using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
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
        private Button keyboardResetButton;

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private Slider masterSlider;

        [SerializeField]
        private Slider bgmSlider;

        [SerializeField]
        private Slider sfxSlider;

        [SerializeField]
        [FormerlySerializedAs("screenShakeSlider")]
        private Button screenShakeButton;

        [SerializeField]
        private TextMeshProUGUI masterValueLabel;

        [SerializeField]
        private TextMeshProUGUI bgmValueLabel;

        [SerializeField]
        private TextMeshProUGUI sfxValueLabel;

        [SerializeField]
        private TextMeshProUGUI screenShakeValueLabel;

        [SerializeField]
        private KeyboardBindingView[] keyboardBindings = Array.Empty<KeyboardBindingView>();

        private static readonly Color DuplicateBindingColor =
            new Color(1f, 0.34f, 0.22f, 1f);

        public const string ScreenShakeEnabledLabel = "켜짐";
        public const string ScreenShakeDisabledLabel = "꺼짐";

        private const string DuplicateBindingMessage = "이미 사용 중";
        private const float DuplicateBindingShakeDistance = 8f;
        private const float DuplicateBindingShakeDuration = 0.32f;
        private const float DuplicateBindingNoticeDuration = 0.55f;

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
        private Sequence _duplicateBindingSequence;
        private KeyboardBindingView _duplicateBindingView;
        private Vector2 _duplicateBindingBasePosition;
        private Color _duplicateBindingBaseColor;

        public bool HasAuthoredViewReferences =>
            controlsPage != null &&
            audioPage != null &&
            controlsTabButton != null &&
            audioTabButton != null &&
            backButton != null &&
            fullscreenButton != null &&
            keyboardResetButton != null &&
            keyboardResetButton.transform.IsChildOf(controlsPage.transform) &&
            resetButton != null &&
            masterSlider != null &&
            bgmSlider != null &&
            sfxSlider != null &&
            screenShakeButton != null &&
            screenShakeButton.transform.IsChildOf(audioPage.transform) &&
            masterValueLabel != null &&
            bgmValueLabel != null &&
            sfxValueLabel != null &&
            screenShakeValueLabel != null &&
            screenShakeValueLabel.transform.IsChildOf(
                screenShakeButton.transform) &&
            keyboardBindings != null &&
            keyboardBindings.Length == PrototypeSettingsPanelFactory.KeyboardBindingCount;

        public bool IsOpen => gameObject.activeSelf;

        public bool IsControlsPageVisible => controlsPage != null && controlsPage.activeSelf;

        public bool IsRebinding => _rebindingOperation != null;

        public int KeyboardBindingCount =>
            keyboardBindings != null ? keyboardBindings.Length : 0;

        public KeyboardBindingView GetKeyboardBinding(int index)
        {
            if (keyboardBindings == null)
            {
                throw new InvalidOperationException(
                    "Keyboard binding views are not configured.");
            }

            return keyboardBindings[index];
        }

        public PrototypeUserSettingsRuntime Settings => _settings;

        public Button BackButton => backButton;

        public GameObject ControlsPage => controlsPage;

        public Button KeyboardResetButton => keyboardResetButton;

        public GameObject AudioPage => audioPage;

        public Button ScreenShakeButton => screenShakeButton;

        public TextMeshProUGUI ScreenShakeValueLabel => screenShakeValueLabel;

        public bool IsDuplicateBindingFeedbackPlaying =>
            _duplicateBindingSequence != null &&
            _duplicateBindingSequence.IsActive();

        public void BindAuthoredView(
            GameObject authoredControlsPage,
            GameObject authoredAudioPage,
            Button authoredControlsTabButton,
            Button authoredAudioTabButton,
            Button authoredBackButton,
            Button authoredFullscreenButton,
            Button authoredKeyboardResetButton,
            Button authoredResetButton,
            Slider authoredMasterSlider,
            Slider authoredBgmSlider,
            Slider authoredSfxSlider,
            Button authoredScreenShakeButton,
            TextMeshProUGUI authoredMasterValueLabel,
            TextMeshProUGUI authoredBgmValueLabel,
            TextMeshProUGUI authoredSfxValueLabel,
            TextMeshProUGUI authoredScreenShakeValueLabel,
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
            keyboardResetButton = authoredKeyboardResetButton;
            resetButton = authoredResetButton;
            masterSlider = authoredMasterSlider;
            bgmSlider = authoredBgmSlider;
            sfxSlider = authoredSfxSlider;
            screenShakeButton = authoredScreenShakeButton;
            masterValueLabel = authoredMasterValueLabel;
            bgmValueLabel = authoredBgmValueLabel;
            sfxValueLabel = authoredSfxValueLabel;
            screenShakeValueLabel = authoredScreenShakeValueLabel;
            keyboardBindings = authoredKeyboardBindings ??
                throw new ArgumentNullException(nameof(authoredKeyboardBindings));
        }

        public void BindScreenShakeToggle(
            Button authoredScreenShakeButton,
            TextMeshProUGUI authoredScreenShakeValueLabel)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable the settings panel before changing the screen-shake toggle.");
            }

            screenShakeButton = authoredScreenShakeButton ??
                throw new ArgumentNullException(nameof(authoredScreenShakeButton));
            screenShakeValueLabel = authoredScreenShakeValueLabel ??
                throw new ArgumentNullException(nameof(authoredScreenShakeValueLabel));
            if (!screenShakeValueLabel.transform.IsChildOf(
                    screenShakeButton.transform))
            {
                throw new ArgumentException(
                    "Screen-shake value label must be a child of its button.",
                    nameof(authoredScreenShakeValueLabel));
            }
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
            StopDuplicateBindingFeedback();
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
        }

        public void ShowAudioPage()
        {
            controlsPage.SetActive(false);
            audioPage.SetActive(true);
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
            keyboardResetButton.onClick.AddListener(ResetKeyboardBindings);
            resetButton.onClick.AddListener(ResetDefaults);
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            screenShakeButton.onClick.AddListener(ToggleScreenShake);
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
            keyboardResetButton.onClick.RemoveListener(ResetKeyboardBindings);
            resetButton.onClick.RemoveListener(ResetDefaults);
            masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            screenShakeButton.onClick.RemoveListener(ToggleScreenShake);
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

        private void ToggleScreenShake()
        {
            _settings.SetScreenShakeEnabled(
                !_settings.Current.IsScreenShakeEnabled);
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
            masterValueLabel.text = FormatPercent(settings.MasterVolume);
            bgmValueLabel.text = FormatPercent(settings.BgmVolume);
            sfxValueLabel.text = FormatPercent(settings.SfxVolume);
            screenShakeValueLabel.text = settings.IsScreenShakeEnabled
                ? ScreenShakeEnabledLabel
                : ScreenShakeDisabledLabel;
            _synchronizing = false;
        }

        private void RefreshBindingLabels()
        {
            for (int index = 0; index < keyboardBindings.Length; index++)
            {
                RefreshBindingLabel(keyboardBindings[index]);
            }
        }

        private void BeginRebind(KeyboardBindingView view)
        {
            if (_rebindingOperation != null ||
                !TryResolveBinding(view, out InputAction action, out int bindingIndex))
            {
                return;
            }

            StopDuplicateBindingFeedback();
            _activeBinding = view;
            _rebindingAction = action;
            _rebindingActionWasEnabled = action.enabled;
            _previousOverridePath = action.bindings[bindingIndex].overridePath;
            action.Disable();
            view.ValueLabel.text = "키 입력...";

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
            bool duplicateBinding = false;

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
                    duplicateBinding = true;
                    if (string.IsNullOrEmpty(previousOverridePath))
                    {
                        action.RemoveBindingOverride(bindingIndex);
                    }
                    else
                    {
                        action.ApplyBindingOverride(bindingIndex, previousOverridePath);
                    }
                    WebGlHarnessReporter.Report("settings-key-duplicate");
                }
                else
                {
                    _settings.SaveInputOverrides();
                    WebGlHarnessReporter.Report("settings-key-rebound");
                }
            }
            else
            {
                _rebindCanceledFrame = Time.frameCount;
            }

            if (completedAction != null && actionWasEnabled)
            {
                completedAction.Enable();
            }
            RefreshBindingLabels();
            if (duplicateBinding && completedView != null)
            {
                PlayDuplicateBindingFeedback(completedView);
            }
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
            WebGlHarnessReporter.Report("settings-fullscreen-toggled");
        }

        private void ResetKeyboardBindings()
        {
            CancelRebind();
            StopDuplicateBindingFeedback();
            _settings.ResetKeyboardBindingsToDefaults();
            RefreshBindingLabels();
            Select(keyboardResetButton);
            WebGlHarnessReporter.Report("settings-keyboard-defaults-restored");
        }

        private void ResetDefaults()
        {
            CancelRebind();
            StopDuplicateBindingFeedback();
            _settings.ResetToDefaults();
            RefreshAll();
            WebGlHarnessReporter.Report("settings-defaults-restored");
        }

        private void PlayDuplicateBindingFeedback(KeyboardBindingView view)
        {
            StopDuplicateBindingFeedback();

            RectTransform target = view.Button.transform as RectTransform;
            if (target == null || view.ValueLabel == null)
            {
                return;
            }

            _duplicateBindingView = view;
            _duplicateBindingBasePosition = target.anchoredPosition;
            _duplicateBindingBaseColor = view.ValueLabel.color;
            view.ValueLabel.text = DuplicateBindingMessage;

            float segmentDuration = DuplicateBindingShakeDuration / 5f;
            _duplicateBindingSequence = DOTween.Sequence()
                .Append(TweenAnchoredPosition(
                    target,
                    _duplicateBindingBasePosition +
                        new Vector2(DuplicateBindingShakeDistance, 0f),
                    segmentDuration))
                .Append(TweenAnchoredPosition(
                    target,
                    _duplicateBindingBasePosition +
                        new Vector2(-6f, 0f),
                    segmentDuration))
                .Append(TweenAnchoredPosition(
                    target,
                    _duplicateBindingBasePosition + new Vector2(4f, 0f),
                    segmentDuration))
                .Append(TweenAnchoredPosition(
                    target,
                    _duplicateBindingBasePosition + new Vector2(-2f, 0f),
                    segmentDuration))
                .Append(TweenAnchoredPosition(
                    target,
                    _duplicateBindingBasePosition,
                    segmentDuration))
                .Insert(0f, TweenGraphicColor(
                    view.ValueLabel,
                    DuplicateBindingColor,
                    0.08f))
                .AppendInterval(DuplicateBindingNoticeDuration)
                .Append(TweenGraphicColor(
                    view.ValueLabel,
                    _duplicateBindingBaseColor,
                    0.12f))
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(CompleteDuplicateBindingFeedback);
        }

        private void CompleteDuplicateBindingFeedback()
        {
            RestoreDuplicateBindingVisual();
            _duplicateBindingSequence = null;
            _duplicateBindingView = null;
        }

        private void StopDuplicateBindingFeedback()
        {
            if (_duplicateBindingSequence != null)
            {
                _duplicateBindingSequence.Kill(false);
                _duplicateBindingSequence = null;
            }

            if (_duplicateBindingView == null)
            {
                return;
            }

            RestoreDuplicateBindingVisual();
            _duplicateBindingView = null;
        }

        private void RestoreDuplicateBindingVisual()
        {
            if (_duplicateBindingView == null)
            {
                return;
            }

            RectTransform target =
                _duplicateBindingView.Button.transform as RectTransform;
            if (target != null)
            {
                target.anchoredPosition = _duplicateBindingBasePosition;
            }
            if (_duplicateBindingView.ValueLabel != null)
            {
                _duplicateBindingView.ValueLabel.color =
                    _duplicateBindingBaseColor;
                RefreshBindingLabel(_duplicateBindingView);
            }
        }

        private void RefreshBindingLabel(KeyboardBindingView view)
        {
            if (!TryResolveBinding(
                    view,
                    out InputAction action,
                    out int bindingIndex))
            {
                view.ValueLabel.text = "누락";
                return;
            }

            view.ValueLabel.text = action.GetBindingDisplayString(bindingIndex);
        }

        private static Tween TweenAnchoredPosition(
            RectTransform target,
            Vector2 desiredPosition,
            float duration) =>
            DOTween.To(
                    () => target.anchoredPosition,
                    value => target.anchoredPosition = value,
                    desiredPosition,
                    duration)
                .SetEase(Ease.InOutQuad);

        private static Tween TweenGraphicColor(
            Graphic target,
            Color desiredColor,
            float duration) =>
            DOTween.To(
                    () => target.color,
                    value => target.color = value,
                    desiredColor,
                    duration)
                .SetEase(Ease.OutQuad);

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
