using System;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeSelfDestruct",
        menuName = "Bomb Swap/Prototype/Self-Destruct Enemy")]
    public sealed class PrototypeSelfDestructDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-self-destruct";

        [SerializeField]
        [FormerlySerializedAs("approachCellsPerSecond")]
        private float chaseCellsPerSecond = 2f;

        [SerializeField]
        private int warningDistance = 3;

        [SerializeField]
        private int primeDistance = 1;

        [SerializeField]
        private PrototypeBombDefinitionAsset detonationBombDefinition;

        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        private GameObject telegraphCellPrefab;

        [SerializeField]
        private float visualHeight = 0.45f;

        [SerializeField]
        private float deathVisualSeconds = 0.12f;

        public string DefinitionId => definitionId;

        public float ChaseCellsPerSecond => chaseCellsPerSecond;

        public int WarningDistance => warningDistance;

        public int PrimeDistance => primeDistance;

        public PrototypeBombDefinitionAsset DetonationBombDefinition =>
            detonationBombDefinition;

        public GameObject EnemyPrefab => enemyPrefab;

        public GameObject TelegraphCellPrefab => telegraphCellPrefab;

        public float VisualHeight => visualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

        public void Configure(
            string authoredDefinitionId,
            float authoredChaseCellsPerSecond,
            int authoredWarningDistance,
            int authoredPrimeDistance,
            PrototypeBombDefinitionAsset authoredDetonationBombDefinition,
            GameObject authoredEnemyPrefab,
            GameObject authoredTelegraphCellPrefab,
            float authoredVisualHeight,
            float authoredDeathVisualSeconds)
        {
            SelfDestructEnemyDefinition definition = CreateCoreDefinition(
                authoredDefinitionId,
                authoredChaseCellsPerSecond,
                authoredWarningDistance,
                authoredPrimeDistance,
                authoredDetonationBombDefinition);
            if (authoredEnemyPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredEnemyPrefab));
            }
            if (authoredTelegraphCellPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredTelegraphCellPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFinitePositive(
                authoredDeathVisualSeconds,
                nameof(authoredDeathVisualSeconds));

            definitionId = definition.Id.Value;
            chaseCellsPerSecond = authoredChaseCellsPerSecond;
            warningDistance = authoredWarningDistance;
            primeDistance = authoredPrimeDistance;
            detonationBombDefinition = authoredDetonationBombDefinition;
            enemyPrefab = authoredEnemyPrefab;
            telegraphCellPrefab = authoredTelegraphCellPrefab;
            visualHeight = authoredVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public SelfDestructEnemyDefinition CreateCoreDefinition()
        {
            return CreateCoreDefinition(
                definitionId,
                chaseCellsPerSecond,
                warningDistance,
                primeDistance,
                detonationBombDefinition);
        }

        public void ValidatePresentationReferences()
        {
            if (enemyPrefab == null)
            {
                throw new InvalidOperationException("Self-destruct enemy prefab is required.");
            }
            if (enemyPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException(
                    "Self-destruct enemy prefab requires a renderer.");
            }
            if (enemyPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Self-destruct enemy prefab must not own logical colliders.");
            }
            if (telegraphCellPrefab == null)
            {
                throw new InvalidOperationException(
                    "Self-destruct telegraph-cell prefab is required.");
            }
            if (telegraphCellPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException(
                    "Self-destruct telegraph-cell prefab requires a renderer.");
            }
            if (telegraphCellPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Self-destruct telegraph-cell prefab must not own logical colliders.");
            }

            if (detonationBombDefinition == null)
            {
                throw new InvalidOperationException(
                    "Self-destruct detonation bomb definition is required.");
            }

            detonationBombDefinition.ValidatePresentationReferences();
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFinitePositive(deathVisualSeconds, nameof(deathVisualSeconds));
        }

        private static SelfDestructEnemyDefinition CreateCoreDefinition(
            string id,
            float speed,
            int authoredWarningDistance,
            int authoredPrimeDistance,
            PrototypeBombDefinitionAsset bombDefinition)
        {
            ValidateFinitePositive(speed, nameof(speed));
            if (bombDefinition == null)
            {
                throw new ArgumentNullException(nameof(bombDefinition));
            }

            return new SelfDestructEnemyDefinition(
                new EnemyDefinitionId(id),
                TimeSpan.FromSeconds(1f / speed),
                authoredWarningDistance,
                authoredPrimeDistance,
                bombDefinition.CreateCoreDefinition());
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
