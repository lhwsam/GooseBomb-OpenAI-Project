using System;

namespace BombSwap.Core
{
    public sealed class ArmoredEnemySimulation
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
        private readonly EnemyHealthSimulation health;
        private readonly GridPosition guardOrigin;
        private readonly GridPosition[] panicPath;
        private TimeSpan nextStepAt;
        private TimeSpan nextPanicStepAt;
        private TimeSpan behaviorEndsAt;
        private TimeSpan lastObservedTime;
        private int remainingCommittedSteps;
        private int panicPathCellCount;
        private int nextPanicPathIndex;

        public ArmoredEnemySimulation(
            GridState grid,
            IGameClock clock,
            ArmoredEnemyDefinition definition,
            ActorId actorId,
            ActorId targetActorId,
            GridPosition startPosition)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Armored enemy actor ID must be valid.", nameof(actorId));
            }
            if (!targetActorId.IsValid)
            {
                throw new ArgumentException("Armored enemy target actor ID must be valid.", nameof(targetActorId));
            }
            if (actorId == targetActorId)
            {
                throw new ArgumentException(
                    "Armored enemy actor and target actor must be different.",
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
                throw new InvalidOperationException("Armored enemy target must already occupy the grid.");
            }
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException(
                    $"Armored enemy cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            TargetActorId = targetActorId;
            CurrentPosition = startPosition;
            guardOrigin = startPosition;
            State = ArmoredEnemyState.Armored;
            BehaviorState = ArmoredEnemyBehaviorState.Guard;
            health = new EnemyHealthSimulation(actorId, ArmoredEnemyDefinition.StageCount);
            panicPath = new GridPosition[definition.PanicRunDistance];
            lastObservedTime = clock.Now;
            nextStepAt = AddWithSaturation(clock.Now, definition.ArmoredStepInterval);
        }

        public ArmoredEnemyDefinition Definition { get; }

        public ActorId ActorId { get; }

        public ActorId TargetActorId { get; }

        public GridPosition CurrentPosition { get; private set; }

        public GridPosition GuardOrigin => guardOrigin;

        public CardinalDirection CurrentDirection { get; private set; }

        public int RemainingCommittedSteps => remainingCommittedSteps;

        public int CurrentHealth => health.CurrentHealth;

        public bool IsDead => State == ArmoredEnemyState.Dead;

        public ArmoredEnemyState State { get; private set; }

        public ArmoredEnemyBehaviorState BehaviorState { get; private set; }

        public CardinalDirection PanicDirection { get; private set; }

        public int PanicPathCellCount => panicPathCellCount;

        public GridPosition PanicDestination =>
            panicPathCellCount > 0 ? panicPath[panicPathCellCount - 1] : CurrentPosition;

        public TimeSpan BehaviorEndsAt => behaviorEndsAt;

        public GridPosition GetPanicPathCell(int index)
        {
            if (index < 0 || index >= panicPathCellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return panicPath[index];
        }

        public ArmoredEnemyAdvanceResult Advance()
        {
            TimeSpan now = ObserveTime();
            if (IsDead)
            {
                return NoActivity();
            }

            switch (BehaviorState)
            {
                case ArmoredEnemyBehaviorState.Guard:
                    return AdvancePursuit(now, true);
                case ArmoredEnemyBehaviorState.PanicTelegraph:
                    return AdvancePanicTelegraph(now);
                case ArmoredEnemyBehaviorState.PanicRun:
                    return AdvancePanicRun(now);
                case ArmoredEnemyBehaviorState.PanicRecover:
                    return AdvancePanicRecover(now);
                case ArmoredEnemyBehaviorState.Chase:
                    return AdvancePursuit(now, false);
                default:
                    throw new InvalidOperationException(
                        $"Unsupported armored enemy behavior state: {BehaviorState}.");
            }
        }

        public bool TryAdvance(out EnemyMovementStep step)
        {
            ArmoredEnemyAdvanceResult result = Advance();
            step = result.Movement;
            return result.HasMovement;
        }

        public ArmoredEnemyDamageResult ApplyExplosion(
            BombId explosionId,
            GridPosition explosionOrigin)
        {
            TimeSpan now = ObserveTime();
            ArmoredEnemyState previousState = State;
            ArmoredEnemyBehaviorState previousBehavior = BehaviorState;
            EnemyDamageResult damage = health.ApplyExplosionDamage(explosionId, 1);
            if (!damage.WasApplied)
            {
                return new ArmoredEnemyDamageResult(
                    damage,
                    previousState,
                    State,
                    previousBehavior,
                    BehaviorState);
            }

            if (damage.WasFatal)
            {
                State = ArmoredEnemyState.Dead;
                BehaviorState = ArmoredEnemyBehaviorState.Dead;
                ResetDirectionCommitment();
                ClearPanicPlan();
                if (!grid.TryRemoveActor(ActorId))
                {
                    throw new InvalidOperationException(
                        "Dead armored enemy no longer occupied its authoritative grid cell.");
                }
            }
            else
            {
                State = ArmoredEnemyState.Broken;
                ResetDirectionCommitment();
                BeginPanic(now, explosionOrigin);
            }

            return new ArmoredEnemyDamageResult(
                damage,
                previousState,
                State,
                previousBehavior,
                BehaviorState);
        }

        private ArmoredEnemyAdvanceResult AdvancePursuit(TimeSpan now, bool enforceGuardRadius)
        {
            if (now < nextStepAt)
            {
                return NoActivity();
            }

            nextStepAt = AddWithSaturation(now, Definition.GetStepInterval(State));
            if (!grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition))
            {
                return NoActivity();
            }
            if (ManhattanDistance(CurrentPosition, targetPosition) <= 1L)
            {
                return NoActivity();
            }

            CardinalDirection direction;
            if (remainingCommittedSteps > 0 &&
                IsAvailable(CurrentDirection, enforceGuardRadius) &&
                (!enforceGuardRadius || IsCloserToTarget(CurrentDirection, targetPosition)))
            {
                direction = CurrentDirection;
            }
            else if (!TrySelectDirection(targetPosition, enforceGuardRadius, out direction))
            {
                ResetDirectionCommitment();
                return NoActivity();
            }
            else
            {
                CurrentDirection = direction;
                remainingCommittedSteps = Definition.DirectionCommitmentSteps;
            }

            GridPosition from = CurrentPosition;
            GridPosition to = GetTarget(from, direction);
            if (!grid.TryMoveActor(ActorId, to))
            {
                ResetDirectionCommitment();
                return NoActivity();
            }

            CurrentPosition = to;
            remainingCommittedSteps--;
            var movement = new EnemyMovementStep(ActorId, from, to, direction);
            return Activity(BehaviorState, movement, true);
        }

        private ArmoredEnemyAdvanceResult AdvancePanicTelegraph(TimeSpan now)
        {
            if (now < behaviorEndsAt)
            {
                return NoActivity();
            }

            ArmoredEnemyBehaviorState previous = BehaviorState;
            BehaviorState = ArmoredEnemyBehaviorState.PanicRun;
            nextPanicStepAt = now;
            behaviorEndsAt = TimeSpan.Zero;
            return Transition(previous);
        }

        private ArmoredEnemyAdvanceResult AdvancePanicRun(TimeSpan now)
        {
            if (nextPanicPathIndex >= panicPathCellCount)
            {
                return BeginPanicRecover(now);
            }
            if (now < nextPanicStepAt)
            {
                return NoActivity();
            }

            GridPosition destination = panicPath[nextPanicPathIndex];
            GridPosition from = CurrentPosition;
            if (!grid.TryMoveActor(ActorId, destination))
            {
                return BeginPanicRecover(now);
            }

            CurrentPosition = destination;
            nextPanicPathIndex++;
            nextPanicStepAt = AddWithSaturation(now, Definition.PanicStepInterval);
            var movement = new EnemyMovementStep(
                ActorId,
                from,
                destination,
                PanicDirection);
            if (nextPanicPathIndex >= panicPathCellCount)
            {
                ArmoredEnemyBehaviorState previous = BehaviorState;
                BehaviorState = ArmoredEnemyBehaviorState.PanicRecover;
                behaviorEndsAt = AddWithSaturation(now, Definition.PanicRecoverDuration);
                return Activity(previous, movement, true);
            }

            return Activity(BehaviorState, movement, true);
        }

        private ArmoredEnemyAdvanceResult AdvancePanicRecover(TimeSpan now)
        {
            if (now < behaviorEndsAt)
            {
                return NoActivity();
            }

            ArmoredEnemyBehaviorState previous = BehaviorState;
            BehaviorState = ArmoredEnemyBehaviorState.Chase;
            nextStepAt = now;
            behaviorEndsAt = TimeSpan.Zero;
            ArmoredEnemyAdvanceResult result = Transition(previous);
            ClearPanicPlan();
            return result;
        }

        private ArmoredEnemyAdvanceResult BeginPanicRecover(TimeSpan now)
        {
            ArmoredEnemyBehaviorState previous = BehaviorState;
            BehaviorState = ArmoredEnemyBehaviorState.PanicRecover;
            behaviorEndsAt = AddWithSaturation(now, Definition.PanicRecoverDuration);
            return Transition(previous);
        }

        private void BeginPanic(TimeSpan now, GridPosition explosionOrigin)
        {
            PlanPanicPath(explosionOrigin);
            nextPanicPathIndex = 0;
            if (panicPathCellCount > 0)
            {
                BehaviorState = ArmoredEnemyBehaviorState.PanicTelegraph;
                behaviorEndsAt = AddWithSaturation(now, Definition.PanicTelegraphDuration);
            }
            else
            {
                BehaviorState = ArmoredEnemyBehaviorState.PanicRecover;
                behaviorEndsAt = AddWithSaturation(now, Definition.PanicRecoverDuration);
            }
        }

        private void PlanPanicPath(GridPosition explosionOrigin)
        {
            ClearPanicPlan();
            int bestCellCount = 0;
            long bestAwayProjection = long.MinValue;
            long bestDistance = long.MinValue;
            CardinalDirection selected = CardinalDirection.None;
            int selectedCellCount = 0;

            foreach (CardinalDirection direction in DirectionPriority)
            {
                int cellCount = CountAvailablePanicCells(direction);
                if (cellCount <= 0)
                {
                    continue;
                }

                GridPosition destination = Offset(CurrentPosition, direction, cellCount);
                long awayProjection = GetAwayProjection(direction, explosionOrigin);
                long distance = ManhattanDistance(destination, explosionOrigin);
                if (cellCount > bestCellCount ||
                    (cellCount == bestCellCount && awayProjection > bestAwayProjection) ||
                    (cellCount == bestCellCount &&
                     awayProjection == bestAwayProjection &&
                     distance > bestDistance))
                {
                    bestCellCount = cellCount;
                    bestAwayProjection = awayProjection;
                    bestDistance = distance;
                    selected = direction;
                    selectedCellCount = cellCount;
                }
            }

            if (selected == CardinalDirection.None)
            {
                return;
            }

            PanicDirection = selected;
            panicPathCellCount = selectedCellCount;
            for (int index = 0; index < selectedCellCount; index++)
            {
                panicPath[index] = Offset(CurrentPosition, selected, index + 1);
            }
        }

        private int CountAvailablePanicCells(CardinalDirection direction)
        {
            int count = 0;
            for (int distance = 1; distance <= Definition.PanicRunDistance; distance++)
            {
                GridPosition candidate = Offset(CurrentPosition, direction, distance);
                GridCellState cell = grid.GetCell(candidate);
                if (!cell.IsWalkableTerrain || cell.Occupancy != GridOccupancy.None)
                {
                    break;
                }

                count++;
            }

            return count;
        }

        private TimeSpan ObserveTime()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during armored enemy simulation.");
            }

            lastObservedTime = now;
            return now;
        }

        private bool TrySelectDirection(
            GridPosition targetPosition,
            bool enforceGuardRadius,
            out CardinalDirection selected)
        {
            selected = CardinalDirection.None;
            long bestDistance = enforceGuardRadius
                ? ManhattanDistance(CurrentPosition, targetPosition)
                : long.MaxValue;

            if (CurrentDirection != CardinalDirection.None)
            {
                ConsiderDirection(
                    CurrentDirection,
                    targetPosition,
                    enforceGuardRadius,
                    ref selected,
                    ref bestDistance);
            }

            foreach (CardinalDirection direction in DirectionPriority)
            {
                ConsiderDirection(
                    direction,
                    targetPosition,
                    enforceGuardRadius,
                    ref selected,
                    ref bestDistance);
            }

            return selected != CardinalDirection.None;
        }

        private void ConsiderDirection(
            CardinalDirection candidate,
            GridPosition targetPosition,
            bool enforceGuardRadius,
            ref CardinalDirection selected,
            ref long bestDistance)
        {
            if (candidate == selected || !IsAvailable(candidate, enforceGuardRadius))
            {
                return;
            }

            long distance = ManhattanDistance(GetTarget(CurrentPosition, candidate), targetPosition);
            if (distance < bestDistance)
            {
                selected = candidate;
                bestDistance = distance;
            }
        }

        private bool IsCloserToTarget(
            CardinalDirection direction,
            GridPosition targetPosition)
        {
            return ManhattanDistance(GetTarget(CurrentPosition, direction), targetPosition) <
                ManhattanDistance(CurrentPosition, targetPosition);
        }

        private bool IsAvailable(CardinalDirection direction, bool enforceGuardRadius)
        {
            if (direction == CardinalDirection.None)
            {
                return false;
            }

            GridPosition target = GetTarget(CurrentPosition, direction);
            if (enforceGuardRadius &&
                ManhattanDistance(guardOrigin, target) > Definition.GuardRadius)
            {
                return false;
            }

            GridCellState cell = grid.GetCell(target);
            return cell.IsWalkableTerrain && cell.Occupancy == GridOccupancy.None;
        }

        private void ResetDirectionCommitment()
        {
            CurrentDirection = CardinalDirection.None;
            remainingCommittedSteps = 0;
        }

        private void ClearPanicPlan()
        {
            PanicDirection = CardinalDirection.None;
            panicPathCellCount = 0;
            nextPanicPathIndex = 0;
            nextPanicStepAt = TimeSpan.Zero;
        }

        private ArmoredEnemyAdvanceResult NoActivity()
        {
            return new ArmoredEnemyAdvanceResult(
                ActorId,
                BehaviorState,
                BehaviorState,
                PanicDirection,
                PanicDestination,
                panicPathCellCount,
                default,
                false);
        }

        private ArmoredEnemyAdvanceResult Transition(ArmoredEnemyBehaviorState previous)
        {
            return new ArmoredEnemyAdvanceResult(
                ActorId,
                previous,
                BehaviorState,
                PanicDirection,
                PanicDestination,
                panicPathCellCount,
                default,
                false);
        }

        private ArmoredEnemyAdvanceResult Activity(
            ArmoredEnemyBehaviorState previous,
            EnemyMovementStep movement,
            bool hasMovement)
        {
            return new ArmoredEnemyAdvanceResult(
                ActorId,
                previous,
                BehaviorState,
                PanicDirection,
                PanicDestination,
                panicPathCellCount,
                movement,
                hasMovement);
        }

        private static long ManhattanDistance(GridPosition left, GridPosition right)
        {
            return Math.Abs((long)left.X - right.X) + Math.Abs((long)left.Z - right.Z);
        }

        private long GetAwayProjection(
            CardinalDirection direction,
            GridPosition explosionOrigin)
        {
            long awayX = (long)CurrentPosition.X - explosionOrigin.X;
            long awayZ = (long)CurrentPosition.Z - explosionOrigin.Z;
            switch (direction)
            {
                case CardinalDirection.North:
                    return awayZ;
                case CardinalDirection.East:
                    return awayX;
                case CardinalDirection.South:
                    return -awayZ;
                case CardinalDirection.West:
                    return -awayX;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private static GridPosition Offset(
            GridPosition current,
            CardinalDirection direction,
            int distance)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return current.Offset(0, distance);
                case CardinalDirection.East:
                    return current.Offset(distance, 0);
                case CardinalDirection.South:
                    return current.Offset(0, -distance);
                case CardinalDirection.West:
                    return current.Offset(-distance, 0);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Armored enemy movement requires a non-zero cardinal direction.");
            }
        }

        private static GridPosition GetTarget(
            GridPosition current,
            CardinalDirection direction)
        {
            return Offset(current, direction, 1);
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }
    }
}
