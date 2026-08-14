using System;

namespace BombSwap.Core
{
    public readonly struct DungeonRoomNodeId :
        IEquatable<DungeonRoomNodeId>,
        IComparable<DungeonRoomNodeId>
    {
        public DungeonRoomNodeId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Dungeon room node ID must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public int CompareTo(DungeonRoomNodeId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(DungeonRoomNodeId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DungeonRoomNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(DungeonRoomNodeId left, DungeonRoomNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DungeonRoomNodeId left, DungeonRoomNodeId right)
        {
            return !left.Equals(right);
        }
    }
}
