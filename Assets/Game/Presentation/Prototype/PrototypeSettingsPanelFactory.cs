using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    public static class PrototypeSettingsPanelFactory
    {
        public const int KeyboardBindingCount = 8;

        private static readonly (string Label, string Action, string BindingId)[]
            KeyboardBindingDefinitions =
            {
                ("위로 이동", BombSwapInputActionNames.Move, "0748632d-5703-4556-8b80-6e94e4db6b42"),
                ("아래로 이동", BombSwapInputActionNames.Move, "479adb8d-b1b7-40c0-9900-3c1a1531bea5"),
                ("왼쪽 이동", BombSwapInputActionNames.Move, "3f5a4d01-be9e-465b-a969-fe26d2a25f94"),
                ("오른쪽 이동", BombSwapInputActionNames.Move, "9775c482-4489-4d74-9808-11dce0263701"),
                ("폭탄 설치", BombSwapInputActionNames.PlaceBomb, "24d48abd-8c97-47c9-9ccc-2dddad0ea27c"),
                ("폭탄 교체", BombSwapInputActionNames.SwapBomb, "7aa5d5cf-fbf6-4762-a8a5-29917b368c7d"),
                ("일시정지", BombSwapInputActionNames.Pause, "afedc0a1-6906-45b8-90ce-f0eebf188ab2"),
                ("결과 재시작", BombSwapInputActionNames.RestartRun, "e4f7d305-22d9-42f0-a19f-2ab3c59ecc5d"),
            };

        public static PrototypeSettingsPanelPresenter Create(
            Transform parent,
            string objectName = "SettingsPanel")
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            RectTransform root = PrototypeUiFactory.CreateRect(objectName, parent);
            // Runtime-created pause UI adds the presenter after its child views exist.
            // Keep the root inactive so OnEnable cannot run before Configure completes.
            root.gameObject.SetActive(false);
            SetAnchors(root, new Vector2(0.14f, 0.06f), new Vector2(0.86f, 0.94f));
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.018f, 0.028f, 0.045f, 0.985f);

            TextMeshProUGUI heading = PrototypeUiFactory.CreateText(
                "HeadingText",
                root,
                40f,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetAnchors(heading.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
            heading.text = "설정";
            heading.color = new Color(1f, 0.78f, 0.26f, 1f);

            Button controlsTab = CreateButton(
                "ControlsTabButton", root, "조작", 22f,
                new Vector2(0.08f, 0.8f), new Vector2(0.48f, 0.88f));
            Button audioTab = CreateButton(
                "AudioTabButton", root, "오디오 / 화면", 22f,
                new Vector2(0.52f, 0.8f), new Vector2(0.92f, 0.88f));

            RectTransform controlsPage = PrototypeUiFactory.CreateRect("ControlsPage", root);
            SetAnchors(controlsPage, new Vector2(0.07f, 0.19f), new Vector2(0.93f, 0.78f));
            var bindingViews = new List<PrototypeSettingsPanelPresenter.KeyboardBindingView>(
                KeyboardBindingDefinitions.Length);
            float rowHeight = 1f / KeyboardBindingDefinitions.Length;
            for (int index = 0; index < KeyboardBindingDefinitions.Length; index++)
            {
                var definition = KeyboardBindingDefinitions[index];
                float top = 1f - index * rowHeight;
                float bottom = top - rowHeight + 0.012f;
                TextMeshProUGUI label = PrototypeUiFactory.CreateText(
                    $"BindingLabel{index}", controlsPage, 18f,
                    TextAlignmentOptions.MidlineLeft);
                SetAnchors(label.rectTransform, new Vector2(0.02f, bottom), new Vector2(0.52f, top));
                label.text = definition.Label;

                Button rebindButton = CreateButton(
                    $"RebindButton{index}", controlsPage, "-", 18f,
                    new Vector2(0.58f, bottom + 0.01f), new Vector2(0.98f, top - 0.01f));
                var view = new PrototypeSettingsPanelPresenter.KeyboardBindingView();
                view.Configure(
                    definition.Action,
                    definition.BindingId,
                    rebindButton,
                    rebindButton.GetComponentInChildren<TextMeshProUGUI>(true));
                bindingViews.Add(view);
            }

            RectTransform audioPage = PrototypeUiFactory.CreateRect("AudioPage", root);
            SetAnchors(audioPage, new Vector2(0.07f, 0.19f), new Vector2(0.93f, 0.78f));
            CreateSliderRow(audioPage, "전체 음량", 0.77f, out Slider master, out TextMeshProUGUI masterValue);
            CreateSliderRow(audioPage, "배경음", 0.57f, out Slider bgm, out TextMeshProUGUI bgmValue);
            CreateSliderRow(audioPage, "효과음", 0.37f, out Slider sfx, out TextMeshProUGUI sfxValue);
            CreateSliderRow(audioPage, "화면 흔들림", 0.17f, out Slider shake, out TextMeshProUGUI shakeValue);
            Button fullscreen = CreateButton(
                "FullscreenButton", audioPage, "전체 화면 전환", 20f,
                new Vector2(0.03f, 0.0f), new Vector2(0.48f, 0.12f));
            Button reset = CreateButton(
                "ResetDefaultsButton", audioPage, "기본값 복원", 20f,
                new Vector2(0.52f, 0.0f), new Vector2(0.97f, 0.12f));
            audioPage.gameObject.SetActive(false);

            TextMeshProUGUI status = PrototypeUiFactory.CreateText(
                "SettingsStatusText", root, 16f,
                TextAlignmentOptions.Center,
                FontStyles.Normal,
                TextWrappingModes.Normal);
            SetAnchors(status.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.18f));
            status.color = new Color(0.7f, 0.78f, 0.88f, 1f);

            Button back = CreateButton(
                "BackButton", root, "돌아가기", 22f,
                new Vector2(0.3f, 0.025f), new Vector2(0.7f, 0.105f));

            PrototypeSettingsPanelPresenter presenter =
                root.gameObject.AddComponent<PrototypeSettingsPanelPresenter>();
            presenter.BindAuthoredView(
                controlsPage.gameObject,
                audioPage.gameObject,
                controlsTab,
                audioTab,
                back,
                fullscreen,
                reset,
                master,
                bgm,
                sfx,
                shake,
                masterValue,
                bgmValue,
                sfxValue,
                shakeValue,
                status,
                bindingViews.ToArray());
            return presenter;
        }

        private static void CreateSliderRow(
            Transform parent,
            string labelText,
            float top,
            out Slider slider,
            out TextMeshProUGUI valueLabel)
        {
            TextMeshProUGUI label = PrototypeUiFactory.CreateText(
                labelText.Replace(" ", string.Empty) + "Label",
                parent,
                20f,
                TextAlignmentOptions.MidlineLeft);
            SetAnchors(label.rectTransform, new Vector2(0.03f, top - 0.12f), new Vector2(0.31f, top));
            label.text = labelText;

            slider = PrototypeUiFactory.CreateSlider(
                labelText.Replace(" ", string.Empty) + "Slider",
                parent,
                new Color(0.25f, 0.65f, 0.95f, 1f),
                new Color(1f, 0.78f, 0.26f, 1f));
            SetAnchors(slider.GetComponent<RectTransform>(), new Vector2(0.32f, top - 0.095f), new Vector2(0.82f, top - 0.025f));

            valueLabel = PrototypeUiFactory.CreateText(
                labelText.Replace(" ", string.Empty) + "Value",
                parent,
                18f,
                TextAlignmentOptions.Center);
            SetAnchors(valueLabel.rectTransform, new Vector2(0.84f, top - 0.12f), new Vector2(0.98f, top));
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            float fontSize,
            Vector2 min,
            Vector2 max)
        {
            Button button = PrototypeUiFactory.CreateButton(
                name,
                parent,
                label,
                fontSize,
                new Color(0.1f, 0.17f, 0.25f, 1f),
                new Color(0.2f, 0.46f, 0.65f, 1f));
            SetAnchors(button.GetComponent<RectTransform>(), min, max);
            return button;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
