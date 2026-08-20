using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonMinimapPresenter : MonoBehaviour
    {
        public static readonly Vector2 DefaultPanelPosition =
            new Vector2(-24f, -96f);
        public static readonly Vector2 DefaultPanelSize =
            new Vector2(270f, 230f);

        private static readonly Color PanelColor =
            new Color(0.02f, 0.025f, 0.04f, 0.86f);
        private static readonly Color ConnectionColor =
            new Color(0.46f, 0.54f, 0.66f, 0.9f);
        private static readonly Color DiscoveredColor =
            new Color(0.19f, 0.23f, 0.31f, 1f);
        private static readonly Color VisitedColor =
            new Color(0.14f, 0.62f, 0.84f, 1f);
        private static readonly Color CurrentColor =
            new Color(1f, 0.72f, 0.12f, 1f);

        private const float MaximumCellPitch = 38f;
        private const float RoomSize = 26f;
        private const float ConnectionThickness = 5f;

        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        private GameObject _canvasObject;
        private RectTransform _mapRoot;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public bool IsInitialized { get; private set; }

        public DungeonRoomNodeId DisplayedCurrentRoomId { get; private set; }

        public int DisplayedRoomCount { get; private set; }

        public int DisplayedConnectionCount { get; private set; }

        public DungeonMinimapSnapshot DisplayedSnapshot { get; private set; }

        public void Configure(PrototypeDungeonRoomBinder authoredRoomBinder)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeDungeonMinimapPresenter before changing its configuration.");
            }

            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (roomBinder == null || roomBinder.RunHost == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonMinimapPresenter requires an initialized dungeon room binder.");
            }

            roomBinder.RunHost.RoomCommitted += OnRoomCommitted;
            roomBinder.SecretExitRevealed += OnSecretExitRevealed;
        }

        private void Start()
        {
            if (Application.isPlaying && !roomBinder.RunHost.HasPendingTransition)
            {
                RefreshFromRun();
            }
        }

        private void OnDisable()
        {
            if (roomBinder != null && roomBinder.RunHost != null)
            {
                roomBinder.RunHost.RoomCommitted -= OnRoomCommitted;
            }
            if (roomBinder != null)
            {
                roomBinder.SecretExitRevealed -= OnSecretExitRevealed;
            }
        }

        private void OnRoomCommitted()
        {
            RefreshFromRun();
        }

        private void OnSecretExitRevealed(DungeonSecretExitRevealResult result)
        {
            if (result.WasRevealed)
            {
                RefreshFromRun();
            }
        }

        public void RefreshFromRun()
        {
            if (roomBinder == null || roomBinder.RunHost == null ||
                roomBinder.RunHost.RunSession == null)
            {
                throw new InvalidOperationException(
                    "Minimap refresh requires an active dungeon run.");
            }
            if (roomBinder.RunHost.HasPendingTransition)
            {
                throw new InvalidOperationException(
                    "Minimap cannot refresh before a room transition commits.");
            }

            DungeonMinimapSnapshot snapshot =
                roomBinder.RunHost.RunSession.RunState.CreateMinimapSnapshot();
            if (!_canvasObject)
            {
                CreateUi();
            }
            RebuildMap(snapshot);
            DisplayedSnapshot = snapshot;
            DisplayedCurrentRoomId = snapshot.CurrentRoomId;
            DisplayedRoomCount = snapshot.Rooms.Count;
            DisplayedConnectionCount = snapshot.Connections.Count;
            IsInitialized = true;
            WebGlHarnessReporter.ReportMinimapSnapshot(
                snapshot.CurrentRoomId,
                snapshot.Rooms.Count,
                snapshot.Connections.Count);
        }

        private void CreateUi()
        {
            _canvasObject = new GameObject(
                "PrototypeDungeonMinimapCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 109;
            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreateRect("MinimapPanel", _canvasObject.transform);
            panel.anchorMin = Vector2.one;
            panel.anchorMax = Vector2.one;
            panel.pivot = Vector2.one;
            panel.anchoredPosition = DefaultPanelPosition;
            panel.sizeDelta = DefaultPanelSize;
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;

            TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                "Title",
                panel,
                18f,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(12f, -38f);
            titleRect.offsetMax = new Vector2(-12f, -8f);
            title.text = "DUNGEON MAP";

            TextMeshProUGUI legend = PrototypeUiFactory.CreateText(
                "Legend",
                panel,
                13f,
                TextAlignmentOptions.Center);
            RectTransform legendRect = legend.rectTransform;
            legendRect.anchorMin = Vector2.zero;
            legendRect.anchorMax = new Vector2(1f, 0f);
            legendRect.pivot = new Vector2(0.5f, 0f);
            legendRect.offsetMin = new Vector2(12f, 8f);
            legendRect.offsetMax = new Vector2(-12f, 34f);
            legend.text = "C CURRENT   V VISITED   ? DISCOVERED";
            legend.color = new Color(0.78f, 0.82f, 0.9f, 1f);

            _mapRoot = CreateRect("Map", panel);
            _mapRoot.anchorMin = Vector2.zero;
            _mapRoot.anchorMax = Vector2.one;
            _mapRoot.offsetMin = new Vector2(14f, 40f);
            _mapRoot.offsetMax = new Vector2(-14f, -44f);
        }

        private void RebuildMap(DungeonMinimapSnapshot snapshot)
        {
            for (int index = _mapRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(_mapRoot.GetChild(index).gameObject);
            }

            int minimumX = int.MaxValue;
            int maximumX = int.MinValue;
            int minimumZ = int.MaxValue;
            int maximumZ = int.MinValue;
            for (int index = 0; index < snapshot.Rooms.Count; index++)
            {
                RoomGraphPosition position = snapshot.Rooms[index].Position;
                minimumX = Math.Min(minimumX, position.X);
                maximumX = Math.Max(maximumX, position.X);
                minimumZ = Math.Min(minimumZ, position.Z);
                maximumZ = Math.Max(maximumZ, position.Z);
            }

            int spanX = maximumX - minimumX;
            int spanZ = maximumZ - minimumZ;
            float availableWidth = DefaultPanelSize.x - 28f;
            float availableHeight = DefaultPanelSize.y - 84f;
            float pitch = Mathf.Min(
                MaximumCellPitch,
                availableWidth / Math.Max(1, spanX + 1),
                availableHeight / Math.Max(1, spanZ + 1));
            float centerX = (minimumX + maximumX) * 0.5f;
            float centerZ = (minimumZ + maximumZ) * 0.5f;

            for (int index = 0; index < snapshot.Connections.Count; index++)
            {
                DungeonRoomConnection connection = snapshot.Connections[index];
                DungeonMinimapRoomSnapshot first = snapshot.GetRoom(connection.First);
                DungeonMinimapRoomSnapshot second = snapshot.GetRoom(connection.Second);
                CreateConnection(
                    ToUiPosition(first.Position, centerX, centerZ, pitch),
                    ToUiPosition(second.Position, centerX, centerZ, pitch));
            }

            for (int index = 0; index < snapshot.Rooms.Count; index++)
            {
                DungeonMinimapRoomSnapshot room = snapshot.Rooms[index];
                CreateRoom(
                    room,
                    ToUiPosition(room.Position, centerX, centerZ, pitch));
            }
        }

        private void CreateConnection(Vector2 from, Vector2 to)
        {
            RectTransform rect = CreateRect("Connection", _mapRoot);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            bool horizontal = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.y - from.y);
            rect.sizeDelta = horizontal
                ? new Vector2(Mathf.Abs(to.x - from.x), ConnectionThickness)
                : new Vector2(ConnectionThickness, Mathf.Abs(to.y - from.y));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ConnectionColor;
            image.raycastTarget = false;
        }

        private void CreateRoom(
            DungeonMinimapRoomSnapshot room,
            Vector2 position)
        {
            RectTransform rect = CreateRect("Room_" + room.RoomId.Value, _mapRoot);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(RoomSize, RoomSize);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = GetRoomColor(room.State);
            image.raycastTarget = false;

            TextMeshProUGUI label = PrototypeUiFactory.CreateText(
                "State",
                rect,
                12f,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = room.IsCurrent ? Color.black : Color.white;
            label.text = GetRoomLabel(room.State);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static Vector2 ToUiPosition(
            RoomGraphPosition position,
            float centerX,
            float centerZ,
            float pitch)
        {
            return new Vector2(
                (position.X - centerX) * pitch,
                (position.Z - centerZ) * pitch);
        }

        private static Color GetRoomColor(DungeonMinimapRoomState state)
        {
            switch (state)
            {
                case DungeonMinimapRoomState.Discovered:
                    return DiscoveredColor;
                case DungeonMinimapRoomState.Visited:
                    return VisitedColor;
                case DungeonMinimapRoomState.Current:
                    return CurrentColor;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(state),
                        state,
                        "Unsupported minimap room state.");
            }
        }

        private static string GetRoomLabel(DungeonMinimapRoomState state)
        {
            switch (state)
            {
                case DungeonMinimapRoomState.Discovered:
                    return "?";
                case DungeonMinimapRoomState.Visited:
                    return "V";
                case DungeonMinimapRoomState.Current:
                    return "C";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(state),
                        state,
                        "Unsupported minimap room state.");
            }
        }
    }
}
