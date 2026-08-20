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
            int directionCommitmentSteps,
            int guardRadius,
            TimeSpan panicTelegraphDuration,
            TimeSpan panicStepInterval,
            int panicRunDistance,
            TimeSpan panicRecoverDuration)
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
            if (guardRadius <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(guardRadius),
                    guardRadius,
                    "Armored guard radius must be positive.");
            }
            if (panicTelegraphDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panicTelegraphDuration),
                    panicTelegraphDuration,
                    "Panic telegraph duration must be positive.");
            }
            if (panicStepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panicStepInterval),
                    panicStepInterval,
                    "Panic movement step interval must be positive.");
            }
            if (panicStepInterval >= brokenStepInterval)
            {
                throw new ArgumentException(
                    "Panic movement cadence must be faster than broken chase cadence.",
                    nameof(panicStepInterval));
            }
            if (panicRunDistance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panicRunDistance),
                    panicRunDistance,
                    "Panic run distance must be positive.");
            }
            if (panicRecoverDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panicRecoverDuration),
                    panicRecoverDuration,
                    "Panic recover duration must be positive.");
            }

            Id = id;
            ContactDamage = contactDamage;
            ArmoredStepInterval = armoredStepInterval;
            BrokenStepInterval = brokenStepInterval;
            DirectionCommitmentSteps = directionCommitmentSteps;
            GuardRadius = guardRadius;
            PanicTelegraphDuration = panicTelegraphDuration;
            PanicStepInterval = panicStepInterval;
            PanicRunDistance = panicRunDistance;
            PanicRecoverDuration = panicRecoverDuration;
        }

        public EnemyDefinitionId Id { get; }

        public int MaxHealth => StageCount;

        public int ContactDamage { get; }

        public TimeSpan ArmoredStepInterval { get; }

        public TimeSpan BrokenStepInterval { get; }

        public int DirectionCommitmentSteps { get; }

        public int GuardRadius { get; }

        public TimeSpan PanicTelegraphDuration { get; }

        public TimeSpan PanicStepInterval { get; }

        public int PanicRunDistance { get; }

        public TimeSpan PanicRecoverDuration { get; }

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
