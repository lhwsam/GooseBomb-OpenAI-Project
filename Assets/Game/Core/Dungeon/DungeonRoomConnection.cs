using System;

namespace BombSwap.Core
{
    public readonly struct DungeonRoomConnection : IEquatable<DungeonRoomConnection>
    {
        internal DungeonRoomConnection(DungeonRoomNodeId left, DungeonRoomNodeId right)
        {
            if (!left.IsValid)
            {
                throw new ArgumentException("Left room node ID must be valid.", nameof(left));
            }
            if (!right.IsValid)
            {
                throw new ArgumentException("Right room node ID must be valid.", nameof(right));
            }
            if (left == right)
            {
                throw new ArgumentException("A room cannot connect to itself.", nameof(right));
            }

            if (left.CompareTo(right) < 0)
            {
                First = left;
                Second = right;
            }
            else
            {
                First = right;
                Second = left;
            }
        }

        public DungeonRoomNodeId First { get; }

        public DungeonRoomNodeId Second { get; }

        public bool Contains(DungeonRoomNodeId roomId)
        {
            return First == roomId || Second == roomId;
        }

        public DungeonRoomNodeId GetOther(DungeonRoomNodeId roomId)
        {
            if (First == roomId)
            {
                return Second;
            }
            if (Second == roomId)
            {
                return First;
            }

            throw new ArgumentException(
                "Room node is not part of this connection.",
                nameof(roomId));
        }

        public bool Equals(DungeonRoomConnection other)
        {
            return First == other.First && Second == other.Second;
        }

        public override bool Equals(object obj)
        {
            return obj is DungeonRoomConnection other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (First.GetHashCode() * 397) ^ Second.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{First}<->{Second}";
        }

        public static bool operator ==(
            DungeonRoomConnection left,
            DungeonRoomConnection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DungeonRoomConnection left,
            DungeonRoomConnection right)
        {
            return !left.Equals(right);
        }
    }
}
