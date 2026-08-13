using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class PlayerHealthSimulation
    {
        private readonly IGameClock clock;
        private readonly HashSet<BombId> processedExplosionIds = new HashSet<BombId>();
        private TimeSpan lastObservedTime;

        public PlayerHealthSimulation(
            ActorId actorId,
            IGameClock clock,
            PlayerHealthDefinition definition)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Player actor ID must be valid.", nameof(actorId));
            }

            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clock),
                    clock.Now,
                    "Game time cannot be negative.");
            }

            ActorId = actorId;
            CurrentHealth = definition.MaxHealth;
            lastObservedTime = clock.Now;
        }

        public ActorId ActorId { get; }

        public PlayerHealthDefinition Definition { get; }

        public int MaxHealth => Definition.MaxHealth;

        public int CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth == 0;

        public TimeSpan InvulnerableUntil { get; private set; }

        public bool IsInvulnerable
        {
            get
            {
                TimeSpan now = clock.Now;
                EnsureClockDidNotMoveBackwards(now);
                lastObservedTime = now;
                return !IsDead && now < InvulnerableUntil;
            }
        }

        public PlayerDamageResult ApplyExplosionDamage(BombId explosionId, int damage)
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
                    "Damage must be positive.");
            }

            TimeSpan now = clock.Now;
            EnsureClockDidNotMoveBackwards(now);
            lastObservedTime = now;

            if (IsDead)
            {
                return CreateIgnoredResult(
                    explosionId,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredDead);
            }
            if (!processedExplosionIds.Add(explosionId))
            {
                return CreateIgnoredResult(
                    explosionId,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredDuplicateExplosion);
            }
            if (now < InvulnerableUntil)
            {
                return CreateIgnoredResult(
                    explosionId,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredInvulnerable);
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            InvulnerableUntil = AddWithSaturation(now, Definition.InvulnerabilityDuration);
            return new PlayerDamageResult(
                explosionId,
                damage,
                previousHealth,
                CurrentHealth,
                now,
                InvulnerableUntil,
                PlayerDamageStatus.Applied);
        }

        private PlayerDamageResult CreateIgnoredResult(
            BombId explosionId,
            int damage,
            TimeSpan now,
            PlayerDamageStatus status)
        {
            return new PlayerDamageResult(
                explosionId,
                damage,
                CurrentHealth,
                CurrentHealth,
                now,
                InvulnerableUntil,
                status);
        }

        private void EnsureClockDidNotMoveBackwards(TimeSpan now)
        {
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during player health simulation.");
            }
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }
    }
}
