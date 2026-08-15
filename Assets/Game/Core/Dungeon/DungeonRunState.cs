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

        private const int CombatRoomTokenReward = 1;

        public DungeonRunState(DungeonGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _visited = new bool[graph.Rooms.Count];
            _cleared = new bool[graph.Rooms.Count];
            _consumedRecoveryRooms = new bool[graph.Rooms.Count];
            CurrentRoomId = graph.StartRoomId;
            _visited[GetRoomIndex(CurrentRoomId)] = true;
        }

        public DungeonGraph Graph { get; }

        public DungeonRoomNodeId CurrentRoomId { get; private set; }

        public DungeonRoomNodeId PreviousRoomId { get; private set; }

        public DungeonRunOutcome Outcome { get; private set; }

        public PlayerDamageResult? FailureDamage { get; private set; }

        public int CombatRewardTokenCount { get; private set; }

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
                    visible[neighbors[neighborIndex].Value - 1] = true;
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
                if (_visited[connection.First.Value - 1] ||
                    _visited[connection.Second.Value - 1])
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
            if (!Graph.TryGetNeighbor(CurrentRoomId, direction, out DungeonRoomNodeId target))
            {
                return new DungeonRoomExitState(
                    direction,
                    default,
                    DungeonRoomExitStatus.Inactive);
            }

            return new DungeonRoomExitState(
                direction,
                target,
                IsCurrentRoomLocked
                    ? DungeonRoomExitStatus.Locked
                    : DungeonRoomExitStatus.Open);
        }

        public IReadOnlyList<DungeonRoomExitState> GetCurrentExitStates()
        {
            var exits = new DungeonRoomExitState[ExitDirectionOrder.Length];
            for (int index = 0; index < ExitDirectionOrder.Length; index++)
            {
                exits[index] = GetCurrentExitState(ExitDirectionOrder[index]);
            }
            return Array.AsReadOnly(exits);
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
                CombatRewardTokenCount += CombatRoomTokenReward;
            }
            if (CurrentRoomId == Graph.BossRoomId)
            {
                Outcome = DungeonRunOutcome.Completed;
            }
            return DungeonRoomClearStatus.Cleared;
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
