using System;
using UnityEngine;

namespace BombSwap
{
    [Serializable]
    public readonly struct PrototypeUserSettings : IEquatable<PrototypeUserSettings>
    {
        public const float DefaultMasterVolume = 1f;
        public const float DefaultBgmVolume = 0.7f;
        public const float DefaultSfxVolume = 1f;
        public const float DefaultScreenShakeIntensity = 1f;
        public const float ScreenShakeEnabledIntensity = 1f;
        public const float ScreenShakeDisabledIntensity = 0f;

        public PrototypeUserSettings(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            float screenShakeIntensity)
        {
            MasterVolume = Mathf.Clamp01(masterVolume);
            BgmVolume = Mathf.Clamp01(bgmVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
            ScreenShakeIntensity = screenShakeIntensity > 0.001f
                ? ScreenShakeEnabledIntensity
                : ScreenShakeDisabledIntensity;
        }

        public float MasterVolume { get; }

        public float BgmVolume { get; }

        public float SfxVolume { get; }

        public float ScreenShakeIntensity { get; }

        public bool IsScreenShakeEnabled =>
            ScreenShakeIntensity > ScreenShakeDisabledIntensity;

        public static PrototypeUserSettings Default => new PrototypeUserSettings(
            DefaultMasterVolume,
            DefaultBgmVolume,
            DefaultSfxVolume,
            DefaultScreenShakeIntensity);

        public PrototypeUserSettings WithMasterVolume(float value) =>
            new PrototypeUserSettings(value, BgmVolume, SfxVolume, ScreenShakeIntensity);

        public PrototypeUserSettings WithBgmVolume(float value) =>
            new PrototypeUserSettings(MasterVolume, value, SfxVolume, ScreenShakeIntensity);

        public PrototypeUserSettings WithSfxVolume(float value) =>
            new PrototypeUserSettings(MasterVolume, BgmVolume, value, ScreenShakeIntensity);

        public PrototypeUserSettings WithScreenShakeIntensity(float value) =>
            new PrototypeUserSettings(MasterVolume, BgmVolume, SfxVolume, value);

        public PrototypeUserSettings WithScreenShakeEnabled(bool isEnabled) =>
            WithScreenShakeIntensity(
                isEnabled
                    ? ScreenShakeEnabledIntensity
                    : ScreenShakeDisabledIntensity);

        public float ScaleScreenShake(float authoredAmplitude) =>
            Mathf.Max(0f, authoredAmplitude) * ScreenShakeIntensity;

        public bool Equals(PrototypeUserSettings other) =>
            Mathf.Approximately(MasterVolume, other.MasterVolume) &&
            Mathf.Approximately(BgmVolume, other.BgmVolume) &&
            Mathf.Approximately(SfxVolume, other.SfxVolume) &&
            Mathf.Approximately(ScreenShakeIntensity, other.ScreenShakeIntensity);

        public override bool Equals(object obj) =>
            obj is PrototypeUserSettings other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            MasterVolume,
            BgmVolume,
            SfxVolume,
            ScreenShakeIntensity);
    }
}
