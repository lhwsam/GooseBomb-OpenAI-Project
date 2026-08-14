using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public enum DungeonTravelStatus
    {
        Moved = 0,
        NotConnected = 1,
        BlockedByUnclearedRoom = 2,
    }

    public enum DungeonRoomClearStatus
    {
        Cleared = 0,
        AlreadyCleared = 1,
        NotClearable = 2,
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
        private readonly bool[] _visited;
        private readonly bool[] _cleared;

        public DungeonRunState(DungeonGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _visited = new bool[graph.Rooms.Count];
            _cleared = new bool[graph.Rooms.Count];
            CurrentRoomId = graph.StartRoomId;
            _visited[GetRoomIndex(CurrentRoomId)] = true;
        }

        public DungeonGraph Graph { get; }

        public DungeonRoomNodeId CurrentRoomId { get; private set; }

        public DungeonRoomNodeId PreviousRoomId { get; private set; }

        public bool IsCurrentRoomLocked => IsRoomLocked(CurrentRoomId);

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

        public IReadOnlyList<DungeonRoomNodeId> GetVisitedRooms()
        {
            return CreateRoomIdSnapshot(_visited);
        }

        public IReadOnlyList<DungeonRoomNodeId> GetClearedRooms()
        {
            return CreateRoomIdSnapshot(_cleared);
        }

        public DungeonTravelResult TryTravelTo(DungeonRoomNodeId targetRoomId)
        {
            Graph.GetRoom(targetRoomId);
            DungeonRoomNodeId fromRoomId = CurrentRoomId;
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
            return DungeonRoomClearStatus.Cleared;
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
