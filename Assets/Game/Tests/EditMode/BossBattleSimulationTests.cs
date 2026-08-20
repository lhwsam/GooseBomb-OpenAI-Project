using System;
using System.Collections.Generic;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BossBattleSimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId BossActor = new ActorId(5);
        private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(10);

        [Test]
        public void Definition_ValidatesThreePhaseAndBombContracts()
        {
            BossBattleDefinition definition = CreateDefinition();

            Assert.That(definition.MaxHealth, Is.EqualTo(10));
            Assert.That(definition.PhaseTwoHealthThreshold, Is.EqualTo(7));
            Assert.That(definition.LastStandHealthThreshold, Is.EqualTo(2));
            Assert.That(definition.Tuning.PhaseOneChaseCount, Is.EqualTo(2));
            Assert.That(definition.Tuning.PhaseTwoChaseCount, Is.EqualTo(3));
            Assert.That(
                definition.GetTimings(BossPhase.LastStand, BossPatternKind.Overheat)
                    .RecoveryDuration,
                Is.EqualTo(TimeSpan.FromMilliseconds(50)));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateDefinition(maxHealth: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateDefinition(phaseTwoThreshold: 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateDefinition(lastStandThreshold: 7));
            Assert.Throws<ArgumentException>(() =>
                new BossBattleDefinition(
                    new EnemyDefinitionId("boss"),
                    10,
                    7,
                    2,
                    1,
                    CreateTuning(),
                    CreateThrowBomb(),
                    CreateThrowBomb()));
        }

        [Test]
        public void InitialSequence_ChasesTwiceThenLocksThreeCellCharge()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateArenaGrid();
            BossBattleSimulation boss = CreateSimulation(grid, clock);

            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.LimitedChase));
            Assert.That(boss.CurrentDangerCells, Is.Empty);
            CompleteCurrentPattern(boss, clock);
            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.LimitedChase));

            GridPosition playerDestination = new GridPosition(4, 0);
            MovePlayerAlongGrid(grid, playerDestination);
            CompleteCurrentPattern(boss, clock);

            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.FixedCharge));
            Assert.That(boss.CurrentDangerCells.Count, Is.InRange(2, 3));
            IReadOnlyList<GridPosition> locked = boss.CurrentDangerCells;
            MovePlayerAlongGrid(grid, new GridPosition(-4, -3));
            Assert.That(boss.CurrentDangerCells, Is.SameAs(locked));
        }

        [Test]
        public void Charge_ExecutesCardinalPathWithoutEnteringPlayerCell()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateArenaGrid(new GridPosition(5, 1));
            BossBattleSimulation boss = CreateSimulation(grid, clock);
            AdvanceUntilPattern(boss, clock, BossPatternKind.FixedCharge);
            IReadOnlyList<GridPosition> danger = boss.CurrentDangerCells;

            AdvanceOneTransition(boss, clock, out BossPatternTransition execute);

            Assert.That(execute.AttackResolved, Is.True);
            Assert.That(execute.Movements, Has.Count.EqualTo(2));
            Assert.That(danger, Does.Contain(new GridPosition(5, 1)));
            Assert.That(boss.BossPosition, Is.EqualTo(new GridPosition(4, 1)));
            Assert.That(grid.TryGetActorPosition(PlayerActor, out GridPosition player), Is.True);
            Assert.That(player, Is.EqualTo(new GridPosition(5, 1)));
        }

        [Test]
        public void PhaseOneVolley_ReservesThreeUniqueSequentialFlights()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            AdvanceUntilPattern(boss, clock, BossPatternKind.BombVolley);

            Assert.That(boss.CurrentAttackPlan.Placements, Has.Count.EqualTo(3));
            var positions = new HashSet<GridPosition>();
            for (int index = 0; index < boss.CurrentAttackPlan.Placements.Count; index++)
            {
                BossBombPlacement placement = boss.CurrentAttackPlan.Placements[index];
                Assert.That(positions.Add(placement.Position), Is.True);
                Assert.That(placement.Definition.Id, Is.EqualTo(CreateThrowBomb().Id));
                Assert.That(
                    placement.LaunchOffset,
                    Is.EqualTo(TimeSpan.FromMilliseconds(10 * index)));
                Assert.That(placement.FlightDuration, Is.EqualTo(Tick));
            }
            Assert.That(boss.CurrentDangerCells, Is.Not.Empty);
        }

        [Test]
        public void ParityWave_AdvancesRowsAndLeavesAlternatingSafeCells()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            AdvanceUntilPattern(boss, clock, BossPatternKind.ParityWave);
            IReadOnlyList<GridPosition> first = boss.CurrentDangerCells;

            Assert.That(first, Is.Not.Empty);
            int row = first[0].Z;
            int parity = (first[0].X + first[0].Z) & 1;
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(first[index].Z, Is.EqualTo(row));
                Assert.That((first[index].X + first[index].Z) & 1, Is.EqualTo(parity));
            }
            Assert.That(first.Count, Is.LessThan(11));

            CompleteCurrentPattern(boss, clock);
            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.ParityWave));
            Assert.That(boss.CurrentDangerCells[0].Z, Is.Not.EqualTo(row));
        }

        [Test]
        public void PlayerBombs_ApplyInTelegraphExecuteAndRecovery()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);

            Assert.That(boss.State, Is.EqualTo(BossBattleState.Telegraph));
            BossDamageResult first = boss.ApplyExplosion(CreateBombId(1), 1);
            AdvanceOneTransition(boss, clock, out _);
            Assert.That(boss.State, Is.EqualTo(BossBattleState.Execute));
            BossDamageResult second = boss.ApplyExplosion(CreateBombId(2), 1);
            AdvanceOneTransition(boss, clock, out _);
            Assert.That(boss.State, Is.EqualTo(BossBattleState.Recovery));
            BossDamageResult third = boss.ApplyExplosion(CreateBombId(3), 1);
            BossDamageResult duplicate = boss.ApplyExplosion(CreateBombId(3), 1);

            Assert.That(first.WasApplied, Is.True);
            Assert.That(second.WasApplied, Is.True);
            Assert.That(third.WasApplied, Is.True);
            Assert.That(boss.CurrentHealth, Is.EqualTo(7));
            Assert.That(
                duplicate.Status,
                Is.EqualTo(BossDamageStatus.IgnoredDuplicateExplosion));
        }

        [Test]
        public void Overheat_AcceptsMoreThanTwoDistinctPlayerBombs()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            AdvanceUntilRecovery(boss, clock, BossPatternKind.Overheat);

            Assert.That(boss.ApplyExplosion(CreateBombId(1), 1).WasApplied, Is.True);
            Assert.That(boss.ApplyExplosion(CreateBombId(2), 1).WasApplied, Is.True);
            Assert.That(boss.ApplyExplosion(CreateBombId(3), 1).WasApplied, Is.True);
            Assert.That(boss.CurrentHealth, Is.EqualTo(7));
        }

        [Test]
        public void PhaseTwoTransition_IsDeferredUntilOverheatCompletes()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            DealOverheatDamage(boss, clock, 2);
            DealOverheatDamage(boss, clock, 1, completeRecovery: false);

            Assert.That(boss.CurrentHealth, Is.EqualTo(7));
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.One));
            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.Overheat));

            AdvanceOneTransition(boss, clock, out BossPatternTransition transition);
            Assert.That(transition.BeganTelegraph, Is.True);
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Two));
            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.ReturnToCenter));
        }

        [Test]
        public void PhaseTwoFirstCycle_SummonsOnceAndWaitsForResolution()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(CreateArenaGrid(), clock);
            DealOverheatDamage(boss, clock, 2);
            DealOverheatDamage(boss, clock, 1);
            AdvanceUntilPattern(boss, clock, BossPatternKind.SummonSelfDestruct);
            Assert.That(boss.CurrentDangerCells, Has.Count.EqualTo(1));
            Assert.That(
                boss.CurrentDangerCells[0],
                Is.EqualTo(new GridPosition(-3, 3)));

            AdvanceOneTransition(boss, clock, out BossPatternTransition summon);
            Assert.That(summon.AttackResolved, Is.True);
            AdvanceUntilPattern(boss, clock, BossPatternKind.WaitForSelfDestruct);
            AdvanceClockToBoundary(boss, clock);
            Assert.That(boss.TryAdvance(out _), Is.False);

            boss.NotifySelfDestructResolved();
            Assert.That(boss.TryAdvance(out BossPatternTransition resolved), Is.True);
            Assert.That(resolved.AttackResolved, Is.True);

            AdvanceUntilPattern(boss, clock, BossPatternKind.BombVolley);
            Assert.That(boss.CurrentAttackPlan.Placements, Has.Count.EqualTo(4));
            Assert.That(
                boss.CurrentAttackPlan.Placements[0].Definition.Id,
                Is.EqualTo(CreateThrowBomb().Id));
            Assert.That(
                boss.CurrentAttackPlan.Placements[1].Definition.Id,
                Is.EqualTo(CreateChainBomb().Id));
        }

        [Test]
        public void SelfDestructHit_BypassesOverheatAndReservesLastStand()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(
                CreateArenaGrid(),
                clock,
                CreateDefinition(phaseTwoThreshold: 9, lastStandThreshold: 8));
            DealOverheatDamage(boss, clock, 2);
            Assert.That(boss.Phase, Is.EqualTo(BossPhase.Two));

            BossDamageResult result = boss.ApplySelfDestructExplosion(CreateBombId(9), 1);
            Assert.That(result.WasApplied, Is.True);
            Assert.That(result.Source, Is.EqualTo(BossDamageSource.SelfDestruct));
            Assert.That(boss.CurrentHealth, Is.EqualTo(7));

            AdvanceUntilPhase(boss, clock, BossPhase.LastStand);
            Assert.That(boss.CurrentPattern, Is.EqualTo(BossPatternKind.LimitedChase));
            AdvanceUntilPattern(boss, clock, BossPatternKind.LastStandBombChain);
            Assert.That(boss.CurrentAttackPlan.Placements, Has.Count.EqualTo(4));
        }

        [Test]
        public void PlayerExplosion_AppliesOutsideOverheat()
        {
            BossBattleSimulation boss = CreateSimulation(
                CreateArenaGrid(),
                new ManualGameClock());

            BossDamageResult result = boss.ApplyExplosion(CreateBombId(1), 1);

            Assert.That(result.WasApplied, Is.True);
            Assert.That(boss.CurrentHealth, Is.EqualTo(9));
        }

        [Test]
        public void FatalHit_RemovesBossAndStopsTransitions()
        {
            var clock = new ManualGameClock();
            BossBattleSimulation boss = CreateSimulation(
                CreateArenaGrid(),
                clock,
                CreateDefinition(
                    maxHealth: 3,
                    phaseTwoThreshold: 2,
                    lastStandThreshold: 1));
            AdvanceUntilRecovery(boss, clock, BossPatternKind.Overheat);
            BossDamageResult result = boss.ApplyExplosion(CreateBombId(1), 3);

            Assert.That(result.WasFatal, Is.True);
            Assert.That(boss.IsDead, Is.True);
            Assert.That(boss.CurrentDangerCells, Is.Empty);
            Assert.That(boss.TryAdvance(out _), Is.False);
        }

        [Test]
        public void Constructor_RejectsTooFewThrowAnchorsAndClockRegression()
        {
            var clock = new MutableGameClock(TimeSpan.Zero);
            GridState grid = CreateArenaGrid();
            Assert.Throws<ArgumentException>(() =>
                new BossBattleSimulation(
                    grid,
                    clock,
                    CreateDefinition(),
                    BossActor,
                    PlayerActor,
                    new GridPosition(0, 1),
                    CreateArenaCells(),
                    new[]
                    {
                        new GridPosition(-4, -2),
                        new GridPosition(4, -2),
                    },
                    CreateSummonAnchors()));

            BossBattleSimulation boss = CreateSimulation(grid, clock);
            clock.Now = Tick;
            Assert.That(boss.TryAdvance(out _), Is.True);
            clock.Now = TimeSpan.Zero;
            Assert.Throws<InvalidOperationException>(() => boss.TryAdvance(out _));
        }

        private static BossBattleSimulation CreateSimulation(
            GridState grid,
            IGameClock clock,
            BossBattleDefinition definition = null)
        {
            return new BossBattleSimulation(
                grid,
                clock,
                definition ?? CreateDefinition(),
                BossActor,
                PlayerActor,
                new GridPosition(0, 1),
                CreateArenaCells(),
                CreateThrowAnchors(),
                CreateSummonAnchors());
        }

        private static BossBattleDefinition CreateDefinition(
            int maxHealth = 10,
            int phaseTwoThreshold = 7,
            int lastStandThreshold = 2)
        {
            return new BossBattleDefinition(
                new EnemyDefinitionId("prototype-boss"),
                maxHealth,
                phaseTwoThreshold,
                lastStandThreshold,
                1,
                CreateTuning(),
                CreateThrowBomb(),
                CreateChainBomb());
        }

        private static BossPatternTuning CreateTuning()
        {
            var timings = new BossPatternTimings(Tick, Tick, Tick);
            return new BossPatternTuning(
                timings,
                timings,
                timings,
                timings,
                timings,
                timings,
                timings,
                timings,
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(50),
                2,
                3,
                2,
                3,
                Tick,
                Tick,
                TimeSpan.FromMilliseconds(45));
        }

        private static BombDefinition CreateThrowBomb()
        {
            return new BombDefinition(
                new BombDefinitionId("boss-throw"),
                BombExplosionShape.Cross,
                TimeSpan.FromMilliseconds(100),
                2);
        }

        private static BombDefinition CreateChainBomb()
        {
            return new BombDefinition(
                new BombDefinitionId("boss-chain"),
                BombExplosionShape.Cross,
                TimeSpan.FromMilliseconds(200),
                2);
        }

        private static GridState CreateArenaGrid(GridPosition? playerPosition = null)
        {
            var grid = new GridState();
            IReadOnlyList<GridPosition> cells = CreateArenaCells();
            for (int index = 0; index < cells.Count; index++)
            {
                Assert.That(grid.TrySetTerrain(cells[index], GridTerrain.Floor), Is.True);
            }
            Assert.That(
                grid.TryAddActor(PlayerActor, playerPosition ?? new GridPosition(0, -3)),
                Is.True);
            return grid;
        }

        private static IReadOnlyList<GridPosition> CreateArenaCells()
        {
            var cells = new List<GridPosition>();
            for (int z = -4; z <= 4; z++)
            {
                for (int x = -5; x <= 5; x++)
                {
                    cells.Add(new GridPosition(x, z));
                }
            }
            return cells;
        }

        private static IReadOnlyList<GridPosition> CreateThrowAnchors()
        {
            return new[]
            {
                new GridPosition(-4, -2),
                new GridPosition(-3, 3),
                new GridPosition(0, -3),
                new GridPosition(0, 3),
                new GridPosition(3, 3),
                new GridPosition(4, -2),
            };
        }

        private static IReadOnlyList<GridPosition> CreateSummonAnchors()
        {
            return new[]
            {
                new GridPosition(-3, 3),
                new GridPosition(3, 3),
            };
        }

        private static void CompleteCurrentPattern(
            BossBattleSimulation boss,
            ManualGameClock clock)
        {
            AdvanceOneTransition(boss, clock, out _);
            AdvanceOneTransition(boss, clock, out _);
            AdvanceOneTransition(boss, clock, out _);
        }

        private static void AdvanceUntilPattern(
            BossBattleSimulation boss,
            ManualGameClock clock,
            BossPatternKind pattern)
        {
            int guard = 0;
            while ((boss.CurrentPattern != pattern ||
                    boss.State != BossBattleState.Telegraph) && guard++ < 1000)
            {
                if (boss.IsWaitingForSelfDestruct)
                {
                    boss.NotifySelfDestructResolved();
                }
                AdvanceOneTransition(boss, clock, out _);
            }
            Assert.That(guard, Is.LessThan(1000), $"Did not reach pattern {pattern}.");
        }

        private static void AdvanceUntilRecovery(
            BossBattleSimulation boss,
            ManualGameClock clock,
            BossPatternKind pattern)
        {
            AdvanceUntilPattern(boss, clock, pattern);
            AdvanceOneTransition(boss, clock, out _);
            AdvanceOneTransition(boss, clock, out _);
            Assert.That(boss.State, Is.EqualTo(BossBattleState.Recovery));
        }

        private static void AdvanceUntilPhase(
            BossBattleSimulation boss,
            ManualGameClock clock,
            BossPhase phase)
        {
            int guard = 0;
            while (boss.Phase != phase && guard++ < 2000)
            {
                if (boss.IsWaitingForSelfDestruct)
                {
                    boss.NotifySelfDestructResolved();
                }
                AdvanceOneTransition(boss, clock, out _);
            }
            Assert.That(guard, Is.LessThan(2000), $"Did not reach phase {phase}.");
        }

        private static void DealOverheatDamage(
            BossBattleSimulation boss,
            ManualGameClock clock,
            int damage,
            bool completeRecovery = true)
        {
            AdvanceUntilRecovery(boss, clock, BossPatternKind.Overheat);
            for (int index = 0; index < damage; index++)
            {
                Assert.That(
                    boss.ApplyExplosion(
                            CreateBombId((boss.CurrentHealth * 10) + index),
                            1)
                        .WasApplied,
                    Is.True);
            }
            if (completeRecovery)
            {
                AdvanceOneTransition(boss, clock, out _);
            }
        }

        private static void AdvanceOneTransition(
            BossBattleSimulation boss,
            ManualGameClock clock,
            out BossPatternTransition transition)
        {
            AdvanceClockToBoundary(boss, clock);
            Assert.That(boss.TryAdvance(out transition), Is.True);
        }

        private static void AdvanceClockToBoundary(
            BossBattleSimulation boss,
            ManualGameClock clock)
        {
            TimeSpan remaining = boss.StateEndsAt - clock.Now;
            if (remaining > TimeSpan.Zero)
            {
                clock.Advance(remaining);
            }
        }

        private static void MovePlayerAlongGrid(GridState grid, GridPosition destination)
        {
            Assert.That(grid.TryGetActorPosition(PlayerActor, out GridPosition current), Is.True);
            while (current.X != destination.X)
            {
                current = current.Offset(Math.Sign(destination.X - current.X), 0);
                Assert.That(grid.TryMoveActor(PlayerActor, current), Is.True);
            }
            while (current.Z != destination.Z)
            {
                current = current.Offset(0, Math.Sign(destination.Z - current.Z));
                Assert.That(grid.TryMoveActor(PlayerActor, current), Is.True);
            }
        }

        private static BombId CreateBombId(int sequence)
        {
            var grid = new GridState();
            var clock = new ManualGameClock();
            var bombs = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(1));
            BombId created = default;
            for (int index = 0; index <= sequence; index++)
            {
                GridPosition position = new GridPosition(index, 0);
                grid.TrySetTerrain(position, GridTerrain.Floor);
                Assert.That(
                    bombs.TryPlaceBomb(CreateThrowBomb(), position, new ActorId(10), out created),
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
