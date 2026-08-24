using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using BombSwap.Editor.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeContentValidator
    {
        private static readonly string[] PrivateVendorAssetRoots =
        {
            "Assets/ThirdParty/",
            "Assets/Feel/",
            "Assets/Plugins/Demigiant/DOTweenPro/",
            "Assets/Arts/VFX/",
        };

        public const string LobbyScenePath =
            "Assets/Game/Scenes/Lobby/DungeonLobby.unity";
        public const string GameFontAssetPath =
            "Assets/TextMesh Pro/Fonts/DungGeunMo.asset";
        public const string TmpSettingsAssetPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string InputActionsPath = "Assets/Game/Content/Input/BombSwapInputActions.inputactions";
        public const string AudioMixerPath =
            "Assets/Game/Content/Audio/BombSwapAudioMixer.mixer";
        public const string BgmCatalogPath =
            "Assets/Game/Content/Audio/PrototypeBgmCatalog.asset";
        public const string TestSandboxScenePath = "Assets/Game/Scenes/TestSandbox/TestSandbox.unity";
        public const string TestSandboxLanesScenePath =
            "Assets/Game/Scenes/TestSandbox/TestSandboxLanes.unity";
        public const string TestSandboxThrowerScenePath =
            "Assets/Game/Scenes/TestSandbox/TestSandboxThrower.unity";
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

        public static readonly string[] BgmScenePaths =
        {
            LobbyScenePath,
            DungeonStartScenePath,
            DungeonRewardScenePath,
            DungeonBossAnteScenePath,
            DungeonRecoveryScenePath,
            DungeonSecretScenePath,
            DungeonBossScenePath,
            TestSandboxScenePath,
            TestSandboxLanesScenePath,
            TestSandboxThrowerScenePath,
            TestSandboxPillarsScenePath,
            TestSandboxArmorScenePath,
            TestSandboxGatesScenePath,
            ArmoredPanicPlaytestScenePath,
            SelfDestructGatesPlaytestScenePath,
            BossBattlePlaytestScenePath,
            ThrowerLanesPlaytestScenePath,
        };

        public static bool IsDungeonPresentationScenePath(string scenePath)
        {
            return string.Equals(scenePath, DungeonStartScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, DungeonRewardScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, DungeonBossAnteScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, DungeonRecoveryScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, DungeonSecretScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, DungeonBossScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, TestSandboxScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, TestSandboxThrowerScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, TestSandboxPillarsScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, TestSandboxArmorScenePath, StringComparison.Ordinal) ||
                string.Equals(scenePath, TestSandboxGatesScenePath, StringComparison.Ordinal);
        }
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
        public const string PlayerPrefabPath =
            "Assets/Game/Content/Prefabs/Player/PlayerDuck.prefab";
        public const string PlayerAnimatorControllerPath =
            "Assets/Arts/Character/PlayerDuck/Animations/AC_Player_Duck.controller";
        public const string PlayerUpperBodyMaskPath =
            "Assets/Arts/Character/PlayerDuck/Animations/AM_Player_UpperBody.mask";
        public const string PlayerPlaceBombAnimationPath =
            "Assets/Arts/Character/PlayerDuck/Animations/put-down-bomb.fbx";
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
            "Assets/Game/Content/Prefabs/Bomb/Player/NormalBomb.prefab";
        public const string ExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ExplosionCellPlaceholder.prefab";
        public const string AreaBombPrefabPath =
            "Assets/Game/Content/Prefabs/Bomb/Player/RangeBomb.prefab";
        public const string AreaExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/AreaExplosionCellPlaceholder.prefab";
        public const string LineBombPrefabPath =
            "Assets/Game/Content/Prefabs/Bomb/Player/StraightBomb.prefab";
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
            "Assets/Game/Content/Prefabs/Enemies/ChaserPig.prefab";
        public const string ChaserAnimatorControllerPath =
            "Assets/Arts/Character/Pig/Chaser/Animations/normal-pig-rigged.controller";
        public const string ChaserIdleClipPath =
            "Assets/Arts/Character/Pig/Chaser/Animations/normal-pig-idle.fbx";
        public const string ChaserRunClipPath =
            "Assets/Arts/Character/Pig/Chaser/Animations/normal-pig-run.fbx";
        public const string ChaserAttackClipPath =
            "Assets/Arts/Character/Pig/Chaser/Animations/normal-pig-attack.fbx";
        public const string ChaserDieClipPath =
            "Assets/Arts/Character/Pig/Chaser/Animations/normal-pig-die.fbx";
        public const string ChargerPrefabPath =
            "Assets/Game/Content/Prefabs/Enemies/ChargerPig.prefab";
        public const string ChargerAnimatorControllerPath =
            "Assets/Arts/Character/Pig/Charger/Animations/AC_Charger_Pig.controller";
        public const string ChargerIdleClipPath =
            "Assets/Arts/Character/Pig/Charger/Animations/charger-pig-idle.fbx";
        public const string ChargerRunClipPath =
            "Assets/Arts/Character/Pig/Charger/Animations/charger-pig-walk.fbx";
        public const string ChargerTelegraphClipPath =
            "Assets/Arts/Character/Pig/Charger/Animations/charger-pig-telegraph.fbx";
        public const string ChargerChargeClipPath =
            "Assets/Arts/Character/Pig/Charger/Animations/charger-pig-charge.fbx";
        public const string ChargerRecoverClipPath =
            "Assets/Arts/Character/Pig/Charger/Animations/charger-pig-recover.fbx";
        public const string ChargerDieClipPath =
            "Assets/Arts/Character/Pig/Charger/Animations/charger-pig-die.fbx";
        public const string ChargerTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChargerTelegraphCellPlaceholder.prefab";
        public const string ArmoredPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ArmoredPlaceholder.prefab";
        public const string ArmoredPanicTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ArmoredPanicTelegraphCellPlaceholder.prefab";
        public const string SelfDestructPrefabPath =
            "Assets/Game/Content/Prefabs/Enemies/SelfDestructPig.prefab";
        public const string SelfDestructAnimatorControllerPath =
            "Assets/Arts/Character/Pig/SelfDestruct/Animations/AC_SelfDestruct_Pig.controller";
        public const string SelfDestructIdleClipPath =
            "Assets/Arts/Character/Pig/SelfDestruct/Animations/selfdestruct-pig-idle.fbx";
        public const string SelfDestructRunClipPath =
            "Assets/Arts/Character/Pig/SelfDestruct/Animations/selfdestruct-pig-run.fbx";
        public const string SelfDestructTelegraphClipPath =
            "Assets/Arts/Character/Pig/SelfDestruct/Animations/selfdestruct-pig-telegraph.fbx";
        public const string SelfDestructDetonateClipPath =
            "Assets/Arts/Character/Pig/SelfDestruct/Animations/selfdestruct-pig-detonate.fbx";
        public const string SelfDestructTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/SelfDestructTelegraphCellPlaceholder.prefab";
        public const string ThrowerPrefabPath =
            "Assets/Game/Content/Prefabs/Enemies/ThrowerPig.prefab";
        public const string ThrowerAnimatorControllerPath =
            "Assets/Arts/Character/Pig/Thrower/Animations/AC_Thrower_Pig.controller";
        public const string ThrowerIdleClipPath =
            "Assets/Arts/Character/Pig/Thrower/Animations/thrower-pig-idle.fbx";
        public const string ThrowerWalkClipPath =
            "Assets/Arts/Character/Pig/Thrower/Animations/thrower-pig-walk.fbx";
        public const string ThrowerThrowClipPath =
            "Assets/Arts/Character/Pig/Thrower/Animations/thrower-pig-throw.fbx";
        public const string ThrowerDieClipPath =
            "Assets/Arts/Character/Pig/Thrower/Animations/thrower-pig-die.fbx";
        public const string ThrowerTelegraphCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ThrowerTelegraphCellPlaceholder.prefab";
        public const string BossPrefabPath =
            "Assets/Game/Content/Prefabs/Enemies/BossPig.prefab";
        public const string BossAnimatorControllerPath =
            "Assets/Arts/Character/Pig/Boss/Animations/AC_Boss_Pig.controller";
        public const string BossIdleClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-idle.fbx";
        public const string BossWalkClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-walk.fbx";
        public const string BossTelegraphClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-telegraph.fbx";
        public const string BossChargeClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-charge.fbx";
        public const string BossSummonClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-summon.fbx";
        public const string BossThrowLeftClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-throw-left.fbx";
        public const string BossThrowRightClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-throw-right.fbx";
        public const string BossDieClipPath =
            "Assets/Arts/Character/Pig/Boss/Animations/boss-pig-die.fbx";
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

            ValidateGameFont(errors);
            PixelFontStyleAuthoring.Validate(errors);
            ValidateAudioMixer(errors);
            ValidateBgmContent(errors);
            ValidateInGameUiPrefabs(errors);
            ValidateLobbyScene(errors);
            PrototypeThirdPartyAssetAuthoring.ValidatePublicDependencies(errors);
            PrototypeThirdPartyAssetAuthoring.ValidateOptionalUiBindings(errors);
            PrototypeThirdPartyAssetAuthoring.ValidateNoObsoleteVendorDefines(
                errors);
            ValidateInputActions(errors);
            ValidatePrototypeBombDefinitions(errors);
            ValidatePrototypePlayerVitals(errors);
            ValidatePlayerPrefab(errors);
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
            ValidateStandaloneLegacyLanesPlaytestScene(errors);
            ValidateStandaloneArmoredPlaytestScene(errors);
            ValidateStandaloneSelfDestructPlaytestScene(errors);
            ValidateStandaloneBossPlaytestScene(errors);
            ValidateStandaloneThrowerPlaytestScene(errors);
            ValidateBuildSettings(errors);
            ValidatePublicAssetDependencies(errors);
        }

        private static void ValidatePublicAssetDependencies(
            ICollection<string> errors)
        {
            ISet<string> approvedPlayerBombVfxDependencies =
                LocalLicensedVfxSetup.GetApprovedPlayerBombVfxDependencies();
            string[] assetGuids = AssetDatabase.FindAssets(
                string.Empty,
                new[] { "Assets/Game" });
            for (int assetIndex = 0; assetIndex < assetGuids.Length; assetIndex++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]);
                if (string.IsNullOrEmpty(assetPath) ||
                    AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                string[] dependencies = AssetDatabase.GetDependencies(
                    assetPath,
                    true);
                for (int dependencyIndex = 0;
                     dependencyIndex < dependencies.Length;
                     dependencyIndex++)
                {
                    string dependency = dependencies[dependencyIndex];
                    if (LocalLicensedVfxSetup.IsApprovedPlayerBombVfxReference(
                            assetPath,
                            dependency,
                            dependencies,
                            approvedPlayerBombVfxDependencies))
                    {
                        continue;
                    }
                    if (PrivateVendorAssetRoots.Any(root =>
                            dependency.StartsWith(
                                root,
                                StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add(
                            $"Public asset '{assetPath}' directly references private vendor asset '{dependency}'.");
                    }
                }
            }
        }

        private static void ValidatePlayerPrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                errors.Add($"Missing canonical player prefab: {PlayerPrefabPath}");
                return;
            }

            Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
            SkinnedMeshRenderer[] renderers =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            AnimatorController controller = animators.Length == 1
                ? animators[0].runtimeAnimatorController as AnimatorController
                : null;
            bool hasIsMoving = controller != null && controller.parameters.Any(parameter =>
                parameter.name == "IsMoving" &&
                parameter.type == AnimatorControllerParameterType.Bool);
            bool hasPlaceBomb = controller != null && controller.parameters.Any(parameter =>
                parameter.name == "PlaceBomb" &&
                parameter.type == AnimatorControllerParameterType.Trigger);
            bool hasDie = controller != null && controller.parameters.Any(parameter =>
                parameter.name == "Die" &&
                parameter.type == AnimatorControllerParameterType.Trigger);
            bool hasUpperBodyLayer = ValidatePlayerAnimatorLayerContract(controller);
            if (!prefab.CompareTag("Player") ||
                animators.Length != 1 ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                    PlayerAnimatorControllerPath,
                    StringComparison.Ordinal) ||
                animators[0].avatar == null ||
                !animators[0].avatar.isValid ||
                !animators[0].avatar.isHuman ||
                animators[0].applyRootMotion ||
                !hasIsMoving ||
                !hasPlaceBomb ||
                !hasDie ||
                !hasUpperBodyLayer ||
                renderers.Length != 1 ||
                prefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            {
                errors.Add(
                    "Canonical player prefab requires the Player tag, one valid Humanoid Animator, " +
                    "IsMoving (Bool), PlaceBomb and Die (Trigger) parameters, one " +
                    "masked Upper Body override layer for bomb placement, one SkinnedMeshRenderer, " +
                    "disabled root motion, and no Collider or Rigidbody.");
            }
        }

        private static bool ValidatePlayerAnimatorLayerContract(AnimatorController controller)
        {
            if (controller == null ||
                controller.layers.Length != 2 ||
                controller.layers[1].name != "Upper Body" ||
                controller.layers[1].blendingMode != AnimatorLayerBlendingMode.Override ||
                Math.Abs(controller.layers[1].defaultWeight - 1f) > 0.0001f ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(controller.layers[1].avatarMask),
                    PlayerUpperBodyMaskPath,
                    StringComparison.Ordinal) ||
                !ValidatePlayerUpperBodyMask(controller.layers[1].avatarMask) ||
                controller.layers[0].stateMachine.states.Any(child =>
                    child.state != null && child.state.name == "player_put_down_bomb"))
            {
                return false;
            }

            AnimatorStateMachine stateMachine = controller.layers[1].stateMachine;
            AnimatorState[] emptyStates = stateMachine.states
                .Select(child => child.state)
                .Where(state => state != null && state.name == "UpperBodyEmpty")
                .ToArray();
            AnimatorState[] placeBombStates = stateMachine.states
                .Select(child => child.state)
                .Where(state => state != null && state.name == "player_put_down_bomb")
                .ToArray();
            if (stateMachine.states.Length != 2 ||
                emptyStates.Length != 1 ||
                placeBombStates.Length != 1)
            {
                return false;
            }

            AnimatorState empty = emptyStates[0];
            AnimatorState placeBomb = placeBombStates[0];
            return stateMachine.defaultState == empty &&
                   string.Equals(
                       AssetDatabase.GetAssetPath(placeBomb.motion),
                       PlayerPlaceBombAnimationPath,
                       StringComparison.Ordinal) &&
                   HasSingleAnimatorTransition(
                       empty.transitions,
                       placeBomb,
                       "PlaceBomb",
                       AnimatorConditionMode.If,
                       false) &&
                   placeBomb.transitions.Length == 1 &&
                   placeBomb.transitions[0].destinationState == empty &&
                   placeBomb.transitions[0].hasExitTime &&
                   placeBomb.transitions[0].conditions.Length == 0 &&
                   HasSingleAnimatorTransition(
                       stateMachine.anyStateTransitions,
                       empty,
                       "Die",
                       AnimatorConditionMode.If,
                       false) &&
                   !stateMachine.anyStateTransitions[0].canTransitionToSelf;
        }

        private static bool HasSingleAnimatorTransition(
            AnimatorStateTransition[] transitions,
            AnimatorState destination,
            string parameter,
            AnimatorConditionMode mode,
            bool hasExitTime)
        {
            return transitions.Length == 1 &&
                   transitions[0].destinationState == destination &&
                   transitions[0].hasExitTime == hasExitTime &&
                   transitions[0].conditions.Length == 1 &&
                   transitions[0].conditions[0].parameter == parameter &&
                   transitions[0].conditions[0].mode == mode;
        }

        private static bool ValidatePlayerUpperBodyMask(AvatarMask mask)
        {
            if (mask == null)
            {
                return false;
            }

            for (int bodyPart = 0; bodyPart < (int)AvatarMaskBodyPart.LastBodyPart; bodyPart++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)bodyPart;
                bool expected = part == AvatarMaskBodyPart.Body ||
                                part == AvatarMaskBodyPart.Head ||
                                part == AvatarMaskBodyPart.LeftArm ||
                                part == AvatarMaskBodyPart.RightArm ||
                                part == AvatarMaskBodyPart.LeftFingers ||
                                part == AvatarMaskBodyPart.RightFingers ||
                                part == AvatarMaskBodyPart.LeftHandIK ||
                                part == AvatarMaskBodyPart.RightHandIK;
                if (mask.GetHumanoidBodyPartActive(part) != expected)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateGameFont(ICollection<string> errors)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                GameFontAssetPath);
            if (font == null)
            {
                errors.Add($"Missing game TMP font asset: {GameFontAssetPath}");
                return;
            }
            if (!font.HasCharacters(
                    PrototypeLobbyPresenter.GameTitle,
                    out List<char> missingCharacters))
            {
                errors.Add(
                    $"{PrototypeUiFactory.GameFontAssetName} is missing title characters: {string.Join(", ", missingCharacters)}");
            }

            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                TmpSettingsAssetPath);
            if (settings == null)
            {
                errors.Add($"Missing TMP Settings asset: {TmpSettingsAssetPath}");
                return;
            }

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty defaultFontProperty =
                serializedSettings.FindProperty("m_defaultFontAsset");
            if (defaultFontProperty == null ||
                defaultFontProperty.objectReferenceValue != font)
            {
                errors.Add(
                    $"TMP Settings default font must be {PrototypeUiFactory.GameFontAssetName}.");
            }
        }

        private static void ValidateAudioMixer(ICollection<string> errors)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerPath);
            if (mixer == null)
            {
                errors.Add($"Missing prototype AudioMixer: {AudioMixerPath}");
                return;
            }

            string[] requiredGroups = { "Master", "BGM", "SFX" };
            for (int index = 0; index < requiredGroups.Length; index++)
            {
                if (mixer.FindMatchingGroups(requiredGroups[index]).Length == 0)
                {
                    errors.Add(
                        $"Prototype AudioMixer is missing group '{requiredGroups[index]}'.");
                }
            }

            string[] requiredParameters =
            {
                PrototypeUserSettingsRuntime.MasterVolumeParameter,
                PrototypeUserSettingsRuntime.BgmVolumeParameter,
                PrototypeUserSettingsRuntime.SfxVolumeParameter
            };
            for (int index = 0; index < requiredParameters.Length; index++)
            {
                if (!mixer.GetFloat(requiredParameters[index], out _))
                {
                    errors.Add(
                        $"Prototype AudioMixer is missing exposed parameter '{requiredParameters[index]}'.");
                }
            }
        }

        private static void ValidateBgmContent(ICollection<string> errors)
        {
            PrototypeBgmCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeBgmCatalogAsset>(BgmCatalogPath);
            if (catalog == null)
            {
                errors.Add($"Missing prototype BGM catalog: {BgmCatalogPath}");
                return;
            }

            catalog.CollectValidationErrors(errors);
            AudioClip[] runtimeClips = catalog.GetRuntimeClips();
            for (int index = 0; index < runtimeClips.Length; index++)
            {
                string path = AssetDatabase.GetAssetPath(runtimeClips[index]);
                if (string.IsNullOrEmpty(path) ||
                    !path.StartsWith("Assets/Game/Content/Audio/", StringComparison.Ordinal))
                {
                    errors.Add(
                        $"Prototype BGM runtime clip {index} must be a first-party Assets/Game audio asset.");
                }
            }

            string[] previewPaths =
            {
                "Assets/Game/Content/Audio/Music/BGM_DungeonCombat_PowderCorridor_8Bit_Loop.wav",
                "Assets/Game/Content/Audio/Music/BGM_DungeonRecovery_PowderCorridor_8Bit_Loop.wav",
                "Assets/Game/Content/Audio/Music/BGM_BossBattle_OverheatedThrone_8Bit_Loop.wav",
            };
            for (int index = 0; index < previewPaths.Length; index++)
            {
                AudioClip preview = AssetDatabase.LoadAssetAtPath<AudioClip>(previewPaths[index]);
                if (runtimeClips.Contains(preview))
                {
                    errors.Add(
                        $"BGM preview mix must remain unreferenced at runtime: {previewPaths[index]}");
                }
            }

            for (int sceneIndex = 0; sceneIndex < BgmScenePaths.Length; sceneIndex++)
            {
                ValidateBgmScene(BgmScenePaths[sceneIndex], catalog, errors);
            }
        }

        private static void ValidateBgmScene(
            string scenePath,
            PrototypeBgmCatalogAsset catalog,
            ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                errors.Add($"Missing BGM target scene: {scenePath}");
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
                PrototypeBgmPresenter[] presenters =
                    FindComponents<PrototypeBgmPresenter>(scene);
                if (presenters.Length != 1)
                {
                    errors.Add(
                        $"BGM target scene '{scenePath}' must contain exactly one PrototypeBgmPresenter, found {presenters.Length}.");
                    return;
                }

                PrototypeBgmPresenter presenter = presenters[0];
                if (presenter.transform.parent != null ||
                    presenter.gameObject.scene != scene)
                {
                    errors.Add(
                        $"BGM presenter in '{scenePath}' must be a scene root.");
                }
                if (presenter.Catalog != catalog)
                {
                    errors.Add(
                        $"BGM presenter in '{scenePath}' must reference {BgmCatalogPath}.");
                }
                if (presenter.GetComponents<AudioSource>().Length != 0)
                {
                    errors.Add(
                        $"BGM presenter in '{scenePath}' must create AudioSources at runtime, not serialize them in the scene.");
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

        private static void ValidateLobbyScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyScenePath) == null)
            {
                errors.Add($"Missing prototype lobby scene: {LobbyScenePath}");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(LobbyScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(
                    LobbyScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                PrototypeLobbyPresenter[] presenters =
                    FindComponents<PrototypeLobbyPresenter>(scene);
                if (presenters.Length != 1)
                {
                    errors.Add(
                        $"Lobby must contain exactly one PrototypeLobbyPresenter, found {presenters.Length}.");
                }
                else
                {
                    PrototypeLobbyPresenter presenter = presenters[0];
                    if (!string.Equals(
                            presenter.StartSceneName,
                            PrototypeLobbyPresenter.DefaultStartSceneName,
                            StringComparison.Ordinal))
                    {
                        errors.Add(
                            $"Lobby start scene must be '{PrototypeLobbyPresenter.DefaultStartSceneName}'.");
                    }
                    bool hasAuthoredViewReferences =
                        presenter.HasAuthoredViewReferences;
                    if (!hasAuthoredViewReferences)
                    {
                        errors.Add(
                            "Lobby presenter must reference its scene-authored Canvas, EventSystem, controls panel, labels, and buttons.");
                    }
                    if (!presenter.HasVersionLabelReference)
                    {
                        errors.Add(
                            "Lobby presenter must reference its scene-authored version label.");
                    }
                    else if (presenter.LobbyCanvas == null ||
                             presenter.VersionLabel.gameObject.scene != scene ||
                             !presenter.VersionLabel.transform.IsChildOf(
                                 presenter.LobbyCanvas.transform))
                    {
                        errors.Add(
                            "Lobby version label must belong to the authored LobbyCanvas.");
                    }

                    if (hasAuthoredViewReferences)
                    {
                        if (presenter.LobbyCanvas.gameObject.scene != scene ||
                            presenter.LobbyEventSystem.gameObject.scene != scene ||
                            presenter.ControlsPanel.scene != scene)
                        {
                            errors.Add(
                                "Lobby presenter view references must belong to the lobby scene.");
                        }
                        if (presenter.TitleText.IndexOf(
                                PrototypeLobbyPresenter.GameTitle,
                                StringComparison.Ordinal) < 0)
                        {
                            errors.Add(
                                $"Lobby title must include '{PrototypeLobbyPresenter.GameTitle}'.");
                        }
                        if (presenter.ControlsPanel.activeSelf)
                        {
                            errors.Add(
                                "Lobby controls panel must be inactive in the authored scene.");
                        }
                        if (presenter.SettingsRuntime == null ||
                            !presenter.SettingsRuntime.HasRequiredReferences ||
                            presenter.SettingsPanel == null ||
                            !presenter.SettingsPanel.HasAuthoredViewReferences)
                        {
                            errors.Add(
                                "Lobby settings panel must reference the shared input actions and AudioMixer.");
                        }
                        if (presenter.SettingsPanel != null &&
                            presenter.SettingsPanel
                                .GetComponentsInChildren<TextMeshProUGUI>(true)
                                .Any(label => string.Equals(
                                    label.name,
                                    "SettingsStatusText",
                                    StringComparison.Ordinal)))
                        {
                            errors.Add(
                                "Lobby settings panel must not contain the obsolete SettingsStatusText label.");
                        }

                        Image settingsPanelImage = presenter.SettingsPanel != null
                            ? presenter.SettingsPanel.GetComponent<Image>()
                            : null;
                        if (!PrototypeDirectThirdPartyUiAuthoring
                                .HasSettingsPanelConfiguration(
                                    settingsPanelImage))
                        {
                            errors.Add(
                                "Lobby settings panel must keep an integer-scaled " +
                                "Simple Image configuration. " +
                                "Designer-authored RectTransform layout is preserved.");
                        }

                        if (!PrototypeDirectThirdPartyUiAuthoring
                                .HasExpectedDirectSpriteSlots(
                                    presenter.LobbyCanvas != null
                                        ? presenter.LobbyCanvas.gameObject
                                        : null,
                                    PrototypeDirectThirdPartyUiAuthoring
                                        .MinimumLobbySlots))
                        {
                            errors.Add(
                                "Lobby direct Sprite slots must use per-Image " +
                                "runtime fallbacks without a legacy role applicator.");
                        }
                    }
                }

                if (FindComponents<PrototypeUserSettingsRuntime>(scene).Length != 1 ||
                    FindComponents<PrototypeSettingsPanelPresenter>(scene).Length != 1)
                {
                    errors.Add(
                        "Lobby must contain one authored user-settings runtime and settings panel.");
                }

                Canvas[] canvases = FindComponents<Canvas>(scene);
                if (canvases.Length != 1 || canvases[0].renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    errors.Add(
                        "Lobby must contain exactly one scene-authored Screen Space Overlay Canvas.");
                }
                CanvasScaler[] canvasScalers = FindComponents<CanvasScaler>(scene);
                if (canvasScalers.Length != 1 ||
                    FindComponents<GraphicRaycaster>(scene).Length != 1)
                {
                    errors.Add(
                        "Lobby Canvas must contain exactly one CanvasScaler and GraphicRaycaster.");
                }
                else if (!PrototypeUiFactory.HasReferenceCanvasScale(canvasScalers[0]))
                {
                    errors.Add(
                        $"Lobby CanvasScaler must use the shared {PrototypeUiFactory.ReferenceWidth:0}x{PrototypeUiFactory.ReferenceHeight:0} reference resolution.");
                }

                EventSystem[] eventSystems = FindComponents<EventSystem>(scene);
                InputSystemUIInputModule[] inputModules =
                    FindComponents<InputSystemUIInputModule>(scene);
                if (eventSystems.Length != 1 || inputModules.Length != 1 ||
                    inputModules[0].gameObject != eventSystems[0].gameObject)
                {
                    errors.Add(
                        "Lobby must contain one scene-authored EventSystem with InputSystemUIInputModule.");
                }

                TextMeshProUGUI[] labels = FindComponents<TextMeshProUGUI>(scene);
                if (labels.Length == 0)
                {
                    errors.Add("Lobby scene must contain authored TextMeshProUGUI labels.");
                }
                else
                {
                    for (int index = 0; index < labels.Length; index++)
                    {
                        if (!PrototypeUiFactory.IsSupportedGameFont(labels[index].font))
                        {
                            errors.Add(
                                $"Lobby label '{labels[index].name}' must use {PrototypeUiFactory.GameFontAssetName} or {PrototypeUiFactory.AlternateGameFontAssetName}.");
                        }
                    }
                }

                Button[] buttons = FindComponents<Button>(scene);
                if (buttons.Length == 0)
                {
                    errors.Add("Lobby scene must contain authored buttons.");
                }
                else
                {
                    for (int index = 0; index < buttons.Length; index++)
                    {
                        Button button = buttons[index];
                        PrototypeButtonScaleFeedback[] feedbacks =
                            button.GetComponents<PrototypeButtonScaleFeedback>();
                        if (feedbacks.Length != 1)
                        {
                            errors.Add(
                                $"Lobby button '{button.name}' must contain exactly one PrototypeButtonScaleFeedback.");
                            continue;
                        }

                        RectTransform buttonRect =
                            button.transform as RectTransform;
                        RectTransform visualTarget = feedbacks[0].VisualTarget;
                        bool ownsVisualTarget = visualTarget != null &&
                            (visualTarget == buttonRect ||
                             visualTarget.IsChildOf(buttonRect));
                        if (!ownsVisualTarget ||
                            !feedbacks[0].HasConfiguration(visualTarget))
                        {
                            errors.Add(
                                $"Lobby button '{button.name}' must use the shared hover, press, and timing feedback configuration.");
                        }

                        for (int hoverIndex = 0;
                             hoverIndex < feedbacks[0].HoverVisualTargetCount;
                             hoverIndex++)
                        {
                            GameObject hoverTarget =
                                feedbacks[0].GetHoverVisualTarget(hoverIndex);
                            bool ownsHoverTarget = hoverTarget != null &&
                                hoverTarget.transform != buttonRect &&
                                hoverTarget.transform.IsChildOf(buttonRect);
                            if (!ownsHoverTarget)
                            {
                                errors.Add(
                                    $"Lobby button '{button.name}' contains a hover visual outside its hierarchy.");
                            }
                            else if (hoverTarget.activeSelf)
                            {
                                errors.Add(
                                    $"Lobby button '{button.name}' hover visual '{hoverTarget.name}' must be inactive in the authored Normal state.");
                            }
                        }

                        bool isMainMenuButton = presenters.Length == 1 &&
                            (button == presenters[0].StartButton ||
                             button == presenters[0].ControlsButton);
                        if (isMainMenuButton &&
                            feedbacks[0].HoverVisualTargetCount != 2)
                        {
                            errors.Add(
                                $"Lobby main-menu button '{button.name}' must explicitly reference its two hover arrow visuals.");
                        }
                    }
                }

                if (FindComponents<PrototypeDungeonRunHost>(scene).Length != 0 ||
                    FindComponents<PrototypeDungeonRoomBinder>(scene).Length != 0 ||
                    FindComponents<PrototypeGameSession>(scene).Length != 0 ||
                    FindComponents<BombSwapInputReader>(scene).Length != 0)
                {
                    errors.Add(
                        "Lobby must not contain dungeon run, room, game-session, or gameplay-input components.");
                }

                Camera[] cameras = FindComponents<Camera>(scene);
                if (cameras.Length != 1 || !cameras[0].CompareTag("MainCamera"))
                {
                    errors.Add(
                        "Lobby must contain exactly one camera tagged MainCamera.");
                }
                if (FindComponents<AudioListener>(scene).Length != 1)
                {
                    errors.Add("Lobby must contain exactly one AudioListener.");
                }
                if (FindComponents<Light>(scene).Length < 1)
                {
                    errors.Add("Lobby must contain at least one light.");
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
            if ((firstDefinition != null &&
                 firstDefinition.FuseSeconds != 2f) ||
                (secondDefinition != null &&
                 secondDefinition.FuseSeconds != 2f) ||
                (lineDefinition != null &&
                 lineDefinition.FuseSeconds != 2f) ||
                (bossThrowDefinition != null &&
                 bossThrowDefinition.FuseSeconds != 2f) ||
                (bossChainDefinition != null &&
                 bossChainDefinition.FuseSeconds != 2f))
            {
                errors.Add(
                    "Prototype cross, area, line, boss throw, and boss chain bomb fuses must be 2 seconds.");
            }
            ValidatePlayerBombVisuals(errors);
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

        private static void ValidatePlayerBombVisuals(ICollection<string> errors)
        {
            string[] prefabPaths =
            {
                BombPrefabPath,
                AreaBombPrefabPath,
                LineBombPrefabPath,
            };
            string[] expectedNames =
            {
                "NormalBomb",
                "RangeBomb",
                "StraightBomb",
            };
            for (int index = 0; index < prefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPaths[index]);
                if (prefab == null)
                {
                    continue;
                }

                Renderer[] renderers =
                    prefab.GetComponentsInChildren<Renderer>(true);
                Animator[] animators =
                    prefab.GetComponentsInChildren<Animator>(true);
                if (prefab.name != expectedNames[index] ||
                    renderers.Length == 0 ||
                    animators.Length != 1 ||
                    animators[0].runtimeAnimatorController == null ||
                    animators[0].applyRootMotion ||
                    prefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                    prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                {
                    errors.Add(
                        $"Player bomb prefab '{prefabPaths[index]}' must use its animated collider-free visual with root motion disabled.");
                }
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
            if (definition.ChaserPrefab != null)
            {
                Animator[] animators =
                    definition.ChaserPrefab.GetComponentsInChildren<Animator>(true);
                SkinnedMeshRenderer[] renderers =
                    definition.ChaserPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                AnimatorController controller = animators.Length == 1
                    ? animators[0].runtimeAnimatorController as AnimatorController
                    : null;
                bool hasAnimatorContract = ValidateChaserAnimatorContract(controller);
                if (animators.Length != 1 ||
                    renderers.Length == 0 ||
                    animators[0].avatar == null ||
                    !animators[0].avatar.isValid ||
                    !animators[0].avatar.isHuman ||
                    animators[0].applyRootMotion ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                        ChaserAnimatorControllerPath,
                        StringComparison.Ordinal) ||
                    !hasAnimatorContract ||
                    definition.ChaserPrefab.GetComponentInChildren<Rigidbody>(true) != null)
                {
                    errors.Add(
                        "Canonical chaser prefab requires one valid Humanoid Animator with the " +
                        "Chaser Idle/Run/Attack/Die contract, a skinned renderer, " +
                        "disabled root motion, and no Rigidbody.");
                }
            }
        }

        private static bool ValidateChaserAnimatorContract(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1 ||
                controller.parameters.Length != 3 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "IsMoving" &&
                    parameter.type == AnimatorControllerParameterType.Bool) != 1 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "Attack" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "Die" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1)
            {
                return false;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindSingleAnimatorState(machine, "ChaserIdle");
            AnimatorState run = FindSingleAnimatorState(machine, "ChaserRun");
            AnimatorState attack = FindSingleAnimatorState(machine, "ChaserAttack");
            AnimatorState die = FindSingleAnimatorState(machine, "ChaserDie");
            if (idle == null || run == null || attack == null || die == null ||
                machine.states.Length != 4 || machine.stateMachines.Length != 0 ||
                !string.Equals(AssetDatabase.GetAssetPath(idle.motion), ChaserIdleClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(run.motion), ChaserRunClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(attack.motion), ChaserAttackClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(die.motion), ChaserDieClipPath, StringComparison.Ordinal))
            {
                return false;
            }

            return machine.defaultState == idle &&
                   HasAnimatorTransition(idle.transitions, run, "IsMoving", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(idle.transitions, attack, "Attack", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(run.transitions, idle, "IsMoving", AnimatorConditionMode.IfNot, false) &&
                   HasAnimatorTransition(run.transitions, attack, "Attack", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(attack.transitions, idle, "IsMoving", AnimatorConditionMode.IfNot, true) &&
                   HasAnimatorTransition(attack.transitions, run, "IsMoving", AnimatorConditionMode.If, true) &&
                   HasAnimatorTransition(machine.anyStateTransitions, die, "Die", AnimatorConditionMode.If, false) &&
                   idle.transitions.Length == 2 && run.transitions.Length == 2 &&
                   attack.transitions.Length == 2 && die.transitions.Length == 0 &&
                   machine.anyStateTransitions.Length == 1 &&
                   !machine.anyStateTransitions[0].canTransitionToSelf;
        }

        private static AnimatorState FindSingleAnimatorState(
            AnimatorStateMachine machine,
            string stateName)
        {
            AnimatorState[] matches = machine.states
                .Select(child => child.state)
                .Where(state => state != null && state.name == stateName)
                .ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static bool HasAnimatorTransition(
            AnimatorStateTransition[] transitions,
            AnimatorState destination,
            string parameter,
            AnimatorConditionMode mode,
            bool hasExitTime)
        {
            return transitions.Count(transition =>
                transition.destinationState == destination &&
                transition.hasExitTime == hasExitTime &&
                transition.conditions.Length == 1 &&
                transition.conditions[0].parameter == parameter &&
                transition.conditions[0].mode == mode) == 1;
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
            if (definition.ChargerPrefab != null)
            {
                Animator[] animators =
                    definition.ChargerPrefab.GetComponentsInChildren<Animator>(true);
                SkinnedMeshRenderer[] renderers =
                    definition.ChargerPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                AnimatorController controller = animators.Length == 1
                    ? animators[0].runtimeAnimatorController as AnimatorController
                    : null;
                if (animators.Length != 1 || renderers.Length == 0 ||
                    animators[0].avatar == null || !animators[0].avatar.isValid ||
                    !animators[0].avatar.isHuman || animators[0].applyRootMotion ||
                    definition.ChargerPrefab.GetComponentInChildren<Rigidbody>(true) != null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                        ChargerAnimatorControllerPath,
                        StringComparison.Ordinal) ||
                    !ValidateChargerAnimatorContract(controller))
                {
                    errors.Add(
                        "Canonical charger prefab requires one valid Humanoid Animator with " +
                        "Track/Telegraph/Charge/Recover/Die states, a skinned renderer, " +
                        "disabled root motion, and no Rigidbody.");
                }
            }
            if (definition.TelegraphCellPrefab != null &&
                definition.TelegraphCellPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add(
                    "Prototype charger telegraph-cell prefab must not contain a Collider; " +
                    "logical grid owns collision.");
            }
        }

        private static bool ValidateChargerAnimatorContract(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1 ||
                controller.parameters.Length != 6 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "IsMoving" &&
                    parameter.type == AnimatorControllerParameterType.Bool) != 1)
            {
                return false;
            }
            foreach (string trigger in new[] { "Track", "Telegraph", "Charge", "Recover", "Die" })
            {
                if (controller.parameters.Count(parameter =>
                        parameter.name == trigger &&
                        parameter.type == AnimatorControllerParameterType.Trigger) != 1)
                {
                    return false;
                }
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindSingleAnimatorState(machine, "ChargerIdle");
            AnimatorState run = FindSingleAnimatorState(machine, "ChargerRun");
            AnimatorState telegraph = FindSingleAnimatorState(machine, "ChargerTelegraph");
            AnimatorState charge = FindSingleAnimatorState(machine, "ChargerCharge");
            AnimatorState recover = FindSingleAnimatorState(machine, "ChargerRecover");
            AnimatorState die = FindSingleAnimatorState(machine, "ChargerDie");
            if (idle == null || run == null || telegraph == null || charge == null ||
                recover == null || die == null || machine.states.Length != 6 ||
                machine.stateMachines.Length != 0 ||
                !string.Equals(AssetDatabase.GetAssetPath(idle.motion), ChargerIdleClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(run.motion), ChargerRunClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(telegraph.motion), ChargerTelegraphClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(charge.motion), ChargerChargeClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(recover.motion), ChargerRecoverClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(die.motion), ChargerDieClipPath, StringComparison.Ordinal))
            {
                return false;
            }

            return machine.defaultState == idle &&
                   HasAnimatorTransition(idle.transitions, run, "IsMoving", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(run.transitions, idle, "IsMoving", AnimatorConditionMode.IfNot, false) &&
                   HasAnimatorTransition(idle.transitions, telegraph, "Telegraph", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(run.transitions, telegraph, "Telegraph", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(telegraph.transitions, charge, "Charge", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(charge.transitions, recover, "Recover", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(recover.transitions, idle, "Track", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(machine.anyStateTransitions, die, "Die", AnimatorConditionMode.If, false) &&
                   idle.transitions.Length == 2 && run.transitions.Length == 2 &&
                   telegraph.transitions.Length == 1 && charge.transitions.Length == 1 &&
                   recover.transitions.Length == 1 && die.transitions.Length == 0 &&
                   machine.anyStateTransitions.Length == 1 &&
                   !machine.anyStateTransitions[0].canTransitionToSelf;
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
            if (definition.EnemyPrefab != null)
            {
                Animator[] animators =
                    definition.EnemyPrefab.GetComponentsInChildren<Animator>(true);
                SkinnedMeshRenderer[] renderers =
                    definition.EnemyPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                AnimatorController controller = animators.Length == 1
                    ? animators[0].runtimeAnimatorController as AnimatorController
                    : null;
                if (animators.Length != 1 || renderers.Length == 0 ||
                    animators[0].avatar == null || !animators[0].avatar.isValid ||
                    !animators[0].avatar.isHuman || animators[0].applyRootMotion ||
                    definition.EnemyPrefab.GetComponentInChildren<Collider>(true) != null ||
                    definition.EnemyPrefab.GetComponentInChildren<Rigidbody>(true) != null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                        SelfDestructAnimatorControllerPath,
                        StringComparison.Ordinal) ||
                    !ValidateSelfDestructAnimatorContract(controller))
                {
                    errors.Add(
                        "Canonical self-destruct prefab requires one valid Humanoid Animator " +
                        "with Idle/Run/Telegraph/Detonate states, a skinned renderer, " +
                        "disabled root motion, and no Collider or Rigidbody.");
                }
            }
        }

        private static bool ValidateSelfDestructAnimatorContract(
            AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1 ||
                controller.parameters.Length != 3 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "IsMoving" &&
                    parameter.type == AnimatorControllerParameterType.Bool) != 1 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "Telegraph" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1 ||
                controller.parameters.Count(parameter =>
                    parameter.name == "Detonate" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1)
            {
                return false;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindSingleAnimatorState(machine, "SelfDestructIdle");
            AnimatorState run = FindSingleAnimatorState(machine, "SelfDestructRun");
            AnimatorState telegraph =
                FindSingleAnimatorState(machine, "SelfDestructTelegraph");
            AnimatorState detonate =
                FindSingleAnimatorState(machine, "SelfDestructDetonate");
            if (idle == null || run == null || telegraph == null || detonate == null ||
                machine.states.Length != 4 || machine.stateMachines.Length != 0 ||
                !string.Equals(AssetDatabase.GetAssetPath(idle.motion), SelfDestructIdleClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(run.motion), SelfDestructRunClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(telegraph.motion), SelfDestructTelegraphClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(detonate.motion), SelfDestructDetonateClipPath, StringComparison.Ordinal))
            {
                return false;
            }

            return machine.defaultState == idle &&
                   HasAnimatorTransition(idle.transitions, run, "IsMoving", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(run.transitions, idle, "IsMoving", AnimatorConditionMode.IfNot, false) &&
                   HasAnimatorTransition(idle.transitions, telegraph, "Telegraph", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(run.transitions, telegraph, "Telegraph", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(telegraph.transitions, detonate, "Detonate", AnimatorConditionMode.If, false) &&
                   idle.transitions.Length == 2 && run.transitions.Length == 2 &&
                   telegraph.transitions.Length == 1 && detonate.transitions.Length == 0 &&
                   machine.anyStateTransitions.Length == 0;
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
                    core.ThrowBombDefinition.FuseDuration != TimeSpan.FromSeconds(2) ||
                    core.ThrowBombDefinition.Range != 2 ||
                    core.ChainBombDefinition.Id !=
                        new BombDefinitionId("prototype-boss-chain") ||
                    core.ChainBombDefinition.FuseDuration != TimeSpan.FromSeconds(2) ||
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
            if (definition.BossPrefab != null)
            {
                Animator[] animators =
                    definition.BossPrefab.GetComponentsInChildren<Animator>(true);
                SkinnedMeshRenderer[] renderers =
                    definition.BossPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                AnimatorController controller = animators.Length == 1
                    ? animators[0].runtimeAnimatorController as AnimatorController
                    : null;
                if (animators.Length != 1 || renderers.Length == 0 ||
                    animators[0].avatar == null || !animators[0].avatar.isValid ||
                    !animators[0].avatar.isHuman || animators[0].applyRootMotion ||
                    definition.BossPrefab.GetComponentInChildren<Rigidbody>(true) != null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                        BossAnimatorControllerPath,
                        StringComparison.Ordinal) ||
                    !ValidateBossAnimatorContract(controller))
                {
                    errors.Add(
                        "Canonical boss prefab requires one valid Humanoid Animator with the " +
                        "Idle/Walk/Telegraph/Charge/Summon/alternating Throw/Die contract, " +
                        "a skinned renderer, disabled root motion, and no Rigidbody.");
                }
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

        private static bool ValidateBossAnimatorContract(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1 ||
                controller.parameters.Length != 8 ||
                !HasSingleAnimatorParameter(controller, "Alive", AnimatorControllerParameterType.Bool) ||
                !HasSingleAnimatorParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool) ||
                !HasSingleAnimatorParameter(controller, "Telegraph", AnimatorControllerParameterType.Trigger) ||
                !HasSingleAnimatorParameter(controller, "Charge", AnimatorControllerParameterType.Trigger) ||
                !HasSingleAnimatorParameter(controller, "Summon", AnimatorControllerParameterType.Trigger) ||
                !HasSingleAnimatorParameter(controller, "ThrowLeft", AnimatorControllerParameterType.Trigger) ||
                !HasSingleAnimatorParameter(controller, "ThrowRight", AnimatorControllerParameterType.Trigger) ||
                !HasSingleAnimatorParameter(controller, "Die", AnimatorControllerParameterType.Trigger))
            {
                return false;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindSingleAnimatorState(machine, "BossIdle");
            AnimatorState walk = FindSingleAnimatorState(machine, "BossWalk");
            AnimatorState telegraph = FindSingleAnimatorState(machine, "BossTelegraph");
            AnimatorState charge = FindSingleAnimatorState(machine, "BossCharge");
            AnimatorState summon = FindSingleAnimatorState(machine, "BossSummon");
            AnimatorState throwLeft = FindSingleAnimatorState(machine, "BossThrowLeft");
            AnimatorState throwRight = FindSingleAnimatorState(machine, "BossThrowRight");
            AnimatorState die = FindSingleAnimatorState(machine, "BossDie");
            if (idle == null || walk == null || telegraph == null || charge == null ||
                summon == null || throwLeft == null || throwRight == null || die == null ||
                machine.states.Length != 8 || machine.stateMachines.Length != 0 ||
                !HasAnimatorMotion(idle, BossIdleClipPath) ||
                !HasAnimatorMotion(walk, BossWalkClipPath) ||
                !HasAnimatorMotion(telegraph, BossTelegraphClipPath) ||
                !HasAnimatorMotion(charge, BossChargeClipPath) ||
                !HasAnimatorMotion(summon, BossSummonClipPath) ||
                !HasAnimatorMotion(throwLeft, BossThrowLeftClipPath) ||
                !HasAnimatorMotion(throwRight, BossThrowRightClipPath) ||
                !HasAnimatorMotion(die, BossDieClipPath))
            {
                return false;
            }

            return machine.defaultState == idle &&
                   HasAnimatorTransition(idle.transitions, walk, "IsMoving", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(walk.transitions, idle, "IsMoving", AnimatorConditionMode.IfNot, false) &&
                   HasBossLivingTransition(machine.anyStateTransitions, telegraph, "Telegraph") &&
                   HasBossLivingTransition(machine.anyStateTransitions, charge, "Charge") &&
                   HasBossLivingTransition(machine.anyStateTransitions, summon, "Summon") &&
                   HasBossLivingTransition(machine.anyStateTransitions, throwLeft, "ThrowLeft") &&
                   HasBossLivingTransition(machine.anyStateTransitions, throwRight, "ThrowRight") &&
                   HasAnimatorTransition(machine.anyStateTransitions, die, "Die", AnimatorConditionMode.If, false) &&
                   idle.transitions.Length == 1 && walk.transitions.Length == 1 &&
                   telegraph.transitions.Length == 2 && charge.transitions.Length == 2 &&
                   summon.transitions.Length == 2 && throwLeft.transitions.Length == 2 &&
                   throwRight.transitions.Length == 2 && die.transitions.Length == 0 &&
                   machine.anyStateTransitions.Length == 6 &&
                   machine.anyStateTransitions.All(transition => !transition.canTransitionToSelf);
        }

        private static bool HasSingleAnimatorParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            return controller.parameters.Count(parameter =>
                parameter.name == name && parameter.type == type) == 1;
        }

        private static bool HasAnimatorMotion(AnimatorState state, string assetPath)
        {
            return string.Equals(
                AssetDatabase.GetAssetPath(state.motion), assetPath, StringComparison.Ordinal);
        }

        private static bool HasBossLivingTransition(
            AnimatorStateTransition[] transitions,
            AnimatorState destination,
            string trigger)
        {
            return transitions.Count(transition =>
                transition.destinationState == destination &&
                !transition.hasExitTime && transition.conditions.Length == 2 &&
                transition.conditions.Any(condition =>
                    condition.parameter == trigger &&
                    condition.mode == AnimatorConditionMode.If) &&
                transition.conditions.Any(condition =>
                    condition.parameter == "Alive" &&
                    condition.mode == AnimatorConditionMode.If)) == 1;
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
                    bomb.FuseDuration != TimeSpan.FromSeconds(2) ||
                    bomb.Range != 1 ||
                    room.Id != new RoomDefinitionId("prototype-combat-thrower") ||
                    room.ChaserSpawn != new GridPosition(-2, 2) ||
                    room.ThrowerSpawn != new GridPosition(3, 2) ||
                    !room.ThrowerFiringAnchors.SequenceEqual(new[]
                    {
                        new GridPosition(0, 3),
                        new GridPosition(-3, 2),
                        new GridPosition(3, -2),
                    }) ||
                    !room.ThrowerTargetAnchors.SequenceEqual(new[]
                    {
                        new GridPosition(0, 0),
                        new GridPosition(-3, -2),
                        new GridPosition(2, -3),
                        new GridPosition(-4, 1),
                        new GridPosition(4, 1),
                        new GridPosition(0, 2),
                    }) ||
                    !HasMinimumExitDistance(room.ThrowerSpawn.Value, room.Exits, 4) ||
                    !HasMinimumExitDistance(room.ChaserSpawn, room.Exits, 4) ||
                    ManhattanDistance(
                        room.ThrowerSpawn.Value,
                        room.ThrowerFiringAnchors[0]) < 4 ||
                    !HasMinimumAnchorDistance(
                        room.ChaserSpawn,
                        room.ThrowerTargetAnchors,
                        bomb.Range + 1) ||
                    HasExitAnchorOverlap(room.ThrowerTargetAnchors, room.Exits))
                {
                    errors.Add(
                        "Prototype thrower content does not match the Proposed timing, bomb, " +
                        "staging-route, dedicated Lanes anchor, entry-clearance, or initial " +
                        "friendly-fire safety contract.");
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
            if (definition.EnemyPrefab != null)
            {
                Animator[] animators =
                    definition.EnemyPrefab.GetComponentsInChildren<Animator>(true);
                SkinnedMeshRenderer[] renderers =
                    definition.EnemyPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                AnimatorController controller = animators.Length == 1
                    ? animators[0].runtimeAnimatorController as AnimatorController
                    : null;
                if (animators.Length != 1 || renderers.Length == 0 ||
                    animators[0].avatar == null || !animators[0].avatar.isValid ||
                    !animators[0].avatar.isHuman || animators[0].applyRootMotion ||
                    definition.EnemyPrefab.GetComponentInChildren<Collider>(true) != null ||
                    definition.EnemyPrefab.GetComponentInChildren<Rigidbody>(true) != null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                        ThrowerAnimatorControllerPath,
                        StringComparison.Ordinal) ||
                    !ValidateThrowerAnimatorContract(controller))
                {
                    errors.Add(
                        "Canonical thrower prefab requires one valid Humanoid Animator " +
                        "with Idle/Walk/Throw/Die states, a skinned renderer, " +
                        "disabled root motion, and no Collider or Rigidbody.");
                }
            }
        }

        private static bool ValidateThrowerAnimatorContract(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1 ||
                controller.parameters.Length != 4 ||
                controller.parameters.Count(parameter => parameter.name == "IsMoving" &&
                    parameter.type == AnimatorControllerParameterType.Bool) != 1 ||
                controller.parameters.Count(parameter => parameter.name == "Throw" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1 ||
                controller.parameters.Count(parameter => parameter.name == "Recover" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1 ||
                controller.parameters.Count(parameter => parameter.name == "Die" &&
                    parameter.type == AnimatorControllerParameterType.Trigger) != 1)
            {
                return false;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindSingleAnimatorState(machine, "ThrowerIdle");
            AnimatorState walk = FindSingleAnimatorState(machine, "ThrowerWalk");
            AnimatorState throwState = FindSingleAnimatorState(machine, "ThrowerThrow");
            AnimatorState die = FindSingleAnimatorState(machine, "ThrowerDie");
            if (idle == null || walk == null || throwState == null || die == null ||
                machine.states.Length != 4 || machine.stateMachines.Length != 0 ||
                !string.Equals(AssetDatabase.GetAssetPath(idle.motion), ThrowerIdleClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(walk.motion), ThrowerWalkClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(throwState.motion), ThrowerThrowClipPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(die.motion), ThrowerDieClipPath, StringComparison.Ordinal))
            {
                return false;
            }

            return machine.defaultState == idle &&
                   HasAnimatorTransition(idle.transitions, walk, "IsMoving", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(walk.transitions, idle, "IsMoving", AnimatorConditionMode.IfNot, false) &&
                   HasAnimatorTransition(idle.transitions, throwState, "Throw", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(walk.transitions, throwState, "Throw", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(throwState.transitions, idle, "Recover", AnimatorConditionMode.If, false) &&
                   HasAnimatorTransition(machine.anyStateTransitions, die, "Die", AnimatorConditionMode.If, false) &&
                   idle.transitions.Length == 2 && walk.transitions.Length == 2 &&
                   throwState.transitions.Length == 1 && die.transitions.Length == 0 &&
                   machine.anyStateTransitions.Length == 1 &&
                   !machine.anyStateTransitions[0].canTransitionToSelf;
        }

        private static bool HasMinimumExitDistance(
            GridPosition position,
            IReadOnlyList<RoomExit> exits,
            int minimumDistance)
        {
            for (int index = 0; index < exits.Count; index++)
            {
                GridPosition exit = exits[index].Cell;
                int distance = Math.Abs(position.X - exit.X) +
                    Math.Abs(position.Z - exit.Z);
                if (distance < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasMinimumAnchorDistance(
            GridPosition position,
            IReadOnlyList<GridPosition> anchors,
            int minimumDistance)
        {
            for (int index = 0; index < anchors.Count; index++)
            {
                if (ManhattanDistance(position, anchors[index]) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasExitAnchorOverlap(
            IReadOnlyList<GridPosition> anchors,
            IReadOnlyList<RoomExit> exits)
        {
            for (int anchorIndex = 0; anchorIndex < anchors.Count; anchorIndex++)
            {
                for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
                {
                    if (anchors[anchorIndex] == exits[exitIndex].Cell)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int ManhattanDistance(GridPosition first, GridPosition second)
        {
            return Math.Abs(first.X - second.X) + Math.Abs(first.Z - second.Z);
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
                PrototypeCombatThrowerDefinitionPath,
                PrototypeCombatPillarsDefinitionPath,
                PrototypeCombatArmorDefinitionPath,
                PrototypeCombatGatesDefinitionPath,
            };
            string[] expectedSceneNames =
            {
                "TestSandbox",
                "TestSandboxThrower",
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
                TestSandboxThrowerScenePath,
                PrototypeCombatThrowerDefinitionPath,
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

        private static void ValidateStandaloneLegacyLanesPlaytestScene(
            ICollection<string> errors)
        {
            ValidateStandaloneCombatPlaytestScene(
                TestSandboxLanesScenePath,
                PrototypeCombatLanesDefinitionPath,
                typeof(PrototypeChaserPresenter),
                "Legacy Lanes",
                false,
                errors);
        }

        private static void ValidateInGameUiPrefabs(
            ICollection<string> errors)
        {
            ValidateInGameUiPrefab<PrototypeWeaponHudView>(
                PrototypeInGameUiPrefabAuthoring.WeaponHudPrefabPath,
                view => view.HasRequiredReferences,
                errors);
            ValidateInGameUiPrefab<PrototypeHealthHudView>(
                PrototypeInGameUiPrefabAuthoring.HealthHudPrefabPath,
                view => view.HasRequiredReferences,
                errors);
            ValidateInGameUiPrefab<PrototypeDungeonMinimapView>(
                PrototypeInGameUiPrefabAuthoring.MinimapPrefabPath,
                view => view.HasRequiredReferences,
                errors);
            ValidateInGameUiPrefab<PrototypeDungeonMinimapRoomView>(
                PrototypeInGameUiPrefabAuthoring.MinimapRoomPrefabPath,
                view => view.HasRequiredReferences,
                errors);
            ValidateInGameUiPrefab<PrototypeDungeonMinimapConnectionView>(
                PrototypeInGameUiPrefabAuthoring.MinimapConnectionPrefabPath,
                view => view.HasRequiredReferences,
                errors);
            ValidateInGameUiPrefab<PrototypePauseView>(
                PrototypeInGameUiPrefabAuthoring.PausePrefabPath,
                view => view.HasRequiredReferences,
                errors);
            ValidateMinimapComposition(errors);
            ValidatePauseTitleWave(errors);
        }

        private static void ValidateMinimapComposition(
            ICollection<string> errors)
        {
            PrototypeDungeonMinimapView minimap =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonMinimapView>(
                    PrototypeInGameUiPrefabAuthoring.MinimapPrefabPath);
            if (minimap == null || !minimap.HasRequiredReferences)
            {
                return;
            }

            if (!string.Equals(
                    AssetDatabase.GetAssetPath(minimap.RoomViewPrefab),
                    PrototypeInGameUiPrefabAuthoring.MinimapRoomPrefabPath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(minimap.ConnectionViewPrefab),
                    PrototypeInGameUiPrefabAuthoring.MinimapConnectionPrefabPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "Minimap must reference the shared room and connection view prefabs.");
            }

            PrototypeDungeonMinimapRoomView room = minimap.RoomViewPrefab;
            ValidateSpriteName(
                room.CurrentRoomBackground,
                "BlackandWhiteUI_16",
                "Minimap current-room background",
                errors);
            ValidateSpriteName(
                room.OtherRoomBackground,
                "BlackandWhiteUI_3",
                "Minimap non-current-room background",
                errors);
            ValidateSpriteName(
                room.GetIcon(null),
                "icon_interrogation",
                "Minimap undiscovered-room icon",
                errors);
            ValidateSpriteName(
                room.GetIcon(RoomType.Start),
                "icon_flag",
                "Minimap start-room icon",
                errors);
            ValidateSpriteName(
                room.GetIcon(RoomType.Combat),
                "icon_skull",
                "Minimap combat-room icon",
                errors);
            ValidateSpriteName(
                room.GetIcon(RoomType.BombReward),
                "icon_ring",
                "Minimap bomb-reward-room icon",
                errors);
            ValidateSpriteName(
                room.GetIcon(RoomType.Recovery),
                "icon_heart",
                "Minimap recovery-room icon",
                errors);
            ValidateSpriteName(
                room.GetIcon(RoomType.Secret),
                "icon_chest",
                "Minimap secret-room icon",
                errors);
            ValidateSpriteName(
                room.GetIcon(RoomType.Boss),
                "icon_door",
                "Minimap boss-room icon",
                errors);
            if (room.GetIcon(RoomType.BossAntechamber) != null)
            {
                errors.Add(
                    "Minimap boss-antechamber must not expose a room-type icon.");
            }
        }

        private static void ValidateSpriteName(
            Sprite sprite,
            string expectedName,
            string context,
            ICollection<string> errors)
        {
            if (sprite == null ||
                !string.Equals(
                    sprite.name,
                    expectedName,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"{context} must reference sprite '{expectedName}'.");
            }
        }

        private static void ValidatePauseTitleWave(
            ICollection<string> errors)
        {
            PrototypePauseView pause =
                AssetDatabase.LoadAssetAtPath<PrototypePauseView>(
                    PrototypeInGameUiPrefabAuthoring.PausePrefabPath);
            if (pause == null)
            {
                return;
            }

            PrototypePauseTitleWave[] waves =
                pause.GetComponentsInChildren<PrototypePauseTitleWave>(true);
            if (waves.Length != 1 ||
                waves[0].Target == null ||
                waves[0].Target.gameObject != waves[0].gameObject)
            {
                errors.Add(
                    "Pause UI prefab must contain exactly one title wave " +
                    "with its same-object TMP target assigned.");
            }
        }

        private static void ValidateInGameUiPrefab<T>(
            string assetPath,
            Func<T, bool> hasRequiredReferences,
            ICollection<string> errors)
            where T : MonoBehaviour
        {
            T view = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (view == null)
            {
                errors.Add($"Missing in-game UI prefab view: {assetPath}");
                return;
            }
            if (!hasRequiredReferences(view))
            {
                errors.Add(
                    $"In-game UI prefab has missing authored references: {assetPath}");
            }
        }

        private static void ValidateInGameUiBindings(
            Scene scene,
            ICollection<string> errors)
        {
            PrototypeWeaponHudView weaponPrefab =
                AssetDatabase.LoadAssetAtPath<PrototypeWeaponHudView>(
                    PrototypeInGameUiPrefabAuthoring.WeaponHudPrefabPath);
            PrototypeHealthHudView healthPrefab =
                AssetDatabase.LoadAssetAtPath<PrototypeHealthHudView>(
                    PrototypeInGameUiPrefabAuthoring.HealthHudPrefabPath);
            PrototypeDungeonMinimapView minimapPrefab =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonMinimapView>(
                    PrototypeInGameUiPrefabAuthoring.MinimapPrefabPath);
            PrototypePauseView pausePrefab =
                AssetDatabase.LoadAssetAtPath<PrototypePauseView>(
                    PrototypeInGameUiPrefabAuthoring.PausePrefabPath);

            PrototypeWeaponHud[] weaponHuds =
                FindComponents<PrototypeWeaponHud>(scene);
            for (int index = 0; index < weaponHuds.Length; index++)
            {
                if (weaponHuds[index].ViewPrefab != weaponPrefab)
                {
                    errors.Add(
                        "PrototypeWeaponHud must reference the shared editable weapon HUD prefab.");
                }
            }

            PrototypeHealthHud[] healthHuds =
                FindComponents<PrototypeHealthHud>(scene);
            for (int index = 0; index < healthHuds.Length; index++)
            {
                if (healthHuds[index].ViewPrefab != healthPrefab)
                {
                    errors.Add(
                        "PrototypeHealthHud must reference the shared editable health HUD prefab.");
                }
            }

            PrototypeDungeonMinimapPresenter[] minimaps =
                FindComponents<PrototypeDungeonMinimapPresenter>(scene);
            for (int index = 0; index < minimaps.Length; index++)
            {
                if (minimaps[index].ViewPrefab != minimapPrefab)
                {
                    errors.Add(
                        "PrototypeDungeonMinimapPresenter must reference the shared editable minimap prefab.");
                }
            }

            PrototypeGameSession[] sessions =
                FindComponents<PrototypeGameSession>(scene);
            for (int index = 0; index < sessions.Length; index++)
            {
                if (sessions[index].PauseViewPrefab != pausePrefab)
                {
                    errors.Add(
                        "PrototypeGameSession must reference the shared editable pause prefab.");
                }
            }
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
                ValidateInGameUiBindings(scene, sceneErrors);
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
                PrototypeUserSettingsRuntime[] userSettings =
                    FindComponents<PrototypeUserSettingsRuntime>(scene);
                PrototypeGameSession[] sessions = FindComponents<PrototypeGameSession>(scene);
                PrototypePlayerController[] playerControllers =
                    FindComponents<PrototypePlayerController>(scene);
                PrototypePlayerAnimationPresenter[] playerAnimationPresenters =
                    FindComponents<PrototypePlayerAnimationPresenter>(scene);
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
                PrototypeThrowerPresenter[] throwerPresenters =
                    FindComponents<PrototypeThrowerPresenter>(scene);
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

                ValidateInGameUiBindings(scene, errors);

                if (contexts.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one TestSandboxContext; found {contexts.Length}.");
                }
                if (readers.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one BombSwapInputReader; found {readers.Length}.");
                }
                if (userSettings.Length != 1 ||
                    !userSettings[0].HasRequiredReferences)
                {
                    errors.Add(
                        "TestSandbox must contain one configured PrototypeUserSettingsRuntime.");
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
                if (playerAnimationPresenters.Length != 1)
                {
                    errors.Add(
                        "TestSandbox must contain exactly one " +
                        $"PrototypePlayerAnimationPresenter; found {playerAnimationPresenters.Length}.");
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
                int expectedThrowerPresenterCount = string.Equals(
                    expectedRoomPath,
                    PrototypeCombatThrowerDefinitionPath,
                    StringComparison.Ordinal) ? 1 : 0;
                if (throwerPresenters.Length != expectedThrowerPresenterCount)
                {
                    errors.Add(
                        $"TestSandbox must contain {expectedThrowerPresenterCount} " +
                        "PrototypeThrowerPresenter component(s); found " +
                        $"{throwerPresenters.Length}.");
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
                    Transform player = contexts[0].PlayerPlaceholder;
                    if (controller.Session != sessions[0] ||
                        controller.PlayerTransform != player)
                    {
                        errors.Add(
                            "TestSandbox player controller must reference the canonical player Transform.");
                    }
                    if (float.IsNaN(controller.CellsPerSecond) ||
                        float.IsInfinity(controller.CellsPerSecond) ||
                        controller.CellsPerSecond <= 0f)
                    {
                        errors.Add("TestSandbox player controller speed must be finite and positive.");
                    }
                }

                if (playerAnimationPresenters.Length == 1 &&
                    sessions.Length == 1 && contexts.Length == 1)
                {
                    PrototypePlayerAnimationPresenter presenter = playerAnimationPresenters[0];
                    Transform player = contexts[0].PlayerPlaceholder;
                    GameObject playerSource = player != null
                        ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(player.gameObject)
                        : null;
                    string playerPrefabPath = AssetDatabase.GetAssetPath(playerSource);
                    Animator playerAnimator = player != null
                        ? player.GetComponentInChildren<Animator>(true)
                        : null;
                    if (presenter.Session != sessions[0] ||
                        presenter.Animator != playerAnimator ||
                        playerAnimator == null ||
                        !string.Equals(
                            playerPrefabPath,
                            PlayerPrefabPath,
                            StringComparison.Ordinal))
                    {
                        errors.Add(
                            "TestSandbox player animation presenter must reference the session and " +
                            "Animator on the canonical player prefab.");
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

                if (throwerPresenters.Length == 1 && sessions.Length == 1 &&
                    contexts.Length == 1)
                {
                    PrototypeThrowerPresenter presenter = throwerPresenters[0];
                    Transform runtimePresentation =
                        contexts[0].GridRoot.Find("RuntimePresentation");
                    if (presenter.Session != sessions[0] ||
                        presenter.PresentationRoot != runtimePresentation)
                    {
                        errors.Add(
                            "TestSandbox thrower presenter has inconsistent scene references.");
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
                errors.Add(
                    "Dungeon door presenter is missing a door renderer or secret-crack root.");
                return;
            }

            GameObject authoredSecretWallBreakVfx =
                presenter.SecretWallBreakVfxPrefab;
            if (authoredSecretWallBreakVfx != null &&
                authoredSecretWallBreakVfx
                    .GetComponentsInChildren<ParticleSystem>(true).Length == 0)
            {
                errors.Add(
                    "Dungeon door presenter secret-wall VFX requires at least one ParticleSystem.");
            }

            Renderer[] doors =
            {
                presenter.NorthDoor,
                presenter.EastDoor,
                presenter.SouthDoor,
                presenter.WestDoor,
            };
            Animator[] doorAnimators =
            {
                presenter.NorthDoorAnimator,
                presenter.EastDoorAnimator,
                presenter.SouthDoorAnimator,
                presenter.WestDoorAnimator,
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
            bool validatesDoorPrefabs = IsDungeonPresentationScenePath(
                context.gameObject.scene.path);
            GameObject expectedDoorPrefab = validatesDoorPrefabs
                ? AssetDatabase.LoadAssetAtPath<GameObject>(
                    EnvironmentBlockVisualAuthoring.DoorPrefabPath)
                : null;
            for (int index = 0; index < doors.Length; index++)
            {
                Renderer door = doors[index];
                Animator animator = doorAnimators[index];
                bool matchesDoor = validatesDoorPrefabs
                    ? door != null && animator != null && expectedDoorPrefab != null &&
                      animator.transform.parent == boundary &&
                      string.Equals(
                          animator.gameObject.name,
                          expectedNames[index],
                          StringComparison.Ordinal) &&
                      PrefabUtility.GetCorrespondingObjectFromSource(
                          animator.gameObject) == expectedDoorPrefab &&
                      animator.GetComponentsInChildren<Renderer>(true).Length == 1 &&
                      animator.GetComponentInChildren<Renderer>(true) == door &&
                      animator.GetComponentsInChildren<Collider>(true).Length == 0 &&
                      animator.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                      HasAnimatorBoolParameter(animator, "IsOpen")
                    : door != null && door.transform.parent == boundary &&
                      string.Equals(
                          door.gameObject.name,
                          expectedNames[index],
                          StringComparison.Ordinal) &&
                      door.GetComponent<Collider>() == null;
                if (!matchesDoor)
                {
                    errors.Add(
                        validatesDoorPrefabs
                            ? $"Dungeon {expectedNames[index]} must use the collider-free Door prefab with one Animator and an IsOpen bool parameter."
                            : $"Dungeon {expectedNames[index]} must be a collider-free panel under BoundaryWalls.");
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
                if (validatesDoorPrefabs)
                {
                    GameObject expectedCrackedPrefab =
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            EnvironmentBlockVisualAuthoring.CrackedBrickBlockPrefabPath);
                    Transform doorRoot = doorAnimators[index] != null
                        ? doorAnimators[index].transform
                        : null;
                    if (root == null || expectedCrackedPrefab == null ||
                        root.transform.parent != boundary || root.activeSelf ||
                        !string.Equals(
                            root.name,
                            expectedCrackNames[index],
                            StringComparison.Ordinal) ||
                        PrefabUtility.GetCorrespondingObjectFromSource(root) !=
                            expectedCrackedPrefab ||
                        root.GetComponentsInChildren<Renderer>(true).Length == 0 ||
                        root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                        root.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                        doorRoot == null ||
                        Vector3.SqrMagnitude(
                            root.transform.position - doorRoot.position) > 0.000001f)
                    {
                        errors.Add(
                            $"Dungeon {expectedCrackNames[index]} must use the inactive collider-free CrackedBrickBlock prefab at the matching {expectedNames[index]} position.");
                    }
                    continue;
                }
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
            bool validatesEnvironmentVisuals = IsDungeonPresentationScenePath(
                context.gameObject.scene.path);
            for (int index = 0; index < obstacles.childCount; index++)
            {
                Transform obstacle = obstacles.GetChild(index);
                GridPosition cell = context.GridSpace.WorldToGrid(obstacle.position);
                if (validatesEnvironmentVisuals &&
                    !Mathf.Approximately(obstacle.localPosition.y, 0f))
                {
                    errors.Add(
                        $"Dungeon obstacle {obstacle.name} local Y must be 0.");
                }
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

            GameObject brickPrefab = validatesEnvironmentVisuals
                ? AssetDatabase.LoadAssetAtPath<GameObject>(
                    EnvironmentBlockVisualAuthoring.BrickBlockPrefabPath)
                : null;
            GameObject cornerPrefab = validatesEnvironmentVisuals
                ? AssetDatabase.LoadAssetAtPath<GameObject>(
                    EnvironmentBlockVisualAuthoring.BrickCornerPrefabPath)
                : null;
            for (int index = 0;
                 validatesEnvironmentVisuals && index < obstacles.childCount;
                 index++)
            {
                Transform visual = obstacles.GetChild(index).Find("Visual");
                if (!IsExpectedVisualPrefab(visual, cornerPrefab))
                {
                    errors.Add(
                        $"TestSandbox obstacle {obstacles.GetChild(index).name} must use the collider-free BrickCorner visual prefab.");
                }
            }

            if (validatesEnvironmentVisuals)
            {
                Transform floorVisuals =
                    context.GridRoot.Find("Environment/FloorVisuals");
                int expectedFloorCount = (room.Width * room.Depth) + 4;
                if (floorVisuals != null &&
                    !Mathf.Approximately(
                        floorVisuals.localPosition.y,
                        EnvironmentBlockVisualAuthoring.FloorVisualRootY))
                {
                    errors.Add(
                        $"Dungeon FloorVisuals Y must be {EnvironmentBlockVisualAuthoring.FloorVisualRootY}.");
                }
                if (floorVisuals == null || floorVisuals.childCount != expectedFloorCount)
                {
                    errors.Add(
                        $"TestSandbox floor must contain {expectedFloorCount} BrickBlock visual cells.");
                }
                else
                {
                    var actualFloorPositions = new HashSet<Vector3>();
                    for (int index = 0; index < floorVisuals.childCount; index++)
                    {
                        actualFloorPositions.Add(
                            floorVisuals.GetChild(index).localPosition);
                        if (!IsExpectedVisualPrefab(floorVisuals.GetChild(index), brickPrefab))
                        {
                            errors.Add("TestSandbox floor contains an invalid visual prefab.");
                            break;
                        }
                    }
                    float cellSize = context.GridSpace.CellSize;
                    int halfWidth = room.Width / 2;
                    int halfDepth = room.Depth / 2;
                    var expectedFloorPositions = new HashSet<Vector3>();
                    for (int x = -halfWidth; x <= halfWidth; x++)
                    {
                        for (int z = -halfDepth; z <= halfDepth; z++)
                        {
                            expectedFloorPositions.Add(
                                new Vector3(x * cellSize, 0f, z * cellSize));
                        }
                    }
                    int edgeX = halfWidth + 1;
                    int edgeZ = halfDepth + 1;
                    expectedFloorPositions.Add(new Vector3(0f, 0f, edgeZ * cellSize));
                    expectedFloorPositions.Add(new Vector3(edgeX * cellSize, 0f, 0f));
                    expectedFloorPositions.Add(new Vector3(0f, 0f, -edgeZ * cellSize));
                    expectedFloorPositions.Add(new Vector3(-edgeX * cellSize, 0f, 0f));
                    if (!actualFloorPositions.SetEquals(expectedFloorPositions))
                    {
                        errors.Add(
                            "TestSandbox floor cells must cover the authored room and all four door positions exactly.");
                    }
                }

                Transform boundaryVisuals =
                    context.GridRoot.Find("Environment/BoundaryVisuals");
                Transform boundaryBaseVisuals =
                    context.GridRoot.Find("Environment/BoundaryBaseVisuals");
                int expectedBoundaryCount =
                    (2 * (room.Width - 1)) + (2 * (room.Depth - 1)) + 4;
                if (boundaryVisuals == null ||
                    boundaryVisuals.childCount != expectedBoundaryCount)
                {
                    errors.Add(
                        $"TestSandbox boundary must contain {expectedBoundaryCount} brick visual cells.");
                }
                else
                {
                    for (int index = 0; index < boundaryVisuals.childCount; index++)
                    {
                        Transform visual = boundaryVisuals.GetChild(index);
                        if (!IsExpectedVisualPrefab(visual, cornerPrefab))
                        {
                            errors.Add("TestSandbox boundary contains an invalid visual prefab.");
                            break;
                        }
                    }
                }
                if (boundaryBaseVisuals != null)
                {
                    errors.Add(
                        "TestSandbox must not contain legacy BoundaryBaseVisuals; door support belongs to FloorVisuals.");
                }
            }


            Transform destructibleObstacles =
                context.GridRoot.Find("Environment/DestructibleObstacles");
            if (destructibleObstacles == null)
            {
                errors.Add("TestSandbox is missing Environment/DestructibleObstacles.");
                return;
            }

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
                Material destructibleMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    DestructibleWallMaterialPath);
                Transform visual = obstacle.Find("Visual");
                GameObject woodBoxPrefab = validatesEnvironmentVisuals
                    ? AssetDatabase.LoadAssetAtPath<GameObject>(
                        EnvironmentBlockVisualAuthoring.WoodBoxPrefabPath)
                    : null;
                bool matchesVisual = validatesEnvironmentVisuals
                    ? renderers.Length > 0 &&
                      IsExpectedVisualPrefab(visual, woodBoxPrefab)
                    : renderers.Length == 4 && destructibleMaterial != null &&
                      renderers.All(renderer =>
                          renderer.sharedMaterial == destructibleMaterial);
                if (!matchesVisual)
                {
                    errors.Add(
                        $"TestSandbox destructible visual {obstacle.name} must contain a visible model.");
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

        private static bool HasAnimatorBoolParameter(
            Animator animator,
            string parameterName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }
            return animator.parameters.Any(parameter =>
                parameter.type == AnimatorControllerParameterType.Bool &&
                string.Equals(
                    parameter.name,
                    parameterName,
                    StringComparison.Ordinal));
        }

        private static bool IsExpectedVisualPrefab(
            Transform visual,
            GameObject expectedPrefab)
        {
            if (visual == null || expectedPrefab == null ||
                PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject) !=
                    expectedPrefab ||
                visual.GetComponentsInChildren<Collider>(true).Length != 0)
            {
                return false;
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 && renderers.Any(renderer => renderer.enabled);
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
                LobbyScenePath,
                DungeonStartScenePath,
                DungeonRewardScenePath,
                DungeonBossAnteScenePath,
                DungeonRecoveryScenePath,
                DungeonSecretScenePath,
                DungeonBossScenePath,
                TestSandboxScenePath,
                TestSandboxThrowerScenePath,
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
                    "Build Settings must enable the Lobby first, followed by the Start placeholder and every dungeon room scene.");
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
