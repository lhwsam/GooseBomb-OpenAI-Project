using System;

namespace BombSwap.Core
{
    public readonly struct ActorId : IEquatable<ActorId>, IComparable<ActorId>
    {
        public ActorId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Actor ID must be positive.");
            }

            Value = value;
        }

        public long Value { get; }

        public bool IsValid => Value > 0;

        public int CompareTo(ActorId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(ActorId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorId left, ActorId right)
        {
            return !left.Equals(right);
        }
    }
}
