using System;

namespace BombSwap.Core
{
    public enum DungeonRoomConnectionKind
    {
        Normal = 0,
        Secret = 1,
    }

    public readonly struct DungeonRoomConnection : IEquatable<DungeonRoomConnection>
    {
        internal DungeonRoomConnection(DungeonRoomNodeId left, DungeonRoomNodeId right)
            : this(left, right, DungeonRoomConnectionKind.Normal)
        {
        }

        internal DungeonRoomConnection(
            DungeonRoomNodeId left,
            DungeonRoomNodeId right,
            DungeonRoomConnectionKind kind)
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
            ValidateKind(kind);

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
            Kind = kind;
        }

        public DungeonRoomNodeId First { get; }

        public DungeonRoomNodeId Second { get; }

        public DungeonRoomConnectionKind Kind { get; }

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
            return First == other.First &&
                Second == other.Second &&
                Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            return obj is DungeonRoomConnection other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (First.GetHashCode() * 397) ^ Second.GetHashCode();
                return (hashCode * 397) ^ (int)Kind;
            }
        }

        public override string ToString()
        {
            return $"{First}<->{Second}:{Kind}";
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

        private static void ValidateKind(DungeonRoomConnectionKind kind)
        {
            switch (kind)
            {
                case DungeonRoomConnectionKind.Normal:
                case DungeonRoomConnectionKind.Secret:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Unsupported dungeon room connection kind.");
            }
        }
    }
}
