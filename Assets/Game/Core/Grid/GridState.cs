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
        private readonly Dictionary<ActorId, GridPosition> actorMoveReservations =
            new Dictionary<ActorId, GridPosition>();
        private readonly Dictionary<GridPosition, ActorId> reservedActorIdsByPosition =
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
            if ((current.Occupancy != GridOccupancy.None ||
                    reservedActorIdsByPosition.ContainsKey(position)) &&
                terrain != GridTerrain.Floor)
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
                current.Occupancy != GridOccupancy.None ||
                reservedActorIdsByPosition.ContainsKey(position))
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
            ReleaseActorMoveReservation(actorId);
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
            if (!current.IsWalkableTerrain || current.HasBomb ||
                (reservedActorIdsByPosition.ContainsKey(position) && !current.HasActor))
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
            return TryMoveActor(actorId, to, false, false);
        }

        public bool TryMoveActorAllowingBombOverlap(
            ActorId actorId,
            GridPosition to)
        {
            return TryMoveActor(actorId, to, true, false);
        }

        private bool TryMoveActor(
            ActorId actorId,
            GridPosition to,
            bool allowBombOverlap,
            bool isReservationCommit)
        {
            ValidateActorId(actorId);
            if (!actorPositions.TryGetValue(actorId, out GridPosition from))
            {
                return false;
            }
            if (actorMoveReservations.TryGetValue(actorId, out GridPosition reservedTo) &&
                (!isReservationCommit || reservedTo != to))
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
                destination.HasActor ||
                IsReservedByAnotherActor(actorId, to) ||
                (!allowBombOverlap && destination.HasBomb))
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

        public bool TryReserveActorMove(ActorId actorId, GridPosition to)
        {
            ValidateActorId(actorId);
            if (!actorPositions.TryGetValue(actorId, out GridPosition from))
            {
                return false;
            }
            if (actorMoveReservations.ContainsKey(actorId))
            {
                throw new InvalidOperationException("Actor already has a movement reservation.");
            }

            long distanceX = Math.Abs((long)to.X - from.X);
            long distanceZ = Math.Abs((long)to.Z - from.Z);
            if (distanceX + distanceZ != 1L)
            {
                throw new ArgumentException(
                    "Actor movement must reserve one cardinally adjacent cell.",
                    nameof(to));
            }

            GridCellState destination = GetCell(to);
            if (!destination.IsWalkableTerrain ||
                destination.Occupancy != GridOccupancy.None ||
                reservedActorIdsByPosition.ContainsKey(to))
            {
                return false;
            }

            actorMoveReservations.Add(actorId, to);
            reservedActorIdsByPosition.Add(to, actorId);
            return true;
        }

        public bool TryCommitReservedActorMove(ActorId actorId)
        {
            ValidateActorId(actorId);
            if (!actorMoveReservations.TryGetValue(actorId, out GridPosition to))
            {
                return false;
            }

            return TryMoveActor(actorId, to, false, true);
        }

        public bool CompleteActorMove(ActorId actorId)
        {
            ValidateActorId(actorId);
            return ReleaseActorMoveReservation(actorId);
        }

        public bool TryGetActorMoveReservation(ActorId actorId, out GridPosition position)
        {
            ValidateActorId(actorId);
            return actorMoveReservations.TryGetValue(actorId, out position);
        }

        public bool IsCellReservedForActorMove(GridPosition position)
        {
            return reservedActorIdsByPosition.ContainsKey(position);
        }

        private bool IsReservedByAnotherActor(ActorId actorId, GridPosition position)
        {
            return reservedActorIdsByPosition.TryGetValue(position, out ActorId reservedActorId) &&
                reservedActorId != actorId;
        }

        private bool ReleaseActorMoveReservation(ActorId actorId)
        {
            if (!actorMoveReservations.TryGetValue(actorId, out GridPosition position))
            {
                return false;
            }

            actorMoveReservations.Remove(actorId);
            if (!reservedActorIdsByPosition.Remove(position))
            {
                throw new InvalidOperationException("Grid movement reservation is inconsistent.");
            }
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
