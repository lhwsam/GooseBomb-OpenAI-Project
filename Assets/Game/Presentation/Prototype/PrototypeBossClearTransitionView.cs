using System;
using DG.Tweening;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBossClearTransitionView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private RectTransform topCurtain;

        [SerializeField]
        private RectTransform bottomCurtain;

        private Vector2 _topClosedPosition;
        private Vector2 _bottomClosedPosition;
        private bool _positionsCaptured;

        public Canvas Canvas => canvas;

        public CanvasGroup CanvasGroup => canvasGroup;

        public RectTransform TopCurtain => topCurtain;

        public RectTransform BottomCurtain => bottomCurtain;

        public bool HasRequiredReferences =>
            canvas != null &&
            canvasGroup != null &&
            topCurtain != null &&
            bottomCurtain != null &&
            topCurtain != bottomCurtain;

        public bool IsPrepared { get; private set; }

        public bool IsClosed { get; private set; }

        public void BindAuthoredView(
            Canvas authoredCanvas,
            CanvasGroup authoredCanvasGroup,
            RectTransform authoredTopCurtain,
            RectTransform authoredBottomCurtain)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Boss-clear transition view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            canvasGroup = authoredCanvasGroup ??
                throw new ArgumentNullException(nameof(authoredCanvasGroup));
            topCurtain = authoredTopCurtain ??
                throw new ArgumentNullException(nameof(authoredTopCurtain));
            bottomCurtain = authoredBottomCurtain ??
                throw new ArgumentNullException(nameof(authoredBottomCurtain));
            if (topCurtain == bottomCurtain)
            {
                throw new ArgumentException(
                    "Boss-clear transition curtains must be different RectTransforms.");
            }
        }

        public void PrepareForClosing()
        {
            ValidateReferences();
            CaptureClosedPositions();
            Canvas.ForceUpdateCanvases();

            float topTravel = Mathf.Max(1f, topCurtain.rect.height);
            float bottomTravel = Mathf.Max(1f, bottomCurtain.rect.height);
            topCurtain.anchoredPosition =
                _topClosedPosition + (Vector2.up * topTravel);
            bottomCurtain.anchoredPosition =
                _bottomClosedPosition + (Vector2.down * bottomTravel);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            IsPrepared = true;
            IsClosed = false;
        }

        public Sequence CreateCloseTween(float duration)
        {
            ValidateFinitePositive(duration, nameof(duration));
            if (!IsPrepared)
            {
                throw new InvalidOperationException(
                    "Prepare the boss-clear transition before closing it.");
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(DOTween.To(
                    () => topCurtain.anchoredPosition,
                    value => topCurtain.anchoredPosition = value,
                    _topClosedPosition,
                    duration)
                .SetEase(Ease.InCubic));
            sequence.Join(DOTween.To(
                    () => bottomCurtain.anchoredPosition,
                    value => bottomCurtain.anchoredPosition = value,
                    _bottomClosedPosition,
                    duration)
                .SetEase(Ease.InCubic));
            sequence.OnComplete(() => IsClosed = true);
            return sequence;
        }

        public void PlayClose(float duration)
        {
            CreateCloseTween(duration);
        }

        public Tween CreateRevealTween(float duration)
        {
            ValidateFinitePositive(duration, nameof(duration));
            if (!IsPrepared)
            {
                throw new InvalidOperationException(
                    "Prepare the boss-clear transition before revealing the next view.");
            }

            return DOTween.To(
                    () => canvasGroup.alpha,
                    value => canvasGroup.alpha = value,
                    0f,
                    duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        }

        public void PlayReveal(float duration)
        {
            CreateRevealTween(duration);
        }

        private void CaptureClosedPositions()
        {
            if (_positionsCaptured)
            {
                return;
            }

            _topClosedPosition = topCurtain.anchoredPosition;
            _bottomClosedPosition = bottomCurtain.anchoredPosition;
            _positionsCaptured = true;
        }

        private void ValidateReferences()
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeBossClearTransitionView requires a Canvas, CanvasGroup, and two curtain RectTransforms.");
            }
        }

        private static void ValidateFinitePositive(float value, string parameterName)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and positive.");
            }
        }
    }
}
