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
        public const string GameFontAssetName = "DungGeunMo SDF";

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
            return button;
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
