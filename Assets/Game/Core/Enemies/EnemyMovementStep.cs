using System;

namespace BombSwap.Core
{
    public readonly struct EnemyMovementStep : IEquatable<EnemyMovementStep>
    {
        public EnemyMovementStep(
            ActorId actorId,
            GridPosition from,
            GridPosition to,
            CardinalDirection direction)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Enemy movement requires a valid actor ID.", nameof(actorId));
            }
            if (direction == CardinalDirection.None ||
                direction < CardinalDirection.None ||
                direction > CardinalDirection.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    "Enemy movement requires a defined non-zero direction.");
            }
            if (to != GetTarget(from, direction))
            {
                throw new ArgumentException(
                    "Enemy movement target must be one cardinal cell away.",
                    nameof(to));
            }

            ActorId = actorId;
            From = from;
            To = to;
            Direction = direction;
        }

        public ActorId ActorId { get; }

        public GridPosition From { get; }

        public GridPosition To { get; }

        public CardinalDirection Direction { get; }

        public bool Equals(EnemyMovementStep other)
        {
            return ActorId == other.ActorId && From == other.From &&
                To == other.To && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is EnemyMovementStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ActorId.GetHashCode();
                hash = (hash * 397) ^ From.GetHashCode();
                hash = (hash * 397) ^ To.GetHashCode();
                return (hash * 397) ^ (int)Direction;
            }
        }

        public static bool operator ==(EnemyMovementStep left, EnemyMovementStep right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EnemyMovementStep left, EnemyMovementStep right)
        {
            return !left.Equals(right);
        }

        private static GridPosition GetTarget(
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
