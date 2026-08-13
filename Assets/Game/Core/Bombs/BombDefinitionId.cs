using System;

namespace BombSwap.Core
{
    public readonly struct BombDefinitionId : IEquatable<BombDefinitionId>
    {
        public BombDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Bomb definition ID cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(BombDefinitionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is BombDefinitionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(BombDefinitionId left, BombDefinitionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BombDefinitionId left, BombDefinitionId right)
        {
            return !left.Equals(right);
        }
    }
}
