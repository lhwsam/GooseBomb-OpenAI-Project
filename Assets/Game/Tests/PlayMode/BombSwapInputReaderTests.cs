using System.Collections.Generic;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BombSwap.Tests.PlayMode
{
    public sealed class BombSwapInputReaderTests
    {
        private InputActionAsset _inputActions;
        private Keyboard _keyboard;
        private GameObject _gameObject;
        private BombSwapInputReader _reader;
        private List<PlayerCommand> _commands;

        [SetUp]
        public void SetUp()
        {
            _inputActions = CreateInputActions();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _commands = new List<PlayerCommand>();

            _gameObject = new GameObject("InputReaderTest");
            _gameObject.SetActive(false);
            _reader = _gameObject.AddComponent<BombSwapInputReader>();
            _reader.Configure(_inputActions);
            _reader.CommandIssued += _commands.Add;
            _gameObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_reader != null)
            {
                _reader.CommandIssued -= _commands.Add;
            }
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
            if (_inputActions != null)
            {
                _inputActions.Disable();
                Object.DestroyImmediate(_inputActions);
            }
            if (_keyboard != null && _keyboard.added)
            {
                InputSystem.RemoveDevice(_keyboard);
            }
        }

        [Test]
        public void KeyboardMove_EmitsCardinalIntentAndRelease()
        {
            QueueKeyboardState(Key.W);
            QueueKeyboardState();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.None),
            }));
        }

        [Test]
        public void KeyboardTurn_EmitsNewPerpendicularDirectionBeforePreviousKeyRelease()
        {
            QueueKeyboardState(Key.UpArrow);
            QueueKeyboardState(Key.UpArrow, Key.RightArrow);

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.East),
            }), "The new perpendicular key must win while the previous key is still held.");

            QueueKeyboardState(Key.RightArrow);
            QueueKeyboardState();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.East),
                PlayerCommand.Move(CardinalDirection.None),
            }));
        }

        [Test]
        public void RefreshMoveIntent_UnchangedDiagonalKeepsLatestPressedAxis()
        {
            QueueKeyboardState(Key.UpArrow);
            QueueKeyboardState(Key.UpArrow, Key.RightArrow);
            _commands.Clear();

            _reader.RefreshMoveIntent();
            _reader.RefreshMoveIntent();
            _reader.RefreshMoveIntent();

            Assert.That(_reader.CurrentMoveDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(
                _commands,
                Is.Empty,
                "Unchanged held keys must not be reinterpreted or emit duplicate commands each frame.");
        }

        [Test]
        public void GameplayButtons_EmitSemanticCommands()
        {
            QueueKeyboardState(Key.Z);
            QueueKeyboardState();
            QueueKeyboardState(Key.X);
            QueueKeyboardState();
            QueueKeyboardState(Key.Escape);

            Assert.That(_commands, Has.Member(PlayerCommand.PlaceBomb()));
            Assert.That(_commands, Has.Member(PlayerCommand.SwapBomb()));
            Assert.That(_commands, Has.Member(PlayerCommand.Pause()));
        }

        [Test]
        public void FocusLoss_ReleasesMoveAndBlocksCommandsUntilFocusReturns()
        {
            QueueKeyboardState(Key.D);

            _reader.SetInputFocus(false);
            QueueKeyboardState(Key.Z);

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.East),
                PlayerCommand.Move(CardinalDirection.None),
            }));

            QueueKeyboardState();
            _reader.SetInputFocus(true);
            QueueKeyboardState(Key.Z);

            Assert.That(_commands[_commands.Count - 1], Is.EqualTo(PlayerCommand.PlaceBomb()));
        }

        [Test]
        public void FocusReturn_DoesNotRestoreAKeyWhoseReleaseWasLost()
        {
            QueueKeyboardState(Key.W);

            _reader.SetInputFocus(false);
            _reader.SetInputFocus(true);
            InputSystem.Update();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.None),
            }));
            Assert.That(_reader.CurrentMoveDirection, Is.EqualTo(CardinalDirection.None));
        }

        [Test]
        public void Reenable_DoesNotDuplicateButtonSubscriptions()
        {
            _gameObject.SetActive(false);
            _gameObject.SetActive(true);
            _commands.Clear();

            QueueKeyboardState(Key.Z);

            Assert.That(_commands, Is.EqualTo(new[] { PlayerCommand.PlaceBomb() }));
        }

        private void QueueKeyboardState(params Key[] pressedKeys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(pressedKeys));
            InputSystem.Update();
        }

        private static InputActionAsset CreateInputActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "TestBombSwapInputActions";
            InputActionMap gameplay = asset.AddActionMap(BombSwapInputActionNames.GameplayMap);

            InputAction move = gameplay.AddAction(
                BombSwapInputActionNames.Move,
                InputActionType.Value,
                expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector(mode=1)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector(mode=1)")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            gameplay.AddAction(BombSwapInputActionNames.PlaceBomb, InputActionType.Button, "<Keyboard>/z");
            gameplay.AddAction(BombSwapInputActionNames.SwapBomb, InputActionType.Button, "<Keyboard>/x");
            gameplay.AddAction(BombSwapInputActionNames.Pause, InputActionType.Button, "<Keyboard>/escape");
            return asset;
        }
    }
}
