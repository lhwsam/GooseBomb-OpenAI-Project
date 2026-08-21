using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BombSwap
{
    public static class PrototypeUiFactory
    {
        public const string GameFontAssetName = "DungGeunMo";
        public const string AlternateGameFontAssetName = "DNFBitBitv2";
        public const float ReferenceWidth = 960f;
        public const float ReferenceHeight = 600f;
        public const float ReferenceMatchWidthOrHeight = 0.5f;

        public static Vector2 ReferenceResolution =>
            new Vector2(ReferenceWidth, ReferenceHeight);

        public static void ConfigureCanvasScaler(CanvasScaler scaler)
        {
            if (scaler == null)
            {
                throw new ArgumentNullException(nameof(scaler));
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ReferenceMatchWidthOrHeight;
        }

        public static bool HasReferenceCanvasScale(CanvasScaler scaler)
        {
            return scaler != null &&
                   scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                   scaler.referenceResolution == ReferenceResolution &&
                   scaler.screenMatchMode ==
                   CanvasScaler.ScreenMatchMode.MatchWidthOrHeight &&
                   Mathf.Approximately(
                       scaler.matchWidthOrHeight,
                       ReferenceMatchWidthOrHeight);
        }

        public static TMP_FontAsset RequireGameFont()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                throw new InvalidOperationException(
                    $"TextMesh Pro default font must be configured to {GameFontAssetName}.");
            }
            if (!string.Equals(
                    font.name,
                    GameFontAssetName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"TextMesh Pro default font is '{font.name}', expected '{GameFontAssetName}'.");
            }
            return font;
        }

        public static bool IsSupportedGameFont(TMP_FontAsset font)
        {
            return font != null &&
                   (string.Equals(
                        font.name,
                        GameFontAssetName,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        font.name,
                        AlternateGameFontAssetName,
                        StringComparison.Ordinal));
        }

        public static RectTransform CreateRect(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        public static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            TextAlignmentOptions alignment,
            FontStyles fontStyle = FontStyles.Normal,
            TextWrappingModes wrappingMode = TextWrappingModes.NoWrap)
        {
            RectTransform rect = CreateRect(objectName, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = RequireGameFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.textWrappingMode = wrappingMode;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            float fontSize,
            Color normalColor,
            Color highlightedColor)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = normalColor;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.selectedColor = highlightedColor;
            colors.pressedColor = new Color(
                highlightedColor.r * 0.82f,
                highlightedColor.g * 0.82f,
                highlightedColor.b * 0.82f,
                1f);
            colors.disabledColor = new Color(
                normalColor.r,
                normalColor.g,
                normalColor.b,
                0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI buttonLabel = CreateText(
                "Label",
                rect,
                fontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = new Vector2(18f, 4f);
            buttonLabel.rectTransform.offsetMax = new Vector2(-18f, -4f);
            buttonLabel.text = label;

            PrototypeButtonScaleFeedback feedback =
                rect.gameObject.AddComponent<PrototypeButtonScaleFeedback>();
            feedback.Configure(rect);
            return button;
        }

        public static Slider CreateSlider(
            string objectName,
            Transform parent,
            Color fillColor,
            Color handleColor)
        {
            RectTransform root = CreateRect(objectName, parent);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.09f, 0.12f, 0.17f, 1f);

            RectTransform fillArea = CreateRect("FillArea", root);
            fillArea.anchorMin = new Vector2(0f, 0.2f);
            fillArea.anchorMax = new Vector2(1f, 0.8f);
            fillArea.offsetMin = new Vector2(8f, 0f);
            fillArea.offsetMax = new Vector2(-8f, 0f);

            RectTransform fill = CreateRect("Fill", fillArea);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;

            RectTransform handleArea = CreateRect("HandleSlideArea", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);

            RectTransform handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(18f, 34f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = handleColor;

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            return slider;
        }

        public static EventSystem EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return EventSystem.current;
            }

            var eventSystemObject = new GameObject(
                "PrototypeEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            return eventSystemObject.GetComponent<EventSystem>();
        }
    }
}
