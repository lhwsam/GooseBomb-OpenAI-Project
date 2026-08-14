using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class DungeonBombLoadoutStateTests
    {
        private static readonly BombDefinitionId Starter =
            new BombDefinitionId("starter");
        private static readonly BombDefinitionId Area =
            new BombDefinitionId("area");
        private static readonly BombDefinitionId LongCross =
            new BombDefinitionId("long-cross");

        [Test]
        public void NewRun_HasOneBombAndReadOnlyRewardCandidates()
        {
            var source = new[] { Area, LongCross };
            var state = new DungeonBombLoadoutState(Starter, source);

            source[0] = new BombDefinitionId("mutated");

            Assert.That(state.FirstSlot, Is.EqualTo(Starter));
            Assert.That(state.SecondSlot.HasValue, Is.False);
            Assert.That(state.HasSelectedReward, Is.False);
            Assert.That(state.RewardCandidates, Is.EqualTo(new[] { Area, LongCross }));
        }

        [Test]
        public void CandidateSelection_FillsSecondSlotExactlyOnce()
        {
            var state = new DungeonBombLoadoutState(
                Starter,
                new[] { Area, LongCross });

            Assert.That(
                state.TrySelectReward(LongCross),
                Is.EqualTo(DungeonBombRewardSelectionStatus.Selected));
            Assert.That(state.SecondSlot, Is.EqualTo(LongCross));
            Assert.That(state.HasSelectedReward, Is.True);
            Assert.That(
                state.TrySelectReward(Area),
                Is.EqualTo(DungeonBombRewardSelectionStatus.AlreadySelected));
            Assert.That(state.SecondSlot, Is.EqualTo(LongCross));
        }

        [Test]
        public void UnknownCandidate_DoesNotChangeLoadout()
        {
            var state = new DungeonBombLoadoutState(
                Starter,
                new[] { Area, LongCross });

            Assert.That(
                state.TrySelectReward(new BombDefinitionId("unknown")),
                Is.EqualTo(DungeonBombRewardSelectionStatus.NotCandidate));
            Assert.That(state.HasSelectedReward, Is.False);
        }

        [Test]
        public void Constructor_RejectsInvalidCountsAndDuplicateIds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DungeonBombLoadoutState(Starter, new[] { Area }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DungeonBombLoadoutState(
                    Starter,
                    new[]
                    {
                        Area,
                        LongCross,
                        new BombDefinitionId("third"),
                        new BombDefinitionId("fourth"),
                    }));
            Assert.Throws<ArgumentException>(() =>
                new DungeonBombLoadoutState(Starter, new[] { Starter, Area }));
            Assert.Throws<ArgumentException>(() =>
                new DungeonBombLoadoutState(Starter, new[] { Area, Area }));
            Assert.Throws<ArgumentException>(() =>
                new DungeonBombLoadoutState(
                    default,
                    new[] { Area, LongCross }));
        }
    }
}
