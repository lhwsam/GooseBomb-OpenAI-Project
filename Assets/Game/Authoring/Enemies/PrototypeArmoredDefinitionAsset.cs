using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeArmored",
        menuName = "Bomb Swap/Prototype/Armored Enemy")]
    public sealed class PrototypeArmoredDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-armored";

        [SerializeField]
        private int contactDamage = 1;

        [SerializeField]
        private float armoredCellsPerSecond = 1f;

        [SerializeField]
        private float brokenCellsPerSecond = 3f;

        [SerializeField]
        private int directionCommitmentSteps = 2;

        [SerializeField]
        private int guardRadius = 1;

        [SerializeField]
        private float panicTelegraphSeconds = 0.6f;

        [SerializeField]
        private float panicCellsPerSecond = 6f;

        [SerializeField]
        private int panicRunDistance = 3;

        [SerializeField]
        private float panicRecoverSeconds = 0.5f;

        [SerializeField]
        private GameObject armoredPrefab;

        [SerializeField]
        private GameObject panicTelegraphCellPrefab;

        [SerializeField]
        private float visualHeight = 0.5f;

        [SerializeField]
        private float deathVisualSeconds = 0.12f;

        public string DefinitionId => definitionId;

        public int MaxHealth => ArmoredEnemyDefinition.StageCount;

        public int ContactDamage => contactDamage;

        public float ArmoredCellsPerSecond => armoredCellsPerSecond;

        public float BrokenCellsPerSecond => brokenCellsPerSecond;

        public int DirectionCommitmentSteps => directionCommitmentSteps;

        public int GuardRadius => guardRadius;

        public float PanicTelegraphSeconds => panicTelegraphSeconds;

        public float PanicCellsPerSecond => panicCellsPerSecond;

        public int PanicRunDistance => panicRunDistance;

        public float PanicRecoverSeconds => panicRecoverSeconds;

        public GameObject ArmoredPrefab => armoredPrefab;

        public GameObject PanicTelegraphCellPrefab => panicTelegraphCellPrefab;

        public float VisualHeight => visualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

        public void Configure(
            string authoredDefinitionId,
            int authoredContactDamage,
            float authoredArmoredCellsPerSecond,
            float authoredBrokenCellsPerSecond,
            int authoredDirectionCommitmentSteps,
            int authoredGuardRadius,
            float authoredPanicTelegraphSeconds,
            float authoredPanicCellsPerSecond,
            int authoredPanicRunDistance,
            float authoredPanicRecoverSeconds,
            GameObject authoredArmoredPrefab,
            GameObject authoredPanicTelegraphCellPrefab,
            float authoredVisualHeight,
            float authoredDeathVisualSeconds)
        {
            var definition = CreateCoreDefinition(
                authoredDefinitionId,
                authoredContactDamage,
                authoredArmoredCellsPerSecond,
                authoredBrokenCellsPerSecond,
                authoredDirectionCommitmentSteps,
                authoredGuardRadius,
                authoredPanicTelegraphSeconds,
                authoredPanicCellsPerSecond,
                authoredPanicRunDistance,
                authoredPanicRecoverSeconds);
            if (authoredArmoredPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredArmoredPrefab));
            }
            if (authoredPanicTelegraphCellPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredPanicTelegraphCellPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFinitePositive(authoredDeathVisualSeconds, nameof(authoredDeathVisualSeconds));

            definitionId = definition.Id.Value;
            contactDamage = authoredContactDamage;
            armoredCellsPerSecond = authoredArmoredCellsPerSecond;
            brokenCellsPerSecond = authoredBrokenCellsPerSecond;
            directionCommitmentSteps = authoredDirectionCommitmentSteps;
            guardRadius = authoredGuardRadius;
            panicTelegraphSeconds = authoredPanicTelegraphSeconds;
            panicCellsPerSecond = authoredPanicCellsPerSecond;
            panicRunDistance = authoredPanicRunDistance;
            panicRecoverSeconds = authoredPanicRecoverSeconds;
            armoredPrefab = authoredArmoredPrefab;
            panicTelegraphCellPrefab = authoredPanicTelegraphCellPrefab;
            visualHeight = authoredVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public ArmoredEnemyDefinition CreateCoreDefinition()
        {
            return CreateCoreDefinition(
                definitionId,
                contactDamage,
                armoredCellsPerSecond,
                brokenCellsPerSecond,
                directionCommitmentSteps,
                guardRadius,
                panicTelegraphSeconds,
                panicCellsPerSecond,
                panicRunDistance,
                panicRecoverSeconds);
        }

        public void ValidatePresentationReferences()
        {
            if (armoredPrefab == null)
            {
                throw new InvalidOperationException("Armored enemy prefab is required.");
            }
            if (armoredPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException("Armored enemy prefab requires a renderer.");
            }
            if (armoredPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Armored enemy prefab must not own logical colliders.");
            }
            if (panicTelegraphCellPrefab == null)
            {
                throw new InvalidOperationException("Armored panic telegraph-cell prefab is required.");
            }
            if (panicTelegraphCellPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException(
                    "Armored panic telegraph-cell prefab requires a renderer.");
            }
            if (panicTelegraphCellPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Armored panic telegraph-cell prefab must not own logical colliders.");
            }
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFinitePositive(deathVisualSeconds, nameof(deathVisualSeconds));
        }

        private static ArmoredEnemyDefinition CreateCoreDefinition(
            string id,
            int damage,
            float armoredSpeed,
            float brokenSpeed,
            int commitment,
            int authoredGuardRadius,
            float authoredPanicTelegraphSeconds,
            float authoredPanicSpeed,
            int authoredPanicRunDistance,
            float authoredPanicRecoverSeconds)
        {
            ValidateFinitePositive(armoredSpeed, nameof(armoredSpeed));
            ValidateFinitePositive(brokenSpeed, nameof(brokenSpeed));
            ValidateFinitePositive(
                authoredPanicTelegraphSeconds,
                nameof(authoredPanicTelegraphSeconds));
            ValidateFinitePositive(authoredPanicSpeed, nameof(authoredPanicSpeed));
            ValidateFinitePositive(
                authoredPanicRecoverSeconds,
                nameof(authoredPanicRecoverSeconds));
            return new ArmoredEnemyDefinition(
                new EnemyDefinitionId(id),
                damage,
                TimeSpan.FromSeconds(1f / armoredSpeed),
                TimeSpan.FromSeconds(1f / brokenSpeed),
                commitment,
                authoredGuardRadius,
                TimeSpan.FromSeconds(authoredPanicTelegraphSeconds),
                TimeSpan.FromSeconds(1f / authoredPanicSpeed),
                authoredPanicRunDistance,
                TimeSpan.FromSeconds(authoredPanicRecoverSeconds));
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
