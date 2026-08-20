using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeCharger",
        menuName = "Bomb Swap/Prototype/Charger Enemy")]
    public sealed class PrototypeChargerDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-charger";

        [SerializeField]
        private int maxHealth = 1;

        [SerializeField]
        private int contactDamage = 1;

        [SerializeField]
        private float laneAcquireCellsPerSecond = 1f;

        [SerializeField]
        private float telegraphSeconds = 0.75f;

        [SerializeField]
        private float chargeCellsPerSecond = 8f;

        [SerializeField]
        private float recoverSeconds = 0.75f;

        [SerializeField]
        private GameObject chargerPrefab;

        [SerializeField]
        private GameObject telegraphCellPrefab;

        [SerializeField]
        private float visualHeight = 0.45f;

        [SerializeField]
        private float deathVisualSeconds = 0.12f;

        public string DefinitionId => definitionId;

        public int MaxHealth => maxHealth;

        public int ContactDamage => contactDamage;

        public float LaneAcquireCellsPerSecond => laneAcquireCellsPerSecond;

        public float TelegraphSeconds => telegraphSeconds;

        public float ChargeCellsPerSecond => chargeCellsPerSecond;

        public float RecoverSeconds => recoverSeconds;

        public GameObject ChargerPrefab => chargerPrefab;

        public GameObject TelegraphCellPrefab => telegraphCellPrefab;

        public float VisualHeight => visualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

        public void Configure(
            string authoredDefinitionId,
            int authoredMaxHealth,
            int authoredContactDamage,
            float authoredLaneAcquireCellsPerSecond,
            float authoredTelegraphSeconds,
            float authoredChargeCellsPerSecond,
            float authoredRecoverSeconds,
            GameObject authoredChargerPrefab,
            GameObject authoredTelegraphCellPrefab,
            float authoredVisualHeight,
            float authoredDeathVisualSeconds)
        {
            var id = new EnemyDefinitionId(authoredDefinitionId);
            ValidatePositive(authoredMaxHealth, nameof(authoredMaxHealth));
            ValidatePositive(authoredContactDamage, nameof(authoredContactDamage));
            ValidateFinitePositive(
                authoredLaneAcquireCellsPerSecond,
                nameof(authoredLaneAcquireCellsPerSecond));
            ValidateFinitePositive(authoredTelegraphSeconds, nameof(authoredTelegraphSeconds));
            ValidateFinitePositive(
                authoredChargeCellsPerSecond,
                nameof(authoredChargeCellsPerSecond));
            ValidateFinitePositive(authoredRecoverSeconds, nameof(authoredRecoverSeconds));
            if (authoredChargerPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredChargerPrefab));
            }
            if (authoredTelegraphCellPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredTelegraphCellPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFinitePositive(
                authoredDeathVisualSeconds,
                nameof(authoredDeathVisualSeconds));

            definitionId = id.Value;
            maxHealth = authoredMaxHealth;
            contactDamage = authoredContactDamage;
            laneAcquireCellsPerSecond = authoredLaneAcquireCellsPerSecond;
            telegraphSeconds = authoredTelegraphSeconds;
            chargeCellsPerSecond = authoredChargeCellsPerSecond;
            recoverSeconds = authoredRecoverSeconds;
            chargerPrefab = authoredChargerPrefab;
            telegraphCellPrefab = authoredTelegraphCellPrefab;
            visualHeight = authoredVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public ChargerEnemyDefinition CreateCoreDefinition()
        {
            var id = new EnemyDefinitionId(definitionId);
            ValidatePositive(maxHealth, nameof(maxHealth));
            ValidatePositive(contactDamage, nameof(contactDamage));
            ValidateFinitePositive(
                laneAcquireCellsPerSecond,
                nameof(laneAcquireCellsPerSecond));
            ValidateFinitePositive(telegraphSeconds, nameof(telegraphSeconds));
            ValidateFinitePositive(chargeCellsPerSecond, nameof(chargeCellsPerSecond));
            ValidateFinitePositive(recoverSeconds, nameof(recoverSeconds));

            return new ChargerEnemyDefinition(
                id,
                maxHealth,
                contactDamage,
                TimeSpan.FromSeconds(1f / laneAcquireCellsPerSecond),
                TimeSpan.FromSeconds(telegraphSeconds),
                TimeSpan.FromSeconds(1f / chargeCellsPerSecond),
                TimeSpan.FromSeconds(recoverSeconds));
        }

        public void ValidatePresentationReferences()
        {
            if (chargerPrefab == null)
            {
                throw new InvalidOperationException("Charger prefab is required.");
            }
            if (telegraphCellPrefab == null)
            {
                throw new InvalidOperationException(
                    "Charger telegraph-cell prefab is required.");
            }
            if (chargerPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException("Charger prefab requires a renderer.");
            }
            if (chargerPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Charger prefab must not own logical colliders.");
            }
            if (telegraphCellPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException(
                    "Charger telegraph-cell prefab requires a renderer.");
            }
            if (telegraphCellPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Charger telegraph-cell prefab must not own logical colliders.");
            }
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFinitePositive(deathVisualSeconds, nameof(deathVisualSeconds));
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
