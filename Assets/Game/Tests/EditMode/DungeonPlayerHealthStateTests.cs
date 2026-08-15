using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class DungeonPlayerHealthStateTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId EnemyActor = new ActorId(2);

        [Test]
        public void NewRun_StartsAtMaximumHealth()
        {
            var state = new DungeonPlayerHealthState(5);

            Assert.That(state.MaxHealth, Is.EqualTo(5));
            Assert.That(state.CurrentHealth, Is.EqualTo(5));
            Assert.That(state.IsDead, Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsNonPositiveMaximumHealth(int maxHealth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DungeonPlayerHealthState(maxHealth));
        }

        [Test]
        public void AppliedDamage_UpdatesRunHealthIncludingFatalDamage()
        {
            var state = new DungeonPlayerHealthState(5);
            var clock = new ManualGameClock();
            var roomHealth = new PlayerHealthSimulation(
                PlayerActor,
                clock,
                new PlayerHealthDefinition(5, TimeSpan.FromMilliseconds(750)));

            PlayerDamageResult first = roomHealth.ApplyContactDamage(EnemyActor, 2);
            state.RecordAppliedDamage(first);
            clock.Advance(TimeSpan.FromMilliseconds(750));
            PlayerDamageResult fatal = roomHealth.ApplyContactDamage(EnemyActor, 10);
            state.RecordAppliedDamage(fatal);

            Assert.That(state.CurrentHealth, Is.Zero);
            Assert.That(state.IsDead, Is.True);
        }

        [Test]
        public void IgnoredOrMalformedDamage_DoesNotChangeRunHealth()
        {
            var state = new DungeonPlayerHealthState(5);
            var roomHealth = new PlayerHealthSimulation(
                PlayerActor,
                new ManualGameClock(),
                new PlayerHealthDefinition(5, TimeSpan.FromMilliseconds(750)));
            roomHealth.ApplyContactDamage(EnemyActor, 1);
            PlayerDamageResult ignored = roomHealth.ApplyContactDamage(EnemyActor, 1);

            Assert.Throws<ArgumentException>(() => state.RecordAppliedDamage(ignored));
            Assert.Throws<ArgumentException>(() => state.RecordAppliedDamage(default));
            Assert.That(state.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void DamageFromDifferentHealthSnapshot_IsRejectedWithoutMutation()
        {
            var state = new DungeonPlayerHealthState(5);
            var roomHealth = new PlayerHealthSimulation(
                PlayerActor,
                new ManualGameClock(),
                new PlayerHealthDefinition(5, TimeSpan.FromMilliseconds(750)),
                4);
            PlayerDamageResult damage = roomHealth.ApplyContactDamage(EnemyActor, 1);

            Assert.Throws<InvalidOperationException>(() =>
                state.RecordAppliedDamage(damage));
            Assert.That(state.CurrentHealth, Is.EqualTo(5));
        }
    }
}
