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
        public const int DefaultEnemyExplosionDamage = 1;

        private static readonly ActorId PrototypePlayerActorId = new ActorId(1);
        private static readonly ActorId PrototypeChaserActorId = new ActorId(2);
        private static readonly ActorId PrototypeChargerActorId = new ActorId(3);

        [SerializeField]
        private TestSandboxContext context;

        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private PrototypeBombDefinitionAsset bombDefinition;

        [SerializeField]
        private PrototypeBombLoadoutAsset bombLoadout;

        [SerializeField]
        private PrototypePlayerVitalsAsset playerVitals;

        [SerializeField]
        private PrototypeChaserDefinitionAsset chaserDefinition;

        [SerializeField]
        private PrototypeChargerDefinitionAsset chargerDefinition;

        [SerializeField]
        private float cellsPerSecond = DefaultCellsPerSecond;

        [SerializeField]
        private float chainDelaySeconds = DefaultChainDelaySeconds;

        private GridState _grid;
        private ManualGameClock _clock;
        private PlayerMovementSimulation _movement;
        private BombSimulation _bombs;
        private BombWeaponLoadout _weapons;
        private PlayerHealthSimulation _health;
        private ChaserEnemySimulation _chaser;
        private EnemyHealthSimulation _chaserHealth;
        private ChaserEnemyDefinition _coreChaserDefinition;
        private ChargerEnemySimulation _charger;
        private EnemyHealthSimulation _chargerHealth;
        private ChargerEnemyDefinition _coreChargerDefinition;
        private readonly List<PlayerDamageResult> _appliedDamageResults =
            new List<PlayerDamageResult>();
        private readonly List<EnemyDamageResult> _appliedEnemyDamageResults =
            new List<EnemyDamageResult>();
        private bool _roomCleared;
        private bool _hasCharger;

        public event Action<PlayerMovementStep> PlayerMoved;

        public event Action<GridSubcellPosition, CardinalDirection> PlayerPositionChanged;

        public event Action<BombSnapshot> BombPlaced;

        public event Action<BombExplosion> BombExploded;

        public event Action<int> ActiveBombSlotChanged;

        public event Action<PlayerDamageResult> PlayerDamaged;

        public event Action<PlayerDamageResult> PlayerDied;

        public event Action<EnemyMovementStep> ChaserMoved;

        public event Action<ChargerEnemyAdvanceResult> ChargerAdvanced;

        public event Action<EnemyDamageResult> EnemyDamaged;

        public event Action<EnemyDamageResult> EnemyDied;

        public event Action RoomCleared;

        public event Action Ready;

        public TestSandboxContext Context => context;

        public BombSwapInputReader InputReader => inputReader;

        public PrototypeBombDefinitionAsset BombDefinition =>
            bombLoadout != null ? bombLoadout.FirstSlot : bombDefinition;

        public PrototypeBombLoadoutAsset BombLoadout => bombLoadout;

        public PrototypePlayerVitalsAsset PlayerVitals => playerVitals;

        public PrototypeChaserDefinitionAsset ChaserDefinition => chaserDefinition;

        public PrototypeChargerDefinitionAsset ChargerDefinition => chargerDefinition;

        public float CellsPerSecond => cellsPerSecond;

        public float ChainDelaySeconds => chainDelaySeconds;

        public bool IsInitialized => _movement != null && _bombs != null && _weapons != null &&
            _health != null &&
            _chaser != null && _chaserHealth != null &&
            (!_hasCharger || (_charger != null && _chargerHealth != null));

        public bool IsReady { get; private set; }

        public GridPosition CurrentGridPosition =>
            _movement != null ? _movement.CurrentPosition : default;

        public GridSubcellPosition CurrentMovementPosition =>
            _movement != null ? _movement.Position : default;

        public int ActiveBombCount => _bombs != null ? _bombs.ActiveBombCount : 0;

        public int ActiveBombSlotIndex => _weapons != null ? _weapons.ActiveSlotIndex : 0;

        public TimeSpan BombSwapCooldownRemaining =>
            _weapons != null ? _weapons.SwapCooldownRemaining : TimeSpan.Zero;

        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;

        public int MaxHealth => _health != null ? _health.MaxHealth : 0;

        public ActorId ChaserActorId => _chaser != null ? _chaser.ActorId : default;

        public GridPosition CurrentChaserGridPosition =>
            _chaser != null ? _chaser.CurrentPosition : default;

        public bool IsChaserAlive => _chaserHealth != null && !_chaserHealth.IsDead;

        public bool HasCharger => _hasCharger;

        public ActorId ChargerActorId => _charger != null ? _charger.ActorId : default;

        public GridPosition CurrentChargerGridPosition =>
            _charger != null ? _charger.CurrentPosition : default;

        public ChargerEnemyState CurrentChargerState =>
            _charger != null ? _charger.State : ChargerEnemyState.Track;

        public bool IsChargerAlive =>
            _hasCharger && _chargerHealth != null && !_chargerHealth.IsDead;

        public int EnemyActiveCount
        {
            get
            {
                int count = _chaserHealth != null && !_chaserHealth.IsDead ? 1 : 0;
                if (_chargerHealth != null && !_chargerHealth.IsDead)
                {
                    count++;
                }
                return count;
            }
        }

        public bool IsRoomCleared => _roomCleared;

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
            PrototypeBombLoadoutAsset startingBombLoadout,
            PrototypePlayerVitalsAsset startingPlayerVitals,
            PrototypeChaserDefinitionAsset startingChaser,
            float movementCellsPerSecond = DefaultCellsPerSecond,
            float bombChainDelaySeconds = DefaultChainDelaySeconds,
            PrototypeChargerDefinitionAsset startingCharger = null)
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
            if (startingBombLoadout == null)
            {
                throw new ArgumentNullException(nameof(startingBombLoadout));
            }
            if (startingPlayerVitals == null)
            {
                throw new ArgumentNullException(nameof(startingPlayerVitals));
            }
            if (startingChaser == null)
            {
                throw new ArgumentNullException(nameof(startingChaser));
            }

            ValidateFinitePositive(movementCellsPerSecond, nameof(movementCellsPerSecond));
            ValidateFinitePositive(bombChainDelaySeconds, nameof(bombChainDelaySeconds));

            context = sandboxContext;
            inputReader = reader;
            bombLoadout = startingBombLoadout;
            bombDefinition = startingBombLoadout.FirstSlot;
            playerVitals = startingPlayerVitals;
            chaserDefinition = startingChaser;
            chargerDefinition = startingCharger;
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

        public BombWeaponSlotSnapshot GetBombSlot(int slotIndex)
        {
            if (_weapons == null)
            {
                throw new InvalidOperationException("Prototype bomb loadout is not initialized.");
            }

            return _weapons.GetSlot(slotIndex);
        }

        public PrototypeBombDefinitionAsset GetBombDefinition(BombDefinitionId definitionId)
        {
            if (bombLoadout == null)
            {
                throw new InvalidOperationException("Prototype game session has no bomb loadout.");
            }

            return bombLoadout.GetDefinition(definitionId);
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
            if (!_health.IsDead)
            {
                bool playerPositionChanged = _movement.Advance();
                IReadOnlyList<PlayerMovementStep> playerSteps = _movement.LastCellSteps;
                for (int index = 0; index < playerSteps.Count; index++)
                {
                    PlayerMoved?.Invoke(playerSteps[index]);
                }
                if (playerPositionChanged)
                {
                    PlayerPositionChanged?.Invoke(
                        _movement.Position,
                        _movement.MoveDirection);
                }
            }
            if (!_chaserHealth.IsDead && _chaser.TryAdvance(out EnemyMovementStep chaserStep))
            {
                ChaserMoved?.Invoke(chaserStep);
            }
            ChargerEnemyAdvanceResult chargerAdvance = default;
            if (_hasCharger && !_chargerHealth.IsDead)
            {
                chargerAdvance = _charger.Advance();
                if (chargerAdvance.HasActivity)
                {
                    ChargerAdvanced?.Invoke(chargerAdvance);
                }
            }

            var explosions = _bombs.ProcessDueBombs();
            _appliedDamageResults.Clear();
            _appliedEnemyDamageResults.Clear();
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
                ApplyEnemyExplosionDamage(
                    explosion,
                    _chaser.ActorId,
                    _chaserHealth,
                    "chaser");
                if (_hasCharger)
                {
                    ApplyEnemyExplosionDamage(
                        explosion,
                        _charger.ActorId,
                        _chargerHealth,
                        "charger");
                }
            }

            if (!_health.IsDead && !_chaserHealth.IsDead &&
                _movement.CurrentPosition.IsCardinallyAdjacentTo(_chaser.CurrentPosition))
            {
                PlayerDamageResult contactDamage = _health.ApplyContactDamage(
                    _chaser.ActorId,
                    _coreChaserDefinition.ContactDamage);
                if (contactDamage.WasApplied)
                {
                    _appliedDamageResults.Add(contactDamage);
                }
            }
            if (!_health.IsDead && _hasCharger && !_chargerHealth.IsDead &&
                chargerAdvance.ImpactedTarget)
            {
                PlayerDamageResult chargeDamage = _health.ApplyContactDamage(
                    _charger.ActorId,
                    _coreChargerDefinition.ContactDamage);
                if (chargeDamage.WasApplied)
                {
                    _appliedDamageResults.Add(chargeDamage);
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
            for (int index = 0; index < _appliedEnemyDamageResults.Count; index++)
            {
                EnemyDamageResult damage = _appliedEnemyDamageResults[index];
                EnemyDamaged?.Invoke(damage);
                if (damage.WasFatal)
                {
                    EnemyDied?.Invoke(damage);
                }
            }
            if (!_roomCleared && EnemyActiveCount == 0)
            {
                _roomCleared = true;
                RoomCleared?.Invoke();
            }
        }

        private void Initialize()
        {
            if (context == null || inputReader == null || bombLoadout == null ||
                playerVitals == null || chaserDefinition == null)
            {
                throw new InvalidOperationException(
                    "PrototypeGameSession requires context, input reader, bomb loadout, player-vitals, and chaser references.");
            }

            ValidateFinitePositive(cellsPerSecond, nameof(cellsPerSecond));
            ValidateFinitePositive(chainDelaySeconds, nameof(chainDelaySeconds));
            CombatRoomDefinition roomDefinition = context.RoomDefinition.CreateCoreDefinition();
            _hasCharger = roomDefinition.ChargerSpawn.HasValue;
            if (_hasCharger && chargerDefinition == null)
            {
                throw new InvalidOperationException(
                    "A room with a charger spawn requires a charger definition reference.");
            }

            _grid = CreateGrid(context);
            _clock = new ManualGameClock();
            GridPosition start = context.GridSpace.WorldToGrid(context.PlayerSpawn.position);
            _movement = new PlayerMovementSimulation(
                _grid,
                _clock,
                PrototypePlayerActorId,
                start,
                cellsPerSecond);
            _bombs = new BombSimulation(
                _grid,
                _clock,
                TimeSpan.FromSeconds(chainDelaySeconds));
            _weapons = bombLoadout.CreateCoreLoadout(_clock);
            _health = new PlayerHealthSimulation(
                _movement.ActorId,
                _clock,
                playerVitals.CreateCoreDefinition());

            _coreChaserDefinition = chaserDefinition.CreateCoreDefinition();
            GridPosition chaserStart = context.GridSpace.WorldToGrid(
                context.ChaserSpawn.position);
            _chaser = new ChaserEnemySimulation(
                _grid,
                _clock,
                _coreChaserDefinition,
                PrototypeChaserActorId,
                _movement.ActorId,
                chaserStart);
            _chaserHealth = new EnemyHealthSimulation(
                _chaser.ActorId,
                _coreChaserDefinition.MaxHealth);
            if (_hasCharger)
            {
                if (context.ChargerSpawn == null)
                {
                    throw new InvalidOperationException(
                        "A room with a charger requires a charger spawn Transform.");
                }

                _coreChargerDefinition = chargerDefinition.CreateCoreDefinition();
                GridPosition chargerStart = context.GridSpace.WorldToGrid(
                    context.ChargerSpawn.position);
                _charger = new ChargerEnemySimulation(
                    _grid,
                    _clock,
                    _coreChargerDefinition,
                    PrototypeChargerActorId,
                    _movement.ActorId,
                    chargerStart);
                _chargerHealth = new EnemyHealthSimulation(
                    _charger.ActorId,
                    _coreChargerDefinition.MaxHealth);
            }
            _roomCleared = false;

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
                    if (_weapons.TrySwap())
                    {
                        ActiveBombSlotChanged?.Invoke(_weapons.ActiveSlotIndex);
                    }
                    break;
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
            if (!_weapons.TryPlaceActiveBomb(
                _bombs,
                _movement.CurrentPosition,
                _movement.ActorId,
                out BombSnapshot snapshot))
            {
                return;
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

        private void ApplyEnemyExplosionDamage(
            BombExplosion explosion,
            ActorId actorId,
            EnemyHealthSimulation enemyHealth,
            string enemyLabel)
        {
            if (enemyHealth.IsDead ||
                !_grid.TryGetActorPosition(actorId, out GridPosition position) ||
                !Contains(explosion.AffectedCells, position))
            {
                return;
            }

            EnemyDamageResult damage = enemyHealth.ApplyExplosionDamage(
                explosion.BombId,
                DefaultEnemyExplosionDamage);
            if (damage.WasApplied)
            {
                _appliedEnemyDamageResults.Add(damage);
            }
            if (damage.WasFatal && !_grid.TryRemoveActor(actorId))
            {
                throw new InvalidOperationException(
                    $"Dead prototype {enemyLabel} could not be removed from the logical grid.");
            }
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

            foreach (Vector2Int blocker in sandboxContext.DestructibleCells)
            {
                if (!grid.TrySetTerrain(
                    new GridPosition(blocker.x, blocker.y),
                    GridTerrain.DestructibleWall))
                {
                    throw new InvalidOperationException(
                        $"Could not author destructible cell {blocker}.");
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
