using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeBombDefinition",
        menuName = "Bomb Swap/Prototype/Bomb Definition")]
    public sealed class PrototypeBombDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "prototype-cross";

        [SerializeField]
        private float fuseSeconds = 2f;

        [SerializeField]
        private int range = 2;

        [SerializeField]
        private BombExplosionShape explosionShape = BombExplosionShape.Cross;

        [SerializeField]
        private float placementCooldownSeconds = 1.5f;

        [SerializeField]
        private GameObject bombPrefab;

        [SerializeField]
        private GameObject explosionCellPrefab;

        [SerializeField]
        private float explosionVisualSeconds = 0.25f;

        public string DefinitionId => definitionId;

        public float FuseSeconds => fuseSeconds;

        public int Range => range;

        public BombExplosionShape ExplosionShape => explosionShape;

        public float PlacementCooldownSeconds => placementCooldownSeconds;

        public GameObject BombPrefab => bombPrefab;

        public GameObject ExplosionCellPrefab => explosionCellPrefab;

        public float ExplosionVisualSeconds => explosionVisualSeconds;

        public void Configure(
            string stableDefinitionId,
            float fuseDurationSeconds,
            int explosionRange,
            GameObject bombVisualPrefab,
            GameObject explosionVisualPrefab,
            float explosionPresentationSeconds,
            float placementCooldownDurationSeconds = 1.5f,
            BombExplosionShape authoredExplosionShape = BombExplosionShape.Cross)
        {
            ValidateFinitePositive(fuseDurationSeconds, nameof(fuseDurationSeconds));
            ValidateFinitePositive(
                placementCooldownDurationSeconds,
                nameof(placementCooldownDurationSeconds));
            ValidateFinitePositive(
                explosionPresentationSeconds,
                nameof(explosionPresentationSeconds));
            if (string.IsNullOrWhiteSpace(stableDefinitionId))
            {
                throw new ArgumentException(
                    "Bomb definition ID cannot be empty.",
                    nameof(stableDefinitionId));
            }
            if (explosionRange < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(explosionRange),
                    explosionRange,
                    "Bomb range cannot be negative.");
            }
            if (authoredExplosionShape != BombExplosionShape.Cross &&
                authoredExplosionShape != BombExplosionShape.SquareArea &&
                authoredExplosionShape != BombExplosionShape.ForwardLine)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredExplosionShape),
                    authoredExplosionShape,
                    "Unsupported authored bomb explosion shape.");
            }
            if (bombVisualPrefab == null)
            {
                throw new ArgumentNullException(nameof(bombVisualPrefab));
            }
            if (explosionVisualPrefab == null)
            {
                throw new ArgumentNullException(nameof(explosionVisualPrefab));
            }

            definitionId = stableDefinitionId;
            fuseSeconds = fuseDurationSeconds;
            range = explosionRange;
            explosionShape = authoredExplosionShape;
            placementCooldownSeconds = placementCooldownDurationSeconds;
            bombPrefab = bombVisualPrefab;
            explosionCellPrefab = explosionVisualPrefab;
            explosionVisualSeconds = explosionPresentationSeconds;
        }

        public BombDefinition CreateCoreDefinition()
        {
            ValidateFinitePositive(fuseSeconds, nameof(fuseSeconds));
            if (range < 0)
            {
                throw new InvalidOperationException("Authored bomb range cannot be negative.");
            }

            return new BombDefinition(
                new BombDefinitionId(definitionId),
                explosionShape,
                TimeSpan.FromSeconds(fuseSeconds),
                range);
        }

        public BombWeaponDefinition CreateCoreWeaponDefinition()
        {
            ValidateFinitePositive(
                placementCooldownSeconds,
                nameof(placementCooldownSeconds));
            return new BombWeaponDefinition(
                CreateCoreDefinition(),
                TimeSpan.FromSeconds(placementCooldownSeconds));
        }

        public void ValidatePresentationReferences()
        {
            if (bombPrefab == null || explosionCellPrefab == null)
            {
                throw new InvalidOperationException(
                    "Prototype bomb definition requires bomb and explosion-cell prefabs.");
            }

            ValidateFinitePositive(explosionVisualSeconds, nameof(explosionVisualSeconds));
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
    }
}
