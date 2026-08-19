using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public readonly struct BossPatternTransition
    {
        internal BossPatternTransition(
            ActorId actorId,
            BossBattleState previousState,
            BossBattleState state,
            BossPhase phase,
            BossPatternKind pattern,
            int patternSequence,
            TimeSpan scheduledAt,
            IReadOnlyList<GridPosition> dangerCells,
            BossBombAttackPlan attackPlan,
            GridPosition bossPosition,
            GridPosition nextBossPosition,
            IReadOnlyList<EnemyMovementStep> movements,
            bool movementBlocked)
        {
            ActorId = actorId;
            PreviousState = previousState;
            State = state;
            Phase = phase;
            Pattern = pattern;
            PatternSequence = patternSequence;
            ScheduledAt = scheduledAt;
            DangerCells = dangerCells;
            AttackPlan = attackPlan;
            BossPosition = bossPosition;
            NextBossPosition = nextBossPosition;
            Movements = movements ?? throw new ArgumentNullException(nameof(movements));
            MovementBlocked = movementBlocked;
        }

        public ActorId ActorId { get; }

        public BossBattleState PreviousState { get; }

        public BossBattleState State { get; }

        public BossPhase Phase { get; }

        public BossPatternKind Pattern { get; }

        public int PatternSequence { get; }

        public TimeSpan ScheduledAt { get; }

        public IReadOnlyList<GridPosition> DangerCells { get; }

        public BossBombAttackPlan AttackPlan { get; }

        public GridPosition BossPosition { get; }

        public GridPosition NextBossPosition { get; }

        public IReadOnlyList<EnemyMovementStep> Movements { get; }

        public EnemyMovementStep Movement =>
            Movements.Count > 0 ? Movements[Movements.Count - 1] : default;

        public bool BossMoved => Movements.Count > 0;

        public bool MovementBlocked { get; }

        public bool AttackResolved =>
            PreviousState == BossBattleState.Telegraph && State == BossBattleState.Execute;

        public bool BeganTelegraph =>
            PreviousState == BossBattleState.Recovery && State == BossBattleState.Telegraph;
    }
}
