using System;

namespace BombSwap.Core
{
    public readonly struct RoomDefinitionId : IEquatable<RoomDefinitionId>
    {
        public RoomDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Room definition ID cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(RoomDefinitionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RoomDefinitionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(RoomDefinitionId left, RoomDefinitionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RoomDefinitionId left, RoomDefinitionId right)
        {
            return !left.Equals(right);
        }
    }
}
