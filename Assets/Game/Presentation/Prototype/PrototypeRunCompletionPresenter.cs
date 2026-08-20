using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombSwap
{
    public enum PrototypePlayerDeathCause
    {
        Unknown = 0,
        BombExplosion = 1,
        ChaserContact = 2,
        ChargerCharge = 3,
        ArmoredContact = 4,
        EnemyContact = 5,
        BossAttack = 6,
    }

    public static class PrototypePlayerDeathCauseFormatter
    {
        public static PrototypePlayerDeathCause Resolve(
            PlayerDamageSourceKind sourceKind,
            ActorId sourceActorId,
            ActorId chaserActorId,
            ActorId chargerActorId,
            ActorId armoredActorId)
        {
            switch (sourceKind)
            {
                case PlayerDamageSourceKind.Explosion:
                    return PrototypePlayerDeathCause.BombExplosion;
                case PlayerDamageSourceKind.EnemyContact:
                    if (sourceActorId == chaserActorId)
                    {
                        return PrototypePlayerDeathCause.ChaserContact;
                    }
                    if (sourceActorId == chargerActorId)
                    {
                        return PrototypePlayerDeathCause.ChargerCharge;
                    }
                    if (sourceActorId == armoredActorId)
                    {
                        return PrototypePlayerDeathCause.ArmoredContact;
                    }
                    return PrototypePlayerDeathCause.EnemyContact;
                case PlayerDamageSourceKind.BossPattern:
                    return PrototypePlayerDeathCause.BossAttack;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceKind),
                        sourceKind,
                        "Unsupported player death source kind.");
            }
        }

        public static string GetDisplayText(PrototypePlayerDeathCause cause)
        {
            switch (cause)
            {
                case PrototypePlayerDeathCause.Unknown:
                    return "CAUSE: UNKNOWN";
                case PrototypePlayerDeathCause.BombExplosion:
                    return "CAUSE: BOMB EXPLOSION";
                case PrototypePlayerDeathCause.ChaserContact:
                    return "CAUSE: CHASER CONTACT";
                case PrototypePlayerDeathCause.ChargerCharge:
                    return "CAUSE: CHARGER CHARGE";
                case PrototypePlayerDeathCause.ArmoredContact:
                    return "CAUSE: ARMORED ENEMY CONTACT";
                case PrototypePlayerDeathCause.EnemyContact:
                    return "CAUSE: ENEMY CONTACT";
                case PrototypePlayerDeathCause.BossAttack:
                    return "CAUSE: BOSS ATTACK";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cause),
                        cause,
                        "Unsupported prototype player death cause.");
            }
        }

        public static string GetHarnessEvent(PrototypePlayerDeathCause cause)
        {
            switch (cause)
            {
                case PrototypePlayerDeathCause.Unknown:
                    return "run-failed-cause-unknown";
                case PrototypePlayerDeathCause.BombExplosion:
                    return "run-failed-cause-bomb-explosion";
                case PrototypePlayerDeathCause.ChaserContact:
                    return "run-failed-cause-chaser-contact";
                case PrototypePlayerDeathCause.ChargerCharge:
                    return "run-failed-cause-charger-charge";
                case PrototypePlayerDeathCause.ArmoredContact:
                    return "run-failed-cause-armored-contact";
                case PrototypePlayerDeathCause.EnemyContact:
                    return "run-failed-cause-enemy-contact";
                case PrototypePlayerDeathCause.BossAttack:
                    return "run-failed-cause-boss-attack";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cause),
                        cause,
                        "Unsupported prototype player death cause.");
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeRunCompletionPresenter : MonoBehaviour
    {
        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        [SerializeField]
        private BombSwapInputReader inputReader;

        private TextMeshProUGUI _statusLabel;
        private TextMeshProUGUI _failureCauseLabel;
        private Button _restartButton;
        private Button _lobbyButton;
        private bool _checkResultNextFrame;
        private bool _restartRequested;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public BombSwapInputReader InputReader => inputReader;

        public bool IsVisible { get; private set; }

        public int CompletionCount { get; private set; }

        public int FailureCount { get; private set; }

        public int RestartRequestCount { get; private set; }

        public int LobbyRequestCount { get; private set; }

        public Button RestartButton => _restartButton;

        public Button LobbyButton => _lobbyButton;

        public PrototypePlayerDeathCause? FailureCause { get; private set; }

        public string FailureCauseText =>
            _failureCauseLabel != null ? _failureCauseLabel.text : string.Empty;

        public string StatusText => _statusLabel != null ? _statusLabel.text : string.Empty;

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            BombSwapInputReader authoredInputReader)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeRunCompletionPresenter before changing its configuration.");
            }

            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
            inputReader = authoredInputReader ??
                throw new ArgumentNullException(nameof(authoredInputReader));
        }

        public void RequestRestart()
        {
            if (!IsVisible || _restartRequested)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            if (host == null || host.RunSession == null || !host.RunSession.IsFinished)
            {
                throw new InvalidOperationException(
                    "Run restart requires a completed or failed primary dungeon run.");
            }

            _restartRequested = true;
            RestartRequestCount++;
            SetButtonsInteractable(false);
            _statusLabel.text = "RESTARTING...";
            WebGlHarnessReporter.Report("run-restart-requested");
            try
            {
                host.RestartFinishedRun();
            }
            catch
            {
                _restartRequested = false;
                SetButtonsInteractable(true);
                _statusLabel.text = "RESTART FAILED - PRESS R TO RETRY";
                throw;
            }
        }

        public void RequestReturnToLobby()
        {
            if (!IsVisible || _restartRequested)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            if (host == null || host.RunSession == null || !host.RunSession.IsFinished)
            {
                throw new InvalidOperationException(
                    "Lobby return requires a completed or failed primary dungeon run.");
            }

            _restartRequested = true;
            LobbyRequestCount++;
            SetButtonsInteractable(false);
            _statusLabel.text = "RETURNING TO LOBBY...";
            try
            {
                host.ExitFinishedRunToScene(
                    PrototypeLobbyPresenter.DefaultLobbySceneName);
            }
            catch
            {
                _restartRequested = false;
                SetButtonsInteractable(true);
                _statusLabel.text = "LOBBY RETURN FAILED";
                throw;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (roomBinder == null || roomBinder.RoomSession == null || inputReader == null)
            {
                throw new InvalidOperationException(
                    "PrototypeRunCompletionPresenter requires room-binder and input-reader references.");
            }

            roomBinder.RoomSession.Ready += OnSessionReady;
            roomBinder.RoomSession.RoomCleared += OnRoomCleared;
            roomBinder.RoomSession.PlayerDied += OnPlayerDied;
            inputReader.CommandIssued += OnCommandIssued;
            if (roomBinder.RoomSession.IsReady)
            {
                _checkResultNextFrame = true;
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                TryShowResult();
            }
        }

        private void LateUpdate()
        {
            if (!_checkResultNextFrame)
            {
                return;
            }

            _checkResultNextFrame = false;
            TryShowResult();
        }

        private void OnDisable()
        {
            if (roomBinder != null && roomBinder.RoomSession != null)
            {
                roomBinder.RoomSession.Ready -= OnSessionReady;
                roomBinder.RoomSession.RoomCleared -= OnRoomCleared;
                roomBinder.RoomSession.PlayerDied -= OnPlayerDied;
            }
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
        }

        private void OnSessionReady()
        {
            _checkResultNextFrame = true;
        }

        private void OnRoomCleared()
        {
            _checkResultNextFrame = true;
        }

        private void OnPlayerDied(PlayerDamageResult _)
        {
            _checkResultNextFrame = true;
        }

        private void OnCommandIssued(PlayerCommand command)
        {
            if (command.Kind == PlayerCommandKind.RestartRun)
            {
                RequestRestart();
            }
        }

        private void TryShowResult()
        {
            if (IsVisible)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            PrototypeGameSession session = roomBinder.RoomSession;
            if (host == null || host.RunSession == null)
            {
                return;
            }

            bool failed = host.RunSession.IsFailed;
            bool completed = host.RunSession.IsComplete &&
                roomBinder.RuntimeRoomType == RoomType.Boss &&
                session.IsRoomCleared;
            if (!failed && !completed)
            {
                return;
            }

            if (failed)
            {
                PlayerDamageResult? failureDamage = host.RunSession.FailureDamage;
                if (!failureDamage.HasValue)
                {
                    throw new InvalidOperationException(
                        "A failed dungeon run is missing its fatal damage result.");
                }

                FailureCause = PrototypePlayerDeathCauseFormatter.Resolve(
                    failureDamage.Value.SourceKind,
                    failureDamage.Value.SourceActorId,
                    session.ChaserActorId,
                    session.ChargerActorId,
                    session.ArmoredActorId);
            }
            else
            {
                FailureCause = null;
            }

            CreateUi(failed);
            IsVisible = true;
            if (failed)
            {
                FailureCount++;
            }
            else
            {
                CompletionCount++;
            }
            session.enabled = false;
            WebGlHarnessReporter.Report(failed ? "run-failed" : "run-completed");
            if (failed)
            {
                WebGlHarnessReporter.Report(
                    PrototypePlayerDeathCauseFormatter.GetHarnessEvent(
                        FailureCause.Value));
            }
        }

        private void CreateUi(bool failed)
        {
            GameObject canvasObject = new GameObject(
                "PrototypeRunCompletionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = PrototypeUiFactory.CreateRect(
                "Backdrop",
                canvasObject.transform);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.015f, 0.02f, 0.04f, 0.82f);
            backdropImage.raycastTarget = false;

            TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                "Title",
                backdrop,
                52f,
                TextAlignmentOptions.Center,
                FontStyles.Bold,
                TextWrappingModes.Normal);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.48f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.68f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.text = failed ? "RUN FAILED" : "FLOOR CLEARED";
            title.color = failed
                ? new Color(1f, 0.32f, 0.26f, 1f)
                : new Color(0.22f, 0.95f, 0.5f, 1f);

            if (failed)
            {
                _failureCauseLabel = CreateText(
                    "FailureCause",
                    backdrop,
                    24f,
                    FontStyles.Bold);
                _failureCauseLabel.rectTransform.anchorMin = new Vector2(0.1f, 0.42f);
                _failureCauseLabel.rectTransform.anchorMax = new Vector2(0.9f, 0.53f);
                _failureCauseLabel.rectTransform.offsetMin = Vector2.zero;
                _failureCauseLabel.rectTransform.offsetMax = Vector2.zero;
                _failureCauseLabel.text =
                    PrototypePlayerDeathCauseFormatter.GetDisplayText(
                        FailureCause.Value);
                _failureCauseLabel.color = new Color(1f, 0.72f, 0.42f, 1f);
            }

            _restartButton = PrototypeUiFactory.CreateButton(
                "RestartButton",
                backdrop,
                "다시 시작",
                27f,
                new Color(0.12f, 0.42f, 0.68f, 1f),
                new Color(0.2f, 0.66f, 0.92f, 1f));
            ConfigureButtonRect(
                _restartButton,
                new Vector2(0.27f, failed ? 0.27f : 0.31f),
                new Vector2(0.49f, failed ? 0.38f : 0.42f));
            _restartButton.onClick.AddListener(RequestRestart);

            _lobbyButton = PrototypeUiFactory.CreateButton(
                "LobbyButton",
                backdrop,
                "로비로 돌아가기",
                27f,
                new Color(0.18f, 0.21f, 0.28f, 1f),
                new Color(0.34f, 0.4f, 0.52f, 1f));
            ConfigureButtonRect(
                _lobbyButton,
                new Vector2(0.51f, failed ? 0.27f : 0.31f),
                new Vector2(0.73f, failed ? 0.38f : 0.42f));
            _lobbyButton.onClick.AddListener(RequestReturnToLobby);

            _statusLabel = CreateText(
                "Status",
                backdrop,
                19f,
                FontStyles.Normal);
            _statusLabel.rectTransform.anchorMin = new Vector2(0.1f, 0.15f);
            _statusLabel.rectTransform.anchorMax = new Vector2(0.9f, 0.25f);
            _statusLabel.rectTransform.offsetMin = Vector2.zero;
            _statusLabel.rectTransform.offsetMax = Vector2.zero;
            _statusLabel.text = "R / 게임패드 Select로 즉시 다시 시작";
            _statusLabel.color = new Color(0.78f, 0.84f, 0.92f, 1f);

            EventSystem eventSystem = PrototypeUiFactory.EnsureEventSystem();
            eventSystem.SetSelectedGameObject(_restartButton.gameObject);
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyles fontStyle)
        {
            return PrototypeUiFactory.CreateText(
                objectName,
                parent,
                fontSize,
                TextAlignmentOptions.Center,
                fontStyle,
                TextWrappingModes.Normal);
        }

        private static void ConfigureButtonRect(
            Button button,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_restartButton != null)
            {
                _restartButton.interactable = interactable;
            }
            if (_lobbyButton != null)
            {
                _lobbyButton.interactable = interactable;
            }
        }
    }
}
