using System;

namespace BombSwap.Core
{
    public readonly struct ThrowerBombFlight
    {
        public ThrowerBombFlight(
            ActorId ownerId,
            BombDefinition definition,
            GridPosition origin,
            GridPosition target,
            TimeSpan launchedAt,
            TimeSpan landsAt)
        {
            if (!ownerId.IsValid)
            {
                throw new ArgumentException("Thrower owner ID must be valid.", nameof(ownerId));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (launchedAt < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(launchedAt));
            }
            if (landsAt <= launchedAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(landsAt),
                    landsAt,
                    "Landing time must be after launch time.");
            }

            OwnerId = ownerId;
            Definition = definition;
            Origin = origin;
            Target = target;
            LaunchedAt = launchedAt;
            LandsAt = landsAt;
        }

        public ActorId OwnerId { get; }

        public BombDefinition Definition { get; }

        public GridPosition Origin { get; }

        public GridPosition Target { get; }

        public TimeSpan LaunchedAt { get; }

        public TimeSpan LandsAt { get; }

        public TimeSpan Duration => LandsAt - LaunchedAt;
    }
}
