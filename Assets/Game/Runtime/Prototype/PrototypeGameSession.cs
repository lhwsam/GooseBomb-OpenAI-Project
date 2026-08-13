using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PrototypeGameSession : MonoBehaviour
    {
        public const float DefaultCellsPerSecond = 5f;
        public const float DefaultChainDelaySeconds = 0.15f;
        public const int DefaultExplosionDamage = 1;

        private static readonly ActorId PrototypePlayerActorId = new ActorId(1);

        [SerializeField]
        private TestSandboxContext context;

        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private PrototypeBombDefinitionAsset bombDefinition;

        [SerializeField]
        private PrototypePlayerVitalsAsset playerVitals;

        [SerializeField]
        private float cellsPerSecond = DefaultCellsPerSecond;

        [SerializeField]
        private float chainDelaySeconds = DefaultChainDelaySeconds;

        private GridState _grid;
        private ManualGameClock _clock;
        private PlayerMovementSimulation _movement;
        private BombSimulation _bombs;
        private PlayerHealthSimulation _health;
        private BombDefinition _coreBombDefinition;
        private readonly List<PlayerDamageResult> _appliedDamageResults =
            new List<PlayerDamageResult>();

        public event Action<PlayerMovementStep> PlayerMoved;

        public event Action<BombSnapshot> BombPlaced;

        public event Action<BombExplosion> BombExploded;

        public event Action<PlayerDamageResult> PlayerDamaged;

        public event Action<PlayerDamageResult> PlayerDied;

        public event Action Ready;

        public TestSandboxContext Context => context;

        public BombSwapInputReader InputReader => inputReader;

        public PrototypeBombDefinitionAsset BombDefinition => bombDefinition;

        public PrototypePlayerVitalsAsset PlayerVitals => playerVitals;

        public float CellsPerSecond => cellsPerSecond;

        public float ChainDelaySeconds => chainDelaySeconds;

        public bool IsInitialized => _movement != null && _bombs != null && _health != null;

        public bool IsReady { get; private set; }

        public GridPosition CurrentGridPosition =>
            _movement != null ? _movement.CurrentPosition : default;

        public int ActiveBombCount => _bombs != null ? _bombs.ActiveBombCount : 0;

        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;

        public int MaxHealth => _health != null ? _health.MaxHealth : 0;

        public bool IsPlayerDead => _health != null && _health.IsDead;

        public bool IsPlayerInvulnerable => _health != null && _health.IsInvulnerable;

        public bool HasPlayerBombPassThrough =>
            _movement != null && _movement.HasBombPassThrough;

        public GridSpace GridSpace
        {
            get
            {
                if (context == null)
                {
                    throw new InvalidOperationException("Prototype game session has no sandbox context.");
                }

                return context.GridSpace;
            }
        }

        public void Configure(
            TestSandboxContext sandboxContext,
            BombSwapInputReader reader,
            PrototypeBombDefinitionAsset startingBomb,
            PrototypePlayerVitalsAsset startingPlayerVitals,
            float movementCellsPerSecond = DefaultCellsPerSecond,
            float bombChainDelaySeconds = DefaultChainDelaySeconds)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeGameSession before changing its runtime configuration.");
            }
            if (sandboxContext == null)
            {
                throw new ArgumentNullException(nameof(sandboxContext));
            }
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (startingBomb == null)
            {
                throw new ArgumentNullException(nameof(startingBomb));
            }
            if (startingPlayerVitals == null)
            {
                throw new ArgumentNullException(nameof(startingPlayerVitals));
            }

            ValidateFinitePositive(movementCellsPerSecond, nameof(movementCellsPerSecond));
            ValidateFinitePositive(bombChainDelaySeconds, nameof(bombChainDelaySeconds));

            context = sandboxContext;
            inputReader = reader;
            bombDefinition = startingBomb;
            playerVitals = startingPlayerVitals;
            cellsPerSecond = movementCellsPerSecond;
            chainDelaySeconds = bombChainDelaySeconds;
        }

        public GridCellState GetCell(GridPosition position)
        {
            return _grid != null ? _grid.GetCell(position) : default;
        }

        public bool TryGetBomb(BombId bombId, out BombSnapshot snapshot)
        {
            if (_bombs != null)
            {
                return _bombs.TryGetBomb(bombId, out snapshot);
            }

            snapshot = default;
            return false;
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
                throw new InvalidOperationException("Unity supplied an invalid simulation delta time.");
            }

            _clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));
            if (!_health.IsDead && _movement.TryAdvance(out PlayerMovementStep step))
            {
                PlayerMoved?.Invoke(step);
            }

            var explosions = _bombs.ProcessDueBombs();
            _appliedDamageResults.Clear();
            for (int index = 0; index < explosions.Count; index++)
            {
                BombExplosion explosion = explosions[index];
                _movement.NotifyBombRemoved(explosion.BombId);
                if (Contains(explosion.AffectedCells, _movement.CurrentPosition))
                {
                    PlayerDamageResult damage = _health.ApplyExplosionDamage(
                        explosion.BombId,
                        DefaultExplosionDamage);
                    if (damage.WasApplied)
                    {
                        _appliedDamageResults.Add(damage);
                    }
                }
            }

            for (int index = 0; index < explosions.Count; index++)
            {
                BombExploded?.Invoke(explosions[index]);
            }
            for (int index = 0; index < _appliedDamageResults.Count; index++)
            {
                PlayerDamageResult damage = _appliedDamageResults[index];
                PlayerDamaged?.Invoke(damage);
                if (damage.WasFatal)
                {
                    _movement.SetMoveDirection(CardinalDirection.None);
                    PlayerDied?.Invoke(damage);
                }
            }
        }

        private void Initialize()
        {
            if (context == null || inputReader == null || bombDefinition == null ||
                playerVitals == null)
            {
                throw new InvalidOperationException(
                    "PrototypeGameSession requires context, input reader, bomb definition, and player vitals references.");
            }

            ValidateFinitePositive(cellsPerSecond, nameof(cellsPerSecond));
            ValidateFinitePositive(chainDelaySeconds, nameof(chainDelaySeconds));

            _grid = CreateGrid(context);
            _clock = new ManualGameClock();
            GridPosition start = context.GridSpace.WorldToGrid(context.PlayerSpawn.position);
            _movement = new PlayerMovementSimulation(
                _grid,
                _clock,
                PrototypePlayerActorId,
                start,
                TimeSpan.FromSeconds(1f / cellsPerSecond));
            _bombs = new BombSimulation(
                _grid,
                _clock,
                TimeSpan.FromSeconds(chainDelaySeconds));
            _health = new PlayerHealthSimulation(
                _movement.ActorId,
                _clock,
                playerVitals.CreateCoreDefinition());

            _coreBombDefinition = bombDefinition.CreateCoreDefinition();
        }

        private void OnCommandIssued(PlayerCommand command)
        {
            if (_health.IsDead)
            {
                return;
            }

            switch (command.Kind)
            {
                case PlayerCommandKind.Move:
                    _movement.SetMoveDirection(command.MoveDirection);
                    break;
                case PlayerCommandKind.PlaceBomb:
                    TryPlaceBomb();
                    break;
                case PlayerCommandKind.SwapBomb:
                case PlayerCommandKind.Pause:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(command),
                        command.Kind,
                        "Unsupported player command kind.");
            }
        }

        private void TryPlaceBomb()
        {
            if (!_bombs.TryPlaceBomb(
                _coreBombDefinition,
                _movement.CurrentPosition,
                _movement.ActorId,
                out BombId bombId))
            {
                return;
            }

            if (!_bombs.TryGetBomb(bombId, out BombSnapshot snapshot))
            {
                throw new InvalidOperationException("Placed bomb was not available for presentation.");
            }

            _movement.GrantBombPassThrough(snapshot);
            BombPlaced?.Invoke(snapshot);
        }

        private static bool Contains(
            IReadOnlyList<GridPosition> positions,
            GridPosition target)
        {
            for (int index = 0; index < positions.Count; index++)
            {
                if (positions[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static GridState CreateGrid(TestSandboxContext sandboxContext)
        {
            var grid = new GridState();
            int halfWidth = sandboxContext.GridWidth / 2;
            int halfDepth = sandboxContext.GridDepth / 2;
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int z = -halfDepth; z <= halfDepth; z++)
                {
                    if (!grid.TrySetTerrain(new GridPosition(x, z), GridTerrain.Floor))
                    {
                        throw new InvalidOperationException("Could not create TestSandbox floor cell.");
                    }
                }
            }

            foreach (Vector2Int blocker in sandboxContext.BlockedCells)
            {
                if (!grid.TrySetTerrain(
                    new GridPosition(blocker.x, blocker.y),
                    GridTerrain.IndestructibleWall))
                {
                    throw new InvalidOperationException($"Could not author blocked cell {blocker}.");
                }
            }

            return grid;
        }

        private static void ValidateFinitePositive(float value, string parameterName)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and positive.");
            }
        }
    }
}
