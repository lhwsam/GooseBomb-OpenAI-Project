using System;
using UnityEngine;

namespace BombSwap
{
    public sealed class PrototypeLocalVfxOverrides : ScriptableObject
    {
        public const string ResourcesLoadPath = "BombSwapLocalVfxOverrides";

        [SerializeField]
        private GameObject secretWallBreakVfxPrefab;

        [SerializeField]
        private GameObject bombReadyVfxPrefab;

        [SerializeField]
        private Vector3 bombReadyLocalPosition = new Vector3(-0.031f, 0.926f, -0.152f);

        [SerializeField]
        private Vector3 bombReadyLocalEulerAngles = new Vector3(-90f, 180f, 0f);

        public GameObject SecretWallBreakVfxPrefab => secretWallBreakVfxPrefab;

        public GameObject BombReadyVfxPrefab => bombReadyVfxPrefab;

        public Vector3 BombReadyLocalPosition => bombReadyLocalPosition;

        public Quaternion BombReadyLocalRotation =>
            Quaternion.Euler(bombReadyLocalEulerAngles);

        public void Configure(
            GameObject authoredSecretWallBreakVfxPrefab,
            GameObject authoredBombReadyVfxPrefab,
            Vector3 authoredBombReadyLocalPosition,
            Vector3 authoredBombReadyLocalEulerAngles)
        {
            ValidateParticlePrefab(
                authoredSecretWallBreakVfxPrefab,
                nameof(authoredSecretWallBreakVfxPrefab));
            ValidateParticlePrefab(
                authoredBombReadyVfxPrefab,
                nameof(authoredBombReadyVfxPrefab));

            secretWallBreakVfxPrefab = authoredSecretWallBreakVfxPrefab;
            bombReadyVfxPrefab = authoredBombReadyVfxPrefab;
            bombReadyLocalPosition = authoredBombReadyLocalPosition;
            bombReadyLocalEulerAngles = authoredBombReadyLocalEulerAngles;
        }

        public void ValidateConfiguration()
        {
            ValidateParticlePrefab(
                secretWallBreakVfxPrefab,
                nameof(secretWallBreakVfxPrefab));
            ValidateParticlePrefab(
                bombReadyVfxPrefab,
                nameof(bombReadyVfxPrefab));
        }

        public static PrototypeLocalVfxOverrides LoadOptional()
        {
            return Resources.Load<PrototypeLocalVfxOverrides>(ResourcesLoadPath);
        }

        private static void ValidateParticlePrefab(GameObject prefab, string parameterName)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
            {
                throw new ArgumentException(
                    "A local VFX override prefab requires at least one ParticleSystem.",
                    parameterName);
            }
        }
    }
}
