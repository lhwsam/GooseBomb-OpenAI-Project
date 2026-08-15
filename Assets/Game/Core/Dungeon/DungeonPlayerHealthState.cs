using System;

namespace BombSwap.Core
{
    public sealed class DungeonPlayerHealthState
    {
        public DungeonPlayerHealthState(int maxHealth)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    maxHealth,
                    "Maximum player health must be positive.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public int MaxHealth { get; }

        public int CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth == 0;

        public void RecordAppliedDamage(PlayerDamageResult result)
        {
            if (!result.WasApplied || result.RequestedDamage <= 0 ||
                result.PreviousHealth <= result.CurrentHealth ||
                result.CurrentHealth < 0 || result.PreviousHealth > MaxHealth)
            {
                throw new ArgumentException(
                    "Run health can record only a valid applied player damage result.",
                    nameof(result));
            }
            if (result.PreviousHealth != CurrentHealth)
            {
                throw new InvalidOperationException(
                    $"Player health desynchronized before damage: run={CurrentHealth}, " +
                    $"room={result.PreviousHealth}.");
            }

            CurrentHealth = result.CurrentHealth;
        }
    }
}
