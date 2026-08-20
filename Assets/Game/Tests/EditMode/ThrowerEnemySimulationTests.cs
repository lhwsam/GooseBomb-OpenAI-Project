using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class ThrowerEnemySimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId ThrowerActor = new ActorId(7);
        private static readonly TimeSpan MoveInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan TelegraphDuration = TimeSpan.FromSeconds(0.3);
        private static readonly TimeSpan FlightDuration = TimeSpan.FromSeconds(0.45);
        private static readonly TimeSpan RecoveryDuration = TimeSpan.FromSeconds(0.75);
        private const int BombsPerVolley = 3;

        [Test]
        public void Definition_StoresValidatedTimingHealthAndBomb()
        {
            BombDefinition bomb = CreateBomb();
            ThrowerEnemyDefinition definition = CreateDefinition(bomb);

            Assert.That(definition.Id, Is.EqualTo(new EnemyDefinitionId("test-thrower")));
            Assert.That(definition.MoveStepInterval, Is.EqualTo(MoveInterval));
            Assert.That(definition.TelegraphDuration, Is.EqualTo(TelegraphDuration));
            Assert.That(definition.FlightDuration, Is.EqualTo(FlightDuration));
            Assert.That(definition.RecoveryDuration, Is.EqualTo(RecoveryDuration));
            Assert.That(definition.MaxHealth, Is.EqualTo(1));
            Assert.That(definition.BombsPerVolley, Is.EqualTo(BombsPerVolley));
            Assert.That(definition.BombDefinition, Is.SameAs(bomb));
        }

        [Test]
        public void Definition_RejectsNonPositiveDurationsAndHealth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThrowerEnemyDefinition(
                    new EnemyDefinitionId("test"),
                    TimeSpan.Zero,
                    TelegraphDuration,
                    FlightDuration,
                    RecoveryDuration,
                    1,
                    BombsPerVolley,
                    CreateBomb()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThrowerEnemyDefinition(
                    new EnemyDefinitionId("test"),
                    MoveInterval,
                    TelegraphDuration,
                    FlightDuration,
                    RecoveryDuration,
                    0,
                    BombsPerVolley,
                    CreateBomb()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThrowerEnemyDefinition(
                    new EnemyDefinitionId("test"),
                    MoveInterval,
                    TelegraphDuration,
                    FlightDuration,
                    RecoveryDuration,
                    1,
                    0,
                    CreateBomb()));
        }

        [Test]
        public void Track_AfterReachingFirstAnchorLocksNearestAuthoredRetreatInsteadOfPlayerCell()
        {
            var clock = new ManualGameClock();
            ThrowerEnemySimulation simulation = CreateSimulation(
                clock,
                new GridPosition(2, -1),
                new GridPosition(0, 1));

            Assert.That(simulation.Advance().HasMovement, Is.True);

            ThrowerEnemyAdvanceResult result = simulation.Advance();

            Assert.That(result.HasStateTransition, Is.True);
            Assert.That(result.State, Is.EqualTo(ThrowerEnemyState.Telegraph));
            Assert.That(result.LockedTarget, Is.EqualTo(new GridPosition(3, -2)));
            Assert.That(result.LockedTarget, Is.Not.EqualTo(new GridPosition(2, -1)));
            Assert.That(
                result.LockedTargets,
                Is.EqualTo(new[]
                {
                    new GridPosition(3, -2),
                    new GridPosition(0, 0),
                    new GridPosition(4, 1),
                }));
        }

        [Test]
        public void Track_FromDistinctStagingSpawnMovesBeforeFirstTelegraph()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(4, 0)), Is.True);
            var simulation = new ThrowerEnemySimulation(
                grid,
                clock,
                CreateDefinition(CreateBomb()),
                ThrowerActor,
                PlayerActor,
                new GridPosition(3, 2),
                new[]
                {
                    new GridPosition(0, 3),
                    new GridPosition(-3, 2),
                },
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(-3, -2),
                    new GridPosition(2, -3),
                    new GridPosition(-4, 1),
                    new GridPosition(4, 1),
                    new GridPosition(0, 2),
                });

            ThrowerEnemyAdvanceResult first = simulation.Advance();

            Assert.That(first.HasMovement, Is.True);
            Assert.That(first.HasStateTransition, Is.False);
            Assert.That(first.State, Is.EqualTo(ThrowerEnemyState.Track));
            Assert.That(first.Movement.To, Is.EqualTo(new GridPosition(3, 3)));
            Assert.That(simulation.CurrentFiringAnchor, Is.EqualTo(new GridPosition(0, 3)));

            for (int step = 0; step < 3; step++)
            {
                clock.Advance(MoveInterval);
                Assert.That(simulation.Advance().HasMovement, Is.True);
            }

            ThrowerEnemyAdvanceResult telegraph = simulation.Advance();
            Assert.That(telegraph.HasStateTransition, Is.True);
            Assert.That(telegraph.State, Is.EqualTo(ThrowerEnemyState.Telegraph));
        }

        [Test]
        public void TargetTie_UsesStableAuthoredOrderAndDoesNotRetargetDuringTelegraph()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -2)), Is.True);
            var simulation = new ThrowerEnemySimulation(
                grid,
                clock,
                CreateDefinition(CreateBomb()),
                ThrowerActor,
                PlayerActor,
                new GridPosition(0, 1),
                new[] { new GridPosition(0, 2), new GridPosition(3, 2) },
                new[]
                {
                    new GridPosition(-3, -2),
                    new GridPosition(3, -2),
                    new GridPosition(0, 4),
                    new GridPosition(-4, 1),
                    new GridPosition(4, 1),
                    new GridPosition(0, 3),
                });

            Assert.That(simulation.Advance().HasMovement, Is.True);
            Assert.That(simulation.Advance().LockedTarget, Is.EqualTo(new GridPosition(-3, -2)));
            Assert.That(grid.TryMoveActor(PlayerActor, new GridPosition(1, -2)), Is.True);
            clock.Advance(TimeSpan.FromSeconds(0.4));

            Assert.That(simulation.Advance().LockedTarget, Is.EqualTo(new GridPosition(-3, -2)));
        }

        [Test]
        public void RepeatedVolleys_RotateSideTargetsWhileKeepingNearestPressureAnchor()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -2)), Is.True);
            var simulation = new ThrowerEnemySimulation(
                grid,
                clock,
                CreateDefinition(CreateBomb()),
                ThrowerActor,
                PlayerActor,
                new GridPosition(0, 1),
                new[]
                {
                    new GridPosition(0, 2),
                    new GridPosition(3, 2),
                    new GridPosition(-3, 2),
                },
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(-3, -2),
                    new GridPosition(3, -2),
                    new GridPosition(-4, 1),
                    new GridPosition(4, 1),
                    new GridPosition(0, 4),
                });

            Assert.That(simulation.Advance().HasMovement, Is.True);
            ThrowerEnemyAdvanceResult first = simulation.Advance();
            Assert.That(
                first.LockedTargets,
                Is.EqualTo(new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(-3, -2),
                    new GridPosition(3, -2),
                }));
            clock.Advance(TelegraphDuration);
            Assert.That(simulation.Advance().ShouldLaunch, Is.True);
            for (int index = 0; index < BombsPerVolley; index++)
            {
                simulation.NotifyLaunchFailed();
            }
            clock.Advance(RecoveryDuration);
            simulation.Advance();

            for (int step = 0; step < 3; step++)
            {
                Assert.That(simulation.Advance().HasMovement, Is.True);
                if (step < 2)
                {
                    clock.Advance(MoveInterval);
                }
            }

            ThrowerEnemyAdvanceResult second = simulation.Advance();

            Assert.That(second.State, Is.EqualTo(ThrowerEnemyState.Telegraph));
            Assert.That(
                second.LockedTargets,
                Is.EqualTo(new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(0, 4),
                    new GridPosition(-4, 1),
                }));
        }

        [Test]
        public void Telegraph_ExpiresOnceAndRequiresOutstandingBombResolutionBeforeNextShot()
        {
            var clock = new ManualGameClock();
            ThrowerEnemySimulation simulation = CreateSimulation(
                clock,
                new GridPosition(0, -2),
                new GridPosition(0, 1));
            Assert.That(simulation.Advance().HasMovement, Is.True);
            simulation.Advance();
            clock.Advance(TelegraphDuration);

            ThrowerEnemyAdvanceResult launch = simulation.Advance();

            Assert.That(launch.ShouldLaunch, Is.True);
            Assert.That(launch.State, Is.EqualTo(ThrowerEnemyState.Recover));
            BombId[] bombIds = CreateValidBombIds(BombsPerVolley);
            for (int index = 0; index < bombIds.Length; index++)
            {
                simulation.ConfirmBombPlaced(bombIds[index]);
            }
            Assert.That(simulation.PendingFlightCount, Is.Zero);
            Assert.That(simulation.ActiveBombCount, Is.EqualTo(BombsPerVolley));
            clock.Advance(RecoveryDuration);
            Assert.That(simulation.Advance().State, Is.EqualTo(ThrowerEnemyState.Track));

            for (int index = 0; index < 4; index++)
            {
                ThrowerEnemyAdvanceResult movement = simulation.Advance();
                if (!movement.HasMovement)
                {
                    clock.Advance(MoveInterval);
                }
            }

            Assert.That(simulation.State, Is.EqualTo(ThrowerEnemyState.Track));
            Assert.That(simulation.HasOutstandingBomb, Is.True);
            for (int index = 0; index < bombIds.Length; index++)
            {
                simulation.NotifyBombResolved(bombIds[index]);
            }
            Assert.That(simulation.HasOutstandingBomb, Is.False);
        }

        [Test]
        public void Volley_OneFailedLandingDoesNotCancelOtherFlights()
        {
            var clock = new ManualGameClock();
            ThrowerEnemySimulation simulation = CreateSimulation(
                clock,
                new GridPosition(0, -2),
                new GridPosition(0, 1));
            Assert.That(simulation.Advance().HasMovement, Is.True);
            simulation.Advance();
            clock.Advance(TelegraphDuration);
            Assert.That(simulation.Advance().ShouldLaunch, Is.True);
            BombId[] bombIds = CreateValidBombIds(2);

            simulation.ConfirmBombPlaced(bombIds[0]);
            simulation.NotifyLaunchFailed();

            Assert.That(simulation.PendingFlightCount, Is.EqualTo(1));
            Assert.That(simulation.ActiveBombCount, Is.EqualTo(1));
            Assert.That(simulation.IsActiveBomb(bombIds[0]), Is.True);
            simulation.ConfirmBombPlaced(bombIds[1]);
            Assert.That(simulation.PendingFlightCount, Is.Zero);
            Assert.That(simulation.ActiveBombCount, Is.EqualTo(2));

            simulation.NotifyBombResolved(bombIds[0]);
            Assert.That(simulation.HasOutstandingBomb, Is.True);
            simulation.NotifyBombResolved(bombIds[1]);
            Assert.That(simulation.HasOutstandingBomb, Is.False);
        }

        [Test]
        public void Recover_AdvancesToNextFiringAnchorAndMovesDeterministically()
        {
            var clock = new ManualGameClock();
            ThrowerEnemySimulation simulation = CreateSimulation(
                clock,
                new GridPosition(0, -2),
                new GridPosition(0, 1));
            Assert.That(simulation.Advance().HasMovement, Is.True);
            simulation.Advance();
            clock.Advance(TelegraphDuration);
            simulation.Advance();
            for (int index = 0; index < BombsPerVolley; index++)
            {
                simulation.NotifyLaunchFailed();
            }
            clock.Advance(RecoveryDuration);

            ThrowerEnemyAdvanceResult recovered = simulation.Advance();
            ThrowerEnemyAdvanceResult movement = simulation.Advance();

            Assert.That(recovered.HasStateTransition, Is.True);
            Assert.That(simulation.CurrentFiringAnchor, Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(movement.HasMovement, Is.True);
            Assert.That(movement.Movement.To, Is.EqualTo(new GridPosition(1, 2)));
        }

        [Test]
        public void BlockedPath_ProducesNoMovementWithoutChangingAuthorityState()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -2)), Is.True);
            var simulation = new ThrowerEnemySimulation(
                grid,
                clock,
                CreateDefinition(CreateBomb()),
                ThrowerActor,
                PlayerActor,
                new GridPosition(0, 1),
                new[] { new GridPosition(0, 2), new GridPosition(3, 2) },
                new[]
                {
                    new GridPosition(-3, -2),
                    new GridPosition(3, -2),
                    new GridPosition(0, 0),
                    new GridPosition(-4, 1),
                    new GridPosition(4, 1),
                    new GridPosition(0, 4),
                });

            Assert.That(simulation.Advance().HasMovement, Is.True);
            simulation.Advance();
            clock.Advance(TelegraphDuration);
            simulation.Advance();
            for (int index = 0; index < BombsPerVolley; index++)
            {
                simulation.NotifyLaunchFailed();
            }
            Assert.That(grid.TrySetTerrain(new GridPosition(1, 2), GridTerrain.IndestructibleWall), Is.True);
            Assert.That(grid.TrySetTerrain(new GridPosition(0, 1), GridTerrain.IndestructibleWall), Is.True);
            Assert.That(grid.TrySetTerrain(new GridPosition(-1, 2), GridTerrain.IndestructibleWall), Is.True);
            Assert.That(grid.TrySetTerrain(new GridPosition(0, 3), GridTerrain.IndestructibleWall), Is.True);
            clock.Advance(RecoveryDuration);
            simulation.Advance();
            ThrowerEnemyAdvanceResult result = simulation.Advance();

            Assert.That(result.HasActivity, Is.False);
            Assert.That(simulation.CurrentPosition, Is.EqualTo(new GridPosition(0, 2)));
        }

        [Test]
        public void Constructor_RejectsDuplicateAnchorsAndStartInsideFiringAnchors()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -2)), Is.True);
            Assert.Throws<ArgumentException>(() =>
                new ThrowerEnemySimulation(
                    grid,
                    clock,
                    CreateDefinition(CreateBomb()),
                    ThrowerActor,
                    PlayerActor,
                    new GridPosition(0, 2),
                    new[] { new GridPosition(0, 2), new GridPosition(0, 2) },
                    new[]
                    {
                        new GridPosition(-3, -2),
                        new GridPosition(3, -2),
                        new GridPosition(0, 0),
                        new GridPosition(-4, 1),
                        new GridPosition(4, 1),
                        new GridPosition(0, 4),
                    }));
            Assert.Throws<ArgumentException>(() =>
                new ThrowerEnemySimulation(
                    grid,
                    clock,
                    CreateDefinition(CreateBomb()),
                    ThrowerActor,
                    PlayerActor,
                    new GridPosition(0, 2),
                    new[] { new GridPosition(0, 2), new GridPosition(3, 2) },
                    new[]
                    {
                        new GridPosition(-3, -2),
                        new GridPosition(3, -2),
                        new GridPosition(0, 0),
                        new GridPosition(-4, 1),
                        new GridPosition(4, 1),
                        new GridPosition(0, 4),
                    }));
            Assert.Throws<ArgumentException>(() =>
                new ThrowerEnemySimulation(
                    grid,
                    clock,
                    CreateDefinition(CreateBomb()),
                    ThrowerActor,
                    PlayerActor,
                    new GridPosition(0, 1),
                    new[] { new GridPosition(0, 2), new GridPosition(3, 2) },
                    new[] { new GridPosition(-3, -2), new GridPosition(3, -2) }));
        }

        [Test]
        public void Advance_RejectsClockMovingBackwards()
        {
            var clock = new MutableClock { Now = TimeSpan.FromSeconds(1) };
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, -2)), Is.True);
            var simulation = new ThrowerEnemySimulation(
                grid,
                clock,
                CreateDefinition(CreateBomb()),
                ThrowerActor,
                PlayerActor,
                new GridPosition(0, 1),
                new[] { new GridPosition(0, 2), new GridPosition(3, 2) },
                new[]
                {
                    new GridPosition(-3, -2),
                    new GridPosition(3, -2),
                    new GridPosition(0, 0),
                    new GridPosition(-4, 1),
                    new GridPosition(4, 1),
                    new GridPosition(0, 4),
                });
            clock.Now = TimeSpan.Zero;

            Assert.Throws<InvalidOperationException>(() => simulation.Advance());
        }

        private static ThrowerEnemySimulation CreateSimulation(
            ManualGameClock clock,
            GridPosition playerPosition,
            GridPosition throwerPosition)
        {
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, playerPosition), Is.True);
            return new ThrowerEnemySimulation(
                grid,
                clock,
                CreateDefinition(CreateBomb()),
                ThrowerActor,
                PlayerActor,
                throwerPosition,
                new[] { new GridPosition(0, 2), new GridPosition(3, 2) },
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(-3, -2),
                    new GridPosition(3, -2),
                    new GridPosition(-4, 1),
                    new GridPosition(4, 1),
                    new GridPosition(0, 4),
                });
        }

        private static ThrowerEnemyDefinition CreateDefinition(BombDefinition bomb)
        {
            return new ThrowerEnemyDefinition(
                new EnemyDefinitionId("test-thrower"),
                MoveInterval,
                TelegraphDuration,
                FlightDuration,
                RecoveryDuration,
                1,
                BombsPerVolley,
                bomb);
        }

        private static BombDefinition CreateBomb()
        {
            return new BombDefinition(
                new BombDefinitionId("test-thrower-bomb"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(1.5),
                1);
        }

        private static GridState CreateFloorGrid()
        {
            var grid = new GridState();
            for (int z = -4; z <= 4; z++)
            {
                for (int x = -5; x <= 5; x++)
                {
                    Assert.That(
                        grid.TrySetTerrain(new GridPosition(x, z), GridTerrain.Floor),
                        Is.True);
                }
            }
            return grid;
        }

        private static BombId[] CreateValidBombIds(int count)
        {
            var grid = new GridState();
            for (int index = 0; index < count; index++)
            {
                Assert.That(
                    grid.TrySetTerrain(new GridPosition(index, 0), GridTerrain.Floor),
                    Is.True);
            }
            var clock = new ManualGameClock();
            var bombs = new BombSimulation(
                grid,
                clock,
                TimeSpan.FromMilliseconds(150));
            var bombIds = new BombId[count];
            for (int index = 0; index < count; index++)
            {
                Assert.That(
                    bombs.TryPlaceBomb(
                        CreateBomb(),
                        new GridPosition(index, 0),
                        PlayerActor,
                        out bombIds[index]),
                    Is.True);
            }
            return bombIds;
        }

        private sealed class MutableClock : IGameClock
        {
            public TimeSpan Now { get; set; }
        }
    }
}
