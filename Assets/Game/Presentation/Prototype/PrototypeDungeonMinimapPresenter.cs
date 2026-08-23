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

        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        [SerializeField]
        private PrototypeDungeonMinimapView viewPrefab;

        private PrototypeDungeonMinimapView _viewInstance;
        private RectTransform _mapRoot;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public PrototypeDungeonMinimapView ViewPrefab => viewPrefab;

        public PrototypeDungeonMinimapView ViewInstance => _viewInstance;

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

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            PrototypeDungeonMinimapView authoredViewPrefab)
        {
            Configure(authoredRoomBinder);
            BindViewPrefab(authoredViewPrefab);
        }

        public void BindViewPrefab(
            PrototypeDungeonMinimapView authoredViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeDungeonMinimapPresenter before changing its view prefab.");
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
            if (roomBinder == null || roomBinder.RunHost == null ||
                viewPrefab == null || !viewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonMinimapPresenter requires an initialized dungeon room binder and a configured view prefab.");
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
            if (_viewInstance == null)
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
            _viewInstance = Instantiate(viewPrefab, transform, false);
            _viewInstance.name = viewPrefab.name;
            if (!_viewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated minimap view is missing required references.");
            }

            _mapRoot = _viewInstance.MapRoot;
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
            float availableWidth = Mathf.Max(1f, _mapRoot.rect.width);
            float availableHeight = Mathf.Max(1f, _mapRoot.rect.height);
            float pitch = Mathf.Min(
                _viewInstance.MaximumCellPitch,
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
                ? new Vector2(
                    Mathf.Abs(to.x - from.x),
                    _viewInstance.ConnectionThickness)
                : new Vector2(
                    _viewInstance.ConnectionThickness,
                    Mathf.Abs(to.y - from.y));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = _viewInstance.ConnectionColor;
            image.sprite = _viewInstance.ConnectionSprite;
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
            rect.sizeDelta = new Vector2(
                _viewInstance.RoomSize,
                _viewInstance.RoomSize);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = GetRoomColor(room.State);
            image.sprite = _viewInstance.RoomSprite;
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

        private Color GetRoomColor(DungeonMinimapRoomState state)
        {
            switch (state)
            {
                case DungeonMinimapRoomState.Discovered:
                    return _viewInstance.DiscoveredColor;
                case DungeonMinimapRoomState.Visited:
                    return _viewInstance.VisitedColor;
                case DungeonMinimapRoomState.Current:
                    return _viewInstance.CurrentColor;
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
