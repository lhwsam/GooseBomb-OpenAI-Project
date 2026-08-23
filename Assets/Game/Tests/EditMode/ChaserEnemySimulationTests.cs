using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class ChaserEnemySimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId ChaserActor = new ActorId(2);
        private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(500);

        [Test]
        public void Definition_StoresStableIdHealthCadenceAndCommitment()
        {
            var id = new EnemyDefinitionId("prototype-chaser");
            var definition = new ChaserEnemyDefinition(id, 1, 1, StepInterval, 2);

            Assert.That(definition.Id, Is.EqualTo(id));
            Assert.That(definition.MaxHealth, Is.EqualTo(1));
            Assert.That(definition.ContactDamage, Is.EqualTo(1));
            Assert.That(definition.StepInterval, Is.EqualTo(StepInterval));
            Assert.That(definition.DirectionCommitmentSteps, Is.EqualTo(2));
        }

        [Test]
        public void DefinitionId_UsesOrdinalValueEquality()
        {
            var first = new EnemyDefinitionId("prototype-chaser");
            var same = new EnemyDefinitionId("prototype-chaser");
            var differentCase = new EnemyDefinitionId("Prototype-Chaser");

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(differentCase));
            Assert.That(first.ToString(), Is.EqualTo("prototype-chaser"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveHealth(int health)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChaserEnemyDefinition(CreateDefinitionId(), health, 1, StepInterval, 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveContactDamage(int contactDamage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChaserEnemyDefinition(
                    CreateDefinitionId(),
                    1,
                    contactDamage,
                    StepInterval,
                    2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveCommitment(int commitment)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChaserEnemyDefinition(CreateDefinitionId(), 1, 1, StepInterval, commitment));
        }

        [Test]
        public void FirstStep_IsImmediateAndNorthWinsNewDirectionTie()
        {
            var clock = new ManualGameClock();
            ChaserEnemySimulation chaser = CreateSimulation(
                clock,
                new GridPosition(0, 0),
                new GridPosition(1, -1));

            bool moved = chaser.TryAdvance(out EnemyMovementStep step);

            Assert.That(moved, Is.True);
            Assert.That(step.ActorId, Is.EqualTo(ChaserActor));
            Assert.That(step.From, Is.EqualTo(new GridPosition(1, -1)));
            Assert.That(step.To, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(step.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(chaser.RemainingCommittedSteps, Is.EqualTo(1));
            Assert.That(chaser.CurrentPosition, Is.EqualTo(new GridPosition(1, -1)));

            clock.Advance(TimeSpan.FromTicks(StepInterval.Ticks / 2));
            Assert.That(chaser.TryAdvance(out _), Is.False);
            Assert.That(chaser.CurrentPosition, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(chaser.Position.Z, Is.EqualTo(-0.5d).Within(0.000001d));
        }

        [Test]
        public void Cadence_AllowsAtMostOneStepAtExactIntervals()
        {
            var clock = new ManualGameClock();
            ChaserEnemySimulation chaser = CreateSimulation(
                clock,
                new GridPosition(0, 4),
                new GridPosition(0, -4));

            Assert.That(chaser.TryAdvance(out _), Is.True);
            Assert.That(chaser.TryAdvance(out _), Is.False);
            clock.Advance(StepInterval - TimeSpan.FromTicks(1));
            Assert.That(chaser.TryAdvance(out _), Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(chaser.TryAdvance(out _), Is.True);
        }

        [Test]
        public void CommittedDirection_IsHeldForConfiguredSuccessfulSteps()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, 4));
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(0, 0));
            Assert.That(chaser.TryAdvance(out EnemyMovementStep first), Is.True);
            Assert.That(chaser.MovementTransition.Movement, Is.EqualTo(first));
            Assert.That(chaser.MovementTransition.StartedAt, Is.EqualTo(TimeSpan.Zero));
            Assert.That(chaser.MovementTransition.EndsAt, Is.EqualTo(StepInterval));
            Assert.That(first.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(grid.TryRemoveActor(PlayerActor), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(4, 0)), Is.True);

            clock.Advance(StepInterval);
            Assert.That(chaser.TryAdvance(out EnemyMovementStep second), Is.True);

            Assert.That(second.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(second.To, Is.EqualTo(new GridPosition(0, 2)));
            Assert.That(chaser.RemainingCommittedSteps, Is.Zero);
        }

        [Test]
        public void ExpiredCommitment_RepathsTowardMovedTarget()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, 4));
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(0, 0));
            chaser.TryAdvance(out _);
            clock.Advance(StepInterval);
            chaser.TryAdvance(out _);
            Assert.That(grid.TryRemoveActor(PlayerActor), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(4, 0)), Is.True);

            clock.Advance(StepInterval);
            Assert.That(chaser.TryAdvance(out EnemyMovementStep repathed), Is.True);

            Assert.That(repathed.Direction, Is.EqualTo(CardinalDirection.East));
            Assert.That(repathed.To, Is.EqualTo(new GridPosition(1, 2)));
        }

        [Test]
        public void BlockedCommittedDirection_RepathsWithoutEnteringBombCell()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, 4));
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(0, 0));
            chaser.TryAdvance(out _);
            Assert.That(grid.TryAddBomb(new GridPosition(0, 2)), Is.True);

            clock.Advance(StepInterval);
            Assert.That(chaser.TryAdvance(out EnemyMovementStep repathed), Is.True);

            Assert.That(repathed.Direction, Is.EqualTo(CardinalDirection.East));
            Assert.That(repathed.To, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(grid.GetCell(new GridPosition(0, 2)).HasBomb, Is.True);
        }

        [Test]
        public void BfsPathfinding_AvoidsDeadEndAndStopsCommitmentBeforeOvershoot()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            GridPosition[] deadEndWalls =
            {
                new GridPosition(1, 0),
                new GridPosition(1, 1),
                new GridPosition(2, 2),
                new GridPosition(3, 1),
            };
            foreach (GridPosition wall in deadEndWalls)
            {
                Assert.That(
                    grid.TrySetTerrain(wall, GridTerrain.IndestructibleWall),
                    Is.True);
            }

            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 0)), Is.True);
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(2, 0));

            Assert.That(chaser.TryAdvance(out EnemyMovementStep first), Is.True);
            Assert.That(first.Direction, Is.EqualTo(CardinalDirection.South));
            Assert.That(first.To, Is.EqualTo(new GridPosition(2, -1)));
            Assert.That(chaser.RemainingCommittedSteps, Is.EqualTo(1));

            clock.Advance(StepInterval);
            Assert.That(chaser.TryAdvance(out EnemyMovementStep second), Is.True);
            Assert.That(
                second.Direction,
                Is.EqualTo(CardinalDirection.West),
                "A commitment must end before it leaves the planned shortest route.");
            Assert.That(second.To, Is.EqualTo(new GridPosition(1, -1)));

            clock.Advance(StepInterval);
            Assert.That(chaser.TryAdvance(out EnemyMovementStep third), Is.True);
            Assert.That(third.Direction, Is.EqualTo(CardinalDirection.West));
            Assert.That(third.To, Is.EqualTo(new GridPosition(0, -1)));

            clock.Advance(StepInterval);
            Assert.That(
                chaser.TryAdvance(out _),
                Is.False,
                "The chaser must reach adjacency instead of returning to the dead-end loop.");
        }

        [Test]
        public void BfsPathfinding_WaitsWhenTargetHasNoReachableRoute()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            GridPosition[] targetWalls =
            {
                new GridPosition(0, 1),
                new GridPosition(1, 0),
                new GridPosition(0, -1),
                new GridPosition(-1, 0),
            };
            foreach (GridPosition wall in targetWalls)
            {
                Assert.That(
                    grid.TrySetTerrain(wall, GridTerrain.IndestructibleWall),
                    Is.True);
            }

            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 0)), Is.True);
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(2, 2));

            Assert.That(chaser.TryAdvance(out _), Is.False);
            Assert.That(chaser.CurrentPosition, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(chaser.CurrentDirection, Is.EqualTo(CardinalDirection.None));
            Assert.That(chaser.LocomotionState, Is.EqualTo(EnemyLocomotionState.Idle));
            Assert.That(chaser.RemainingCommittedSteps, Is.Zero);
        }

        [Test]
        public void CardinalAdjacency_StopsWithoutEnteringPlayerCell()
        {
            var clock = new ManualGameClock();
            ChaserEnemySimulation chaser = CreateSimulation(
                clock,
                new GridPosition(0, 0),
                new GridPosition(0, -1));

            Assert.That(chaser.TryAdvance(out _), Is.False);
            Assert.That(chaser.CurrentPosition, Is.EqualTo(new GridPosition(0, -1)));
            Assert.That(chaser.LocomotionState, Is.EqualTo(EnemyLocomotionState.Idle));
            Assert.That(chaser.CanDealContactDamage, Is.True);
        }

        [Test]
        public void ContactDamage_WaitsForStepArrivalAndRequiresPersistingAdjacency()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 0)), Is.True);
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(0, 2));

            Assert.That(chaser.CanDealContactDamage, Is.False);
            Assert.That(chaser.TryAdvance(out EnemyMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(chaser.CanDealContactDamage, Is.False);

            clock.Advance(StepInterval - TimeSpan.FromTicks(1));
            Assert.That(chaser.CanDealContactDamage, Is.False);

            Assert.That(grid.TryRemoveActor(PlayerActor), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -1)), Is.True);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(chaser.CanDealContactDamage, Is.False);

            Assert.That(grid.TryRemoveActor(PlayerActor), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 0)), Is.True);
            Assert.That(chaser.CanDealContactDamage, Is.True);
        }

        [Test]
        public void ClockMovingBackwards_IsRejectedWithoutMovement()
        {
            var grid = CreateFloorGrid();
            var clock = new MutableGameClock(TimeSpan.FromSeconds(2));
            grid.TryAddActor(PlayerActor, new GridPosition(0, 4));
            var chaser = new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                new GridPosition(0, 0));
            clock.Now = TimeSpan.FromSeconds(1);

            Assert.Throws<InvalidOperationException>(() => chaser.TryAdvance(out _));
            Assert.Throws<InvalidOperationException>(() =>
            {
                bool _ = chaser.CanDealContactDamage;
            });
            Assert.That(chaser.CurrentPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void Constructor_RejectsMissingTargetAndInvalidSpawn()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();

            Assert.Throws<InvalidOperationException>(() =>
                new ChaserEnemySimulation(
                    grid,
                    clock,
                    CreateDefinition(),
                    ChaserActor,
                    PlayerActor,
                    new GridPosition(0, 0)));

            grid.TryAddActor(PlayerActor, new GridPosition(0, 0));
            grid.TrySetTerrain(new GridPosition(1, 0), GridTerrain.IndestructibleWall);
            Assert.Throws<InvalidOperationException>(() =>
                new ChaserEnemySimulation(
                    grid,
                    clock,
                    CreateDefinition(),
                    ChaserActor,
                    PlayerActor,
                    new GridPosition(1, 0)));
        }

        private static ChaserEnemySimulation CreateSimulation(
            IGameClock clock,
            GridPosition playerPosition,
            GridPosition chaserPosition)
        {
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, playerPosition), Is.True);
            return new ChaserEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChaserActor,
                PlayerActor,
                chaserPosition);
        }

        private static GridState CreateFloorGrid()
        {
            var grid = new GridState();
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    grid.TrySetTerrain(new GridPosition(x, z), GridTerrain.Floor);
                }
            }

            return grid;
        }

        private static EnemyDefinitionId CreateDefinitionId()
        {
            return new EnemyDefinitionId("prototype-chaser");
        }

        private static ChaserEnemyDefinition CreateDefinition()
        {
            return new ChaserEnemyDefinition(CreateDefinitionId(), 1, 1, StepInterval, 2);
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
