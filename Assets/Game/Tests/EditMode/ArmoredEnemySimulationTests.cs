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

        [Test]
        public void Definition_StoresTwoStageSlowToFastContract()
        {
            var definition = CreateDefinition();

            Assert.That(definition.Id, Is.EqualTo(new EnemyDefinitionId("prototype-armored")));
            Assert.That(definition.MaxHealth, Is.EqualTo(2));
            Assert.That(definition.ContactDamage, Is.EqualTo(1));
            Assert.That(definition.ArmoredStepInterval, Is.EqualTo(ArmoredInterval));
            Assert.That(definition.BrokenStepInterval, Is.EqualTo(BrokenInterval));
            Assert.That(definition.DirectionCommitmentSteps, Is.EqualTo(2));
            Assert.That(definition.GetStepInterval(ArmoredEnemyState.Armored), Is.EqualTo(ArmoredInterval));
            Assert.That(definition.GetStepInterval(ArmoredEnemyState.Broken), Is.EqualTo(BrokenInterval));
            Assert.Throws<InvalidOperationException>(() =>
                definition.GetStepInterval(ArmoredEnemyState.Dead));
        }

        [Test]
        public void Definition_RejectsInvalidCadenceDamageAndCommitment()
        {
            EnemyDefinitionId id = new EnemyDefinitionId("prototype-armored");
            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemyDefinition(default, 1, ArmoredInterval, BrokenInterval, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(id, 0, ArmoredInterval, BrokenInterval, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(id, 1, TimeSpan.Zero, BrokenInterval, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(id, 1, ArmoredInterval, TimeSpan.Zero, 2));
            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemyDefinition(id, 1, ArmoredInterval, ArmoredInterval, 2));
            Assert.Throws<ArgumentException>(() =>
                new ArmoredEnemyDefinition(id, 1, ArmoredInterval, TimeSpan.FromSeconds(2), 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArmoredEnemyDefinition(id, 1, ArmoredInterval, BrokenInterval, 0));
        }

        [Test]
        public void ArmoredPhase_WaitsForSlowInitialCadenceAndMovesAtMostOnce()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(clock, new GridPosition(0, 4));

            Assert.That(enemy.TryAdvance(out _), Is.False);
            clock.Advance(ArmoredInterval - TimeSpan.FromTicks(1));
            Assert.That(enemy.TryAdvance(out _), Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(enemy.TryAdvance(out EnemyMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new GridPosition(0, 3)));
            Assert.That(enemy.TryAdvance(out _), Is.False);
        }

        [Test]
        public void FirstExplosion_BreaksArmorWithoutKillingAndRepathsImmediately()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(clock, new GridPosition(0, 4));
            clock.Advance(ArmoredInterval);
            Assert.That(enemy.TryAdvance(out _), Is.True);

            ArmoredEnemyDamageResult result = enemy.ApplyExplosion(CreateBombId(1));

            Assert.That(result.Damage.AppliedDamage, Is.EqualTo(1));
            Assert.That(result.PreviousState, Is.EqualTo(ArmoredEnemyState.Armored));
            Assert.That(result.CurrentState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(result.ArmorWasBroken, Is.True);
            Assert.That(result.WasFatal, Is.False);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(1));
            Assert.That(enemy.IsDead, Is.False);
            Assert.That(enemy.CurrentDirection, Is.EqualTo(CardinalDirection.None));
            Assert.That(enemy.RemainingCommittedSteps, Is.Zero);
            Assert.That(enemy.TryAdvance(out EnemyMovementStep fastStep), Is.True);
            Assert.That(fastStep.To, Is.EqualTo(new GridPosition(0, 2)));
        }

        [Test]
        public void BrokenPhase_UsesFastCadenceAtExactInterval()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(clock, new GridPosition(0, 5));
            enemy.ApplyExplosion(CreateBombId(1));
            Assert.That(enemy.TryAdvance(out _), Is.True);

            clock.Advance(BrokenInterval - TimeSpan.FromTicks(1));
            Assert.That(enemy.TryAdvance(out _), Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.That(enemy.TryAdvance(out EnemyMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new GridPosition(0, 3)));
        }

        [Test]
        public void TwoDistinctExplosions_AdvanceExactlyOneStageEachAndFreeCellOnDeath()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, 0));
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(0, 3));

            ArmoredEnemyDamageResult first = enemy.ApplyExplosion(CreateBombId(1));
            ArmoredEnemyDamageResult second = enemy.ApplyExplosion(CreateBombId(2));

            Assert.That(first.CurrentState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(second.PreviousState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(second.CurrentState, Is.EqualTo(ArmoredEnemyState.Dead));
            Assert.That(second.WasFatal, Is.True);
            Assert.That(enemy.CurrentHealth, Is.Zero);
            Assert.That(enemy.TryAdvance(out _), Is.False);
            Assert.That(grid.TryGetActorPosition(ArmoredActor, out _), Is.False);
            Assert.That(grid.GetCell(new GridPosition(0, 3)).Occupancy, Is.EqualTo(GridOccupancy.None));
        }

        [Test]
        public void DuplicateExplosionAndDamageAfterDeath_DoNotAdvanceState()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(clock, new GridPosition(0, 3));
            BombId firstId = CreateBombId(1);
            enemy.ApplyExplosion(firstId);

            ArmoredEnemyDamageResult duplicate = enemy.ApplyExplosion(firstId);
            ArmoredEnemyDamageResult fatal = enemy.ApplyExplosion(CreateBombId(2));
            ArmoredEnemyDamageResult afterDeath = enemy.ApplyExplosion(CreateBombId(3));

            Assert.That(duplicate.Damage.Status, Is.EqualTo(EnemyDamageStatus.IgnoredDuplicateExplosion));
            Assert.That(duplicate.HasStateTransition, Is.False);
            Assert.That(duplicate.CurrentState, Is.EqualTo(ArmoredEnemyState.Broken));
            Assert.That(fatal.WasFatal, Is.True);
            Assert.That(afterDeath.Damage.Status, Is.EqualTo(EnemyDamageStatus.IgnoredDead));
            Assert.That(afterDeath.HasStateTransition, Is.False);
            Assert.That(afterDeath.CurrentState, Is.EqualTo(ArmoredEnemyState.Dead));
        }

        [Test]
        public void LocalMovement_UsesNorthTieBreakCommitmentAndRepathsAroundBomb()
        {
            GridState grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            grid.TryAddActor(PlayerActor, new GridPosition(0, 0));
            var enemy = new ArmoredEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ArmoredActor,
                PlayerActor,
                new GridPosition(1, -3));
            enemy.ApplyExplosion(CreateBombId(1));

            Assert.That(enemy.TryAdvance(out EnemyMovementStep first), Is.True);
            Assert.That(first.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(grid.TryAddBomb(new GridPosition(1, -1)), Is.True);
            clock.Advance(BrokenInterval);
            Assert.That(enemy.TryAdvance(out EnemyMovementStep second), Is.True);

            Assert.That(second.Direction, Is.EqualTo(CardinalDirection.West));
            Assert.That(second.To, Is.EqualTo(new GridPosition(0, -2)));
            Assert.That(second.To, Is.Not.EqualTo(new GridPosition(0, 0)),
                "The enemy must never enter the occupied player cell.");
        }

        [Test]
        public void CardinalAdjacency_StopsWithoutEnteringPlayerCell()
        {
            var clock = new ManualGameClock();
            ArmoredEnemySimulation enemy = CreateSimulation(clock, new GridPosition(0, 1));
            enemy.ApplyExplosion(CreateBombId(1));

            Assert.That(enemy.TryAdvance(out _), Is.False);
            Assert.That(enemy.CurrentPosition, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void ClockMovingBackwards_IsRejectedForMovementAndDamage()
        {
            var grid = CreateFloorGrid();
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

            Assert.Throws<InvalidOperationException>(() => enemy.TryAdvance(out _));
            Assert.Throws<InvalidOperationException>(() => enemy.ApplyExplosion(CreateBombId(1)));
            Assert.That(enemy.State, Is.EqualTo(ArmoredEnemyState.Armored));
        }

        [Test]
        public void Constructor_RejectsMissingTargetInvalidIdsAndInvalidSpawn()
        {
            var grid = CreateFloorGrid();
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

        private static ArmoredEnemySimulation CreateSimulation(
            IGameClock clock,
            GridPosition armoredPosition)
        {
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 0)), Is.True);
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
            return new ArmoredEnemyDefinition(
                new EnemyDefinitionId("prototype-armored"),
                1,
                ArmoredInterval,
                BrokenInterval,
                2);
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
