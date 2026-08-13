using System;

namespace BombSwap.Core
{
    public readonly struct PlayerMovementStep : IEquatable<PlayerMovementStep>
    {
        public PlayerMovementStep(
            GridPosition from,
            GridPosition to,
            CardinalDirection direction)
        {
            if (direction == CardinalDirection.None ||
                direction < CardinalDirection.None ||
                direction > CardinalDirection.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    "A movement step requires a defined non-zero direction.");
            }
            if (to != GetExpectedTarget(from, direction))
            {
                throw new ArgumentException(
                    "Movement step target must be one cell away in its declared direction.",
                    nameof(to));
            }

            From = from;
            To = to;
            Direction = direction;
        }

        public GridPosition From { get; }

        public GridPosition To { get; }

        public CardinalDirection Direction { get; }

        public bool Equals(PlayerMovementStep other)
        {
            return From == other.From && To == other.To && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerMovementStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (((From.GetHashCode() * 397) ^ To.GetHashCode()) * 397) ^ (int)Direction;
            }
        }

        public static bool operator ==(PlayerMovementStep left, PlayerMovementStep right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerMovementStep left, PlayerMovementStep right)
        {
            return !left.Equals(right);
        }

        private static GridPosition GetExpectedTarget(
            GridPosition from,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return from.Offset(0, 1);
                case CardinalDirection.East:
                    return from.Offset(1, 0);
                case CardinalDirection.South:
                    return from.Offset(0, -1);
                case CardinalDirection.West:
                    return from.Offset(-1, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
