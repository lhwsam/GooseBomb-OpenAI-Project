using System;
using System.Collections.Generic;
using System.Globalization;
using BombSwap.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHealthHud : MonoBehaviour
    {
        public const float DefaultBossIntroVerticalOffset = 72f;

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypeHealthHudView viewPrefab;

        private PrototypeHealthHudView _viewInstance;
        private GameObject _bossPanelObject;
        private RectTransform _bossPanelRectTransform;
        private CanvasGroup _bossPanelCanvasGroup;
        private Vector2 _bossPanelRestingAnchoredPosition;
        private RectTransform _playerHeartContainer;
        private PrototypeHealthHeartView _playerHeartPrefab;
        private readonly List<PrototypeHealthHeartView> _playerHearts = new();
        private Image _bossHealthFill;
        private TextMeshProUGUI _bossNameLabel;
        private TextMeshProUGUI _bossPhaseLabel;
        private TextMeshProUGUI _bossHealthValueLabel;
        private TextMeshProUGUI _combatRewardLabel;
        private PrototypeDungeonRoomBinder _roomBinder;
        private bool _isSubscribed;
        private string _bossNameText = "BOSS";

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
            _bossPanelObject != null &&
            _bossPanelObject.activeSelf &&
            (_bossPanelCanvasGroup == null || _bossPanelCanvasGroup.alpha > 0.001f);

        public float BossPanelAlpha =>
            _bossPanelCanvasGroup != null ? _bossPanelCanvasGroup.alpha : 0f;

        public Vector2 BossPanelAnchoredPosition =>
            _bossPanelRectTransform != null
                ? _bossPanelRectTransform.anchoredPosition
                : Vector2.zero;

        public Vector2 BossPanelRestingAnchoredPosition =>
            _bossPanelRestingAnchoredPosition;

        public bool IsBossIntroPrepared { get; private set; }

        public int DisplayedPlayerHeartCount => DisplayedPlayerMaxHealth;

        public int DisplayedFilledPlayerHeartCount => DisplayedPlayerHealth;

        public int InstantiatedPlayerHeartCount => _playerHearts.Count;

        public float BossHealthFillFraction =>
            _bossHealthFill != null ? _bossHealthFill.fillAmount : 0f;

        public string BossNameText =>
            _bossNameLabel != null ? _bossNameLabel.text : _bossNameText;

        public string BossPhaseText =>
            _bossPhaseLabel != null
                ? _bossPhaseLabel.text
                : GetBossPhaseText(DisplayedBossHealth, DisplayedBossPhase);

        public string BossHealthValueText =>
            _bossHealthValueLabel != null
                ? _bossHealthValueLabel.text
                : GetBossHealthValueText(
                    DisplayedBossHealth,
                    DisplayedBossMaxHealth);

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
                RefreshBoss(
                    session.CurrentBossHealth,
                    session.MaxBossHealth,
                    session.CurrentBossPhase);
                if (session.IsBossIntroPending)
                {
                    PrepareBossIntro();
                }
                else
                {
                    ShowBossPanelImmediately();
                }
            }
            else
            {
                _bossPanelObject.SetActive(false);
                DisplayedBossHealth = 0;
                DisplayedBossMaxHealth = 0;
                DisplayedBossPhase = BossPhase.One;
            }
        }

        public void PrepareBossIntro(
            float verticalOffset = DefaultBossIntroVerticalOffset)
        {
            if (verticalOffset < 0f || float.IsNaN(verticalOffset) ||
                float.IsInfinity(verticalOffset))
            {
                throw new ArgumentOutOfRangeException(nameof(verticalOffset));
            }
            if (!IsInitialized)
            {
                if (session == null || !session.IsReady)
                {
                    throw new InvalidOperationException(
                        "Boss HUD intro requires a ready session.");
                }
                Initialize();
            }
            if (!session.HasBoss)
            {
                throw new InvalidOperationException(
                    "Boss HUD intro requires a boss encounter.");
            }

            _bossPanelObject.SetActive(true);
            _bossPanelCanvasGroup.alpha = 0f;
            _bossPanelRectTransform.anchoredPosition =
                _bossPanelRestingAnchoredPosition + (Vector2.up * verticalOffset);
            _bossHealthFill.fillAmount = 0f;
            IsBossIntroPrepared = true;
        }

        public Sequence CreateBossIntroReveal(
            float panelDuration,
            float fillDuration)
        {
            if (!IsBossIntroPrepared)
            {
                throw new InvalidOperationException(
                    "Prepare the boss HUD before creating its intro reveal.");
            }
            if (panelDuration <= 0f || float.IsNaN(panelDuration) ||
                float.IsInfinity(panelDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(panelDuration));
            }
            if (fillDuration <= 0f || float.IsNaN(fillDuration) ||
                float.IsInfinity(fillDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(fillDuration));
            }

            float targetFill = GetFraction(
                DisplayedBossHealth,
                DisplayedBossMaxHealth);
            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(
                    () => _bossPanelCanvasGroup.alpha,
                    value => _bossPanelCanvasGroup.alpha = value,
                    1f,
                    panelDuration)
                .SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(
                    () => _bossPanelRectTransform.anchoredPosition,
                    value => _bossPanelRectTransform.anchoredPosition = value,
                    _bossPanelRestingAnchoredPosition,
                    panelDuration)
                .SetEase(Ease.OutCubic));
            sequence.Append(DOTween.To(
                    () => _bossHealthFill.fillAmount,
                    value => _bossHealthFill.fillAmount = value,
                    targetFill,
                    fillDuration)
                .SetEase(Ease.OutCubic));
            sequence.OnComplete(CompleteBossIntroReveal);
            return sequence;
        }

        private void RefreshPlayer(int currentHealth, int maxHealth)
        {
            ValidateHealth(currentHealth, maxHealth, nameof(currentHealth));
            DisplayedPlayerHealth = currentHealth;
            DisplayedPlayerMaxHealth = maxHealth;
            EnsurePlayerHeartCapacity(maxHealth);
            for (int index = 0; index < _playerHearts.Count; index++)
            {
                bool isUsed = index < maxHealth;
                PrototypeHealthHeartView heart = _playerHearts[index];
                heart.gameObject.SetActive(isUsed);
                if (isUsed)
                {
                    heart.SetFilled(index < currentHealth);
                }
            }
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

            ValidateHealth(currentHealth, maxHealth, nameof(currentHealth));
            _bossPanelObject.SetActive(true);
            DisplayedBossHealth = currentHealth;
            DisplayedBossMaxHealth = maxHealth;
            DisplayedBossPhase = phase;
            _bossHealthFill.fillAmount = GetFraction(currentHealth, maxHealth);
            _bossPhaseLabel.text = GetBossPhaseText(currentHealth, phase);
            _bossHealthValueLabel.text =
                GetBossHealthValueText(currentHealth, maxHealth);
        }

        private void RefreshCombatRewardTokens(int tokenCount)
        {
            if (tokenCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenCount));
            }

            DisplayedCombatRewardTokenCount = tokenCount;
            _combatRewardLabel.text =
                tokenCount.ToString(CultureInfo.InvariantCulture);
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
            _bossPanelRectTransform =
                _bossPanelObject.GetComponent<RectTransform>() ??
                throw new InvalidOperationException(
                    "Boss HUD panel requires a RectTransform.");
            _bossPanelCanvasGroup =
                _bossPanelObject.GetComponent<CanvasGroup>();
            if (_bossPanelCanvasGroup == null)
            {
                _bossPanelCanvasGroup =
                    _bossPanelObject.AddComponent<CanvasGroup>();
            }
            _bossPanelRestingAnchoredPosition =
                _bossPanelRectTransform.anchoredPosition;
            _playerHeartContainer = _viewInstance.PlayerHeartContainer;
            _playerHeartPrefab = _viewInstance.PlayerHeartPrefab;
            _bossHealthFill = _viewInstance.BossHealthFill;
            _bossNameLabel = _viewInstance.BossNameLabel;
            _bossPhaseLabel = _viewInstance.BossPhaseLabel;
            _bossHealthValueLabel = _viewInstance.BossHealthValueLabel;
            _combatRewardLabel = _viewInstance.CombatRewardLabel;
            _bossNameText = GetAuthoredBossName(_bossNameLabel.text);
            _bossNameLabel.text = _bossNameText;

            PrototypeHealthHeartView[] authoredHearts =
                _playerHeartContainer.GetComponentsInChildren<
                    PrototypeHealthHeartView>(true);
            for (int index = 0; index < authoredHearts.Length; index++)
            {
                _playerHearts.Add(authoredHearts[index]);
            }
        }

        private void ShowBossPanelImmediately()
        {
            _bossPanelObject.SetActive(true);
            _bossPanelCanvasGroup.alpha = 1f;
            _bossPanelRectTransform.anchoredPosition =
                _bossPanelRestingAnchoredPosition;
            _bossHealthFill.fillAmount = GetFraction(
                DisplayedBossHealth,
                DisplayedBossMaxHealth);
            IsBossIntroPrepared = false;
        }

        private void CompleteBossIntroReveal()
        {
            ShowBossPanelImmediately();
        }

        private void EnsurePlayerHeartCapacity(int maxHealth)
        {
            if (maxHealth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            while (_playerHearts.Count < maxHealth)
            {
                PrototypeHealthHeartView heart = Instantiate(
                    _playerHeartPrefab,
                    _playerHeartContainer,
                    false);
                heart.name = "PlayerHeart" +
                    (_playerHearts.Count + 1).ToString(
                        "00",
                        CultureInfo.InvariantCulture);
                _playerHearts.Add(heart);
            }
        }

        private static float GetFraction(int currentHealth, int maxHealth)
        {
            return maxHealth > 0
                ? Mathf.Clamp01((float)currentHealth / maxHealth)
                : 0f;
        }

        private static void ValidateHealth(
            int currentHealth,
            int maxHealth,
            string currentHealthParameterName)
        {
            if (maxHealth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }
            if (currentHealth < 0 || currentHealth > maxHealth)
            {
                throw new ArgumentOutOfRangeException(currentHealthParameterName);
            }
        }

        private static string GetAuthoredBossName(string authoredText)
        {
            if (string.IsNullOrWhiteSpace(authoredText))
            {
                return "BOSS";
            }

            int separatorIndex = authoredText.IndexOf('|');
            string name = separatorIndex >= 0
                ? authoredText.Substring(0, separatorIndex)
                : authoredText;
            name = name.Trim();
            return name.Length > 0 ? name : "BOSS";
        }

        private static string GetBossPhaseText(
            int currentHealth,
            BossPhase phase)
        {
            return currentHealth > 0
                ? "PHASE " + GetPhaseNumber(phase)
                : "DEFEATED";
        }

        private static string GetBossHealthValueText(
            int currentHealth,
            int maxHealth)
        {
            return currentHealth.ToString(CultureInfo.InvariantCulture) +
                " / " +
                maxHealth.ToString(CultureInfo.InvariantCulture);
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
