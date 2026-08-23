using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class ThrowerEnemySimulation
    {
        private static readonly CardinalDirection[] DirectionPriority =
        {
            CardinalDirection.North,
            CardinalDirection.East,
            CardinalDirection.South,
            CardinalDirection.West,
        };
        private static readonly IReadOnlyList<GridPosition> NoLockedTargets =
            Array.AsReadOnly(Array.Empty<GridPosition>());

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly GridPosition[] firingAnchors;
        private readonly GridPosition[] targetAnchors;
        private readonly Dictionary<GridPosition, int> distances =
            new Dictionary<GridPosition, int>();
        private readonly Queue<GridPosition> frontier = new Queue<GridPosition>();
        private readonly CommittedActorMovement movement;
        private TimeSpan lastObservedTime;
        private TimeSpan nextMoveAt;
        private TimeSpan telegraphStartedAt;
        private TimeSpan recoveryStartedAt;
        private int firingAnchorIndex;
        private int pendingFlightCount;
        private IReadOnlyList<GridPosition> lockedTargets = NoLockedTargets;
        private readonly HashSet<BombId> activeBombIds = new HashSet<BombId>();
        private EnemyLocomotionState locomotionState;
        private EnemyMovementTransition movementTransition;

        public ThrowerEnemySimulation(
            GridState grid,
            IGameClock clock,
            ThrowerEnemyDefinition definition,
            ActorId actorId,
            ActorId targetActorId,
            GridPosition startPosition,
            IReadOnlyList<GridPosition> authoredFiringAnchors,
            IReadOnlyList<GridPosition> authoredTargetAnchors)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Thrower actor ID must be valid.", nameof(actorId));
            }
            if (!targetActorId.IsValid || targetActorId == actorId)
            {
                throw new ArgumentException(
                    "Thrower target actor ID must be valid and distinct.",
                    nameof(targetActorId));
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(clock));
            }
            if (!grid.TryGetActorPosition(targetActorId, out _))
            {
                throw new InvalidOperationException(
                    "Thrower target must already occupy the grid.");
            }

            firingAnchors = CopyAndValidateAnchors(
                authoredFiringAnchors,
                nameof(authoredFiringAnchors));
            targetAnchors = CopyAndValidateAnchors(
                authoredTargetAnchors,
                nameof(authoredTargetAnchors));
            long requiredTargetAnchorCount = (long)Definition.BombsPerVolley * 2;
            if (targetAnchors.Length < requiredTargetAnchorCount)
            {
                throw new ArgumentException(
                    $"Thrower requires at least {requiredTargetAnchorCount} distinct target anchors " +
                    "so repeated volleys can rotate their side targets.",
                    nameof(authoredTargetAnchors));
            }
            if (Array.IndexOf(firingAnchors, startPosition) >= 0)
            {
                throw new ArgumentException(
                    "Thrower start position must be a distinct staging cell outside its firing anchors.",
                    nameof(startPosition));
            }
            firingAnchorIndex = 0;
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException(
                    $"Thrower enemy cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            TargetActorId = targetActorId;
            movement = new CommittedActorMovement(grid, actorId, startPosition, clock.Now);
            State = ThrowerEnemyState.Track;
            nextMoveAt = clock.Now;
            lastObservedTime = clock.Now;
        }

        public ThrowerEnemyDefinition Definition { get; }

        public ActorId ActorId { get; }

        public ActorId TargetActorId { get; }

        public GridPosition CurrentPosition => movement.CurrentCell;

        public GridSubcellPosition Position => movement.Position;

        public GridPosition GetCurrentCellAt(TimeSpan gameTime) =>
            movement.GetCurrentCellAt(gameTime);

        public ThrowerEnemyState State { get; private set; }

        public EnemyLocomotionState LocomotionState => locomotionState;

        public EnemyMovementTransition MovementTransition => movementTransition;

        public GridPosition LockedTarget =>
            lockedTargets.Count > 0 ? lockedTargets[0] : default;

        public IReadOnlyList<GridPosition> LockedTargets => lockedTargets;

        public GridPosition CurrentFiringAnchor => firingAnchors[firingAnchorIndex];

        public bool HasOutstandingBomb =>
            pendingFlightCount > 0 || activeBombIds.Count > 0;

        public int PendingFlightCount => pendingFlightCount;

        public int ActiveBombCount => activeBombIds.Count;

        public ThrowerEnemyAdvanceResult Advance()
        {
            TimeSpan now = ObserveTime();
            movement.Advance(now);
            if (movement.IsMoving)
            {
                locomotionState = EnemyLocomotionState.Moving;
                return NoActivity();
            }
            switch (State)
            {
                case ThrowerEnemyState.Track:
                    return AdvanceTrack(now);
                case ThrowerEnemyState.Telegraph:
                    if (now - telegraphStartedAt < Definition.TelegraphDuration)
                    {
                        return NoActivity();
                    }

                    ThrowerEnemyState previous = State;
                    State = ThrowerEnemyState.Recover;
                    locomotionState = EnemyLocomotionState.Idle;
                    recoveryStartedAt = now;
                    pendingFlightCount = lockedTargets.Count;
                    return CreateResult(previous, default, TimeSpan.Zero, false, true);
                case ThrowerEnemyState.Recover:
                    if (now - recoveryStartedAt < Definition.RecoveryDuration)
                    {
                        return NoActivity();
                    }

                    ThrowerEnemyState recoveryPrevious = State;
                    State = ThrowerEnemyState.Track;
                    locomotionState = EnemyLocomotionState.Idle;
                    firingAnchorIndex = (firingAnchorIndex + 1) % firingAnchors.Length;
                    nextMoveAt = now;
                    return CreateResult(
                        recoveryPrevious,
                        default,
                        TimeSpan.Zero,
                        false,
                        false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(State), State, null);
            }
        }

        public void ConfirmBombPlaced(BombId bombId)
        {
            if (!bombId.IsValid)
            {
                throw new ArgumentException("Placed bomb ID must be valid.", nameof(bombId));
            }
            if (pendingFlightCount <= 0)
            {
                throw new InvalidOperationException(
                    "A thrower bomb can only be confirmed for a pending volley flight.");
            }
            if (!activeBombIds.Add(bombId))
            {
                throw new InvalidOperationException(
                    $"Thrower bomb {bombId} was already confirmed.");
            }

            pendingFlightCount--;
        }

        public void NotifyLaunchFailed()
        {
            if (pendingFlightCount <= 0)
            {
                throw new InvalidOperationException(
                    "Only a pending thrower volley flight can fail to land.");
            }

            pendingFlightCount--;
        }

        public void NotifyBombResolved(BombId bombId)
        {
            if (!bombId.IsValid)
            {
                throw new ArgumentException("Resolved bomb ID must be valid.", nameof(bombId));
            }
            activeBombIds.Remove(bombId);
        }

        public bool IsActiveBomb(BombId bombId)
        {
            return bombId.IsValid && activeBombIds.Contains(bombId);
        }

        private ThrowerEnemyAdvanceResult AdvanceTrack(TimeSpan now)
        {
            if (CurrentPosition == CurrentFiringAnchor)
            {
                if (HasOutstandingBomb)
                {
                    locomotionState = EnemyLocomotionState.Idle;
                    return NoActivity();
                }
                if (!grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition))
                {
                    locomotionState = EnemyLocomotionState.Idle;
                    return NoActivity();
                }

                ThrowerEnemyState previous = State;
                lockedTargets = SelectTargetAnchors(targetPosition);
                State = ThrowerEnemyState.Telegraph;
                locomotionState = EnemyLocomotionState.Idle;
                telegraphStartedAt = now;
                return CreateResult(previous, default, TimeSpan.Zero, false, false);
            }
            if (now < nextMoveAt)
            {
                return NoActivity();
            }

            bool moved = TryMoveToward(
                CurrentFiringAnchor,
                now,
                Definition.MoveStepInterval,
                out EnemyMovementStep movementStep);
            locomotionState = moved
                ? EnemyLocomotionState.Moving
                : EnemyLocomotionState.Idle;
            nextMoveAt = AddWithSaturation(now, Definition.MoveStepInterval);
            if (moved)
            {
                movementTransition = new EnemyMovementTransition(
                    movementStep,
                    now,
                    Definition.MoveStepInterval);
            }
            return CreateResult(
                State,
                movementStep,
                Definition.MoveStepInterval,
                moved,
                false);
        }

        private bool TryMoveToward(
            GridPosition destination,
            TimeSpan now,
            TimeSpan duration,
            out EnemyMovementStep movementStep)
        {
            BuildDistanceField(destination);
            CardinalDirection selectedDirection = CardinalDirection.None;
            int bestDistance = int.MaxValue;
            for (int index = 0; index < DirectionPriority.Length; index++)
            {
                CardinalDirection direction = DirectionPriority[index];
                GridPosition candidate = Offset(CurrentPosition, direction);
                if (!IsAvailable(candidate) ||
                    !distances.TryGetValue(candidate, out int distance) ||
                    distance >= bestDistance)
                {
                    continue;
                }

                selectedDirection = direction;
                bestDistance = distance;
            }
            if (selectedDirection == CardinalDirection.None)
            {
                movementStep = default;
                return false;
            }

            GridPosition previous = CurrentPosition;
            GridPosition next = Offset(previous, selectedDirection);
            if (!movement.TryStart(next, selectedDirection, now, duration))
            {
                movementStep = default;
                return false;
            }

            movementStep = new EnemyMovementStep(ActorId, previous, next, selectedDirection);
            return true;
        }

        private void BuildDistanceField(GridPosition destination)
        {
            distances.Clear();
            frontier.Clear();
            distances.Add(destination, 0);
            frontier.Enqueue(destination);
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                int distance = distances[current] + 1;
                for (int index = 0; index < DirectionPriority.Length; index++)
                {
                    GridPosition next = Offset(current, DirectionPriority[index]);
                    if (distances.ContainsKey(next) || !IsPathTraversable(next, destination))
                    {
                        continue;
                    }

                    distances.Add(next, distance);
                    frontier.Enqueue(next);
                }
            }
        }

        private bool IsPathTraversable(GridPosition position, GridPosition destination)
        {
            GridCellState cell = grid.GetCell(position);
            return cell.IsWalkableTerrain &&
                (position == CurrentPosition ||
                 position == destination ||
                 (cell.Occupancy == GridOccupancy.None &&
                  !grid.IsCellReservedForActorMove(position)));
        }

        private bool IsAvailable(GridPosition position)
        {
            GridCellState cell = grid.GetCell(position);
            return cell.IsWalkableTerrain &&
                cell.Occupancy == GridOccupancy.None &&
                !grid.IsCellReservedForActorMove(position);
        }

        private IReadOnlyList<GridPosition> SelectTargetAnchors(
            GridPosition targetPosition)
        {
            var rankedIndices = new int[targetAnchors.Length];
            for (int index = 0; index < rankedIndices.Length; index++)
            {
                rankedIndices[index] = index;
            }
            for (int index = 1; index < rankedIndices.Length; index++)
            {
                int candidateIndex = rankedIndices[index];
                long candidateDistance = ManhattanDistance(
                    targetAnchors[candidateIndex],
                    targetPosition);
                int insertionIndex = index;
                while (insertionIndex > 0)
                {
                    int previousIndex = rankedIndices[insertionIndex - 1];
                    long previousDistance = ManhattanDistance(
                        targetAnchors[previousIndex],
                        targetPosition);
                    if (candidateDistance >= previousDistance)
                    {
                        break;
                    }

                    rankedIndices[insertionIndex] = previousIndex;
                    insertionIndex--;
                }
                rankedIndices[insertionIndex] = candidateIndex;
            }

            var selected = new GridPosition[Definition.BombsPerVolley];
            selected[0] = targetAnchors[rankedIndices[0]];
            int rotatingTargetCount = rankedIndices.Length - 1;
            int rotatingSelectionCount = selected.Length - 1;
            int rotationStart = (int)(
                ((long)firingAnchorIndex * rotatingSelectionCount) %
                rotatingTargetCount);
            int selectedCount = 1;
            AppendRotatingTargets(
                selected,
                ref selectedCount,
                rankedIndices,
                rotationStart,
                allowPreviouslyLocked: false);
            AppendRotatingTargets(
                selected,
                ref selectedCount,
                rankedIndices,
                rotationStart,
                allowPreviouslyLocked: true);
            return Array.AsReadOnly(selected);
        }

        private void AppendRotatingTargets(
            GridPosition[] selected,
            ref int selectedCount,
            IReadOnlyList<int> rankedIndices,
            int rotationStart,
            bool allowPreviouslyLocked)
        {
            int rotatingTargetCount = rankedIndices.Count - 1;
            for (int offset = 0;
                offset < rotatingTargetCount && selectedCount < selected.Length;
                offset++)
            {
                int rankedIndex = 1 +
                    ((rotationStart + offset) % rotatingTargetCount);
                GridPosition candidate = targetAnchors[rankedIndices[rankedIndex]];
                if (ContainsPosition(selected, selectedCount, candidate) ||
                    (!allowPreviouslyLocked &&
                     ContainsPosition(lockedTargets, lockedTargets.Count, candidate)))
                {
                    continue;
                }

                selected[selectedCount] = candidate;
                selectedCount++;
            }
        }

        private static bool ContainsPosition(
            IReadOnlyList<GridPosition> positions,
            int count,
            GridPosition candidate)
        {
            for (int index = 0; index < count; index++)
            {
                if (positions[index] == candidate)
                {
                    return true;
                }
            }
            return false;
        }

        private GridPosition[] CopyAndValidateAnchors(
            IReadOnlyList<GridPosition> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (source.Count < 2)
            {
                throw new ArgumentException(
                    "Thrower anchor lists require at least two cells.",
                    parameterName);
            }

            var copy = new GridPosition[source.Count];
            var seen = new HashSet<GridPosition>();
            for (int index = 0; index < source.Count; index++)
            {
                GridPosition position = source[index];
                GridCellState cell = grid.GetCell(position);
                if (!cell.IsWalkableTerrain)
                {
                    throw new ArgumentException(
                        $"Thrower anchor {position} must be walkable.",
                        parameterName);
                }
                if (!seen.Add(position))
                {
                    throw new ArgumentException(
                        $"Duplicate thrower anchor {position}.",
                        parameterName);
                }
                copy[index] = position;
            }
            return copy;
        }

        private TimeSpan ObserveTime()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during thrower simulation.");
            }
            lastObservedTime = now;
            return now;
        }

        private ThrowerEnemyAdvanceResult NoActivity()
        {
            return CreateResult(State, default, TimeSpan.Zero, false, false);
        }

        private ThrowerEnemyAdvanceResult CreateResult(
            ThrowerEnemyState previous,
            EnemyMovementStep movement,
            TimeSpan movementDuration,
            bool hasMovement,
            bool shouldLaunch)
        {
            return new ThrowerEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                lockedTargets,
                movement,
                movementDuration,
                hasMovement,
                shouldLaunch);
        }

        private static GridPosition Offset(
            GridPosition position,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return position.Offset(0, 1);
                case CardinalDirection.East:
                    return position.Offset(1, 0);
                case CardinalDirection.South:
                    return position.Offset(0, -1);
                case CardinalDirection.West:
                    return position.Offset(-1, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private static long ManhattanDistance(GridPosition left, GridPosition right)
        {
            return Math.Abs((long)left.X - right.X) + Math.Abs((long)left.Z - right.Z);
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }
    }
}
