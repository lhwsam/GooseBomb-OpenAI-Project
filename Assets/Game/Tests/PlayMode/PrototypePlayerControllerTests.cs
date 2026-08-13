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
        private GameObject _bombPrefab;
        private GameObject _explosionPrefab;
        private PrototypeBombDefinitionAsset _definition;
        private PrototypeGameSession _session;
        private PrototypePlayerController _controller;
        private PrototypeBombPresenter _presenter;
        private PrototypeInputHarnessProbe _probe;
        private Transform _player;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
            if (_bombPrefab != null)
            {
                Object.DestroyImmediate(_bombPrefab);
            }
            if (_explosionPrefab != null)
            {
                Object.DestroyImmediate(_explosionPrefab);
            }
            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
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
        public IEnumerator HeldDirection_AdvancesSharedLogicalCellAndInterpolatesPlaceholder()
        {
            CreateRuntime(Vector2Int.zero, false);
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_session.CurrentGridPosition, Is.EqualTo(new GridPosition(0, 1)));
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

            Assert.That(_session.CurrentGridPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(_player.position, Is.EqualTo(new Vector3(0f, 0.5f, 0f)));
        }

        [UnityTest]
        public IEnumerator PlaceBombInput_UsesSharedGridAndPublishesFuseExplosion()
        {
            CreateRuntime(Vector2Int.zero, false, fuseSeconds: 0.08f);
            BombSnapshot placed = default;
            BombExplosion exploded = null;
            int placementCount = 0;
            _session.BombPlaced += snapshot =>
            {
                placed = snapshot;
                placementCount++;
            };
            _session.BombExploded += explosion => exploded = explosion;
            yield return null;

            PressAndRelease(Key.Z);

            Assert.That(placementCount, Is.EqualTo(1));
            Assert.That(placed.Id.IsValid, Is.True);
            var origin = new GridPosition(0, 0);
            Assert.That(placed.Position, Is.EqualTo(origin));
            Assert.That(_session.ActiveBombCount, Is.EqualTo(1));
            Assert.That(_session.GetCell(origin).HasActor, Is.True);
            Assert.That(_session.GetCell(origin).HasBomb, Is.True);

            PressAndRelease(Key.Z);
            Assert.That(placementCount, Is.EqualTo(1), "Duplicate placement must not publish an event.");

            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(exploded, Is.Not.Null);
            Assert.That(exploded.BombId, Is.EqualTo(placed.Id));
            Assert.That(exploded.Origin, Is.EqualTo(origin));
            Assert.That(exploded.AffectedCells, Has.Count.EqualTo(5));
            Assert.That(_session.ActiveBombCount, Is.Zero);
            Assert.That(_session.GetCell(origin).HasActor, Is.True);
            Assert.That(_session.GetCell(origin).HasBomb, Is.False);
        }

        [UnityTest]
        public IEnumerator BombPresenter_PoolsPlacedBombAndExplosionCells()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includePresenter: true,
                fuseSeconds: 0.08f,
                explosionVisualSeconds: 0.4f);
            yield return null;

            BombId placedId = default;
            _session.BombPlaced += snapshot => placedId = snapshot.Id;
            PressAndRelease(Key.Z);
            yield return null;

            Assert.That(_presenter.ActiveBombVisualCount, Is.EqualTo(1));
            Assert.That(_presenter.HasBombVisual(placedId), Is.True);

            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_presenter.ActiveBombVisualCount, Is.Zero);
            Assert.That(_presenter.ActiveExplosionVisualCount, Is.EqualTo(5));

            yield return new WaitForSecondsRealtime(0.45f);

            Assert.That(_presenter.ActiveExplosionVisualCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HarnessProbe_RemainsEnabledAcrossSessionInitializationOrder()
        {
            CreateRuntime(Vector2Int.zero, false, includeProbe: true);

            yield return null;

            Assert.That(_session.IsInitialized, Is.True);
            Assert.That(_session.IsReady, Is.True);
            Assert.That(_controller.IsInitialized, Is.True);
            Assert.That(_probe.enabled, Is.True);
            Assert.That(_probe.InputReader, Is.SameAs(_session.InputReader));
            Assert.That(_probe.Session, Is.SameAs(_session));
        }

        private void CreateRuntime(
            Vector2Int blocker,
            bool includeBlocker,
            bool includeProbe = false,
            bool includePresenter = false,
            float fuseSeconds = 1f,
            float explosionVisualSeconds = 0.25f)
        {
            _inputActions = CreateInputActions();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _bombPrefab = new GameObject("BombVisualPrefab");
            _bombPrefab.SetActive(false);
            _explosionPrefab = new GameObject("ExplosionVisualPrefab");
            _explosionPrefab.SetActive(false);
            _definition = ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
            _definition.Configure(
                "test-cross",
                fuseSeconds,
                1,
                _bombPrefab,
                _explosionPrefab,
                explosionVisualSeconds);

            _root = new GameObject("PrototypePlayerControllerTest");
            _root.SetActive(false);

            var gridRoot = new GameObject("GridRoot").transform;
            gridRoot.SetParent(_root.transform, false);
            var presentationRoot = new GameObject("RuntimePresentation").transform;
            presentationRoot.SetParent(gridRoot, false);
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

            _session = _root.AddComponent<PrototypeGameSession>();
            _session.Configure(context, reader, _definition, 10f, 0.05f);
            _controller = _root.AddComponent<PrototypePlayerController>();
            _controller.Configure(_session, _player);
            if (includePresenter)
            {
                _presenter = _root.AddComponent<PrototypeBombPresenter>();
                _presenter.Configure(_session, presentationRoot, 1, 5);
            }
            if (includeProbe)
            {
                _probe = _root.AddComponent<PrototypeInputHarnessProbe>();
                _probe.Configure(reader, _session);
            }
            _root.SetActive(true);
        }

        private void PressAndRelease(Key key)
        {
            QueueKeyboardState(key);
            QueueKeyboardState();
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
