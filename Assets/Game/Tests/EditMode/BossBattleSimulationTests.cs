using System;
using System.Collections.Generic;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BossBattleSimulationTests
    {
        private static readonly ActorId BossActor = new ActorId(5);
        private static readonly TimeSpan PhaseOneTelegraph = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan PhaseOneExecute = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan PhaseOneRecovery = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PhaseTwoTelegraph = TimeSpan.FromMilliseconds(750);
        private static readonly TimeSpan PhaseTwoExecute = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan PhaseTwoRecovery = TimeSpan.FromSeconds(1.5);

        [Test]
        public void Definition_StoresHealthDamageThresholdAndPhaseTimings()
        {
            BossBattleDefinition definition = CreateDefinition();

            Assert.That(definition.Id, Is.EqualTo(new EnemyDefinitionId("prototype-boss")));
            Assert.That(definition.MaxHealth, Is.EqualTo(4));
            Assert.That(definition.PhaseTwoHealthThreshold, Is.EqualTo(2));
            Assert.That(definition.PatternDamage, Is.EqualTo(1));
            Assert.That(definition.PhaseOneTimings.TelegraphDuration, Is.EqualTo(PhaseOneTelegraph));
            Assert.That(definition.PhaseOneTimings.ExecuteDuration, Is.EqualTo(PhaseOneExecute));
            Assert.That(definition.PhaseOneTimings.RecoveryDuration, Is.EqualTo(PhaseOneRecovery));
            Assert.That(definition.GetTimings(BossPhase.One), Is.EqualTo(definition.PhaseOneTimings));
            Assert.That(definition.GetTimings(BossPhase.Two), Is.EqualTo(definition.PhaseTwoTimings));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                definition.GetTimings((BossPhase)99));
        }

        [Test]
        public void Definition_RejectsInvalidIdentityHealthThresholdDamageAndTimings()
        {
            BossPatternTimings phaseOne = CreateDefinition().PhaseOneTimings;
            BossPatternTimings phaseTwo = CreateDefinition().PhaseTwoTimings;
            EnemyDefinitionId id = new EnemyDefinitionId("prototype-boss");

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossPatternTimings(TimeSpan.Zero, PhaseOneExecute, PhaseOneRecovery));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossPatternTimings(PhaseOneTelegraph, TimeSpan.Zero, PhaseOneRecovery));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossPatternTimings(PhaseOneTelegraph, PhaseOneExecute, TimeSpan.Zero));
            Assert.Throws<ArgumentException>(() =>
                new BossBattleDefinition(default, 4, 2, 1, phaseOne, phaseTwo));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossBattleDefinition(id, 0, 0, 1, phaseOne, phaseTwo));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossBattleDefinition(id, 4, 0, 1, phaseOne, phaseTwo));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossBattleDefinition(id, 4, 4, 1, phaseOne, phaseTwo));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BossBattleDefinition(id, 4, 2, 0, phaseOne, phaseTwo));
        }

        [Test]
        public void Constructor_StartsWithReadableColumnTelegraphAndOwnsBossCell()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateArenaGrid();
            BossBattleSimulation boss = CreateSimulation(grid, clock);

            Assert.That(boss.State, Is.EqualTo(BossBattleState.Telegraph));
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.One));
            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.AlternatingColumns));
            Assert.That(boss.PatternSequence, Is.Zero);
            Assert.That(boss.StateEndsAt, Is.EqualTo(PhaseOneTelegraph));
            Assert.That(boss.IsVulnerable, Is.False);
            Assert.That(boss.CurrentDangerCells, Is.EqualTo(new[]
            {
                new GridPosition(-2, -2), new GridPosition(0, -2), new GridPosition(2, -2),
                new GridPosition(-2, -1), new GridPosition(0, -1), new GridPosition(2, -1),
                new GridPosition(-2, 0), new GridPosition(0, 0), new GridPosition(2, 0),
                new GridPosition(-2, 1), new GridPosition(0, 1), new GridPosition(2, 1),
                new GridPosition(-2, 2), new GridPosition(0, 2), new GridPosition(2, 2),
            }));
            Assert.That(boss.CurrentDangerCells, Is.Not.InstanceOf<GridPosition[]>());
            Assert.That(grid.TryGetActorPosition(BossActor, out GridPosition position), Is.True);
            Assert.That(position, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void Telegraph_ResolvesAtExactBoundaryWithIdenticalDangerSnapshot()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            IReadOnlyList<GridPosition> telegraphed = boss.CurrentDangerCells;

            clock.Advance(PhaseOneTelegraph - TimeSpan.FromTicks(1));
            Assert.That(boss.TryAdvance(out _), Is.False);
            clock.Advance(TimeSpan.FromTicks(1));

            Assert.That(boss.TryAdvance(out BossPatternTransition transition), Is.True);
            Assert.That(transition.PreviousState, Is.EqualTo(BossBattleState.Telegraph));
            Assert.That(transition.State, Is.EqualTo(BossBattleState.Execute));
            Assert.That(transition.Pattern, Is.EqualTo(BossPatternKind.AlternatingColumns));
            Assert.That(transition.Phase, Is.EqualTo(BossPhase.One));
            Assert.That(transition.PatternSequence, Is.Zero);
            Assert.That(transition.ScheduledAt, Is.EqualTo(PhaseOneTelegraph));
            Assert.That(transition.AttackResolved, Is.True);
            Assert.That(transition.BecameVulnerable, Is.False);
            Assert.That(transition.DangerCells, Is.SameAs(telegraphed));
            Assert.That(boss.StateEndsAt, Is.EqualTo(PhaseOneTelegraph + PhaseOneExecute));
        }

        [Test]
        public void Execute_EntersRecoveryAtExactBoundaryAndBecomesVulnerable()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            clock.Advance(PhaseOneTelegraph);
            Assert.That(boss.TryAdvance(out _), Is.True);
            clock.Advance(PhaseOneExecute);

            Assert.That(boss.TryAdvance(out BossPatternTransition transition), Is.True);
            Assert.That(transition.PreviousState, Is.EqualTo(BossBattleState.Execute));
            Assert.That(transition.State, Is.EqualTo(BossBattleState.Recovery));
            Assert.That(transition.BecameVulnerable, Is.True);
            Assert.That(transition.AttackResolved, Is.False);
            Assert.That(boss.IsVulnerable, Is.True);
            Assert.That(
                boss.StateEndsAt,
                Is.EqualTo(PhaseOneTelegraph + PhaseOneExecute + PhaseOneRecovery));
        }

        [Test]
        public void Explosion_IsIgnoredOutsideRecoveryThenAppliesOnceDuringOpening()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            BombId firstBomb = CreateBombId(1);

            BossDamageResult early = boss.ApplyExplosion(firstBomb, 1);
            Assert.That(early.Status, Is.EqualTo(BossDamageStatus.IgnoredNotVulnerable));
            Assert.That(boss.CurrentHealth, Is.EqualTo(4));

            AdvanceToRecovery(clock, boss);
            BossDamageResult applied = boss.ApplyExplosion(firstBomb, 1);
            BossDamageResult duplicate = boss.ApplyExplosion(firstBomb, 1);

            Assert.That(applied.Status, Is.EqualTo(BossDamageStatus.Applied));
            Assert.That(applied.AppliedDamage, Is.EqualTo(1));
            Assert.That(applied.WasFatal, Is.False);
            Assert.That(duplicate.Status, Is.EqualTo(BossDamageStatus.IgnoredDuplicateExplosion));
            Assert.That(boss.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void PhaseOne_AlternatesFromColumnsToRowsAtSafeTransitionPoint()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            AdvanceToRecovery(clock, boss);
            clock.Advance(PhaseOneRecovery);

            Assert.That(boss.TryAdvance(out BossPatternTransition transition), Is.True);
            Assert.That(transition.PreviousState, Is.EqualTo(BossBattleState.Recovery));
            Assert.That(transition.State, Is.EqualTo(BossBattleState.Telegraph));
            Assert.That(transition.Phase, Is.EqualTo(BossPhase.One));
            Assert.That(transition.Pattern, Is.EqualTo(BossPatternKind.AlternatingRows));
            Assert.That(transition.PatternSequence, Is.EqualTo(1));
            Assert.That(transition.DangerCells, Is.EqualTo(new[]
            {
                new GridPosition(-2, -1), new GridPosition(-1, -1),
                new GridPosition(0, -1), new GridPosition(1, -1), new GridPosition(2, -1),
                new GridPosition(-2, 1), new GridPosition(-1, 1),
                new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(2, 1),
            }));
        }

        [Test]
        public void HealthThreshold_ChangesToCheckerboardOnlyAfterRecoveryEnds()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            AdvanceToRecovery(clock, boss);
            boss.ApplyExplosion(CreateBombId(1), 1);
            boss.ApplyExplosion(CreateBombId(2), 1);

            Assert.That(boss.CurrentHealth, Is.EqualTo(2));
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.One));
            clock.Advance(PhaseOneRecovery);
            Assert.That(boss.TryAdvance(out BossPatternTransition transition), Is.True);

            Assert.That(transition.Phase, Is.EqualTo(BossPhase.Two));
            Assert.That(transition.Pattern, Is.EqualTo(BossPatternKind.Checkerboard));
            Assert.That(transition.PatternSequence, Is.EqualTo(1));
            Assert.That(boss.StateEndsAt, Is.EqualTo(
                PhaseOneTelegraph + PhaseOneExecute + PhaseOneRecovery + PhaseTwoTelegraph));
            Assert.That(transition.DangerCells, Is.EqualTo(new[]
            {
                new GridPosition(-1, -2), new GridPosition(1, -2),
                new GridPosition(-2, -1), new GridPosition(0, -1), new GridPosition(2, -1),
                new GridPosition(-1, 0), new GridPosition(1, 0),
                new GridPosition(-2, 1), new GridPosition(0, 1), new GridPosition(2, 1),
                new GridPosition(-1, 2), new GridPosition(1, 2),
            }));
        }

        [Test]
        public void FatalRecoveryHit_RemovesBossAndCannotClearTwice()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateArenaGrid();
            BossBattleSimulation boss = CreateSimulation(grid, clock);
            AdvanceToRecovery(clock, boss);

            BossDamageResult fatal = default;
            for (int index = 1; index <= 4; index++)
            {
                fatal = boss.ApplyExplosion(CreateBombId(index), 1);
            }

            Assert.That(fatal.WasFatal, Is.True);
            Assert.That(boss.State, Is.EqualTo(BossBattleState.Defeated));
            Assert.That(boss.IsDead, Is.True);
            Assert.That(boss.IsVulnerable, Is.False);
            Assert.That(boss.CurrentDangerCells, Is.Empty);
            Assert.That(grid.TryGetActorPosition(BossActor, out _), Is.False);
            Assert.That(boss.TryAdvance(out _), Is.False);

            BossDamageResult afterDeath = boss.ApplyExplosion(CreateBombId(5), 1);
            Assert.That(afterDeath.Status, Is.EqualTo(BossDamageStatus.IgnoredDefeated));
            Assert.That(afterDeath.WasFatal, Is.False);
            Assert.That(boss.CurrentHealth, Is.Zero);
        }

        [Test]
        public void LargeClockAdvance_PreservesScheduledBoundariesAcrossTransitions()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            clock.Advance(TimeSpan.FromSeconds(10));

            Assert.That(boss.TryAdvance(out BossPatternTransition execute), Is.True);
            Assert.That(execute.ScheduledAt, Is.EqualTo(PhaseOneTelegraph));
            Assert.That(boss.TryAdvance(out BossPatternTransition recovery), Is.True);
            Assert.That(recovery.ScheduledAt, Is.EqualTo(PhaseOneTelegraph + PhaseOneExecute));
            Assert.That(boss.TryAdvance(out BossPatternTransition nextTelegraph), Is.True);
            Assert.That(
                nextTelegraph.ScheduledAt,
                Is.EqualTo(PhaseOneTelegraph + PhaseOneExecute + PhaseOneRecovery));
            Assert.That(nextTelegraph.Pattern, Is.EqualTo(BossPatternKind.AlternatingRows));
        }

        [Test]
        public void ConstructorAndAdvance_RejectInvalidArenaAndClockRegression()
        {
            var clock = new MutableGameClock(TimeSpan.Zero);
            GridState grid = CreateArenaGrid();
            IReadOnlyList<GridPosition> arena = CreateArenaCells();

            Assert.Throws<ArgumentException>(() =>
                new BossBattleSimulation(
                    grid,
                    clock,
                    CreateDefinition(),
                    default,
                    new GridPosition(0, 1),
                    arena));
            Assert.Throws<ArgumentException>(() =>
                new BossBattleSimulation(
                    grid,
                    clock,
                    CreateDefinition(),
                    BossActor,
                    new GridPosition(3, 3),
                    arena));
            Assert.Throws<ArgumentException>(() =>
                new BossBattleSimulation(
                    grid,
                    clock,
                    CreateDefinition(),
                    BossActor,
                    new GridPosition(0, 1),
                    new[] { new GridPosition(0, 1), new GridPosition(0, 1) }));

            BossBattleSimulation boss = CreateSimulation(grid, clock);
            clock.Now = TimeSpan.FromSeconds(2);
            Assert.That(boss.TryAdvance(out _), Is.True);
            clock.Now = TimeSpan.FromSeconds(1);
            Assert.Throws<InvalidOperationException>(() => boss.TryAdvance(out _));
        }

        private static BossBattleSimulation CreateSimulation(GridState grid, IGameClock clock)
        {
            return new BossBattleSimulation(
                grid,
                clock,
                CreateDefinition(),
                BossActor,
                new GridPosition(0, 1),
                CreateArenaCells());
        }

        private static BossBattleDefinition CreateDefinition()
        {
            return new BossBattleDefinition(
                new EnemyDefinitionId("prototype-boss"),
                4,
                2,
                1,
                new BossPatternTimings(
                    PhaseOneTelegraph,
                    PhaseOneExecute,
                    PhaseOneRecovery),
                new BossPatternTimings(
                    PhaseTwoTelegraph,
                    PhaseTwoExecute,
                    PhaseTwoRecovery));
        }

        private static GridState CreateArenaGrid()
        {
            var grid = new GridState();
            foreach (GridPosition position in CreateArenaCells())
            {
                Assert.That(grid.TrySetTerrain(position, GridTerrain.Floor), Is.True);
            }
            return grid;
        }

        private static IReadOnlyList<GridPosition> CreateArenaCells()
        {
            var cells = new List<GridPosition>();
            for (int z = -2; z <= 2; z++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    cells.Add(new GridPosition(x, z));
                }
            }
            return cells;
        }

        private static void AdvanceToRecovery(
            ManualGameClock clock,
            BossBattleSimulation boss)
        {
            clock.Advance(PhaseOneTelegraph);
            Assert.That(boss.TryAdvance(out _), Is.True);
            clock.Advance(PhaseOneExecute);
            Assert.That(boss.TryAdvance(out _), Is.True);
            Assert.That(boss.State, Is.EqualTo(BossBattleState.Recovery));
        }

        private static BombId CreateBombId(int sequence)
        {
            var grid = new GridState();
            var clock = new ManualGameClock();
            var bombs = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(100));
            var definition = new BombDefinition(
                new BombDefinitionId("boss-test"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(10),
                0);
            BombId created = default;
            for (int index = 1; index <= sequence; index++)
            {
                var position = new GridPosition(index, 0);
                grid.TrySetTerrain(position, GridTerrain.Floor);
                Assert.That(
                    bombs.TryPlaceBomb(definition, position, new ActorId(10), out created),
                    Is.True);
            }
            return created;
        }

        private sealed class MutableGameClock : IGameClock
        {
            public MutableGameClock(TimeSpan now)
            {
                Now = now;
            }

            public TimeSpan Now { get; set; }
        }
    }
}
