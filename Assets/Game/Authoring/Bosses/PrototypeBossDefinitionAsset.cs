using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(fileName = "PrototypeBoss", menuName = "Bomb Swap/Prototype/Boss")]
    public sealed class PrototypeBossDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string definitionId = "prototype-boss";
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int phaseTwoHealthThreshold = 7;
        [SerializeField] private int lastStandHealthThreshold = 2;
        [SerializeField] private int patternDamage = 1;
        [SerializeField, Tooltip(
            "Legacy serialized value retained for existing assets. " +
            "Player bombs are no longer capped per overheat.")]
        private int maxOverheatDamage = 2;

        [Header("Chase and charge")]
        [SerializeField] private float chaseTelegraphSeconds = 0.12f;
        [SerializeField] private float chaseExecuteSeconds = 0.22f;
        [SerializeField] private float chaseRecoverySeconds = 0.08f;
        [SerializeField] private float lastStandChaseTelegraphSeconds = 0.08f;
        [SerializeField] private float lastStandChaseExecuteSeconds = 0.18f;
        [SerializeField] private float lastStandChaseRecoverySeconds = 0.04f;
        [SerializeField] private float chargeTelegraphSeconds = 0.7f;
        [SerializeField] private float chargeExecuteSeconds = 0.3f;
        [SerializeField] private float chargeRecoverySeconds = 0.5f;
        [SerializeField] private int phaseOneChaseCount = 2;
        [SerializeField] private int phaseTwoChaseCount = 3;
        [SerializeField] private int lastStandChaseCount = 2;
        [SerializeField] private int chargeDistance = 3;

        [Header("Sequence timings")]
        [SerializeField] private float returnTelegraphSeconds = 0.2f;
        [SerializeField] private float returnExecuteSeconds = 0.7f;
        [SerializeField] private float returnRecoverySeconds = 0.1f;
        [SerializeField] private float transitionTelegraphSeconds = 1.1f;
        [SerializeField] private float transitionExecuteSeconds = 0.1f;
        [SerializeField] private float transitionRecoverySeconds = 0.1f;
        [SerializeField] private float summonTelegraphSeconds = 0.8f;
        [SerializeField] private float summonExecuteSeconds = 0.2f;
        [SerializeField] private float summonRecoverySeconds = 0.2f;
        [SerializeField] private float volleyTelegraphSeconds = 0.35f;
        [SerializeField] private float volleyExecuteSeconds = 1.8f;
        [SerializeField] private float volleyRecoverySeconds = 0.1f;
        [SerializeField] private float parityTelegraphSeconds = 0.12f;
        [SerializeField] private float parityExecuteSeconds = 0.08f;
        [SerializeField] private float parityRecoverySeconds = 0.08f;
        [SerializeField] private float phaseOneOverheatSeconds = 2f;
        [SerializeField] private float phaseTwoOverheatSeconds = 1.5f;
        [SerializeField] private float lastStandOverheatSeconds = 2.25f;

        [Header("Thrown bombs and summon")]
        [SerializeField] private float bombFlightSeconds = 0.45f;
        [SerializeField] private float bombThrowIntervalSeconds = 0.4f;
        [SerializeField] private float selfDestructForceSeconds = 4.5f;

        [Header("Presentation")]
        [SerializeField] private Vector2Int bossSpawn = new Vector2Int(0, 1);
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private GameObject dangerCellPrefab;
        [SerializeField] private PrototypeBombDefinitionAsset throwBombDefinition;
        [SerializeField] private PrototypeBombDefinitionAsset chainBombDefinition;
        [SerializeField] private float visualHeight = 0.6f;
        [SerializeField] private float dangerCellVisualHeight = 0.03f;
        [SerializeField] private float deathVisualSeconds = 0.2f;

        public string DefinitionId => definitionId;
        public int MaxHealth => maxHealth;
        public int PhaseTwoHealthThreshold => phaseTwoHealthThreshold;
        public int LastStandHealthThreshold => lastStandHealthThreshold;
        public int PatternDamage => patternDamage;
        public GridPosition BossSpawn => new GridPosition(bossSpawn.x, bossSpawn.y);
        public GameObject BossPrefab => bossPrefab;
        public GameObject DangerCellPrefab => dangerCellPrefab;
        public PrototypeBombDefinitionAsset ThrowBombDefinition => throwBombDefinition;
        public PrototypeBombDefinitionAsset ChainBombDefinition => chainBombDefinition;
        public float VisualHeight => visualHeight;
        public float DangerCellVisualHeight => dangerCellVisualHeight;
        public float DeathVisualSeconds => deathVisualSeconds;
        public float BombFlightSeconds => bombFlightSeconds;
        public float SelfDestructForceSeconds => selfDestructForceSeconds;

        public void Configure(
            string authoredDefinitionId,
            int authoredMaxHealth,
            int authoredPhaseTwoHealthThreshold,
            int authoredPatternDamage,
            float authoredPhaseOneTelegraphSeconds,
            float authoredPhaseOneExecuteSeconds,
            float authoredPhaseOneRecoverySeconds,
            float authoredPhaseTwoTelegraphSeconds,
            float authoredPhaseTwoExecuteSeconds,
            float authoredPhaseTwoRecoverySeconds,
            Vector2Int authoredBossSpawn,
            GameObject authoredBossPrefab,
            GameObject authoredDangerCellPrefab,
            PrototypeBombDefinitionAsset authoredThrowBombDefinition,
            PrototypeBombDefinitionAsset authoredChainBombDefinition,
            float authoredVisualHeight,
            float authoredDangerCellVisualHeight,
            float authoredDeathVisualSeconds)
        {
            chaseTelegraphSeconds = authoredPhaseOneTelegraphSeconds;
            chaseExecuteSeconds = authoredPhaseOneExecuteSeconds;
            chaseRecoverySeconds = Mathf.Max(0.01f, authoredPhaseOneExecuteSeconds);
            lastStandChaseTelegraphSeconds = Mathf.Max(
                0.01f,
                authoredPhaseTwoTelegraphSeconds * 0.75f);
            lastStandChaseExecuteSeconds = authoredPhaseTwoExecuteSeconds;
            lastStandChaseRecoverySeconds = Mathf.Max(
                0.01f,
                authoredPhaseTwoExecuteSeconds * 0.5f);
            chargeTelegraphSeconds = authoredPhaseOneTelegraphSeconds;
            chargeExecuteSeconds = authoredPhaseOneExecuteSeconds;
            chargeRecoverySeconds = Mathf.Max(0.01f, authoredPhaseOneExecuteSeconds);
            returnTelegraphSeconds = Mathf.Max(0.01f, authoredPhaseOneExecuteSeconds);
            returnExecuteSeconds = authoredPhaseOneExecuteSeconds;
            returnRecoverySeconds = Mathf.Max(0.01f, authoredPhaseOneExecuteSeconds);
            transitionTelegraphSeconds = authoredPhaseTwoTelegraphSeconds;
            transitionExecuteSeconds = authoredPhaseTwoExecuteSeconds;
            transitionRecoverySeconds = Mathf.Max(0.01f, authoredPhaseTwoExecuteSeconds);
            summonTelegraphSeconds = authoredPhaseTwoTelegraphSeconds;
            summonExecuteSeconds = authoredPhaseTwoExecuteSeconds;
            summonRecoverySeconds = Mathf.Max(0.01f, authoredPhaseTwoExecuteSeconds);
            volleyTelegraphSeconds = authoredPhaseOneTelegraphSeconds;
            volleyExecuteSeconds = Mathf.Max(
                authoredPhaseOneExecuteSeconds,
                bombFlightSeconds + (bombThrowIntervalSeconds * 3f));
            volleyRecoverySeconds = Mathf.Max(0.01f, authoredPhaseOneExecuteSeconds);
            parityTelegraphSeconds = Mathf.Max(0.01f, authoredPhaseTwoTelegraphSeconds * 0.2f);
            parityExecuteSeconds = Mathf.Max(0.01f, authoredPhaseTwoExecuteSeconds);
            parityRecoverySeconds = Mathf.Max(0.01f, authoredPhaseTwoExecuteSeconds);
            phaseOneOverheatSeconds = authoredPhaseOneRecoverySeconds;
            phaseTwoOverheatSeconds = authoredPhaseTwoRecoverySeconds;
            lastStandOverheatSeconds = authoredPhaseTwoRecoverySeconds;
            Configure(
                authoredDefinitionId,
                authoredMaxHealth,
                authoredPhaseTwoHealthThreshold,
                Math.Max(1, authoredPhaseTwoHealthThreshold - 1),
                authoredPatternDamage,
                Math.Min(2, authoredMaxHealth),
                authoredBossSpawn,
                authoredBossPrefab,
                authoredDangerCellPrefab,
                authoredThrowBombDefinition,
                authoredChainBombDefinition,
                authoredVisualHeight,
                authoredDangerCellVisualHeight,
                authoredDeathVisualSeconds);
        }

        public void Configure(
            string authoredDefinitionId,
            int authoredMaxHealth,
            int authoredPhaseTwoHealthThreshold,
            int authoredLastStandHealthThreshold,
            int authoredPatternDamage,
            int authoredMaxOverheatDamage,
            Vector2Int authoredBossSpawn,
            GameObject authoredBossPrefab,
            GameObject authoredDangerCellPrefab,
            PrototypeBombDefinitionAsset authoredThrowBombDefinition,
            PrototypeBombDefinitionAsset authoredChainBombDefinition,
            float authoredVisualHeight,
            float authoredDangerCellVisualHeight,
            float authoredDeathVisualSeconds)
        {
            definitionId = authoredDefinitionId;
            maxHealth = authoredMaxHealth;
            phaseTwoHealthThreshold = authoredPhaseTwoHealthThreshold;
            lastStandHealthThreshold = authoredLastStandHealthThreshold;
            patternDamage = authoredPatternDamage;
            maxOverheatDamage = authoredMaxOverheatDamage;
            bossSpawn = authoredBossSpawn;
            bossPrefab = authoredBossPrefab ?? throw new ArgumentNullException(nameof(authoredBossPrefab));
            dangerCellPrefab = authoredDangerCellPrefab ??
                throw new ArgumentNullException(nameof(authoredDangerCellPrefab));
            throwBombDefinition = authoredThrowBombDefinition;
            chainBombDefinition = authoredChainBombDefinition;
            visualHeight = authoredVisualHeight;
            dangerCellVisualHeight = authoredDangerCellVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
            CreateCoreDefinition();
            ValidatePresentationReferences();
        }

        public BossBattleDefinition CreateCoreDefinition()
        {
            if (throwBombDefinition == null || chainBombDefinition == null)
            {
                throw new InvalidOperationException(
                    "Boss definition requires throw and chain bomb definitions.");
            }

            return new BossBattleDefinition(
                new EnemyDefinitionId(definitionId),
                maxHealth,
                phaseTwoHealthThreshold,
                lastStandHealthThreshold,
                patternDamage,
                new BossPatternTuning(
                    CreateTimings(
                        chaseTelegraphSeconds,
                        chaseExecuteSeconds,
                        chaseRecoverySeconds),
                    CreateTimings(
                        lastStandChaseTelegraphSeconds,
                        lastStandChaseExecuteSeconds,
                        lastStandChaseRecoverySeconds),
                    CreateTimings(
                        chargeTelegraphSeconds,
                        chargeExecuteSeconds,
                        chargeRecoverySeconds),
                    CreateTimings(
                        returnTelegraphSeconds,
                        returnExecuteSeconds,
                        returnRecoverySeconds),
                    CreateTimings(
                        transitionTelegraphSeconds,
                        transitionExecuteSeconds,
                        transitionRecoverySeconds),
                    CreateTimings(
                        summonTelegraphSeconds,
                        summonExecuteSeconds,
                        summonRecoverySeconds),
                    CreateTimings(
                        volleyTelegraphSeconds,
                        volleyExecuteSeconds,
                        volleyRecoverySeconds),
                    CreateTimings(
                        parityTelegraphSeconds,
                        parityExecuteSeconds,
                        parityRecoverySeconds),
                    ToDuration(phaseOneOverheatSeconds, nameof(phaseOneOverheatSeconds)),
                    ToDuration(phaseTwoOverheatSeconds, nameof(phaseTwoOverheatSeconds)),
                    ToDuration(lastStandOverheatSeconds, nameof(lastStandOverheatSeconds)),
                    phaseOneChaseCount,
                    phaseTwoChaseCount,
                    lastStandChaseCount,
                    chargeDistance,
                    ToDuration(bombFlightSeconds, nameof(bombFlightSeconds)),
                    ToDuration(
                        bombThrowIntervalSeconds,
                        nameof(bombThrowIntervalSeconds)),
                    ToDuration(selfDestructForceSeconds, nameof(selfDestructForceSeconds))),
                throwBombDefinition.CreateCoreDefinition(),
                chainBombDefinition.CreateCoreDefinition());
        }

        public float GetPatternExecuteSeconds(BossPhase phase, BossPatternKind pattern)
        {
            switch (pattern)
            {
                case BossPatternKind.LimitedChase:
                    return phase == BossPhase.LastStand
                        ? lastStandChaseExecuteSeconds
                        : chaseExecuteSeconds;
                case BossPatternKind.FixedCharge:
                    return chargeExecuteSeconds;
                case BossPatternKind.ReturnToCenter:
                    return returnExecuteSeconds;
                case BossPatternKind.PhaseTransition:
                    return transitionExecuteSeconds;
                case BossPatternKind.SummonSelfDestruct:
                    return summonExecuteSeconds;
                case BossPatternKind.BombVolley:
                case BossPatternKind.LastStandBombChain:
                    return volleyExecuteSeconds;
                case BossPatternKind.ParityWave:
                    return parityExecuteSeconds;
                case BossPatternKind.Overheat:
                    return phase == BossPhase.One
                        ? phaseOneOverheatSeconds
                        : phase == BossPhase.Two
                            ? phaseTwoOverheatSeconds
                            : lastStandOverheatSeconds;
                case BossPatternKind.WaitForSelfDestruct:
                    return summonRecoverySeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null);
            }
        }

        public void ValidatePresentationReferences()
        {
            ValidateVisualPrefab(bossPrefab, "Boss");
            ValidateVisualPrefab(dangerCellPrefab, "Boss danger-cell");
            throwBombDefinition.ValidatePresentationReferences();
            chainBombDefinition.ValidatePresentationReferences();
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFiniteNonNegative(dangerCellVisualHeight, nameof(dangerCellVisualHeight));
            ToDuration(deathVisualSeconds, nameof(deathVisualSeconds));
        }

        private static BossPatternTimings CreateTimings(
            float telegraphSeconds,
            float executeSeconds,
            float recoverySeconds)
        {
            return new BossPatternTimings(
                ToDuration(telegraphSeconds, nameof(telegraphSeconds)),
                ToDuration(executeSeconds, nameof(executeSeconds)),
                ToDuration(recoverySeconds, nameof(recoverySeconds)));
        }

        private static TimeSpan ToDuration(float value, string parameterName)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and positive.");
            }
            return TimeSpan.FromSeconds(value);
        }

        private static void ValidateVisualPrefab(GameObject prefab, string label)
        {
            if (prefab == null || prefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException($"{label} prefab requires a renderer.");
            }
            if (prefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    $"{label} prefab must not own logical colliders.");
            }
        }

        private static void ValidateFiniteNonNegative(float value, string parameterName)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, null);
            }
        }
    }
}
