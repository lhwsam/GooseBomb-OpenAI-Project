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
        public const string PrototypeBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeCrossBomb.asset";
        public const string PrototypeAreaBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeAreaBomb.asset";
        public const string PrototypeBombLoadoutPath =
            "Assets/Game/Content/Bombs/PrototypeBombLoadout.asset";
        public const string PrototypePlayerVitalsPath =
            "Assets/Game/Content/Player/PrototypePlayerVitals.asset";
        public const string PrototypeChaserDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeChaser.asset";
        public const string PrototypeChargerDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeCharger.asset";
        public const string PrototypeArmoredDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeArmored.asset";
        public const string PrototypeCombatRoomDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatLoop.asset";
        public const string PrototypeCombatLanesDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatLanes.asset";
        public const string PrototypeCombatPillarsDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatPillars.asset";
        public const string PrototypeCombatArmorDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatArmor.asset";
        public const string BombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BombPlaceholder.prefab";
        public const string ExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ExplosionCellPlaceholder.prefab";
        public const string AreaBombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/AreaBombPlaceholder.prefab";
        public const string AreaExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/AreaExplosionCellPlaceholder.prefab";
        public const string ChaserPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChaserPlaceholder.prefab";
        public const string ChargerPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChargerPlaceholder.prefab";
        public const string ArmoredPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ArmoredPlaceholder.prefab";
        public const string DestructibleWallMaterialPath =
            "Assets/Game/Content/Materials/Prototype/DestructibleWall.mat";

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
            ValidatePrototypeCombatRoomDefinitions(errors);
            ValidateDestructibleWallMaterial(errors);
            ValidateTestSandboxes(errors);
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
            if (definition.ChargerPrefab != null &&
                definition.ChargerPrefab.GetComponentInChildren<Collider>(true) != null)
            {
                errors.Add("Prototype charger prefab must not contain a Collider; logical grid owns collision.");
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
                    definition.BrokenCellsPerSecond != 3f)
                {
                    errors.Add(
                        "Prototype armored definition must use two stages and the 1-to-3 cells/second phase contract.");
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
        }

        private static void ValidatePrototypeCombatRoomDefinitions(ICollection<string> errors)
        {
            string[] expectedPaths =
            {
                PrototypeCombatRoomDefinitionPath,
                PrototypeCombatLanesDefinitionPath,
                PrototypeCombatPillarsDefinitionPath,
                PrototypeCombatArmorDefinitionPath,
            };
            string[] expectedIds =
            {
                "prototype-combat-loop",
                "prototype-combat-lanes",
                "prototype-combat-pillars",
                "prototype-combat-armor",
            };
            GridPosition?[] expectedChargerSpawns =
            {
                null,
                null,
                new GridPosition(-3, 2),
                null,
            };
            GridPosition?[] expectedArmoredSpawns =
            {
                null,
                null,
                null,
                new GridPosition(0, 1),
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
                }
                catch (Exception exception)
                {
                    errors.Add($"Invalid prototype combat room definition '{path}': {exception.Message}");
                }
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
                "TestSandboxLanes",
                errors);
            ValidateTestSandboxScene(
                TestSandboxLanesScenePath,
                PrototypeCombatLanesDefinitionPath,
                "TestSandboxPillars",
                errors);
            ValidateTestSandboxScene(
                TestSandboxPillarsScenePath,
                PrototypeCombatPillarsDefinitionPath,
                "TestSandboxArmor",
                errors);
            ValidateTestSandboxScene(
                TestSandboxArmorScenePath,
                PrototypeCombatArmorDefinitionPath,
                string.Empty,
                errors);
        }

        private static void ValidateTestSandboxScene(
            string scenePath,
            string expectedRoomPath,
            string expectedNextSceneName,
            ICollection<string> errors)
        {
            var sceneErrors = new List<string>();
            ValidateTestSandboxSceneContents(
                scenePath,
                expectedRoomPath,
                expectedNextSceneName,
                sceneErrors);
            foreach (string error in sceneErrors)
            {
                errors.Add($"{scenePath}: {error}");
            }
        }

        private static void ValidateTestSandboxSceneContents(
            string scenePath,
            string expectedRoomPath,
            string expectedNextSceneName,
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
                PrototypeWeaponHud[] weaponHuds = FindComponents<PrototypeWeaponHud>(scene);
                PrototypeInputHarnessProbe[] probes = FindComponents<PrototypeInputHarnessProbe>(scene);
                PrototypeRoomAdvanceController[] roomAdvanceControllers =
                    FindComponents<PrototypeRoomAdvanceController>(scene);
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
                if (weaponHuds.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeWeaponHud; found {weaponHuds.Length}.");
                }
                if (probes.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one PrototypeInputHarnessProbe; found {probes.Length}.");
                }
                if (roomAdvanceControllers.Length != 1)
                {
                    errors.Add(
                        $"TestSandbox must contain exactly one PrototypeRoomAdvanceController; found {roomAdvanceControllers.Length}.");
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
                            StringComparison.Ordinal))
                    {
                        errors.Add("TestSandbox game session has inconsistent runtime references.");
                    }
                    if (!IsFinitePositive(session.CellsPerSecond) ||
                        !IsFinitePositive(session.ChainDelaySeconds))
                    {
                        errors.Add("TestSandbox game session timing values must be finite and positive.");
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

                if (weaponHuds.Length == 1 && sessions.Length == 1 &&
                    weaponHuds[0].Session != sessions[0])
                {
                    errors.Add("TestSandbox weapon HUD has an inconsistent session reference.");
                }

                if (probes.Length == 1 && readers.Length == 1 && sessions.Length == 1 &&
                    (probes[0].InputReader != readers[0] ||
                     probes[0].Session != sessions[0]))
                {
                    errors.Add("TestSandbox harness probe has inconsistent runtime references.");
                }

                if (roomAdvanceControllers.Length == 1 && sessions.Length == 1)
                {
                    PrototypeRoomAdvanceController controller = roomAdvanceControllers[0];
                    if (controller.Session != sessions[0] ||
                        !string.Equals(
                            controller.NextSceneName,
                            expectedNextSceneName,
                            StringComparison.Ordinal) ||
                        !IsFinitePositive(controller.TransitionDelaySeconds))
                    {
                        errors.Add(
                            "TestSandbox room advance controller has inconsistent session, next scene, or timing.");
                    }
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

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
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
                TestSandboxScenePath,
                TestSandboxLanesScenePath,
                TestSandboxPillarsScenePath,
                TestSandboxArmorScenePath,
            };
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();
            if (enabledScenes.Length < expectedScenePaths.Length)
            {
                errors.Add("Build Settings must enable all four playtest room scenes first.");
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
