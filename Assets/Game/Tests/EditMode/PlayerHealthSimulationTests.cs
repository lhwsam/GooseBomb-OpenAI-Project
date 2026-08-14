using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class PlayerHealthSimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId EnemyActor = new ActorId(2);
        private static readonly TimeSpan Invulnerability = TimeSpan.FromMilliseconds(750);

        [Test]
        public void Definition_StoresPositiveHealthAndInvulnerability()
        {
            var definition = new PlayerHealthDefinition(5, Invulnerability);

            Assert.That(definition.MaxHealth, Is.EqualTo(5));
            Assert.That(definition.InvulnerabilityDuration, Is.EqualTo(Invulnerability));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveHealth(int maxHealth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerHealthDefinition(maxHealth, Invulnerability));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveInvulnerability(long ticks)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerHealthDefinition(5, TimeSpan.FromTicks(ticks)));
        }

        [Test]
        public void Constructor_StartsAliveAtMaximumHealth()
        {
            var simulation = CreateSimulation(new ManualGameClock());

            Assert.That(simulation.ActorId, Is.EqualTo(PlayerActor));
            Assert.That(simulation.MaxHealth, Is.EqualTo(5));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(5));
            Assert.That(simulation.IsDead, Is.False);
            Assert.That(simulation.IsInvulnerable, Is.False);
        }

        [Test]
        public void ApplyExplosionDamage_ReducesHealthAndStartsInvulnerability()
        {
            var clock = new ManualGameClock(TimeSpan.FromSeconds(2));
            var simulation = CreateSimulation(clock);

            PlayerDamageResult result = simulation.ApplyExplosionDamage(CreateBombId(1), 1);

            Assert.That(result.Status, Is.EqualTo(PlayerDamageStatus.Applied));
            Assert.That(result.SourceKind, Is.EqualTo(PlayerDamageSourceKind.Explosion));
            Assert.That(result.ExplosionId, Is.EqualTo(CreateBombId(1)));
            Assert.That(result.SourceActorId.IsValid, Is.False);
            Assert.That(result.WasApplied, Is.True);
            Assert.That(result.WasFatal, Is.False);
            Assert.That(result.PreviousHealth, Is.EqualTo(5));
            Assert.That(result.CurrentHealth, Is.EqualTo(4));
            Assert.That(result.AppliedDamage, Is.EqualTo(1));
            Assert.That(result.ResolvedAt, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(result.InvulnerableUntil, Is.EqualTo(TimeSpan.FromSeconds(2) + Invulnerability));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(4));
            Assert.That(simulation.IsInvulnerable, Is.True);
        }

        [Test]
        public void ApplyContactDamage_PreservesEnemySourceAndSharesInvulnerability()
        {
            var clock = new ManualGameClock(TimeSpan.FromSeconds(2));
            var simulation = CreateSimulation(clock);

            PlayerDamageResult applied = simulation.ApplyContactDamage(EnemyActor, 1);
            PlayerDamageResult ignored = simulation.ApplyContactDamage(EnemyActor, 1);
            clock.Advance(Invulnerability);
            PlayerDamageResult repeated = simulation.ApplyContactDamage(EnemyActor, 1);

            Assert.That(applied.SourceKind, Is.EqualTo(PlayerDamageSourceKind.EnemyContact));
            Assert.That(applied.SourceActorId, Is.EqualTo(EnemyActor));
            Assert.That(applied.ExplosionId.IsValid, Is.False);
            Assert.That(applied.WasApplied, Is.True);
            Assert.That(ignored.Status, Is.EqualTo(PlayerDamageStatus.IgnoredInvulnerable));
            Assert.That(repeated.WasApplied, Is.True);
            Assert.That(repeated.ResolvedAt, Is.EqualTo(TimeSpan.FromSeconds(2) + Invulnerability));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void ExplosionAndContactDamage_UseOneInvulnerabilityWindow()
        {
            var clock = new ManualGameClock();
            var simulation = CreateSimulation(clock);
            simulation.ApplyExplosionDamage(CreateBombId(1), 1);

            PlayerDamageResult ignoredContact = simulation.ApplyContactDamage(EnemyActor, 1);
            clock.Advance(Invulnerability);
            PlayerDamageResult laterContact = simulation.ApplyContactDamage(EnemyActor, 1);

            Assert.That(ignoredContact.Status, Is.EqualTo(PlayerDamageStatus.IgnoredInvulnerable));
            Assert.That(laterContact.WasApplied, Is.True);
            Assert.That(simulation.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void BossPatternDamage_PreservesBossSourceAndSharesInvulnerability()
        {
            var clock = new ManualGameClock();
            var simulation = CreateSimulation(clock);

            PlayerDamageResult applied = simulation.ApplyBossPatternDamage(EnemyActor, 1);
            PlayerDamageResult ignoredExplosion =
                simulation.ApplyExplosionDamage(CreateBombId(1), 1);
            clock.Advance(Invulnerability);
            PlayerDamageResult repeated =
                simulation.ApplyBossPatternDamage(EnemyActor, 2);

            Assert.That(applied.SourceKind, Is.EqualTo(PlayerDamageSourceKind.BossPattern));
            Assert.That(applied.SourceActorId, Is.EqualTo(EnemyActor));
            Assert.That(applied.ExplosionId.IsValid, Is.False);
            Assert.That(applied.WasApplied, Is.True);
            Assert.That(
                ignoredExplosion.Status,
                Is.EqualTo(PlayerDamageStatus.IgnoredInvulnerable));
            Assert.That(repeated.WasApplied, Is.True);
            Assert.That(simulation.CurrentHealth, Is.EqualTo(2));
        }

        [Test]
        public void ApplyContactDamage_RejectsInvalidSelfSourceAndDamage()
        {
            var simulation = CreateSimulation(new ManualGameClock());

            Assert.Throws<ArgumentException>(() =>
                simulation.ApplyContactDamage(default, 1));
            Assert.Throws<ArgumentException>(() =>
                simulation.ApplyContactDamage(PlayerActor, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simulation.ApplyContactDamage(EnemyActor, 0));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void ApplyBossPatternDamage_RejectsInvalidSelfSourceAndDamage()
        {
            var simulation = CreateSimulation(new ManualGameClock());

            Assert.Throws<ArgumentException>(() =>
                simulation.ApplyBossPatternDamage(default, 1));
            Assert.Throws<ArgumentException>(() =>
                simulation.ApplyBossPatternDamage(PlayerActor, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simulation.ApplyBossPatternDamage(EnemyActor, 0));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void SameExplosion_IsProcessedAtMostOnce()
        {
            var simulation = CreateSimulation(new ManualGameClock());
            BombId explosionId = CreateBombId(1);
            simulation.ApplyExplosionDamage(explosionId, 1);

            PlayerDamageResult duplicate = simulation.ApplyExplosionDamage(explosionId, 1);

            Assert.That(duplicate.Status, Is.EqualTo(PlayerDamageStatus.IgnoredDuplicateExplosion));
            Assert.That(duplicate.WasApplied, Is.False);
            Assert.That(duplicate.AppliedDamage, Is.Zero);
            Assert.That(simulation.CurrentHealth, Is.EqualTo(4));
        }

        [Test]
        public void DifferentExplosionDuringInvulnerability_IsIgnoredAndNotDeferred()
        {
            var clock = new ManualGameClock();
            var simulation = CreateSimulation(clock);
            simulation.ApplyExplosionDamage(CreateBombId(1), 1);

            PlayerDamageResult ignored = simulation.ApplyExplosionDamage(CreateBombId(2), 1);
            clock.Advance(Invulnerability);
            PlayerDamageResult replay = simulation.ApplyExplosionDamage(CreateBombId(2), 1);

            Assert.That(ignored.Status, Is.EqualTo(PlayerDamageStatus.IgnoredInvulnerable));
            Assert.That(replay.Status, Is.EqualTo(PlayerDamageStatus.IgnoredDuplicateExplosion));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(4));
        }

        [Test]
        public void Invulnerability_IsActiveJustBeforeBoundaryAndEndsAtExactBoundary()
        {
            var clock = new ManualGameClock();
            var simulation = CreateSimulation(clock);
            simulation.ApplyExplosionDamage(CreateBombId(1), 1);

            clock.Advance(Invulnerability - TimeSpan.FromTicks(1));
            PlayerDamageResult justBefore = simulation.ApplyExplosionDamage(CreateBombId(2), 1);
            clock.Advance(TimeSpan.FromTicks(1));
            PlayerDamageResult atBoundary = simulation.ApplyExplosionDamage(CreateBombId(3), 1);

            Assert.That(justBefore.Status, Is.EqualTo(PlayerDamageStatus.IgnoredInvulnerable));
            Assert.That(atBoundary.Status, Is.EqualTo(PlayerDamageStatus.Applied));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void FatalDamage_ClampsHealthAndLaterDamageCannotRepeatDeath()
        {
            var clock = new ManualGameClock();
            var definition = new PlayerHealthDefinition(2, Invulnerability);
            var simulation = new PlayerHealthSimulation(PlayerActor, clock, definition);
            simulation.ApplyExplosionDamage(CreateBombId(1), 1);
            clock.Advance(Invulnerability);

            PlayerDamageResult fatal = simulation.ApplyExplosionDamage(CreateBombId(2), 10);
            clock.Advance(Invulnerability);
            PlayerDamageResult afterDeath = simulation.ApplyExplosionDamage(CreateBombId(3), 1);

            Assert.That(fatal.WasApplied, Is.True);
            Assert.That(fatal.WasFatal, Is.True);
            Assert.That(fatal.CurrentHealth, Is.Zero);
            Assert.That(fatal.AppliedDamage, Is.EqualTo(1));
            Assert.That(simulation.CurrentHealth, Is.Zero);
            Assert.That(simulation.IsDead, Is.True);
            Assert.That(simulation.IsInvulnerable, Is.False);
            Assert.That(afterDeath.Status, Is.EqualTo(PlayerDamageStatus.IgnoredDead));
            Assert.That(afterDeath.WasFatal, Is.False);
        }

        [Test]
        public void ApplyExplosionDamage_SaturatesInvulnerabilityAtMaximumTime()
        {
            var clock = new ManualGameClock(TimeSpan.MaxValue - TimeSpan.FromTicks(1));
            var definition = new PlayerHealthDefinition(5, TimeSpan.FromTicks(2));
            var simulation = new PlayerHealthSimulation(PlayerActor, clock, definition);

            PlayerDamageResult result = simulation.ApplyExplosionDamage(CreateBombId(1), 1);

            Assert.That(result.InvulnerableUntil, Is.EqualTo(TimeSpan.MaxValue));
        }

        [Test]
        public void ClockMovingBackwards_IsRejectedWithoutHealthMutation()
        {
            var clock = new MutableGameClock(TimeSpan.FromSeconds(2));
            var simulation = CreateSimulation(clock);
            clock.Now = TimeSpan.FromSeconds(1);

            Assert.Throws<InvalidOperationException>(() =>
                simulation.ApplyExplosionDamage(CreateBombId(1), 1));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void InvalidExplosionOrDamage_IsRejectedWithoutHealthMutation()
        {
            var simulation = CreateSimulation(new ManualGameClock());

            Assert.Throws<ArgumentException>(() =>
                simulation.ApplyExplosionDamage(default, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simulation.ApplyExplosionDamage(CreateBombId(1), 0));
            Assert.That(simulation.CurrentHealth, Is.EqualTo(5));
        }

        private static PlayerHealthSimulation CreateSimulation(IGameClock clock)
        {
            return new PlayerHealthSimulation(
                PlayerActor,
                clock,
                new PlayerHealthDefinition(5, Invulnerability));
        }

        private static BombId CreateBombId(int sequence)
        {
            var grid = new GridState();
            var clock = new ManualGameClock();
            var bombs = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(100));
            var definition = new BombDefinition(
                new BombDefinitionId("health-test"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(10),
                0);
            BombId created = default;
            for (int index = 1; index <= sequence; index++)
            {
                var position = new GridPosition(index, 0);
                grid.TrySetTerrain(position, GridTerrain.Floor);
                Assert.That(
                    bombs.TryPlaceBomb(definition, position, PlayerActor, out created),
                    Is.True);
            }

            return created;
        }

        private sealed class MutableGameClock : IGameClock
        {
            public MutableGameClock(TimeSpan now)
            {
                Now = now;
            }

            public TimeSpan Now { get; set; }
        }
    }
}
