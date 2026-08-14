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
        private static readonly ActorId PrototypeArmoredActorId = new ActorId(4);

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
        private PrototypeArmoredDefinitionAsset armoredDefinition;

        [SerializeField]
        private float cellsPerSecond = DefaultCellsPerSecond;

        [SerializeField]
        private float chainDelaySeconds = DefaultChainDelaySeconds;

        [SerializeField]
        private bool combatEnabled = true;

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
        private ArmoredEnemySimulation _armored;
        private ArmoredEnemyDefinition _coreArmoredDefinition;
        private CombatRoomDefinition _runtimeRoomDefinition;
        private GridPosition? _runtimePlayerStart;
        private bool? _runtimeCombatEnabled;
        private PrototypeBombDefinitionAsset _runtimeFirstBombDefinition;
        private PrototypeBombDefinitionAsset _runtimeSecondBombDefinition;
        private PrototypeBombDefinitionAsset[] _runtimeBombDefinitions;
        private float _runtimeSwapCooldownSeconds;
        private readonly List<PlayerDamageResult> _appliedDamageResults =
            new List<PlayerDamageResult>();
        private readonly List<EnemyDamageResult> _appliedEnemyDamageResults =
            new List<EnemyDamageResult>();
        private readonly List<ArmoredEnemyDamageResult> _armoredDamageResults =
            new List<ArmoredEnemyDamageResult>();
        private bool _roomCleared;
        private bool _hasCharger;
        private bool _hasArmored;

        public event Action<PlayerMovementStep> PlayerMoved;

        public event Action<GridSubcellPosition, CardinalDirection> PlayerPositionChanged;

        public event Action<BombSnapshot> BombPlaced;

        public event Action<BombExplosion> BombExploded;

        public event Action<int> ActiveBombSlotChanged;

        public event Action<int> BombSlotEquipped;

        public event Action<PlayerDamageResult> PlayerDamaged;

        public event Action<PlayerDamageResult> PlayerDied;

        public event Action<EnemyMovementStep> ChaserMoved;

        public event Action<ChargerEnemyAdvanceResult> ChargerAdvanced;

        public event Action<EnemyMovementStep> ArmoredMoved;

        public event Action<ArmoredEnemyDamageResult> ArmoredStateChanged;

        public event Action<EnemyDamageResult> EnemyDamaged;

        public event Action<EnemyDamageResult> EnemyDied;

        public event Action RoomCleared;

        public event Action Ready;

        public TestSandboxContext Context => context;

        public BombSwapInputReader InputReader => inputReader;

        public PrototypeBombDefinitionAsset BombDefinition =>
            _runtimeFirstBombDefinition != null
                ? _runtimeFirstBombDefinition
                : bombLoadout != null
                    ? bombLoadout.FirstSlot
                    : bombDefinition;

        public PrototypeBombLoadoutAsset BombLoadout => bombLoadout;

        public PrototypePlayerVitalsAsset PlayerVitals => playerVitals;

        public PrototypeChaserDefinitionAsset ChaserDefinition => chaserDefinition;

        public PrototypeChargerDefinitionAsset ChargerDefinition => chargerDefinition;

        public PrototypeArmoredDefinitionAsset ArmoredDefinition => armoredDefinition;

        public float CellsPerSecond => cellsPerSecond;

        public float ChainDelaySeconds => chainDelaySeconds;

        public bool IsCombatEnabledByDefault => combatEnabled;

        public bool HasChaser => IsCombatEnabledForVisit;

        public bool IsInitialized => _movement != null && _bombs != null && _weapons != null &&
            _health != null &&
            (!IsCombatEnabledForVisit || (_chaser != null && _chaserHealth != null)) &&
            (!_hasCharger || (_charger != null && _chargerHealth != null)) &&
            (!_hasArmored || _armored != null);

        public bool IsReady { get; private set; }

        public GridPosition CurrentGridPosition =>
            _movement != null ? _movement.CurrentPosition : default;

        public GridSubcellPosition CurrentMovementPosition =>
            _movement != null ? _movement.Position : default;

        public int ActiveBombCount => _bombs != null ? _bombs.ActiveBombCount : 0;

        public int ActiveBombSlotIndex => _weapons != null ? _weapons.ActiveSlotIndex : 0;

        public bool HasSecondBombSlot => _weapons != null
            ? _weapons.HasSecondSlot
            : _runtimeSecondBombDefinition != null ||
              (_runtimeFirstBombDefinition == null &&
               bombLoadout != null && bombLoadout.SecondSlot != null);

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

        public bool HasArmored => _hasArmored;

        public ActorId ArmoredActorId => _armored != null ? _armored.ActorId : default;

        public GridPosition CurrentArmoredGridPosition =>
            _armored != null ? _armored.CurrentPosition : default;

        public ArmoredEnemyState CurrentArmoredState =>
            _armored != null ? _armored.State : ArmoredEnemyState.Armored;

        public bool IsArmoredAlive => _hasArmored && _armored != null && !_armored.IsDead;

        public int EnemyActiveCount
        {
            get
            {
                int count = _chaserHealth != null && !_chaserHealth.IsDead ? 1 : 0;
                if (_chargerHealth != null && !_chargerHealth.IsDead)
                {
                    count++;
                }
                if (_armored != null && !_armored.IsDead)
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
            PrototypeChargerDefinitionAsset startingCharger = null,
            PrototypeArmoredDefinitionAsset startingArmored = null,
            bool startingCombatEnabled = true)
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
            if (startingCombatEnabled && startingChaser == null)
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
            armoredDefinition = startingArmored;
            cellsPerSecond = movementCellsPerSecond;
            chainDelaySeconds = bombChainDelaySeconds;
            combatEnabled = startingCombatEnabled;
            _runtimeCombatEnabled = null;
        }

        public void PrepareRuntimeRoom(
            CombatRoomDefinition roomDefinition,
            GridPosition playerStart)
        {
            PrepareRuntimeRoom(roomDefinition, playerStart, combatEnabled);
        }

        public void PrepareRuntimeRoom(
            CombatRoomDefinition roomDefinition,
            GridPosition playerStart,
            bool combatEnabledForVisit)
        {
            if (_movement != null)
            {
                throw new InvalidOperationException(
                    "Runtime room configuration must be prepared before session initialization.");
            }
            if (roomDefinition == null)
            {
                throw new ArgumentNullException(nameof(roomDefinition));
            }
            if (!roomDefinition.IsInside(playerStart) || roomDefinition.IsBlocked(playerStart))
            {
                throw new ArgumentException(
                    $"Runtime player start {playerStart} must be a traversable room cell.",
                    nameof(playerStart));
            }
            if (combatEnabledForVisit && !combatEnabled)
            {
                throw new InvalidOperationException(
                    "A room authored without combat cannot enable enemies for a runtime visit.");
            }
            if (combatEnabledForVisit &&
                (playerStart == roomDefinition.ChaserSpawn ||
                 (roomDefinition.ChargerSpawn.HasValue &&
                  playerStart == roomDefinition.ChargerSpawn.Value) ||
                 (roomDefinition.ArmoredSpawn.HasValue &&
                  playerStart == roomDefinition.ArmoredSpawn.Value)))
            {
                throw new ArgumentException(
                    $"Runtime player start {playerStart} cannot overlap an enemy spawn.",
                    nameof(playerStart));
            }

            _runtimeRoomDefinition = roomDefinition;
            _runtimePlayerStart = playerStart;
            _runtimeCombatEnabled = combatEnabledForVisit;
        }

        public void PrepareRuntimeBombLoadout(
            PrototypeBombDefinitionAsset firstSlot,
            PrototypeBombDefinitionAsset secondSlot,
            PrototypeBombDefinitionAsset[] availableDefinitions,
            float swapCooldownSeconds)
        {
            if (_weapons != null)
            {
                throw new InvalidOperationException(
                    "Runtime bomb loadout must be prepared before session initialization.");
            }
            if (firstSlot == null)
            {
                throw new ArgumentNullException(nameof(firstSlot));
            }
            if (availableDefinitions == null)
            {
                throw new ArgumentNullException(nameof(availableDefinitions));
            }
            ValidateFinitePositive(swapCooldownSeconds, nameof(swapCooldownSeconds));

            var copy = new PrototypeBombDefinitionAsset[availableDefinitions.Length];
            bool foundFirst = false;
            bool foundSecond = secondSlot == null;
            for (int index = 0; index < availableDefinitions.Length; index++)
            {
                PrototypeBombDefinitionAsset definition = availableDefinitions[index];
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Runtime bomb definitions cannot contain null.",
                        nameof(availableDefinitions));
                }
                definition.CreateCoreWeaponDefinition();
                for (int previous = 0; previous < index; previous++)
                {
                    if (copy[previous].DefinitionId == definition.DefinitionId)
                    {
                        throw new ArgumentException(
                            "Runtime bomb definition IDs must be unique.",
                            nameof(availableDefinitions));
                    }
                }
                copy[index] = definition;
                foundFirst |= definition.DefinitionId == firstSlot.DefinitionId;
                foundSecond |= secondSlot != null &&
                    definition.DefinitionId == secondSlot.DefinitionId;
            }
            if (!foundFirst || !foundSecond)
            {
                throw new ArgumentException(
                    "Runtime bomb definitions must include every equipped slot.",
                    nameof(availableDefinitions));
            }
            if (secondSlot != null && secondSlot.DefinitionId == firstSlot.DefinitionId)
            {
                throw new ArgumentException(
                    "Runtime bomb slots must use different definition IDs.",
                    nameof(secondSlot));
            }

            _runtimeFirstBombDefinition = firstSlot;
            _runtimeSecondBombDefinition = secondSlot;
            _runtimeBombDefinitions = copy;
            _runtimeSwapCooldownSeconds = swapCooldownSeconds;
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
            if (_runtimeBombDefinitions != null)
            {
                for (int index = 0; index < _runtimeBombDefinitions.Length; index++)
                {
                    PrototypeBombDefinitionAsset definition = _runtimeBombDefinitions[index];
                    if (definition.DefinitionId == definitionId.Value)
                    {
                        return definition;
                    }
                }
                throw new InvalidOperationException(
                    $"Bomb definition '{definitionId}' is not part of this runtime loadout catalog.");
            }
            if (bombLoadout == null)
            {
                throw new InvalidOperationException("Prototype game session has no bomb loadout.");
            }

            return bombLoadout.GetDefinition(definitionId);
        }

        public PrototypeBombDefinitionAsset GetBombDefinitionForSlot(int slotIndex)
        {
            BombWeaponSlotSnapshot slot = GetBombSlot(slotIndex);
            return slot.HasDefinition ? GetBombDefinition(slot.DefinitionId) : null;
        }

        public bool TryEquipSecondBomb(PrototypeBombDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (_weapons == null || _runtimeBombDefinitions == null)
            {
                throw new InvalidOperationException(
                    "Only an initialized dungeon runtime loadout can receive a bomb reward.");
            }

            PrototypeBombDefinitionAsset canonical = null;
            for (int index = 0; index < _runtimeBombDefinitions.Length; index++)
            {
                if (_runtimeBombDefinitions[index].DefinitionId == definition.DefinitionId)
                {
                    canonical = _runtimeBombDefinitions[index];
                    break;
                }
            }
            if (canonical == null)
            {
                return false;
            }
            if (!_weapons.TryEquipSecondSlot(canonical.CreateCoreWeaponDefinition()))
            {
                return false;
            }

            _runtimeSecondBombDefinition = canonical;
            BombSlotEquipped?.Invoke(1);
            return true;
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
            inputReader.RefreshMoveIntent();

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
            if (IsCombatEnabledForVisit && !_chaserHealth.IsDead &&
                _chaser.TryAdvance(out EnemyMovementStep chaserStep))
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
            if (_hasArmored && !_armored.IsDead &&
                _armored.TryAdvance(out EnemyMovementStep armoredStep))
            {
                ArmoredMoved?.Invoke(armoredStep);
            }

            var explosions = _bombs.ProcessDueBombs();
            _appliedDamageResults.Clear();
            _appliedEnemyDamageResults.Clear();
            _armoredDamageResults.Clear();
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
                if (IsCombatEnabledForVisit)
                {
                    ApplyEnemyExplosionDamage(
                        explosion,
                        _chaser.ActorId,
                        _chaserHealth,
                        "chaser");
                }
                if (_hasCharger)
                {
                    ApplyEnemyExplosionDamage(
                        explosion,
                        _charger.ActorId,
                        _chargerHealth,
                        "charger");
                }
                if (_hasArmored)
                {
                    ApplyArmoredExplosionDamage(explosion);
                }
            }

            if (!_health.IsDead && IsCombatEnabledForVisit && !_chaserHealth.IsDead &&
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
            if (!_health.IsDead && _hasArmored && !_armored.IsDead &&
                _movement.CurrentPosition.IsCardinallyAdjacentTo(_armored.CurrentPosition))
            {
                PlayerDamageResult armoredContactDamage = _health.ApplyContactDamage(
                    _armored.ActorId,
                    _coreArmoredDefinition.ContactDamage);
                if (armoredContactDamage.WasApplied)
                {
                    _appliedDamageResults.Add(armoredContactDamage);
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
            for (int index = 0; index < _armoredDamageResults.Count; index++)
            {
                ArmoredStateChanged?.Invoke(_armoredDamageResults[index]);
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
            bool combatEnabledForVisit = IsCombatEnabledForVisit;
            if (context == null || inputReader == null || bombLoadout == null ||
                playerVitals == null || (combatEnabledForVisit && chaserDefinition == null))
            {
                throw new InvalidOperationException(
                    "PrototypeGameSession requires context, input reader, bomb loadout, player-vitals, and a chaser reference when combat is enabled.");
            }

            ValidateFinitePositive(cellsPerSecond, nameof(cellsPerSecond));
            ValidateFinitePositive(chainDelaySeconds, nameof(chainDelaySeconds));
            CombatRoomDefinition roomDefinition = _runtimeRoomDefinition ??
                context.RoomDefinition.CreateCoreDefinition();
            _hasCharger = combatEnabledForVisit && roomDefinition.ChargerSpawn.HasValue;
            _hasArmored = combatEnabledForVisit && roomDefinition.ArmoredSpawn.HasValue;
            if (_hasCharger && chargerDefinition == null)
            {
                throw new InvalidOperationException(
                    "A room with a charger spawn requires a charger definition reference.");
            }
            if (_hasArmored && armoredDefinition == null)
            {
                throw new InvalidOperationException(
                    "A room with an armored spawn requires an armored definition reference.");
            }

            _grid = CreateGrid(roomDefinition);
            _clock = new ManualGameClock();
            GridPosition start = _runtimePlayerStart ?? roomDefinition.PlayerSpawn;
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
            _weapons = _runtimeFirstBombDefinition != null
                ? new BombWeaponLoadout(
                    _clock,
                    _runtimeFirstBombDefinition.CreateCoreWeaponDefinition(),
                    _runtimeSecondBombDefinition != null
                        ? _runtimeSecondBombDefinition.CreateCoreWeaponDefinition()
                        : null,
                    TimeSpan.FromSeconds(_runtimeSwapCooldownSeconds))
                : bombLoadout.CreateCoreLoadout(_clock);
            _health = new PlayerHealthSimulation(
                _movement.ActorId,
                _clock,
                playerVitals.CreateCoreDefinition());

            if (combatEnabledForVisit)
            {
                _coreChaserDefinition = chaserDefinition.CreateCoreDefinition();
                _chaser = new ChaserEnemySimulation(
                    _grid,
                    _clock,
                    _coreChaserDefinition,
                    PrototypeChaserActorId,
                    _movement.ActorId,
                    roomDefinition.ChaserSpawn);
                _chaserHealth = new EnemyHealthSimulation(
                    _chaser.ActorId,
                    _coreChaserDefinition.MaxHealth);
            }
            if (_hasCharger)
            {
                if (context.ChargerSpawn == null)
                {
                    throw new InvalidOperationException(
                        "A room with a charger requires a charger spawn Transform.");
                }

                _coreChargerDefinition = chargerDefinition.CreateCoreDefinition();
                _charger = new ChargerEnemySimulation(
                    _grid,
                    _clock,
                    _coreChargerDefinition,
                    PrototypeChargerActorId,
                    _movement.ActorId,
                    roomDefinition.ChargerSpawn.Value);
                _chargerHealth = new EnemyHealthSimulation(
                    _charger.ActorId,
                    _coreChargerDefinition.MaxHealth);
            }
            if (_hasArmored)
            {
                if (context.ArmoredSpawn == null)
                {
                    throw new InvalidOperationException(
                        "A room with an armored enemy requires an armored spawn Transform.");
                }

                _coreArmoredDefinition = armoredDefinition.CreateCoreDefinition();
                _armored = new ArmoredEnemySimulation(
                    _grid,
                    _clock,
                    _coreArmoredDefinition,
                    PrototypeArmoredActorId,
                    _movement.ActorId,
                    roomDefinition.ArmoredSpawn.Value);
            }
            _roomCleared = !combatEnabledForVisit;

        }

        private bool IsCombatEnabledForVisit =>
            _runtimeCombatEnabled ?? combatEnabled;

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

        private void ApplyArmoredExplosionDamage(BombExplosion explosion)
        {
            if (_armored.IsDead ||
                !_grid.TryGetActorPosition(_armored.ActorId, out GridPosition position) ||
                !Contains(explosion.AffectedCells, position))
            {
                return;
            }

            ArmoredEnemyDamageResult result = _armored.ApplyExplosion(explosion.BombId);
            if (!result.Damage.WasApplied)
            {
                return;
            }

            _armoredDamageResults.Add(result);
            _appliedEnemyDamageResults.Add(result.Damage);
        }

        private static GridState CreateGrid(CombatRoomDefinition roomDefinition)
        {
            var grid = new GridState();
            int halfWidth = roomDefinition.Width / 2;
            int halfDepth = roomDefinition.Depth / 2;
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

            foreach (GridPosition blocker in roomDefinition.IndestructibleWalls)
            {
                if (!grid.TrySetTerrain(
                    blocker,
                    GridTerrain.IndestructibleWall))
                {
                    throw new InvalidOperationException($"Could not author blocked cell {blocker}.");
                }
            }

            foreach (GridPosition blocker in roomDefinition.DestructibleWalls)
            {
                if (!grid.TrySetTerrain(
                    blocker,
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
