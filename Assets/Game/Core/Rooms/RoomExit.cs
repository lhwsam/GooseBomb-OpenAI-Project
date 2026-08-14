using System;

namespace BombSwap.Core
{
    public enum RoomExitDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public readonly struct RoomExit : IEquatable<RoomExit>
    {
        public RoomExit(GridPosition cell, RoomExitDirection direction)
        {
            Cell = cell;
            Direction = direction;
        }

        public GridPosition Cell { get; }

        public RoomExitDirection Direction { get; }

        public bool Equals(RoomExit other)
        {
            return Cell == other.Cell && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is RoomExit other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Cell.GetHashCode() * 397) ^ (int)Direction;
            }
        }

        public override string ToString()
        {
            return $"{Direction} at {Cell}";
        }
    }
}
