using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class BombSwapInputReader : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;

        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _placeBombAction;
        private InputAction _swapBombAction;
        private InputAction _pauseAction;
        private bool _isSubscribed;
        private bool _hasInputFocus = true;
        private bool _hasSampledMoveValue;
        private Vector2 _lastSampledMoveValue;

        public event Action<PlayerCommand> CommandIssued;

        public InputActionAsset InputActions => inputActions;

        public CardinalDirection CurrentMoveDirection { get; private set; }

        public bool IsReady => _isSubscribed;

        public void RefreshMoveIntent()
        {
            if (!_isSubscribed || !_hasInputFocus)
            {
                return;
            }

            ApplyMoveValue(_moveAction.ReadValue<Vector2>());
        }

        public void Configure(InputActionAsset actions)
        {
            if (isActiveAndEnabled)
            {
                throw new InvalidOperationException("Disable BombSwapInputReader before changing its Input Actions asset.");
            }

            inputActions = actions;
            ClearResolvedActions();
        }

        public void SetInputFocus(bool hasFocus)
        {
            if (_hasInputFocus == hasFocus)
            {
                if (!hasFocus)
                {
                    ReleaseMovement();
                }

                return;
            }

            _hasInputFocus = hasFocus;
            if (!_isSubscribed)
            {
                return;
            }

            if (hasFocus)
            {
                _gameplayMap.Enable();
            }
            else
            {
                ReleaseMovement();
                _gameplayMap.Disable();
                ResetBoundDevices();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!TryResolveActions())
            {
                enabled = false;
                return;
            }

            Subscribe();
            if (_hasInputFocus)
            {
                _gameplayMap.Enable();
            }
        }

        private void OnDisable()
        {
            if (!_isSubscribed)
            {
                return;
            }

            ReleaseMovement();
            _gameplayMap.Disable();
            Unsubscribe();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetInputFocus(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SetInputFocus(!pauseStatus);
        }

        private bool TryResolveActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("BombSwapInputReader requires a BombSwap Input Actions asset.", this);
                return false;
            }

            _gameplayMap = inputActions.FindActionMap(BombSwapInputActionNames.GameplayMap, false);
            if (_gameplayMap == null)
            {
                Debug.LogError(
                    $"Input Actions asset '{inputActions.name}' is missing the '{BombSwapInputActionNames.GameplayMap}' map.",
                    this);
                return false;
            }

            _moveAction = FindRequiredAction(BombSwapInputActionNames.Move);
            _placeBombAction = FindRequiredAction(BombSwapInputActionNames.PlaceBomb);
            _swapBombAction = FindRequiredAction(BombSwapInputActionNames.SwapBomb);
            _pauseAction = FindRequiredAction(BombSwapInputActionNames.Pause);

            return _moveAction != null &&
                   _placeBombAction != null &&
                   _swapBombAction != null &&
                   _pauseAction != null;
        }

        private InputAction FindRequiredAction(string actionName)
        {
            InputAction action = _gameplayMap.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogError(
                    $"Input map '{BombSwapInputActionNames.GameplayMap}' is missing action '{actionName}'.",
                    this);
            }

            return action;
        }

        private void Subscribe()
        {
            _moveAction.performed += OnMoveChanged;
            _moveAction.canceled += OnMoveChanged;
            _placeBombAction.performed += OnPlaceBomb;
            _swapBombAction.performed += OnSwapBomb;
            _pauseAction.performed += OnPause;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            _moveAction.performed -= OnMoveChanged;
            _moveAction.canceled -= OnMoveChanged;
            _placeBombAction.performed -= OnPlaceBomb;
            _swapBombAction.performed -= OnSwapBomb;
            _pauseAction.performed -= OnPause;
            _isSubscribed = false;
        }

        private void OnMoveChanged(InputAction.CallbackContext context)
        {
            ApplyMoveValue(context.ReadValue<Vector2>());
        }

        private void OnPlaceBomb(InputAction.CallbackContext context)
        {
            Issue(PlayerCommand.PlaceBomb());
        }

        private void OnSwapBomb(InputAction.CallbackContext context)
        {
            Issue(PlayerCommand.SwapBomb());
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            Issue(PlayerCommand.Pause());
        }

        private void SetMoveDirection(CardinalDirection direction)
        {
            if (direction == CurrentMoveDirection)
            {
                return;
            }

            CurrentMoveDirection = direction;
            Issue(PlayerCommand.Move(direction));
        }

        private void ApplyMoveValue(Vector2 value)
        {
            if (_hasSampledMoveValue && value == _lastSampledMoveValue)
            {
                return;
            }

            _lastSampledMoveValue = value;
            _hasSampledMoveValue = true;
            CardinalDirection nextDirection = CardinalInputInterpreter.Resolve(
                value,
                CurrentMoveDirection);
            SetMoveDirection(nextDirection);
        }

        private void ReleaseMovement()
        {
            _lastSampledMoveValue = Vector2.zero;
            _hasSampledMoveValue = true;
            SetMoveDirection(CardinalDirection.None);
        }

        private void Issue(PlayerCommand command)
        {
            CommandIssued?.Invoke(command);
        }

        private void ClearResolvedActions()
        {
            _gameplayMap = null;
            _moveAction = null;
            _placeBombAction = null;
            _swapBombAction = null;
            _pauseAction = null;
            _lastSampledMoveValue = Vector2.zero;
            _hasSampledMoveValue = false;
        }

        private void ResetBoundDevices()
        {
            var resetDevices = new HashSet<InputDevice>();
            foreach (InputAction action in _gameplayMap.actions)
            {
                foreach (InputControl control in action.controls)
                {
                    InputDevice device = control.device;
                    if (device != null && device.added && resetDevices.Add(device))
                    {
                        InputSystem.ResetDevice(device);
                    }
                }
            }
        }
    }
}
