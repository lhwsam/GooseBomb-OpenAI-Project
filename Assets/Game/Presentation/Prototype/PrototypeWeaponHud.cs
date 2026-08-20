using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeWeaponHud : MonoBehaviour
    {
        private static readonly Color ActiveSlotColor = new Color(0.1f, 0.54f, 0.8f, 0.94f);
        private static readonly Color InactiveSlotColor = new Color(0.12f, 0.15f, 0.2f, 0.86f);
        private static readonly Color ReadyColor = new Color(0.22f, 0.9f, 0.46f, 1f);
        private static readonly Color CoolingColor = new Color(0.95f, 0.48f, 0.12f, 1f);

        [SerializeField]
        private PrototypeGameSession session;

        private Image[] _slotBackgrounds;
        private Image[] _slotCooldownFills;
        private TextMeshProUGUI[] _slotLabels;
        private TextMeshProUGUI[] _slotCooldownLabels;
        private TextMeshProUGUI _swapLabel;
        private int _lastActiveSlot = -1;
        private int _lastFirstCooldownDeciseconds = -1;
        private int _lastSecondCooldownDeciseconds = -1;
        private int _lastSwapCooldownDeciseconds = int.MinValue;
        private bool _initialized;

        public PrototypeGameSession Session => session;

        public bool IsInitialized => _initialized;

        public int DisplayedActiveSlotIndex => _lastActiveSlot;

        public float FirstSlotReadyFraction =>
            _initialized ? _slotCooldownFills[0].fillAmount : 0f;

        public float SecondSlotReadyFraction =>
            _initialized ? _slotCooldownFills[1].fillAmount : 0f;

        public void Configure(PrototypeGameSession gameSession)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeWeaponHud before changing its runtime configuration.");
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null)
            {
                throw new InvalidOperationException(
                    "PrototypeWeaponHud requires a game-session reference.");
            }

            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.Ready -= OnSessionReady;
            }
        }

        private void Update()
        {
            if (_initialized && session.IsReady)
            {
                RefreshDisplay();
            }
        }

        private void OnSessionReady()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "PrototypeWeaponHudCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreateRect("WeaponPanel", canvasObject.transform);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(24f, 24f);
            panel.sizeDelta = new Vector2(520f, 126f);
            Image panelBackground = panel.gameObject.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.025f, 0.04f, 0.82f);

            _slotBackgrounds = new Image[BombWeaponLoadout.SlotCount];
            _slotCooldownFills = new Image[BombWeaponLoadout.SlotCount];
            _slotLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
            _slotCooldownLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
            for (int slotIndex = 0; slotIndex < BombWeaponLoadout.SlotCount; slotIndex++)
            {
                CreateSlot(panel, slotIndex);
            }

            _swapLabel = PrototypeUiFactory.CreateText(
                "SwapStatus",
                panel,
                18f,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RectTransform swapRect = _swapLabel.rectTransform;
            swapRect.anchorMin = new Vector2(0f, 0f);
            swapRect.anchorMax = new Vector2(1f, 0f);
            swapRect.pivot = new Vector2(0.5f, 0f);
            swapRect.anchoredPosition = new Vector2(0f, 6f);
            swapRect.sizeDelta = new Vector2(-16f, 24f);
            _swapLabel.color = Color.white;

            _initialized = true;
            RefreshDisplay();
        }

        private void CreateSlot(RectTransform panel, int slotIndex)
        {
            RectTransform slot = CreateRect("Slot" + (slotIndex + 1), panel);
            slot.anchorMin = new Vector2(slotIndex * 0.5f, 1f);
            slot.anchorMax = new Vector2((slotIndex + 1) * 0.5f, 1f);
            slot.pivot = new Vector2(0.5f, 1f);
            slot.anchoredPosition = new Vector2(slotIndex == 0 ? 4f : -4f, -8f);
            slot.sizeDelta = new Vector2(-16f, 82f);
            Image background = slot.gameObject.AddComponent<Image>();
            background.color = InactiveSlotColor;
            Outline outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, -2f);
            _slotBackgrounds[slotIndex] = background;

            RectTransform bar = CreateRect("CooldownBar", slot);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 0f);
            bar.sizeDelta = new Vector2(0f, 12f);
            Image barBackground = bar.gameObject.AddComponent<Image>();
            barBackground.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);

            RectTransform fill = CreateRect("ReadyFill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            _slotCooldownFills[slotIndex] = fillImage;

            TextMeshProUGUI slotLabel = PrototypeUiFactory.CreateText(
                "Definition",
                slot,
                18f,
                TextAlignmentOptions.MidlineLeft);
            slotLabel.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            slotLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            slotLabel.rectTransform.offsetMin = new Vector2(10f, 0f);
            slotLabel.rectTransform.offsetMax = new Vector2(-10f, 0f);
            slotLabel.text = string.Empty;
            slotLabel.color = Color.white;
            _slotLabels[slotIndex] = slotLabel;

            TextMeshProUGUI cooldownLabel = PrototypeUiFactory.CreateText(
                "Cooldown",
                slot,
                16f,
                TextAlignmentOptions.MidlineLeft);
            cooldownLabel.rectTransform.anchorMin = new Vector2(0f, 0.14f);
            cooldownLabel.rectTransform.anchorMax = new Vector2(1f, 0.46f);
            cooldownLabel.rectTransform.offsetMin = new Vector2(10f, 0f);
            cooldownLabel.rectTransform.offsetMax = new Vector2(-10f, 0f);
            cooldownLabel.color = Color.white;
            _slotCooldownLabels[slotIndex] = cooldownLabel;
        }

        private void RefreshDisplay()
        {
            int activeSlot = session.ActiveBombSlotIndex;
            if (activeSlot != _lastActiveSlot)
            {
                for (int slotIndex = 0; slotIndex < BombWeaponLoadout.SlotCount; slotIndex++)
                {
                    bool isActive = slotIndex == activeSlot;
                    _slotBackgrounds[slotIndex].color =
                        isActive ? ActiveSlotColor : InactiveSlotColor;
                    _slotLabels[slotIndex].fontStyle =
                        isActive ? FontStyles.Bold : FontStyles.Normal;
                }

                _lastActiveSlot = activeSlot;
            }

            RefreshSlot(0, ref _lastFirstCooldownDeciseconds);
            RefreshSlot(1, ref _lastSecondCooldownDeciseconds);

            int swapDeciseconds = session.HasSecondBombSlot
                ? ToRemainingDeciseconds(session.BombSwapCooldownRemaining)
                : -1;
            if (swapDeciseconds != _lastSwapCooldownDeciseconds)
            {
                _swapLabel.text = swapDeciseconds < 0
                    ? "X  SWAP LOCKED"
                    : swapDeciseconds == 0
                        ? "X  SWAP READY"
                        : "X  SWAP  " + FormatDeciseconds(swapDeciseconds);
                _swapLabel.color = swapDeciseconds == 0 ? ReadyColor : CoolingColor;
                _lastSwapCooldownDeciseconds = swapDeciseconds;
            }
        }

        private void RefreshSlot(int slotIndex, ref int lastCooldownDeciseconds)
        {
            BombWeaponSlotSnapshot slot = session.GetBombSlot(slotIndex);
            if (!slot.HasDefinition)
            {
                _slotLabels[slotIndex].text = (slotIndex + 1) + "  EMPTY — FIND A BOMB";
                _slotLabels[slotIndex].fontStyle = FontStyles.Normal;
                _slotCooldownFills[slotIndex].fillAmount = 0f;
                _slotCooldownFills[slotIndex].color = CoolingColor;
                _slotCooldownLabels[slotIndex].text = "LOCKED";
                lastCooldownDeciseconds = -1;
                return;
            }

            _slotLabels[slotIndex].text =
                (slotIndex + 1) + "  " + slot.DefinitionId.Value;
            _slotCooldownFills[slotIndex].fillAmount = (float)slot.ReadyFraction;
            _slotCooldownFills[slotIndex].color = slot.IsReady ? ReadyColor : CoolingColor;

            int cooldownDeciseconds = ToRemainingDeciseconds(slot.PlacementCooldownRemaining);
            if (cooldownDeciseconds == lastCooldownDeciseconds)
            {
                return;
            }

            _slotCooldownLabels[slotIndex].text = cooldownDeciseconds == 0
                ? "Z  PLACE READY"
                : "COOLDOWN  " + FormatDeciseconds(cooldownDeciseconds);
            lastCooldownDeciseconds = cooldownDeciseconds;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static int ToRemainingDeciseconds(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds * 10d));
        }

        private static string FormatDeciseconds(int deciseconds)
        {
            return (deciseconds / 10) + "." + (deciseconds % 10) + "s";
        }
    }
}
