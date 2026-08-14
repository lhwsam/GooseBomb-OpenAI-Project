using System;
using BombSwap.Core;
using UnityEngine;
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

        private Text _statusLabel;
        private Text _failureCauseLabel;
        private bool _checkResultNextFrame;
        private bool _restartRequested;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public BombSwapInputReader InputReader => inputReader;

        public bool IsVisible { get; private set; }

        public int CompletionCount { get; private set; }

        public int FailureCount { get; private set; }

        public int RestartRequestCount { get; private set; }

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
            _statusLabel.text = "RESTARTING...";
            WebGlHarnessReporter.Report("run-restart-requested");
            try
            {
                host.RestartFinishedRun();
            }
            catch
            {
                _restartRequested = false;
                _statusLabel.text = "RESTART FAILED - PRESS R TO RETRY";
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
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                throw new InvalidOperationException("Unity built-in runtime font was not found.");
            }

            GameObject canvasObject = new GameObject(
                "PrototypeRunCompletionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = CreateRect("Backdrop", canvasObject.transform);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.015f, 0.02f, 0.04f, 0.82f);
            backdropImage.raycastTarget = false;

            Text title = CreateText("Title", backdrop, font, 52, FontStyle.Bold);
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
                    font,
                    24,
                    FontStyle.Bold);
                _failureCauseLabel.rectTransform.anchorMin = new Vector2(0.1f, 0.42f);
                _failureCauseLabel.rectTransform.anchorMax = new Vector2(0.9f, 0.53f);
                _failureCauseLabel.rectTransform.offsetMin = Vector2.zero;
                _failureCauseLabel.rectTransform.offsetMax = Vector2.zero;
                _failureCauseLabel.text =
                    PrototypePlayerDeathCauseFormatter.GetDisplayText(
                        FailureCause.Value);
                _failureCauseLabel.color = new Color(1f, 0.72f, 0.42f, 1f);
            }

            _statusLabel = CreateText("Restart", backdrop, font, 26, FontStyle.Normal);
            _statusLabel.rectTransform.anchorMin = failed
                ? new Vector2(0.1f, 0.29f)
                : new Vector2(0.1f, 0.34f);
            _statusLabel.rectTransform.anchorMax = failed
                ? new Vector2(0.9f, 0.41f)
                : new Vector2(0.9f, 0.48f);
            _statusLabel.rectTransform.offsetMin = Vector2.zero;
            _statusLabel.rectTransform.offsetMax = Vector2.zero;
            _statusLabel.text = "R / GAMEPAD SELECT - RESTART RUN";
            _statusLabel.color = Color.white;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            FontStyle fontStyle)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
