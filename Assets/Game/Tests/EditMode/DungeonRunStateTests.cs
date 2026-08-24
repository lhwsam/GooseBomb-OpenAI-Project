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
            Assert.That(run.CombatRewardTokenCount, Is.Zero);
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
        public void MinimapSnapshot_InitiallyRevealsOnlyStartAndItsDirectConnection()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId).Single();
            DungeonRoomNodeId reward = graph.BombRewardRoomId;

            DungeonMinimapSnapshot snapshot = run.CreateMinimapSnapshot();

            Assert.That(snapshot.CurrentRoomId, Is.EqualTo(graph.StartRoomId));
            Assert.That(snapshot.Rooms.Select(room => room.RoomId), Is.EqualTo(
                new[] { graph.StartRoomId, firstCombat }));
            Assert.That(
                snapshot.GetRoom(graph.StartRoomId).State,
                Is.EqualTo(DungeonMinimapRoomState.Current));
            Assert.That(
                snapshot.GetRoom(graph.StartRoomId).KnownRoomType,
                Is.EqualTo(RoomType.Start));
            Assert.That(
                snapshot.GetRoom(firstCombat).State,
                Is.EqualTo(DungeonMinimapRoomState.Discovered));
            Assert.That(
                snapshot.GetRoom(firstCombat).HasKnownRoomType,
                Is.False);
            Assert.That(
                snapshot.Connections,
                Is.EqualTo(new[] { graph.Connections.Single(
                    connection => connection.Contains(graph.StartRoomId)) }));
            Assert.Throws<KeyNotFoundException>(() => snapshot.GetRoom(reward));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<DungeonMinimapRoomSnapshot>)snapshot.Rooms).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<DungeonRoomConnection>)snapshot.Connections).Clear());
        }

        [Test]
        public void MinimapSnapshot_AfterTravelShowsVisitedRoomsAndOneStepFrontierOnly()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId).Single();
            Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);

            DungeonMinimapSnapshot snapshot = run.CreateMinimapSnapshot();

            Assert.That(snapshot.Rooms.Count, Is.EqualTo(3));
            Assert.That(snapshot.Connections.Count, Is.EqualTo(2));
            Assert.That(
                snapshot.GetRoom(graph.StartRoomId).State,
                Is.EqualTo(DungeonMinimapRoomState.Visited));
            Assert.That(
                snapshot.GetRoom(graph.StartRoomId).KnownRoomType,
                Is.EqualTo(RoomType.Start));
            Assert.That(
                snapshot.GetRoom(firstCombat).State,
                Is.EqualTo(DungeonMinimapRoomState.Current));
            Assert.That(
                snapshot.GetRoom(firstCombat).KnownRoomType,
                Is.EqualTo(RoomType.Combat));
            Assert.That(
                snapshot.GetRoom(graph.BombRewardRoomId).State,
                Is.EqualTo(DungeonMinimapRoomState.Discovered));
            Assert.That(
                snapshot.GetRoom(graph.BombRewardRoomId).HasKnownRoomType,
                Is.False);
            Assert.That(
                snapshot.Rooms.All(room =>
                    run.IsVisited(room.RoomId) ||
                    graph.GetNeighbors(room.RoomId).Any(run.IsVisited)),
                Is.True);
            Assert.That(
                snapshot.Connections.All(connection =>
                    run.IsVisited(connection.First) ||
                    run.IsVisited(connection.Second)),
                Is.True);
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
            Assert.That(run.CombatRewardTokenCount, Is.EqualTo(1));
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.AlreadyCleared));
            Assert.That(run.CombatRewardTokenCount, Is.EqualTo(1));
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
            Assert.That(run.CombatRewardTokenCount, Is.Zero);
            TraversePath(run, graph.GetShortestPath(graph.StartRoomId, graph.BombRewardRoomId));

            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.BombRewardRoomId));
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            int tokensBeforeSafeRoomClear = run.CombatRewardTokenCount;
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.NotClearable));
            Assert.That(
                run.CombatRewardTokenCount,
                Is.EqualTo(tokensBeforeSafeRoomClear));
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
        public void CombatExitSnapshot_LocksNormalConnectionsAndPreservesSecretWalls()
        {
            DungeonGraph graph = DungeonGenerator.Generate(91);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId)[0];
            Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);

            IReadOnlyList<DungeonRoomExitState> locked = run.GetCurrentExitStates();

            Assert.That(locked.Where(exit => exit.IsConnected), Is.Not.Empty);
            Assert.That(
                locked.Where(exit => exit.IsConnected &&
                    exit.Status != DungeonRoomExitStatus.SecretWall)
                    .Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Locked));
            Assert.That(locked.Any(exit => exit.CanTravel), Is.False);

            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            IReadOnlyList<DungeonRoomExitState> opened = run.GetCurrentExitStates();

            Assert.That(
                opened.Where(exit => exit.IsConnected &&
                    exit.Status != DungeonRoomExitStatus.SecretWall)
                    .Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Open));
            Assert.That(
                opened.Count(exit => exit.Status == DungeonRoomExitStatus.SecretWall),
                Is.EqualTo(locked.Count(exit =>
                    exit.Status == DungeonRoomExitStatus.SecretWall)));
            Assert.That(
                opened.Where(exit => exit.IsConnected).Select(exit => exit.TargetRoomId),
                Is.EqualTo(locked.Where(exit => exit.IsConnected)
                    .Select(exit => exit.TargetRoomId)));
        }

        [Test]
        public void SecretExit_StaysOffMinimapAndBlocksTravelUntilRevealedIndividually()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            Assert.That(graph.HasSecretRoom, Is.True);
            var run = new DungeonRunState(graph);
            DungeonRoomNodeId firstCombat = graph.GetNeighbors(graph.StartRoomId).Single();
            Assert.That(graph.GetNeighbors(graph.SecretRoomId), Does.Contain(firstCombat));
            RoomExitDirection secretDirection =
                graph.GetExitDirection(firstCombat, graph.SecretRoomId);

            Assert.That(run.CreateMinimapSnapshot().ContainsRoom(graph.SecretRoomId), Is.False);
            Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);
            DungeonMinimapSnapshot hidden = run.CreateMinimapSnapshot();
            Assert.That(hidden.ContainsRoom(graph.SecretRoomId), Is.False);
            Assert.That(
                hidden.Connections.Any(connection =>
                    connection.Contains(graph.SecretRoomId)),
                Is.False);

            DungeonRoomExitState secretWall = run.GetCurrentExitState(secretDirection);
            Assert.That(secretWall.TargetRoomId, Is.EqualTo(graph.SecretRoomId));
            Assert.That(secretWall.Status, Is.EqualTo(DungeonRoomExitStatus.SecretWall));
            Assert.That(secretWall.CanTravel, Is.False);
            Assert.That(
                run.TryTravel(secretDirection).Status,
                Is.EqualTo(DungeonTravelStatus.BlockedBySecretWall));
            Assert.That(run.CurrentRoomId, Is.EqualTo(firstCombat));

            DungeonSecretExitRevealResult revealed =
                run.TryRevealCurrentSecretExit(secretDirection);
            Assert.That(revealed.Status, Is.EqualTo(DungeonSecretExitRevealStatus.Revealed));
            Assert.That(revealed.WasRevealed, Is.True);
            Assert.That(revealed.TargetRoomId, Is.EqualTo(graph.SecretRoomId));
            Assert.That(
                run.IsSecretConnectionRevealed(firstCombat, graph.SecretRoomId),
                Is.True);
            Assert.That(
                run.TryRevealCurrentSecretExit(secretDirection).Status,
                Is.EqualTo(DungeonSecretExitRevealStatus.AlreadyRevealed));

            DungeonMinimapSnapshot disclosed = run.CreateMinimapSnapshot();
            Assert.That(disclosed.ContainsRoom(graph.SecretRoomId), Is.True);
            Assert.That(
                disclosed.GetRoom(graph.SecretRoomId).State,
                Is.EqualTo(DungeonMinimapRoomState.Discovered));
            Assert.That(
                disclosed.GetRoom(graph.SecretRoomId).HasKnownRoomType,
                Is.False);
            Assert.That(
                disclosed.Connections.Count(connection =>
                    connection.Contains(graph.SecretRoomId)),
                Is.EqualTo(1));
            Assert.That(
                run.GetCurrentExitState(secretDirection).Status,
                Is.EqualTo(DungeonRoomExitStatus.Locked),
                "Revealing the wall must not bypass the current combat-room lock.");
            Assert.That(
                run.TryTravel(secretDirection).Status,
                Is.EqualTo(DungeonTravelStatus.BlockedByUnclearedRoom));

            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                run.GetCurrentExitState(secretDirection).Status,
                Is.EqualTo(DungeonRoomExitStatus.Open));
            Assert.That(run.TryTravel(secretDirection).Moved, Is.True);
            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.SecretRoomId));
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            Assert.That(
                run.CreateMinimapSnapshot()
                    .GetRoom(graph.SecretRoomId)
                    .KnownRoomType,
                Is.EqualTo(RoomType.Secret));

            DungeonRoomNodeId[] otherNeighbors = graph.GetNeighbors(graph.SecretRoomId)
                .Where(roomId => roomId != firstCombat)
                .ToArray();
            Assert.That(otherNeighbors, Is.Not.Empty);
            foreach (DungeonRoomNodeId other in otherNeighbors)
            {
                RoomExitDirection direction =
                    graph.GetExitDirection(graph.SecretRoomId, other);
                Assert.That(
                    run.GetCurrentExitState(direction).Status,
                    Is.EqualTo(DungeonRoomExitStatus.SecretWall));
                Assert.That(run.IsSecretConnectionRevealed(graph.SecretRoomId, other), Is.False);
                Assert.That(run.CreateMinimapSnapshot().ContainsRoom(other), Is.False);
            }

            RoomExitDirection secondDirection =
                graph.GetExitDirection(graph.SecretRoomId, otherNeighbors[0]);
            Assert.That(
                run.TryRevealCurrentSecretExit(secondDirection).Status,
                Is.EqualTo(DungeonSecretExitRevealStatus.Revealed));
            Assert.That(
                run.CreateMinimapSnapshot().ContainsRoom(otherNeighbors[0]),
                Is.True);
        }

        [Test]
        public void SecretReward_AddsThreeRoomTokensOnceAndNewRunResetsState()
        {
            DungeonGraph graph = DungeonGenerator.Generate(0);
            var run = new DungeonRunState(graph);
            EnterSecretRoom(run, graph);
            int tokensBeforeReward = run.RoomRewardTokenCount;

            DungeonSecretRewardCollectResult collected =
                run.TryCollectCurrentSecretReward(3);

            Assert.That(collected.Status, Is.EqualTo(
                DungeonSecretRewardCollectStatus.Collected));
            Assert.That(collected.WasCollected, Is.True);
            Assert.That(collected.RequestedTokens, Is.EqualTo(3));
            Assert.That(collected.PreviousTokens, Is.EqualTo(tokensBeforeReward));
            Assert.That(collected.AwardedTokens, Is.EqualTo(3));
            Assert.That(collected.CurrentTokens, Is.EqualTo(tokensBeforeReward + 3));
            Assert.That(run.RoomRewardTokenCount, Is.EqualTo(tokensBeforeReward + 3));
            Assert.That(run.CombatRewardTokenCount, Is.EqualTo(run.RoomRewardTokenCount));
            Assert.That(run.IsSecretRewardCollected(graph.SecretRoomId), Is.True);
            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.NotClearable));

            DungeonSecretRewardCollectResult repeated =
                run.TryCollectCurrentSecretReward(3);
            Assert.That(repeated.Status, Is.EqualTo(
                DungeonSecretRewardCollectStatus.AlreadyCollected));
            Assert.That(repeated.AwardedTokens, Is.Zero);
            Assert.That(run.RoomRewardTokenCount, Is.EqualTo(tokensBeforeReward + 3));

            Assert.That(run.TryFail(CreateContactDamage(1, 1)), Is.True);
            Assert.That(
                run.TryCollectCurrentSecretReward(3).Status,
                Is.EqualTo(DungeonSecretRewardCollectStatus.RunFinished));
            RoomExitDirection hiddenDirection = graph.GetNeighbors(graph.SecretRoomId)
                .Where(roomId => !run.IsSecretConnectionRevealed(
                    graph.SecretRoomId,
                    roomId))
                .Select(roomId => graph.GetExitDirection(graph.SecretRoomId, roomId))
                .First();
            Assert.That(
                run.TryRevealCurrentSecretExit(hiddenDirection).Status,
                Is.EqualTo(DungeonSecretExitRevealStatus.RunFinished));

            var restarted = new DungeonRunState(graph);
            Assert.That(restarted.RoomRewardTokenCount, Is.Zero);
            Assert.That(restarted.IsSecretRewardCollected(graph.SecretRoomId), Is.False);
            foreach (DungeonRoomNodeId neighbor in graph.GetNeighbors(graph.SecretRoomId))
            {
                Assert.That(
                    restarted.IsSecretConnectionRevealed(graph.SecretRoomId, neighbor),
                    Is.False);
            }
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
        public void FullExploration_CanClearAndRevisitEveryRoomIncludingSecretLoop()
        {
            DungeonGraph graph = DungeonGenerator.Generate(144);
            var run = new DungeonRunState(graph);

            foreach (DungeonRoomNode room in graph.Rooms.Where(
                room => room.Id != graph.BossRoomId &&
                    room.RoomType != RoomType.Secret))
            {
                TraversePath(run, graph.GetShortestPath(run.CurrentRoomId, room.Id));
            }
            if (graph.HasSecretRoom)
            {
                EnterSecretRoom(run, graph);
                foreach (DungeonRoomNodeId neighbor in graph.GetNeighbors(graph.SecretRoomId))
                {
                    RoomExitDirection direction =
                        graph.GetExitDirection(graph.SecretRoomId, neighbor);
                    run.TryRevealCurrentSecretExit(direction);
                }
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
            Assert.That(
                run.CombatRewardTokenCount,
                Is.EqualTo(graph.Rooms.Count(room => room.RoomType == RoomType.Combat)));
        }

        [Test]
        public void BossClear_CompletesRunAndRejectsFurtherMutation()
        {
            DungeonGraph graph = DungeonGenerator.Generate(377);
            var run = new DungeonRunState(graph);
            TraverseToBoss(run, graph);

            int tokensBeforeBossClear = run.CombatRewardTokenCount;

            Assert.That(
                run.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));

            Assert.That(run.Outcome, Is.EqualTo(DungeonRunOutcome.Completed));
            Assert.That(run.IsTerminal, Is.True);
            Assert.That(run.FailureDamage.HasValue, Is.False);
            Assert.That(run.CombatRewardTokenCount, Is.EqualTo(tokensBeforeBossClear));
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

            int tokensBeforeFailure = run.CombatRewardTokenCount;

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
            Assert.That(run.CombatRewardTokenCount, Is.EqualTo(tokensBeforeFailure));
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

        [Test]
        public void RecoveryRoom_RestoresUpToMaximumOnceWithoutLockOrTokenReward()
        {
            DungeonGraph graph = DungeonGenerator.Generate(233);
            var run = new DungeonRunState(graph);
            var health = new DungeonPlayerHealthState(5);
            ApplyRunDamage(health, 3);
            TraversePath(
                run,
                graph.GetShortestPath(run.CurrentRoomId, graph.RecoveryRoomId));

            int tokensBeforeRecovery = run.CombatRewardTokenCount;
            DungeonRecoveryUseResult restored =
                run.TryUseCurrentRecovery(health, 3);

            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.RecoveryRoomId));
            Assert.That(run.IsCurrentRoomLocked, Is.False);
            Assert.That(run.TryClearCurrentRoom(), Is.EqualTo(
                DungeonRoomClearStatus.NotClearable));
            Assert.That(restored.Status, Is.EqualTo(DungeonRecoveryUseStatus.Restored));
            Assert.That(restored.RoomId, Is.EqualTo(graph.RecoveryRoomId));
            Assert.That(restored.RequestedHealth, Is.EqualTo(3));
            Assert.That(restored.PreviousHealth, Is.EqualTo(2));
            Assert.That(restored.CurrentHealth, Is.EqualTo(5));
            Assert.That(restored.RestoredHealth, Is.EqualTo(3));
            Assert.That(restored.WasRestored, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(5));
            Assert.That(run.IsRecoveryConsumed(graph.RecoveryRoomId), Is.True);
            Assert.That(run.CombatRewardTokenCount, Is.EqualTo(tokensBeforeRecovery));

            DungeonRecoveryUseResult repeated =
                run.TryUseCurrentRecovery(health, 2);
            Assert.That(
                repeated.Status,
                Is.EqualTo(DungeonRecoveryUseStatus.AlreadyConsumed));
            Assert.That(repeated.RestoredHealth, Is.Zero);
            Assert.That(health.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void RecoveryRoom_AtFullHealthDoesNotConsumeAndCanBeUsedLater()
        {
            DungeonGraph graph = DungeonGenerator.Generate(377);
            var run = new DungeonRunState(graph);
            var health = new DungeonPlayerHealthState(5);
            TraversePath(
                run,
                graph.GetShortestPath(run.CurrentRoomId, graph.RecoveryRoomId));

            DungeonRecoveryUseResult full = run.TryUseCurrentRecovery(health, 2);
            Assert.That(full.Status, Is.EqualTo(DungeonRecoveryUseStatus.AtFullHealth));
            Assert.That(run.IsRecoveryConsumed(graph.RecoveryRoomId), Is.False);

            ApplyRunDamage(health, 1);
            DungeonRecoveryUseResult restored = run.TryUseCurrentRecovery(health, 2);
            Assert.That(restored.Status, Is.EqualTo(DungeonRecoveryUseStatus.Restored));
            Assert.That(restored.RestoredHealth, Is.EqualTo(1));
            Assert.That(health.CurrentHealth, Is.EqualTo(5));
            Assert.That(run.IsRecoveryConsumed(graph.RecoveryRoomId), Is.True);
        }

        [Test]
        public void RecoveryRoom_RejectsWrongRoomDeadTerminalAndInvalidRequests()
        {
            DungeonGraph graph = DungeonGenerator.Generate(610);
            var run = new DungeonRunState(graph);
            var health = new DungeonPlayerHealthState(5);

            Assert.That(
                run.TryUseCurrentRecovery(health, 2).Status,
                Is.EqualTo(DungeonRecoveryUseStatus.NotInRecoveryRoom));
            Assert.Throws<ArgumentNullException>(() =>
                run.TryUseCurrentRecovery(null, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                run.TryUseCurrentRecovery(health, 0));
            Assert.Throws<ArgumentException>(() =>
                run.IsRecoveryConsumed(graph.StartRoomId));

            TraversePath(
                run,
                graph.GetShortestPath(run.CurrentRoomId, graph.RecoveryRoomId));
            PlayerDamageResult fatal = ApplyRunDamage(health, 5);
            Assert.That(
                run.TryUseCurrentRecovery(health, 2).Status,
                Is.EqualTo(DungeonRecoveryUseStatus.PlayerDead));
            Assert.That(run.IsRecoveryConsumed(graph.RecoveryRoomId), Is.False);
            Assert.That(run.TryFail(fatal), Is.True);
            Assert.That(
                run.TryUseCurrentRecovery(health, 2).Status,
                Is.EqualTo(DungeonRecoveryUseStatus.RunFinished));
            Assert.That(health.CurrentHealth, Is.Zero);
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

        private static void EnterSecretRoom(DungeonRunState run, DungeonGraph graph)
        {
            Assert.That(graph.HasSecretRoom, Is.True);
            DungeonRoomNodeId entrance = graph.GetNeighbors(graph.SecretRoomId)[0];
            TraversePath(run, graph.GetShortestPath(run.CurrentRoomId, entrance));
            RoomExitDirection direction =
                graph.GetExitDirection(entrance, graph.SecretRoomId);
            DungeonSecretExitRevealResult reveal =
                run.TryRevealCurrentSecretExit(direction);
            Assert.That(
                reveal.Status,
                Is.EqualTo(DungeonSecretExitRevealStatus.Revealed).Or.EqualTo(
                    DungeonSecretExitRevealStatus.AlreadyRevealed));
            Assert.That(run.TryTravel(direction).Moved, Is.True);
            Assert.That(run.CurrentRoomId, Is.EqualTo(graph.SecretRoomId));
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

        private static PlayerDamageResult ApplyRunDamage(
            DungeonPlayerHealthState runHealth,
            int damage)
        {
            var roomHealth = new PlayerHealthSimulation(
                new ActorId(1),
                new ManualGameClock(),
                new PlayerHealthDefinition(5, TimeSpan.FromSeconds(0.75)),
                runHealth.CurrentHealth);
            PlayerDamageResult result =
                roomHealth.ApplyContactDamage(new ActorId(2), damage);
            runHealth.RecordAppliedDamage(result);
            return result;
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
