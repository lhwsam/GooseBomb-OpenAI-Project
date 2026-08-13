using System;

namespace BombSwap.Core
{
    public sealed class PlayerMovementSimulation
    {
        private readonly GridState grid;
        private readonly IGameClock clock;
        private TimeSpan nextStepAt;
        private TimeSpan lastObservedTime;
        private BombId passThroughBombId;
        private GridPosition passThroughPosition;
        private bool hasCadence;

        public PlayerMovementSimulation(
            GridState grid,
            IGameClock clock,
            ActorId actorId,
            GridPosition startPosition,
            TimeSpan stepInterval)
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
            if (stepInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stepInterval),
                    stepInterval,
                    "Movement step interval must be positive.");
            }
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException(
                    $"Player cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            CurrentPosition = startPosition;
            StepInterval = stepInterval;
            lastObservedTime = clock.Now;
        }

        public ActorId ActorId { get; }

        public GridPosition CurrentPosition { get; private set; }

        public CardinalDirection MoveDirection { get; private set; }

        public TimeSpan StepInterval { get; }

        public bool HasBombPassThrough => passThroughBombId.IsValid;

        public BombId PassThroughBombId => passThroughBombId;

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
            GridCellState source = grid.GetCell(CurrentPosition);
            if (HasBombPassThrough && !source.HasBomb)
            {
                ClearBombPassThrough();
            }
            if (source.HasBomb &&
                (!HasBombPassThrough || passThroughPosition != CurrentPosition))
            {
                return false;
            }

            GridPosition from = CurrentPosition;
            GridPosition target = GetTarget(from, MoveDirection);
            if (!grid.TryMoveActor(ActorId, target))
            {
                return false;
            }

            step = new PlayerMovementStep(from, target, MoveDirection);
            CurrentPosition = target;
            if (HasBombPassThrough && from == passThroughPosition)
            {
                ClearBombPassThrough();
            }

            return true;
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
