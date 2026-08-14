using System;

namespace BombSwap.Core
{
    public sealed class ArmoredEnemyDefinition
    {
        public const int StageCount = 2;

        public ArmoredEnemyDefinition(
            EnemyDefinitionId id,
            int contactDamage,
            TimeSpan armoredStepInterval,
            TimeSpan brokenStepInterval,
            int directionCommitmentSteps)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Enemy definition ID must be valid.", nameof(id));
            }
            if (contactDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contactDamage),
                    contactDamage,
                    "Enemy contact damage must be positive.");
            }
            if (armoredStepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(armoredStepInterval),
                    armoredStepInterval,
                    "Armored movement step interval must be positive.");
            }
            if (brokenStepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(brokenStepInterval),
                    brokenStepInterval,
                    "Broken movement step interval must be positive.");
            }
            if (brokenStepInterval >= armoredStepInterval)
            {
                throw new ArgumentException(
                    "Broken armor cadence must be faster than armored cadence.",
                    nameof(brokenStepInterval));
            }
            if (directionCommitmentSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directionCommitmentSteps),
                    directionCommitmentSteps,
                    "Enemy direction commitment must be positive.");
            }

            Id = id;
            ContactDamage = contactDamage;
            ArmoredStepInterval = armoredStepInterval;
            BrokenStepInterval = brokenStepInterval;
            DirectionCommitmentSteps = directionCommitmentSteps;
        }

        public EnemyDefinitionId Id { get; }

        public int MaxHealth => StageCount;

        public int ContactDamage { get; }

        public TimeSpan ArmoredStepInterval { get; }

        public TimeSpan BrokenStepInterval { get; }

        public int DirectionCommitmentSteps { get; }

        public TimeSpan GetStepInterval(ArmoredEnemyState state)
        {
            switch (state)
            {
                case ArmoredEnemyState.Armored:
                    return ArmoredStepInterval;
                case ArmoredEnemyState.Broken:
                    return BrokenStepInterval;
                default:
                    throw new InvalidOperationException("A dead armored enemy has no movement cadence.");
            }
        }
    }
}
