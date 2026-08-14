using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class EnemyHealthSimulationTests
    {
        private static readonly ActorId EnemyActor = new ActorId(2);

        [Test]
        public void Constructor_StartsAliveAtMaximumHealth()
        {
            var health = new EnemyHealthSimulation(EnemyActor, 2);

            Assert.That(health.ActorId, Is.EqualTo(EnemyActor));
            Assert.That(health.MaxHealth, Is.EqualTo(2));
            Assert.That(health.CurrentHealth, Is.EqualTo(2));
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void OneHitEnemy_DiesAndClampsAtZero()
        {
            var health = new EnemyHealthSimulation(EnemyActor, 1);

            EnemyDamageResult result = health.ApplyExplosionDamage(CreateBombId(1), 3);

            Assert.That(result.Status, Is.EqualTo(EnemyDamageStatus.Applied));
            Assert.That(result.ActorId, Is.EqualTo(EnemyActor));
            Assert.That(result.RequestedDamage, Is.EqualTo(3));
            Assert.That(result.AppliedDamage, Is.EqualTo(1));
            Assert.That(result.PreviousHealth, Is.EqualTo(1));
            Assert.That(result.CurrentHealth, Is.Zero);
            Assert.That(result.WasApplied, Is.True);
            Assert.That(result.WasFatal, Is.True);
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void SameExplosion_IsProcessedAtMostOnce()
        {
            var health = new EnemyHealthSimulation(EnemyActor, 2);
            BombId explosionId = CreateBombId(1);
            health.ApplyExplosionDamage(explosionId, 1);

            EnemyDamageResult duplicate = health.ApplyExplosionDamage(explosionId, 1);

            Assert.That(duplicate.Status, Is.EqualTo(EnemyDamageStatus.IgnoredDuplicateExplosion));
            Assert.That(duplicate.WasApplied, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(1));
        }

        [Test]
        public void DamageAfterDeath_CannotRepeatFatalResult()
        {
            var health = new EnemyHealthSimulation(EnemyActor, 1);
            health.ApplyExplosionDamage(CreateBombId(1), 1);

            EnemyDamageResult ignored = health.ApplyExplosionDamage(CreateBombId(2), 1);

            Assert.That(ignored.Status, Is.EqualTo(EnemyDamageStatus.IgnoredDead));
            Assert.That(ignored.WasFatal, Is.False);
            Assert.That(health.CurrentHealth, Is.Zero);
        }

        [Test]
        public void InvalidConstructionAndDamage_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new EnemyHealthSimulation(default, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyHealthSimulation(EnemyActor, 0));

            var health = new EnemyHealthSimulation(EnemyActor, 1);
            Assert.Throws<ArgumentException>(() =>
                health.ApplyExplosionDamage(default, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                health.ApplyExplosionDamage(CreateBombId(1), 0));
            Assert.That(health.CurrentHealth, Is.EqualTo(1));
        }

        private static BombId CreateBombId(int sequence)
        {
            var grid = new GridState();
            var clock = new ManualGameClock();
            var bombs = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(100));
            var definition = new BombDefinition(
                new BombDefinitionId("enemy-health-test"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(10),
                0);
            BombId created = default;
            for (int index = 1; index <= sequence; index++)
            {
                var position = new GridPosition(index, 0);
                grid.TrySetTerrain(position, GridTerrain.Floor);
                Assert.That(
                    bombs.TryPlaceBomb(definition, position, new ActorId(1), out created),
                    Is.True);
            }

            return created;
        }
    }
}
