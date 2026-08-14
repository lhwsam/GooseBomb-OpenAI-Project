using System;

namespace BombSwap.Core
{
    public readonly struct PlayerCommand : IEquatable<PlayerCommand>
    {
        private PlayerCommand(PlayerCommandKind kind, CardinalDirection moveDirection)
        {
            Kind = kind;
            MoveDirection = moveDirection;
        }

        public PlayerCommandKind Kind { get; }

        public CardinalDirection MoveDirection { get; }

        public bool IsValid
        {
            get
            {
                switch (Kind)
                {
                    case PlayerCommandKind.Move:
                        return IsDefinedDirection(MoveDirection);
                    case PlayerCommandKind.PlaceBomb:
                    case PlayerCommandKind.SwapBomb:
                    case PlayerCommandKind.Pause:
                    case PlayerCommandKind.RestartRun:
                        return MoveDirection == CardinalDirection.None;
                    default:
                        return false;
                }
            }
        }

        public static PlayerCommand Move(CardinalDirection direction)
        {
            if (!IsDefinedDirection(direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Move direction is not defined.");
            }

            return new PlayerCommand(PlayerCommandKind.Move, direction);
        }

        public static PlayerCommand PlaceBomb()
        {
            return new PlayerCommand(PlayerCommandKind.PlaceBomb, CardinalDirection.None);
        }

        public static PlayerCommand SwapBomb()
        {
            return new PlayerCommand(PlayerCommandKind.SwapBomb, CardinalDirection.None);
        }

        public static PlayerCommand Pause()
        {
            return new PlayerCommand(PlayerCommandKind.Pause, CardinalDirection.None);
        }

        public static PlayerCommand RestartRun()
        {
            return new PlayerCommand(PlayerCommandKind.RestartRun, CardinalDirection.None);
        }

        public bool Equals(PlayerCommand other)
        {
            return Kind == other.Kind && MoveDirection == other.MoveDirection;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerCommand other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (int)MoveDirection;
            }
        }

        public override string ToString()
        {
            return Kind == PlayerCommandKind.Move
                ? $"{Kind}({MoveDirection})"
                : Kind.ToString();
        }

        public static bool operator ==(PlayerCommand left, PlayerCommand right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerCommand left, PlayerCommand right)
        {
            return !left.Equals(right);
        }

        private static bool IsDefinedDirection(CardinalDirection direction)
        {
            return direction >= CardinalDirection.None && direction <= CardinalDirection.West;
        }
    }
}
