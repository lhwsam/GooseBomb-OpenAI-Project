using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class PlayerMovementSimulation
    {
        private const double DistanceTolerance = 0.000000001d;

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly List<PlayerMovementStep> lastCellSteps =
            new List<PlayerMovementStep>();
        private TimeSpan lastObservedTime;
        private BombId passThroughBombId;
        private GridPosition passThroughPosition;

        public PlayerMovementSimulation(
            GridState grid,
            IGameClock clock,
            ActorId actorId,
            GridPosition startPosition,
            double cellsPerSecond)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Player actor ID must be valid.", nameof(actorId));
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(clock), clock.Now, "Game time cannot be negative.");
            }
            if (double.IsNaN(cellsPerSecond) ||
                double.IsInfinity(cellsPerSecond) ||
                cellsPerSecond <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellsPerSecond),
                    cellsPerSecond,
                    "Movement speed must be finite and positive.");
            }
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException(
                    $"Player cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            CurrentPosition = startPosition;
            Position = GridSubcellPosition.AtCellCenter(startPosition);
            FacingDirection = CardinalDirection.North;
            CellsPerSecond = cellsPerSecond;
            lastObservedTime = clock.Now;
        }

        public ActorId ActorId { get; }

        public GridPosition CurrentPosition { get; private set; }

        public GridSubcellPosition Position { get; private set; }

        public CardinalDirection MoveDirection { get; private set; }

        public CardinalDirection FacingDirection { get; private set; }

        public double CellsPerSecond { get; }

        public IReadOnlyList<PlayerMovementStep> LastCellSteps => lastCellSteps;

        public bool HasBombPassThrough => passThroughBombId.IsValid;

        public BombId PassThroughBombId => passThroughBombId;

        public void SetMoveDirection(CardinalDirection direction)
        {
            ValidateDirection(direction);
            MoveDirection = direction;
            if (direction != CardinalDirection.None)
            {
                FacingDirection = direction;
            }
        }

        public bool Advance()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException("Game clock moved backwards during player movement.");
            }

            TimeSpan elapsed = now - lastObservedTime;
            lastObservedTime = now;
            lastCellSteps.Clear();
            if (MoveDirection == CardinalDirection.None || elapsed <= TimeSpan.Zero)
            {
                return false;
            }

            double remainingDistance = elapsed.TotalSeconds * CellsPerSecond;
            if (double.IsNaN(remainingDistance) || double.IsInfinity(remainingDistance))
            {
                throw new InvalidOperationException("Player movement distance was not finite.");
            }

            GridSubcellPosition initialPosition = Position;
            while (remainingDistance > 0d)
            {
                GridCellState source = grid.GetCell(CurrentPosition);
                if (HasBombPassThrough && !source.HasBomb)
                {
                    ClearBombPassThrough();
                }
                if (source.HasBomb &&
                    (!HasBombPassThrough || passThroughPosition != CurrentPosition))
                {
                    break;
                }

                GridPosition target = GetTarget(CurrentPosition, MoveDirection);
                GridCellState targetCell = grid.GetCell(target);
                if (!targetCell.IsWalkableTerrain || targetCell.Occupancy != GridOccupancy.None)
                {
                    double distanceToCenter = GetDistanceToCurrentCenter(
                        Position,
                        CurrentPosition,
                        MoveDirection);
                    if (distanceToCenter > 0d)
                    {
                        MoveBy(MoveDirection, Math.Min(remainingDistance, distanceToCenter));
                    }
                    break;
                }

                double distanceToBoundary = GetDistanceToBoundary(
                    Position,
                    CurrentPosition,
                    MoveDirection);
                if (remainingDistance + DistanceTolerance < distanceToBoundary)
                {
                    MoveBy(MoveDirection, remainingDistance);
                    remainingDistance = 0d;
                    continue;
                }

                GridPosition from = CurrentPosition;
                if (!grid.TryMoveActor(ActorId, target))
                {
                    break;
                }

                MoveBy(MoveDirection, distanceToBoundary);
                remainingDistance -= distanceToBoundary;
                CurrentPosition = target;
                lastCellSteps.Add(new PlayerMovementStep(from, target, MoveDirection));
                if (HasBombPassThrough && from == passThroughPosition)
                {
                    ClearBombPassThrough();
                }
            }

            return Position != initialPosition;
        }

        public void GrantBombPassThrough(BombSnapshot bomb)
        {
            if (!bomb.Id.IsValid)
            {
                throw new ArgumentException("Bomb snapshot must identify an active bomb.", nameof(bomb));
            }
            if (bomb.OwnerId != ActorId)
            {
                throw new InvalidOperationException(
                    "Only the bomb owner can receive placement-cell pass-through.");
            }
            if (bomb.Position != CurrentPosition || !grid.GetCell(CurrentPosition).HasBomb)
            {
                throw new InvalidOperationException(
                    "Pass-through can only be granted at the owner's current bomb cell.");
            }
            if (HasBombPassThrough && passThroughBombId != bomb.Id)
            {
                throw new InvalidOperationException(
                    "Player already has pass-through for another active bomb.");
            }

            passThroughBombId = bomb.Id;
            passThroughPosition = bomb.Position;
        }

        public void NotifyBombRemoved(BombId bombId)
        {
            if (!bombId.IsValid)
            {
                throw new ArgumentException("Bomb ID must be valid.", nameof(bombId));
            }
            if (passThroughBombId == bombId)
            {
                ClearBombPassThrough();
            }
        }

        private void MoveBy(CardinalDirection direction, double distance)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    Position = new GridSubcellPosition(Position.X, Position.Z + distance);
                    break;
                case CardinalDirection.East:
                    Position = new GridSubcellPosition(Position.X + distance, Position.Z);
                    break;
                case CardinalDirection.South:
                    Position = new GridSubcellPosition(Position.X, Position.Z - distance);
                    break;
                case CardinalDirection.West:
                    Position = new GridSubcellPosition(Position.X - distance, Position.Z);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Movement requires a non-zero cardinal direction.");
            }
        }

        private static double GetDistanceToBoundary(
            GridSubcellPosition position,
            GridPosition current,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return Math.Max(0d, current.Z + 0.5d - position.Z);
                case CardinalDirection.East:
                    return Math.Max(0d, current.X + 0.5d - position.X);
                case CardinalDirection.South:
                    return Math.Max(0d, position.Z - (current.Z - 0.5d));
                case CardinalDirection.West:
                    return Math.Max(0d, position.X - (current.X - 0.5d));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Movement requires a non-zero cardinal direction.");
            }
        }

        private static double GetDistanceToCurrentCenter(
            GridSubcellPosition position,
            GridPosition current,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return position.Z < current.Z ? current.Z - position.Z : 0d;
                case CardinalDirection.East:
                    return position.X < current.X ? current.X - position.X : 0d;
                case CardinalDirection.South:
                    return position.Z > current.Z ? position.Z - current.Z : 0d;
                case CardinalDirection.West:
                    return position.X > current.X ? position.X - current.X : 0d;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Movement requires a non-zero cardinal direction.");
            }
        }

        private void ClearBombPassThrough()
        {
            passThroughBombId = default;
            passThroughPosition = default;
        }

        private static GridPosition GetTarget(
            GridPosition current,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return current.Offset(0, 1);
                case CardinalDirection.East:
                    return current.Offset(1, 0);
                case CardinalDirection.South:
                    return current.Offset(0, -1);
                case CardinalDirection.West:
                    return current.Offset(-1, 0);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Movement requires a non-zero cardinal direction.");
            }
        }

        private static void ValidateDirection(CardinalDirection direction)
        {
            if (direction < CardinalDirection.None || direction > CardinalDirection.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    "Move direction is not defined.");
            }
        }
    }
}
