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
        private GameObject crossBombCenterExplosionVfxPrefab;

        [SerializeField]
        private GameObject crossBombStraightExplosionVfxPrefab;

        [SerializeField]
        private GameObject areaBombGridExplosionVfxPrefab;

        [SerializeField]
        private GameObject bossIntroSpawnVfxPrefab;

        [SerializeField]
        private GameObject bossIntroLightningVfxPrefab;

        [SerializeField]
        private Vector3 bombReadyLocalPosition = new Vector3(-0.031f, 0.926f, -0.152f);

        [SerializeField]
        private Vector3 bombReadyLocalEulerAngles = new Vector3(-90f, 180f, 0f);

        public GameObject SecretWallBreakVfxPrefab => secretWallBreakVfxPrefab;

        public GameObject BombReadyVfxPrefab => bombReadyVfxPrefab;

        public GameObject CrossBombCenterExplosionVfxPrefab =>
            crossBombCenterExplosionVfxPrefab;

        public GameObject CrossBombStraightExplosionVfxPrefab =>
            crossBombStraightExplosionVfxPrefab;

        public GameObject AreaBombGridExplosionVfxPrefab =>
            areaBombGridExplosionVfxPrefab;

        public GameObject BossIntroSpawnVfxPrefab => bossIntroSpawnVfxPrefab;

        public GameObject BossIntroLightningVfxPrefab =>
            bossIntroLightningVfxPrefab;

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
            if (crossBombCenterExplosionVfxPrefab != null ||
                crossBombStraightExplosionVfxPrefab != null)
            {
                ValidateParticlePrefab(
                    crossBombCenterExplosionVfxPrefab,
                    nameof(crossBombCenterExplosionVfxPrefab));
                ValidateParticlePrefab(
                    crossBombStraightExplosionVfxPrefab,
                    nameof(crossBombStraightExplosionVfxPrefab));
            }
            if (areaBombGridExplosionVfxPrefab != null)
            {
                ValidateParticlePrefab(
                    areaBombGridExplosionVfxPrefab,
                    nameof(areaBombGridExplosionVfxPrefab));
            }
            if (bossIntroSpawnVfxPrefab != null ||
                bossIntroLightningVfxPrefab != null)
            {
                ValidateParticlePrefab(
                    bossIntroSpawnVfxPrefab,
                    nameof(bossIntroSpawnVfxPrefab));
                ValidateParticlePrefab(
                    bossIntroLightningVfxPrefab,
                    nameof(bossIntroLightningVfxPrefab));
            }
        }

        public void ConfigureCrossBombExplosionVfx(
            GameObject authoredCenterExplosionVfxPrefab,
            GameObject authoredStraightExplosionVfxPrefab)
        {
            ValidateParticlePrefab(
                authoredCenterExplosionVfxPrefab,
                nameof(authoredCenterExplosionVfxPrefab));
            ValidateParticlePrefab(
                authoredStraightExplosionVfxPrefab,
                nameof(authoredStraightExplosionVfxPrefab));

            crossBombCenterExplosionVfxPrefab = authoredCenterExplosionVfxPrefab;
            crossBombStraightExplosionVfxPrefab = authoredStraightExplosionVfxPrefab;
        }

        public void ConfigureAreaBombExplosionVfx(
            GameObject authoredGridExplosionVfxPrefab)
        {
            ValidateParticlePrefab(
                authoredGridExplosionVfxPrefab,
                nameof(authoredGridExplosionVfxPrefab));
            areaBombGridExplosionVfxPrefab = authoredGridExplosionVfxPrefab;
        }

        public void ConfigureBossIntroVfx(
            GameObject authoredSpawnVfxPrefab,
            GameObject authoredLightningVfxPrefab)
        {
            ValidateParticlePrefab(
                authoredSpawnVfxPrefab,
                nameof(authoredSpawnVfxPrefab));
            ValidateParticlePrefab(
                authoredLightningVfxPrefab,
                nameof(authoredLightningVfxPrefab));
            bossIntroSpawnVfxPrefab = authoredSpawnVfxPrefab;
            bossIntroLightningVfxPrefab = authoredLightningVfxPrefab;
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
