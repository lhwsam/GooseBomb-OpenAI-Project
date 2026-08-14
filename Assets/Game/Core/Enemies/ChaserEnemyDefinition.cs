using System;

namespace BombSwap.Core
{
    public sealed class ChaserEnemyDefinition
    {
        public ChaserEnemyDefinition(
            EnemyDefinitionId id,
            int maxHealth,
            int contactDamage,
            TimeSpan stepInterval,
            int directionCommitmentSteps)
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
            if (stepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stepInterval),
                    stepInterval,
                    "Enemy movement step interval must be positive.");
            }
            if (directionCommitmentSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directionCommitmentSteps),
                    directionCommitmentSteps,
                    "Enemy direction commitment must be positive.");
            }

            Id = id;
            MaxHealth = maxHealth;
            ContactDamage = contactDamage;
            StepInterval = stepInterval;
            DirectionCommitmentSteps = directionCommitmentSteps;
        }

        public EnemyDefinitionId Id { get; }

        public int MaxHealth { get; }

        public int ContactDamage { get; }

        public TimeSpan StepInterval { get; }

        public int DirectionCommitmentSteps { get; }
    }
}
