using System;
using DG.Tweening;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeCameraShake : MonoBehaviour
    {
        public const float DefaultMaxAmplitude = 0.25f;

        [SerializeField]
        private Transform shakeTarget;

        [SerializeField]
        [Min(0f)]
        private float maxAmplitude = DefaultMaxAmplitude;

        private Tween _shakeTween;
        private float _progress;
        private float _duration;
        private float _amplitude;
        private float _frequency;
        private Vector3 _appliedOffset;

        public Transform ShakeTarget => shakeTarget;

        public float MaxAmplitude => maxAmplitude;

        public bool IsShaking => _shakeTween != null && _shakeTween.IsActive();

        public float ActiveAmplitude => _amplitude;

        public Vector3 AppliedOffset => _appliedOffset;

        public void Configure(
            Transform target,
            float authoredMaxAmplitude = DefaultMaxAmplitude)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeCameraShake before changing its configuration.");
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (authoredMaxAmplitude < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(authoredMaxAmplitude));
            }

            shakeTarget = target;
            maxAmplitude = authoredMaxAmplitude;
        }

        public bool Play(float amplitude, float duration, float frequency)
        {
            if (shakeTarget == null)
            {
                throw new InvalidOperationException(
                    "PrototypeCameraShake requires a shake-target reference.");
            }
            if (amplitude <= 0f || duration <= 0f || frequency <= 0f ||
                maxAmplitude <= 0f)
            {
                return false;
            }

            float requestedAmplitude = Mathf.Min(amplitude, maxAmplitude);
            float remainingAmplitude = IsShaking
                ? _amplitude * Mathf.Clamp01(1f - _progress)
                : 0f;

            KillTween();
            _progress = 0f;
            _duration = duration;
            _amplitude = Mathf.Max(requestedAmplitude, remainingAmplitude);
            _frequency = frequency;
            _shakeTween = DOTween.To(
                    () => _progress,
                    value => _progress = value,
                    1f,
                    duration)
                .SetEase(Ease.Linear)
                .OnComplete(CompleteShake);
            return true;
        }

        public void Stop()
        {
            KillTween();
            RestoreTargetPosition();
            ClearState();
        }

        private void LateUpdate()
        {
            if (shakeTarget == null)
            {
                return;
            }

            Vector3 baseLocalPosition = shakeTarget.localPosition - _appliedOffset;
            Vector3 nextOffset = IsShaking
                ? EvaluateOffset(_progress, _duration, _amplitude, _frequency)
                : Vector3.zero;
            shakeTarget.localPosition = baseLocalPosition + nextOffset;
            _appliedOffset = nextOffset;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }

        private void OnValidate()
        {
            maxAmplitude = Mathf.Max(0f, maxAmplitude);
        }

        private void CompleteShake()
        {
            _shakeTween = null;
            RestoreTargetPosition();
            ClearState();
        }

        private void KillTween()
        {
            if (_shakeTween == null)
            {
                return;
            }

            _shakeTween.Kill(false);
            _shakeTween = null;
        }

        private void RestoreTargetPosition()
        {
            if (shakeTarget != null && _appliedOffset != Vector3.zero)
            {
                shakeTarget.localPosition -= _appliedOffset;
            }
            _appliedOffset = Vector3.zero;
        }

        private void ClearState()
        {
            _progress = 0f;
            _duration = 0f;
            _amplitude = 0f;
            _frequency = 0f;
        }

        private static Vector3 EvaluateOffset(
            float progress,
            float duration,
            float amplitude,
            float frequency)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            float elapsed = clampedProgress * duration;
            float fade = 1f - clampedProgress;
            float baseAngle = elapsed * frequency * Mathf.PI * 2f;
            float x = Mathf.Sin(baseAngle);
            float y = Mathf.Sin(baseAngle * 1.37f);
            return new Vector3(x, y, 0f) * (amplitude * fade);
        }
    }
}
