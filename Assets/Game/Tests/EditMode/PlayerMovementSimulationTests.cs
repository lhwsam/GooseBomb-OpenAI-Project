using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class PlayerMovementSimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId OtherActor = new ActorId(2);
        private static readonly GridPosition Start = new GridPosition(0, 0);
        private const double CellsPerSecond = 5d;

        [Test]
        public void Constructor_ClaimsStartingCellAndSubcellCenterAsAuthority()
        {
            GridState grid = CreateFloorGrid();

            PlayerMovementSimulation movement = CreateMovement(
                grid,
                new ManualGameClock());

            Assert.That(movement.ActorId, Is.EqualTo(PlayerActor));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.Position, Is.EqualTo(GridSubcellPosition.AtCellCenter(Start)));
            Assert.That(movement.FacingDirection, Is.EqualTo(CardinalDirection.North));
            Assert.That(movement.CellsPerSecond, Is.EqualTo(CellsPerSecond));
            Assert.That(grid.GetCell(Start).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(PlayerActor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(Start));
        }

        [Test]
        public void HeldDirection_AdvancesEveryObservedFrameWithoutCadenceGate()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);

            Assert.That(movement.Advance(), Is.False);

            clock.Advance(TimeSpan.FromMilliseconds(50));
            Assert.That(movement.Advance(), Is.True);
            Assert.That(movement.Position.X, Is.Zero);
            Assert.That(movement.Position.Z, Is.EqualTo(0.25d).Within(0.000001d));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.LastCellSteps, Is.Empty);

            clock.Advance(TimeSpan.FromMilliseconds(50));
            Assert.That(movement.Advance(), Is.True);
            Assert.That(movement.Position.Z, Is.EqualTo(0.5d).Within(0.000001d));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start.Offset(0, 1)));
            Assert.That(movement.LastCellSteps, Has.Count.EqualTo(1));
            Assert.That(movement.LastCellSteps[0].Direction, Is.EqualTo(CardinalDirection.North));
        }

        [Test]
        public void ReleasedDirection_StopsImmediatelyWithoutCompletingOrAccumulatingDistance()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(50));
            Assert.That(movement.Advance(), Is.True);
            GridSubcellPosition released = movement.Position;

            movement.SetMoveDirection(CardinalDirection.None);
            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.That(movement.Advance(), Is.False);
            Assert.That(movement.Position, Is.EqualTo(released));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.IsMoving, Is.False);
            Assert.That(grid.TryGetActorMoveReservation(PlayerActor, out _), Is.False);

            movement.SetMoveDirection(CardinalDirection.North);
            Assert.That(movement.Advance(), Is.False);
            clock.Advance(TimeSpan.FromMilliseconds(20));
            Assert.That(movement.Advance(), Is.True);
            Assert.That(movement.Position.X, Is.Zero.Within(0.000001d));
            Assert.That(
                movement.Position.Z,
                Is.EqualTo(released.Z + 0.1d).Within(0.000001d));
        }

        [Test]
        public void FacingDirection_RetainsLastCardinalIntentAfterReleaseAndBlockedMovement()
        {
            var grid = new GridState();
            grid.TrySetTerrain(Start, GridTerrain.Floor);
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);

            movement.SetMoveDirection(CardinalDirection.West);
            clock.Advance(TimeSpan.FromMilliseconds(50));
            Assert.That(movement.Advance(), Is.False);
            Assert.That(movement.FacingDirection, Is.EqualTo(CardinalDirection.West));

            movement.SetMoveDirection(CardinalDirection.None);

            Assert.That(movement.MoveDirection, Is.EqualTo(CardinalDirection.None));
            Assert.That(movement.FacingDirection, Is.EqualTo(CardinalDirection.West));
        }

        [Test]
        public void DirectionChange_AppliesDuringNextObservedStepAndReplacesReservation()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(40));
            movement.Advance();

            movement.SetMoveDirection(CardinalDirection.East);
            clock.Advance(TimeSpan.FromMilliseconds(40));

            Assert.That(movement.Advance(), Is.True);
            Assert.That(movement.FacingDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(movement.CurrentMovementDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(movement.Position.X, Is.EqualTo(0.2d).Within(0.000001d));
            Assert.That(movement.Position.Z, Is.EqualTo(0.2d).Within(0.000001d));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(
                grid.TryGetActorMoveReservation(PlayerActor, out GridPosition reserved),
                Is.True);
            Assert.That(reserved, Is.EqualTo(Start.Offset(1, 0)));
        }

        [Test]
        public void DirectionChange_AfterBoundaryReleasesForwardReservationAndKeepsOneOccupancy()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(110));
            movement.Advance();

            GridPosition crossedCell = Start.Offset(0, 1);
            Assert.That(movement.CurrentPosition, Is.EqualTo(crossedCell));
            Assert.That(
                grid.TryGetActorMoveReservation(PlayerActor, out GridPosition forward),
                Is.True);
            Assert.That(forward, Is.EqualTo(Start.Offset(0, 2)));

            movement.SetMoveDirection(CardinalDirection.East);

            Assert.That(grid.TryGetActorMoveReservation(PlayerActor, out _), Is.False);
            Assert.That(grid.GetCell(Start).HasActor, Is.False);
            Assert.That(grid.GetCell(crossedCell).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(PlayerActor, out GridPosition occupied), Is.True);
            Assert.That(occupied, Is.EqualTo(crossedCell));

            clock.Advance(TimeSpan.FromMilliseconds(20));
            Assert.That(movement.Advance(), Is.True);
            Assert.That(movement.CurrentMovementDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(movement.Position.X, Is.EqualTo(0.1d).Within(0.000001d));
            Assert.That(movement.Position.Z, Is.EqualTo(0.55d).Within(0.000001d));
            Assert.That(
                grid.TryGetActorMoveReservation(PlayerActor, out GridPosition turnReservation),
                Is.True);
            Assert.That(turnReservation, Is.EqualTo(crossedCell.Offset(1, 0)));
        }

        [Test]
        public void RapidAlternatingDirections_PreserveEveryObservedStepIntent()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            CardinalDirection[] directions =
            {
                CardinalDirection.North,
                CardinalDirection.East,
                CardinalDirection.North,
                CardinalDirection.East,
                CardinalDirection.North,
                CardinalDirection.East,
            };

            for (int index = 0; index < directions.Length; index++)
            {
                movement.SetMoveDirection(directions[index]);
                clock.Advance(TimeSpan.FromMilliseconds(10));
                Assert.That(movement.Advance(), Is.True);
            }

            Assert.That(movement.Position.X, Is.EqualTo(0.15d).Within(0.000001d));
            Assert.That(movement.Position.Z, Is.EqualTo(0.15d).Within(0.000001d));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
        }

        [Test]
        public void LargeAdvance_MatchesEquivalentSplitAdvances()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(500));

            Assert.That(movement.Advance(), Is.True);
            Assert.That(movement.CurrentPosition, Is.EqualTo(new GridPosition(0, 2)));
            Assert.That(movement.Position.Z, Is.EqualTo(2d).Within(0.000001d));
            Assert.That(movement.LastCellSteps, Has.Count.EqualTo(2));

            GridState splitGrid = CreateFloorGrid();
            var splitClock = new ManualGameClock();
            PlayerMovementSimulation splitMovement = CreateMovement(splitGrid, splitClock);
            splitMovement.SetMoveDirection(CardinalDirection.North);
            for (int index = 0; index < 5; index++)
            {
                splitClock.Advance(TimeSpan.FromMilliseconds(100));
                splitMovement.Advance();
            }

            Assert.That(splitMovement.Position, Is.EqualTo(movement.Position));
            Assert.That(splitMovement.CurrentPosition, Is.EqualTo(movement.CurrentPosition));
            Assert.That(splitMovement.IsMoving, Is.EqualTo(movement.IsMoving));
        }

        [Test]
        public void PressAndReleaseBeforeAdvance_DoesNotCreateCoreBacklog()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);

            movement.SetMoveDirection(CardinalDirection.North);
            movement.SetMoveDirection(CardinalDirection.None);
            movement.Advance();
            clock.Advance(TimeSpan.FromMilliseconds(200));

            Assert.That(movement.Advance(), Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.Position, Is.EqualTo(GridSubcellPosition.AtCellCenter(Start)));
            Assert.That(movement.IsMoving, Is.False);
            Assert.That(movement.MoveDirection, Is.EqualTo(CardinalDirection.None));
            Assert.That(movement.LastCellSteps, Is.Empty);
        }

        [Test]
        public void ClearMoveIntent_StopsImmediatelyAndReleasesReservation()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);

            movement.SetMoveDirection(CardinalDirection.North);
            movement.ClearMoveIntent();
            clock.Advance(TimeSpan.FromMilliseconds(200));

            Assert.That(movement.Advance(), Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));

            movement.SetMoveDirection(CardinalDirection.North);
            movement.Advance();
            clock.Advance(TimeSpan.FromMilliseconds(50));
            movement.Advance();
            movement.ClearMoveIntent();
            GridSubcellPosition cleared = movement.Position;
            clock.Advance(TimeSpan.FromMilliseconds(150));

            Assert.That(movement.Advance(), Is.False);
            Assert.That(movement.Position, Is.EqualTo(cleared));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.IsMoving, Is.False);
            Assert.That(grid.TryGetActorMoveReservation(PlayerActor, out _), Is.False);
        }

        [Test]
        public void CellHistory_UsesHalfwayBoundaryForLatestObservedInterval()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(500));
            movement.Advance();

            Assert.That(
                movement.GetCurrentCellAt(TimeSpan.FromMilliseconds(99)),
                Is.EqualTo(Start));
            Assert.That(
                movement.GetCurrentCellAt(TimeSpan.FromMilliseconds(100)),
                Is.EqualTo(Start.Offset(0, 1)));
            Assert.That(
                movement.GetCurrentCellAt(TimeSpan.FromMilliseconds(299)),
                Is.EqualTo(Start.Offset(0, 1)));
            Assert.That(
                movement.GetCurrentCellAt(TimeSpan.FromMilliseconds(300)),
                Is.EqualTo(Start.Offset(0, 2)));
        }

        [TestCase(50, 0, 0)]
        [TestCase(150, 0, 1)]
        public void CancelMovement_ReleasesReservationAndKeepsBoundarySelectedCell(
            int elapsedMilliseconds,
            int expectedX,
            int expectedZ)
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(elapsedMilliseconds));
            movement.Advance();

            movement.CancelMovement();

            Assert.That(movement.IsMoving, Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(
                new GridPosition(expectedX, expectedZ)));
            Assert.That(
                grid.TryGetActorMoveReservation(PlayerActor, out _),
                Is.False);
        }

        [Test]
        public void WallAndBombDestinations_BlockWithoutMovingFromCellCenter()
        {
            GridState wallGrid = CreateFloorGrid();
            wallGrid.TrySetTerrain(Start.Offset(0, 1), GridTerrain.IndestructibleWall);
            var wallClock = new ManualGameClock();
            PlayerMovementSimulation wallMovement = CreateMovement(wallGrid, wallClock);
            wallMovement.SetMoveDirection(CardinalDirection.North);
            wallClock.Advance(TimeSpan.FromMilliseconds(100));

            GridState bombGrid = CreateFloorGrid();
            bombGrid.TryAddBomb(Start.Offset(1, 0));
            var bombClock = new ManualGameClock();
            PlayerMovementSimulation bombMovement = CreateMovement(bombGrid, bombClock);
            bombMovement.SetMoveDirection(CardinalDirection.East);
            bombClock.Advance(TimeSpan.FromMilliseconds(100));

            Assert.That(wallMovement.Advance(), Is.False);
            Assert.That(wallMovement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(wallMovement.Position, Is.EqualTo(GridSubcellPosition.AtCellCenter(Start)));
            Assert.That(bombMovement.Advance(), Is.False);
            Assert.That(bombMovement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(bombMovement.Position, Is.EqualTo(GridSubcellPosition.AtCellCenter(Start)));
        }

        [Test]
        public void BombUnderPlayerWithoutGrant_BlocksLeaving()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            BombSimulation bombs = CreateBombSimulation(grid, clock);
            Assert.That(bombs.TryPlaceBomb(CreateBombDefinition(), Start, PlayerActor, out _), Is.True);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(100));

            bool moved = movement.Advance();

            Assert.That(moved, Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.Position, Is.EqualTo(GridSubcellPosition.AtCellCenter(Start)));
            Assert.That(movement.HasBombPassThrough, Is.False);
        }

        [Test]
        public void OwnerPassThrough_AllowsOneBoundaryExitThenBombBlocksReentry()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            BombSimulation bombs = CreateBombSimulation(grid, clock);
            Assert.That(bombs.TryPlaceBomb(CreateBombDefinition(), Start, PlayerActor, out BombId bombId), Is.True);
            Assert.That(bombs.TryGetBomb(bombId, out BombSnapshot bomb), Is.True);
            movement.GrantBombPassThrough(bomb);

            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(110));
            bool exited = movement.Advance();
            GridSubcellPosition exitedPosition = movement.Position;

            movement.SetMoveDirection(CardinalDirection.South);
            clock.Advance(TimeSpan.FromMilliseconds(200));
            bool reentered = movement.Advance();

            Assert.That(exited, Is.True);
            Assert.That(movement.HasBombPassThrough, Is.False);
            Assert.That(reentered, Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start.Offset(0, 1)));
            Assert.That(movement.Position, Is.EqualTo(exitedPosition));
            Assert.That(grid.GetCell(Start).HasBomb, Is.True);
        }

        [Test]
        public void GrantBombPassThrough_RejectsNonOwner()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            BombSimulation bombs = CreateBombSimulation(grid, clock);
            Assert.That(bombs.TryPlaceBomb(CreateBombDefinition(), Start, OtherActor, out BombId bombId), Is.True);
            Assert.That(bombs.TryGetBomb(bombId, out BombSnapshot bomb), Is.True);

            Assert.Throws<InvalidOperationException>(() => movement.GrantBombPassThrough(bomb));
            Assert.That(movement.HasBombPassThrough, Is.False);
        }

        [Test]
        public void BombRemoval_ClearsUnusedPassThrough()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            BombSimulation bombs = CreateBombSimulation(grid, clock);
            Assert.That(bombs.TryPlaceBomb(CreateBombDefinition(), Start, PlayerActor, out BombId bombId), Is.True);
            Assert.That(bombs.TryGetBomb(bombId, out BombSnapshot bomb), Is.True);
            movement.GrantBombPassThrough(bomb);

            clock.Advance(TimeSpan.FromSeconds(1));
            BombExplosion explosion = bombs.ProcessDueBombs()[0];
            movement.NotifyBombRemoved(explosion.BombId);
            movement.SetMoveDirection(CardinalDirection.North);

            Assert.That(movement.HasBombPassThrough, Is.False);
            Assert.That(movement.Advance(), Is.True);
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Constructor_RejectsInvalidMovementSpeed(double cellsPerSecond)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerMovementSimulation(
                    CreateFloorGrid(),
                    new ManualGameClock(),
                    PlayerActor,
                    Start,
                    cellsPerSecond));
        }

        [Test]
        public void UndefinedDirection_IsRejectedWithoutChangingIntent()
        {
            PlayerMovementSimulation movement = CreateMovement(
                CreateFloorGrid(),
                new ManualGameClock());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                movement.SetMoveDirection((CardinalDirection)99));
            Assert.That(movement.MoveDirection, Is.EqualTo(CardinalDirection.None));
        }

        [Test]
        public void GridSubcellPosition_RejectsNonFiniteCoordinates()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GridSubcellPosition(double.NaN, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GridSubcellPosition(0d, double.PositiveInfinity));
        }

        [Test]
        public void MovementStep_RejectsTargetThatDoesNotMatchDirection()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerMovementStep(Start, Start.Offset(1, 0), CardinalDirection.North));
        }

        private static PlayerMovementSimulation CreateMovement(
            GridState grid,
            ManualGameClock clock)
        {
            return new PlayerMovementSimulation(
                grid,
                clock,
                PlayerActor,
                Start,
                CellsPerSecond);
        }

        private static BombSimulation CreateBombSimulation(
            GridState grid,
            ManualGameClock clock)
        {
            return new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(150));
        }

        private static BombDefinition CreateBombDefinition()
        {
            return new BombDefinition(
                new BombDefinitionId("pass-through-test"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(1),
                1);
        }

        private static GridState CreateFloorGrid()
        {
            var grid = new GridState();
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    grid.TrySetTerrain(new GridPosition(x, z), GridTerrain.Floor);
                }
            }

            return grid;
        }
    }
}
