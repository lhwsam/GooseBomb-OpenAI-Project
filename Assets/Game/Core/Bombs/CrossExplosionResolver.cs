using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    internal static class CrossExplosionResolver
    {
        private static readonly Direction[] Directions =
        {
            new Direction(1, 0),
            new Direction(-1, 0),
            new Direction(0, 1),
            new Direction(0, -1)
        };

        public static ExplosionResolution Resolve(GridState grid, GridPosition origin, int range)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (grid.GetCell(origin).Terrain != GridTerrain.Floor)
            {
                throw new InvalidOperationException("A bomb explosion origin must remain a floor cell.");
            }

            var affectedCells = new List<GridPosition>();
            var destroyedWalls = new List<GridPosition>();
            affectedCells.Add(origin);

            for (int index = 0; index < Directions.Length; index++)
            {
                AddDirection(grid, origin, range, Directions[index], affectedCells, destroyedWalls);
            }

            return new ExplosionResolution(affectedCells, destroyedWalls);
        }

        private static void AddDirection(
            GridState grid,
            GridPosition origin,
            int range,
            Direction direction,
            List<GridPosition> affectedCells,
            List<GridPosition> destroyedWalls)
        {
            for (int distance = 1; distance <= range; distance++)
            {
                GridPosition position = origin.Offset(
                    direction.DeltaX * distance,
                    direction.DeltaZ * distance);
                GridTerrain terrain = grid.GetCell(position).Terrain;

                if (terrain == GridTerrain.Void || terrain == GridTerrain.IndestructibleWall)
                {
                    return;
                }

                affectedCells.Add(position);

                if (terrain == GridTerrain.DestructibleWall)
                {
                    destroyedWalls.Add(position);
                    return;
                }
            }
        }

        private readonly struct Direction
        {
            public Direction(int deltaX, int deltaZ)
            {
                DeltaX = deltaX;
                DeltaZ = deltaZ;
            }

            public int DeltaX { get; }

            public int DeltaZ { get; }
        }
    }

    internal sealed class ExplosionResolution
    {
        public ExplosionResolution(
            List<GridPosition> affectedCells,
            List<GridPosition> destroyedWalls)
        {
            AffectedCells = affectedCells;
            DestroyedWalls = destroyedWalls;
        }

        public List<GridPosition> AffectedCells { get; }

        public List<GridPosition> DestroyedWalls { get; }
    }
}
