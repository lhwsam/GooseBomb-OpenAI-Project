using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    internal static class ForwardLineExplosionResolver
    {
        public static ExplosionResolution Resolve(
            GridState grid,
            GridPosition origin,
            int range,
            CardinalDirection direction)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }
            if (range < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(range));
            }
            if (direction < CardinalDirection.North ||
                direction > CardinalDirection.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    "Forward-line explosions require a cardinal direction.");
            }
            if (grid.GetCell(origin).Terrain != GridTerrain.Floor)
            {
                throw new InvalidOperationException(
                    "A bomb explosion origin must remain a floor cell.");
            }

            GetDelta(direction, out int deltaX, out int deltaZ);
            var affectedCells = new List<GridPosition> { origin };
            var destroyedWalls = new List<GridPosition>();
            for (int distance = 1; distance <= range; distance++)
            {
                GridPosition position = origin.Offset(
                    deltaX * distance,
                    deltaZ * distance);
                GridTerrain terrain = grid.GetCell(position).Terrain;
                if (terrain == GridTerrain.Void ||
                    terrain == GridTerrain.IndestructibleWall)
                {
                    break;
                }

                affectedCells.Add(position);
                if (terrain == GridTerrain.DestructibleWall)
                {
                    destroyedWalls.Add(position);
                    break;
                }
            }

            return new ExplosionResolution(affectedCells, destroyedWalls);
        }

        private static void GetDelta(
            CardinalDirection direction,
            out int deltaX,
            out int deltaZ)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    deltaX = 0;
                    deltaZ = 1;
                    return;
                case CardinalDirection.East:
                    deltaX = 1;
                    deltaZ = 0;
                    return;
                case CardinalDirection.South:
                    deltaX = 0;
                    deltaZ = -1;
                    return;
                case CardinalDirection.West:
                    deltaX = -1;
                    deltaZ = 0;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
