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
        private static readonly ActorId PrototypeBossActorId = new ActorId(5);
        private static readonly IReadOnlyList<GridPosition> NoBossDangerCells =
            Array.AsReadOnly(Array.Empty<GridPosition>());

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
        private PrototypeBossDefinitionAsset bossDefinition;

        [SerializeField]
        private float cellsPerSecond = DefaultCellsPerSecond;

        [SerializeField]
        private float chainDelaySeconds = DefaultChainDelaySeconds;

        [SerializeField]
        private bool combatEnabled = true;

        [SerializeField]
        private bool bossEnabled;

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
        private BossBattleSimulation _boss;
        private CombatRoomDefinition _runtimeRoomDefinition;
        private GridPosition? _runtimePlayerStart;
        private bool? _runtimeCombatEnabled;
        private bool? _runtimeBossEnabled;
        private PrototypeBombDefinitionAsset _runtimeFirstBombDefinition;
        private PrototypeBombDefinitionAsset _runtimeSecondBombDefinition;
        private PrototypeBombDefinitionAsset[] _runtimeBombDefinitions;
        private float _runtimeSwapCooldownSeconds;
        private int _runtimeActiveBombSlotIndex;
        private int? _runtimeInitialPlayerHealth;
        private readonly List<PlayerDamageResult> _appliedDamageResults =
            new List<PlayerDamageResult>();
        private readonly List<EnemyDamageResult> _appliedEnemyDamageResults =
            new List<EnemyDamageResult>();
        private readonly List<ArmoredEnemyDamageResult> _armoredDamageResults =
            new List<ArmoredEnemyDamageResult>();
        private readonly List<BossDamageResult> _bossDamageResults =
            new List<BossDamageResult>();
        private bool _roomCleared;
        private bool _hasCharger;
        private bool _hasArmored;
        private bool _isPaused;

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

        public event Action<BossPatternTransition> BossPatternTransitioned;

        public event Action<BossDamageResult> BossDamaged;

        public event Action RoomCleared;

        public event Action<bool> PauseStateChanged;

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

        public PrototypeBossDefinitionAsset BossDefinition => bossDefinition;

        public float CellsPerSecond => cellsPerSecond;

        public float ChainDelaySeconds => chainDelaySeconds;

        public bool IsCombatEnabledByDefault => combatEnabled;

        public bool IsBossEnabledByDefault => bossEnabled;

        public bool HasChaser => IsCombatEnabledForVisit && !IsBossEnabledForVisit;

        public bool HasBoss => IsBossEnabledForVisit;

        public bool IsInitialized => _movement != null && _bombs != null && _weapons != null &&
            _health != null &&
            (!HasChaser || (_chaser != null && _chaserHealth != null)) &&
            (!_hasCharger || (_charger != null && _chargerHealth != null)) &&
            (!_hasArmored || _armored != null) &&
            (!HasBoss || _boss != null);

        public bool IsReady { get; private set; }

        public bool IsPaused => _isPaused;

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

        public ActorId BossActorId => _boss != null ? _boss.ActorId : default;

        public GridPosition CurrentBossGridPosition =>
            _boss != null ? _boss.BossPosition : default;

        public BossBattleState CurrentBossState =>
            _boss != null ? _boss.State : BossBattleState.Telegraph;

        public BossPhase CurrentBossPhase =>
            _boss != null ? _boss.Phase : BossPhase.One;

        public BossPatternKind CurrentBossPattern =>
            _boss != null ? _boss.CurrentPattern : BossPatternKind.AlternatingColumns;

        public IReadOnlyList<GridPosition> CurrentBossDangerCells =>
            _boss != null ? _boss.CurrentDangerCells : NoBossDangerCells;

        public int CurrentBossHealth => _boss != null ? _boss.CurrentHealth : 0;

        public int MaxBossHealth => _boss != null ? _boss.MaxHealth : 0;

        public bool IsBossAlive => HasBoss && _boss != null && !_boss.IsDead;

        public bool IsBossVulnerable => HasBoss && _boss != null && _boss.IsVulnerable;

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
                if (_boss != null && !_boss.IsDead)
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
            bool startingCombatEnabled = true,
            PrototypeBossDefinitionAsset startingBoss = null,
            bool startingBossEnabled = false)
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
            if (startingBossEnabled && !startingCombatEnabled)
            {
                throw new ArgumentException(
                    "A boss encounter must also enable combat.",
                    nameof(startingBossEnabled));
            }
            if (startingCombatEnabled && !startingBossEnabled && startingChaser == null)
            {
                throw new ArgumentNullException(nameof(startingChaser));
            }
            if (startingBossEnabled && startingBoss == null)
            {
                throw new ArgumentNullException(nameof(startingBoss));
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
            bossDefinition = startingBoss;
            cellsPerSecond = movementCellsPerSecond;
            chainDelaySeconds = bombChainDelaySeconds;
            combatEnabled = startingCombatEnabled;
            bossEnabled = startingBossEnabled;
            _runtimeCombatEnabled = null;
            _runtimeBossEnabled = null;
            _runtimeInitialPlayerHealth = null;
        }

        public void PrepareRuntimePlayerHealth(int currentHealth)
        {
            if (_health != null)
            {
                throw new InvalidOperationException(
                    "Runtime player health must be prepared before session initialization.");
            }
            if (playerVitals == null)
            {
                throw new InvalidOperationException(
                    "Prototype game session requires player vitals before preparing runtime health.");
            }
            if (currentHealth < 0 || currentHealth > playerVitals.MaxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentHealth),
                    currentHealth,
                    $"Runtime player health must be between 0 and {playerVitals.MaxHealth}.");
            }

            _runtimeInitialPlayerHealth = currentHealth;
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
            PrepareRuntimeRoom(
                roomDefinition,
                playerStart,
                combatEnabledForVisit,
                bossEnabled && combatEnabledForVisit);
        }

        public void PrepareRuntimeRoom(
            CombatRoomDefinition roomDefinition,
            GridPosition playerStart,
            bool combatEnabledForVisit,
            bool bossEnabledForVisit)
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
            if (bossEnabledForVisit && !combatEnabledForVisit)
            {
                throw new ArgumentException(
                    "A boss runtime visit must also enable combat.",
                    nameof(bossEnabledForVisit));
            }
            if (bossEnabledForVisit && !bossEnabled)
            {
                throw new InvalidOperationException(
                    "A room authored without a boss cannot enable one for a runtime visit.");
            }
            if (bossEnabledForVisit && bossDefinition == null)
            {
                throw new InvalidOperationException(
                    "A boss runtime visit requires a boss definition reference.");
            }
            if (bossEnabledForVisit &&
                (!roomDefinition.IsInside(bossDefinition.BossSpawn) ||
                 roomDefinition.IsBlocked(bossDefinition.BossSpawn)))
            {
                throw new InvalidOperationException(
                    $"Boss spawn {bossDefinition.BossSpawn} must be a traversable room cell.");
            }
            if (bossEnabledForVisit && playerStart == bossDefinition.BossSpawn)
            {
                throw new ArgumentException(
                    $"Runtime player start {playerStart} cannot overlap the boss spawn.",
                    nameof(playerStart));
            }
            if (combatEnabledForVisit && !bossEnabledForVisit &&
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
            _runtimeBossEnabled = bossEnabledForVisit;
        }

        public void PrepareRuntimeBombLoadout(
            PrototypeBombDefinitionAsset firstSlot,
            PrototypeBombDefinitionAsset secondSlot,
            PrototypeBombDefinitionAsset[] availableDefinitions,
            float swapCooldownSeconds,
            int activeSlotIndex)
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
            if (activeSlotIndex < 0 || activeSlotIndex >= BombWeaponLoadout.SlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeSlotIndex),
                    activeSlotIndex,
                    $"Runtime active bomb slot must be between 0 and {BombWeaponLoadout.SlotCount - 1}.");
            }
            if (activeSlotIndex == 1 && secondSlot == null)
            {
                throw new ArgumentException(
                    "The runtime second bomb slot must be equipped before it can be active.",
                    nameof(activeSlotIndex));
            }

            _runtimeFirstBombDefinition = firstSlot;
            _runtimeSecondBombDefinition = secondSlot;
            _runtimeBombDefinitions = copy;
            _runtimeSwapCooldownSeconds = swapCooldownSeconds;
            _runtimeActiveBombSlotIndex = activeSlotIndex;
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

        public bool TrySwapActiveBomb()
        {
            if (_weapons == null)
            {
                throw new InvalidOperationException("Prototype bomb loadout is not initialized.");
            }
            if (!_weapons.TrySwap())
            {
                return false;
            }

            ActiveBombSlotChanged?.Invoke(_weapons.ActiveSlotIndex);
            return true;
        }

        public bool TryPlaceBomb()
        {
            if (_weapons == null || _bombs == null || _movement == null || _health == null)
            {
                throw new InvalidOperationException(
                    "Prototype bomb placement is not initialized.");
            }
            if (_health.IsDead || _isPaused)
            {
                return false;
            }
            if (!_weapons.TryPlaceActiveBomb(
                _bombs,
                _movement.CurrentPosition,
                _movement.ActorId,
                _movement.FacingDirection,
                out BombSnapshot snapshot))
            {
                return false;
            }

            _movement.GrantBombPassThrough(snapshot);
            BombPlaced?.Invoke(snapshot);
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

            EnsurePausePresenter();
            inputReader.CommandIssued += OnCommandIssued;
            IsReady = true;
            Ready?.Invoke();
        }

        private void OnDisable()
        {
            if (_isPaused)
            {
                _isPaused = false;
                PauseStateChanged?.Invoke(false);
            }
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
            if (_isPaused)
            {
                return;
            }

            inputReader.RefreshMoveIntent();

            float elapsedSeconds = Time.deltaTime;
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                throw new InvalidOperationException("Unity supplied an invalid simulation delta time.");
            }

            _clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));
            _appliedDamageResults.Clear();
            _appliedEnemyDamageResults.Clear();
            _armoredDamageResults.Clear();
            _bossDamageResults.Clear();
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
            if (HasChaser && !_chaserHealth.IsDead &&
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

            BossPatternTransition? bossTransition = null;
            if (HasBoss && !_boss.IsDead &&
                _boss.TryAdvance(out BossPatternTransition transition))
            {
                bossTransition = transition;
                if (!_health.IsDead && transition.AttackResolved &&
                    Contains(transition.DangerCells, _movement.CurrentPosition))
                {
                    PlayerDamageResult patternDamage = _health.ApplyBossPatternDamage(
                        _boss.ActorId,
                        _boss.Definition.PatternDamage);
                    if (patternDamage.WasApplied)
                    {
                        _appliedDamageResults.Add(patternDamage);
                    }
                }
            }

            var explosions = _bombs.ProcessDueBombs();
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
                if (HasChaser)
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
                if (HasBoss)
                {
                    ApplyBossExplosionDamage(explosion);
                }
            }

            if (!_health.IsDead && HasChaser && !_chaserHealth.IsDead &&
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
            if (bossTransition.HasValue)
            {
                BossPatternTransitioned?.Invoke(bossTransition.Value);
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
            for (int index = 0; index < _bossDamageResults.Count; index++)
            {
                BossDamaged?.Invoke(_bossDamageResults[index]);
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
            bool bossEnabledForVisit = IsBossEnabledForVisit;
            if (context == null || inputReader == null || bombLoadout == null ||
                playerVitals == null || (HasChaser && chaserDefinition == null) ||
                (bossEnabledForVisit && bossDefinition == null))
            {
                throw new InvalidOperationException(
                    "PrototypeGameSession is missing one or more required encounter references.");
            }

            ValidateFinitePositive(cellsPerSecond, nameof(cellsPerSecond));
            ValidateFinitePositive(chainDelaySeconds, nameof(chainDelaySeconds));
            CombatRoomDefinition roomDefinition = _runtimeRoomDefinition ??
                context.RoomDefinition.CreateCoreDefinition();
            _hasCharger = HasChaser && roomDefinition.ChargerSpawn.HasValue;
            _hasArmored = HasChaser && roomDefinition.ArmoredSpawn.HasValue;
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
                     TimeSpan.FromSeconds(_runtimeSwapCooldownSeconds),
                     _runtimeActiveBombSlotIndex)
                : bombLoadout.CreateCoreLoadout(_clock);
            PlayerHealthDefinition healthDefinition =
                playerVitals.CreateCoreDefinition();
            _health = _runtimeInitialPlayerHealth.HasValue
                ? new PlayerHealthSimulation(
                    _movement.ActorId,
                    _clock,
                    healthDefinition,
                    _runtimeInitialPlayerHealth.Value)
                : new PlayerHealthSimulation(
                    _movement.ActorId,
                    _clock,
                    healthDefinition);

            if (HasChaser)
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
            if (bossEnabledForVisit)
            {
                BossBattleDefinition coreBossDefinition =
                    bossDefinition.CreateCoreDefinition();
                _boss = new BossBattleSimulation(
                    _grid,
                    _clock,
                    coreBossDefinition,
                    PrototypeBossActorId,
                    bossDefinition.BossSpawn,
                    CreatePlayableArenaCells(roomDefinition));
            }
            _roomCleared = !combatEnabledForVisit;

        }

        private bool IsCombatEnabledForVisit =>
            _runtimeCombatEnabled ?? combatEnabled;

        private bool IsBossEnabledForVisit =>
            _runtimeBossEnabled ?? bossEnabled;

        private void OnCommandIssued(PlayerCommand command)
        {
            if (_health.IsDead)
            {
                return;
            }

            if (command.Kind == PlayerCommandKind.Pause)
            {
                TogglePause();
                return;
            }
            if (_isPaused)
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
                    TrySwapActiveBomb();
                    break;
                case PlayerCommandKind.RestartRun:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(command),
                        command.Kind,
                        "Unsupported player command kind.");
            }
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            _movement.SetMoveDirection(CardinalDirection.None);
            if (_isPaused)
            {
                inputReader.ReleaseMoveIntent();
            }
            else
            {
                inputReader.RefreshMoveIntent();
            }
            PauseStateChanged?.Invoke(_isPaused);
        }

        private void EnsurePausePresenter()
        {
            PrototypePausePresenter presenter = GetComponent<PrototypePausePresenter>();
            if (presenter == null)
            {
                presenter = gameObject.AddComponent<PrototypePausePresenter>();
            }
            presenter.Configure(this);
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

        private void ApplyBossExplosionDamage(BombExplosion explosion)
        {
            if (_boss.IsDead ||
                !Contains(explosion.AffectedCells, _boss.BossPosition))
            {
                return;
            }

            BossDamageResult result = _boss.ApplyExplosion(
                explosion.BombId,
                DefaultEnemyExplosionDamage);
            if (result.WasApplied)
            {
                _bossDamageResults.Add(result);
            }
        }

        private static IReadOnlyList<GridPosition> CreatePlayableArenaCells(
            CombatRoomDefinition roomDefinition)
        {
            var cells = new List<GridPosition>();
            int halfWidth = roomDefinition.Width / 2;
            int halfDepth = roomDefinition.Depth / 2;
            for (int z = -halfDepth; z <= halfDepth; z++)
            {
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    var position = new GridPosition(x, z);
                    if (!roomDefinition.IsBlocked(position))
                    {
                        cells.Add(position);
                    }
                }
            }
            return cells;
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
