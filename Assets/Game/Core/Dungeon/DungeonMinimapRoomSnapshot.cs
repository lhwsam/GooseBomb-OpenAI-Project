using System;

namespace BombSwap.Core
{
    public enum DungeonMinimapRoomState
    {
        Discovered = 0,
        Visited = 1,
        Current = 2,
    }

    public readonly struct DungeonMinimapRoomSnapshot :
        IEquatable<DungeonMinimapRoomSnapshot>
    {
        internal DungeonMinimapRoomSnapshot(
            DungeonRoomNodeId roomId,
            RoomGraphPosition position,
            DungeonMinimapRoomState state)
        {
            if (!roomId.IsValid)
            {
                throw new ArgumentException(
                    "Minimap room ID must be valid.",
                    nameof(roomId));
            }
            ValidateState(state);
            RoomId = roomId;
            Position = position;
            State = state;
        }

        public DungeonRoomNodeId RoomId { get; }

        public RoomGraphPosition Position { get; }

        public DungeonMinimapRoomState State { get; }

        public bool IsVisited => State != DungeonMinimapRoomState.Discovered;

        public bool IsCurrent => State == DungeonMinimapRoomState.Current;

        public bool Equals(DungeonMinimapRoomSnapshot other)
        {
            return RoomId == other.RoomId &&
                Position == other.Position &&
                State == other.State;
        }

        public override bool Equals(object obj)
        {
            return obj is DungeonMinimapRoomSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RoomId.GetHashCode();
                hash = (hash * 397) ^ Position.GetHashCode();
                return (hash * 397) ^ (int)State;
            }
        }

        public static bool operator ==(
            DungeonMinimapRoomSnapshot left,
            DungeonMinimapRoomSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DungeonMinimapRoomSnapshot left,
            DungeonMinimapRoomSnapshot right)
        {
            return !left.Equals(right);
        }

        private static void ValidateState(DungeonMinimapRoomState state)
        {
            switch (state)
            {
                case DungeonMinimapRoomState.Discovered:
                case DungeonMinimapRoomState.Visited:
                case DungeonMinimapRoomState.Current:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(state),
                        state,
                        "Unsupported minimap room state.");
            }
        }
    }
}
