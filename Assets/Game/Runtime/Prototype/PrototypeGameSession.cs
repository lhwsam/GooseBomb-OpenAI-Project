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
        private static readonly TimeSpan SimulationStep = TimeSpan.FromMilliseconds(10);
        private const int MaxSimulationStepsPerFrame = 4096;
        private sealed class PendingBossBombFlight
        {
            public PendingBossBombFlight(BossBombFlight flight)
            {
                Flight = flight;
            }

            public BossBombFlight Flight { get; }

            public bool WasLaunched { get; set; }
        }

        public const float DefaultCellsPerSecond = 5f;
        public const float DefaultChainDelaySeconds = 0.15f;
        public const int DefaultExplosionDamage = 1;
        public const int DefaultEnemyExplosionDamage = 1;

        private static readonly ActorId PrototypePlayerActorId = new ActorId(1);
        private static readonly ActorId PrototypeChaserActorId = new ActorId(2);
        private static readonly ActorId PrototypeChargerActorId = new ActorId(3);
        private static readonly ActorId PrototypeArmoredActorId = new ActorId(4);
        private static readonly ActorId PrototypeBossActorId = new ActorId(5);
        private static readonly ActorId PrototypeSelfDestructActorId = new ActorId(6);
        private static readonly ActorId PrototypeThrowerActorId = new ActorId(7);
        private static readonly IReadOnlyList<GridPosition> NoBossDangerCells =
            Array.AsReadOnly(Array.Empty<GridPosition>());
        private static readonly IReadOnlyList<GridPosition> NoSelfDestructTelegraphCells =
            Array.AsReadOnly(Array.Empty<GridPosition>());
        private static readonly IReadOnlyList<GridPosition> NoThrowerLockedTargets =
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
        private PrototypeSelfDestructDefinitionAsset selfDestructDefinition;

        [SerializeField]
        private PrototypeThrowerDefinitionAsset throwerDefinition;

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

        [SerializeField]
        private PrototypePauseView pauseViewPrefab;

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
        private SelfDestructEnemySimulation _selfDestruct;
        private EnemyHealthSimulation _selfDestructHealth;
        private SelfDestructEnemyDefinition _coreSelfDestructDefinition;
        private ThrowerEnemySimulation _thrower;
        private EnemyHealthSimulation _throwerHealth;
        private ThrowerEnemyDefinition _coreThrowerDefinition;
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
        private readonly List<PendingBossBombFlight> _pendingBossBombFlights =
            new List<PendingBossBombFlight>(4);
        private readonly List<ThrowerBombFlight> _pendingThrowerBombFlights =
            new List<ThrowerBombFlight>(3);
        private readonly HashSet<GridPosition> _bossReservedBombCells =
            new HashSet<GridPosition>();
        private bool _roomCleared;
        private bool _hasCharger;
        private bool _hasArmored;
        private bool _hasSelfDestruct;
        private bool _hasThrower;
        private bool _bossSummonedSelfDestruct;
        private TimeSpan _bossSelfDestructForceAt;
        private bool _isPaused;
        private FixedStepAccumulator _simulationAccumulator;
        private PrototypePausePresenter _pausePresenter;

        public event Action<PlayerMovementStep> PlayerMoved;

        public event Action InteractionRequested;

        public event Action<GridSubcellPosition, CardinalDirection> PlayerPositionChanged;

        public event Action<BombSnapshot> BombPlaced;

        public event Action<BombSnapshot> BossBombPlaced;

        public event Action<BossBombFlight> BossBombLaunched;

        public event Action<BombExplosion> BombExploded;

        public event Action<int> ActiveBombSlotChanged;

        public event Action<int> BombSlotEquipped;

        public event Action<PlayerDamageResult> PlayerDamaged;

        public event Action<PlayerDamageResult> PlayerDied;

        public event Action<PlayerHealthRecoveryResult> PlayerRecovered;

        public event Action<EnemyMovementStep> ChaserMoved;

        public event Action<ChargerEnemyAdvanceResult> ChargerAdvanced;

        public event Action<EnemyMovementStep> ArmoredMoved;

        public event Action<ArmoredEnemyAdvanceResult> ArmoredAdvanced;

        public event Action<ArmoredEnemyDamageResult> ArmoredStateChanged;

        public event Action<SelfDestructEnemyAdvanceResult> SelfDestructAdvanced;

        public event Action<ActorId> SelfDestructSpawned;

        public event Action<BombSnapshot> SelfDestructArmed;

        public event Action<ThrowerEnemyAdvanceResult> ThrowerAdvanced;

        public event Action<ThrowerBombFlight> ThrowerBombLaunched;

        public event Action<BombSnapshot> ThrowerBombPlaced;

        public event Action<EnemyMovementStep> BossMoved;

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

        public PrototypeSelfDestructDefinitionAsset SelfDestructDefinition =>
            selfDestructDefinition;

        public PrototypeThrowerDefinitionAsset ThrowerDefinition => throwerDefinition;

        public PrototypeBossDefinitionAsset BossDefinition => bossDefinition;

        public float CellsPerSecond => cellsPerSecond;

        public float ChainDelaySeconds => chainDelaySeconds;

        public bool IsCombatEnabledByDefault => combatEnabled;

        public bool IsBossEnabledByDefault => bossEnabled;

        public PrototypePauseView PauseViewPrefab => pauseViewPrefab;

        public bool HasChaser => IsCombatEnabledForVisit && !IsBossEnabledForVisit;

        public bool HasBoss => IsBossEnabledForVisit;

        public bool IsInitialized => _movement != null && _bombs != null && _weapons != null &&
            _health != null &&
            (!HasChaser || (_chaser != null && _chaserHealth != null)) &&
            (!_hasCharger || (_charger != null && _chargerHealth != null)) &&
            (!_hasArmored || _armored != null) &&
            (!_hasSelfDestruct || (_selfDestruct != null && _selfDestructHealth != null)) &&
            (!_hasThrower || (_thrower != null && _throwerHealth != null)) &&
            (!HasBoss || _boss != null);

        public bool IsReady { get; private set; }

        public bool IsPaused => _isPaused;

        public TimeSpan CurrentGameTime => _clock != null ? _clock.Now : TimeSpan.Zero;

        public GridPosition CurrentGridPosition =>
            _movement != null ? _movement.CurrentPosition : default;

        public GridSubcellPosition CurrentMovementPosition =>
            _movement != null ? _movement.Position : default;

        public CardinalDirection FacingDirection =>
            _movement != null ? _movement.FacingDirection : CardinalDirection.North;

        public bool IsPlayerMoving =>
            _movement != null && _movement.IsMoving;

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

        public ActorId PlayerActorId => PrototypePlayerActorId;

        public GridPosition CurrentChaserGridPosition =>
            _chaser != null ? _chaser.CurrentPosition : default;

        public GridSubcellPosition CurrentChaserMovementPosition =>
            _chaser != null ? _chaser.Position : default;

        public EnemyLocomotionState CurrentChaserLocomotionState =>
            IsChaserAlive ? _chaser.LocomotionState : EnemyLocomotionState.Idle;

        public EnemyMovementTransition CurrentChaserMovementTransition =>
            _chaser != null ? _chaser.MovementTransition : default;

        public bool IsChaserAlive => _chaserHealth != null && !_chaserHealth.IsDead;

        public bool HasCharger => _hasCharger;

        public ActorId ChargerActorId => _charger != null ? _charger.ActorId : default;

        public GridPosition CurrentChargerGridPosition =>
            _charger != null ? _charger.CurrentPosition : default;

        public GridSubcellPosition CurrentChargerMovementPosition =>
            _charger != null ? _charger.Position : default;

        public ChargerEnemyState CurrentChargerState =>
            _charger != null ? _charger.State : ChargerEnemyState.Track;

        public EnemyLocomotionState CurrentChargerLocomotionState =>
            IsChargerAlive ? _charger.LocomotionState : EnemyLocomotionState.Idle;

        public EnemyMovementTransition CurrentChargerMovementTransition =>
            _charger != null ? _charger.MovementTransition : default;

        public CardinalDirection CurrentChargerLockedDirection =>
            _charger != null ? _charger.LockedDirection : CardinalDirection.None;

        public int CurrentChargerLockedChargeDistance =>
            _charger != null ? _charger.LockedChargeDistance : 0;

        public bool IsChargerAlive =>
            _hasCharger && _chargerHealth != null && !_chargerHealth.IsDead;

        public bool HasArmored => _hasArmored;

        public ActorId ArmoredActorId => _armored != null ? _armored.ActorId : default;

        public GridPosition CurrentArmoredGridPosition =>
            _armored != null ? _armored.CurrentPosition : default;

        public ArmoredEnemyState CurrentArmoredState =>
            _armored != null ? _armored.State : ArmoredEnemyState.Armored;

        public ArmoredEnemyBehaviorState CurrentArmoredBehaviorState =>
            _armored != null
                ? _armored.BehaviorState
                : ArmoredEnemyBehaviorState.Guard;

        public CardinalDirection CurrentArmoredPanicDirection =>
            _armored != null ? _armored.PanicDirection : CardinalDirection.None;

        public int CurrentArmoredPanicPathCellCount =>
            _armored != null ? _armored.PanicPathCellCount : 0;

        public GridPosition CurrentArmoredPanicDestination =>
            _armored != null ? _armored.PanicDestination : default;

        public bool IsArmoredAlive => _hasArmored && _armored != null && !_armored.IsDead;

        public bool HasSelfDestruct => _hasSelfDestruct;

        public ActorId SelfDestructActorId =>
            _selfDestruct != null ? _selfDestruct.ActorId : default;

        public GridPosition CurrentSelfDestructGridPosition =>
            _selfDestruct != null ? _selfDestruct.CurrentPosition : default;

        public GridSubcellPosition CurrentSelfDestructMovementPosition =>
            _selfDestruct != null ? _selfDestruct.Position : default;

        public SelfDestructEnemyState CurrentSelfDestructState =>
            _selfDestruct != null
                ? _selfDestruct.State
                : SelfDestructEnemyState.Chase;

        public EnemyLocomotionState CurrentSelfDestructLocomotionState =>
            IsSelfDestructAlive
                ? _selfDestruct.LocomotionState
                : EnemyLocomotionState.Idle;

        public EnemyMovementTransition CurrentSelfDestructMovementTransition =>
            _selfDestruct != null ? _selfDestruct.MovementTransition : default;

        public float CurrentSelfDestructWarningProgress =>
            _selfDestruct != null ? (float)_selfDestruct.WarningProgress : 0f;

        public bool IsSelfDestructAlive =>
            _hasSelfDestruct && _selfDestructHealth != null && !_selfDestructHealth.IsDead;

        public IReadOnlyList<GridPosition> CurrentSelfDestructTelegraphCells =>
            _selfDestruct != null
                ? _selfDestruct.TelegraphCells
                : NoSelfDestructTelegraphCells;

        public bool HasThrower => _hasThrower;

        public ActorId ThrowerActorId => _thrower != null ? _thrower.ActorId : default;

        public GridPosition CurrentThrowerGridPosition =>
            _thrower != null ? _thrower.CurrentPosition : default;

        public GridSubcellPosition CurrentThrowerMovementPosition =>
            _thrower != null ? _thrower.Position : default;

        public ThrowerEnemyState CurrentThrowerState =>
            _thrower != null ? _thrower.State : ThrowerEnemyState.Track;

        public EnemyLocomotionState CurrentThrowerLocomotionState =>
            IsThrowerAlive ? _thrower.LocomotionState : EnemyLocomotionState.Idle;

        public EnemyMovementTransition CurrentThrowerMovementTransition =>
            _thrower != null ? _thrower.MovementTransition : default;

        public GridPosition CurrentThrowerLockedTarget =>
            _thrower != null ? _thrower.LockedTarget : default;

        public IReadOnlyList<GridPosition> CurrentThrowerLockedTargets =>
            _thrower != null ? _thrower.LockedTargets : NoThrowerLockedTargets;

        public bool IsThrowerAlive =>
            _hasThrower && _throwerHealth != null && !_throwerHealth.IsDead;

        public bool HasPendingThrowerBombFlight => _pendingThrowerBombFlights.Count > 0;

        public int PendingThrowerBombFlightCount => _pendingThrowerBombFlights.Count;

        public GridPosition GetCurrentArmoredPanicPathCell(int index)
        {
            if (_armored == null)
            {
                throw new InvalidOperationException("This session has no armored enemy.");
            }

            return _armored.GetPanicPathCell(index);
        }

        public ActorId BossActorId => _boss != null ? _boss.ActorId : default;

        public GridPosition CurrentBossGridPosition =>
            _boss != null ? _boss.BossPosition : default;

        public GridPosition NextBossGridPosition =>
            _boss != null ? _boss.NextBossPosition : default;

        public BossBattleState CurrentBossState =>
            _boss != null ? _boss.State : BossBattleState.Telegraph;

        public BossPhase CurrentBossPhase =>
            _boss != null ? _boss.Phase : BossPhase.One;

        public BossPatternKind CurrentBossPattern =>
            _boss != null ? _boss.CurrentPattern : BossPatternKind.LimitedChase;

        public IReadOnlyList<GridPosition> CurrentBossDangerCells =>
            _boss != null ? _boss.CurrentDangerCells : NoBossDangerCells;

        public int CurrentBossHealth => _boss != null ? _boss.CurrentHealth : 0;

        public int MaxBossHealth => _boss != null ? _boss.MaxHealth : 0;

        public bool IsBossAlive => HasBoss && _boss != null && !_boss.IsDead;

        public int PendingBossBombFlightCount => _pendingBossBombFlights.Count;

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
                if (_selfDestructHealth != null && !_selfDestructHealth.IsDead)
                {
                    count++;
                }
                if (_throwerHealth != null && !_throwerHealth.IsDead)
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
            bool startingBossEnabled = false,
            PrototypeSelfDestructDefinitionAsset startingSelfDestruct = null,
            PrototypeThrowerDefinitionAsset startingThrower = null)
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
            selfDestructDefinition = startingSelfDestruct;
            throwerDefinition = startingThrower;
            bossDefinition = startingBoss;
            cellsPerSecond = movementCellsPerSecond;
            chainDelaySeconds = bombChainDelaySeconds;
            combatEnabled = startingCombatEnabled;
            bossEnabled = startingBossEnabled;
            _runtimeCombatEnabled = null;
            _runtimeBossEnabled = null;
            _runtimeInitialPlayerHealth = null;
        }

        public void BindPauseViewPrefab(PrototypePauseView authoredViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeGameSession before changing its pause view prefab.");
            }

            pauseViewPrefab = authoredViewPrefab ??
                throw new ArgumentNullException(nameof(authoredViewPrefab));
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
                   playerStart == roomDefinition.ArmoredSpawn.Value) ||
                   (roomDefinition.SelfDestructSpawn.HasValue &&
                    playerStart == roomDefinition.SelfDestructSpawn.Value) ||
                   (roomDefinition.ThrowerSpawn.HasValue &&
                    playerStart == roomDefinition.ThrowerSpawn.Value)))
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

        public bool TryRegisterInteractable(GridPosition position)
        {
            return _grid != null && _grid.TryAddInteractable(position);
        }

        public bool TryUnregisterInteractable(GridPosition position)
        {
            return _grid != null && _grid.TryRemoveInteractable(position);
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
            if (bossDefinition != null)
            {
                if (bossDefinition.ThrowBombDefinition != null &&
                    bossDefinition.ThrowBombDefinition.DefinitionId == definitionId.Value)
                {
                    return bossDefinition.ThrowBombDefinition;
                }
                if (bossDefinition.ChainBombDefinition != null &&
                    bossDefinition.ChainBombDefinition.DefinitionId == definitionId.Value)
                {
                    return bossDefinition.ChainBombDefinition;
                }
            }
            if (selfDestructDefinition != null &&
                selfDestructDefinition.DetonationBombDefinition != null &&
                selfDestructDefinition.DetonationBombDefinition.DefinitionId ==
                definitionId.Value)
            {
                return selfDestructDefinition.DetonationBombDefinition;
            }
            if (throwerDefinition != null &&
                throwerDefinition.BombDefinition != null &&
                throwerDefinition.BombDefinition.DefinitionId == definitionId.Value)
            {
                return throwerDefinition.BombDefinition;
            }
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
            if (_bossReservedBombCells.Contains(_movement.CurrentPosition))
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

        public PlayerHealthRecoveryResult ApplyPlayerRecovery(int requestedHealth)
        {
            if (!IsReady || _health == null)
            {
                throw new InvalidOperationException(
                    "Player recovery requires a ready game session.");
            }

            PlayerHealthRecoveryResult result =
                _health.ApplyRecovery(requestedHealth);
            if (result.WasApplied)
            {
                PlayerRecovered?.Invoke(result);
            }
            return result;
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
                _movement.ClearMoveIntent();
            }
        }

        private void Update()
        {
            if (_isPaused)
            {
                return;
            }

            float elapsedSeconds = Time.deltaTime;
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                throw new InvalidOperationException("Unity supplied an invalid simulation delta time.");
            }

            int simulationStepCount = _simulationAccumulator.AddElapsed(
                TimeSpan.FromSeconds(elapsedSeconds),
                MaxSimulationStepsPerFrame);
            if (simulationStepCount == 0)
            {
                return;
            }

            inputReader.RefreshMoveIntent();
            for (int stepIndex = 0; stepIndex < simulationStepCount; stepIndex++)
            {
                AdvanceSimulationStep();
            }
        }

        private void AdvanceSimulationStep()
        {
            _clock.Advance(SimulationStep);
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
                        _movement.LastAdvanceDirection);
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
            if (_hasArmored && !_armored.IsDead)
            {
                ArmoredEnemyAdvanceResult armoredAdvance = _armored.Advance();
                if (armoredAdvance.HasActivity)
                {
                    ArmoredAdvanced?.Invoke(armoredAdvance);
                }
                if (armoredAdvance.HasMovement)
                {
                    ArmoredMoved?.Invoke(armoredAdvance.Movement);
                }
            }
            if (_hasSelfDestruct && !_selfDestructHealth.IsDead)
            {
                SelfDestructEnemyAdvanceResult selfDestructAdvance = default;
                bool forceBossSummon = _bossSummonedSelfDestruct &&
                    _clock.Now >= _bossSelfDestructForceAt &&
                    (_boss == null || !_boss.IsHeavyAttackActive) &&
                    _selfDestruct.TryForceTrigger(out selfDestructAdvance);
                if (!forceBossSummon)
                {
                    selfDestructAdvance = _bossSummonedSelfDestruct &&
                        _boss != null &&
                        _boss.IsHeavyAttackActive
                            ? default
                            : _selfDestruct.Advance();
                }
                if (selfDestructAdvance.ShouldArm)
                {
                    ArmSelfDestruct();
                }
                if (selfDestructAdvance.HasActivity)
                {
                    SelfDestructAdvanced?.Invoke(selfDestructAdvance);
                }
            }
            if (_hasThrower && !_throwerHealth.IsDead)
            {
                ThrowerEnemyAdvanceResult throwerAdvance = _thrower.Advance();
                if (throwerAdvance.ShouldLaunch)
                {
                    BeginThrowerBombFlights(throwerAdvance.LockedTargets);
                }
                if (throwerAdvance.HasActivity)
                {
                    ThrowerAdvanced?.Invoke(throwerAdvance);
                }
            }

            BossPatternTransition? bossTransition = null;
            if (HasBoss && !_boss.IsDead &&
                _boss.TryAdvance(out BossPatternTransition transition))
            {
                bossTransition = transition;
                if (transition.BeganTelegraph && transition.AttackPlan.HasPlacements)
                {
                    ReserveBossAttack(transition.AttackPlan);
                }
                if (transition.AttackResolved)
                {
                    if (transition.AttackPlan.HasPlacements)
                    {
                        BeginBossAttackFlights(transition.AttackPlan);
                    }
                    if (transition.Pattern == BossPatternKind.SummonSelfDestruct)
                    {
                        SpawnBossSelfDestruct();
                    }
                    ApplyBossPatternDamage(transition);
                }
            }

            ProcessBossBombFlights();
            ProcessThrowerBombFlights();

            var explosions = _bombs.ProcessDueBombs();
            if (bossTransition.HasValue && bossTransition.Value.BossMoved)
            {
                IReadOnlyList<EnemyMovementStep> bossMovements =
                    bossTransition.Value.Movements;
                for (int index = 0; index < bossMovements.Count; index++)
                {
                    BossMoved?.Invoke(bossMovements[index]);
                }
            }
            for (int index = 0; index < explosions.Count; index++)
            {
                BombExplosion explosion = explosions[index];
                _movement.NotifyBombRemoved(explosion.BombId);
                if (!_health.IsDead)
                {
                    GridPosition playerExplosionCell =
                        _movement.GetCurrentCellAt(explosion.DetonatedAt);
                    if (Contains(explosion.AffectedCells, playerExplosionCell))
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
                if (HasChaser && !_chaserHealth.IsDead)
                {
                    ApplyEnemyExplosionDamage(
                        explosion,
                        _chaser.ActorId,
                        _chaser.GetCurrentCellAt(explosion.DetonatedAt),
                        _chaserHealth,
                        "chaser");
                }
                if (_hasCharger && !_chargerHealth.IsDead)
                {
                    ApplyEnemyExplosionDamage(
                        explosion,
                        _charger.ActorId,
                        _charger.GetCurrentCellAt(explosion.DetonatedAt),
                        _chargerHealth,
                        "charger");
                }
                if (_hasArmored)
                {
                    ApplyArmoredExplosionDamage(explosion);
                }
                if (_hasSelfDestruct)
                {
                    ApplySelfDestructExplosion(explosion);
                }
                if (_hasThrower)
                {
                    if (_thrower.IsActiveBomb(explosion.BombId))
                    {
                        _thrower.NotifyBombResolved(explosion.BombId);
                    }
                    if (explosion.OwnerId != _thrower.ActorId && !_throwerHealth.IsDead)
                    {
                        ApplyEnemyExplosionDamage(
                            explosion,
                            _thrower.ActorId,
                            _thrower.GetCurrentCellAt(explosion.DetonatedAt),
                            _throwerHealth,
                            "thrower");
                    }
                }
                if (HasBoss)
                {
                    ApplyBossExplosionDamage(explosion);
                }
            }

            if (!_health.IsDead && HasChaser && !_chaserHealth.IsDead &&
                _chaser.CanDealContactDamage)
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
                    _movement.CancelMovement();
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
            _hasSelfDestruct = HasChaser && roomDefinition.SelfDestructSpawn.HasValue;
            _hasThrower = HasChaser && roomDefinition.ThrowerSpawn.HasValue;
            bool bossHasSelfDestructSummon = bossEnabledForVisit &&
                roomDefinition.SelfDestructSpawn.HasValue &&
                roomDefinition.SelfDestructAnchors.Count >= 2;
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
            if ((_hasSelfDestruct || bossHasSelfDestructSummon) &&
                selfDestructDefinition == null)
            {
                throw new InvalidOperationException(
                    "A room with a self-destruct spawn requires a self-destruct definition reference.");
            }
            if (_hasThrower && throwerDefinition == null)
            {
                throw new InvalidOperationException(
                    "A room with a thrower spawn requires a thrower definition reference.");
            }

            _grid = CreateGrid(roomDefinition);
            _clock = new ManualGameClock();
            _simulationAccumulator = new FixedStepAccumulator(SimulationStep);
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
            if (_hasSelfDestruct)
            {
                if (context.SelfDestructSpawn == null)
                {
                    throw new InvalidOperationException(
                        "A room with a self-destruct enemy requires a self-destruct spawn Transform.");
                }

                _coreSelfDestructDefinition = selfDestructDefinition.CreateCoreDefinition();
                _selfDestruct = new SelfDestructEnemySimulation(
                    _grid,
                    _clock,
                    _coreSelfDestructDefinition,
                    PrototypeSelfDestructActorId,
                    _movement.ActorId,
                    roomDefinition.SelfDestructSpawn.Value);
                _selfDestructHealth = new EnemyHealthSimulation(
                    _selfDestruct.ActorId,
                    1);
            }
            else if (bossHasSelfDestructSummon)
            {
                _coreSelfDestructDefinition = selfDestructDefinition.CreateCoreDefinition();
            }
            if (_hasThrower)
            {
                if (context.ThrowerSpawn == null)
                {
                    throw new InvalidOperationException(
                        "A room with a thrower requires a thrower spawn Transform.");
                }

                _coreThrowerDefinition = throwerDefinition.CreateCoreDefinition();
                _thrower = new ThrowerEnemySimulation(
                    _grid,
                    _clock,
                    _coreThrowerDefinition,
                    PrototypeThrowerActorId,
                    _movement.ActorId,
                    roomDefinition.ThrowerSpawn.Value,
                    roomDefinition.ThrowerFiringAnchors,
                    roomDefinition.ThrowerTargetAnchors);
                _throwerHealth = new EnemyHealthSimulation(
                    _thrower.ActorId,
                    _coreThrowerDefinition.MaxHealth);
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
                    _movement.ActorId,
                    bossDefinition.BossSpawn,
                    CreatePlayableArenaCells(roomDefinition),
                    roomDefinition.RetreatAnchors,
                    roomDefinition.SelfDestructAnchors);
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
                if (_isPaused &&
                    _pausePresenter != null &&
                    _pausePresenter.TryHandlePauseCommand())
                {
                    return;
                }
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
                case PlayerCommandKind.Interact:
                    InteractionRequested?.Invoke();
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
            _movement.ClearMoveIntent();
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

        public void ResumeFromPause()
        {
            if (_isPaused)
            {
                TogglePause();
            }
        }

        private void EnsurePausePresenter()
        {
            if (pauseViewPrefab == null ||
                !pauseViewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeGameSession requires a configured pause view prefab.");
            }

            _pausePresenter = GetComponent<PrototypePausePresenter>();
            if (_pausePresenter == null)
            {
                _pausePresenter = gameObject.AddComponent<PrototypePausePresenter>();
            }
            _pausePresenter.Configure(this, pauseViewPrefab);
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
            GridPosition position,
            EnemyHealthSimulation enemyHealth,
            string enemyLabel)
        {
            if (enemyHealth.IsDead ||
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

            ArmoredEnemyDamageResult result = _armored.ApplyExplosion(
                explosion.BombId,
                explosion.Origin);
            if (!result.Damage.WasApplied)
            {
                return;
            }

            _armoredDamageResults.Add(result);
            _appliedEnemyDamageResults.Add(result.Damage);
        }

        private void ApplySelfDestructExplosion(BombExplosion explosion)
        {
            if (_selfDestructHealth.IsDead)
            {
                return;
            }
            if (_selfDestruct.IsArmed && explosion.BombId == _selfDestruct.ArmedBombId)
            {
                SelfDestructEnemyAdvanceResult result =
                    _selfDestruct.CompleteDetonation(explosion.BombId);
                EnemyDamageResult damage = _selfDestructHealth.ApplyExplosionDamage(
                    explosion.BombId,
                    DefaultEnemyExplosionDamage);
                if (!damage.WasApplied || !damage.WasFatal)
                {
                    throw new InvalidOperationException(
                        "A self-destruct enemy must die when its armed bomb detonates.");
                }

                _appliedEnemyDamageResults.Add(damage);
                if (!_grid.TryRemoveActor(_selfDestruct.ActorId))
                {
                    throw new InvalidOperationException(
                        "Detonated self-destruct enemy could not be removed from the logical grid.");
                }

                if (_bossSummonedSelfDestruct && _boss != null && !_boss.IsDead)
                {
                    _boss.NotifySelfDestructResolved();
                }

                SelfDestructAdvanced?.Invoke(result);
                return;
            }
            GridPosition position =
                _selfDestruct.GetCurrentCellAt(explosion.DetonatedAt);
            if ((_selfDestruct.State != SelfDestructEnemyState.Chase &&
                    _selfDestruct.State != SelfDestructEnemyState.WarningChase) ||
                !_grid.TryGetActorPosition(_selfDestruct.ActorId, out _) ||
                !Contains(explosion.AffectedCells, position) ||
                !_selfDestruct.TryTriggerFromExplosion(
                    explosion.BombId,
                    out SelfDestructEnemyAdvanceResult triggerResult))
            {
                return;
            }

            ArmSelfDestruct();
            SelfDestructAdvanced?.Invoke(triggerResult);
        }

        private void ArmSelfDestruct()
        {
            if (!_bombs.TryPlaceBomb(
                _coreSelfDestructDefinition.DetonationBombDefinition,
                _selfDestruct.CurrentPosition,
                _selfDestruct.ActorId,
                out BombId bombId))
            {
                throw new InvalidOperationException(
                    "Self-destruct enemy could not arm its logical bomb.");
            }

            _selfDestruct.ConfirmArmed(bombId);
            if (!_bombs.TryGetBomb(bombId, out BombSnapshot snapshot))
            {
                throw new InvalidOperationException(
                    "Armed self-destruct bomb could not be read from the simulation.");
            }

            SelfDestructArmed?.Invoke(snapshot);
        }

        private void ApplyBossExplosionDamage(BombExplosion explosion)
        {
            if (_boss.IsDead || explosion.OwnerId == _boss.ActorId ||
                !Contains(explosion.AffectedCells, _boss.BossPosition))
            {
                return;
            }

            BossDamageResult result = _bossSummonedSelfDestruct &&
                _selfDestruct != null &&
                explosion.OwnerId == _selfDestruct.ActorId
                    ? _boss.ApplySelfDestructExplosion(
                        explosion.BombId,
                        DefaultEnemyExplosionDamage)
                    : _boss.ApplyExplosion(
                        explosion.BombId,
                        DefaultEnemyExplosionDamage);
            if (result.WasApplied)
            {
                _bossDamageResults.Add(result);
            }
        }

        private void ReserveBossAttack(BossBombAttackPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            for (int index = 0; index < plan.Placements.Count; index++)
            {
                BossBombPlacement placement = plan.Placements[index];
                if (!_bossReservedBombCells.Add(placement.Position))
                {
                    throw new InvalidOperationException(
                        $"Boss attack reserved duplicate cell {placement.Position}.");
                }
            }
        }

        private void BeginBossAttackFlights(BossBombAttackPlan plan)
        {
            for (int index = 0; index < plan.Placements.Count; index++)
            {
                BossBombPlacement placement = plan.Placements[index];
                TimeSpan launchesAt = _clock.Now.Add(placement.LaunchOffset);
                TimeSpan landsAt = launchesAt.Add(placement.FlightDuration);
                _pendingBossBombFlights.Add(new PendingBossBombFlight(
                    new BossBombFlight(
                        index,
                        placement.Definition,
                        _boss.BossPosition,
                        placement.Position,
                        launchesAt,
                        landsAt)));
            }
        }

        private void ProcessBossBombFlights()
        {
            int index = 0;
            while (index < _pendingBossBombFlights.Count)
            {
                PendingBossBombFlight pending = _pendingBossBombFlights[index];
                if (!pending.WasLaunched && _clock.Now >= pending.Flight.LaunchedAt)
                {
                    pending.WasLaunched = true;
                    BossBombLaunched?.Invoke(pending.Flight);
                }
                if (_clock.Now < pending.Flight.LandsAt)
                {
                    index++;
                    continue;
                }

                if (!_bombs.TryPlaceBomb(
                        pending.Flight.Definition,
                        pending.Flight.Target,
                        _boss.ActorId,
                        out BombId bombId) ||
                    !_bombs.TryGetBomb(bombId, out BombSnapshot snapshot))
                {
                    throw new InvalidOperationException(
                        $"Boss bomb could not land at {pending.Flight.Target}.");
                }

                _bossReservedBombCells.Remove(pending.Flight.Target);
                BossBombPlaced?.Invoke(snapshot);
                _pendingBossBombFlights.RemoveAt(index);
            }
        }

        private void BeginThrowerBombFlights(
            IReadOnlyList<GridPosition> targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }
            if (targets.Count != _coreThrowerDefinition.BombsPerVolley)
            {
                throw new InvalidOperationException(
                    "Thrower launch targets must match its configured volley size.");
            }
            if (_pendingThrowerBombFlights.Count > 0)
            {
                throw new InvalidOperationException(
                    "A thrower cannot launch another volley while bombs are in flight.");
            }

            TimeSpan launchedAt = _clock.Now;
            TimeSpan landsAt = launchedAt.Add(_coreThrowerDefinition.FlightDuration);
            for (int index = 0; index < targets.Count; index++)
            {
                var flight = new ThrowerBombFlight(
                    _thrower.ActorId,
                    _coreThrowerDefinition.BombDefinition,
                    _thrower.CurrentPosition,
                    targets[index],
                    launchedAt,
                    landsAt);
                _pendingThrowerBombFlights.Add(flight);
                ThrowerBombLaunched?.Invoke(flight);
            }
        }

        private void ProcessThrowerBombFlights()
        {
            int index = 0;
            while (index < _pendingThrowerBombFlights.Count)
            {
                ThrowerBombFlight flight = _pendingThrowerBombFlights[index];
                if (_clock.Now < flight.LandsAt)
                {
                    index++;
                    continue;
                }

                _pendingThrowerBombFlights.RemoveAt(index);
                if (!_bombs.TryPlaceBomb(
                        flight.Definition,
                        flight.Target,
                        flight.OwnerId,
                        out BombId bombId) ||
                    !_bombs.TryGetBomb(bombId, out BombSnapshot snapshot))
                {
                    _thrower.NotifyLaunchFailed();
                    continue;
                }

                _thrower.ConfirmBombPlaced(bombId);
                ThrowerBombPlaced?.Invoke(snapshot);
            }
        }

        private void SpawnBossSelfDestruct()
        {
            if (_bossSummonedSelfDestruct || _coreSelfDestructDefinition == null)
            {
                return;
            }

            CombatRoomDefinition room = _runtimeRoomDefinition ??
                context.RoomDefinition.CreateCoreDefinition();
            GridPosition spawn = _boss.CurrentDangerCells.Count == 1
                ? _boss.CurrentDangerCells[0]
                : SelectBossSelfDestructSpawn(room.SelfDestructAnchors);
            _selfDestruct = new SelfDestructEnemySimulation(
                _grid,
                _clock,
                _coreSelfDestructDefinition,
                PrototypeSelfDestructActorId,
                _movement.ActorId,
                spawn);
            _selfDestructHealth = new EnemyHealthSimulation(_selfDestruct.ActorId, 1);
            _hasSelfDestruct = true;
            _bossSummonedSelfDestruct = true;
            _bossSelfDestructForceAt = _clock.Now.Add(
                TimeSpan.FromSeconds(bossDefinition.SelfDestructForceSeconds));
            SelfDestructSpawned?.Invoke(_selfDestruct.ActorId);
        }

        private GridPosition SelectBossSelfDestructSpawn(
            IReadOnlyList<GridPosition> candidates)
        {
            GridPosition selected = default;
            long selectedDistance = long.MinValue;
            for (int index = 0; index < candidates.Count; index++)
            {
                GridPosition candidate = candidates[index];
                GridCellState cell = _grid.GetCell(candidate);
                if (!cell.IsWalkableTerrain || cell.HasActor || cell.HasBomb)
                {
                    continue;
                }
                long distance = Math.Abs((long)candidate.X - _movement.CurrentPosition.X) +
                    Math.Abs((long)candidate.Z - _movement.CurrentPosition.Z);
                if (distance > selectedDistance)
                {
                    selected = candidate;
                    selectedDistance = distance;
                }
            }
            if (selectedDistance == long.MinValue)
            {
                throw new InvalidOperationException(
                    "Boss has no available authored self-destruct summon anchor.");
            }
            return selected;
        }

        private void ApplyBossPatternDamage(BossPatternTransition transition)
        {
            if (_health.IsDead ||
                (transition.Pattern != BossPatternKind.FixedCharge &&
                 transition.Pattern != BossPatternKind.ParityWave) ||
                !Contains(transition.DangerCells, _movement.CurrentPosition))
            {
                return;
            }

            PlayerDamageResult damage = _health.ApplyBossPatternDamage(
                _boss.ActorId,
                _boss.Definition.PatternDamage);
            if (damage.WasApplied)
            {
                _appliedDamageResults.Add(damage);
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

        private GridState CreateGrid(CombatRoomDefinition roomDefinition)
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
