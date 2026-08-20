using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BombSwap.Core
{
    public sealed class BossBombAttackPlan
    {
        internal BossBombAttackPlan(
            BossPatternKind pattern,
            List<BossBombPlacement> placements,
            List<GridPosition> dangerCells)
        {
            Pattern = pattern;
            Placements = new ReadOnlyCollection<BossBombPlacement>(placements.ToArray());
            DangerCells = new ReadOnlyCollection<GridPosition>(dangerCells.ToArray());
        }

        public BossPatternKind Pattern { get; }

        public IReadOnlyList<BossBombPlacement> Placements { get; }

        public IReadOnlyList<GridPosition> DangerCells { get; }

        public bool HasPlacements => Placements.Count > 0;

        internal static BossBombAttackPlan Empty(BossPatternKind pattern)
        {
            return new BossBombAttackPlan(
                pattern,
                new List<BossBombPlacement>(),
                new List<GridPosition>());
        }
    }
}
