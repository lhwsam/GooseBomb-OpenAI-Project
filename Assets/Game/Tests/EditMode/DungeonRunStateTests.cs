using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class DungeonRunStateTests
    {
        [Test]
        public void Constructor_StartsAtVisitedUnlockedStartRoom()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            var run = new DungeonRunState(graph);

            Assert.That(run.Graph, Is.SameAs(graph));
            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.StartRoomId));
            Assert.That(run.PreviousRoomId.IsValid, Is.False);
            Assert.That(run.Outcome, Is.EqualTo(DungeonRunOutcome.InProgress));
            Assert.That(run.FailureDamage.HasValue, Is.False);
            Assert.That(run.IsTerminal, Is.False);
            Assert.That(run.IsVisited(graph.StartRoomId), Is.True);
            Assert.That(run.IsCleared(graph.StartRoomId), Is.False);
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            Assert.That(run.GetVisitedRooms(), Is.EqualTo(new[] { graph.StartRoomId }));
            Assert.That(run.GetClearedRooms(), Is.Empty);
            Assert.Throws<NotSupportedException>(
                () => ((IList<DungeonRoomNodeId>)run.GetVisitedRooms()).Clear());
            Assert.Throws<NotSupportedException>(
                () => ((IList<DungeonRoomNodeId>)run.GetClearedRooms()).Clear());
        }

        [Test]
        public void FirstCombatEntry_LocksAllConnectedTravelUntilCleared()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId)[0];

            DungeonTravelResult entered = run.TryTravelTo(firstCombat);
            DungeonTravelResult blocked = run.TryTravelTo(graph.StartRoomId);

            Assert.That(entered.Moved, Is.True);
            Assert.That(entered.EnteredFirstTime, Is.True);
            Assert.That(run.CurrentRoomId, Is.EqualTo(firstCombat));
            Assert.That(run.IsCurrentRoomLocked, Is.True);
            Assert.That(blocked.Status, Is.EqualTo(DungeonTravelStatus.BlockedByUnclearedRoom));
            Assert.That(blocked.Moved, Is.False);
            Assert.That(run.CurrentRoomId, Is.EqualTo(firstCombat));
        }

        [Test]
        public void ClearedCombatRoom_RemainsOpenAcrossBidirectionalRevisit()
        {
            DungeonGraph graph = DungeonGenerator.Generate(12);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId)[0];

            Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.AlreadyCleared));
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            Assert.That(run.TryTravelTo(graph.StartRoomId).Moved, Is.True);

            DungeonTravelResult revisited = run.TryTravelTo(firstCombat);

            Assert.That(revisited.Moved, Is.True);
            Assert.That(revisited.EnteredFirstTime, Is.False);
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            Assert.That(run.IsCleared(firstCombat), Is.True);
            Assert.That(run.PreviousRoomId, Is.EqualTo(graph.StartRoomId));
        }

        [Test]
        public void SafeRooms_CannotBeMarkedClearedAndNeverLockTravel()
        {
            DungeonGraph graph = DungeonGenerator.Generate(8);
            var run = new DungeonRunState(graph);

            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.NotClearable));
            TraversePath(run, graph.GetShortestPath(graph.StartRoomId, graph.BombRewardRoomId));

            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.BombRewardRoomId));
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.NotClearable));
        }

        [Test]
        public void NonConnectedTravel_DoesNotMutateRunState()
        {
            DungeonGraph graph = DungeonGenerator.Generate(21);
            var run = new DungeonRunState(graph);

            DungeonTravelResult result = run.TryTravelTo(graph.BossRoomId);

            Assert.That(result.Status, Is.EqualTo(DungeonTravelStatus.NotConnected));
            Assert.That(result.FromRoomId, Is.EqualTo(graph.StartRoomId));
            Assert.That(result.TargetRoomId, Is.EqualTo(graph.BossRoomId));
            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.StartRoomId));
            Assert.That(run.PreviousRoomId.IsValid, Is.False);
            Assert.That(run.GetVisitedRooms(), Is.EqualTo(new[] { graph.StartRoomId }));
        }

        [Test]
        public void DirectionTravel_UsesGraphCoordinatesAndRejectsMissingExit()
        {
            DungeonGraph graph = DungeonGenerator.Generate(34);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId)[0];
            RoomExitDirection direction = graph.GetExitDirection(graph.StartRoomId, firstCombat);
            RoomExitDirection missingDirection = Enum.GetValues(typeof(RoomExitDirection))
                .Cast<RoomExitDirection>()
                .First(candidate => candidate != direction &&
                    !graph.TryGetNeighbor(graph.StartRoomId, candidate, out _));

            DungeonTravelResult missing = run.TryTravel(missingDirection);
            DungeonTravelResult moved = run.TryTravel(direction);

            Assert.That(missing.Status, Is.EqualTo(DungeonTravelStatus.NotConnected));
            Assert.That(missing.TargetRoomId.IsValid, Is.False);
            Assert.That(moved.Moved, Is.True);
            Assert.That(moved.TargetRoomId, Is.EqualTo(firstCombat));
        }

        [Test]
        public void StartRoomExitSnapshot_UsesStableFourDirectionOrder()
        {
            DungeonGraph graph = DungeonGenerator.Generate(34);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId)[0];
            RoomExitDirection connectedDirection =
                graph.GetExitDirection(graph.StartRoomId, firstCombat);

            IReadOnlyList<DungeonRoomExitState> exits = run.GetCurrentExitStates();

            Assert.That(
                exits.Select(exit => exit.Direction),
                Is.EqualTo(new[]
                {
                    RoomExitDirection.North,
                    RoomExitDirection.East,
                    RoomExitDirection.South,
                    RoomExitDirection.West,
                }));
            Assert.That(exits.Count(exit => exit.IsConnected), Is.EqualTo(1));
            DungeonRoomExitState connected =
                exits.Single(exit => exit.Direction == connectedDirection);
            Assert.That(connected.TargetRoomId, Is.EqualTo(firstCombat));
            Assert.That(connected.Status, Is.EqualTo(DungeonRoomExitStatus.Open));
            Assert.That(connected.CanTravel, Is.True);
            Assert.Throws<NotSupportedException>(
                () => ((IList<DungeonRoomExitState>)exits).Clear());
        }

        [Test]
        public void CombatExitSnapshot_LocksEveryConnectionUntilRoomClear()
        {
            DungeonGraph graph = DungeonGenerator.Generate(91);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId)[0];
            Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);

            IReadOnlyList<DungeonRoomExitState> locked = run.GetCurrentExitStates();

            Assert.That(locked.Where(exit => exit.IsConnected), Is.Not.Empty);
            Assert.That(
                locked.Where(exit => exit.IsConnected).Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Locked));
            Assert.That(locked.Any(exit => exit.CanTravel), Is.False);

            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            IReadOnlyList<DungeonRoomExitState> opened = run.GetCurrentExitStates();

            Assert.That(
                opened.Where(exit => exit.IsConnected).Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Open));
            Assert.That(
                opened.Where(exit => exit.IsConnected).Select(exit => exit.TargetRoomId),
                Is.EqualTo(locked.Where(exit => exit.IsConnected)
                    .Select(exit => exit.TargetRoomId)));
        }

        [Test]
        public void GraphDirections_AreOppositeAcrossEveryConnection()
        {
            DungeonGraph graph = DungeonGenerator.Generate(55);

            foreach (DungeonRoomConnection connection in graph.Connections)
            {
                RoomExitDirection forward =
                    graph.GetExitDirection(connection.First, connection.Second);
                RoomExitDirection reverse =
                    graph.GetExitDirection(connection.Second, connection.First);

                Assert.That(reverse, Is.EqualTo(Opposite(forward)));
                Assert.That(
                    graph.TryGetNeighbor(connection.First, forward, out DungeonRoomNodeId found),
                    Is.True);
                Assert.That(found, Is.EqualTo(connection.Second));
            }
        }

        [Test]
        public void FullExploration_CanClearAndRevisitEveryRoomOnTree()
        {
            DungeonGraph graph = DungeonGenerator.Generate(144);
            var run = new DungeonRunState(graph);

            foreach (DungeonRoomNode room in graph.Rooms.Where(
                room => room.Id != graph.BossRoomId))
            {
                TraversePath(run, graph.GetShortestPath(run.CurrentRoomId, room.Id));
            }
            TraversePath(run, graph.GetShortestPath(run.CurrentRoomId, graph.BossRoomId));

            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.BossRoomId));
            Assert.That(run.Outcome, Is.EqualTo(DungeonRunOutcome.Completed));
            Assert.That(run.GetVisitedRooms(), Is.EqualTo(graph.Rooms.Select(room => room.Id)));
            Assert.That(
                run.GetClearedRooms(),
                Is.EqualTo(graph.Rooms
                    .Where(room => DungeonRunState.RequiresClear(room.RoomType))
                    .Select(room => room.Id)));
        }

        [Test]
        public void BossClear_CompletesRunAndRejectsFurtherMutation()
        {
            DungeonGraph graph = DungeonGenerator.Generate(377);
            var run = new DungeonRunState(graph);
            TraverseToBoss(run, graph);

            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));

            Assert.That(run.Outcome, Is.EqualTo(DungeonRunOutcome.Completed));
            Assert.That(run.IsTerminal, Is.True);
            Assert.That(run.FailureDamage.HasValue, Is.False);
            Assert.That(run.TryFail(CreateContactDamage(1, 1)), Is.False);
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.RunFinished));
            DungeonRoomNodeId neighbor = graph.GetNeighbors(graph.BossRoomId)[0];
            Assert.That(
                run.TryTravelTo(neighbor).Status,
                Is.EqualTo(DungeonTravelStatus.RunFinished));
            Assert.That(
                run.GetCurrentExitStates()
                    .Where(exit => exit.IsConnected)
                    .Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Locked));
        }

        [Test]
        public void Failure_IsIdempotentAndWinsBeforeSameFrameBossClear()
        {
            DungeonGraph graph = DungeonGenerator.Generate(610);
            var run = new DungeonRunState(graph);
            TraverseToBoss(run, graph);

            PlayerDamageResult nonFatal = CreateContactDamage(2, 1);
            PlayerDamageResult fatal = CreateContactDamage(1, 1);
            Assert.Throws<ArgumentException>(() => run.TryFail(nonFatal));
            Assert.That(run.Outcome, Is.EqualTo(DungeonRunOutcome.InProgress));
            Assert.That(run.FailureDamage.HasValue, Is.False);

            Assert.That(run.TryFail(fatal), Is.True);
            Assert.That(run.TryFail(fatal), Is.False);
            Assert.That(run.Outcome, Is.EqualTo(DungeonRunOutcome.Failed));
            Assert.That(run.IsTerminal, Is.True);
            Assert.That(run.FailureDamage, Is.EqualTo(fatal));
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.RunFinished));
            Assert.That(run.IsCleared(graph.BossRoomId), Is.False);
            Assert.That(
                run.TryTravelTo(graph.GetNeighbors(graph.BossRoomId)[0]).Status,
                Is.EqualTo(DungeonTravelStatus.RunFinished));
        }

        [Test]
        public void InvalidInputs_AreRejectedWithoutPartialMutation()
        {
            DungeonGraph graph = DungeonGenerator.Generate(233);
            var run = new DungeonRunState(graph);

            Assert.Throws<ArgumentNullException>(() => new DungeonRunState(null));
            Assert.Throws<ArgumentException>(() => run.IsVisited(default));
            Assert.Throws<KeyNotFoundException>(
                () => run.TryTravelTo(new DungeonRoomNodeId(graph.Rooms.Count + 1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => run.TryTravel((RoomExitDirection)999));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => run.GetCurrentExitState((RoomExitDirection)999));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => DungeonRunState.RequiresClear((RoomType)999));
            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.StartRoomId));
            Assert.That(run.GetVisitedRooms(), Is.EqualTo(new[] { graph.StartRoomId }));
        }

        private static void TraversePath(
            DungeonRunState run,
            IReadOnlyList<DungeonRoomNodeId> path)
        {
            Assert.That(path[0], Is.EqualTo(run.CurrentRoomId));
            for (int index = 1; index < path.Count; index++)
            {
                if (run.IsCurrentRoomLocked)
                {
                    Assert.That(
                        run.TryClearCurrentRoom(),
                        Is.EqualTo(DungeonRoomClearStatus.Cleared));
                }

                Assert.That(run.TryTravelTo(path[index]).Moved, Is.True);
            }
            if (run.IsCurrentRoomLocked)
            {
                Assert.That(
                    run.TryClearCurrentRoom(),
                    Is.EqualTo(DungeonRoomClearStatus.Cleared));
            }
        }

        private static void TraverseToBoss(DungeonRunState run, DungeonGraph graph)
        {
            IReadOnlyList<DungeonRoomNodeId> path = graph.GetShortestPath(
                run.CurrentRoomId,
                graph.BossRoomId);
            Assert.That(path[0], Is.EqualTo(run.CurrentRoomId));
            for (int index = 1; index < path.Count; index++)
            {
                if (run.IsCurrentRoomLocked)
                {
                    Assert.That(
                        run.TryClearCurrentRoom(),
                        Is.EqualTo(DungeonRoomClearStatus.Cleared));
                }

                Assert.That(run.TryTravelTo(path[index]).Moved, Is.True);
            }
        }

        private static PlayerDamageResult CreateContactDamage(
            int maxHealth,
            int damage)
        {
            var health = new PlayerHealthSimulation(
                new ActorId(1),
                new ManualGameClock(),
                new PlayerHealthDefinition(maxHealth, TimeSpan.FromSeconds(0.75)));
            return health.ApplyContactDamage(new ActorId(2), damage);
        }

        private static RoomExitDirection Opposite(RoomExitDirection direction)
        {
            switch (direction)
            {
                case RoomExitDirection.North:
                    return RoomExitDirection.South;
                case RoomExitDirection.East:
                    return RoomExitDirection.West;
                case RoomExitDirection.South:
                    return RoomExitDirection.North;
                case RoomExitDirection.West:
                    return RoomExitDirection.East;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
