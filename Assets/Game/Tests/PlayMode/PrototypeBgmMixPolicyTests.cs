using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeBgmMixPolicyTests
    {
        [TestCase(RoomType.Start, 1f, 0f, 0f, 0f)]
        [TestCase(RoomType.BossAntechamber, 1f, 0f, 0f, 0f)]
        [TestCase(RoomType.Secret, 1f, 0f, 0f, 0f)]
        [TestCase(RoomType.Recovery, 0.75f, 0f, 0f, 1f)]
        [TestCase(RoomType.BombReward, 0.85f, 0f, 0f, 0.6f)]
        [TestCase(RoomType.Combat, 1f, 1f, 0.45f, 0f)]
        public void DungeonMix_UsesAuthoredRoomPolicy(
            RoomType roomType,
            float baseWeight,
            float accentWeight,
            float dangerWeight,
            float sanctuaryWeight)
        {
            PrototypeBgmMix mix = PrototypeBgmMixPolicy.GetDungeonMix(roomType, false);

            Assert.That(mix.BaseWeight, Is.EqualTo(baseWeight));
            Assert.That(mix.AccentWeight, Is.EqualTo(accentWeight));
            Assert.That(mix.DangerWeight, Is.EqualTo(dangerWeight));
            Assert.That(mix.SanctuaryWeight, Is.EqualTo(sanctuaryWeight));
        }

        [Test]
        public void DungeonMix_WhenRoomIsCleared_UsesBaseOnly()
        {
            PrototypeBgmMix mix =
                PrototypeBgmMixPolicy.GetDungeonMix(RoomType.Combat, true);

            Assert.That(mix, Is.EqualTo(new PrototypeBgmMix(1f, 0f, 0f, 0f)));
        }

        [TestCase(BossPhase.One, 1f, 0.35f, 0.25f)]
        [TestCase(BossPhase.Two, 1f, 0.7f, 0.6f)]
        [TestCase(BossPhase.LastStand, 1f, 1f, 1f)]
        public void BossMix_UsesPhasePolicy(
            BossPhase phase,
            float baseWeight,
            float grandWeight,
            float dangerWeight)
        {
            PrototypeBgmMix mix = PrototypeBgmMixPolicy.GetBossMix(phase);

            Assert.That(mix.BaseWeight, Is.EqualTo(baseWeight));
            Assert.That(mix.AccentWeight, Is.EqualTo(grandWeight));
            Assert.That(mix.DangerWeight, Is.EqualTo(dangerWeight));
            Assert.That(mix.SanctuaryWeight, Is.Zero);
        }

        [Test]
        public void NextBarBoundary_LeavesSchedulingLeadAndStaysOnGrid()
        {
            const double familyStartedAt = 10d;
            double boundary = PrototypeBgmMixPolicy.GetNextBarBoundary(
                13.9d,
                familyStartedAt,
                2d,
                0.2d);

            Assert.That(boundary, Is.EqualTo(16d).Within(0.000001d));
            Assert.That((boundary - familyStartedAt) % 2d, Is.EqualTo(0d).Within(0.000001d));
        }

        [Test]
        public void DungeonMix_RejectsBossRoom()
        {
            Assert.Throws<ArgumentException>(() =>
                PrototypeBgmMixPolicy.GetDungeonMix(RoomType.Boss, false));
        }
    }
}
