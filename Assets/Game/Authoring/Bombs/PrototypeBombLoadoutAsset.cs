using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeBombLoadout",
        menuName = "Bomb Swap/Prototype/Bomb Loadout")]
    public sealed class PrototypeBombLoadoutAsset : ScriptableObject
    {
        [SerializeField]
        private PrototypeBombDefinitionAsset firstSlot;

        [SerializeField]
        private PrototypeBombDefinitionAsset secondSlot;

        [SerializeField]
        private float swapCooldownSeconds = 2f;

        public PrototypeBombDefinitionAsset FirstSlot => firstSlot;

        public PrototypeBombDefinitionAsset SecondSlot => secondSlot;

        public float SwapCooldownSeconds => swapCooldownSeconds;

        public void Configure(
            PrototypeBombDefinitionAsset firstBomb,
            PrototypeBombDefinitionAsset secondBomb,
            float swapCooldownDurationSeconds)
        {
            if (firstBomb == null)
            {
                throw new ArgumentNullException(nameof(firstBomb));
            }
            if (secondBomb == null)
            {
                throw new ArgumentNullException(nameof(secondBomb));
            }
            ValidateFinitePositive(
                swapCooldownDurationSeconds,
                nameof(swapCooldownDurationSeconds));

            BombWeaponDefinition firstDefinition = firstBomb.CreateCoreWeaponDefinition();
            BombWeaponDefinition secondDefinition = secondBomb.CreateCoreWeaponDefinition();
            if (firstDefinition.Id == secondDefinition.Id)
            {
                throw new ArgumentException(
                    "Bomb loadout slots must use different definition IDs.",
                    nameof(secondBomb));
            }

            firstSlot = firstBomb;
            secondSlot = secondBomb;
            swapCooldownSeconds = swapCooldownDurationSeconds;
        }

        public BombWeaponLoadout CreateCoreLoadout(IGameClock clock)
        {
            if (firstSlot == null || secondSlot == null)
            {
                throw new InvalidOperationException(
                    "Prototype bomb loadout requires two bomb definition assets.");
            }

            ValidateFinitePositive(swapCooldownSeconds, nameof(swapCooldownSeconds));
            return new BombWeaponLoadout(
                clock,
                firstSlot.CreateCoreWeaponDefinition(),
                secondSlot.CreateCoreWeaponDefinition(),
                TimeSpan.FromSeconds(swapCooldownSeconds));
        }

        public PrototypeBombDefinitionAsset GetSlot(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return firstSlot;
                case 1:
                    return secondSlot;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }

        public PrototypeBombDefinitionAsset GetDefinition(BombDefinitionId definitionId)
        {
            if (firstSlot != null && firstSlot.DefinitionId == definitionId.Value)
            {
                return firstSlot;
            }
            if (secondSlot != null && secondSlot.DefinitionId == definitionId.Value)
            {
                return secondSlot;
            }

            throw new InvalidOperationException(
                $"Bomb definition '{definitionId}' is not part of this prototype loadout.");
        }

        private static void ValidateFinitePositive(float value, string parameterName)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and positive.");
            }
        }
    }
}
