using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerController : MonoBehaviour
    {
        public const float DefaultCellsPerSecond = 5f;

        [SerializeField]
        private TestSandboxContext context;

        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private Transform playerTransform;

        [SerializeField]
        private float cellsPerSecond = DefaultCellsPerSecond;

        private ManualGameClock _clock;
        private PlayerMovementSimulation _movement;
        private GridSpace _gridSpace;
        private float _presentationHeight;
        private Vector3 _visualStart;
        private Vector3 _visualTarget;
        private float _visualElapsed;
        private float _visualDuration;
        private bool _isInterpolating;

        public event Action<PlayerMovementStep> CellEntered;

        public event Action Ready;

        public TestSandboxContext Context => context;

        public BombSwapInputReader InputReader => inputReader;

        public Transform PlayerTransform => playerTransform;

        public float CellsPerSecond => cellsPerSecond;

        public bool IsInitialized => _movement != null;

        public bool IsReady { get; private set; }

        public GridPosition CurrentGridPosition =>
            _movement != null ? _movement.CurrentPosition : default;

        public void Configure(
            TestSandboxContext sandboxContext,
            BombSwapInputReader reader,
            Transform player,
            float movementCellsPerSecond = DefaultCellsPerSecond)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerController before changing its configuration at runtime.");
            }
            if (sandboxContext == null)
            {
                throw new ArgumentNullException(nameof(sandboxContext));
            }
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }
            ValidateCellsPerSecond(movementCellsPerSecond);

            context = sandboxContext;
            inputReader = reader;
            playerTransform = player;
            cellsPerSecond = movementCellsPerSecond;
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!IsInitialized)
            {
                Initialize();
            }

            inputReader.CommandIssued += OnCommandIssued;
            IsReady = true;
            Ready?.Invoke();
        }

        private void OnDisable()
        {
            IsReady = false;
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
            if (_movement != null)
            {
                _movement.SetMoveDirection(CardinalDirection.None);
            }
        }

        private void Update()
        {
            float elapsedSeconds = Time.deltaTime;
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                throw new InvalidOperationException("Unity supplied an invalid movement delta time.");
            }

            _clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));
            if (_movement.TryAdvance(out PlayerMovementStep step))
            {
                BeginVisualStep(step);
                CellEntered?.Invoke(step);
            }

            AdvanceVisual(elapsedSeconds);
        }

        private void Initialize()
        {
            if (context == null || inputReader == null || playerTransform == null)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerController requires context, input reader, and player Transform references.");
            }
            ValidateCellsPerSecond(cellsPerSecond);

            _gridSpace = context.GridSpace;
            var grid = new GridState();
            int halfWidth = context.GridWidth / 2;
            int halfDepth = context.GridDepth / 2;
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int z = -halfDepth; z <= halfDepth; z++)
                {
                    grid.TrySetTerrain(new GridPosition(x, z), GridTerrain.Floor);
                }
            }

            foreach (Vector2Int blocker in context.BlockedCells)
            {
                if (!grid.TrySetTerrain(
                    new GridPosition(blocker.x, blocker.y),
                    GridTerrain.IndestructibleWall))
                {
                    throw new InvalidOperationException($"Could not author blocked cell {blocker}.");
                }
            }

            GridPosition start = _gridSpace.WorldToGrid(context.PlayerSpawn.position);
            _clock = new ManualGameClock();
            _visualDuration = 1f / cellsPerSecond;
            _movement = new PlayerMovementSimulation(
                grid,
                _clock,
                start,
                TimeSpan.FromSeconds(_visualDuration));
            _presentationHeight = playerTransform.position.y - _gridSpace.Origin.y;
            playerTransform.position = ToPresentationPosition(start);
            _visualStart = playerTransform.position;
            _visualTarget = playerTransform.position;
        }

        private void OnCommandIssued(PlayerCommand command)
        {
            if (command.Kind == PlayerCommandKind.Move)
            {
                _movement.SetMoveDirection(command.MoveDirection);
            }
        }

        private void BeginVisualStep(PlayerMovementStep step)
        {
            _visualStart = playerTransform.position;
            _visualTarget = ToPresentationPosition(step.To);
            _visualElapsed = 0f;
            _isInterpolating = true;
        }

        private void AdvanceVisual(float elapsedSeconds)
        {
            if (!_isInterpolating)
            {
                return;
            }

            _visualElapsed += elapsedSeconds;
            float progress = Mathf.Clamp01(_visualElapsed / _visualDuration);
            playerTransform.position = Vector3.LerpUnclamped(_visualStart, _visualTarget, progress);
            if (progress >= 1f)
            {
                playerTransform.position = _visualTarget;
                _isInterpolating = false;
            }
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return _gridSpace.GridToWorld(position) + (Vector3.up * _presentationHeight);
        }

        private static void ValidateCellsPerSecond(float value)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Movement speed must be finite and positive.");
            }
        }
    }
}
