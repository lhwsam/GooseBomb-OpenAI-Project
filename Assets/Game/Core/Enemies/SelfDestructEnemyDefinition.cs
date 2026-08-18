using System;

namespace BombSwap.Core
{
    public sealed class SelfDestructEnemyDefinition
    {
        public SelfDestructEnemyDefinition(
            EnemyDefinitionId id,
            TimeSpan chaseStepInterval,
            TimeSpan warningMinimumStepInterval,
            TimeSpan warningEscalationDuration,
            int warningDistance,
            int primeDistance,
            BombDefinition detonationBombDefinition)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Enemy definition ID must be valid.", nameof(id));
            }
            if (chaseStepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chaseStepInterval),
                    chaseStepInterval,
                    "Chase step interval must be greater than zero.");
            }
            if (warningMinimumStepInterval <= TimeSpan.Zero ||
                warningMinimumStepInterval > chaseStepInterval)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(warningMinimumStepInterval),
                    warningMinimumStepInterval,
                    "Warning minimum step interval must be positive and no greater than the chase interval.");
            }
            if (warningEscalationDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(warningEscalationDuration),
                    warningEscalationDuration,
                    "Warning escalation duration must be greater than zero.");
            }
            if (warningDistance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(warningDistance),
                    warningDistance,
                    "Warning distance must be greater than zero.");
            }
            if (primeDistance <= 0 || primeDistance >= warningDistance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(primeDistance),
                    primeDistance,
                    "Prime distance must be positive and less than warning distance.");
            }
            if (detonationBombDefinition == null)
            {
                throw new ArgumentNullException(nameof(detonationBombDefinition));
            }
            if (detonationBombDefinition.ExplosionShape != BombExplosionShape.Cross ||
                detonationBombDefinition.Range <= 0)
            {
                throw new ArgumentException(
                    "Self-destruct detonation must be a positive-range cross explosion.",
                    nameof(detonationBombDefinition));
            }

            Id = id;
            ChaseStepInterval = chaseStepInterval;
            WarningMinimumStepInterval = warningMinimumStepInterval;
            WarningEscalationDuration = warningEscalationDuration;
            WarningDistance = warningDistance;
            PrimeDistance = primeDistance;
            DetonationBombDefinition = detonationBombDefinition;
        }

        public EnemyDefinitionId Id { get; }

        public TimeSpan ChaseStepInterval { get; }

        public TimeSpan WarningMinimumStepInterval { get; }

        public TimeSpan WarningEscalationDuration { get; }

        public int WarningDistance { get; }

        public int PrimeDistance { get; }

        public BombDefinition DetonationBombDefinition { get; }
    }
}
