using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class GridState
    {
        private readonly Dictionary<GridPosition, GridCellState> cells =
            new Dictionary<GridPosition, GridCellState>();
        private readonly Dictionary<ActorId, GridPosition> actorPositions =
            new Dictionary<ActorId, GridPosition>();
        private readonly Dictionary<GridPosition, ActorId> actorIdsByPosition =
            new Dictionary<GridPosition, ActorId>();

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

        public bool TryAddActor(ActorId actorId, GridPosition position)
        {
            ValidateActorId(actorId);

            GridCellState current = GetCell(position);
            if (actorPositions.ContainsKey(actorId) ||
                !current.IsWalkableTerrain ||
                current.Occupancy != GridOccupancy.None)
            {
                return false;
            }

            cells[position] = new GridCellState(
                current.Terrain,
                current.Occupancy | GridOccupancy.Actor);
            actorPositions.Add(actorId, position);
            actorIdsByPosition.Add(position, actorId);
            return true;
        }

        public bool TryRemoveActor(ActorId actorId)
        {
            ValidateActorId(actorId);
            if (!actorPositions.TryGetValue(actorId, out GridPosition position))
            {
                return false;
            }

            GridCellState current = GetCell(position);
            if (!current.HasActor ||
                !actorIdsByPosition.TryGetValue(position, out ActorId storedActorId) ||
                storedActorId != actorId)
            {
                throw new InvalidOperationException("Grid actor identity is inconsistent.");
            }

            actorPositions.Remove(actorId);
            actorIdsByPosition.Remove(position);
            SetOrRemoveCell(
                position,
                new GridCellState(
                    current.Terrain,
                    current.Occupancy & ~GridOccupancy.Actor));
            return true;
        }

        public bool TryGetActorPosition(ActorId actorId, out GridPosition position)
        {
            ValidateActorId(actorId);
            return actorPositions.TryGetValue(actorId, out position);
        }

        public bool TryAddBomb(GridPosition position)
        {
            GridCellState current = GetCell(position);
            if (!current.IsWalkableTerrain || current.HasBomb)
            {
                return false;
            }

            cells[position] = new GridCellState(
                current.Terrain,
                current.Occupancy | GridOccupancy.Bomb);
            return true;
        }

        public bool TryRemoveBomb(GridPosition position)
        {
            GridCellState current = GetCell(position);
            if (!current.HasBomb)
            {
                return false;
            }

            SetOrRemoveCell(
                position,
                new GridCellState(
                    current.Terrain,
                    current.Occupancy & ~GridOccupancy.Bomb));
            return true;
        }

        public bool TryMoveActor(ActorId actorId, GridPosition to)
        {
            ValidateActorId(actorId);
            if (!actorPositions.TryGetValue(actorId, out GridPosition from))
            {
                return false;
            }

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
                !actorIdsByPosition.TryGetValue(from, out ActorId storedActorId) ||
                storedActorId != actorId)
            {
                throw new InvalidOperationException("Grid actor identity is inconsistent.");
            }
            if (!destination.IsWalkableTerrain ||
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
            actorPositions[actorId] = to;
            actorIdsByPosition.Remove(from);
            actorIdsByPosition.Add(to, actorId);
            return true;
        }

        private static void ValidateActorId(ActorId actorId)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Actor ID must be valid.", nameof(actorId));
            }
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
