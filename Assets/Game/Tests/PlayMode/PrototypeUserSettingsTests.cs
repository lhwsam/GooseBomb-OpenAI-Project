using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeUserSettingsTests
    {
        private readonly string[] _floatKeys =
        {
            PrototypeUserSettingsStorage.MasterVolumeKey,
            PrototypeUserSettingsStorage.BgmVolumeKey,
            PrototypeUserSettingsStorage.SfxVolumeKey,
            PrototypeUserSettingsStorage.ScreenShakeKey
        };

        private bool[] _hadFloatValues;
        private float[] _floatValues;
        private bool _hadInputOverrides;
        private string _inputOverrides;
        private InputActionAsset _actions;

        [SetUp]
        public void SetUp()
        {
            _hadFloatValues = new bool[_floatKeys.Length];
            _floatValues = new float[_floatKeys.Length];
            for (int index = 0; index < _floatKeys.Length; index++)
            {
                _hadFloatValues[index] = PlayerPrefs.HasKey(_floatKeys[index]);
                _floatValues[index] = PlayerPrefs.GetFloat(_floatKeys[index]);
            }
            _hadInputOverrides = PlayerPrefs.HasKey(
                PrototypeUserSettingsStorage.InputOverridesKey);
            _inputOverrides = PlayerPrefs.GetString(
                PrototypeUserSettingsStorage.InputOverridesKey,
                string.Empty);

            PrototypeUserSettingsStorage.Reset(null);
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = _actions.AddActionMap("Gameplay");
            InputAction action = map.AddAction("PlaceBomb", InputActionType.Button);
            action.AddBinding("<Keyboard>/z");
        }

        [TearDown]
        public void TearDown()
        {
            if (_actions != null)
            {
                Object.DestroyImmediate(_actions);
            }

            for (int index = 0; index < _floatKeys.Length; index++)
            {
                if (_hadFloatValues[index])
                {
                    PlayerPrefs.SetFloat(_floatKeys[index], _floatValues[index]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(_floatKeys[index]);
                }
            }
            if (_hadInputOverrides)
            {
                PlayerPrefs.SetString(
                    PrototypeUserSettingsStorage.InputOverridesKey,
                    _inputOverrides);
            }
            else
            {
                PlayerPrefs.DeleteKey(
                    PrototypeUserSettingsStorage.InputOverridesKey);
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void ValueObject_ClampsVolumesAndScalesFutureScreenShake()
        {
            var settings = new PrototypeUserSettings(-1f, 0.25f, 2f, 0.4f);

            Assert.That(settings.MasterVolume, Is.Zero);
            Assert.That(settings.BgmVolume, Is.EqualTo(0.25f));
            Assert.That(settings.SfxVolume, Is.EqualTo(1f));
            Assert.That(settings.ScreenShakeIntensity, Is.EqualTo(0.4f));
            Assert.That(settings.ScaleScreenShake(2f), Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(settings.ScaleScreenShake(-2f), Is.Zero);
            Assert.That(
                PrototypeUserSettingsRuntime.LinearToDecibels(0f),
                Is.EqualTo(PrototypeUserSettingsRuntime.MutedDecibels));
            Assert.That(
                PrototypeUserSettingsRuntime.LinearToDecibels(1f),
                Is.Zero.Within(0.0001f));
        }

        [Test]
        public void Storage_RoundTripsSettingsAndKeyboardOverrides()
        {
            var expected = new PrototypeUserSettings(0.8f, 0.3f, 0.6f, 0.2f);
            InputAction action = _actions.FindAction("PlaceBomb", true);
            action.ApplyBindingOverride(0, "<Keyboard>/q");

            PrototypeUserSettingsStorage.Save(expected);
            PrototypeUserSettingsStorage.SaveInputOverrides(_actions);
            action.RemoveBindingOverride(0);
            PrototypeUserSettingsStorage.ApplyInputOverrides(_actions);

            Assert.That(PrototypeUserSettingsStorage.Load(), Is.EqualTo(expected));
            Assert.That(action.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/q"));
        }

        [Test]
        public void InvalidOverrideJson_IsDiscardedWithoutBreakingInput()
        {
            PlayerPrefs.SetString(
                PrototypeUserSettingsStorage.InputOverridesKey,
                "not-valid-json");
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    "Ignored invalid saved keyboard bindings"));

            PrototypeUserSettingsStorage.ApplyInputOverrides(_actions);

            Assert.That(
                PlayerPrefs.HasKey(PrototypeUserSettingsStorage.InputOverridesKey),
                Is.False);
            Assert.That(
                _actions.FindAction("PlaceBomb", true).bindings[0].effectivePath,
                Is.EqualTo("<Keyboard>/z"));
        }

        [Test]
        public void ResetInputOverrides_RestoresKeysWithoutChangingOtherSettings()
        {
            var settings = new PrototypeUserSettings(0.8f, 0.3f, 0.6f, 0.2f);
            InputAction action = _actions.FindAction("PlaceBomb", true);
            action.ApplyBindingOverride(0, "<Keyboard>/q");
            PrototypeUserSettingsStorage.Save(settings);
            PrototypeUserSettingsStorage.SaveInputOverrides(_actions);

            PrototypeUserSettingsStorage.ResetInputOverrides(_actions);

            Assert.That(action.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/z"));
            Assert.That(
                PlayerPrefs.HasKey(PrototypeUserSettingsStorage.InputOverridesKey),
                Is.False);
            Assert.That(PrototypeUserSettingsStorage.Load(), Is.EqualTo(settings));
        }
    }
}
