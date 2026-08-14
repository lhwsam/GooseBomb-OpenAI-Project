using System;

namespace BombSwap.Core
{
    public readonly struct BombSnapshot
    {
        internal BombSnapshot(
            BombId id,
            BombDefinitionId definitionId,
            GridPosition position,
            ActorId ownerId,
            CardinalDirection placementDirection,
            TimeSpan detonatesAt,
            BombDetonationCause scheduledCause)
        {
            Id = id;
            DefinitionId = definitionId;
            Position = position;
            OwnerId = ownerId;
            PlacementDirection = placementDirection;
            DetonatesAt = detonatesAt;
            ScheduledCause = scheduledCause;
        }

        public BombId Id { get; }

        public BombDefinitionId DefinitionId { get; }

        public GridPosition Position { get; }

        public ActorId OwnerId { get; }

        public CardinalDirection PlacementDirection { get; }

        public TimeSpan DetonatesAt { get; }

        public BombDetonationCause ScheduledCause { get; }
    }
}
