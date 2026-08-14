using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class DungeonCombatRoomAssignerTests
    {
        [Test]
        public void Assign_SameSeedAndCatalog_IgnoresCatalogInputOrder()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            CombatRoomDefinition[] catalog = CreateFourWayCatalog();
            CombatRoomDefinition[] reversed = catalog.Reverse().ToArray();

            DungeonCombatRoomLayout first =
                DungeonCombatRoomAssigner.Assign(graph, catalog);
            DungeonCombatRoomLayout second =
                DungeonCombatRoomAssigner.Assign(graph, reversed);

            Assert.That(
                first.AssignmentVersion,
                Is.EqualTo(DungeonCombatRoomAssigner.AssignmentVersion));
            Assert.That(BuildSignature(second), Is.EqualTo(BuildSignature(first)));
        }

        [Test]
        public void Assign_ActiveExitsMatchGraphAndRotatedAuthoredExits()
        {
            DungeonGraph graph = DungeonGenerator.Generate(1729);
            CombatRoomDefinition[] catalog = CreateFourWayCatalog();

            DungeonCombatRoomLayout layout =
                DungeonCombatRoomAssigner.Assign(graph, catalog);

            Assert.That(layout.Graph, Is.SameAs(graph));
            Assert.That(layout.Assignments, Has.Count.EqualTo(graph.CombatRoomCount));
            foreach (DungeonCombatRoomAssignment assignment in layout.Assignments)
            {
                DungeonRoomNode room = graph.GetRoom(assignment.RoomId);
                CombatRoomDefinition definition = catalog.Single(candidate =>
                    candidate.Id == assignment.DefinitionId);
                RoomExitDirection[] expected = graph.GetNeighbors(room.Id)
                    .Select(neighbor => graph.GetExitDirection(room.Id, neighbor))
                    .OrderBy(direction => direction)
                    .ToArray();

                Assert.That(room.RoomType, Is.EqualTo(RoomType.Combat));
                Assert.That(assignment.ActiveExitDirections, Is.EqualTo(expected));
                Assert.That(
                    DungeonCombatRoomAssigner.SupportsActiveExits(
                        definition,
                        assignment.Rotation,
                        assignment.ActiveExitDirections),
                    Is.True);
                foreach (RoomExitDirection direction in expected)
                {
                    Assert.That(assignment.IsExitActive(direction), Is.True);
                }
            }
        }

        [Test]
        public void Assign_UsesEveryCompatibleDefinitionBeforeReuse()
        {
            DungeonGraph graph = Enumerable.Range(0, 128)
                .Select(DungeonGenerator.Generate)
                .First(candidate => candidate.CombatRoomCount == 5);
            CombatRoomDefinition[] catalog = CreateFourWayCatalog();

            DungeonCombatRoomLayout layout =
                DungeonCombatRoomAssigner.Assign(graph, catalog);
            int[] usageCounts = catalog
                .Select(definition => layout.Assignments.Count(assignment =>
                    assignment.DefinitionId == definition.Id))
                .ToArray();

            Assert.That(usageCounts, Is.All.GreaterThanOrEqualTo(1));
            Assert.That(usageCounts.Max() - usageCounts.Min(), Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void Assign_FiveRoomCatalogAndFiveCombatGraph_UsesEveryDefinitionOnce()
        {
            DungeonGraph graph = Enumerable.Range(0, 128)
                .Select(DungeonGenerator.Generate)
                .First(candidate => candidate.CombatRoomCount == 5);
            CombatRoomDefinition[] catalog = CreateFiveWayCatalog();

            DungeonCombatRoomLayout layout =
                DungeonCombatRoomAssigner.Assign(graph, catalog);

            Assert.That(layout.Assignments, Has.Count.EqualTo(5));
            Assert.That(
                layout.Assignments
                    .Select(assignment => assignment.DefinitionId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5));
            foreach (CombatRoomDefinition definition in catalog)
            {
                Assert.That(
                    layout.Assignments.Count(assignment =>
                        assignment.DefinitionId == definition.Id),
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void Assign_ManySeedsProduceVariedDeterministicLayouts()
        {
            CombatRoomDefinition[] catalog = CreateFourWayCatalog();
            var signatures = new HashSet<string>(StringComparer.Ordinal);

            for (int seed = 0; seed < 128; seed++)
            {
                DungeonCombatRoomLayout layout = DungeonCombatRoomAssigner.Assign(
                    DungeonGenerator.Generate(seed),
                    catalog);
                signatures.Add(BuildSignature(layout));
            }

            Assert.That(signatures.Count, Is.GreaterThan(32));
        }

        [Test]
        public void Assign_StraightOnlyCatalogRejectsCornerOrBranchNode()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            CombatRoomDefinition straightOnly = CreateDefinition(
                "straight-only",
                CreateStraightExits());

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                DungeonCombatRoomAssigner.Assign(graph, new[] { straightOnly }));

            Assert.That(exception.Message, Does.Contain("No authored combat room"));
        }

        [Test]
        public void SupportsActiveExits_RotationTurnsStraightExitPair()
        {
            CombatRoomDefinition straight = CreateDefinition(
                "straight",
                CreateStraightExits());

            Assert.That(
                DungeonCombatRoomAssigner.SupportsActiveExits(
                    straight,
                    RoomRotation.None,
                    new[] { RoomExitDirection.North, RoomExitDirection.South }),
                Is.True);
            Assert.That(
                DungeonCombatRoomAssigner.SupportsActiveExits(
                    straight,
                    RoomRotation.Clockwise90,
                    new[] { RoomExitDirection.East, RoomExitDirection.West }),
                Is.True);
            Assert.That(
                DungeonCombatRoomAssigner.SupportsActiveExits(
                    straight,
                    RoomRotation.Clockwise90,
                    new[] { RoomExitDirection.North, RoomExitDirection.East }),
                Is.False);
        }

        [Test]
        public void RoomRotation_UsesClockwiseUnityYawDirectionOrder()
        {
            Assert.That(
                RoomRotationUtility.Rotate(
                    RoomExitDirection.North,
                    RoomRotation.Clockwise90),
                Is.EqualTo(RoomExitDirection.East));
            Assert.That(
                RoomRotationUtility.Rotate(
                    RoomExitDirection.West,
                    RoomRotation.Clockwise270),
                Is.EqualTo(RoomExitDirection.South));
            Assert.That(
                RoomRotationUtility.GetClockwiseDegrees(RoomRotation.Clockwise180),
                Is.EqualTo(180));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoomRotationUtility.Rotate((RoomExitDirection)999, RoomRotation.None));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoomRotationUtility.GetClockwiseDegrees((RoomRotation)999));
        }

        [Test]
        public void Assign_RejectsInvalidCatalogWithoutPartialLayout()
        {
            DungeonGraph graph = DungeonGenerator.Generate(9);
            CombatRoomDefinition duplicate = CreateDefinition(
                "duplicate",
                CreateFourWayExits());

            Assert.Throws<ArgumentNullException>(() =>
                DungeonCombatRoomAssigner.Assign(null, CreateFourWayCatalog()));
            Assert.Throws<ArgumentNullException>(() =>
                DungeonCombatRoomAssigner.Assign(graph, null));
            Assert.Throws<ArgumentException>(() =>
                DungeonCombatRoomAssigner.Assign(
                    graph,
                    Array.Empty<CombatRoomDefinition>()));
            Assert.Throws<ArgumentException>(() =>
                DungeonCombatRoomAssigner.Assign(
                    graph,
                    new CombatRoomDefinition[] { null }));
            Assert.Throws<ArgumentException>(() =>
                DungeonCombatRoomAssigner.Assign(
                    graph,
                    new[] { duplicate, duplicate }));
        }

        [Test]
        public void Layout_CollectionsAndLookupAreReadOnlyAndCombatOnly()
        {
            DungeonGraph graph = DungeonGenerator.Generate(19);
            DungeonCombatRoomLayout layout = DungeonCombatRoomAssigner.Assign(
                graph,
                CreateFourWayCatalog());
            DungeonCombatRoomAssignment first = layout.Assignments[0];

            Assert.That(layout.GetAssignment(first.RoomId), Is.SameAs(first));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<DungeonCombatRoomAssignment>)layout.Assignments).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<RoomExitDirection>)first.ActiveExitDirections).Clear());
            Assert.Throws<InvalidOperationException>(() =>
                layout.GetAssignment(graph.StartRoomId));
            Assert.Throws<KeyNotFoundException>(() =>
                layout.GetAssignment(new DungeonRoomNodeId(graph.Rooms.Count + 1)));
        }

        private static CombatRoomDefinition[] CreateFourWayCatalog()
        {
            return new[]
            {
                CreateDefinition("room-alpha", CreateFourWayExits()),
                CreateDefinition("room-bravo", CreateFourWayExits()),
                CreateDefinition("room-charlie", CreateFourWayExits()),
                CreateDefinition("room-delta", CreateFourWayExits()),
            };
        }

        private static CombatRoomDefinition[] CreateFiveWayCatalog()
        {
            return new[]
            {
                CreateDefinition("room-alpha", CreateFourWayExits()),
                CreateDefinition("room-bravo", CreateFourWayExits()),
                CreateDefinition("room-charlie", CreateFourWayExits()),
                CreateDefinition("room-delta", CreateFourWayExits()),
                CreateDefinition("room-echo", CreateFourWayExits()),
            };
        }

        private static CombatRoomDefinition CreateDefinition(
            string id,
            RoomExit[] exits)
        {
            return new CombatRoomDefinition(
                new RoomDefinitionId(id),
                RoomType.Combat,
                11,
                9,
                new GridPosition(0, 0),
                new GridPosition(1, -1),
                Array.Empty<GridPosition>(),
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(0, 1),
                    new GridPosition(-1, 0),
                },
                new[]
                {
                    new GridPosition(-3, 1),
                    new GridPosition(3, 1),
                },
                new[]
                {
                    new GridPosition(-1, -1),
                    new GridPosition(-1, 0),
                    new GridPosition(-1, 1),
                    new GridPosition(0, 1),
                    new GridPosition(1, 1),
                    new GridPosition(1, 0),
                    new GridPosition(1, -1),
                    new GridPosition(0, -1),
                },
                exits);
        }

        private static RoomExit[] CreateStraightExits()
        {
            return new[]
            {
                new RoomExit(new GridPosition(0, 4), RoomExitDirection.North),
                new RoomExit(new GridPosition(0, -4), RoomExitDirection.South),
            };
        }

        private static RoomExit[] CreateFourWayExits()
        {
            return new[]
            {
                new RoomExit(new GridPosition(0, 4), RoomExitDirection.North),
                new RoomExit(new GridPosition(5, 0), RoomExitDirection.East),
                new RoomExit(new GridPosition(0, -4), RoomExitDirection.South),
                new RoomExit(new GridPosition(-5, 0), RoomExitDirection.West),
            };
        }

        private static string BuildSignature(DungeonCombatRoomLayout layout)
        {
            var builder = new StringBuilder();
            builder.Append(layout.AssignmentVersion).Append('|');
            foreach (DungeonCombatRoomAssignment assignment in layout.Assignments)
            {
                builder
                    .Append(assignment.RoomId.Value)
                    .Append(':')
                    .Append(assignment.DefinitionId.Value)
                    .Append('@')
                    .Append((int)assignment.Rotation)
                    .Append('[');
                foreach (RoomExitDirection direction in assignment.ActiveExitDirections)
                {
                    builder.Append((int)direction);
                }
                builder.Append("];");
            }
            return builder.ToString();
        }
    }
}
