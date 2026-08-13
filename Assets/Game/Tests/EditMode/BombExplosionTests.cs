using System;
using System.Collections.Generic;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BombExplosionTests
    {
        private static readonly GridPosition Origin = new GridPosition(0, 0);

        [Test]
        public void ProcessDueBombs_BeforeFuseReturnsNoExplosion()
        {
            var fixture = CreateFixture(range: 1, fuse: TimeSpan.FromSeconds(2));
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> explosions = fixture.Simulation.ProcessDueBombs();

            Assert.That(explosions, Is.Empty);
            Assert.That(fixture.Simulation.ActiveBombCount, Is.EqualTo(1));
            Assert.That(fixture.Grid.GetCell(Origin).HasBomb, Is.True);
        }

        [Test]
        public void ProcessDueBombs_AtFuseCreatesExplosionAndRemovesBomb()
        {
            var fixture = CreateFixture(range: 0, fuse: TimeSpan.FromSeconds(2));
            fixture.Clock.Advance(TimeSpan.FromSeconds(2));

            IReadOnlyList<BombExplosion> explosions = fixture.Simulation.ProcessDueBombs();

            Assert.That(explosions, Has.Count.EqualTo(1));
            Assert.That(explosions[0].BombId, Is.EqualTo(fixture.BombId));
            Assert.That(explosions[0].DefinitionId, Is.EqualTo(fixture.Definition.Id));
            Assert.That(explosions[0].Origin, Is.EqualTo(Origin));
            Assert.That(explosions[0].DetonatedAt, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(explosions[0].Cause, Is.EqualTo(BombDetonationCause.Fuse));
            Assert.That(explosions[0].AffectedCells, Is.EquivalentTo(new[] { Origin }));
            Assert.That(fixture.Simulation.ActiveBombCount, Is.Zero);
            Assert.That(fixture.Grid.GetCell(Origin).HasBomb, Is.False);
        }

        [Test]
        public void CrossRangeTwo_AffectsOriginAndFourCardinalRays()
        {
            var fixture = CreateFixture(range: 2, fuse: TimeSpan.FromSeconds(1));
            AddCrossFloor(fixture.Grid, Origin, 2);
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            BombExplosion explosion = fixture.Simulation.ProcessDueBombs()[0];

            Assert.That(explosion.AffectedCells, Is.EquivalentTo(new[]
            {
                Origin,
                new GridPosition(1, 0),
                new GridPosition(2, 0),
                new GridPosition(-1, 0),
                new GridPosition(-2, 0),
                new GridPosition(0, 1),
                new GridPosition(0, 2),
                new GridPosition(0, -1),
                new GridPosition(0, -2)
            }));
            Assert.That(explosion.DestroyedWalls, Is.Empty);
        }

        [Test]
        public void IndestructibleWall_IsExcludedAndStopsRay()
        {
            var fixture = CreateFixture(range: 3, fuse: TimeSpan.FromSeconds(1));
            var wall = new GridPosition(1, 0);
            var behindWall = new GridPosition(2, 0);
            fixture.Grid.TrySetTerrain(wall, GridTerrain.IndestructibleWall);
            fixture.Grid.TrySetTerrain(behindWall, GridTerrain.Floor);
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            BombExplosion explosion = fixture.Simulation.ProcessDueBombs()[0];

            Assert.That(explosion.AffectedCells, Has.No.Member(wall));
            Assert.That(explosion.AffectedCells, Has.No.Member(behindWall));
            Assert.That(fixture.Grid.GetCell(wall).Terrain, Is.EqualTo(GridTerrain.IndestructibleWall));
        }

        [Test]
        public void DestructibleWall_IsIncludedDestroyedAndStopsRay()
        {
            var fixture = CreateFixture(range: 3, fuse: TimeSpan.FromSeconds(1));
            var wall = new GridPosition(1, 0);
            var behindWall = new GridPosition(2, 0);
            fixture.Grid.TrySetTerrain(wall, GridTerrain.DestructibleWall);
            fixture.Grid.TrySetTerrain(behindWall, GridTerrain.Floor);
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            BombExplosion explosion = fixture.Simulation.ProcessDueBombs()[0];

            Assert.That(explosion.AffectedCells, Has.Member(wall));
            Assert.That(explosion.AffectedCells, Has.No.Member(behindWall));
            Assert.That(explosion.DestroyedWalls, Is.EquivalentTo(new[] { wall }));
            Assert.That(fixture.Grid.GetCell(wall).Terrain, Is.EqualTo(GridTerrain.Floor));
        }

        [Test]
        public void VoidCell_StopsRayWithoutExplosionEffect()
        {
            var fixture = CreateFixture(range: 2, fuse: TimeSpan.FromSeconds(1));
            var behindVoid = new GridPosition(2, 0);
            fixture.Grid.TrySetTerrain(behindVoid, GridTerrain.Floor);
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));

            BombExplosion explosion = fixture.Simulation.ProcessDueBombs()[0];

            Assert.That(explosion.AffectedCells, Has.No.Member(new GridPosition(1, 0)));
            Assert.That(explosion.AffectedCells, Has.No.Member(behindVoid));
        }

        [Test]
        public void SimultaneousExplosions_ResolveBeforeSharedWallIsDestroyed()
        {
            var grid = new GridState();
            var left = new GridPosition(0, 0);
            var wall = new GridPosition(1, 0);
            var right = new GridPosition(2, 0);
            grid.TrySetTerrain(left, GridTerrain.Floor);
            grid.TrySetTerrain(wall, GridTerrain.DestructibleWall);
            grid.TrySetTerrain(right, GridTerrain.Floor);
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(200));
            BombDefinition definition = CreateDefinition("basic-cross", TimeSpan.FromSeconds(1), 2);
            simulation.TryPlaceBomb(definition, left, out BombId leftId);
            simulation.TryPlaceBomb(definition, right, out BombId rightId);
            clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> explosions = simulation.ProcessDueBombs();

            Assert.That(explosions, Has.Count.EqualTo(2));
            Assert.That(explosions[0].BombId, Is.EqualTo(leftId));
            Assert.That(explosions[1].BombId, Is.EqualTo(rightId));
            Assert.That(explosions[0].AffectedCells, Has.No.Member(right));
            Assert.That(explosions[1].AffectedCells, Has.No.Member(left));
            Assert.That(grid.GetCell(wall).Terrain, Is.EqualTo(GridTerrain.Floor));
        }

        [Test]
        public void SameTimeExplosions_AreReportedInBombIdOrder()
        {
            var grid = new GridState();
            var firstPosition = new GridPosition(3, 0);
            var secondPosition = new GridPosition(-3, 0);
            grid.TrySetTerrain(firstPosition, GridTerrain.Floor);
            grid.TrySetTerrain(secondPosition, GridTerrain.Floor);
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(200));
            BombDefinition definition = CreateDefinition("basic-cross", TimeSpan.FromSeconds(1), 0);
            simulation.TryPlaceBomb(definition, firstPosition, out BombId firstId);
            simulation.TryPlaceBomb(definition, secondPosition, out BombId secondId);
            clock.Advance(TimeSpan.FromSeconds(1));

            IReadOnlyList<BombExplosion> explosions = simulation.ProcessDueBombs();

            Assert.That(explosions[0].BombId, Is.EqualTo(firstId));
            Assert.That(explosions[1].BombId, Is.EqualTo(secondId));
        }

        private static ExplosionFixture CreateFixture(int range, TimeSpan fuse)
        {
            var grid = new GridState();
            grid.TrySetTerrain(Origin, GridTerrain.Floor);
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(200));
            BombDefinition definition = CreateDefinition("basic-cross", fuse, range);
            simulation.TryPlaceBomb(definition, Origin, out BombId bombId);
            return new ExplosionFixture(grid, clock, simulation, definition, bombId);
        }

        private static BombDefinition CreateDefinition(string id, TimeSpan fuse, int range)
        {
            return new BombDefinition(
                new BombDefinitionId(id),
                BombExplosionShape.Cross,
                fuse,
                range);
        }

        private static void AddCrossFloor(GridState grid, GridPosition origin, int range)
        {
            for (int distance = 1; distance <= range; distance++)
            {
                grid.TrySetTerrain(origin.Offset(distance, 0), GridTerrain.Floor);
                grid.TrySetTerrain(origin.Offset(-distance, 0), GridTerrain.Floor);
                grid.TrySetTerrain(origin.Offset(0, distance), GridTerrain.Floor);
                grid.TrySetTerrain(origin.Offset(0, -distance), GridTerrain.Floor);
            }
        }

        private sealed class ExplosionFixture
        {
            public ExplosionFixture(
                GridState grid,
                ManualGameClock clock,
                BombSimulation simulation,
                BombDefinition definition,
                BombId bombId)
            {
                Grid = grid;
                Clock = clock;
                Simulation = simulation;
                Definition = definition;
                BombId = bombId;
            }

            public GridState Grid { get; }

            public ManualGameClock Clock { get; }

            public BombSimulation Simulation { get; }

            public BombDefinition Definition { get; }

            public BombId BombId { get; }
        }
    }
}
