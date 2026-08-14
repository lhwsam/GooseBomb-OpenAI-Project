using System;

namespace BombSwap.Core
{
    public sealed class BossBattleDefinition
    {
        public BossBattleDefinition(
            EnemyDefinitionId id,
            int maxHealth,
            int phaseTwoHealthThreshold,
            int patternDamage,
            BossPatternTimings phaseOneTimings,
            BossPatternTimings phaseTwoTimings)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Boss definition ID must be valid.", nameof(id));
            }
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth),
                    maxHealth,
                    "Boss maximum health must be positive.");
            }
            if (phaseTwoHealthThreshold <= 0 || phaseTwoHealthThreshold >= maxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phaseTwoHealthThreshold),
                    phaseTwoHealthThreshold,
                    "Phase-two threshold must be positive and below maximum health.");
            }
            if (patternDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(patternDamage),
                    patternDamage,
                    "Boss pattern damage must be positive.");
            }
            ValidateTimings(phaseOneTimings, nameof(phaseOneTimings));
            ValidateTimings(phaseTwoTimings, nameof(phaseTwoTimings));

            Id = id;
            MaxHealth = maxHealth;
            PhaseTwoHealthThreshold = phaseTwoHealthThreshold;
            PatternDamage = patternDamage;
            PhaseOneTimings = phaseOneTimings;
            PhaseTwoTimings = phaseTwoTimings;
        }

        public EnemyDefinitionId Id { get; }

        public int MaxHealth { get; }

        public int PhaseTwoHealthThreshold { get; }

        public int PatternDamage { get; }

        public BossPatternTimings PhaseOneTimings { get; }

        public BossPatternTimings PhaseTwoTimings { get; }

        public BossPatternTimings GetTimings(BossPhase phase)
        {
            switch (phase)
            {
                case BossPhase.One:
                    return PhaseOneTimings;
                case BossPhase.Two:
                    return PhaseTwoTimings;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase),
                        phase,
                        "Unsupported boss phase.");
            }
        }

        private static void ValidateTimings(
            BossPatternTimings timings,
            string parameterName)
        {
            if (timings.TelegraphDuration <= TimeSpan.Zero ||
                timings.ExecuteDuration <= TimeSpan.Zero ||
                timings.RecoveryDuration <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "Boss phase timings must be initialized with positive durations.",
                    parameterName);
            }
        }
    }
}
