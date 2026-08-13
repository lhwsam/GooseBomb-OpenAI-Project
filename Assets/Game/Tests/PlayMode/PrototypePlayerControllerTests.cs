using System.Collections;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypePlayerControllerTests
    {
        private InputActionAsset _inputActions;
        private Keyboard _keyboard;
        private GameObject _root;
        private PrototypePlayerController _controller;
        private PrototypeInputHarnessProbe _probe;
        private Transform _player;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
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

        [UnityTest]
        public IEnumerator HeldDirection_AdvancesLogicalCellAndInterpolatesPlaceholder()
        {
            CreateRuntime(Vector2Int.zero, false);
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_controller.CurrentGridPosition, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(_player.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(_player.position.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(_player.position.z, Is.EqualTo(1f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator AuthoredBlockedCell_PreventsLogicalAndVisualMovement()
        {
            CreateRuntime(new Vector2Int(0, 1), true);
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_controller.CurrentGridPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(_player.position, Is.EqualTo(new Vector3(0f, 0.5f, 0f)));
        }

        [UnityTest]
        public IEnumerator HarnessProbe_RemainsEnabledAcrossControllerInitializationOrder()
        {
            CreateRuntime(Vector2Int.zero, false, true);

            yield return null;

            Assert.That(_controller.IsInitialized, Is.True);
            Assert.That(_controller.IsReady, Is.True);
            Assert.That(_probe.enabled, Is.True);
            Assert.That(_probe.InputReader, Is.SameAs(_controller.InputReader));
            Assert.That(_probe.PlayerController, Is.SameAs(_controller));
        }

        private void CreateRuntime(
            Vector2Int blocker,
            bool includeBlocker,
            bool includeProbe = false)
        {
            _inputActions = CreateInputActions();
            _keyboard = InputSystem.AddDevice<Keyboard>();

            _root = new GameObject("PrototypePlayerControllerTest");
            _root.SetActive(false);

            var gridRoot = new GameObject("GridRoot").transform;
            gridRoot.SetParent(_root.transform, false);
            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetParent(gridRoot, false);
            _player = new GameObject("PlayerPlaceholder").transform;
            _player.SetParent(gridRoot, false);
            _player.position = new Vector3(0f, 0.5f, 0f);

            BombSwapInputReader reader = _root.AddComponent<BombSwapInputReader>();
            reader.Configure(_inputActions);
            TestSandboxContext context = _root.AddComponent<TestSandboxContext>();
            context.Configure(
                reader,
                gridRoot,
                spawn,
                _player,
                3,
                3,
                1f,
                includeBlocker ? new[] { blocker } : new Vector2Int[0]);

            _controller = _root.AddComponent<PrototypePlayerController>();
            _controller.Configure(context, reader, _player, 10f);
            if (includeProbe)
            {
                _probe = _root.AddComponent<PrototypeInputHarnessProbe>();
                _probe.Configure(reader, _controller);
            }
            _root.SetActive(true);
        }

        private void QueueKeyboardState(params Key[] pressedKeys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(pressedKeys));
            InputSystem.Update();
        }

        private static InputActionAsset CreateInputActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "PrototypeMovementTestActions";
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
            gameplay.AddAction(BombSwapInputActionNames.PlaceBomb, InputActionType.Button, "<Keyboard>/z");
            gameplay.AddAction(BombSwapInputActionNames.SwapBomb, InputActionType.Button, "<Keyboard>/x");
            gameplay.AddAction(BombSwapInputActionNames.Pause, InputActionType.Button, "<Keyboard>/escape");
            return asset;
        }
    }
}
