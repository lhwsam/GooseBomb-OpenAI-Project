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
            IReadOnlyList<GridPosition> dangerCells)
        {
            ActorId = actorId;
            PreviousState = previousState;
            State = state;
            Phase = phase;
            Pattern = pattern;
            PatternSequence = patternSequence;
            ScheduledAt = scheduledAt;
            DangerCells = dangerCells;
        }

        public ActorId ActorId { get; }

        public BossBattleState PreviousState { get; }

        public BossBattleState State { get; }

        public BossPhase Phase { get; }

        public BossPatternKind Pattern { get; }

        public int PatternSequence { get; }

        public TimeSpan ScheduledAt { get; }

        public IReadOnlyList<GridPosition> DangerCells { get; }

        public bool AttackResolved =>
            PreviousState == BossBattleState.Telegraph && State == BossBattleState.Execute;

        public bool BecameVulnerable => State == BossBattleState.Recovery;
    }
}
