using System;

namespace BombSwap.Core
{
    public sealed class ManualGameClock : IGameClock
    {
        public ManualGameClock()
            : this(TimeSpan.Zero)
        {
        }

        public ManualGameClock(TimeSpan initialTime)
        {
            if (initialTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialTime),
                    initialTime,
                    "Game time cannot start below zero.");
            }

            Now = initialTime;
        }

        public TimeSpan Now { get; private set; }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsed),
                    elapsed,
                    "Game time cannot move backwards.");
            }

            Now = Now.Add(elapsed);
        }
    }
}
