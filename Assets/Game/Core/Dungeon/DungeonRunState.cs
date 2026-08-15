using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public enum DungeonRunOutcome
    {
        InProgress = 0,
        Completed = 1,
        Failed = 2,
    }

    public enum DungeonTravelStatus
    {
        Moved = 0,
        NotConnected = 1,
        BlockedByUnclearedRoom = 2,
        RunFinished = 3,
        BlockedBySecretWall = 4,
    }

    public enum DungeonRoomClearStatus
    {
        Cleared = 0,
        AlreadyCleared = 1,
        NotClearable = 2,
        RunFinished = 3,
    }

    public enum DungeonRecoveryUseStatus
    {
        Restored = 0,
        NotInRecoveryRoom = 1,
        AlreadyConsumed = 2,
        AtFullHealth = 3,
        PlayerDead = 4,
        RunFinished = 5,
    }

    public enum DungeonSecretExitRevealStatus
    {
        Revealed = 0,
        AlreadyRevealed = 1,
        NotConnected = 2,
        NotSecretConnection = 3,
        RunFinished = 4,
    }

    public enum DungeonSecretRewardCollectStatus
    {
        Collected = 0,
        NotInSecretRoom = 1,
        AlreadyCollected = 2,
        RunFinished = 3,
    }

    public readonly struct DungeonSecretExitRevealResult
    {
        internal DungeonSecretExitRevealResult(
            DungeonRoomNodeId fromRoomId,
            DungeonRoomNodeId targetRoomId,
            RoomExitDirection direction,
            DungeonSecretExitRevealStatus status)
        {
            FromRoomId = fromRoomId;
            TargetRoomId = targetRoomId;
            Direction = direction;
            Status = status;
        }

        public DungeonRoomNodeId FromRoomId { get; }

        public DungeonRoomNodeId TargetRoomId { get; }

        public RoomExitDirection Direction { get; }

        public DungeonSecretExitRevealStatus Status { get; }

        public bool WasRevealed => Status == DungeonSecretExitRevealStatus.Revealed;
    }

    public readonly struct DungeonSecretRewardCollectResult
    {
        internal DungeonSecretRewardCollectResult(
            DungeonRoomNodeId roomId,
            int requestedTokens,
            int previousTokens,
            int currentTokens,
            DungeonSecretRewardCollectStatus status)
        {
            RoomId = roomId;
            RequestedTokens = requestedTokens;
            PreviousTokens = previousTokens;
            CurrentTokens = currentTokens;
            Status = status;
        }

        public DungeonRoomNodeId RoomId { get; }

        public int RequestedTokens { get; }

        public int PreviousTokens { get; }

        public int CurrentTokens { get; }

        public int AwardedTokens => CurrentTokens - PreviousTokens;

        public DungeonSecretRewardCollectStatus Status { get; }

        public bool WasCollected =>
            Status == DungeonSecretRewardCollectStatus.Collected;
    }

    public readonly struct DungeonRecoveryUseResult
    {
        internal DungeonRecoveryUseResult(
            DungeonRoomNodeId roomId,
            int requestedHealth,
            int previousHealth,
            int currentHealth,
            DungeonRecoveryUseStatus status)
        {
            RoomId = roomId;
            RequestedHealth = requestedHealth;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            Status = status;
        }

        public DungeonRoomNodeId RoomId { get; }

        public int RequestedHealth { get; }

        public int PreviousHealth { get; }

        public int CurrentHealth { get; }

        public int RestoredHealth => CurrentHealth - PreviousHealth;

        public DungeonRecoveryUseStatus Status { get; }

        public bool WasRestored => Status == DungeonRecoveryUseStatus.Restored;
    }

    public readonly struct DungeonTravelResult
    {
        internal DungeonTravelResult(
            DungeonRoomNodeId fromRoomId,
            DungeonRoomNodeId targetRoomId,
            DungeonTravelStatus status,
            bool enteredFirstTime)
        {
            FromRoomId = fromRoomId;
            TargetRoomId = targetRoomId;
            Status = status;
            EnteredFirstTime = enteredFirstTime;
        }

        public DungeonRoomNodeId FromRoomId { get; }

        public DungeonRoomNodeId TargetRoomId { get; }

        public DungeonTravelStatus Status { get; }

        public bool EnteredFirstTime { get; }

        public bool Moved => Status == DungeonTravelStatus.Moved;
    }

    public sealed class DungeonRunState
    {
        private static readonly RoomExitDirection[] ExitDirectionOrder =
        {
            RoomExitDirection.North,
            RoomExitDirection.East,
            RoomExitDirection.South,
            RoomExitDirection.West,
        };

        private readonly bool[] _visited;
        private readonly bool[] _cleared;
        private readonly bool[] _consumedRecoveryRooms;
        private readonly bool[] _revealedSecretConnections;
        private readonly bool[] _collectedSecretRewards;

        private const int CombatRoomTokenReward = 1;

        public DungeonRunState(DungeonGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _visited = new bool[graph.Rooms.Count];
            _cleared = new bool[graph.Rooms.Count];
            _consumedRecoveryRooms = new bool[graph.Rooms.Count];
            _revealedSecretConnections = new bool[graph.Connections.Count];
            _collectedSecretRewards = new bool[graph.Rooms.Count];
            CurrentRoomId = graph.StartRoomId;
            _visited[GetRoomIndex(CurrentRoomId)] = true;
        }

        public DungeonGraph Graph { get; }

        public DungeonRoomNodeId CurrentRoomId { get; private set; }

        public DungeonRoomNodeId PreviousRoomId { get; private set; }

        public DungeonRunOutcome Outcome { get; private set; }

        public PlayerDamageResult? FailureDamage { get; private set; }

        public int RoomRewardTokenCount { get; private set; }

        public int CombatRewardTokenCount => RoomRewardTokenCount;

        public bool IsTerminal => Outcome != DungeonRunOutcome.InProgress;

        public bool IsCurrentRoomLocked => IsTerminal || IsRoomLocked(CurrentRoomId);

        public bool IsVisited(DungeonRoomNodeId roomId)
        {
            return _visited[GetRoomIndex(roomId)];
        }

        public bool IsCleared(DungeonRoomNodeId roomId)
        {
            return _cleared[GetRoomIndex(roomId)];
        }

        public bool IsRoomLocked(DungeonRoomNodeId roomId)
        {
            DungeonRoomNode room = Graph.GetRoom(roomId);
            return RequiresClear(room.RoomType) && !_cleared[roomId.Value - 1];
        }

        public bool IsRecoveryConsumed(DungeonRoomNodeId roomId)
        {
            DungeonRoomNode room = Graph.GetRoom(roomId);
            if (room.RoomType != RoomType.Recovery)
            {
                throw new ArgumentException(
                    $"Dungeon room {roomId} is not a recovery room.",
                    nameof(roomId));
            }
            return _consumedRecoveryRooms[roomId.Value - 1];
        }

        public bool IsSecretConnectionRevealed(
            DungeonRoomNodeId first,
            DungeonRoomNodeId second)
        {
            int connectionIndex = GetConnectionIndex(first, second);
            DungeonRoomConnection connection = Graph.Connections[connectionIndex];
            if (connection.Kind != DungeonRoomConnectionKind.Secret)
            {
                throw new ArgumentException(
                    $"Dungeon connection {connection} is not a secret connection.",
                    nameof(second));
            }
            return _revealedSecretConnections[connectionIndex];
        }

        public bool IsSecretRewardCollected(DungeonRoomNodeId roomId)
        {
            DungeonRoomNode room = Graph.GetRoom(roomId);
            if (room.RoomType != RoomType.Secret)
            {
                throw new ArgumentException(
                    $"Dungeon room {roomId} is not a secret room.",
                    nameof(roomId));
            }
            return _collectedSecretRewards[roomId.Value - 1];
        }

        public IReadOnlyList<DungeonRoomNodeId> GetVisitedRooms()
        {
            return CreateRoomIdSnapshot(_visited);
        }

        public IReadOnlyList<DungeonRoomNodeId> GetClearedRooms()
        {
            return CreateRoomIdSnapshot(_cleared);
        }

        public DungeonMinimapSnapshot CreateMinimapSnapshot()
        {
            var visible = new bool[Graph.Rooms.Count];
            for (int roomIndex = 0; roomIndex < Graph.Rooms.Count; roomIndex++)
            {
                if (!_visited[roomIndex])
                {
                    continue;
                }

                DungeonRoomNodeId visitedRoomId = Graph.Rooms[roomIndex].Id;
                visible[roomIndex] = true;
                IReadOnlyList<DungeonRoomNodeId> neighbors =
                    Graph.GetNeighbors(visitedRoomId);
                for (int neighborIndex = 0;
                    neighborIndex < neighbors.Count;
                    neighborIndex++)
                {
                    DungeonRoomNodeId neighbor = neighbors[neighborIndex];
                    if (Graph.TryGetConnection(
                            visitedRoomId,
                            neighbor,
                            out DungeonRoomConnection connection) &&
                        IsConnectionVisible(connection))
                    {
                        visible[neighbor.Value - 1] = true;
                    }
                }
            }

            var rooms = new List<DungeonMinimapRoomSnapshot>();
            for (int roomIndex = 0; roomIndex < Graph.Rooms.Count; roomIndex++)
            {
                if (!visible[roomIndex])
                {
                    continue;
                }

                DungeonRoomNode room = Graph.Rooms[roomIndex];
                DungeonMinimapRoomState state = room.Id == CurrentRoomId
                    ? DungeonMinimapRoomState.Current
                    : _visited[roomIndex]
                        ? DungeonMinimapRoomState.Visited
                        : DungeonMinimapRoomState.Discovered;
                rooms.Add(new DungeonMinimapRoomSnapshot(
                    room.Id,
                    room.Position,
                    state));
            }

            var connections = new List<DungeonRoomConnection>();
            for (int connectionIndex = 0;
                connectionIndex < Graph.Connections.Count;
                connectionIndex++)
            {
                DungeonRoomConnection connection =
                    Graph.Connections[connectionIndex];
                if (IsConnectionVisible(connection) &&
                    (_visited[connection.First.Value - 1] ||
                        _visited[connection.Second.Value - 1]))
                {
                    connections.Add(connection);
                }
            }

            return new DungeonMinimapSnapshot(
                CurrentRoomId,
                rooms.ToArray(),
                connections.ToArray());
        }

        public DungeonRoomExitState GetCurrentExitState(RoomExitDirection direction)
        {
            return GetExitState(CurrentRoomId, direction);
        }

        public DungeonRoomExitState GetExitState(
            DungeonRoomNodeId roomId,
            RoomExitDirection direction)
        {
            Graph.GetRoom(roomId);
            if (!Graph.TryGetNeighbor(roomId, direction, out DungeonRoomNodeId target))
            {
                return new DungeonRoomExitState(
                    direction,
                    default,
                    DungeonRoomExitStatus.Inactive);
            }

            if (!Graph.TryGetConnection(
                    roomId,
                    target,
                    out DungeonRoomConnection connection))
            {
                throw new InvalidOperationException(
                    $"Dungeon neighbor {roomId} to {target} has no connection snapshot.");
            }

            DungeonRoomExitStatus status;
            if (IsTerminal)
            {
                status = DungeonRoomExitStatus.Locked;
            }
            else if (connection.Kind == DungeonRoomConnectionKind.Secret &&
                !_revealedSecretConnections[GetConnectionIndex(roomId, target)])
            {
                status = DungeonRoomExitStatus.SecretWall;
            }
            else
            {
                status = IsRoomLocked(roomId)
                    ? DungeonRoomExitStatus.Locked
                    : DungeonRoomExitStatus.Open;
            }

            return new DungeonRoomExitState(
                direction,
                target,
                status);
        }

        public IReadOnlyList<DungeonRoomExitState> GetCurrentExitStates()
        {
            return GetExitStates(CurrentRoomId);
        }

        public IReadOnlyList<DungeonRoomExitState> GetExitStates(
            DungeonRoomNodeId roomId)
        {
            Graph.GetRoom(roomId);
            var exits = new DungeonRoomExitState[ExitDirectionOrder.Length];
            for (int index = 0; index < ExitDirectionOrder.Length; index++)
            {
                exits[index] = GetExitState(roomId, ExitDirectionOrder[index]);
            }
            return Array.AsReadOnly(exits);
        }

        public DungeonSecretExitRevealResult TryRevealCurrentSecretExit(
            RoomExitDirection direction)
        {
            DungeonRoomNodeId fromRoomId = CurrentRoomId;
            if (!Graph.TryGetNeighbor(fromRoomId, direction, out DungeonRoomNodeId target))
            {
                return new DungeonSecretExitRevealResult(
                    fromRoomId,
                    default,
                    direction,
                    DungeonSecretExitRevealStatus.NotConnected);
            }
            if (IsTerminal)
            {
                return new DungeonSecretExitRevealResult(
                    fromRoomId,
                    target,
                    direction,
                    DungeonSecretExitRevealStatus.RunFinished);
            }

            int connectionIndex = GetConnectionIndex(fromRoomId, target);
            DungeonRoomConnection connection = Graph.Connections[connectionIndex];
            if (connection.Kind != DungeonRoomConnectionKind.Secret)
            {
                return new DungeonSecretExitRevealResult(
                    fromRoomId,
                    target,
                    direction,
                    DungeonSecretExitRevealStatus.NotSecretConnection);
            }
            if (_revealedSecretConnections[connectionIndex])
            {
                return new DungeonSecretExitRevealResult(
                    fromRoomId,
                    target,
                    direction,
                    DungeonSecretExitRevealStatus.AlreadyRevealed);
            }

            _revealedSecretConnections[connectionIndex] = true;
            return new DungeonSecretExitRevealResult(
                fromRoomId,
                target,
                direction,
                DungeonSecretExitRevealStatus.Revealed);
        }

        public DungeonTravelResult TryTravelTo(DungeonRoomNodeId targetRoomId)
        {
            Graph.GetRoom(targetRoomId);
            DungeonRoomNodeId fromRoomId = CurrentRoomId;
            if (IsTerminal)
            {
                return new DungeonTravelResult(
                    fromRoomId,
                    targetRoomId,
                    DungeonTravelStatus.RunFinished,
                    false);
            }
            if (!AreConnected(fromRoomId, targetRoomId))
            {
                return new DungeonTravelResult(
                    fromRoomId,
                    targetRoomId,
                    DungeonTravelStatus.NotConnected,
                    false);
            }
            int connectionIndex = GetConnectionIndex(fromRoomId, targetRoomId);
            if (Graph.Connections[connectionIndex].Kind ==
                    DungeonRoomConnectionKind.Secret &&
                !_revealedSecretConnections[connectionIndex])
            {
                return new DungeonTravelResult(
                    fromRoomId,
                    targetRoomId,
                    DungeonTravelStatus.BlockedBySecretWall,
                    false);
            }
            if (IsCurrentRoomLocked)
            {
                return new DungeonTravelResult(
                    fromRoomId,
                    targetRoomId,
                    DungeonTravelStatus.BlockedByUnclearedRoom,
                    false);
            }

            int targetIndex = GetRoomIndex(targetRoomId);
            bool enteredFirstTime = !_visited[targetIndex];
            PreviousRoomId = fromRoomId;
            CurrentRoomId = targetRoomId;
            _visited[targetIndex] = true;
            return new DungeonTravelResult(
                fromRoomId,
                targetRoomId,
                DungeonTravelStatus.Moved,
                enteredFirstTime);
        }

        public DungeonTravelResult TryTravel(RoomExitDirection direction)
        {
            if (!Graph.TryGetNeighbor(CurrentRoomId, direction, out DungeonRoomNodeId target))
            {
                return new DungeonTravelResult(
                    CurrentRoomId,
                    default,
                    DungeonTravelStatus.NotConnected,
                    false);
            }

            return TryTravelTo(target);
        }

        public DungeonRoomClearStatus TryClearCurrentRoom()
        {
            if (IsTerminal)
            {
                return DungeonRoomClearStatus.RunFinished;
            }

            DungeonRoomNode current = Graph.GetRoom(CurrentRoomId);
            if (!RequiresClear(current.RoomType))
            {
                return DungeonRoomClearStatus.NotClearable;
            }

            int index = GetRoomIndex(CurrentRoomId);
            if (_cleared[index])
            {
                return DungeonRoomClearStatus.AlreadyCleared;
            }

            _cleared[index] = true;
            if (current.RoomType == RoomType.Combat)
            {
                RoomRewardTokenCount += CombatRoomTokenReward;
            }
            if (CurrentRoomId == Graph.BossRoomId)
            {
                Outcome = DungeonRunOutcome.Completed;
            }
            return DungeonRoomClearStatus.Cleared;
        }

        public DungeonSecretRewardCollectResult TryCollectCurrentSecretReward(
            int requestedTokens)
        {
            if (requestedTokens <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedTokens),
                    requestedTokens,
                    "Secret-room token reward must be positive.");
            }

            int previousTokens = RoomRewardTokenCount;
            if (IsTerminal)
            {
                return CreateSecretRewardResult(
                    requestedTokens,
                    previousTokens,
                    DungeonSecretRewardCollectStatus.RunFinished);
            }

            DungeonRoomNode current = Graph.GetRoom(CurrentRoomId);
            if (current.RoomType != RoomType.Secret)
            {
                return CreateSecretRewardResult(
                    requestedTokens,
                    previousTokens,
                    DungeonSecretRewardCollectStatus.NotInSecretRoom);
            }

            int roomIndex = GetRoomIndex(CurrentRoomId);
            if (_collectedSecretRewards[roomIndex])
            {
                return CreateSecretRewardResult(
                    requestedTokens,
                    previousTokens,
                    DungeonSecretRewardCollectStatus.AlreadyCollected);
            }

            checked
            {
                RoomRewardTokenCount += requestedTokens;
            }
            _collectedSecretRewards[roomIndex] = true;
            return new DungeonSecretRewardCollectResult(
                CurrentRoomId,
                requestedTokens,
                previousTokens,
                RoomRewardTokenCount,
                DungeonSecretRewardCollectStatus.Collected);
        }

        public DungeonRecoveryUseResult TryUseCurrentRecovery(
            DungeonPlayerHealthState playerHealth,
            int requestedHealth)
        {
            if (playerHealth == null)
            {
                throw new ArgumentNullException(nameof(playerHealth));
            }
            if (requestedHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedHealth),
                    requestedHealth,
                    "Requested recovery must be positive.");
            }

            int previousHealth = playerHealth.CurrentHealth;
            if (IsTerminal)
            {
                return CreateRecoveryResult(
                    requestedHealth,
                    previousHealth,
                    DungeonRecoveryUseStatus.RunFinished);
            }

            DungeonRoomNode current = Graph.GetRoom(CurrentRoomId);
            if (current.RoomType != RoomType.Recovery)
            {
                return CreateRecoveryResult(
                    requestedHealth,
                    previousHealth,
                    DungeonRecoveryUseStatus.NotInRecoveryRoom);
            }

            int roomIndex = GetRoomIndex(CurrentRoomId);
            if (_consumedRecoveryRooms[roomIndex])
            {
                return CreateRecoveryResult(
                    requestedHealth,
                    previousHealth,
                    DungeonRecoveryUseStatus.AlreadyConsumed);
            }
            if (playerHealth.IsDead)
            {
                return CreateRecoveryResult(
                    requestedHealth,
                    previousHealth,
                    DungeonRecoveryUseStatus.PlayerDead);
            }
            if (previousHealth == playerHealth.MaxHealth)
            {
                return CreateRecoveryResult(
                    requestedHealth,
                    previousHealth,
                    DungeonRecoveryUseStatus.AtFullHealth);
            }

            PlayerHealthRecoveryResult recovery =
                playerHealth.ApplyRecovery(requestedHealth);
            if (!recovery.WasApplied || recovery.PreviousHealth != previousHealth)
            {
                throw new InvalidOperationException(
                    "Dungeon recovery produced an inconsistent player-health result.");
            }
            _consumedRecoveryRooms[roomIndex] = true;
            return new DungeonRecoveryUseResult(
                CurrentRoomId,
                requestedHealth,
                previousHealth,
                recovery.CurrentHealth,
                DungeonRecoveryUseStatus.Restored);
        }

        public bool TryFail(PlayerDamageResult fatalDamage)
        {
            if (!fatalDamage.WasFatal)
            {
                throw new ArgumentException(
                    "A failed dungeon run requires an applied fatal damage result.",
                    nameof(fatalDamage));
            }
            if (IsTerminal)
            {
                return false;
            }

            Outcome = DungeonRunOutcome.Failed;
            FailureDamage = fatalDamage;
            return true;
        }

        public static bool RequiresClear(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat:
                case RoomType.Boss:
                    return true;
                case RoomType.Start:
                case RoomType.BombReward:
                case RoomType.BossAntechamber:
                case RoomType.Recovery:
                case RoomType.Secret:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        "Unsupported dungeon room type.");
            }
        }

        private bool AreConnected(DungeonRoomNodeId from, DungeonRoomNodeId to)
        {
            IReadOnlyList<DungeonRoomNodeId> neighbors = Graph.GetNeighbors(from);
            for (int index = 0; index < neighbors.Count; index++)
            {
                if (neighbors[index] == to)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsConnectionVisible(DungeonRoomConnection connection)
        {
            if (connection.Kind == DungeonRoomConnectionKind.Normal)
            {
                return true;
            }
            return _revealedSecretConnections[
                GetConnectionIndex(connection.First, connection.Second)];
        }

        private int GetConnectionIndex(
            DungeonRoomNodeId first,
            DungeonRoomNodeId second)
        {
            Graph.GetRoom(first);
            Graph.GetRoom(second);
            for (int index = 0; index < Graph.Connections.Count; index++)
            {
                DungeonRoomConnection connection = Graph.Connections[index];
                if (connection.Contains(first) && connection.Contains(second))
                {
                    return index;
                }
            }

            throw new ArgumentException(
                $"Dungeon rooms {first} and {second} are not connected.",
                nameof(second));
        }

        private DungeonRecoveryUseResult CreateRecoveryResult(
            int requestedHealth,
            int currentHealth,
            DungeonRecoveryUseStatus status)
        {
            return new DungeonRecoveryUseResult(
                CurrentRoomId,
                requestedHealth,
                currentHealth,
                currentHealth,
                status);
        }

        private DungeonSecretRewardCollectResult CreateSecretRewardResult(
            int requestedTokens,
            int currentTokens,
            DungeonSecretRewardCollectStatus status)
        {
            return new DungeonSecretRewardCollectResult(
                CurrentRoomId,
                requestedTokens,
                currentTokens,
                currentTokens,
                status);
        }

        private int GetRoomIndex(DungeonRoomNodeId roomId)
        {
            Graph.GetRoom(roomId);
            return roomId.Value - 1;
        }

        private IReadOnlyList<DungeonRoomNodeId> CreateRoomIdSnapshot(bool[] states)
        {
            var rooms = new List<DungeonRoomNodeId>();
            for (int index = 0; index < states.Length; index++)
            {
                if (states[index])
                {
                    rooms.Add(Graph.Rooms[index].Id);
                }
            }
            return Array.AsReadOnly(rooms.ToArray());
        }
    }
}
