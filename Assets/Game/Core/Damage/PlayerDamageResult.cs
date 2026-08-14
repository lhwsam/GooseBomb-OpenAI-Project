using System;

namespace BombSwap.Core
{
    public enum PlayerDamageSourceKind
    {
        Explosion = 0,
        EnemyContact = 1,
    }

    public enum PlayerDamageStatus
    {
        Applied = 0,
        IgnoredInvulnerable = 1,
        IgnoredDuplicateExplosion = 2,
        IgnoredDead = 3,
    }

    public readonly struct PlayerDamageResult
    {
        internal PlayerDamageResult(
            PlayerDamageSourceKind sourceKind,
            BombId explosionId,
            ActorId sourceActorId,
            int requestedDamage,
            int previousHealth,
            int currentHealth,
            TimeSpan resolvedAt,
            TimeSpan invulnerableUntil,
            PlayerDamageStatus status)
        {
            SourceKind = sourceKind;
            ExplosionId = explosionId;
            SourceActorId = sourceActorId;
            RequestedDamage = requestedDamage;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            ResolvedAt = resolvedAt;
            InvulnerableUntil = invulnerableUntil;
            Status = status;
        }

        public PlayerDamageSourceKind SourceKind { get; }

        public BombId ExplosionId { get; }

        public ActorId SourceActorId { get; }

        public int RequestedDamage { get; }

        public int AppliedDamage => PreviousHealth - CurrentHealth;

        public int PreviousHealth { get; }

        public int CurrentHealth { get; }

        public TimeSpan ResolvedAt { get; }

        public TimeSpan InvulnerableUntil { get; }

        public PlayerDamageStatus Status { get; }

        public bool WasApplied => Status == PlayerDamageStatus.Applied;

        public bool WasFatal => WasApplied && PreviousHealth > 0 && CurrentHealth == 0;
    }
}
