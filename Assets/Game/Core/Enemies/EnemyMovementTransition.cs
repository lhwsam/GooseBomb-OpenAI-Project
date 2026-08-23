using System;

namespace BombSwap.Core
{
    public readonly struct EnemyMovementTransition : IEquatable<EnemyMovementTransition>
    {
        public EnemyMovementTransition(
            EnemyMovementStep movement,
            TimeSpan startedAt,
            TimeSpan duration)
        {
            if (!movement.ActorId.IsValid)
            {
                throw new ArgumentException("Movement actor ID must be valid.", nameof(movement));
            }
            if (startedAt < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(startedAt));
            }
            if (startedAt == TimeSpan.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(startedAt));
            }
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Movement = movement;
            StartedAt = startedAt;
            EndsAt = AddWithSaturation(startedAt, duration);
        }

        public EnemyMovementStep Movement { get; }

        public TimeSpan StartedAt { get; }

        public TimeSpan EndsAt { get; }

        public bool IsValid => Movement.ActorId.IsValid && EndsAt > StartedAt;

        public double GetProgress(TimeSpan now)
        {
            if (!IsValid)
            {
                return 1d;
            }
            if (now <= StartedAt)
            {
                return 0d;
            }
            if (now >= EndsAt)
            {
                return 1d;
            }

            return (now - StartedAt).TotalSeconds /
                (EndsAt - StartedAt).TotalSeconds;
        }

        public bool Equals(EnemyMovementTransition other)
        {
            return Movement.Equals(other.Movement) &&
                StartedAt == other.StartedAt &&
                EndsAt == other.EndsAt;
        }

        public override bool Equals(object obj)
        {
            return obj is EnemyMovementTransition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Movement.GetHashCode();
                hash = (hash * 397) ^ StartedAt.GetHashCode();
                hash = (hash * 397) ^ EndsAt.GetHashCode();
                return hash;
            }
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan delta)
        {
            long remainingTicks = TimeSpan.MaxValue.Ticks - value.Ticks;
            return delta.Ticks >= remainingTicks
                ? TimeSpan.MaxValue
                : value + delta;
        }
    }
}
