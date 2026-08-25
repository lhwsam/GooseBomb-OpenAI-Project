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
        private Image[] _slotBombIcons;
        private GameObject[] _slotCooldownPanels;
        private Image[] _slotCooldownFills;
        private TextMeshProUGUI[] _slotCooldownLabels;
        private GameObject[] _slotEmptyIndicators;
        private GameObject[] _slotSelections;
        private Image[] _slotKeyIcons;
        private string[] _lastDefinitionIds;
        private int _lastActiveSlot = -1;
        private int _lastFirstCooldownDeciseconds = -1;
        private int _lastSecondCooldownDeciseconds = -1;
        private bool _initialized;

        public PrototypeGameSession Session => session;

        public PrototypeWeaponHudView ViewPrefab => viewPrefab;

        public PrototypeWeaponHudView ViewInstance => _viewInstance;

        public bool IsInitialized => _initialized;

        public int DisplayedActiveSlotIndex => _lastActiveSlot;

        public float FirstSlotCooldownFraction =>
            _initialized ? _slotCooldownFills[0].fillAmount : 0f;

        public float SecondSlotCooldownFraction =>
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

            _slotBombIcons = new Image[BombWeaponLoadout.SlotCount];
            _slotCooldownPanels = new GameObject[BombWeaponLoadout.SlotCount];
            _slotCooldownFills = new Image[BombWeaponLoadout.SlotCount];
            _slotCooldownLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
            _slotEmptyIndicators = new GameObject[BombWeaponLoadout.SlotCount];
            _slotSelections = new GameObject[BombWeaponLoadout.SlotCount];
            _slotKeyIcons = new Image[BombWeaponLoadout.SlotCount];
            _lastDefinitionIds = new string[BombWeaponLoadout.SlotCount];
            for (int slotIndex = 0; slotIndex < BombWeaponLoadout.SlotCount; slotIndex++)
            {
                _slotBombIcons[slotIndex] =
                    _viewInstance.GetSlotBombIcon(slotIndex);
                _slotCooldownPanels[slotIndex] =
                    _viewInstance.GetSlotCooldownPanel(slotIndex);
                _slotCooldownFills[slotIndex] =
                    _viewInstance.GetSlotCooldownFill(slotIndex);
                _slotCooldownLabels[slotIndex] =
                    _viewInstance.GetSlotCooldownLabel(slotIndex);
                _slotEmptyIndicators[slotIndex] =
                    _viewInstance.GetSlotEmptyIndicator(slotIndex);
                _slotSelections[slotIndex] =
                    _viewInstance.GetSlotSelection(slotIndex);
                _slotKeyIcons[slotIndex] =
                    _viewInstance.GetSlotKeyIcon(slotIndex);
            }

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
                    SetActiveIfChanged(
                        _slotSelections[slotIndex],
                        slotIndex == activeSlot);
                    _slotKeyIcons[slotIndex].sprite = slotIndex == activeSlot
                        ? _viewInstance.SelectedSlotKeyIcon
                        : _viewInstance.UnselectedSlotKeyIcon;
                }
                _lastActiveSlot = activeSlot;
            }

            TimeSpan swapCooldown = session.BombSwapCooldown;
            TimeSpan swapCooldownRemaining = session.BombSwapCooldownRemaining;
            RefreshSlot(
                0,
                activeSlot,
                swapCooldown,
                swapCooldownRemaining,
                ref _lastFirstCooldownDeciseconds);
            RefreshSlot(
                1,
                activeSlot,
                swapCooldown,
                swapCooldownRemaining,
                ref _lastSecondCooldownDeciseconds);
        }

        private void RefreshSlot(
            int slotIndex,
            int activeSlot,
            TimeSpan swapCooldown,
            TimeSpan swapCooldownRemaining,
            ref int lastCooldownDeciseconds)
        {
            BombWeaponSlotSnapshot slot = session.GetBombSlot(slotIndex);
            if (!slot.HasDefinition)
            {
                _lastDefinitionIds[slotIndex] = null;
                _slotBombIcons[slotIndex].sprite = null;
                SetActiveIfChanged(_slotBombIcons[slotIndex].gameObject, false);
                SetActiveIfChanged(_slotEmptyIndicators[slotIndex], true);
                SetActiveIfChanged(_slotCooldownPanels[slotIndex], false);
                _slotCooldownFills[slotIndex].fillAmount = 0f;
                _slotCooldownLabels[slotIndex].text = string.Empty;
                lastCooldownDeciseconds = -1;
                return;
            }

            string definitionId = slot.DefinitionId.Value;
            if (!string.Equals(
                    _lastDefinitionIds[slotIndex],
                    definitionId,
                    StringComparison.Ordinal))
            {
                PrototypeBombDefinitionAsset definition =
                    session.GetBombDefinitionForSlot(slotIndex);
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"Weapon HUD could not resolve bomb definition {definitionId}.");
                }

                _slotBombIcons[slotIndex].sprite =
                    _viewInstance.GetBombIcon(definition.ExplosionShape);
                _lastDefinitionIds[slotIndex] = definitionId;
            }

            SetActiveIfChanged(_slotBombIcons[slotIndex].gameObject, true);
            SetActiveIfChanged(_slotEmptyIndicators[slotIndex], false);

            TimeSpan displayedCooldown = slot.PlacementCooldown;
            TimeSpan displayedRemaining = slot.PlacementCooldownRemaining;
            if (slotIndex != activeSlot &&
                swapCooldownRemaining > TimeSpan.Zero &&
                swapCooldownRemaining >= displayedRemaining)
            {
                displayedCooldown = swapCooldown;
                displayedRemaining = swapCooldownRemaining;
            }

            _slotCooldownFills[slotIndex].fillAmount =
                CalculateRemainingFraction(displayedRemaining, displayedCooldown);
            int cooldownDeciseconds = ToRemainingDeciseconds(displayedRemaining);
            bool isCooling = cooldownDeciseconds > 0;
            SetActiveIfChanged(_slotCooldownPanels[slotIndex], isCooling);
            if (cooldownDeciseconds == lastCooldownDeciseconds)
            {
                return;
            }

            _slotCooldownLabels[slotIndex].text = isCooling
                ? FormatDeciseconds(cooldownDeciseconds)
                : string.Empty;
            lastCooldownDeciseconds = cooldownDeciseconds;
        }

        private static float CalculateRemainingFraction(
            TimeSpan remaining,
            TimeSpan cooldown)
        {
            if (remaining <= TimeSpan.Zero || cooldown <= TimeSpan.Zero)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(remaining.Ticks / (double)cooldown.Ticks));
        }

        private static void SetActiveIfChanged(GameObject target, bool active)
        {
            if (target.activeSelf != active)
            {
                target.SetActive(active);
            }
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
