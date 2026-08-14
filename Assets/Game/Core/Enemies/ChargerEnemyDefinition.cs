using System;

namespace BombSwap.Core
{
    public sealed class ChargerEnemyDefinition
    {
        public ChargerEnemyDefinition(
            EnemyDefinitionId id,
            int maxHealth,
            int contactDamage,
            TimeSpan telegraphDuration,
            TimeSpan chargeStepInterval,
            TimeSpan recoverDuration)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Enemy definition ID must be valid.", nameof(id));
            }
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    maxHealth,
                    "Enemy maximum health must be positive.");
            }
            if (contactDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contactDamage),
                    contactDamage,
                    "Enemy contact damage must be positive.");
            }
            ValidatePositive(telegraphDuration, nameof(telegraphDuration));
            ValidatePositive(chargeStepInterval, nameof(chargeStepInterval));
            ValidatePositive(recoverDuration, nameof(recoverDuration));

            Id = id;
            MaxHealth = maxHealth;
            ContactDamage = contactDamage;
            TelegraphDuration = telegraphDuration;
            ChargeStepInterval = chargeStepInterval;
            RecoverDuration = recoverDuration;
        }

        public EnemyDefinitionId Id { get; }

        public int MaxHealth { get; }

        public int ContactDamage { get; }

        public TimeSpan TelegraphDuration { get; }

        public TimeSpan ChargeStepInterval { get; }

        public TimeSpan RecoverDuration { get; }

        private static void ValidatePositive(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy timing must be positive.");
            }
        }
    }
}
