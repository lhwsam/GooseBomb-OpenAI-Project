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
            : this(
                actorId,
                clock,
                definition,
                definition != null ? definition.MaxHealth : 0)
        {
        }

        public PlayerHealthSimulation(
            ActorId actorId,
            IGameClock clock,
            PlayerHealthDefinition definition,
            int initialHealth)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Player actor ID must be valid.", nameof(actorId));
            }

            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (initialHealth < 0 || initialHealth > definition.MaxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialHealth),
                    initialHealth,
                    $"Initial player health must be between 0 and {definition.MaxHealth}.");
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clock),
                    clock.Now,
                    "Game time cannot be negative.");
            }

            ActorId = actorId;
            CurrentHealth = initialHealth;
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
            ValidateDamage(damage);

            TimeSpan now = clock.Now;
            ObserveTime(now);
            if (IsDead)
            {
                return CreateIgnoredResult(
                    PlayerDamageSourceKind.Explosion,
                    explosionId,
                    default,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredDead);
            }
            if (!processedExplosionIds.Add(explosionId))
            {
                return CreateIgnoredResult(
                    PlayerDamageSourceKind.Explosion,
                    explosionId,
                    default,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredDuplicateExplosion);
            }
            return ApplyDamage(
                PlayerDamageSourceKind.Explosion,
                explosionId,
                default,
                damage,
                now);
        }

        public PlayerDamageResult ApplyContactDamage(ActorId sourceActorId, int damage)
        {
            ValidateEnemyDamageSource(sourceActorId, nameof(sourceActorId));
            ValidateDamage(damage);

            TimeSpan now = clock.Now;
            ObserveTime(now);
            if (IsDead)
            {
                return CreateIgnoredResult(
                    PlayerDamageSourceKind.EnemyContact,
                    default,
                    sourceActorId,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredDead);
            }

            return ApplyDamage(
                PlayerDamageSourceKind.EnemyContact,
                default,
                sourceActorId,
                damage,
                now);
        }

        public PlayerDamageResult ApplyBossPatternDamage(
            ActorId sourceActorId,
            int damage)
        {
            ValidateEnemyDamageSource(sourceActorId, nameof(sourceActorId));
            ValidateDamage(damage);

            TimeSpan now = clock.Now;
            ObserveTime(now);
            if (IsDead)
            {
                return CreateIgnoredResult(
                    PlayerDamageSourceKind.BossPattern,
                    default,
                    sourceActorId,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredDead);
            }

            return ApplyDamage(
                PlayerDamageSourceKind.BossPattern,
                default,
                sourceActorId,
                damage,
                now);
        }

        private PlayerDamageResult ApplyDamage(
            PlayerDamageSourceKind sourceKind,
            BombId explosionId,
            ActorId sourceActorId,
            int damage,
            TimeSpan now)
        {
            if (now < InvulnerableUntil)
            {
                return CreateIgnoredResult(
                    sourceKind,
                    explosionId,
                    sourceActorId,
                    damage,
                    now,
                    PlayerDamageStatus.IgnoredInvulnerable);
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            InvulnerableUntil = AddWithSaturation(now, Definition.InvulnerabilityDuration);
            return new PlayerDamageResult(
                sourceKind,
                explosionId,
                sourceActorId,
                damage,
                previousHealth,
                CurrentHealth,
                now,
                InvulnerableUntil,
                PlayerDamageStatus.Applied);
        }

        private PlayerDamageResult CreateIgnoredResult(
            PlayerDamageSourceKind sourceKind,
            BombId explosionId,
            ActorId sourceActorId,
            int damage,
            TimeSpan now,
            PlayerDamageStatus status)
        {
            return new PlayerDamageResult(
                sourceKind,
                explosionId,
                sourceActorId,
                damage,
                CurrentHealth,
                CurrentHealth,
                now,
                InvulnerableUntil,
                status);
        }

        private void ObserveTime(TimeSpan now)
        {
            EnsureClockDidNotMoveBackwards(now);
            lastObservedTime = now;
        }

        private static void ValidateDamage(int damage)
        {
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage),
                    damage,
                    "Damage must be positive.");
            }
        }

        private void ValidateEnemyDamageSource(
            ActorId sourceActorId,
            string parameterName)
        {
            if (!sourceActorId.IsValid)
            {
                throw new ArgumentException(
                    "Enemy damage source actor ID must be valid.",
                    parameterName);
            }
            if (sourceActorId == ActorId)
            {
                throw new ArgumentException(
                    "Player cannot be the source of enemy damage.",
                    parameterName);
            }
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
