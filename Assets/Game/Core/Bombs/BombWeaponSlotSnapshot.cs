using System;

namespace BombSwap.Core
{
    public readonly struct BombWeaponSlotSnapshot
    {
        private readonly BombDefinitionId? definitionId;

        internal BombWeaponSlotSnapshot(int slotIndex)
        {
            SlotIndex = slotIndex;
            definitionId = null;
            PlacementCooldown = TimeSpan.Zero;
            PlacementCooldownRemaining = TimeSpan.Zero;
        }

        internal BombWeaponSlotSnapshot(
            int slotIndex,
            BombDefinitionId definitionId,
            TimeSpan placementCooldown,
            TimeSpan placementCooldownRemaining)
        {
            SlotIndex = slotIndex;
            this.definitionId = definitionId;
            PlacementCooldown = placementCooldown;
            PlacementCooldownRemaining = placementCooldownRemaining;
        }

        public int SlotIndex { get; }

        public bool HasDefinition => definitionId.HasValue;

        public BombDefinitionId DefinitionId => definitionId ??
            throw new InvalidOperationException("An empty bomb slot has no definition ID.");

        public TimeSpan PlacementCooldown { get; }

        public TimeSpan PlacementCooldownRemaining { get; }

        public bool IsReady => HasDefinition && PlacementCooldownRemaining == TimeSpan.Zero;

        public double ReadyFraction
        {
            get
            {
                if (!HasDefinition)
                {
                    return 0d;
                }
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
