using System;
using BombSwap.Core;

namespace BombSwap
{
    public enum PrototypeBgmFamily
    {
        None = 0,
        Lobby = 1,
        Dungeon = 2,
        Boss = 3,
    }

    public readonly struct PrototypeBgmMix : IEquatable<PrototypeBgmMix>
    {
        public PrototypeBgmMix(float baseWeight, float accentWeight, float dangerWeight, float sanctuaryWeight)
        {
            ValidateWeight(baseWeight, nameof(baseWeight));
            ValidateWeight(accentWeight, nameof(accentWeight));
            ValidateWeight(dangerWeight, nameof(dangerWeight));
            ValidateWeight(sanctuaryWeight, nameof(sanctuaryWeight));

            BaseWeight = baseWeight;
            AccentWeight = accentWeight;
            DangerWeight = dangerWeight;
            SanctuaryWeight = sanctuaryWeight;
        }

        public float BaseWeight { get; }

        public float AccentWeight { get; }

        public float DangerWeight { get; }

        public float SanctuaryWeight { get; }

        public bool Equals(PrototypeBgmMix other)
        {
            return BaseWeight.Equals(other.BaseWeight) &&
                AccentWeight.Equals(other.AccentWeight) &&
                DangerWeight.Equals(other.DangerWeight) &&
                SanctuaryWeight.Equals(other.SanctuaryWeight);
        }

        public override bool Equals(object obj)
        {
            return obj is PrototypeBgmMix other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = BaseWeight.GetHashCode();
                hashCode = (hashCode * 397) ^ AccentWeight.GetHashCode();
                hashCode = (hashCode * 397) ^ DangerWeight.GetHashCode();
                hashCode = (hashCode * 397) ^ SanctuaryWeight.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(PrototypeBgmMix left, PrototypeBgmMix right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PrototypeBgmMix left, PrototypeBgmMix right)
        {
            return !left.Equals(right);
        }

        private static void ValidateWeight(float value, string parameterName)
        {
            if (value < 0f || value > 1f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "BGM stem weights must be finite values from zero to one.");
            }
        }
    }

    public static class PrototypeBgmMixPolicy
    {
        public const double LobbyBarSeconds = 2.5d;
        public const double DungeonBarSeconds = 60d / 116d * 4d;
        public const double BossBarSeconds = 60d / 128d * 4d;

        public static readonly PrototypeBgmMix Lobby =
            new PrototypeBgmMix(1f, 0f, 0f, 0f);

        public static PrototypeBgmMix GetDungeonMix(RoomType roomType, bool isCleared)
        {
            if (isCleared)
            {
                return new PrototypeBgmMix(1f, 0f, 0f, 0f);
            }

            switch (roomType)
            {
                case RoomType.Combat:
                    return new PrototypeBgmMix(1f, 1f, 0.45f, 0f);
                case RoomType.Recovery:
                    return new PrototypeBgmMix(0.75f, 0f, 0f, 1f);
                case RoomType.BombReward:
                    return new PrototypeBgmMix(0.85f, 0f, 0f, 0.6f);
                case RoomType.Start:
                case RoomType.BossAntechamber:
                case RoomType.Secret:
                    return new PrototypeBgmMix(1f, 0f, 0f, 0f);
                case RoomType.Boss:
                    throw new ArgumentException(
                        "Boss rooms use the boss stem policy.",
                        nameof(roomType));
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomType), roomType, null);
            }
        }

        public static PrototypeBgmMix GetBossMix(BossPhase phase)
        {
            switch (phase)
            {
                case BossPhase.One:
                    return new PrototypeBgmMix(1f, 0.35f, 0.25f, 0f);
                case BossPhase.Two:
                    return new PrototypeBgmMix(1f, 0.7f, 0.6f, 0f);
                case BossPhase.LastStand:
                    return new PrototypeBgmMix(1f, 1f, 1f, 0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        public static double GetBarSeconds(PrototypeBgmFamily family)
        {
            switch (family)
            {
                case PrototypeBgmFamily.Lobby:
                    return LobbyBarSeconds;
                case PrototypeBgmFamily.Dungeon:
                    return DungeonBarSeconds;
                case PrototypeBgmFamily.Boss:
                    return BossBarSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, null);
            }
        }

        public static double GetNextBarBoundary(
            double currentDspTime,
            double familyStartedAtDsp,
            double barSeconds,
            double scheduleLeadSeconds)
        {
            if (barSeconds <= 0d || double.IsNaN(barSeconds) || double.IsInfinity(barSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(barSeconds));
            }
            if (scheduleLeadSeconds < 0d || double.IsNaN(scheduleLeadSeconds) ||
                double.IsInfinity(scheduleLeadSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(scheduleLeadSeconds));
            }

            double earliest = Math.Max(currentDspTime + scheduleLeadSeconds, familyStartedAtDsp);
            double elapsed = earliest - familyStartedAtDsp;
            long completedBars = Math.Max(0L, (long)Math.Floor(elapsed / barSeconds));
            double boundary = familyStartedAtDsp + ((completedBars + 1L) * barSeconds);
            return boundary < earliest ? boundary + barSeconds : boundary;
        }
    }
}
