using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeThrower",
        menuName = "Bomb Swap/Prototype/Thrower Enemy")]
    public sealed class PrototypeThrowerDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-thrower";

        [SerializeField]
        private float moveCellsPerSecond = 1f;

        [SerializeField]
        private float telegraphSeconds = 0.3f;

        [SerializeField]
        private float flightSeconds = 0.45f;

        [SerializeField]
        private float recoverySeconds = 0.75f;

        [SerializeField]
        private int maxHealth = 1;

        [SerializeField]
        private int bombsPerVolley = 3;

        [SerializeField]
        private PrototypeBombDefinitionAsset bombDefinition;

        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        private GameObject telegraphCellPrefab;

        [SerializeField]
        private float visualHeight = 0.5f;

        [SerializeField]
        private float deathVisualSeconds = 0.12f;

        public string DefinitionId => definitionId;

        public float MoveCellsPerSecond => moveCellsPerSecond;

        public float TelegraphSeconds => telegraphSeconds;

        public float FlightSeconds => flightSeconds;

        public float RecoverySeconds => recoverySeconds;

        public int MaxHealth => maxHealth;

        public int BombsPerVolley => bombsPerVolley;

        public PrototypeBombDefinitionAsset BombDefinition => bombDefinition;

        public GameObject EnemyPrefab => enemyPrefab;

        public GameObject TelegraphCellPrefab => telegraphCellPrefab;

        public float VisualHeight => visualHeight;

        public float DeathVisualSeconds => deathVisualSeconds;

        public void Configure(
            string authoredDefinitionId,
            float authoredMoveCellsPerSecond,
            float authoredTelegraphSeconds,
            float authoredFlightSeconds,
            float authoredRecoverySeconds,
            int authoredMaxHealth,
            int authoredBombsPerVolley,
            PrototypeBombDefinitionAsset authoredBombDefinition,
            GameObject authoredEnemyPrefab,
            GameObject authoredTelegraphCellPrefab,
            float authoredVisualHeight,
            float authoredDeathVisualSeconds)
        {
            ThrowerEnemyDefinition definition = CreateCoreDefinition(
                authoredDefinitionId,
                authoredMoveCellsPerSecond,
                authoredTelegraphSeconds,
                authoredFlightSeconds,
                authoredRecoverySeconds,
                authoredMaxHealth,
                authoredBombsPerVolley,
                authoredBombDefinition);
            if (authoredEnemyPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredEnemyPrefab));
            }
            if (authoredTelegraphCellPrefab == null)
            {
                throw new ArgumentNullException(nameof(authoredTelegraphCellPrefab));
            }
            ValidateFiniteNonNegative(authoredVisualHeight, nameof(authoredVisualHeight));
            ValidateFinitePositive(authoredDeathVisualSeconds, nameof(authoredDeathVisualSeconds));

            definitionId = definition.Id.Value;
            moveCellsPerSecond = authoredMoveCellsPerSecond;
            telegraphSeconds = authoredTelegraphSeconds;
            flightSeconds = authoredFlightSeconds;
            recoverySeconds = authoredRecoverySeconds;
            maxHealth = authoredMaxHealth;
            bombsPerVolley = authoredBombsPerVolley;
            bombDefinition = authoredBombDefinition;
            enemyPrefab = authoredEnemyPrefab;
            telegraphCellPrefab = authoredTelegraphCellPrefab;
            visualHeight = authoredVisualHeight;
            deathVisualSeconds = authoredDeathVisualSeconds;
        }

        public ThrowerEnemyDefinition CreateCoreDefinition()
        {
            return CreateCoreDefinition(
                definitionId,
                moveCellsPerSecond,
                telegraphSeconds,
                flightSeconds,
                recoverySeconds,
                maxHealth,
                bombsPerVolley,
                bombDefinition);
        }

        public void ValidatePresentationReferences()
        {
            ValidateVisualPrefab(enemyPrefab, "Thrower enemy");
            ValidateVisualPrefab(telegraphCellPrefab, "Thrower telegraph cell");
            if (bombDefinition == null)
            {
                throw new InvalidOperationException("Thrower bomb definition is required.");
            }
            bombDefinition.ValidatePresentationReferences();
            ValidateFiniteNonNegative(visualHeight, nameof(visualHeight));
            ValidateFinitePositive(deathVisualSeconds, nameof(deathVisualSeconds));
            CreateCoreDefinition();
        }

        private static ThrowerEnemyDefinition CreateCoreDefinition(
            string id,
            float speed,
            float telegraph,
            float flight,
            float recovery,
            int health,
            int authoredBombsPerVolley,
            PrototypeBombDefinitionAsset authoredBombDefinition)
        {
            ValidateFinitePositive(speed, nameof(speed));
            ValidateFinitePositive(telegraph, nameof(telegraph));
            ValidateFinitePositive(flight, nameof(flight));
            ValidateFinitePositive(recovery, nameof(recovery));
            if (authoredBombDefinition == null)
            {
                throw new ArgumentNullException(nameof(authoredBombDefinition));
            }

            return new ThrowerEnemyDefinition(
                new EnemyDefinitionId(id),
                TimeSpan.FromSeconds(1f / speed),
                TimeSpan.FromSeconds(telegraph),
                TimeSpan.FromSeconds(flight),
                TimeSpan.FromSeconds(recovery),
                health,
                authoredBombsPerVolley,
                authoredBombDefinition.CreateCoreDefinition());
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
