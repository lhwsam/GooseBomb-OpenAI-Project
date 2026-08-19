using System;

namespace BombSwap.Core
{
    public readonly struct BossBombPlacement
    {
        internal BossBombPlacement(
            BombDefinition definition,
            GridPosition position,
            TimeSpan launchOffset,
            TimeSpan flightDuration)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (launchOffset < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(launchOffset));
            }
            if (flightDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(flightDuration));
            }

            Position = position;
            LaunchOffset = launchOffset;
            FlightDuration = flightDuration;
        }

        public BombDefinition Definition { get; }

        public GridPosition Position { get; }

        public TimeSpan LaunchOffset { get; }

        public TimeSpan FlightDuration { get; }
    }
}
