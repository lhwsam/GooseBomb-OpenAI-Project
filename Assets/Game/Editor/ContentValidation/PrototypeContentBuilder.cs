using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BombSwap.Core;
using BombSwap.Editor.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeContentBuilder
    {
        // Serialized prototype content is synchronized through these Editor-only entry points.
        private const string MaterialsPath = "Assets/Game/Content/Materials/Prototype";
        private const string PrototypePrefabsPath = "Assets/Game/Content/Prefabs/Prototype";
        private static readonly string[] LegacyQuickBombAssetPaths =
        {
            "Assets/Game/Content/Bombs/PrototypeQuickCrossBomb.asset",
            MaterialsPath + "/QuickBomb.mat",
            MaterialsPath + "/QuickExplosion.mat",
            PrototypePrefabsPath + "/QuickBombPlaceholder.prefab",
            PrototypePrefabsPath + "/QuickExplosionCellPlaceholder.prefab"
        };

        private sealed class DungeonBoundaryPresentation
        {
            public DungeonBoundaryPresentation(
                Renderer[] doors,
                Animator[] doorAnimators,
                GameObject[] secretCrackRoots)
            {
                Doors = doors;
                DoorAnimators = doorAnimators;
                SecretCrackRoots = secretCrackRoots;
            }

            public Renderer[] Doors { get; }

            public Animator[] DoorAnimators { get; }

            public GameObject[] SecretCrackRoots { get; }
        }

        private sealed class DoorVisualPresentation
        {
            public DoorVisualPresentation(Renderer renderer, Animator animator)
            {
                Renderer = renderer;
                Animator = animator;
            }

            public Renderer Renderer { get; }

            public Animator Animator { get; }
        }

        private sealed class LobbyUiBindings
        {
            public Canvas Canvas { get; set; }

            public EventSystem EventSystem { get; set; }

            public GameObject ControlsPanel { get; set; }

            public TextMeshProUGUI TitleLabel { get; set; }

            public TextMeshProUGUI VersionLabel { get; set; }

            public Button StartButton { get; set; }

            public Button ControlsButton { get; set; }

            public Button BackButton { get; set; }

            public PrototypeSettingsPanelPresenter SettingsPanel { get; set; }
        }

        [MenuItem("Bomb Swap/Prototype/Create Missing Prototype Content")]
        public static void CreateMissingPrototypeContentMenu()
        {
            string summary = CreateMissingPrototypeContent();
            Debug.Log(summary);
        }

        [MenuItem("Bomb Swap/Prototype/Refresh Self-Destruct Content")]
        public static void RefreshSelfDestructContentMenu()
        {
            CreatePrototypeSelfDestructContentIfMissing();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Refreshed prototype self-destruct definition, blast, and presentation content.");
        }

        [MenuItem("Bomb Swap/Prototype/Refresh Thrower Content")]
        public static void RefreshThrowerContentMenu()
        {
            CreatePrototypeThrowerContentIfMissing();
            CreatePrototypeThrowerRoomContentIfMissing();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Refreshed prototype thrower definition, bomb, room, and presentation content.");
        }

        [MenuItem("Bomb Swap/Prototype/Apply Game UI Font")]
        public static void ApplyGameUiFontMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before applying the game UI font.");
            }

            TMP_FontAsset font = ConfigureGameDefaultFont();
            int changedLabelCount = ApplyGameFontToLobbyScene(font);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Applied {PrototypeUiFactory.GameFontAssetName} as the TMP default and updated {changedLabelCount} lobby labels.");
        }

        [MenuItem("Bomb Swap/Prototype/Apply Game UI Reference Resolution")]
        public static void ApplyGameUiReferenceResolutionMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before applying the game UI reference resolution.");
            }

            bool changed = ApplyGameUiReferenceResolutionToLobbyScene();
            Debug.Log(
                changed
                    ? $"Applied the {PrototypeUiFactory.ReferenceWidth:0}x{PrototypeUiFactory.ReferenceHeight:0} UI reference resolution to the lobby."
                    : "The lobby already uses the shared UI reference resolution.");
        }

        public static string CreateMissingPrototypeContent()
        {
            TMP_FontAsset gameFont = ConfigureGameDefaultFont();
            InputActionAsset inputActions = CreateInputActionsIfMissing();
            AudioMixer audioMixer = LoadRequiredAsset<AudioMixer>(
                PrototypeContentValidator.AudioMixerPath);
            bool lobbySceneCreated = EnsureLobbyScene(inputActions, audioMixer);
            ApplyGameFontToLobbyScene(gameFont);
            ApplyGameUiReferenceResolutionToLobbyScene();
            PrototypeBombDefinitionAsset bombDefinition =
                CreatePrototypeBombContentIfMissing();
            PrototypeBombDefinitionAsset areaBombDefinition =
                CreatePrototypeAreaBombContentIfMissing();
            PrototypeBombDefinitionAsset lineBombDefinition =
                CreatePrototypeLineBombContentIfMissing();
            PrototypeBombLoadoutAsset bombLoadout =
                CreatePrototypeBombLoadoutIfMissing(bombDefinition, areaBombDefinition);
            PrototypeBombRewardCatalogAsset bombRewardCatalog =
                CreatePrototypeBombRewardCatalog(
                    bombDefinition,
                    areaBombDefinition,
                    lineBombDefinition);
            DeleteLegacyQuickBombAssets();
            PrototypePlayerVitalsAsset playerVitals = CreatePrototypePlayerVitalsIfMissing();
            PrototypeChaserDefinitionAsset chaserDefinition =
                CreatePrototypeChaserContentIfMissing();
            PrototypeChargerDefinitionAsset chargerDefinition =
                CreatePrototypeChargerContentIfMissing();
            PrototypeArmoredDefinitionAsset armoredDefinition =
                CreatePrototypeArmoredContentIfMissing();
            CreatePrototypeSelfDestructContentIfMissing();
            CreatePrototypeThrowerContentIfMissing();
            PrototypeBombDefinitionAsset[] bossBombDefinitions =
                CreatePrototypeBossBombContentIfMissing();
            PrototypeBossDefinitionAsset bossDefinition =
                CreatePrototypeBossContentIfMissing(
                    bossBombDefinitions[0],
                    bossBombDefinitions[1]);
            CreatePrototypeRecoveryMaterialIfMissing();
            CreatePrototypeSecretMaterialsIfMissing();
            PrototypeCombatRoomDefinitionAsset[] roomDefinitions =
                CreatePrototypeCombatRoomContentIfMissing();
            PrototypeCombatRoomDefinitionAsset bossRoomDefinition =
                CreatePrototypeBossRoomContentIfMissing();
            PrototypeCombatRoomDefinitionAsset throwerRoomDefinition =
                CreatePrototypeThrowerRoomContentIfMissing();
            PrototypeDungeonCombatRoomCatalogAsset combatRoomCatalog =
                CreatePrototypeDungeonCombatRoomCatalog(
                    roomDefinitions,
                    throwerRoomDefinition);
            PrototypeDungeonSpecialRoomCatalogAsset specialRoomCatalog =
                CreatePrototypeDungeonSpecialRoomCatalog();
            bool sceneCreated = EnsureTestSandbox(
                inputActions,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[0],
                "TestSandboxThrower");
            bool lanesSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxLanesScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[1],
                string.Empty);
            StripDungeonAdaptersFromStandalonePlaytest(
                PrototypeContentValidator.TestSandboxLanesScenePath);
            bool throwerDungeonSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxThrowerScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                throwerRoomDefinition,
                "TestSandboxPillars");
            bool pillarsSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxPillarsScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[2],
                "TestSandboxArmor");
            bool armorSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxArmorScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[3],
                "TestSandboxGates");
            bool gatesSceneCreated = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.TestSandboxGatesScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[4],
                string.Empty);
            bool armoredPlaytestSceneCreated = EnsureArmoredPanicPlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[3]);
            bool selfDestructPlaytestSceneCreated = EnsureSelfDestructGatesPlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[4]);
            bool bossPlaytestSceneCreated = EnsureBossBattlePlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                bossRoomDefinition);
            bool throwerPlaytestSceneCreated = EnsureThrowerLanesPlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                throwerRoomDefinition);
            EnsureDungeonRoomBinding(
                PrototypeContentValidator.TestSandboxScenePath,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals,
                true);
            EnsureDungeonRoomBinding(
                PrototypeContentValidator.TestSandboxThrowerScenePath,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals,
                true);
            EnsureDungeonRoomBinding(
                PrototypeContentValidator.TestSandboxPillarsScenePath,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals,
                true);
            EnsureDungeonRoomBinding(
                PrototypeContentValidator.TestSandboxArmorScenePath,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals,
                true);
            EnsureDungeonRoomBinding(
                PrototypeContentValidator.TestSandboxGatesScenePath,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals,
                true);
            bool startSceneCreated = EnsureDungeonSpecialRoom(
                PrototypeContentValidator.DungeonStartScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[0],
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                false,
                false);
            bool rewardSceneCreated = EnsureDungeonSpecialRoom(
                PrototypeContentValidator.DungeonRewardScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[0],
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                false,
                false);
            bool bossAnteSceneCreated = EnsureDungeonSpecialRoom(
                PrototypeContentValidator.DungeonBossAnteScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[0],
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                false,
                false);
            bool recoverySceneCreated = EnsureDungeonSpecialRoom(
                PrototypeContentValidator.DungeonRecoveryScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[0],
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                false,
                false);
            bool secretSceneCreated = EnsureDungeonSpecialRoom(
                PrototypeContentValidator.DungeonSecretScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinitions[0],
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                false,
                false);
            bool bossSceneCreated = EnsureDungeonSpecialRoom(
                PrototypeContentValidator.DungeonBossScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                bossRoomDefinition,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                true,
                true);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();

            return lobbySceneCreated || sceneCreated || lanesSceneCreated ||
                throwerDungeonSceneCreated ||
                pillarsSceneCreated ||
                armorSceneCreated || gatesSceneCreated || armoredPlaytestSceneCreated ||
                selfDestructPlaytestSceneCreated || bossPlaytestSceneCreated ||
                throwerPlaytestSceneCreated ||
                startSceneCreated ||
                rewardSceneCreated ||
                bossAnteSceneCreated || recoverySceneCreated || secretSceneCreated ||
                bossSceneCreated
                ? "Created BombSwap lobby, prototype dungeon content, eleven graph room scenes, and standalone enemy playtest scenes."
                : $"BombSwap prototype content exists; synchronized lobby, dungeon rooms, standalone enemy playtests, graph bindings, references, {PrototypeUiFactory.GameFontAssetName}, and Build Settings.";
        }

        private static TMP_FontAsset ConfigureGameDefaultFont()
        {
            TMP_FontAsset font = LoadRequiredAsset<TMP_FontAsset>(
                PrototypeContentValidator.GameFontAssetPath);
            TMP_Settings settings = LoadRequiredAsset<TMP_Settings>(
                PrototypeContentValidator.TmpSettingsAssetPath);
            if (!font.HasCharacters(
                    PrototypeLobbyPresenter.GameTitle,
                    out List<char> missingCharacters))
            {
                throw new InvalidOperationException(
                    $"{PrototypeUiFactory.GameFontAssetName} is missing required title characters: {string.Join(", ", missingCharacters)}");
            }

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty defaultFontProperty =
                serializedSettings.FindProperty("m_defaultFontAsset");
            if (defaultFontProperty == null)
            {
                throw new InvalidOperationException(
                    "TMP Settings is missing m_defaultFontAsset.");
            }
            if (defaultFontProperty.objectReferenceValue == font)
            {
                return font;
            }

            Undo.RecordObject(settings, "Set Bomb Swap TMP Default Font");
            defaultFontProperty.objectReferenceValue = font;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return font;
        }

        private static int ApplyGameFontToLobbyScene(TMP_FontAsset font)
        {
            string scenePath = PrototypeContentValidator.LobbyScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Lobby scene '{scenePath}' does not exist.");
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForMigration = !scene.IsValid() || !scene.isLoaded;
            if (openedForMigration)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                TextMeshProUGUI[] labels = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    .ToArray();
                TextMeshProUGUI[] changedLabels = labels
                    .Where(label => label.font != font)
                    .ToArray();
                if (changedLabels.Length == 0)
                {
                    return 0;
                }

                Undo.RecordObjects(changedLabels, "Apply Bomb Swap UI Font");
                for (int index = 0; index < changedLabels.Length; index++)
                {
                    changedLabels[index].font = font;
                    EditorUtility.SetDirty(changedLabels[index]);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save lobby scene '{scenePath}' after applying the game UI font.");
                }
                return changedLabels.Length;
            }
            finally
            {
                if (openedForMigration && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool ApplyGameUiReferenceResolutionToLobbyScene()
        {
            string scenePath = PrototypeContentValidator.LobbyScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Lobby scene '{scenePath}' does not exist.");
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForMigration = !scene.IsValid() || !scene.isLoaded;
            if (openedForMigration)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                CanvasScaler[] scalers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CanvasScaler>(true))
                    .ToArray();
                if (scalers.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Lobby scene must contain exactly one CanvasScaler, found {scalers.Length}.");
                }

                CanvasScaler scaler = scalers[0];
                if (PrototypeUiFactory.HasReferenceCanvasScale(scaler))
                {
                    return false;
                }

                Undo.RecordObject(scaler, "Apply Bomb Swap UI Reference Resolution");
                PrototypeUiFactory.ConfigureCanvasScaler(scaler);
                EditorUtility.SetDirty(scaler);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save lobby scene '{scenePath}' after applying the UI reference resolution.");
                }
                return true;
            }
            finally
            {
                if (openedForMigration && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool EnsureLobbyScene(
            InputActionAsset inputActions,
            AudioMixer audioMixer)
        {
            EnsureAssetFolder("Assets/Game/Scenes/Lobby");
            EnsureAssetFolder(MaterialsPath);

            string scenePath = PrototypeContentValidator.LobbyScenePath;
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool created = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null;
            bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpgrade)
            {
                scene = created
                    ? EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Additive)
                    : EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Additive);
            }

            try
            {
                PrototypeLobbyPresenter lobby = FindLobbyPresenter(scene);
                bool changed = false;
                if (created || lobby == null || !lobby.HasBaseAuthoredViewReferences)
                {
                    SynchronizeLobbyScene(scene, inputActions, audioMixer);
                    changed = true;
                }
                else if (!lobby.HasAuthoredViewReferences)
                {
                    UpgradeLobbySettings(lobby, inputActions, audioMixer);
                    changed = true;
                }

                if (lobby != null && !lobby.HasVersionLabelReference)
                {
                    TextMeshProUGUI[] versionLabels = lobby.LobbyCanvas
                        .GetComponentsInChildren<TextMeshProUGUI>(true)
                        .Where(label => string.Equals(
                            label.name,
                            "VersionText",
                            StringComparison.Ordinal))
                        .ToArray();
                    if (versionLabels.Length == 1)
                    {
                        lobby.BindVersionLabel(versionLabels[0]);
                        EditorUtility.SetDirty(lobby);
                        changed = true;
                    }
                }

                if (LobbyButtonFeedbackAuthoring.ApplyToScene(scene) > 0)
                {
                    changed = true;
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool saved = created
                        ? EditorSceneManager.SaveScene(scene, scenePath)
                        : EditorSceneManager.SaveScene(scene);
                    if (!saved)
                    {
                        throw new InvalidOperationException(
                            $"Unity failed to save lobby scene '{scenePath}'.");
                    }
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

        private static PrototypeLobbyPresenter FindLobbyPresenter(Scene scene)
        {
            PrototypeLobbyPresenter[] presenters = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<PrototypeLobbyPresenter>(true))
                .ToArray();
            return presenters.Length == 1 ? presenters[0] : null;
        }

        private static void UpgradeLobbySettings(
            PrototypeLobbyPresenter presenter,
            InputActionAsset inputActions,
            AudioMixer audioMixer)
        {
            GameObject systems = presenter.gameObject;
            PrototypeUserSettingsRuntime settingsRuntime =
                systems.GetComponent<PrototypeUserSettingsRuntime>();
            if (settingsRuntime == null)
            {
                settingsRuntime = systems.AddComponent<PrototypeUserSettingsRuntime>();
            }
            settingsRuntime.Configure(inputActions, audioMixer);

            Transform panelParent = presenter.ControlsPanel.transform.parent;
            UnityEngine.Object.DestroyImmediate(presenter.ControlsPanel);
            PrototypeSettingsPanelPresenter settingsPanel =
                PrototypeSettingsPanelFactory.Create(panelParent);

            TextMeshProUGUI controlsButtonLabel =
                presenter.ControlsButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (controlsButtonLabel != null)
            {
                controlsButtonLabel.text = "설정";
            }
            presenter.BindAuthoredView(
                presenter.LobbyCanvas,
                presenter.LobbyEventSystem,
                settingsPanel.gameObject,
                presenter.TitleLabel,
                presenter.StatusLabel,
                presenter.StartButton,
                presenter.ControlsButton,
                settingsPanel.BackButton,
                settingsRuntime,
                settingsPanel);
            EditorUtility.SetDirty(presenter);
        }

        private static void SynchronizeLobbyScene(
            Scene scene,
            InputActionAsset inputActions,
            AudioMixer audioMixer)
        {
            foreach (GameObject existingRoot in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required URP Lit shader was not found for the lobby.");
            }

            Material floorMaterial = GetOrCreateMaterial(
                MaterialsPath + "/LobbyFloor.mat",
                shader,
                new Color(0.055f, 0.085f, 0.13f, 1f));
            Material gooseMaterial = GetOrCreateMaterial(
                MaterialsPath + "/LobbyGoose.mat",
                shader,
                new Color(0.92f, 0.92f, 0.84f, 1f));
            Material orangeMaterial = GetOrCreateMaterial(
                MaterialsPath + "/LobbyOrange.mat",
                shader,
                new Color(1f, 0.48f, 0.08f, 1f));
            Material darkMaterial = GetOrCreateMaterial(
                MaterialsPath + "/LobbyDark.mat",
                shader,
                new Color(0.035f, 0.045f, 0.06f, 1f));

            var root = new GameObject("DungeonLobby");
            SceneManager.MoveGameObjectToScene(root, scene);

            var systems = new GameObject("LobbySystems");
            systems.transform.SetParent(root.transform, false);
            PrototypeLobbyPresenter presenter =
                systems.AddComponent<PrototypeLobbyPresenter>();
            presenter.Configure(PrototypeLobbyPresenter.DefaultStartSceneName);
            PrototypeUserSettingsRuntime settingsRuntime =
                systems.AddComponent<PrototypeUserSettingsRuntime>();
            settingsRuntime.Configure(inputActions, audioMixer);

            Transform stage = CreateChild("LobbyStage", root.transform);
            CreatePrimitive(
                "Floor",
                PrimitiveType.Cube,
                stage,
                new Vector3(3.2f, -0.2f, 0.4f),
                new Vector3(15f, 0.35f, 10f),
                floorMaterial,
                false);

            Transform goose = CreateChild("BombLayingGoose", stage);
            goose.localPosition = new Vector3(3.5f, 0.2f, 0.7f);
            CreatePrimitive(
                "Body",
                PrimitiveType.Sphere,
                goose,
                new Vector3(0f, 0.78f, 0.25f),
                new Vector3(2.2f, 1.35f, 2.55f),
                gooseMaterial,
                false);
            CreatePrimitive(
                "LeftWing",
                PrimitiveType.Sphere,
                goose,
                new Vector3(-1f, 0.9f, 0.25f),
                new Vector3(0.5f, 0.86f, 1.55f),
                gooseMaterial,
                false);
            CreatePrimitive(
                "RightWing",
                PrimitiveType.Sphere,
                goose,
                new Vector3(1f, 0.9f, 0.25f),
                new Vector3(0.5f, 0.86f, 1.55f),
                gooseMaterial,
                false);
            CreatePrimitive(
                "Neck",
                PrimitiveType.Capsule,
                goose,
                new Vector3(0f, 1.7f, -0.45f),
                new Vector3(0.65f, 1.05f, 0.65f),
                gooseMaterial,
                false);
            CreatePrimitive(
                "Head",
                PrimitiveType.Sphere,
                goose,
                new Vector3(0f, 2.75f, -0.55f),
                new Vector3(1.08f, 0.95f, 1.05f),
                gooseMaterial,
                false);
            CreatePrimitive(
                "Beak",
                PrimitiveType.Cube,
                goose,
                new Vector3(0f, 2.62f, -1.18f),
                new Vector3(0.92f, 0.25f, 0.8f),
                orangeMaterial,
                false);
            CreatePrimitive(
                "LeftEye",
                PrimitiveType.Sphere,
                goose,
                new Vector3(-0.29f, 2.92f, -1.02f),
                new Vector3(0.16f, 0.16f, 0.14f),
                darkMaterial,
                false);
            CreatePrimitive(
                "RightEye",
                PrimitiveType.Sphere,
                goose,
                new Vector3(0.29f, 2.92f, -1.02f),
                new Vector3(0.16f, 0.16f, 0.14f),
                darkMaterial,
                false);

            Transform bomb = CreateChild("LaidBomb", stage);
            bomb.localPosition = new Vector3(6.1f, 0.22f, -0.25f);
            CreatePrimitive(
                "BombBody",
                PrimitiveType.Sphere,
                bomb,
                new Vector3(0f, 0.62f, 0f),
                new Vector3(1.35f, 1.35f, 1.35f),
                darkMaterial,
                false);
            GameObject fuse = CreatePrimitive(
                "Fuse",
                PrimitiveType.Cylinder,
                bomb,
                new Vector3(0.18f, 1.45f, 0f),
                new Vector3(0.12f, 0.34f, 0.12f),
                orangeMaterial,
                false);
            fuse.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);

            LobbyUiBindings ui = CreateLobbyUi(root.transform);
            presenter.BindAuthoredView(
                ui.Canvas,
                ui.EventSystem,
                ui.ControlsPanel,
                ui.TitleLabel,
                null,
                ui.StartButton,
                ui.ControlsButton,
                ui.BackButton,
                settingsRuntime,
                ui.SettingsPanel);
            presenter.BindVersionLabel(ui.VersionLabel);
            EditorUtility.SetDirty(presenter);

            CreateLobbyCamera(root.transform);
            CreateDirectionalLight(root.transform);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.43f, 1f);
        }

        public static string CreateOrUpdateArmoredPanicPlaytestScene()
        {
            PrototypeBombLoadoutAsset bombLoadout = LoadRequiredAsset<
                PrototypeBombLoadoutAsset>(PrototypeContentValidator.PrototypeBombLoadoutPath);
            PrototypePlayerVitalsAsset playerVitals = LoadRequiredAsset<
                PrototypePlayerVitalsAsset>(PrototypeContentValidator.PrototypePlayerVitalsPath);
            PrototypeChaserDefinitionAsset chaserDefinition = LoadRequiredAsset<
                PrototypeChaserDefinitionAsset>(PrototypeContentValidator.PrototypeChaserDefinitionPath);
            PrototypeChargerDefinitionAsset chargerDefinition = LoadRequiredAsset<
                PrototypeChargerDefinitionAsset>(PrototypeContentValidator.PrototypeChargerDefinitionPath);
            PrototypeArmoredDefinitionAsset armoredDefinition = LoadRequiredAsset<
                PrototypeArmoredDefinitionAsset>(PrototypeContentValidator.PrototypeArmoredDefinitionPath);
            PrototypeBossDefinitionAsset bossDefinition = LoadRequiredAsset<
                PrototypeBossDefinitionAsset>(PrototypeContentValidator.PrototypeBossDefinitionPath);
            PrototypeCombatRoomDefinitionAsset roomDefinition = LoadRequiredAsset<
                PrototypeCombatRoomDefinitionAsset>(PrototypeContentValidator.PrototypeCombatArmorDefinitionPath);

            bool created = EnsureArmoredPanicPlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition);
            AssetDatabase.SaveAssets();
            return created
                ? $"Created standalone Armor playtest scene at '{PrototypeContentValidator.ArmoredPanicPlaytestScenePath}'."
                : $"Synchronized standalone Armor playtest scene at '{PrototypeContentValidator.ArmoredPanicPlaytestScenePath}'.";
        }

        public static string CreateOrUpdateSelfDestructGatesPlaytestScene()
        {
            PrototypeBombLoadoutAsset bombLoadout = LoadRequiredAsset<
                PrototypeBombLoadoutAsset>(PrototypeContentValidator.PrototypeBombLoadoutPath);
            PrototypePlayerVitalsAsset playerVitals = LoadRequiredAsset<
                PrototypePlayerVitalsAsset>(PrototypeContentValidator.PrototypePlayerVitalsPath);
            PrototypeChaserDefinitionAsset chaserDefinition = LoadRequiredAsset<
                PrototypeChaserDefinitionAsset>(PrototypeContentValidator.PrototypeChaserDefinitionPath);
            PrototypeChargerDefinitionAsset chargerDefinition = LoadRequiredAsset<
                PrototypeChargerDefinitionAsset>(PrototypeContentValidator.PrototypeChargerDefinitionPath);
            PrototypeArmoredDefinitionAsset armoredDefinition = LoadRequiredAsset<
                PrototypeArmoredDefinitionAsset>(PrototypeContentValidator.PrototypeArmoredDefinitionPath);
            PrototypeBossDefinitionAsset bossDefinition = LoadRequiredAsset<
                PrototypeBossDefinitionAsset>(PrototypeContentValidator.PrototypeBossDefinitionPath);
            PrototypeCombatRoomDefinitionAsset roomDefinition = LoadRequiredAsset<
                PrototypeCombatRoomDefinitionAsset>(PrototypeContentValidator.PrototypeCombatGatesDefinitionPath);

            bool created = EnsureSelfDestructGatesPlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition);
            AssetDatabase.SaveAssets();
            return created
                ? $"Created standalone Self-Destruct Gates playtest scene at '{PrototypeContentValidator.SelfDestructGatesPlaytestScenePath}'."
                : $"Synchronized standalone Self-Destruct Gates playtest scene at '{PrototypeContentValidator.SelfDestructGatesPlaytestScenePath}'.";
        }

        public static string CreateOrUpdateBossBattlePlaytestScene()
        {
            PrototypeBombLoadoutAsset bombLoadout = LoadRequiredAsset<
                PrototypeBombLoadoutAsset>(PrototypeContentValidator.PrototypeBombLoadoutPath);
            PrototypePlayerVitalsAsset playerVitals = LoadRequiredAsset<
                PrototypePlayerVitalsAsset>(PrototypeContentValidator.PrototypePlayerVitalsPath);
            PrototypeChaserDefinitionAsset chaserDefinition = LoadRequiredAsset<
                PrototypeChaserDefinitionAsset>(PrototypeContentValidator.PrototypeChaserDefinitionPath);
            PrototypeChargerDefinitionAsset chargerDefinition = LoadRequiredAsset<
                PrototypeChargerDefinitionAsset>(PrototypeContentValidator.PrototypeChargerDefinitionPath);
            PrototypeArmoredDefinitionAsset armoredDefinition = LoadRequiredAsset<
                PrototypeArmoredDefinitionAsset>(PrototypeContentValidator.PrototypeArmoredDefinitionPath);
            PrototypeBossDefinitionAsset bossDefinition = LoadRequiredAsset<
                PrototypeBossDefinitionAsset>(PrototypeContentValidator.PrototypeBossDefinitionPath);
            PrototypeCombatRoomDefinitionAsset roomDefinition = LoadRequiredAsset<
                PrototypeCombatRoomDefinitionAsset>(PrototypeContentValidator.PrototypeBossArenaDefinitionPath);

            bool created = EnsureBossBattlePlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition);
            AssetDatabase.SaveAssets();
            return created
                ? $"Created standalone Boss Battle playtest scene at '{PrototypeContentValidator.BossBattlePlaytestScenePath}'."
                : $"Synchronized standalone Boss Battle playtest scene at '{PrototypeContentValidator.BossBattlePlaytestScenePath}'.";
        }

        public static string CreateOrUpdateThrowerLanesPlaytestScene()
        {
            CreatePrototypeThrowerContentIfMissing();
            PrototypeCombatRoomDefinitionAsset roomDefinition =
                CreatePrototypeThrowerRoomContentIfMissing();
            PrototypeBombLoadoutAsset bombLoadout = LoadRequiredAsset<
                PrototypeBombLoadoutAsset>(PrototypeContentValidator.PrototypeBombLoadoutPath);
            PrototypePlayerVitalsAsset playerVitals = LoadRequiredAsset<
                PrototypePlayerVitalsAsset>(PrototypeContentValidator.PrototypePlayerVitalsPath);
            PrototypeChaserDefinitionAsset chaserDefinition = LoadRequiredAsset<
                PrototypeChaserDefinitionAsset>(PrototypeContentValidator.PrototypeChaserDefinitionPath);
            PrototypeChargerDefinitionAsset chargerDefinition = LoadRequiredAsset<
                PrototypeChargerDefinitionAsset>(PrototypeContentValidator.PrototypeChargerDefinitionPath);
            PrototypeArmoredDefinitionAsset armoredDefinition = LoadRequiredAsset<
                PrototypeArmoredDefinitionAsset>(PrototypeContentValidator.PrototypeArmoredDefinitionPath);
            PrototypeBossDefinitionAsset bossDefinition = LoadRequiredAsset<
                PrototypeBossDefinitionAsset>(PrototypeContentValidator.PrototypeBossDefinitionPath);

            bool created = EnsureThrowerLanesPlaytestScene(
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition);
            AssetDatabase.SaveAssets();
            return created
                ? $"Created standalone Thrower Lanes playtest scene at '{PrototypeContentValidator.ThrowerLanesPlaytestScenePath}'."
                : $"Synchronized standalone Thrower Lanes playtest scene at '{PrototypeContentValidator.ThrowerLanesPlaytestScenePath}'.";
        }

        private static InputActionAsset CreateInputActionsIfMissing()
        {
            string absolutePath = Path.Combine(
                Application.dataPath,
                "Game/Content/Input/BombSwapInputActions.inputactions");
            InputActionAsset imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                PrototypeContentValidator.InputActionsPath);
            if (imported != null)
            {
                InputActionMap gameplay = imported.FindActionMap(
                    BombSwapInputActionNames.GameplayMap,
                    false);
                if (gameplay == null)
                {
                    throw new InvalidOperationException(
                        $"Input Actions asset is missing map '{BombSwapInputActionNames.GameplayMap}'.");
                }
                if (gameplay.FindAction(BombSwapInputActionNames.RestartRun, false) == null)
                {
                    AddButtonBindings(
                        gameplay,
                        BombSwapInputActionNames.RestartRun,
                        "<Keyboard>/r",
                        "<Gamepad>/select");
                    File.WriteAllText(
                        absolutePath,
                        imported.ToJson(),
                        new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(
                        PrototypeContentValidator.InputActionsPath,
                        ImportAssetOptions.ForceSynchronousImport);
                    imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                        PrototypeContentValidator.InputActionsPath);
                    if (imported == null)
                    {
                        throw new InvalidOperationException(
                            $"Unity could not reimport upgraded Input Actions: {PrototypeContentValidator.InputActionsPath}");
                    }
                }
                return imported;
            }

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
                AddButtonBindings(
                    gameplay,
                    BombSwapInputActionNames.RestartRun,
                    "<Keyboard>/r",
                    "<Gamepad>/select");

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

            GameObject bombPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.BombPrefabPath);
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
                EditorUtility.SetDirty(definition);
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
                0.25f,
                1.5f);
            AssetDatabase.CreateAsset(
                definition,
                PrototypeContentValidator.PrototypeBombDefinitionPath);
            return definition;
        }

        private static PrototypeBombDefinitionAsset CreatePrototypeAreaBombContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Bombs");
            Material explosionMaterial = GetOrCreateMaterial(
                MaterialsPath + "/AreaExplosion.mat",
                shader,
                new Color(1f, 0.08f, 0.55f, 1f));
            if (explosionMaterial.HasProperty("_EmissionColor"))
            {
                explosionMaterial.EnableKeyword("_EMISSION");
                explosionMaterial.SetColor("_EmissionColor", new Color(0.8f, 0f, 0.3f, 1f));
                EditorUtility.SetDirty(explosionMaterial);
            }

            GameObject bombPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.AreaBombPrefabPath);
            GameObject explosionPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.AreaExplosionCellPrefabPath,
                "AreaExplosionCellPlaceholder",
                PrimitiveType.Cube,
                new Vector3(0f, 0.08f, 0f),
                new Vector3(0.92f, 0.16f, 0.92f),
                explosionMaterial);

            PrototypeBombDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeContentValidator.PrototypeAreaBombDefinitionPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
            definition.name = "PrototypeAreaBomb";
            definition.Configure(
                "prototype-area",
                2f,
                1,
                bombPrefab,
                explosionPrefab,
                0.25f,
                2.5f,
                BombExplosionShape.SquareArea);
            AssetDatabase.CreateAsset(
                definition,
                PrototypeContentValidator.PrototypeAreaBombDefinitionPath);
            return definition;
        }

        private static PrototypeBombDefinitionAsset CreatePrototypeLineBombContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Bombs");
            Material explosionMaterial = GetOrCreateMaterial(
                MaterialsPath + "/LineExplosion.mat",
                shader,
                new Color(0.1f, 0.92f, 1f, 1f));
            if (explosionMaterial.HasProperty("_EmissionColor"))
            {
                explosionMaterial.EnableKeyword("_EMISSION");
                explosionMaterial.SetColor(
                    "_EmissionColor",
                    new Color(0f, 0.48f, 0.72f, 1f));
                EditorUtility.SetDirty(explosionMaterial);
            }

            GameObject bombPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.LineBombPrefabPath);
            GameObject explosionPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.LineExplosionCellPrefabPath,
                "LineExplosionCellPlaceholder",
                PrimitiveType.Cube,
                new Vector3(0f, 0.09f, 0f),
                new Vector3(0.86f, 0.18f, 0.86f),
                explosionMaterial);

            PrototypeBombDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeContentValidator.PrototypeLineBombDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    PrototypeBombDefinitionAsset>();
                definition.name = "PrototypeLineBomb";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeLineBombDefinitionPath);
            }
            definition.name = "PrototypeLineBomb";
            definition.Configure(
                "prototype-line",
                2f,
                3,
                bombPrefab,
                explosionPrefab,
                0.25f,
                2.25f,
                BombExplosionShape.ForwardLine);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void DeleteLegacyQuickBombAssets()
        {
            for (int index = 0; index < LegacyQuickBombAssetPaths.Length; index++)
            {
                string assetPath = LegacyQuickBombAssetPaths[index];
                if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null &&
                    !AssetDatabase.DeleteAsset(assetPath))
                {
                    throw new InvalidOperationException(
                        $"Could not remove legacy quick-cross prototype asset: {assetPath}");
                }
            }
        }

        private static PrototypeBombLoadoutAsset CreatePrototypeBombLoadoutIfMissing(
            PrototypeBombDefinitionAsset firstSlot,
            PrototypeBombDefinitionAsset secondSlot)
        {
            PrototypeBombLoadoutAsset loadout =
                AssetDatabase.LoadAssetAtPath<PrototypeBombLoadoutAsset>(
                    PrototypeContentValidator.PrototypeBombLoadoutPath);
            if (loadout == null)
            {
                loadout = ScriptableObject.CreateInstance<PrototypeBombLoadoutAsset>();
                loadout.name = "PrototypeBombLoadout";
                AssetDatabase.CreateAsset(
                    loadout,
                    PrototypeContentValidator.PrototypeBombLoadoutPath);
            }

            loadout.Configure(firstSlot, secondSlot, 2f);
            EditorUtility.SetDirty(loadout);
            return loadout;
        }

        private static PrototypeBombRewardCatalogAsset CreatePrototypeBombRewardCatalog(
            PrototypeBombDefinitionAsset firstSlot,
            PrototypeBombDefinitionAsset areaCandidate,
            PrototypeBombDefinitionAsset lineCandidate)
        {
            PrototypeBombRewardCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeBombRewardCatalogAsset>(
                    PrototypeContentValidator.PrototypeBombRewardCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    PrototypeBombRewardCatalogAsset>();
                catalog.name = "PrototypeBombRewardCatalog";
                AssetDatabase.CreateAsset(
                    catalog,
                    PrototypeContentValidator.PrototypeBombRewardCatalogPath);
            }

            catalog.Configure(
                firstSlot,
                new[] { areaCandidate, lineCandidate },
                2f);
            EditorUtility.SetDirty(catalog);
            return catalog;
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
            EnsureAssetFolder("Assets/Game/Content/Enemies");
            GameObject chaserPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.ChaserPrefabPath);

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
                0f,
                2.2f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeChargerDefinitionAsset CreatePrototypeChargerContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Enemies");
            Material chargerMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Charger.mat",
                shader,
                new Color(0.78f, 0.16f, 0.12f, 1f));
            GameObject chargerPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ChargerPrefabPath,
                "ChargerPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.7f, 0.7f, 0.7f),
                chargerMaterial);
            Material chargerTelegraphMaterial = GetOrCreateMaterial(
                MaterialsPath + "/ChargerTelegraph.mat",
                shader,
                new Color(1f, 0.72f, 0.05f, 1f));
            GameObject chargerTelegraphCellPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ChargerTelegraphCellPrefabPath,
                "ChargerTelegraphCellPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.82f, 0.04f, 0.82f),
                chargerTelegraphMaterial);

            PrototypeChargerDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeChargerDefinitionAsset>(
                    PrototypeContentValidator.PrototypeChargerDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PrototypeChargerDefinitionAsset>();
                definition.name = "PrototypeCharger";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeChargerDefinitionPath);
            }

            definition.Configure(
                "prototype-charger",
                1,
                1,
                1f,
                0.75f,
                8f,
                1f,
                chargerPrefab,
                chargerTelegraphCellPrefab,
                0f,
                2.2f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeArmoredDefinitionAsset CreatePrototypeArmoredContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Enemies");
            Material armoredMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Armored.mat",
                shader,
                new Color(0.28f, 0.38f, 0.52f, 1f));
            GameObject armoredPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ArmoredPrefabPath,
                "ArmoredPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.82f, 0.82f, 0.82f),
                armoredMaterial);
            Material panicTelegraphMaterial = GetOrCreateMaterial(
                MaterialsPath + "/ArmoredPanicTelegraph.mat",
                shader,
                new Color(1f, 0.22f, 0.05f, 1f));
            GameObject panicTelegraphCellPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ArmoredPanicTelegraphCellPrefabPath,
                "ArmoredPanicTelegraphCellPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.86f, 0.05f, 0.86f),
                panicTelegraphMaterial);

            PrototypeArmoredDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeArmoredDefinitionAsset>(
                    PrototypeContentValidator.PrototypeArmoredDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PrototypeArmoredDefinitionAsset>();
                definition.name = "PrototypeArmored";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeArmoredDefinitionPath);
            }

            definition.Configure(
                "prototype-armored",
                1,
                1f,
                3f,
                2,
                1,
                0.6f,
                6f,
                3,
                0.5f,
                armoredPrefab,
                panicTelegraphCellPrefab,
                0.5f,
                0.12f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeSelfDestructDefinitionAsset
            CreatePrototypeSelfDestructContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Enemies");
            EnsureAssetFolder("Assets/Game/Content/Bombs");
            Material enemyMaterial = GetOrCreateMaterial(
                MaterialsPath + "/SelfDestruct.mat",
                shader,
                new Color(0.92f, 0.18f, 0.08f, 1f));
            Material telegraphMaterial = GetOrCreateMaterial(
                MaterialsPath + "/SelfDestructTelegraph.mat",
                shader,
                new Color(1f, 0.38f, 0.04f, 1f));
            GameObject enemyPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.SelfDestructPrefabPath,
                "SelfDestructPlaceholder",
                PrimitiveType.Sphere,
                Vector3.zero,
                new Vector3(0.72f, 0.72f, 0.72f),
                enemyMaterial);
            GameObject telegraphPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.SelfDestructTelegraphCellPrefabPath,
                "SelfDestructTelegraphCellPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.86f, 0.05f, 0.86f),
                telegraphMaterial);
            GameObject bombPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.BombPrefabPath);
            GameObject explosionPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.ExplosionCellPrefabPath);

            PrototypeBombDefinitionAsset blastDefinition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeContentValidator.PrototypeSelfDestructBombDefinitionPath);
            if (blastDefinition == null)
            {
                blastDefinition = ScriptableObject.CreateInstance<
                    PrototypeBombDefinitionAsset>();
                blastDefinition.name = "PrototypeSelfDestructBlast";
                AssetDatabase.CreateAsset(
                    blastDefinition,
                    PrototypeContentValidator.PrototypeSelfDestructBombDefinitionPath);
            }
            blastDefinition.Configure(
                "prototype-self-destruct-blast",
                0.75f,
                2,
                bombPrefab,
                explosionPrefab,
                0.25f,
                1f,
                BombExplosionShape.Cross);
            EditorUtility.SetDirty(blastDefinition);

            PrototypeSelfDestructDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeSelfDestructDefinitionAsset>(
                    PrototypeContentValidator.PrototypeSelfDestructDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    PrototypeSelfDestructDefinitionAsset>();
                definition.name = "PrototypeSelfDestruct";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeSelfDestructDefinitionPath);
            }
            definition.Configure(
                "prototype-self-destruct",
                2f,
                5f,
                1.5f,
                3,
                1,
                blastDefinition,
                enemyPrefab,
                telegraphPrefab,
                0f,
                0.12f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeBombDefinitionAsset[] CreatePrototypeBossBombContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Bombs");
            Material throwBombMaterial = GetOrCreateMaterial(
                MaterialsPath + "/BossThrowBomb.mat",
                shader,
                new Color(0.95f, 0.22f, 0.04f, 1f));
            Material throwExplosionMaterial = GetOrCreateMaterial(
                MaterialsPath + "/BossThrowExplosion.mat",
                shader,
                new Color(1f, 0.46f, 0.03f, 1f));
            Material chainBombMaterial = GetOrCreateMaterial(
                MaterialsPath + "/BossChainBomb.mat",
                shader,
                new Color(0.78f, 0.06f, 0.68f, 1f));
            Material chainExplosionMaterial = GetOrCreateMaterial(
                MaterialsPath + "/BossChainExplosion.mat",
                shader,
                new Color(1f, 0.08f, 0.72f, 1f));

            GameObject throwBombPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BossThrowBombPrefabPath,
                "BossThrowBombPlaceholder",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.7f, 0.7f, 0.7f),
                throwBombMaterial);
            GameObject throwExplosionPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BossThrowExplosionCellPrefabPath,
                "BossThrowExplosionCellPlaceholder",
                PrimitiveType.Cube,
                new Vector3(0f, 0.09f, 0f),
                new Vector3(0.92f, 0.18f, 0.92f),
                throwExplosionMaterial);
            GameObject chainBombPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BossChainBombPrefabPath,
                "BossChainBombPlaceholder",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.2f, 0f),
                new Vector3(0.58f, 0.2f, 0.58f),
                chainBombMaterial);
            GameObject chainExplosionPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BossChainExplosionCellPrefabPath,
                "BossChainExplosionCellPlaceholder",
                PrimitiveType.Cube,
                new Vector3(0f, 0.1f, 0f),
                new Vector3(0.94f, 0.2f, 0.94f),
                chainExplosionMaterial);

            PrototypeBombDefinitionAsset throwDefinition =
                GetOrCreateBossBombDefinition(
                    PrototypeContentValidator.PrototypeBossThrowBombDefinitionPath,
                    "PrototypeBossThrowBomb",
                    "prototype-boss-throw",
                    2f,
                    throwBombPrefab,
                    throwExplosionPrefab);
            PrototypeBombDefinitionAsset chainDefinition =
                GetOrCreateBossBombDefinition(
                    PrototypeContentValidator.PrototypeBossChainBombDefinitionPath,
                    "PrototypeBossChainBomb",
                    "prototype-boss-chain",
                    2f,
                    chainBombPrefab,
                    chainExplosionPrefab);
            return new[] { throwDefinition, chainDefinition };
        }

        private static PrototypeThrowerDefinitionAsset CreatePrototypeThrowerContentIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Enemies");
            EnsureAssetFolder("Assets/Game/Content/Bombs");
            Material enemyMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Thrower.mat",
                shader,
                new Color(0.55f, 0.14f, 0.78f, 1f));
            Material telegraphMaterial = GetOrCreateMaterial(
                MaterialsPath + "/ThrowerTelegraph.mat",
                shader,
                new Color(1f, 0.12f, 0.85f, 1f));
            GameObject enemyPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ThrowerPrefabPath,
                "ThrowerPlaceholder",
                PrimitiveType.Cylinder,
                Vector3.zero,
                new Vector3(0.68f, 0.48f, 0.68f),
                enemyMaterial);
            GameObject telegraphPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.ThrowerTelegraphCellPrefabPath,
                "ThrowerTelegraphCellPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.88f, 0.05f, 0.88f),
                telegraphMaterial);
            GameObject bombPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.BombPrefabPath);
            GameObject explosionPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.ExplosionCellPrefabPath);

            PrototypeBombDefinitionAsset bombDefinition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(
                    PrototypeContentValidator.PrototypeThrowerBombDefinitionPath);
            if (bombDefinition == null)
            {
                bombDefinition = ScriptableObject.CreateInstance<
                    PrototypeBombDefinitionAsset>();
                bombDefinition.name = "PrototypeThrowerBlocker";
                AssetDatabase.CreateAsset(
                    bombDefinition,
                    PrototypeContentValidator.PrototypeThrowerBombDefinitionPath);
            }
            bombDefinition.Configure(
                "prototype-thrower-blocker",
                2f,
                1,
                bombPrefab,
                explosionPrefab,
                0.25f,
                1f,
                BombExplosionShape.Cross);
            EditorUtility.SetDirty(bombDefinition);

            PrototypeThrowerDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeThrowerDefinitionAsset>(
                    PrototypeContentValidator.PrototypeThrowerDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    PrototypeThrowerDefinitionAsset>();
                definition.name = "PrototypeThrower";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeThrowerDefinitionPath);
            }
            definition.Configure(
                "prototype-thrower",
                1f,
                0.3f,
                0.45f,
                0.75f,
                1,
                3,
                bombDefinition,
                enemyPrefab,
                telegraphPrefab,
                0f,
                0.12f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeBombDefinitionAsset GetOrCreateBossBombDefinition(
            string assetPath,
            string assetName,
            string definitionId,
            float fuseSeconds,
            GameObject bombPrefab,
            GameObject explosionPrefab)
        {
            PrototypeBombDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBombDefinitionAsset>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PrototypeBombDefinitionAsset>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.name = assetName;
            definition.Configure(
                definitionId,
                fuseSeconds,
                2,
                bombPrefab,
                explosionPrefab,
                0.25f,
                1.5f,
                BombExplosionShape.Cross);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeBossDefinitionAsset CreatePrototypeBossContentIfMissing(
            PrototypeBombDefinitionAsset throwBombDefinition,
            PrototypeBombDefinitionAsset chainBombDefinition)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }

            EnsureAssetFolder(PrototypePrefabsPath);
            EnsureAssetFolder("Assets/Game/Content/Bosses");
            Material bossMaterial = GetOrCreateMaterial(
                MaterialsPath + "/Boss.mat",
                shader,
                new Color(0.46f, 0.12f, 0.68f, 1f));
            Material dangerCellMaterial = GetOrCreateMaterial(
                MaterialsPath + "/BossDangerCell.mat",
                shader,
                new Color(1f, 0.7f, 0.08f, 0.68f));
            GameObject bossPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BossPrefabPath,
                "BossPlaceholder",
                PrimitiveType.Sphere,
                Vector3.zero,
                new Vector3(1.15f, 1.15f, 1.15f),
                bossMaterial);
            GameObject dangerCellPrefab = CreateVisualPrefabIfMissing(
                PrototypeContentValidator.BossDangerCellPrefabPath,
                "BossDangerCellPlaceholder",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.92f, 0.04f, 0.92f),
                dangerCellMaterial);

            PrototypeBossDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<PrototypeBossDefinitionAsset>(
                    PrototypeContentValidator.PrototypeBossDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PrototypeBossDefinitionAsset>();
                definition.name = "PrototypeBoss";
                AssetDatabase.CreateAsset(
                    definition,
                    PrototypeContentValidator.PrototypeBossDefinitionPath);
            }

            definition.Configure(
                "prototype-boss",
                10,
                7,
                2,
                1,
                2,
                new Vector2Int(0, 1),
                bossPrefab,
                dangerCellPrefab,
                throwBombDefinition,
                chainBombDefinition,
                0f,
                0.03f,
                0.2f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PrototypeCombatRoomDefinitionAsset[] CreatePrototypeCombatRoomContentIfMissing()
        {
            EnsureAssetFolder("Assets/Game/Content/Rooms");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Required URP Lit shader was not found.");
            }
            GetOrCreateMaterial(
                PrototypeContentValidator.DestructibleWallMaterialPath,
                shader,
                new Color(0.72f, 0.34f, 0.08f, 1f));
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
                CreateCardinalRoomExits(5, 4),
                Array.Empty<Vector2Int>());
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
                CreateCardinalRoomExits(5, 4),
                new[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
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
                    new Vector2Int(-4, -2),
                    new Vector2Int(-2, 1),
                    new Vector2Int(2, 1),
                    new Vector2Int(-3, -3),
                    new Vector2Int(-3, 3),
                    new Vector2Int(3, -3),
                    new Vector2Int(3, 3),
                },
                new[]
                {
                    new Vector2Int(-3, -2),
                    new Vector2Int(-3, -1),
                    new Vector2Int(-2, -2),
                },
                new[]
                {
                    new Vector2Int(-3, -1),
                    new Vector2Int(-2, -2),
                },
                CreateRectangleLoop(-1, 1, -1, 1),
                CreateCardinalRoomExits(5, 4),
                new[]
                {
                    new Vector2Int(2, -2),
                },
                new Vector2Int(-1, 1));
            EditorUtility.SetDirty(pillars);

            PrototypeCombatRoomDefinitionAsset armor = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeCombatArmorDefinitionPath,
                "PrototypeCombatArmor");
            armor.Configure(
                "prototype-combat-armor",
                RoomType.Combat,
                11,
                9,
                1f,
                new Vector2Int(0, -2),
                new Vector2Int(4, 4),
                new[]
                {
                    new Vector2Int(-2, -2),
                    new Vector2Int(2, -2),
                    new Vector2Int(-2, -1),
                    new Vector2Int(2, -1),
                    new Vector2Int(-1, 2),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(-4, 0),
                    new Vector2Int(4, 0),
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
                CreateRectangleLoop(-3, 3, -3, 3),
                CreateCardinalRoomExits(5, 4),
                Array.Empty<Vector2Int>(),
                null,
                new Vector2Int(0, 1));
            EditorUtility.SetDirty(armor);

            PrototypeCombatRoomDefinitionAsset gates = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeCombatGatesDefinitionPath,
                "PrototypeCombatGates");
            gates.Configure(
                "prototype-combat-gates",
                RoomType.Combat,
                11,
                9,
                1f,
                new Vector2Int(0, -3),
                new Vector2Int(0, 3),
                new[]
                {
                    new Vector2Int(-2, -1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
                    new Vector2Int(2, -1),
                    new Vector2Int(-2, 1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1),
                },
                new[]
                {
                    new Vector2Int(0, -3),
                    new Vector2Int(-1, -3),
                    new Vector2Int(1, -3),
                },
                new[]
                {
                    new Vector2Int(-3, -2),
                    new Vector2Int(3, -2),
                },
                CreateRectangleLoop(-3, 3, -2, 2),
                CreateCardinalRoomExits(5, 4),
                new[]
                {
                    new Vector2Int(0, -1),
                    new Vector2Int(0, 1),
                },
                null,
                null,
                new Vector2Int(3, 0),
                new[]
                {
                    new Vector2Int(0, -2),
                    new Vector2Int(0, 2),
                });
            EditorUtility.SetDirty(gates);

            return new[] { loop, lanes, pillars, armor, gates };
        }

        private static PrototypeCombatRoomDefinitionAsset
            CreatePrototypeBossRoomContentIfMissing()
        {
            PrototypeCombatRoomDefinitionAsset arena = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeBossArenaDefinitionPath,
                "PrototypeBossArena");
            arena.Configure(
                "prototype-boss-arena",
                RoomType.Combat,
                11,
                9,
                1f,
                new Vector2Int(0, -3),
                new Vector2Int(4, 3),
                new[]
                {
                    new Vector2Int(-2, -1),
                    new Vector2Int(2, -1),
                    new Vector2Int(-2, 1),
                    new Vector2Int(2, 1),
                },
                new[]
                {
                    new Vector2Int(0, -3),
                    new Vector2Int(-1, -3),
                    new Vector2Int(1, -3),
                },
                new[]
                {
                    new Vector2Int(-4, -2),
                    new Vector2Int(-3, 3),
                    new Vector2Int(0, -3),
                    new Vector2Int(0, 3),
                    new Vector2Int(3, 3),
                    new Vector2Int(4, -2),
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
                CreateCardinalRoomExits(5, 4),
                Array.Empty<Vector2Int>(),
                null,
                null,
                new Vector2Int(-4, 3),
                new[]
                {
                    new Vector2Int(-3, 3),
                    new Vector2Int(0, 3),
                    new Vector2Int(3, 3),
                });
            EditorUtility.SetDirty(arena);
            return arena;
        }

        private static PrototypeCombatRoomDefinitionAsset
            CreatePrototypeThrowerRoomContentIfMissing()
        {
            EnsureAssetFolder("Assets/Game/Content/Rooms");
            PrototypeCombatRoomDefinitionAsset room = GetOrCreateRoomDefinition(
                PrototypeContentValidator.PrototypeCombatThrowerDefinitionPath,
                "PrototypeCombatThrower");
            room.Configure(
                "prototype-combat-thrower",
                RoomType.Combat,
                11,
                9,
                1f,
                new Vector2Int(0, -2),
                new Vector2Int(-2, 2),
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
                CreateCardinalRoomExits(5, 4),
                new[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
                },
                authoredThrowerSpawn: new Vector2Int(3, 2),
                authoredThrowerFiringAnchors: new[]
                {
                    new Vector2Int(0, 3),
                    new Vector2Int(-3, 2),
                    new Vector2Int(3, -2),
                },
                authoredThrowerTargetAnchors: new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(-3, -2),
                    new Vector2Int(2, -3),
                    new Vector2Int(-4, 1),
                    new Vector2Int(4, 1),
                    new Vector2Int(0, 2),
                });
            EditorUtility.SetDirty(room);
            return room;
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

        private static PrototypeDungeonCombatRoomCatalogAsset
            CreatePrototypeDungeonCombatRoomCatalog(
                IReadOnlyList<PrototypeCombatRoomDefinitionAsset> roomDefinitions,
                PrototypeCombatRoomDefinitionAsset throwerRoomDefinition)
        {
            if (roomDefinitions == null || roomDefinitions.Count != 5)
            {
                throw new ArgumentException(
                    "Prototype dungeon combat room catalog requires five definitions.",
                    nameof(roomDefinitions));
            }
            if (throwerRoomDefinition == null)
            {
                throw new ArgumentNullException(nameof(throwerRoomDefinition));
            }

            PrototypeDungeonCombatRoomCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonCombatRoomCatalogAsset>(
                    PrototypeContentValidator.PrototypeDungeonCombatRoomCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    PrototypeDungeonCombatRoomCatalogAsset>();
                catalog.name = "PrototypeDungeonCombatRoomCatalog";
                AssetDatabase.CreateAsset(
                    catalog,
                    PrototypeContentValidator.PrototypeDungeonCombatRoomCatalogPath);
            }

            catalog.Configure(new[]
            {
                new PrototypeDungeonCombatRoomEntry(roomDefinitions[0], "TestSandbox"),
                new PrototypeDungeonCombatRoomEntry(
                    throwerRoomDefinition,
                    "TestSandboxThrower"),
                new PrototypeDungeonCombatRoomEntry(roomDefinitions[2], "TestSandboxPillars"),
                new PrototypeDungeonCombatRoomEntry(roomDefinitions[3], "TestSandboxArmor"),
                new PrototypeDungeonCombatRoomEntry(roomDefinitions[4], "TestSandboxGates"),
            });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static PrototypeDungeonSpecialRoomCatalogAsset
            CreatePrototypeDungeonSpecialRoomCatalog()
        {
            PrototypeDungeonSpecialRoomCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonSpecialRoomCatalogAsset>(
                    PrototypeContentValidator.PrototypeDungeonSpecialRoomCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    PrototypeDungeonSpecialRoomCatalogAsset>();
                catalog.name = "PrototypeDungeonSpecialRoomCatalog";
                AssetDatabase.CreateAsset(
                    catalog,
                    PrototypeContentValidator.PrototypeDungeonSpecialRoomCatalogPath);
            }

            catalog.Configure(new[]
            {
                new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "DungeonStart"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.BombReward,
                    "DungeonReward"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.BossAntechamber,
                    "DungeonBossAnte"),
                new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "DungeonBoss"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.Recovery,
                    "DungeonRecovery"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.Secret,
                    "DungeonSecret"),
            });
            EditorUtility.SetDirty(catalog);
            return catalog;
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

        private static PrototypeRoomExitData[] CreateCardinalRoomExits(
            int halfWidth,
            int halfDepth)
        {
            return new[]
            {
                new PrototypeRoomExitData(
                    new Vector2Int(0, halfDepth),
                    RoomExitDirection.North),
                new PrototypeRoomExitData(
                    new Vector2Int(halfWidth, 0),
                    RoomExitDirection.East),
                new PrototypeRoomExitData(
                    new Vector2Int(0, -halfDepth),
                    RoomExitDirection.South),
                new PrototypeRoomExitData(
                    new Vector2Int(-halfWidth, 0),
                    RoomExitDirection.West),
            };
        }

        private static bool EnsureTestSandbox(
            InputActionAsset inputActions,
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeContentValidator.TestSandboxScenePath) == null)
            {
                CreateTestSandbox(
                    inputActions,
                    bombLoadout,
                    playerVitals,
                    chaserDefinition,
                    chargerDefinition,
                    armoredDefinition,
                    bossDefinition,
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
                    bombLoadout,
                    playerVitals,
                    chaserDefinition,
                    chargerDefinition,
                    armoredDefinition,
                    bossDefinition,
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
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName,
            bool bossEnabled = false)
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
                    bombLoadout,
                    playerVitals,
                    chaserDefinition,
                    chargerDefinition,
                    armoredDefinition,
                    bossDefinition,
                    roomDefinition,
                    nextSceneName,
                    bossEnabled);
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

        private static bool EnsureArmoredPanicPlaytestScene(
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition)
        {
            bool created = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.ArmoredPanicPlaytestScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition,
                string.Empty);
            StripDungeonAdaptersFromStandalonePlaytest(
                PrototypeContentValidator.ArmoredPanicPlaytestScenePath);
            return created;
        }

        private static bool EnsureSelfDestructGatesPlaytestScene(
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition)
        {
            bool created = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.SelfDestructGatesPlaytestScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition,
                string.Empty);
            StripDungeonAdaptersFromStandalonePlaytest(
                PrototypeContentValidator.SelfDestructGatesPlaytestScenePath);
            return created;
        }

        private static bool EnsureBossBattlePlaytestScene(
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition)
        {
            bool created = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.BossBattlePlaytestScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition,
                string.Empty,
                true);
            StripDungeonAdaptersFromStandalonePlaytest(
                PrototypeContentValidator.BossBattlePlaytestScenePath);
            return created;
        }

        private static bool EnsureThrowerLanesPlaytestScene(
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition)
        {
            bool created = EnsurePlaytestRoomVariant(
                PrototypeContentValidator.ThrowerLanesPlaytestScenePath,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                chargerDefinition,
                armoredDefinition,
                bossDefinition,
                roomDefinition,
                string.Empty);
            StripDungeonAdaptersFromStandalonePlaytest(
                PrototypeContentValidator.ThrowerLanesPlaytestScenePath);
            return created;
        }

        private static void StripDungeonAdaptersFromStandalonePlaytest(
            string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpgrade)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                foreach (PrototypeDungeonRunHost host in
                    FindAllInScene<PrototypeDungeonRunHost>(scene))
                {
                    UnityEngine.Object.DestroyImmediate(host.gameObject);
                }
                DestroyAllInScene<PrototypeDungeonRoomBinder>(scene);
                DestroyAllInScene<PrototypeDungeonMinimapPresenter>(scene);
                DestroyAllInScene<PrototypeDungeonDoorPresenter>(scene);
                DestroyAllInScene<PrototypeRunCompletionPresenter>(scene);

                PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
                PrototypeRoomAdvanceController advance =
                    FindExactlyOne<PrototypeRoomAdvanceController>(scene);
                advance.Configure(session, string.Empty);
                EditorUtility.SetDirty(advance);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save standalone playtest scene '{scenePath}'.");
                }
            }
            finally
            {
                if (openedForUpgrade && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void EnsureDungeonRoomBinding(
            string scenePath,
            PrototypeDungeonCombatRoomCatalogAsset combatRoomCatalog,
            PrototypeDungeonSpecialRoomCatalogAsset specialRoomCatalog,
            PrototypeBombRewardCatalogAsset bombRewardCatalog,
            PrototypePlayerVitalsAsset playerVitals,
            bool combatEnabled)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpgrade)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                SynchronizeDungeonSceneBindings(
                    scene,
                    combatRoomCatalog,
                    specialRoomCatalog,
                    bombRewardCatalog,
                    playerVitals,
                    combatEnabled,
                    false);
                SynchronizeBombRewardPresenter(scene, false);
                SynchronizeRecoveryPickupPresenter(scene, false);
                SynchronizeSecretRewardPresenter(scene, false);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save dungeon-bound room scene '{scenePath}'.");
                }
            }
            finally
            {
                if (openedForUpgrade && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool EnsureDungeonSpecialRoom(
            string scenePath,
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset shellRoomDefinition,
            PrototypeDungeonCombatRoomCatalogAsset combatRoomCatalog,
            PrototypeDungeonSpecialRoomCatalogAsset specialRoomCatalog,
            PrototypeBombRewardCatalogAsset bombRewardCatalog,
            bool combatEnabled,
            bool bossEnabled)
        {
            EnsureAssetFolder("Assets/Game/Scenes/Dungeon");
            bool created = false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(
                        PrototypeContentValidator.TestSandboxScenePath,
                        scenePath))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to create dungeon special-room scene '{scenePath}'.");
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
                    bombLoadout,
                    playerVitals,
                    chaserDefinition,
                    chargerDefinition,
                    armoredDefinition,
                    bossDefinition,
                    shellRoomDefinition,
                    string.Empty);
                ConfigureDungeonCombatMode(
                    scene,
                    bombLoadout,
                    playerVitals,
                    chaserDefinition,
                    chargerDefinition,
                    armoredDefinition,
                    bossDefinition,
                    combatEnabled,
                    bossEnabled);
                SynchronizeDungeonSceneBindings(
                    scene,
                    combatRoomCatalog,
                    specialRoomCatalog,
                    bombRewardCatalog,
                    playerVitals,
                    combatEnabled,
                    bossEnabled);
                SynchronizeBombRewardPresenter(
                    scene,
                    string.Equals(
                        scenePath,
                        PrototypeContentValidator.DungeonRewardScenePath,
                        StringComparison.Ordinal));
                SynchronizeRecoveryPickupPresenter(
                    scene,
                    string.Equals(
                        scenePath,
                        PrototypeContentValidator.DungeonRecoveryScenePath,
                        StringComparison.Ordinal));
                SynchronizeSecretRewardPresenter(
                    scene,
                    string.Equals(
                        scenePath,
                        PrototypeContentValidator.DungeonSecretScenePath,
                        StringComparison.Ordinal));
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save dungeon special-room scene '{scenePath}'.");
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

        private static void ConfigureDungeonCombatMode(
            Scene scene,
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            bool combatEnabled,
            bool bossEnabled)
        {
            PrototypeSelfDestructDefinitionAsset selfDestructDefinition =
                LoadRequiredAsset<PrototypeSelfDestructDefinitionAsset>(
                    PrototypeContentValidator.PrototypeSelfDestructDefinitionPath);
            TestSandboxContext context = FindExactlyOne<TestSandboxContext>(scene);
            BombSwapInputReader inputReader = FindExactlyOne<BombSwapInputReader>(scene);
            PrototypeGameSession gameSession = FindExactlyOne<PrototypeGameSession>(scene);
            gameSession.Configure(
                context,
                inputReader,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                startingCharger: chargerDefinition,
                startingArmored: armoredDefinition,
                startingCombatEnabled: combatEnabled,
                startingBoss: bossDefinition,
                startingBossEnabled: bossEnabled,
                startingSelfDestruct: selfDestructDefinition);
            EditorUtility.SetDirty(gameSession);
        }

        private static void SynchronizeDungeonSceneBindings(
            Scene scene,
            PrototypeDungeonCombatRoomCatalogAsset combatRoomCatalog,
            PrototypeDungeonSpecialRoomCatalogAsset specialRoomCatalog,
            PrototypeBombRewardCatalogAsset bombRewardCatalog,
            PrototypePlayerVitalsAsset playerVitals,
            bool combatEnabled,
            bool bossEnabled)
        {
            TestSandboxContext context = FindExactlyOne<TestSandboxContext>(scene);
            PrototypeGameSession gameSession = FindExactlyOne<PrototypeGameSession>(scene);
            if (gameSession.HasChaser != (combatEnabled && !bossEnabled) ||
                gameSession.HasBoss != bossEnabled)
            {
                throw new InvalidOperationException(
                    $"Dungeon scene '{scene.path}' combat mode was not configured consistently.");
            }

            context.GridRoot.localRotation = Quaternion.identity;
            DungeonBoundaryPresentation boundaryPresentation =
                SynchronizeDungeonBoundary(context);
            GameObject systems = gameSession.gameObject;
            PrototypeRoomAdvanceController[] legacyControllers =
                systems.GetComponents<PrototypeRoomAdvanceController>();
            for (int index = 0; index < legacyControllers.Length; index++)
            {
                UnityEngine.Object.DestroyImmediate(legacyControllers[index]);
            }

            PrototypeDungeonDoorPresenter doorPresenter =
                systems.GetComponent<PrototypeDungeonDoorPresenter>();
            if (doorPresenter == null)
            {
                doorPresenter = systems.AddComponent<PrototypeDungeonDoorPresenter>();
            }
            doorPresenter.Configure(
                boundaryPresentation.Doors[0],
                boundaryPresentation.Doors[1],
                boundaryPresentation.Doors[2],
                boundaryPresentation.Doors[3],
                boundaryPresentation.DoorAnimators[0],
                boundaryPresentation.DoorAnimators[1],
                boundaryPresentation.DoorAnimators[2],
                boundaryPresentation.DoorAnimators[3],
                boundaryPresentation.SecretCrackRoots[0],
                boundaryPresentation.SecretCrackRoots[1],
                boundaryPresentation.SecretCrackRoots[2],
                boundaryPresentation.SecretCrackRoots[3]);
            doorPresenter.ConfigureSecretWallBreakVfx(null);

            PrototypeDungeonRoomBinder binder =
                systems.GetComponent<PrototypeDungeonRoomBinder>();
            if (binder == null)
            {
                binder = systems.AddComponent<PrototypeDungeonRoomBinder>();
            }
            binder.Configure(gameSession, doorPresenter, context.GridRoot);

            PrototypeDungeonMinimapPresenter minimapPresenter =
                systems.GetComponent<PrototypeDungeonMinimapPresenter>();
            if (minimapPresenter == null)
            {
                minimapPresenter =
                    systems.AddComponent<PrototypeDungeonMinimapPresenter>();
            }
            minimapPresenter.Configure(binder);

            BombSwapInputReader inputReader =
                systems.GetComponent<BombSwapInputReader>();
            if (inputReader == null)
            {
                throw new InvalidOperationException(
                    $"Dungeon scene '{scene.path}' is missing its input reader.");
            }
            PrototypeRunCompletionPresenter completionPresenter =
                systems.GetComponent<PrototypeRunCompletionPresenter>();
            if (completionPresenter == null)
            {
                completionPresenter =
                    systems.AddComponent<PrototypeRunCompletionPresenter>();
            }
            completionPresenter.Configure(binder, inputReader);
            SetSerializedObjectName(
                completionPresenter,
                nameof(PrototypeRunCompletionPresenter));

            PrototypeDungeonRunHost[] hosts = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<PrototypeDungeonRunHost>(true))
                .ToArray();
            if (hosts.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Dungeon scene '{scene.path}' contains multiple run hosts.");
            }

            PrototypeDungeonRunHost host;
            if (hosts.Length == 0)
            {
                var hostObject = new GameObject("DungeonRunBootstrap");
                SceneManager.MoveGameObjectToScene(hostObject, scene);
                host = hostObject.AddComponent<PrototypeDungeonRunHost>();
            }
            else
            {
                host = hosts[0];
                host.gameObject.name = "DungeonRunBootstrap";
                host.transform.SetParent(null);
            }
            host.Configure(
                0,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals,
                true);

            EditorUtility.SetDirty(context.GridRoot);
            EditorUtility.SetDirty(doorPresenter);
            EditorUtility.SetDirty(binder);
            EditorUtility.SetDirty(minimapPresenter);
            EditorUtility.SetDirty(completionPresenter);
            EditorUtility.SetDirty(host);
        }

        private static void SynchronizeBombRewardPresenter(
            Scene scene,
            bool shouldExist)
        {
            PrototypeBombRewardPresenter[] presenters =
                FindAllInScene<PrototypeBombRewardPresenter>(scene);
            if (!shouldExist)
            {
                for (int index = 0; index < presenters.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(presenters[index]);
                }
                return;
            }
            if (presenters.Length > 1)
            {
                for (int index = 1; index < presenters.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(presenters[index]);
                }
            }

            PrototypeDungeonRoomBinder binder =
                FindExactlyOne<PrototypeDungeonRoomBinder>(scene);
            PrototypeBombRewardPresenter presenter = presenters.Length > 0
                ? presenters[0]
                : binder.gameObject.AddComponent<PrototypeBombRewardPresenter>();
            presenter.Configure(binder);
            EditorUtility.SetDirty(presenter);
        }

        private static void SynchronizeRecoveryPickupPresenter(
            Scene scene,
            bool shouldExist)
        {
            PrototypeRecoveryPickupPresenter[] presenters =
                FindAllInScene<PrototypeRecoveryPickupPresenter>(scene);
            if (!shouldExist)
            {
                for (int index = 0; index < presenters.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(presenters[index]);
                }
                return;
            }
            if (presenters.Length > 1)
            {
                for (int index = 1; index < presenters.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(presenters[index]);
                }
            }

            PrototypeDungeonRoomBinder binder =
                FindExactlyOne<PrototypeDungeonRoomBinder>(scene);
            Material pickupMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                PrototypeContentValidator.RecoveryPickupMaterialPath);
            if (pickupMaterial == null)
            {
                throw new InvalidOperationException(
                    "Prototype recovery pickup material is missing.");
            }
            PrototypeRecoveryPickupPresenter presenter = presenters.Length > 0
                ? presenters[0]
                : binder.gameObject.AddComponent<PrototypeRecoveryPickupPresenter>();
            presenter.Configure(
                binder,
                pickupMaterial,
                PrototypeRecoveryPickupPresenter.DefaultRecoveryAmount,
                Vector2Int.zero);
            EditorUtility.SetDirty(presenter);
        }

        private static void SynchronizeSecretRewardPresenter(
            Scene scene,
            bool shouldExist)
        {
            PrototypeSecretRewardPresenter[] presenters =
                FindAllInScene<PrototypeSecretRewardPresenter>(scene);
            if (!shouldExist)
            {
                for (int index = 0; index < presenters.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(presenters[index]);
                }
                return;
            }
            if (presenters.Length > 1)
            {
                for (int index = 1; index < presenters.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(presenters[index]);
                }
            }

            PrototypeDungeonRoomBinder binder =
                FindExactlyOne<PrototypeDungeonRoomBinder>(scene);
            Material rewardMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                PrototypeContentValidator.SecretRewardMaterialPath);
            if (rewardMaterial == null)
            {
                throw new InvalidOperationException(
                    "Prototype secret reward material is missing.");
            }
            PrototypeSecretRewardPresenter presenter = presenters.Length > 0
                ? presenters[0]
                : binder.gameObject.AddComponent<PrototypeSecretRewardPresenter>();
            presenter.Configure(
                binder,
                rewardMaterial,
                PrototypeSecretRewardPresenter.DefaultTokenReward,
                Vector2Int.zero);
            EditorUtility.SetDirty(presenter);
        }

        private static void CreatePrototypeRecoveryMaterialIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required URP Lit shader was not found.");
            }
            GetOrCreateMaterial(
                PrototypeContentValidator.RecoveryPickupMaterialPath,
                shader,
                PrototypeRecoveryPickupPresenter.DefaultPickupColor);
        }

        private static void CreatePrototypeSecretMaterialsIfMissing()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required URP Lit shader was not found.");
            }
            GetOrCreateMaterial(
                PrototypeContentValidator.SecretRewardMaterialPath,
                shader,
                PrototypeSecretRewardPresenter.DefaultRewardColor);
            GetOrCreateMaterial(
                PrototypeContentValidator.SecretCrackMaterialPath,
                shader,
                new Color(0.08f, 0.045f, 0.025f, 1f));
        }

        private static DungeonBoundaryPresentation SynchronizeDungeonBoundary(
            TestSandboxContext context)
        {
            Transform environment = context.GridRoot.Find("Environment");
            if (environment == null)
            {
                throw new InvalidOperationException(
                    "Dungeon room is missing its Environment root.");
            }
            Transform boundary = environment.Find("BoundaryWalls");
            if (boundary == null)
            {
                boundary = CreateChild("BoundaryWalls", environment);
            }

            string[] expectedNames =
            {
                "NorthWallWest", "NorthWallEast",
                "SouthWallWest", "SouthWallEast",
                "EastWallSouth", "EastWallNorth",
                "WestWallSouth", "WestWallNorth",
                "NorthDoor", "EastDoor", "SouthDoor", "WestDoor",
                "NorthSecretCracks", "EastSecretCracks",
                "SouthSecretCracks", "WestSecretCracks",
            };
            var expected = new HashSet<string>(expectedNames, StringComparer.Ordinal);
            for (int index = boundary.childCount - 1; index >= 0; index--)
            {
                Transform child = boundary.GetChild(index);
                if (!expected.Contains(child.name))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            PrototypeCombatRoomDefinitionAsset definition = context.RoomDefinition;
            CombatRoomDefinition room = definition.CreateCoreDefinition();
            float cellSize = definition.CellSize;
            float boundaryX = ((room.Width / 2) + 1) * cellSize;
            float boundaryZ = ((room.Depth / 2) + 1) * cellSize;
            float horizontalLength = ((room.Width + 2) * cellSize - cellSize) * 0.5f;
            float verticalLength = (room.Depth * cellSize - cellSize) * 0.5f;
            float horizontalCenter = (cellSize + horizontalLength) * 0.5f;
            float verticalCenter = (cellSize + verticalLength) * 0.5f;
            Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialsPath + "/Wall.mat");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material doorMaterial = GetOrCreateMaterial(
                MaterialsPath + "/DungeonDoor.mat",
                shader,
                new Color(0.18f, 0.22f, 0.27f, 1f));
            Material crackMaterial = GetOrCreateMaterial(
                PrototypeContentValidator.SecretCrackMaterialPath,
                shader,
                new Color(0.08f, 0.045f, 0.025f, 1f));
            Material destructibleWallMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                PrototypeContentValidator.DestructibleWallMaterialPath);
            if (destructibleWallMaterial == null)
            {
                throw new InvalidOperationException(
                    "Prototype destructible-wall material is missing.");
            }

            EnsureBoundaryPrimitive(
                boundary, "NorthWallWest",
                new Vector3(-horizontalCenter, 0.5f, boundaryZ),
                new Vector3(horizontalLength, 1f, cellSize), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "NorthWallEast",
                new Vector3(horizontalCenter, 0.5f, boundaryZ),
                new Vector3(horizontalLength, 1f, cellSize), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "SouthWallWest",
                new Vector3(-horizontalCenter, 0.5f, -boundaryZ),
                new Vector3(horizontalLength, 1f, cellSize), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "SouthWallEast",
                new Vector3(horizontalCenter, 0.5f, -boundaryZ),
                new Vector3(horizontalLength, 1f, cellSize), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "EastWallSouth",
                new Vector3(boundaryX, 0.5f, -verticalCenter),
                new Vector3(cellSize, 1f, verticalLength), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "EastWallNorth",
                new Vector3(boundaryX, 0.5f, verticalCenter),
                new Vector3(cellSize, 1f, verticalLength), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "WestWallSouth",
                new Vector3(-boundaryX, 0.5f, -verticalCenter),
                new Vector3(cellSize, 1f, verticalLength), wallMaterial, true);
            EnsureBoundaryPrimitive(
                boundary, "WestWallNorth",
                new Vector3(-boundaryX, 0.5f, verticalCenter),
                new Vector3(cellSize, 1f, verticalLength), wallMaterial, true);

            bool usesDoorPrefabs =
                PrototypeContentValidator.IsDungeonPresentationScenePath(
                    context.gameObject.scene.path);
            DoorVisualPresentation northPresentation = usesDoorPrefabs
                ? EnsureDoorVisualPrefab(
                    boundary, "NorthDoor", new Vector3(0f, 0f, boundaryZ),
                    Quaternion.identity)
                : new DoorVisualPresentation(
                    EnsureBoundaryPrimitive(
                        boundary, "NorthDoor", new Vector3(0f, 0.45f, boundaryZ),
                        new Vector3(0.85f * cellSize, 0.9f, 0.3f * cellSize),
                        doorMaterial, false),
                    null);
            DoorVisualPresentation eastPresentation = usesDoorPrefabs
                ? EnsureDoorVisualPrefab(
                    boundary, "EastDoor", new Vector3(boundaryX, 0f, 0f),
                    Quaternion.Euler(0f, 90f, 0f))
                : new DoorVisualPresentation(
                    EnsureBoundaryPrimitive(
                        boundary, "EastDoor", new Vector3(boundaryX, 0.45f, 0f),
                        new Vector3(0.3f * cellSize, 0.9f, 0.85f * cellSize),
                        doorMaterial, false),
                    null);
            DoorVisualPresentation southPresentation = usesDoorPrefabs
                ? EnsureDoorVisualPrefab(
                    boundary, "SouthDoor", new Vector3(0f, 0f, -boundaryZ),
                    Quaternion.Euler(0f, 180f, 0f))
                : new DoorVisualPresentation(
                    EnsureBoundaryPrimitive(
                        boundary, "SouthDoor", new Vector3(0f, 0.45f, -boundaryZ),
                        new Vector3(0.85f * cellSize, 0.9f, 0.3f * cellSize),
                        doorMaterial, false),
                    null);
            DoorVisualPresentation westPresentation = usesDoorPrefabs
                ? EnsureDoorVisualPrefab(
                    boundary, "WestDoor", new Vector3(-boundaryX, 0f, 0f),
                    Quaternion.Euler(0f, 270f, 0f))
                : new DoorVisualPresentation(
                    EnsureBoundaryPrimitive(
                        boundary, "WestDoor", new Vector3(-boundaryX, 0.45f, 0f),
                        new Vector3(0.3f * cellSize, 0.9f, 0.85f * cellSize),
                        doorMaterial, false),
                    null);
            Renderer north = northPresentation.Renderer;
            Renderer east = eastPresentation.Renderer;
            Renderer south = southPresentation.Renderer;
            Renderer west = westPresentation.Renderer;
            GameObject northCracks = EnsureSecretCrackRoot(
                boundary,
                "NorthSecretCracks",
                northPresentation.Animator != null
                    ? northPresentation.Animator.transform.localPosition
                    : north.transform.localPosition,
                true,
                cellSize,
                destructibleWallMaterial,
                crackMaterial);
            GameObject eastCracks = EnsureSecretCrackRoot(
                boundary,
                "EastSecretCracks",
                eastPresentation.Animator != null
                    ? eastPresentation.Animator.transform.localPosition
                    : east.transform.localPosition,
                false,
                cellSize,
                destructibleWallMaterial,
                crackMaterial);
            GameObject southCracks = EnsureSecretCrackRoot(
                boundary,
                "SouthSecretCracks",
                southPresentation.Animator != null
                    ? southPresentation.Animator.transform.localPosition
                    : south.transform.localPosition,
                true,
                cellSize,
                destructibleWallMaterial,
                crackMaterial);
            GameObject westCracks = EnsureSecretCrackRoot(
                boundary,
                "WestSecretCracks",
                westPresentation.Animator != null
                    ? westPresentation.Animator.transform.localPosition
                    : west.transform.localPosition,
                false,
                cellSize,
                destructibleWallMaterial,
                crackMaterial);
            return new DungeonBoundaryPresentation(
                new[] { north, east, south, west },
                new[]
                {
                    northPresentation.Animator,
                    eastPresentation.Animator,
                    southPresentation.Animator,
                    westPresentation.Animator,
                },
                new[] { northCracks, eastCracks, southCracks, westCracks });
        }

        [MenuItem("Bomb Swap/Prototype/Synchronize TestSandbox Door Presentation")]
        public static void SynchronizeTestSandboxDoorPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before synchronizing TestSandbox doors.");
            }

            SynchronizeDungeonEnvironmentPresentationScene(
                PrototypeContentValidator.TestSandboxScenePath);
        }

        [MenuItem("Bomb Swap/Prototype/Synchronize All Dungeon Environment Presentation")]
        public static void SynchronizeAllDungeonEnvironmentPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before synchronizing dungeon environment presentation.");
            }

            string[] scenePaths =
            {
                PrototypeContentValidator.DungeonStartScenePath,
                PrototypeContentValidator.DungeonRewardScenePath,
                PrototypeContentValidator.DungeonBossAnteScenePath,
                PrototypeContentValidator.DungeonRecoveryScenePath,
                PrototypeContentValidator.DungeonSecretScenePath,
                PrototypeContentValidator.DungeonBossScenePath,
                PrototypeContentValidator.TestSandboxScenePath,
                PrototypeContentValidator.TestSandboxThrowerScenePath,
                PrototypeContentValidator.TestSandboxPillarsScenePath,
                PrototypeContentValidator.TestSandboxArmorScenePath,
                PrototypeContentValidator.TestSandboxGatesScenePath,
            };
            for (int index = 0; index < scenePaths.Length; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePaths[index]) == null)
                {
                    throw new InvalidOperationException(
                        $"Dungeon scene is missing at '{scenePaths[index]}'.");
                }
                Scene loadedScene = SceneManager.GetSceneByPath(scenePaths[index]);
                if (loadedScene.IsValid() && loadedScene.isLoaded && loadedScene.isDirty)
                {
                    throw new InvalidOperationException(
                        $"Dungeon scene '{scenePaths[index]}' has unsaved changes. Save or revert them before synchronization.");
                }
            }
            for (int index = 0; index < scenePaths.Length; index++)
            {
                SynchronizeDungeonEnvironmentPresentationScene(scenePaths[index]);
            }
        }

        private static void SynchronizeDungeonEnvironmentPresentationScene(
            string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForSynchronization = !scene.IsValid() || !scene.isLoaded;
            if (openedForSynchronization)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
            else if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    $"Dungeon scene '{scenePath}' has unsaved changes. Save or revert them before synchronization.");
            }

            try
            {
                TestSandboxContext context = FindExactlyOne<TestSandboxContext>(scene);
                PrototypeDungeonDoorPresenter presenter =
                    FindExactlyOne<PrototypeDungeonDoorPresenter>(scene);
                PrototypeCombatRoomDefinitionAsset roomDefinition =
                    context.RoomDefinition ?? throw new InvalidOperationException(
                        $"Dungeon scene '{scenePath}' context is missing its room definition.");
                CombatRoomDefinition room = roomDefinition.CreateCoreDefinition();
                SynchronizeInteriorObstacles(
                    context.GridRoot,
                    roomDefinition,
                    room);
                EnvironmentBlockVisualAuthoring.Synchronize(
                    context.GridRoot,
                    roomDefinition,
                    room);
                DungeonBoundaryPresentation presentation =
                    SynchronizeDungeonBoundary(context);
                presenter.Configure(
                    presentation.Doors[0],
                    presentation.Doors[1],
                    presentation.Doors[2],
                    presentation.Doors[3],
                    presentation.DoorAnimators[0],
                    presentation.DoorAnimators[1],
                    presentation.DoorAnimators[2],
                    presentation.DoorAnimators[3],
                    presentation.SecretCrackRoots[0],
                    presentation.SecretCrackRoots[1],
                    presentation.SecretCrackRoots[2],
                    presentation.SecretCrackRoots[3]);
                presenter.ConfigureSecretWallBreakVfx(null);
                EditorUtility.SetDirty(presenter);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save dungeon environment presentation '{scenePath}'.");
                }
            }
            finally
            {
                if (openedForSynchronization && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static DoorVisualPresentation EnsureDoorVisualPrefab(
            Transform boundary,
            string name,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnvironmentBlockVisualAuthoring.DoorPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Dungeon door visual prefab is missing at '{EnvironmentBlockVisualAuthoring.DoorPrefabPath}'.");
            }

            Transform existing = boundary.Find(name);
            GameObject instance = existing != null ? existing.gameObject : null;
            if (instance != null &&
                PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefab)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                instance = null;
            }
            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, boundary) as GameObject;
            }
            if (instance == null)
            {
                throw new InvalidOperationException("Unity failed to instantiate the dungeon door prefab.");
            }

            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            RemovePhysicsComponents(instance);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            if (renderers.Length != 1 || animators.Length != 1)
            {
                throw new InvalidOperationException(
                    "Dungeon door prefab requires exactly one Renderer and one Animator.");
            }
            return new DoorVisualPresentation(renderers[0], animators[0]);
        }

        private static GameObject EnsureSecretCrackRoot(
            Transform boundary,
            string rootName,
            Vector3 secretDoorLocalPosition,
            bool northSouthDoor,
            float cellSize,
            Material wallMaterial,
            Material crackMaterial)
        {
            bool usesCrackedBrickPrefab =
                PrototypeContentValidator.IsDungeonPresentationScenePath(
                    boundary.gameObject.scene.path);
            if (usesCrackedBrickPrefab)
            {
                return EnsureSecretCrackPrefab(
                    boundary,
                    rootName,
                    secretDoorLocalPosition,
                    northSouthDoor ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f));
            }

            Transform root = boundary.Find(rootName);
            if (root == null)
            {
                root = CreateChild(rootName, boundary);
            }
            root.localPosition = secretDoorLocalPosition;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            const string surfaceName = "SecretWallSurface";
            string[] barNames = { "CrackA", "CrackB", "CrackC" };
            var expected = new HashSet<string>(barNames, StringComparer.Ordinal)
            {
                surfaceName,
            };
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                if (!expected.Contains(root.GetChild(index).name))
                {
                    UnityEngine.Object.DestroyImmediate(root.GetChild(index).gameObject);
                }
            }

            Vector3 surfaceScale = northSouthDoor
                ? new Vector3(0.85f * cellSize, 0.9f, 0.3f * cellSize)
                : new Vector3(0.3f * cellSize, 0.9f, 0.85f * cellSize);
            EnsureBoundaryPrimitive(
                root,
                surfaceName,
                new Vector3(0f, 0.45f, 0f),
                surfaceScale,
                wallMaterial,
                false);

            float[] offsets = { -0.2f, 0f, 0.2f };
            float[] angles = northSouthDoor
                ? new[] { 28f, -36f, 32f }
                : new[] { 62f, -54f, 58f };
            for (int index = 0; index < barNames.Length; index++)
            {
                Vector3 position = northSouthDoor
                    ? new Vector3(offsets[index] * cellSize, 0.47f, 0f)
                    : new Vector3(0f, 0.47f, offsets[index] * cellSize);
                Vector3 scale = northSouthDoor
                    ? new Vector3(0.38f * cellSize, 0.055f, 0.07f * cellSize)
                    : new Vector3(0.07f * cellSize, 0.055f, 0.38f * cellSize);
                Renderer bar = EnsureBoundaryPrimitive(
                    root,
                    barNames[index],
                    position,
                    scale,
                    crackMaterial,
                    false);
                bar.transform.localRotation = Quaternion.Euler(0f, angles[index], 0f);
            }

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject EnsureSecretCrackPrefab(
            Transform boundary,
            string name,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnvironmentBlockVisualAuthoring.CrackedBrickBlockPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Secret door visual prefab is missing at '{EnvironmentBlockVisualAuthoring.CrackedBrickBlockPrefabPath}'.");
            }

            Transform existing = boundary.Find(name);
            GameObject instance = existing != null ? existing.gameObject : null;
            if (instance != null &&
                PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefab)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                instance = null;
            }
            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, boundary) as GameObject;
            }
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Unity failed to instantiate the secret door visual prefab.");
            }

            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            RemovePhysicsComponents(instance);
            if (instance.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException(
                    "Secret door visual prefab requires at least one Renderer.");
            }
            instance.SetActive(false);
            return instance;
        }

        private static void RemovePhysicsComponents(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
            {
                UnityEngine.Object.DestroyImmediate(rigidbody);
            }
        }

        private static Renderer EnsureBoundaryPrimitive(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            Transform existing = parent.Find(name);
            GameObject instance = existing != null
                ? existing.gameObject
                : CreatePrimitive(
                    name,
                    PrimitiveType.Cube,
                    parent,
                    localPosition,
                    localScale,
                    material,
                    keepCollider);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = localScale;
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"Dungeon boundary primitive '{name}' requires a renderer.");
            }
            renderer.sharedMaterial = material;
            Collider collider = instance.GetComponent<Collider>();
            if (keepCollider && collider == null)
            {
                instance.AddComponent<BoxCollider>();
            }
            else if (!keepCollider && collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            if (!keepCollider)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return renderer;
        }

        private static void CreateTestSandbox(
            InputActionAsset inputActions,
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName)
        {
            PrototypeSelfDestructDefinitionAsset selfDestructDefinition =
                LoadRequiredAsset<PrototypeSelfDestructDefinitionAsset>(
                    PrototypeContentValidator.PrototypeSelfDestructDefinitionPath);
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
            Material destructibleWallMaterial = GetOrCreateMaterial(
                PrototypeContentValidator.DestructibleWallMaterialPath,
                shader,
                new Color(0.72f, 0.34f, 0.08f, 1f));
            CombatRoomDefinition room = roomDefinition.CreateCoreDefinition();
            float cellSize = roomDefinition.CellSize;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TestSandbox");

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            systems.SetActive(false);
            BombSwapInputReader inputReader = systems.AddComponent<BombSwapInputReader>();
            inputReader.Configure(inputActions);
            PrototypeUserSettingsRuntime settingsRuntime =
                systems.AddComponent<PrototypeUserSettingsRuntime>();
            settingsRuntime.Configure(
                inputActions,
                LoadRequiredAsset<AudioMixer>(PrototypeContentValidator.AudioMixerPath));
            PrototypeGameSession gameSession = systems.AddComponent<PrototypeGameSession>();
            PrototypePlayerController playerController =
                systems.AddComponent<PrototypePlayerController>();
            PrototypePlayerAnimationPresenter playerAnimationPresenter =
                systems.AddComponent<PrototypePlayerAnimationPresenter>();
            PrototypeBombPresenter bombPresenter = systems.AddComponent<PrototypeBombPresenter>();
            PrototypeDestructibleWallPresenter destructibleWallPresenter =
                systems.AddComponent<PrototypeDestructibleWallPresenter>();
            PrototypePlayerHealthPresenter healthPresenter =
                systems.AddComponent<PrototypePlayerHealthPresenter>();
            PrototypeChaserPresenter chaserPresenter =
                systems.AddComponent<PrototypeChaserPresenter>();
            PrototypeChargerPresenter chargerPresenter =
                systems.AddComponent<PrototypeChargerPresenter>();
            PrototypeArmoredPresenter armoredPresenter =
                systems.AddComponent<PrototypeArmoredPresenter>();
            PrototypeSelfDestructPresenter selfDestructPresenter =
                systems.AddComponent<PrototypeSelfDestructPresenter>();
            PrototypeBossPresenter bossPresenter =
                systems.AddComponent<PrototypeBossPresenter>();
            PrototypeWeaponHud weaponHud = systems.AddComponent<PrototypeWeaponHud>();
            PrototypeHealthHud healthHud = systems.AddComponent<PrototypeHealthHud>();
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
                    new Vector3(wall.X * cellSize, 0f, wall.Z * cellSize),
                    new Vector3(0.9f * cellSize, 1f, 0.9f * cellSize),
                    wallMaterial,
                    true);
            }

            Transform destructibleObstacles = CreateChild("DestructibleObstacles", environment);
            for (int index = 0; index < room.DestructibleWalls.Count; index++)
            {
                CreateDestructibleWallVisual(
                    destructibleObstacles,
                    room.DestructibleWalls[index],
                    cellSize,
                    destructibleWallMaterial,
                    index);
            }
            EnvironmentBlockVisualAuthoring.Synchronize(
                gridRoot.transform,
                roomDefinition,
                room);

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
            Transform chargerSpawn = null;
            if (room.ChargerSpawn.HasValue)
            {
                GridPosition chargerCell = room.ChargerSpawn.Value;
                chargerSpawn = CreateChild("ChargerSpawn", gridRoot.transform);
                chargerSpawn.localPosition = new Vector3(
                    chargerCell.X * cellSize,
                    0f,
                    chargerCell.Z * cellSize);
            }
            Transform armoredSpawn = null;
            if (room.ArmoredSpawn.HasValue)
            {
                GridPosition armoredCell = room.ArmoredSpawn.Value;
                armoredSpawn = CreateChild("ArmoredSpawn", gridRoot.transform);
                armoredSpawn.localPosition = new Vector3(
                    armoredCell.X * cellSize,
                    0f,
                    armoredCell.Z * cellSize);
            }
            Transform selfDestructSpawn = null;
            if (room.SelfDestructSpawn.HasValue)
            {
                GridPosition selfDestructCell = room.SelfDestructSpawn.Value;
                selfDestructSpawn = CreateChild("SelfDestructSpawn", gridRoot.transform);
                selfDestructSpawn.localPosition = new Vector3(
                    selfDestructCell.X * cellSize,
                    0f,
                    selfDestructCell.Z * cellSize);
            }
            Transform player = SynchronizePlayerPresentation(
                gridRoot.transform,
                null,
                new Vector3(
                    room.PlayerSpawn.X * cellSize,
                    0f,
                    room.PlayerSpawn.Z * cellSize));
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
                player,
                chaserSpawn,
                roomDefinition,
                chargerSpawn,
                armoredSpawn,
                selfDestructSpawn);
            gameSession.Configure(
                context,
                inputReader,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                startingCharger: chargerDefinition,
                startingArmored: armoredDefinition,
                startingBoss: bossDefinition,
                startingSelfDestruct: selfDestructDefinition);
            Animator playerAnimator = player.GetComponentInChildren<Animator>(true);
            playerController.Configure(gameSession, player);
            playerAnimationPresenter.Configure(gameSession, playerAnimator);
            bombPresenter.Configure(gameSession, runtimePresentation);
            destructibleWallPresenter.Configure(gameSession, destructibleObstacles);
            healthPresenter.Configure(gameSession, player.GetComponentInChildren<Renderer>());
            chaserPresenter.Configure(gameSession, runtimePresentation);
            chargerPresenter.Configure(gameSession, runtimePresentation);
            armoredPresenter.Configure(gameSession, runtimePresentation);
            selfDestructPresenter.Configure(gameSession, runtimePresentation);
            bossPresenter.Configure(gameSession, runtimePresentation);
            SetSerializedObjectName(bossPresenter, nameof(PrototypeBossPresenter));
            weaponHud.Configure(gameSession);
            healthHud.Configure(gameSession);
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
            PrototypeBombLoadoutAsset bombLoadout,
            PrototypePlayerVitalsAsset playerVitals,
            PrototypeChaserDefinitionAsset chaserDefinition,
            PrototypeChargerDefinitionAsset chargerDefinition,
            PrototypeArmoredDefinitionAsset armoredDefinition,
            PrototypeBossDefinitionAsset bossDefinition,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string nextSceneName,
            bool bossEnabled = false)
        {
            PrototypeSelfDestructDefinitionAsset selfDestructDefinition =
                LoadRequiredAsset<PrototypeSelfDestructDefinitionAsset>(
                    PrototypeContentValidator.PrototypeSelfDestructDefinitionPath);
            PrototypeThrowerDefinitionAsset throwerDefinition =
                LoadRequiredAsset<PrototypeThrowerDefinitionAsset>(
                    PrototypeContentValidator.PrototypeThrowerDefinitionPath);
            TestSandboxContext context = FindExactlyOne<TestSandboxContext>(scene);
            BombSwapInputReader inputReader = FindExactlyOne<BombSwapInputReader>(scene);
            PrototypePlayerController playerController = FindExactlyOne<PrototypePlayerController>(scene);
            PrototypeInputHarnessProbe harnessProbe = FindExactlyOne<PrototypeInputHarnessProbe>(scene);

            GameObject systems = inputReader.gameObject;
            PrototypePlayerAnimationPresenter playerAnimationPresenter =
                systems.GetComponent<PrototypePlayerAnimationPresenter>();
            if (playerAnimationPresenter == null)
            {
                playerAnimationPresenter =
                    systems.AddComponent<PrototypePlayerAnimationPresenter>();
            }
            PrototypeUserSettingsRuntime settingsRuntime =
                systems.GetComponent<PrototypeUserSettingsRuntime>();
            if (settingsRuntime == null)
            {
                settingsRuntime = systems.AddComponent<PrototypeUserSettingsRuntime>();
            }
            settingsRuntime.Configure(
                inputReader.InputActions,
                LoadRequiredAsset<AudioMixer>(PrototypeContentValidator.AudioMixerPath));
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
            PrototypeDestructibleWallPresenter destructibleWallPresenter =
                systems.GetComponent<PrototypeDestructibleWallPresenter>();
            if (destructibleWallPresenter == null)
            {
                destructibleWallPresenter =
                    systems.AddComponent<PrototypeDestructibleWallPresenter>();
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
            PrototypeChargerPresenter chargerPresenter =
                systems.GetComponent<PrototypeChargerPresenter>();
            if (chargerPresenter == null)
            {
                chargerPresenter = systems.AddComponent<PrototypeChargerPresenter>();
            }
            PrototypeArmoredPresenter armoredPresenter =
                systems.GetComponent<PrototypeArmoredPresenter>();
            if (armoredPresenter == null)
            {
                armoredPresenter = systems.AddComponent<PrototypeArmoredPresenter>();
            }
            PrototypeSelfDestructPresenter selfDestructPresenter =
                systems.GetComponent<PrototypeSelfDestructPresenter>();
            if (selfDestructPresenter == null)
            {
                selfDestructPresenter =
                    systems.AddComponent<PrototypeSelfDestructPresenter>();
            }
            PrototypeThrowerPresenter throwerPresenter =
                systems.GetComponent<PrototypeThrowerPresenter>();
            if (roomDefinition.HasThrower && throwerPresenter == null)
            {
                throwerPresenter = systems.AddComponent<PrototypeThrowerPresenter>();
            }
            else if (!roomDefinition.HasThrower && throwerPresenter != null)
            {
                UnityEngine.Object.DestroyImmediate(throwerPresenter);
                throwerPresenter = null;
            }
            PrototypeBossPresenter bossPresenter =
                systems.GetComponent<PrototypeBossPresenter>();
            if (bossPresenter == null)
            {
                bossPresenter = systems.AddComponent<PrototypeBossPresenter>();
            }
            PrototypeWeaponHud weaponHud = systems.GetComponent<PrototypeWeaponHud>();
            if (weaponHud == null)
            {
                weaponHud = systems.AddComponent<PrototypeWeaponHud>();
            }
            PrototypeHealthHud healthHud = systems.GetComponent<PrototypeHealthHud>();
            if (healthHud == null)
            {
                healthHud = systems.AddComponent<PrototypeHealthHud>();
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
            Transform chargerSpawn = context.GridRoot.Find("ChargerSpawn");
            if (room.ChargerSpawn.HasValue)
            {
                if (chargerSpawn == null)
                {
                    chargerSpawn = CreateChild("ChargerSpawn", context.GridRoot);
                }
            }
            else if (chargerSpawn != null)
            {
                UnityEngine.Object.DestroyImmediate(chargerSpawn.gameObject);
                chargerSpawn = null;
            }
            Transform armoredSpawn = context.GridRoot.Find("ArmoredSpawn");
            if (room.ArmoredSpawn.HasValue)
            {
                if (armoredSpawn == null)
                {
                    armoredSpawn = CreateChild("ArmoredSpawn", context.GridRoot);
                }
            }
            else if (armoredSpawn != null)
            {
                UnityEngine.Object.DestroyImmediate(armoredSpawn.gameObject);
                armoredSpawn = null;
            }
            Transform selfDestructSpawn = context.GridRoot.Find("SelfDestructSpawn");
            if (room.SelfDestructSpawn.HasValue)
            {
                if (selfDestructSpawn == null)
                {
                    selfDestructSpawn = CreateChild(
                        "SelfDestructSpawn",
                        context.GridRoot);
                }
            }
            else if (selfDestructSpawn != null)
            {
                UnityEngine.Object.DestroyImmediate(selfDestructSpawn.gameObject);
                selfDestructSpawn = null;
            }
            Transform throwerSpawn = context.GridRoot.Find("ThrowerSpawn");
            if (room.ThrowerSpawn.HasValue)
            {
                if (throwerSpawn == null)
                {
                    throwerSpawn = CreateChild("ThrowerSpawn", context.GridRoot);
                }
            }
            else if (throwerSpawn != null)
            {
                UnityEngine.Object.DestroyImmediate(throwerSpawn.gameObject);
                throwerSpawn = null;
            }
            SynchronizeInteriorObstacles(context.GridRoot, roomDefinition, room);
            Transform destructibleObstacles = SynchronizeDestructibleObstacles(
                context.GridRoot,
                roomDefinition,
                room);
            if (PrototypeContentValidator.IsDungeonPresentationScenePath(scene.path))
            {
                EnvironmentBlockVisualAuthoring.Synchronize(
                    context.GridRoot,
                    roomDefinition,
                    room);
            }
            var gridSpace = new GridSpace(context.GridRoot.position, roomDefinition.CellSize);
            context.PlayerSpawn.position = gridSpace.GridToWorld(room.PlayerSpawn);
            Transform player = SynchronizePlayerPresentation(
                context.GridRoot,
                context.PlayerPlaceholder,
                gridSpace.GridToWorld(room.PlayerSpawn));
            chaserSpawn.position = gridSpace.GridToWorld(room.ChaserSpawn);
            if (room.ChargerSpawn.HasValue)
            {
                chargerSpawn.position = gridSpace.GridToWorld(room.ChargerSpawn.Value);
            }
            if (room.ArmoredSpawn.HasValue)
            {
                armoredSpawn.position = gridSpace.GridToWorld(room.ArmoredSpawn.Value);
            }
            if (room.SelfDestructSpawn.HasValue)
            {
                selfDestructSpawn.position =
                    gridSpace.GridToWorld(room.SelfDestructSpawn.Value);
            }
            if (room.ThrowerSpawn.HasValue)
            {
                throwerSpawn.position = gridSpace.GridToWorld(room.ThrowerSpawn.Value);
            }

            Renderer playerRenderer =
                player.GetComponentInChildren<Renderer>();
            if (playerRenderer == null)
            {
                throw new InvalidOperationException(
                    "TestSandbox player placeholder requires a renderer.");
            }

            context.Configure(
                inputReader,
                context.GridRoot,
                context.PlayerSpawn,
                player,
                chaserSpawn,
                roomDefinition,
                chargerSpawn,
                armoredSpawn,
                selfDestructSpawn,
                throwerSpawn);
            gameSession.Configure(
                context,
                inputReader,
                bombLoadout,
                playerVitals,
                chaserDefinition,
                startingCharger: chargerDefinition,
                startingArmored: armoredDefinition,
                startingBossEnabled: bossEnabled,
                startingBoss: bossDefinition,
                startingSelfDestruct: selfDestructDefinition,
                startingThrower: throwerDefinition);
            Animator playerAnimator = player.GetComponentInChildren<Animator>(true);
            playerController.Configure(gameSession, player);
            playerAnimationPresenter.Configure(gameSession, playerAnimator);
            bombPresenter.Configure(gameSession, runtimePresentation);
            destructibleWallPresenter.Configure(gameSession, destructibleObstacles);
            healthPresenter.Configure(gameSession, playerRenderer);
            chaserPresenter.Configure(gameSession, runtimePresentation);
            chargerPresenter.Configure(gameSession, runtimePresentation);
            armoredPresenter.Configure(gameSession, runtimePresentation);
            selfDestructPresenter.Configure(gameSession, runtimePresentation);
            if (throwerPresenter != null)
            {
                throwerPresenter.Configure(gameSession, runtimePresentation);
            }
            bossPresenter.Configure(gameSession, runtimePresentation);
            SetSerializedObjectName(bossPresenter, nameof(PrototypeBossPresenter));
            weaponHud.Configure(gameSession);
            healthHud.Configure(gameSession);
            harnessProbe.Configure(inputReader, gameSession);
            roomAdvanceController.Configure(gameSession, nextSceneName);
            EditorUtility.SetDirty(context);
            EditorUtility.SetDirty(gameSession);
            EditorUtility.SetDirty(playerController);
            EditorUtility.SetDirty(playerAnimationPresenter);
            EditorUtility.SetDirty(bombPresenter);
            EditorUtility.SetDirty(destructibleWallPresenter);
            EditorUtility.SetDirty(healthPresenter);
            EditorUtility.SetDirty(chaserPresenter);
            EditorUtility.SetDirty(chargerPresenter);
            EditorUtility.SetDirty(armoredPresenter);
            EditorUtility.SetDirty(selfDestructPresenter);
            if (throwerPresenter != null)
            {
                EditorUtility.SetDirty(throwerPresenter);
            }
            EditorUtility.SetDirty(bossPresenter);
            EditorUtility.SetDirty(weaponHud);
            EditorUtility.SetDirty(harnessProbe);
            EditorUtility.SetDirty(roomAdvanceController);
        }

        private static Transform SynchronizePlayerPresentation(
            Transform gridRoot,
            Transform currentPlayer,
            Vector3 worldPosition)
        {
            if (gridRoot == null)
            {
                throw new ArgumentNullException(nameof(gridRoot));
            }

            GameObject playerPrefab = LoadRequiredAsset<GameObject>(
                PrototypeContentValidator.PlayerPrefabPath);
            GameObject source = currentPlayer != null
                ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                    currentPlayer.gameObject)
                : null;
            bool usesCanonicalPrefab = source != null && string.Equals(
                AssetDatabase.GetAssetPath(source),
                PrototypeContentValidator.PlayerPrefabPath,
                StringComparison.Ordinal);

            Transform player = currentPlayer;
            if (!usesCanonicalPrefab)
            {
                if (currentPlayer != null)
                {
                    UnityEngine.Object.DestroyImmediate(currentPlayer.gameObject);
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(
                    playerPrefab,
                    gridRoot) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to instantiate the canonical player prefab.");
                }

                player = instance.transform;
            }

            player.SetParent(gridRoot, true);
            player.position = worldPosition;
            player.rotation = Quaternion.identity;
            player.localScale = Vector3.one;

            Animator[] animators = player.GetComponentsInChildren<Animator>(true);
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            if (!player.CompareTag("Player") ||
                animators.Length != 1 ||
                renderers.Length == 0 ||
                player.GetComponentsInChildren<Collider>(true).Length != 0 ||
                player.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    "Canonical player instance has invalid tag, Animator, Renderer, or physics components.");
            }

            return player;
        }

        private static void SetSerializedObjectName(
            UnityEngine.Object target,
            string serializedName)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty nameProperty = serializedObject.FindProperty("m_Name");
            if (nameProperty == null)
            {
                throw new InvalidOperationException(
                    $"Unity object '{target}' does not expose a serialized name.");
            }
            nameProperty.stringValue = serializedName;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
            float obstacleY = PrototypeContentValidator.IsDungeonPresentationScenePath(
                gridRoot.gameObject.scene.path)
                ? 0f
                : 0.5f;
            bool matches = obstacles.childCount == expected.Count;
            for (int index = 0; index < obstacles.childCount; index++)
            {
                Transform obstacle = obstacles.GetChild(index);
                GridPosition cell = gridSpace.WorldToGrid(obstacle.position);
                if (!actual.Add(cell) || !expected.Contains(cell))
                {
                    matches = false;
                }
                if (!Mathf.Approximately(obstacle.localPosition.y, obstacleY))
                {
                    Vector3 localPosition = obstacle.localPosition;
                    localPosition.y = obstacleY;
                    obstacle.localPosition = localPosition;
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
                    new Vector3(wall.X * cellSize, obstacleY, wall.Z * cellSize),
                    new Vector3(0.9f * cellSize, 1f, 0.9f * cellSize),
                    wallMaterial,
                    true);
            }
        }

        private static Transform SynchronizeDestructibleObstacles(
            Transform gridRoot,
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            CombatRoomDefinition room)
        {
            Transform environment = gridRoot.Find("Environment");
            if (environment == null)
            {
                throw new InvalidOperationException("TestSandbox is missing its Environment root.");
            }
            Transform obstacles = environment.Find("DestructibleObstacles");
            if (obstacles == null)
            {
                obstacles = CreateChild("DestructibleObstacles", environment);
            }

            var expected = new HashSet<GridPosition>(room.DestructibleWalls);
            var actual = new HashSet<GridPosition>();
            var gridSpace = new GridSpace(gridRoot.position, roomDefinition.CellSize);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                PrototypeContentValidator.DestructibleWallMaterialPath);
            GameObject woodBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnvironmentBlockVisualAuthoring.WoodBoxPrefabPath);
            bool usesEnvironmentVisuals = string.Equals(
                gridRoot.gameObject.scene.path,
                PrototypeContentValidator.TestSandboxScenePath,
                StringComparison.Ordinal);
            if (material == null)
            {
                throw new InvalidOperationException("Prototype destructible-wall material is missing.");
            }
            bool matches = obstacles.childCount == expected.Count;
            for (int index = 0; index < obstacles.childCount; index++)
            {
                Transform obstacle = obstacles.GetChild(index);
                GridPosition cell = gridSpace.WorldToGrid(obstacle.position);
                if (!actual.Add(cell) || !expected.Contains(cell))
                {
                    matches = false;
                }
                Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>(true);
                Transform visual = obstacle.Find("Visual");
                bool matchesVisual = usesEnvironmentVisuals
                    ? renderers.Length > 0 && woodBoxPrefab != null && visual != null &&
                      PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject) ==
                          woodBoxPrefab
                    : renderers.Length == 4 &&
                      renderers.All(renderer => renderer.sharedMaterial == material);
                if (!matchesVisual ||
                    obstacle.GetComponentsInChildren<Collider>(true).Length != 0)
                {
                    matches = false;
                }
            }
            if (matches && actual.SetEquals(expected))
            {
                return obstacles;
            }

            for (int index = obstacles.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(obstacles.GetChild(index).gameObject);
            }

            for (int index = 0; index < room.DestructibleWalls.Count; index++)
            {
                CreateDestructibleWallVisual(
                    obstacles,
                    room.DestructibleWalls[index],
                    roomDefinition.CellSize,
                    material,
                    index);
            }

            return obstacles;
        }

        private static Transform CreateDestructibleWallVisual(
            Transform parent,
            GridPosition position,
            float cellSize,
            Material material,
            int index)
        {
            Transform root = CreateChild(
                $"Destructible_{index}_{position.X}_{position.Z}",
                parent);
            root.localPosition = new Vector3(
                position.X * cellSize,
                0f,
                position.Z * cellSize);

            float blockOffset = 0.225f * cellSize;
            float blockWidth = 0.4f * cellSize;
            Vector3[] offsets =
            {
                new Vector3(-blockOffset, 0.36f * cellSize, -blockOffset),
                new Vector3(blockOffset, 0.4f * cellSize, -blockOffset),
                new Vector3(-blockOffset, 0.4f * cellSize, blockOffset),
                new Vector3(blockOffset, 0.36f * cellSize, blockOffset),
            };
            for (int blockIndex = 0; blockIndex < offsets.Length; blockIndex++)
            {
                float height = blockIndex == 1 || blockIndex == 2 ? 0.8f : 0.72f;
                CreatePrimitive(
                    "BreakableBlock_" + blockIndex,
                    PrimitiveType.Cube,
                    root,
                    offsets[blockIndex],
                    new Vector3(blockWidth, height * cellSize, blockWidth),
                    material,
                    false);
            }

            return root;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.LobbyScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.DungeonStartScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.DungeonRewardScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.DungeonBossAnteScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.DungeonRecoveryScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.DungeonSecretScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.DungeonBossScenePath,
                    true),
                new EditorBuildSettingsScene(PrototypeContentValidator.TestSandboxScenePath, true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxThrowerScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxPillarsScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxArmorScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxGatesScenePath,
                    true),
                new EditorBuildSettingsScene(
                    PrototypeContentValidator.TestSandboxLanesScenePath,
                    false),
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

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Required prototype asset is missing: {assetPath}");
            }

            return asset;
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

        private static void DestroyAllInScene<T>(Scene scene) where T : Component
        {
            foreach (T component in FindAllInScene<T>(scene))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
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

        private static void CreateLobbyCamera(Transform parent)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(5.2f, 4.4f, -10.5f);
            cameraObject.transform.LookAt(new Vector3(3.8f, 1.3f, 0.2f));

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.045f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.AddComponent<AudioListener>();
        }

        private static LobbyUiBindings CreateLobbyUi(Transform parent)
        {
            var canvasObject = new GameObject(
                "LobbyCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            PrototypeUiFactory.ConfigureCanvasScaler(scaler);

            RectTransform shade = PrototypeUiFactory.CreateRect(
                "BackgroundShade",
                canvasObject.transform);
            SetRectAnchors(shade, Vector2.zero, Vector2.one);
            Image shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.color = new Color(0.01f, 0.018f, 0.03f, 0.48f);
            shadeImage.raycastTarget = false;

            RectTransform menuPanel = PrototypeUiFactory.CreateRect(
                "MainMenuPanel",
                shade);
            SetRectAnchors(
                menuPanel,
                new Vector2(0.06f, 0.1f),
                new Vector2(0.56f, 0.9f));
            Image menuPanelImage = menuPanel.gameObject.AddComponent<Image>();
            menuPanelImage.color = new Color(0.025f, 0.035f, 0.055f, 0.94f);
            menuPanelImage.raycastTarget = true;

            TextMeshProUGUI titleLabel = PrototypeUiFactory.CreateText(
                "GameTitleText",
                menuPanel,
                58f,
                TextAlignmentOptions.Center,
                FontStyles.Bold,
                TextWrappingModes.NoWrap);
            SetRectAnchors(
                titleLabel.rectTransform,
                new Vector2(0.06f, 0.68f),
                new Vector2(0.94f, 0.94f));
            titleLabel.text = PrototypeLobbyPresenter.GameTitle;
            titleLabel.color = new Color(1f, 0.78f, 0.26f, 1f);

            TextMeshProUGUI subtitle = PrototypeUiFactory.CreateText(
                "SubtitleText",
                menuPanel,
                24f,
                TextAlignmentOptions.Center,
                FontStyles.Normal,
                TextWrappingModes.Normal);
            SetRectAnchors(
                subtitle.rectTransform,
                new Vector2(0.08f, 0.58f),
                new Vector2(0.92f, 0.7f));
            subtitle.text = "미래의 폭발을 설계하는 룸 액션 로그라이트";
            subtitle.color = new Color(0.76f, 0.84f, 0.93f, 1f);

            TextMeshProUGUI versionLabel = PrototypeUiFactory.CreateText(
                "VersionText",
                menuPanel,
                18f,
                TextAlignmentOptions.BottomLeft);
            SetRectAnchors(
                versionLabel.rectTransform,
                new Vector2(0.04f, 0.04f),
                new Vector2(0.35f, 0.1f));
            versionLabel.text = PrototypeLobbyPresenter.FormatVersionText(
                PlayerSettings.bundleVersion);
            versionLabel.color = new Color(0.53f, 0.53f, 0.53f, 1f);

            Button startButton = PrototypeUiFactory.CreateButton(
                "StartRunButton",
                menuPanel,
                "게임 시작",
                34f,
                new Color(0.94f, 0.55f, 0.12f, 1f),
                new Color(1f, 0.72f, 0.22f, 1f));
            SetRectAnchors(
                startButton.GetComponent<RectTransform>(),
                new Vector2(0.17f, 0.39f),
                new Vector2(0.83f, 0.51f));

            Button controlsButton = PrototypeUiFactory.CreateButton(
                "ControlsButton",
                menuPanel,
                "설정",
                29f,
                new Color(0.12f, 0.2f, 0.3f, 1f),
                new Color(0.2f, 0.42f, 0.58f, 1f));
            SetRectAnchors(
                controlsButton.GetComponent<RectTransform>(),
                new Vector2(0.17f, 0.23f),
                new Vector2(0.83f, 0.35f));

            PrototypeSettingsPanelPresenter settingsPanel =
                PrototypeSettingsPanelFactory.Create(shade);

            var eventSystemObject = new GameObject(
                "LobbyEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(parent, false);

            return new LobbyUiBindings
            {
                Canvas = canvas,
                EventSystem = eventSystemObject.GetComponent<EventSystem>(),
                ControlsPanel = settingsPanel.gameObject,
                TitleLabel = titleLabel,
                VersionLabel = versionLabel,
                StartButton = startButton,
                ControlsButton = controlsButton,
                BackButton = settingsPanel.BackButton,
                SettingsPanel = settingsPanel,
            };
        }

        private static void SetRectAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
