using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeContentBuilder
    {
        private const string MaterialsPath = "Assets/Game/Content/Materials/Prototype";

        [MenuItem("Bomb Swap/Prototype/Create Missing Prototype Content")]
        public static void CreateMissingPrototypeContentMenu()
        {
            string summary = CreateMissingPrototypeContent();
            Debug.Log(summary);
        }

        public static string CreateMissingPrototypeContent()
        {
            InputActionAsset inputActions = CreateInputActionsIfMissing();
            bool sceneCreated = CreateTestSandboxIfMissing(inputActions);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();

            return sceneCreated
                ? "Created BombSwap Input Actions, TestSandbox content, and Build Settings entry."
                : "BombSwap prototype content already existed; ensured Build Settings entry.";
        }

        private static InputActionAsset CreateInputActionsIfMissing()
        {
            InputActionAsset imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                PrototypeContentValidator.InputActionsPath);
            if (imported != null)
            {
                return imported;
            }

            string absolutePath = Path.Combine(
                Application.dataPath,
                "Game/Content/Input/BombSwapInputActions.inputactions");
            if (File.Exists(absolutePath))
            {
                throw new InvalidOperationException(
                    $"Input Actions file exists but Unity could not import it: {PrototypeContentValidator.InputActionsPath}");
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                asset.name = "BombSwapInputActions";
                InputActionMap gameplay = asset.AddActionMap(BombSwapInputActionNames.GameplayMap);

                InputAction move = gameplay.AddAction(
                    BombSwapInputActionNames.Move,
                    InputActionType.Value,
                    expectedControlLayout: "Vector2");
                move.AddCompositeBinding("2DVector(mode=1)")
                    .With("Up", "<Keyboard>/w", "Keyboard")
                    .With("Down", "<Keyboard>/s", "Keyboard")
                    .With("Left", "<Keyboard>/a", "Keyboard")
                    .With("Right", "<Keyboard>/d", "Keyboard");
                move.AddCompositeBinding("2DVector(mode=1)")
                    .With("Up", "<Keyboard>/upArrow", "Keyboard")
                    .With("Down", "<Keyboard>/downArrow", "Keyboard")
                    .With("Left", "<Keyboard>/leftArrow", "Keyboard")
                    .With("Right", "<Keyboard>/rightArrow", "Keyboard");
                move.AddBinding(
                    "<Gamepad>/leftStick",
                    processors: "stickDeadzone(min=0.5)",
                    groups: "Gamepad");
                move.AddBinding("<Gamepad>/dpad", groups: "Gamepad");

                AddButtonBindings(
                    gameplay,
                    BombSwapInputActionNames.PlaceBomb,
                    "<Keyboard>/z",
                    "<Gamepad>/buttonSouth");
                AddButtonBindings(
                    gameplay,
                    BombSwapInputActionNames.SwapBomb,
                    "<Keyboard>/x",
                    "<Gamepad>/buttonWest");
                AddButtonBindings(
                    gameplay,
                    BombSwapInputActionNames.Pause,
                    "<Keyboard>/escape",
                    "<Gamepad>/start");

                asset.AddControlScheme("Keyboard").WithRequiredDevice("<Keyboard>");
                asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");

                File.WriteAllText(absolutePath, asset.ToJson(), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(
                    PrototypeContentValidator.InputActionsPath,
                    ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }

            imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                PrototypeContentValidator.InputActionsPath);
            if (imported == null)
            {
                throw new InvalidOperationException(
                    $"Unity could not import generated Input Actions: {PrototypeContentValidator.InputActionsPath}");
            }

            return imported;
        }

        private static bool CreateTestSandboxIfMissing(InputActionAsset inputActions)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeContentValidator.TestSandboxScenePath) != null)
            {
                return false;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            Material floorMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Floor.mat",
                shader,
                new Color(0.12f, 0.18f, 0.24f, 1f));
            Material wallMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Wall.mat",
                shader,
                new Color(0.26f, 0.31f, 0.38f, 1f));
            Material gridMaterial = GetOrCreateMaterial(
                MaterialsPath + "/GridLine.mat",
                shader,
                new Color(0.28f, 0.39f, 0.48f, 1f));
            Material playerMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Player.mat",
                shader,
                new Color(1f, 0.69f, 0.12f, 1f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TestSandbox");

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            systems.SetActive(false);
            BombSwapInputReader inputReader = systems.AddComponent<BombSwapInputReader>();
            inputReader.Configure(inputActions);
            PrototypeInputHarnessProbe harnessProbe = systems.AddComponent<PrototypeInputHarnessProbe>();
            harnessProbe.Configure(inputReader);
            systems.SetActive(true);

            var gridRoot = new GameObject("GridRoot");
            gridRoot.transform.SetParent(root.transform, false);

            Transform environment = CreateChild("Environment", gridRoot.transform);
            CreatePrimitive(
                "Floor",
                PrimitiveType.Cube,
                environment,
                new Vector3(0f, -0.1f, 0f),
                new Vector3(11f, 0.2f, 9f),
                floorMaterial,
                true);

            Transform gridLines = CreateChild("GridLines", environment);
            for (int x = -5; x <= 6; x++)
            {
                CreatePrimitive(
                    "GridLineX_" + x,
                    PrimitiveType.Cube,
                    gridLines,
                    new Vector3(x - 0.5f, 0.0125f, 0f),
                    new Vector3(0.025f, 0.025f, 9f),
                    gridMaterial,
                    false);
            }
            for (int z = -4; z <= 5; z++)
            {
                CreatePrimitive(
                    "GridLineZ_" + z,
                    PrimitiveType.Cube,
                    gridLines,
                    new Vector3(0f, 0.0125f, z - 0.5f),
                    new Vector3(11f, 0.025f, 0.025f),
                    gridMaterial,
                    false);
            }

            Transform boundary = CreateChild("BoundaryWalls", environment);
            CreatePrimitive("NorthWall", PrimitiveType.Cube, boundary, new Vector3(0f, 0.5f, 5f), new Vector3(13f, 1f, 1f), wallMaterial, true);
            CreatePrimitive("SouthWall", PrimitiveType.Cube, boundary, new Vector3(0f, 0.5f, -5f), new Vector3(13f, 1f, 1f), wallMaterial, true);
            CreatePrimitive("EastWall", PrimitiveType.Cube, boundary, new Vector3(6f, 0.5f, 0f), new Vector3(1f, 1f, 9f), wallMaterial, true);
            CreatePrimitive("WestWall", PrimitiveType.Cube, boundary, new Vector3(-6f, 0.5f, 0f), new Vector3(1f, 1f, 9f), wallMaterial, true);

            Transform obstacles = CreateChild("InteriorObstacles", environment);
            CreatePrimitive("Obstacle_West", PrimitiveType.Cube, obstacles, new Vector3(-2f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), wallMaterial, true);
            CreatePrimitive("Obstacle_East", PrimitiveType.Cube, obstacles, new Vector3(2f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), wallMaterial, true);
            CreatePrimitive("Obstacle_North", PrimitiveType.Cube, obstacles, new Vector3(0f, 0.5f, 2f), new Vector3(0.9f, 1f, 0.9f), wallMaterial, true);
            CreatePrimitive("Obstacle_South", PrimitiveType.Cube, obstacles, new Vector3(0f, 0.5f, -2f), new Vector3(0.9f, 1f, 0.9f), wallMaterial, true);

            Transform playerSpawn = CreateChild("PlayerSpawn", gridRoot.transform);
            GameObject player = CreatePrimitive(
                "PlayerPlaceholder",
                PrimitiveType.Capsule,
                gridRoot.transform,
                new Vector3(0f, 0.5f, 0f),
                new Vector3(0.35f, 0.5f, 0.35f),
                playerMaterial,
                true);
            player.tag = "Player";

            CreateCamera(root.transform);
            CreateDirectionalLight(root.transform);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.42f, 1f);

            TestSandboxContext context = root.AddComponent<TestSandboxContext>();
            context.Configure(
                inputReader,
                gridRoot.transform,
                playerSpawn,
                player.transform,
                11,
                9,
                1f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, PrototypeContentValidator.TestSandboxScenePath))
            {
                throw new InvalidOperationException("Unity failed to save TestSandbox scene.");
            }

            return true;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(PrototypeContentValidator.TestSandboxScenePath, true),
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (string.Equals(
                    existing.path,
                    PrototypeContentValidator.TestSandboxScenePath,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                bool enabled = !string.Equals(
                    existing.path,
                    "Assets/Scenes/SampleScene.unity",
                    StringComparison.Ordinal) && existing.enabled;
                scenes.Add(new EditorBuildSettingsScene(existing.path, enabled));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddButtonBindings(
            InputActionMap map,
            string actionName,
            string keyboardPath,
            string gamepadPath)
        {
            InputAction action = map.AddAction(
                actionName,
                InputActionType.Button,
                expectedControlLayout: "Button");
            action.AddBinding(keyboardPath, groups: "Keyboard");
            action.AddBinding(gamepadPath, groups: "Gamepad");
        }

        private static Material GetOrCreateMaterial(string assetPath, Shader shader, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                return material;
            }

            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(assetPath),
                color = color,
                enableInstancing = true,
            };
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.15f);
            }
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static Transform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            GameObject instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = localScale;

            Renderer renderer = instance.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return instance;
        }

        private static void CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 12f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateDirectionalLight(Transform parent)
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }
    }
}
