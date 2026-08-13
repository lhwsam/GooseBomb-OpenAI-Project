using System;

namespace BombSwap.Core
{
    public sealed class PlayerHealthDefinition
    {
        public PlayerHealthDefinition(int maxHealth, TimeSpan invulnerabilityDuration)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    maxHealth,
                    "Maximum health must be positive.");
            }
            if (invulnerabilityDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(invulnerabilityDuration),
                    invulnerabilityDuration,
                    "Invulnerability duration must be positive.");
            }

            MaxHealth = maxHealth;
            InvulnerabilityDuration = invulnerabilityDuration;
        }

        public int MaxHealth { get; }

        public TimeSpan InvulnerabilityDuration { get; }
    }
}
