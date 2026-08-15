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
        private bool _armoredMovedReported;
        private bool _armoredBrokenReported;
        private bool _armoredDiedReported;
        private bool _enemyDiedReported;
        private bool _bossPhaseTwoReported;
        private bool _bossDefeatedReported;
        private bool _roomClearedReported;
        private bool _swapBombReported;
        private bool _pauseReported;
        private bool _pauseEnteredReported;
        private bool _readyReported;
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
            session.BombExploded += OnBombExploded;
            session.ActiveBombSlotChanged += OnActiveBombSlotChanged;
            session.PlayerDamaged += OnPlayerDamaged;
            session.PlayerDied += OnPlayerDied;
            session.PlayerRecovered += OnPlayerRecovered;
            session.ChaserMoved += OnChaserMoved;
            session.ChargerAdvanced += OnChargerAdvanced;
            session.ArmoredMoved += OnArmoredMoved;
            session.ArmoredStateChanged += OnArmoredStateChanged;
            session.EnemyDied += OnEnemyDied;
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
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
            if (session != null)
            {
                session.PlayerMoved -= OnPlayerMoved;
                session.PlayerPositionChanged -= OnPlayerPositionChanged;
                session.BombPlaced -= OnBombPlaced;
                session.BombExploded -= OnBombExploded;
                session.ActiveBombSlotChanged -= OnActiveBombSlotChanged;
                session.PlayerDamaged -= OnPlayerDamaged;
                session.PlayerDied -= OnPlayerDied;
                session.PlayerRecovered -= OnPlayerRecovered;
                session.ChaserMoved -= OnChaserMoved;
                session.ChargerAdvanced -= OnChargerAdvanced;
                session.ArmoredMoved -= OnArmoredMoved;
                session.ArmoredStateChanged -= OnArmoredStateChanged;
                session.EnemyDied -= OnEnemyDied;
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
            if (session.HasBoss &&
                session.CurrentBossState == BossBattleState.Telegraph)
            {
                WebGlHarnessReporter.Report("boss-pattern-telegraph");
            }
            ReportPlayerHealth(session.CurrentHealth);
            WebGlHarnessReporter.ReportPlayerCell(session.CurrentGridPosition);
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
            WebGlHarnessReporter.Report(
                "bomb-exploded-definition-" + explosion.DefinitionId.Value);
            if (explosion.DefinitionId.Value == "prototype-line")
            {
                ReportLineBombDirection("line-bomb-exploded", explosion.PlacementDirection);
            }
            if (!_destructibleWallDestroyedReported && explosion.DestroyedWalls.Count > 0)
            {
                WebGlHarnessReporter.Report("destructible-wall-destroyed");
                _destructibleWallDestroyedReported = true;
            }

            WebGlHarnessReporter.Report("bomb-exploded");
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

        private void OnArmoredStateChanged(ArmoredEnemyDamageResult result)
        {
            if (!_armoredBrokenReported && result.ArmorWasBroken)
            {
                WebGlHarnessReporter.Report("armored-broken");
                _armoredBrokenReported = true;
            }
            if (!_armoredDiedReported && result.WasFatal)
            {
                WebGlHarnessReporter.Report("armored-died");
                _armoredDiedReported = true;
            }
        }

        private void OnEnemyDied(EnemyDamageResult result)
        {
            if (_enemyDiedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("enemy-died");
            _enemyDiedReported = true;
        }

        private void OnBossPatternTransitioned(BossPatternTransition transition)
        {
            switch (transition.State)
            {
                case BossBattleState.Telegraph:
                    WebGlHarnessReporter.Report("boss-pattern-telegraph");
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

            if (!_bossPhaseTwoReported && transition.Phase == BossPhase.Two)
            {
                WebGlHarnessReporter.Report("boss-phase-two");
                _bossPhaseTwoReported = true;
            }
        }

        private void OnBossDamaged(BossDamageResult result)
        {
            WebGlHarnessReporter.Report("boss-damaged");
            if (!_bossDefeatedReported && result.WasFatal)
            {
                WebGlHarnessReporter.Report("boss-defeated");
                _bossDefeatedReported = true;
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
