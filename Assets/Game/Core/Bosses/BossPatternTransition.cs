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
            GridPosition bossPosition,
            GridPosition nextBossPosition,
            EnemyMovementStep movement,
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
            BossPosition = bossPosition;
            NextBossPosition = nextBossPosition;
            Movement = movement;
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

        public GridPosition BossPosition { get; }

        public GridPosition NextBossPosition { get; }

        public EnemyMovementStep Movement { get; }

        public bool BossMoved => Movement.ActorId.IsValid;

        public bool MovementBlocked { get; }

        public bool AttackResolved =>
            PreviousState == BossBattleState.Telegraph && State == BossBattleState.Execute;

        public bool BecameVulnerable => State == BossBattleState.Recovery;
    }
}
