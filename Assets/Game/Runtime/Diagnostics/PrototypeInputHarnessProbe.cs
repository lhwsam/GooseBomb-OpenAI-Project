#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
using System.Collections.Generic;
#endif
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BombSwapInputReader))]
    [RequireComponent(typeof(PrototypeGameSession))]
    public sealed class PrototypeInputHarnessProbe : MonoBehaviour
    {
        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private PrototypeGameSession session;

        private bool _audioUnlockReported;
        private bool _moveReported;
        private bool _contactEscapeMovedReported;
        private bool _placeBombReported;
        private bool _destructibleWallDestroyedReported;
        private bool _playerDamagedReported;
        private bool _playerExplosionDamagedReported;
        private bool _playerContactDamagedReported;
        private bool _playerBossPatternDamagedReported;
        private bool _chaserMovedReported;
        private bool _chargerTelegraphReported;
        private bool _chargerChargeReported;
        private bool _chargerMovedReported;
        private bool _chargerTrackMovedReported;
        private bool _chargerChargeMovedReported;
        private bool _chargerRecoverReported;
        private bool _armoredMovedReported;
        private bool _armoredBrokenReported;
        private bool _armoredPanicTelegraphReported;
        private bool _armoredPanicRunReported;
        private bool _armoredPanicRecoverReported;
        private bool _armoredChaseReported;
        private bool _armoredDiedReported;
        private bool _selfDestructMovedReported;
        private bool _selfDestructWarningReported;
        private bool _selfDestructTelegraphReported;
        private bool _selfDestructArmedReported;
        private bool _selfDestructDetonatedReported;
        private bool _selfDestructDiedReported;
        private bool _throwerMovedReported;
        private bool _throwerDiedReported;
        private bool _enemyDiedReported;
        private bool _bossPhaseTwoReported;
        private bool _bossLastStandReported;
        private bool _bossDefeatedReported;
        private bool _roomClearedReported;
        private bool _swapBombReported;
        private bool _pauseReported;
        private bool _pauseEnteredReported;
        private bool _readyReported;
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
        private readonly Dictionary<BombId, string> _bombDefinitionsById =
            new Dictionary<BombId, string>();
        private BossBattleState _lastBossState;
        private BossPatternKind _lastBossPattern;
#endif
        private CardinalDirection _lastMotionDirection;

        public BombSwapInputReader InputReader => inputReader;

        public PrototypeGameSession Session => session;

        public void Configure(BombSwapInputReader reader, PrototypeGameSession gameSession)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new System.InvalidOperationException(
                    "Disable PrototypeInputHarnessProbe before changing its runtime configuration.");
            }

            inputReader = reader;
            session = gameSession;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<BombSwapInputReader>();
            }
            if (session == null)
            {
                session = GetComponent<PrototypeGameSession>();
            }

            if (inputReader == null || session == null)
            {
                Debug.LogError(
                    "PrototypeInputHarnessProbe requires input and game-session components.",
                    this);
                enabled = false;
                return;
            }

            inputReader.CommandIssued += OnCommandIssued;
            session.PlayerMoved += OnPlayerMoved;
            session.PlayerPositionChanged += OnPlayerPositionChanged;
            session.BombPlaced += OnBombPlaced;
            session.BossBombLaunched += OnBossBombLaunched;
            session.BossBombPlaced += OnBossBombPlaced;
            session.BombExploded += OnBombExploded;
            session.ActiveBombSlotChanged += OnActiveBombSlotChanged;
            session.PlayerDamaged += OnPlayerDamaged;
            session.PlayerDied += OnPlayerDied;
            session.PlayerRecovered += OnPlayerRecovered;
            session.ChaserMoved += OnChaserMoved;
            session.ChargerAdvanced += OnChargerAdvanced;
            session.ArmoredAdvanced += OnArmoredAdvanced;
            session.ArmoredMoved += OnArmoredMoved;
            session.ArmoredStateChanged += OnArmoredStateChanged;
            session.SelfDestructAdvanced += OnSelfDestructAdvanced;
            session.SelfDestructArmed += OnSelfDestructArmed;
            session.SelfDestructSpawned += OnSelfDestructSpawned;
            session.ThrowerAdvanced += OnThrowerAdvanced;
            session.ThrowerBombLaunched += OnThrowerBombLaunched;
            session.ThrowerBombPlaced += OnThrowerBombPlaced;
            session.EnemyDied += OnEnemyDied;
            session.BossMoved += OnBossMoved;
            session.BossPatternTransitioned += OnBossPatternTransitioned;
            session.BossDamaged += OnBossDamaged;
            session.RoomCleared += OnRoomCleared;
            session.PauseStateChanged += OnPauseStateChanged;
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                ReportReady();
            }
        }

        private void OnDisable()
        {
            _readyReported = false;
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _bombDefinitionsById.Clear();
#endif
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
            if (session != null)
            {
                session.PlayerMoved -= OnPlayerMoved;
                session.PlayerPositionChanged -= OnPlayerPositionChanged;
                session.BombPlaced -= OnBombPlaced;
                session.BossBombLaunched -= OnBossBombLaunched;
                session.BossBombPlaced -= OnBossBombPlaced;
                session.BombExploded -= OnBombExploded;
                session.ActiveBombSlotChanged -= OnActiveBombSlotChanged;
                session.PlayerDamaged -= OnPlayerDamaged;
                session.PlayerDied -= OnPlayerDied;
                session.PlayerRecovered -= OnPlayerRecovered;
                session.ChaserMoved -= OnChaserMoved;
                session.ChargerAdvanced -= OnChargerAdvanced;
                session.ArmoredAdvanced -= OnArmoredAdvanced;
                session.ArmoredMoved -= OnArmoredMoved;
                session.ArmoredStateChanged -= OnArmoredStateChanged;
                session.SelfDestructAdvanced -= OnSelfDestructAdvanced;
                session.SelfDestructArmed -= OnSelfDestructArmed;
                session.SelfDestructSpawned -= OnSelfDestructSpawned;
                session.ThrowerAdvanced -= OnThrowerAdvanced;
                session.ThrowerBombLaunched -= OnThrowerBombLaunched;
                session.ThrowerBombPlaced -= OnThrowerBombPlaced;
                session.EnemyDied -= OnEnemyDied;
                session.BossMoved -= OnBossMoved;
                session.BossPatternTransitioned -= OnBossPatternTransitioned;
                session.BossDamaged -= OnBossDamaged;
                session.RoomCleared -= OnRoomCleared;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }
        }

        private void OnSessionReady()
        {
            ReportReady();
        }

        private void ReportReady()
        {
            if (_readyReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("probe-ready");
            PrototypeCombatRoomDefinitionAsset room = session.Context.RoomDefinition;
            if (room != null)
            {
                WebGlHarnessReporter.Report("room-ready-" + room.RoomId);
            }
            if (session.HasBoss)
            {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
                _lastBossState = session.CurrentBossState;
                _lastBossPattern = session.CurrentBossPattern;
#endif
            }
            if (session.HasBoss &&
                session.CurrentBossState == BossBattleState.Telegraph)
            {
                WebGlHarnessReporter.Report("boss-pattern-telegraph");
                ReportBossPattern(
                    session.CurrentBossPhase,
                    session.CurrentBossPattern,
                    session.CurrentBossState,
                    session.CurrentBossDangerCells);
                WebGlHarnessReporter.ReportBossCell(
                    session.CurrentBossGridPosition);
                WebGlHarnessReporter.ReportBossMoveTarget(
                    session.NextBossGridPosition);
            }
            ReportPlayerHealth(session.CurrentHealth);
            WebGlHarnessReporter.ReportPlayerCell(session.CurrentGridPosition);
            if (session.HasSelfDestruct)
            {
                ReportSelfDestructCell(session.CurrentSelfDestructGridPosition);
                if (session.CurrentSelfDestructState ==
                    SelfDestructEnemyState.WarningChase)
                {
                    ReportSelfDestructWarning();
                }
            }
            if (session.HasThrower)
            {
                ReportThrowerCell(session.CurrentThrowerGridPosition);
            }
            _readyReported = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            ReportMovementStepDirection(step.Direction);
            WebGlHarnessReporter.ReportPlayerCell(step.To);

            if (!_moveReported)
            {
                WebGlHarnessReporter.Report("move");
                _moveReported = true;
            }

            if (_playerContactDamagedReported && !_contactEscapeMovedReported)
            {
                WebGlHarnessReporter.Report("contact-escape-moved");
                _contactEscapeMovedReported = true;
            }
        }

        private void OnPlayerPositionChanged(
            GridSubcellPosition _,
            CardinalDirection direction)
        {
            if (direction == _lastMotionDirection)
            {
                return;
            }

            _lastMotionDirection = direction;
            ReportMotionDirection(direction);
        }

        private void OnBombPlaced(BombSnapshot snapshot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _bombDefinitionsById[snapshot.Id] = snapshot.DefinitionId.Value;
#endif
            WebGlHarnessReporter.Report("place-bomb-definition-" + snapshot.DefinitionId.Value);
            if (snapshot.DefinitionId.Value == "prototype-line")
            {
                ReportLineBombDirection("line-bomb-placed", snapshot.PlacementDirection);
            }
            if (_placeBombReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("place-bomb");
            _placeBombReported = true;
        }

        private static void OnActiveBombSlotChanged(int slotIndex)
        {
            WebGlHarnessReporter.Report("active-bomb-slot-" + slotIndex);
        }

        private void OnBombExploded(BombExplosion explosion)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _bombDefinitionsById[explosion.BombId] = explosion.DefinitionId.Value;
#endif
            WebGlHarnessReporter.Report(
                "bomb-exploded-definition-" + explosion.DefinitionId.Value);
            if (explosion.DefinitionId.Value == "prototype-line")
            {
                ReportLineBombDirection("line-bomb-exploded", explosion.PlacementDirection);
            }
            if (explosion.DefinitionId.Value == "prototype-boss-chain" &&
                explosion.Cause == BombDetonationCause.Chain)
            {
                WebGlHarnessReporter.Report("boss-chain-bomb-detonated-by-chain");
            }
            if (explosion.DefinitionId.Value == "prototype-thrower-blocker")
            {
                WebGlHarnessReporter.Report("thrower-bomb-detonated");
                if (explosion.Cause == BombDetonationCause.Chain)
                {
                    WebGlHarnessReporter.Report(
                        "thrower-bomb-detonated-by-chain");
                }
            }
            if (!_destructibleWallDestroyedReported && explosion.DestroyedWalls.Count > 0)
            {
                WebGlHarnessReporter.Report("destructible-wall-destroyed");
                _destructibleWallDestroyedReported = true;
            }

            WebGlHarnessReporter.Report("bomb-exploded");
        }

        private void OnBossBombPlaced(BombSnapshot snapshot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _bombDefinitionsById[snapshot.Id] = snapshot.DefinitionId.Value;
#endif
            WebGlHarnessReporter.Report(
                "boss-bomb-armed-definition-" + snapshot.DefinitionId.Value);
        }

        private static void OnBossBombLaunched(BossBombFlight flight)
        {
            WebGlHarnessReporter.Report(
                "boss-bomb-launched-definition-" + flight.Definition.Id.Value);
        }

        private static void ReportLineBombDirection(
            string prefix,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    WebGlHarnessReporter.Report(prefix + "-north");
                    break;
                case CardinalDirection.East:
                    WebGlHarnessReporter.Report(prefix + "-east");
                    break;
                case CardinalDirection.South:
                    WebGlHarnessReporter.Report(prefix + "-south");
                    break;
                case CardinalDirection.West:
                    WebGlHarnessReporter.Report(prefix + "-west");
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "A line-bomb marker requires a cardinal direction.");
            }
        }

        private void OnPlayerDamaged(PlayerDamageResult result)
        {
            ReportPlayerHealth(result.CurrentHealth);
            if (!_playerDamagedReported)
            {
                WebGlHarnessReporter.Report("player-damaged");
                _playerDamagedReported = true;
            }

            switch (result.SourceKind)
            {
                case PlayerDamageSourceKind.Explosion:
                    if (!_playerExplosionDamagedReported)
                    {
                        WebGlHarnessReporter.Report("player-explosion-damaged");
                        _playerExplosionDamagedReported = true;
                    }
                    break;
                case PlayerDamageSourceKind.EnemyContact:
                    if (!_playerContactDamagedReported)
                    {
                        WebGlHarnessReporter.Report("player-contact-damaged");
                        _playerContactDamagedReported = true;
                    }
                    break;
                case PlayerDamageSourceKind.BossPattern:
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
                    WebGlHarnessReporter.Report(
                        "boss-player-damaged-phase-" +
                        GetBossPhaseName(session.CurrentBossPhase) +
                        "-pattern-" + GetBossPatternName(_lastBossPattern) +
                        "-health-" + result.CurrentHealth);
#endif
                    if (!_playerBossPatternDamagedReported)
                    {
                        WebGlHarnessReporter.Report("player-boss-pattern-damaged");
                        _playerBossPatternDamagedReported = true;
                    }
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(result),
                        result.SourceKind,
                        "Unsupported player damage source kind.");
            }
        }

        private void OnPlayerDied(PlayerDamageResult _)
        {
            WebGlHarnessReporter.Report("player-died");
        }

        private void OnPlayerRecovered(PlayerHealthRecoveryResult result)
        {
            ReportPlayerHealth(result.CurrentHealth);
        }

        private static void ReportPlayerHealth(int currentHealth)
        {
            WebGlHarnessReporter.Report("player-health-current-" + currentHealth);
        }

        private void OnChaserMoved(EnemyMovementStep step)
        {
            WebGlHarnessReporter.ReportChaserCell(step.To);
            if (_chaserMovedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("chaser-moved");
            _chaserMovedReported = true;
        }

        private void OnChargerAdvanced(ChargerEnemyAdvanceResult result)
        {
            if (!_chargerTelegraphReported && result.HasStateTransition &&
                result.State == ChargerEnemyState.Telegraph)
            {
                WebGlHarnessReporter.Report("charger-telegraph");
                WebGlHarnessReporter.Report(
                    "charger-telegraph-" +
                    ToMarkerDirection(result.Direction) +
                    "-distance-" +
                    result.LockedChargeDistance);
                _chargerTelegraphReported = true;
            }
            if (!_chargerChargeReported && result.HasStateTransition &&
                result.State == ChargerEnemyState.Charge)
            {
                WebGlHarnessReporter.Report("charger-charge");
                _chargerChargeReported = true;
            }
            if (!_chargerMovedReported && result.HasMovement)
            {
                WebGlHarnessReporter.Report("charger-moved");
                _chargerMovedReported = true;
            }
            if (!_chargerTrackMovedReported && result.HasMovement &&
                result.State == ChargerEnemyState.Track)
            {
                WebGlHarnessReporter.Report("charger-track-moved");
                _chargerTrackMovedReported = true;
            }
            if (!_chargerChargeMovedReported && result.HasMovement &&
                result.State == ChargerEnemyState.Charge)
            {
                WebGlHarnessReporter.Report("charger-charge-moved");
                _chargerChargeMovedReported = true;
            }
            if (!_chargerRecoverReported && result.HasStateTransition &&
                result.State == ChargerEnemyState.Recover)
            {
                WebGlHarnessReporter.Report("charger-recover");
                WebGlHarnessReporter.Report(
                    result.ImpactedTarget
                        ? "charger-recover-target"
                        : "charger-recover-obstacle-or-limit");
                _chargerRecoverReported = true;
            }
        }

        private static string ToMarkerDirection(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return "north";
                case CardinalDirection.East:
                    return "east";
                case CardinalDirection.South:
                    return "south";
                case CardinalDirection.West:
                    return "west";
                default:
                    return "none";
            }
        }

        private void OnArmoredMoved(EnemyMovementStep step)
        {
            if (_armoredMovedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("armored-moved");
            _armoredMovedReported = true;
        }

        private void OnArmoredAdvanced(ArmoredEnemyAdvanceResult result)
        {
            if (!_armoredPanicRunReported &&
                result.HasMovement &&
                result.PreviousState == ArmoredEnemyBehaviorState.PanicRun)
            {
                WebGlHarnessReporter.Report("armored-panic-run-moved");
                _armoredPanicRunReported = true;
            }
            if (!_armoredPanicRecoverReported &&
                result.State == ArmoredEnemyBehaviorState.PanicRecover)
            {
                WebGlHarnessReporter.Report("armored-panic-recover");
                _armoredPanicRecoverReported = true;
            }
            if (!_armoredChaseReported &&
                result.State == ArmoredEnemyBehaviorState.Chase)
            {
                WebGlHarnessReporter.Report("armored-chase");
                _armoredChaseReported = true;
            }
        }

        private void OnArmoredStateChanged(ArmoredEnemyDamageResult result)
        {
            if (!_armoredBrokenReported && result.ArmorWasBroken)
            {
                WebGlHarnessReporter.Report("armored-broken");
                _armoredBrokenReported = true;
            }
            if (!_armoredPanicTelegraphReported &&
                result.ArmorWasBroken &&
                result.CurrentBehaviorState == ArmoredEnemyBehaviorState.PanicTelegraph)
            {
                WebGlHarnessReporter.Report(
                    "armored-panic-telegraph-" +
                    ToMarkerDirection(session.CurrentArmoredPanicDirection) +
                    "-distance-" + session.CurrentArmoredPanicPathCellCount);
                _armoredPanicTelegraphReported = true;
            }
            if (!_armoredDiedReported && result.WasFatal)
            {
                WebGlHarnessReporter.Report("armored-died");
                _armoredDiedReported = true;
            }
        }

        private void OnEnemyDied(EnemyDamageResult result)
        {
            if (!_selfDestructDiedReported && session.HasSelfDestruct &&
                result.ActorId == session.SelfDestructActorId)
            {
                WebGlHarnessReporter.Report("self-destruct-died");
                _selfDestructDiedReported = true;
            }
            if (!_throwerDiedReported && session.HasThrower &&
                result.ActorId == session.ThrowerActorId)
            {
                WebGlHarnessReporter.Report("thrower-died");
                _throwerDiedReported = true;
            }
            if (_enemyDiedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("enemy-died");
            _enemyDiedReported = true;
        }

        private void OnSelfDestructAdvanced(SelfDestructEnemyAdvanceResult result)
        {
            if (result.HasMovement)
            {
                ReportSelfDestructCell(result.Movement.To);
                if (!_selfDestructMovedReported)
                {
                    WebGlHarnessReporter.Report("self-destruct-moved");
                    _selfDestructMovedReported = true;
                }
            }
            if (!_selfDestructWarningReported && result.HasStateTransition &&
                result.State == SelfDestructEnemyState.WarningChase)
            {
                ReportSelfDestructWarning();
            }
            if (!_selfDestructTelegraphReported && result.HasStateTransition &&
                result.State == SelfDestructEnemyState.Telegraph)
            {
                WebGlHarnessReporter.Report("self-destruct-telegraph");
                _selfDestructTelegraphReported = true;
            }
            if (!_selfDestructDetonatedReported && result.HasStateTransition &&
                result.State == SelfDestructEnemyState.Detonated)
            {
                WebGlHarnessReporter.Report("self-destruct-detonated");
                _selfDestructDetonatedReported = true;
            }
        }

        private void ReportSelfDestructWarning()
        {
            WebGlHarnessReporter.Report("self-destruct-warning-chase");
            _selfDestructWarningReported = true;
        }

        private static void ReportSelfDestructCell(GridPosition position)
        {
            WebGlHarnessReporter.Report(
                "self-destruct-cell-x-" + position.X + "-z-" + position.Z);
        }

        private void OnSelfDestructArmed(BombSnapshot snapshot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _bombDefinitionsById[snapshot.Id] = snapshot.DefinitionId.Value;
#endif
            if (_selfDestructArmedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("self-destruct-armed");
            _selfDestructArmedReported = true;
        }

        private static void OnSelfDestructSpawned(ActorId actorId)
        {
            WebGlHarnessReporter.Report("boss-self-destruct-spawned");
        }

        private void OnThrowerAdvanced(ThrowerEnemyAdvanceResult result)
        {
            if (result.HasMovement)
            {
                ReportThrowerCell(result.Movement.To);
                if (!_throwerMovedReported)
                {
                    WebGlHarnessReporter.Report("thrower-track-moved");
                    _throwerMovedReported = true;
                }
            }
            if (result.HasStateTransition &&
                result.State == ThrowerEnemyState.Telegraph)
            {
                WebGlHarnessReporter.Report("thrower-telegraph");
                for (int index = 0; index < result.LockedTargets.Count; index++)
                {
                    GridPosition target = result.LockedTargets[index];
                    WebGlHarnessReporter.Report(
                        "thrower-telegraph-x-" + target.X +
                        "-z-" + target.Z);
                }
            }
        }

        private void OnThrowerBombLaunched(ThrowerBombFlight flight)
        {
            WebGlHarnessReporter.Report("thrower-bomb-launched");
        }

        private void OnThrowerBombPlaced(BombSnapshot snapshot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _bombDefinitionsById[snapshot.Id] = snapshot.DefinitionId.Value;
#endif
            WebGlHarnessReporter.Report(
                "thrower-bomb-armed-definition-" + snapshot.DefinitionId.Value);
        }

        private static void ReportThrowerCell(GridPosition position)
        {
            WebGlHarnessReporter.Report(
                "thrower-cell-x-" + position.X + "-z-" + position.Z);
        }

        private void OnBossPatternTransitioned(BossPatternTransition transition)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            _lastBossState = transition.State;
            _lastBossPattern = transition.Pattern;
#endif
            ReportBossPattern(
                transition.Phase,
                transition.Pattern,
                transition.State,
                transition.DangerCells);
            switch (transition.State)
            {
                case BossBattleState.Telegraph:
                    WebGlHarnessReporter.Report("boss-pattern-telegraph");
                    WebGlHarnessReporter.ReportBossMoveTarget(
                        transition.NextBossPosition);
                    break;
                case BossBattleState.Execute:
                    WebGlHarnessReporter.Report("boss-pattern-execute");
                    break;
                case BossBattleState.Recovery:
                    WebGlHarnessReporter.Report("boss-pattern-recovery");
                    break;
                case BossBattleState.Defeated:
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(transition),
                        transition.State,
                        "Unsupported boss battle state.");
            }

            if (transition.MovementBlocked)
            {
                WebGlHarnessReporter.Report("boss-move-blocked");
            }

            if (!_bossPhaseTwoReported && transition.Phase == BossPhase.Two)
            {
                WebGlHarnessReporter.Report("boss-phase-two");
                _bossPhaseTwoReported = true;
            }
            if (!_bossLastStandReported && transition.Phase == BossPhase.LastStand)
            {
                WebGlHarnessReporter.Report("boss-phase-last-stand");
                _bossLastStandReported = true;
            }
        }

        private static void ReportBossPattern(
            BossPhase phase,
            BossPatternKind pattern,
            BossBattleState state,
            System.Collections.Generic.IReadOnlyList<GridPosition> dangerCells)
        {
            string patternName = GetBossPatternName(pattern);
            string stateName = state.ToString().ToLowerInvariant();
            WebGlHarnessReporter.Report(
                "boss-pattern-" + patternName + "-" + stateName);

            if (state != BossBattleState.Telegraph || dangerCells.Count == 0)
            {
                return;
            }
            if (pattern == BossPatternKind.ParityWave)
            {
                WebGlHarnessReporter.Report(
                    "boss-parity-telegraph-phase-" + GetBossPhaseName(phase) +
                    "-row-" + dangerCells[0].Z);
            }
            else if (pattern == BossPatternKind.SummonSelfDestruct)
            {
                GridPosition target = dangerCells[0];
                WebGlHarnessReporter.Report(
                    "boss-summon-target-x-" + target.X + "-z-" + target.Z);
            }
        }

        private static string GetBossPatternName(BossPatternKind pattern)
        {
            switch (pattern)
            {
                case BossPatternKind.LimitedChase:
                    return "limited-chase";
                case BossPatternKind.FixedCharge:
                    return "fixed-charge";
                case BossPatternKind.ReturnToCenter:
                    return "return-to-center";
                case BossPatternKind.PhaseTransition:
                    return "phase-transition";
                case BossPatternKind.SummonSelfDestruct:
                    return "summon-self-destruct";
                case BossPatternKind.WaitForSelfDestruct:
                    return "wait-for-self-destruct";
                case BossPatternKind.BombVolley:
                    return "bomb-volley";
                case BossPatternKind.ParityWave:
                    return "parity-wave";
                case BossPatternKind.Overheat:
                    return "overheat";
                case BossPatternKind.LastStandBombChain:
                    return "last-stand-bomb-chain";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(pattern), pattern, null);
            }
        }

        private static string GetBossPhaseName(BossPhase phase)
        {
            switch (phase)
            {
                case BossPhase.One:
                    return "one";
                case BossPhase.Two:
                    return "two";
                case BossPhase.LastStand:
                    return "last-stand";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        private static void OnBossMoved(EnemyMovementStep step)
        {
            WebGlHarnessReporter.Report("boss-moved");
            WebGlHarnessReporter.ReportBossCell(step.To);
        }

        private void OnBossDamaged(BossDamageResult result)
        {
            WebGlHarnessReporter.Report("boss-damaged");
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            string definitionId = _bombDefinitionsById.TryGetValue(
                result.ExplosionId,
                out string recordedDefinitionId)
                    ? recordedDefinitionId
                    : "unknown";
            _bombDefinitionsById.Remove(result.ExplosionId);
            WebGlHarnessReporter.Report(
                "boss-damaged-phase-" + GetBossPhaseName(result.Phase) +
                "-state-" + GetBossStateName(_lastBossState) +
                "-source-" + GetBossDamageSourceName(result.Source) +
                "-definition-" + definitionId +
                "-health-" + result.CurrentHealth);
#endif
            if (!_bossDefeatedReported && result.WasFatal)
            {
                WebGlHarnessReporter.Report("boss-defeated");
                _bossDefeatedReported = true;
            }
        }

        private static string GetBossStateName(BossBattleState state)
        {
            switch (state)
            {
                case BossBattleState.Telegraph:
                    return "telegraph";
                case BossBattleState.Execute:
                    return "execute";
                case BossBattleState.Recovery:
                    return "recovery";
                case BossBattleState.Defeated:
                    return "defeated";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private static string GetBossDamageSourceName(BossDamageSource source)
        {
            switch (source)
            {
                case BossDamageSource.PlayerBomb:
                    return "player-bomb";
                case BossDamageSource.SelfDestruct:
                    return "self-destruct";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }

        private void OnRoomCleared()
        {
            if (_roomClearedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("room-cleared");
            _roomClearedReported = true;
        }

        private void OnCommandIssued(PlayerCommand command)
        {
            if (!_audioUnlockReported)
            {
                WebGlHarnessReporter.Report("audio-unlocked");
                _audioUnlockReported = true;
            }

            switch (command.Kind)
            {
                case PlayerCommandKind.Move:
                    ReportMoveDirection(command.MoveDirection);
                    if (command.MoveDirection == CardinalDirection.None)
                    {
                        _lastMotionDirection = CardinalDirection.None;
                    }
                    break;
                case PlayerCommandKind.PlaceBomb:
                    break;
                case PlayerCommandKind.SwapBomb:
                    if (!_swapBombReported)
                    {
                        WebGlHarnessReporter.Report("swap-bomb");
                        _swapBombReported = true;
                    }
                    break;
                case PlayerCommandKind.Pause:
                    break;
                case PlayerCommandKind.RestartRun:
                    break;
            }
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                WebGlHarnessReporter.Report("pause-entered");
                _pauseEnteredReported = true;
                return;
            }

            WebGlHarnessReporter.Report("pause-resumed");
            if (_pauseEnteredReported && !_pauseReported)
            {
                WebGlHarnessReporter.Report("pause-resume");
                _pauseReported = true;
            }
        }

        private static void ReportMoveDirection(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.None:
                    WebGlHarnessReporter.Report("move-direction-none");
                    break;
                case CardinalDirection.North:
                    WebGlHarnessReporter.Report("move-direction-north");
                    break;
                case CardinalDirection.East:
                    WebGlHarnessReporter.Report("move-direction-east");
                    break;
                case CardinalDirection.South:
                    WebGlHarnessReporter.Report("move-direction-south");
                    break;
                case CardinalDirection.West:
                    WebGlHarnessReporter.Report("move-direction-west");
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unsupported move direction for the WebGL harness.");
            }
        }

        private static void ReportMovementStepDirection(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    WebGlHarnessReporter.Report("move-step-direction-north");
                    break;
                case CardinalDirection.East:
                    WebGlHarnessReporter.Report("move-step-direction-east");
                    break;
                case CardinalDirection.South:
                    WebGlHarnessReporter.Report("move-step-direction-south");
                    break;
                case CardinalDirection.West:
                    WebGlHarnessReporter.Report("move-step-direction-west");
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unsupported movement-step direction for the WebGL harness.");
            }
        }

        private static void ReportMotionDirection(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    WebGlHarnessReporter.Report("move-motion-direction-north");
                    break;
                case CardinalDirection.East:
                    WebGlHarnessReporter.Report("move-motion-direction-east");
                    break;
                case CardinalDirection.South:
                    WebGlHarnessReporter.Report("move-motion-direction-south");
                    break;
                case CardinalDirection.West:
                    WebGlHarnessReporter.Report("move-motion-direction-west");
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unsupported motion direction for the WebGL harness.");
            }
        }
    }
}
