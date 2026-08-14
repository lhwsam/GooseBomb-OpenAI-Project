using System;

namespace BombSwap.Core
{
    public sealed class BombWeaponLoadout
    {
        public const int SlotCount = 2;

        private readonly IGameClock clock;
        private readonly BombWeaponDefinition[] slots;
        private readonly TimeSpan[] nextPlacementAllowedAt = new TimeSpan[SlotCount];
        private readonly TimeSpan swapCooldown;
        private TimeSpan nextSwapAllowedAt;
        private int activeSlotIndex;

        public BombWeaponLoadout(
            IGameClock clock,
            BombWeaponDefinition firstSlot,
            BombWeaponDefinition secondSlot,
            TimeSpan swapCooldown)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (firstSlot == null)
            {
                throw new ArgumentNullException(nameof(firstSlot));
            }
            if (secondSlot != null && firstSlot.Id == secondSlot.Id)
            {
                throw new ArgumentException(
                    "The two bomb weapon slots must use different definition IDs.",
                    nameof(secondSlot));
            }
            if (swapCooldown <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(swapCooldown),
                    swapCooldown,
                    "Bomb swap cooldown must be greater than zero.");
            }

            slots = new[] { firstSlot, secondSlot };
            this.swapCooldown = swapCooldown;
        }

        public int ActiveSlotIndex => activeSlotIndex;

        public BombWeaponDefinition ActiveDefinition => slots[activeSlotIndex];

        public bool HasSecondSlot => slots[1] != null;

        public bool CanSwap => HasSecondSlot && IsSwapReady;

        public TimeSpan SwapCooldown => swapCooldown;

        public TimeSpan SwapCooldownRemaining => RemainingUntil(nextSwapAllowedAt);

        public bool IsSwapReady => SwapCooldownRemaining == TimeSpan.Zero;

        public BombWeaponSlotSnapshot GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            BombWeaponDefinition definition = slots[slotIndex];
            if (definition == null)
            {
                return new BombWeaponSlotSnapshot(slotIndex);
            }
            return new BombWeaponSlotSnapshot(
                slotIndex,
                definition.Id,
                definition.PlacementCooldown,
                RemainingUntil(nextPlacementAllowedAt[slotIndex]));
        }

        public bool TrySwap()
        {
            if (!CanSwap)
            {
                return false;
            }

            activeSlotIndex = 1 - activeSlotIndex;
            nextSwapAllowedAt = AddWithSaturation(clock.Now, swapCooldown);
            return true;
        }

        public bool TryEquipSecondSlot(BombWeaponDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (HasSecondSlot || definition.Id == slots[0].Id)
            {
                return false;
            }

            slots[1] = definition;
            nextPlacementAllowedAt[1] = TimeSpan.Zero;
            return true;
        }

        public bool TryPlaceActiveBomb(
            BombSimulation bombs,
            GridPosition position,
            ActorId ownerId,
            out BombSnapshot snapshot)
        {
            if (bombs == null)
            {
                throw new ArgumentNullException(nameof(bombs));
            }

            snapshot = default;
            if (!GetSlot(activeSlotIndex).IsReady)
            {
                return false;
            }

            BombWeaponDefinition definition = slots[activeSlotIndex];
            if (!bombs.TryPlaceBomb(definition.Bomb, position, ownerId, out BombId bombId))
            {
                return false;
            }

            nextPlacementAllowedAt[activeSlotIndex] =
                AddWithSaturation(clock.Now, definition.PlacementCooldown);
            if (!bombs.TryGetBomb(bombId, out snapshot))
            {
                throw new InvalidOperationException(
                    "A successfully placed bomb was not available for the loadout snapshot.");
            }

            return true;
        }

        private TimeSpan RemainingUntil(TimeSpan availableAt)
        {
            TimeSpan now = clock.Now;
            return availableAt <= now ? TimeSpan.Zero : availableAt - now;
        }

        private static TimeSpan AddWithSaturation(TimeSpan value, TimeSpan increment)
        {
            return value > TimeSpan.MaxValue - increment
                ? TimeSpan.MaxValue
                : value.Add(increment);
        }
    }
}
