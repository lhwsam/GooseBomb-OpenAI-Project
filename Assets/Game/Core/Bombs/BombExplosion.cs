using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BombSwap.Core
{
    public sealed class BombExplosion
    {
        internal BombExplosion(
            BombId bombId,
            BombDefinitionId definitionId,
            GridPosition origin,
            ActorId ownerId,
            TimeSpan detonatedAt,
            BombDetonationCause cause,
            List<GridPosition> affectedCells,
            List<GridPosition> destroyedWalls)
        {
            BombId = bombId;
            DefinitionId = definitionId;
            Origin = origin;
            OwnerId = ownerId;
            DetonatedAt = detonatedAt;
            Cause = cause;
            AffectedCells = new ReadOnlyCollection<GridPosition>(affectedCells.ToArray());
            DestroyedWalls = new ReadOnlyCollection<GridPosition>(destroyedWalls.ToArray());
        }

        public BombId BombId { get; }

        public BombDefinitionId DefinitionId { get; }

        public GridPosition Origin { get; }

        public ActorId OwnerId { get; }

        public TimeSpan DetonatedAt { get; }

        public BombDetonationCause Cause { get; }

        public IReadOnlyList<GridPosition> AffectedCells { get; }

        public IReadOnlyList<GridPosition> DestroyedWalls { get; }
    }
}
