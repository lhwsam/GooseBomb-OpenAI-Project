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
        private bool _bombExplosionReported;
        private bool _playerDamagedReported;
        private bool _playerExplosionDamagedReported;
        private bool _playerContactDamagedReported;
        private bool _chaserMovedReported;
        private bool _enemyDiedReported;
        private bool _roomClearedReported;
        private bool _swapBombReported;
        private bool _pauseReported;
        private bool _isPaused;
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
            session.ChaserMoved += OnChaserMoved;
            session.EnemyDied += OnEnemyDied;
            session.RoomCleared += OnRoomCleared;
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
                session.ChaserMoved -= OnChaserMoved;
                session.EnemyDied -= OnEnemyDied;
                session.RoomCleared -= OnRoomCleared;
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
            _readyReported = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            ReportMovementStepDirection(step.Direction);

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
            if (_bombExplosionReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("bomb-exploded");
            _bombExplosionReported = true;
        }

        private void OnPlayerDamaged(PlayerDamageResult result)
        {
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
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(result),
                        result.SourceKind,
                        "Unsupported player damage source kind.");
            }
        }

        private void OnChaserMoved(EnemyMovementStep step)
        {
            if (_chaserMovedReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("chaser-moved");
            _chaserMovedReported = true;
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
                    _isPaused = !_isPaused;
                    if (!_pauseReported && !_isPaused)
                    {
                        WebGlHarnessReporter.Report("pause-resume");
                        _pauseReported = true;
                    }
                    break;
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
