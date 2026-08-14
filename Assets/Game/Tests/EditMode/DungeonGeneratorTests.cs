using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class DungeonGeneratorTests
    {
        [Test]
        public void PrototypeDefinition_PreservesBranchAndBossPathBudget()
        {
            DungeonGenerationDefinition definition =
                DungeonGenerationDefinition.CreatePrototype();

            Assert.That(definition.MinimumCombatRooms, Is.EqualTo(4));
            Assert.That(definition.MaximumCombatRooms, Is.EqualTo(5));
            Assert.That(definition.BossPathCombatRooms, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void Generate_SameSeedAndDefinition_ReturnsIdenticalVersionedGraph(int seed)
        {
            DungeonGenerationDefinition definition =
                DungeonGenerationDefinition.CreatePrototype();

            DungeonGraph first = DungeonGenerator.Generate(seed, definition);
            DungeonGraph second = DungeonGenerator.Generate(seed, definition);

            Assert.That(first.GenerationVersion, Is.EqualTo(DungeonGenerator.GenerationVersion));
            Assert.That(second.GenerationVersion, Is.EqualTo(first.GenerationVersion));
            Assert.That(BuildSignature(second), Is.EqualTo(BuildSignature(first)));
        }

        [Test]
        public void Generate_SeedZero_MatchesVersionedGoldenSnapshot()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);

            Assert.That(
                BuildSignature(graph),
                Is.EqualTo(
                    "prototype-tree-v1|" +
                    "1:1@0,0;2:0@-1,0;3:2@-1,1;4:0@-1,2;" +
                    "5:0@0,2;6:3@1,2;7:4@1,1;8:0@-2,1;|" +
                    "1-2;2-3;3-4;4-5;5-6;6-7;3-8;"));
        }

        [Test]
        public void Generate_DefaultGraph_HasRequiredMainPathAndOptionalBranch()
        {
            DungeonGraph graph = DungeonGenerator.Generate(1729);

            Assert.That(graph.Seed, Is.EqualTo(1729));
            Assert.That(graph.CombatRoomCount, Is.InRange(4, 5));
            Assert.That(graph.Rooms.Count, Is.EqualTo(graph.CombatRoomCount + 4));
            Assert.That(graph.Connections.Count, Is.EqualTo(graph.Rooms.Count - 1));
            Assert.That(graph.GetRoom(graph.StartRoomId).RoomType, Is.EqualTo(RoomType.Start));
            Assert.That(
                graph.GetRoom(graph.BombRewardRoomId).RoomType,
                Is.EqualTo(RoomType.BombReward));
            Assert.That(
                graph.GetRoom(graph.BossAntechamberRoomId).RoomType,
                Is.EqualTo(RoomType.BossAntechamber));
            Assert.That(graph.GetRoom(graph.BossRoomId).RoomType, Is.EqualTo(RoomType.Boss));

            IReadOnlyList<DungeonRoomNodeId> bossPath =
                graph.GetShortestPath(graph.StartRoomId, graph.BossRoomId);
            Assert.That(graph.GetRoom(bossPath[0]).RoomType, Is.EqualTo(RoomType.Start));
            Assert.That(graph.GetRoom(bossPath[1]).RoomType, Is.EqualTo(RoomType.Combat));
            Assert.That(graph.GetRoom(bossPath[2]).RoomType, Is.EqualTo(RoomType.BombReward));
            Assert.That(
                graph.GetRoom(bossPath[bossPath.Count - 2]).RoomType,
                Is.EqualTo(RoomType.BossAntechamber));
            Assert.That(graph.GetRoom(bossPath[bossPath.Count - 1]).RoomType, Is.EqualTo(RoomType.Boss));
            Assert.That(
                bossPath.Count(id => graph.GetRoom(id).RoomType == RoomType.Combat),
                Is.EqualTo(3));

            var bossPathSet = new HashSet<DungeonRoomNodeId>(bossPath);
            DungeonRoomNode[] branchRooms = graph.Rooms
                .Where(room =>
                    room.RoomType == RoomType.Combat &&
                    !bossPathSet.Contains(room.Id))
                .ToArray();
            Assert.That(branchRooms.Length, Is.EqualTo(graph.CombatRoomCount - 3));
            Assert.That(branchRooms.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                branchRooms.Any(room => graph.GetNeighbors(room.Id).Count == 1),
                Is.True,
                "The optional combat branch must end in a dead end.");
            Assert.That(
                graph.Connections.Count(connection =>
                    bossPathSet.Contains(connection.First) !=
                    bossPathSet.Contains(connection.Second)),
                Is.EqualTo(1),
                "Optional combat rooms must form one branch from the boss path.");
            foreach (DungeonRoomNode branchRoom in branchRooms)
            {
                Assert.That(
                    graph.GetShortestPath(graph.StartRoomId, branchRoom.Id),
                    Does.Contain(graph.BombRewardRoomId),
                    "Optional combat branches must become reachable after the bomb reward.");
            }
        }

        [Test]
        public void Generate_BossCanOnlyBeEnteredThroughAntechamber()
        {
            DungeonGraph graph = DungeonGenerator.Generate(91);

            Assert.That(
                graph.GetNeighbors(graph.BossRoomId),
                Is.EqualTo(new[] { graph.BossAntechamberRoomId }));
            Assert.That(graph.GetNeighbors(graph.BossAntechamberRoomId).Count, Is.EqualTo(2));
            IReadOnlyList<DungeonRoomNodeId> bossPath =
                graph.GetShortestPath(graph.StartRoomId, graph.BossRoomId);
            Assert.That(
                bossPath[bossPath.Count - 2],
                Is.EqualTo(graph.BossAntechamberRoomId));
            Assert.That(bossPath[bossPath.Count - 1], Is.EqualTo(graph.BossRoomId));
        }

        [Test]
        public void Generate_ManySeeds_PreserveTreeLayoutAndUseBothCombatCounts()
        {
            var combatCounts = new HashSet<int>();
            var signatures = new HashSet<string>(StringComparer.Ordinal);

            for (int seed = -256; seed < 256; seed++)
            {
                DungeonGraph graph = DungeonGenerator.Generate(seed);
                combatCounts.Add(graph.CombatRoomCount);
                signatures.Add(BuildSignature(graph));
                AssertGraphLayout(graph);
            }

            Assert.That(combatCounts, Is.EquivalentTo(new[] { 4, 5 }));
            Assert.That(
                signatures.Count,
                Is.GreaterThan(64),
                "Seeded topology and layout should produce more than a token variation.");
        }

        [Test]
        public void Generate_CustomFixedDefinition_UsesRequestedCounts()
        {
            var definition = new DungeonGenerationDefinition(
                minimumCombatRooms: 5,
                maximumCombatRooms: 5,
                bossPathCombatRooms: 3);

            DungeonGraph graph = DungeonGenerator.Generate(44, definition);
            IReadOnlyList<DungeonRoomNodeId> bossPath =
                graph.GetShortestPath(graph.StartRoomId, graph.BossRoomId);

            Assert.That(graph.Definition, Is.SameAs(definition));
            Assert.That(graph.CombatRoomCount, Is.EqualTo(5));
            Assert.That(
                bossPath.Count(id => graph.GetRoom(id).RoomType == RoomType.Combat),
                Is.EqualTo(3));
            Assert.That(
                graph.Rooms.Count(room =>
                    room.RoomType == RoomType.Combat && !bossPath.Contains(room.Id)),
                Is.EqualTo(2));
        }

        [Test]
        public void GraphLookup_ValidatesIdsAndReturnsStablePaths()
        {
            DungeonGraph graph = DungeonGenerator.Generate(7);

            Assert.That(
                graph.GetShortestPath(graph.StartRoomId, graph.StartRoomId),
                Is.EqualTo(new[] { graph.StartRoomId }));
            Assert.That(graph.GetDistance(graph.StartRoomId, graph.StartRoomId), Is.Zero);
            Assert.That(
                graph.GetDistance(graph.StartRoomId, graph.BombRewardRoomId),
                Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() => graph.GetRoom(default));
            Assert.Throws<KeyNotFoundException>(
                () => graph.GetRoom(new DungeonRoomNodeId(graph.Rooms.Count + 1)));
        }

        [Test]
        public void GraphCollections_AreReadOnlySnapshots()
        {
            DungeonGraph graph = DungeonGenerator.Generate(8);
            var rooms = (IList<DungeonRoomNode>)graph.Rooms;
            var connections = (IList<DungeonRoomConnection>)graph.Connections;
            var neighbors = (IList<DungeonRoomNodeId>)graph.GetNeighbors(graph.StartRoomId);

            Assert.Throws<NotSupportedException>(() => rooms[0] = rooms[1]);
            Assert.Throws<NotSupportedException>(() => connections.Clear());
            Assert.Throws<NotSupportedException>(() => neighbors.Clear());
        }

        [Test]
        public void Definition_RejectsInvalidCountsWithoutPartialConstruction()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DungeonGenerationDefinition(0, 4, 3));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DungeonGenerationDefinition(4, 3, 2));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DungeonGenerationDefinition(
                    4,
                    DungeonGenerationDefinition.MaximumSupportedCombatRooms + 1,
                    3));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DungeonGenerationDefinition(4, 5, 0));
            Assert.Throws<ArgumentException>(
                () => new DungeonGenerationDefinition(3, 5, 3));
            Assert.Throws<ArgumentNullException>(() => DungeonGenerator.Generate(0, null));
        }

        [Test]
        public void RoomGraphPosition_UsesCardinalLongSafeAdjacencyAndCheckedOffsets()
        {
            var origin = new RoomGraphPosition(0, 0);

            Assert.That(origin.IsCardinallyAdjacentTo(new RoomGraphPosition(0, 1)), Is.True);
            Assert.That(origin.IsCardinallyAdjacentTo(new RoomGraphPosition(1, 1)), Is.False);
            Assert.That(
                new RoomGraphPosition(int.MinValue, 0).IsCardinallyAdjacentTo(
                    new RoomGraphPosition(int.MaxValue, 0)),
                Is.False);
            Assert.Throws<OverflowException>(
                () => new RoomGraphPosition(int.MaxValue, 0).Offset(1, 0));
        }

        private static void AssertGraphLayout(DungeonGraph graph)
        {
            Assert.That(graph.Connections.Count, Is.EqualTo(graph.Rooms.Count - 1));
            Assert.That(graph.Rooms.Select(room => room.Id.Value), Is.EqualTo(
                Enumerable.Range(1, graph.Rooms.Count)));
            Assert.That(
                graph.Rooms.Select(room => room.Position).Distinct().Count(),
                Is.EqualTo(graph.Rooms.Count));

            foreach (DungeonRoomConnection connection in graph.Connections)
            {
                Assert.That(
                    graph.GetRoom(connection.First).Position.IsCardinallyAdjacentTo(
                        graph.GetRoom(connection.Second).Position),
                    Is.True);
            }

            for (int left = 0; left < graph.Rooms.Count; left++)
            {
                for (int right = left + 1; right < graph.Rooms.Count; right++)
                {
                    DungeonRoomNode leftRoom = graph.Rooms[left];
                    DungeonRoomNode rightRoom = graph.Rooms[right];
                    bool adjacent = leftRoom.Position.IsCardinallyAdjacentTo(rightRoom.Position);
                    bool connected = graph.Connections.Any(connection =>
                        connection.Contains(leftRoom.Id) &&
                        connection.Contains(rightRoom.Id));
                    Assert.That(
                        adjacent,
                        Is.EqualTo(connected),
                        $"Rooms {leftRoom.Id} and {rightRoom.Id} adjacency/connection mismatch.");
                }
            }

            foreach (DungeonRoomNode room in graph.Rooms)
            {
                Assert.That(
                    graph.GetDistance(graph.StartRoomId, room.Id),
                    Is.GreaterThanOrEqualTo(0));
            }
        }

        private static string BuildSignature(DungeonGraph graph)
        {
            var builder = new StringBuilder();
            builder.Append(graph.GenerationVersion);
            builder.Append('|');
            foreach (DungeonRoomNode room in graph.Rooms)
            {
                builder.Append(room.Id.Value);
                builder.Append(':');
                builder.Append((int)room.RoomType);
                builder.Append('@');
                builder.Append(room.Position.X);
                builder.Append(',');
                builder.Append(room.Position.Z);
                builder.Append(';');
            }
            builder.Append('|');
            foreach (DungeonRoomConnection connection in graph.Connections)
            {
                builder.Append(connection.First.Value);
                builder.Append('-');
                builder.Append(connection.Second.Value);
                builder.Append(';');
            }
            return builder.ToString();
        }
    }
}
