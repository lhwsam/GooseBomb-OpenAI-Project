using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BombSwap.Core;
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
        private const string PrototypePrefabsPath = "Assets/Game/Content/Prefabs/Prototype";

        [MenuItem("Bomb Swap/Prototype/Create Missing Prototype Content")]
        public static void CreateMissingPrototypeContentMenu()
        {
            string summary = CreateMissingPrototypeContent();
            Debug.Log(summary);
        }

        public static string CreateMissingPrototypeContent()
        {
            InputActionAsset inputActions = CreateInputActionsIfMissing();
            PrototypeBombDefinitionAsset bombDefinition =
                CreatePrototypeBombContentIfMissing();
            PrototypePlayerVitalsAsset playerVitals = CreatePrototypePlayerVitalsIfMissing();
            PrototypeChaserDefinitionAsset chaserDefinition =
                CreatePrototypeChaserContentIfMissing();
            PrototypeCombatRoomDefinitionAsset[] roomDefinitions =
                CreatePrototypeCombatRoomContentIfMissing();
            bool sceneCreated = EnsureTestSandbox(
                inputActions,
                bombDefinition,
                playerVitals,
                chaserDefinition,
                roomDefinitions[0],
                "TestSandboxLanes");
            bool lanesSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxLanesScenePath,
                bombDefinition,
                playerVitals,
                chaserDefinition,
                roomDefinitions[1],
                "TestSandboxPillars");
            bool pillarsSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxPillarsScenePath,
                bombDefinition,
                playerVitals,
                chaserDefinition,
                roomDefinitions[2],
                string.Empty);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();

            return sceneCreated || lanesSceneCreated || pillarsSceneCreated
                ? "Created BombSwap prototype content and the three-room TestSandbox playtest sequence."
                : "BombSwap prototype content exists; synchronized three room scenes, transitions, references, and Build Settings.";
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

        private static PrototypeBombDefinitionAsset CreatePrototypeBombContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Bombs");

            Material bombMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Bomb.mat",
                shader,
                new Color(0.07f, 0.08f, 0.1f, 1f));
            Material explosionMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Explosion.mat",
                shader,
                new Color(1f, 0.24f, 0.02f, 1f));
            if (explosionMaterial.HasProperty("_EmissionColor"))
            {
                explosionMaterial.EnableKeyword("_EMISSION");
                explosionMaterial.SetColor("_EmissionColor", new Color(1f, 0.08f, 0f, 1f));
                EditorUtility.SetDirty(explosionMaterial);
            }

            GameObject bombPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BombPrefabPath,
                "BombPlaceholder",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.32f, 0f),
                new Vector3(0.62f, 0.62f, 0.62f),
                bombMaterial);
            GameObject explosionPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ExplosionCellPrefabPath,
                "ExplosionCellPlaceholder",
                PrimitiveType.Cube,
                new Vector3(0f, 0.07f, 0f),
                new Vector3(0.9f, 0.14f, 0.9f),
                explosionMaterial);

            PrototypeBombDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeContentValidator.PrototypeBombDefinitionPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
            definition.name = "PrototypeCrossBomb";
            definition.Configure(
                "prototype-cross",
                2f,
                2,
                bombPrefab,
                explosionPrefab,
                0.25f);
            AssetDatabase.CreateAsset(
                definition,
                PrototypeContentValidator.PrototypeBombDefinitionPath);
            return definition;
        }

        private static PrototypePlayerVitalsAsset CreatePrototypePlayerVitalsIfMissing()
        {
            EnsureAssetFolder("Assets/Game/Content/Player");
            PrototypePlayerVitalsAsset vitals =
                AssetDatabase.LoadAssetAtPath<PrototypePlayerVitalsAsset>(
                    PrototypeContentValidator.PrototypePlayerVitalsPath);
            if (vitals != null)
            {
                return vitals;
            }

            vitals = ScriptableObject.CreateInstance<PrototypePlayerVitalsAsset>();
            vitals.name = "PrototypePlayerVitals";
            vitals.Configure(5, 0.75f);
            AssetDatabase.CreateAsset(
                vitals,
                PrototypeContentValidator.PrototypePlayerVitalsPath);
            return vitals;
        }

        private static PrototypeChaserDefinitionAsset CreatePrototypeChaserContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Enemies");
            Material chaserMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Chaser.mat",
                shader,
                new Color(0.18f, 0.82f, 0.38f, 1f));
            GameObject chaserPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ChaserPrefabPath,
                "ChaserPlaceholder",
                PrimitiveType.Capsule,
                Vector3.zero,
                new Vector3(0.36f, 0.45f, 0.36f),
                chaserMaterial);

            PrototypeChaserDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeChaserDefinitionAsset>(
                    PrototypeContentValidator.PrototypeChaserDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PrototypeChaserDefinitionAsset>();
                definition.name = "PrototypeChaser";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeChaserDefinitionPath);
            }

            definition.Configure(
                "prototype-chaser",
                1,
                1,
                2f,
                2,
                chaserPrefab,
                0.45f,
                0.12f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeCombatRoomDefinitionAsset[] CreatePrototypeCombatRoomContentIfMissing()
        {
            EnsureAssetFolder("Assets/Game/Content/Rooms");
            PrototypeCombatRoomDefinitionAsset loop = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeCombatRoomDefinitionPath,
                "PrototypeCombatLoop");
            loop.Configure(
                "prototype-combat-loop",
                RoomType.Combat,
                11,
                9,
                1f,
                Vector2Int.zero,
                new Vector2Int(1, -1),
                new[]
                {
                    new Vector2Int(-2, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(0, 2),
                    new Vector2Int(0, -2),
                },
                new[]
                {
                    Vector2Int.zero,
                    new Vector2Int(0, 1),
                    new Vector2Int(-1, 0),
                },
                new[]
                {
                    new Vector2Int(-3, 1),
                    new Vector2Int(3, 1),
                },
                new[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, -1),
                    new Vector2Int(0, -1),
                },
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(0, 4),
                        RoomExitDirection.North),
                    new PrototypeRoomExitData(
                        new Vector2Int(0, -4),
                        RoomExitDirection.South),
                });
            EditorUtility.SetDirty(loop);

            PrototypeCombatRoomDefinitionAsset lanes = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeCombatLanesDefinitionPath,
                "PrototypeCombatLanes");
            lanes.Configure(
                "prototype-combat-lanes",
                RoomType.Combat,
                11,
                9,
                1f,
                new Vector2Int(0, -2),
                new Vector2Int(0, 2),
                new[]
                {
                    new Vector2Int(-2, -1),
                    new Vector2Int(-2, 0),
                    new Vector2Int(-2, 1),
                    new Vector2Int(2, -1),
                    new Vector2Int(2, 0),
                    new Vector2Int(2, 1),
                },
                new[]
                {
                    new Vector2Int(0, -2),
                    new Vector2Int(-1, -2),
                    new Vector2Int(1, -2),
                },
                new[]
                {
                    new Vector2Int(-3, -2),
                    new Vector2Int(3, -2),
                },
                CreateRectangleLoop(-3, 3, -2, 2),
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(0, 4),
                        RoomExitDirection.North),
                    new PrototypeRoomExitData(
                        new Vector2Int(0, -4),
                        RoomExitDirection.South),
                });
            EditorUtility.SetDirty(lanes);

            PrototypeCombatRoomDefinitionAsset pillars = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeCombatPillarsDefinitionPath,
                "PrototypeCombatPillars");
            pillars.Configure(
                "prototype-combat-pillars",
                RoomType.Combat,
                11,
                9,
                1f,
                new Vector2Int(-3, -2),
                new Vector2Int(3, 2),
                new[]
                {
                    new Vector2Int(-1, -2),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(-1, 2),
                },
                new[]
                {
                    new Vector2Int(-3, -2),
                    new Vector2Int(-3, -1),
                    new Vector2Int(-2, -2),
                },
                new[]
                {
                    new Vector2Int(-4, 1),
                    new Vector2Int(0, -3),
                },
                CreateRectangleLoop(-2, 2, -3, 3),
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(-5, 0),
                        RoomExitDirection.West),
                    new PrototypeRoomExitData(
                        new Vector2Int(5, 0),
                        RoomExitDirection.East),
                });
            EditorUtility.SetDirty(pillars);

            return new[] { loop, lanes, pillars };
        }

        private static PrototypeCombatRoomDefinitionAsset GetOrCreateRoomDefinition(
            string assetPath,
            string assetName)
        {
            PrototypeCombatRoomDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeCombatRoomDefinitionAsset>(assetPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<PrototypeCombatRoomDefinitionAsset>();
            definition.name = assetName;
            AssetDatabase.CreateAsset(definition, assetPath);
            return definition;
        }

        private static Vector2Int[] CreateRectangleLoop(
            int minX,
            int maxX,
            int minZ,
            int maxZ)
        {
            var cells = new List<Vector2Int>();
            for (int z = minZ; z <= maxZ; z++)
            {
                cells.Add(new Vector2Int(minX, z));
            }
            for (int x = minX + 1; x <= maxX; x++)
            {
                cells.Add(new Vector2Int(x, maxZ));
            }
            for (int z = maxZ - 1; z >= minZ; z--)
            {
                cells.Add(new Vector2Int(maxX, z));
            }
            for (int x = maxX - 1; x > minX; x--)
            {
                cells.Add(new Vector2Int(x, minZ));
            }
            return cells.ToArray();
        }

        private static bool EnsureTestSandbox(
            InputActionAsset inputActions,
            PrototypeBombDefinitionAsset bombDefinition,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeContentValidator.TestSandboxScenePath) == null)
            {
                CreateTestSandbox(
                    inputActions,
                    bombDefinition,
                    playerVitals,
                    chaserDefinition,
                    roomDefinition,
                    nextSceneName);
                return true;
            }

            Scene scene = SceneManager.GetSceneByPath(PrototypeContentValidator.TestSandboxScenePath);
            bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpgrade)
            {
                scene = EditorSceneManager.OpenScene(
                    PrototypeContentValidator.TestSandboxScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                UpgradeTestSandbox(
                    scene,
                    bombDefinition,
                    playerVitals,
                    chaserDefinition,
                    roomDefinition,
                    nextSceneName);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Unity failed to upgrade TestSandbox scene.");
                }
            }
            finally
            {
                if (openedForUpgrade && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return false;
        }

        private static bool EnsurePlaytestRoomVariant(
            string scenePath,
            PrototypeBombDefinitionAsset bombDefinition,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName)
        {
            bool created = false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(
                        PrototypeContentValidator.TestSandboxScenePath,
                        scenePath))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to copy the TestSandbox scene to '{scenePath}'.");
                }
                AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
                created = true;
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpgrade)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                UpgradeTestSandbox(
                    scene,
                    bombDefinition,
                    playerVitals,
                    chaserDefinition,
                    roomDefinition,
                    nextSceneName);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save playtest room scene '{scenePath}'.");
                }
            }
            finally
            {
                if (openedForUpgrade && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return created;
        }

        private static void CreateTestSandbox(
            InputActionAsset inputActions,
            PrototypeBombDefinitionAsset bombDefinition,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName)
        {
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
            CombatRoomDefinition room = roomDefinition.CreateCoreDefinition();
            float cellSize = roomDefinition.CellSize;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TestSandbox");

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            systems.SetActive(false);
            BombSwapInputReader inputReader = systems.AddComponent<BombSwapInputReader>();
            inputReader.Configure(inputActions);
            PrototypeGameSession gameSession = systems.AddComponent<PrototypeGameSession>();
            PrototypePlayerController playerController =
                systems.AddComponent<PrototypePlayerController>();
            PrototypeBombPresenter bombPresenter = systems.AddComponent<PrototypeBombPresenter>();
            PrototypePlayerHealthPresenter healthPresenter =
                systems.AddComponent<PrototypePlayerHealthPresenter>();
            PrototypeChaserPresenter chaserPresenter =
                systems.AddComponent<PrototypeChaserPresenter>();
            PrototypeInputHarnessProbe harnessProbe = systems.AddComponent<PrototypeInputHarnessProbe>();
            PrototypeRoomAdvanceController roomAdvanceController =
                systems.AddComponent<PrototypeRoomAdvanceController>();

            var gridRoot = new GameObject("GridRoot");
            gridRoot.transform.SetParent(root.transform, false);

            Transform environment = CreateChild("Environment", gridRoot.transform);
            CreatePrimitive(
                "Floor",
                PrimitiveType.Cube,
                environment,
                new Vector3(0f, -0.1f, 0f),
                new Vector3(room.Width * cellSize, 0.2f, room.Depth * cellSize),
                floorMaterial,
                true);

            Transform gridLines = CreateChild("GridLines", environment);
            for (int x = -(room.Width / 2); x <= (room.Width / 2) + 1; x++)
            {
                CreatePrimitive(
                    "GridLineX_" + x,
                    PrimitiveType.Cube,
                    gridLines,
                    new Vector3((x - 0.5f) * cellSize, 0.0125f, 0f),
                    new Vector3(0.025f, 0.025f, room.Depth * cellSize),
                    gridMaterial,
                    false);
            }
            for (int z = -(room.Depth / 2); z <= (room.Depth / 2) + 1; z++)
            {
                CreatePrimitive(
                    "GridLineZ_" + z,
                    PrimitiveType.Cube,
                    gridLines,
                    new Vector3(0f, 0.0125f, (z - 0.5f) * cellSize),
                    new Vector3(room.Width * cellSize, 0.025f, 0.025f),
                    gridMaterial,
                    false);
            }

            Transform boundary = CreateChild("BoundaryWalls", environment);
            float boundaryX = ((room.Width / 2) + 1) * cellSize;
            float boundaryZ = ((room.Depth / 2) + 1) * cellSize;
            CreatePrimitive("NorthWall", PrimitiveType.Cube, boundary, new Vector3(0f, 0.5f, boundaryZ), new Vector3((room.Width + 2) * cellSize, 1f, cellSize), wallMaterial, true);
            CreatePrimitive("SouthWall", PrimitiveType.Cube, boundary, new Vector3(0f, 0.5f, -boundaryZ), new Vector3((room.Width + 2) * cellSize, 1f, cellSize), wallMaterial, true);
            CreatePrimitive("EastWall", PrimitiveType.Cube, boundary, new Vector3(boundaryX, 0.5f, 0f), new Vector3(cellSize, 1f, room.Depth * cellSize), wallMaterial, true);
            CreatePrimitive("WestWall", PrimitiveType.Cube, boundary, new Vector3(-boundaryX, 0.5f, 0f), new Vector3(cellSize, 1f, room.Depth * cellSize), wallMaterial, true);

            Transform obstacles = CreateChild("InteriorObstacles", environment);
            for (int index = 0; index < room.IndestructibleWalls.Count; index++)
            {
                GridPosition wall = room.IndestructibleWalls[index];
                CreatePrimitive(
                    $"Obstacle_{index}_{wall.X}_{wall.Z}",
                    PrimitiveType.Cube,
                    obstacles,
                    new Vector3(wall.X * cellSize, 0.5f, wall.Z * cellSize),
                    new Vector3(0.9f * cellSize, 1f, 0.9f * cellSize),
                    wallMaterial,
                    true);
            }

            Transform playerSpawn = CreateChild("PlayerSpawn", gridRoot.transform);
            playerSpawn.localPosition = new Vector3(
                room.PlayerSpawn.X * cellSize,
                0f,
                room.PlayerSpawn.Z * cellSize);
            Transform chaserSpawn = CreateChild("ChaserSpawn", gridRoot.transform);
            chaserSpawn.localPosition = new Vector3(
                room.ChaserSpawn.X * cellSize,
                0f,
                room.ChaserSpawn.Z * cellSize);
            GameObject player = CreatePrimitive(
                "PlayerPlaceholder",
                PrimitiveType.Capsule,
                gridRoot.transform,
                new Vector3(
                    room.PlayerSpawn.X * cellSize,
                    0.5f,
                    room.PlayerSpawn.Z * cellSize),
                new Vector3(0.35f, 0.5f, 0.35f),
                playerMaterial,
                true);
            player.tag = "Player";
            Transform runtimePresentation = CreateChild("RuntimePresentation", gridRoot.transform);

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
                chaserSpawn,
                roomDefinition);
            gameSession.Configure(
                context,
                inputReader,
                bombDefinition,
                playerVitals,
                chaserDefinition);
            playerController.Configure(gameSession, player.transform);
            bombPresenter.Configure(gameSession, runtimePresentation);
            healthPresenter.Configure(gameSession, player.GetComponentInChildren<Renderer>());
            chaserPresenter.Configure(gameSession, runtimePresentation);
            harnessProbe.Configure(inputReader, gameSession);
            roomAdvanceController.Configure(gameSession, nextSceneName);
            systems.SetActive(true);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, PrototypeContentValidator.TestSandboxScenePath))
            {
                throw new InvalidOperationException("Unity failed to save TestSandbox scene.");
            }

        }

        private static void UpgradeTestSandbox(
            Scene scene,
            PrototypeBombDefinitionAsset bombDefinition,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName)
        {
            TestSandboxContext context = FindExactlyOne<TestSandboxContext>(scene);
            BombSwapInputReader inputReader = FindExactlyOne<BombSwapInputReader>(scene);
            PrototypePlayerController playerController = FindExactlyOne<PrototypePlayerController>(scene);
            PrototypeInputHarnessProbe harnessProbe = FindExactlyOne<PrototypeInputHarnessProbe>(scene);

            GameObject systems = inputReader.gameObject;
            PrototypeGameSession gameSession = systems.GetComponent<PrototypeGameSession>();
            if (gameSession == null)
            {
                gameSession = systems.AddComponent<PrototypeGameSession>();
            }
            PrototypeBombPresenter bombPresenter = systems.GetComponent<PrototypeBombPresenter>();
            if (bombPresenter == null)
            {
                bombPresenter = systems.AddComponent<PrototypeBombPresenter>();
            }
            PrototypePlayerHealthPresenter healthPresenter =
                systems.GetComponent<PrototypePlayerHealthPresenter>();
            if (healthPresenter == null)
            {
                healthPresenter = systems.AddComponent<PrototypePlayerHealthPresenter>();
            }
            PrototypeChaserPresenter chaserPresenter =
                systems.GetComponent<PrototypeChaserPresenter>();
            if (chaserPresenter == null)
            {
                chaserPresenter = systems.AddComponent<PrototypeChaserPresenter>();
            }
            PrototypeRoomAdvanceController roomAdvanceController =
                systems.GetComponent<PrototypeRoomAdvanceController>();
            if (roomAdvanceController == null)
            {
                roomAdvanceController = systems.AddComponent<PrototypeRoomAdvanceController>();
            }

            Transform runtimePresentation = context.GridRoot.Find("RuntimePresentation");
            if (runtimePresentation == null)
            {
                runtimePresentation = CreateChild("RuntimePresentation", context.GridRoot);
            }
            Transform chaserSpawn = context.GridRoot.Find("ChaserSpawn");
            if (chaserSpawn == null)
            {
                chaserSpawn = CreateChild("ChaserSpawn", context.GridRoot);
            }
            CombatRoomDefinition room = roomDefinition.CreateCoreDefinition();
            SynchronizeInteriorObstacles(context.GridRoot, roomDefinition, room);
            var gridSpace = new GridSpace(context.GridRoot.position, roomDefinition.CellSize);
            context.PlayerSpawn.position = gridSpace.GridToWorld(room.PlayerSpawn);
            Vector3 playerPosition = gridSpace.GridToWorld(room.PlayerSpawn);
            playerPosition.y = context.PlayerPlaceholder.position.y;
            context.PlayerPlaceholder.position = playerPosition;
            chaserSpawn.position = gridSpace.GridToWorld(room.ChaserSpawn);

            Renderer playerRenderer =
                context.PlayerPlaceholder.GetComponentInChildren<Renderer>();
            if (playerRenderer == null)
            {
                throw new InvalidOperationException(
                    "TestSandbox player placeholder requires a renderer.");
            }

            context.Configure(
                inputReader,
                context.GridRoot,
                context.PlayerSpawn,
                context.PlayerPlaceholder,
                chaserSpawn,
                roomDefinition);
            gameSession.Configure(
                context,
                inputReader,
                bombDefinition,
                playerVitals,
                chaserDefinition);
            playerController.Configure(gameSession, context.PlayerPlaceholder);
            bombPresenter.Configure(gameSession, runtimePresentation);
            healthPresenter.Configure(gameSession, playerRenderer);
            chaserPresenter.Configure(gameSession, runtimePresentation);
            harnessProbe.Configure(inputReader, gameSession);
            roomAdvanceController.Configure(gameSession, nextSceneName);
            EditorUtility.SetDirty(context);
            EditorUtility.SetDirty(gameSession);
            EditorUtility.SetDirty(playerController);
            EditorUtility.SetDirty(bombPresenter);
            EditorUtility.SetDirty(healthPresenter);
            EditorUtility.SetDirty(chaserPresenter);
            EditorUtility.SetDirty(harnessProbe);
            EditorUtility.SetDirty(roomAdvanceController);
        }

        private static void SynchronizeInteriorObstacles(
            Transform gridRoot,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            CombatRoomDefinition room)
        {
            Transform environment = gridRoot.Find("Environment");
            if (environment == null)
            {
                throw new InvalidOperationException("TestSandbox is missing its Environment root.");
            }
            Transform obstacles = environment.Find("InteriorObstacles");
            if (obstacles == null)
            {
                obstacles = CreateChild("InteriorObstacles", environment);
            }

            var expected = new HashSet<GridPosition>(room.IndestructibleWalls);
            var actual = new HashSet<GridPosition>();
            var gridSpace = new GridSpace(gridRoot.position, roomDefinition.CellSize);
            bool matches = obstacles.childCount == expected.Count;
            for (int index = 0; index < obstacles.childCount; index++)
            {
                GridPosition cell = gridSpace.WorldToGrid(obstacles.GetChild(index).position);
                if (!actual.Add(cell) || !expected.Contains(cell))
                {
                    matches = false;
                }
            }
            if (matches && actual.SetEquals(expected))
            {
                return;
            }

            for (int index = obstacles.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(obstacles.GetChild(index).gameObject);
            }

            Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialsPath + "/Wall.mat");
            if (wallMaterial == null)
            {
                throw new InvalidOperationException("Prototype wall material is missing.");
            }

            float cellSize = roomDefinition.CellSize;
            for (int index = 0; index < room.IndestructibleWalls.Count; index++)
            {
                GridPosition wall = room.IndestructibleWalls[index];
                CreatePrimitive(
                    $"Obstacle_{index}_{wall.X}_{wall.Z}",
                    PrimitiveType.Cube,
                    obstacles,
                    new Vector3(wall.X * cellSize, 0.5f, wall.Z * cellSize),
                    new Vector3(0.9f * cellSize, 1f, 0.9f * cellSize),
                    wallMaterial,
                    true);
            }
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(PrototypeContentValidator.TestSandboxScenePath, true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxLanesScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxPillarsScenePath,
                    true),
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (scenes.Any(scene => string.Equals(
                        scene.path,
                        existing.path,
                        StringComparison.Ordinal)))
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

        private static GameObject CreateVisualPrefabIfMissing(
            string assetPath,
            string prefabName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(prefabName);
            try
            {
                CreatePrimitive(
                    "Visual",
                    primitiveType,
                    root.transform,
                    localPosition,
                    localScale,
                    material,
                    false);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Unity failed to save prototype prefab: {assetPath}");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string[] segments = assetPath.Split('/');
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

        private static T FindExactlyOne<T>(Scene scene) where T : Component
        {
            T found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] components = root.GetComponentsInChildren<T>(true);
                for (int index = 0; index < components.Length; index++)
                {
                    if (found != null)
                    {
                        throw new InvalidOperationException(
                            $"TestSandbox contains more than one {typeof(T).Name}.");
                    }

                    found = components[index];
                }
            }

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"TestSandbox is missing required {typeof(T).Name}.");
            }

            return found;
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
