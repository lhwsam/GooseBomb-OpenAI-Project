using System;

namespace BombSwap.Core
{
    public sealed class BossPatternTuning
    {
        public BossPatternTuning(
            BossPatternTimings chaseTimings,
            BossPatternTimings lastStandChaseTimings,
            BossPatternTimings chargeTimings,
            BossPatternTimings returnToCenterTimings,
            BossPatternTimings transitionTimings,
            BossPatternTimings summonTimings,
            BossPatternTimings bombVolleyTimings,
            BossPatternTimings parityWaveTimings,
            TimeSpan phaseOneOverheatDuration,
            TimeSpan phaseTwoOverheatDuration,
            TimeSpan lastStandOverheatDuration,
            int phaseOneChaseCount,
            int phaseTwoChaseCount,
            int lastStandChaseCount,
            int chargeDistance,
            TimeSpan bombFlightDuration,
            TimeSpan bombThrowInterval,
            TimeSpan selfDestructForceDelay)
        {
            ValidateTimings(chaseTimings, nameof(chaseTimings));
            ValidateTimings(lastStandChaseTimings, nameof(lastStandChaseTimings));
            ValidateTimings(chargeTimings, nameof(chargeTimings));
            ValidateTimings(returnToCenterTimings, nameof(returnToCenterTimings));
            ValidateTimings(transitionTimings, nameof(transitionTimings));
            ValidateTimings(summonTimings, nameof(summonTimings));
            ValidateTimings(bombVolleyTimings, nameof(bombVolleyTimings));
            ValidateTimings(parityWaveTimings, nameof(parityWaveTimings));
            ValidatePositive(phaseOneOverheatDuration, nameof(phaseOneOverheatDuration));
            ValidatePositive(phaseTwoOverheatDuration, nameof(phaseTwoOverheatDuration));
            ValidatePositive(lastStandOverheatDuration, nameof(lastStandOverheatDuration));
            ValidatePositive(phaseOneChaseCount, nameof(phaseOneChaseCount));
            ValidatePositive(phaseTwoChaseCount, nameof(phaseTwoChaseCount));
            ValidatePositive(lastStandChaseCount, nameof(lastStandChaseCount));
            if (chargeDistance < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chargeDistance),
                    chargeDistance,
                    "Boss charge distance must be at least two cells.");
            }
            ValidatePositive(bombFlightDuration, nameof(bombFlightDuration));
            ValidatePositive(bombThrowInterval, nameof(bombThrowInterval));
            ValidatePositive(selfDestructForceDelay, nameof(selfDestructForceDelay));

            ChaseTimings = chaseTimings;
            LastStandChaseTimings = lastStandChaseTimings;
            ChargeTimings = chargeTimings;
            ReturnToCenterTimings = returnToCenterTimings;
            TransitionTimings = transitionTimings;
            SummonTimings = summonTimings;
            BombVolleyTimings = bombVolleyTimings;
            ParityWaveTimings = parityWaveTimings;
            PhaseOneOverheatDuration = phaseOneOverheatDuration;
            PhaseTwoOverheatDuration = phaseTwoOverheatDuration;
            LastStandOverheatDuration = lastStandOverheatDuration;
            PhaseOneChaseCount = phaseOneChaseCount;
            PhaseTwoChaseCount = phaseTwoChaseCount;
            LastStandChaseCount = lastStandChaseCount;
            ChargeDistance = chargeDistance;
            BombFlightDuration = bombFlightDuration;
            BombThrowInterval = bombThrowInterval;
            SelfDestructForceDelay = selfDestructForceDelay;
        }

        public BossPatternTimings ChaseTimings { get; }

        public BossPatternTimings LastStandChaseTimings { get; }

        public BossPatternTimings ChargeTimings { get; }

        public BossPatternTimings ReturnToCenterTimings { get; }

        public BossPatternTimings TransitionTimings { get; }

        public BossPatternTimings SummonTimings { get; }

        public BossPatternTimings BombVolleyTimings { get; }

        public BossPatternTimings ParityWaveTimings { get; }

        public TimeSpan PhaseOneOverheatDuration { get; }

        public TimeSpan PhaseTwoOverheatDuration { get; }

        public TimeSpan LastStandOverheatDuration { get; }

        public int PhaseOneChaseCount { get; }

        public int PhaseTwoChaseCount { get; }

        public int LastStandChaseCount { get; }

        public int ChargeDistance { get; }

        public TimeSpan BombFlightDuration { get; }

        public TimeSpan BombThrowInterval { get; }

        public TimeSpan SelfDestructForceDelay { get; }

        public BossPatternTimings GetTimings(BossPhase phase, BossPatternKind pattern)
        {
            switch (pattern)
            {
                case BossPatternKind.LimitedChase:
                    return phase == BossPhase.LastStand
                        ? LastStandChaseTimings
                        : ChaseTimings;
                case BossPatternKind.FixedCharge:
                    return ChargeTimings;
                case BossPatternKind.ReturnToCenter:
                    return ReturnToCenterTimings;
                case BossPatternKind.PhaseTransition:
                case BossPatternKind.WaitForSelfDestruct:
                    return TransitionTimings;
                case BossPatternKind.SummonSelfDestruct:
                    return SummonTimings;
                case BossPatternKind.BombVolley:
                case BossPatternKind.LastStandBombChain:
                    return BombVolleyTimings;
                case BossPatternKind.ParityWave:
                    return ParityWaveTimings;
                case BossPatternKind.Overheat:
                    return new BossPatternTimings(
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromMilliseconds(100),
                        GetOverheatDuration(phase));
                default:
                    throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null);
            }
        }

        private TimeSpan GetOverheatDuration(BossPhase phase)
        {
            switch (phase)
            {
                case BossPhase.One:
                    return PhaseOneOverheatDuration;
                case BossPhase.Two:
                    return PhaseTwoOverheatDuration;
                case BossPhase.LastStand:
                    return LastStandOverheatDuration;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        private static void ValidateTimings(BossPatternTimings timings, string parameterName)
        {
            if (timings.TelegraphDuration <= TimeSpan.Zero ||
                timings.ExecuteDuration <= TimeSpan.Zero ||
                timings.RecoveryDuration <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "Boss pattern timings must contain positive durations.",
                    parameterName);
            }
        }

        private static void ValidatePositive(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Duration must be positive.");
            }
        }

        private static void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be positive.");
            }
        }
    }
}
