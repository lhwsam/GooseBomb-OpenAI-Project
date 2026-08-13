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
        private Vector3 _visualStart;
        private Vector3 _visualTarget;
        private float _visualElapsed;
        private float _visualDuration;
        private bool _isInterpolating;

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

            session.PlayerMoved += OnPlayerMoved;
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
                session.PlayerMoved -= OnPlayerMoved;
                session.Ready -= OnSessionReady;
            }
        }

        private void Update()
        {
            if (!_isInterpolating)
            {
                return;
            }

            _visualElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_visualElapsed / _visualDuration);
            playerTransform.position = Vector3.LerpUnclamped(_visualStart, _visualTarget, progress);
            if (progress >= 1f)
            {
                playerTransform.position = _visualTarget;
                _isInterpolating = false;
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
            _visualDuration = 1f / session.CellsPerSecond;
            _presentationHeight = playerTransform.position.y - _gridSpace.Origin.y;
            playerTransform.position = ToPresentationPosition(session.CurrentGridPosition);
            _visualStart = playerTransform.position;
            _visualTarget = playerTransform.position;
            IsInitialized = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            _visualStart = playerTransform.position;
            _visualTarget = ToPresentationPosition(step.To);
            _visualElapsed = 0f;
            _isInterpolating = true;
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return _gridSpace.GridToWorld(position) + (Vector3.up * _presentationHeight);
        }
    }
}
