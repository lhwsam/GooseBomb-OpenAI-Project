using System;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonMinimapView : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private RectTransform mapRoot;

        [SerializeField]
        private PrototypeDungeonMinimapRoomView roomViewPrefab;

        [SerializeField]
        private PrototypeDungeonMinimapConnectionView connectionViewPrefab;

        [Header("Runtime map style")]
        [SerializeField]
        private Color connectionColor =
            new Color(0.46f, 0.54f, 0.66f, 0.9f);

        [SerializeField]
        private Sprite connectionSprite;

        [SerializeField, Min(1f)]
        private float maximumCellPitch = 38f;

        [SerializeField, Min(1f)]
        private float roomSize = 26f;

        [SerializeField, Min(1f)]
        private float connectionThickness = 5f;

        public Canvas Canvas => canvas;

        public RectTransform MapRoot => mapRoot;

        public PrototypeDungeonMinimapRoomView RoomViewPrefab => roomViewPrefab;

        public PrototypeDungeonMinimapConnectionView ConnectionViewPrefab =>
            connectionViewPrefab;

        public Color ConnectionColor => connectionColor;

        public Sprite ConnectionSprite => connectionSprite;

        public float MaximumCellPitch => maximumCellPitch;

        public float RoomSize => roomSize;

        public float ConnectionThickness => connectionThickness;

        public bool HasRequiredReferences =>
            canvas != null &&
            mapRoot != null &&
            roomViewPrefab != null &&
            roomViewPrefab.HasRequiredReferences &&
            connectionViewPrefab != null &&
            connectionViewPrefab.HasRequiredReferences &&
            maximumCellPitch > 0f &&
            roomSize > 0f &&
            connectionThickness > 0f;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            RectTransform authoredMapRoot,
            PrototypeDungeonMinimapRoomView authoredRoomViewPrefab,
            PrototypeDungeonMinimapConnectionView authoredConnectionViewPrefab)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Dungeon minimap view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            mapRoot = authoredMapRoot ?? throw new ArgumentNullException(nameof(authoredMapRoot));
            roomViewPrefab = authoredRoomViewPrefab ??
                throw new ArgumentNullException(nameof(authoredRoomViewPrefab));
            connectionViewPrefab = authoredConnectionViewPrefab ??
                throw new ArgumentNullException(nameof(authoredConnectionViewPrefab));
        }
    }
}
