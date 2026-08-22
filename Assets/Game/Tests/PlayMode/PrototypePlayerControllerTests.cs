using System.Collections;
using System.Collections.Generic;
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
        private GameObject _chaserPrefab;
        private GameObject _chargerPrefab;
        private GameObject _chargerTelegraphCellPrefab;
        private GameObject _armoredPrefab;
        private GameObject _armoredPanicTelegraphCellPrefab;
        private GameObject _selfDestructPrefab;
        private GameObject _selfDestructTelegraphCellPrefab;
        private GameObject _throwerPrefab;
        private GameObject _throwerTelegraphCellPrefab;
        private GameObject _bossPrefab;
        private GameObject _bossDangerCellPrefab;
        private PrototypeBombDefinitionAsset _definition;
        private PrototypeBombDefinitionAsset _areaDefinition;
        private PrototypeBombLoadoutAsset _loadout;
        private PrototypePlayerVitalsAsset _vitals;
        private PrototypeChaserDefinitionAsset _chaserDefinition;
        private PrototypeChargerDefinitionAsset _chargerDefinition;
        private PrototypeArmoredDefinitionAsset _armoredDefinition;
        private PrototypeBombDefinitionAsset _selfDestructBombDefinition;
        private PrototypeSelfDestructDefinitionAsset _selfDestructDefinition;
        private PrototypeBombDefinitionAsset _throwerBombDefinition;
        private PrototypeThrowerDefinitionAsset _throwerDefinition;
        private PrototypeBombDefinitionAsset _bossThrowBombDefinition;
        private PrototypeBombDefinitionAsset _bossChainBombDefinition;
        private PrototypeBossDefinitionAsset _bossDefinition;
        private PrototypeCombatRoomDefinitionAsset _roomDefinition;
        private Material _playerMaterial;
        private Material _chaserMaterial;
        private Material _chargerMaterial;
        private Material _chargerTelegraphCellMaterial;
        private Material _armoredMaterial;
        private Material _armoredPanicTelegraphCellMaterial;
        private Material _selfDestructMaterial;
        private Material _selfDestructTelegraphCellMaterial;
        private Material _throwerMaterial;
        private Material _throwerTelegraphCellMaterial;
        private Material _bossMaterial;
        private Material _bossDangerCellMaterial;
        private PrototypeGameSession _session;
        private BombSwapInputReader _reader;
        private PrototypePlayerController _controller;
        private PrototypeBombPresenter _presenter;
        private PrototypeDestructibleWallPresenter _destructibleWallPresenter;
        private PrototypeInputHarnessProbe _probe;
        private PrototypePlayerHealthPresenter _healthPresenter;
        private PrototypeChaserPresenter _chaserPresenter;
        private PrototypeChargerPresenter _chargerPresenter;
        private PrototypeArmoredPresenter _armoredPresenter;
        private PrototypeSelfDestructPresenter _selfDestructPresenter;
        private PrototypeThrowerPresenter _throwerPresenter;
        private PrototypeBossPresenter _bossPresenter;
        private PrototypeWeaponHud _weaponHud;
        private PrototypeHealthHud _healthHud;
        private PrototypeRoomAdvanceController _roomAdvanceController;
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
            if (_chaserPrefab != null)
            {
                Object.DestroyImmediate(_chaserPrefab);
            }
            if (_chargerPrefab != null)
            {
                Object.DestroyImmediate(_chargerPrefab);
            }
            if (_chargerTelegraphCellPrefab != null)
            {
                Object.DestroyImmediate(_chargerTelegraphCellPrefab);
            }
            if (_armoredPrefab != null)
            {
                Object.DestroyImmediate(_armoredPrefab);
            }
            if (_armoredPanicTelegraphCellPrefab != null)
            {
                Object.DestroyImmediate(_armoredPanicTelegraphCellPrefab);
            }
            if (_selfDestructPrefab != null)
            {
                Object.DestroyImmediate(_selfDestructPrefab);
            }
            if (_selfDestructTelegraphCellPrefab != null)
            {
                Object.DestroyImmediate(_selfDestructTelegraphCellPrefab);
            }
            if (_throwerPrefab != null)
            {
                Object.DestroyImmediate(_throwerPrefab);
            }
            if (_throwerTelegraphCellPrefab != null)
            {
                Object.DestroyImmediate(_throwerTelegraphCellPrefab);
            }
            if (_bossPrefab != null)
            {
                Object.DestroyImmediate(_bossPrefab);
            }
            if (_bossDangerCellPrefab != null)
            {
                Object.DestroyImmediate(_bossDangerCellPrefab);
            }
            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
            }
            if (_areaDefinition != null)
            {
                Object.DestroyImmediate(_areaDefinition);
            }
            if (_loadout != null)
            {
                Object.DestroyImmediate(_loadout);
            }
            if (_vitals != null)
            {
                Object.DestroyImmediate(_vitals);
            }
            if (_chaserDefinition != null)
            {
                Object.DestroyImmediate(_chaserDefinition);
            }
            if (_chargerDefinition != null)
            {
                Object.DestroyImmediate(_chargerDefinition);
            }
            if (_armoredDefinition != null)
            {
                Object.DestroyImmediate(_armoredDefinition);
            }
            if (_selfDestructBombDefinition != null)
            {
                Object.DestroyImmediate(_selfDestructBombDefinition);
            }
            if (_selfDestructDefinition != null)
            {
                Object.DestroyImmediate(_selfDestructDefinition);
            }
            if (_throwerBombDefinition != null)
            {
                Object.DestroyImmediate(_throwerBombDefinition);
            }
            if (_throwerDefinition != null)
            {
                Object.DestroyImmediate(_throwerDefinition);
            }
            if (_bossDefinition != null)
            {
                Object.DestroyImmediate(_bossDefinition);
            }
            if (_bossThrowBombDefinition != null)
            {
                Object.DestroyImmediate(_bossThrowBombDefinition);
            }
            if (_bossChainBombDefinition != null)
            {
                Object.DestroyImmediate(_bossChainBombDefinition);
            }
            if (_roomDefinition != null)
            {
                Object.DestroyImmediate(_roomDefinition);
            }
            if (_playerMaterial != null)
            {
                Object.DestroyImmediate(_playerMaterial);
            }
            if (_chaserMaterial != null)
            {
                Object.DestroyImmediate(_chaserMaterial);
            }
            if (_chargerMaterial != null)
            {
                Object.DestroyImmediate(_chargerMaterial);
            }
            if (_chargerTelegraphCellMaterial != null)
            {
                Object.DestroyImmediate(_chargerTelegraphCellMaterial);
            }
            if (_armoredMaterial != null)
            {
                Object.DestroyImmediate(_armoredMaterial);
            }
            if (_armoredPanicTelegraphCellMaterial != null)
            {
                Object.DestroyImmediate(_armoredPanicTelegraphCellMaterial);
            }
            if (_selfDestructMaterial != null)
            {
                Object.DestroyImmediate(_selfDestructMaterial);
            }
            if (_selfDestructTelegraphCellMaterial != null)
            {
                Object.DestroyImmediate(_selfDestructTelegraphCellMaterial);
            }
            if (_throwerMaterial != null)
            {
                Object.DestroyImmediate(_throwerMaterial);
            }
            if (_throwerTelegraphCellMaterial != null)
            {
                Object.DestroyImmediate(_throwerTelegraphCellMaterial);
            }
            if (_bossMaterial != null)
            {
                Object.DestroyImmediate(_bossMaterial);
            }
            if (_bossDangerCellMaterial != null)
            {
                Object.DestroyImmediate(_bossDangerCellMaterial);
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
        public void PlayerAnimationPresenter_ConfigureAssignsSessionAndAnimator()
        {
            var root = new GameObject("PlayerAnimatorConfigureTest");
            root.SetActive(false);
            try
            {
                PrototypeGameSession session = root.AddComponent<PrototypeGameSession>();
                PrototypePlayerAnimationPresenter presenter =
                    root.AddComponent<PrototypePlayerAnimationPresenter>();
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform, false);
                Animator playerAnimator = player.AddComponent<Animator>();

                presenter.Configure(session, playerAnimator);

                Assert.That(presenter.Session, Is.SameAs(session));
                Assert.That(presenter.Animator, Is.SameAs(playerAnimator));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator RoomAuthority_DrivesSandboxGridAndAuthoredSpawns()
        {
            CreateRuntime(new Vector2Int(0, 1), true);

            yield return null;

            Assert.That(_session.Context.RoomDefinition, Is.SameAs(_roomDefinition));
            Assert.That(_session.Context.GridWidth, Is.EqualTo(5));
            Assert.That(_session.Context.GridDepth, Is.EqualTo(5));
            Assert.That(_session.Context.BlockedCells.Count, Is.EqualTo(1));
            Assert.That(_session.Context.BlockedCells[0], Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(
                _session.Context.GridSpace.WorldToGrid(_session.Context.PlayerSpawn.position),
                Is.EqualTo(_roomDefinition.CreateCoreDefinition().PlayerSpawn));
            Assert.That(
                _session.Context.GridSpace.WorldToGrid(_session.Context.ChaserSpawn.position),
                Is.EqualTo(_roomDefinition.CreateCoreDefinition().ChaserSpawn));
        }

        [UnityTest]
        public IEnumerator SafePlaceholderRoom_ReusesMovementBombsAndVitalsWithoutEnemies()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 1f,
                combatEnabled: false);

            yield return null;

            Assert.That(_session.IsInitialized, Is.True);
            Assert.That(_session.HasChaser, Is.False);
            Assert.That(_session.IsChaserAlive, Is.False);
            Assert.That(_session.HasCharger, Is.False);
            Assert.That(_session.HasArmored, Is.False);
            Assert.That(_session.EnemyActiveCount, Is.Zero);
            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(
                _session.GetCell(_roomDefinition.CreateCoreDefinition().ChaserSpawn).Occupancy,
                Is.EqualTo(GridOccupancy.None));

            QueueKeyboardState(Key.W);
            yield return null;
            Assert.That(_session.CurrentMovementPosition.Z, Is.GreaterThan(0d));

            QueueKeyboardState();
            PressAndRelease(Key.Z);
            Assert.That(_session.ActiveBombCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Pause_FreezesMovementFuseAndActionsUntilResume()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.15f,
                combatEnabled: false);

            yield return null;

            PrototypePausePresenter pausePresenter =
                _root.GetComponent<PrototypePausePresenter>();
            Assert.That(pausePresenter, Is.Not.Null);
            Assert.That(pausePresenter.Session, Is.SameAs(_session));
            Assert.That(pausePresenter.IsVisible, Is.False);

            var pauseStates = new List<bool>();
            _session.PauseStateChanged += pauseStates.Add;
            PressAndRelease(Key.Z);
            Assert.That(_session.ActiveBombCount, Is.EqualTo(1));

            PressAndRelease(Key.Escape);
            yield return null;

            Assert.That(_session.IsPaused, Is.True);
            Assert.That(pausePresenter.IsVisible, Is.True);
            Assert.That(pausePresenter.ShowCount, Is.EqualTo(1));
            Assert.That(pausePresenter.StatusText, Does.Contain("RESUME"));
            GridSubcellPosition pausedPosition = _session.CurrentMovementPosition;
            int pausedSlot = _session.ActiveBombSlotIndex;

            QueueKeyboardState(Key.W);
            yield return new WaitForSecondsRealtime(0.25f);

            Assert.That(_session.CurrentMovementPosition, Is.EqualTo(pausedPosition));
            Assert.That(_session.ActiveBombCount, Is.EqualTo(1));
            Assert.That(_session.ActiveBombSlotIndex, Is.EqualTo(pausedSlot));

            QueueKeyboardState(Key.W, Key.Z, Key.X);
            QueueKeyboardState(Key.W);
            Assert.That(_session.ActiveBombCount, Is.EqualTo(1));
            Assert.That(_session.ActiveBombSlotIndex, Is.EqualTo(pausedSlot));

            QueueKeyboardState(Key.W, Key.Escape);
            QueueKeyboardState(Key.W);
            yield return null;

            Assert.That(_session.IsPaused, Is.False);
            Assert.That(pausePresenter.IsVisible, Is.False);
            Assert.That(pausePresenter.HideCount, Is.EqualTo(1));
            Assert.That(
                _session.CurrentMovementPosition.Z,
                Is.GreaterThan(pausedPosition.Z));
            Assert.That(pauseStates, Is.EqualTo(new[] { true, false }));

            QueueKeyboardState();
            float deadline = Time.realtimeSinceStartup + 1f;
            while (_session.ActiveBombCount > 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(_session.ActiveBombCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RuntimeRoomPreparation_OverridesPlayerStartBeforeAwakeOnly()
        {
            var runtimeStart = new GridPosition(1, 0);
            CreateRuntime(
                Vector2Int.zero,
                false,
                runtimePlayerStart: runtimeStart);

            yield return null;

            Assert.That(_session.CurrentGridPosition, Is.EqualTo(runtimeStart));
            Assert.That(_player.position.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                _session.GetCell(runtimeStart).Occupancy,
                Is.EqualTo(GridOccupancy.Actor));
            Assert.Throws<System.InvalidOperationException>(() =>
                _session.PrepareRuntimeRoom(
                    _roomDefinition.CreateCoreDefinition(),
                    new GridPosition(0, 0)));
        }

        [UnityTest]
        public IEnumerator RuntimeRoomPreparation_DisablesAuthoredCombatForClearedVisit()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeChaserPresenter: true,
                combatEnabled: true,
                runtimeCombatEnabled: false);

            yield return null;

            Assert.That(_session.IsCombatEnabledByDefault, Is.True);
            Assert.That(_session.HasChaser, Is.False);
            Assert.That(_session.IsChaserAlive, Is.False);
            Assert.That(_session.EnemyActiveCount, Is.Zero);
            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(_chaserPresenter.IsInitialized, Is.True);
            Assert.That(_chaserPresenter.Instance, Is.Null);
            Assert.That(
                _session.GetCell(_roomDefinition.CreateCoreDefinition().ChaserSpawn).Occupancy,
                Is.EqualTo(GridOccupancy.None));
        }

        [Test]
        public void RuntimeRoomPreparation_CannotEnableCombatInAuthoredSafeRoom()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                combatEnabled: false);

            Assert.Throws<System.InvalidOperationException>(() =>
                _session.PrepareRuntimeRoom(
                    _roomDefinition.CreateCoreDefinition(),
                    _roomDefinition.CreateCoreDefinition().PlayerSpawn,
                    true));
        }

        [UnityTest]
        public IEnumerator HeldDirection_AdvancesSubcellPositionAndPresentationEveryFrame()
        {
            CreateRuntime(Vector2Int.zero, false);
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;

            Assert.That(_session.CurrentMovementPosition.Z, Is.GreaterThan(0d));
            Assert.That(_player.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(_player.position.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(
                _player.position.z,
                Is.EqualTo((float)_session.CurrentMovementPosition.Z).Within(0.001f));

            QueueKeyboardState();
        }

        [UnityTest]
        public IEnumerator ReleasedDirection_StopsPresentationWithoutFinishingCommittedCell()
        {
            CreateRuntime(Vector2Int.zero, false);
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();
            float releasedPosition = _player.position.z;

            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(
                _player.position.z,
                Is.EqualTo(releasedPosition).Within(0.02f),
                "Releasing movement should stop the authoritative presentation on the next frame.");
        }

        [UnityTest]
        public IEnumerator OverlappingPerpendicularInput_ChangesMotionOnConsecutiveFrames()
        {
            CreateRuntime(Vector2Int.zero, false);
            var directions = new List<CardinalDirection>();
            _session.PlayerPositionChanged += (_, direction) =>
            {
                if (directions.Count == 0 || directions[directions.Count - 1] != direction)
                {
                    directions.Add(direction);
                }
            };
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState(Key.W, Key.D);
            yield return null;
            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();

            Assert.That(
                directions,
                Has.Count.GreaterThanOrEqualTo(3),
                "Each held direction change should affect motion on the next observed frame.");
            Assert.That(directions[0], Is.EqualTo(CardinalDirection.North));
            Assert.That(directions[1], Is.EqualTo(CardinalDirection.East));
            Assert.That(directions[2], Is.EqualTo(CardinalDirection.North));
            Assert.That(_session.CurrentMovementPosition.X, Is.GreaterThan(0d));
            Assert.That(_session.CurrentMovementPosition.Z, Is.GreaterThan(0d));
        }

        [UnityTest]
        public IEnumerator HeldDiagonal_RemainsOnLatestPressedAxisAcrossFrames()
        {
            CreateRuntime(Vector2Int.zero, false);
            yield return null;

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState(Key.W, Key.D);
            yield return null;

            double eastStart = _session.CurrentMovementPosition.X;
            double northStart = _session.CurrentMovementPosition.Z;
            yield return null;
            yield return null;
            yield return null;

            Assert.That(_session.CurrentMovementPosition.X, Is.GreaterThan(eastStart));
            Assert.That(
                _session.CurrentMovementPosition.Z,
                Is.EqualTo(northStart).Within(0.000001d),
                "Holding the unchanged diagonal must keep moving east instead of alternating axes each frame.");

            QueueKeyboardState();
        }

        [UnityTest]
        public IEnumerator RapidAlternatingTaps_ApplyEachDirectionOnTheNextFrame()
        {
            CreateRuntime(Vector2Int.zero, false);
            var directions = new List<CardinalDirection>();
            _session.PlayerPositionChanged += (_, direction) =>
            {
                if (directions.Count == 0 || directions[directions.Count - 1] != direction)
                {
                    directions.Add(direction);
                }
            };
            yield return null;

            Key[] keys = { Key.W, Key.D, Key.W, Key.D, Key.W, Key.D };
            for (int index = 0; index < keys.Length; index++)
            {
                QueueKeyboardState(keys[index]);
                yield return null;
                QueueKeyboardState();
                yield return null;
            }

            Assert.That(directions, Has.Count.GreaterThanOrEqualTo(keys.Length));
            for (int index = 0; index < keys.Length; index++)
            {
                CardinalDirection expected = index % 2 == 0
                    ? CardinalDirection.North
                    : CardinalDirection.East;
                Assert.That(directions[index], Is.EqualTo(expected));
            }
        }

        [UnityTest]
        public IEnumerator RapidAlternatingSubframeTaps_MoveOnEveryFollowingFrame()
        {
            CreateRuntime(Vector2Int.zero, false);
            var directions = new List<CardinalDirection>();
            _session.PlayerPositionChanged += (_, direction) =>
            {
                if (directions.Count == 0 || directions[directions.Count - 1] != direction)
                {
                    directions.Add(direction);
                }
            };
            yield return null;

            Key[] keys = { Key.W, Key.D, Key.W, Key.D, Key.W, Key.D };
            for (int index = 0; index < keys.Length; index++)
            {
                QueueKeyboardState(keys[index]);
                QueueKeyboardState();
                yield return null;
            }
            yield return null;

            Assert.That(directions, Has.Count.GreaterThanOrEqualTo(keys.Length));
            for (int index = 0; index < keys.Length; index++)
            {
                CardinalDirection expected = index % 2 == 0
                    ? CardinalDirection.North
                    : CardinalDirection.East;
                Assert.That(directions[index], Is.EqualTo(expected));
            }

            GridSubcellPosition stopped = _session.CurrentMovementPosition;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(_session.CurrentMovementPosition, Is.EqualTo(stopped));
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
            Assert.That(placed.OwnerId.IsValid, Is.True);
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
            Assert.That(exploded.OwnerId, Is.EqualTo(placed.OwnerId));
            Assert.That(exploded.Origin, Is.EqualTo(origin));
            Assert.That(exploded.AffectedCells, Has.Count.EqualTo(5));
            Assert.That(_session.ActiveBombCount, Is.Zero);
            Assert.That(_session.GetCell(origin).HasActor, Is.True);
            Assert.That(_session.GetCell(origin).HasBomb, Is.False);
        }

        [UnityTest]
        public IEnumerator BombOwner_CanExitPlacementCellOnceButCannotReenter()
        {
            CreateRuntime(Vector2Int.zero, false, fuseSeconds: 1f);
            yield return null;

            PressAndRelease(Key.Z);
            Assert.That(_session.HasPlayerBombPassThrough, Is.True);

            QueueKeyboardState(Key.W);
            yield return new WaitForSecondsRealtime(0.12f);
            QueueKeyboardState();

            var origin = new GridPosition(0, 0);
            var north = new GridPosition(0, 1);
            Assert.That(_session.CurrentGridPosition, Is.EqualTo(north));
            Assert.That(_session.HasPlayerBombPassThrough, Is.False);
            Assert.That(_session.GetCell(origin).HasBomb, Is.True);

            QueueKeyboardState(Key.S);
            yield return new WaitForSecondsRealtime(0.12f);
            QueueKeyboardState();

            Assert.That(_session.CurrentGridPosition, Is.EqualTo(north));
            Assert.That(_player.position.z, Is.GreaterThanOrEqualTo(0.5f));
        }

        [UnityTest]
        public IEnumerator SwapInput_SelectsSecondDefinitionAndKeepsSlotCooldownsIndependent()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 2f,
                placementCooldownSeconds: 1f,
                areaPlacementCooldownSeconds: 0.5f,
                swapCooldownSeconds: 0.05f);
            var placed = new List<BombSnapshot>();
            _session.BombPlaced += snapshot => placed.Add(snapshot);
            yield return null;

            PressAndRelease(Key.Z);
            QueueKeyboardState(Key.W);
            yield return new WaitForSecondsRealtime(0.12f);
            QueueKeyboardState();
            PressAndRelease(Key.X);
            PressAndRelease(Key.Z);

            Assert.That(_session.ActiveBombSlotIndex, Is.EqualTo(1));
            Assert.That(placed, Has.Count.EqualTo(2));
            Assert.That(placed[0].DefinitionId.Value, Is.EqualTo("test-cross"));
            Assert.That(placed[1].DefinitionId.Value, Is.EqualTo("test-area"));
            Assert.That(_session.GetBombSlot(0).IsReady, Is.False);
            Assert.That(_session.GetBombSlot(1).IsReady, Is.False);

            PressAndRelease(Key.X);
            Assert.That(
                _session.ActiveBombSlotIndex,
                Is.EqualTo(1),
                "Swap cooldown must reject an immediate second swap.");

            yield return new WaitForSecondsRealtime(0.06f);
            PressAndRelease(Key.X);
            Assert.That(_session.ActiveBombSlotIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AreaBomb_UsesTheAuthoredThreeByThreeExplosionPattern()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.05f,
                swapCooldownSeconds: 0.01f);
            BombExplosion exploded = null;
            _session.BombExploded += explosion =>
            {
                if (explosion.DefinitionId.Value == "test-area")
                {
                    exploded = explosion;
                }
            };
            yield return null;

            PressAndRelease(Key.X);
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(exploded, Is.Not.Null);
            Assert.That(exploded.DefinitionId.Value, Is.EqualTo("test-area"));
            Assert.That(exploded.AffectedCells, Has.Count.EqualTo(9));
            Assert.That(exploded.AffectedCells, Has.Member(new GridPosition(1, 1)));
            Assert.That(exploded.AffectedCells, Has.Member(new GridPosition(-1, -1)));
        }

        [UnityTest]
        public IEnumerator ForwardLineBomb_UsesReleasedFacingAndKeepsPlacementDirection()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.12f,
                areaExplosionRange: 2,
                swapCooldownSeconds: 0.01f,
                secondExplosionShape: BombExplosionShape.ForwardLine,
                secondDefinitionId: "test-line",
                combatEnabled: false);
            BombSnapshot placed = default;
            BombExplosion exploded = null;
            _session.BombPlaced += snapshot =>
            {
                if (snapshot.DefinitionId.Value == "test-line")
                {
                    placed = snapshot;
                }
            };
            _session.BombExploded += explosion =>
            {
                if (explosion.DefinitionId.Value == "test-line")
                {
                    exploded = explosion;
                }
            };
            yield return null;

            QueueKeyboardState(Key.D);
            yield return null;
            QueueKeyboardState();
            yield return null;
            PressAndRelease(Key.X);
            PressAndRelease(Key.Z);

            Assert.That(placed.Id.IsValid, Is.True);
            Assert.That(placed.PlacementDirection, Is.EqualTo(CardinalDirection.East));

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(exploded, Is.Not.Null);
            Assert.That(exploded.PlacementDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(exploded.AffectedCells, Is.EqualTo(new[]
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(2, 0),
            }));
            Assert.That(exploded.AffectedCells, Has.No.Member(new GridPosition(0, 1)));
            Assert.That(exploded.AffectedCells, Has.No.Member(new GridPosition(-1, 0)));
        }

        [UnityTest]
        public IEnumerator DestructibleWallPresenter_RemovesConfirmedWallAndOpensGridCell()
        {
            var wall = new Vector2Int(1, 0);
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.05f,
                destructibleWalls: new[] { wall },
                includeDestructibleWallPresenter: true);
            BombExplosion exploded = null;
            _session.BombExploded += explosion =>
            {
                if (explosion.DefinitionId.Value == "test-area")
                {
                    exploded = explosion;
                }
            };
            yield return null;

            var coreWall = new GridPosition(wall.x, wall.y);
            Assert.That(_session.GetCell(coreWall).Terrain, Is.EqualTo(GridTerrain.DestructibleWall));
            Assert.That(_destructibleWallPresenter.HasWallVisual(coreWall), Is.True);

            PressAndRelease(Key.X);
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(exploded, Is.Not.Null);
            Assert.That(exploded.DestroyedWalls, Has.Member(coreWall));
            Assert.That(_session.GetCell(coreWall).Terrain, Is.EqualTo(GridTerrain.Floor));
            Assert.That(_destructibleWallPresenter.HasWallVisual(coreWall), Is.False);
            Assert.That(_destructibleWallPresenter.ActiveWallVisualCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator WeaponHud_ReflectsSuccessfulSwapAndCoreCooldownSnapshots()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeWeaponHud: true,
                placementCooldownSeconds: 0.5f,
                swapCooldownSeconds: 0.05f);
            yield return null;

            Assert.That(_weaponHud.IsInitialized, Is.True);
            Assert.That(_weaponHud.DisplayedActiveSlotIndex, Is.Zero);
            Assert.That(_weaponHud.FirstSlotReadyFraction, Is.EqualTo(1f));

            PressAndRelease(Key.Z);
            PressAndRelease(Key.X);
            yield return null;

            Assert.That(_weaponHud.DisplayedActiveSlotIndex, Is.EqualTo(1));
            Assert.That(_weaponHud.FirstSlotReadyFraction, Is.LessThan(1f));
            Assert.That(_weaponHud.SecondSlotReadyFraction, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator HealthHud_ReflectsPlayerDamageAndHidesBossOutsideBossRoom()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeHealthHud: true,
                fuseSeconds: 0.08f);
            yield return null;

            Assert.That(_healthHud.IsInitialized, Is.True);
            Assert.That(_healthHud.DisplayedPlayerHealth, Is.EqualTo(5));
            Assert.That(_healthHud.DisplayedPlayerMaxHealth, Is.EqualTo(5));
            Assert.That(_healthHud.PlayerHealthFillFraction, Is.EqualTo(1f));
            Assert.That(_healthHud.PlayerHealthText, Does.Contain("5 / 5"));
            Assert.That(_healthHud.IsBossPanelVisible, Is.False);
            Assert.That(_healthHud.DisplayedCombatRewardTokenCount, Is.Zero);
            Assert.That(_healthHud.CombatRewardText, Is.EqualTo("ROOM TOKENS  0"));

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_healthHud.DisplayedPlayerHealth, Is.EqualTo(4));
            Assert.That(_healthHud.PlayerHealthFillFraction, Is.EqualTo(0.8f));
            Assert.That(_healthHud.PlayerHealthText, Does.Contain("4 / 5"));
            Assert.That(_healthHud.IsBossPanelVisible, Is.False);
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
        public IEnumerator SelfExplosion_AppliesOneDamageAndNextExplosionDuringInvulnerabilityIsIgnored()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.08f,
                invulnerabilitySeconds: 0.75f);
            PlayerDamageResult applied = default;
            int damageEventCount = 0;
            _session.PlayerDamaged += result =>
            {
                applied = result;
                damageEventCount++;
            };
            yield return null;

            Assert.That(_session.CurrentHealth, Is.EqualTo(5));
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(applied.WasApplied, Is.True);
            Assert.That(applied.PreviousHealth, Is.EqualTo(5));
            Assert.That(applied.CurrentHealth, Is.EqualTo(4));
            Assert.That(_session.CurrentHealth, Is.EqualTo(4));
            Assert.That(_session.IsPlayerInvulnerable, Is.True);

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(_session.CurrentHealth, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator HealthPresenter_TracksHealthAndUsesPropertyBlockDamagePulse()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeHealthPresenter: true,
                fuseSeconds: 0.08f,
                healthDamagePulseSeconds: 0.4f);
            yield return null;
            Color normalColor = _healthPresenter.CurrentColor;

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_healthPresenter.DisplayedHealth, Is.EqualTo(4));
            Assert.That(_healthPresenter.DamagePulseCount, Is.EqualTo(1));
            Assert.That(_healthPresenter.CurrentColor, Is.Not.EqualTo(normalColor));
            Assert.That(_healthPresenter.IsDisplayingDeath, Is.False);

            yield return new WaitForSecondsRealtime(0.35f);
            int pulseFrameGuard = 0;
            while (_healthPresenter.CurrentColor != normalColor &&
                   pulseFrameGuard++ < 30)
            {
                yield return null;
            }

            Assert.That(_healthPresenter.CurrentColor, Is.EqualTo(normalColor));
            Assert.That(_playerMaterial.color, Is.EqualTo(normalColor));
        }

        [UnityTest]
        public IEnumerator FatalSelfExplosion_PublishesDeathOnceAndStopsConsumingCommands()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.08f,
                maxHealth: 1);
            int deathEventCount = 0;
            _session.PlayerDied += _ => deathEventCount++;
            yield return null;

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_session.IsPlayerDead, Is.True);
            Assert.That(_session.CurrentHealth, Is.Zero);
            Assert.That(deathEventCount, Is.EqualTo(1));

            QueueKeyboardState(Key.W);
            yield return null;
            QueueKeyboardState();
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_session.CurrentGridPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(_session.ActiveBombCount, Is.Zero);
            Assert.That(deathEventCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Chaser_UsesSharedGridAndPresenterInterpolatesFirstStep()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeChaserPresenter: true,
                chaserSpawnPosition: new Vector2Int(1, -1));

            yield return null;

            var expected = new GridPosition(1, 0);
            Assert.That(_session.ChaserActorId, Is.EqualTo(new ActorId(2)));
            Assert.That(_session.CurrentChaserGridPosition, Is.EqualTo(expected));
            Assert.That(_session.GetCell(expected).HasActor, Is.True);
            Assert.That(_session.EnemyActiveCount, Is.EqualTo(1));
            Assert.That(_chaserPresenter.MoveCount, Is.EqualTo(1));
            Assert.That(_chaserPresenter.IsEnemyVisible, Is.True);

            yield return new WaitForSecondsRealtime(0.55f);

            Assert.That(_chaserPresenter.Instance.transform.position.x, Is.EqualTo(1f).Within(0.02f));
            Assert.That(_chaserPresenter.Instance.transform.position.y, Is.EqualTo(0.45f).Within(0.02f));
            Assert.That(_chaserPresenter.Instance.transform.position.z, Is.EqualTo(0f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator Charger_TelegraphsThenMovesInLockedDirectionWithPresenterState()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeChargerPresenter: true,
                chargerSpawnPosition: new Vector2Int(0, 2),
                chargerTelegraphSeconds: 0.05f,
                chargerCellsPerSecond: 4f);

            yield return null;

            Assert.That(_session.HasCharger, Is.True);
            Assert.That(_session.ChargerActorId, Is.EqualTo(new ActorId(3)));
            Assert.That(_session.EnemyActiveCount, Is.EqualTo(2));
            Assert.That(_session.CurrentChargerState, Is.EqualTo(ChargerEnemyState.Telegraph));
            Assert.That(_chargerPresenter.StateChangeCount, Is.EqualTo(1));
            Assert.That(_chargerPresenter.CurrentState, Is.EqualTo(ChargerEnemyState.Telegraph));
            Assert.That(_chargerPresenter.IsEnemyVisible, Is.True);
            Assert.That(_chargerPresenter.ActiveTelegraphCellCount, Is.EqualTo(4));

            yield return new WaitForSecondsRealtime(0.09f);

            Assert.That(_session.CurrentChargerState, Is.EqualTo(ChargerEnemyState.Charge));
            Assert.That(_session.CurrentChargerGridPosition, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(_chargerPresenter.MoveCount, Is.EqualTo(1));
            Assert.That(_chargerPresenter.CurrentState, Is.EqualTo(ChargerEnemyState.Charge));
            Assert.That(_chargerPresenter.ActiveTelegraphCellCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Charger_AcquiresLaneBeforeTelegraphAndUsesTrackInterpolation()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeChargerPresenter: true,
                chaserSpawnPosition: new Vector2Int(-2, -2),
                chargerSpawnPosition: new Vector2Int(2, 2),
                chargerLaneAcquireCellsPerSecond: 20f,
                chargerTelegraphSeconds: 0.5f,
                chargerCellsPerSecond: 8f);

            yield return new WaitForSecondsRealtime(0.18f);

            Assert.That(_session.CurrentChargerState, Is.EqualTo(ChargerEnemyState.Telegraph));
            Assert.That(
                _session.CurrentChargerGridPosition,
                Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(_chargerPresenter.MoveCount, Is.EqualTo(2));
            Assert.That(_chargerPresenter.ActiveTelegraphCellCount, Is.EqualTo(4));
            Assert.That(
                _chargerPresenter.Instance.transform.position.x,
                Is.EqualTo(2f).Within(0.02f));
            Assert.That(
                _chargerPresenter.Instance.transform.position.y,
                Is.EqualTo(0.45f).Within(0.02f));
            Assert.That(
                _chargerPresenter.Instance.transform.position.z,
                Is.EqualTo(0f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator ChargerImpact_DamagesPlayerOnceWithoutOverlappingTargetCell()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                chargerSpawnPosition: new Vector2Int(0, 2),
                chargerTelegraphSeconds: 0.02f,
                chargerCellsPerSecond: 20f,
                chargerRecoverSeconds: 0.3f);
            int chargerDamageCount = 0;
            PlayerDamageResult chargerDamage = default;
            _session.PlayerDamaged += result =>
            {
                if (result.SourceActorId == _session.ChargerActorId)
                {
                    chargerDamage = result;
                    chargerDamageCount++;
                }
            };

            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(chargerDamageCount, Is.EqualTo(1));
            Assert.That(chargerDamage.SourceKind, Is.EqualTo(PlayerDamageSourceKind.EnemyContact));
            Assert.That(chargerDamage.AppliedDamage, Is.EqualTo(1));
            Assert.That(_session.CurrentHealth, Is.EqualTo(4));
            Assert.That(_session.CurrentChargerState, Is.EqualTo(ChargerEnemyState.Recover));
            Assert.That(_session.CurrentChargerGridPosition, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(_session.GetCell(new GridPosition(0, 0)).HasActor, Is.True);
        }

        [UnityTest]
        public IEnumerator AreaExplosion_KillsBothEnemiesBeforePublishingRoomClear()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.05f,
                chaserSpawnPosition: new Vector2Int(1, 1),
                chargerSpawnPosition: new Vector2Int(-1, 1));
            var eventOrder = new List<string>();
            _session.EnemyDied += result => eventOrder.Add("dead-" + result.ActorId.Value);
            _session.RoomCleared += () => eventOrder.Add("clear");
            yield return null;

            PressAndRelease(Key.X);
            Assert.That(_session.TryPlaceBomb(), Is.True);
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(_session.EnemyActiveCount, Is.Zero);
            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(eventOrder, Is.EqualTo(new[] { "dead-2", "dead-3", "clear" }));
        }

        [UnityTest]
        public IEnumerator ArmoredEnemy_RequiresTwoExplosionsAndPublishesVisiblePhaseChange()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.05f,
                areaExplosionRange: 2,
                chaserSpawnPosition: new Vector2Int(2, -2),
                armoredSpawnPosition: new Vector2Int(0, 2),
                includeArmoredPresenter: true,
                armoredCellsPerSecond: 0.2f,
                brokenCellsPerSecond: 3f);
            var eventOrder = new List<string>();
            _session.ArmoredStateChanged += result =>
                eventOrder.Add("armor-" + result.CurrentState.ToString().ToLowerInvariant());
            _session.EnemyDied += result => eventOrder.Add("dead-" + result.ActorId.Value);
            _session.RoomCleared += () => eventOrder.Add("clear");
            yield return null;

            Assert.That(_session.HasArmored, Is.True);
            Assert.That(_session.ArmoredActorId, Is.EqualTo(new ActorId(4)));
            Assert.That(_session.CurrentArmoredState, Is.EqualTo(ArmoredEnemyState.Armored));
            Assert.That(_session.EnemyActiveCount, Is.EqualTo(2));
            Assert.That(_armoredPresenter.IsEnemyVisible, Is.True);
            Color armoredColor = _armoredPresenter.CurrentColor;
            Vector3 armoredScale = _armoredPresenter.Instance.transform.localScale;

            PressAndRelease(Key.X);
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(_session.CurrentArmoredState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(
                _session.CurrentArmoredBehaviorState,
                Is.EqualTo(ArmoredEnemyBehaviorState.PanicTelegraph));
            Assert.That(_session.IsArmoredAlive, Is.True);
            Assert.That(_session.EnemyActiveCount, Is.EqualTo(1));
            Assert.That(_session.IsRoomCleared, Is.False);
            Assert.That(_armoredPresenter.StateChangeCount, Is.EqualTo(1));
            Assert.That(_armoredPresenter.DeathCount, Is.Zero);
            Assert.That(_armoredPresenter.ActivePanicTelegraphCellCount, Is.GreaterThan(0));
            Assert.That(
                _armoredPresenter.CurrentBehaviorState,
                Is.EqualTo(ArmoredEnemyBehaviorState.PanicTelegraph));
            Assert.That(_armoredPresenter.CurrentColor, Is.Not.EqualTo(armoredColor));
            Assert.That(
                _armoredPresenter.Instance.transform.localScale.y,
                Is.LessThan(armoredScale.y));
            Assert.That(eventOrder, Is.EqualTo(new[] { "armor-broken", "dead-2" }));

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(_session.CurrentArmoredState, Is.EqualTo(ArmoredEnemyState.Dead));
            Assert.That(_session.IsArmoredAlive, Is.False);
            Assert.That(_session.EnemyActiveCount, Is.Zero);
            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(_armoredPresenter.StateChangeCount, Is.EqualTo(2));
            Assert.That(_armoredPresenter.DeathCount, Is.EqualTo(1));
            Assert.That(_armoredPresenter.ActivePanicTelegraphCellCount, Is.Zero);
            Assert.That(
                eventOrder,
                Is.EqualTo(new[]
                {
                    "armor-broken",
                    "dead-2",
                    "armor-dead",
                    "dead-4",
                    "clear",
                }));
        }

        [UnityTest]
        public IEnumerator ArmoredEnemy_TelegraphsLockedPanicPathRunsAndRecoversIntoChase()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.05f,
                areaExplosionRange: 2,
                chaserSpawnPosition: new Vector2Int(2, -2),
                armoredSpawnPosition: new Vector2Int(0, 2),
                includeArmoredPresenter: true,
                armoredCellsPerSecond: 0.2f,
                brokenCellsPerSecond: 0.4f,
                armoredPanicTelegraphSeconds: 0.05f,
                armoredPanicCellsPerSecond: 20f,
                armoredPanicRecoverSeconds: 0.05f);
            var behaviorOrder = new List<ArmoredEnemyBehaviorState>();
            GridPosition finalPanicPosition = default;
            int panicMoveCount = 0;
            _session.ArmoredAdvanced += result =>
            {
                if (result.HasStateTransition)
                {
                    behaviorOrder.Add(result.State);
                }
                if (result.HasMovement &&
                    result.PreviousState == ArmoredEnemyBehaviorState.PanicRun)
                {
                    finalPanicPosition = result.Movement.To;
                    panicMoveCount++;
                }
            };
            yield return null;

            PressAndRelease(Key.X);
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.075f);

            Assert.That(_session.CurrentArmoredState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(_armoredPresenter.ActivePanicTelegraphCellCount, Is.GreaterThan(0));

            yield return new WaitForSecondsRealtime(0.25f);

            Assert.That(
                _session.CurrentArmoredBehaviorState,
                Is.EqualTo(ArmoredEnemyBehaviorState.Chase));
            Assert.That(finalPanicPosition, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(panicMoveCount, Is.EqualTo(2));
            Assert.That(_session.CurrentArmoredGridPosition, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(_armoredPresenter.MoveCount, Is.EqualTo(3));
            Assert.That(_armoredPresenter.ActivePanicTelegraphCellCount, Is.Zero);
            Assert.That(
                behaviorOrder,
                Is.EqualTo(new[]
                {
                    ArmoredEnemyBehaviorState.PanicRun,
                    ArmoredEnemyBehaviorState.PanicRecover,
                    ArmoredEnemyBehaviorState.Chase,
                }));
        }

        [UnityTest]
        public IEnumerator Explosion_KillsChaserRemovesOccupancyAndClearsRoomOnce()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeChaserPresenter: true,
                fuseSeconds: 0.08f,
                chaserDeathVisualSeconds: 0.4f,
                chaserSpawnPosition: new Vector2Int(1, -1));
            int damagedCount = 0;
            int diedCount = 0;
            int roomClearedCount = 0;
            EnemyDamageResult fatal = default;
            _session.EnemyDamaged += result => damagedCount++;
            _session.EnemyDied += result =>
            {
                fatal = result;
                diedCount++;
            };
            _session.RoomCleared += () => roomClearedCount++;
            yield return null;
            GridPosition occupiedCell = _session.CurrentChaserGridPosition;
            Color normalColor = _chaserMaterial.color;

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(damagedCount, Is.EqualTo(1));
            Assert.That(diedCount, Is.EqualTo(1));
            Assert.That(roomClearedCount, Is.EqualTo(1));
            Assert.That(fatal.ActorId, Is.EqualTo(_session.ChaserActorId));
            Assert.That(fatal.WasFatal, Is.True);
            Assert.That(_session.EnemyActiveCount, Is.Zero);
            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(_session.GetCell(occupiedCell).HasActor, Is.False);
            Assert.That(_chaserPresenter.DeathCount, Is.EqualTo(1));
            Assert.That(_chaserPresenter.IsEnemyVisible, Is.True);
            Assert.That(_chaserPresenter.CurrentColor, Is.Not.EqualTo(normalColor));
            Assert.That(_chaserMaterial.color, Is.EqualTo(normalColor));

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(_chaserPresenter.IsEnemyVisible, Is.False);
            Assert.That(diedCount, Is.EqualTo(1));
            Assert.That(roomClearedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RoomClear_WithNextScene_SchedulesOneDelayedTransition()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.08f,
                chaserSpawnPosition: new Vector2Int(1, -1),
                includeRoomAdvanceController: true,
                nextSceneName: "UnusedPlayModeTarget",
                roomTransitionDelaySeconds: 10f);
            yield return null;

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(_roomAdvanceController.IsTransitionPending, Is.True);
            Assert.That(_roomAdvanceController.NextSceneName, Is.EqualTo("UnusedPlayModeTarget"));
            Assert.That(_roomAdvanceController.TransitionDelaySeconds, Is.EqualTo(10f));

            yield return null;
            Assert.That(_roomAdvanceController.IsTransitionPending, Is.True);
        }

        [UnityTest]
        public IEnumerator RoomClear_WithoutNextScene_RemainsInFinalRoom()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.08f,
                chaserSpawnPosition: new Vector2Int(1, -1),
                includeRoomAdvanceController: true,
                nextSceneName: string.Empty);
            yield return null;

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(_roomAdvanceController.IsTransitionPending, Is.False);
        }

        [UnityTest]
        public IEnumerator ChaserContact_WaitsForStepArrivalAndUsesSharedInvulnerability()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeHealthPresenter: true,
                invulnerabilitySeconds: 0.2f,
                healthDamagePulseSeconds: 0.4f,
                chaserSpawnPosition: new Vector2Int(1, -1));
            int contactDamageCount = 0;
            PlayerDamageResult firstContact = default;
            _session.PlayerDamaged += result =>
            {
                if (result.SourceKind == PlayerDamageSourceKind.EnemyContact)
                {
                    if (contactDamageCount == 0)
                    {
                        firstContact = result;
                    }
                    contactDamageCount++;
                }
            };

            yield return null;

            Assert.That(_session.CurrentChaserGridPosition, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(contactDamageCount, Is.Zero);
            Assert.That(_session.CurrentHealth, Is.EqualTo(5));
            Assert.That(_healthPresenter.DamagePulseCount, Is.Zero);

            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(contactDamageCount, Is.Zero);
            Assert.That(_session.CurrentHealth, Is.EqualTo(5));

            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(contactDamageCount, Is.EqualTo(1));
            Assert.That(firstContact.SourceActorId, Is.EqualTo(_session.ChaserActorId));
            Assert.That(firstContact.AppliedDamage, Is.EqualTo(1));
            Assert.That(_session.CurrentHealth, Is.EqualTo(4));
            Assert.That(_healthPresenter.DamagePulseCount, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(contactDamageCount, Is.EqualTo(1));
            Assert.That(_session.CurrentHealth, Is.EqualTo(4));

            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(contactDamageCount, Is.EqualTo(2));
            Assert.That(_session.CurrentHealth, Is.EqualTo(3));
            Assert.That(_healthPresenter.DamagePulseCount, Is.EqualTo(2));
            Assert.That(_playerMaterial.color, Is.Not.EqualTo(_healthPresenter.CurrentColor));
        }

        [UnityTest]
        public IEnumerator ExplosionDeath_RemovesChaserBeforeSameFrameContactDamage()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.001f,
                invulnerabilitySeconds: 0.05f,
                chaserSpawnPosition: new Vector2Int(1, -1));
            int explosionDamageCount = 0;
            int contactDamageCount = 0;
            _session.PlayerDamaged += result =>
            {
                if (result.SourceKind == PlayerDamageSourceKind.Explosion)
                {
                    explosionDamageCount++;
                }
                else if (result.SourceKind == PlayerDamageSourceKind.EnemyContact)
                {
                    contactDamageCount++;
                }
            };

            PressAndRelease(Key.Z);
            yield return null;

            Assert.That(_session.EnemyActiveCount, Is.Zero);
            Assert.That(_session.IsRoomCleared, Is.True);
            Assert.That(explosionDamageCount, Is.EqualTo(1));
            Assert.That(contactDamageCount, Is.Zero);
            Assert.That(_session.CurrentHealth, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator BossEncounter_OverheatAcceptsTwoBombsAndHidesDestinationGhost()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.03f,
                placementCooldownSeconds: 0.01f,
                areaPlacementCooldownSeconds: 0.01f,
                invulnerabilitySeconds: 0.75f,
                combatEnabled: true,
                bossEnabled: true,
                includePresenter: true,
                includeBossPresenter: true,
                retreatAnchors: new[]
                {
                    new Vector2Int(-2, -1),
                    new Vector2Int(-1, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(2, -1),
                },
                bossMaxHealth: 3,
                bossPhaseTwoHealthThreshold: 2,
                bossPhaseOneTelegraphSeconds: 0.02f,
                bossPhaseOneExecuteSeconds: 0.02f,
                bossPhaseOneRecoverySeconds: 1f,
                bossPhaseTwoTelegraphSeconds: 0.02f,
                bossPhaseTwoExecuteSeconds: 0.02f);
            int bossDamageCount = 0;
            _session.BossDamaged += _ => bossDamageCount++;

            Assert.That(_session.IsInitialized, Is.True);
            Assert.That(_session.HasBoss, Is.True);
            Assert.That(_session.HasChaser, Is.False);
            Assert.That(_session.BossActorId, Is.EqualTo(new ActorId(5)));
            Assert.That(_session.CurrentBossState, Is.EqualTo(BossBattleState.Telegraph));
            Assert.That(_session.EnemyActiveCount, Is.EqualTo(1));
            Assert.That(_bossPresenter.IsBossVisible, Is.True);
            Assert.That(_bossPresenter.IsMoveTargetVisible, Is.False);

            int frameGuard = 0;
            while ((_session.CurrentBossPattern != BossPatternKind.Overheat ||
                    _session.CurrentBossState != BossBattleState.Recovery) &&
                   frameGuard++ < 600)
            {
                yield return null;
            }
            Assert.That(frameGuard, Is.LessThan(600));
            Assert.That(_bossPresenter.CurrentPattern, Is.EqualTo(BossPatternKind.Overheat));

            PressAndRelease(Key.X);
            yield return null;
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.08f);
            PressAndRelease(Key.X);
            yield return null;
            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(_session.CurrentBossHealth, Is.EqualTo(1));
            Assert.That(bossDamageCount, Is.EqualTo(2));
            Assert.That(_session.IsRoomCleared, Is.False);
            Assert.That(_bossPresenter.DisplayedHealth, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BossEncounter_ExplosionDuringTelegraphDamagesBossImmediately()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.03f,
                combatEnabled: true,
                bossEnabled: true,
                includeBossPresenter: true,
                includeHealthHud: true,
                bossMaxHealth: 3,
                bossPhaseTwoHealthThreshold: 2,
                bossPhaseOneTelegraphSeconds: 1.5f,
                bossPhaseOneExecuteSeconds: 0.02f,
                bossPhaseOneRecoverySeconds: 0.3f);
            int damagedCount = 0;
            _session.BossDamaged += _ => damagedCount++;

            Assert.That(_session.CurrentBossState, Is.EqualTo(BossBattleState.Telegraph));
            Assert.That(_session.IsBossAlive, Is.True);
            Assert.That(_healthHud.BossHealthText, Does.Not.Contain("VULNERABLE"));
            Assert.That(_healthHud.BossHealthText, Does.Not.Contain("BLOCKED"));

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(damagedCount, Is.EqualTo(1));
            Assert.That(_session.CurrentBossHealth, Is.EqualTo(2));
            Assert.That(_bossPresenter.DamageCount, Is.EqualTo(1));
            Assert.That(_bossPresenter.DisplayedHealth, Is.EqualTo(2));
            Assert.That(_healthHud.DisplayedBossHealth, Is.EqualTo(2));
            Assert.That(_healthHud.BossHealthText, Does.Contain("2 / 3"));
        }

        [UnityTest]
        public IEnumerator BossEncounter_AttackDamageReportsBossPatternSource()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                invulnerabilitySeconds: 0.01f,
                combatEnabled: true,
                bossEnabled: true,
                bossMaxHealth: 3,
                bossPhaseTwoHealthThreshold: 2,
                bossPhaseOneTelegraphSeconds: 0.01f,
                bossPhaseOneExecuteSeconds: 0.01f,
                bossPhaseOneRecoverySeconds: 0.02f,
                bossPhaseTwoTelegraphSeconds: 0.01f,
                bossPhaseTwoExecuteSeconds: 0.01f);
            PlayerDamageResult? bossPatternDamage = null;
            _session.PlayerDamaged += result =>
            {
                if (result.SourceKind == PlayerDamageSourceKind.BossPattern)
                {
                    bossPatternDamage = result;
                }
            };

            int frameGuard = 0;
            while (!bossPatternDamage.HasValue && frameGuard++ < 600)
            {
                yield return null;
            }

            Assert.That(frameGuard, Is.LessThan(600));
            Assert.That(bossPatternDamage.HasValue, Is.True);
            Assert.That(
                bossPatternDamage.Value.SourceActorId,
                Is.EqualTo(_session.BossActorId));
        }

        [UnityTest]
        public IEnumerator BossEncounter_ThrownBombFliesThenBeginsFuseOnLanding()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includePresenter: true,
                combatEnabled: true,
                bossEnabled: true,
                bossMaxHealth: 3,
                bossPhaseTwoHealthThreshold: 2,
                bossPhaseOneTelegraphSeconds: 0.01f,
                bossPhaseOneExecuteSeconds: 0.01f,
                bossPhaseOneRecoverySeconds: 0.2f,
                bossPhaseTwoTelegraphSeconds: 0.01f,
                bossPhaseTwoExecuteSeconds: 0.01f);
            BossBombFlight launched = default;
            BombSnapshot landed = default;
            _session.BossBombLaunched += flight =>
            {
                if (launched.Definition == null)
                {
                    launched = flight;
                }
            };
            _session.BossBombPlaced += snapshot =>
            {
                if (launched.Definition != null && snapshot.Position == launched.Target)
                {
                    landed = snapshot;
                }
            };

            int frameGuard = 0;
            while (launched.Definition == null && frameGuard++ < 600)
            {
                yield return null;
            }
            Assert.That(launched.Definition, Is.Not.Null);
            Assert.That(_presenter.ActiveBossFlightVisualCount, Is.GreaterThan(0));

            frameGuard = 0;
            while (!landed.Id.IsValid && frameGuard++ < 180)
            {
                yield return null;
            }
            Assert.That(landed.Id.IsValid, Is.True);
            Assert.That(landed.OwnerId, Is.EqualTo(_session.BossActorId));
            Assert.That(landed.Position, Is.EqualTo(launched.Target));
            Assert.That(landed.DetonatesAt, Is.GreaterThan(launched.LandsAt));
            Assert.That(_presenter.ActiveBossFlightVisualCount, Is.LessThan(3));
        }

        [UnityTest]
        public IEnumerator BossPresenter_PauseFreezesExecuteInterpolationUntilResume()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                combatEnabled: true,
                runtimePlayerStart: new GridPosition(0, -2),
                bossEnabled: true,
                includeBossPresenter: true,
                bossPhaseOneTelegraphSeconds: 0.04f,
                bossPhaseOneExecuteSeconds: 0.3f,
                bossPhaseOneRecoverySeconds: 0.3f);

            int frameGuard = 0;
            while (_bossPresenter.MovementCount == 0 && frameGuard++ < 60)
            {
                yield return null;
            }
            Assert.That(_bossPresenter.MovementCount, Is.EqualTo(1));

            PressAndRelease(Key.Escape);
            yield return null;
            Assert.That(_session.IsPaused, Is.True);
            Vector3 pausedPosition = _bossPresenter.BossInstance.transform.position;

            yield return new WaitForSecondsRealtime(0.12f);

            Assert.That(
                Vector3.Distance(
                    _bossPresenter.BossInstance.transform.position,
                    pausedPosition),
                Is.LessThan(0.0001f));
            PressAndRelease(Key.Escape);
            yield return null;
            Assert.That(_session.IsPaused, Is.False);

            yield return new WaitForSecondsRealtime(0.35f);

            Vector3 expectedBossPosition =
                _session.GridSpace.GridToWorld(_session.CurrentBossGridPosition) +
                (Vector3.up * _session.BossDefinition.VisualHeight);
            Assert.That(
                Vector3.Distance(
                    _bossPresenter.BossInstance.transform.position,
                    expectedBossPosition),
                Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator HealthHud_TracksBossHealthAndDeferredPhaseTwo()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includeHealthHud: true,
                fuseSeconds: 0.03f,
                placementCooldownSeconds: 0.01f,
                combatEnabled: true,
                bossEnabled: true,
                bossMaxHealth: 3,
                bossPhaseTwoHealthThreshold: 2,
                bossPhaseOneTelegraphSeconds: 0.01f,
                bossPhaseOneExecuteSeconds: 0.01f,
                bossPhaseOneRecoverySeconds: 0.35f,
                bossPhaseTwoTelegraphSeconds: 0.01f,
                bossPhaseTwoExecuteSeconds: 0.01f,
                bossPhaseTwoRecoverySeconds: 0.25f);
            yield return null;

            Assert.That(_healthHud.IsInitialized, Is.True);
            Assert.That(_healthHud.IsBossPanelVisible, Is.True);
            Assert.That(_healthHud.DisplayedBossHealth, Is.EqualTo(3));
            Assert.That(_healthHud.DisplayedBossMaxHealth, Is.EqualTo(3));
            Assert.That(_healthHud.DisplayedBossPhase, Is.EqualTo(BossPhase.One));
            Assert.That(_healthHud.BossHealthFillFraction, Is.EqualTo(1f));

            PressAndRelease(Key.X);
            yield return null;
            Assert.That(_session.ActiveBombSlotIndex, Is.EqualTo(1));

            int frameGuard = 0;
            while ((_session.CurrentBossPattern != BossPatternKind.Overheat ||
                    _session.CurrentBossState != BossBattleState.Recovery) &&
                   frameGuard++ < 600)
            {
                yield return null;
            }
            Assert.That(frameGuard, Is.LessThan(600));

            yield return new WaitForSecondsRealtime(0.04f);
            Assert.That(
                _session.CurrentBossHealth,
                Is.EqualTo(3),
                "Boss must ignore its own blast even while vulnerable.");

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(_healthHud.DisplayedBossHealth, Is.EqualTo(2));
            Assert.That(_healthHud.BossHealthFillFraction, Is.EqualTo(2f / 3f).Within(0.001f));

            frameGuard = 0;
            while (_session.CurrentBossPhase != BossPhase.Two && frameGuard++ < 600)
            {
                yield return null;
            }
            Assert.That(_session.CurrentBossPhase, Is.EqualTo(BossPhase.Two));
            Assert.That(_healthHud.DisplayedBossPhase, Is.EqualTo(BossPhase.Two));
            Assert.That(_healthHud.BossHealthText, Does.Contain("PHASE 2"));

            Assert.That(_healthHud.DisplayedBossHealth, Is.EqualTo(2));
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

        [UnityTest]
        public IEnumerator SelfDestructEnemy_PlayerBlastArmsAndOwnBlastDestroysWall()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                fuseSeconds: 0.03f,
                areaExplosionRange: 1,
                chaserSpawnPosition: new Vector2Int(2, -2),
                destructibleWalls: new[] { new Vector2Int(1, 1) },
                retreatAnchors: new[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                },
                includeDestructibleWallPresenter: true,
                selfDestructSpawnPosition: new Vector2Int(0, 2),
                selfDestructAnchors: new[] { new Vector2Int(-1, 0) },
                includeSelfDestructPresenter: true,
                selfDestructChaseCellsPerSecond: 1f,
                selfDestructFuseSeconds: 0.25f,
                selfDestructExplosionRange: 2);
            int armedCount = 0;
            int selfDestructDeathCount = 0;
            _session.SelfDestructArmed += _ => armedCount++;
            _session.EnemyDied += damage =>
            {
                if (damage.ActorId == _session.SelfDestructActorId)
                {
                    selfDestructDeathCount++;
                }
            };

            yield return null;
            Assert.That(
                _session.CurrentSelfDestructGridPosition,
                Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(
                _session.CurrentSelfDestructState,
                Is.EqualTo(SelfDestructEnemyState.WarningChase));
            Vector3 warningScale = _selfDestructPresenter.Instance.transform.localScale;
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(
                Vector3.Distance(
                    warningScale,
                    _selfDestructPresenter.Instance.transform.localScale),
                Is.GreaterThan(0.0001f));

            PressAndRelease(Key.Z);
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(
                _session.CurrentSelfDestructState,
                Is.EqualTo(SelfDestructEnemyState.Telegraph));
            Assert.That(_session.IsSelfDestructAlive, Is.True);
            Assert.That(armedCount, Is.EqualTo(1));
            Assert.That(_selfDestructPresenter.ActiveTelegraphCellCount, Is.GreaterThan(0));
            Vector3 expectedTelegraphPosition =
                _session.GridSpace.GridToWorld(
                    _session.CurrentSelfDestructGridPosition) +
                (Vector3.up * _session.SelfDestructDefinition.VisualHeight);
            Assert.That(
                Vector3.Distance(
                    _selfDestructPresenter.Instance.transform.position,
                    expectedTelegraphPosition),
                Is.LessThan(0.001f));
            Assert.That(_destructibleWallPresenter.ActiveWallVisualCount, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(
                _session.CurrentSelfDestructState,
                Is.EqualTo(SelfDestructEnemyState.Detonated));
            Assert.That(_session.IsSelfDestructAlive, Is.False);
            Assert.That(selfDestructDeathCount, Is.EqualTo(1));
            Assert.That(_selfDestructPresenter.DeathCount, Is.EqualTo(1));
            Assert.That(_selfDestructPresenter.ActiveTelegraphCellCount, Is.Zero);
            Assert.That(_destructibleWallPresenter.ActiveWallVisualCount, Is.Zero);
            Assert.That(
                _session.GetCell(new GridPosition(1, 1)).Terrain,
                Is.EqualTo(GridTerrain.Floor));
        }

        [UnityTest]
        public IEnumerator SelfDestructEnemy_ContinuousWarningArmsBeforeBaseCadence()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                chaserSpawnPosition: new Vector2Int(2, -2),
                retreatAnchors: new[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                },
                selfDestructSpawnPosition: new Vector2Int(2, 2),
                selfDestructAnchors: new[] { new Vector2Int(2, 1) },
                includeSelfDestructPresenter: true,
                selfDestructChaseCellsPerSecond: 1f,
                selfDestructWarningMaxCellsPerSecond: 2f,
                selfDestructWarningEscalationSeconds: 0.15f,
                selfDestructFuseSeconds: 1f);
            int armedCount = 0;
            _session.SelfDestructArmed += _ => armedCount++;

            yield return null;
            Assert.That(
                _session.CurrentSelfDestructGridPosition,
                Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(
                _session.CurrentSelfDestructState,
                Is.EqualTo(SelfDestructEnemyState.WarningChase));
            float initialProgress = _session.CurrentSelfDestructWarningProgress;

            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(
                _session.CurrentSelfDestructWarningProgress,
                Is.GreaterThan(initialProgress));

            int frameGuard = 0;
            while (_session.CurrentSelfDestructState !=
                       SelfDestructEnemyState.Telegraph &&
                   frameGuard++ < 60)
            {
                yield return null;
            }

            Assert.That(
                _session.CurrentSelfDestructState,
                Is.EqualTo(SelfDestructEnemyState.Telegraph));
            Assert.That(
                _session.CurrentSelfDestructGridPosition,
                Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(armedCount, Is.EqualTo(1));
            Assert.That(_selfDestructPresenter.ActiveTelegraphCellCount, Is.GreaterThan(0));
            Assert.That(
                Vector3.Distance(
                    _selfDestructPresenter.Instance.transform.position,
                    _session.GridSpace.GridToWorld(new GridPosition(2, 1)) +
                        (Vector3.up * _session.SelfDestructDefinition.VisualHeight)),
                Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator ThrowerEnemy_TelegraphsFlightsAndUsesSharedChainScheduler()
        {
            CreateRuntime(
                Vector2Int.zero,
                false,
                includePresenter: true,
                fuseSeconds: 0.12f,
                maxHealth: 5,
                invulnerabilitySeconds: 0.01f,
                chaserCellsPerSecond: 0.1f,
                chaserSpawnPosition: new Vector2Int(-2, -2),
                retreatAnchors: new[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                },
                throwerSpawnPosition: new Vector2Int(2, 1),
                throwerFiringAnchors: new[]
                {
                    new Vector2Int(2, -1),
                    new Vector2Int(-2, 1),
                },
                throwerTargetAnchors: new[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 2),
                    new Vector2Int(-2, 2),
                    new Vector2Int(2, 2),
                    new Vector2Int(0, -2),
                },
                includeThrowerPresenter: true,
                throwerMoveCellsPerSecond: 20f,
                throwerTelegraphSeconds: 0.04f,
                throwerFlightSeconds: 0.04f,
                throwerRecoverySeconds: 0.04f,
                throwerBombFuseSeconds: 1f);
            int telegraphCount = 0;
            int visibleTelegraphCellCount = 0;
            int launchCount = 0;
            int placedCount = 0;
            var thrownBombIds = new List<BombId>();
            var thrownCauses = new Dictionary<BombId, BombDetonationCause>();
            _session.ThrowerAdvanced += result =>
            {
                if (result.State == ThrowerEnemyState.Telegraph &&
                    result.HasStateTransition)
                {
                    telegraphCount++;
                    visibleTelegraphCellCount =
                        _throwerPresenter.ActiveTelegraphCellCount;
                }
            };
            _session.ThrowerBombLaunched += _ => launchCount++;
            _session.ThrowerBombPlaced += snapshot =>
            {
                placedCount++;
                thrownBombIds.Add(snapshot.Id);
            };
            _session.BombExploded += explosion =>
            {
                if (thrownBombIds.Contains(explosion.BombId))
                {
                    thrownCauses[explosion.BombId] = explosion.Cause;
                }
            };

            int frameGuard = 0;
            while (placedCount < 3 && frameGuard++ < 120)
            {
                yield return null;
            }

            Assert.That(_session.HasThrower, Is.True);
            Assert.That(_session.CurrentThrowerLockedTarget, Is.EqualTo(new GridPosition(-1, 0)));
            Assert.That(
                _session.CurrentThrowerLockedTargets,
                Is.EqualTo(new[]
                {
                    new GridPosition(-1, 0),
                    new GridPosition(1, 0),
                    new GridPosition(0, 2),
                }));
            Assert.That(telegraphCount, Is.EqualTo(1));
            Assert.That(visibleTelegraphCellCount, Is.EqualTo(3));
            Assert.That(launchCount, Is.EqualTo(3));
            Assert.That(placedCount, Is.EqualTo(3));
            Assert.That(thrownBombIds, Has.All.Matches<BombId>(id => id.IsValid));
            Assert.That(
                thrownBombIds,
                Has.All.Matches<BombId>(id => _presenter.HasBombVisual(id)));
            Assert.That(_throwerPresenter.TelegraphCount, Is.EqualTo(1));
            Assert.That(_throwerPresenter.IsTelegraphVisible, Is.False);
            Assert.That(_throwerPresenter.ActiveTelegraphCellCount, Is.Zero);

            Assert.That(_session.TryPlaceBomb(), Is.True);
            frameGuard = 0;
            while (thrownCauses.Count < 3 && frameGuard++ < 240)
            {
                yield return null;
            }

            Assert.That(
                thrownCauses.Values,
                Has.Exactly(2).EqualTo(BombDetonationCause.Chain));
            Assert.That(
                thrownCauses.Values,
                Has.Exactly(1).EqualTo(BombDetonationCause.Fuse));
            Assert.That(_session.HasPendingThrowerBombFlight, Is.False);
            Assert.That(
                thrownBombIds,
                Has.None.Matches<BombId>(id => _presenter.HasBombVisual(id)));
        }

        private void CreateRuntime(
            Vector2Int blocker,
            bool includeBlocker,
            bool includeProbe = false,
            bool includePresenter = false,
            bool includeHealthPresenter = false,
            bool includeChaserPresenter = false,
            bool includeWeaponHud = false,
            bool includeHealthHud = false,
            float fuseSeconds = 1f,
            float explosionVisualSeconds = 0.25f,
            float placementCooldownSeconds = 0.01f,
            int areaExplosionRange = 1,
            float areaPlacementCooldownSeconds = 0.01f,
            BombExplosionShape secondExplosionShape = BombExplosionShape.SquareArea,
            string secondDefinitionId = "test-area",
            float swapCooldownSeconds = 0.05f,
            int maxHealth = 5,
            float invulnerabilitySeconds = 0.75f,
            float healthDamagePulseSeconds = PrototypePlayerHealthPresenter.DefaultDamagePulseSeconds,
            float chaserCellsPerSecond = 2f,
            float chaserDeathVisualSeconds = 0.12f,
            Vector2Int? chaserSpawnPosition = null,
            bool includeRoomAdvanceController = false,
            string nextSceneName = "UnusedPlayModeTarget",
            float roomTransitionDelaySeconds =
                PrototypeRoomAdvanceController.DefaultTransitionDelaySeconds,
            Vector2Int[] destructibleWalls = null,
            Vector2Int[] retreatAnchors = null,
            bool includeDestructibleWallPresenter = false,
            Vector2Int? chargerSpawnPosition = null,
            bool includeChargerPresenter = false,
            float chargerLaneAcquireCellsPerSecond = 20f,
            float chargerTelegraphSeconds = 0.05f,
            float chargerCellsPerSecond = 8f,
            float chargerRecoverSeconds = 0.05f,
            Vector2Int? armoredSpawnPosition = null,
            bool includeArmoredPresenter = false,
            float armoredCellsPerSecond = 1f,
            float brokenCellsPerSecond = 3f,
            int armoredGuardRadius = 1,
            float armoredPanicTelegraphSeconds = 0.5f,
            float armoredPanicCellsPerSecond = 20f,
            int armoredPanicRunDistance = 3,
            float armoredPanicRecoverSeconds = 0.05f,
            bool combatEnabled = true,
            GridPosition? runtimePlayerStart = null,
            bool? runtimeCombatEnabled = null,
            bool bossEnabled = false,
            bool includeBossPresenter = false,
            Vector2Int? bossSpawnPosition = null,
            int bossMaxHealth = 3,
            int bossPhaseTwoHealthThreshold = 2,
            float bossPhaseOneTelegraphSeconds = 0.05f,
            float bossPhaseOneExecuteSeconds = 0.02f,
            float bossPhaseOneRecoverySeconds = 0.3f,
            float bossPhaseTwoTelegraphSeconds = 0.04f,
            float bossPhaseTwoExecuteSeconds = 0.02f,
            float bossPhaseTwoRecoverySeconds = 0.25f,
            Vector2Int? selfDestructSpawnPosition = null,
            Vector2Int[] selfDestructAnchors = null,
            bool includeSelfDestructPresenter = false,
            float selfDestructChaseCellsPerSecond = 2f,
            float selfDestructWarningMaxCellsPerSecond = 5f,
            float selfDestructWarningEscalationSeconds = 1.5f,
            int selfDestructWarningDistance = 3,
            int selfDestructPrimeDistance = 1,
            float selfDestructFuseSeconds = 0.08f,
            int selfDestructExplosionRange = 2,
            Vector2Int? throwerSpawnPosition = null,
            Vector2Int[] throwerFiringAnchors = null,
            Vector2Int[] throwerTargetAnchors = null,
            bool includeThrowerPresenter = false,
            float throwerMoveCellsPerSecond = 1f,
            float throwerTelegraphSeconds = 0.3f,
            float throwerFlightSeconds = 0.45f,
            float throwerRecoverySeconds = 0.75f,
            int throwerBombsPerVolley = 3,
            float throwerBombFuseSeconds = 1.5f,
            int throwerBombRange = 1)
        {
            if (bossEnabled && retreatAnchors == null)
            {
                retreatAnchors = new[]
                {
                    new Vector2Int(-2, -1),
                    new Vector2Int(-1, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(2, -1),
                };
            }
            if (bossEnabled && !selfDestructSpawnPosition.HasValue)
            {
                selfDestructSpawnPosition = new Vector2Int(-2, 2);
                selfDestructAnchors = new[]
                {
                    new Vector2Int(-1, 2),
                    new Vector2Int(2, 2),
                };
            }
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
                explosionVisualSeconds,
                placementCooldownSeconds);
            _areaDefinition = ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
            _areaDefinition.Configure(
                secondDefinitionId,
                fuseSeconds,
                areaExplosionRange,
                _bombPrefab,
                _explosionPrefab,
                explosionVisualSeconds,
                areaPlacementCooldownSeconds,
                secondExplosionShape);
            _loadout = ScriptableObject.CreateInstance<PrototypeBombLoadoutAsset>();
            _loadout.Configure(_definition, _areaDefinition, swapCooldownSeconds);
            _vitals = ScriptableObject.CreateInstance<PrototypePlayerVitalsAsset>();
            _vitals.Configure(maxHealth, invulnerabilitySeconds);

            _root = new GameObject("PrototypePlayerControllerTest");
            _root.SetActive(false);

            var gridRoot = new GameObject("GridRoot").transform;
            gridRoot.SetParent(_root.transform, false);
            var presentationRoot = new GameObject("RuntimePresentation").transform;
            presentationRoot.SetParent(gridRoot, false);
            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetParent(gridRoot, false);
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "PlayerPlaceholder";
            Collider playerCollider = playerObject.GetComponent<Collider>();
            Object.DestroyImmediate(playerCollider);
            Shader playerShader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(playerShader, Is.Not.Null);
            _playerMaterial = new Material(playerShader);
            _playerMaterial.color = new Color(1f, 0.69f, 0.12f, 1f);
            playerObject.GetComponent<Renderer>().sharedMaterial = _playerMaterial;
            _player = playerObject.transform;
            _player.SetParent(gridRoot, false);
            _player.position = new Vector3(0f, 0.5f, 0f);
            var chaserSpawn = new GameObject("ChaserSpawn").transform;
            chaserSpawn.SetParent(gridRoot, false);
            Vector2Int authoredChaserSpawn =
                chaserSpawnPosition ?? new Vector2Int(2, -2);
            chaserSpawn.localPosition = new Vector3(
                authoredChaserSpawn.x,
                0f,
                authoredChaserSpawn.y);
            _chaserPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _chaserPrefab.name = "ChaserVisualPrefab";
            Collider chaserCollider = _chaserPrefab.GetComponent<Collider>();
            Object.DestroyImmediate(chaserCollider);
            _chaserMaterial = new Material(playerShader);
            _chaserMaterial.color = new Color(0.18f, 0.82f, 0.38f, 1f);
            _chaserPrefab.GetComponent<Renderer>().sharedMaterial = _chaserMaterial;
            _chaserPrefab.SetActive(false);
            _chaserDefinition = ScriptableObject.CreateInstance<PrototypeChaserDefinitionAsset>();
            _chaserDefinition.Configure(
                "test-chaser",
                1,
                1,
                chaserCellsPerSecond,
                2,
                _chaserPrefab,
                0.45f,
                chaserDeathVisualSeconds);
            Transform chargerSpawn = null;
            if (chargerSpawnPosition.HasValue)
            {
                Vector2Int authoredChargerSpawn = chargerSpawnPosition.Value;
                chargerSpawn = new GameObject("ChargerSpawn").transform;
                chargerSpawn.SetParent(gridRoot, false);
                chargerSpawn.localPosition = new Vector3(
                    authoredChargerSpawn.x,
                    0f,
                    authoredChargerSpawn.y);
                _chargerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _chargerPrefab.name = "ChargerVisualPrefab";
                Collider chargerCollider = _chargerPrefab.GetComponent<Collider>();
                Object.DestroyImmediate(chargerCollider);
                _chargerMaterial = new Material(playerShader);
                _chargerMaterial.color = new Color(0.95f, 0.28f, 0.08f, 1f);
                _chargerPrefab.GetComponent<Renderer>().sharedMaterial = _chargerMaterial;
                _chargerPrefab.SetActive(false);
                _chargerTelegraphCellPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _chargerTelegraphCellPrefab.name = "ChargerTelegraphCellVisualPrefab";
                Collider telegraphCollider =
                    _chargerTelegraphCellPrefab.GetComponent<Collider>();
                Object.DestroyImmediate(telegraphCollider);
                _chargerTelegraphCellMaterial = new Material(playerShader);
                _chargerTelegraphCellMaterial.color = new Color(1f, 0.72f, 0.05f, 1f);
                _chargerTelegraphCellPrefab.GetComponent<Renderer>().sharedMaterial =
                    _chargerTelegraphCellMaterial;
                _chargerTelegraphCellPrefab.SetActive(false);
                _chargerDefinition =
                    ScriptableObject.CreateInstance<PrototypeChargerDefinitionAsset>();
                _chargerDefinition.Configure(
                    "test-charger",
                    1,
                    1,
                    chargerLaneAcquireCellsPerSecond,
                    chargerTelegraphSeconds,
                    chargerCellsPerSecond,
                    chargerRecoverSeconds,
                    _chargerPrefab,
                    _chargerTelegraphCellPrefab,
                    0.45f,
                    0.12f);
            }
            Transform armoredSpawn = null;
            if (armoredSpawnPosition.HasValue)
            {
                Vector2Int authoredArmoredSpawn = armoredSpawnPosition.Value;
                armoredSpawn = new GameObject("ArmoredSpawn").transform;
                armoredSpawn.SetParent(gridRoot, false);
                armoredSpawn.localPosition = new Vector3(
                    authoredArmoredSpawn.x,
                    0f,
                    authoredArmoredSpawn.y);
                _armoredPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _armoredPrefab.name = "ArmoredVisualPrefab";
                Collider armoredCollider = _armoredPrefab.GetComponent<Collider>();
                Object.DestroyImmediate(armoredCollider);
                _armoredMaterial = new Material(playerShader);
                _armoredMaterial.color = new Color(0.28f, 0.38f, 0.52f, 1f);
                _armoredPrefab.GetComponent<Renderer>().sharedMaterial = _armoredMaterial;
                _armoredPrefab.SetActive(false);
                _armoredPanicTelegraphCellPrefab =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                _armoredPanicTelegraphCellPrefab.name =
                    "ArmoredPanicTelegraphCellVisualPrefab";
                Object.DestroyImmediate(
                    _armoredPanicTelegraphCellPrefab.GetComponent<Collider>());
                _armoredPanicTelegraphCellMaterial = new Material(playerShader);
                _armoredPanicTelegraphCellMaterial.color =
                    new Color(1f, 0.22f, 0.05f, 1f);
                _armoredPanicTelegraphCellPrefab.GetComponent<Renderer>().sharedMaterial =
                    _armoredPanicTelegraphCellMaterial;
                _armoredPanicTelegraphCellPrefab.transform.localScale =
                    new Vector3(0.86f, 0.05f, 0.86f);
                _armoredPanicTelegraphCellPrefab.SetActive(false);
                _armoredDefinition =
                    ScriptableObject.CreateInstance<PrototypeArmoredDefinitionAsset>();
                _armoredDefinition.Configure(
                    "test-armored",
                    1,
                    armoredCellsPerSecond,
                    brokenCellsPerSecond,
                    2,
                    armoredGuardRadius,
                    armoredPanicTelegraphSeconds,
                    armoredPanicCellsPerSecond,
                    armoredPanicRunDistance,
                    armoredPanicRecoverSeconds,
                    _armoredPrefab,
                    _armoredPanicTelegraphCellPrefab,
                    0.5f,
                    0.12f);
            }
            Transform selfDestructSpawn = null;
            if (selfDestructSpawnPosition.HasValue)
            {
                Vector2Int authoredSelfDestructSpawn =
                    selfDestructSpawnPosition.Value;
                selfDestructSpawn = new GameObject("SelfDestructSpawn").transform;
                selfDestructSpawn.SetParent(gridRoot, false);
                selfDestructSpawn.localPosition = new Vector3(
                    authoredSelfDestructSpawn.x,
                    0f,
                    authoredSelfDestructSpawn.y);

                _selfDestructPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _selfDestructPrefab.name = "SelfDestructVisualPrefab";
                Object.DestroyImmediate(_selfDestructPrefab.GetComponent<Collider>());
                _selfDestructMaterial = new Material(playerShader);
                _selfDestructMaterial.color = new Color(0.92f, 0.18f, 0.08f, 1f);
                _selfDestructPrefab.GetComponent<Renderer>().sharedMaterial =
                    _selfDestructMaterial;
                _selfDestructPrefab.SetActive(false);

                _selfDestructTelegraphCellPrefab =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                _selfDestructTelegraphCellPrefab.name =
                    "SelfDestructTelegraphCellVisualPrefab";
                Object.DestroyImmediate(
                    _selfDestructTelegraphCellPrefab.GetComponent<Collider>());
                _selfDestructTelegraphCellMaterial = new Material(playerShader);
                _selfDestructTelegraphCellMaterial.color =
                    new Color(1f, 0.38f, 0.04f, 1f);
                _selfDestructTelegraphCellPrefab.GetComponent<Renderer>().sharedMaterial =
                    _selfDestructTelegraphCellMaterial;
                _selfDestructTelegraphCellPrefab.transform.localScale =
                    new Vector3(0.86f, 0.05f, 0.86f);
                _selfDestructTelegraphCellPrefab.SetActive(false);

                _selfDestructBombDefinition =
                    ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
                _selfDestructBombDefinition.Configure(
                    "test-self-destruct-blast",
                    selfDestructFuseSeconds,
                    selfDestructExplosionRange,
                    _bombPrefab,
                    _explosionPrefab,
                    explosionVisualSeconds,
                    1f,
                    BombExplosionShape.Cross);
                _selfDestructDefinition =
                    ScriptableObject.CreateInstance<PrototypeSelfDestructDefinitionAsset>();
                _selfDestructDefinition.Configure(
                    "test-self-destruct",
                    selfDestructChaseCellsPerSecond,
                    selfDestructWarningMaxCellsPerSecond,
                    selfDestructWarningEscalationSeconds,
                    selfDestructWarningDistance,
                    selfDestructPrimeDistance,
                    _selfDestructBombDefinition,
                    _selfDestructPrefab,
                    _selfDestructTelegraphCellPrefab,
                    0.45f,
                    0.12f);
            }
            Transform throwerSpawn = null;
            if (throwerSpawnPosition.HasValue)
            {
                Vector2Int authoredThrowerSpawn = throwerSpawnPosition.Value;
                throwerSpawn = new GameObject("ThrowerSpawn").transform;
                throwerSpawn.SetParent(gridRoot, false);
                throwerSpawn.localPosition = new Vector3(
                    authoredThrowerSpawn.x,
                    0f,
                    authoredThrowerSpawn.y);

                _throwerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _throwerPrefab.name = "ThrowerVisualPrefab";
                Object.DestroyImmediate(_throwerPrefab.GetComponent<Collider>());
                _throwerMaterial = new Material(playerShader);
                _throwerMaterial.color = new Color(0.55f, 0.14f, 0.78f, 1f);
                _throwerPrefab.GetComponent<Renderer>().sharedMaterial = _throwerMaterial;
                _throwerPrefab.SetActive(false);

                _throwerTelegraphCellPrefab =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                _throwerTelegraphCellPrefab.name = "ThrowerTelegraphCellVisualPrefab";
                Object.DestroyImmediate(
                    _throwerTelegraphCellPrefab.GetComponent<Collider>());
                _throwerTelegraphCellMaterial = new Material(playerShader);
                _throwerTelegraphCellMaterial.color =
                    new Color(1f, 0.12f, 0.85f, 1f);
                _throwerTelegraphCellPrefab.GetComponent<Renderer>().sharedMaterial =
                    _throwerTelegraphCellMaterial;
                _throwerTelegraphCellPrefab.transform.localScale =
                    new Vector3(0.88f, 0.05f, 0.88f);
                _throwerTelegraphCellPrefab.SetActive(false);

                _throwerBombDefinition =
                    ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
                _throwerBombDefinition.Configure(
                    "test-thrower-blocker",
                    throwerBombFuseSeconds,
                    throwerBombRange,
                    _bombPrefab,
                    _explosionPrefab,
                    explosionVisualSeconds,
                    1f,
                    BombExplosionShape.Cross);
                _throwerDefinition =
                    ScriptableObject.CreateInstance<PrototypeThrowerDefinitionAsset>();
                _throwerDefinition.Configure(
                    "test-thrower",
                    throwerMoveCellsPerSecond,
                    throwerTelegraphSeconds,
                    throwerFlightSeconds,
                    throwerRecoverySeconds,
                    1,
                    throwerBombsPerVolley,
                    _throwerBombDefinition,
                    _throwerPrefab,
                    _throwerTelegraphCellPrefab,
                    0.5f,
                    0.12f);
            }
            if (bossEnabled)
            {
                _bossThrowBombDefinition =
                    ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
                _bossThrowBombDefinition.Configure(
                    "test-boss-throw",
                    0.08f,
                    2,
                    _bombPrefab,
                    _explosionPrefab,
                    0.04f,
                    0.01f);
                _bossChainBombDefinition =
                    ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
                _bossChainBombDefinition.Configure(
                    "test-boss-chain",
                    0.18f,
                    2,
                    _bombPrefab,
                    _explosionPrefab,
                    0.04f,
                    0.01f);
                _bossPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _bossPrefab.name = "BossVisualPrefab";
                Object.DestroyImmediate(_bossPrefab.GetComponent<Collider>());
                _bossMaterial = new Material(playerShader);
                _bossMaterial.color = new Color(0.46f, 0.12f, 0.68f, 1f);
                _bossPrefab.GetComponent<Renderer>().sharedMaterial = _bossMaterial;
                _bossPrefab.SetActive(false);

                _bossDangerCellPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _bossDangerCellPrefab.name = "BossDangerCellVisualPrefab";
                Object.DestroyImmediate(_bossDangerCellPrefab.GetComponent<Collider>());
                _bossDangerCellMaterial = new Material(playerShader);
                _bossDangerCellMaterial.color = new Color(1f, 0.7f, 0.08f, 1f);
                _bossDangerCellPrefab.GetComponent<Renderer>().sharedMaterial =
                    _bossDangerCellMaterial;
                _bossDangerCellPrefab.SetActive(false);

                _bossDefinition =
                    ScriptableObject.CreateInstance<PrototypeBossDefinitionAsset>();
                Vector2Int authoredBossSpawn =
                    bossSpawnPosition ?? new Vector2Int(0, 1);
                _bossDefinition.Configure(
                    "test-boss",
                    bossMaxHealth,
                    bossPhaseTwoHealthThreshold,
                    1,
                    bossPhaseOneTelegraphSeconds,
                    bossPhaseOneExecuteSeconds,
                    bossPhaseOneRecoverySeconds,
                    bossPhaseTwoTelegraphSeconds,
                    bossPhaseTwoExecuteSeconds,
                    bossPhaseTwoRecoverySeconds,
                    authoredBossSpawn,
                    _bossPrefab,
                    _bossDangerCellPrefab,
                    _bossThrowBombDefinition,
                    _bossChainBombDefinition,
                    0.6f,
                    0.03f,
                    0.12f);
            }
            _roomDefinition = ScriptableObject.CreateInstance<PrototypeCombatRoomDefinitionAsset>();
            Vector2Int[] authoredLureLoop = bossEnabled
                ? new[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, -1),
                    new Vector2Int(0, -1),
                }
                : new[]
                {
                    new Vector2Int(-2, -2),
                    new Vector2Int(-2, -1),
                    new Vector2Int(-2, 0),
                    new Vector2Int(-2, 1),
                    new Vector2Int(-2, 2),
                    new Vector2Int(-1, 2),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(2, 2),
                    new Vector2Int(2, 1),
                    new Vector2Int(2, 0),
                    new Vector2Int(2, -1),
                    new Vector2Int(2, -2),
                    new Vector2Int(1, -2),
                    new Vector2Int(0, -2),
                    new Vector2Int(-1, -2),
                };
            _roomDefinition.Configure(
                "test-combat-loop",
                RoomType.Combat,
                5,
                5,
                1f,
                Vector2Int.zero,
                authoredChaserSpawn,
                includeBlocker ? new[] { blocker } : new Vector2Int[0],
                new[] { Vector2Int.zero },
                retreatAnchors ?? new[]
                {
                    new Vector2Int(-1, 1),
                    new Vector2Int(1, 1),
                },
                authoredLureLoop,
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(0, 2),
                        RoomExitDirection.North),
                    new PrototypeRoomExitData(
                        new Vector2Int(0, -2),
                        RoomExitDirection.South),
                },
                destructibleWalls ?? new Vector2Int[0],
                chargerSpawnPosition,
                armoredSpawnPosition,
                selfDestructSpawnPosition,
                selfDestructAnchors,
                throwerSpawnPosition,
                throwerFiringAnchors,
                throwerTargetAnchors);

            _reader = _root.AddComponent<BombSwapInputReader>();
            _reader.Configure(_inputActions);
            TestSandboxContext context = _root.AddComponent<TestSandboxContext>();
            context.Configure(
                _reader,
                gridRoot,
                spawn,
                _player,
                chaserSpawn,
                _roomDefinition,
                chargerSpawn,
                armoredSpawn,
                selfDestructSpawn,
                throwerSpawn);

            _session = _root.AddComponent<PrototypeGameSession>();
            _session.Configure(
                context,
                _reader,
                _loadout,
                _vitals,
                _chaserDefinition,
                10f,
                0.05f,
                _chargerDefinition,
                _armoredDefinition,
                combatEnabled,
                _bossDefinition,
                bossEnabled,
                _selfDestructDefinition,
                _throwerDefinition);
            if (runtimePlayerStart.HasValue || runtimeCombatEnabled.HasValue)
            {
                CombatRoomDefinition runtimeRoom =
                    _roomDefinition.CreateCoreDefinition();
                GridPosition runtimeStart =
                    runtimePlayerStart ?? runtimeRoom.PlayerSpawn;
                if (runtimeCombatEnabled.HasValue)
                {
                    _session.PrepareRuntimeRoom(
                        runtimeRoom,
                        runtimeStart,
                        runtimeCombatEnabled.Value);
                }
                else
                {
                    _session.PrepareRuntimeRoom(runtimeRoom, runtimeStart);
                }
            }
            Transform destructibleRoot = new GameObject("DestructibleObstacles").transform;
            destructibleRoot.SetParent(gridRoot, false);
            Vector2Int[] authoredDestructibleWalls = destructibleWalls ?? new Vector2Int[0];
            for (int index = 0; index < authoredDestructibleWalls.Length; index++)
            {
                Vector2Int wall = authoredDestructibleWalls[index];
                Transform wallVisual = new GameObject($"Destructible_{wall.x}_{wall.y}").transform;
                wallVisual.SetParent(destructibleRoot, false);
                wallVisual.localPosition = new Vector3(wall.x, 0f, wall.y);
            }
            _controller = _root.AddComponent<PrototypePlayerController>();
            _controller.Configure(_session, _player);
            if (includePresenter)
            {
                _presenter = _root.AddComponent<PrototypeBombPresenter>();
                _presenter.Configure(_session, presentationRoot, 1, 5);
            }
            if (includeDestructibleWallPresenter)
            {
                _destructibleWallPresenter =
                    _root.AddComponent<PrototypeDestructibleWallPresenter>();
                _destructibleWallPresenter.Configure(_session, destructibleRoot);
            }
            if (includeHealthPresenter)
            {
                _healthPresenter = _root.AddComponent<PrototypePlayerHealthPresenter>();
                _healthPresenter.Configure(
                    _session,
                    playerObject.GetComponent<Renderer>(),
                    healthDamagePulseSeconds);
            }
            if (includeChaserPresenter)
            {
                _chaserPresenter = _root.AddComponent<PrototypeChaserPresenter>();
                _chaserPresenter.Configure(_session, presentationRoot);
            }
            if (includeChargerPresenter)
            {
                _chargerPresenter = _root.AddComponent<PrototypeChargerPresenter>();
                _chargerPresenter.Configure(_session, presentationRoot);
            }
            if (includeArmoredPresenter)
            {
                _armoredPresenter = _root.AddComponent<PrototypeArmoredPresenter>();
                _armoredPresenter.Configure(_session, presentationRoot);
            }
            if (includeSelfDestructPresenter)
            {
                _selfDestructPresenter =
                    _root.AddComponent<PrototypeSelfDestructPresenter>();
                _selfDestructPresenter.Configure(_session, presentationRoot);
            }
            if (includeThrowerPresenter)
            {
                _throwerPresenter = _root.AddComponent<PrototypeThrowerPresenter>();
                _throwerPresenter.Configure(_session, presentationRoot);
            }
            if (includeBossPresenter)
            {
                _bossPresenter = _root.AddComponent<PrototypeBossPresenter>();
                _bossPresenter.Configure(_session, presentationRoot);
            }
            if (includeWeaponHud)
            {
                _weaponHud = _root.AddComponent<PrototypeWeaponHud>();
                _weaponHud.Configure(_session);
            }
            if (includeHealthHud)
            {
                _healthHud = _root.AddComponent<PrototypeHealthHud>();
                _healthHud.Configure(_session);
            }
            if (includeProbe)
            {
                _probe = _root.AddComponent<PrototypeInputHarnessProbe>();
                _probe.Configure(_reader, _session);
            }
            if (includeRoomAdvanceController)
            {
                _roomAdvanceController = _root.AddComponent<PrototypeRoomAdvanceController>();
                _roomAdvanceController.Configure(
                    _session,
                    nextSceneName,
                    roomTransitionDelaySeconds);
            }
            _root.SetActive(true);
            _reader.SetInputFocus(true);
        }

        private void PressAndRelease(Key key)
        {
            QueueKeyboardState(key);
            QueueKeyboardState();
        }

        private void QueueKeyboardState(params Key[] pressedKeys)
        {
            _reader.SetInputFocus(true);
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
            gameplay.AddAction(BombSwapInputActionNames.RestartRun, InputActionType.Button, "<Keyboard>/r");
            return asset;
        }
    }
}
