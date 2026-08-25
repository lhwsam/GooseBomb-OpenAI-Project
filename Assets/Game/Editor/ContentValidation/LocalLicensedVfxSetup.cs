using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BombSwap.Editor.ContentValidation
{
    public static class LocalLicensedVfxSetup
    {
        public const string SettingsDirectory = "Assets/Arts/VFX/Resources";
        public const string SettingsAssetPath =
            SettingsDirectory + "/BombSwapLocalVfxOverrides.asset";
        public const string DustExplosionPrefabPath =
            "Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/" +
            "Fire & Explosion Effects/Prefabs/DustExplosion.prefab";
        public const string SparksEffectPrefabPath =
            "Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/" +
            "Legacy Particles/Prefabs/SparksEffect.prefab";
        public const string CrossBombCenterExplosionPrefabPath =
            "Assets/Arts/VFX/EffectPrefab/bomb/vfx_Explosion.prefab";
        public const string CrossBombStraightExplosionPrefabPath =
            "Assets/Arts/VFX/EffectPrefab/bomb/vfx_Bomb_Straight.prefab";
        public const string AreaBombGridExplosionPrefabPath =
            "Assets/Arts/VFX/EffectPrefab/bomb/vfx_Explosion_Grid.prefab";
        public const string BossIntroSpawnPrefabPath =
            "Assets/Arts/VFX/EffectPrefab/bomb/vfx_Spawn.prefab";
        public const string BossIntroLightningPrefabPath =
            "Assets/Arts/VFX/EffectPrefab/bomb/vfx_Lightning.prefab";

        private static readonly string[] PlayerBombPrefabPaths =
        {
            "Assets/Game/Content/Prefabs/Bomb/Player/NormalBomb.prefab",
            "Assets/Game/Content/Prefabs/Bomb/Player/RangeBomb.prefab",
            "Assets/Game/Content/Prefabs/Bomb/Player/StraightBomb.prefab",
        };

        private static readonly Vector3 BombReadyLocalPosition =
            new Vector3(-0.031f, 0.926f, -0.152f);
        private static readonly Vector3 BombReadyLocalEulerAngles =
            new Vector3(-90f, 180f, 0f);

        [MenuItem("Bomb Swap/Local Setup/Connect Licensed VFX")]
        public static void Connect()
        {
            GameObject dustExplosion = LoadParticlePrefab(DustExplosionPrefabPath);
            GameObject sparksEffect = LoadParticlePrefab(SparksEffectPrefabPath);
            GameObject centerExplosion = LoadParticlePrefab(
                CrossBombCenterExplosionPrefabPath);
            GameObject straightExplosion = LoadParticlePrefab(
                CrossBombStraightExplosionPrefabPath);
            GameObject areaGridExplosion = LoadParticlePrefab(
                AreaBombGridExplosionPrefabPath);
            GameObject bossIntroSpawn = LoadParticlePrefab(
                BossIntroSpawnPrefabPath);
            GameObject bossIntroLightning = LoadParticlePrefab(
                BossIntroLightningPrefabPath);
            SynchronizePlayerBombVfx(sparksEffect);
            EnsureDirectory(SettingsDirectory);

            PrototypeLocalVfxOverrides settings =
                AssetDatabase.LoadAssetAtPath<PrototypeLocalVfxOverrides>(SettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PrototypeLocalVfxOverrides>();
                settings.name = PrototypeLocalVfxOverrides.ResourcesLoadPath;
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
                Undo.RegisterCreatedObjectUndo(settings, "Connect Licensed VFX");
            }
            else
            {
                Undo.RecordObject(settings, "Connect Licensed VFX");
            }

            settings.Configure(
                dustExplosion,
                sparksEffect,
                BombReadyLocalPosition,
                BombReadyLocalEulerAngles);
            settings.ConfigureCrossBombExplosionVfx(
                centerExplosion,
                straightExplosion);
            settings.ConfigureAreaBombExplosionVfx(areaGridExplosion);
            settings.ConfigureBossIntroVfx(
                bossIntroSpawn,
                bossIntroLightning);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SettingsAssetPath, ImportAssetOptions.ForceUpdate);

            ValidateOrThrow();
            Debug.Log(
                $"[LocalLicensedVfxSetup] Connected licensed VFX through local settings at " +
                $"'{SettingsAssetPath}'. This asset is excluded from Git.");
        }

        [MenuItem("Bomb Swap/Local Setup/Reset Player Bomb VFX to Public Fallback")]
        public static void ResetPlayerBombVfxToPublicFallback()
        {
            SynchronizePlayerBombVfx(null);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[LocalLicensedVfxSetup] Cleared local particle children from player bomb " +
                "prefabs. The empty SparksEffect anchors are the public fallback.");
        }


        [MenuItem("Bomb Swap/Local Setup/Validate Licensed VFX")]
        public static void ValidateMenu()
        {
            ValidateOrThrow();
            Debug.Log("[LocalLicensedVfxSetup] Licensed VFX setup is valid.");
        }

        public static void ValidateOrThrow()
        {
            PrototypeLocalVfxOverrides settings =
                AssetDatabase.LoadAssetAtPath<PrototypeLocalVfxOverrides>(SettingsAssetPath);
            if (settings == null)
            {
                throw new InvalidOperationException(
                    $"Local VFX settings are missing at '{SettingsAssetPath}'. " +
                    "Run Bomb Swap/Local Setup/Connect Licensed VFX after importing the package.");
            }

            settings.ValidateConfiguration();
            ValidateExpectedReference(
                settings.SecretWallBreakVfxPrefab,
                DustExplosionPrefabPath,
                "secret-wall break VFX");
            ValidateExpectedReference(
                settings.BombReadyVfxPrefab,
                SparksEffectPrefabPath,
                "bomb-ready VFX");
            ValidateExpectedReference(
                settings.CrossBombCenterExplosionVfxPrefab,
                CrossBombCenterExplosionPrefabPath,
                "cross-bomb center explosion VFX");
            ValidateExpectedReference(
                settings.CrossBombStraightExplosionVfxPrefab,
                CrossBombStraightExplosionPrefabPath,
                "cross-bomb straight explosion VFX");
            ValidateStraightExplosionPrefab(
                settings.CrossBombStraightExplosionVfxPrefab);
            ValidateExpectedReference(
                settings.AreaBombGridExplosionVfxPrefab,
                AreaBombGridExplosionPrefabPath,
                "area-bomb grid explosion VFX");
            ValidateExpectedReference(
                settings.BossIntroSpawnVfxPrefab,
                BossIntroSpawnPrefabPath,
                "boss-intro spawn VFX");
            ValidateExpectedReference(
                settings.BossIntroLightningVfxPrefab,
                BossIntroLightningPrefabPath,
                "boss-intro lightning VFX");
            ValidatePlayerBombVfx();
        }

        public static bool IsApprovedPlayerBombVfxReference(
            string assetPath,
            string dependencyPath,
            string[] assetDependencies,
            ISet<string> approvedVfxDependencies)
        {
            bool referencesPlayerBomb = Array.IndexOf(PlayerBombPrefabPaths, assetPath) >= 0;
            for (int index = 0;
                 !referencesPlayerBomb && index < PlayerBombPrefabPaths.Length;
                 index++)
            {
                referencesPlayerBomb =
                    Array.IndexOf(assetDependencies, PlayerBombPrefabPaths[index]) >= 0;
            }
            return referencesPlayerBomb &&
                approvedVfxDependencies.Contains(dependencyPath);
        }

        public static ISet<string> GetApprovedPlayerBombVfxDependencies()
        {
            var dependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(SparksEffectPrefabPath, true),
                StringComparer.OrdinalIgnoreCase);
            dependencies.Add(SparksEffectPrefabPath);
            return dependencies;
        }

        private static GameObject LoadParticlePrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Licensed VFX prefab is missing at '{assetPath}'. " +
                    "Import the BombSwap VFX unitypackage first.");
            }
            if (prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
            {
                throw new InvalidOperationException(
                    $"Licensed VFX prefab at '{assetPath}' has no ParticleSystem.");
            }
            return prefab;
        }

        private static void ValidateExpectedReference(
            GameObject actual,
            string expectedPath,
            string label)
        {
            GameObject expected = LoadParticlePrefab(expectedPath);
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"Local {label} must reference '{expectedPath}'.");
            }
        }

        private static void ValidateStraightExplosionPrefab(GameObject prefab)
        {
            ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            for (int index = 0; index < systems.Length; index++)
            {
                if (systems[index].name == "Flames_F")
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Cross-bomb straight VFX at '{CrossBombStraightExplosionPrefabPath}' " +
                "requires a child ParticleSystem named 'Flames_F'.");
        }

        private static void EnsureDirectory(string assetDirectory)
        {
            string[] segments = assetDirectory.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }

        private static void SynchronizePlayerBombVfx(GameObject sparksEffectPrefab)
        {
            for (int index = 0; index < PlayerBombPrefabPaths.Length; index++)
            {
                string prefabPath = PlayerBombPrefabPaths[index];
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    Transform anchor = contents.transform.Find("SparksEffect");
                    ValidatePlayerBombAnchor(anchor, prefabPath);
                    anchor.localPosition = sparksEffectPrefab != null
                        ? BombReadyLocalPosition
                        : Vector3.zero;
                    anchor.localRotation = sparksEffectPrefab != null
                        ? Quaternion.Euler(BombReadyLocalEulerAngles)
                        : Quaternion.identity;
                    anchor.localScale = Vector3.one;
                    for (int childIndex = anchor.childCount - 1; childIndex >= 0; childIndex--)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            anchor.GetChild(childIndex).gameObject);
                    }

                    if (sparksEffectPrefab != null)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                            sparksEffectPrefab,
                            anchor);
                        instance.name = "Particle";
                        instance.transform.localPosition = Vector3.zero;
                        instance.transform.localRotation = Quaternion.identity;
                        instance.transform.localScale = Vector3.one;
                        ConfigureBombReadyParticles(instance);
                    }

                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private static void ValidatePlayerBombVfx()
        {
            for (int index = 0; index < PlayerBombPrefabPaths.Length; index++)
            {
                string prefabPath = PlayerBombPrefabPaths[index];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Player bomb prefab is missing at '{prefabPath}'.");
                }
                Transform anchor = prefab.transform.Find("SparksEffect");
                ValidatePlayerBombAnchor(anchor, prefabPath);
                if (Vector3.Distance(anchor.localPosition, BombReadyLocalPosition) > 0.0001f ||
                    Quaternion.Angle(
                        anchor.localRotation,
                        Quaternion.Euler(BombReadyLocalEulerAngles)) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"Player bomb anchor '{prefabPath}/SparksEffect' must use the " +
                        "configured fuse position and rotation.");
                }
                if (anchor.childCount != 1)
                {
                    throw new InvalidOperationException(
                        $"Player bomb anchor '{prefabPath}/SparksEffect' must contain the " +
                        "licensed particle after connection.");
                }
                Transform particle = anchor.GetChild(0);
                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    particle.gameObject);
                if (particle.name != "Particle" || sourcePath != SparksEffectPrefabPath)
                {
                    throw new InvalidOperationException(
                        $"Player bomb anchor '{prefabPath}/SparksEffect' contains an unexpected " +
                        $"child. Expected a 'Particle' instance of '{SparksEffectPrefabPath}'.");
                }

                if (particle.localPosition != Vector3.zero ||
                    Quaternion.Angle(particle.localRotation, Quaternion.identity) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"Player bomb particle '{prefabPath}/SparksEffect/Particle' must keep " +
                        "an identity transform under the configured anchor.");
                }

                ValidateBombReadyParticles(particle.gameObject, prefabPath);
            }
        }

        private static void ConfigureBombReadyParticles(GameObject root)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem.MainModule main = systems[index].main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;

                ParticleSystem.CollisionModule collision = systems[index].collision;
                collision.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(systems[index]);
            }
        }

        private static void ValidateBombReadyParticles(GameObject root, string prefabPath)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Player bomb particle '{prefabPath}/SparksEffect/Particle' requires at " +
                    "least one ParticleSystem.");
            }

            for (int index = 0; index < systems.Length; index++)
            {
                if (systems[index].main.simulationSpace !=
                    ParticleSystemSimulationSpace.Local)
                {
                    throw new InvalidOperationException(
                        $"Bomb-ready ParticleSystem '{systems[index].name}' in '{prefabPath}' " +
                        "must use Local simulation space.");
                }
                if (systems[index].collision.enabled)
                {
                    throw new InvalidOperationException(
                        $"Bomb-ready ParticleSystem '{systems[index].name}' in '{prefabPath}' " +
                        "must not collide with world geometry.");
                }
            }
        }

        private static void ValidatePlayerBombAnchor(
            Transform anchor,
            string prefabPath)
        {
            if (anchor == null)
            {
                throw new InvalidOperationException(
                    $"Player bomb prefab '{prefabPath}' requires a direct child named " +
                    "'SparksEffect'.");
            }
        }
    }
}
