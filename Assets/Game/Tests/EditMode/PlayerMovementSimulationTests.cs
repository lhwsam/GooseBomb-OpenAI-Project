using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class PlayerMovementSimulationTests
    {
        private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(200);
        private static readonly GridPosition Start = new GridPosition(0, 0);

        [Test]
        public void Constructor_ClaimsStartingCellAsAuthoritativeActorPosition()
        {
            GridState grid = CreateFloorGrid();

            var movement = new PlayerMovementSimulation(
                grid,
                new ManualGameClock(),
                Start,
                StepInterval);

            Assert.That(movement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(grid.GetCell(Start).HasActor, Is.True);
        }

        [Test]
        public void FirstHeldDirection_MovesImmediatelyThenObservesCadence()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            var movement = new PlayerMovementSimulation(grid, clock, Start, StepInterval);
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
            var movement = new PlayerMovementSimulation(grid, clock, Start, StepInterval);
            movement.SetMoveDirection(CardinalDirection.North);
            movement.TryAdvance(out _);

            movement.SetMoveDirection(CardinalDirection.East);
            clock.Advance(StepInterval);

            Assert.That(movement.TryAdvance(out PlayerMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(step.Direction, Is.EqualTo(CardinalDirection.East));
        }

        [Test]
        public void StopAndImmediateResume_DoesNotBypassExistingCadence()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            var movement = new PlayerMovementSimulation(grid, clock, Start, StepInterval);
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
            var wallMovement = new PlayerMovementSimulation(
                wallGrid,
                new ManualGameClock(),
                Start,
                StepInterval);
            wallMovement.SetMoveDirection(CardinalDirection.North);

            GridState bombGrid = CreateFloorGrid();
            bombGrid.TryAddOccupancy(Start.Offset(1, 0), GridOccupancy.Bomb);
            var bombMovement = new PlayerMovementSimulation(
                bombGrid,
                new ManualGameClock(),
                Start,
                StepInterval);
            bombMovement.SetMoveDirection(CardinalDirection.East);

            Assert.That(wallMovement.TryAdvance(out _), Is.False);
            Assert.That(wallMovement.CurrentPosition, Is.EqualTo(Start));
            Assert.That(bombMovement.TryAdvance(out _), Is.False);
            Assert.That(bombMovement.CurrentPosition, Is.EqualTo(Start));
        }

        [Test]
        public void UndefinedDirection_IsRejectedWithoutChangingIntent()
        {
            var movement = new PlayerMovementSimulation(
                CreateFloorGrid(),
                new ManualGameClock(),
                Start,
                StepInterval);

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
