using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeDungeonRunSessionTests
    {
        private readonly List<ScriptableObject> _createdAssets =
            new List<ScriptableObject>();
        private readonly List<GameObject> _createdGameObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdGameObjects.Count - 1; index >= 0; index--)
            {
                if (_createdGameObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdGameObjects[index]);
                }
            }
            _createdGameObjects.Clear();
            for (int index = _createdAssets.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(_createdAssets[index]);
            }
            _createdAssets.Clear();
        }

        [Test]
        public void Session_CreatesVersionedCoreRunAndResolvesEveryCombatSelection()
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalog();
            var session = new PrototypeDungeonRunSession(0, catalog);

            Assert.That(session.Seed, Is.Zero);
            Assert.That(session.Graph.Seed, Is.Zero);
            Assert.That(session.RunState.Graph, Is.SameAs(session.Graph));
            Assert.That(
                session.CombatRoomLayout.AssignmentVersion,
                Is.EqualTo(DungeonCombatRoomAssigner.AssignmentVersion));
            foreach (DungeonRoomNode room in session.Graph.Rooms)
            {
                bool found = session.TryGetCombatRoom(
                    room.Id,
                    out PrototypeDungeonCombatRoomSelection selection);
                if (room.RoomType != RoomType.Combat)
                {
                    Assert.That(found, Is.False);
                    continue;
                }

                Assert.That(found, Is.True);
                Assert.That(selection.RoomDefinition, Is.Not.Null);
                Assert.That(selection.SceneName, Is.Not.Empty);
                Assert.That(selection.Assignment.RoomId, Is.EqualTo(room.Id));
                Assert.That(
                    selection.RoomDefinition.CreateCoreDefinition().Id,
                    Is.EqualTo(selection.Assignment.DefinitionId));
            }
        }

        [Test]
        public void Session_DelegatesLockedClearAndBidirectionalTravelToCoreState()
        {
            var session = new PrototypeDungeonRunSession(12, CreateCatalog());
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];

            Assert.That(session.TryGetCurrentCombatRoom(out _), Is.False);
            Assert.That(session.TryTravelTo(firstCombat).Moved, Is.True);
            Assert.That(session.TryGetCurrentCombatRoom(out var selection), Is.True);
            Assert.That(selection.Assignment.RoomId, Is.EqualTo(firstCombat));
            Assert.That(
                session.TryTravelTo(session.Graph.StartRoomId).Status,
                Is.EqualTo(DungeonTravelStatus.BlockedByUnclearedRoom));
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                session.TryTravelTo(session.Graph.StartRoomId).Moved,
                Is.True);
            Assert.That(session.RunState.IsCleared(firstCombat), Is.True);
        }

        [Test]
        public void Session_ExposesGraphDoorsThatMatchCombatAssignmentAndClearState()
        {
            var session = new PrototypeDungeonRunSession(41, CreateCatalog());
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];
            Assert.That(session.TryTravelTo(firstCombat).Moved, Is.True);
            Assert.That(
                session.TryGetCurrentCombatRoom(out var selection),
                Is.True);

            IReadOnlyList<DungeonRoomExitState> locked =
                session.GetCurrentExitStates();
            Assert.That(
                locked.Where(exit => exit.IsConnected).Select(exit => exit.Direction),
                Is.EqualTo(selection.Assignment.ActiveExitDirections));
            Assert.That(
                locked.Where(exit => exit.IsConnected).Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Locked));

            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                session.GetCurrentExitStates()
                    .Where(exit => exit.IsConnected)
                    .Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Open));
        }

        [Test]
        public void SpecialCatalog_RequiresEveryUniqueNonCombatTypeAndScene()
        {
            PrototypeDungeonSpecialRoomCatalogAsset catalog =
                CreateSpecialCatalogAsset();
            PrototypeDungeonSpecialRoomEntry[] valid = CreateSpecialEntries();

            catalog.Configure(valid);
            valid[0] = default;

            Assert.That(catalog.GetSceneName(RoomType.Start), Is.EqualTo("DungeonStart"));
            Assert.That(
                catalog.GetSceneName(RoomType.BombReward),
                Is.EqualTo("DungeonReward"));
            Assert.That(
                catalog.GetSceneName(RoomType.BossAntechamber),
                Is.EqualTo("DungeonBossAnte"));
            Assert.That(catalog.GetSceneName(RoomType.Boss), Is.EqualTo("DungeonBoss"));
            Assert.Throws<ArgumentNullException>(() => catalog.Configure(null));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(Array.Empty<PrototypeDungeonSpecialRoomEntry>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Combat, "Combat"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.BombReward, "Reward"),
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.BossAntechamber,
                        "Ante"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "Boss"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "Same"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.BombReward, "Same"),
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.BossAntechamber,
                        "Ante"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "Boss"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "Start"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "OtherStart"),
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.BossAntechamber,
                        "Ante"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "Boss"),
                }));
        }

        [Test]
        public void Session_ResolvesCombatAndEveryRequiredSpecialScene()
        {
            var session = new PrototypeDungeonRunSession(
                73,
                CreateCatalog(),
                CreateSpecialCatalog());

            foreach (DungeonRoomNode room in session.Graph.Rooms)
            {
                Assert.That(
                    session.TryGetSceneName(room.Id, out string sceneName),
                    Is.True,
                    room.ToString());
                Assert.That(sceneName, Is.Not.Empty);
                if (room.RoomType != RoomType.Combat)
                {
                    Assert.That(
                        sceneName,
                        Is.EqualTo(ExpectedSpecialScene(room.RoomType)));
                }
            }
            Assert.That(
                session.TryGetCurrentSceneName(out string currentScene),
                Is.True);
            Assert.That(currentScene, Is.EqualTo("DungeonStart"));

            var withoutSpecialCatalog = new PrototypeDungeonRunSession(73, CreateCatalog());
            Assert.That(withoutSpecialCatalog.TryGetCurrentSceneName(out _), Is.False);
        }

        [Test]
        public void Navigator_CommitsCoreTravelOnlyAfterExpectedSceneLoads()
        {
            var session = new PrototypeDungeonRunSession(
                17,
                CreateCatalog(),
                CreateSpecialCatalog());
            var navigator = new PrototypeDungeonRunNavigator(session);
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];
            RoomExitDirection direction = session.Graph.GetExitDirection(
                session.Graph.StartRoomId,
                firstCombat);

            PrototypeDungeonTransitionStartResult unavailable =
                navigator.TryBeginTravel(direction, _ => false);

            Assert.That(
                unavailable.Status,
                Is.EqualTo(PrototypeDungeonTransitionStartStatus.SceneNotLoadable));
            Assert.That(navigator.HasPendingTransition, Is.False);
            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.StartRoomId));

            PrototypeDungeonTransitionStartResult started =
                navigator.TryBeginTravel(direction, _ => true);

            Assert.That(started.Started, Is.True);
            Assert.That(navigator.HasPendingTransition, Is.True);
            Assert.That(started.Transition.FromRoomId, Is.EqualTo(session.Graph.StartRoomId));
            Assert.That(started.Transition.TargetRoomId, Is.EqualTo(firstCombat));
            Assert.That(
                started.Transition.EntryDirection,
                Is.EqualTo(Opposite(direction)));
            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.StartRoomId));
            Assert.That(
                navigator.TryBeginTravel(direction, _ => true).Status,
                Is.EqualTo(
                    PrototypeDungeonTransitionStartStatus.TransitionAlreadyPending));
            Assert.That(
                navigator.CommitLoadedScene("WrongScene"),
                Is.EqualTo(PrototypeDungeonTransitionCommitStatus.SceneMismatch));
            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.StartRoomId));
            Assert.That(navigator.HasPendingTransition, Is.True);

            Assert.That(
                navigator.CommitLoadedScene(started.Transition.TargetSceneName),
                Is.EqualTo(PrototypeDungeonTransitionCommitStatus.Committed));
            Assert.That(session.CurrentRoomId, Is.EqualTo(firstCombat));
            Assert.That(navigator.HasPendingTransition, Is.False);
            Assert.That(
                navigator.CommitLoadedScene(started.Transition.TargetSceneName),
                Is.EqualTo(
                    PrototypeDungeonTransitionCommitStatus.NoTransitionPending));

            RoomExitDirection reverse = session.Graph.GetExitDirection(
                firstCombat,
                session.Graph.StartRoomId);
            Assert.That(
                navigator.TryBeginTravel(reverse, _ => true).Status,
                Is.EqualTo(PrototypeDungeonTransitionStartStatus.Locked));
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(navigator.TryBeginTravel(reverse, _ => true).Started, Is.True);
            navigator.CancelPendingTransition();
            Assert.That(navigator.HasPendingTransition, Is.False);
            Assert.That(session.CurrentRoomId, Is.EqualTo(firstCombat));
            Assert.Throws<ArgumentNullException>(() =>
                navigator.TryBeginTravel(reverse, null));
            Assert.Throws<ArgumentException>(() => navigator.CommitLoadedScene(" "));
        }

        [UnityTest]
        public IEnumerator RunHost_KeepsOneExplicitPersistentPrimary()
        {
            PrototypeDungeonCombatRoomCatalogAsset combatCatalog = CreateCatalog();
            PrototypeDungeonSpecialRoomCatalogAsset specialCatalog =
                CreateSpecialCatalog();
            GameObject firstRoot = CreateGameObject("FirstDungeonRunHost");
            firstRoot.SetActive(false);
            PrototypeDungeonRunHost first =
                firstRoot.AddComponent<PrototypeDungeonRunHost>();
            first.Configure(5, combatCatalog, specialCatalog, false);
            firstRoot.SetActive(true);

            GameObject duplicateRoot = CreateGameObject("DuplicateDungeonRunHost");
            duplicateRoot.SetActive(false);
            PrototypeDungeonRunHost duplicate =
                duplicateRoot.AddComponent<PrototypeDungeonRunHost>();
            duplicate.Configure(5, combatCatalog, specialCatalog, false);
            duplicateRoot.SetActive(true);

            yield return null;

            Assert.That(first, Is.Not.Null);
            Assert.That(first.IsPrimary, Is.True);
            Assert.That(first.RunSession, Is.Not.Null);
            Assert.That(first.RunSession.CurrentRoomId, Is.EqualTo(first.RunSession.Graph.StartRoomId));
            Assert.That(duplicate == null || !duplicate.IsPrimary, Is.True);
            Assert.That(
                UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Count(host => host.IsPrimary),
                Is.EqualTo(1));
        }

        [Test]
        public void Catalog_ClonesConfigurationAndLooksUpStableRoomId()
        {
            PrototypeCombatRoomDefinitionAsset room = CreateRoom("room-one");
            var source = new[]
            {
                new PrototypeDungeonCombatRoomEntry(room, "SceneOne"),
            };
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalogAsset();

            catalog.Configure(source);
            source[0] = default;
            PrototypeDungeonCombatRoomEntry stored =
                catalog.GetEntry(new RoomDefinitionId("room-one"));

            Assert.That(stored.RoomDefinition, Is.SameAs(room));
            Assert.That(stored.SceneName, Is.EqualTo("SceneOne"));
            Assert.That(catalog.CreateCoreDefinitions().Single().Id.Value, Is.EqualTo("room-one"));
            Assert.Throws<ArgumentException>(() => catalog.GetEntry(default));
            Assert.Throws<KeyNotFoundException>(() =>
                catalog.GetEntry(new RoomDefinitionId("missing")));
        }

        [Test]
        public void Catalog_RejectsMissingDuplicateOrAmbiguousEntries()
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalogAsset();
            PrototypeCombatRoomDefinitionAsset first = CreateRoom("room-one");
            PrototypeCombatRoomDefinitionAsset duplicateId = CreateRoom("room-one");
            PrototypeCombatRoomDefinitionAsset second = CreateRoom("room-two");

            Assert.Throws<ArgumentNullException>(() => catalog.Configure(null));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(Array.Empty<PrototypeDungeonCombatRoomEntry>()));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(null, "SceneOne"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(first, " "),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(first, "SceneOne"),
                    new PrototypeDungeonCombatRoomEntry(duplicateId, "SceneTwo"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(first, "SceneOne"),
                    new PrototypeDungeonCombatRoomEntry(second, "SceneOne"),
                }));
        }

        [Test]
        public void Session_RejectsMissingOrInvalidCatalogAtConstruction()
        {
            PrototypeDungeonCombatRoomCatalogAsset empty = CreateCatalogAsset();
            PrototypeDungeonSpecialRoomCatalogAsset emptySpecial =
                CreateSpecialCatalogAsset();

            Assert.Throws<ArgumentNullException>(() =>
                new PrototypeDungeonRunSession(0, null));
            Assert.Throws<ArgumentException>(() =>
                new PrototypeDungeonRunSession(0, empty));
            Assert.Throws<ArgumentException>(() =>
                new PrototypeDungeonRunSession(0, CreateCatalog(), emptySpecial));
        }

        private PrototypeDungeonCombatRoomCatalogAsset CreateCatalog()
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalogAsset();
            catalog.Configure(new[]
            {
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-alpha"), "SceneAlpha"),
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-bravo"), "SceneBravo"),
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-charlie"), "SceneCharlie"),
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-delta"), "SceneDelta"),
            });
            return catalog;
        }

        private PrototypeDungeonSpecialRoomCatalogAsset CreateSpecialCatalog()
        {
            PrototypeDungeonSpecialRoomCatalogAsset catalog =
                CreateSpecialCatalogAsset();
            catalog.Configure(CreateSpecialEntries());
            return catalog;
        }

        private PrototypeDungeonSpecialRoomCatalogAsset CreateSpecialCatalogAsset()
        {
            var catalog = ScriptableObject.CreateInstance<
                PrototypeDungeonSpecialRoomCatalogAsset>();
            _createdAssets.Add(catalog);
            return catalog;
        }

        private static PrototypeDungeonSpecialRoomEntry[] CreateSpecialEntries()
        {
            return new[]
            {
                new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "DungeonStart"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.BombReward,
                    "DungeonReward"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.BossAntechamber,
                    "DungeonBossAnte"),
                new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "DungeonBoss"),
            };
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdGameObjects.Add(gameObject);
            return gameObject;
        }

        private static string ExpectedSpecialScene(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Start:
                    return "DungeonStart";
                case RoomType.BombReward:
                    return "DungeonReward";
                case RoomType.BossAntechamber:
                    return "DungeonBossAnte";
                case RoomType.Boss:
                    return "DungeonBoss";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        null);
            }
        }

        private static RoomExitDirection Opposite(RoomExitDirection direction)
        {
            return RoomRotationUtility.Rotate(direction, RoomRotation.Clockwise180);
        }

        private PrototypeDungeonCombatRoomCatalogAsset CreateCatalogAsset()
        {
            var catalog = ScriptableObject.CreateInstance<
                PrototypeDungeonCombatRoomCatalogAsset>();
            _createdAssets.Add(catalog);
            return catalog;
        }

        private PrototypeCombatRoomDefinitionAsset CreateRoom(string id)
        {
            var room = ScriptableObject.CreateInstance<PrototypeCombatRoomDefinitionAsset>();
            room.Configure(
                id,
                RoomType.Combat,
                11,
                9,
                1f,
                Vector2Int.zero,
                new Vector2Int(1, -1),
                Array.Empty<Vector2Int>(),
                new[]
                {
                    Vector2Int.zero,
                    new Vector2Int(0, 1),
                    new Vector2Int(-1, 0),
                },
                new[]
                {
                    new Vector2Int(-3, 1),
                    new Vector2Int(3, 1),
                },
                new[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, -1),
                    new Vector2Int(0, -1),
                },
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(0, 4),
                        RoomExitDirection.North),
                    new PrototypeRoomExitData(
                        new Vector2Int(5, 0),
                        RoomExitDirection.East),
                    new PrototypeRoomExitData(
                        new Vector2Int(0, -4),
                        RoomExitDirection.South),
                    new PrototypeRoomExitData(
                        new Vector2Int(-5, 0),
                        RoomExitDirection.West),
                });
            _createdAssets.Add(room);
            return room;
        }
    }
}
