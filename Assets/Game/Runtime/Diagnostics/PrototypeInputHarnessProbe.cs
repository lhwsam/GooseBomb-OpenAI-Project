using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BombSwapInputReader))]
    public sealed class PrototypeInputHarnessProbe : MonoBehaviour
    {
        [SerializeField]
        private BombSwapInputReader inputReader;

        private bool _audioUnlockReported;
        private bool _moveReported;
        private bool _placeBombReported;
        private bool _swapBombReported;
        private bool _pauseReported;
        private bool _isPaused;

        public BombSwapInputReader InputReader => inputReader;

        public void Configure(BombSwapInputReader reader)
        {
            if (isActiveAndEnabled)
            {
                throw new System.InvalidOperationException(
                    "Disable PrototypeInputHarnessProbe before changing its input reader.");
            }

            inputReader = reader;
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

            if (inputReader == null)
            {
                Debug.LogError("PrototypeInputHarnessProbe requires BombSwapInputReader.", this);
                enabled = false;
                return;
            }

            inputReader.CommandIssued += OnCommandIssued;
            WebGlHarnessReporter.Report("probe-ready");
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.CommandIssued -= OnCommandIssued;
            }
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
                    if (!_moveReported && command.MoveDirection != CardinalDirection.None)
                    {
                        WebGlHarnessReporter.Report("move");
                        _moveReported = true;
                    }
                    break;
                case PlayerCommandKind.PlaceBomb:
                    if (!_placeBombReported)
                    {
                        WebGlHarnessReporter.Report("place-bomb");
                        _placeBombReported = true;
                    }
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
