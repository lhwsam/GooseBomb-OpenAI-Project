using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class EnemyHealthSimulation
    {
        private readonly HashSet<BombId> processedExplosionIds = new HashSet<BombId>();

        public EnemyHealthSimulation(ActorId actorId, int maxHealth)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Enemy actor ID must be valid.", nameof(actorId));
            }
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    maxHealth,
                    "Enemy maximum health must be positive.");
            }

            ActorId = actorId;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public ActorId ActorId { get; }

        public int MaxHealth { get; }

        public int CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth == 0;

        public EnemyDamageResult ApplyExplosionDamage(BombId explosionId, int damage)
        {
            if (!explosionId.IsValid)
            {
                throw new ArgumentException("Explosion ID must be valid.", nameof(explosionId));
            }
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage),
                    damage,
                    "Enemy damage must be positive.");
            }

            if (IsDead)
            {
                return CreateIgnoredResult(
                    explosionId,
                    damage,
                    EnemyDamageStatus.IgnoredDead);
            }
            if (!processedExplosionIds.Add(explosionId))
            {
                return CreateIgnoredResult(
                    explosionId,
                    damage,
                    EnemyDamageStatus.IgnoredDuplicateExplosion);
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            return new EnemyDamageResult(
                ActorId,
                explosionId,
                damage,
                previousHealth,
                CurrentHealth,
                EnemyDamageStatus.Applied);
        }

        private EnemyDamageResult CreateIgnoredResult(
            BombId explosionId,
            int damage,
            EnemyDamageStatus status)
        {
            return new EnemyDamageResult(
                ActorId,
                explosionId,
                damage,
                CurrentHealth,
                CurrentHealth,
                status);
        }
    }
}
