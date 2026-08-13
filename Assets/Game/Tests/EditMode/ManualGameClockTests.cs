using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class ManualGameClockTests
    {
        [Test]
        public void DefaultClock_StartsAtZero()
        {
            IGameClock clock = new ManualGameClock();

            Assert.That(clock.Now, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void InitialTime_IsObservedThroughClockContract()
        {
            TimeSpan initialTime = TimeSpan.FromSeconds(3.5);
            IGameClock clock = new ManualGameClock(initialTime);

            Assert.That(clock.Now, Is.EqualTo(initialTime));
        }

        [Test]
        public void Advance_AccumulatesElapsedTimeDeterministically()
        {
            var clock = new ManualGameClock();

            clock.Advance(TimeSpan.FromMilliseconds(125));
            clock.Advance(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromMilliseconds(375));

            Assert.That(clock.Now, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
        }

        [Test]
        public void Constructor_RejectsNegativeInitialTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ManualGameClock(TimeSpan.FromTicks(-1)));
        }

        [Test]
        public void Advance_RejectsNegativeElapsedTimeWithoutChangingClock()
        {
            var clock = new ManualGameClock(TimeSpan.FromSeconds(2));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => clock.Advance(TimeSpan.FromTicks(-1)));
            Assert.That(clock.Now, Is.EqualTo(TimeSpan.FromSeconds(2)));
        }
    }
}
