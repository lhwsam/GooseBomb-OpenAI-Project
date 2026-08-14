using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeBoss",
        menuName = "Bomb Swap/Prototype/Boss")]
    public sealed class PrototypeBossDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-boss";

        [SerializeField]
        private int maxHealth = 4;

        [SerializeField]
        private int phaseTwoHealthThreshold = 2;

        [SerializeField]
        private int patternDamage = 1;

        [SerializeField]
        private float phaseOneTelegraphSeconds = 1f;

        [SerializeField]
        private float phaseOneExecuteSeconds = 0.25f;

        [SerializeField]
        private float phaseOneRecoverySeconds = 2.75f;

        [SerializeField]
        private float phaseTwoTelegraphSeconds = 0.75f;

        [SerializeField]
        private float phaseTwoExecuteSeconds = 0.25f;

        [SerializeField]
        private float phaseTwoRecoverySeconds = 2.75f;

        [SerializeField]
        private Vector2Int bossSpawn = new Vector2Int(0, 1);

        [SerializeField]
        private GameObject bossPrefab;

        [SerializeField]
        private GameObject dangerCellPrefab;

        [SerializeField]
        private float visualHeight = 0.6f;

        [SerializeField]
        private float dangerCellVisualHeight = 0.03f;

        [SerializeField]
        private float deathVisualSeconds = 0.2f;

        public string DefinitionId => definitionId;

        public int MaxHealth => maxHealth;

        public int PhaseTwoHealthThreshold => phaseTwoHealthThreshold;

        public int PatternDamage => patternDamage;

        public float PhaseOneTelegraphSeconds => phaseOneTelegraphSeconds;

        public float PhaseOneExecuteSeconds => phaseOneExecuteSeconds;

        public float PhaseOneRecoverySeconds => phaseOneRecoverySeconds;

        public float PhaseTwoTelegraphSeconds => phaseTwoTelegraphSeconds;

        public float PhaseTwoExecuteSeconds => phaseTwoExecuteSeconds;

        public float PhaseTwoRecoverySeconds => phaseTwoRecoverySeconds;

        public GridPosition BossSpawn => new GridPosition(bossSpawn.x, bossSpawn.y);

        public GameObject BossPrefab => bossPrefab;

        public GameObject DangerCellPrefab => dangerCellPrefab;

        public float VisualHeight => visualHeight;

        public float DangerCellVisualHeight => dangerCellVisualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

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
            float authoredVisualHeight,
            float authoredDangerCellVisualHeight,
            float authoredDeathVisualSeconds)
        {
            CreateCoreDefinition(
                authoredDefinitionId,
                authoredMaxHealth,
                authoredPhaseTwoHealthThreshold,
                authoredPatternDamage,
                authoredPhaseOneTelegraphSeconds,
                authoredPhaseOneExecuteSeconds,
                authoredPhaseOneRecoverySeconds,
                authoredPhaseTwoTelegraphSeconds,
                authoredPhaseTwoExecuteSeconds,
                authoredPhaseTwoRecoverySeconds);
            if (authoredBossPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredBossPrefab));
            }
            if (authoredDangerCellPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredDangerCellPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFiniteNonNegative(
                authoredDangerCellVisualHeight,
                nameof(authoredDangerCellVisualHeight));
            ValidateFinitePositive(
                authoredDeathVisualSeconds,
                nameof(authoredDeathVisualSeconds));

            definitionId = authoredDefinitionId;
            maxHealth = authoredMaxHealth;
            phaseTwoHealthThreshold = authoredPhaseTwoHealthThreshold;
            patternDamage = authoredPatternDamage;
            phaseOneTelegraphSeconds = authoredPhaseOneTelegraphSeconds;
            phaseOneExecuteSeconds = authoredPhaseOneExecuteSeconds;
            phaseOneRecoverySeconds = authoredPhaseOneRecoverySeconds;
            phaseTwoTelegraphSeconds = authoredPhaseTwoTelegraphSeconds;
            phaseTwoExecuteSeconds = authoredPhaseTwoExecuteSeconds;
            phaseTwoRecoverySeconds = authoredPhaseTwoRecoverySeconds;
            bossSpawn = authoredBossSpawn;
            bossPrefab = authoredBossPrefab;
            dangerCellPrefab = authoredDangerCellPrefab;
            visualHeight = authoredVisualHeight;
            dangerCellVisualHeight = authoredDangerCellVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public BossBattleDefinition CreateCoreDefinition()
        {
            return CreateCoreDefinition(
                definitionId,
                maxHealth,
                phaseTwoHealthThreshold,
                patternDamage,
                phaseOneTelegraphSeconds,
                phaseOneExecuteSeconds,
                phaseOneRecoverySeconds,
                phaseTwoTelegraphSeconds,
                phaseTwoExecuteSeconds,
                phaseTwoRecoverySeconds);
        }

        public void ValidatePresentationReferences()
        {
            ValidateVisualPrefab(bossPrefab, "Boss");
            ValidateVisualPrefab(dangerCellPrefab, "Boss danger-cell");
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFiniteNonNegative(dangerCellVisualHeight, nameof(dangerCellVisualHeight));
            ValidateFinitePositive(deathVisualSeconds, nameof(deathVisualSeconds));
        }

        private static BossBattleDefinition CreateCoreDefinition(
            string id,
            int authoredMaxHealth,
            int threshold,
            int damage,
            float phaseOneTelegraph,
            float phaseOneExecute,
            float phaseOneRecovery,
            float phaseTwoTelegraph,
            float phaseTwoExecute,
            float phaseTwoRecovery)
        {
            return new BossBattleDefinition(
                new EnemyDefinitionId(id),
                authoredMaxHealth,
                threshold,
                damage,
                CreateTimings(
                    phaseOneTelegraph,
                    phaseOneExecute,
                    phaseOneRecovery),
                CreateTimings(
                    phaseTwoTelegraph,
                    phaseTwoExecute,
                    phaseTwoRecovery));
        }

        private static BossPatternTimings CreateTimings(
            float telegraphSeconds,
            float executeSeconds,
            float recoverySeconds)
        {
            ValidateFinitePositive(telegraphSeconds, nameof(telegraphSeconds));
            ValidateFinitePositive(executeSeconds, nameof(executeSeconds));
            ValidateFinitePositive(recoverySeconds, nameof(recoverySeconds));
            return new BossPatternTimings(
                TimeSpan.FromSeconds(telegraphSeconds),
                TimeSpan.FromSeconds(executeSeconds),
                TimeSpan.FromSeconds(recoverySeconds));
        }

        private static void ValidateVisualPrefab(GameObject prefab, string label)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException($"{label} prefab is required.");
            }
            if (prefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException($"{label} prefab requires a renderer.");
            }
            if (prefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    $"{label} prefab must not own logical colliders.");
            }
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

        private static void ValidateFiniteNonNegative(float value, string parameterName)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and non-negative.");
            }
        }
    }
}
