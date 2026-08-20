using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class ArmoredEnemySimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId ArmoredActor = new ActorId(4);
        private static readonly TimeSpan ArmoredInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan BrokenInterval = TimeSpan.FromMilliseconds(333);
        private static readonly TimeSpan PanicTelegraph = TimeSpan.FromMilliseconds(600);
        private static readonly TimeSpan PanicInterval = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan PanicRecover = TimeSpan.FromMilliseconds(200);

        [Test]
        public void Definition_StoresGuardPanicAndTwoStageContract()
        {
            ArmoredEnemyDefinition definition = CreateDefinition();

            Assert.That(definition.Id, Is.EqualTo(new EnemyDefinitionId("prototype-armored")));
            Assert.That(definition.MaxHealth, Is.EqualTo(2));
            Assert.That(definition.ContactDamage, Is.EqualTo(1));
            Assert.That(definition.ArmoredStepInterval, Is.EqualTo(ArmoredInterval));
            Assert.That(definition.BrokenStepInterval, Is.EqualTo(BrokenInterval));
            Assert.That(definition.DirectionCommitmentSteps, Is.EqualTo(2));
            Assert.That(definition.GuardRadius, Is.EqualTo(1));
            Assert.That(definition.PanicTelegraphDuration, Is.EqualTo(PanicTelegraph));
            Assert.That(definition.PanicStepInterval, Is.EqualTo(PanicInterval));
            Assert.That(definition.PanicRunDistance, Is.EqualTo(3));
            Assert.That(definition.PanicRecoverDuration, Is.EqualTo(PanicRecover));
            Assert.That(definition.GetStepInterval(ArmoredEnemyState.Armored), Is.EqualTo(ArmoredInterval));
            Assert.That(definition.GetStepInterval(ArmoredEnemyState.Broken), Is.EqualTo(BrokenInterval));
            Assert.Throws<InvalidOperationException>(() =>
                definition.GetStepInterval(ArmoredEnemyState.Dead));
        }

        [Test]
        public void Definition_RejectsInvalidCadenceDamageCommitmentAndPanicValues()
        {
            EnemyDefinitionId id = new EnemyDefinitionId("prototype-armored");
            Assert.Throws<ArgumentException>(() => CreateRaw(default, 1, 2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRaw(id, 0, 2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(
                    id,
                    1,
                    TimeSpan.Zero,
                    BrokenInterval,
                    2,
                    1,
                    PanicTelegraph,
                    PanicInterval,
                    3,
                    PanicRecover));
            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemyDefinition(
                    id,
                    1,
                    ArmoredInterval,
                    BrokenInterval,
                    2,
                    1,
                    PanicTelegraph,
                    BrokenInterval,
                    3,
                    PanicRecover));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRaw(id, 1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRaw(id, 1, 2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(
                    id,
                    1,
                    ArmoredInterval,
                    BrokenInterval,
                    2,
                    1,
                    TimeSpan.Zero,
                    PanicInterval,
                    3,
                    PanicRecover));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(
                    id,
                    1,
                    ArmoredInterval,
                    BrokenInterval,
                    2,
                    1,
                    PanicTelegraph,
                    PanicInterval,
                    0,
                    PanicRecover));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(
                    id,
                    1,
                    ArmoredInterval,
                    BrokenInterval,
                    2,
                    1,
                    PanicTelegraph,
                    PanicInterval,
                    3,
                    TimeSpan.Zero));
        }

        [Test]
        public void Guard_ApproachesOnlyInsideSpawnRadiusAndDoesNotOscillateAtBoundary()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, -3));
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 1));

            clock.Advance(ArmoredInterval);
            Assert.That(enemy.Advance().Movement.To, Is.EqualTo(new GridPosition(0, 0)));
            clock.Advance(ArmoredInterval);
            Assert.That(enemy.Advance().HasActivity, Is.False);
            clock.Advance(ArmoredInterval);
            Assert.That(enemy.Advance().HasActivity, Is.False);

            Assert.That(enemy.CurrentPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(enemy.GuardOrigin, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(enemy.BehaviorState, Is.EqualTo(ArmoredEnemyBehaviorState.Guard));
        }

        [Test]
        public void FirstExplosion_LocksFarthestValidBranchWithCardinalTieBreak()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, -4));
            grid.TrySetTerrain(new GridPosition(0, 2), GridTerrain.IndestructibleWall);
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 0));

            ArmoredEnemyDamageResult result = enemy.ApplyExplosion(
                CreateBombId(1),
                new GridPosition(0, -1));

            Assert.That(result.ArmorWasBroken, Is.True);
            Assert.That(result.CurrentBehaviorState, Is.EqualTo(ArmoredEnemyBehaviorState.PanicTelegraph));
            Assert.That(result.HasBehaviorTransition, Is.True);
            Assert.That(enemy.PanicDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(enemy.PanicPathCellCount, Is.EqualTo(3));
            Assert.That(enemy.GetPanicPathCell(0), Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(enemy.GetPanicPathCell(1), Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(enemy.GetPanicPathCell(2), Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(enemy.PanicDestination, Is.EqualTo(new GridPosition(3, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => enemy.GetPanicPathCell(3));
        }

        [Test]
        public void DiagonalExplosion_UsesSouthBeforeWestWhenBothEndpointsAreEquallyFar()
        {
            ArmoredEnemySimulation enemy = CreateSimulation(
                new ManualGameClock(),
                new GridPosition(0, 0),
                new GridPosition(-4, -4));

            enemy.ApplyExplosion(CreateBombId(1), new GridPosition(3, 3));

            Assert.That(enemy.PanicDirection, Is.EqualTo(CardinalDirection.South));
            Assert.That(enemy.PanicDestination, Is.EqualTo(new GridPosition(0, -3)));
        }

        [Test]
        public void Telegraph_LocksPathUntilExactDurationDespiteTerrainChange()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, -4));
            grid.TrySetTerrain(new GridPosition(0, 2), GridTerrain.IndestructibleWall);
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 0));
            enemy.ApplyExplosion(CreateBombId(1), new GridPosition(0, -1));
            grid.TrySetTerrain(new GridPosition(0, 2), GridTerrain.Floor);

            clock.Advance(PanicTelegraph - TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().HasActivity, Is.False);
            Assert.That(enemy.PanicDirection, Is.EqualTo(CardinalDirection.East));
            clock.Advance(TimeSpan.FromTicks(1));
            ArmoredEnemyAdvanceResult transition = enemy.Advance();

            Assert.That(transition.HasStateTransition, Is.True);
            Assert.That(transition.PreviousState, Is.EqualTo(ArmoredEnemyBehaviorState.PanicTelegraph));
            Assert.That(transition.State, Is.EqualTo(ArmoredEnemyBehaviorState.PanicRun));
            Assert.That(transition.PanicDestination, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(enemy.CurrentPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void PanicRun_MovesThreeCellsAtCadenceThenRecoversAndChases()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(
                clock,
                new GridPosition(0, 0),
                new GridPosition(0, -4));
            enemy.ApplyExplosion(CreateBombId(1), new GridPosition(-3, 0));
            clock.Advance(PanicTelegraph);
            enemy.Advance();

            ArmoredEnemyAdvanceResult first = enemy.Advance();
            Assert.That(first.HasMovement, Is.True);
            Assert.That(first.Movement.To, Is.EqualTo(new GridPosition(1, 0)));
            clock.Advance(PanicInterval - TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().HasActivity, Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().Movement.To, Is.EqualTo(new GridPosition(2, 0)));
            clock.Advance(PanicInterval);
            ArmoredEnemyAdvanceResult finalRun = enemy.Advance();

            Assert.That(finalRun.HasMovement, Is.True);
            Assert.That(finalRun.HasStateTransition, Is.True);
            Assert.That(finalRun.PreviousState, Is.EqualTo(ArmoredEnemyBehaviorState.PanicRun));
            Assert.That(finalRun.State, Is.EqualTo(ArmoredEnemyBehaviorState.PanicRecover));
            Assert.That(enemy.CurrentPosition, Is.EqualTo(new GridPosition(3, 0)));
            clock.Advance(PanicRecover - TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().HasActivity, Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(enemy.Advance().State, Is.EqualTo(ArmoredEnemyBehaviorState.Chase));
            Assert.That(enemy.PanicPathCellCount, Is.Zero);
            Assert.That(enemy.Advance().HasMovement, Is.True, "Chase can repath immediately after recovery.");
        }

        [Test]
        public void PanicRun_NewBombBlocksLockedPathAndCausesEarlyRecoverWithoutReplan()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, -4));
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 0));
            enemy.ApplyExplosion(CreateBombId(1), new GridPosition(-3, 0));
            clock.Advance(PanicTelegraph);
            enemy.Advance();
            grid.TryAddBomb(new GridPosition(1, 0));

            ArmoredEnemyAdvanceResult blocked = enemy.Advance();

            Assert.That(blocked.HasMovement, Is.False);
            Assert.That(blocked.PreviousState, Is.EqualTo(ArmoredEnemyBehaviorState.PanicRun));
            Assert.That(blocked.State, Is.EqualTo(ArmoredEnemyBehaviorState.PanicRecover));
            Assert.That(enemy.CurrentPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(enemy.PanicDirection, Is.EqualTo(CardinalDirection.East));
            Assert.That(enemy.PanicDestination, Is.EqualTo(new GridPosition(3, 0)));
        }

        [Test]
        public void SurroundedFirstHit_SkipsTelegraphAndRecoversIntoChase()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, -3));
            foreach (GridPosition wall in new[]
                     {
                         new GridPosition(0, 1),
                         new GridPosition(1, 0),
                         new GridPosition(0, -1),
                         new GridPosition(-1, 0),
                     })
            {
                grid.TrySetTerrain(wall, GridTerrain.IndestructibleWall);
            }
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 0));

            ArmoredEnemyDamageResult result = enemy.ApplyExplosion(
                CreateBombId(1),
                new GridPosition(0, -2));

            Assert.That(result.CurrentBehaviorState, Is.EqualTo(ArmoredEnemyBehaviorState.PanicRecover));
            Assert.That(enemy.PanicPathCellCount, Is.Zero);
            clock.Advance(PanicRecover);
            Assert.That(enemy.Advance().State, Is.EqualTo(ArmoredEnemyBehaviorState.Chase));
        }

        [Test]
        public void TwoDistinctExplosions_AdvanceDurabilityAndFreeCellDuringTelegraph()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, -3));
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 0));

            ArmoredEnemyDamageResult first = enemy.ApplyExplosion(
                CreateBombId(1),
                new GridPosition(0, -1));
            ArmoredEnemyDamageResult second = enemy.ApplyExplosion(
                CreateBombId(2),
                new GridPosition(0, -1));

            Assert.That(first.CurrentState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(first.CurrentBehaviorState, Is.EqualTo(ArmoredEnemyBehaviorState.PanicTelegraph));
            Assert.That(second.PreviousState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(second.CurrentState, Is.EqualTo(ArmoredEnemyState.Dead));
            Assert.That(second.CurrentBehaviorState, Is.EqualTo(ArmoredEnemyBehaviorState.Dead));
            Assert.That(second.WasFatal, Is.True);
            Assert.That(enemy.CurrentHealth, Is.Zero);
            Assert.That(enemy.Advance().HasActivity, Is.False);
            Assert.That(grid.TryGetActorPosition(ArmoredActor, out _), Is.False);
            Assert.That(grid.GetCell(new GridPosition(0, 0)).Occupancy, Is.EqualTo(GridOccupancy.None));
        }

        [Test]
        public void DuplicateExplosionAndDamageAfterDeath_DoNotAdvanceState()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(
                clock,
                new GridPosition(0, 0),
                new GridPosition(0, -3));
            BombId firstId = CreateBombId(1);
            enemy.ApplyExplosion(firstId, new GridPosition(0, -1));

            ArmoredEnemyDamageResult duplicate = enemy.ApplyExplosion(
                firstId,
                new GridPosition(1, 0));
            ArmoredEnemyDamageResult fatal = enemy.ApplyExplosion(
                CreateBombId(2),
                new GridPosition(0, -1));
            ArmoredEnemyDamageResult afterDeath = enemy.ApplyExplosion(
                CreateBombId(3),
                new GridPosition(0, -1));

            Assert.That(duplicate.Damage.Status, Is.EqualTo(EnemyDamageStatus.IgnoredDuplicateExplosion));
            Assert.That(duplicate.HasStateTransition, Is.False);
            Assert.That(duplicate.HasBehaviorTransition, Is.False);
            Assert.That(fatal.WasFatal, Is.True);
            Assert.That(afterDeath.Damage.Status, Is.EqualTo(EnemyDamageStatus.IgnoredDead));
            Assert.That(afterDeath.CurrentBehaviorState, Is.EqualTo(ArmoredEnemyBehaviorState.Dead));
        }

        [Test]
        public void CardinalAdjacency_GuardStopsWithoutEnteringPlayerCell()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(
                clock,
                new GridPosition(0, 1),
                new GridPosition(0, 0));
            clock.Advance(ArmoredInterval);

            Assert.That(enemy.Advance().HasActivity, Is.False);
            Assert.That(enemy.CurrentPosition, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void ClockMovingBackwards_IsRejectedForMovementAndDamage()
        {
            GridState grid = CreateFloorGrid();
            var clock = new MutableGameClock(TimeSpan.FromSeconds(2));
            grid.TryAddActor(PlayerActor, new GridPosition(0, 0));
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 3));
            clock.Now = TimeSpan.FromSeconds(1);

            Assert.Throws<InvalidOperationException>(() => enemy.Advance());
            Assert.Throws<InvalidOperationException>(() =>
                enemy.ApplyExplosion(CreateBombId(1), new GridPosition(0, 2)));
            Assert.That(enemy.State, Is.EqualTo(ArmoredEnemyState.Armored));
        }

        [Test]
        public void Constructor_RejectsMissingTargetInvalidIdsAndInvalidSpawn()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();

            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemySimulation(grid, clock, CreateDefinition(), default, PlayerActor, new GridPosition(0, 1)));
            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemySimulation(grid, clock, CreateDefinition(), ArmoredActor, default, new GridPosition(0, 1)));
            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemySimulation(grid, clock, CreateDefinition(), PlayerActor, PlayerActor, new GridPosition(0, 1)));
            Assert.Throws<InvalidOperationException>(() =>
                new ArmoredEnemySimulation(grid, clock, CreateDefinition(), ArmoredActor, PlayerActor, new GridPosition(0, 1)));

            grid.TryAddActor(PlayerActor, new GridPosition(0, 0));
            grid.TrySetTerrain(new GridPosition(0, 1), GridTerrain.IndestructibleWall);
            Assert.Throws<InvalidOperationException>(() =>
                new ArmoredEnemySimulation(grid, clock, CreateDefinition(), ArmoredActor, PlayerActor, new GridPosition(0, 1)));
        }

        private static ArmoredEnemyDefinition CreateRaw(
            EnemyDefinitionId id,
            int damage,
            int commitment,
            int guardRadius)
        {
            return new ArmoredEnemyDefinition(
                id,
                damage,
                ArmoredInterval,
                BrokenInterval,
                commitment,
                guardRadius,
                PanicTelegraph,
                PanicInterval,
                3,
                PanicRecover);
        }

        private static ArmoredEnemySimulation CreateSimulation(
            IGameClock clock,
            GridPosition armoredPosition,
            GridPosition playerPosition)
        {
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, playerPosition), Is.True);
            return new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                armoredPosition);
        }

        private static ArmoredEnemyDefinition CreateDefinition()
        {
            return CreateRaw(
                new EnemyDefinitionId("prototype-armored"),
                1,
                2,
                1);
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

        private static BombId CreateBombId(int sequence)
        {
            var grid = new GridState();
            var clock = new ManualGameClock();
            var bombs = new BombSimulation(grid, clock, TimeSpan.FromMilliseconds(100));
            var definition = new BombDefinition(
                new BombDefinitionId("armored-enemy-test"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(10),
                0);
            BombId created = default;
            for (int index = 1; index <= sequence; index++)
            {
                var position = new GridPosition(index, 0);
                grid.TrySetTerrain(position, GridTerrain.Floor);
                Assert.That(
                    bombs.TryPlaceBomb(definition, position, new ActorId(10), out created),
                    Is.True);
            }

            return created;
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
