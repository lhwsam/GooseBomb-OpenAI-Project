using System;
using System.Collections.Generic;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BombChainReactionTests
    {
        private static readonly GridPosition SourcePosition = new GridPosition(0, 0);
        private static readonly GridPosition TargetPosition = new GridPosition(2, 0);
        private static readonly TimeSpan ChainDelay = TimeSpan.FromMilliseconds(250);

        [Test]
        public void Explosion_SchedulesReachedBombAfterFixedPositiveDelay()
        {
            ChainFixture fixture = CreateChainFixture();
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> sourceExplosions = fixture.Simulation.ProcessDueBombs();

            Assert.That(sourceExplosions, Has.Count.EqualTo(1));
            Assert.That(sourceExplosions[0].BombId, Is.EqualTo(fixture.SourceId));
            Assert.That(fixture.Simulation.TryGetBomb(fixture.TargetId, out BombSnapshot target), Is.True);
            Assert.That(target.DetonatesAt, Is.EqualTo(TimeSpan.FromSeconds(1) + ChainDelay));
            Assert.That(target.ScheduledCause, Is.EqualTo(BombDetonationCause.Chain));
        }

        [Test]
        public void ChainedBomb_DoesNotExplodeRecursivelyAtSourceTime()
        {
            ChainFixture fixture = CreateChainFixture();
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> firstPass = fixture.Simulation.ProcessDueBombs();
            IReadOnlyList<BombExplosion> secondPass = fixture.Simulation.ProcessDueBombs();

            Assert.That(firstPass, Has.Count.EqualTo(1));
            Assert.That(secondPass, Is.Empty);
            Assert.That(fixture.Simulation.ActiveBombCount, Is.EqualTo(1));
        }

        [Test]
        public void ChainedBomb_ExplodesOnceWhenDelayElapses()
        {
            ChainFixture fixture = CreateChainFixture();
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));
            fixture.Simulation.ProcessDueBombs();
            fixture.Clock.Advance(ChainDelay);

            IReadOnlyList<BombExplosion> explosions = fixture.Simulation.ProcessDueBombs();

            Assert.That(explosions, Has.Count.EqualTo(1));
            Assert.That(explosions[0].BombId, Is.EqualTo(fixture.TargetId));
            Assert.That(explosions[0].Cause, Is.EqualTo(BombDetonationCause.Chain));
            Assert.That(explosions[0].DetonatedAt, Is.EqualTo(TimeSpan.FromSeconds(1) + ChainDelay));
            Assert.That(fixture.Simulation.ActiveBombCount, Is.Zero);
        }

        [Test]
        public void TwoSimultaneousExplosions_ScheduleSharedTargetOnlyOnce()
        {
            var grid = new GridState();
            for (int x = 0; x <= 4; x++)
            {
                grid.TrySetTerrain(new GridPosition(x, 0), GridTerrain.Floor);
            }

            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, ChainDelay);
            BombDefinition sourceDefinition = CreateDefinition("source", 1, 2);
            BombDefinition targetDefinition = CreateDefinition("target", 10, 0);
            simulation.TryPlaceBomb(sourceDefinition, new GridPosition(0, 0), out BombId firstSourceId);
            simulation.TryPlaceBomb(targetDefinition, new GridPosition(2, 0), out BombId targetId);
            simulation.TryPlaceBomb(sourceDefinition, new GridPosition(4, 0), out BombId secondSourceId);
            clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> sourceExplosions = simulation.ProcessDueBombs();
            simulation.TryGetBomb(targetId, out BombSnapshot target);
            clock.Advance(ChainDelay);
            IReadOnlyList<BombExplosion> targetExplosions = simulation.ProcessDueBombs();

            Assert.That(sourceExplosions, Has.Count.EqualTo(2));
            Assert.That(sourceExplosions[0].BombId, Is.EqualTo(firstSourceId));
            Assert.That(sourceExplosions[1].BombId, Is.EqualTo(secondSourceId));
            Assert.That(target.DetonatesAt, Is.EqualTo(TimeSpan.FromSeconds(1) + ChainDelay));
            Assert.That(targetExplosions, Has.Count.EqualTo(1));
            Assert.That(targetExplosions[0].BombId, Is.EqualTo(targetId));
        }

        [Test]
        public void FuseAndChainArrivalAtSameTime_StillExplodeTargetOnce()
        {
            var grid = CreateLineFloor(0, 2);
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, ChainDelay);
            BombDefinition sourceDefinition = CreateDefinition("source", 1, 2);
            BombDefinition targetDefinition = CreateDefinition("target", 1, 0);
            simulation.TryPlaceBomb(sourceDefinition, SourcePosition, out BombId sourceId);
            simulation.TryPlaceBomb(targetDefinition, TargetPosition, out BombId targetId);
            clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> explosions = simulation.ProcessDueBombs();

            Assert.That(explosions, Has.Count.EqualTo(2));
            Assert.That(explosions[0].BombId, Is.EqualTo(sourceId));
            Assert.That(explosions[1].BombId, Is.EqualTo(targetId));
            Assert.That(explosions[1].Cause, Is.EqualTo(BombDetonationCause.Fuse));
            Assert.That(simulation.ActiveBombCount, Is.Zero);
        }

        [Test]
        public void LargeClockAdvance_ProcessesChainAtScheduledLogicalTimes()
        {
            ChainFixture fixture = CreateChainFixture();
            fixture.Clock.Advance(TimeSpan.FromSeconds(5));

            IReadOnlyList<BombExplosion> explosions = fixture.Simulation.ProcessDueBombs();

            Assert.That(explosions, Has.Count.EqualTo(2));
            Assert.That(explosions[0].BombId, Is.EqualTo(fixture.SourceId));
            Assert.That(explosions[0].DetonatedAt, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(explosions[1].BombId, Is.EqualTo(fixture.TargetId));
            Assert.That(explosions[1].DetonatedAt, Is.EqualTo(TimeSpan.FromSeconds(1) + ChainDelay));
            Assert.That(explosions[1].Cause, Is.EqualTo(BombDetonationCause.Chain));
        }

        [Test]
        public void BombOutsideAffectedCells_KeepsOriginalFuseSchedule()
        {
            var grid = CreateLineFloor(0, 3);
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, ChainDelay);
            simulation.TryPlaceBomb(CreateDefinition("source", 1, 1), SourcePosition, out BombId _);
            simulation.TryPlaceBomb(CreateDefinition("target", 10, 0), new GridPosition(3, 0), out BombId targetId);
            clock.Advance(TimeSpan.FromSeconds(1));

            simulation.ProcessDueBombs();
            simulation.TryGetBomb(targetId, out BombSnapshot target);

            Assert.That(target.DetonatesAt, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(target.ScheduledCause, Is.EqualTo(BombDetonationCause.Fuse));
        }

        private static ChainFixture CreateChainFixture()
        {
            GridState grid = CreateLineFloor(0, 2);
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, ChainDelay);
            simulation.TryPlaceBomb(
                CreateDefinition("source", 1, 2),
                SourcePosition,
                out BombId sourceId);
            simulation.TryPlaceBomb(
                CreateDefinition("different-target-type", 10, 0),
                TargetPosition,
                out BombId targetId);
            return new ChainFixture(clock, simulation, sourceId, targetId);
        }

        private static GridState CreateLineFloor(int minimumX, int maximumX)
        {
            var grid = new GridState();
            for (int x = minimumX; x <= maximumX; x++)
            {
                grid.TrySetTerrain(new GridPosition(x, 0), GridTerrain.Floor);
            }

            return grid;
        }

        private static BombDefinition CreateDefinition(string id, double fuseSeconds, int range)
        {
            return new BombDefinition(
                new BombDefinitionId(id),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(fuseSeconds),
                range);
        }

        private sealed class ChainFixture
        {
            public ChainFixture(
                ManualGameClock clock,
                BombSimulation simulation,
                BombId sourceId,
                BombId targetId)
            {
                Clock = clock;
                Simulation = simulation;
                SourceId = sourceId;
                TargetId = targetId;
            }

            public ManualGameClock Clock { get; }

            public BombSimulation Simulation { get; }

            public BombId SourceId { get; }

            public BombId TargetId { get; }
        }
    }
}
