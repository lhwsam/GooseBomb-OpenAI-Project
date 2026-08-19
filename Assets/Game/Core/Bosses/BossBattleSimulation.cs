using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class BossBattleSimulation
    {
        private static readonly CardinalDirection[] DirectionPriority =
        {
            CardinalDirection.North,
            CardinalDirection.East,
            CardinalDirection.South,
            CardinalDirection.West,
        };

        private static readonly IReadOnlyList<GridPosition> NoDangerCells =
            Array.AsReadOnly(Array.Empty<GridPosition>());
        private static readonly IReadOnlyList<EnemyMovementStep> NoMovements =
            Array.AsReadOnly(Array.Empty<EnemyMovementStep>());

        private readonly struct SequenceStep
        {
            public SequenceStep(BossPatternKind pattern, int variant = 0, int row = 0)
            {
                Pattern = pattern;
                Variant = variant;
                Row = row;
            }

            public BossPatternKind Pattern { get; }
            public int Variant { get; }
            public int Row { get; }
        }

        private readonly GridState grid;
        private readonly IGameClock clock;
        private readonly EnemyHealthSimulation health;
        private readonly GridPosition centerPosition;
        private readonly GridPosition[] arenaCells;
        private readonly HashSet<GridPosition> arenaSet;
        private readonly GridPosition[] throwAnchors;
        private readonly GridPosition[] summonAnchors;
        private readonly int[] parityRows;
        private readonly List<SequenceStep> sequence = new List<SequenceStep>(32);
        private readonly List<GridPosition> plannedMovement = new List<GridPosition>(8);
        private readonly List<EnemyMovementStep> movementResults =
            new List<EnemyMovementStep>(8);
        private readonly Dictionary<GridPosition, int> pathDistances =
            new Dictionary<GridPosition, int>();
        private readonly Dictionary<GridPosition, GridPosition> pathPrevious =
            new Dictionary<GridPosition, GridPosition>();
        private readonly Queue<GridPosition> pathFrontier = new Queue<GridPosition>();
        private TimeSpan lastObservedTime;
        private TimeSpan stateEndsAt;
        private int sequenceIndex;
        private int phaseOneCycleIndex;
        private int phaseTwoCycleIndex;
        private bool phaseTwoPending;
        private bool phaseTwoTransitionPlayed;
        private bool lastStandPending;
        private bool lastStandPlayed;
        private bool selfDestructResolved;

        public BossBattleSimulation(
            GridState grid,
            IGameClock clock,
            BossBattleDefinition definition,
            ActorId actorId,
            ActorId playerActorId,
            GridPosition bossPosition,
            IReadOnlyList<GridPosition> playableArenaCells,
            IReadOnlyList<GridPosition> authoredThrowAnchors,
            IReadOnlyList<GridPosition> authoredSummonAnchors)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Boss actor ID must be valid.", nameof(actorId));
            }
            if (!playerActorId.IsValid || playerActorId == actorId)
            {
                throw new ArgumentException(
                    "Boss battle requires a distinct valid player actor ID.",
                    nameof(playerActorId));
            }
            if (clock.Now < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(clock));
            }
            if (!grid.TryGetActorPosition(playerActorId, out _))
            {
                throw new ArgumentException(
                    "Player actor must occupy the grid before the boss battle starts.",
                    nameof(playerActorId));
            }

            arenaCells = CopyAndValidateArena(playableArenaCells, bossPosition);
            arenaSet = new HashSet<GridPosition>(arenaCells);
            throwAnchors = CopyAndValidateThrowAnchors(authoredThrowAnchors);
            summonAnchors = CopyAndValidateSummonAnchors(authoredSummonAnchors);
            parityRows = CollectParityRows(arenaCells);
            if (!grid.TryAddActor(actorId, bossPosition))
            {
                throw new InvalidOperationException(
                    $"Boss cannot occupy the starting cell {bossPosition}.");
            }

            ActorId = actorId;
            PlayerActorId = playerActorId;
            BossPosition = bossPosition;
            centerPosition = bossPosition;
            health = new EnemyHealthSimulation(actorId, definition.MaxHealth);
            lastObservedTime = clock.Now;
            Phase = BossPhase.One;
            PatternSequence = -1;
            BuildPhaseOneSequence(clock.Now);
        }

        public BossBattleDefinition Definition { get; }
        public ActorId ActorId { get; }
        public ActorId PlayerActorId { get; }
        public GridPosition BossPosition { get; private set; }
        public GridPosition NextBossPosition =>
            plannedMovement.Count > 0
                ? plannedMovement[plannedMovement.Count - 1]
                : BossPosition;
        public BossBattleState State { get; private set; }
        public BossPhase Phase { get; private set; }
        public BossPatternKind CurrentPattern { get; private set; }
        public int PatternSequence { get; private set; }
        public IReadOnlyList<GridPosition> CurrentDangerCells { get; private set; }
        public BossBombAttackPlan CurrentAttackPlan { get; private set; }
        public TimeSpan StateEndsAt => stateEndsAt;
        public int MaxHealth => health.MaxHealth;
        public int CurrentHealth => health.CurrentHealth;
        public bool IsDead => State == BossBattleState.Defeated;
        public bool IsWaitingForSelfDestruct =>
            CurrentPattern == BossPatternKind.WaitForSelfDestruct &&
            State != BossBattleState.Defeated;
        public bool IsHeavyAttackActive =>
            CurrentPattern == BossPatternKind.FixedCharge &&
            (State == BossBattleState.Telegraph || State == BossBattleState.Execute);

        public bool TryAdvance(out BossPatternTransition transition)
        {
            TimeSpan now = ObserveTime();
            transition = default;
            if (IsDead || now < stateEndsAt)
            {
                return false;
            }
            if (CurrentPattern == BossPatternKind.WaitForSelfDestruct &&
                State == BossBattleState.Telegraph &&
                !selfDestructResolved)
            {
                return false;
            }

            TimeSpan scheduledAt = stateEndsAt;
            BossBattleState previous = State;
            movementResults.Clear();
            bool movementBlocked = false;
            switch (State)
            {
                case BossBattleState.Telegraph:
                    movementBlocked = !ExecutePlannedMovement();
                    State = BossBattleState.Execute;
                    stateEndsAt = AddWithSaturation(
                        scheduledAt,
                        Definition.GetTimings(Phase, CurrentPattern).ExecuteDuration);
                    break;
                case BossBattleState.Execute:
                    State = BossBattleState.Recovery;
                    CurrentDangerCells = NoDangerCells;
                    stateEndsAt = AddWithSaturation(
                        scheduledAt,
                        Definition.GetTimings(Phase, CurrentPattern).RecoveryDuration);
                    break;
                case BossBattleState.Recovery:
                    BeginNextStep(scheduledAt);
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
                CurrentDangerCells,
                CurrentAttackPlan,
                BossPosition,
                NextBossPosition,
                movementResults.Count == 0
                    ? NoMovements
                    : Array.AsReadOnly(movementResults.ToArray()),
                movementBlocked);
            return true;
        }

        public BossDamageResult ApplyExplosion(BombId explosionId, int damage)
        {
            ObserveTime();
            if (!explosionId.IsValid)
            {
                throw new ArgumentException("Explosion ID must be valid.", nameof(explosionId));
            }
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }
            if (IsDead)
            {
                return CreateIgnoredDamage(
                    explosionId,
                    damage,
                    BossDamageSource.PlayerBomb,
                    BossDamageStatus.IgnoredDefeated);
            }
            return ApplyDamage(
                explosionId,
                damage,
                BossDamageSource.PlayerBomb);
        }

        public BossDamageResult ApplySelfDestructExplosion(BombId explosionId, int damage)
        {
            ObserveTime();
            if (!explosionId.IsValid)
            {
                throw new ArgumentException("Explosion ID must be valid.", nameof(explosionId));
            }
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }
            if (IsDead)
            {
                return CreateIgnoredDamage(
                    explosionId,
                    damage,
                    BossDamageSource.SelfDestruct,
                    BossDamageStatus.IgnoredDefeated);
            }

            return ApplyDamage(
                explosionId,
                damage,
                BossDamageSource.SelfDestruct);
        }

        public void NotifySelfDestructResolved()
        {
            TimeSpan now = ObserveTime();
            selfDestructResolved = true;
            if (CurrentPattern == BossPatternKind.WaitForSelfDestruct &&
                State == BossBattleState.Telegraph)
            {
                stateEndsAt = now;
            }
        }

        private BossDamageResult ApplyDamage(
            BombId explosionId,
            int damage,
            BossDamageSource source)
        {
            EnemyDamageResult enemyDamage = health.ApplyExplosionDamage(explosionId, damage);
            if (!enemyDamage.WasApplied)
            {
                BossDamageStatus status =
                    enemyDamage.Status == EnemyDamageStatus.IgnoredDuplicateExplosion
                        ? BossDamageStatus.IgnoredDuplicateExplosion
                        : BossDamageStatus.IgnoredDefeated;
                return new BossDamageResult(
                    ActorId,
                    explosionId,
                    damage,
                    enemyDamage.PreviousHealth,
                    enemyDamage.CurrentHealth,
                    Phase,
                    source,
                    status);
            }

            ReserveHealthTransitions();
            var result = new BossDamageResult(
                ActorId,
                explosionId,
                damage,
                enemyDamage.PreviousHealth,
                enemyDamage.CurrentHealth,
                Phase,
                source,
                BossDamageStatus.Applied);
            if (!result.WasFatal)
            {
                return result;
            }

            State = BossBattleState.Defeated;
            stateEndsAt = TimeSpan.Zero;
            CurrentDangerCells = NoDangerCells;
            CurrentAttackPlan = BossBombAttackPlan.Empty(CurrentPattern);
            plannedMovement.Clear();
            if (!grid.TryRemoveActor(ActorId))
            {
                throw new InvalidOperationException(
                    "Defeated boss no longer occupied its authoritative grid cell.");
            }
            return result;
        }

        private void ReserveHealthTransitions()
        {
            if (!phaseTwoTransitionPlayed &&
                CurrentHealth <= Definition.PhaseTwoHealthThreshold)
            {
                phaseTwoPending = true;
            }
            if (phaseTwoTransitionPlayed && !lastStandPlayed &&
                CurrentHealth <= Definition.LastStandHealthThreshold)
            {
                lastStandPending = true;
            }
        }

        private BossDamageResult CreateIgnoredDamage(
            BombId explosionId,
            int damage,
            BossDamageSource source,
            BossDamageStatus status)
        {
            return new BossDamageResult(
                ActorId,
                explosionId,
                damage,
                CurrentHealth,
                CurrentHealth,
                Phase,
                source,
                status);
        }

        private void BeginNextStep(TimeSpan scheduledAt)
        {
            sequenceIndex++;
            if (sequenceIndex >= sequence.Count)
            {
                if (lastStandPending && !lastStandPlayed)
                {
                    BuildLastStandSequence(scheduledAt);
                }
                else if (phaseTwoPending && !phaseTwoTransitionPlayed)
                {
                    BuildPhaseTwoFirstSequence(scheduledAt);
                }
                else if (Phase == BossPhase.One)
                {
                    BuildPhaseOneSequence(scheduledAt);
                }
                else
                {
                    BuildPhaseTwoRepeatSequence(scheduledAt);
                }
                return;
            }

            BeginCurrentStep(scheduledAt);
        }

        private void BuildPhaseOneSequence(TimeSpan scheduledAt)
        {
            Phase = BossPhase.One;
            sequence.Clear();
            AddRepeated(BossPatternKind.LimitedChase, Definition.Tuning.PhaseOneChaseCount);
            sequence.Add(new SequenceStep(BossPatternKind.FixedCharge));
            sequence.Add(new SequenceStep(BossPatternKind.ReturnToCenter));
            sequence.Add(new SequenceStep(BossPatternKind.BombVolley, 3));
            AddParityPass(phaseOneCycleIndex & 1, (phaseOneCycleIndex & 1) == 0);
            sequence.Add(new SequenceStep(BossPatternKind.Overheat));
            phaseOneCycleIndex++;
            StartSequence(scheduledAt);
        }

        private void BuildPhaseTwoFirstSequence(TimeSpan scheduledAt)
        {
            Phase = BossPhase.Two;
            phaseTwoTransitionPlayed = true;
            phaseTwoPending = false;
            selfDestructResolved = false;
            sequence.Clear();
            sequence.Add(new SequenceStep(BossPatternKind.ReturnToCenter));
            sequence.Add(new SequenceStep(BossPatternKind.PhaseTransition));
            sequence.Add(new SequenceStep(BossPatternKind.SummonSelfDestruct));
            AddRepeated(BossPatternKind.LimitedChase, Definition.Tuning.PhaseTwoChaseCount);
            sequence.Add(new SequenceStep(BossPatternKind.FixedCharge));
            sequence.Add(new SequenceStep(BossPatternKind.WaitForSelfDestruct));
            sequence.Add(new SequenceStep(BossPatternKind.ReturnToCenter));
            sequence.Add(new SequenceStep(BossPatternKind.BombVolley, 4));
            int firstParity = phaseTwoCycleIndex & 1;
            AddParityPass(firstParity, true);
            AddParityPass(1 - firstParity, false);
            sequence.Add(new SequenceStep(BossPatternKind.Overheat));
            phaseTwoCycleIndex++;
            StartSequence(scheduledAt);
        }

        private void BuildPhaseTwoRepeatSequence(TimeSpan scheduledAt)
        {
            Phase = BossPhase.Two;
            sequence.Clear();
            AddRepeated(BossPatternKind.LimitedChase, Definition.Tuning.PhaseTwoChaseCount);
            sequence.Add(new SequenceStep(BossPatternKind.FixedCharge));
            sequence.Add(new SequenceStep(BossPatternKind.ReturnToCenter));
            sequence.Add(new SequenceStep(BossPatternKind.BombVolley, 4));
            int firstParity = phaseTwoCycleIndex & 1;
            AddParityPass(firstParity, true);
            AddParityPass(1 - firstParity, false);
            sequence.Add(new SequenceStep(BossPatternKind.Overheat));
            phaseTwoCycleIndex++;
            StartSequence(scheduledAt);
        }

        private void BuildLastStandSequence(TimeSpan scheduledAt)
        {
            Phase = BossPhase.LastStand;
            lastStandPlayed = true;
            lastStandPending = false;
            sequence.Clear();
            AddRepeated(BossPatternKind.LimitedChase, Definition.Tuning.LastStandChaseCount);
            sequence.Add(new SequenceStep(BossPatternKind.FixedCharge));
            sequence.Add(new SequenceStep(BossPatternKind.ReturnToCenter));
            sequence.Add(new SequenceStep(BossPatternKind.LastStandBombChain, 4));
            int firstParity = phaseTwoCycleIndex & 1;
            AddParityPass(firstParity, true);
            AddParityPass(1 - firstParity, false);
            sequence.Add(new SequenceStep(BossPatternKind.Overheat));
            StartSequence(scheduledAt);
        }

        private void AddRepeated(BossPatternKind pattern, int count)
        {
            for (int index = 0; index < count; index++)
            {
                sequence.Add(new SequenceStep(pattern));
            }
        }

        private void AddParityPass(int parity, bool forward)
        {
            if (forward)
            {
                for (int index = 0; index < parityRows.Length; index++)
                {
                    sequence.Add(new SequenceStep(
                        BossPatternKind.ParityWave,
                        parity,
                        parityRows[index]));
                }
                return;
            }

            for (int index = parityRows.Length - 1; index >= 0; index--)
            {
                sequence.Add(new SequenceStep(
                    BossPatternKind.ParityWave,
                    parity,
                    parityRows[index]));
            }
        }

        private void StartSequence(TimeSpan scheduledAt)
        {
            sequenceIndex = 0;
            BeginCurrentStep(scheduledAt);
        }

        private void BeginCurrentStep(TimeSpan scheduledAt)
        {
            SequenceStep step = sequence[sequenceIndex];
            PatternSequence++;
            CurrentPattern = step.Pattern;
            State = BossBattleState.Telegraph;
            plannedMovement.Clear();
            CurrentDangerCells = NoDangerCells;
            CurrentAttackPlan = BossBombAttackPlan.Empty(CurrentPattern);
            switch (CurrentPattern)
            {
                case BossPatternKind.LimitedChase:
                    PlanChaseStep();
                    break;
                case BossPatternKind.FixedCharge:
                    PlanFixedCharge();
                    break;
                case BossPatternKind.ReturnToCenter:
                    PlanReturnToCenter();
                    break;
                case BossPatternKind.SummonSelfDestruct:
                    PlanSelfDestructSummon();
                    break;
                case BossPatternKind.BombVolley:
                    CurrentAttackPlan = CreateVolleyPlan(step.Variant);
                    CurrentDangerCells = CurrentAttackPlan.DangerCells;
                    break;
                case BossPatternKind.LastStandBombChain:
                    CurrentAttackPlan = CreateLastStandChainPlan();
                    CurrentDangerCells = CurrentAttackPlan.DangerCells;
                    break;
                case BossPatternKind.ParityWave:
                    CurrentDangerCells = CreateParityDangerCells(step.Variant, step.Row);
                    break;
            }

            stateEndsAt = AddWithSaturation(
                scheduledAt,
                Definition.GetTimings(Phase, CurrentPattern).TelegraphDuration);
        }

        private void PlanChaseStep()
        {
            if (!grid.TryGetActorPosition(PlayerActorId, out GridPosition target))
            {
                return;
            }
            if (TryFindNextStep(BossPosition, target, out GridPosition next))
            {
                plannedMovement.Add(next);
            }
        }

        private void PlanFixedCharge()
        {
            if (!grid.TryGetActorPosition(PlayerActorId, out GridPosition target))
            {
                return;
            }

            CardinalDirection direction = ResolveChargeDirection(BossPosition, target);
            var danger = new List<GridPosition>();
            GridPosition current = BossPosition;
            for (int index = 0; index < Definition.Tuning.ChargeDistance; index++)
            {
                GridPosition next = GetTarget(current, direction);
                GridCellState cell = grid.GetCell(next);
                if (!arenaSet.Contains(next) || !cell.IsWalkableTerrain)
                {
                    break;
                }

                danger.Add(next);
                if (cell.HasActor)
                {
                    break;
                }
                plannedMovement.Add(next);
                current = next;
            }
            CurrentDangerCells = Array.AsReadOnly(danger.ToArray());
        }

        private void PlanReturnToCenter()
        {
            if (BossPosition == centerPosition)
            {
                return;
            }
            BuildPath(BossPosition, centerPosition);
            if (!pathDistances.ContainsKey(centerPosition))
            {
                return;
            }

            var reverse = new List<GridPosition>();
            GridPosition current = centerPosition;
            while (current != BossPosition)
            {
                reverse.Add(current);
                current = pathPrevious[current];
            }
            for (int index = reverse.Count - 1; index >= 0; index--)
            {
                plannedMovement.Add(reverse[index]);
            }
        }

        private void PlanSelfDestructSummon()
        {
            if (!grid.TryGetActorPosition(PlayerActorId, out GridPosition player))
            {
                return;
            }

            GridPosition selected = default;
            long selectedDistance = long.MinValue;
            bool found = false;
            for (int index = 0; index < summonAnchors.Length; index++)
            {
                GridPosition candidate = summonAnchors[index];
                GridCellState cell = grid.GetCell(candidate);
                if (cell.HasActor || cell.HasBomb)
                {
                    continue;
                }

                long distance = ManhattanDistance(candidate, player);
                if (!found || distance > selectedDistance ||
                    (distance == selectedDistance && ComparePositions(candidate, selected) < 0))
                {
                    selected = candidate;
                    selectedDistance = distance;
                    found = true;
                }
            }

            CurrentDangerCells = found
                ? Array.AsReadOnly(new[] { selected })
                : NoDangerCells;
        }

        private bool ExecutePlannedMovement()
        {
            if (plannedMovement.Count == 0)
            {
                return true;
            }

            bool completed = true;
            for (int index = 0; index < plannedMovement.Count; index++)
            {
                GridPosition from = BossPosition;
                GridPosition to = plannedMovement[index];
                if (!grid.TryMoveActorAllowingBombOverlap(ActorId, to))
                {
                    completed = false;
                    break;
                }
                BossPosition = to;
                movementResults.Add(new EnemyMovementStep(
                    ActorId,
                    from,
                    to,
                    ResolveDirection(from, to)));
            }
            return completed;
        }

        private BossBombAttackPlan CreateVolleyPlan(int bombCount)
        {
            var placements = new List<BossBombPlacement>(bombCount);
            if (!grid.TryGetActorPosition(PlayerActorId, out GridPosition player))
            {
                return BossBombAttackPlan.Empty(BossPatternKind.BombVolley);
            }

            GridPosition[] candidates = SortAvailableAnchorsByDistance(player);
            if (bombCount <= 3)
            {
                for (int index = 0; index < candidates.Length && placements.Count < bombCount; index++)
                {
                    AddPlacement(placements, Definition.ThrowBombDefinition, candidates[index]);
                }
            }
            else if (candidates.Length > 0)
            {
                GridPosition first = candidates[0];
                GridPosition second = SelectSeparatedAnchor(first, candidates);
                AddPlacement(placements, Definition.ThrowBombDefinition, first);
                TryAddChainPlacement(placements, first);
                if (second != first)
                {
                    AddPlacement(placements, Definition.ThrowBombDefinition, second);
                    TryAddChainPlacement(placements, second);
                }
            }

            return CreateAttackPlan(BossPatternKind.BombVolley, placements);
        }

        private BossBombAttackPlan CreateLastStandChainPlan()
        {
            var placements = new List<BossBombPlacement>(4);
            if (!grid.TryGetActorPosition(PlayerActorId, out GridPosition player))
            {
                return BossBombAttackPlan.Empty(BossPatternKind.LastStandBombChain);
            }

            GridPosition[] candidates = SortAvailableAnchorsByDistance(player);
            if (candidates.Length == 0)
            {
                return BossBombAttackPlan.Empty(BossPatternKind.LastStandBombChain);
            }
            GridPosition first = candidates[candidates.Length - 1];
            GridPosition second = SelectSeparatedAnchor(first, candidates);
            AddPlacement(placements, Definition.ChainBombDefinition, first);
            if (second != first)
            {
                AddPlacement(placements, Definition.ChainBombDefinition, second);
            }
            TryAddChainPlacement(placements, first, Definition.ChainBombDefinition);
            if (second != first)
            {
                TryAddChainPlacement(placements, second, Definition.ChainBombDefinition);
            }
            return CreateAttackPlan(BossPatternKind.LastStandBombChain, placements);
        }

        private void TryAddChainPlacement(
            List<BossBombPlacement> placements,
            GridPosition primary,
            BombDefinition definition = null)
        {
            GridPosition chain = MoveOneCellToward(primary, centerPosition);
            if (!arenaSet.Contains(chain) ||
                !grid.GetCell(chain).IsWalkableTerrain ||
                grid.GetCell(chain).HasBomb ||
                ContainsPlacement(placements, chain))
            {
                return;
            }
            AddPlacement(
                placements,
                definition ?? Definition.ChainBombDefinition,
                chain);
        }

        private void AddPlacement(
            List<BossBombPlacement> placements,
            BombDefinition definition,
            GridPosition position)
        {
            if (ContainsPlacement(placements, position))
            {
                return;
            }
            TimeSpan offset = TimeSpan.FromTicks(
                Definition.Tuning.BombThrowInterval.Ticks * placements.Count);
            placements.Add(new BossBombPlacement(
                definition,
                position,
                offset,
                Definition.Tuning.BombFlightDuration));
        }

        private BossBombAttackPlan CreateAttackPlan(
            BossPatternKind pattern,
            List<BossBombPlacement> placements)
        {
            var danger = new List<GridPosition>();
            var seen = new HashSet<GridPosition>();
            for (int index = 0; index < placements.Count; index++)
            {
                BossBombPlacement placement = placements[index];
                ExplosionResolution resolution = CrossExplosionResolver.Resolve(
                    grid,
                    placement.Position,
                    placement.Definition.Range);
                for (int cellIndex = 0; cellIndex < resolution.AffectedCells.Count; cellIndex++)
                {
                    GridPosition cell = resolution.AffectedCells[cellIndex];
                    if (seen.Add(cell))
                    {
                        danger.Add(cell);
                    }
                }
            }
            danger.Sort(ComparePositions);
            return new BossBombAttackPlan(pattern, placements, danger);
        }

        private IReadOnlyList<GridPosition> CreateParityDangerCells(int parity, int row)
        {
            var cells = new List<GridPosition>();
            for (int index = 0; index < arenaCells.Length; index++)
            {
                GridPosition cell = arenaCells[index];
                if (cell.Z == row && ((cell.X + cell.Z) & 1) == parity)
                {
                    cells.Add(cell);
                }
            }
            cells.Sort(ComparePositions);
            return Array.AsReadOnly(cells.ToArray());
        }

        private GridPosition[] SortAvailableAnchorsByDistance(GridPosition target)
        {
            var candidates = new List<GridPosition>(throwAnchors.Length);
            for (int index = 0; index < throwAnchors.Length; index++)
            {
                GridPosition anchor = throwAnchors[index];
                GridCellState cell = grid.GetCell(anchor);
                if (!cell.HasBomb && !cell.HasActor)
                {
                    candidates.Add(anchor);
                }
            }
            candidates.Sort((left, right) =>
            {
                int distance = ManhattanDistance(left, target).CompareTo(
                    ManhattanDistance(right, target));
                return distance != 0 ? distance : ComparePositions(left, right);
            });
            return candidates.ToArray();
        }

        private static GridPosition SelectSeparatedAnchor(
            GridPosition first,
            IReadOnlyList<GridPosition> candidates)
        {
            GridPosition selected = first;
            long bestDistance = -1;
            for (int index = 0; index < candidates.Count; index++)
            {
                GridPosition candidate = candidates[index];
                long distance = ManhattanDistance(first, candidate);
                if (distance > bestDistance)
                {
                    selected = candidate;
                    bestDistance = distance;
                }
            }
            return selected;
        }

        private bool TryFindNextStep(
            GridPosition from,
            GridPosition target,
            out GridPosition next)
        {
            BuildPath(from, target);
            if (!pathDistances.ContainsKey(target))
            {
                next = default;
                return false;
            }
            GridPosition current = target;
            GridPosition previous = current;
            while (current != from)
            {
                previous = current;
                current = pathPrevious[current];
            }
            next = previous;
            return next != target || !grid.GetCell(target).HasActor;
        }

        private void BuildPath(GridPosition from, GridPosition target)
        {
            pathDistances.Clear();
            pathPrevious.Clear();
            pathFrontier.Clear();
            pathDistances.Add(from, 0);
            pathFrontier.Enqueue(from);
            while (pathFrontier.Count > 0)
            {
                GridPosition current = pathFrontier.Dequeue();
                if (current == target)
                {
                    break;
                }
                for (int index = 0; index < DirectionPriority.Length; index++)
                {
                    GridPosition candidate = GetTarget(current, DirectionPriority[index]);
                    if (pathDistances.ContainsKey(candidate) ||
                        !IsPathTraversable(candidate, from, target))
                    {
                        continue;
                    }
                    pathDistances.Add(candidate, pathDistances[current] + 1);
                    pathPrevious.Add(candidate, current);
                    pathFrontier.Enqueue(candidate);
                }
            }
        }

        private bool IsPathTraversable(
            GridPosition position,
            GridPosition from,
            GridPosition target)
        {
            if (!arenaSet.Contains(position))
            {
                return false;
            }
            GridCellState cell = grid.GetCell(position);
            if (!cell.IsWalkableTerrain)
            {
                return false;
            }
            return position == from || position == target || !cell.HasActor;
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
                    throw new ArgumentException($"Duplicate boss arena cell: {position}.");
                }
                if (!grid.GetCell(position).IsWalkableTerrain)
                {
                    throw new ArgumentException(
                        $"Boss arena cell must use walkable terrain: {position}.");
                }
                copy[index] = position;
                foundBossPosition |= position == bossPosition;
            }
            if (!foundBossPosition)
            {
                throw new ArgumentException("Boss position must be part of the playable arena.");
            }
            Array.Sort(copy, ComparePositions);
            return copy;
        }

        private GridPosition[] CopyAndValidateThrowAnchors(
            IReadOnlyList<GridPosition> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Count < 4 || source.Count > 6)
            {
                throw new ArgumentException(
                    "Boss bomb attacks require four to six authored throw anchors.",
                    nameof(source));
            }

            var copy = new GridPosition[source.Count];
            var seen = new HashSet<GridPosition>();
            for (int index = 0; index < source.Count; index++)
            {
                GridPosition anchor = source[index];
                if (!seen.Add(anchor) || !arenaSet.Contains(anchor) ||
                    !grid.GetCell(anchor).IsWalkableTerrain)
                {
                    throw new ArgumentException(
                        $"Boss throw anchor must be unique playable floor: {anchor}.",
                        nameof(source));
                }
                copy[index] = anchor;
            }
            return copy;
        }

        private GridPosition[] CopyAndValidateSummonAnchors(
            IReadOnlyList<GridPosition> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Count < 2)
            {
                throw new ArgumentException(
                    "Boss battle requires at least two authored summon anchors.",
                    nameof(source));
            }

            var copy = new GridPosition[source.Count];
            var seen = new HashSet<GridPosition>();
            for (int index = 0; index < source.Count; index++)
            {
                GridPosition anchor = source[index];
                if (!seen.Add(anchor) || !arenaSet.Contains(anchor) ||
                    !grid.GetCell(anchor).IsWalkableTerrain)
                {
                    throw new ArgumentException(
                        $"Boss summon anchor must be unique playable floor: {anchor}.",
                        nameof(source));
                }
                copy[index] = anchor;
            }
            Array.Sort(copy, ComparePositions);
            return copy;
        }

        private static int[] CollectParityRows(IReadOnlyList<GridPosition> cells)
        {
            var rows = new List<int>();
            var seen = new HashSet<int>();
            for (int index = 0; index < cells.Count; index++)
            {
                if (seen.Add(cells[index].Z))
                {
                    rows.Add(cells[index].Z);
                }
            }
            rows.Sort();
            return rows.ToArray();
        }

        private static bool ContainsPlacement(
            IReadOnlyList<BossBombPlacement> placements,
            GridPosition position)
        {
            for (int index = 0; index < placements.Count; index++)
            {
                if (placements[index].Position == position)
                {
                    return true;
                }
            }
            return false;
        }

        private static CardinalDirection ResolveChargeDirection(
            GridPosition from,
            GridPosition target)
        {
            int deltaX = target.X - from.X;
            int deltaZ = target.Z - from.Z;
            if (Math.Abs(deltaX) >= Math.Abs(deltaZ) && deltaX != 0)
            {
                return deltaX > 0 ? CardinalDirection.East : CardinalDirection.West;
            }
            return deltaZ >= 0 ? CardinalDirection.North : CardinalDirection.South;
        }

        private static CardinalDirection ResolveDirection(
            GridPosition from,
            GridPosition to)
        {
            if (to.X == from.X + 1 && to.Z == from.Z)
            {
                return CardinalDirection.East;
            }
            if (to.X == from.X - 1 && to.Z == from.Z)
            {
                return CardinalDirection.West;
            }
            if (to.X == from.X && to.Z == from.Z + 1)
            {
                return CardinalDirection.North;
            }
            if (to.X == from.X && to.Z == from.Z - 1)
            {
                return CardinalDirection.South;
            }
            throw new InvalidOperationException("Boss movement must be cardinal and adjacent.");
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

        private static GridPosition MoveOneCellToward(
            GridPosition position,
            GridPosition target)
        {
            int deltaX = target.X - position.X;
            int deltaZ = target.Z - position.Z;
            if (Math.Abs(deltaX) >= Math.Abs(deltaZ) && deltaX != 0)
            {
                return position.Offset(Math.Sign(deltaX), 0);
            }
            if (deltaZ != 0)
            {
                return position.Offset(0, Math.Sign(deltaZ));
            }
            return position;
        }

        private static int ComparePositions(GridPosition left, GridPosition right)
        {
            int z = left.Z.CompareTo(right.Z);
            return z != 0 ? z : left.X.CompareTo(right.X);
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
