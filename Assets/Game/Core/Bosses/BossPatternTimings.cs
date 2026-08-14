using System;

namespace BombSwap.Core
{
    public readonly struct BossPatternTimings : IEquatable<BossPatternTimings>
    {
        public BossPatternTimings(
            TimeSpan telegraphDuration,
            TimeSpan executeDuration,
            TimeSpan recoveryDuration)
        {
            ValidatePositive(telegraphDuration, nameof(telegraphDuration));
            ValidatePositive(executeDuration, nameof(executeDuration));
            ValidatePositive(recoveryDuration, nameof(recoveryDuration));

            TelegraphDuration = telegraphDuration;
            ExecuteDuration = executeDuration;
            RecoveryDuration = recoveryDuration;
        }

        public TimeSpan TelegraphDuration { get; }

        public TimeSpan ExecuteDuration { get; }

        public TimeSpan RecoveryDuration { get; }

        public bool Equals(BossPatternTimings other)
        {
            return TelegraphDuration == other.TelegraphDuration &&
                   ExecuteDuration == other.ExecuteDuration &&
                   RecoveryDuration == other.RecoveryDuration;
        }

        public override bool Equals(object obj)
        {
            return obj is BossPatternTimings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = TelegraphDuration.GetHashCode();
                hashCode = (hashCode * 397) ^ ExecuteDuration.GetHashCode();
                hashCode = (hashCode * 397) ^ RecoveryDuration.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(BossPatternTimings left, BossPatternTimings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BossPatternTimings left, BossPatternTimings right)
        {
            return !left.Equals(right);
        }

        private static void ValidatePositive(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Boss pattern timing must be positive.");
            }
        }
    }
}
