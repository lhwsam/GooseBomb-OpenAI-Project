using System;

namespace BombSwap.Core
{
    public sealed class CommittedActorMovement
    {
        private readonly GridState grid;
        private GridPosition from;
        private GridPosition to;
        private TimeSpan startedAt;
        private TimeSpan endsAt;
        private bool crossedCellBoundary;
        private TimeSpan lastObservedAt;
        private TimeSpan lastAdvanceStartedAt;
        private GridPosition lastAdvanceStartedCell;
        private bool hasLastBoundaryCrossing;
        private TimeSpan lastBoundaryCrossedAt;
        private GridPosition lastBoundaryDestination;

        public CommittedActorMovement(
            GridState grid,
            ActorId actorId,
            GridPosition startPosition,
            TimeSpan startedAt)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Movement actor ID must be valid.", nameof(actorId));
            }
            if (startedAt < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(startedAt));
            }
            if (!grid.TryGetActorPosition(actorId, out GridPosition occupied) ||
                occupied != startPosition)
            {
                throw new InvalidOperationException(
                    "Committed movement requires the actor to occupy its starting cell.");
            }

            ActorId = actorId;
            CurrentCell = startPosition;
            Position = GridSubcellPosition.AtCellCenter(startPosition);
            lastObservedAt = startedAt;
            lastAdvanceStartedAt = startedAt;
            lastAdvanceStartedCell = startPosition;
        }

        public ActorId ActorId { get; }
        public GridPosition CurrentCell { get; private set; }
        public GridSubcellPosition Position { get; private set; }
        public bool IsMoving { get; private set; }
        public CardinalDirection Direction { get; private set; }
        public GridPosition From => from;
        public GridPosition To => to;
        public TimeSpan StartedAt => startedAt;
        public TimeSpan EndsAt => endsAt;

        public bool TryStart(
            GridPosition destination,
            CardinalDirection direction,
            TimeSpan startTime,
            TimeSpan duration)
        {
            if (IsMoving)
            {
                throw new InvalidOperationException("Actor already has a committed movement.");
            }
            if (direction == CardinalDirection.None)
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }
            if (startTime < lastObservedAt || startTime == TimeSpan.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(startTime));
            }
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }
            if (!grid.TryReserveActorMove(ActorId, destination))
            {
                return false;
            }

            from = CurrentCell;
            to = destination;
            Direction = direction;
            startedAt = startTime;
            endsAt = AddWithSaturation(startTime, duration);
            crossedCellBoundary = false;
            IsMoving = true;
            return true;
        }

        public bool Advance(TimeSpan now)
        {
            if (now < lastObservedAt)
            {
                throw new InvalidOperationException("Game clock moved backwards during actor movement.");
            }

            lastAdvanceStartedAt = lastObservedAt;
            lastAdvanceStartedCell = CurrentCell;
            hasLastBoundaryCrossing = false;
            lastObservedAt = now;
            if (!IsMoving)
            {
                return false;
            }

            double progress = GetProgress(now);
            Position = Lerp(from, to, progress);
            if (!crossedCellBoundary && progress >= 0.5d)
            {
                if (!grid.TryCommitReservedActorMove(ActorId))
                {
                    throw new InvalidOperationException(
                        "Reserved actor movement could not cross its cell boundary.");
                }
                CurrentCell = to;
                crossedCellBoundary = true;
                hasLastBoundaryCrossing = true;
                lastBoundaryCrossedAt = AddWithSaturation(
                    startedAt,
                    TimeSpan.FromTicks(
                        ((endsAt - startedAt).Ticks / 2) +
                        ((endsAt - startedAt).Ticks % 2)));
                lastBoundaryDestination = to;
            }
            if (progress < 1d)
            {
                return false;
            }
            if (!grid.CompleteActorMove(ActorId))
            {
                throw new InvalidOperationException(
                    "Completed actor movement had no destination reservation.");
            }

            Position = GridSubcellPosition.AtCellCenter(to);
            IsMoving = false;
            return true;
        }

        public GridPosition GetCurrentCellAt(TimeSpan gameTime)
        {
            if (gameTime < lastAdvanceStartedAt || gameTime > lastObservedAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameTime),
                    gameTime,
                    "Cell history is available only for the latest observed interval.");
            }

            if (hasLastBoundaryCrossing && gameTime >= lastBoundaryCrossedAt)
            {
                return lastBoundaryDestination;
            }
            return lastAdvanceStartedCell;
        }

        public void Cancel()
        {
            if (IsMoving && !grid.CompleteActorMove(ActorId))
            {
                throw new InvalidOperationException(
                    "Cancelled actor movement had no destination reservation.");
            }
            IsMoving = false;
            Direction = CardinalDirection.None;
        }

        private double GetProgress(TimeSpan now)
        {
            if (now <= startedAt)
            {
                return 0d;
            }
            if (now >= endsAt)
            {
                return 1d;
            }
            return (now - startedAt).TotalSeconds / (endsAt - startedAt).TotalSeconds;
        }

        private static GridSubcellPosition Lerp(
            GridPosition start,
            GridPosition destination,
            double progress)
        {
            return new GridSubcellPosition(
                start.X + ((destination.X - start.X) * progress),
                start.Z + ((destination.Z - start.Z) * progress));
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan duration)
        {
            long remainingTicks = TimeSpan.MaxValue.Ticks - value.Ticks;
            return duration.Ticks >= remainingTicks ? TimeSpan.MaxValue : value + duration;
        }
    }
}
