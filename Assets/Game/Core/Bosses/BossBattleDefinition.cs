using System;

namespace BombSwap.Core
{
    public sealed class BossBattleDefinition
    {
        public BossBattleDefinition(
            EnemyDefinitionId id,
            int maxHealth,
            int phaseTwoHealthThreshold,
            int lastStandHealthThreshold,
            int patternDamage,
            int maxOverheatDamage,
            BossPatternTuning tuning,
            BombDefinition throwBombDefinition,
            BombDefinition chainBombDefinition)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Boss definition ID must be valid.", nameof(id));
            }
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth), maxHealth, "Boss maximum health must be positive.");
            }
            if (phaseTwoHealthThreshold <= 0 || phaseTwoHealthThreshold >= maxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phaseTwoHealthThreshold),
                    phaseTwoHealthThreshold,
                    "Phase-two threshold must be positive and below maximum health.");
            }
            if (lastStandHealthThreshold <= 0 ||
                lastStandHealthThreshold >= phaseTwoHealthThreshold)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastStandHealthThreshold),
                    lastStandHealthThreshold,
                    "Last-stand threshold must be positive and below phase two.");
            }
            if (patternDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(patternDamage), patternDamage, "Boss pattern damage must be positive.");
            }
            if (maxOverheatDamage <= 0 || maxOverheatDamage > maxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxOverheatDamage),
                    maxOverheatDamage,
                    "Overheat damage cap must be positive and no greater than maximum health.");
            }

            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            ValidateBossBombDefinition(throwBombDefinition, nameof(throwBombDefinition));
            ValidateBossBombDefinition(chainBombDefinition, nameof(chainBombDefinition));
            if (throwBombDefinition.Id == chainBombDefinition.Id)
            {
                throw new ArgumentException(
                    "Boss throw and chain bombs must use different definition IDs.",
                    nameof(chainBombDefinition));
            }
            if (chainBombDefinition.FuseDuration <= throwBombDefinition.FuseDuration)
            {
                throw new ArgumentException(
                    "Boss chain bomb fuse must outlast the throw bomb fuse.",
                    nameof(chainBombDefinition));
            }

            Id = id;
            MaxHealth = maxHealth;
            PhaseTwoHealthThreshold = phaseTwoHealthThreshold;
            LastStandHealthThreshold = lastStandHealthThreshold;
            PatternDamage = patternDamage;
            MaxOverheatDamage = maxOverheatDamage;
            ThrowBombDefinition = throwBombDefinition;
            ChainBombDefinition = chainBombDefinition;
        }

        public EnemyDefinitionId Id { get; }
        public int MaxHealth { get; }
        public int PhaseTwoHealthThreshold { get; }
        public int LastStandHealthThreshold { get; }
        public int PatternDamage { get; }
        public int MaxOverheatDamage { get; }
        public BossPatternTuning Tuning { get; }
        public BombDefinition ThrowBombDefinition { get; }
        public BombDefinition ChainBombDefinition { get; }

        public BossPatternTimings GetTimings(BossPhase phase, BossPatternKind pattern)
        {
            return Tuning.GetTimings(phase, pattern);
        }

        private static void ValidateBossBombDefinition(
            BombDefinition definition,
            string parameterName)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (definition.ExplosionShape != BombExplosionShape.Cross ||
                definition.Range <= 0)
            {
                throw new ArgumentException(
                    "Boss bombs must use a positive-range cross explosion.",
                    parameterName);
            }
        }
    }
}
