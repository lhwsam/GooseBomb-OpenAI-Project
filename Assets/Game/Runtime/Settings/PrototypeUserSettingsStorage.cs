using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BombSwap
{
    public static class PrototypeUserSettingsStorage
    {
        public const string MasterVolumeKey = "BombSwap.Settings.MasterVolume.v1";
        public const string BgmVolumeKey = "BombSwap.Settings.BgmVolume.v1";
        public const string SfxVolumeKey = "BombSwap.Settings.SfxVolume.v1";
        public const string ScreenShakeKey = "BombSwap.Settings.ScreenShake.v1";
        public const string InputOverridesKey = "BombSwap.Settings.InputOverrides.v1";

        public static PrototypeUserSettings Load()
        {
            PrototypeUserSettings defaults = PrototypeUserSettings.Default;
            return new PrototypeUserSettings(
                PlayerPrefs.GetFloat(MasterVolumeKey, defaults.MasterVolume),
                PlayerPrefs.GetFloat(BgmVolumeKey, defaults.BgmVolume),
                PlayerPrefs.GetFloat(SfxVolumeKey, defaults.SfxVolume),
                PlayerPrefs.GetFloat(ScreenShakeKey, defaults.ScreenShakeIntensity));
        }

        public static void Save(PrototypeUserSettings settings)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, settings.MasterVolume);
            PlayerPrefs.SetFloat(BgmVolumeKey, settings.BgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, settings.SfxVolume);
            PlayerPrefs.SetFloat(ScreenShakeKey, settings.ScreenShakeIntensity);
            PlayerPrefs.Save();
        }

        public static void ApplyInputOverrides(InputActionAsset actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            actions.RemoveAllBindingOverrides();
            string json = PlayerPrefs.GetString(InputOverridesKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                actions.LoadBindingOverridesFromJson(json, true);
            }
            catch (Exception exception)
            {
                PlayerPrefs.DeleteKey(InputOverridesKey);
                Debug.LogWarning(
                    $"Ignored invalid saved keyboard bindings: {exception.Message}");
            }
        }

        public static void SaveInputOverrides(InputActionAsset actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            string json = actions.SaveBindingOverridesAsJson();
            if (string.IsNullOrWhiteSpace(json))
            {
                PlayerPrefs.DeleteKey(InputOverridesKey);
            }
            else
            {
                PlayerPrefs.SetString(InputOverridesKey, json);
            }
            PlayerPrefs.Save();
        }

        public static void Reset(InputActionAsset actions)
        {
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(BgmVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.DeleteKey(ScreenShakeKey);
            ResetInputOverrides(actions, false);
            PlayerPrefs.Save();
        }

        public static void ResetInputOverrides(InputActionAsset actions)
        {
            ResetInputOverrides(actions, true);
        }

        private static void ResetInputOverrides(
            InputActionAsset actions,
            bool savePlayerPrefs)
        {
            PlayerPrefs.DeleteKey(InputOverridesKey);
            if (actions != null)
            {
                actions.RemoveAllBindingOverrides();
            }
            if (savePlayerPrefs)
            {
                PlayerPrefs.Save();
            }
        }
    }
}
