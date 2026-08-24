using System.Collections.Generic;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BombSwap.Tests.PlayMode
{
    public sealed class BombSwapInputReaderTests : InputTestFixture
    {
        private InputActionAsset _inputActions;
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private GameObject _gameObject;
        private BombSwapInputReader _reader;
        private List<PlayerCommand> _commands;

        [SetUp]
        public void SetUp()
        {
            _inputActions = CreateInputActions();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _commands = new List<PlayerCommand>();

            _gameObject = new GameObject("InputReaderTest");
            _gameObject.SetActive(false);
            _reader = _gameObject.AddComponent<BombSwapInputReader>();
            _reader.Configure(_inputActions);
            _reader.CommandIssued += _commands.Add;
            _gameObject.SetActive(true);
            _reader.SetInputFocus(true);
        }

        [TearDown]
        public override void TearDown()
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
            if (_gamepad != null && _gamepad.added)
            {
                InputSystem.RemoveDevice(_gamepad);
            }
            base.TearDown();
        }

        [Test]
        public void KeyboardMove_EmitsCardinalIntentAndRelease()
        {
            QueueKeyboardState(Key.W);
            _reader.RefreshMoveIntent();
            QueueKeyboardState();
            _reader.RefreshMoveIntent();

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
            _reader.RefreshMoveIntent();
            QueueKeyboardState(Key.UpArrow, Key.RightArrow);
            _reader.RefreshMoveIntent();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.East),
            }), "The new perpendicular key must win while the previous key is still held.");

            QueueKeyboardState(Key.RightArrow);
            QueueKeyboardState();
            _reader.RefreshMoveIntent();

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
            _reader.RefreshMoveIntent();
            QueueKeyboardState(Key.UpArrow, Key.RightArrow);
            _reader.RefreshMoveIntent();
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
        public void TapCompletedBetweenFrames_IsVisibleForOneFrameThenReleases()
        {
            QueueKeyboardState(Key.UpArrow);
            QueueKeyboardState();

            _reader.RefreshMoveIntent();
            _reader.RefreshMoveIntent();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.None),
            }), "A press and release inside one frame must not collapse into no movement.");
        }

        [Test]
        public void PerpendicularTapWhileHeld_WinsOneFrameThenRestoresHeldDirection()
        {
            QueueKeyboardState(Key.UpArrow);
            _reader.RefreshMoveIntent();
            _commands.Clear();

            QueueKeyboardState(Key.UpArrow, Key.RightArrow);
            QueueKeyboardState(Key.UpArrow);
            _reader.RefreshMoveIntent();
            _reader.RefreshMoveIntent();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.East),
                PlayerCommand.Move(CardinalDirection.North),
            }), "A short perpendicular turn must affect one frame before the held fallback resumes.");
        }

        [Test]
        public void RapidAlternatingSubframeTaps_EmitOneDirectionPerFrame()
        {
            Key[] keys = { Key.UpArrow, Key.RightArrow, Key.UpArrow, Key.RightArrow };
            for (int index = 0; index < keys.Length; index++)
            {
                QueueKeyboardState(keys[index]);
                QueueKeyboardState();
                _reader.RefreshMoveIntent();
            }
            _reader.RefreshMoveIntent();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.East),
                PlayerCommand.Move(CardinalDirection.North),
                PlayerCommand.Move(CardinalDirection.East),
                PlayerCommand.Move(CardinalDirection.None),
            }));
        }

        [Test]
        public void GameplayButtons_EmitSemanticCommands()
        {
            QueueKeyboardState(Key.Z);
            QueueKeyboardState();
            QueueKeyboardState(Key.X);
            QueueKeyboardState();
            QueueKeyboardState(Key.Escape);
            QueueKeyboardState();
            QueueKeyboardState(Key.R);
            QueueKeyboardState();
            QueueKeyboardState(Key.E);

            Assert.That(_commands, Has.Member(PlayerCommand.PlaceBomb()));
            Assert.That(_commands, Has.Member(PlayerCommand.SwapBomb()));
            Assert.That(_commands, Has.Member(PlayerCommand.Pause()));
            Assert.That(_commands, Has.Member(PlayerCommand.RestartRun()));
            Assert.That(_commands, Has.Member(PlayerCommand.Interact()));
        }

        [TestCase(0f, 1f, CardinalDirection.North)]
        [TestCase(1f, 0f, CardinalDirection.East)]
        [TestCase(0f, -1f, CardinalDirection.South)]
        [TestCase(-1f, 0f, CardinalDirection.West)]
        public void GamepadLeftStick_EmitsCardinalIntentAndRelease(
            float x,
            float y,
            CardinalDirection expectedDirection)
        {
            QueueGamepadState(new GamepadState { leftStick = new Vector2(x, y) });
            _reader.RefreshMoveIntent();
            QueueGamepadState(new GamepadState());
            _reader.RefreshMoveIntent();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(expectedDirection),
                PlayerCommand.Move(CardinalDirection.None),
            }));
        }

        [TestCase(GamepadButton.DpadUp, CardinalDirection.North)]
        [TestCase(GamepadButton.DpadRight, CardinalDirection.East)]
        [TestCase(GamepadButton.DpadDown, CardinalDirection.South)]
        [TestCase(GamepadButton.DpadLeft, CardinalDirection.West)]
        public void GamepadDpad_EmitsCardinalIntentAndRelease(
            GamepadButton button,
            CardinalDirection expectedDirection)
        {
            QueueGamepadState(new GamepadState(button));
            _reader.RefreshMoveIntent();
            QueueGamepadState(new GamepadState());
            _reader.RefreshMoveIntent();

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.Move(expectedDirection),
                PlayerCommand.Move(CardinalDirection.None),
            }));
        }

        [Test]
        public void GamepadButtons_EmitSemanticCommands()
        {
            QueueGamepadState(new GamepadState(GamepadButton.South));
            QueueGamepadState(new GamepadState());
            QueueGamepadState(new GamepadState(GamepadButton.West));
            QueueGamepadState(new GamepadState());
            QueueGamepadState(new GamepadState(GamepadButton.Start));
            QueueGamepadState(new GamepadState());
            QueueGamepadState(new GamepadState(GamepadButton.Select));
            QueueGamepadState(new GamepadState());
            QueueGamepadState(new GamepadState(GamepadButton.North));

            Assert.That(_commands, Is.EqualTo(new[]
            {
                PlayerCommand.PlaceBomb(),
                PlayerCommand.SwapBomb(),
                PlayerCommand.Pause(),
                PlayerCommand.RestartRun(),
                PlayerCommand.Interact(),
            }));
        }

        [Test]
        public void FocusLoss_ReleasesMoveAndBlocksCommandsUntilFocusReturns()
        {
            QueueKeyboardState(Key.D);
            _reader.RefreshMoveIntent();

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
            _reader.RefreshMoveIntent();

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

        private void QueueGamepadState(GamepadState state)
        {
            InputSystem.QueueStateEvent(_gamepad, state);
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
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            InputAction placeBomb = gameplay.AddAction(
                BombSwapInputActionNames.PlaceBomb,
                InputActionType.Button,
                "<Keyboard>/z");
            placeBomb.AddBinding("<Gamepad>/buttonSouth");

            InputAction swapBomb = gameplay.AddAction(
                BombSwapInputActionNames.SwapBomb,
                InputActionType.Button,
                "<Keyboard>/x");
            swapBomb.AddBinding("<Gamepad>/buttonWest");

            InputAction pause = gameplay.AddAction(
                BombSwapInputActionNames.Pause,
                InputActionType.Button,
                "<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            InputAction restartRun = gameplay.AddAction(
                BombSwapInputActionNames.RestartRun,
                InputActionType.Button,
                "<Keyboard>/r");
            restartRun.AddBinding("<Gamepad>/select");
            InputAction interact = gameplay.AddAction(
                BombSwapInputActionNames.Interact,
                InputActionType.Button,
                "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonNorth");
            return asset;
        }
    }
}
