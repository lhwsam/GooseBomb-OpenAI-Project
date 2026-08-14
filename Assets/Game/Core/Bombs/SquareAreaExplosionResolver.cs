using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    internal static class SquareAreaExplosionResolver
    {
        public static ExplosionResolution Resolve(
            GridState grid,
            GridPosition origin,
            int radius)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }
            if (grid.GetCell(origin).Terrain != GridTerrain.Floor)
            {
                throw new InvalidOperationException(
                    "A bomb explosion origin must remain a floor cell.");
            }

            var affectedCells = new List<GridPosition> { origin };
            var destroyedWalls = new List<GridPosition>();

            for (int deltaZ = -radius; deltaZ <= radius; deltaZ++)
            {
                for (int deltaX = -radius; deltaX <= radius; deltaX++)
                {
                    if (deltaX == 0 && deltaZ == 0)
                    {
                        continue;
                    }

                    GridPosition position = origin.Offset(deltaX, deltaZ);
                    GridTerrain terrain = grid.GetCell(position).Terrain;
                    if (terrain == GridTerrain.Void ||
                        terrain == GridTerrain.IndestructibleWall)
                    {
                        continue;
                    }

                    affectedCells.Add(position);
                    if (terrain == GridTerrain.DestructibleWall)
                    {
                        destroyedWalls.Add(position);
                    }
                }
            }

            return new ExplosionResolution(affectedCells, destroyedWalls);
        }
    }
}
