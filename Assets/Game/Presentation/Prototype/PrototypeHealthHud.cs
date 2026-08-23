using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHealthHud : MonoBehaviour
    {
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypeHealthHudView viewPrefab;

        private PrototypeHealthHudView _viewInstance;
        private GameObject _bossPanelObject;
        private Image _playerHealthFill;
        private Image _bossHealthFill;
        private TextMeshProUGUI _playerHealthLabel;
        private TextMeshProUGUI _bossHealthLabel;
        private TextMeshProUGUI _combatRewardLabel;
        private PrototypeDungeonRoomBinder _roomBinder;
        private bool _isSubscribed;

        public PrototypeGameSession Session => session;

        public PrototypeHealthHudView ViewPrefab => viewPrefab;

        public PrototypeHealthHudView ViewInstance => _viewInstance;

        public bool IsInitialized { get; private set; }

        public int DisplayedPlayerHealth { get; private set; }

        public int DisplayedPlayerMaxHealth { get; private set; }

        public int DisplayedBossHealth { get; private set; }

        public int DisplayedBossMaxHealth { get; private set; }

        public BossPhase DisplayedBossPhase { get; private set; }

        public int DisplayedCombatRewardTokenCount { get; private set; }

        public bool IsBossPanelVisible =>
            _bossPanelObject != null && _bossPanelObject.activeSelf;

        public float PlayerHealthFillFraction =>
            _playerHealthFill != null ? _playerHealthFill.fillAmount : 0f;

        public float BossHealthFillFraction =>
            _bossHealthFill != null ? _bossHealthFill.fillAmount : 0f;

        public string PlayerHealthText =>
            _playerHealthLabel != null ? _playerHealthLabel.text : string.Empty;

        public string BossHealthText =>
            _bossHealthLabel != null ? _bossHealthLabel.text : string.Empty;

        public string CombatRewardText =>
            _combatRewardLabel != null ? _combatRewardLabel.text : string.Empty;

        public void Configure(PrototypeGameSession gameSession)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeHealthHud before changing its runtime configuration.");
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        }

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypeHealthHudView authoredViewPrefab)
        {
            Configure(gameSession);
            BindViewPrefab(authoredViewPrefab);
        }

        public void BindViewPrefab(PrototypeHealthHudView authoredViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeHealthHud before changing its view prefab.");
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
                    "PrototypeHealthHud requires a game session and a configured view prefab.");
            }

            _roomBinder = GetComponent<PrototypeDungeonRoomBinder>();

            Subscribe();
            if (session.IsReady)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            session.Ready += OnSessionReady;
            session.PlayerDamaged += OnPlayerDamaged;
            session.PlayerDied += OnPlayerDied;
            session.PlayerRecovered += OnPlayerRecovered;
            session.BossDamaged += OnBossDamaged;
            session.BossPatternTransitioned += OnBossPatternTransitioned;
            if (_roomBinder != null)
            {
                _roomBinder.RoomRewardTokenCountChanged +=
                    OnCombatRewardTokenCountChanged;
            }
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || session == null)
            {
                return;
            }

            session.Ready -= OnSessionReady;
            session.PlayerDamaged -= OnPlayerDamaged;
            session.PlayerDied -= OnPlayerDied;
            session.PlayerRecovered -= OnPlayerRecovered;
            session.BossDamaged -= OnBossDamaged;
            session.BossPatternTransitioned -= OnBossPatternTransitioned;
            if (_roomBinder != null)
            {
                _roomBinder.RoomRewardTokenCountChanged -=
                    OnCombatRewardTokenCountChanged;
            }
            _isSubscribed = false;
        }

        private void OnSessionReady()
        {
            Initialize();
        }

        private void OnPlayerDamaged(PlayerDamageResult result)
        {
            RefreshPlayer(result.CurrentHealth, session.MaxHealth);
        }

        private void OnPlayerDied(PlayerDamageResult result)
        {
            RefreshPlayer(result.CurrentHealth, session.MaxHealth);
        }

        private void OnPlayerRecovered(PlayerHealthRecoveryResult result)
        {
            RefreshPlayer(result.CurrentHealth, session.MaxHealth);
        }

        private void OnBossDamaged(BossDamageResult result)
        {
            RefreshBoss(result.CurrentHealth, session.MaxBossHealth, result.Phase);
        }

        private void OnBossPatternTransitioned(BossPatternTransition transition)
        {
            RefreshBoss(
                session.CurrentBossHealth,
                session.MaxBossHealth,
                transition.Phase);
        }

        private void OnCombatRewardTokenCountChanged(int tokenCount)
        {
            RefreshCombatRewardTokens(tokenCount);
        }

        private void Initialize()
        {
            if (!IsInitialized)
            {
                CreateUi();
                IsInitialized = true;
            }

            RefreshPlayer(session.CurrentHealth, session.MaxHealth);
            RefreshCombatRewardTokens(
                _roomBinder != null ? _roomBinder.RoomRewardTokenCount : 0);
            if (session.HasBoss)
            {
                _bossPanelObject.SetActive(true);
                RefreshBoss(
                    session.CurrentBossHealth,
                    session.MaxBossHealth,
                    session.CurrentBossPhase);
            }
            else
            {
                _bossPanelObject.SetActive(false);
                DisplayedBossHealth = 0;
                DisplayedBossMaxHealth = 0;
                DisplayedBossPhase = BossPhase.One;
            }
        }

        private void RefreshPlayer(int currentHealth, int maxHealth)
        {
            DisplayedPlayerHealth = currentHealth;
            DisplayedPlayerMaxHealth = maxHealth;
            _playerHealthFill.fillAmount = GetFraction(currentHealth, maxHealth);
            _playerHealthLabel.text =
                "PLAYER HP  " + currentHealth + " / " + maxHealth;
        }

        private void RefreshBoss(
            int currentHealth,
            int maxHealth,
            BossPhase phase)
        {
            if (_bossPanelObject == null)
            {
                return;
            }

            _bossPanelObject.SetActive(true);
            DisplayedBossHealth = currentHealth;
            DisplayedBossMaxHealth = maxHealth;
            DisplayedBossPhase = phase;
            _bossHealthFill.fillAmount = GetFraction(currentHealth, maxHealth);
            _bossHealthLabel.text = currentHealth > 0
                ? "BOSS  |  PHASE " + GetPhaseNumber(phase) +
                  "  |  " + currentHealth + " / " + maxHealth
                : "BOSS DEFEATED  |  0 / " + maxHealth;
            _bossHealthLabel.color = Color.white;
        }

        private void RefreshCombatRewardTokens(int tokenCount)
        {
            if (tokenCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenCount));
            }

            DisplayedCombatRewardTokenCount = tokenCount;
            _combatRewardLabel.text = "ROOM TOKENS  " + tokenCount;
        }

        private void CreateUi()
        {
            _viewInstance = Instantiate(viewPrefab, transform, false);
            _viewInstance.name = viewPrefab.name;
            if (!_viewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated health HUD view is missing required references.");
            }

            _bossPanelObject = _viewInstance.BossPanel;
            _playerHealthFill = _viewInstance.PlayerHealthFill;
            _bossHealthFill = _viewInstance.BossHealthFill;
            _playerHealthLabel = _viewInstance.PlayerHealthLabel;
            _bossHealthLabel = _viewInstance.BossHealthLabel;
            _combatRewardLabel = _viewInstance.CombatRewardLabel;
        }

        private static float GetFraction(int currentHealth, int maxHealth)
        {
            return maxHealth > 0
                ? Mathf.Clamp01((float)currentHealth / maxHealth)
                : 0f;
        }

        private static int GetPhaseNumber(BossPhase phase)
        {
            switch (phase)
            {
                case BossPhase.One:
                    return 1;
                case BossPhase.Two:
                    return 2;
                case BossPhase.LastStand:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase),
                        phase,
                        "Unsupported boss phase.");
            }
        }
    }
}
