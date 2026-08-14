using System;

namespace BombSwap.Core
{
    public readonly struct BombWeaponSlotSnapshot
    {
        internal BombWeaponSlotSnapshot(
            int slotIndex,
            BombDefinitionId definitionId,
            TimeSpan placementCooldown,
            TimeSpan placementCooldownRemaining)
        {
            SlotIndex = slotIndex;
            DefinitionId = definitionId;
            PlacementCooldown = placementCooldown;
            PlacementCooldownRemaining = placementCooldownRemaining;
        }

        public int SlotIndex { get; }

        public BombDefinitionId DefinitionId { get; }

        public TimeSpan PlacementCooldown { get; }

        public TimeSpan PlacementCooldownRemaining { get; }

        public bool IsReady => PlacementCooldownRemaining == TimeSpan.Zero;

        public double ReadyFraction
        {
            get
            {
                if (PlacementCooldown <= TimeSpan.Zero)
                {
                    return 1d;
                }

                double remainingFraction =
                    PlacementCooldownRemaining.Ticks / (double)PlacementCooldown.Ticks;
                return Math.Max(0d, Math.Min(1d, 1d - remainingFraction));
            }
        }
    }
}
