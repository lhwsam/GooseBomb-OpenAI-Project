using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BombWeaponLoadoutTests
    {
        private static readonly ActorId Player = new ActorId(1);
        private static readonly GridPosition Origin = new GridPosition(0, 0);
        private static readonly GridPosition East = new GridPosition(1, 0);

        [Test]
        public void SuccessfulPlacement_ConsumesOnlyTheActiveSlotCooldown()
        {
            ManualGameClock clock = new ManualGameClock();
            BombSimulation bombs = CreateBombSimulation(clock, Origin, East);
            BombWeaponLoadout loadout = CreateLoadout(clock);

            Assert.That(
                loadout.TryPlaceActiveBomb(bombs, Origin, Player, out BombSnapshot placed),
                Is.True);

            Assert.That(placed.DefinitionId, Is.EqualTo(new BombDefinitionId("primary")));
            Assert.That(loadout.GetSlot(0).IsReady, Is.False);
            Assert.That(
                loadout.GetSlot(0).PlacementCooldownRemaining,
                Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(loadout.GetSlot(1).IsReady, Is.True);
            Assert.That(loadout.GetSlot(1).PlacementCooldownRemaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void InactiveSlotCooldown_RecoversOnTheSharedGameClock()
        {
            ManualGameClock clock = new ManualGameClock();
            BombSimulation bombs = CreateBombSimulation(clock, Origin, East);
            BombWeaponLoadout loadout = CreateLoadout(clock);
            Assert.That(loadout.TryPlaceActiveBomb(bombs, Origin, Player, out _), Is.True);
            Assert.That(loadout.TrySwap(), Is.True);

            clock.Advance(TimeSpan.FromSeconds(1.25));

            Assert.That(loadout.ActiveSlotIndex, Is.EqualTo(1));
            Assert.That(
                loadout.GetSlot(0).PlacementCooldownRemaining,
                Is.EqualTo(TimeSpan.FromSeconds(0.75)));
            Assert.That(loadout.GetSlot(1).IsReady, Is.True);

            clock.Advance(TimeSpan.FromSeconds(0.75));

            Assert.That(loadout.GetSlot(0).IsReady, Is.True);
            Assert.That(loadout.GetSlot(0).PlacementCooldownRemaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void FailedPlacement_DoesNotConsumeTheActiveSlotCooldown()
        {
            ManualGameClock clock = new ManualGameClock();
            BombSimulation bombs = CreateBombSimulation(clock, Origin);
            BombWeaponLoadout loadout = CreateLoadout(clock);
            var unavailable = new GridPosition(9, 9);

            Assert.That(
                loadout.TryPlaceActiveBomb(bombs, unavailable, Player, out BombSnapshot placed),
                Is.False);

            Assert.That(placed, Is.EqualTo(default(BombSnapshot)));
            Assert.That(loadout.GetSlot(0).IsReady, Is.True);
            Assert.That(loadout.GetSlot(0).PlacementCooldownRemaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void SwapCooldown_RejectsEarlySwapWithoutChangingOrExtendingState()
        {
            ManualGameClock clock = new ManualGameClock();
            BombWeaponLoadout loadout = CreateLoadout(clock);
            Assert.That(loadout.TrySwap(), Is.True);
            Assert.That(loadout.ActiveSlotIndex, Is.EqualTo(1));
            Assert.That(loadout.SwapCooldownRemaining, Is.EqualTo(TimeSpan.FromSeconds(0.5)));

            clock.Advance(TimeSpan.FromSeconds(0.25));
            Assert.That(loadout.TrySwap(), Is.False);

            Assert.That(loadout.ActiveSlotIndex, Is.EqualTo(1));
            Assert.That(loadout.SwapCooldownRemaining, Is.EqualTo(TimeSpan.FromSeconds(0.25)));

            clock.Advance(TimeSpan.FromSeconds(0.25));
            Assert.That(loadout.TrySwap(), Is.True);
            Assert.That(loadout.ActiveSlotIndex, Is.Zero);
        }

        [Test]
        public void IndependentSlots_CanPlaceDifferentDefinitionsWhileFirstSlotCoolsDown()
        {
            ManualGameClock clock = new ManualGameClock();
            BombSimulation bombs = CreateBombSimulation(clock, Origin, East);
            BombWeaponLoadout loadout = CreateLoadout(clock);
            Assert.That(
                loadout.TryPlaceActiveBomb(bombs, Origin, Player, out BombSnapshot first),
                Is.True);
            Assert.That(loadout.TrySwap(), Is.True);

            Assert.That(
                loadout.TryPlaceActiveBomb(bombs, East, Player, out BombSnapshot second),
                Is.True);

            Assert.That(first.DefinitionId, Is.EqualTo(new BombDefinitionId("primary")));
            Assert.That(second.DefinitionId, Is.EqualTo(new BombDefinitionId("secondary")));
            Assert.That(bombs.ActiveBombCount, Is.EqualTo(2));
            Assert.That(loadout.GetSlot(0).IsReady, Is.False);
            Assert.That(loadout.GetSlot(1).IsReady, Is.False);
        }

        [Test]
        public void Cooldowns_DoNotAdvanceWhenTheInjectedClockDoesNotAdvance()
        {
            ManualGameClock clock = new ManualGameClock();
            BombSimulation bombs = CreateBombSimulation(clock, Origin);
            BombWeaponLoadout loadout = CreateLoadout(clock);
            Assert.That(loadout.TryPlaceActiveBomb(bombs, Origin, Player, out _), Is.True);
            Assert.That(loadout.TrySwap(), Is.True);

            BombWeaponSlotSnapshot before = loadout.GetSlot(0);
            TimeSpan swapBefore = loadout.SwapCooldownRemaining;
            BombWeaponSlotSnapshot after = loadout.GetSlot(0);

            Assert.That(after.PlacementCooldownRemaining, Is.EqualTo(before.PlacementCooldownRemaining));
            Assert.That(loadout.SwapCooldownRemaining, Is.EqualTo(swapBefore));
        }

        [Test]
        public void EmptySecondSlot_BlocksSwapAndReportsLockedSnapshot()
        {
            ManualGameClock clock = new ManualGameClock();
            BombWeaponDefinition primary = new BombWeaponDefinition(
                CreateBombDefinition("primary"),
                TimeSpan.FromSeconds(2));
            var loadout = new BombWeaponLoadout(
                clock,
                primary,
                null,
                TimeSpan.FromSeconds(0.5));

            BombWeaponSlotSnapshot empty = loadout.GetSlot(1);

            Assert.That(loadout.HasSecondSlot, Is.False);
            Assert.That(loadout.CanSwap, Is.False);
            Assert.That(loadout.TrySwap(), Is.False);
            Assert.That(loadout.ActiveSlotIndex, Is.Zero);
            Assert.That(empty.HasDefinition, Is.False);
            Assert.That(empty.IsReady, Is.False);
            Assert.That(empty.ReadyFraction, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => _ = empty.DefinitionId);
        }

        [Test]
        public void EquipSecondSlot_FillsOnlyOnceWithoutConsumingCooldown()
        {
            ManualGameClock clock = new ManualGameClock();
            BombWeaponDefinition primary = new BombWeaponDefinition(
                CreateBombDefinition("primary"),
                TimeSpan.FromSeconds(2));
            BombWeaponDefinition reward = new BombWeaponDefinition(
                CreateBombDefinition("reward"),
                TimeSpan.FromSeconds(1));
            var loadout = new BombWeaponLoadout(
                clock,
                primary,
                null,
                TimeSpan.FromSeconds(0.5));

            Assert.That(loadout.TryEquipSecondSlot(reward), Is.True);

            BombWeaponSlotSnapshot equipped = loadout.GetSlot(1);
            Assert.That(loadout.HasSecondSlot, Is.True);
            Assert.That(loadout.CanSwap, Is.True);
            Assert.That(equipped.HasDefinition, Is.True);
            Assert.That(equipped.DefinitionId, Is.EqualTo(reward.Id));
            Assert.That(equipped.IsReady, Is.True);
            Assert.That(equipped.PlacementCooldownRemaining, Is.EqualTo(TimeSpan.Zero));
            Assert.That(
                loadout.TryEquipSecondSlot(new BombWeaponDefinition(
                    CreateBombDefinition("other"),
                    TimeSpan.FromSeconds(1))),
                Is.False);
        }

        [Test]
        public void EquipSecondSlot_RejectsNullAndDuplicateFirstDefinition()
        {
            ManualGameClock clock = new ManualGameClock();
            BombWeaponDefinition primary = new BombWeaponDefinition(
                CreateBombDefinition("primary"),
                TimeSpan.FromSeconds(2));
            var loadout = new BombWeaponLoadout(
                clock,
                primary,
                null,
                TimeSpan.FromSeconds(0.5));

            Assert.Throws<ArgumentNullException>(() => loadout.TryEquipSecondSlot(null));
            Assert.That(
                loadout.TryEquipSecondSlot(new BombWeaponDefinition(
                    CreateBombDefinition("primary"),
                    TimeSpan.FromSeconds(1))),
                Is.False);
            Assert.That(loadout.HasSecondSlot, Is.False);
        }

        [Test]
        public void Constructor_RejectsDuplicateDefinitionIdsAndNonPositiveCooldowns()
        {
            ManualGameClock clock = new ManualGameClock();
            BombDefinition duplicate = CreateBombDefinition("primary");
            BombWeaponDefinition primary = new BombWeaponDefinition(
                duplicate,
                TimeSpan.FromSeconds(1));
            BombWeaponDefinition sameId = new BombWeaponDefinition(
                CreateBombDefinition("primary"),
                TimeSpan.FromSeconds(2));

            Assert.Throws<ArgumentException>(() => new BombWeaponLoadout(
                clock,
                primary,
                sameId,
                TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BombWeaponDefinition(
                duplicate,
                TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BombWeaponLoadout(
                clock,
                primary,
                new BombWeaponDefinition(CreateBombDefinition("secondary"), TimeSpan.FromSeconds(1)),
                TimeSpan.Zero));
        }

        private static BombWeaponLoadout CreateLoadout(ManualGameClock clock)
        {
            return new BombWeaponLoadout(
                clock,
                new BombWeaponDefinition(
                    CreateBombDefinition("primary"),
                    TimeSpan.FromSeconds(2)),
                new BombWeaponDefinition(
                    CreateBombDefinition("secondary"),
                    TimeSpan.FromSeconds(1)),
                TimeSpan.FromSeconds(0.5));
        }

        private static BombSimulation CreateBombSimulation(
            ManualGameClock clock,
            params GridPosition[] floorCells)
        {
            var grid = new GridState();
            for (int index = 0; index < floorCells.Length; index++)
            {
                Assert.That(grid.TrySetTerrain(floorCells[index], GridTerrain.Floor), Is.True);
            }

            return new BombSimulation(grid, clock, TimeSpan.FromSeconds(0.1));
        }

        private static BombDefinition CreateBombDefinition(string id)
        {
            return new BombDefinition(
                new BombDefinitionId(id),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(10),
                1);
        }
    }
}
