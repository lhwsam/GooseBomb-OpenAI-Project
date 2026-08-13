using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class GridState
    {
        private readonly Dictionary<GridPosition, GridCellState> cells =
            new Dictionary<GridPosition, GridCellState>();

        public GridCellState GetCell(GridPosition position)
        {
            return cells.TryGetValue(position, out GridCellState cell)
                ? cell
                : default;
        }

        public bool TrySetTerrain(GridPosition position, GridTerrain terrain)
        {
            ValidateTerrain(terrain);

            GridCellState current = GetCell(position);
            if (current.Occupancy != GridOccupancy.None && terrain != GridTerrain.Floor)
            {
                return false;
            }

            SetOrRemoveCell(position, new GridCellState(terrain, current.Occupancy));
            return true;
        }

        public bool TryAddOccupancy(GridPosition position, GridOccupancy occupancy)
        {
            ValidateSingleOccupancy(occupancy);

            GridCellState current = GetCell(position);
            if (!current.IsWalkableTerrain || (current.Occupancy & occupancy) != 0)
            {
                return false;
            }

            GridOccupancy updated = current.Occupancy | occupancy;
            cells[position] = new GridCellState(current.Terrain, updated);
            return true;
        }

        public bool TryRemoveOccupancy(GridPosition position, GridOccupancy occupancy)
        {
            ValidateSingleOccupancy(occupancy);

            GridCellState current = GetCell(position);
            if ((current.Occupancy & occupancy) == 0)
            {
                return false;
            }

            GridOccupancy updated = current.Occupancy & ~occupancy;
            SetOrRemoveCell(position, new GridCellState(current.Terrain, updated));
            return true;
        }

        public bool TryMoveActor(GridPosition from, GridPosition to)
        {
            long distanceX = Math.Abs((long)to.X - from.X);
            long distanceZ = Math.Abs((long)to.Z - from.Z);
            if (distanceX + distanceZ != 1L)
            {
                throw new ArgumentException(
                    "Actor movement must target one cardinally adjacent cell.",
                    nameof(to));
            }

            GridCellState source = GetCell(from);
            GridCellState destination = GetCell(to);
            if (!source.HasActor ||
                !destination.IsWalkableTerrain ||
                destination.Occupancy != GridOccupancy.None)
            {
                return false;
            }

            SetOrRemoveCell(
                from,
                new GridCellState(source.Terrain, source.Occupancy & ~GridOccupancy.Actor));
            cells[to] = new GridCellState(
                destination.Terrain,
                destination.Occupancy | GridOccupancy.Actor);
            return true;
        }

        private static void ValidateTerrain(GridTerrain terrain)
        {
            if (terrain != GridTerrain.Void &&
                terrain != GridTerrain.Floor &&
                terrain != GridTerrain.IndestructibleWall &&
                terrain != GridTerrain.DestructibleWall)
            {
                throw new ArgumentOutOfRangeException(nameof(terrain), terrain, "Unsupported grid terrain.");
            }
        }

        private static void ValidateSingleOccupancy(GridOccupancy occupancy)
        {
            if (occupancy != GridOccupancy.Actor && occupancy != GridOccupancy.Bomb)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occupancy),
                    occupancy,
                    "Specify exactly one supported occupancy type.");
            }
        }

        private void SetOrRemoveCell(GridPosition position, GridCellState cell)
        {
            if (cell.Terrain == GridTerrain.Void && cell.Occupancy == GridOccupancy.None)
            {
                cells.Remove(position);
                return;
            }

            cells[position] = cell;
        }
    }
}
