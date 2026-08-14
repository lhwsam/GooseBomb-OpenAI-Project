using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeChaser",
        menuName = "Bomb Swap/Prototype/Chaser Enemy")]
    public sealed class PrototypeChaserDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-chaser";

        [SerializeField]
        private int maxHealth = 1;

        [SerializeField]
        private int contactDamage = 1;

        [SerializeField]
        private float cellsPerSecond = 2f;

        [SerializeField]
        private int directionCommitmentSteps = 2;

        [SerializeField]
        private GameObject chaserPrefab;

        [SerializeField]
        private float visualHeight = 0.45f;

        [SerializeField]
        private float deathVisualSeconds = 0.12f;

        public string DefinitionId => definitionId;

        public int MaxHealth => maxHealth;

        public int ContactDamage => contactDamage;

        public float CellsPerSecond => cellsPerSecond;

        public int DirectionCommitmentSteps => directionCommitmentSteps;

        public GameObject ChaserPrefab => chaserPrefab;

        public float VisualHeight => visualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

        public void Configure(
            string authoredDefinitionId,
            int authoredMaxHealth,
            int authoredContactDamage,
            float authoredCellsPerSecond,
            int authoredDirectionCommitmentSteps,
            GameObject authoredChaserPrefab,
            float authoredVisualHeight,
            float authoredDeathVisualSeconds)
        {
            var id = new EnemyDefinitionId(authoredDefinitionId);
            if (authoredMaxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredMaxHealth),
                    authoredMaxHealth,
                    "Enemy maximum health must be positive.");
            }
            if (authoredContactDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredContactDamage),
                    authoredContactDamage,
                    "Enemy contact damage must be positive.");
            }
            ValidateFinitePositive(
                authoredCellsPerSecond,
                nameof(authoredCellsPerSecond));
            if (authoredDirectionCommitmentSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredDirectionCommitmentSteps),
                    authoredDirectionCommitmentSteps,
                    "Direction commitment must be positive.");
            }
            if (authoredChaserPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredChaserPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFinitePositive(
                authoredDeathVisualSeconds,
                nameof(authoredDeathVisualSeconds));

            definitionId = id.Value;
            maxHealth = authoredMaxHealth;
            contactDamage = authoredContactDamage;
            cellsPerSecond = authoredCellsPerSecond;
            directionCommitmentSteps = authoredDirectionCommitmentSteps;
            chaserPrefab = authoredChaserPrefab;
            visualHeight = authoredVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public ChaserEnemyDefinition CreateCoreDefinition()
        {
            var id = new EnemyDefinitionId(definitionId);
            if (maxHealth <= 0)
            {
                throw new InvalidOperationException(
                    "Authored enemy maximum health must be positive.");
            }
            if (contactDamage <= 0)
            {
                throw new InvalidOperationException(
                    "Authored enemy contact damage must be positive.");
            }
            ValidateFinitePositive(cellsPerSecond, nameof(cellsPerSecond));
            if (directionCommitmentSteps <= 0)
            {
                throw new InvalidOperationException(
                    "Authored direction commitment must be positive.");
            }

            return new ChaserEnemyDefinition(
                id,
                maxHealth,
                contactDamage,
                TimeSpan.FromSeconds(1f / cellsPerSecond),
                directionCommitmentSteps);
        }

        public void ValidatePresentationReferences()
        {
            if (chaserPrefab == null)
            {
                throw new InvalidOperationException("Chaser prefab is required.");
            }
            if (chaserPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException("Chaser prefab requires a renderer.");
            }
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFinitePositive(deathVisualSeconds, nameof(deathVisualSeconds));
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
