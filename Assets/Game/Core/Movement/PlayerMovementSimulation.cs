using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class PlayerMovementSimulation
    {
        private const double DistanceTolerance = 0.000000001d;
        private const int MaxBoundaryCrossingsPerAdvance = 4096;

        private readonly struct CellBoundaryCrossing
        {
            public CellBoundaryCrossing(TimeSpan crossedAt, GridPosition cell)
            {
                CrossedAt = crossedAt;
                Cell = cell;
            }

            public TimeSpan CrossedAt { get; }
            public GridPosition Cell { get; }
        }

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly List<PlayerMovementStep> lastCellSteps =
            new List<PlayerMovementStep>(4);
        private readonly List<CellBoundaryCrossing> lastBoundaryCrossings =
            new List<CellBoundaryCrossing>(4);
        private TimeSpan lastObservedTime;
        private TimeSpan lastAdvanceStartedAt;
        private GridPosition lastAdvanceStartedCell;
        private BombId passThroughBombId;
        private GridPosition passThroughPosition;
        private CardinalDirection movementDirection;

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
                throw new ArgumentOutOfRangeException(
                    nameof(clock),
                    clock.Now,
                    "Game time cannot be negative.");
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
            lastAdvanceStartedAt = clock.Now;
            lastAdvanceStartedCell = startPosition;
        }

        public ActorId ActorId { get; }
        public GridPosition CurrentPosition { get; private set; }
        public GridSubcellPosition Position { get; private set; }
        public CardinalDirection MoveDirection { get; private set; }
        public CardinalDirection FacingDirection { get; private set; }
        public double CellsPerSecond { get; }
        public bool IsMoving { get; private set; }
        public CardinalDirection CurrentMovementDirection =>
            IsMoving ? movementDirection : CardinalDirection.None;
        public CardinalDirection LastAdvanceDirection { get; private set; }
        public IReadOnlyList<PlayerMovementStep> LastCellSteps => lastCellSteps;
        public bool HasBombPassThrough => passThroughBombId.IsValid;
        public BombId PassThroughBombId => passThroughBombId;

        public void SetMoveDirection(CardinalDirection direction)
        {
            ValidateDirection(direction);
            if (direction == MoveDirection)
            {
                return;
            }

            ReleaseUnusedReservation();
            MoveDirection = direction;
            IsMoving = false;
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
                throw new InvalidOperationException(
                    "Game clock moved backwards during player movement.");
            }

            TimeSpan elapsed = now - lastObservedTime;
            lastCellSteps.Clear();
            lastBoundaryCrossings.Clear();
            lastAdvanceStartedAt = lastObservedTime;
            lastAdvanceStartedCell = CurrentPosition;
            GridSubcellPosition initialPosition = Position;
            LastAdvanceDirection = CardinalDirection.None;

            if (MoveDirection == CardinalDirection.None)
            {
                ReleaseUnusedReservation();
                IsMoving = false;
                lastObservedTime = now;
                return false;
            }

            double remainingDistance = elapsed.TotalSeconds * CellsPerSecond;
            if (double.IsNaN(remainingDistance) || double.IsInfinity(remainingDistance))
            {
                throw new InvalidOperationException("Player movement distance was not finite.");
            }

            CardinalDirection direction = MoveDirection;
            movementDirection = direction;
            double consumedDistance = 0d;
            int boundaryCrossingCount = 0;
            while (true)
            {
                if (!CanLeaveCurrentCell())
                {
                    ReleaseUnusedReservation();
                    IsMoving = false;
                    break;
                }

                GridPosition target = GetTarget(CurrentPosition, direction);
                if (!TryEnsureReservation(target))
                {
                    double distanceToCenter = GetDistanceToCurrentCenter(
                        Position,
                        CurrentPosition,
                        direction);
                    if (remainingDistance <= DistanceTolerance ||
                        distanceToCenter <= DistanceTolerance)
                    {
                        IsMoving = false;
                        break;
                    }

                    double distance = Math.Min(remainingDistance, distanceToCenter);
                    MoveBy(direction, distance);
                    consumedDistance += distance;
                    remainingDistance -= distance;
                    IsMoving = remainingDistance <= DistanceTolerance &&
                        distance + DistanceTolerance < distanceToCenter;
                    break;
                }

                IsMoving = true;
                if (remainingDistance <= DistanceTolerance)
                {
                    break;
                }

                double distanceToBoundary = GetDistanceToBoundary(
                    Position,
                    CurrentPosition,
                    direction);
                if (remainingDistance + DistanceTolerance < distanceToBoundary)
                {
                    MoveBy(direction, remainingDistance);
                    consumedDistance += remainingDistance;
                    remainingDistance = 0d;
                    break;
                }

                if (distanceToBoundary > 0d)
                {
                    MoveBy(direction, distanceToBoundary);
                    consumedDistance += distanceToBoundary;
                    remainingDistance = Math.Max(0d, remainingDistance - distanceToBoundary);
                }

                GridPosition from = CurrentPosition;
                if (!grid.TryCommitReservedActorMove(ActorId))
                {
                    ReleaseUnusedReservation();
                    IsMoving = false;
                    break;
                }

                CurrentPosition = target;
                lastCellSteps.Add(new PlayerMovementStep(from, target, direction));
                lastBoundaryCrossings.Add(new CellBoundaryCrossing(
                    GetCrossingTime(consumedDistance),
                    target));
                if (HasBombPassThrough && from == passThroughPosition)
                {
                    ClearBombPassThrough();
                }
                if (!grid.CompleteActorMove(ActorId))
                {
                    throw new InvalidOperationException(
                        "Committed player movement had no destination reservation.");
                }

                boundaryCrossingCount++;
                if (boundaryCrossingCount >= MaxBoundaryCrossingsPerAdvance)
                {
                    throw new InvalidOperationException(
                        "Player movement exceeded the per-advance cell safety limit.");
                }
            }

            lastObservedTime = now;
            bool positionChanged = Position != initialPosition;
            if (positionChanged)
            {
                LastAdvanceDirection = direction;
            }
            return positionChanged;
        }

        public GridPosition GetCurrentCellAt(TimeSpan gameTime)
        {
            if (gameTime < lastAdvanceStartedAt || gameTime > lastObservedTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameTime),
                    gameTime,
                    "Cell history is available only for the latest observed interval.");
            }

            GridPosition result = lastAdvanceStartedCell;
            for (int index = 0; index < lastBoundaryCrossings.Count; index++)
            {
                CellBoundaryCrossing crossing = lastBoundaryCrossings[index];
                if (gameTime < crossing.CrossedAt)
                {
                    break;
                }
                result = crossing.Cell;
            }
            return result;
        }

        public void CancelMovement()
        {
            ReleaseUnusedReservation();
            IsMoving = false;
            LastAdvanceDirection = CardinalDirection.None;
            MoveDirection = CardinalDirection.None;
        }

        public void ClearMoveIntent()
        {
            ReleaseUnusedReservation();
            IsMoving = false;
            MoveDirection = CardinalDirection.None;
        }

        public void GrantBombPassThrough(BombSnapshot bomb)
        {
            if (!bomb.Id.IsValid)
            {
                throw new ArgumentException(
                    "Bomb snapshot must identify an active bomb.",
                    nameof(bomb));
            }
            if (bomb.OwnerId != ActorId)
            {
                throw new InvalidOperationException(
                    "Only the bomb owner can receive placement-cell pass-through.");
            }
            if (bomb.Position != CurrentPosition ||
                !grid.GetCell(CurrentPosition).HasBomb)
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

        private bool CanLeaveCurrentCell()
        {
            GridCellState source = grid.GetCell(CurrentPosition);
            if (HasBombPassThrough && !source.HasBomb)
            {
                ClearBombPassThrough();
            }

            return !source.HasBomb ||
                (HasBombPassThrough && passThroughPosition == CurrentPosition);
        }

        private bool TryEnsureReservation(GridPosition target)
        {
            if (grid.TryGetActorMoveReservation(ActorId, out GridPosition reserved))
            {
                if (reserved != target)
                {
                    throw new InvalidOperationException(
                        "Player movement reservation does not match the active direction.");
                }
                return true;
            }

            return grid.TryReserveActorMove(ActorId, target);
        }

        private void ReleaseUnusedReservation()
        {
            if (!grid.TryGetActorMoveReservation(ActorId, out _))
            {
                return;
            }
            if (!grid.CompleteActorMove(ActorId))
            {
                throw new InvalidOperationException(
                    "Player movement reservation could not be released.");
            }
        }

        private TimeSpan GetCrossingTime(double consumedDistance)
        {
            double seconds = consumedDistance / CellsPerSecond;
            TimeSpan offset = TimeSpan.FromSeconds(seconds);
            return AddWithSaturation(lastAdvanceStartedAt, offset);
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

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan delta)
        {
            long remainingTicks = TimeSpan.MaxValue.Ticks - value.Ticks;
            return delta.Ticks >= remainingTicks ? TimeSpan.MaxValue : value + delta;
        }

        private static void ValidateDirection(CardinalDirection direction)
        {
            if (direction < CardinalDirection.None ||
                direction > CardinalDirection.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    "Move direction is not defined.");
            }
        }
    }
}
