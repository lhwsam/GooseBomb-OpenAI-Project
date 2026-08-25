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

        [Header("Slot presentation")]
        [SerializeField]
        private Image[] slotBombIcons;

        [SerializeField]
        private GameObject[] slotCooldownPanels;

        [SerializeField]
        private Image[] slotCooldownFills;

        [SerializeField]
        private TextMeshProUGUI[] slotCooldownLabels;

        [SerializeField]
        private GameObject[] slotEmptyIndicators;

        [SerializeField]
        private GameObject[] slotSelections;

        [SerializeField]
        private Image[] slotKeyIcons;

        [Header("Slot key icons")]
        [SerializeField]
        private Sprite selectedSlotKeyIcon;

        [SerializeField]
        private Sprite unselectedSlotKeyIcon;

        [Header("Bomb type icons")]
        [SerializeField]
        private Sprite crossBombIcon;

        [SerializeField]
        private Sprite areaBombIcon;

        [SerializeField]
        private Sprite lineBombIcon;

        // Compatibility-only fields from the previous text-based HUD.
        [SerializeField, HideInInspector]
        private Image[] slotBackgrounds;

        [SerializeField, HideInInspector]
        private TextMeshProUGUI[] slotLabels;

        [SerializeField, HideInInspector]
        private TextMeshProUGUI swapLabel;

        [SerializeField, HideInInspector]
        private Color activeSlotColor;

        [SerializeField, HideInInspector]
        private Color inactiveSlotColor;

        [SerializeField, HideInInspector]
        private Color readyColor;

        [SerializeField, HideInInspector]
        private Color coolingColor;

        public Canvas Canvas => canvas;

        public bool HasRequiredReferences =>
            canvas != null &&
            HasSlotCount(slotBombIcons) &&
            HasSlotCount(slotCooldownPanels) &&
            HasSlotCount(slotCooldownFills) &&
            HasSlotCount(slotCooldownLabels) &&
            HasSlotCount(slotEmptyIndicators) &&
            HasSlotCount(slotSelections) &&
            HasSlotCount(slotKeyIcons) &&
            selectedSlotKeyIcon != null &&
            unselectedSlotKeyIcon != null &&
            crossBombIcon != null &&
            areaBombIcon != null &&
            lineBombIcon != null;

        public Sprite SelectedSlotKeyIcon => selectedSlotKeyIcon;

        public Sprite UnselectedSlotKeyIcon => unselectedSlotKeyIcon;

        public Image GetSlotBombIcon(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotBombIcons[slotIndex];
        }

        public GameObject GetSlotCooldownPanel(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotCooldownPanels[slotIndex];
        }

        public Image GetSlotCooldownFill(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotCooldownFills[slotIndex];
        }

        public TextMeshProUGUI GetSlotCooldownLabel(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotCooldownLabels[slotIndex];
        }

        public GameObject GetSlotEmptyIndicator(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotEmptyIndicators[slotIndex];
        }

        public GameObject GetSlotSelection(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotSelections[slotIndex];
        }

        public Image GetSlotKeyIcon(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return slotKeyIcons[slotIndex];
        }

        public Sprite GetBombIcon(BombExplosionShape explosionShape)
        {
            switch (explosionShape)
            {
                case BombExplosionShape.Cross:
                    return crossBombIcon;
                case BombExplosionShape.SquareArea:
                    return areaBombIcon;
                case BombExplosionShape.ForwardLine:
                    return lineBombIcon;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(explosionShape),
                        explosionShape,
                        "Weapon HUD does not have an icon for this bomb shape.");
            }
        }

        public void BindAuthoredView(
            Canvas authoredCanvas,
            Image[] authoredSlotBombIcons,
            GameObject[] authoredSlotCooldownPanels,
            Image[] authoredSlotCooldownFills,
            TextMeshProUGUI[] authoredSlotCooldownLabels,
            GameObject[] authoredSlotEmptyIndicators,
            GameObject[] authoredSlotSelections,
            Sprite authoredCrossBombIcon,
            Sprite authoredAreaBombIcon,
            Sprite authoredLineBombIcon)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Weapon HUD view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            slotBombIcons = RequireSlotArray(
                authoredSlotBombIcons,
                nameof(authoredSlotBombIcons));
            slotCooldownPanels = RequireSlotArray(
                authoredSlotCooldownPanels,
                nameof(authoredSlotCooldownPanels));
            slotCooldownFills = RequireSlotArray(
                authoredSlotCooldownFills,
                nameof(authoredSlotCooldownFills));
            slotCooldownLabels = RequireSlotArray(
                authoredSlotCooldownLabels,
                nameof(authoredSlotCooldownLabels));
            slotEmptyIndicators = RequireSlotArray(
                authoredSlotEmptyIndicators,
                nameof(authoredSlotEmptyIndicators));
            slotSelections = RequireSlotArray(
                authoredSlotSelections,
                nameof(authoredSlotSelections));
            crossBombIcon = authoredCrossBombIcon ??
                throw new ArgumentNullException(nameof(authoredCrossBombIcon));
            areaBombIcon = authoredAreaBombIcon ??
                throw new ArgumentNullException(nameof(authoredAreaBombIcon));
            lineBombIcon = authoredLineBombIcon ??
                throw new ArgumentNullException(nameof(authoredLineBombIcon));
        }

        public void BindKeyIcons(
            Image[] authoredSlotKeyIcons,
            Sprite authoredSelectedSlotKeyIcon,
            Sprite authoredUnselectedSlotKeyIcon)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Weapon HUD key icons can only be authored outside Play Mode.");
            }

            slotKeyIcons = RequireSlotArray(
                authoredSlotKeyIcons,
                nameof(authoredSlotKeyIcons));
            selectedSlotKeyIcon = authoredSelectedSlotKeyIcon ??
                throw new ArgumentNullException(nameof(authoredSelectedSlotKeyIcon));
            unselectedSlotKeyIcon = authoredUnselectedSlotKeyIcon ??
                throw new ArgumentNullException(nameof(authoredUnselectedSlotKeyIcon));
        }

        private static bool HasSlotCount<T>(T[] values)
            where T : UnityEngine.Object
        {
            if (values == null || values.Length != BombWeaponLoadout.SlotCount)
            {
                return false;
            }

            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == null)
                {
                    return false;
                }
            }
            return true;
        }

        private static T[] RequireSlotArray<T>(T[] values, string parameterName)
            where T : UnityEngine.Object
        {
            if (!HasSlotCount(values))
            {
                throw new ArgumentException(
                    $"Weapon HUD requires exactly {BombWeaponLoadout.SlotCount} non-null slot references.",
                    parameterName);
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
