using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class EnemyMovementTransitionTests
    {
        [Test]
        public void Progress_UsesAuthoritativeGameTimeAndClampsAtBoundaries()
        {
            var movement = new EnemyMovementStep(
                new ActorId(2),
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                CardinalDirection.East);
            var transition = new EnemyMovementTransition(
                movement,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(0.5));

            Assert.That(transition.IsValid, Is.True);
            Assert.That(transition.StartedAt, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(transition.EndsAt, Is.EqualTo(TimeSpan.FromSeconds(2.5)));
            Assert.That(transition.GetProgress(TimeSpan.FromSeconds(1)), Is.Zero);
            Assert.That(transition.GetProgress(TimeSpan.FromSeconds(2.25)), Is.EqualTo(0.5d));
            Assert.That(transition.GetProgress(TimeSpan.FromSeconds(3)), Is.EqualTo(1d));
        }

        [Test]
        public void DefaultTransition_IsInvalidAndSamplesAsComplete()
        {
            EnemyMovementTransition transition = default;

            Assert.That(transition.IsValid, Is.False);
            Assert.That(transition.GetProgress(TimeSpan.Zero), Is.EqualTo(1d));
        }

        [Test]
        public void Constructor_RejectsMaximumStartTime()
        {
            var movement = new EnemyMovementStep(
                new ActorId(2),
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                CardinalDirection.East);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyMovementTransition(
                    movement,
                    TimeSpan.MaxValue,
                    TimeSpan.FromTicks(1)));
        }
    }
}
