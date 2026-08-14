using System;

namespace BombSwap.Core
{
    public sealed class BombWeaponDefinition
    {
        public BombWeaponDefinition(BombDefinition bomb, TimeSpan placementCooldown)
        {
            Bomb = bomb ?? throw new ArgumentNullException(nameof(bomb));
            if (placementCooldown <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementCooldown),
                    placementCooldown,
                    "Bomb placement cooldown must be greater than zero.");
            }

            PlacementCooldown = placementCooldown;
        }

        public BombDefinition Bomb { get; }

        public BombDefinitionId Id => Bomb.Id;

        public TimeSpan PlacementCooldown { get; }
    }
}
