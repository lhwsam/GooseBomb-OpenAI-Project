using System;

namespace BombSwap.Core
{
    public sealed class ArmoredEnemySimulation
    {
        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly EnemyHealthSimulation health;
        private TimeSpan nextStepAt;
        private TimeSpan lastObservedTime;
        private int remainingCommittedSteps;

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
            State = ArmoredEnemyState.Armored;
            health = new EnemyHealthSimulation(actorId, ArmoredEnemyDefinition.StageCount);
            lastObservedTime = clock.Now;
            nextStepAt = AddWithSaturation(clock.Now, definition.ArmoredStepInterval);
        }

        public ArmoredEnemyDefinition Definition { get; }

        public ActorId ActorId { get; }

        public ActorId TargetActorId { get; }

        public GridPosition CurrentPosition { get; private set; }

        public CardinalDirection CurrentDirection { get; private set; }

        public int RemainingCommittedSteps => remainingCommittedSteps;

        public int CurrentHealth => health.CurrentHealth;

        public bool IsDead => State == ArmoredEnemyState.Dead;

        public ArmoredEnemyState State { get; private set; }

        public bool TryAdvance(out EnemyMovementStep step)
        {
            TimeSpan now = ObserveTime();
            step = default;
            if (IsDead || now < nextStepAt)
            {
                return false;
            }

            nextStepAt = AddWithSaturation(now, Definition.GetStepInterval(State));
            if (!grid.TryGetActorPosition(TargetActorId, out GridPosition targetPosition))
            {
                return false;
            }
            if (ManhattanDistance(CurrentPosition, targetPosition) <= 1L)
            {
                return false;
            }

            CardinalDirection direction;
            if (remainingCommittedSteps > 0 && IsAvailable(CurrentDirection))
            {
                direction = CurrentDirection;
            }
            else if (!TrySelectDirection(targetPosition, out direction))
            {
                ResetDirectionCommitment();
                return false;
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
                return false;
            }

            CurrentPosition = to;
            remainingCommittedSteps--;
            step = new EnemyMovementStep(ActorId, from, to, direction);
            return true;
        }

        public ArmoredEnemyDamageResult ApplyExplosion(BombId explosionId)
        {
            TimeSpan now = ObserveTime();
            ArmoredEnemyState previousState = State;
            EnemyDamageResult damage = health.ApplyExplosionDamage(explosionId, 1);
            if (!damage.WasApplied)
            {
                return new ArmoredEnemyDamageResult(damage, previousState, State);
            }

            if (damage.WasFatal)
            {
                State = ArmoredEnemyState.Dead;
                ResetDirectionCommitment();
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
                nextStepAt = now;
            }

            return new ArmoredEnemyDamageResult(damage, previousState, State);
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
            out CardinalDirection selected)
        {
            selected = CardinalDirection.None;
            long bestDistance = long.MaxValue;

            if (CurrentDirection != CardinalDirection.None)
            {
                ConsiderDirection(CurrentDirection, targetPosition, ref selected, ref bestDistance);
            }

            ConsiderDirection(CardinalDirection.North, targetPosition, ref selected, ref bestDistance);
            ConsiderDirection(CardinalDirection.East, targetPosition, ref selected, ref bestDistance);
            ConsiderDirection(CardinalDirection.South, targetPosition, ref selected, ref bestDistance);
            ConsiderDirection(CardinalDirection.West, targetPosition, ref selected, ref bestDistance);
            return selected != CardinalDirection.None;
        }

        private void ConsiderDirection(
            CardinalDirection candidate,
            GridPosition targetPosition,
            ref CardinalDirection selected,
            ref long bestDistance)
        {
            if (candidate == selected || !IsAvailable(candidate))
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

        private bool IsAvailable(CardinalDirection direction)
        {
            if (direction == CardinalDirection.None)
            {
                return false;
            }

            GridCellState cell = grid.GetCell(GetTarget(CurrentPosition, direction));
            return cell.IsWalkableTerrain && cell.Occupancy == GridOccupancy.None;
        }

        private void ResetDirectionCommitment()
        {
            CurrentDirection = CardinalDirection.None;
            remainingCommittedSteps = 0;
        }

        private static long ManhattanDistance(GridPosition left, GridPosition right)
        {
            return Math.Abs((long)left.X - right.X) + Math.Abs((long)left.Z - right.Z);
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
                        "Armored enemy movement requires a non-zero cardinal direction.");
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
