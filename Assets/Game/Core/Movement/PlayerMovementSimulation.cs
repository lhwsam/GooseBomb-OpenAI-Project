using System;

namespace BombSwap.Core
{
    public sealed class PlayerMovementSimulation
    {
        private readonly GridState grid;
        private readonly IGameClock clock;
        private TimeSpan nextStepAt;
        private TimeSpan lastObservedTime;
        private bool hasCadence;

        public PlayerMovementSimulation(
            GridState grid,
            IGameClock clock,
            GridPosition startPosition,
            TimeSpan stepInterval)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(clock), clock.Now, "Game time cannot be negative.");
            }
            if (stepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stepInterval),
                    stepInterval,
                    "Movement step interval must be positive.");
            }
            if (!grid.TryAddOccupancy(startPosition, GridOccupancy.Actor))
            {
                throw new InvalidOperationException(
                    $"Player cannot occupy the starting cell {startPosition}.");
            }

            CurrentPosition = startPosition;
            StepInterval = stepInterval;
            lastObservedTime = clock.Now;
        }

        public GridPosition CurrentPosition { get; private set; }

        public CardinalDirection MoveDirection { get; private set; }

        public TimeSpan StepInterval { get; }

        public void SetMoveDirection(CardinalDirection direction)
        {
            ValidateDirection(direction);
            if (MoveDirection == direction)
            {
                return;
            }

            MoveDirection = direction;
            if (direction != CardinalDirection.None && !hasCadence)
            {
                nextStepAt = clock.Now;
                hasCadence = true;
            }
        }

        public bool TryAdvance(out PlayerMovementStep step)
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException("Game clock moved backwards during player movement.");
            }

            lastObservedTime = now;
            step = default;
            if (MoveDirection == CardinalDirection.None || !hasCadence || now < nextStepAt)
            {
                return false;
            }

            nextStepAt = now.Add(StepInterval);
            GridPosition target = GetTarget(CurrentPosition, MoveDirection);
            if (!grid.TryMoveActor(CurrentPosition, target))
            {
                return false;
            }

            step = new PlayerMovementStep(CurrentPosition, target, MoveDirection);
            CurrentPosition = target;
            return true;
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
