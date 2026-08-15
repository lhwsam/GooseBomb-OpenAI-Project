using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class DungeonBombLoadoutState
    {
        public const int MinimumRewardCandidateCount = 2;
        public const int MaximumRewardCandidateCount = 3;

        private readonly BombDefinitionId[] rewardCandidates;
        private readonly IReadOnlyList<BombDefinitionId> readOnlyRewardCandidates;
        private BombDefinitionId? secondSlot;

        public DungeonBombLoadoutState(
            BombDefinitionId firstSlot,
            IReadOnlyList<BombDefinitionId> authoredRewardCandidates)
        {
            if (!firstSlot.IsValid)
            {
                throw new ArgumentException(
                    "Dungeon first bomb definition ID must be valid.",
                    nameof(firstSlot));
            }
            if (authoredRewardCandidates == null)
            {
                throw new ArgumentNullException(nameof(authoredRewardCandidates));
            }
            if (authoredRewardCandidates.Count < MinimumRewardCandidateCount ||
                authoredRewardCandidates.Count > MaximumRewardCandidateCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredRewardCandidates),
                    authoredRewardCandidates.Count,
                    $"Bomb reward requires {MinimumRewardCandidateCount} to " +
                    $"{MaximumRewardCandidateCount} candidates.");
            }

            FirstSlot = firstSlot;
            rewardCandidates = new BombDefinitionId[authoredRewardCandidates.Count];
            for (int index = 0; index < authoredRewardCandidates.Count; index++)
            {
                BombDefinitionId candidate = authoredRewardCandidates[index];
                if (!candidate.IsValid)
                {
                    throw new ArgumentException(
                        "Bomb reward candidate IDs must be valid.",
                        nameof(authoredRewardCandidates));
                }
                if (candidate == firstSlot)
                {
                    throw new ArgumentException(
                        "The first bomb cannot also be a reward candidate.",
                        nameof(authoredRewardCandidates));
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (rewardCandidates[previous] == candidate)
                    {
                        throw new ArgumentException(
                            "Bomb reward candidate IDs must be unique.",
                            nameof(authoredRewardCandidates));
                    }
                }
                rewardCandidates[index] = candidate;
            }
            readOnlyRewardCandidates = Array.AsReadOnly(rewardCandidates);
        }

        public BombDefinitionId FirstSlot { get; }

        public BombDefinitionId? SecondSlot => secondSlot;

        public int ActiveSlotIndex { get; private set; }

        public bool HasSelectedReward => secondSlot.HasValue;

        public IReadOnlyList<BombDefinitionId> RewardCandidates =>
            readOnlyRewardCandidates;

        public DungeonBombRewardSelectionStatus TrySelectReward(
            BombDefinitionId candidateId)
        {
            if (secondSlot.HasValue)
            {
                return DungeonBombRewardSelectionStatus.AlreadySelected;
            }
            for (int index = 0; index < rewardCandidates.Length; index++)
            {
                if (rewardCandidates[index] != candidateId)
                {
                    continue;
                }

                secondSlot = candidateId;
                return DungeonBombRewardSelectionStatus.Selected;
            }
            return DungeonBombRewardSelectionStatus.NotCandidate;
        }

        public bool TrySetActiveSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= BombWeaponLoadout.SlotCount)
            {
                return false;
            }
            if (slotIndex == 1 && !secondSlot.HasValue)
            {
                return false;
            }

            ActiveSlotIndex = slotIndex;
            return true;
        }
    }
}
