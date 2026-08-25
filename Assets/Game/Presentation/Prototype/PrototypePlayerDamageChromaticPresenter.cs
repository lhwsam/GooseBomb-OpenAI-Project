using System;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerDamageChromaticPresenter : MonoBehaviour
    {
        public const float DefaultPulseSeconds = 0.24f;

        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private Volume volume;
        [SerializeField, Min(0.01f)] private float pulseSeconds = DefaultPulseSeconds;

        private ChromaticAberration _chromaticAberration;
        private float _originalIntensity;
        private bool _originalOverrideState;
        private float _elapsed;
        private bool _isPulsing;

        public PrototypeGameSession Session => session;
        public Volume Volume => volume;
        public float PulseSeconds => pulseSeconds;
        public int PulseCount { get; private set; }
        public float CurrentIntensity =>
            _chromaticAberration != null ? _chromaticAberration.intensity.value : 0f;

        public void Configure(
            PrototypeGameSession gameSession,
            Volume authoredVolume,
            float authoredPulseSeconds = DefaultPulseSeconds)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerDamageChromaticPresenter before changing its configuration.");
            }
            if (authoredPulseSeconds <= 0f || float.IsNaN(authoredPulseSeconds) ||
                float.IsInfinity(authoredPulseSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(authoredPulseSeconds));
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            volume = authoredVolume ?? throw new ArgumentNullException(nameof(authoredVolume));
            pulseSeconds = authoredPulseSeconds;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || volume == null || volume.sharedProfile == null)
            {
                throw new InvalidOperationException(
                    "Player damage chromatic feedback requires session, Volume, and Volume Profile references.");
            }
            if (!volume.profile.TryGet(out _chromaticAberration))
            {
                throw new InvalidOperationException(
                    "Player damage chromatic feedback requires Chromatic Aberration in the Volume Profile.");
            }

            _originalIntensity = _chromaticAberration.intensity.value;
            _originalOverrideState = _chromaticAberration.intensity.overrideState;
            SetIntensity(0f);
            session.PlayerDamaged += OnPlayerDamaged;
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.PlayerDamaged -= OnPlayerDamaged;
            }
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.overrideState = _originalOverrideState;
                _chromaticAberration.intensity.value = _originalIntensity;
            }

            _isPulsing = false;
            _chromaticAberration = null;
        }

        private void Update()
        {
            if (!_isPulsing || _chromaticAberration == null)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(_elapsed / pulseSeconds);
            SetIntensity(EvaluateIntensity(normalized));
            if (normalized >= 1f)
            {
                _isPulsing = false;
                SetIntensity(0f);
            }
        }

        private void OnPlayerDamaged(PlayerDamageResult result)
        {
            if (!result.WasApplied)
            {
                return;
            }

            PulseCount++;
            _elapsed = 0f;
            _isPulsing = true;
            SetIntensity(0f);
        }

        private void SetIntensity(float intensity)
        {
            _chromaticAberration.intensity.overrideState = true;
            _chromaticAberration.intensity.value = Mathf.Clamp01(intensity);
        }

        public static float EvaluateIntensity(float normalizedTime)
        {
            float scaled = Mathf.Clamp01(normalizedTime) * 4f;
            int segment = Mathf.Min(Mathf.FloorToInt(scaled), 3);
            float progress = scaled - segment;
            return segment % 2 == 0 ? progress : 1f - progress;
        }
    }
}
