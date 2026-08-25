using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeUserSettingsRuntime : MonoBehaviour
    {
        public const string MasterVolumeParameter = "MasterVolume";
        public const string BgmVolumeParameter = "BgmVolume";
        public const string SfxVolumeParameter = "SfxVolume";
        public const float MutedDecibels = -80f;

        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private AudioMixer audioMixer;

        private PrototypeUserSettings _current;
        private bool _isLoaded;
        private bool _hasStarted;

        public event Action<PrototypeUserSettings> Changed;

        public InputActionAsset InputActions => inputActions;

        public AudioMixer AudioMixer => audioMixer;

        public PrototypeUserSettings Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public bool HasRequiredReferences => inputActions != null && audioMixer != null;

        public void Configure(InputActionAsset authoredInputActions, AudioMixer authoredAudioMixer)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeUserSettingsRuntime before changing its configuration.");
            }

            inputActions = authoredInputActions ??
                throw new ArgumentNullException(nameof(authoredInputActions));
            audioMixer = authoredAudioMixer ??
                throw new ArgumentNullException(nameof(authoredAudioMixer));
            _isLoaded = false;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            EnsureLoaded();
            if (_hasStarted)
            {
                ApplyAudio();
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            EnsureLoaded();
            _hasStarted = true;
            ApplyAudio();
        }

        private void OnDisable()
        {
            if (Application.isPlaying && _isLoaded)
            {
                Persist();
            }
        }

        public void SetMasterVolume(float value) =>
            SetCurrent(Current.WithMasterVolume(value));

        public void SetBgmVolume(float value) =>
            SetCurrent(Current.WithBgmVolume(value));

        public void SetSfxVolume(float value) =>
            SetCurrent(Current.WithSfxVolume(value));

        public void SetScreenShakeIntensity(float value) =>
            SetCurrent(Current.WithScreenShakeIntensity(value));

        public void SetScreenShakeEnabled(bool isEnabled) =>
            SetCurrent(Current.WithScreenShakeEnabled(isEnabled));

        public float ScaleScreenShake(float authoredAmplitude) =>
            Current.ScaleScreenShake(authoredAmplitude);

        public void Persist()
        {
            EnsureLoaded();
            PrototypeUserSettingsStorage.Save(_current);
            PrototypeUserSettingsStorage.SaveInputOverrides(inputActions);
        }

        public void ResetToDefaults()
        {
            PrototypeUserSettingsStorage.Reset(inputActions);
            _current = PrototypeUserSettings.Default;
            _isLoaded = true;
            if (_hasStarted)
            {
                ApplyAudio();
            }
            Changed?.Invoke(_current);
        }

        public void SaveInputOverrides()
        {
            PrototypeUserSettingsStorage.SaveInputOverrides(inputActions);
        }

        public void ResetKeyboardBindingsToDefaults()
        {
            EnsureLoaded();
            PrototypeUserSettingsStorage.ResetInputOverrides(inputActions);
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeUserSettingsRuntime requires input actions and an AudioMixer.");
            }

            _current = PrototypeUserSettingsStorage.Load();
            PrototypeUserSettingsStorage.ApplyInputOverrides(inputActions);
            _isLoaded = true;
        }

        private void SetCurrent(PrototypeUserSettings settings)
        {
            if (settings.Equals(Current))
            {
                return;
            }
            _current = settings;
            if (_hasStarted)
            {
                ApplyAudio();
            }
            Changed?.Invoke(_current);
        }

        private void ApplyAudio()
        {
            SetMixerVolume(MasterVolumeParameter, _current.MasterVolume);
            SetMixerVolume(BgmVolumeParameter, _current.BgmVolume);
            SetMixerVolume(SfxVolumeParameter, _current.SfxVolume);
        }

        private void SetMixerVolume(string parameter, float linearVolume)
        {
            float decibels = LinearToDecibels(linearVolume);
            if (!audioMixer.SetFloat(parameter, decibels))
            {
                Debug.LogError(
                    $"AudioMixer is missing exposed parameter '{parameter}'.",
                    this);
            }
        }

        public static float LinearToDecibels(float linearVolume)
        {
            float clamped = Mathf.Clamp01(linearVolume);
            return clamped <= 0.0001f
                ? MutedDecibels
                : Mathf.Log10(clamped) * 20f;
        }
    }
}
