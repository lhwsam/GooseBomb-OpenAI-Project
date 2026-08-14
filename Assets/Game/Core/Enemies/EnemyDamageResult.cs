using System;

namespace BombSwap.Core
{
    public enum EnemyDamageStatus
    {
        Applied = 0,
        IgnoredDuplicateExplosion = 1,
        IgnoredDead = 2,
    }

    public readonly struct EnemyDamageResult
    {
        internal EnemyDamageResult(
            ActorId actorId,
            BombId explosionId,
            int requestedDamage,
            int previousHealth,
            int currentHealth,
            EnemyDamageStatus status)
        {
            ActorId = actorId;
            ExplosionId = explosionId;
            RequestedDamage = requestedDamage;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            Status = status;
        }

        public ActorId ActorId { get; }

        public BombId ExplosionId { get; }

        public int RequestedDamage { get; }

        public int AppliedDamage => PreviousHealth - CurrentHealth;

        public int PreviousHealth { get; }

        public int CurrentHealth { get; }

        public EnemyDamageStatus Status { get; }

        public bool WasApplied => Status == EnemyDamageStatus.Applied;

        public bool WasFatal => WasApplied && PreviousHealth > 0 && CurrentHealth == 0;
    }
}
