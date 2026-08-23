using DG.Tweening;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeLoopingLocalMove : MonoBehaviour
    {
        public const float DefaultDuration = 1.5f;

        [SerializeField]
        private Vector3 endLocalPosition;

        [SerializeField, Min(0.01f)]
        private float duration = DefaultDuration;

        [SerializeField]
        private Ease ease = Ease.OutQuad;

        [SerializeField]
        private bool useUnscaledTime;

        private Tween _tween;
        private Vector3 _authoredLocalPosition;

        public Vector3 EndLocalPosition => endLocalPosition;

        public float Duration => duration;

        public Ease Ease => ease;

        public bool UseUnscaledTime => useUnscaledTime;

        public bool IsAnimating =>
            _tween != null &&
            _tween.IsActive() &&
            _tween.IsPlaying();

        public void Configure(
            Vector3 authoredEndLocalPosition,
            float authoredDuration = DefaultDuration,
            Ease authoredEase = Ease.OutQuad,
            bool authoredUseUnscaledTime = false)
        {
            if (Application.isPlaying)
            {
                StopAndRestore();
            }
            else
            {
                StopTween();
            }
            endLocalPosition = authoredEndLocalPosition;
            duration = Mathf.Max(0.01f, authoredDuration);
            ease = authoredEase;
            useUnscaledTime = authoredUseUnscaledTime;

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartTween();
            }
        }

        private void OnEnable()
        {
            _authoredLocalPosition = transform.localPosition;
            if (Application.isPlaying)
            {
                StartTween();
            }
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private void OnDestroy()
        {
            StopTween();
        }

        private void StartTween()
        {
            StopTween();
            if (duration <= 0f ||
                transform.localPosition == endLocalPosition)
            {
                return;
            }

            _tween = DOTween.To(
                    () => transform.localPosition,
                    value => transform.localPosition = value,
                    endLocalPosition,
                    duration)
                .SetEase(ease)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(useUnscaledTime)
                .SetTarget(this);
        }

        private void StopAndRestore()
        {
            StopTween();
            transform.localPosition = _authoredLocalPosition;
        }

        private void StopTween()
        {
            if (_tween == null)
            {
                return;
            }

            _tween.Kill(false);
            _tween = null;
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
        }
    }
}
