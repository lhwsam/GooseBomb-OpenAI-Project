using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class SelfDestructEnemySimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId SelfDestructActor = new ActorId(6);
        private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(500);

        [Test]
        public void Definition_StoresChaseWarningPrimeAndCrossDetonation()
        {
            BombDefinition bomb = CreateBombDefinition();

            var definition = new SelfDestructEnemyDefinition(
                new EnemyDefinitionId("prototype-self-destruct"),
                StepInterval,
                3,
                1,
                bomb);

            Assert.That(
                definition.Id,
                Is.EqualTo(new EnemyDefinitionId("prototype-self-destruct")));
            Assert.That(definition.ChaseStepInterval, Is.EqualTo(StepInterval));
            Assert.That(definition.WarningDistance, Is.EqualTo(3));
            Assert.That(definition.PrimeDistance, Is.EqualTo(1));
            Assert.That(definition.DetonationBombDefinition, Is.SameAs(bomb));
        }

        [Test]
        public void Definition_RejectsInvalidChaseDistancesAndNonCrossDetonation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SelfDestructEnemyDefinition(
                    new EnemyDefinitionId("test-self-destruct"),
                    TimeSpan.Zero,
                    3,
                    1,
                    CreateBombDefinition()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SelfDestructEnemyDefinition(
                    new EnemyDefinitionId("test-self-destruct"),
                    StepInterval,
                    0,
                    1,
                    CreateBombDefinition()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SelfDestructEnemyDefinition(
                    new EnemyDefinitionId("test-self-destruct"),
                    StepInterval,
                    3,
                    0,
                    CreateBombDefinition()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SelfDestructEnemyDefinition(
                    new EnemyDefinitionId("test-self-destruct"),
                    StepInterval,
                    3,
                    3,
                    CreateBombDefinition()));
            Assert.Throws<ArgumentException>(() =>
                new SelfDestructEnemyDefinition(
                    new EnemyDefinitionId("test-self-destruct"),
                    StepInterval,
                    3,
                    1,
                    new BombDefinition(
                        new BombDefinitionId("test-area"),
                        BombExplosionShape.SquareArea,
                        TimeSpan.FromSeconds(1),
                        1)));
        }

        [Test]
        public void Chase_MovesTowardCurrentPlayerWithDeterministicShortestPath()
        {
            var clock = new ManualGameClock();
            SelfDestructEnemySimulation enemy = CreateSimulation(
                clock,
                new GridPosition(0, -3),
                new GridPosition(3, 0));

            SelfDestructEnemyAdvanceResult result = enemy.Advance();

            Assert.That(result.HasMovement, Is.True);
            Assert.That(result.TargetPosition, Is.EqualTo(new GridPosition(0, -3)));
            Assert.That(result.Movement.To, Is.EqualTo(new GridPosition(3, -1)));
            Assert.That(enemy.State, Is.EqualTo(SelfDestructEnemyState.Chase));
        }

        [Test]
        public void Chase_EnteringWarningDistanceContinuesMovingAndChangesState()
        {
            var clock = new ManualGameClock();
            SelfDestructEnemySimulation enemy = CreateSimulation(
                clock,
                GridPositionAtOrigin(),
                new GridPosition(0, 4));

            SelfDestructEnemyAdvanceResult result = enemy.Advance();

            Assert.That(result.HasMovement, Is.True);
            Assert.That(result.Movement.To, Is.EqualTo(new GridPosition(0, 3)));
            Assert.That(result.HasStateTransition, Is.True);
            Assert.That(result.State, Is.EqualTo(SelfDestructEnemyState.WarningChase));
            Assert.That(result.ShouldArm, Is.False);
        }

        [Test]
        public void WarningChase_PlayerEscapesWarningDistanceReturnsToChase()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, GridPositionAtOrigin()), Is.True);
            var enemy = new SelfDestructEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                SelfDestructActor,
                PlayerActor,
                new GridPosition(0, 4));
            Assert.That(enemy.Advance().State, Is.EqualTo(SelfDestructEnemyState.WarningChase));
            Assert.That(grid.TryRemoveActor(PlayerActor), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -4)), Is.True);

            clock.Advance(StepInterval);
            SelfDestructEnemyAdvanceResult result = enemy.Advance();

            Assert.That(result.HasMovement, Is.True);
            Assert.That(result.HasStateTransition, Is.True);
            Assert.That(result.PreviousState, Is.EqualTo(SelfDestructEnemyState.WarningChase));
            Assert.That(result.State, Is.EqualTo(SelfDestructEnemyState.Chase));
        }

        [Test]
        public void Chase_EnteringPrimeDistanceWaitsOneCadenceBeforeStoppingAndArming()
        {
            var clock = new ManualGameClock();
            SelfDestructEnemySimulation enemy = CreateSimulation(
                clock,
                GridPositionAtOrigin(),
                new GridPosition(0, 2));

            SelfDestructEnemyAdvanceResult arrival = enemy.Advance();

            Assert.That(arrival.HasMovement, Is.True);
            Assert.That(arrival.Movement.To, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(arrival.State, Is.EqualTo(SelfDestructEnemyState.WarningChase));
            Assert.That(arrival.ShouldArm, Is.False);
            Assert.That(enemy.Advance().HasActivity, Is.False);

            clock.Advance(StepInterval);
            SelfDestructEnemyAdvanceResult telegraph = enemy.Advance();

            Assert.That(telegraph.HasMovement, Is.False);
            Assert.That(telegraph.HasStateTransition, Is.True);
            Assert.That(telegraph.State, Is.EqualTo(SelfDestructEnemyState.Telegraph));
            Assert.That(telegraph.TargetPosition, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(telegraph.ShouldArm, Is.True);
            enemy.ConfirmArmed(CreateBombId(10));
            Assert.That(enemy.TelegraphCells, Does.Contain(new GridPosition(0, 1)));
            Assert.That(enemy.TelegraphCells, Does.Contain(GridPositionAtOrigin()));
        }

        [Test]
        public void WarningChase_PlayerLeavesBeforePrimeCadenceContinuesPursuit()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, GridPositionAtOrigin()), Is.True);
            var enemy = new SelfDestructEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                SelfDestructActor,
                PlayerActor,
                new GridPosition(0, 2));
            Assert.That(enemy.Advance().Movement.To, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(grid.TryRemoveActor(PlayerActor), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -2)), Is.True);

            clock.Advance(StepInterval);
            SelfDestructEnemyAdvanceResult result = enemy.Advance();

            Assert.That(result.HasMovement, Is.True);
            Assert.That(result.Movement.To, Is.EqualTo(GridPositionAtOrigin()));
            Assert.That(result.ShouldArm, Is.False);
            Assert.That(enemy.State, Is.EqualTo(SelfDestructEnemyState.WarningChase));
        }

        [Test]
        public void Chase_RoutesAroundBlockedDirectCell()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TrySetTerrain(
                new GridPosition(0, 1),
                GridTerrain.IndestructibleWall), Is.True);
            Assert.That(grid.TryAddActor(PlayerActor, GridPositionAtOrigin()), Is.True);
            var enemy = new SelfDestructEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                SelfDestructActor,
                PlayerActor,
                new GridPosition(0, 2));

            SelfDestructEnemyAdvanceResult result = enemy.Advance();

            Assert.That(result.HasMovement, Is.True);
            Assert.That(result.Movement.To, Is.EqualTo(new GridPosition(1, 2)));
        }

        [Test]
        public void Chase_NoReachablePathWaitsWithoutInventingMovement()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            var enemyPosition = new GridPosition(3, 3);
            foreach (GridPosition wall in new[]
            {
                enemyPosition.Offset(0, 1),
                enemyPosition.Offset(1, 0),
                enemyPosition.Offset(0, -1),
                enemyPosition.Offset(-1, 0),
            })
            {
                Assert.That(grid.TrySetTerrain(wall, GridTerrain.IndestructibleWall), Is.True);
            }
            Assert.That(grid.TryAddActor(PlayerActor, GridPositionAtOrigin()), Is.True);
            var enemy = new SelfDestructEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                SelfDestructActor,
                PlayerActor,
                enemyPosition);

            SelfDestructEnemyAdvanceResult result = enemy.Advance();

            Assert.That(result.HasActivity, Is.False);
            Assert.That(enemy.CurrentPosition, Is.EqualTo(enemyPosition));
        }

        [Test]
        public void Chase_WaitsUntilCadenceBeforeSecondStep()
        {
            var clock = new ManualGameClock();
            SelfDestructEnemySimulation enemy = CreateSimulation(
                clock,
                new GridPosition(0, -3),
                new GridPosition(3, 0));
            Assert.That(enemy.Advance().HasMovement, Is.True);

            clock.Advance(StepInterval - TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().HasActivity, Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().HasMovement, Is.True);
        }

        [Test]
        public void PlayerExplosion_TriggersCurrentCellFromEitherChaseState()
        {
            var clock = new ManualGameClock();
            SelfDestructEnemySimulation enemy = CreateSimulation(
                clock,
                GridPositionAtOrigin(),
                new GridPosition(0, 4));
            Assert.That(enemy.Advance().State, Is.EqualTo(SelfDestructEnemyState.WarningChase));

            bool triggered = enemy.TryTriggerFromExplosion(
                CreateBombId(7),
                out SelfDestructEnemyAdvanceResult result);

            Assert.That(triggered, Is.True);
            Assert.That(result.ShouldArm, Is.True);
            Assert.That(result.TargetPosition, Is.EqualTo(new GridPosition(0, 3)));
            Assert.That(enemy.IsDetonated, Is.False);
            Assert.That(enemy.TriggeringExplosionId, Is.EqualTo(CreateBombId(7)));
            Assert.That(enemy.TryTriggerFromExplosion(CreateBombId(8), out _), Is.False);
        }

        [Test]
        public void Detonation_RequiresConfirmedMatchingBombAndTransitionsOnce()
        {
            var clock = new ManualGameClock();
            SelfDestructEnemySimulation enemy = CreateSimulation(
                clock,
                new GridPosition(0, -3),
                new GridPosition(3, 0));
            Assert.That(enemy.TryTriggerFromExplosion(CreateBombId(7), out _), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                enemy.CompleteDetonation(CreateBombId(9)));

            enemy.ConfirmArmed(CreateBombId(10));
            Assert.Throws<InvalidOperationException>(() =>
                enemy.CompleteDetonation(CreateBombId(9)));

            SelfDestructEnemyAdvanceResult result =
                enemy.CompleteDetonation(CreateBombId(10));

            Assert.That(result.HasStateTransition, Is.True);
            Assert.That(result.State, Is.EqualTo(SelfDestructEnemyState.Detonated));
            Assert.That(enemy.IsDetonated, Is.True);
            Assert.That(enemy.TelegraphCells, Is.Empty);
            Assert.Throws<InvalidOperationException>(() =>
                enemy.CompleteDetonation(CreateBombId(10)));
        }

        private static SelfDestructEnemySimulation CreateSimulation(
            ManualGameClock clock,
            GridPosition playerPosition,
            GridPosition enemyPosition)
        {
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, playerPosition), Is.True);
            return new SelfDestructEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                SelfDestructActor,
                PlayerActor,
                enemyPosition);
        }

        private static SelfDestructEnemyDefinition CreateDefinition()
        {
            return new SelfDestructEnemyDefinition(
                new EnemyDefinitionId("test-self-destruct"),
                StepInterval,
                3,
                1,
                CreateBombDefinition());
        }

        private static BombDefinition CreateBombDefinition()
        {
            return new BombDefinition(
                new BombDefinitionId("test-self-destruct-blast"),
                BombExplosionShape.Cross,
                TimeSpan.FromMilliseconds(750),
                1);
        }

        private static BombId CreateBombId(int sequence)
        {
            var grid = new GridState();
            var clock = new ManualGameClock();
            var simulation = new BombSimulation(
                grid,
                clock,
                TimeSpan.FromMilliseconds(100));
            BombId result = default;
            BombDefinition definition = CreateBombDefinition();
            for (int index = 0; index < sequence; index++)
            {
                GridPosition position = new GridPosition(index, 0);
                Assert.That(grid.TrySetTerrain(position, GridTerrain.Floor), Is.True);
                Assert.That(
                    simulation.TryPlaceBomb(
                        definition,
                        position,
                        PlayerActor,
                        out result),
                    Is.True);
            }

            return result;
        }

        private static GridState CreateFloorGrid()
        {
            var grid = new GridState();
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    Assert.That(
                        grid.TrySetTerrain(
                            new GridPosition(x, z),
                            GridTerrain.Floor),
                        Is.True);
                }
            }
            return grid;
        }

        private static GridPosition GridPositionAtOrigin()
        {
            return new GridPosition(0, 0);
        }
    }
}
