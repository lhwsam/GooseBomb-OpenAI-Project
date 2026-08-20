namespace BombSwap.Core
{
    public enum BossDamageStatus
    {
        Applied = 0,
        IgnoredDuplicateExplosion = 2,
        IgnoredDefeated = 3,
    }

    public enum BossDamageSource
    {
        PlayerBomb = 0,
        SelfDestruct = 1,
    }

    public readonly struct BossDamageResult
    {
        internal BossDamageResult(
            ActorId actorId,
            BombId explosionId,
            int requestedDamage,
            int previousHealth,
            int currentHealth,
            BossPhase phase,
            BossDamageSource source,
            BossDamageStatus status)
        {
            ActorId = actorId;
            ExplosionId = explosionId;
            RequestedDamage = requestedDamage;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            Phase = phase;
            Source = source;
            Status = status;
        }

        public ActorId ActorId { get; }

        public BombId ExplosionId { get; }

        public int RequestedDamage { get; }

        public int AppliedDamage => PreviousHealth - CurrentHealth;

        public int PreviousHealth { get; }

        public int CurrentHealth { get; }

        public BossPhase Phase { get; }

        public BossDamageSource Source { get; }

        public BossDamageStatus Status { get; }

        public bool WasApplied => Status == BossDamageStatus.Applied;

        public bool WasFatal => WasApplied && PreviousHealth > 0 && CurrentHealth == 0;
    }
}
