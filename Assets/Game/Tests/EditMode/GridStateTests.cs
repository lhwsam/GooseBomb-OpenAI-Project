using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class GridStateTests
    {
        private static readonly ActorId Actor = new ActorId(1);
        private static readonly ActorId OtherActor = new ActorId(2);
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

        [Test]
        public void TryAddActor_StoresIdentityAndPosition()
        {
            var grid = CreateFloorGrid();

            bool added = grid.TryAddActor(Actor, Position);

            Assert.That(added, Is.True);
            Assert.That(grid.GetCell(Position).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(Position));
        }

        [Test]
        public void ActorAndBomb_CanShareFloorCellOnlyByPlacingBombAfterActor()
        {
            var grid = CreateFloorGrid();

            bool actorAdded = grid.TryAddActor(Actor, Position);
            bool bombAdded = grid.TryAddBomb(Position);

            GridCellState cell = grid.GetCell(Position);
            Assert.That(actorAdded, Is.True);
            Assert.That(bombAdded, Is.True);
            Assert.That(cell.HasActor, Is.True);
            Assert.That(cell.HasBomb, Is.True);
        }

        [Test]
        public void TryAddActor_RejectsDuplicateIdentityAndOccupiedCellAtomically()
        {
            GridPosition secondPosition = Position.Offset(1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(secondPosition, GridTerrain.Floor);
            Assert.That(grid.TryAddActor(Actor, Position), Is.True);

            bool duplicateIdentity = grid.TryAddActor(Actor, secondPosition);
            bool occupiedCell = grid.TryAddActor(OtherActor, Position);

            Assert.That(duplicateIdentity, Is.False);
            Assert.That(occupiedCell, Is.False);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(Position));
            Assert.That(grid.TryGetActorPosition(OtherActor, out _), Is.False);
            Assert.That(grid.GetCell(secondPosition).HasActor, Is.False);
        }

        [TestCase(GridTerrain.Void)]
        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TryAddActor_RejectsNonFloorTerrain(GridTerrain terrain)
        {
            var grid = new GridState();
            grid.TrySetTerrain(Position, terrain);

            bool added = grid.TryAddActor(Actor, Position);

            Assert.That(added, Is.False);
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(GridOccupancy.None));
            Assert.That(grid.TryGetActorPosition(Actor, out _), Is.False);
        }

        [Test]
        public void TryAddActor_RejectsBombCell()
        {
            var grid = CreateFloorGrid();
            grid.TryAddBomb(Position);

            bool added = grid.TryAddActor(Actor, Position);

            Assert.That(added, Is.False);
            Assert.That(grid.GetCell(Position).HasBomb, Is.True);
            Assert.That(grid.GetCell(Position).HasActor, Is.False);
        }

        [TestCase(GridTerrain.Void)]
        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TrySetTerrain_RejectsNonFloorWhileOccupied(GridTerrain terrain)
        {
            var grid = CreateFloorGrid();
            grid.TryAddBomb(Position);

            bool changed = grid.TrySetTerrain(Position, terrain);

            Assert.That(changed, Is.False);
            Assert.That(grid.GetCell(Position).Terrain, Is.EqualTo(GridTerrain.Floor));
            Assert.That(grid.GetCell(Position).HasBomb, Is.True);
        }

        [Test]
        public void TryRemoveActor_RemovesIdentityAndPreservesBomb()
        {
            var grid = CreateFloorGrid();
            grid.TryAddActor(Actor, Position);
            grid.TryAddBomb(Position);

            bool removed = grid.TryRemoveActor(Actor);

            GridCellState cell = grid.GetCell(Position);
            Assert.That(removed, Is.True);
            Assert.That(cell.HasActor, Is.False);
            Assert.That(cell.HasBomb, Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out _), Is.False);
        }

        [Test]
        public void TryRemoveBomb_ReturnsFalseWhenBombIsAbsent()
        {
            var grid = CreateFloorGrid();

            bool removed = grid.TryRemoveBomb(Position);

            Assert.That(removed, Is.False);
            Assert.That(grid.GetCell(Position).Occupancy, Is.EqualTo(GridOccupancy.None));
        }

        [Test]
        public void TryMoveActor_AtomicallyTransfersIdentityToAdjacentFloor()
        {
            GridPosition destination = Position.Offset(1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            grid.TryAddActor(Actor, Position);

            bool moved = grid.TryMoveActor(Actor, destination);

            Assert.That(moved, Is.True);
            Assert.That(grid.GetCell(Position).HasActor, Is.False);
            Assert.That(grid.GetCell(destination).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(destination));
        }

        [Test]
        public void ReservedDestination_BlocksOtherActorsAndBombsUntilMoveCompletes()
        {
            GridPosition destination = Position.Offset(1, 0);
            GridPosition otherStart = destination.Offset(1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            grid.TrySetTerrain(otherStart, GridTerrain.Floor);
            Assert.That(grid.TryAddActor(Actor, Position), Is.True);
            Assert.That(grid.TryAddActor(OtherActor, otherStart), Is.True);

            Assert.That(grid.TryReserveActorMove(Actor, destination), Is.True);
            Assert.That(grid.IsCellReservedForActorMove(destination), Is.True);
            Assert.That(grid.TryMoveActor(Actor, destination), Is.False);
            Assert.That(grid.TryMoveActor(OtherActor, destination), Is.False);
            Assert.That(grid.TryAddBomb(destination), Is.False);
            Assert.That(grid.TryCommitReservedActorMove(Actor), Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(destination));
            Assert.That(grid.CompleteActorMove(Actor), Is.True);
            Assert.That(grid.IsCellReservedForActorMove(destination), Is.False);
        }

        [Test]
        public void RemovingMovingActor_ReleasesItsDestinationReservation()
        {
            GridPosition destination = Position.Offset(1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            Assert.That(grid.TryAddActor(Actor, Position), Is.True);
            Assert.That(grid.TryReserveActorMove(Actor, destination), Is.True);

            Assert.That(grid.TryRemoveActor(Actor), Is.True);

            Assert.That(grid.IsCellReservedForActorMove(destination), Is.False);
            Assert.That(grid.TryAddBomb(destination), Is.True);
        }

        [Test]
        public void ReservedDestination_RejectsNonWalkableTerrainChange()
        {
            GridPosition destination = Position.Offset(1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            Assert.That(grid.TryAddActor(Actor, Position), Is.True);
            Assert.That(grid.TryReserveActorMove(Actor, destination), Is.True);

            Assert.That(
                grid.TrySetTerrain(destination, GridTerrain.IndestructibleWall),
                Is.False);
            Assert.That(grid.GetCell(destination).Terrain, Is.EqualTo(GridTerrain.Floor));
            Assert.That(grid.TryCommitReservedActorMove(Actor), Is.True);
        }

        [Test]
        public void TryMoveActor_PreservesBombLeftInSourceCell()
        {
            GridPosition destination = Position.Offset(0, 1);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            grid.TryAddActor(Actor, Position);
            grid.TryAddBomb(Position);

            bool moved = grid.TryMoveActor(Actor, destination);

            Assert.That(moved, Is.True);
            Assert.That(grid.GetCell(Position).HasBomb, Is.True);
            Assert.That(grid.GetCell(Position).HasActor, Is.False);
            Assert.That(grid.GetCell(destination).HasActor, Is.True);
        }

        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void TryMoveActor_BlockedDestinationDoesNotPartiallyChangeState(GridTerrain terrain)
        {
            GridPosition destination = Position.Offset(-1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, terrain);
            grid.TryAddActor(Actor, Position);

            bool moved = grid.TryMoveActor(Actor, destination);

            Assert.That(moved, Is.False);
            Assert.That(grid.GetCell(Position).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(Position));
            Assert.That(grid.GetCell(destination).HasActor, Is.False);
        }

        [Test]
        public void TryMoveActor_BombBlocksDestination()
        {
            GridPosition destination = Position.Offset(0, -1);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            grid.TryAddActor(Actor, Position);
            grid.TryAddBomb(destination);

            bool moved = grid.TryMoveActor(Actor, destination);

            Assert.That(moved, Is.False);
            Assert.That(grid.GetCell(Position).HasActor, Is.True);
            Assert.That(grid.GetCell(destination).HasBomb, Is.True);
        }

        [Test]
        public void TryMoveActorAllowingBombOverlap_MovesIntoBombAndPreservesActorAfterBombRemoval()
        {
            GridPosition destination = Position.Offset(0, -1);
            GridPosition occupiedDestination = destination.Offset(1, 0);
            var grid = CreateFloorGrid();
            Assert.That(grid.TrySetTerrain(destination, GridTerrain.Floor), Is.True);
            Assert.That(
                grid.TrySetTerrain(occupiedDestination, GridTerrain.Floor),
                Is.True);
            Assert.That(grid.TryAddActor(Actor, Position), Is.True);
            Assert.That(grid.TryAddBomb(destination), Is.True);
            Assert.That(grid.TryAddActor(OtherActor, occupiedDestination), Is.True);

            bool moved = grid.TryMoveActorAllowingBombOverlap(Actor, destination);

            Assert.That(moved, Is.True);
            Assert.That(grid.GetCell(Position).HasActor, Is.False);
            Assert.That(grid.GetCell(destination).HasActor, Is.True);
            Assert.That(grid.GetCell(destination).HasBomb, Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(destination));
            Assert.That(
                grid.TryMoveActorAllowingBombOverlap(Actor, occupiedDestination),
                Is.False);

            Assert.That(grid.TryRemoveBomb(destination), Is.True);
            Assert.That(grid.GetCell(destination).HasActor, Is.True);
            Assert.That(grid.GetCell(destination).HasBomb, Is.False);
            Assert.That(grid.TryGetActorPosition(Actor, out stored), Is.True);
            Assert.That(stored, Is.EqualTo(destination));
        }

        [Test]
        public void TryMoveActor_CannotMoveAnotherIdentity()
        {
            GridPosition destination = Position.Offset(1, 0);
            var grid = CreateFloorGrid();
            grid.TrySetTerrain(destination, GridTerrain.Floor);
            grid.TryAddActor(Actor, Position);

            bool moved = grid.TryMoveActor(OtherActor, destination);

            Assert.That(moved, Is.False);
            Assert.That(grid.GetCell(Position).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(Position));
        }

        [Test]
        public void TryMoveActor_RejectsNonAdjacentTarget()
        {
            var grid = CreateFloorGrid();
            grid.TryAddActor(Actor, Position);

            Assert.Throws<ArgumentException>(() =>
                grid.TryMoveActor(Actor, Position.Offset(2, 0)));
        }

        [Test]
        public void ActorOperations_RejectDefaultActorId()
        {
            var grid = CreateFloorGrid();

            Assert.Throws<ArgumentException>(() => grid.TryAddActor(default, Position));
            Assert.Throws<ArgumentException>(() => grid.TryRemoveActor(default));
            Assert.Throws<ArgumentException>(() => grid.TryGetActorPosition(default, out _));
            Assert.Throws<ArgumentException>(() => grid.TryMoveActor(default, Position.Offset(1, 0)));
            Assert.Throws<ArgumentException>(() =>
                grid.TryMoveActorAllowingBombOverlap(default, Position.Offset(1, 0)));
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
