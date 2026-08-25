using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class PrototypeButtonScaleFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        public const float DefaultHoverScaleMultiplier = 1.06f;
        public const float DefaultPressedScaleMultiplier = 0.96f;
        public const float DefaultTransitionDuration = 0.1f;
        public const float DefaultSubmitPulseDuration = 0.08f;

        [SerializeField]
        private RectTransform visualTarget;

        [SerializeField, Min(1f)]
        private float hoverScaleMultiplier = DefaultHoverScaleMultiplier;

        [SerializeField, Range(0.01f, 1f)]
        private float pressedScaleMultiplier = DefaultPressedScaleMultiplier;

        [SerializeField, Min(0f)]
        private float transitionDuration = DefaultTransitionDuration;

        [SerializeField, Min(0f)]
        private float submitPulseDuration = DefaultSubmitPulseDuration;

        [Header("Optional color feedback")]
        [SerializeField]
        private Graphic colorTarget;

        [SerializeField]
        private Color startColor = new Color(
            0.7372549f,
            0.7372549f,
            0.7372549f,
            1f);

        [SerializeField]
        private Color targetColor = new Color(1f, 0.72f, 0.22f, 1f);

        [Header("Optional hover visuals")]
        [SerializeField]
        private GameObject[] hoverVisualTargets = Array.Empty<GameObject>();

        [Header("Button audio")]
        [SerializeField]
        private PrototypeUiButtonAudioPlayer audioPlayer;

        private Button _button;
        private Vector3 _baseScale = Vector3.one;
        private Tween _scaleTween;
        private Tween _colorTween;
        private float _submitPulseRemaining;
        private bool _pointerInside;
        private bool _pointerPressed;
        private bool _selected;
        private bool _ignorePointerSelection;
        private bool _wasInteractable;
        private VisualState _visualState = VisualState.Normal;

        private enum VisualState
        {
            Normal,
            Hover,
            Pressed
        }

        public RectTransform VisualTarget => visualTarget;

        public float HoverScaleMultiplier => hoverScaleMultiplier;

        public float PressedScaleMultiplier => pressedScaleMultiplier;

        public float TransitionDuration => transitionDuration;

        public float SubmitPulseDuration => submitPulseDuration;

        public Graphic ColorTarget => colorTarget;

        public Color StartColor => startColor;

        public Color TargetColor => targetColor;

        public int HoverVisualTargetCount => hoverVisualTargets?.Length ?? 0;

        public PrototypeUiButtonAudioPlayer AudioPlayer => audioPlayer;

        public void Configure(
            RectTransform authoredVisualTarget,
            float authoredHoverScaleMultiplier = DefaultHoverScaleMultiplier,
            float authoredPressedScaleMultiplier = DefaultPressedScaleMultiplier,
            float authoredTransitionDuration = DefaultTransitionDuration,
            float authoredSubmitPulseDuration = DefaultSubmitPulseDuration)
        {
            RectTransform configuredTarget = authoredVisualTarget != null
                ? authoredVisualTarget
                : transform as RectTransform;
            hoverScaleMultiplier = Mathf.Max(1f, authoredHoverScaleMultiplier);
            pressedScaleMultiplier = Mathf.Clamp(
                authoredPressedScaleMultiplier,
                0.01f,
                1f);
            transitionDuration = Mathf.Max(0f, authoredTransitionDuration);
            submitPulseDuration = Mathf.Max(0f, authoredSubmitPulseDuration);

            if (Application.isPlaying)
            {
                KillActiveTweens();
                RestoreAuthoredVisuals();
                visualTarget = configuredTarget;
                CaptureAuthoredVisuals();
                ResetInteractionState();
                ApplyVisualState(VisualState.Normal, true);
                return;
            }

            visualTarget = configuredTarget;
        }

        public void ConfigureColorFeedback(
            Graphic authoredColorTarget,
            Color authoredStartColor,
            Color authoredTargetColor)
        {
            KillColorTween();
            RestoreConfiguredColor();
            colorTarget = authoredColorTarget;
            startColor = authoredStartColor;
            targetColor = authoredTargetColor;
            ApplyVisualState(ResolveVisualState(), true);
        }

        public void EnsureColorTarget(Graphic fallbackColorTarget)
        {
            if (colorTarget != null || fallbackColorTarget == null)
            {
                return;
            }

            colorTarget = fallbackColorTarget;
            ApplyVisualState(ResolveVisualState(), true);
        }

        public void ConfigureHoverVisuals(GameObject[] authoredHoverVisualTargets)
        {
            hoverVisualTargets = authoredHoverVisualTargets ??
                Array.Empty<GameObject>();

            if (Application.isPlaying)
            {
                SetHoverVisualsActive(
                    ResolveVisualState() != VisualState.Normal);
            }
        }

        public void ConfigureAudio(PrototypeUiButtonAudioPlayer authoredAudioPlayer)
        {
            audioPlayer = authoredAudioPlayer;
        }

        public GameObject GetHoverVisualTarget(int index)
        {
            if (hoverVisualTargets == null ||
                index < 0 ||
                index >= hoverVisualTargets.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return hoverVisualTargets[index];
        }

        public bool HasHoverVisualTargets(GameObject[] expectedTargets)
        {
            int expectedCount = expectedTargets?.Length ?? 0;
            if (HoverVisualTargetCount != expectedCount)
            {
                return false;
            }

            for (int index = 0; index < expectedCount; index++)
            {
                if (hoverVisualTargets[index] != expectedTargets[index])
                {
                    return false;
                }
            }

            return true;
        }

        public void SuppressSelectionVisualUntilInteraction()
        {
            _ignorePointerSelection = true;
            RefreshVisualState();
        }

        public bool HasConfiguration(
            RectTransform expectedVisualTarget,
            float expectedHoverScaleMultiplier = DefaultHoverScaleMultiplier,
            float expectedPressedScaleMultiplier = DefaultPressedScaleMultiplier,
            float expectedTransitionDuration = DefaultTransitionDuration,
            float expectedSubmitPulseDuration = DefaultSubmitPulseDuration)
        {
            return visualTarget == expectedVisualTarget &&
                   Mathf.Approximately(
                       hoverScaleMultiplier,
                       expectedHoverScaleMultiplier) &&
                   Mathf.Approximately(
                       pressedScaleMultiplier,
                       expectedPressedScaleMultiplier) &&
                   Mathf.Approximately(
                       transitionDuration,
                       expectedTransitionDuration) &&
                   Mathf.Approximately(
                       submitPulseDuration,
                       expectedSubmitPulseDuration);
        }

        private void Reset()
        {
            visualTarget = transform as RectTransform;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (visualTarget == null)
            {
                visualTarget = transform as RectTransform;
            }

            if (visualTarget != null)
            {
                _baseScale = visualTarget.localScale;
            }

            _wasInteractable = _button != null && _button.IsInteractable();
        }

        private void OnEnable()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            if (visualTarget == null)
            {
                visualTarget = transform as RectTransform;
            }
            CaptureAuthoredVisuals();
            ResetInteractionState();
            _wasInteractable = _button != null && _button.IsInteractable();
            ApplyVisualState(VisualState.Normal, true);
        }

        private void OnDisable()
        {
            KillActiveTweens();
            ResetInteractionState();
            RestoreAuthoredVisuals();
        }

        private void OnDestroy()
        {
            KillActiveTweens();
            SetHoverVisualsActive(false);
        }

        private void Update()
        {
            bool interactable = _button != null && _button.IsInteractable();
            bool shouldRefresh = interactable != _wasInteractable;
            _wasInteractable = interactable;

            if (_submitPulseRemaining <= 0f)
            {
                if (shouldRefresh)
                {
                    RefreshVisualState();
                }
                return;
            }

            _submitPulseRemaining = Mathf.Max(
                0f,
                _submitPulseRemaining - Time.unscaledDeltaTime);
            if (_submitPulseRemaining <= 0f)
            {
                shouldRefresh = true;
            }

            if (shouldRefresh)
            {
                RefreshVisualState();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bool enteredInteractableButton = !_pointerInside &&
                _button != null &&
                _button.IsInteractable();
            _pointerInside = true;
            _ignorePointerSelection = true;

            if (enteredInteractableButton)
            {
                audioPlayer?.PlayHover();
            }

            EventSystem pointerEventSystem = EventSystem.current;
            if (pointerEventSystem != null &&
                pointerEventSystem.currentSelectedGameObject != gameObject)
            {
                pointerEventSystem.SetSelectedGameObject(gameObject, eventData);
            }

            RefreshVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            _pointerPressed = false;
            RefreshVisualState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null &&
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pointerPressed = true;
            _ignorePointerSelection = true;
            RefreshVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData != null &&
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pointerPressed = false;
            RefreshVisualState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            if (!_pointerInside)
            {
                _ignorePointerSelection = false;
            }

            RefreshVisualState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _ignorePointerSelection = false;
            RefreshVisualState();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_button != null && _button.IsInteractable())
            {
                _ignorePointerSelection = false;
                _submitPulseRemaining = submitPulseDuration;
                RefreshVisualState();
            }
        }

        private void RefreshVisualState()
        {
            ApplyVisualState(ResolveVisualState(), false);
        }

        private VisualState ResolveVisualState()
        {
            if (_button == null || !_button.IsInteractable())
            {
                return VisualState.Normal;
            }
            if ((_pointerPressed && _pointerInside) ||
                _submitPulseRemaining > 0f)
            {
                return VisualState.Pressed;
            }
            if (_pointerInside || (_selected && !_ignorePointerSelection))
            {
                return VisualState.Hover;
            }

            return VisualState.Normal;
        }

        private void ApplyVisualState(VisualState state, bool immediate)
        {
            if (!immediate && state == _visualState)
            {
                return;
            }

            _visualState = state;
            float multiplier = 1f;
            Color desiredColor = startColor;
            switch (state)
            {
                case VisualState.Hover:
                    multiplier = hoverScaleMultiplier;
                    desiredColor = targetColor;
                    break;
                case VisualState.Pressed:
                    multiplier = pressedScaleMultiplier;
                    desiredColor = targetColor;
                    break;
            }

            TweenScale(_baseScale * multiplier, immediate);
            TweenColor(desiredColor, immediate);
            SetHoverVisualsActive(state != VisualState.Normal);
        }

        private void TweenScale(Vector3 targetScale, bool immediate)
        {
            KillScaleTween();
            if (visualTarget == null)
            {
                return;
            }
            if (immediate || transitionDuration <= 0f ||
                !isActiveAndEnabled)
            {
                visualTarget.localScale = targetScale;
                return;
            }

            _scaleTween = DOTween.To(
                    () => visualTarget.localScale,
                    value => visualTarget.localScale = value,
                    targetScale,
                    transitionDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void TweenColor(Color targetColor, bool immediate)
        {
            KillColorTween();
            if (colorTarget == null)
            {
                return;
            }
            if (immediate || transitionDuration <= 0f ||
                !isActiveAndEnabled)
            {
                colorTarget.color = targetColor;
                return;
            }

            _colorTween = DOTween.To(
                    () => colorTarget.color,
                    value => colorTarget.color = value,
                    targetColor,
                    transitionDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void CaptureAuthoredVisuals()
        {
            if (visualTarget != null)
            {
                _baseScale = visualTarget.localScale;
            }
        }

        private void RestoreAuthoredVisuals()
        {
            if (visualTarget != null)
            {
                visualTarget.localScale = _baseScale;
            }

            RestoreConfiguredColor();
            SetHoverVisualsActive(false);
        }

        private void SetHoverVisualsActive(bool active)
        {
            if (hoverVisualTargets == null)
            {
                return;
            }

            for (int index = 0;
                 index < hoverVisualTargets.Length;
                 index++)
            {
                GameObject target = hoverVisualTargets[index];
                if (target != null &&
                    target != gameObject &&
                    target.transform.IsChildOf(transform) &&
                    target.activeSelf != active)
                {
                    target.SetActive(active);
                }
            }
        }

        private void RestoreConfiguredColor()
        {
            if (colorTarget != null)
            {
                colorTarget.color = startColor;
            }
        }

        private void KillActiveTweens()
        {
            KillScaleTween();
            KillColorTween();
        }

        private void KillScaleTween()
        {
            if (_scaleTween == null)
            {
                return;
            }

            _scaleTween.Kill(false);
            _scaleTween = null;
        }

        private void KillColorTween()
        {
            if (_colorTween == null)
            {
                return;
            }

            _colorTween.Kill(false);
            _colorTween = null;
        }

        private void ResetInteractionState()
        {
            _pointerInside = false;
            _pointerPressed = false;
            _selected = false;
            _ignorePointerSelection = false;
            _submitPulseRemaining = 0f;
            _visualState = VisualState.Normal;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            hoverScaleMultiplier = Mathf.Max(1f, hoverScaleMultiplier);
            pressedScaleMultiplier = Mathf.Clamp(
                pressedScaleMultiplier,
                0.01f,
                1f);
            transitionDuration = Mathf.Max(0f, transitionDuration);
            submitPulseDuration = Mathf.Max(0f, submitPulseDuration);
        }
#endif
    }
}
