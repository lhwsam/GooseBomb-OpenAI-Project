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
        private float telegraphSeconds = 0.75f;

        [SerializeField]
        private float chargeCellsPerSecond = 8f;

        [SerializeField]
        private float recoverSeconds = 0.75f;

        [SerializeField]
        private GameObject chargerPrefab;

        [SerializeField]
        private float visualHeight = 0.45f;

        [SerializeField]
        private float deathVisualSeconds = 0.12f;

        public string DefinitionId => definitionId;

        public int MaxHealth => maxHealth;

        public int ContactDamage => contactDamage;

        public float TelegraphSeconds => telegraphSeconds;

        public float ChargeCellsPerSecond => chargeCellsPerSecond;

        public float RecoverSeconds => recoverSeconds;

        public GameObject ChargerPrefab => chargerPrefab;

        public float VisualHeight => visualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

        public void Configure(
            string authoredDefinitionId,
            int authoredMaxHealth,
            int authoredContactDamage,
            float authoredTelegraphSeconds,
            float authoredChargeCellsPerSecond,
            float authoredRecoverSeconds,
            GameObject authoredChargerPrefab,
            float authoredVisualHeight,
            float authoredDeathVisualSeconds)
        {
            var id = new EnemyDefinitionId(authoredDefinitionId);
            ValidatePositive(authoredMaxHealth, nameof(authoredMaxHealth));
            ValidatePositive(authoredContactDamage, nameof(authoredContactDamage));
            ValidateFinitePositive(authoredTelegraphSeconds, nameof(authoredTelegraphSeconds));
            ValidateFinitePositive(
                authoredChargeCellsPerSecond,
                nameof(authoredChargeCellsPerSecond));
            ValidateFinitePositive(authoredRecoverSeconds, nameof(authoredRecoverSeconds));
            if (authoredChargerPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredChargerPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFinitePositive(
                authoredDeathVisualSeconds,
                nameof(authoredDeathVisualSeconds));

            definitionId = id.Value;
            maxHealth = authoredMaxHealth;
            contactDamage = authoredContactDamage;
            telegraphSeconds = authoredTelegraphSeconds;
            chargeCellsPerSecond = authoredChargeCellsPerSecond;
            recoverSeconds = authoredRecoverSeconds;
            chargerPrefab = authoredChargerPrefab;
            visualHeight = authoredVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public ChargerEnemyDefinition CreateCoreDefinition()
        {
            var id = new EnemyDefinitionId(definitionId);
            ValidatePositive(maxHealth, nameof(maxHealth));
            ValidatePositive(contactDamage, nameof(contactDamage));
            ValidateFinitePositive(telegraphSeconds, nameof(telegraphSeconds));
            ValidateFinitePositive(chargeCellsPerSecond, nameof(chargeCellsPerSecond));
            ValidateFinitePositive(recoverSeconds, nameof(recoverSeconds));

            return new ChargerEnemyDefinition(
                id,
                maxHealth,
                contactDamage,
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
            if (chargerPrefab.GetComponentInChildren<Renderer>(true) == null)
            {
                throw new InvalidOperationException("Charger prefab requires a renderer.");
            }
            if (chargerPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                throw new InvalidOperationException(
                    "Charger prefab must not own logical colliders.");
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
