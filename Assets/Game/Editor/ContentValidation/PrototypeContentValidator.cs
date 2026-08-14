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
        public const string PrototypeBombDefinitionPath =
            "Assets/Game/Content/Bombs/PrototypeCrossBomb.asset";
        public const string PrototypePlayerVitalsPath =
            "Assets/Game/Content/Player/PrototypePlayerVitals.asset";
        public const string PrototypeChaserDefinitionPath =
            "Assets/Game/Content/Enemies/PrototypeChaser.asset";
        public const string PrototypeCombatRoomDefinitionPath =
            "Assets/Game/Content/Rooms/PrototypeCombatLoop.asset";
        public const string BombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BombPlaceholder.prefab";
        public const string ExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ExplosionCellPlaceholder.prefab";
        public const string ChaserPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ChaserPlaceholder.prefab";

        public static void Validate(ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ValidateInputActions(errors);
            ValidatePrototypeBombDefinition(errors);
            ValidatePrototypePlayerVitals(errors);
            ValidatePrototypeChaserDefinition(errors);
            ValidatePrototypeCombatRoomDefinition(errors);
            ValidateTestSandbox(errors);
            ValidateBuildSettings(errors);
        }

        private static void ValidatePrototypeBombDefinition(ICollection<string> errors)
        {
            PrototypeBombDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeBombDefinitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype bomb definition: {PrototypeBombDefinitionPath}");
                return;
            }

            try
            {
                definition.CreateCoreDefinition();
                definition.ValidatePresentationReferences();
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype bomb definition: {exception.Message}");
            }

            string bombPrefabPath = AssetDatabase.GetAssetPath(definition.BombPrefab);
            if (!string.Equals(bombPrefabPath, BombPrefabPath, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype bomb definition must reference '{BombPrefabPath}', found '{bombPrefabPath}'.");
            }
            string explosionPrefabPath = AssetDatabase.GetAssetPath(definition.ExplosionCellPrefab);
            if (!string.Equals(explosionPrefabPath, ExplosionCellPrefabPath, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Prototype bomb definition must reference '{ExplosionCellPrefabPath}', found '{explosionPrefabPath}'.");
            }
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

        private static void ValidatePrototypeCombatRoomDefinition(ICollection<string> errors)
        {
            PrototypeCombatRoomDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeCombatRoomDefinitionAsset>(
                    PrototypeCombatRoomDefinitionPath);
            if (definition == null)
            {
                errors.Add($"Missing prototype combat room definition: {PrototypeCombatRoomDefinitionPath}");
                return;
            }

            try
            {
                CombatRoomDefinition room = definition.CreateCoreDefinition();
                if (room.Id != new RoomDefinitionId("prototype-combat-loop") ||
                    room.RoomType != RoomType.Combat)
                {
                    errors.Add(
                        "Prototype combat room must use ID 'prototype-combat-loop' and Combat type.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Invalid prototype combat room definition: {exception.Message}");
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

        private static void ValidateTestSandbox(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestSandboxScenePath) == null)
            {
                errors.Add($"Missing TestSandbox scene: {TestSandboxScenePath}");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(TestSandboxScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(TestSandboxScenePath, OpenSceneMode.Additive);
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
                PrototypePlayerHealthPresenter[] healthPresenters =
                    FindComponents<PrototypePlayerHealthPresenter>(scene);
                PrototypeChaserPresenter[] chaserPresenters =
                    FindComponents<PrototypeChaserPresenter>(scene);
                PrototypeInputHarnessProbe[] probes = FindComponents<PrototypeInputHarnessProbe>(scene);
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
                if (probes.Length != 1)
                {
                    errors.Add($"TestSandbox must contain exactly one PrototypeInputHarnessProbe; found {probes.Length}.");
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
                    string definitionPath = AssetDatabase.GetAssetPath(session.BombDefinition);
                    string playerVitalsPath = AssetDatabase.GetAssetPath(session.PlayerVitals);
                    string chaserDefinitionPath = AssetDatabase.GetAssetPath(
                        session.ChaserDefinition);
                    if (session.Context != contexts[0] || session.InputReader != readers[0] ||
                        !string.Equals(
                            definitionPath,
                            PrototypeBombDefinitionPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            playerVitalsPath,
                            PrototypePlayerVitalsPath,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            chaserDefinitionPath,
                            PrototypeChaserDefinitionPath,
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

                if (probes.Length == 1 && readers.Length == 1 && sessions.Length == 1 &&
                    (probes[0].InputReader != readers[0] ||
                     probes[0].Session != sessions[0]))
                {
                    errors.Add("TestSandbox harness probe has inconsistent runtime references.");
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
                    ValidateRoomSceneBinding(context, errors);
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
            ICollection<string> errors)
        {
            if (context.RoomDefinition == null || context.GridRoot == null)
            {
                return;
            }

            string roomPath = AssetDatabase.GetAssetPath(context.RoomDefinition);
            if (!string.Equals(
                    roomPath,
                    PrototypeCombatRoomDefinitionPath,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"TestSandbox room authority must reference '{PrototypeCombatRoomDefinitionPath}', found '{roomPath}'.");
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
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();
            if (enabledScenes.Length == 0 ||
                !string.Equals(enabledScenes[0].path, TestSandboxScenePath, StringComparison.Ordinal))
            {
                errors.Add($"TestSandbox must be the first enabled Build Settings scene: {TestSandboxScenePath}");
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
