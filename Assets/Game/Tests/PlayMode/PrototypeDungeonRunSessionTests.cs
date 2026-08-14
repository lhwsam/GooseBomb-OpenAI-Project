using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeDungeonRunSessionTests
    {
        private readonly List<ScriptableObject> _createdAssets =
            new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
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

            Assert.Throws<ArgumentNullException>(() =>
                new PrototypeDungeonRunSession(0, null));
            Assert.Throws<ArgumentException>(() =>
                new PrototypeDungeonRunSession(0, empty));
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
