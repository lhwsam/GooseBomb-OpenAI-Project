using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class ChargerEnemySimulationTests
    {
        private static readonly ActorId PlayerActor = new ActorId(1);
        private static readonly ActorId ChaserActor = new ActorId(2);
        private static readonly ActorId ChargerActor = new ActorId(3);
        private static readonly TimeSpan TelegraphDuration = TimeSpan.FromMilliseconds(750);
        private static readonly TimeSpan ChargeStepInterval = TimeSpan.FromMilliseconds(125);
        private static readonly TimeSpan RecoverDuration = TimeSpan.FromMilliseconds(750);

        [Test]
        public void Definition_StoresStableIdCombatValuesAndTimings()
        {
            var id = new EnemyDefinitionId("prototype-charger");

            var definition = new ChargerEnemyDefinition(
                id,
                1,
                1,
                TelegraphDuration,
                ChargeStepInterval,
                RecoverDuration);

            Assert.That(definition.Id, Is.EqualTo(id));
            Assert.That(definition.MaxHealth, Is.EqualTo(1));
            Assert.That(definition.ContactDamage, Is.EqualTo(1));
            Assert.That(definition.TelegraphDuration, Is.EqualTo(TelegraphDuration));
            Assert.That(definition.ChargeStepInterval, Is.EqualTo(ChargeStepInterval));
            Assert.That(definition.RecoverDuration, Is.EqualTo(RecoverDuration));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveCombatValues(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChargerEnemyDefinition(
                    CreateDefinitionId(), value, 1,
                    TelegraphDuration, ChargeStepInterval, RecoverDuration));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChargerEnemyDefinition(
                    CreateDefinitionId(), 1, value,
                    TelegraphDuration, ChargeStepInterval, RecoverDuration));
        }

        [Test]
        public void Definition_RejectsNonPositiveTimings()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChargerEnemyDefinition(
                    CreateDefinitionId(), 1, 1,
                    TimeSpan.Zero, ChargeStepInterval, RecoverDuration));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChargerEnemyDefinition(
                    CreateDefinitionId(), 1, 1,
                    TelegraphDuration, TimeSpan.Zero, RecoverDuration));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChargerEnemyDefinition(
                    CreateDefinitionId(), 1, 1,
                    TelegraphDuration, ChargeStepInterval, TimeSpan.Zero));
        }

        [TestCase(0, 3, CardinalDirection.North)]
        [TestCase(3, 0, CardinalDirection.East)]
        [TestCase(0, -3, CardinalDirection.South)]
        [TestCase(-3, 0, CardinalDirection.West)]
        public void AlignedClearTarget_StartsTelegraphAndLocksDirection(
            int targetX,
            int targetZ,
            CardinalDirection expectedDirection)
        {
            var clock = new ManualGameClock();
            ChargerEnemySimulation charger = CreateSimulation(
                clock,
                new GridPosition(targetX, targetZ),
                GridPositionAtOrigin());

            ChargerEnemyAdvanceResult result = charger.Advance();

            Assert.That(result.HasStateTransition, Is.True);
            Assert.That(result.PreviousState, Is.EqualTo(ChargerEnemyState.Track));
            Assert.That(result.State, Is.EqualTo(ChargerEnemyState.Telegraph));
            Assert.That(result.Direction, Is.EqualTo(expectedDirection));
            Assert.That(result.HasMovement, Is.False);
            Assert.That(result.ImpactedTarget, Is.False);
            Assert.That(charger.State, Is.EqualTo(ChargerEnemyState.Telegraph));
            Assert.That(charger.LockedDirection, Is.EqualTo(expectedDirection));
        }

        [Test]
        public void UnalignedOrOccludedTarget_DoesNotTelegraph()
        {
            var clock = new ManualGameClock();
            GridState unalignedGrid = CreateFloorGrid();
            Assert.That(unalignedGrid.TryAddActor(PlayerActor, new GridPosition(2, 3)), Is.True);
            var unaligned = CreateSimulation(unalignedGrid, clock, GridPositionAtOrigin());

            Assert.That(unaligned.Advance().HasActivity, Is.False);

            GridState wallGrid = CreateFloorGrid();
            Assert.That(wallGrid.TryAddActor(PlayerActor, new GridPosition(0, 3)), Is.True);
            Assert.That(
                wallGrid.TrySetTerrain(new GridPosition(0, 1), GridTerrain.IndestructibleWall),
                Is.True);
            var wallBlocked = CreateSimulation(wallGrid, new ManualGameClock(), GridPositionAtOrigin());

            Assert.That(wallBlocked.Advance().HasActivity, Is.False);

            GridState bombGrid = CreateFloorGrid();
            Assert.That(bombGrid.TryAddActor(PlayerActor, new GridPosition(0, 3)), Is.True);
            Assert.That(bombGrid.TryAddBomb(new GridPosition(0, 1)), Is.True);
            var bombBlocked = CreateSimulation(bombGrid, new ManualGameClock(), GridPositionAtOrigin());

            Assert.That(bombBlocked.Advance().HasActivity, Is.False);
        }

        [Test]
        public void Telegraph_KeepsLockedDirectionWhenTargetMovesAndUsesExactBoundary()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 3)), Is.True);
            ChargerEnemySimulation charger = CreateSimulation(grid, clock, GridPositionAtOrigin());
            charger.Advance();
            Assert.That(grid.TryMoveActor(PlayerActor, new GridPosition(1, 3)), Is.True);

            clock.Advance(TelegraphDuration - TimeSpan.FromTicks(1));
            Assert.That(charger.Advance().HasActivity, Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            ChargerEnemyAdvanceResult result = charger.Advance();

            Assert.That(result.HasStateTransition, Is.True);
            Assert.That(result.PreviousState, Is.EqualTo(ChargerEnemyState.Telegraph));
            Assert.That(result.State, Is.EqualTo(ChargerEnemyState.Charge));
            Assert.That(result.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(charger.LockedDirection, Is.EqualTo(CardinalDirection.North));
        }

        [Test]
        public void Charge_MovesAtCadenceWithoutTurningTowardMovedTarget()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 4)), Is.True);
            ChargerEnemySimulation charger = CreateSimulation(grid, clock, GridPositionAtOrigin());
            EnterCharge(charger, clock);
            Assert.That(grid.TryMoveActor(PlayerActor, new GridPosition(1, 4)), Is.True);

            ChargerEnemyAdvanceResult first = charger.Advance();
            Assert.That(first.HasMovement, Is.True);
            Assert.That(first.Movement.To, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(first.Movement.Direction, Is.EqualTo(CardinalDirection.North));
            Assert.That(charger.Advance().HasActivity, Is.False);
            clock.Advance(ChargeStepInterval - TimeSpan.FromTicks(1));
            Assert.That(charger.Advance().HasActivity, Is.False);
            clock.Advance(TimeSpan.FromTicks(1));

            ChargerEnemyAdvanceResult second = charger.Advance();

            Assert.That(second.HasMovement, Is.True);
            Assert.That(second.Movement.To, Is.EqualTo(new GridPosition(0, 2)));
            Assert.That(second.Movement.Direction, Is.EqualTo(CardinalDirection.North));
        }

        [Test]
        public void Charge_ImpactsTargetWithoutEnteringItsCellThenRecovers()
        {
            var clock = new ManualGameClock();
            ChargerEnemySimulation charger = CreateSimulation(
                clock,
                new GridPosition(0, 2),
                GridPositionAtOrigin());
            EnterCharge(charger, clock);
            Assert.That(charger.Advance().HasMovement, Is.True);
            clock.Advance(ChargeStepInterval);

            ChargerEnemyAdvanceResult impact = charger.Advance();

            Assert.That(impact.ImpactedTarget, Is.True);
            Assert.That(impact.HasStateTransition, Is.True);
            Assert.That(impact.PreviousState, Is.EqualTo(ChargerEnemyState.Charge));
            Assert.That(impact.State, Is.EqualTo(ChargerEnemyState.Recover));
            Assert.That(impact.HasMovement, Is.False);
            Assert.That(charger.CurrentPosition, Is.EqualTo(new GridPosition(0, 1)));
        }

        [TestCase(GridTerrain.IndestructibleWall)]
        [TestCase(GridTerrain.DestructibleWall)]
        public void Charge_WallCollisionRecoversWithoutTargetImpact(GridTerrain terrain)
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 4)), Is.True);
            ChargerEnemySimulation charger = CreateSimulation(grid, clock, GridPositionAtOrigin());
            charger.Advance();
            Assert.That(grid.TrySetTerrain(new GridPosition(0, 1), terrain), Is.True);
            clock.Advance(TelegraphDuration);
            charger.Advance();

            ChargerEnemyAdvanceResult collision = charger.Advance();

            Assert.That(collision.HasStateTransition, Is.True);
            Assert.That(collision.State, Is.EqualTo(ChargerEnemyState.Recover));
            Assert.That(collision.ImpactedTarget, Is.False);
            Assert.That(charger.CurrentPosition, Is.EqualTo(GridPositionAtOrigin()));
        }

        [Test]
        public void Charge_BombOrOtherActorCollisionRecoversWithoutTargetImpact()
        {
            foreach (bool useBomb in new[] { true, false })
            {
                var clock = new ManualGameClock();
                GridState grid = CreateFloorGrid();
                Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 4)), Is.True);
                ChargerEnemySimulation charger = CreateSimulation(grid, clock, GridPositionAtOrigin());
                charger.Advance();
                if (useBomb)
                {
                    Assert.That(grid.TryAddBomb(new GridPosition(0, 1)), Is.True);
                }
                else
                {
                    Assert.That(
                        grid.TryAddActor(ChaserActor, new GridPosition(0, 1)),
                        Is.True);
                }
                clock.Advance(TelegraphDuration);
                charger.Advance();

                ChargerEnemyAdvanceResult collision = charger.Advance();

                Assert.That(collision.State, Is.EqualTo(ChargerEnemyState.Recover));
                Assert.That(collision.ImpactedTarget, Is.False);
            }
        }

        [Test]
        public void Recover_WaitsForExactDurationBeforeTrackingAgain()
        {
            var clock = new ManualGameClock();
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 4)), Is.True);
            ChargerEnemySimulation charger = CreateSimulation(grid, clock, GridPositionAtOrigin());
            charger.Advance();
            Assert.That(grid.TrySetTerrain(
                new GridPosition(0, 1), GridTerrain.IndestructibleWall), Is.True);
            clock.Advance(TelegraphDuration);
            charger.Advance();
            charger.Advance();

            clock.Advance(RecoverDuration - TimeSpan.FromTicks(1));
            Assert.That(charger.Advance().HasActivity, Is.False);
            clock.Advance(TimeSpan.FromTicks(1));
            ChargerEnemyAdvanceResult recovered = charger.Advance();

            Assert.That(recovered.HasStateTransition, Is.True);
            Assert.That(recovered.PreviousState, Is.EqualTo(ChargerEnemyState.Recover));
            Assert.That(recovered.State, Is.EqualTo(ChargerEnemyState.Track));
            Assert.That(recovered.Direction, Is.EqualTo(CardinalDirection.None));
        }

        [Test]
        public void ClockMovingBackwards_IsRejectedWithoutChangingState()
        {
            var grid = CreateFloorGrid();
            var clock = new MutableGameClock(TimeSpan.FromSeconds(2));
            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 3)), Is.True);
            ChargerEnemySimulation charger = CreateSimulation(grid, clock, GridPositionAtOrigin());
            clock.Now = TimeSpan.FromSeconds(1);

            Assert.Throws<InvalidOperationException>(() => charger.Advance());
            Assert.That(charger.State, Is.EqualTo(ChargerEnemyState.Track));
            Assert.That(charger.CurrentPosition, Is.EqualTo(GridPositionAtOrigin()));
        }

        [Test]
        public void Constructor_RejectsInvalidIdsMissingTargetAndBlockedSpawn()
        {
            var grid = CreateFloorGrid();
            var clock = new ManualGameClock();
            ChargerEnemyDefinition definition = CreateDefinition();

            Assert.Throws<ArgumentException>(() =>
                new ChargerEnemySimulation(
                    grid, clock, definition, default, PlayerActor, GridPositionAtOrigin()));
            Assert.Throws<ArgumentException>(() =>
                new ChargerEnemySimulation(
                    grid, clock, definition, PlayerActor, PlayerActor, GridPositionAtOrigin()));
            Assert.Throws<InvalidOperationException>(() =>
                new ChargerEnemySimulation(
                    grid, clock, definition, ChargerActor, PlayerActor, GridPositionAtOrigin()));

            Assert.That(grid.TryAddActor(PlayerActor, new GridPosition(0, 3)), Is.True);
            Assert.That(grid.TrySetTerrain(
                GridPositionAtOrigin(), GridTerrain.IndestructibleWall), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                new ChargerEnemySimulation(
                    grid, clock, definition, ChargerActor, PlayerActor, GridPositionAtOrigin()));
        }

        private static void EnterCharge(
            ChargerEnemySimulation charger,
            ManualGameClock clock)
        {
            ChargerEnemyAdvanceResult telegraph = charger.Advance();
            Assert.That(telegraph.State, Is.EqualTo(ChargerEnemyState.Telegraph));
            clock.Advance(TelegraphDuration);
            ChargerEnemyAdvanceResult charge = charger.Advance();
            Assert.That(charge.State, Is.EqualTo(ChargerEnemyState.Charge));
        }

        private static ChargerEnemySimulation CreateSimulation(
            IGameClock clock,
            GridPosition playerPosition,
            GridPosition chargerPosition)
        {
            GridState grid = CreateFloorGrid();
            Assert.That(grid.TryAddActor(PlayerActor, playerPosition), Is.True);
            return CreateSimulation(grid, clock, chargerPosition);
        }

        private static ChargerEnemySimulation CreateSimulation(
            GridState grid,
            IGameClock clock,
            GridPosition chargerPosition)
        {
            return new ChargerEnemySimulation(
                grid,
                clock,
                CreateDefinition(),
                ChargerActor,
                PlayerActor,
                chargerPosition);
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

        private static GridPosition GridPositionAtOrigin()
        {
            return new GridPosition(0, 0);
        }

        private static EnemyDefinitionId CreateDefinitionId()
        {
            return new EnemyDefinitionId("prototype-charger");
        }

        private static ChargerEnemyDefinition CreateDefinition()
        {
            return new ChargerEnemyDefinition(
                CreateDefinitionId(),
                1,
                1,
                TelegraphDuration,
                ChargeStepInterval,
                RecoverDuration);
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
