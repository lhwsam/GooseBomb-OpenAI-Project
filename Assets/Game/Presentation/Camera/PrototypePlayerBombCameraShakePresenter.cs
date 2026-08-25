using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerBombCameraShakePresenter : MonoBehaviour
    {
        public const float DefaultAmplitude = 0.16f;
        public const float DefaultDuration = 0.18f;
        public const float DefaultFrequency = 24f;

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypeUserSettingsRuntime settings;

        [SerializeField]
        private PrototypeCameraShake cameraShake;

        [SerializeField]
        [Min(0f)]
        private float amplitude = DefaultAmplitude;

        [SerializeField]
        [Min(0f)]
        private float duration = DefaultDuration;

        [SerializeField]
        [Min(0f)]
        private float frequency = DefaultFrequency;

        private int _observedPlayerExplosionCount;
        private int _playedShakeCount;
        private float _lastEffectiveAmplitude;

        public PrototypeGameSession Session => session;

        public PrototypeUserSettingsRuntime Settings => settings;

        public PrototypeCameraShake CameraShake => cameraShake;

        public float Amplitude => amplitude;

        public float Duration => duration;

        public float Frequency => frequency;

        public int ObservedPlayerExplosionCount => _observedPlayerExplosionCount;

        public int PlayedShakeCount => _playedShakeCount;

        public float LastEffectiveAmplitude => _lastEffectiveAmplitude;

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypeUserSettingsRuntime settingsRuntime,
            PrototypeCameraShake shake,
            float authoredAmplitude = DefaultAmplitude,
            float authoredDuration = DefaultDuration,
            float authoredFrequency = DefaultFrequency)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerBombCameraShakePresenter before changing its configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (settingsRuntime == null)
            {
                throw new ArgumentNullException(nameof(settingsRuntime));
            }
            if (shake == null)
            {
                throw new ArgumentNullException(nameof(shake));
            }
            if (authoredAmplitude < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(authoredAmplitude));
            }
            if (authoredDuration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(authoredDuration));
            }
            if (authoredFrequency < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(authoredFrequency));
            }

            session = gameSession;
            settings = settingsRuntime;
            cameraShake = shake;
            amplitude = authoredAmplitude;
            duration = authoredDuration;
            frequency = authoredFrequency;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || settings == null || cameraShake == null)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerBombCameraShakePresenter requires session, settings, and camera-shake references.");
            }

            session.BombExploded += OnBombExploded;
            session.PauseStateChanged += OnPauseStateChanged;
            settings.Changed += OnSettingsChanged;
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.BombExploded -= OnBombExploded;
                session.PauseStateChanged -= OnPauseStateChanged;
            }
            if (settings != null)
            {
                settings.Changed -= OnSettingsChanged;
            }
            if (cameraShake != null)
            {
                cameraShake.Stop();
            }
        }

        private void OnValidate()
        {
            amplitude = Mathf.Max(0f, amplitude);
            duration = Mathf.Max(0f, duration);
            frequency = Mathf.Max(0f, frequency);
        }

        private void OnBombExploded(BombExplosion explosion)
        {
            if (explosion.OwnerId != session.PlayerActorId)
            {
                return;
            }

            _observedPlayerExplosionCount++;
            _lastEffectiveAmplitude = settings.ScaleScreenShake(amplitude);
            if (cameraShake.Play(_lastEffectiveAmplitude, duration, frequency))
            {
                _playedShakeCount++;
            }
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                cameraShake.Stop();
            }
        }

        private void OnSettingsChanged(PrototypeUserSettings current)
        {
            if (current.ScreenShakeIntensity <= 0f)
            {
                cameraShake.Stop();
            }
        }
    }
}
