using System;

namespace BombSwap.Core
{
    public sealed class ChargerEnemySimulation
    {
        private readonly GridState grid;
        private readonly IGameClock clock;
        private TimeSpan lastObservedTime;
        private TimeSpan stateEndsAt;
        private TimeSpan nextChargeStepAt;

        public ChargerEnemySimulation(
            GridState grid,
            IGameClock clock,
            ChargerEnemyDefinition definition,
            ActorId actorId,
            ActorId targetActorId,
            GridPosition startPosition)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Charger actor ID must be valid.", nameof(actorId));
            }
            if (!targetActorId.IsValid)
            {
                throw new ArgumentException(
                    "Charger target actor ID must be valid.",
                    nameof(targetActorId));
            }
            if (actorId == targetActorId)
            {
                throw new ArgumentException(
                    "Charger actor and target actor must be different.",
                    nameof(targetActorId));
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clock),
                    clock.Now,
                    "Game time cannot be negative.");
            }
            if (!grid.TryGetActorPosition(targetActorId, out _))
            {
                throw new InvalidOperationException("Charger target must already occupy the grid.");
            }
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException(
                    $"Charger cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            TargetActorId = targetActorId;
            CurrentPosition = startPosition;
            State = ChargerEnemyState.Track;
            LockedDirection = CardinalDirection.None;
            lastObservedTime = clock.Now;
        }

        public ChargerEnemyDefinition Definition { get; }

        public ActorId ActorId { get; }

        public ActorId TargetActorId { get; }

        public GridPosition CurrentPosition { get; private set; }

        public ChargerEnemyState State { get; private set; }

        public CardinalDirection LockedDirection { get; private set; }

        public TimeSpan StateEndsAt => stateEndsAt;

        public ChargerEnemyAdvanceResult Advance()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during charger movement.");
            }

            lastObservedTime = now;
            switch (State)
            {
                case ChargerEnemyState.Track:
                    return AdvanceTrack(now);
                case ChargerEnemyState.Telegraph:
                    return AdvanceTelegraph(now);
                case ChargerEnemyState.Charge:
                    return AdvanceCharge(now);
                case ChargerEnemyState.Recover:
                    return AdvanceRecover(now);
                default:
                    throw new InvalidOperationException($"Unsupported charger state: {State}.");
            }
        }

        private ChargerEnemyAdvanceResult AdvanceTrack(TimeSpan now)
        {
            if (!grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition) ||
                !TryGetClearChargeDirection(targetPosition, out CardinalDirection direction))
            {
                return NoActivity();
            }

            ChargerEnemyState previous = State;
            State = ChargerEnemyState.Telegraph;
            LockedDirection = direction;
            stateEndsAt = AddWithSaturation(now, Definition.TelegraphDuration);
            return Transition(previous);
        }

        private ChargerEnemyAdvanceResult AdvanceTelegraph(TimeSpan now)
        {
            if (now < stateEndsAt)
            {
                return NoActivity();
            }

            ChargerEnemyState previous = State;
            State = ChargerEnemyState.Charge;
            nextChargeStepAt = now;
            stateEndsAt = TimeSpan.Zero;
            return Transition(previous);
        }

        private ChargerEnemyAdvanceResult AdvanceCharge(TimeSpan now)
        {
            if (now < nextChargeStepAt)
            {
                return NoActivity();
            }

            GridPosition target = GetTarget(CurrentPosition, LockedDirection);
            if (grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition) &&
                target == targetPosition)
            {
                return BeginRecover(now, true);
            }
            if (!grid.TryMoveActor(ActorId, target))
            {
                return BeginRecover(now, false);
            }

            GridPosition previousPosition = CurrentPosition;
            CurrentPosition = target;
            nextChargeStepAt = AddWithSaturation(now, Definition.ChargeStepInterval);
            var movement = new EnemyMovementStep(
                ActorId,
                previousPosition,
                CurrentPosition,
                LockedDirection);
            return new ChargerEnemyAdvanceResult(
                ActorId,
                State,
                State,
                LockedDirection,
                false,
                movement,
                true,
                false);
        }

        private ChargerEnemyAdvanceResult AdvanceRecover(TimeSpan now)
        {
            if (now < stateEndsAt)
            {
                return NoActivity();
            }

            ChargerEnemyState previous = State;
            State = ChargerEnemyState.Track;
            LockedDirection = CardinalDirection.None;
            stateEndsAt = TimeSpan.Zero;
            return Transition(previous);
        }

        private ChargerEnemyAdvanceResult BeginRecover(TimeSpan now, bool impactedTarget)
        {
            ChargerEnemyState previous = State;
            State = ChargerEnemyState.Recover;
            stateEndsAt = AddWithSaturation(now, Definition.RecoverDuration);
            return new ChargerEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                LockedDirection,
                true,
                default,
                false,
                impactedTarget);
        }

        private bool TryGetClearChargeDirection(
            GridPosition targetPosition,
            out CardinalDirection direction)
        {
            if (targetPosition.X == CurrentPosition.X && targetPosition.Z != CurrentPosition.Z)
            {
                direction = targetPosition.Z > CurrentPosition.Z
                    ? CardinalDirection.North
                    : CardinalDirection.South;
            }
            else if (targetPosition.Z == CurrentPosition.Z &&
                     targetPosition.X != CurrentPosition.X)
            {
                direction = targetPosition.X > CurrentPosition.X
                    ? CardinalDirection.East
                    : CardinalDirection.West;
            }
            else
            {
                direction = CardinalDirection.None;
                return false;
            }

            GridPosition inspected = GetTarget(CurrentPosition, direction);
            while (inspected != targetPosition)
            {
                GridCellState cell = grid.GetCell(inspected);
                if (!cell.IsWalkableTerrain || cell.Occupancy != GridOccupancy.None)
                {
                    direction = CardinalDirection.None;
                    return false;
                }

                inspected = GetTarget(inspected, direction);
            }

            return true;
        }

        private ChargerEnemyAdvanceResult Transition(ChargerEnemyState previous)
        {
            return new ChargerEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                LockedDirection,
                true,
                default,
                false,
                false);
        }

        private ChargerEnemyAdvanceResult NoActivity()
        {
            return new ChargerEnemyAdvanceResult(
                ActorId,
                State,
                State,
                LockedDirection,
                false,
                default,
                false,
                false);
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
                    throw new InvalidOperationException(
                        "Charger movement requires a locked cardinal direction.");
            }
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }
    }
}
