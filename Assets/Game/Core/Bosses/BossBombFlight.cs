using System;

namespace BombSwap.Core
{
    public readonly struct BossBombFlight
    {
        public BossBombFlight(
            int sequence,
            BombDefinition definition,
            GridPosition origin,
            GridPosition target,
            TimeSpan launchedAt,
            TimeSpan landsAt)
        {
            if (sequence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (landsAt <= launchedAt)
            {
                throw new ArgumentException("Boss bomb flight must end after launch.");
            }

            Sequence = sequence;
            Origin = origin;
            Target = target;
            LaunchedAt = launchedAt;
            LandsAt = landsAt;
        }

        public int Sequence { get; }

        public BombDefinition Definition { get; }

        public GridPosition Origin { get; }

        public GridPosition Target { get; }

        public TimeSpan LaunchedAt { get; }

        public TimeSpan LandsAt { get; }

        public TimeSpan Duration => LandsAt - LaunchedAt;
    }
}
