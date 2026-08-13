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
        public const string BombPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/BombPlaceholder.prefab";
        public const string ExplosionCellPrefabPath =
            "Assets/Game/Content/Prefabs/Prototype/ExplosionCellPlaceholder.prefab";

        public static void Validate(ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ValidateInputActions(errors);
            ValidatePrototypeBombDefinition(errors);
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
                    if (session.Context != contexts[0] || session.InputReader != readers[0] ||
                        !string.Equals(
                            definitionPath,
                            PrototypeBombDefinitionPath,
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
                        context.PlayerSpawn == null || context.PlayerPlaceholder == null)
                    {
                        errors.Add("TestSandboxContext has missing required references.");
                    }
                    if (context.GridWidth <= 0 || (context.GridWidth & 1) == 0 ||
                        context.GridDepth <= 0 || (context.GridDepth & 1) == 0)
                    {
                        errors.Add("TestSandboxContext grid dimensions must be positive odd numbers.");
                    }
                    if (float.IsNaN(context.CellSize) || float.IsInfinity(context.CellSize) || context.CellSize <= 0f)
                    {
                        errors.Add("TestSandboxContext cell size must be finite and positive.");
                    }

                    ValidateBlockedCells(context, errors);
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

        private static void ValidateBlockedCells(
            TestSandboxContext context,
            ICollection<string> errors)
        {
            if (context.BlockedCells == null)
            {
                errors.Add("TestSandboxContext blocked cells are missing.");
                return;
            }

            int halfWidth = context.GridWidth / 2;
            int halfDepth = context.GridDepth / 2;
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int blocker in context.BlockedCells)
            {
                if (blocker.x < -halfWidth || blocker.x > halfWidth ||
                    blocker.y < -halfDepth || blocker.y > halfDepth)
                {
                    errors.Add($"TestSandbox blocked cell is outside the grid: {blocker}.");
                }
                if (!seen.Add(blocker))
                {
                    errors.Add($"TestSandbox contains duplicate blocked cell: {blocker}.");
                }
            }

            if (seen.Count != 4)
            {
                errors.Add($"TestSandbox must declare four prototype blocked cells; found {seen.Count}.");
            }

            if (context.PlayerSpawn != null)
            {
                GridPosition spawn = context.GridSpace.WorldToGrid(context.PlayerSpawn.position);
                if (seen.Contains(new Vector2Int(spawn.X, spawn.Z)))
                {
                    errors.Add($"TestSandbox player spawn cell is blocked: {spawn}.");
                }
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
