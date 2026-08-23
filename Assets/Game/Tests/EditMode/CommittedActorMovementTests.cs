using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class CommittedActorMovementTests
    {
        private static readonly ActorId Actor = new ActorId(20);
        private static readonly ActorId OtherActor = new ActorId(21);
        private static readonly GridPosition Start = new GridPosition(0, 0);
        private static readonly GridPosition Destination = new GridPosition(1, 0);

        [Test]
        public void Movement_ReservesCrossesAtHalfAndCompletesAtCenter()
        {
            GridState grid = CreateGrid();
            var movement = new CommittedActorMovement(grid, Actor, Start, TimeSpan.Zero);

            Assert.That(movement.TryStart(
                Destination,
                CardinalDirection.East,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(200)), Is.True);
            Assert.That(grid.TryMoveActor(OtherActor, Destination), Is.False);

            Assert.That(movement.Advance(TimeSpan.FromMilliseconds(99)), Is.False);
            Assert.That(movement.CurrentCell, Is.EqualTo(Start));
            Assert.That(movement.Position.X, Is.EqualTo(0.495d).Within(0.000001d));

            Assert.That(movement.Advance(TimeSpan.FromMilliseconds(100)), Is.False);
            Assert.That(movement.CurrentCell, Is.EqualTo(Destination));
            Assert.That(grid.TryGetActorPosition(Actor, out GridPosition occupied), Is.True);
            Assert.That(occupied, Is.EqualTo(Destination));

            Assert.That(movement.Advance(TimeSpan.FromMilliseconds(200)), Is.True);
            Assert.That(movement.Position, Is.EqualTo(
                GridSubcellPosition.AtCellCenter(Destination)));
            Assert.That(movement.IsMoving, Is.False);
            Assert.That(grid.IsCellReservedForActorMove(Destination), Is.False);
        }

        [Test]
        public void Cancel_ReleasesReservationWithoutChangingSelectedCell()
        {
            GridState grid = CreateGrid();
            var movement = new CommittedActorMovement(grid, Actor, Start, TimeSpan.Zero);
            movement.TryStart(
                Destination,
                CardinalDirection.East,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(200));
            movement.Advance(TimeSpan.FromMilliseconds(100));

            movement.Cancel();

            Assert.That(movement.CurrentCell, Is.EqualTo(Destination));
            Assert.That(grid.IsCellReservedForActorMove(Destination), Is.False);
        }

        [Test]
        public void GetCurrentCellAt_UsesBoundaryTimeInsideLatestAdvanceInterval()
        {
            GridState grid = CreateGrid();
            var movement = new CommittedActorMovement(grid, Actor, Start, TimeSpan.Zero);
            movement.TryStart(
                Destination,
                CardinalDirection.East,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(200));

            movement.Advance(TimeSpan.FromMilliseconds(110));

            Assert.That(
                movement.GetCurrentCellAt(TimeSpan.FromMilliseconds(99)),
                Is.EqualTo(Start));
            Assert.That(
                movement.GetCurrentCellAt(TimeSpan.FromMilliseconds(100)),
                Is.EqualTo(Destination));
        }

        private static GridState CreateGrid()
        {
            var grid = new GridState();
            grid.TrySetTerrain(Start, GridTerrain.Floor);
            grid.TrySetTerrain(Destination, GridTerrain.Floor);
            grid.TrySetTerrain(Start.Offset(0, 1), GridTerrain.Floor);
            Assert.That(grid.TryAddActor(Actor, Start), Is.True);
            Assert.That(grid.TryAddActor(OtherActor, Start.Offset(0, 1)), Is.True);
            return grid;
        }
    }
}
