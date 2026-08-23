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

        [Header("Runtime map style")]
        [SerializeField]
        private Color connectionColor =
            new Color(0.46f, 0.54f, 0.66f, 0.9f);

        [SerializeField]
        private Color discoveredColor =
            new Color(0.19f, 0.23f, 0.31f, 1f);

        [SerializeField]
        private Color visitedColor =
            new Color(0.14f, 0.62f, 0.84f, 1f);

        [SerializeField]
        private Color currentColor =
            new Color(1f, 0.72f, 0.12f, 1f);

        [SerializeField]
        private Sprite roomSprite;

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

        public Color ConnectionColor => connectionColor;

        public Color DiscoveredColor => discoveredColor;

        public Color VisitedColor => visitedColor;

        public Color CurrentColor => currentColor;

        public Sprite RoomSprite => roomSprite;

        public Sprite ConnectionSprite => connectionSprite;

        public float MaximumCellPitch => maximumCellPitch;

        public float RoomSize => roomSize;

        public float ConnectionThickness => connectionThickness;

        public bool HasRequiredReferences =>
            canvas != null &&
            mapRoot != null &&
            maximumCellPitch > 0f &&
            roomSize > 0f &&
            connectionThickness > 0f;

        public void BindAuthoredView(
            Canvas authoredCanvas,
            RectTransform authoredMapRoot)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Dungeon minimap view can only be authored outside Play Mode.");
            }

            canvas = authoredCanvas ?? throw new ArgumentNullException(nameof(authoredCanvas));
            mapRoot = authoredMapRoot ?? throw new ArgumentNullException(nameof(authoredMapRoot));
        }
    }
}
