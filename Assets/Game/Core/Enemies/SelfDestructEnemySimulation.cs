using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class SelfDestructEnemySimulation
    {
        private static readonly CardinalDirection[] DirectionPriority =
        {
            CardinalDirection.North,
            CardinalDirection.East,
            CardinalDirection.South,
            CardinalDirection.West,
        };

        private static readonly IReadOnlyList<GridPosition> NoTelegraphCells =
            Array.AsReadOnly(Array.Empty<GridPosition>());

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly Dictionary<GridPosition, int> chaseDistances =
            new Dictionary<GridPosition, int>();
        private readonly Queue<GridPosition> chaseFrontier =
            new Queue<GridPosition>();
        private readonly CommittedActorMovement movement;
        private TimeSpan lastObservedTime;
        private TimeSpan nextChaseStepAt;
        private TimeSpan warningStartedAt;
        private BombId armedBombId;
        private BombId pendingTriggeringExplosionId;
        private bool hasPendingForceTrigger;
        private IReadOnlyList<GridPosition> telegraphCells = NoTelegraphCells;
        private EnemyLocomotionState locomotionState;
        private EnemyMovementTransition movementTransition;

        public SelfDestructEnemySimulation(
            GridState grid,
            IGameClock clock,
            SelfDestructEnemyDefinition definition,
            ActorId actorId,
            ActorId targetActorId,
            GridPosition startPosition)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!actorId.IsValid)
            {
                throw new ArgumentException(
                    "Self-destruct actor ID must be valid.",
                    nameof(actorId));
            }
            if (!targetActorId.IsValid)
            {
                throw new ArgumentException(
                    "Self-destruct target actor ID must be valid.",
                    nameof(targetActorId));
            }
            if (actorId == targetActorId)
            {
                throw new ArgumentException(
                    "Self-destruct actor and target actor must be different.",
                    nameof(targetActorId));
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clock),
                    clock.Now,
                    "Game time cannot be negative.");
            }
            if (!grid.TryGetActorPosition(targetActorId, out GridPosition targetPosition))
            {
                throw new InvalidOperationException(
                    "Self-destruct target must already occupy the grid.");
            }
            if (!grid.TryAddActor(actorId, startPosition))
            {
                throw new InvalidOperationException(
                    $"Self-destruct enemy cannot occupy the starting cell {startPosition}.");
            }

            ActorId = actorId;
            TargetActorId = targetActorId;
            movement = new CommittedActorMovement(grid, actorId, startPosition, clock.Now);
            TargetPosition = targetPosition;
            State = SelfDestructEnemyState.Chase;
            nextChaseStepAt = clock.Now;
            lastObservedTime = clock.Now;
        }

        public SelfDestructEnemyDefinition Definition { get; }

        public ActorId ActorId { get; }

        public ActorId TargetActorId { get; }

        public GridPosition CurrentPosition => movement.CurrentCell;

        public GridSubcellPosition Position => movement.Position;

        public GridPosition GetCurrentCellAt(TimeSpan gameTime) =>
            movement.GetCurrentCellAt(gameTime);

        public GridPosition TargetPosition { get; private set; }

        public SelfDestructEnemyState State { get; private set; }

        public EnemyLocomotionState LocomotionState => locomotionState;

        public EnemyMovementTransition MovementTransition => movementTransition;

        public bool IsArmed => armedBombId.IsValid;

        public BombId ArmedBombId => armedBombId;

        public BombId TriggeringExplosionId { get; private set; }

        public IReadOnlyList<GridPosition> TelegraphCells => telegraphCells;

        public bool IsDetonated => State == SelfDestructEnemyState.Detonated;

        public double WarningProgress => State == SelfDestructEnemyState.WarningChase
            ? CalculateWarningProgress(clock.Now)
            : 0d;

        public SelfDestructEnemyAdvanceResult Advance()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during self-destruct movement.");
            }

            lastObservedTime = now;
            movement.Advance(now);
            if (movement.IsMoving)
            {
                locomotionState = EnemyLocomotionState.Moving;
                return NoActivity();
            }
            if (pendingTriggeringExplosionId.IsValid)
            {
                BombId trigger = pendingTriggeringExplosionId;
                pendingTriggeringExplosionId = default;
                hasPendingForceTrigger = false;
                TargetPosition = CurrentPosition;
                return BeginTelegraph(trigger);
            }
            if (hasPendingForceTrigger)
            {
                hasPendingForceTrigger = false;
                pendingTriggeringExplosionId = default;
                TargetPosition = CurrentPosition;
                return BeginTelegraph(default);
            }
            if (!IsChasing)
            {
                locomotionState = EnemyLocomotionState.Idle;
                return NoActivity();
            }

            if (!grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition))
            {
                locomotionState = EnemyLocomotionState.Idle;
                return NoActivity();
            }

            TargetPosition = targetPosition;
            long distance = ManhattanDistance(CurrentPosition, targetPosition);
            SelfDestructEnemyState previousState = State;
            if (State == SelfDestructEnemyState.WarningChase)
            {
                if (distance > Definition.WarningDistance)
                {
                    State = SelfDestructEnemyState.Chase;
                    warningStartedAt = default;
                }
                else if (CalculateWarningProgress(now) >= 1d)
                {
                    TargetPosition = CurrentPosition;
                    return BeginTelegraph(default);
                }
            }

            if (now < nextChaseStepAt)
            {
                return CreateResult(previousState, default, TimeSpan.Zero, false, false);
            }

            if (distance <= Definition.PrimeDistance)
            {
                TargetPosition = CurrentPosition;
                return BeginTelegraph(default);
            }

            TimeSpan movementDuration = GetCurrentStepInterval(now);
            EnemyMovementStep movementStep = default;
            bool hasMovement = TryMoveToward(
                targetPosition,
                now,
                movementDuration,
                out movementStep);
            locomotionState = hasMovement
                ? EnemyLocomotionState.Moving
                : EnemyLocomotionState.Idle;
            GridPosition selectedCell = hasMovement
                ? movementStep.To
                : CurrentPosition;
            distance = ManhattanDistance(selectedCell, targetPosition);
            if (distance <= Definition.WarningDistance)
            {
                if (State != SelfDestructEnemyState.WarningChase)
                {
                    State = SelfDestructEnemyState.WarningChase;
                    warningStartedAt = now;
                }
            }
            else
            {
                State = SelfDestructEnemyState.Chase;
                warningStartedAt = default;
            }

            nextChaseStepAt = AddWithSaturation(now, movementDuration);
            if (hasMovement)
            {
                movementTransition = new EnemyMovementTransition(
                    movementStep,
                    now,
                    movementDuration);
            }
            return CreateResult(
                previousState,
                movementStep,
                movementDuration,
                hasMovement,
                false);
        }

        public bool TryTriggerFromExplosion(
            BombId triggeringExplosionId,
            out SelfDestructEnemyAdvanceResult result)
        {
            if (!triggeringExplosionId.IsValid)
            {
                throw new ArgumentException(
                    "Triggering explosion ID must be valid.",
                    nameof(triggeringExplosionId));
            }
            if (!IsChasing)
            {
                result = NoActivity();
                return false;
            }

            if (movement.IsMoving)
            {
                if (pendingTriggeringExplosionId.IsValid || hasPendingForceTrigger)
                {
                    result = NoActivity();
                    return false;
                }
                pendingTriggeringExplosionId = triggeringExplosionId;
                result = NoActivity();
                return false;
            }

            TargetPosition = CurrentPosition;
            result = BeginTelegraph(triggeringExplosionId);
            return true;
        }

        public bool TryForceTrigger(out SelfDestructEnemyAdvanceResult result)
        {
            if (!IsChasing)
            {
                result = NoActivity();
                return false;
            }

            if (movement.IsMoving)
            {
                if (!pendingTriggeringExplosionId.IsValid && !hasPendingForceTrigger)
                {
                    hasPendingForceTrigger = true;
                }
                result = NoActivity();
                return false;
            }

            ObserveCurrentTime();
            TargetPosition = CurrentPosition;
            result = BeginTelegraph(default);
            return true;
        }

        public void ConfirmArmed(BombId bombId)
        {
            if (!bombId.IsValid)
            {
                throw new ArgumentException("Armed bomb ID must be valid.", nameof(bombId));
            }
            if (State != SelfDestructEnemyState.Telegraph || IsArmed)
            {
                throw new InvalidOperationException(
                    "Self-destruct enemy can only arm once during Telegraph.");
            }

            armedBombId = bombId;
            ExplosionResolution resolution = CrossExplosionResolver.Resolve(
                grid,
                CurrentPosition,
                Definition.DetonationBombDefinition.Range);
            telegraphCells = Array.AsReadOnly(resolution.AffectedCells.ToArray());
        }

        public SelfDestructEnemyAdvanceResult CompleteDetonation(BombId bombId)
        {
            if (!bombId.IsValid)
            {
                throw new ArgumentException("Detonated bomb ID must be valid.", nameof(bombId));
            }
            if (State != SelfDestructEnemyState.Telegraph ||
                !IsArmed || bombId != armedBombId)
            {
                throw new InvalidOperationException(
                    "Only the armed self-destruct bomb can complete detonation.");
            }

            SelfDestructEnemyState previous = State;
            State = SelfDestructEnemyState.Detonated;
            locomotionState = EnemyLocomotionState.Idle;
            telegraphCells = NoTelegraphCells;
            return new SelfDestructEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                TargetPosition,
                default,
                TimeSpan.Zero,
                false,
                false,
                TriggeringExplosionId);
        }

        private bool IsChasing =>
            State == SelfDestructEnemyState.Chase ||
            State == SelfDestructEnemyState.WarningChase;

        private void ObserveCurrentTime()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during self-destruct movement.");
            }
            lastObservedTime = now;
        }

        private SelfDestructEnemyAdvanceResult BeginTelegraph(
            BombId triggeringExplosionId)
        {
            SelfDestructEnemyState previous = State;
            State = SelfDestructEnemyState.Telegraph;
            locomotionState = EnemyLocomotionState.Idle;
            warningStartedAt = default;
            TriggeringExplosionId = triggeringExplosionId;
            return new SelfDestructEnemyAdvanceResult(
                ActorId,
                previous,
                State,
                TargetPosition,
                default,
                TimeSpan.Zero,
                false,
                true,
                triggeringExplosionId);
        }

        private bool TryMoveToward(
            GridPosition targetPosition,
            TimeSpan now,
            TimeSpan duration,
            out EnemyMovementStep movement)
        {
            BuildDistanceField(targetPosition);
            CardinalDirection selectedDirection = CardinalDirection.None;
            int bestDistance = int.MaxValue;
            foreach (CardinalDirection direction in DirectionPriority)
            {
                GridPosition candidate = GetTarget(CurrentPosition, direction);
                if (!IsAvailable(candidate) ||
                    !chaseDistances.TryGetValue(candidate, out int distance) ||
                    distance >= bestDistance)
                {
                    continue;
                }

                selectedDirection = direction;
                bestDistance = distance;
            }

            if (selectedDirection == CardinalDirection.None)
            {
                movement = default;
                return false;
            }

            GridPosition previousPosition = CurrentPosition;
            GridPosition destination = GetTarget(CurrentPosition, selectedDirection);
            if (!this.movement.TryStart(
                    destination,
                    selectedDirection,
                    now,
                    duration))
            {
                movement = default;
                return false;
            }

            movement = new EnemyMovementStep(
                ActorId,
                previousPosition,
                destination,
                selectedDirection);
            return true;
        }

        private void BuildDistanceField(GridPosition targetPosition)
        {
            chaseDistances.Clear();
            chaseFrontier.Clear();
            chaseDistances.Add(targetPosition, 0);
            chaseFrontier.Enqueue(targetPosition);

            while (chaseFrontier.Count > 0)
            {
                GridPosition current = chaseFrontier.Dequeue();
                int nextDistance = chaseDistances[current] + 1;
                foreach (CardinalDirection direction in DirectionPriority)
                {
                    GridPosition next = GetTarget(current, direction);
                    if (chaseDistances.ContainsKey(next) ||
                        !IsPathTraversable(next, targetPosition))
                    {
                        continue;
                    }

                    chaseDistances.Add(next, nextDistance);
                    chaseFrontier.Enqueue(next);
                }
            }
        }

        private bool IsPathTraversable(
            GridPosition position,
            GridPosition targetPosition)
        {
            GridCellState cell = grid.GetCell(position);
            if (!cell.IsWalkableTerrain)
            {
                return false;
            }

            return position == CurrentPosition ||
                position == targetPosition ||
                (cell.Occupancy == GridOccupancy.None &&
                 !grid.IsCellReservedForActorMove(position));
        }

        private bool IsAvailable(GridPosition position)
        {
            GridCellState cell = grid.GetCell(position);
            return cell.IsWalkableTerrain &&
                cell.Occupancy == GridOccupancy.None &&
                !grid.IsCellReservedForActorMove(position);
        }

        private SelfDestructEnemyAdvanceResult NoActivity()
        {
            return CreateResult(
                State,
                default,
                TimeSpan.Zero,
                false,
                false);
        }

        private SelfDestructEnemyAdvanceResult CreateResult(
            SelfDestructEnemyState previousState,
            EnemyMovementStep movement,
            TimeSpan movementDuration,
            bool hasMovement,
            bool shouldArm)
        {
            return new SelfDestructEnemyAdvanceResult(
                ActorId,
                previousState,
                State,
                TargetPosition,
                movement,
                movementDuration,
                hasMovement,
                shouldArm,
                TriggeringExplosionId);
        }

        private TimeSpan GetCurrentStepInterval(TimeSpan now)
        {
            if (State != SelfDestructEnemyState.WarningChase)
            {
                return Definition.ChaseStepInterval;
            }

            double progress = CalculateWarningProgress(now);
            long chaseTicks = Definition.ChaseStepInterval.Ticks;
            long minimumTicks = Definition.WarningMinimumStepInterval.Ticks;
            long intervalTicks = chaseTicks - (long)Math.Round(
                (chaseTicks - minimumTicks) * progress,
                MidpointRounding.AwayFromZero);
            return TimeSpan.FromTicks(Math.Max(minimumTicks, intervalTicks));
        }

        private double CalculateWarningProgress(TimeSpan now)
        {
            if (now <= warningStartedAt)
            {
                return 0d;
            }

            double progress =
                (now - warningStartedAt).Ticks /
                (double)Definition.WarningEscalationDuration.Ticks;
            return Math.Max(0d, Math.Min(1d, progress));
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
