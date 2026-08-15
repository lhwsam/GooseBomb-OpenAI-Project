using System;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHealthHud : MonoBehaviour
    {
        private static readonly Color PanelColor =
            new Color(0.02f, 0.025f, 0.04f, 0.86f);
        private static readonly Color PlayerHealthColor =
            new Color(0.92f, 0.18f, 0.16f, 1f);
        private static readonly Color BossHealthColor =
            new Color(0.84f, 0.24f, 0.62f, 1f);

        [SerializeField]
        private PrototypeGameSession session;

        private GameObject _canvasObject;
        private GameObject _bossPanelObject;
        private Image _playerHealthFill;
        private Image _bossHealthFill;
        private Text _playerHealthLabel;
        private Text _bossHealthLabel;
        private Text _combatRewardLabel;
        private PrototypeDungeonRoomBinder _roomBinder;
        private bool _isSubscribed;

        public PrototypeGameSession Session => session;

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

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null)
            {
                throw new InvalidOperationException(
                    "PrototypeHealthHud requires a game-session reference.");
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
                ? "BOSS  ·  PHASE " + GetPhaseNumber(phase) +
                  "  ·  " + currentHealth + " / " + maxHealth
                : "BOSS DEFEATED  ·  0 / " + maxHealth;
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
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Unity built-in runtime font was not found.");
            }

            _canvasObject = new GameObject(
                "PrototypeHealthHudCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform playerPanel = CreatePanel(
                "PlayerHealthPanel",
                _canvasObject.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(310f, 78f));
            _playerHealthLabel = CreateLabel(playerPanel, font, 21);
            _playerHealthFill = CreateBar(playerPanel, PlayerHealthColor);

            RectTransform rewardPanel = CreatePanel(
                "CombatRewardPanel",
                _canvasObject.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(250f, 58f));
            _combatRewardLabel = CreateLabel(rewardPanel, font, 21);

            RectTransform bossPanel = CreatePanel(
                "BossHealthPanel",
                _canvasObject.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(560f, 82f));
            _bossPanelObject = bossPanel.gameObject;
            _bossHealthLabel = CreateLabel(bossPanel, font, 22);
            _bossHealthFill = CreateBar(bossPanel, BossHealthColor);
        }

        private static RectTransform CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform panel = CreateRect(objectName, parent);
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.pivot = pivot;
            panel.anchoredPosition = anchoredPosition;
            panel.sizeDelta = size;
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;
            return panel;
        }

        private static Text CreateLabel(
            RectTransform panel,
            Font font,
            int fontSize)
        {
            RectTransform rect = CreateRect("Label", panel);
            rect.anchorMin = new Vector2(0f, 0.38f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 0f);
            rect.offsetMax = new Vector2(-14f, -2f);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateBar(RectTransform panel, Color fillColor)
        {
            RectTransform backgroundRect = CreateRect("Bar", panel);
            backgroundRect.anchorMin = new Vector2(0f, 0f);
            backgroundRect.anchorMax = new Vector2(1f, 0.32f);
            backgroundRect.offsetMin = new Vector2(14f, 12f);
            backgroundRect.offsetMax = new Vector2(-14f, 0f);
            Image background = backgroundRect.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.08f, 1f);
            background.raycastTarget = false;

            RectTransform fillRect = CreateRect("Fill", backgroundRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
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
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase),
                        phase,
                        "Unsupported boss phase.");
            }
        }
    }
}
