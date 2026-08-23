using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class FixedStepAccumulatorTests
    {
        [Test]
        public void EqualTotalElapsed_ProducesEqualStepCountAcrossFramePartitions()
        {
            var single = new FixedStepAccumulator(TimeSpan.FromMilliseconds(10));
            var split = new FixedStepAccumulator(TimeSpan.FromMilliseconds(10));

            int singleCount = single.AddElapsed(TimeSpan.FromMilliseconds(500), 1000);
            int splitCount = 0;
            for (int index = 0; index < 5; index++)
            {
                splitCount += split.AddElapsed(TimeSpan.FromMilliseconds(100), 1000);
            }

            Assert.That(singleCount, Is.EqualTo(50));
            Assert.That(splitCount, Is.EqualTo(singleCount));
            Assert.That(split.Remainder, Is.EqualTo(single.Remainder));
        }

        [Test]
        public void SubstepRemainder_IsPreservedUntilItFormsAWholeStep()
        {
            var accumulator = new FixedStepAccumulator(TimeSpan.FromMilliseconds(10));

            Assert.That(accumulator.AddElapsed(TimeSpan.FromMilliseconds(6), 100), Is.Zero);
            Assert.That(accumulator.AddElapsed(TimeSpan.FromMilliseconds(6), 100), Is.EqualTo(1));
            Assert.That(accumulator.Remainder, Is.EqualTo(TimeSpan.FromMilliseconds(2)));
        }

        [Test]
        public void CatchUpLimit_IsCheckedBeforeAccumulatorStateChanges()
        {
            var accumulator = new FixedStepAccumulator(TimeSpan.FromMilliseconds(10));

            Assert.Throws<InvalidOperationException>(() =>
                accumulator.AddElapsed(TimeSpan.FromMilliseconds(50), 4));
            Assert.That(accumulator.Remainder, Is.EqualTo(TimeSpan.Zero));
        }
    }
}
