using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeWeaponHudView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private Image[] slotBackgrounds;

        [SerializeField]
        private Image[] slotCooldownFills;

        [SerializeField]
        private TextMeshProUGUI[] slotLabels;

        [SerializeField]
        private TextMeshProUGUI[] slotCooldownLabels;

        [SerializeField]
        private TextMeshProUGUI swapLabel;

        [Header("Runtime state colors")]
        [SerializeField]
        private Color activeSlotColor =
            new Color(0.1f, 0.54f, 0.8f, 0.94f);

        [SerializeField]
        private Color inactiveSlotColor =
            new Color(0.12f, 0.15f, 0.2f, 0.86f);

        [SerializeField]
        private Color readyColor =
            new Color(0.22f, 0.9f, 0.46f, 1f);

        [SerializeField]
        private Color coolingColor =
            new Color(0.95f, 0.48f, 0.12f, 1f);

        public Canvas Canvas => canvas;

        public TextMeshProUGUI SwapLabel => swapLabel;

        public Color ActiveSlotColor => activeSlotColor;

        public Color InactiveSlotColor => inactiveSlotColor;

        public Color ReadyColor => readyColor;

        public Color CoolingColor => coolingColor;

        public bool HasRequiredReferences =>
            canvas != null &&
            HasSlotCount(slotBackgrounds) &&
            HasSlotCount(slotCooldownFills) &&
            HasSlotCount(slotLabels) &&
            HasSlotCount(slotCooldownLabels) &&
            swapLabel != null;

        public Image GetSlotBackground(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotBackgrounds[slotIndex];
        }

        public Image GetSlotCooldownFill(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotCooldownFills[slotIndex];
        }

        public TextMeshProUGUI GetSlotLabel(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotLabels[slotIndex];
        }

        public TextMeshProUGUI GetSlotCooldownLabel(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotCooldownLabels[slotIndex];
        }

        public void BindAuthoredView(
            Canvas authoredCanvas,
            Image[] authoredSlotBackgrounds,
            Image[] authoredSlotCooldownFills,
            TextMeshProUGUI[] authoredSlotLabels,
            TextMeshProUGUI[] authoredSlotCooldownLabels,
            TextMeshProUGUI authoredSwapLabel)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Weapon HUD view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            slotBackgrounds = RequireSlotArray(
                authoredSlotBackgrounds,
                nameof(authoredSlotBackgrounds));
            slotCooldownFills = RequireSlotArray(
                authoredSlotCooldownFills,
                nameof(authoredSlotCooldownFills));
            slotLabels = RequireSlotArray(authoredSlotLabels, nameof(authoredSlotLabels));
            slotCooldownLabels = RequireSlotArray(
                authoredSlotCooldownLabels,
                nameof(authoredSlotCooldownLabels));
            swapLabel = authoredSwapLabel ??
                throw new ArgumentNullException(nameof(authoredSwapLabel));
        }

        private static bool HasSlotCount(Array values)
        {
            return values != null && values.Length == BombWeaponLoadout.SlotCount;
        }

        private static T[] RequireSlotArray<T>(T[] values, string parameterName)
            where T : UnityEngine.Object
        {
            if (!HasSlotCount(values))
            {
                throw new ArgumentException(
                    $"Weapon HUD requires exactly {BombWeaponLoadout.SlotCount} slot references.",
                    parameterName);
            }
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == null)
                {
                    throw new ArgumentException(
                        $"Weapon HUD slot reference {index} is missing.",
                        parameterName);
                }
            }
            return values;
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= BombWeaponLoadout.SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }
    }
}
