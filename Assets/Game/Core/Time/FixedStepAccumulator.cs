using System;

namespace BombSwap.Core
{
    public sealed class FixedStepAccumulator
    {
        private long remainderTicks;

        public FixedStepAccumulator(TimeSpan step)
        {
            if (step <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }

            Step = step;
        }

        public TimeSpan Step { get; }

        public TimeSpan Remainder => TimeSpan.FromTicks(remainderTicks);

        public int AddElapsed(TimeSpan elapsed, int maxStepCount)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }
            if (maxStepCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStepCount));
            }
            if (elapsed.Ticks > long.MaxValue - remainderTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }

            long totalTicks = remainderTicks + elapsed.Ticks;
            long stepCount = totalTicks / Step.Ticks;
            if (stepCount > maxStepCount)
            {
                throw new InvalidOperationException(
                    "Fixed-step catch-up exceeded its safety limit.");
            }

            remainderTicks = totalTicks % Step.Ticks;
            return (int)stepCount;
        }
    }
}
