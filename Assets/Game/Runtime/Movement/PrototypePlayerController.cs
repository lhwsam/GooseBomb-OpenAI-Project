using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerController : MonoBehaviour
    {
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform playerTransform;

        private GridSpace _gridSpace;
        private float _presentationHeight;
        public PrototypeGameSession Session => session;

        public Transform PlayerTransform => playerTransform;

        public float CellsPerSecond => session != null ? session.CellsPerSecond : 0f;

        public bool IsInitialized { get; private set; }

        public GridPosition CurrentGridPosition =>
            session != null ? session.CurrentGridPosition : default;

        public void Configure(PrototypeGameSession gameSession, Transform player)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerController before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            session = gameSession;
            playerTransform = player;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || playerTransform == null)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerController requires session and player Transform references.");
            }

            session.PlayerPositionChanged += OnPlayerPositionChanged;
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                InitializePresentation();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.PlayerPositionChanged -= OnPlayerPositionChanged;
                session.Ready -= OnSessionReady;
            }
        }

        private void OnSessionReady()
        {
            InitializePresentation();
        }

        private void InitializePresentation()
        {
            if (IsInitialized)
            {
                return;
            }

            _gridSpace = session.GridSpace;
            _presentationHeight = playerTransform.position.y - _gridSpace.Origin.y;
            playerTransform.position = ToPresentationPosition(session.CurrentMovementPosition);
            playerTransform.rotation = ToPresentationRotation(session.FacingDirection);
            IsInitialized = true;
        }

        private void OnPlayerPositionChanged(
            GridSubcellPosition position,
            CardinalDirection _)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            playerTransform.position = ToPresentationPosition(position);
        }

        private void Update()
        {
            if (!IsInitialized || session.IsPaused)
            {
                return;
            }

            playerTransform.rotation = ToPresentationRotation(session.FacingDirection);
        }

        private Vector3 ToPresentationPosition(GridSubcellPosition position)
        {
            return _gridSpace.GridToWorld(position) + (Vector3.up * _presentationHeight);
        }

        private static Quaternion ToPresentationRotation(CardinalDirection facingDirection)
        {
            return Quaternion.LookRotation(DirectionToForward(facingDirection), Vector3.up);
        }

        private static Vector3 DirectionToForward(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return Vector3.forward;
                case CardinalDirection.East:
                    return Vector3.right;
                case CardinalDirection.South:
                    return Vector3.back;
                case CardinalDirection.West:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }
    }
}
