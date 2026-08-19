using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeContentValidator
    {
        public const string InputActionsPath = "Assets/Game/Content/Input/BombSwapInputActions.inputactions";
        public const string TestSandboxScenePath = "Assets/Game/Scenes/TestSandbox/TestSandbox.unity";
        public const string TestSandboxLanesScenePath =
            "Assets/Game/Scenes/TestSandbox/TestSandboxLanes.unity";
        public const string TestSandboxPillarsScenePath =
            "Assets/Game/Scenes/TestSandbox/TestSandboxPillars.unity";
        public const string TestSandboxArmorScenePath =
            "Assets/Game/Scenes/TestSandbox/TestSandboxArmor.unity";
        public const string ArmoredPanicPlaytestScenePath =
            "Assets/Game/Scenes/TestSandbox/ArmoredPanicPlaytest.unity";
        public const string SelfDestructGatesPlaytestScenePath =
            "Assets/Game/Scenes/TestSandbox/SelfDestructGatesPlaytest.unity";
        public const string BossBattlePlaytestScenePath =
            "Assets/Game/Scenes/TestSandbox/BossBattlePlaytest.unity";
        public const string ThrowerLanesPlaytestScenePath =
            "Assets/Game/Scenes/TestSandbox/ThrowerLanesPlaytest.unity";
        public const string TestSandboxGatesScenePath =
            "Assets/Game/Scenes/TestSandbox/TestSandboxGates.unity";
        public const string DungeonStartScenePath =
            "Assets/Game/Scenes/Dungeon/DungeonStart.unity";
        public const string DungeonRewardScenePath =
            "Assets/Game/Scenes/Dungeon/DungeonReward.unity";
        public const string DungeonBossAnteScenePath =
            "Assets/Game/Scenes/Dungeon/DungeonBossAnte.unity";
        public const string DungeonRecoveryScenePath =
            "Assets/Game/Scenes/Dungeon/DungeonRecovery.unity";
        public const string DungeonSecretScenePath =
            "Assets/Game/Scenes/Dungeon/DungeonSecret.unity";
        public const string DungeonBossScenePath =
            "Assets/Game/Scenes/Dungeon/DungeonBoss.unity";
        public const string PrototypeBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeCrossBomb.asset";
        public const string PrototypeAreaBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeAreaBomb.asset";
        public const string PrototypeLineBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeLineBomb.asset";
        public const string PrototypeSelfDestructBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeSelfDestructBlast.asset";
        public const string PrototypeThrowerBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeThrowerBlocker.asset";
        public const string PrototypeBossThrowBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeBossThrowBomb.asset";
        public const string PrototypeBossChainBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeBossChainBomb.asset";
        public const string PrototypeBombLoadoutPath =
            "Assets/Game/Content/Bombs/PrototypeBombLoadout.asset";
        public const string PrototypeBombRewardCatalogPath =
            "Assets/Game/Content/Bombs/PrototypeBombRewardCatalog.asset";
        public const string PrototypePlayerVitalsPath =
            "Assets/Game/Content/Player/PrototypePlayerVitals.asset";
        public const string PrototypeChaserDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeChaser.asset";
        public const string PrototypeChargerDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeCharger.asset";
        public const string PrototypeArmoredDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeArmored.asset";
        public const string PrototypeSelfDestructDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeSelfDestruct.asset";
        public const string PrototypeThrowerDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeThrower.asset";
        public const string PrototypeBossDefinitionPath =
            "Assets/Game/Content/Bosses/PrototypeBoss.asset";
        public const string PrototypeCombatRoomDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatLoop.asset";
        public const string PrototypeCombatLanesDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatLanes.asset";
        public const string PrototypeCombatPillarsDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatPillars.asset";
        public const string PrototypeCombatArmorDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatArmor.asset";
        public const string PrototypeCombatGatesDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatGates.asset";
        public const string PrototypeBossArenaDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeBossArena.asset";
        public const string PrototypeCombatThrowerDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatThrower.asset";
        public const string BombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BombPlaceholder.prefab";
        public const string ExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ExplosionCellPlaceholder.prefab";
        public const string AreaBombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/AreaBombPlaceholder.prefab";
        public const string AreaExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/AreaExplosionCellPlaceholder.prefab";
        public const string LineBombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/LineBombPlaceholder.prefab";
        public const string LineExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/LineExplosionCellPlaceholder.prefab";
        public const string BossThrowBombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BossThrowBombPlaceholder.prefab";
        public const string BossThrowExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BossThrowExplosionCellPlaceholder.prefab";
        public const string BossChainBombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BossChainBombPlaceholder.prefab";
        public const string BossChainExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BossChainExplosionCellPlaceholder.prefab";
        public const string ChaserPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChaserPlaceholder.prefab";
        public const string ChargerPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChargerPlaceholder.prefab";
        public const string ChargerTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChargerTelegraphCellPlaceholder.prefab";
        public const string ArmoredPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ArmoredPlaceholder.prefab";
        public const string ArmoredPanicTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ArmoredPanicTelegraphCellPlaceholder.prefab";
        public const string SelfDestructPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/SelfDestructPlaceholder.prefab";
        public const string SelfDestructTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/SelfDestructTelegraphCellPlaceholder.prefab";
        public const string ThrowerPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ThrowerPlaceholder.prefab";
        public const string ThrowerTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ThrowerTelegraphCellPlaceholder.prefab";
        public const string BossPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BossPlaceholder.prefab";
        public const string BossDangerCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BossDangerCellPlaceholder.prefab";
        public const string DestructibleWallMaterialPath =
            "Assets/Game/Content/Materials/Prototype/DestructibleWall.mat";
        public const string RecoveryPickupMaterialPath =
            "Assets/Game/Content/Materials/Prototype/RecoveryPickup.mat";
        public const string SecretRewardMaterialPath =
            "Assets/Game/Content/Materials/Prototype/SecretReward.mat";
        public const string SecretCrackMaterialPath =
            "Assets/Game/Content/Materials/Prototype/SecretCrack.mat";
        public const string PrototypeDungeonCombatRoomCatalogPath =
            "Assets/Game/Content/Rooms/PrototypeDungeonCombatRoomCatalog.asset";
        public const string PrototypeDungeonSpecialRoomCatalogPath =
            "Assets/Game/Content/Rooms/PrototypeDungeonSpecialRoomCatalog.asset";

        public static void Validate(ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ValidateInputActions(errors);
            ValidatePrototypeBombDefinitions(errors);
            ValidatePrototypePlayerVitals(errors);
            ValidatePrototypeChaserDefinition(errors);
            ValidatePrototypeChargerDefinition(errors);
            ValidatePrototypeArmoredDefinition(errors);
            ValidatePrototypeSelfDestructDefinition(errors);
            ValidatePrototypeThrowerDefinition(errors);
            ValidatePrototypeBossDefinition(errors);
            ValidatePrototypeRecoveryMaterial(errors);
            ValidatePrototypeSecretMaterials(errors);
            ValidatePrototypeCombatRoomDefinitions(errors);
            ValidatePrototypeDungeonCombatRoomCatalog(errors);
            ValidatePrototypeDungeonSpecialRoomCatalog(errors);
            ValidateDestructibleWallMaterial(errors);
            ValidateTestSandboxes(errors);
            ValidateStandaloneArmoredPlaytestScene(errors);
            ValidateStandaloneSelfDestructPlaytestScene(errors);
            ValidateStandaloneBossPlaytestScene(errors);
            ValidateStandaloneThrowerPlaytestScene(errors);
            ValidateBuildSettings(errors);
        }

        private static void ValidateDestructibleWallMaterial(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(DestructibleWallMaterialPath) == null)
            {
                errors.Add($"Missing prototype destructible-wall material: {DestructibleWallMaterialPath}");
            }
        }

        private static void ValidatePrototypeBombDefinitions(ICollection<string> errors)
        {
            PrototypeBombDefinitionAsset firstDefinition = ValidatePrototypeBombDefinition(
                PrototypeBombDefinitionPath,
                BombPrefabPath,
                ExplosionCellPrefabPath,
                "prototype-cross",
                BombExplosionShape.Cross,
                null,
                errors);
            PrototypeBombDefinitionAsset secondDefinition = ValidatePrototypeBombDefinition(
                PrototypeAreaBombDefinitionPath,
                AreaBombPrefabPath,
                AreaExplosionCellPrefabPath,
                "prototype-area",
                BombExplosionShape.SquareArea,
                1,
                errors);
            PrototypeBombDefinitionAsset lineDefinition =
                ValidatePrototypeBombDefinition(
                    PrototypeLineBombDefinitionPath,
                    LineBombPrefabPath,
                    LineExplosionCellPrefabPath,
                    "prototype-line",
                    BombExplosionShape.ForwardLine,
                    3,
                    errors);
            PrototypeBombDefinitionAsset bossThrowDefinition =
                ValidatePrototypeBombDefinition(
                    PrototypeBossThrowBombDefinitionPath,
                    BossThrowBombPrefabPath,
                    BossThrowExplosionCellPrefabPath,
                    "prototype-boss-throw",
                    BombExplosionShape.Cross,
                    2,
                    errors);
            PrototypeBombDefinitionAsset bossChainDefinition =
                ValidatePrototypeBombDefinition(
                    PrototypeBossChainBombDefinitionPath,
                    BossChainBombPrefabPath,
                    BossChainExplosionCellPrefabPath,
                    "prototype-boss-chain",
                    BombExplosionShape.Cross,
                    2,
                    errors);
            if ((bossThrowDefinition != null &&
                 bossThrowDefinition.FuseSeconds != 1.25f) ||
                (bossChainDefinition != null &&
                 bossChainDefinition.FuseSeconds != 2.25f))
            {
                errors.Add(
                    "Prototype boss throw/chain bomb fuses must be 1.25 and 2.25 seconds.");
            }
            ValidateForwardLineBombVisual(errors);
            PrototypeBombLoadoutAsset loadout =
                AssetDatabase.LoadAssetAtPath<PrototypeBombLoadoutAsset>(
                    PrototypeBombLoadoutPath);
            if (loadout == null)
            {
                errors.Add($"Missing prototype bomb loadout: {PrototypeBombLoadoutPath}");
                return;
            }

            try
            {
                loadout.CreateCoreLoadout(new ManualGameClock());
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype bomb loadout: {exception.Message}");
            }

            if (loadout.FirstSlot != firstDefinition || loadout.SecondSlot != secondDefinition)
            {
                errors.Add("Prototype bomb loadout must reference the validated first and second bomb assets.");
            }

            PrototypeBombRewardCatalogAsset rewardCatalog =
                AssetDatabase.LoadAssetAtPath<PrototypeBombRewardCatalogAsset>(
                    PrototypeBombRewardCatalogPath);
            if (rewardCatalog == null)
            {
                errors.Add(
                    $"Missing prototype bomb reward catalog: {PrototypeBombRewardCatalogPath}");
            }
            else
            {
                try
                {
                    rewardCatalog.Validate();
                    rewardCatalog.CreateRunLoadoutState();
                }
                catch (Exception exception)
                {
                    errors.Add($"Invalid prototype bomb reward catalog: {exception.Message}");
                }

                if (rewardCatalog.FirstSlot != firstDefinition ||
                    rewardCatalog.RewardCandidates.Count != 2 ||
                    rewardCatalog.RewardCandidates[0] != secondDefinition ||
                    rewardCatalog.RewardCandidates[1] != lineDefinition)
                {
                    errors.Add(
                        "Prototype bomb reward catalog must start with prototype-cross and offer prototype-area then prototype-line.");
                }
            }

            string[] legacyPaths =
            {
                "Assets/Game/Content/Bombs/PrototypeQuickCrossBomb.asset",
                "Assets/Game/Content/Materials/Prototype/QuickBomb.mat",
                "Assets/Game/Content/Materials/Prototype/QuickExplosion.mat",
                "Assets/Game/Content/Prefabs/Prototype/QuickBombPlaceholder.prefab",
                "Assets/Game/Content/Prefabs/Prototype/QuickExplosionCellPlaceholder.prefab"
            };
            for (int index = 0; index < legacyPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(legacyPaths[index]) != null)
                {
                    errors.Add($"Legacy quick-cross prototype asset still exists: {legacyPaths[index]}");
                }
            }

            string[] legacyLongCrossPaths =
            {
                "Assets/Game/Content/Bombs/PrototypeLongCrossBomb.asset",
                "Assets/Game/Content/Materials/Prototype/LongCrossBomb.mat",
                "Assets/Game/Content/Materials/Prototype/LongCrossExplosion.mat",
                "Assets/Game/Content/Prefabs/Prototype/LongCrossBombPlaceholder.prefab",
                "Assets/Game/Content/Prefabs/Prototype/LongCrossExplosionCellPlaceholder.prefab"
            };
            for (int index = 0; index < legacyLongCrossPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(legacyLongCrossPaths[index]) != null)
                {
                    errors.Add(
                        $"Legacy long-cross prototype asset still exists: {legacyLongCrossPaths[index]}");
                }
            }
        }

        private static void ValidateForwardLineBombVisual(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LineBombPrefabPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Content/Materials/Prototype/LineBomb.mat");
            if (prefab == null)
            {
                return;
            }

            Transform body = prefab.transform.Find("DirectionBody");
            Transform tip = prefab.transform.Find("DirectionTip");
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (prefab.name != "LineBombPlaceholder" ||
                prefab.transform.childCount != 2 ||
                body == null || tip == null ||
                renderers.Length != 2 || material == null ||
                renderers.Any(renderer => renderer.sharedMaterial != material) ||
                prefab.GetComponentsInChildren<Collider>(true).Length != 0)
            {
                errors.Add(
                    "Prototype line-bomb prefab must be an asymmetric collider-free two-part visual using LineBomb material.");
            }
        }

        private static PrototypeBombDefinitionAsset ValidatePrototypeBombDefinition(
            string definitionPath,
            string expectedBombPrefabPath,
            string expectedExplosionPrefabPath,
            string expectedDefinitionId,
            BombExplosionShape expectedShape,
            int? expectedRange,
            ICollection<string> errors)
        {
            PrototypeBombDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    definitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype bomb definition: {definitionPath}");
                return null;
            }

            try
            {
                definition.CreateCoreWeaponDefinition();
                definition.ValidatePresentationReferences();
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype bomb definition: {exception.Message}");
            }

            if (!string.Equals(
                    definition.DefinitionId,
                    expectedDefinitionId,
                    StringComparison.Ordinal) ||
                definition.ExplosionShape != expectedShape ||
                (expectedRange.HasValue && definition.Range != expectedRange.Value))
            {
                errors.Add(
                    $"Prototype bomb definition '{definitionPath}' must be " +
                    $"ID '{expectedDefinitionId}', shape {expectedShape}" +
                    (expectedRange.HasValue ? $", range {expectedRange.Value}." : "."));
            }

            string bombPrefabPath = AssetDatabase.GetAssetPath(definition.BombPrefab);
            if (!string.Equals(
                    bombPrefabPath,
                    expectedBombPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype bomb definition must reference '{expectedBombPrefabPath}', found '{bombPrefabPath}'.");
            }
            string explosionPrefabPath = AssetDatabase.GetAssetPath(definition.ExplosionCellPrefab);
            if (!string.Equals(
                    explosionPrefabPath,
                    expectedExplosionPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype bomb definition must reference '{expectedExplosionPrefabPath}', found '{explosionPrefabPath}'.");
            }

            return definition;
        }

        private static void ValidatePrototypePlayerVitals(ICollection<string> errors)
        {
            PrototypePlayerVitalsAsset vitals =
                AssetDatabase.LoadAssetAtPath<PrototypePlayerVitalsAsset>(
                    PrototypePlayerVitalsPath);
            if (vitals == null)
            {
                errors.Add($"Missing prototype player vitals: {PrototypePlayerVitalsPath}");
                return;
            }

            try
            {
                vitals.CreateCoreDefinition();
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype player vitals: {exception.Message}");
            }
        }

        private static void ValidatePrototypeChaserDefinition(ICollection<string> errors)
        {
            PrototypeChaserDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeChaserDefinitionAsset>(
                    PrototypeChaserDefinitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype chaser definition: {PrototypeChaserDefinitionPath}");
                return;
            }

            try
            {
                definition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype chaser definition: {exception.Message}");
            }

            string prefabPath = AssetDatabase.GetAssetPath(definition.ChaserPrefab);
            if (!string.Equals(prefabPath, ChaserPrefabPath, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype chaser definition must reference '{ChaserPrefabPath}', found '{prefabPath}'.");
            }
            if (definition.ChaserPrefab != null &&
                definition.ChaserPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add("Prototype chaser prefab must not contain a Collider; logical grid owns collision.");
            }
        }

        private static void ValidatePrototypeChargerDefinition(ICollection<string> errors)
        {
            PrototypeChargerDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeChargerDefinitionAsset>(
                    PrototypeChargerDefinitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype charger definition: {PrototypeChargerDefinitionPath}");
                return;
            }

            try
            {
                definition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype charger definition: {exception.Message}");
            }

            string prefabPath = AssetDatabase.GetAssetPath(definition.ChargerPrefab);
            if (!string.Equals(prefabPath, ChargerPrefabPath, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype charger definition must reference '{ChargerPrefabPath}', found '{prefabPath}'.");
            }
            string telegraphPrefabPath = AssetDatabase.GetAssetPath(
                definition.TelegraphCellPrefab);
            if (!string.Equals(
                    telegraphPrefabPath,
                    ChargerTelegraphCellPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "Prototype charger definition must reference " +
                    $"'{ChargerTelegraphCellPrefabPath}', found '{telegraphPrefabPath}'.");
            }
            if (definition.ChargerPrefab != null &&
                definition.ChargerPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add("Prototype charger prefab must not contain a Collider; logical grid owns collision.");
            }
            if (definition.TelegraphCellPrefab != null &&
                definition.TelegraphCellPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add(
                    "Prototype charger telegraph-cell prefab must not contain a Collider; " +
                    "logical grid owns collision.");
            }
        }

        private static void ValidatePrototypeArmoredDefinition(ICollection<string> errors)
        {
            PrototypeArmoredDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeArmoredDefinitionAsset>(
                    PrototypeArmoredDefinitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype armored definition: {PrototypeArmoredDefinitionPath}");
                return;
            }

            try
            {
                ArmoredEnemyDefinition core = definition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
                if (core.MaxHealth != 2 ||
                    definition.ArmoredCellsPerSecond != 1f ||
                    definition.BrokenCellsPerSecond != 3f ||
                    definition.GuardRadius != 1 ||
                    definition.PanicTelegraphSeconds != 0.6f ||
                    definition.PanicCellsPerSecond != 6f ||
                    definition.PanicRunDistance != 3 ||
                    definition.PanicRecoverSeconds != 0.5f)
                {
                    errors.Add(
                        "Prototype armored definition must use two stages, guard radius 1, " +
                        "0.6-second telegraph, 6 cells/second for 3 panic cells, " +
                        "0.5-second recovery, and 1-to-3 cells/second guard/chase speeds.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype armored definition: {exception.Message}");
            }

            string prefabPath = AssetDatabase.GetAssetPath(definition.ArmoredPrefab);
            if (!string.Equals(prefabPath, ArmoredPrefabPath, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype armored definition must reference '{ArmoredPrefabPath}', found '{prefabPath}'.");
            }
            if (definition.ArmoredPrefab != null &&
                definition.ArmoredPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add(
                    "Prototype armored prefab must not contain a Collider; logical grid owns collision.");
            }
            string telegraphPrefabPath = AssetDatabase.GetAssetPath(
                definition.PanicTelegraphCellPrefab);
            if (!string.Equals(
                    telegraphPrefabPath,
                    ArmoredPanicTelegraphCellPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype armored definition must reference " +
                    $"'{ArmoredPanicTelegraphCellPrefabPath}', found '{telegraphPrefabPath}'.");
            }
            if (definition.PanicTelegraphCellPrefab != null &&
                definition.PanicTelegraphCellPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add(
                    "Prototype armored panic telegraph-cell prefab must not contain a Collider; " +
                    "logical grid owns collision.");
            }
        }

        private static void ValidatePrototypeSelfDestructDefinition(
            ICollection<string> errors)
        {
            PrototypeSelfDestructDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeSelfDestructDefinitionAsset>(
                    PrototypeSelfDestructDefinitionPath);
            PrototypeBombDefinitionAsset blastDefinition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeSelfDestructBombDefinitionPath);
            if (definition == null)
            {
                errors.Add(
                    $"Missing prototype self-destruct definition: {PrototypeSelfDestructDefinitionPath}");
                return;
            }
            if (blastDefinition == null)
            {
                errors.Add(
                    $"Missing prototype self-destruct blast definition: {PrototypeSelfDestructBombDefinitionPath}");
                return;
            }

            try
            {
                SelfDestructEnemyDefinition core = definition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
                BombDefinition blast = blastDefinition.CreateCoreDefinition();
                if (definition.ChaseCellsPerSecond != 2f ||
                    core.ChaseStepInterval != TimeSpan.FromSeconds(0.5) ||
                    definition.WarningMaxCellsPerSecond != 5f ||
                    core.WarningMinimumStepInterval != TimeSpan.FromSeconds(0.2) ||
                    definition.WarningEscalationSeconds != 1.5f ||
                    core.WarningEscalationDuration != TimeSpan.FromSeconds(1.5) ||
                    definition.WarningDistance != 3 ||
                    core.WarningDistance != 3 ||
                    definition.PrimeDistance != 1 ||
                    core.PrimeDistance != 1 ||
                    blast.Id != new BombDefinitionId("prototype-self-destruct-blast") ||
                    blast.ExplosionShape != BombExplosionShape.Cross ||
                    blast.FuseDuration != TimeSpan.FromSeconds(0.75) ||
                    blast.Range != 2)
                {
                    errors.Add(
                        "Prototype self-destruct enemy must chase at 2 cells/second, " +
                        "escalate to 5 cells/second over 1.5 seconds within 3 cells, " +
                        "prime within 1 cell, and " +
                        "use a 0.75-second range-2 cross blast.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype self-destruct definition: {exception.Message}");
            }

            string blastPath = AssetDatabase.GetAssetPath(
                definition.DetonationBombDefinition);
            string prefabPath = AssetDatabase.GetAssetPath(definition.EnemyPrefab);
            string telegraphPath = AssetDatabase.GetAssetPath(
                definition.TelegraphCellPrefab);
            if (!string.Equals(
                    blastPath,
                    PrototypeSelfDestructBombDefinitionPath,
                    StringComparison.Ordinal) ||
                !string.Equals(prefabPath, SelfDestructPrefabPath, StringComparison.Ordinal) ||
                !string.Equals(
                    telegraphPath,
                    SelfDestructTelegraphCellPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "Prototype self-destruct definition has inconsistent blast or presentation references.");
            }
        }

        private static void ValidatePrototypeBossDefinition(ICollection<string> errors)
        {
            PrototypeBossDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBossDefinitionAsset>(
                    PrototypeBossDefinitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype boss definition: {PrototypeBossDefinitionPath}");
                return;
            }

            try
            {
                BossBattleDefinition core = definition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
                if (core.Id != new EnemyDefinitionId("prototype-boss") ||
                    core.MaxHealth != 10 ||
                    core.PhaseTwoHealthThreshold != 7 ||
                    core.LastStandHealthThreshold != 2 ||
                    core.PatternDamage != 1 ||
                    core.Tuning.PhaseOneChaseCount != 2 ||
                    core.Tuning.PhaseTwoChaseCount != 3 ||
                    core.Tuning.LastStandChaseCount != 2 ||
                    core.Tuning.ChargeDistance != 3 ||
                    core.Tuning.BombFlightDuration != TimeSpan.FromSeconds(0.45) ||
                    core.Tuning.BombThrowInterval != TimeSpan.FromSeconds(0.4) ||
                    core.Tuning.SelfDestructForceDelay != TimeSpan.FromSeconds(4.5) ||
                    core.Tuning.PhaseOneOverheatDuration != TimeSpan.FromSeconds(2) ||
                    core.Tuning.PhaseTwoOverheatDuration != TimeSpan.FromSeconds(1.5) ||
                    core.Tuning.LastStandOverheatDuration != TimeSpan.FromSeconds(2.25) ||
                    core.ThrowBombDefinition.Id !=
                        new BombDefinitionId("prototype-boss-throw") ||
                    core.ThrowBombDefinition.FuseDuration != TimeSpan.FromSeconds(1.25) ||
                    core.ThrowBombDefinition.Range != 2 ||
                    core.ChainBombDefinition.Id !=
                        new BombDefinitionId("prototype-boss-chain") ||
                    core.ChainBombDefinition.FuseDuration != TimeSpan.FromSeconds(2.25) ||
                    core.ChainBombDefinition.Range != 2 ||
                    definition.BossSpawn != new GridPosition(0, 1))
                {
                    errors.Add(
                        "Prototype boss definition does not match the validated two-phase encounter contract.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype boss definition: {exception.Message}");
            }

            string bossPrefabPath = AssetDatabase.GetAssetPath(definition.BossPrefab);
            string dangerPrefabPath = AssetDatabase.GetAssetPath(definition.DangerCellPrefab);
            string throwBombPath = AssetDatabase.GetAssetPath(definition.ThrowBombDefinition);
            string chainBombPath = AssetDatabase.GetAssetPath(definition.ChainBombDefinition);
            if (!string.Equals(bossPrefabPath, BossPrefabPath, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype boss definition must reference '{BossPrefabPath}', found '{bossPrefabPath}'.");
            }
            if (!string.Equals(
                    dangerPrefabPath,
                    BossDangerCellPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype boss definition must reference '{BossDangerCellPrefabPath}', found '{dangerPrefabPath}'.");
            }
            if (!string.Equals(
                    throwBombPath,
                    PrototypeBossThrowBombDefinitionPath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    chainBombPath,
                    PrototypeBossChainBombDefinitionPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "Prototype boss definition must reference the canonical throw and chain bomb assets.");
            }
            if ((definition.BossPrefab != null &&
                 definition.BossPrefab.GetComponentInChildren<Collider>(true) != null) ||
                (definition.DangerCellPrefab != null &&
                 definition.DangerCellPrefab.GetComponentInChildren<Collider>(true) != null))
            {
                errors.Add(
                    "Prototype boss presentation prefabs must not contain Colliders; logical grid owns collision.");
            }

            PrototypeCombatRoomDefinitionAsset shell =
                AssetDatabase.LoadAssetAtPath<PrototypeCombatRoomDefinitionAsset>(
                    PrototypeBossArenaDefinitionPath);
            if (shell != null)
            {
                CombatRoomDefinition room = shell.CreateCoreDefinition();
                var expectedPillars = new HashSet<GridPosition>
                {
                    new GridPosition(-2, -1),
                    new GridPosition(2, -1),
                    new GridPosition(-2, 1),
                    new GridPosition(2, 1),
                };
                if (room.Id != new RoomDefinitionId("prototype-boss-arena") ||
                    room.Width != 11 || room.Depth != 9 ||
                    room.PlayerSpawn != new GridPosition(0, -3) ||
                    !expectedPillars.SetEquals(room.IndestructibleWalls) ||
                    !room.RetreatAnchors.SequenceEqual(new[]
                    {
                        new GridPosition(-4, -2),
                        new GridPosition(-3, 3),
                        new GridPosition(0, -3),
                        new GridPosition(0, 3),
                        new GridPosition(3, 3),
                        new GridPosition(4, -2),
                    }) ||
                    room.SelfDestructSpawn != new GridPosition(-4, 3) ||
                    !room.SelfDestructAnchors.SequenceEqual(new[]
                    {
                        new GridPosition(-3, 3),
                        new GridPosition(0, 3),
                        new GridPosition(3, 3),
                    }) ||
                    !room.IsInside(definition.BossSpawn) ||
                    room.IsBlocked(definition.BossSpawn) ||
                    room.PlayerSpawn == definition.BossSpawn ||
                    !room.LureLoop.Contains(definition.BossSpawn))
                {
                    errors.Add(
                        "Prototype boss arena must preserve its 11x9 spawn, four pillars, six throw anchors, three summon anchors, and central lure loop.");
                }
            }
            else
            {
                errors.Add($"Missing prototype boss arena: {PrototypeBossArenaDefinitionPath}");
            }
        }

        private static void ValidatePrototypeThrowerDefinition(
            ICollection<string> errors)
        {
            PrototypeThrowerDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeThrowerDefinitionAsset>(
                    PrototypeThrowerDefinitionPath);
            PrototypeBombDefinitionAsset bombDefinition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeThrowerBombDefinitionPath);
            PrototypeCombatRoomDefinitionAsset roomDefinition =
                AssetDatabase.LoadAssetAtPath<PrototypeCombatRoomDefinitionAsset>(
                    PrototypeCombatThrowerDefinitionPath);
            if (definition == null || bombDefinition == null || roomDefinition == null)
            {
                errors.Add(
                    "Missing prototype thrower definition, blocker bomb, or dedicated room definition.");
                return;
            }

            try
            {
                ThrowerEnemyDefinition core = definition.CreateCoreDefinition();
                BombDefinition bomb = bombDefinition.CreateCoreDefinition();
                CombatRoomDefinition room = roomDefinition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
                if (core.Id != new EnemyDefinitionId("prototype-thrower") ||
                    core.MoveStepInterval != TimeSpan.FromSeconds(1) ||
                    core.TelegraphDuration != TimeSpan.FromSeconds(0.3) ||
                    core.FlightDuration != TimeSpan.FromSeconds(0.45) ||
                    core.RecoveryDuration != TimeSpan.FromSeconds(0.75) ||
                    core.MaxHealth != 1 ||
                    core.BombsPerVolley != 3 ||
                    bomb.Id != new BombDefinitionId("prototype-thrower-blocker") ||
                    bomb.ExplosionShape != BombExplosionShape.Cross ||
                    bomb.FuseDuration != TimeSpan.FromSeconds(1.5) ||
                    bomb.Range != 1 ||
                    room.Id != new RoomDefinitionId("prototype-combat-thrower") ||
                    room.ThrowerSpawn != new GridPosition(0, 3) ||
                    !room.ThrowerFiringAnchors.SequenceEqual(new[]
                    {
                        new GridPosition(0, 3),
                        new GridPosition(3, 2),
                        new GridPosition(-3, 2),
                    }) ||
                    !room.ThrowerTargetAnchors.SequenceEqual(new[]
                    {
                        new GridPosition(0, 0),
                        new GridPosition(-3, -2),
                        new GridPosition(3, -2),
                        new GridPosition(-4, 1),
                        new GridPosition(4, 1),
                        new GridPosition(0, 4),
                    }))
                {
                    errors.Add(
                        "Prototype thrower content does not match the Proposed timing, bomb, " +
                        "or dedicated Lanes anchor contract.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype thrower content: {exception.Message}");
            }

            if (!string.Equals(
                    AssetDatabase.GetAssetPath(definition.BombDefinition),
                    PrototypeThrowerBombDefinitionPath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(definition.EnemyPrefab),
                    ThrowerPrefabPath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(definition.TelegraphCellPrefab),
                    ThrowerTelegraphCellPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "Prototype thrower definition has inconsistent bomb or presentation references.");
            }
        }

        private static void ValidatePrototypeCombatRoomDefinitions(ICollection<string> errors)
        {
            string[] expectedPaths =
            {
                PrototypeCombatRoomDefinitionPath,
                PrototypeCombatLanesDefinitionPath,
                PrototypeCombatPillarsDefinitionPath,
                PrototypeCombatArmorDefinitionPath,
                PrototypeCombatGatesDefinitionPath,
            };
            string[] expectedIds =
            {
                "prototype-combat-loop",
                "prototype-combat-lanes",
                "prototype-combat-pillars",
                "prototype-combat-armor",
                "prototype-combat-gates",
            };
            GridPosition?[] expectedChargerSpawns =
            {
                null,
                null,
                new GridPosition(-1, 1),
                null,
                null,
            };
            GridPosition?[] expectedArmoredSpawns =
            {
                null,
                null,
                null,
                new GridPosition(0, 1),
                null,
            };
            GridPosition?[] expectedSelfDestructSpawns =
            {
                null,
                null,
                null,
                null,
                new GridPosition(3, 0),
            };

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < expectedPaths.Length; index++)
            {
                string path = expectedPaths[index];
                PrototypeCombatRoomDefinitionAsset definition =
                    AssetDatabase.LoadAssetAtPath<PrototypeCombatRoomDefinitionAsset>(path);
                if (definition == null)
                {
                    errors.Add($"Missing prototype combat room definition: {path}");
                    continue;
                }

                try
                {
                    CombatRoomDefinition room = definition.CreateCoreDefinition();
                    if (room.Id != new RoomDefinitionId(expectedIds[index]) ||
                        room.RoomType != RoomType.Combat)
                    {
                        errors.Add(
                            $"Prototype combat room '{path}' must use ID '{expectedIds[index]}' and Combat type.");
                    }
                    if (!seenIds.Add(room.Id.Value))
                    {
                        errors.Add($"Prototype combat room ID is duplicated: '{room.Id.Value}'.");
                    }
                    if (room.ChargerSpawn != expectedChargerSpawns[index])
                    {
                        errors.Add(
                            $"Prototype combat room '{path}' has unexpected charger spawn " +
                            $"'{room.ChargerSpawn}'; expected '{expectedChargerSpawns[index]}'.");
                    }
                    if (room.ArmoredSpawn != expectedArmoredSpawns[index])
                    {
                        errors.Add(
                            $"Prototype combat room '{path}' has unexpected armored spawn " +
                            $"'{room.ArmoredSpawn}'; expected '{expectedArmoredSpawns[index]}'.");
                    }
                    if (room.SelfDestructSpawn != expectedSelfDestructSpawns[index])
                    {
                        errors.Add(
                            $"Prototype combat room '{path}' has unexpected self-destruct spawn " +
                            $"'{room.SelfDestructSpawn}'; expected '{expectedSelfDestructSpawns[index]}'.");
                    }
                    if (index == 4 && !room.SelfDestructAnchors.SequenceEqual(new[]
                        {
                            new GridPosition(0, -2),
                            new GridPosition(0, 2),
                        }))
                    {
                        errors.Add(
                            "Prototype Gates room must use the authored lower/upper self-destruct anchors.");
                    }
                    if (index == 2)
                    {
                        ValidatePillarsLaneLayout(room, errors);
                    }
                    if (index == 3)
                    {
                        ValidateArmorPanicLayout(room, errors);
                    }
                    if (index == 4)
                    {
                        ValidateGatesSelfDestructLayout(room, 2, errors);
                    }
                    RoomExitDirection[] exitDirections = room.Exits
                        .Select(roomExit => roomExit.Direction)
                        .OrderBy(direction => direction)
                        .ToArray();
                    if (!exitDirections.SequenceEqual(new[]
                        {
                            RoomExitDirection.North,
                            RoomExitDirection.East,
                            RoomExitDirection.South,
                            RoomExitDirection.West,
                        }))
                    {
                        errors.Add(
                            $"Prototype combat room '{path}' must author one potential exit " +
                            "in each cardinal direction.");
                    }
                }
                catch (Exception exception)
                {
                    errors.Add($"Invalid prototype combat room definition '{path}': {exception.Message}");
                }
            }
        }

        private static void ValidatePillarsLaneLayout(
            CombatRoomDefinition room,
            ICollection<string> errors)
        {
            var expectedFixedWalls = new HashSet<GridPosition>
            {
                new GridPosition(-4, -2),
                new GridPosition(-2, 1),
                new GridPosition(2, 1),
                new GridPosition(-3, -3),
                new GridPosition(-3, 3),
                new GridPosition(3, -3),
                new GridPosition(3, 3),
            };
            var expectedSafeCells = new HashSet<GridPosition>
            {
                new GridPosition(-3, -2),
                new GridPosition(-3, -1),
                new GridPosition(-2, -2),
            };
            var expectedRetreatAnchors = new HashSet<GridPosition>
            {
                new GridPosition(-3, -1),
                new GridPosition(-2, -2),
            };

            bool hasExpectedLoop = room.LureLoop.Count == 8 &&
                room.LureLoop.All(position =>
                    position.X >= -1 && position.X <= 1 &&
                    position.Z >= -1 && position.Z <= 1 &&
                    (Math.Abs(position.X) == 1 || Math.Abs(position.Z) == 1));
            if (room.PlayerSpawn != new GridPosition(-3, -2) ||
                room.ChaserSpawn != new GridPosition(3, 2) ||
                room.ChargerSpawn != new GridPosition(-1, 1) ||
                !expectedFixedWalls.SetEquals(room.IndestructibleWalls) ||
                room.DestructibleWalls.Count != 1 ||
                room.DestructibleWalls[0] != new GridPosition(2, -2) ||
                !expectedSafeCells.SetEquals(room.SafePlayerCells) ||
                !expectedRetreatAnchors.SetEquals(room.RetreatAnchors) ||
                !hasExpectedLoop)
            {
                errors.Add(
                    "Prototype Pillars room must preserve the authored short charge lanes, " +
                    "side escape cells, collision stops, and central 3x3 lure loop.");
            }
        }

        private static void ValidateArmorPanicLayout(
            CombatRoomDefinition room,
            ICollection<string> errors)
        {
            var expectedFixedWalls = new HashSet<GridPosition>
            {
                new GridPosition(-2, -2),
                new GridPosition(2, -2),
                new GridPosition(-2, -1),
                new GridPosition(2, -1),
                new GridPosition(-1, 2),
                new GridPosition(0, 2),
                new GridPosition(1, 2),
                new GridPosition(-4, 0),
                new GridPosition(4, 0),
            };
            var expectedSafeCells = new HashSet<GridPosition>
            {
                new GridPosition(0, -2),
                new GridPosition(-1, -2),
                new GridPosition(1, -2),
            };
            var expectedRetreatAnchors = new HashSet<GridPosition>
            {
                new GridPosition(-3, -2),
                new GridPosition(3, -2),
            };

            bool hasExpectedLoop = room.LureLoop.Count == 24 &&
                room.LureLoop.All(position =>
                    position.X >= -3 && position.X <= 3 &&
                    position.Z >= -3 && position.Z <= 3 &&
                    (Math.Abs(position.X) == 3 || Math.Abs(position.Z) == 3));
            if (room.PlayerSpawn != new GridPosition(0, -2) ||
                room.ChaserSpawn != new GridPosition(4, 4) ||
                room.ArmoredSpawn != new GridPosition(0, 1) ||
                !expectedFixedWalls.SetEquals(room.IndestructibleWalls) ||
                room.DestructibleWalls.Count != 0 ||
                !expectedSafeCells.SetEquals(room.SafePlayerCells) ||
                !expectedRetreatAnchors.SetEquals(room.RetreatAnchors) ||
                !hasExpectedLoop)
            {
                errors.Add(
                    "Prototype Armor room must preserve the T-junction guard pocket, " +
                    "three-cell east/west panic branches, safe approach, and outer lure loop.");
            }
        }

        private static void ValidateGatesSelfDestructLayout(
            CombatRoomDefinition room,
            int selfDestructBlastRange,
            ICollection<string> errors)
        {
            var expectedFixedWalls = new HashSet<GridPosition>
            {
                new GridPosition(-2, -1),
                new GridPosition(-1, -1),
                new GridPosition(1, -1),
                new GridPosition(2, -1),
                new GridPosition(-2, 1),
                new GridPosition(-1, 1),
                new GridPosition(1, 1),
                new GridPosition(2, 1),
            };
            var expectedDestructibleWalls = new HashSet<GridPosition>
            {
                new GridPosition(0, -1),
                new GridPosition(0, 1),
            };

            if (room.PlayerSpawn != new GridPosition(0, -3) ||
                room.ChaserSpawn != new GridPosition(0, 3) ||
                room.SelfDestructSpawn != new GridPosition(3, 0) ||
                !expectedFixedWalls.SetEquals(room.IndestructibleWalls) ||
                !expectedDestructibleWalls.SetEquals(room.DestructibleWalls))
            {
                errors.Add(
                    "Prototype Gates room must preserve its player/enemy spawns, " +
                    "eight fixed barrier cells, and two central destructible gates.");
                return;
            }

            GridPosition[] anchors =
            {
                new GridPosition(0, -2),
                new GridPosition(0, 2),
            };
            GridPosition[] expectedDestroyedGates =
            {
                new GridPosition(0, -1),
                new GridPosition(0, 1),
            };

            for (int index = 0; index < anchors.Length; index++)
            {
                IReadOnlyCollection<GridPosition> destroyedWalls =
                    ResolveGatesBlastDestroyedWalls(
                        room,
                        anchors[index],
                        selfDestructBlastRange);
                if (destroyedWalls.Count != 1 ||
                    !destroyedWalls.Contains(expectedDestroyedGates[index]))
                {
                    errors.Add(
                        $"Prototype Gates anchor {anchors[index]} with cross range " +
                        $"{selfDestructBlastRange} must destroy only gate " +
                        $"{expectedDestroyedGates[index]}; the first destructible wall " +
                        "must stop further propagation.");
                }
            }
        }

        private static IReadOnlyCollection<GridPosition> ResolveGatesBlastDestroyedWalls(
            CombatRoomDefinition room,
            GridPosition origin,
            int blastRange)
        {
            var grid = new GridState();
            int halfWidth = room.Width / 2;
            int halfDepth = room.Depth / 2;
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int z = -halfDepth; z <= halfDepth; z++)
                {
                    grid.TrySetTerrain(new GridPosition(x, z), GridTerrain.Floor);
                }
            }
            for (int index = 0; index < room.IndestructibleWalls.Count; index++)
            {
                grid.TrySetTerrain(
                    room.IndestructibleWalls[index],
                    GridTerrain.IndestructibleWall);
            }
            for (int index = 0; index < room.DestructibleWalls.Count; index++)
            {
                grid.TrySetTerrain(
                    room.DestructibleWalls[index],
                    GridTerrain.DestructibleWall);
            }

            var clock = new ManualGameClock();
            var bombSimulation = new BombSimulation(
                grid,
                clock,
                TimeSpan.FromMilliseconds(100));
            var definition = new BombDefinition(
                new BombDefinitionId("validator-self-destruct-blast"),
                BombExplosionShape.Cross,
                TimeSpan.FromMilliseconds(1),
                blastRange);
            if (!bombSimulation.TryPlaceBomb(
                    definition,
                    origin,
                    new ActorId(1),
                    out BombId _))
            {
                throw new InvalidOperationException(
                    $"Could not place validator blast at Gates anchor {origin}.");
            }

            clock.Advance(definition.FuseDuration);
            IReadOnlyList<BombExplosion> explosions = bombSimulation.ProcessDueBombs();
            if (explosions.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one validator blast at Gates anchor {origin}.");
            }

            return new HashSet<GridPosition>(explosions[0].DestroyedWalls);
        }

        private static void ValidatePrototypeDungeonCombatRoomCatalog(
            ICollection<string> errors)
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonCombatRoomCatalogAsset>(
                    PrototypeDungeonCombatRoomCatalogPath);
            if (catalog == null)
            {
                errors.Add(
                    $"Missing prototype dungeon combat room catalog: " +
                    PrototypeDungeonCombatRoomCatalogPath);
                return;
            }

            string[] expectedRoomPaths =
            {
                PrototypeCombatRoomDefinitionPath,
                PrototypeCombatLanesDefinitionPath,
                PrototypeCombatPillarsDefinitionPath,
                PrototypeCombatArmorDefinitionPath,
                PrototypeCombatGatesDefinitionPath,
            };
            string[] expectedSceneNames =
            {
                "TestSandbox",
                "TestSandboxLanes",
                "TestSandboxPillars",
                "TestSandboxArmor",
                "TestSandboxGates",
            };
            if (catalog.Entries.Count != expectedRoomPaths.Length)
            {
                errors.Add("Prototype dungeon combat room catalog must contain five entries.");
                return;
            }

            try
            {
                catalog.CreateCoreDefinitions();
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"Invalid prototype dungeon combat room catalog: {exception.Message}");
                return;
            }

            for (int index = 0; index < expectedRoomPaths.Length; index++)
            {
                PrototypeCombatRoomDefinitionAsset expectedRoom =
                    AssetDatabase.LoadAssetAtPath<PrototypeCombatRoomDefinitionAsset>(
                        expectedRoomPaths[index]);
                PrototypeDungeonCombatRoomEntry entry = catalog.Entries[index];
                if (entry.RoomDefinition != expectedRoom ||
                    !string.Equals(
                        entry.SceneName,
                        expectedSceneNames[index],
                        StringComparison.Ordinal))
                {
                    errors.Add(
                        $"Prototype dungeon catalog entry {index} must map " +
                        $"'{expectedRoomPaths[index]}' to '{expectedSceneNames[index]}'.");
                }
            }
        }

        private static void ValidatePrototypeDungeonSpecialRoomCatalog(
            ICollection<string> errors)
        {
            PrototypeDungeonSpecialRoomCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonSpecialRoomCatalogAsset>(
                    PrototypeDungeonSpecialRoomCatalogPath);
            if (catalog == null)
            {
                errors.Add(
                    $"Missing prototype dungeon special-room catalog: " +
                    PrototypeDungeonSpecialRoomCatalogPath);
                return;
            }

            RoomType[] expectedTypes =
            {
                RoomType.Start,
                RoomType.BombReward,
                RoomType.BossAntechamber,
                RoomType.Boss,
                RoomType.Recovery,
                RoomType.Secret,
            };
            string[] expectedSceneNames =
            {
                "DungeonStart",
                "DungeonReward",
                "DungeonBossAnte",
                "DungeonBoss",
                "DungeonRecovery",
                "DungeonSecret",
            };
            if (catalog.Entries.Count != expectedTypes.Length)
            {
                errors.Add(
                    "Prototype dungeon special-room catalog must contain six entries.");
                return;
            }

            try
            {
                for (int index = 0; index < expectedTypes.Length; index++)
                {
                    if (catalog.Entries[index].RoomType != expectedTypes[index] ||
                        !string.Equals(
                            catalog.Entries[index].SceneName,
                            expectedSceneNames[index],
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            catalog.GetSceneName(expectedTypes[index]),
                            expectedSceneNames[index],
                            StringComparison.Ordinal))
                    {
                        errors.Add(
                            $"Prototype dungeon special catalog entry {index} must map " +
                            $"{expectedTypes[index]} to '{expectedSceneNames[index]}'.");
                    }
                }
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"Invalid prototype dungeon special-room catalog: {exception.Message}");
            }
        }

        private static void ValidatePrototypeRecoveryMaterial(
            ICollection<string> errors)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                RecoveryPickupMaterialPath);
            if (material == null)
            {
                errors.Add(
                    $"Missing prototype recovery pickup material: " +
                    RecoveryPickupMaterialPath);
                return;
            }
            if (material.shader == null ||
                !string.Equals(
                    material.shader.name,
                    "Universal Render Pipeline/Lit",
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "Prototype recovery pickup material must use the URP Lit shader.");
            }
            if (!Approximately(
                    material.color,
                    PrototypeRecoveryPickupPresenter.DefaultPickupColor))
            {
                errors.Add(
                    "Prototype recovery pickup material has the wrong base color.");
            }
        }

        private static void ValidatePrototypeSecretMaterials(
            ICollection<string> errors)
        {
            ValidateUrpLitMaterial(
                SecretRewardMaterialPath,
                "secret reward",
                errors);
            ValidateUrpLitMaterial(
                SecretCrackMaterialPath,
                "secret crack",
                errors);
        }

        private static void ValidateUrpLitMaterial(
            string path,
            string label,
            ICollection<string> errors)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                errors.Add($"Missing prototype {label} material: {path}");
                return;
            }
            if (material.shader == null ||
                !string.Equals(
                    material.shader.name,
                    "Universal Render Pipeline/Lit",
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype {label} material must use Universal Render Pipeline/Lit.");
            }
        }

        private static void ValidateInputActions(ICollection<string> errors)
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                errors.Add($"Missing or invalid Input Actions asset: {InputActionsPath}");
                return;
            }

            InputActionMap gameplay = asset.FindActionMap(BombSwapInputActionNames.GameplayMap, false);
            if (gameplay == null)
            {
                errors.Add($"Input Actions asset is missing map '{BombSwapInputActionNames.GameplayMap}'.");
                return;
            }

            ValidateAction(
                gameplay,
                BombSwapInputActionNames.Move,
                InputActionType.Value,
                "Vector2",
                errors);
            ValidateAction(
                gameplay,
                BombSwapInputActionNames.PlaceBomb,
                InputActionType.Button,
                "Button",
                errors);
            ValidateAction(
                gameplay,
                BombSwapInputActionNames.SwapBomb,
                InputActionType.Button,
                "Button",
                errors);
            ValidateAction(
                gameplay,
                BombSwapInputActionNames.Pause,
                InputActionType.Button,
                "Button",
                errors);
            ValidateAction(
                gameplay,
                BombSwapInputActionNames.RestartRun,
                InputActionType.Button,
                "Button",
                errors);

            RequireBindings(gameplay, BombSwapInputActionNames.Move, errors,
                "<Keyboard>/w",
                "<Keyboard>/a",
                "<Keyboard>/s",
                "<Keyboard>/d",
                "<Keyboard>/upArrow",
                "<Keyboard>/leftArrow",
                "<Keyboard>/downArrow",
                "<Keyboard>/rightArrow",
                "<Gamepad>/leftStick",
                "<Gamepad>/dpad");
            RequireBindings(gameplay, BombSwapInputActionNames.PlaceBomb, errors,
                "<Keyboard>/z",
                "<Gamepad>/buttonSouth");
            RequireBindings(gameplay, BombSwapInputActionNames.SwapBomb, errors,
                "<Keyboard>/x",
                "<Gamepad>/buttonWest");
            RequireBindings(gameplay, BombSwapInputActionNames.Pause, errors,
                "<Keyboard>/escape",
                "<Gamepad>/start");
            RequireBindings(gameplay, BombSwapInputActionNames.RestartRun, errors,
                "<Keyboard>/r",
                "<Gamepad>/select");

            RequireControlScheme(asset, "Keyboard", "<Keyboard>", errors);
            RequireControlScheme(asset, "Gamepad", "<Gamepad>", errors);

            var duplicateBindings = gameplay.bindings
                .Where(binding => !binding.isComposite)
                .GroupBy(binding => string.Join("|",
                    binding.action ?? string.Empty,
                    binding.name ?? string.Empty,
                    binding.path ?? string.Empty,
                    binding.groups ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            foreach (string duplicate in duplicateBindings)
            {
                errors.Add($"Input Actions contains a duplicate binding: {duplicate}");
            }
        }

        private static void ValidateAction(
            InputActionMap map,
            string actionName,
            InputActionType expectedType,
            string expectedControlType,
            ICollection<string> errors)
        {
            InputAction action = map.FindAction(actionName, false);
            if (action == null)
            {
                errors.Add($"Gameplay input map is missing action '{actionName}'.");
                return;
            }

            if (action.type != expectedType)
            {
                errors.Add(
                    $"Input action '{actionName}' has type {action.type}; expected {expectedType}.");
            }
            if (!string.Equals(action.expectedControlType, expectedControlType, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Input action '{actionName}' expects '{action.expectedControlType}'; expected '{expectedControlType}'.");
            }
        }

        private static void RequireBindings(
            InputActionMap map,
            string actionName,
            ICollection<string> errors,
            params string[] requiredPaths)
        {
            InputAction action = map.FindAction(actionName, false);
            if (action == null)
            {
                return;
            }

            foreach (string requiredPath in requiredPaths)
            {
                bool found = action.bindings.Any(binding =>
                    string.Equals(binding.path, requiredPath, StringComparison.OrdinalIgnoreCase));
                if (!found)
                {
                    errors.Add($"Input action '{actionName}' is missing binding '{requiredPath}'.");
                }
            }
        }

        private static void RequireControlScheme(
            InputActionAsset asset,
            string schemeName,
            string requiredDevicePath,
            ICollection<string> errors)
        {
            InputControlScheme? scheme = asset.controlSchemes
                .Where(candidate => string.Equals(candidate.name, schemeName, StringComparison.Ordinal))
                .Cast<InputControlScheme?>()
                .FirstOrDefault();
            if (!scheme.HasValue)
            {
                errors.Add($"Input Actions asset is missing control scheme '{schemeName}'.");
                return;
            }

            bool hasRequiredDevice = scheme.Value.deviceRequirements.Any(requirement =>
                !requirement.isOptional &&
                string.Equals(requirement.controlPath, requiredDevicePath, StringComparison.OrdinalIgnoreCase));
            if (!hasRequiredDevice)
            {
                errors.Add(
                    $"Control scheme '{schemeName}' must require device '{requiredDevicePath}'.");
            }
        }

        private static void ValidateTestSandboxes(ICollection<string> errors)
        {
            ValidateTestSandboxScene(
                TestSandboxScenePath,
                PrototypeCombatRoomDefinitionPath,
                true,
                false,
                errors);
            ValidateTestSandboxScene(
                TestSandboxLanesScenePath,
                PrototypeCombatLanesDefinitionPath,
                true,
                false,
                errors);
            ValidateTestSandboxScene(
                TestSandboxPillarsScenePath,
                PrototypeCombatPillarsDefinitionPath,
                true,
                false,
                errors);
            ValidateTestSandboxScene(
                TestSandboxArmorScenePath,
                PrototypeCombatArmorDefinitionPath,
                true,
                false,
                errors);
            ValidateTestSandboxScene(
                TestSandboxGatesScenePath,
                PrototypeCombatGatesDefinitionPath,
                true,
                false,
                errors);
            ValidateTestSandboxScene(
                DungeonStartScenePath,
                PrototypeCombatRoomDefinitionPath,
                false,
                false,
                errors);
            ValidateTestSandboxScene(
                DungeonRewardScenePath,
                PrototypeCombatRoomDefinitionPath,
                false,
                false,
                errors);
            ValidateTestSandboxScene(
                DungeonBossAnteScenePath,
                PrototypeCombatRoomDefinitionPath,
                false,
                false,
                errors);
            ValidateTestSandboxScene(
                DungeonRecoveryScenePath,
                PrototypeCombatRoomDefinitionPath,
                false,
                false,
                errors);
            ValidateTestSandboxScene(
                DungeonSecretScenePath,
                PrototypeCombatRoomDefinitionPath,
                false,
                false,
                errors);
            ValidateTestSandboxScene(
                DungeonBossScenePath,
                PrototypeBossArenaDefinitionPath,
                true,
                true,
                errors);
        }

        private static void ValidateStandaloneArmoredPlaytestScene(
            ICollection<string> errors)
        {
            ValidateStandaloneCombatPlaytestScene(
                ArmoredPanicPlaytestScenePath,
                PrototypeCombatArmorDefinitionPath,
                typeof(PrototypeArmoredPresenter),
                "Armor",
                false,
                errors);
        }

        private static void ValidateStandaloneSelfDestructPlaytestScene(
            ICollection<string> errors)
        {
            ValidateStandaloneCombatPlaytestScene(
                SelfDestructGatesPlaytestScenePath,
                PrototypeCombatGatesDefinitionPath,
                typeof(PrototypeSelfDestructPresenter),
                "Self-Destruct Gates",
                false,
                errors);
        }

        private static void ValidateStandaloneBossPlaytestScene(
            ICollection<string> errors)
        {
            ValidateStandaloneCombatPlaytestScene(
                BossBattlePlaytestScenePath,
                PrototypeBossArenaDefinitionPath,
                typeof(PrototypeBossPresenter),
                "Boss Battle",
                true,
                errors);
        }

        private static void ValidateStandaloneThrowerPlaytestScene(
            ICollection<string> errors)
        {
            ValidateStandaloneCombatPlaytestScene(
                ThrowerLanesPlaytestScenePath,
                PrototypeCombatThrowerDefinitionPath,
                typeof(PrototypeThrowerPresenter),
                "Thrower Lanes",
                false,
                errors);
        }

        private static void ValidateStandaloneCombatPlaytestScene(
            string scenePath,
            string expectedRoomPath,
            Type requiredPresenterType,
            string label,
            bool expectedBoss,
            ICollection<string> errors)
        {
            var sceneErrors = new List<string>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    scenePath) == null)
            {
                errors.Add(
                    $"{scenePath}: Missing standalone {label} playtest scene.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                TestSandboxContext[] contexts = FindComponents<TestSandboxContext>(scene);
                PrototypeGameSession[] sessions = FindComponents<PrototypeGameSession>(scene);
                int requiredPresenterCount = scene.GetRootGameObjects().Sum(root =>
                    root.GetComponentsInChildren(requiredPresenterType, true).Length);
                PrototypeRoomAdvanceController[] advanceControllers =
                    FindComponents<PrototypeRoomAdvanceController>(scene);
                int dungeonAdapterCount =
                    FindComponents<PrototypeDungeonRunHost>(scene).Length +
                    FindComponents<PrototypeDungeonRoomBinder>(scene).Length +
                    FindComponents<PrototypeDungeonMinimapPresenter>(scene).Length +
                    FindComponents<PrototypeDungeonDoorPresenter>(scene).Length +
                    FindComponents<PrototypeRunCompletionPresenter>(scene).Length;

                if (contexts.Length != 1 || sessions.Length != 1 ||
                    requiredPresenterCount != 1 || advanceControllers.Length != 1)
                {
                    sceneErrors.Add(
                        $"Standalone {label} playtest requires exactly one context, session, " +
                        "required enemy presenter, and no-op room advance controller.");
                }
                if (dungeonAdapterCount != 0)
                {
                    sceneErrors.Add(
                        $"Standalone {label} playtest must not contain dungeon host, binder, " +
                        "minimap, door presenter, or run-completion adapters.");
                }
                if (sessions.Length == 1 &&
                    (!sessions[0].IsCombatEnabledByDefault ||
                     sessions[0].IsBossEnabledByDefault != expectedBoss))
                {
                    sceneErrors.Add(
                        $"Standalone {label} playtest has an invalid combat/boss configuration.");
                }
                if (advanceControllers.Length == 1 &&
                    (!string.IsNullOrEmpty(advanceControllers[0].NextSceneName) ||
                     sessions.Length != 1 ||
                     advanceControllers[0].Session != sessions[0]))
                {
                    sceneErrors.Add(
                        $"Standalone {label} playtest room advance must reference its session " +
                        "and keep the next scene empty.");
                }
                if (contexts.Length == 1)
                {
                    ValidateRoomSceneBinding(
                        contexts[0],
                        expectedRoomPath,
                        sceneErrors);
                }
                if (!FindComponents<Camera>(scene).Any(camera =>
                        camera.enabled && camera.CompareTag("MainCamera")))
                {
                    sceneErrors.Add(
                        $"Standalone {label} playtest requires an enabled MainCamera.");
                }
            }
            finally
            {
                if (openedForValidation && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            foreach (string error in sceneErrors)
            {
                errors.Add($"{scenePath}: {error}");
            }

            if (EditorBuildSettings.scenes.Any(sceneSetting =>
                    sceneSetting.enabled && string.Equals(
                        sceneSetting.path,
                        scenePath,
                        StringComparison.Ordinal)))
            {
                errors.Add(
                    $"Standalone {label} playtest scene must stay outside the standard enabled Build Settings scenes.");
            }
        }

        private static void ValidateTestSandboxScene(
            string scenePath,
            string expectedRoomPath,
            bool expectedCombatEnabled,
            bool expectedBossEnabled,
            ICollection<string> errors)
        {
            var sceneErrors = new List<string>();
            ValidateTestSandboxSceneContents(
                scenePath,
                expectedRoomPath,
                expectedCombatEnabled,
                expectedBossEnabled,
                sceneErrors);
            foreach (string error in sceneErrors)
            {
                errors.Add($"{scenePath}: {error}");
            }
        }

        private static void ValidateTestSandboxSceneContents(
            string scenePath,
            string expectedRoomPath,
            bool expectedCombatEnabled,
            bool expectedBossEnabled,
            ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                errors.Add("Missing playtest room scene.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                TestSandboxContext[] contexts = FindComponents<TestSandboxContext>(scene);
                BombSwapInputReader[] readers = FindComponents<BombSwapInputReader>(scene);
                PrototypeGameSession[] sessions = FindComponents<PrototypeGameSession>(scene);
                PrototypePlayerController[] playerControllers =
                    FindComponents<PrototypePlayerController>(scene);
                PrototypeBombPresenter[] bombPresenters =
                    FindComponents<PrototypeBombPresenter>(scene);
                PrototypeDestructibleWallPresenter[] destructibleWallPresenters =
                    FindComponents<PrototypeDestructibleWallPresenter>(scene);
                PrototypePlayerHealthPresenter[] healthPresenters =
                    FindComponents<PrototypePlayerHealthPresenter>(scene);
                PrototypeChaserPresenter[] chaserPresenters =
                    FindComponents<PrototypeChaserPresenter>(scene);
                PrototypeChargerPresenter[] chargerPresenters =
                    FindComponents<PrototypeChargerPresenter>(scene);
                PrototypeArmoredPresenter[] armoredPresenters =
                    FindComponents<PrototypeArmoredPresenter>(scene);
                PrototypeSelfDestructPresenter[] selfDestructPresenters =
                    FindComponents<PrototypeSelfDestructPresenter>(scene);
                PrototypeBossPresenter[] bossPresenters =
                    FindComponents<PrototypeBossPresenter>(scene);
                PrototypeWeaponHud[] weaponHuds = FindComponents<PrototypeWeaponHud>(scene);
                PrototypeHealthHud[] healthHuds = FindComponents<PrototypeHealthHud>(scene);
                PrototypeInputHarnessProbe[] probes = FindComponents<PrototypeInputHarnessProbe>(scene);
                PrototypeRoomAdvanceController[] roomAdvanceControllers =
                    FindComponents<PrototypeRoomAdvanceController>(scene);
                PrototypeDungeonRunHost[] runHosts =
                    FindComponents<PrototypeDungeonRunHost>(scene);
                PrototypeDungeonRoomBinder[] roomBinders =
                    FindComponents<PrototypeDungeonRoomBinder>(scene);
                PrototypeDungeonMinimapPresenter[] minimapPresenters =
                    FindComponents<PrototypeDungeonMinimapPresenter>(scene);
                PrototypeDungeonDoorPresenter[] doorPresenters =
                    FindComponents<PrototypeDungeonDoorPresenter>(scene);
                PrototypeRunCompletionPresenter[] completionPresenters =
                    FindComponents<PrototypeRunCompletionPresenter>(scene);
                PrototypeBombRewardPresenter[] bombRewardPresenters =
                    FindComponents<PrototypeBombRewardPresenter>(scene);
                PrototypeRecoveryPickupPresenter[] recoveryPresenters =
                    FindComponents<PrototypeRecoveryPickupPresenter>(scene);
                PrototypeSecretRewardPresenter[] secretRewardPresenters =
                    FindComponents<PrototypeSecretRewardPresenter>(scene);
                Camera[] cameras = FindComponents<Camera>(scene);
                Light[] lights = FindComponents<Light>(scene);

                if (contexts.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one TestSandboxContext; found {contexts.Length}.");
                }
                if (readers.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one BombSwapInputReader; found {readers.Length}.");
                }
                if (sessions.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeGameSession; found {sessions.Length}.");
                }
                if (playerControllers.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypePlayerController; found {playerControllers.Length}.");
                }
                if (bombPresenters.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeBombPresenter; found {bombPresenters.Length}.");
                }
                if (destructibleWallPresenters.Length != 1)
                {
                    errors.Add(
                        "TestSandbox must contain exactly one " +
                        $"PrototypeDestructibleWallPresenter; found {destructibleWallPresenters.Length}.");
                }
                if (healthPresenters.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypePlayerHealthPresenter; found {healthPresenters.Length}.");
                }
                if (chaserPresenters.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeChaserPresenter; found {chaserPresenters.Length}.");
                }
                if (chargerPresenters.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeChargerPresenter; found {chargerPresenters.Length}.");
                }
                if (armoredPresenters.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeArmoredPresenter; found {armoredPresenters.Length}.");
                }
                if (selfDestructPresenters.Length != 1)
                {
                    errors.Add(
                        "TestSandbox must contain exactly one PrototypeSelfDestructPresenter; " +
                        $"found {selfDestructPresenters.Length}.");
                }
                if (bossPresenters.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeBossPresenter; found {bossPresenters.Length}.");
                }
                if (weaponHuds.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeWeaponHud; found {weaponHuds.Length}.");
                }
                if (healthHuds.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeHealthHud; found {healthHuds.Length}.");
                }
                if (minimapPresenters.Length != 1)
                {
                    errors.Add(
                        $"Dungeon room must contain exactly one PrototypeDungeonMinimapPresenter; found {minimapPresenters.Length}.");
                }
                if (probes.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one PrototypeInputHarnessProbe; found {probes.Length}.");
                }
                if (roomAdvanceControllers.Length != 0)
                {
                    errors.Add(
                        $"Dungeon room must not contain legacy PrototypeRoomAdvanceController; found {roomAdvanceControllers.Length}.");
                }
                if (runHosts.Length != 1)
                {
                    errors.Add(
                        $"Dungeon room must contain exactly one PrototypeDungeonRunHost; found {runHosts.Length}.");
                }
                if (roomBinders.Length != 1)
                {
                    errors.Add(
                        $"Dungeon room must contain exactly one PrototypeDungeonRoomBinder; found {roomBinders.Length}.");
                }
                if (doorPresenters.Length != 1)
                {
                    errors.Add(
                        $"Dungeon room must contain exactly one PrototypeDungeonDoorPresenter; found {doorPresenters.Length}.");
                }
                if (completionPresenters.Length != 1)
                {
                    errors.Add(
                        "Dungeon room must contain exactly one " +
                        $"PrototypeRunCompletionPresenter; found {completionPresenters.Length}.");
                }
                int expectedBombRewardPresenterCount = string.Equals(
                    scenePath,
                    DungeonRewardScenePath,
                    StringComparison.Ordinal) ? 1 : 0;
                if (bombRewardPresenters.Length != expectedBombRewardPresenterCount)
                {
                    errors.Add(
                        $"Dungeon room must contain {expectedBombRewardPresenterCount} " +
                        "PrototypeBombRewardPresenter component(s); found " +
                        $"{bombRewardPresenters.Length}.");
                }
                int expectedRecoveryPresenterCount = string.Equals(
                    scenePath,
                    DungeonRecoveryScenePath,
                    StringComparison.Ordinal) ? 1 : 0;
                if (recoveryPresenters.Length != expectedRecoveryPresenterCount)
                {
                    errors.Add(
                        $"Dungeon room must contain {expectedRecoveryPresenterCount} " +
                        "PrototypeRecoveryPickupPresenter component(s); found " +
                        $"{recoveryPresenters.Length}.");
                }
                int expectedSecretRewardPresenterCount = string.Equals(
                    scenePath,
                    DungeonSecretScenePath,
                    StringComparison.Ordinal) ? 1 : 0;
                if (secretRewardPresenters.Length != expectedSecretRewardPresenterCount)
                {
                    errors.Add(
                        $"Dungeon room must contain {expectedSecretRewardPresenterCount} " +
                        "PrototypeSecretRewardPresenter component(s); found " +
                        $"{secretRewardPresenters.Length}.");
                }
                if (!cameras.Any(camera => camera.enabled && camera.CompareTag("MainCamera")))
                {
                    errors.Add("TestSandbox requires an enabled MainCamera.");
                }
                if (!lights.Any(light => light.enabled && light.type == LightType.Directional))
                {
                    errors.Add("TestSandbox requires an enabled directional light.");
                }

                if (readers.Length == 1)
                {
                    string readerAssetPath = AssetDatabase.GetAssetPath(readers[0].InputActions);
                    if (!string.Equals(readerAssetPath, InputActionsPath, StringComparison.Ordinal))
                    {
                        errors.Add(
                            $"TestSandbox input reader must reference '{InputActionsPath}', found '{readerAssetPath}'.");
                    }
                }

                if (sessions.Length == 1 && contexts.Length == 1 && readers.Length == 1)
                {
                    PrototypeGameSession session = sessions[0];
                    string loadoutPath = AssetDatabase.GetAssetPath(session.BombLoadout);
                    string playerVitalsPath = AssetDatabase.GetAssetPath(session.PlayerVitals);
                    string chaserDefinitionPath = AssetDatabase.GetAssetPath(
                        session.ChaserDefinition);
                    string chargerDefinitionPath = AssetDatabase.GetAssetPath(
                        session.ChargerDefinition);
                    string armoredDefinitionPath = AssetDatabase.GetAssetPath(
                        session.ArmoredDefinition);
                    string selfDestructDefinitionPath = AssetDatabase.GetAssetPath(
                        session.SelfDestructDefinition);
                    string bossDefinitionPath = AssetDatabase.GetAssetPath(
                        session.BossDefinition);
                    if (session.Context != contexts[0] || session.InputReader != readers[0] ||
                        !string.Equals(
                            loadoutPath,
                            PrototypeBombLoadoutPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            playerVitalsPath,
                            PrototypePlayerVitalsPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            chaserDefinitionPath,
                            PrototypeChaserDefinitionPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            chargerDefinitionPath,
                            PrototypeChargerDefinitionPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            armoredDefinitionPath,
                            PrototypeArmoredDefinitionPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            selfDestructDefinitionPath,
                            PrototypeSelfDestructDefinitionPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            bossDefinitionPath,
                            PrototypeBossDefinitionPath,
                            StringComparison.Ordinal))
                    {
                        errors.Add("TestSandbox game session has inconsistent runtime references.");
                    }
                    if (!IsFinitePositive(session.CellsPerSecond) ||
                        !IsFinitePositive(session.ChainDelaySeconds))
                    {
                        errors.Add("TestSandbox game session timing values must be finite and positive.");
                    }
                    bool expectedChaserEnabled =
                        expectedCombatEnabled && !expectedBossEnabled;
                    if (session.IsCombatEnabledByDefault != expectedCombatEnabled ||
                        session.IsBossEnabledByDefault != expectedBossEnabled ||
                        session.HasChaser != expectedChaserEnabled ||
                        session.HasBoss != expectedBossEnabled)
                    {
                        errors.Add(
                            "Dungeon room encounter mode is inconsistent with its room type.");
                    }
                }

                if (playerControllers.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypePlayerController controller = playerControllers[0];
                    if (controller.Session != sessions[0] ||
                        controller.PlayerTransform != contexts[0].PlayerPlaceholder)
                    {
                        errors.Add("TestSandbox player controller has inconsistent scene references.");
                    }
                    if (float.IsNaN(controller.CellsPerSecond) ||
                        float.IsInfinity(controller.CellsPerSecond) ||
                        controller.CellsPerSecond <= 0f)
                    {
                        errors.Add("TestSandbox player controller speed must be finite and positive.");
                    }
                }

                if (bombPresenters.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypeBombPresenter presenter = bombPresenters[0];
                    if (presenter.Session != sessions[0] || presenter.PresentationRoot == null ||
                        !presenter.PresentationRoot.IsChildOf(contexts[0].GridRoot))
                    {
                        errors.Add("TestSandbox bomb presenter has inconsistent scene references.");
                    }
                    if (presenter.BombPoolSize < 0 || presenter.ExplosionPoolSize < 0)
                    {
                        errors.Add("TestSandbox bomb presenter pool sizes cannot be negative.");
                    }
                }

                if (destructibleWallPresenters.Length == 1 &&
                    sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypeDestructibleWallPresenter presenter =
                        destructibleWallPresenters[0];
                    Transform expectedRoot =
                        contexts[0].GridRoot.Find("Environment/DestructibleObstacles");
                    if (presenter.Session != sessions[0] || presenter.WallRoot != expectedRoot)
                    {
                        errors.Add(
                            "TestSandbox destructible-wall presenter has inconsistent scene references.");
                    }
                }

                if (healthPresenters.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypePlayerHealthPresenter presenter = healthPresenters[0];
                    Renderer playerRenderer =
                        contexts[0].PlayerPlaceholder.GetComponentInChildren<Renderer>();
                    if (presenter.Session != sessions[0] ||
                        presenter.TargetRenderer != playerRenderer ||
                        !IsFinitePositive(presenter.DamagePulseSeconds))
                    {
                        errors.Add(
                            "TestSandbox player health presenter has inconsistent scene references or timing.");
                    }
                }

                if (chaserPresenters.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypeChaserPresenter presenter = chaserPresenters[0];
                    Transform runtimePresentation = contexts[0].GridRoot.Find("RuntimePresentation");
                    if (presenter.Session != sessions[0] ||
                        presenter.PresentationRoot != runtimePresentation)
                    {
                        errors.Add("TestSandbox chaser presenter has inconsistent scene references.");
                    }
                }

                if (chargerPresenters.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypeChargerPresenter presenter = chargerPresenters[0];
                    Transform runtimePresentation = contexts[0].GridRoot.Find("RuntimePresentation");
                    if (presenter.Session != sessions[0] ||
                        presenter.PresentationRoot != runtimePresentation)
                    {
                        errors.Add("TestSandbox charger presenter has inconsistent scene references.");
                    }
                }

                if (armoredPresenters.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypeArmoredPresenter presenter = armoredPresenters[0];
                    Transform runtimePresentation = contexts[0].GridRoot.Find("RuntimePresentation");
                    if (presenter.Session != sessions[0] ||
                        presenter.PresentationRoot != runtimePresentation)
                    {
                        errors.Add("TestSandbox armored presenter has inconsistent scene references.");
                    }
                }

                if (selfDestructPresenters.Length == 1 && sessions.Length == 1 &&
                    contexts.Length == 1)
                {
                    PrototypeSelfDestructPresenter presenter = selfDestructPresenters[0];
                    Transform runtimePresentation =
                        contexts[0].GridRoot.Find("RuntimePresentation");
                    if (presenter.Session != sessions[0] ||
                        presenter.PresentationRoot != runtimePresentation)
                    {
                        errors.Add(
                            "TestSandbox self-destruct presenter has inconsistent scene references.");
                    }
                }

                if (bossPresenters.Length == 1 && sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypeBossPresenter presenter = bossPresenters[0];
                    Transform runtimePresentation = contexts[0].GridRoot.Find("RuntimePresentation");
                    if (presenter.Session != sessions[0] ||
                        presenter.PresentationRoot != runtimePresentation)
                    {
                        errors.Add("TestSandbox boss presenter has inconsistent scene references.");
                    }
                }

                if (weaponHuds.Length == 1 && sessions.Length == 1 &&
                    weaponHuds[0].Session != sessions[0])
                {
                    errors.Add("TestSandbox weapon HUD has an inconsistent session reference.");
                }

                if (healthHuds.Length == 1 && sessions.Length == 1 &&
                    healthHuds[0].Session != sessions[0])
                {
                    errors.Add("TestSandbox health HUD has an inconsistent session reference.");
                }

                if (minimapPresenters.Length == 1 && roomBinders.Length == 1 &&
                    minimapPresenters[0].RoomBinder != roomBinders[0])
                {
                    errors.Add(
                        "Dungeon minimap presenter has an inconsistent room binder reference.");
                }

                if (probes.Length == 1 && readers.Length == 1 && sessions.Length == 1 &&
                    (probes[0].InputReader != readers[0] ||
                     probes[0].Session != sessions[0]))
                {
                    errors.Add("TestSandbox harness probe has inconsistent runtime references.");
                }

                if (runHosts.Length == 1)
                {
                    PrototypeDungeonRunHost host = runHosts[0];
                    PrototypeDungeonCombatRoomCatalogAsset expectedCombatCatalog =
                        AssetDatabase.LoadAssetAtPath<
                            PrototypeDungeonCombatRoomCatalogAsset>(
                            PrototypeDungeonCombatRoomCatalogPath);
                    PrototypeDungeonSpecialRoomCatalogAsset expectedSpecialCatalog =
                        AssetDatabase.LoadAssetAtPath<
                            PrototypeDungeonSpecialRoomCatalogAsset>(
                            PrototypeDungeonSpecialRoomCatalogPath);
                    PrototypeBombRewardCatalogAsset expectedRewardCatalog =
                        AssetDatabase.LoadAssetAtPath<
                            PrototypeBombRewardCatalogAsset>(
                            PrototypeBombRewardCatalogPath);
                    PrototypePlayerVitalsAsset expectedPlayerVitals =
                        AssetDatabase.LoadAssetAtPath<PrototypePlayerVitalsAsset>(
                            PrototypePlayerVitalsPath);
                    if (host.transform.parent != null || host.Seed != 0 ||
                        host.CombatRoomCatalog != expectedCombatCatalog ||
                        host.SpecialRoomCatalog != expectedSpecialCatalog ||
                        host.BombRewardCatalog != expectedRewardCatalog ||
                        host.PlayerVitals != expectedPlayerVitals ||
                        !host.RequireInitialSceneMatch)
                    {
                        errors.Add(
                            "Dungeon run host must be a seed-0 root using the validated room, bomb-reward, and player-vitals assets with initial-scene matching.");
                    }
                }

                if (roomBinders.Length == 1 && sessions.Length == 1 &&
                    doorPresenters.Length == 1 && contexts.Length == 1)
                {
                    PrototypeDungeonRoomBinder binder = roomBinders[0];
                    if (binder.RoomSession != sessions[0] ||
                        binder.DoorPresenter != doorPresenters[0] ||
                        binder.transform != sessions[0].transform ||
                        binder.RoomSession.Context != contexts[0] ||
                        binder.GridRoot != contexts[0].GridRoot)
                    {
                        errors.Add(
                            "Dungeon room binder has inconsistent session, presenter, or grid references.");
                    }
                }

                if (bombRewardPresenters.Length == 1 && roomBinders.Length == 1 &&
                    bombRewardPresenters[0].RoomBinder != roomBinders[0])
                {
                    errors.Add(
                        "Bomb reward presenter has an inconsistent dungeon room binder reference.");
                }

                if (recoveryPresenters.Length == 1 && roomBinders.Length == 1)
                {
                    PrototypeRecoveryPickupPresenter presenter =
                        recoveryPresenters[0];
                    Material expectedPickupMaterial =
                        AssetDatabase.LoadAssetAtPath<Material>(
                            RecoveryPickupMaterialPath);
                    if (presenter.RoomBinder != roomBinders[0] ||
                        presenter.RecoveryAmount !=
                            PrototypeRecoveryPickupPresenter.DefaultRecoveryAmount ||
                        presenter.PickupCell != Vector2Int.zero ||
                        presenter.PickupMaterial != expectedPickupMaterial)
                    {
                        errors.Add(
                            "Recovery pickup presenter has inconsistent binder, material, amount, or cell configuration.");
                    }
                }

                if (secretRewardPresenters.Length == 1 && roomBinders.Length == 1)
                {
                    PrototypeSecretRewardPresenter presenter =
                        secretRewardPresenters[0];
                    Material expectedRewardMaterial =
                        AssetDatabase.LoadAssetAtPath<Material>(
                            SecretRewardMaterialPath);
                    if (presenter.RoomBinder != roomBinders[0] ||
                        presenter.TokenReward !=
                            PrototypeSecretRewardPresenter.DefaultTokenReward ||
                        presenter.PickupCell != Vector2Int.zero ||
                        presenter.PickupMaterial != expectedRewardMaterial)
                    {
                        errors.Add(
                            "Secret reward presenter has inconsistent binder, material, amount, or cell configuration.");
                    }
                }

                if (completionPresenters.Length == 1 && roomBinders.Length == 1 &&
                    readers.Length == 1 &&
                    (completionPresenters[0].RoomBinder != roomBinders[0] ||
                     completionPresenters[0].InputReader != readers[0]))
                {
                    errors.Add(
                        "Run completion presenter has inconsistent dungeon room or input references.");
                }

                if (doorPresenters.Length == 1 && contexts.Length == 1)
                {
                    ValidateDungeonDoors(
                        doorPresenters[0],
                        contexts[0],
                        errors);
                }

                if (contexts.Length == 1)
                {
                    TestSandboxContext context = contexts[0];
                    if (context.InputReader == null || context.GridRoot == null ||
                        context.PlayerSpawn == null || context.PlayerPlaceholder == null ||
                        context.ChaserSpawn == null || context.RoomDefinition == null)
                    {
                        errors.Add("TestSandboxContext has missing required references.");
                    }
                    ValidateRoomSceneBinding(context, expectedRoomPath, errors);
                }
            }
            finally
            {
                if (openedForValidation && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateDungeonDoors(
            PrototypeDungeonDoorPresenter presenter,
            TestSandboxContext context,
            ICollection<string> errors)
        {
            if (!presenter.IsConfigured || context.GridRoot == null)
            {
                errors.Add("Dungeon door presenter is missing one or more door renderers.");
                return;
            }

            Renderer[] doors =
            {
                presenter.NorthDoor,
                presenter.EastDoor,
                presenter.SouthDoor,
                presenter.WestDoor,
            };
            GameObject[] secretCrackRoots =
            {
                presenter.NorthSecretCracks,
                presenter.EastSecretCracks,
                presenter.SouthSecretCracks,
                presenter.WestSecretCracks,
            };
            string[] expectedNames =
            {
                "NorthDoor",
                "EastDoor",
                "SouthDoor",
                "WestDoor",
            };
            string[] expectedCrackNames =
            {
                "NorthSecretCracks",
                "EastSecretCracks",
                "SouthSecretCracks",
                "WestSecretCracks",
            };
            if (new HashSet<Renderer>(doors).Count != doors.Length)
            {
                errors.Add("Dungeon door presenter requires four distinct renderers.");
            }

            Transform boundary = context.GridRoot.Find("Environment/BoundaryWalls");
            if (boundary == null || boundary.childCount != 16)
            {
                errors.Add(
                    "Dungeon boundary must contain eight split walls, four door panels, and four secret-crack roots.");
                return;
            }
            for (int index = 0; index < doors.Length; index++)
            {
                Renderer door = doors[index];
                if (door == null || door.transform.parent != boundary ||
                    !string.Equals(
                        door.gameObject.name,
                        expectedNames[index],
                        StringComparison.Ordinal) ||
                    door.GetComponent<Collider>() != null)
                {
                    errors.Add(
                        $"Dungeon {expectedNames[index]} must be a collider-free panel under BoundaryWalls.");
                }
            }

            Material expectedCrackMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(SecretCrackMaterialPath);
            Material expectedWallMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(DestructibleWallMaterialPath);
            if (new HashSet<GameObject>(secretCrackRoots).Count !=
                secretCrackRoots.Length)
            {
                errors.Add(
                    "Dungeon door presenter requires four distinct secret-crack roots.");
            }
            for (int index = 0; index < secretCrackRoots.Length; index++)
            {
                GameObject root = secretCrackRoots[index];
                Renderer[] renderers = root != null
                    ? root.GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();
                Transform surface = root != null
                    ? root.transform.Find("SecretWallSurface")
                    : null;
                Renderer surfaceRenderer = surface != null
                    ? surface.GetComponent<Renderer>()
                    : null;
                Renderer[] crackBars = renderers
                    .Where(renderer => renderer != surfaceRenderer)
                    .ToArray();
                if (root == null || root.transform.parent != boundary ||
                    !string.Equals(
                        root.name,
                        expectedCrackNames[index],
                        StringComparison.Ordinal) ||
                    root.activeSelf || root.transform.childCount != 4 ||
                    renderers.Length != 4 ||
                    Vector3.SqrMagnitude(
                        root.transform.position - doors[index].transform.position) >
                        0.000001f ||
                    surfaceRenderer == null ||
                    surfaceRenderer.sharedMaterial != expectedWallMaterial ||
                    surfaceRenderer.GetComponent<Collider>() != null ||
                    crackBars.Length != 3 ||
                    crackBars.Any(bar =>
                        bar.sharedMaterial != expectedCrackMaterial ||
                        bar.GetComponent<Collider>() != null))
                {
                    errors.Add(
                        $"Dungeon {expectedCrackNames[index]} must be an inactive " +
                        $"collider-free secret-door visual at the matching " +
                        $"{expectedNames[index]} position, using " +
                        "one destructible-wall surface and three secret-crack bars.");
                }
            }

            string[] splitWallNames =
            {
                "NorthWallWest", "NorthWallEast",
                "SouthWallWest", "SouthWallEast",
                "EastWallSouth", "EastWallNorth",
                "WestWallSouth", "WestWallNorth",
            };
            for (int index = 0; index < splitWallNames.Length; index++)
            {
                Transform wall = boundary.Find(splitWallNames[index]);
                if (wall == null || wall.GetComponent<Renderer>() == null ||
                    wall.GetComponent<Collider>() == null)
                {
                    errors.Add(
                        $"Dungeon boundary wall '{splitWallNames[index]}' is missing its renderer or collider.");
                }
            }
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool Approximately(Color first, Color second)
        {
            const float tolerance = 0.001f;
            return Mathf.Abs(first.r - second.r) <= tolerance &&
                Mathf.Abs(first.g - second.g) <= tolerance &&
                Mathf.Abs(first.b - second.b) <= tolerance &&
                Mathf.Abs(first.a - second.a) <= tolerance;
        }

        private static void ValidateRoomSceneBinding(
            TestSandboxContext context,
            string expectedRoomPath,
            ICollection<string> errors)
        {
            if (context.RoomDefinition == null || context.GridRoot == null)
            {
                return;
            }

            string roomPath = AssetDatabase.GetAssetPath(context.RoomDefinition);
            if (!string.Equals(
                    roomPath,
                    expectedRoomPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"TestSandbox room authority must reference '{expectedRoomPath}', found '{roomPath}'.");
                return;
            }

            CombatRoomDefinition room;
            try
            {
                room = context.RoomDefinition.CreateCoreDefinition();
            }
            catch (Exception exception)
            {
                errors.Add($"TestSandbox room authority is invalid: {exception.Message}");
                return;
            }

            if (context.GridWidth != room.Width || context.GridDepth != room.Depth ||
                !IsFinitePositive(context.CellSize))
            {
                errors.Add("TestSandbox grid values must derive from its room authority.");
            }

            ValidateTransformCell(context, context.PlayerSpawn, room.PlayerSpawn, "player spawn", errors);
            ValidateTransformCell(
                context,
                context.PlayerPlaceholder,
                room.PlayerSpawn,
                "player placeholder",
                errors);
            ValidateTransformCell(context, context.ChaserSpawn, room.ChaserSpawn, "chaser spawn", errors);
            if (room.ChargerSpawn.HasValue)
            {
                if (context.ChargerSpawn == null)
                {
                    errors.Add("TestSandbox is missing the authored charger spawn Transform.");
                }
                else
                {
                    ValidateTransformCell(
                        context,
                        context.ChargerSpawn,
                        room.ChargerSpawn.Value,
                        "charger spawn",
                        errors);
                }
            }
            else if (context.ChargerSpawn != null)
            {
                errors.Add("TestSandbox has a charger spawn Transform without an authored charger cell.");
            }
            if (room.ArmoredSpawn.HasValue)
            {
                if (context.ArmoredSpawn == null)
                {
                    errors.Add("TestSandbox is missing the authored armored spawn Transform.");
                }
                else
                {
                    ValidateTransformCell(
                        context,
                        context.ArmoredSpawn,
                        room.ArmoredSpawn.Value,
                        "armored spawn",
                        errors);
                }
            }
            else if (context.ArmoredSpawn != null)
            {
                errors.Add("TestSandbox has an armored spawn Transform without an authored armored cell.");
            }
            if (room.SelfDestructSpawn.HasValue)
            {
                if (context.SelfDestructSpawn == null)
                {
                    errors.Add(
                        "TestSandbox is missing the authored self-destruct spawn Transform.");
                }
                else
                {
                    ValidateTransformCell(
                        context,
                        context.SelfDestructSpawn,
                        room.SelfDestructSpawn.Value,
                        "self-destruct spawn",
                        errors);
                }
            }
            else if (context.SelfDestructSpawn != null)
            {
                errors.Add(
                    "TestSandbox has a self-destruct spawn Transform without an authored cell.");
            }

            Transform obstacles = context.GridRoot.Find("Environment/InteriorObstacles");
            if (obstacles == null)
            {
                errors.Add("TestSandbox is missing Environment/InteriorObstacles.");
                return;
            }

            var authoredWalls = new HashSet<GridPosition>(room.IndestructibleWalls);
            var seenWalls = new HashSet<GridPosition>();
            for (int index = 0; index < obstacles.childCount; index++)
            {
                Transform obstacle = obstacles.GetChild(index);
                GridPosition cell = context.GridSpace.WorldToGrid(obstacle.position);
                if (!seenWalls.Add(cell))
                {
                    errors.Add($"TestSandbox has duplicate obstacle visuals at {cell}.");
                }
                if (!authoredWalls.Contains(cell))
                {
                    errors.Add($"TestSandbox obstacle visual {obstacle.name} is not authored at {cell}.");
                }
            }

            foreach (GridPosition wall in authoredWalls)
            {
                if (!seenWalls.Contains(wall))
                {
                    errors.Add($"TestSandbox is missing an obstacle visual for authored wall {wall}.");
                }
            }


            Transform destructibleObstacles =
                context.GridRoot.Find("Environment/DestructibleObstacles");
            if (destructibleObstacles == null)
            {
                errors.Add("TestSandbox is missing Environment/DestructibleObstacles.");
                return;
            }

            Material destructibleMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                DestructibleWallMaterialPath);
            var authoredDestructibleWalls =
                new HashSet<GridPosition>(room.DestructibleWalls);
            var seenDestructibleWalls = new HashSet<GridPosition>();
            for (int index = 0; index < destructibleObstacles.childCount; index++)
            {
                Transform obstacle = destructibleObstacles.GetChild(index);
                GridPosition cell = context.GridSpace.WorldToGrid(obstacle.position);
                if (!seenDestructibleWalls.Add(cell))
                {
                    errors.Add($"TestSandbox has duplicate destructible visuals at {cell}.");
                }
                if (!authoredDestructibleWalls.Contains(cell))
                {
                    errors.Add(
                        $"TestSandbox destructible visual {obstacle.name} is not authored at {cell}.");
                }

                Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != 4 || destructibleMaterial == null ||
                    renderers.Any(renderer => renderer.sharedMaterial != destructibleMaterial))
                {
                    errors.Add(
                        $"TestSandbox destructible visual {obstacle.name} must use four segmented blocks and the validated material.");
                }
                if (obstacle.GetComponentsInChildren<Collider>(true).Length != 0)
                {
                    errors.Add(
                        $"TestSandbox destructible visual {obstacle.name} must not own logical colliders.");
                }
            }

            foreach (GridPosition wall in authoredDestructibleWalls)
            {
                if (!seenDestructibleWalls.Contains(wall))
                {
                    errors.Add(
                        $"TestSandbox is missing a destructible visual for authored wall {wall}.");
                }
            }
        }

        private static void ValidateTransformCell(
            TestSandboxContext context,
            Transform target,
            GridPosition expected,
            string label,
            ICollection<string> errors)
        {
            if (target == null)
            {
                return;
            }

            GridPosition actual = context.GridSpace.WorldToGrid(target.position);
            if (actual != expected)
            {
                errors.Add($"TestSandbox {label} cell is {actual}; authored room requires {expected}.");
            }
        }

        private static void ValidateBuildSettings(ICollection<string> errors)
        {
            string[] expectedScenePaths =
            {
                DungeonStartScenePath,
                DungeonRewardScenePath,
                DungeonBossAnteScenePath,
                DungeonRecoveryScenePath,
                DungeonSecretScenePath,
                DungeonBossScenePath,
                TestSandboxScenePath,
                TestSandboxLanesScenePath,
                TestSandboxPillarsScenePath,
                TestSandboxArmorScenePath,
                TestSandboxGatesScenePath,
            };
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();
            if (enabledScenes.Length < expectedScenePaths.Length)
            {
                errors.Add(
                    "Build Settings must enable the Start placeholder first, followed by every dungeon room scene.");
                return;
            }

            for (int index = 0; index < expectedScenePaths.Length; index++)
            {
                if (!string.Equals(
                        enabledScenes[index].path,
                        expectedScenePaths[index],
                        StringComparison.Ordinal))
                {
                    errors.Add(
                        $"Build Settings scene {index} must be '{expectedScenePaths[index]}', found '{enabledScenes[index].path}'.");
                }
            }
        }

        private static T[] FindComponents<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
