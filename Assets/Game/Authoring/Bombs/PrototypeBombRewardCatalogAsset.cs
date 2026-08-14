using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeBombRewardCatalog",
        menuName = "Bomb Swap/Prototype/Bomb Reward Catalog")]
    public sealed class PrototypeBombRewardCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private PrototypeBombDefinitionAsset firstSlot;

        [SerializeField]
        private PrototypeBombDefinitionAsset[] rewardCandidates =
            Array.Empty<PrototypeBombDefinitionAsset>();

        [SerializeField]
        private float swapCooldownSeconds = 2f;

        public PrototypeBombDefinitionAsset FirstSlot => firstSlot;

        public IReadOnlyList<PrototypeBombDefinitionAsset> RewardCandidates =>
            Array.AsReadOnly(rewardCandidates ?? Array.Empty<PrototypeBombDefinitionAsset>());

        public float SwapCooldownSeconds => swapCooldownSeconds;

        public void Configure(
            PrototypeBombDefinitionAsset authoredFirstSlot,
            PrototypeBombDefinitionAsset[] authoredRewardCandidates,
            float authoredSwapCooldownSeconds)
        {
            ValidateFinitePositive(
                authoredSwapCooldownSeconds,
                nameof(authoredSwapCooldownSeconds));
            PrototypeBombDefinitionAsset[] copy = ValidateAndCopy(
                authoredFirstSlot,
                authoredRewardCandidates);
            firstSlot = authoredFirstSlot;
            rewardCandidates = copy;
            swapCooldownSeconds = authoredSwapCooldownSeconds;
        }

        public void Validate()
        {
            ValidateFinitePositive(swapCooldownSeconds, nameof(swapCooldownSeconds));
            ValidateAndCopy(firstSlot, rewardCandidates);
        }

        public DungeonBombLoadoutState CreateRunLoadoutState()
        {
            Validate();
            var candidateIds = new BombDefinitionId[rewardCandidates.Length];
            for (int index = 0; index < rewardCandidates.Length; index++)
            {
                candidateIds[index] = new BombDefinitionId(
                    rewardCandidates[index].DefinitionId);
            }
            return new DungeonBombLoadoutState(
                new BombDefinitionId(firstSlot.DefinitionId),
                candidateIds);
        }

        public PrototypeBombDefinitionAsset GetDefinition(BombDefinitionId definitionId)
        {
            if (firstSlot != null && firstSlot.DefinitionId == definitionId.Value)
            {
                return firstSlot;
            }
            if (rewardCandidates != null)
            {
                for (int index = 0; index < rewardCandidates.Length; index++)
                {
                    PrototypeBombDefinitionAsset candidate = rewardCandidates[index];
                    if (candidate != null && candidate.DefinitionId == definitionId.Value)
                    {
                        return candidate;
                    }
                }
            }
            throw new KeyNotFoundException(
                $"Bomb definition '{definitionId}' is not part of the dungeon reward catalog.");
        }

        public PrototypeBombDefinitionAsset[] GetAvailableDefinitions()
        {
            Validate();
            var definitions = new PrototypeBombDefinitionAsset[rewardCandidates.Length + 1];
            definitions[0] = firstSlot;
            Array.Copy(rewardCandidates, 0, definitions, 1, rewardCandidates.Length);
            return definitions;
        }

        private static PrototypeBombDefinitionAsset[] ValidateAndCopy(
            PrototypeBombDefinitionAsset authoredFirstSlot,
            IReadOnlyList<PrototypeBombDefinitionAsset> authoredCandidates)
        {
            if (authoredFirstSlot == null)
            {
                throw new ArgumentNullException(nameof(authoredFirstSlot));
            }
            if (authoredCandidates == null)
            {
                throw new ArgumentNullException(nameof(authoredCandidates));
            }
            if (authoredCandidates.Count < DungeonBombLoadoutState.MinimumRewardCandidateCount ||
                authoredCandidates.Count > DungeonBombLoadoutState.MaximumRewardCandidateCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredCandidates),
                    authoredCandidates.Count,
                    "Dungeon bomb reward requires two or three candidates.");
            }

            BombWeaponDefinition first = authoredFirstSlot.CreateCoreWeaponDefinition();
            var copy = new PrototypeBombDefinitionAsset[authoredCandidates.Count];
            for (int index = 0; index < authoredCandidates.Count; index++)
            {
                PrototypeBombDefinitionAsset candidate = authoredCandidates[index];
                if (candidate == null)
                {
                    throw new ArgumentException(
                        "Dungeon bomb reward candidates cannot contain null.",
                        nameof(authoredCandidates));
                }
                BombWeaponDefinition candidateDefinition =
                    candidate.CreateCoreWeaponDefinition();
                if (candidateDefinition.Id == first.Id)
                {
                    throw new ArgumentException(
                        "The dungeon first bomb cannot also be a reward candidate.",
                        nameof(authoredCandidates));
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (copy[previous].DefinitionId == candidate.DefinitionId)
                    {
                        throw new ArgumentException(
                            "Dungeon bomb reward candidate IDs must be unique.",
                            nameof(authoredCandidates));
                    }
                }
                copy[index] = candidate;
            }
            return copy;
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
