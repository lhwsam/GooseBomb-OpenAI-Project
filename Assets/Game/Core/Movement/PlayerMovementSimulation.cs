using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class PlayerMovementSimulation
    {
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
        private readonly List<PlayerMovementStep> lastCellSteps = new List<PlayerMovementStep>(1);
        private readonly List<CellBoundaryCrossing> lastBoundaryCrossings =
            new List<CellBoundaryCrossing>(4);
        private TimeSpan lastObservedTime;
        private TimeSpan lastAdvanceStartedAt;
        private GridPosition lastAdvanceStartedCell;
        private BombId passThroughBombId;
        private GridPosition passThroughPosition;
        private GridPosition movementFrom;
        private GridPosition movementTo;
        private CardinalDirection movementDirection;
        private TimeSpan movementStartedAt;
        private TimeSpan movementEndsAt;
        private bool hasCommittedDestination;
        private bool hasPendingDirectionIntent;
        private bool releaseAfterPendingIntent;

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
            if (double.IsNaN(cellsPerSecond) || double.IsInfinity(cellsPerSecond) || cellsPerSecond <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellsPerSecond),
                    cellsPerSecond,
                    "Movement speed must be finite and positive.");
            }
            double stepSeconds = 1d / cellsPerSecond;
            if (stepSeconds < TimeSpan.FromTicks(1).TotalSeconds ||
                stepSeconds > TimeSpan.MaxValue.TotalSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellsPerSecond),
                    cellsPerSecond,
                    "Movement speed must produce a representable positive step duration.");
            }
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException($"Player cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            CurrentPosition = startPosition;
            Position = GridSubcellPosition.AtCellCenter(startPosition);
            FacingDirection = CardinalDirection.North;
            CellsPerSecond = cellsPerSecond;
            StepDuration = TimeSpan.FromSeconds(stepSeconds);
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
        public TimeSpan StepDuration { get; }
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
            if (direction == CardinalDirection.None &&
                !IsMoving &&
                hasPendingDirectionIntent)
            {
                releaseAfterPendingIntent = true;
                return;
            }

            MoveDirection = direction;
            if (direction != CardinalDirection.None)
            {
                if (!IsMoving)
                {
                    FacingDirection = direction;
                    hasPendingDirectionIntent = true;
                    releaseAfterPendingIntent = false;
                }
            }
        }

        public bool Advance()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException("Game clock moved backwards during player movement.");
            }

            lastCellSteps.Clear();
            lastBoundaryCrossings.Clear();
            lastAdvanceStartedAt = lastObservedTime;
            lastAdvanceStartedCell = CurrentPosition;
            GridSubcellPosition initialPosition = Position;
            TimeSpan cursor = lastObservedTime;
            int completedStepCount = 0;
            while (true)
            {
                if (!IsMoving)
                {
                    if (MoveDirection == CardinalDirection.None)
                    {
                        if (completedStepCount == 0)
                        {
                            LastAdvanceDirection = CardinalDirection.None;
                        }
                        break;
                    }

                    bool beganMove = TryBeginMove(MoveDirection, cursor);
                    if (hasPendingDirectionIntent)
                    {
                        hasPendingDirectionIntent = false;
                        if (releaseAfterPendingIntent)
                        {
                            MoveDirection = CardinalDirection.None;
                        }
                        releaseAfterPendingIntent = false;
                    }
                    if (!beganMove)
                    {
                        if (completedStepCount == 0)
                        {
                            LastAdvanceDirection = CardinalDirection.None;
                        }
                        break;
                    }
                }

                LastAdvanceDirection = movementDirection;
                if (now < movementEndsAt)
                {
                    UpdateCommittedMove(now);
                    break;
                }

                cursor = movementEndsAt;
                UpdateCommittedMove(cursor);
                completedStepCount++;
                if (completedStepCount >= 4096)
                {
                    throw new InvalidOperationException(
                        "Player movement exceeded the per-advance cell safety limit.");
                }
            }

            lastObservedTime = now;
            return Position != initialPosition;
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
            if (IsMoving && !grid.CompleteActorMove(ActorId))
            {
                throw new InvalidOperationException(
                    "Cancelled player movement had no destination reservation.");
            }

            IsMoving = false;
            LastAdvanceDirection = CardinalDirection.None;
            MoveDirection = CardinalDirection.None;
            hasPendingDirectionIntent = false;
            releaseAfterPendingIntent = false;
        }

        public void ClearMoveIntent()
        {
            MoveDirection = CardinalDirection.None;
            hasPendingDirectionIntent = false;
            releaseAfterPendingIntent = false;
        }

        public void GrantBombPassThrough(BombSnapshot bomb)
        {
            if (!bomb.Id.IsValid)
            {
                throw new ArgumentException("Bomb snapshot must identify an active bomb.", nameof(bomb));
            }
            if (bomb.OwnerId != ActorId)
            {
                throw new InvalidOperationException("Only the bomb owner can receive placement-cell pass-through.");
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

        private bool TryBeginMove(CardinalDirection direction, TimeSpan startedAt)
        {
            GridCellState source = grid.GetCell(CurrentPosition);
            if (HasBombPassThrough && !source.HasBomb)
            {
                ClearBombPassThrough();
            }
            if (source.HasBomb && (!HasBombPassThrough || passThroughPosition != CurrentPosition))
            {
                return false;
            }

            GridPosition target = GetTarget(CurrentPosition, direction);
            if (!grid.TryReserveActorMove(ActorId, target))
            {
                return false;
            }

            movementFrom = CurrentPosition;
            movementTo = target;
            movementDirection = direction;
            FacingDirection = direction;
            movementStartedAt = startedAt;
            movementEndsAt = AddWithSaturation(startedAt, StepDuration);
            hasCommittedDestination = false;
            IsMoving = true;
            return true;
        }

        private void UpdateCommittedMove(TimeSpan now)
        {
            double progress = GetMovementProgress(now);
            Position = Lerp(movementFrom, movementTo, progress);
            if (!hasCommittedDestination && progress >= 0.5d)
            {
                if (!grid.TryCommitReservedActorMove(ActorId))
                {
                    throw new InvalidOperationException(
                        "Reserved player movement could not cross its cell boundary.");
                }

                CurrentPosition = movementTo;
                hasCommittedDestination = true;
                lastBoundaryCrossings.Add(new CellBoundaryCrossing(
                    AddWithSaturation(
                        movementStartedAt,
                        TimeSpan.FromTicks(
                            (StepDuration.Ticks / 2) + (StepDuration.Ticks % 2))),
                    movementTo));
                if (HasBombPassThrough && movementFrom == passThroughPosition)
                {
                    ClearBombPassThrough();
                }
            }
            if (progress < 1d)
            {
                return;
            }
            if (!hasCommittedDestination)
            {
                throw new InvalidOperationException(
                    "Completed player movement never committed its destination cell.");
            }
            if (!grid.CompleteActorMove(ActorId))
            {
                throw new InvalidOperationException(
                    "Completed player movement had no destination reservation.");
            }

            Position = GridSubcellPosition.AtCellCenter(movementTo);
            lastCellSteps.Add(new PlayerMovementStep(movementFrom, movementTo, movementDirection));
            IsMoving = false;
        }

        private double GetMovementProgress(TimeSpan now)
        {
            if (now <= movementStartedAt)
            {
                return 0d;
            }
            if (now >= movementEndsAt)
            {
                return 1d;
            }

            return (now - movementStartedAt).TotalSeconds /
                (movementEndsAt - movementStartedAt).TotalSeconds;
        }

        private static GridSubcellPosition Lerp(GridPosition from, GridPosition to, double progress)
        {
            return new GridSubcellPosition(
                from.X + ((to.X - from.X) * progress),
                from.Z + ((to.Z - from.Z) * progress));
        }

        private void ClearBombPassThrough()
        {
            passThroughBombId = default;
            passThroughPosition = default;
        }

        private static GridPosition GetTarget(GridPosition current, CardinalDirection direction)
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
