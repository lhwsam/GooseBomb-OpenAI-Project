using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class BombSimulation
    {
        private static readonly IReadOnlyList<BombExplosion> NoExplosions =
            Array.Empty<BombExplosion>();
        private static readonly IReadOnlyList<GridPosition> NoAffectedCells =
            Array.Empty<GridPosition>();

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly TimeSpan chainDelay;
        private readonly Dictionary<BombId, ActiveBomb> bombsById =
            new Dictionary<BombId, ActiveBomb>();
        private readonly Dictionary<GridPosition, BombId> bombsByPosition =
            new Dictionary<GridPosition, BombId>();
        private readonly List<ActiveBomb> dueBombs = new List<ActiveBomb>();
        private readonly HashSet<GridPosition> wallsToDestroy = new HashSet<GridPosition>();
        private long nextBombSequence = 1;

        public BombSimulation(GridState grid, IGameClock clock, TimeSpan chainDelay)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (chainDelay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chainDelay),
                    chainDelay,
                    "Chain delay must be greater than zero.");
            }

            this.chainDelay = chainDelay;
        }

        public int ActiveBombCount => bombsById.Count;

        public TimeSpan ChainDelay => chainDelay;

        public bool TryPlaceBomb(
            BombDefinition definition,
            GridPosition position,
            ActorId ownerId,
            out BombId bombId)
        {
            return TryPlaceBomb(
                definition,
                position,
                ownerId,
                CardinalDirection.None,
                out bombId);
        }

        public bool TryPlaceBomb(
            BombDefinition definition,
            GridPosition position,
            ActorId ownerId,
            CardinalDirection placementDirection,
            out BombId bombId)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (!ownerId.IsValid)
            {
                throw new ArgumentException("Bomb owner ID must be valid.", nameof(ownerId));
            }
            if (placementDirection < CardinalDirection.None ||
                placementDirection > CardinalDirection.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementDirection),
                    placementDirection,
                    "Bomb placement direction is not defined.");
            }
            if (definition.ExplosionShape == BombExplosionShape.ForwardLine &&
                placementDirection == CardinalDirection.None)
            {
                throw new ArgumentException(
                    "A forward-line bomb requires a cardinal placement direction.",
                    nameof(placementDirection));
            }

            bombId = default;

            if (nextBombSequence == long.MaxValue)
            {
                throw new InvalidOperationException("Bomb ID sequence is exhausted.");
            }

            TimeSpan detonatesAt = clock.Now.Add(definition.FuseDuration);

            if (bombsByPosition.ContainsKey(position) ||
                !grid.TryAddBomb(position))
            {
                return false;
            }

            bombId = new BombId(nextBombSequence);
            nextBombSequence++;

            var bomb = new ActiveBomb(
                bombId,
                definition,
                position,
                ownerId,
                placementDirection,
                detonatesAt);
            bombsById.Add(bombId, bomb);
            bombsByPosition.Add(position, bombId);
            return true;
        }

        public bool TryGetBomb(BombId bombId, out BombSnapshot snapshot)
        {
            if (bombsById.TryGetValue(bombId, out ActiveBomb bomb))
            {
                snapshot = bomb.CreateSnapshot();
                return true;
            }

            snapshot = default;
            return false;
        }

        public bool TryGetExplosionPreview(
            BombId bombId,
            out IReadOnlyList<GridPosition> affectedCells)
        {
            if (!bombsById.TryGetValue(bombId, out ActiveBomb bomb))
            {
                affectedCells = NoAffectedCells;
                return false;
            }

            affectedCells = ResolveExplosion(bomb).AffectedCells.AsReadOnly();
            return true;
        }

        public IReadOnlyList<BombExplosion> ProcessDueBombs()
        {
            TimeSpan now = clock.Now;
            List<BombExplosion> explosions = null;

            while (CollectNextDueGroup(now, out TimeSpan detonationTime))
            {
                wallsToDestroy.Clear();

                for (int index = 0; index < dueBombs.Count; index++)
                {
                    RemoveActiveBomb(dueBombs[index]);
                }

                TimeSpan chainDetonationTime = AddWithSaturation(detonationTime, chainDelay);

                for (int index = 0; index < dueBombs.Count; index++)
                {
                    ActiveBomb bomb = dueBombs[index];
                    ExplosionResolution resolution = ResolveExplosion(bomb);

                    ScheduleChainedBombs(resolution.AffectedCells, chainDetonationTime);

                    for (int wallIndex = 0; wallIndex < resolution.DestroyedWalls.Count; wallIndex++)
                    {
                        wallsToDestroy.Add(resolution.DestroyedWalls[wallIndex]);
                    }

                    if (explosions == null)
                    {
                        explosions = new List<BombExplosion>();
                    }

                    explosions.Add(new BombExplosion(
                        bomb.Id,
                        bomb.Definition.Id,
                        bomb.Position,
                        bomb.OwnerId,
                        bomb.PlacementDirection,
                        detonationTime,
                        bomb.ScheduledCause,
                        resolution.AffectedCells,
                        resolution.DestroyedWalls));
                }

                DestroyPendingWalls();
            }

            dueBombs.Clear();
            wallsToDestroy.Clear();
            return explosions ?? NoExplosions;
        }

        private bool CollectNextDueGroup(TimeSpan now, out TimeSpan detonationTime)
        {
            dueBombs.Clear();
            detonationTime = default;
            bool found = false;

            foreach (KeyValuePair<BombId, ActiveBomb> pair in bombsById)
            {
                ActiveBomb bomb = pair.Value;
                if (bomb.DetonatesAt > now)
                {
                    continue;
                }

                if (!found || bomb.DetonatesAt < detonationTime)
                {
                    found = true;
                    detonationTime = bomb.DetonatesAt;
                    dueBombs.Clear();
                    dueBombs.Add(bomb);
                }
                else if (bomb.DetonatesAt == detonationTime)
                {
                    dueBombs.Add(bomb);
                }
            }

            if (found)
            {
                dueBombs.Sort(ActiveBombIdComparer.Instance);
            }

            return found;
        }

        private ExplosionResolution ResolveExplosion(ActiveBomb bomb)
        {
            if (bomb.Definition.ExplosionShape == BombExplosionShape.Cross)
            {
                return CrossExplosionResolver.Resolve(grid, bomb.Position, bomb.Definition.Range);
            }
            if (bomb.Definition.ExplosionShape == BombExplosionShape.SquareArea)
            {
                return SquareAreaExplosionResolver.Resolve(
                    grid,
                    bomb.Position,
                    bomb.Definition.Range);
            }
            if (bomb.Definition.ExplosionShape == BombExplosionShape.ForwardLine)
            {
                return ForwardLineExplosionResolver.Resolve(
                    grid,
                    bomb.Position,
                    bomb.Definition.Range,
                    bomb.PlacementDirection);
            }

            throw new InvalidOperationException("The active bomb has an unsupported explosion shape.");
        }

        private void RemoveActiveBomb(ActiveBomb bomb)
        {
            if (!grid.TryRemoveBomb(bomb.Position))
            {
                throw new InvalidOperationException("Grid bomb occupancy is inconsistent with BombSimulation.");
            }

            bombsByPosition.Remove(bomb.Position);
            bombsById.Remove(bomb.Id);
        }

        private void ScheduleChainedBombs(
            List<GridPosition> affectedCells,
            TimeSpan chainDetonationTime)
        {
            for (int index = 0; index < affectedCells.Count; index++)
            {
                if (!bombsByPosition.TryGetValue(affectedCells[index], out BombId bombId) ||
                    !bombsById.TryGetValue(bombId, out ActiveBomb bomb) ||
                    chainDetonationTime >= bomb.DetonatesAt)
                {
                    continue;
                }

                bomb.DetonatesAt = chainDetonationTime;
                bomb.ScheduledCause = BombDetonationCause.Chain;
            }
        }

        private void DestroyPendingWalls()
        {
            foreach (GridPosition position in wallsToDestroy)
            {
                if (!grid.TrySetTerrain(position, GridTerrain.Floor))
                {
                    throw new InvalidOperationException("A scheduled destructible wall could not be destroyed.");
                }
            }
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }

        private sealed class ActiveBomb
        {
            public ActiveBomb(
                BombId id,
                BombDefinition definition,
                GridPosition position,
                ActorId ownerId,
                CardinalDirection placementDirection,
                TimeSpan detonatesAt)
            {
                Id = id;
                Definition = definition;
                Position = position;
                OwnerId = ownerId;
                PlacementDirection = placementDirection;
                DetonatesAt = detonatesAt;
                ScheduledCause = BombDetonationCause.Fuse;
            }

            public BombId Id { get; }

            public BombDefinition Definition { get; }

            public GridPosition Position { get; }

            public ActorId OwnerId { get; }

            public CardinalDirection PlacementDirection { get; }

            public TimeSpan DetonatesAt { get; set; }

            public BombDetonationCause ScheduledCause { get; set; }

            public BombSnapshot CreateSnapshot()
            {
                return new BombSnapshot(
                    Id,
                    Definition.Id,
                    Position,
                    OwnerId,
                    PlacementDirection,
                    DetonatesAt,
                    ScheduledCause);
            }
        }

        private sealed class ActiveBombIdComparer : IComparer<ActiveBomb>
        {
            public static readonly ActiveBombIdComparer Instance = new ActiveBombIdComparer();

            public int Compare(ActiveBomb left, ActiveBomb right)
            {
                return left.Id.CompareTo(right.Id);
            }
        }
    }
}
