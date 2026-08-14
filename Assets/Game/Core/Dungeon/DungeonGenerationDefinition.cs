using System;

namespace BombSwap.Core
{
    public sealed class DungeonGenerationDefinition
    {
        public const int MaximumSupportedCombatRooms = 5;

        public DungeonGenerationDefinition(
            int minimumCombatRooms,
            int maximumCombatRooms,
            int bossPathCombatRooms)
        {
            if (minimumCombatRooms <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumCombatRooms),
                    minimumCombatRooms,
                    "Minimum combat room count must be positive.");
            }
            if (maximumCombatRooms < minimumCombatRooms)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCombatRooms),
                    maximumCombatRooms,
                    "Maximum combat room count cannot be below the minimum.");
            }
            if (maximumCombatRooms > MaximumSupportedCombatRooms)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCombatRooms),
                    maximumCombatRooms,
                    $"Maximum combat room count cannot exceed {MaximumSupportedCombatRooms}.");
            }
            if (bossPathCombatRooms <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bossPathCombatRooms),
                    bossPathCombatRooms,
                    "Boss path combat room count must be positive.");
            }
            if (bossPathCombatRooms >= minimumCombatRooms)
            {
                throw new ArgumentException(
                    "At least one combat room must remain outside the boss path.",
                    nameof(bossPathCombatRooms));
            }

            MinimumCombatRooms = minimumCombatRooms;
            MaximumCombatRooms = maximumCombatRooms;
            BossPathCombatRooms = bossPathCombatRooms;
        }

        public int MinimumCombatRooms { get; }

        public int MaximumCombatRooms { get; }

        public int BossPathCombatRooms { get; }

        public static DungeonGenerationDefinition CreatePrototype()
        {
            return new DungeonGenerationDefinition(
                minimumCombatRooms: 4,
                maximumCombatRooms: 5,
                bossPathCombatRooms: 3);
        }
    }
}
