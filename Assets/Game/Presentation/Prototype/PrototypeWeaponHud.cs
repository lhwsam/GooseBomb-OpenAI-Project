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
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypeWeaponHudView viewPrefab;

        private PrototypeWeaponHudView _viewInstance;
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

        public PrototypeWeaponHudView ViewPrefab => viewPrefab;

        public PrototypeWeaponHudView ViewInstance => _viewInstance;

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

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypeWeaponHudView authoredViewPrefab)
        {
            Configure(gameSession);
            BindViewPrefab(authoredViewPrefab);
        }

        public void BindViewPrefab(PrototypeWeaponHudView authoredViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeWeaponHud before changing its view prefab.");
            }

            viewPrefab = authoredViewPrefab ??
                throw new ArgumentNullException(nameof(authoredViewPrefab));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || viewPrefab == null ||
                !viewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeWeaponHud requires a game session and a configured view prefab.");
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

            _viewInstance = Instantiate(viewPrefab, transform, false);
            _viewInstance.name = viewPrefab.name;
            if (!_viewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated weapon HUD view is missing required references.");
            }
            _slotBackgrounds = new Image[BombWeaponLoadout.SlotCount];
            _slotCooldownFills = new Image[BombWeaponLoadout.SlotCount];
            _slotLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
            _slotCooldownLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
            for (int slotIndex = 0; slotIndex < BombWeaponLoadout.SlotCount; slotIndex++)
            {
                _slotBackgrounds[slotIndex] =
                    _viewInstance.GetSlotBackground(slotIndex);
                _slotCooldownFills[slotIndex] =
                    _viewInstance.GetSlotCooldownFill(slotIndex);
                _slotLabels[slotIndex] = _viewInstance.GetSlotLabel(slotIndex);
                _slotCooldownLabels[slotIndex] =
                    _viewInstance.GetSlotCooldownLabel(slotIndex);
            }
            _swapLabel = _viewInstance.SwapLabel;

            _initialized = true;
            RefreshDisplay();
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
                        isActive
                            ? _viewInstance.ActiveSlotColor
                            : _viewInstance.InactiveSlotColor;
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
                _swapLabel.color = swapDeciseconds == 0
                    ? _viewInstance.ReadyColor
                    : _viewInstance.CoolingColor;
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
                _slotCooldownFills[slotIndex].color =
                    _viewInstance.CoolingColor;
                _slotCooldownLabels[slotIndex].text = "LOCKED";
                lastCooldownDeciseconds = -1;
                return;
            }

            _slotLabels[slotIndex].text =
                (slotIndex + 1) + "  " + slot.DefinitionId.Value;
            _slotCooldownFills[slotIndex].fillAmount = (float)slot.ReadyFraction;
            _slotCooldownFills[slotIndex].color = slot.IsReady
                ? _viewInstance.ReadyColor
                : _viewInstance.CoolingColor;

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
