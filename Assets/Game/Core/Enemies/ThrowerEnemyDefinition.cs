using System;

namespace BombSwap.Core
{
    public sealed class ThrowerEnemyDefinition
    {
        public ThrowerEnemyDefinition(
            EnemyDefinitionId id,
            TimeSpan moveStepInterval,
            TimeSpan telegraphDuration,
            TimeSpan flightDuration,
            TimeSpan recoveryDuration,
            int maxHealth,
            int bombsPerVolley,
            BombDefinition bombDefinition)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Enemy definition ID must be valid.", nameof(id));
            }
            ValidatePositive(moveStepInterval, nameof(moveStepInterval));
            ValidatePositive(telegraphDuration, nameof(telegraphDuration));
            ValidatePositive(flightDuration, nameof(flightDuration));
            ValidatePositive(recoveryDuration, nameof(recoveryDuration));
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    maxHealth,
                    "Maximum health must be greater than zero.");
            }
            if (bombsPerVolley <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bombsPerVolley),
                    bombsPerVolley,
                    "Bombs per volley must be greater than zero.");
            }

            Id = id;
            MoveStepInterval = moveStepInterval;
            TelegraphDuration = telegraphDuration;
            FlightDuration = flightDuration;
            RecoveryDuration = recoveryDuration;
            MaxHealth = maxHealth;
            BombsPerVolley = bombsPerVolley;
            BombDefinition = bombDefinition ??
                throw new ArgumentNullException(nameof(bombDefinition));
        }

        public EnemyDefinitionId Id { get; }

        public TimeSpan MoveStepInterval { get; }

        public TimeSpan TelegraphDuration { get; }

        public TimeSpan FlightDuration { get; }

        public TimeSpan RecoveryDuration { get; }

        public int MaxHealth { get; }

        public int BombsPerVolley { get; }

        public BombDefinition BombDefinition { get; }

        private static void ValidatePositive(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Duration must be greater than zero.");
            }
        }
    }
}
