using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class BossBattleSimulation
    {
        private static readonly IReadOnlyList<GridPosition> NoDangerCells =
            Array.AsReadOnly(Array.Empty<GridPosition>());

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly EnemyHealthSimulation health;
        private readonly GridPosition[] arenaCells;
        private TimeSpan lastObservedTime;
        private TimeSpan stateEndsAt;

        public BossBattleSimulation(
            GridState grid,
            IGameClock clock,
            BossBattleDefinition definition,
            ActorId actorId,
            GridPosition bossPosition,
            IReadOnlyList<GridPosition> playableArenaCells)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Boss actor ID must be valid.", nameof(actorId));
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clock),
                    clock.Now,
                    "Game time cannot be negative.");
            }

            arenaCells = CopyAndValidateArena(playableArenaCells, bossPosition);
            ValidatePatternCoverage(arenaCells);
            if (!grid.TryAddActor(actorId, bossPosition))
            {
                throw new InvalidOperationException(
                    $"Boss cannot occupy the starting cell {bossPosition}.");
            }

            ActorId = actorId;
            BossPosition = bossPosition;
            health = new EnemyHealthSimulation(actorId, definition.MaxHealth);
            State = BossBattleState.Telegraph;
            Phase = BossPhase.One;
            CurrentPattern = BossPatternKind.AlternatingColumns;
            PatternSequence = 0;
            CurrentDangerCells = ResolveDangerCells(
                arenaCells,
                CurrentPattern,
                PatternSequence & 1);
            lastObservedTime = clock.Now;
            stateEndsAt = AddWithSaturation(
                clock.Now,
                definition.PhaseOneTimings.TelegraphDuration);
        }

        public BossBattleDefinition Definition { get; }

        public ActorId ActorId { get; }

        public GridPosition BossPosition { get; }

        public BossBattleState State { get; private set; }

        public BossPhase Phase { get; private set; }

        public BossPatternKind CurrentPattern { get; private set; }

        public int PatternSequence { get; private set; }

        public IReadOnlyList<GridPosition> CurrentDangerCells { get; private set; }

        public TimeSpan StateEndsAt => stateEndsAt;

        public int MaxHealth => health.MaxHealth;

        public int CurrentHealth => health.CurrentHealth;

        public bool IsDead => State == BossBattleState.Defeated;

        public bool IsVulnerable => State == BossBattleState.Recovery;

        public bool TryAdvance(out BossPatternTransition transition)
        {
            TimeSpan now = ObserveTime();
            transition = default;
            if (IsDead || now < stateEndsAt)
            {
                return false;
            }

            TimeSpan scheduledAt = stateEndsAt;
            BossBattleState previous = State;
            switch (State)
            {
                case BossBattleState.Telegraph:
                    State = BossBattleState.Execute;
                    stateEndsAt = AddWithSaturation(
                        scheduledAt,
                        Definition.GetTimings(Phase).ExecuteDuration);
                    break;
                case BossBattleState.Execute:
                    State = BossBattleState.Recovery;
                    stateEndsAt = AddWithSaturation(
                        scheduledAt,
                        Definition.GetTimings(Phase).RecoveryDuration);
                    break;
                case BossBattleState.Recovery:
                    BeginNextTelegraph(scheduledAt);
                    break;
                case BossBattleState.Defeated:
                    return false;
                default:
                    throw new InvalidOperationException($"Unsupported boss state: {State}.");
            }

            transition = new BossPatternTransition(
                ActorId,
                previous,
                State,
                Phase,
                CurrentPattern,
                PatternSequence,
                scheduledAt,
                CurrentDangerCells);
            return true;
        }

        public BossDamageResult ApplyExplosion(BombId explosionId, int damage)
        {
            if (!explosionId.IsValid)
            {
                throw new ArgumentException("Explosion ID must be valid.", nameof(explosionId));
            }
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage),
                    damage,
                    "Boss damage must be positive.");
            }

            ObserveTime();
            if (IsDead)
            {
                return CreateIgnoredDamage(
                    explosionId,
                    damage,
                    BossDamageStatus.IgnoredDefeated);
            }
            if (!IsVulnerable)
            {
                return CreateIgnoredDamage(
                    explosionId,
                    damage,
                    BossDamageStatus.IgnoredNotVulnerable);
            }

            EnemyDamageResult enemyDamage = health.ApplyExplosionDamage(explosionId, damage);
            if (!enemyDamage.WasApplied)
            {
                BossDamageStatus status = enemyDamage.Status == EnemyDamageStatus.IgnoredDuplicateExplosion
                    ? BossDamageStatus.IgnoredDuplicateExplosion
                    : BossDamageStatus.IgnoredDefeated;
                return new BossDamageResult(
                    ActorId,
                    explosionId,
                    damage,
                    enemyDamage.PreviousHealth,
                    enemyDamage.CurrentHealth,
                    Phase,
                    status);
            }

            var result = new BossDamageResult(
                ActorId,
                explosionId,
                damage,
                enemyDamage.PreviousHealth,
                enemyDamage.CurrentHealth,
                Phase,
                BossDamageStatus.Applied);
            if (!result.WasFatal)
            {
                return result;
            }

            State = BossBattleState.Defeated;
            stateEndsAt = TimeSpan.Zero;
            CurrentDangerCells = NoDangerCells;
            if (!grid.TryRemoveActor(ActorId))
            {
                throw new InvalidOperationException(
                    "Defeated boss no longer occupied its authoritative grid cell.");
            }
            return result;
        }

        private void BeginNextTelegraph(TimeSpan scheduledAt)
        {
            PatternSequence++;
            Phase = CurrentHealth <= Definition.PhaseTwoHealthThreshold
                ? BossPhase.Two
                : BossPhase.One;
            CurrentPattern = SelectPattern(Phase, PatternSequence);
            CurrentDangerCells = ResolveDangerCells(
                arenaCells,
                CurrentPattern,
                PatternSequence & 1);
            State = BossBattleState.Telegraph;
            stateEndsAt = AddWithSaturation(
                scheduledAt,
                Definition.GetTimings(Phase).TelegraphDuration);
        }

        private BossDamageResult CreateIgnoredDamage(
            BombId explosionId,
            int damage,
            BossDamageStatus status)
        {
            return new BossDamageResult(
                ActorId,
                explosionId,
                damage,
                CurrentHealth,
                CurrentHealth,
                Phase,
                status);
        }

        private TimeSpan ObserveTime()
        {
            TimeSpan now = clock.Now;
            if (now < lastObservedTime)
            {
                throw new InvalidOperationException(
                    "Game clock moved backwards during boss battle simulation.");
            }
            lastObservedTime = now;
            return now;
        }

        private GridPosition[] CopyAndValidateArena(
            IReadOnlyList<GridPosition> source,
            GridPosition bossPosition)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Count < 4)
            {
                throw new ArgumentException(
                    "Boss arena requires at least four playable cells.",
                    nameof(source));
            }

            var copy = new GridPosition[source.Count];
            var seen = new HashSet<GridPosition>();
            bool foundBossPosition = false;
            for (int index = 0; index < source.Count; index++)
            {
                GridPosition position = source[index];
                if (!seen.Add(position))
                {
                    throw new ArgumentException(
                        $"Duplicate boss arena cell: {position}.",
                        nameof(source));
                }
                if (!grid.GetCell(position).IsWalkableTerrain)
                {
                    throw new ArgumentException(
                        $"Boss arena cell must use walkable terrain: {position}.",
                        nameof(source));
                }

                copy[index] = position;
                foundBossPosition |= position == bossPosition;
            }
            if (!foundBossPosition)
            {
                throw new ArgumentException(
                    "Boss position must be part of the playable arena.",
                    nameof(bossPosition));
            }

            Array.Sort(copy, ComparePositions);
            return copy;
        }

        private static void ValidatePatternCoverage(IReadOnlyList<GridPosition> cells)
        {
            var patterns = new[]
            {
                BossPatternKind.AlternatingColumns,
                BossPatternKind.AlternatingRows,
                BossPatternKind.Checkerboard,
            };
            foreach (BossPatternKind pattern in patterns)
            {
                for (int parity = 0; parity <= 1; parity++)
                {
                    int dangerCount = 0;
                    for (int index = 0; index < cells.Count; index++)
                    {
                        if (IsDangerCell(cells[index], pattern, parity))
                        {
                            dangerCount++;
                        }
                    }
                    if (dangerCount == 0 || dangerCount == cells.Count)
                    {
                        throw new ArgumentException(
                            $"Boss arena must contain danger and safe cells for {pattern} parity {parity}.",
                            nameof(cells));
                    }
                }
            }
        }

        private static IReadOnlyList<GridPosition> ResolveDangerCells(
            IReadOnlyList<GridPosition> cells,
            BossPatternKind pattern,
            int parity)
        {
            var danger = new List<GridPosition>();
            for (int index = 0; index < cells.Count; index++)
            {
                GridPosition position = cells[index];
                if (IsDangerCell(position, pattern, parity))
                {
                    danger.Add(position);
                }
            }
            return Array.AsReadOnly(danger.ToArray());
        }

        private static bool IsDangerCell(
            GridPosition position,
            BossPatternKind pattern,
            int parity)
        {
            switch (pattern)
            {
                case BossPatternKind.AlternatingColumns:
                    return (position.X & 1) == parity;
                case BossPatternKind.AlternatingRows:
                    return (position.Z & 1) == parity;
                case BossPatternKind.Checkerboard:
                    return ((position.X + position.Z) & 1) == parity;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pattern),
                        pattern,
                        "Unsupported boss pattern.");
            }
        }

        private static BossPatternKind SelectPattern(BossPhase phase, int sequence)
        {
            switch (phase)
            {
                case BossPhase.One:
                    return (sequence & 1) == 0
                        ? BossPatternKind.AlternatingColumns
                        : BossPatternKind.AlternatingRows;
                case BossPhase.Two:
                    return BossPatternKind.Checkerboard;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase),
                        phase,
                        "Unsupported boss phase.");
            }
        }

        private static int ComparePositions(GridPosition left, GridPosition right)
        {
            int zComparison = left.Z.CompareTo(right.Z);
            return zComparison != 0 ? zComparison : left.X.CompareTo(right.X);
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }
    }
}
