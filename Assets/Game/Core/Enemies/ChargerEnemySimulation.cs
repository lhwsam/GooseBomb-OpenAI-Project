using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class ChargerEnemySimulation
    {
        private static readonly CardinalDirection[] DirectionPriority =
        {
            CardinalDirection.North,
            CardinalDirection.East,
            CardinalDirection.South,
            CardinalDirection.West,
        };

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly Dictionary<GridPosition, GridPosition> acquisitionParents =
            new Dictionary<GridPosition, GridPosition>();
        private readonly HashSet<GridPosition> acquisitionVisited =
            new HashSet<GridPosition>();
        private readonly Queue<GridPosition> acquisitionFrontier =
            new Queue<GridPosition>();
        private TimeSpan lastObservedTime;
        private TimeSpan stateEndsAt;
        private TimeSpan nextLaneAcquireStepAt;
        private TimeSpan nextChargeStepAt;
        private int lockedChargeDistance;
        private int remainingChargeSteps;

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
            nextLaneAcquireStepAt = clock.Now;
            lastObservedTime = clock.Now;
        }

        public ChargerEnemyDefinition Definition { get; }

        public ActorId ActorId { get; }

        public ActorId TargetActorId { get; }

        public GridPosition CurrentPosition { get; private set; }

        public ChargerEnemyState State { get; private set; }

        public CardinalDirection LockedDirection { get; private set; }

        public int LockedChargeDistance => lockedChargeDistance;

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
            if (now < nextLaneAcquireStepAt)
            {
                return NoActivity();
            }

            nextLaneAcquireStepAt = AddWithSaturation(
                now,
                Definition.LaneAcquireStepInterval);
            if (!grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition))
            {
                return NoActivity();
            }
            if (TryGetClearChargeDirection(
                    CurrentPosition,
                    targetPosition,
                    out CardinalDirection chargeDirection))
            {
                return BeginTelegraph(now, targetPosition, chargeDirection);
            }
            if (!TryGetLaneAcquisitionStep(targetPosition, out CardinalDirection moveDirection))
            {
                return NoActivity();
            }

            GridPosition previousPosition = CurrentPosition;
            GridPosition destination = GetTarget(CurrentPosition, moveDirection);
            if (!grid.TryMoveActor(ActorId, destination))
            {
                return NoActivity();
            }

            CurrentPosition = destination;
            var movement = new EnemyMovementStep(
                ActorId,
                previousPosition,
                CurrentPosition,
                moveDirection);
            return new ChargerEnemyAdvanceResult(
                ActorId,
                State,
                State,
                LockedDirection,
                lockedChargeDistance,
                false,
                movement,
                true,
                false);
        }

        private ChargerEnemyAdvanceResult BeginTelegraph(
            TimeSpan now,
            GridPosition targetPosition,
            CardinalDirection direction)
        {
            ChargerEnemyState previous = State;
            State = ChargerEnemyState.Telegraph;
            LockedDirection = direction;
            lockedChargeDistance = CountChargeDistance(direction, targetPosition);
            remainingChargeSteps = lockedChargeDistance;
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
            if (remainingChargeSteps <= 0)
            {
                return BeginRecover(now, false);
            }
            if (!grid.TryMoveActor(ActorId, target))
            {
                return BeginRecover(now, false);
            }

            GridPosition previousPosition = CurrentPosition;
            CurrentPosition = target;
            remainingChargeSteps--;
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
                lockedChargeDistance,
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
            lockedChargeDistance = 0;
            remainingChargeSteps = 0;
            nextLaneAcquireStepAt = now;
            stateEndsAt = TimeSpan.Zero;
            return Transition(previous);
        }

        private ChargerEnemyAdvanceResult BeginRecover(TimeSpan now, bool impactedTarget)
        {
            ChargerEnemyState previous = State;
            State = ChargerEnemyState.Recover;
            lockedChargeDistance = 0;
            remainingChargeSteps = 0;
            stateEndsAt = AddWithSaturation(now, Definition.RecoverDuration);
            return new ChargerEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                LockedDirection,
                lockedChargeDistance,
                true,
                default,
                false,
                impactedTarget);
        }

        private bool TryGetClearChargeDirection(
            GridPosition origin,
            GridPosition targetPosition,
            out CardinalDirection direction)
        {
            if (targetPosition.X == origin.X && targetPosition.Z != origin.Z)
            {
                direction = targetPosition.Z > origin.Z
                    ? CardinalDirection.North
                    : CardinalDirection.South;
            }
            else if (targetPosition.Z == origin.Z && targetPosition.X != origin.X)
            {
                direction = targetPosition.X > origin.X
                    ? CardinalDirection.East
                    : CardinalDirection.West;
            }
            else
            {
                direction = CardinalDirection.None;
                return false;
            }

            GridPosition inspected = GetTarget(origin, direction);
            while (inspected != targetPosition)
            {
                GridCellState cell = grid.GetCell(inspected);
                if (!cell.IsWalkableTerrain ||
                    (inspected != CurrentPosition &&
                     cell.Occupancy != GridOccupancy.None))
                {
                    direction = CardinalDirection.None;
                    return false;
                }

                inspected = GetTarget(inspected, direction);
            }

            return true;
        }

        private bool TryGetLaneAcquisitionStep(
            GridPosition targetPosition,
            out CardinalDirection direction)
        {
            acquisitionParents.Clear();
            acquisitionVisited.Clear();
            acquisitionFrontier.Clear();
            acquisitionVisited.Add(CurrentPosition);
            acquisitionFrontier.Enqueue(CurrentPosition);

            while (acquisitionFrontier.Count > 0)
            {
                GridPosition current = acquisitionFrontier.Dequeue();
                if (current != CurrentPosition &&
                    TryGetClearChargeDirection(current, targetPosition, out _))
                {
                    direction = GetFirstStepDirection(current);
                    return true;
                }

                foreach (CardinalDirection candidateDirection in DirectionPriority)
                {
                    GridPosition next = GetTarget(current, candidateDirection);
                    if (acquisitionVisited.Contains(next) ||
                        !IsAcquisitionTraversable(next))
                    {
                        continue;
                    }

                    acquisitionVisited.Add(next);
                    acquisitionParents.Add(next, current);
                    acquisitionFrontier.Enqueue(next);
                }
            }

            direction = CardinalDirection.None;
            return false;
        }

        private CardinalDirection GetFirstStepDirection(GridPosition destination)
        {
            GridPosition firstStep = destination;
            while (acquisitionParents[firstStep] != CurrentPosition)
            {
                firstStep = acquisitionParents[firstStep];
            }

            int deltaX = firstStep.X - CurrentPosition.X;
            int deltaZ = firstStep.Z - CurrentPosition.Z;
            if (deltaZ == 1)
            {
                return CardinalDirection.North;
            }
            if (deltaX == 1)
            {
                return CardinalDirection.East;
            }
            if (deltaZ == -1)
            {
                return CardinalDirection.South;
            }
            if (deltaX == -1)
            {
                return CardinalDirection.West;
            }

            throw new InvalidOperationException(
                "Charger lane acquisition produced a non-cardinal first step.");
        }

        private bool IsAcquisitionTraversable(GridPosition position)
        {
            GridCellState cell = grid.GetCell(position);
            return cell.IsWalkableTerrain &&
                (position == CurrentPosition || cell.Occupancy == GridOccupancy.None);
        }

        private int CountChargeDistance(
            CardinalDirection direction,
            GridPosition targetPosition)
        {
            int distance = 0;
            GridPosition inspected = GetTarget(CurrentPosition, direction);
            while (true)
            {
                GridCellState cell = grid.GetCell(inspected);
                if (!cell.IsWalkableTerrain)
                {
                    return distance;
                }

                if (inspected == targetPosition)
                {
                    distance++;
                    if (cell.HasBomb)
                    {
                        return distance;
                    }
                }
                else
                {
                    if (cell.Occupancy != GridOccupancy.None)
                    {
                        return distance;
                    }
                    distance++;
                }

                inspected = GetTarget(inspected, direction);
            }
        }

        private ChargerEnemyAdvanceResult Transition(ChargerEnemyState previous)
        {
            return new ChargerEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                LockedDirection,
                lockedChargeDistance,
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
                lockedChargeDistance,
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
