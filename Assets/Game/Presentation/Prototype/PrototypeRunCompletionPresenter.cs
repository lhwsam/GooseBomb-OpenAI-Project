using System;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeRunCompletionPresenter : MonoBehaviour
    {
        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        [SerializeField]
        private BombSwapInputReader inputReader;

        private Text _statusLabel;
        private bool _checkCompletionNextFrame;
        private bool _restartRequested;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public BombSwapInputReader InputReader => inputReader;

        public bool IsVisible { get; private set; }

        public int CompletionCount { get; private set; }

        public int RestartRequestCount { get; private set; }

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
            if (host == null || host.RunSession == null || !host.RunSession.IsComplete)
            {
                throw new InvalidOperationException(
                    "Run completion restart requires the completed primary dungeon run.");
            }

            _restartRequested = true;
            RestartRequestCount++;
            _statusLabel.text = "RESTARTING...";
            WebGlHarnessReporter.Report("run-restart-requested");
            try
            {
                host.RestartCompletedRun();
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
            inputReader.CommandIssued += OnCommandIssued;
            if (roomBinder.RoomSession.IsReady)
            {
                _checkCompletionNextFrame = true;
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                TryShowCompletion();
            }
        }

        private void LateUpdate()
        {
            if (!_checkCompletionNextFrame)
            {
                return;
            }

            _checkCompletionNextFrame = false;
            TryShowCompletion();
        }

        private void OnDisable()
        {
            if (roomBinder != null && roomBinder.RoomSession != null)
            {
                roomBinder.RoomSession.Ready -= OnSessionReady;
                roomBinder.RoomSession.RoomCleared -= OnRoomCleared;
            }
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
        }

        private void OnSessionReady()
        {
            _checkCompletionNextFrame = true;
        }

        private void OnRoomCleared()
        {
            _checkCompletionNextFrame = true;
        }

        private void OnCommandIssued(PlayerCommand command)
        {
            if (command.Kind == PlayerCommandKind.RestartRun)
            {
                RequestRestart();
            }
        }

        private void TryShowCompletion()
        {
            if (IsVisible || roomBinder.RuntimeRoomType != RoomType.Boss)
            {
                return;
            }

            PrototypeDungeonRunHost host = roomBinder.RunHost;
            PrototypeGameSession session = roomBinder.RoomSession;
            if (host == null || host.RunSession == null ||
                !host.RunSession.IsComplete || !session.IsRoomCleared)
            {
                return;
            }

            CreateUi();
            IsVisible = true;
            CompletionCount++;
            session.enabled = false;
            WebGlHarnessReporter.Report("run-completed");
        }

        private void CreateUi()
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
            title.text = "FLOOR CLEARED";
            title.color = new Color(0.22f, 0.95f, 0.5f, 1f);

            _statusLabel = CreateText("Restart", backdrop, font, 26, FontStyle.Normal);
            _statusLabel.rectTransform.anchorMin = new Vector2(0.1f, 0.34f);
            _statusLabel.rectTransform.anchorMax = new Vector2(0.9f, 0.48f);
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
