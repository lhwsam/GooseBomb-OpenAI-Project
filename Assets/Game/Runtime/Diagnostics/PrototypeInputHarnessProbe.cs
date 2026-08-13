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
        private bool _placeBombReported;
        private bool _bombExplosionReported;
        private bool _swapBombReported;
        private bool _pauseReported;
        private bool _isPaused;
        private bool _readyReported;

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
            session.BombPlaced += OnBombPlaced;
            session.BombExploded += OnBombExploded;
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
                session.BombPlaced -= OnBombPlaced;
                session.BombExploded -= OnBombExploded;
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
            _readyReported = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            if (_moveReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("move");
            _moveReported = true;
        }

        private void OnBombPlaced(BombSnapshot snapshot)
        {
            if (_placeBombReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("place-bomb");
            _placeBombReported = true;
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
    }
}
