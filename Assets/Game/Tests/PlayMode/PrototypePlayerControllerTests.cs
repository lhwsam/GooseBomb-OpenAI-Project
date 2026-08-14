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
        private GameObject _chaserPrefab;
        private PrototypeBombDefinitionAsset _definition;
        private PrototypePlayerVitalsAsset _vitals;
        private PrototypeChaserDefinitionAsset _chaserDefinition;
        private PrototypeCombatRoomDefinitionAsset _roomDefinition;
        private Material _playerMaterial;
        private Material _chaserMaterial;
        private PrototypeGameSession _session;
        private PrototypePlayerController _controller;
        private PrototypeBombPresenter _presenter;
        private PrototypeInputHarnessProbe _probe;
        private PrototypePlayerHealthPresenter _healthPresenter;
        private PrototypeChaserPresenter _chaserPresenter;
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
            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
            }
            if (_vitals != null)
            {
                Object.DestroyImmediate(_vitals);
            }
            if (_chaserDefinition != null)
            {
                Object.DestroyImmediate(_chaserDefinition);
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
            yield return null;
            QueueKeyboardState();
            yield return new WaitForSecondsRealtime(0.15f);

            var origin = new GridPosition(0, 0);
            var north = new GridPosition(0, 1);
            Assert.That(_session.CurrentGridPosition, Is.EqualTo(north));
            Assert.That(_session.HasPlayerBombPassThrough, Is.False);
            Assert.That(_session.GetCell(origin).HasBomb, Is.True);

            QueueKeyboardState(Key.S);
            yield return null;
            QueueKeyboardState();
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_session.CurrentGridPosition, Is.EqualTo(north));
            Assert.That(_player.position.z, Is.EqualTo(1f).Within(0.05f));
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
        public IEnumerator ChaserContact_UsesLogicalAdjacencyAndSharedInvulnerability()
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
            bool includeHealthPresenter = false,
            bool includeChaserPresenter = false,
            float fuseSeconds = 1f,
            float explosionVisualSeconds = 0.25f,
            int maxHealth = 5,
            float invulnerabilitySeconds = 0.75f,
            float healthDamagePulseSeconds = PrototypePlayerHealthPresenter.DefaultDamagePulseSeconds,
            float chaserCellsPerSecond = 2f,
            float chaserDeathVisualSeconds = 0.12f,
            Vector2Int? chaserSpawnPosition = null)
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
            _roomDefinition = ScriptableObject.CreateInstance<PrototypeCombatRoomDefinitionAsset>();
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
                new[]
                {
                    new Vector2Int(-1, 1),
                    new Vector2Int(1, 1),
                },
                new[]
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
                },
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(0, 2),
                        RoomExitDirection.North),
                    new PrototypeRoomExitData(
                        new Vector2Int(0, -2),
                        RoomExitDirection.South),
                });

            BombSwapInputReader reader = _root.AddComponent<BombSwapInputReader>();
            reader.Configure(_inputActions);
            TestSandboxContext context = _root.AddComponent<TestSandboxContext>();
            context.Configure(
                reader,
                gridRoot,
                spawn,
                _player,
                chaserSpawn,
                _roomDefinition);

            _session = _root.AddComponent<PrototypeGameSession>();
            _session.Configure(
                context,
                reader,
                _definition,
                _vitals,
                _chaserDefinition,
                10f,
                0.05f);
            _controller = _root.AddComponent<PrototypePlayerController>();
            _controller.Configure(_session, _player);
            if (includePresenter)
            {
                _presenter = _root.AddComponent<PrototypeBombPresenter>();
                _presenter.Configure(_session, presentationRoot, 1, 5);
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
