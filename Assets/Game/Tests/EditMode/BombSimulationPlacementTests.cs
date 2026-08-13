using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BombSimulationPlacementTests
    {
        private static readonly ActorId Owner = new ActorId(1);
        private static readonly GridPosition Position = new GridPosition(1, -2);

        [Test]
        public void TryPlaceBomb_CreatesUniqueSnapshotAndGridOccupancy()
        {
            var grid = CreateGridWithTerrain(GridTerrain.Floor);
            var clock = new ManualGameClock(TimeSpan.FromSeconds(3));
            var simulation = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(200));
            BombDefinition definition = CreateDefinition(TimeSpan.FromSeconds(2));

            bool placed = simulation.TryPlaceBomb(definition, Position, Owner, out BombId bombId);

            Assert.That(placed, Is.True);
            Assert.That(bombId.IsValid, Is.True);
            Assert.That(simulation.ActiveBombCount, Is.EqualTo(1));
            Assert.That(grid.GetCell(Position).HasBomb, Is.True);
            Assert.That(simulation.TryGetBomb(bombId, out BombSnapshot snapshot), Is.True);
            Assert.That(snapshot.Id, Is.EqualTo(bombId));
            Assert.That(snapshot.DefinitionId, Is.EqualTo(definition.Id));
            Assert.That(snapshot.Position, Is.EqualTo(Position));
            Assert.That(snapshot.OwnerId, Is.EqualTo(Owner));
            Assert.That(snapshot.DetonatesAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(snapshot.ScheduledCause, Is.EqualTo(BombDetonationCause.Fuse));
        }

        [Test]
        public void TryPlaceBomb_AllowsInstallerActorToShareFloorCell()
        {
            var grid = CreateGridWithTerrain(GridTerrain.Floor);
            grid.TryAddActor(Owner, Position);
            var simulation = CreateSimulation(grid);

            bool placed = simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId bombId);

            GridCellState cell = grid.GetCell(Position);
            Assert.That(placed, Is.True);
            Assert.That(bombId.IsValid, Is.True);
            Assert.That(cell.HasActor, Is.True);
            Assert.That(cell.HasBomb, Is.True);
        }

        [TestCase(GridTerrain.Void)]
        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TryPlaceBomb_RejectsNonFloorWithoutPartialState(GridTerrain terrain)
        {
            var grid = CreateGridWithTerrain(terrain);
            var simulation = CreateSimulation(grid);

            bool placed = simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId bombId);

            Assert.That(placed, Is.False);
            Assert.That(bombId.IsValid, Is.False);
            Assert.That(simulation.ActiveBombCount, Is.Zero);
            Assert.That(grid.GetCell(Position).HasBomb, Is.False);
        }

        [Test]
        public void TryPlaceBomb_RejectsSecondBombOnSameCell()
        {
            var grid = CreateGridWithTerrain(GridTerrain.Floor);
            var simulation = CreateSimulation(grid);
            simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId firstId);

            bool placed = simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId secondId);

            Assert.That(placed, Is.False);
            Assert.That(firstId.IsValid, Is.True);
            Assert.That(secondId.IsValid, Is.False);
            Assert.That(simulation.ActiveBombCount, Is.EqualTo(1));
            Assert.That(grid.GetCell(Position).HasBomb, Is.True);
        }

        [Test]
        public void FailedPlacement_DoesNotConsumeBombId()
        {
            var grid = new GridState();
            var simulation = CreateSimulation(grid);

            simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId failedId);
            grid.TrySetTerrain(Position, GridTerrain.Floor);
            simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId placedId);

            Assert.That(failedId.IsValid, Is.False);
            Assert.That(placedId.Value, Is.EqualTo(1));
        }

        [Test]
        public void TryPlaceBomb_AssignsIncreasingSessionIds()
        {
            var secondPosition = new GridPosition(2, -2);
            var grid = CreateGridWithTerrain(GridTerrain.Floor);
            grid.TrySetTerrain(secondPosition, GridTerrain.Floor);
            var simulation = CreateSimulation(grid);

            simulation.TryPlaceBomb(CreateDefinition(), Position, Owner, out BombId firstId);
            simulation.TryPlaceBomb(CreateDefinition(), secondPosition, Owner, out BombId secondId);

            Assert.That(secondId.Value, Is.GreaterThan(firstId.Value));
        }

        [Test]
        public void TryPlaceBomb_RejectsNullDefinition()
        {
            var simulation = CreateSimulation(CreateGridWithTerrain(GridTerrain.Floor));

            Assert.Throws<ArgumentNullException>(
                () => simulation.TryPlaceBomb(null, Position, Owner, out BombId _));
        }

        [Test]
        public void TryPlaceBomb_RejectsDefaultOwner()
        {
            var simulation = CreateSimulation(CreateGridWithTerrain(GridTerrain.Floor));

            Assert.Throws<ArgumentException>(() =>
                simulation.TryPlaceBomb(CreateDefinition(), Position, default, out BombId _));
        }

        [Test]
        public void TryPlaceBomb_TimeOverflowLeavesGridUnchanged()
        {
            var grid = CreateGridWithTerrain(GridTerrain.Floor);
            var clock = new ManualGameClock(TimeSpan.MaxValue - TimeSpan.FromTicks(1));
            var simulation = new BombSimulation(grid, clock, TimeSpan.FromTicks(1));

            Assert.Throws<OverflowException>(() => simulation.TryPlaceBomb(
                CreateDefinition(TimeSpan.FromTicks(2)),
                Position,
                Owner,
                out BombId _));
            Assert.That(simulation.ActiveBombCount, Is.Zero);
            Assert.That(grid.GetCell(Position).HasBomb, Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsNonPositiveChainDelay(long ticks)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BombSimulation(
                new GridState(),
                new ManualGameClock(),
                TimeSpan.FromTicks(ticks)));
        }

        private static BombSimulation CreateSimulation(GridState grid)
        {
            return new BombSimulation(
                grid,
                new ManualGameClock(),
                TimeSpan.FromMilliseconds(200));
        }

        private static BombDefinition CreateDefinition()
        {
            return CreateDefinition(TimeSpan.FromSeconds(2));
        }

        private static BombDefinition CreateDefinition(TimeSpan fuseDuration)
        {
            return new BombDefinition(
                new BombDefinitionId("basic-cross"),
                BombExplosionShape.Cross,
                fuseDuration,
                2);
        }

        private static GridState CreateGridWithTerrain(GridTerrain terrain)
        {
            var grid = new GridState();
            grid.TrySetTerrain(Position, terrain);
            return grid;
        }
    }
}
