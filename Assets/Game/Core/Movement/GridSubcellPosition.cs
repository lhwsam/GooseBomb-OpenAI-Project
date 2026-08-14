using System;

namespace BombSwap.Core
{
    public readonly struct GridSubcellPosition : IEquatable<GridSubcellPosition>
    {
        public GridSubcellPosition(double x, double z)
        {
            if (double.IsNaN(x) || double.IsInfinity(x))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    "Grid subcell coordinates must be finite.");
            }
            if (double.IsNaN(z) || double.IsInfinity(z))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(z),
                    "Grid subcell coordinates must be finite.");
            }

            X = x;
            Z = z;
        }

        public double X { get; }

        public double Z { get; }

        public static GridSubcellPosition AtCellCenter(GridPosition position)
        {
            return new GridSubcellPosition(position.X, position.Z);
        }

        public bool Equals(GridSubcellPosition other)
        {
            return X.Equals(other.X) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is GridSubcellPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Z.GetHashCode();
            }
        }

        public static bool operator ==(GridSubcellPosition left, GridSubcellPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridSubcellPosition left, GridSubcellPosition right)
        {
            return !left.Equals(right);
        }
    }
}
