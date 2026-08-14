using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class PlayerMovementSimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId OtherActor = new ActorId(2);
        private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(200);
        private static readonly GridPosition Start = new GridPosition(0, 0);

        [Test]
        public void Constructor_ClaimsStartingCellAsAuthoritativeActorPosition()
        {
            GridState grid = CreateFloorGrid();

            var movement = CreateMovement(grid, new ManualGameClock());

            Assert.That(movement.ActorId, Is.EqualTo(PlayerActor));
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(grid.GetCell(Start).HasActor, Is.True);
            Assert.That(grid.TryGetActorPosition(PlayerActor, out GridPosition stored), Is.True);
            Assert.That(stored, Is.EqualTo(Start));
        }

        [Test]
        public void FirstHeldDirection_MovesImmediatelyThenObservesCadence()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);

            bool firstMoved = movement.TryAdvance(out PlayerMovementStep firstStep);
            bool repeatedImmediately = movement.TryAdvance(out _);
            clock.Advance(StepInterval - TimeSpan.FromMilliseconds(1));
            bool repeatedTooEarly = movement.TryAdvance(out _);
            clock.Advance(TimeSpan.FromMilliseconds(1));
            bool secondMoved = movement.TryAdvance(out PlayerMovementStep secondStep);

            Assert.That(firstMoved, Is.True);
            Assert.That(firstStep, Is.EqualTo(new PlayerMovementStep(Start, Start.Offset(0, 1), CardinalDirection.North)));
            Assert.That(repeatedImmediately, Is.False);
            Assert.That(repeatedTooEarly, Is.False);
            Assert.That(secondMoved, Is.True);
            Assert.That(secondStep.To, Is.EqualTo(Start.Offset(0, 2)));
        }

        [Test]
        public void DirectionChange_UsesNewDirectionAtNextScheduledStep()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            movement.TryAdvance(out _);

            movement.SetMoveDirection(CardinalDirection.East);
            clock.Advance(StepInterval);

            Assert.That(movement.TryAdvance(out PlayerMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(step.Direction, Is.EqualTo(CardinalDirection.East));
        }

        [Test]
        public void ShortPerpendicularTap_IsConsumedOnceBeforeHeldDirectionResumes()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            Assert.That(movement.TryAdvance(out _), Is.True);

            clock.Advance(TimeSpan.FromMilliseconds(50));
            movement.SetMoveDirection(CardinalDirection.East);
            clock.Advance(TimeSpan.FromMilliseconds(50));
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(TimeSpan.FromMilliseconds(100));

            Assert.That(movement.TryAdvance(out PlayerMovementStep bufferedTurn), Is.True);
            Assert.That(bufferedTurn.Direction, Is.EqualTo(CardinalDirection.East));
            Assert.That(bufferedTurn.To, Is.EqualTo(new GridPosition(1, 1)));

            clock.Advance(StepInterval);

            Assert.That(movement.TryAdvance(out PlayerMovementStep resumedHold), Is.True);
            Assert.That(resumedHold.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(resumedHold.To, Is.EqualTo(new GridPosition(1, 2)));
        }

        [Test]
        public void LatestTurn_ReplacesOlderBufferedTurn()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            Assert.That(movement.TryAdvance(out _), Is.True);

            movement.SetMoveDirection(CardinalDirection.East);
            movement.SetMoveDirection(CardinalDirection.South);
            clock.Advance(StepInterval);

            Assert.That(movement.TryAdvance(out PlayerMovementStep step), Is.True);
            Assert.That(step.Direction, Is.EqualTo(CardinalDirection.South));
            Assert.That(step.To, Is.EqualTo(Start));
        }

        [Test]
        public void BlockedBufferedTurn_FallsBackToStillHeldDirectionInSameStep()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            Assert.That(movement.TryAdvance(out _), Is.True);
            Assert.That(
                grid.TrySetTerrain(new GridPosition(1, 1), GridTerrain.IndestructibleWall),
                Is.True);

            movement.SetMoveDirection(CardinalDirection.East);
            movement.SetMoveDirection(CardinalDirection.North);
            clock.Advance(StepInterval);

            Assert.That(movement.TryAdvance(out PlayerMovementStep step), Is.True);
            Assert.That(step.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(step.To, Is.EqualTo(new GridPosition(0, 2)));
        }

        [Test]
        public void StopAndImmediateResume_DoesNotBypassExistingCadence()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            movement.SetMoveDirection(CardinalDirection.North);
            movement.TryAdvance(out _);

            movement.SetMoveDirection(CardinalDirection.None);
            movement.SetMoveDirection(CardinalDirection.North);

            Assert.That(movement.TryAdvance(out _), Is.False);
            clock.Advance(StepInterval);
            Assert.That(movement.TryAdvance(out _), Is.True);
        }

        [Test]
        public void WallAndBombDestinations_BlockWithoutChangingCurrentPosition()
        {
            GridState wallGrid = CreateFloorGrid();
            wallGrid.TrySetTerrain(Start.Offset(0, 1), GridTerrain.IndestructibleWall);
            PlayerMovementSimulation wallMovement = CreateMovement(wallGrid, new ManualGameClock());
            wallMovement.SetMoveDirection(CardinalDirection.North);

            GridState bombGrid = CreateFloorGrid();
            bombGrid.TryAddBomb(Start.Offset(1, 0));
            PlayerMovementSimulation bombMovement = CreateMovement(bombGrid, new ManualGameClock());
            bombMovement.SetMoveDirection(CardinalDirection.East);

            Assert.That(wallMovement.TryAdvance(out _), Is.False);
            Assert.That(wallMovement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(bombMovement.TryAdvance(out _), Is.False);
            Assert.That(bombMovement.CurrentPosition, Is.EqualTo(Start));
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

            bool moved = movement.TryAdvance(out _);

            Assert.That(moved, Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(movement.HasBombPassThrough, Is.False);
        }

        [Test]
        public void OwnerPassThrough_AllowsOneExitThenBombBlocksReentry()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            PlayerMovementSimulation movement = CreateMovement(grid, clock);
            BombSimulation bombs = CreateBombSimulation(grid, clock);
            Assert.That(bombs.TryPlaceBomb(CreateBombDefinition(), Start, PlayerActor, out BombId bombId), Is.True);
            Assert.That(bombs.TryGetBomb(bombId, out BombSnapshot bomb), Is.True);
            movement.GrantBombPassThrough(bomb);

            movement.SetMoveDirection(CardinalDirection.North);
            bool exited = movement.TryAdvance(out _);
            movement.SetMoveDirection(CardinalDirection.South);
            clock.Advance(StepInterval);
            bool reentered = movement.TryAdvance(out _);

            Assert.That(exited, Is.True);
            Assert.That(movement.HasBombPassThrough, Is.False);
            Assert.That(reentered, Is.False);
            Assert.That(movement.CurrentPosition, Is.EqualTo(Start.Offset(0, 1)));
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
            Assert.That(movement.TryAdvance(out _), Is.True);
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
                StepInterval);
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
