using System;

namespace BombSwap.Core
{
    public readonly struct BombId : IEquatable<BombId>, IComparable<BombId>
    {
        internal BombId(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public bool IsValid => Value > 0;

        public int CompareTo(BombId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(BombId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BombId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(BombId left, BombId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BombId left, BombId right)
        {
            return !left.Equals(right);
        }
    }
}
