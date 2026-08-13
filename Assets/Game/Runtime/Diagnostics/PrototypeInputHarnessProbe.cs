using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BombSwapInputReader))]
    [RequireComponent(typeof(PrototypePlayerController))]
    public sealed class PrototypeInputHarnessProbe : MonoBehaviour
    {
        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private PrototypePlayerController playerController;

        private bool _audioUnlockReported;
        private bool _moveReported;
        private bool _placeBombReported;
        private bool _swapBombReported;
        private bool _pauseReported;
        private bool _isPaused;
        private bool _readyReported;

        public BombSwapInputReader InputReader => inputReader;

        public PrototypePlayerController PlayerController => playerController;

        public void Configure(
            BombSwapInputReader reader,
            PrototypePlayerController controller)
        {
            if (isActiveAndEnabled)
            {
                throw new System.InvalidOperationException(
                    "Disable PrototypeInputHarnessProbe before changing its input reader.");
            }

            inputReader = reader;
            playerController = controller;
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
            if (playerController == null)
            {
                playerController = GetComponent<PrototypePlayerController>();
            }

            if (inputReader == null || playerController == null)
            {
                Debug.LogError(
                    "PrototypeInputHarnessProbe requires input and player movement components.",
                    this);
                enabled = false;
                return;
            }

            inputReader.CommandIssued += OnCommandIssued;
            playerController.CellEntered += OnCellEntered;
            playerController.Ready += OnPlayerControllerReady;
            if (playerController.IsReady)
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
            if (playerController != null)
            {
                playerController.CellEntered -= OnCellEntered;
                playerController.Ready -= OnPlayerControllerReady;
            }
        }

        private void OnPlayerControllerReady()
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

        private void OnCellEntered(PlayerMovementStep step)
        {
            if (_moveReported)
            {
                return;
            }

            WebGlHarnessReporter.Report("move");
            _moveReported = true;
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
