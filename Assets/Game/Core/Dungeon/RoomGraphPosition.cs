using System;

namespace BombSwap.Core
{
    public readonly struct RoomGraphPosition : IEquatable<RoomGraphPosition>
    {
        public RoomGraphPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }

        public int Z { get; }

        public RoomGraphPosition Offset(int deltaX, int deltaZ)
        {
            return new RoomGraphPosition(
                checked(X + deltaX),
                checked(Z + deltaZ));
        }

        public bool IsCardinallyAdjacentTo(RoomGraphPosition other)
        {
            long deltaX = Math.Abs((long)X - other.X);
            long deltaZ = Math.Abs((long)Z - other.Z);
            return deltaX + deltaZ == 1L;
        }

        public bool Equals(RoomGraphPosition other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is RoomGraphPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Z})";
        }

        public static bool operator ==(RoomGraphPosition left, RoomGraphPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RoomGraphPosition left, RoomGraphPosition right)
        {
            return !left.Equals(right);
        }
    }
}
