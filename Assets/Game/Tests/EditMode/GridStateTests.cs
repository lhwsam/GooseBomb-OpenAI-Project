using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class GridStateTests
    {
        private static readonly GridPosition Position = new GridPosition(2, -1);

        [Test]
        public void UnknownPosition_IsVoidAndUnoccupied()
        {
            var grid = new GridState();

            GridCellState cell = grid.GetCell(Position);

            Assert.That(cell.Terrain, Is.EqualTo(GridTerrain.Void));
            Assert.That(cell.Occupancy, Is.EqualTo(GridOccupancy.None));
            Assert.That(cell.IsWalkableTerrain, Is.False);
        }

        [TestCase(GridTerrain.Floor)]
        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TrySetTerrain_StoresSupportedTerrain(GridTerrain terrain)
        {
            var grid = new GridState();

            bool changed = grid.TrySetTerrain(Position, terrain);

            Assert.That(changed, Is.True);
            Assert.That(grid.GetCell(Position).Terrain, Is.EqualTo(terrain));
        }

        [Test]
        public void TrySetTerrain_ReplacesThePreviousTerrain()
        {
            var grid = CreateFloorGrid();

            bool changed = grid.TrySetTerrain(Position, GridTerrain.DestructibleWall);

            Assert.That(changed, Is.True);
            Assert.That(grid.GetCell(Position).Terrain, Is.EqualTo(GridTerrain.DestructibleWall));
        }

        [TestCase(GridOccupancy.Actor)]
        [TestCase(GridOccupancy.Bomb)]
        public void TryAddOccupancy_AddsSingleOccupantToFloor(GridOccupancy occupancy)
        {
            var grid = CreateFloorGrid();

            bool added = grid.TryAddOccupancy(Position, occupancy);

            Assert.That(added, Is.True);
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(occupancy));
        }

        [Test]
        public void ActorAndBomb_CanShareFloorCell()
        {
            var grid = CreateFloorGrid();

            bool actorAdded = grid.TryAddOccupancy(Position, GridOccupancy.Actor);
            bool bombAdded = grid.TryAddOccupancy(Position, GridOccupancy.Bomb);

            GridCellState cell = grid.GetCell(Position);
            Assert.That(actorAdded, Is.True);
            Assert.That(bombAdded, Is.True);
            Assert.That(cell.HasActor, Is.True);
            Assert.That(cell.HasBomb, Is.True);
        }

        [Test]
        public void TryAddOccupancy_RejectsDuplicateWithoutChangingState()
        {
            var grid = CreateFloorGrid();
            grid.TryAddOccupancy(Position, GridOccupancy.Actor);

            bool added = grid.TryAddOccupancy(Position, GridOccupancy.Actor);

            Assert.That(added, Is.False);
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(GridOccupancy.Actor));
        }

        [TestCase(GridTerrain.Void)]
        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TryAddOccupancy_RejectsNonFloorTerrain(GridTerrain terrain)
        {
            var grid = new GridState();
            grid.TrySetTerrain(Position, terrain);

            bool added = grid.TryAddOccupancy(Position, GridOccupancy.Actor);

            Assert.That(added, Is.False);
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(GridOccupancy.None));
        }

        [TestCase(GridTerrain.Void)]
        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TrySetTerrain_RejectsNonFloorWhileOccupied(GridTerrain terrain)
        {
            var grid = CreateFloorGrid();
            grid.TryAddOccupancy(Position, GridOccupancy.Bomb);

            bool changed = grid.TrySetTerrain(Position, terrain);

            Assert.That(changed, Is.False);
            Assert.That(grid.GetCell(Position).Terrain, Is.EqualTo(GridTerrain.Floor));
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(GridOccupancy.Bomb));
        }

        [Test]
        public void TryRemoveOccupancy_RemovesOnlyRequestedOccupant()
        {
            var grid = CreateFloorGrid();
            grid.TryAddOccupancy(Position, GridOccupancy.Actor);
            grid.TryAddOccupancy(Position, GridOccupancy.Bomb);

            bool removed = grid.TryRemoveOccupancy(Position, GridOccupancy.Actor);

            GridCellState cell = grid.GetCell(Position);
            Assert.That(removed, Is.True);
            Assert.That(cell.HasActor, Is.False);
            Assert.That(cell.HasBomb, Is.True);
        }

        [Test]
        public void TryRemoveOccupancy_ReturnsFalseWhenOccupantIsAbsent()
        {
            var grid = CreateFloorGrid();

            bool removed = grid.TryRemoveOccupancy(Position, GridOccupancy.Bomb);

            Assert.That(removed, Is.False);
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(GridOccupancy.None));
        }

        [TestCase(GridOccupancy.None)]
        [TestCase(GridOccupancy.Actor | GridOccupancy.Bomb)]
        [TestCase((GridOccupancy)4)]
        public void OccupancyMutations_RejectInvalidOrCombinedValues(GridOccupancy occupancy)
        {
            var grid = CreateFloorGrid();

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.TryAddOccupancy(Position, occupancy));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.TryRemoveOccupancy(Position, occupancy));
        }

        [Test]
        public void TrySetTerrain_RejectsUnknownTerrainValue()
        {
            var grid = new GridState();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => grid.TrySetTerrain(Position, (GridTerrain)99));
        }

        private static GridState CreateFloorGrid()
        {
            var grid = new GridState();
            grid.TrySetTerrain(Position, GridTerrain.Floor);
            return grid;
        }
    }
}
