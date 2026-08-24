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
            DungeonMinimapRoomState state,
            RoomType? knownRoomType)
        {
            if (!roomId.IsValid)
            {
                throw new ArgumentException(
                    "Minimap room ID must be valid.",
                    nameof(roomId));
            }
            ValidateState(state);
            ValidateKnownRoomType(state, knownRoomType);
            RoomId = roomId;
            Position = position;
            State = state;
            KnownRoomType = knownRoomType;
        }

        public DungeonRoomNodeId RoomId { get; }

        public RoomGraphPosition Position { get; }

        public DungeonMinimapRoomState State { get; }

        public RoomType? KnownRoomType { get; }

        public bool HasKnownRoomType => KnownRoomType.HasValue;

        public bool IsVisited => State != DungeonMinimapRoomState.Discovered;

        public bool IsCurrent => State == DungeonMinimapRoomState.Current;

        public bool Equals(DungeonMinimapRoomSnapshot other)
        {
            return RoomId == other.RoomId &&
                Position == other.Position &&
                State == other.State &&
                KnownRoomType == other.KnownRoomType;
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
                hash = (hash * 397) ^ (int)State;
                return (hash * 397) ^ KnownRoomType.GetHashCode();
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

        private static void ValidateKnownRoomType(
            DungeonMinimapRoomState state,
            RoomType? knownRoomType)
        {
            if (state == DungeonMinimapRoomState.Discovered)
            {
                if (knownRoomType.HasValue)
                {
                    throw new ArgumentException(
                        "Discovered minimap rooms must not reveal their room type.",
                        nameof(knownRoomType));
                }
                return;
            }

            if (!knownRoomType.HasValue)
            {
                throw new ArgumentException(
                    "Visited minimap rooms must expose their known room type.",
                    nameof(knownRoomType));
            }

            switch (knownRoomType.Value)
            {
                case RoomType.Combat:
                case RoomType.Start:
                case RoomType.BombReward:
                case RoomType.BossAntechamber:
                case RoomType.Boss:
                case RoomType.Recovery:
                case RoomType.Secret:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(knownRoomType),
                        knownRoomType,
                        "Unsupported known minimap room type.");
            }
        }
    }
}
