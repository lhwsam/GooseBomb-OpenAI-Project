using System;
using System.Linq;
using BombSwap.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.UI
{
    public static class PrototypeInGameUiPrefabAuthoring
    {
        public const string PrefabFolder =
            "Assets/Game/Content/Resources/UI";
        public const string WeaponHudPrefabPath =
            PrefabFolder + "/PrototypeWeaponHudCanvas.prefab";
        public const string HealthHudPrefabPath =
            PrefabFolder + "/PrototypeHealthHudCanvas.prefab";
        public const string HealthHeartPrefabPath =
            PrefabFolder + "/PrototypePlayerHealthHeart.prefab";
        public const string MinimapPrefabPath =
            PrefabFolder + "/PrototypeDungeonMinimapCanvas.prefab";
        public const string MinimapRoomPrefabPath =
            PrefabFolder + "/PrototypeDungeonMinimapRoom.prefab";
        public const string MinimapConnectionPrefabPath =
            PrefabFolder + "/PrototypeDungeonMinimapConnection.prefab";
        public const string PausePrefabPath =
            PrefabFolder + "/PrototypePauseCanvas.prefab";
        public const string RunCompletionPrefabPath =
            PrefabFolder + "/PrototypeRunCompletionCanvas.prefab";
        public const string WeaponHudResourcePath =
            "UI/PrototypeWeaponHudCanvas";
        public const string HealthHudResourcePath =
            "UI/PrototypeHealthHudCanvas";
        public const string HealthHeartResourcePath =
            "UI/PrototypePlayerHealthHeart";
        public const string MinimapResourcePath =
            "UI/PrototypeDungeonMinimapCanvas";
        public const string MinimapRoomResourcePath =
            "UI/PrototypeDungeonMinimapRoom";
        public const string MinimapConnectionResourcePath =
            "UI/PrototypeDungeonMinimapConnection";
        public const string PauseResourcePath =
            "UI/PrototypePauseCanvas";
        public const string RunCompletionResourcePath =
            "UI/PrototypeRunCompletionCanvas";
        public const string CrossBombIconPath =
            "Assets/Game/Content/UI/Sprites/Common/Bomb/Bomb_Cross.png";
        public const string AreaBombIconPath =
            "Assets/Game/Content/UI/Sprites/Common/Bomb/Bomb_Area.png";
        public const string LineBombIconPath =
            "Assets/Game/Content/UI/Sprites/Common/Bomb/Bomb_Line.png";

        private const string MinimapBackgroundAtlasPath =
            "Assets/ThirdParty/UI/BlackandWhiteUI.png/BlackandWhiteUI.png";
        private const string MinimapIconFolder =
            "Assets/Game/Content/UI/Sprites/CC0/IconGodotNode/white";

        private static readonly Color PanelColor =
            new Color(0.02f, 0.025f, 0.04f, 0.86f);
        private static readonly Color InactiveSlotColor =
            new Color(0.12f, 0.15f, 0.2f, 0.86f);
        private static readonly Color PlayerHealthColor =
            new Color(0.92f, 0.18f, 0.16f, 1f);
        private static readonly Color BossHealthColor =
            new Color(0.84f, 0.24f, 0.62f, 1f);

        public sealed class PrefabSet
        {
            public PrefabSet(
                PrototypeWeaponHudView weaponHud,
                PrototypeHealthHudView healthHud,
                PrototypeDungeonMinimapView minimap,
                PrototypePauseView pause,
                PrototypeRunCompletionView runCompletion)
            {
                WeaponHud = weaponHud;
                HealthHud = healthHud;
                Minimap = minimap;
                Pause = pause;
                RunCompletion = runCompletion;
            }

            public PrototypeWeaponHudView WeaponHud { get; }

            public PrototypeHealthHudView HealthHud { get; }

            public PrototypeDungeonMinimapView Minimap { get; }

            public PrototypePauseView Pause { get; }

            public PrototypeRunCompletionView RunCompletion { get; }
        }

        [MenuItem("Bomb Swap/UI/Create Missing In-Game UI Prefabs and Wire Scenes")]
        private static void CreateAndWireFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before authoring in-game UI prefabs.");
            }

            PrefabSet prefabs = EnsurePrefabAssets();
            int changedSceneCount = WireAllGameScenes(prefabs);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "In-game UI prefab authoring complete. " +
                $"Five canvas views and three reusable child views are valid; " +
                $"{changedSceneCount} scenes changed.");
        }

        [MenuItem("Bomb Swap/UI/Repair Health HUD Boss Label References")]
        private static void RepairHealthHudBossLabelReferencesFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before repairing the health HUD prefab.");
            }

            EnsureAssetFolder(PrefabFolder);
            PrototypeHealthHeartView healthHeart = EnsurePrefab(
                HealthHeartPrefabPath,
                CreateHealthHeartPrefab);
            EnsureHealthHudPrefab(healthHeart);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Health HUD boss name, phase, and health value labels are wired.");
        }

        [MenuItem("Bomb Swap/UI/Repair Weapon HUD References")]
        private static void RepairWeaponHudReferencesFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Stop Play Mode before repairing the weapon HUD prefab.");
            }

            EnsureAssetFolder(PrefabFolder);
            EnsureWeaponHudPrefab(true);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Weapon HUD bomb icons, cooldown panels, empty states, and selections are wired.");
        }

        public static PrefabSet EnsurePrefabAssets()
        {
            EnsureAssetFolder(PrefabFolder);
            PrototypeWeaponHudView weapon = EnsureWeaponHudPrefab(false);
            PrototypeHealthHeartView healthHeart = EnsurePrefab(
                HealthHeartPrefabPath,
                CreateHealthHeartPrefab);
            PrototypeHealthHudView health = EnsureHealthHudPrefab(healthHeart);
            PrototypeDungeonMinimapRoomView minimapRoom =
                EnsureMinimapRoomPrefab();
            PrototypeDungeonMinimapConnectionView minimapConnection =
                EnsurePrefab(
                    MinimapConnectionPrefabPath,
                    CreateMinimapConnectionPrefab);
            PrototypeDungeonMinimapView minimap = EnsureMinimapPrefab(
                minimapRoom,
                minimapConnection);
            PrototypePauseView pause = EnsurePrefab(
                PausePrefabPath,
                CreatePausePrefab);
            PrototypeRunCompletionView completion = EnsurePrefab(
                RunCompletionPrefabPath,
                CreateRunCompletionPrefab);
            return new PrefabSet(
                weapon,
                health,
                minimap,
                pause,
                completion);
        }

        public static int WireAllGameScenes(PrefabSet prefabs)
        {
            if (prefabs == null)
            {
                throw new ArgumentNullException(nameof(prefabs));
            }

            string[] scenePaths = AssetDatabase
                .FindAssets("t:Scene", new[] { "Assets/Game/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            int changedSceneCount = 0;
            for (int index = 0; index < scenePaths.Length; index++)
            {
                if (WireScene(scenePaths[index], prefabs))
                {
                    changedSceneCount++;
                }
            }
            return changedSceneCount;
        }

        public static bool WireScene(string scenePath, PrefabSet prefabs)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException(
                    "A scene asset path is required.",
                    nameof(scenePath));
            }
            if (prefabs == null)
            {
                throw new ArgumentNullException(nameof(prefabs));
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForAuthoring = !scene.IsValid() || !scene.isLoaded;
            if (openedForAuthoring)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                bool changed = false;
                GameObject[] roots = scene.GetRootGameObjects();
                PrototypeWeaponHud[] weaponHuds = roots
                    .SelectMany(root =>
                        root.GetComponentsInChildren<PrototypeWeaponHud>(true))
                    .ToArray();
                for (int index = 0; index < weaponHuds.Length; index++)
                {
                    if (weaponHuds[index].ViewPrefab == prefabs.WeaponHud)
                    {
                        continue;
                    }
                    Undo.RecordObject(weaponHuds[index], "Bind Weapon HUD View Prefab");
                    weaponHuds[index].BindViewPrefab(prefabs.WeaponHud);
                    EditorUtility.SetDirty(weaponHuds[index]);
                    changed = true;
                }

                PrototypeHealthHud[] healthHuds = roots
                    .SelectMany(root =>
                        root.GetComponentsInChildren<PrototypeHealthHud>(true))
                    .ToArray();
                for (int index = 0; index < healthHuds.Length; index++)
                {
                    if (healthHuds[index].ViewPrefab == prefabs.HealthHud)
                    {
                        continue;
                    }
                    Undo.RecordObject(healthHuds[index], "Bind Health HUD View Prefab");
                    healthHuds[index].BindViewPrefab(prefabs.HealthHud);
                    EditorUtility.SetDirty(healthHuds[index]);
                    changed = true;
                }

                PrototypeDungeonMinimapPresenter[] minimaps = roots
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeDungeonMinimapPresenter>(true))
                    .ToArray();
                for (int index = 0; index < minimaps.Length; index++)
                {
                    if (minimaps[index].ViewPrefab == prefabs.Minimap)
                    {
                        continue;
                    }
                    Undo.RecordObject(minimaps[index], "Bind Minimap View Prefab");
                    minimaps[index].BindViewPrefab(prefabs.Minimap);
                    EditorUtility.SetDirty(minimaps[index]);
                    changed = true;
                }

                PrototypeGameSession[] sessions = roots
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeGameSession>(true))
                    .ToArray();
                for (int index = 0; index < sessions.Length; index++)
                {
                    if (sessions[index].PauseViewPrefab == prefabs.Pause)
                    {
                        continue;
                    }
                    Undo.RecordObject(sessions[index], "Bind Pause View Prefab");
                    sessions[index].BindPauseViewPrefab(prefabs.Pause);
                    EditorUtility.SetDirty(sessions[index]);
                    changed = true;
                }

                PrototypeRunCompletionPresenter[] completions = roots
                    .SelectMany(root => root.GetComponentsInChildren<
                        PrototypeRunCompletionPresenter>(true))
                    .ToArray();
                for (int index = 0; index < completions.Length; index++)
                {
                    if (completions[index].ViewPrefab == prefabs.RunCompletion)
                    {
                        continue;
                    }
                    Undo.RecordObject(
                        completions[index],
                        "Bind Run Completion View Prefab");
                    completions[index].BindViewPrefab(prefabs.RunCompletion);
                    EditorUtility.SetDirty(completions[index]);
                    changed = true;
                }

                if (!changed)
                {
                    return false;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (openedForAuthoring && !EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save in-game UI bindings in {scenePath}.");
                }
                return true;
            }
            finally
            {
                if (openedForAuthoring && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static PrototypeWeaponHudView CreateWeaponHudPrefab()
        {
            GameObject root = CreateCanvasRoot(
                "PrototypeWeaponHudCanvas",
                100,
                false);
            try
            {
                RectTransform panel = CreateRect("WeaponPanel", root.transform);
                SetRect(
                    panel,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(24f, 24f),
                    new Vector2(520f, 126f));
                Image panelBackground = panel.gameObject.AddComponent<Image>();
                panelBackground.color = new Color(0.02f, 0.025f, 0.04f, 0.82f);
                panelBackground.raycastTarget = false;

                var slotBombIcons = new Image[BombWeaponLoadout.SlotCount];
                var slotCooldownPanels = new GameObject[BombWeaponLoadout.SlotCount];
                var slotFills = new Image[BombWeaponLoadout.SlotCount];
                var cooldownLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
                var slotEmptyIndicators = new GameObject[BombWeaponLoadout.SlotCount];
                var slotSelections = new GameObject[BombWeaponLoadout.SlotCount];
                for (int slotIndex = 0;
                     slotIndex < BombWeaponLoadout.SlotCount;
                     slotIndex++)
                {
                    CreateWeaponSlot(
                        panel,
                        slotIndex,
                        out slotBombIcons[slotIndex],
                        out slotCooldownPanels[slotIndex],
                        out slotFills[slotIndex],
                        out cooldownLabels[slotIndex],
                        out slotEmptyIndicators[slotIndex],
                        out slotSelections[slotIndex]);
                }

                PrototypeWeaponHudView view =
                    root.AddComponent<PrototypeWeaponHudView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    slotBombIcons,
                    slotCooldownPanels,
                    slotFills,
                    cooldownLabels,
                    slotEmptyIndicators,
                    slotSelections,
                    LoadRequiredSprite(CrossBombIconPath),
                    LoadRequiredSprite(AreaBombIconPath),
                    LoadRequiredSprite(LineBombIconPath));
                return SavePrefabView(root, WeaponHudPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeHealthHeartView CreateHealthHeartPrefab()
        {
            GameObject root = new GameObject(
                "PrototypePlayerHealthHeart",
                typeof(RectTransform));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(45f, 40.5f);

                Image emptyVisual = CreateHeartVisual(
                    "Empty",
                    root.transform,
                    new Color(0.35f, 0.12f, 0.12f, 0.7f));
                Image fullVisual = CreateHeartVisual(
                    "Full",
                    root.transform,
                    PlayerHealthColor);

                PrototypeHealthHeartView view =
                    root.AddComponent<PrototypeHealthHeartView>();
                view.BindAuthoredView(fullVisual, emptyVisual);
                return SavePrefabView(root, HealthHeartPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeHealthHudView CreateHealthHudPrefab(
            PrototypeHealthHeartView healthHeartPrefab)
        {
            if (healthHeartPrefab == null)
            {
                throw new ArgumentNullException(nameof(healthHeartPrefab));
            }

            GameObject root = CreateCanvasRoot(
                "PrototypeHealthHudCanvas",
                110,
                false);
            try
            {
                RectTransform playerPanel = CreatePanel(
                    "PlayerHealthPanel",
                    root.transform,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(24f, -24f),
                    new Vector2(310f, 78f));
                RectTransform playerHeartContainer = CreateRect(
                    "PlayerHearts",
                    playerPanel);
                SetRect(
                    playerHeartContainer,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(14f, -18.75f),
                    new Vector2(282f, 40.5f));
                HorizontalLayoutGroup heartLayout =
                    playerHeartContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                heartLayout.childAlignment = TextAnchor.MiddleLeft;
                heartLayout.spacing = 8f;
                heartLayout.childControlWidth = false;
                heartLayout.childControlHeight = false;
                heartLayout.childForceExpandWidth = false;
                heartLayout.childForceExpandHeight = false;
                CreateAuthoredHeartInstances(
                    playerHeartContainer,
                    healthHeartPrefab,
                    5);

                RectTransform rewardPanel = CreatePanel(
                    "CombatRewardPanel",
                    root.transform,
                    Vector2.one,
                    Vector2.one,
                    Vector2.one,
                    new Vector2(-24f, -24f),
                    new Vector2(250f, 58f));
                TextMeshProUGUI rewardLabel = CreateHealthLabel(
                    rewardPanel,
                    21f,
                    "0");
                rewardLabel.name = "TokenValueLabel";

                RectTransform bossPanel = CreatePanel(
                    "BossHealthPanel",
                    root.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -24f),
                    new Vector2(560f, 82f));
                Image bossFill = CreateHealthBar(
                    bossPanel,
                    BossHealthColor);
                bossFill.name = "BossHealthFill";
                TextMeshProUGUI bossNameLabel = CreateBossStatusLabel(
                    "BossNameLabel",
                    bossPanel,
                    28f,
                    "BOSS",
                    new Vector2(0f, 26f),
                    new Vector2(300f, 38f));
                TextMeshProUGUI bossPhaseLabel = CreateBossStatusLabel(
                    "BossPhaseLabel",
                    bossPanel,
                    20f,
                    "PHASE 1",
                    new Vector2(-110f, -3f),
                    new Vector2(100f, 30f));
                TextMeshProUGUI bossHealthValueLabel = CreateBossStatusLabel(
                    "BossHealthValueLabel",
                    bossPanel,
                    20f,
                    "10 / 10",
                    new Vector2(110f, -3f),
                    new Vector2(100f, 30f));

                PrototypeHealthHudView view =
                    root.AddComponent<PrototypeHealthHudView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    bossPanel.gameObject,
                    playerHeartContainer,
                    healthHeartPrefab,
                    bossFill,
                    bossNameLabel,
                    bossPhaseLabel,
                    bossHealthValueLabel,
                    rewardLabel);
                return SavePrefabView(root, HealthHudPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeDungeonMinimapRoomView CreateMinimapRoomPrefab()
        {
            GameObject root = new GameObject(
                "PrototypeDungeonMinimapRoom",
                typeof(RectTransform));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(26f, 26f);
                Image roomImage = root.AddComponent<Image>();
                roomImage.color = Color.white;
                roomImage.raycastTarget = false;

                Image roomIconImage = CreateMinimapRoomIcon(root.transform);

                PrototypeDungeonMinimapRoomView view =
                    root.AddComponent<PrototypeDungeonMinimapRoomView>();
                BindMinimapRoomView(view, roomImage, roomIconImage);
                return SavePrefabView(root, MinimapRoomPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeDungeonMinimapConnectionView
            CreateMinimapConnectionPrefab()
        {
            GameObject root = new GameObject(
                "PrototypeDungeonMinimapConnection",
                typeof(RectTransform));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(38f, 5f);
                Image connectionImage = root.AddComponent<Image>();
                connectionImage.color = Color.white;
                connectionImage.raycastTarget = false;

                PrototypeDungeonMinimapConnectionView view =
                    root.AddComponent<PrototypeDungeonMinimapConnectionView>();
                view.BindAuthoredView(connectionImage);
                return SavePrefabView(
                    root,
                    MinimapConnectionPrefabPath,
                    view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeDungeonMinimapView CreateMinimapPrefab(
            PrototypeDungeonMinimapRoomView roomViewPrefab,
            PrototypeDungeonMinimapConnectionView connectionViewPrefab)
        {
            GameObject root = CreateCanvasRoot(
                "PrototypeDungeonMinimapCanvas",
                109,
                false);
            try
            {
                RectTransform panel = CreatePanel(
                    "MinimapPanel",
                    root.transform,
                    Vector2.one,
                    Vector2.one,
                    Vector2.one,
                    PrototypeDungeonMinimapPresenter.DefaultPanelPosition,
                    PrototypeDungeonMinimapPresenter.DefaultPanelSize);

                TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                    "Title",
                    panel,
                    18f,
                    TextAlignmentOptions.MidlineLeft,
                    FontStyles.Bold);
                title.rectTransform.anchorMin = new Vector2(0f, 1f);
                title.rectTransform.anchorMax = Vector2.one;
                title.rectTransform.pivot = new Vector2(0.5f, 1f);
                title.rectTransform.offsetMin = new Vector2(12f, -38f);
                title.rectTransform.offsetMax = new Vector2(-12f, -8f);
                title.text = "DUNGEON MAP";

                TextMeshProUGUI legend = PrototypeUiFactory.CreateText(
                    "Legend",
                    panel,
                    13f,
                    TextAlignmentOptions.Center);
                legend.rectTransform.anchorMin = Vector2.zero;
                legend.rectTransform.anchorMax = new Vector2(1f, 0f);
                legend.rectTransform.pivot = new Vector2(0.5f, 0f);
                legend.rectTransform.offsetMin = new Vector2(12f, 8f);
                legend.rectTransform.offsetMax = new Vector2(-12f, 34f);
                legend.text = "C CURRENT   V VISITED   ? DISCOVERED";
                legend.color = new Color(0.78f, 0.82f, 0.9f, 1f);

                RectTransform mapRoot = CreateRect("Map", panel);
                mapRoot.anchorMin = Vector2.zero;
                mapRoot.anchorMax = Vector2.one;
                mapRoot.offsetMin = new Vector2(14f, 40f);
                mapRoot.offsetMax = new Vector2(-14f, -44f);

                PrototypeDungeonMinimapView view =
                    root.AddComponent<PrototypeDungeonMinimapView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    mapRoot,
                    roomViewPrefab,
                    connectionViewPrefab);
                return SavePrefabView(root, MinimapPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeRunCompletionView CreateRunCompletionPrefab()
        {
            GameObject root = CreateCanvasRoot(
                "PrototypeRunCompletionCanvas",
                300,
                true);
            try
            {
                RectTransform backdrop = CreateRect("Backdrop", root.transform);
                SetAnchors(backdrop, Vector2.zero, Vector2.one);
                Image backdropImage = backdrop.gameObject.AddComponent<Image>();
                backdropImage.color = new Color(0.015f, 0.02f, 0.04f, 1f);
                backdropImage.raycastTarget = false;

                TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                    "Title",
                    backdrop,
                    52f,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold,
                    TextWrappingModes.Normal);
                SetAnchors(
                    title.rectTransform,
                    new Vector2(0.1f, 0.48f),
                    new Vector2(0.9f, 0.68f));
                title.text = "RUN FAILED";
                title.color = new Color(1f, 0.32f, 0.26f, 1f);

                TextMeshProUGUI failureCause = PrototypeUiFactory.CreateText(
                    "FailureCause",
                    backdrop,
                    24f,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold,
                    TextWrappingModes.Normal);
                SetAnchors(
                    failureCause.rectTransform,
                    new Vector2(0.1f, 0.42f),
                    new Vector2(0.9f, 0.53f));
                failureCause.text = "CAUSE: BOMB EXPLOSION";
                failureCause.color = new Color(1f, 0.72f, 0.42f, 1f);

                Button restart = PrototypeUiFactory.CreateButton(
                    "RestartButton",
                    backdrop,
                    "다시 시작",
                    27f,
                    new Color(0.12f, 0.42f, 0.68f, 1f),
                    new Color(0.2f, 0.66f, 0.92f, 1f));
                SetAnchors(
                    restart.GetComponent<RectTransform>(),
                    new Vector2(0.27f, 0.27f),
                    new Vector2(0.49f, 0.38f));

                Button lobby = PrototypeUiFactory.CreateButton(
                    "LobbyButton",
                    backdrop,
                    "로비로 돌아가기",
                    27f,
                    new Color(0.18f, 0.21f, 0.28f, 1f),
                    new Color(0.34f, 0.4f, 0.52f, 1f));
                SetAnchors(
                    lobby.GetComponent<RectTransform>(),
                    new Vector2(0.51f, 0.27f),
                    new Vector2(0.73f, 0.38f));

                PrototypeRunCompletionView view =
                    root.AddComponent<PrototypeRunCompletionView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    title,
                    failureCause,
                    restart,
                    lobby);
                return SavePrefabView(root, RunCompletionPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypePauseView CreatePausePrefab()
        {
            GameObject root = CreateCanvasRoot(
                "PrototypePauseCanvas",
                250,
                true);
            try
            {
                RectTransform backdrop = CreateRect("Backdrop", root.transform);
                SetAnchors(backdrop, Vector2.zero, Vector2.one);
                Image backdropImage = backdrop.gameObject.AddComponent<Image>();
                backdropImage.color = new Color(0.015f, 0.02f, 0.04f, 0.76f);

                RectTransform menu = CreateRect("PauseMenu", backdrop);
                SetAnchors(
                    menu,
                    new Vector2(0.22f, 0.2f),
                    new Vector2(0.78f, 0.8f));

                TextMeshProUGUI title = PrototypeUiFactory.CreateText(
                    "Title",
                    menu,
                    56f,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold,
                    TextWrappingModes.Normal);
                SetAnchors(
                    title.rectTransform,
                    new Vector2(0.05f, 0.65f),
                    new Vector2(0.95f, 0.92f));
                title.text = "PAUSED";
                title.color = new Color(0.35f, 0.82f, 1f, 1f);
                PrototypePauseTitleWave titleWave =
                    title.gameObject.AddComponent<PrototypePauseTitleWave>();
                titleWave.Configure(title);

                Button resume = CreateButton(
                    "ResumeButton",
                    menu,
                    "게임 계속",
                    27f,
                    new Vector2(0.18f, 0.42f),
                    new Vector2(0.82f, 0.58f));
                Button settings = CreateButton(
                    "SettingsButton",
                    menu,
                    "설정",
                    27f,
                    new Vector2(0.18f, 0.22f),
                    new Vector2(0.82f, 0.38f));

                PrototypeSettingsPanelPresenter settingsPanel =
                    PrototypeSettingsPanelFactory.Create(
                        backdrop,
                        "PauseSettingsPanel");
                PrototypePauseView view = root.AddComponent<PrototypePauseView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    menu.gameObject,
                    null,
                    resume,
                    settings,
                    settingsPanel);
                return SavePrefabView(root, PausePrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static void CreateWeaponSlot(
            RectTransform panel,
            int slotIndex,
            out Image bombIcon,
            out GameObject cooldownPanel,
            out Image fill,
            out TextMeshProUGUI cooldownLabel,
            out GameObject emptyIndicator,
            out GameObject selection)
        {
            RectTransform slot = CreateRect("Slot" + (slotIndex + 1), panel);
            SetRect(
                slot,
                new Vector2(slotIndex * 0.5f, 1f),
                new Vector2((slotIndex + 1) * 0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(slotIndex == 0 ? 4f : -4f, -8f),
                new Vector2(-16f, 82f));
            Image background = slot.gameObject.AddComponent<Image>();
            background.color = InactiveSlotColor;
            background.raycastTarget = false;

            RectTransform iconRect = CreateRect("BombIcon", slot);
            SetAnchors(iconRect, Vector2.zero, Vector2.one);
            bombIcon = iconRect.gameObject.AddComponent<Image>();
            bombIcon.preserveAspect = true;
            bombIcon.raycastTarget = false;

            RectTransform cooldownPanelRect = CreateRect("CoolDownPanel", slot);
            SetAnchors(cooldownPanelRect, Vector2.zero, Vector2.one);
            cooldownPanel = cooldownPanelRect.gameObject;

            RectTransform fillRect = CreateRect("CooldownBar", cooldownPanelRect);
            SetAnchors(fillRect, Vector2.zero, Vector2.one);
            fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = new Color(0f, 0f, 0f, 0.7f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            cooldownLabel = PrototypeUiFactory.CreateText(
                "Cooldown",
                cooldownPanelRect,
                24f,
                TextAlignmentOptions.Center);
            SetAnchors(cooldownLabel.rectTransform, Vector2.zero, Vector2.one);
            cooldownLabel.text = "1.0s";
            cooldownLabel.color = Color.white;

            TextMeshProUGUI emptyLabel = PrototypeUiFactory.CreateText(
                "Empty",
                slot,
                22f,
                TextAlignmentOptions.Center);
            SetAnchors(emptyLabel.rectTransform, Vector2.zero, Vector2.one);
            emptyLabel.text = "EMPTY";
            emptyLabel.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            emptyIndicator = emptyLabel.gameObject;

            RectTransform selectionRect = CreateRect("Selection", slot);
            SetAnchors(selectionRect, Vector2.zero, Vector2.one);
            Image selectionImage = selectionRect.gameObject.AddComponent<Image>();
            selectionImage.color = new Color(1f, 1f, 1f, 0.18f);
            selectionImage.raycastTarget = false;
            selection = selectionRect.gameObject;

            TextMeshProUGUI keyLabel = PrototypeUiFactory.CreateText(
                "KeyIcon",
                slot,
                16f,
                TextAlignmentOptions.BottomRight,
                FontStyles.Bold);
            SetAnchors(keyLabel.rectTransform, Vector2.zero, Vector2.one);
            keyLabel.text = slotIndex == 0 ? "Z" : "X";
            keyLabel.color = Color.white;

            cooldownPanel.SetActive(false);
            emptyIndicator.SetActive(slotIndex != 0);
            selection.SetActive(slotIndex == 0);
        }

        private static RectTransform CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform panel = CreateRect(objectName, parent);
            SetRect(
                panel,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                size);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;
            return panel;
        }

        private static TextMeshProUGUI CreateHealthLabel(
            RectTransform panel,
            float fontSize,
            string text)
        {
            TextMeshProUGUI label = PrototypeUiFactory.CreateText(
                "Label",
                panel,
                fontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            label.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(14f, 0f);
            label.rectTransform.offsetMax = new Vector2(-14f, -2f);
            label.text = text;
            return label;
        }

        private static Image CreateHealthBar(
            RectTransform panel,
            Color fillColor)
        {
            RectTransform backgroundRect = CreateRect("Bar", panel);
            backgroundRect.anchorMin = new Vector2(0f, 0f);
            backgroundRect.anchorMax = new Vector2(1f, 0.32f);
            backgroundRect.offsetMin = new Vector2(14f, 12f);
            backgroundRect.offsetMax = new Vector2(-14f, 0f);
            Image background = backgroundRect.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.08f, 1f);
            background.raycastTarget = false;

            RectTransform fillRect = CreateRect("Fill", backgroundRect);
            SetAnchors(fillRect, Vector2.zero, Vector2.one);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
        }

        private static TextMeshProUGUI CreateBossStatusLabel(
            string objectName,
            Transform parent,
            float fontSize,
            string text,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            TextMeshProUGUI label = PrototypeUiFactory.CreateText(
                objectName,
                parent,
                fontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            SetRect(
                label.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                size);
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateHeartVisual(
            string objectName,
            Transform parent,
            Color color)
        {
            RectTransform rect = CreateRect(objectName, parent);
            SetAnchors(rect, Vector2.zero, Vector2.one);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateAuthoredHeartInstances(
            Transform parent,
            PrototypeHealthHeartView heartPrefab,
            int count)
        {
            for (int index = 0; index < count; index++)
            {
                GameObject heart = (GameObject)PrefabUtility.InstantiatePrefab(
                    heartPrefab.gameObject,
                    parent);
                heart.name = $"PlayerHeart{index + 1:00}";
            }
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            float fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Button button = PrototypeUiFactory.CreateButton(
                objectName,
                parent,
                label,
                fontSize,
                new Color(0.1f, 0.17f, 0.25f, 1f),
                new Color(0.2f, 0.46f, 0.65f, 1f));
            SetAnchors(button.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return button;
        }

        private static GameObject CreateCanvasRoot(
            string objectName,
            int sortingOrder,
            bool addRaycaster)
        {
            GameObject root = addRaycaster
                ? new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster))
                : new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            PrototypeUiFactory.ConfigureCanvasScaler(
                root.GetComponent<CanvasScaler>());
            return root;
        }

        private static RectTransform CreateRect(
            string objectName,
            Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static PrototypeWeaponHudView EnsureWeaponHudPrefab(
            bool forceRebind)
        {
            Sprite crossIcon = LoadRequiredSprite(CrossBombIconPath);
            Sprite areaIcon = LoadRequiredSprite(AreaBombIconPath);
            Sprite lineIcon = LoadRequiredSprite(LineBombIconPath);
            PrototypeWeaponHudView existing =
                AssetDatabase.LoadAssetAtPath<PrototypeWeaponHudView>(
                    WeaponHudPrefabPath);
            if (existing == null)
            {
                return EnsurePrefab(
                    WeaponHudPrefabPath,
                    CreateWeaponHudPrefab);
            }

            bool hasCanonicalIcons =
                existing.HasRequiredReferences &&
                existing.GetBombIcon(BombExplosionShape.Cross) == crossIcon &&
                existing.GetBombIcon(BombExplosionShape.SquareArea) == areaIcon &&
                existing.GetBombIcon(BombExplosionShape.ForwardLine) == lineIcon;
            if (forceRebind || !hasCanonicalIcons)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(
                    WeaponHudPrefabPath);
                try
                {
                    PrototypeWeaponHudView view =
                        root.GetComponent<PrototypeWeaponHudView>();
                    Canvas authoredCanvas = root.GetComponent<Canvas>();
                    if (view == null || authoredCanvas == null)
                    {
                        throw new InvalidOperationException(
                            "Existing weapon HUD prefab is missing its root View or Canvas.");
                    }

                    var slotBombIcons = new Image[BombWeaponLoadout.SlotCount];
                    var slotCooldownPanels =
                        new GameObject[BombWeaponLoadout.SlotCount];
                    var slotCooldownFills = new Image[BombWeaponLoadout.SlotCount];
                    var slotCooldownLabels =
                        new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
                    var slotEmptyIndicators =
                        new GameObject[BombWeaponLoadout.SlotCount];
                    var slotSelections =
                        new GameObject[BombWeaponLoadout.SlotCount];
                    for (int slotIndex = 0;
                         slotIndex < BombWeaponLoadout.SlotCount;
                         slotIndex++)
                    {
                        Transform slot = FindRequiredDescendant(
                            root.transform,
                            "Slot" + (slotIndex + 1));
                        slotBombIcons[slotIndex] =
                            FindRequiredComponent<Image>(slot, "BombIcon");
                        slotCooldownPanels[slotIndex] =
                            FindRequiredDescendant(slot, "CoolDownPanel").gameObject;
                        slotCooldownFills[slotIndex] =
                            FindRequiredComponent<Image>(slot, "CooldownBar");
                        slotCooldownLabels[slotIndex] =
                            FindRequiredComponent<TextMeshProUGUI>(
                                slot,
                                "Cooldown");
                        slotEmptyIndicators[slotIndex] =
                            FindRequiredDescendant(slot, "Empty").gameObject;
                        slotSelections[slotIndex] =
                            FindRequiredDescendant(slot, "Selection").gameObject;
                    }

                    view.BindAuthoredView(
                        authoredCanvas,
                        slotBombIcons,
                        slotCooldownPanels,
                        slotCooldownFills,
                        slotCooldownLabels,
                        slotEmptyIndicators,
                        slotSelections,
                        crossIcon,
                        areaIcon,
                        lineIcon);
                    EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(root, WeaponHudPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                AssetDatabase.ImportAsset(
                    WeaponHudPrefabPath,
                    ImportAssetOptions.ForceUpdate);
                existing = AssetDatabase.LoadAssetAtPath<PrototypeWeaponHudView>(
                    WeaponHudPrefabPath);
            }

            ValidatePrefabView(existing, WeaponHudPrefabPath);
            return existing;
        }

        private static Sprite LoadRequiredSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Required weapon HUD sprite is missing or not imported as a Sprite: {assetPath}");
            }
            return sprite;
        }

        private static Transform FindRequiredDescendant(
            Transform parent,
            string objectName)
        {
            Transform[] matches = parent
                .GetComponentsInChildren<Transform>(true)
                .Where(candidate =>
                    candidate != parent &&
                    string.Equals(
                        candidate.name,
                        objectName,
                        StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Weapon HUD expects one {objectName} below {parent.name}; found {matches.Length}.");
            }
            return matches[0];
        }

        private static T FindRequiredComponent<T>(
            Transform parent,
            string objectName)
            where T : Component
        {
            Transform target = FindRequiredDescendant(parent, objectName);
            T component = target.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Weapon HUD object {objectName} requires {typeof(T).Name}.");
            }
            return component;
        }

        private static PrototypeDungeonMinimapView EnsureMinimapPrefab(
            PrototypeDungeonMinimapRoomView roomViewPrefab,
            PrototypeDungeonMinimapConnectionView connectionViewPrefab)
        {
            PrototypeDungeonMinimapView existing =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonMinimapView>(
                    MinimapPrefabPath);
            if (existing == null)
            {
                return EnsurePrefab(
                    MinimapPrefabPath,
                    () => CreateMinimapPrefab(
                        roomViewPrefab,
                        connectionViewPrefab));
            }

            if (existing.RoomViewPrefab != roomViewPrefab ||
                existing.ConnectionViewPrefab != connectionViewPrefab)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(
                    MinimapPrefabPath);
                try
                {
                    PrototypeDungeonMinimapView view =
                        root.GetComponent<PrototypeDungeonMinimapView>();
                    if (view == null || view.Canvas == null ||
                        view.MapRoot == null)
                    {
                        throw new InvalidOperationException(
                            "Existing minimap prefab is missing its authored root references.");
                    }

                    view.BindAuthoredView(
                        view.Canvas,
                        view.MapRoot,
                        roomViewPrefab,
                        connectionViewPrefab);
                    PrefabUtility.SaveAsPrefabAsset(root, MinimapPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                AssetDatabase.ImportAsset(
                    MinimapPrefabPath,
                    ImportAssetOptions.ForceUpdate);
                existing = AssetDatabase.LoadAssetAtPath<
                    PrototypeDungeonMinimapView>(MinimapPrefabPath);
            }

            ValidatePrefabView(existing, MinimapPrefabPath);
            return existing;
        }

        private static PrototypeHealthHudView EnsureHealthHudPrefab(
            PrototypeHealthHeartView healthHeartPrefab)
        {
            PrototypeHealthHudView existing =
                AssetDatabase.LoadAssetAtPath<PrototypeHealthHudView>(
                    HealthHudPrefabPath);
            if (existing == null)
            {
                return EnsurePrefab(
                    HealthHudPrefabPath,
                    () => CreateHealthHudPrefab(healthHeartPrefab));
            }

            bool usesCanonicalNames =
                existing.BossNameLabel != null &&
                existing.BossPhaseLabel != null &&
                existing.BossHealthValueLabel != null &&
                string.Equals(
                    existing.BossNameLabel.gameObject.name,
                    "BossNameLabel",
                    StringComparison.Ordinal) &&
                string.Equals(
                    existing.BossPhaseLabel.gameObject.name,
                    "BossPhaseLabel",
                    StringComparison.Ordinal) &&
                string.Equals(
                    existing.BossHealthValueLabel.gameObject.name,
                    "BossHealthValueLabel",
                    StringComparison.Ordinal);
            if (!existing.HasRequiredReferences || !usesCanonicalNames)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(
                    HealthHudPrefabPath);
                try
                {
                    PrototypeHealthHudView view =
                        root.GetComponent<PrototypeHealthHudView>();
                    if (view == null || view.Canvas == null ||
                        view.BossPanel == null ||
                        view.PlayerHeartContainer == null ||
                        view.BossHealthFill == null ||
                        view.CombatRewardLabel == null)
                    {
                        throw new InvalidOperationException(
                            "Existing health HUD prefab is missing its authored root references.");
                    }

                    TextMeshProUGUI[] bossLabels = view.BossPanel
                        .GetComponentsInChildren<TextMeshProUGUI>(true);
                    TextMeshProUGUI phaseLabel = view.BossPhaseLabel ??
                        FindUniqueBossLabel(
                            bossLabels,
                            label => label.text.Trim().StartsWith(
                                "PHASE ",
                                StringComparison.OrdinalIgnoreCase),
                            "phase");
                    TextMeshProUGUI healthValueLabel =
                        view.BossHealthValueLabel ??
                        FindUniqueBossLabel(
                            bossLabels,
                            label => label.text.Contains('/'),
                            "health value");
                    TextMeshProUGUI nameLabel = view.BossNameLabel ??
                        FindUniqueBossLabel(
                            bossLabels,
                            label => label != phaseLabel &&
                                label != healthValueLabel,
                            "name");
                    if (nameLabel == phaseLabel ||
                        nameLabel == healthValueLabel ||
                        phaseLabel == healthValueLabel)
                    {
                        throw new InvalidOperationException(
                            "Health HUD boss labels must be three distinct TMP objects.");
                    }

                    nameLabel.gameObject.name = "BossNameLabel";
                    phaseLabel.gameObject.name = "BossPhaseLabel";
                    healthValueLabel.gameObject.name = "BossHealthValueLabel";
                    view.BindAuthoredView(
                        view.Canvas,
                        view.BossPanel,
                        view.PlayerHeartContainer,
                        healthHeartPrefab,
                        view.BossHealthFill,
                        nameLabel,
                        phaseLabel,
                        healthValueLabel,
                        view.CombatRewardLabel);
                    PrefabUtility.SaveAsPrefabAsset(root, HealthHudPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                AssetDatabase.ImportAsset(
                    HealthHudPrefabPath,
                    ImportAssetOptions.ForceUpdate);
                existing = AssetDatabase.LoadAssetAtPath<PrototypeHealthHudView>(
                    HealthHudPrefabPath);
            }

            ValidatePrefabView(existing, HealthHudPrefabPath);
            return existing;
        }

        private static TextMeshProUGUI FindUniqueBossLabel(
            TextMeshProUGUI[] labels,
            Func<TextMeshProUGUI, bool> predicate,
            string role)
        {
            TextMeshProUGUI[] matches = labels.Where(predicate).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one authored boss {role} label, found {matches.Length}.");
            }
            return matches[0];
        }

        private static PrototypeDungeonMinimapRoomView EnsureMinimapRoomPrefab()
        {
            PrototypeDungeonMinimapRoomView existing =
                AssetDatabase.LoadAssetAtPath<PrototypeDungeonMinimapRoomView>(
                    MinimapRoomPrefabPath);
            if (existing == null)
            {
                return EnsurePrefab(
                    MinimapRoomPrefabPath,
                    CreateMinimapRoomPrefab);
            }

            if (!existing.HasRequiredReferences)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(
                    MinimapRoomPrefabPath);
                try
                {
                    PrototypeDungeonMinimapRoomView view =
                        root.GetComponent<PrototypeDungeonMinimapRoomView>();
                    Image roomImage = view != null
                        ? view.RoomImage
                        : null;
                    if (view == null || roomImage == null)
                    {
                        throw new InvalidOperationException(
                            "Existing minimap room prefab is missing its authored root view or image.");
                    }

                    Image roomIconImage = root.transform
                        .Cast<Transform>()
                        .Where(child => string.Equals(
                            child.name,
                            "Icon",
                            StringComparison.Ordinal))
                        .Select(child => child.GetComponent<Image>())
                        .FirstOrDefault(image => image != null);
                    if (roomIconImage == null)
                    {
                        roomIconImage = CreateMinimapRoomIcon(root.transform);
                    }

                    BindMinimapRoomView(view, roomImage, roomIconImage);
                    PrefabUtility.SaveAsPrefabAsset(root, MinimapRoomPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                AssetDatabase.ImportAsset(
                    MinimapRoomPrefabPath,
                    ImportAssetOptions.ForceUpdate);
                existing = AssetDatabase.LoadAssetAtPath<
                    PrototypeDungeonMinimapRoomView>(MinimapRoomPrefabPath);
            }

            ValidatePrefabView(existing, MinimapRoomPrefabPath);
            return existing;
        }

        private static Image CreateMinimapRoomIcon(Transform parent)
        {
            RectTransform iconRect = CreateRect("Icon", parent);
            SetRect(
                iconRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(10f, 10f));
            Image image = iconRect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void BindMinimapRoomView(
            PrototypeDungeonMinimapRoomView view,
            Image roomImage,
            Image roomIconImage)
        {
            Sprite authoredCurrentBackground = roomImage.sprite;
            Sprite currentBackground = authoredCurrentBackground != null
                ? authoredCurrentBackground
                : LoadSpriteSubAsset(
                    MinimapBackgroundAtlasPath,
                    "BlackandWhiteUI_16");

            view.BindAuthoredView(
                roomImage,
                roomIconImage,
                currentBackground,
                LoadSpriteSubAsset(
                    MinimapBackgroundAtlasPath,
                    "BlackandWhiteUI_3"),
                LoadSpriteAsset("icon_interrogation.png"),
                LoadSpriteAsset("icon_flag.png"),
                LoadSpriteAsset("icon_skull.png"),
                LoadSpriteAsset("icon_ring.png"),
                LoadSpriteAsset("icon_heart.png"),
                LoadSpriteAsset("icon_chest.png"),
                LoadSpriteAsset("icon_door.png"));
        }

        private static Sprite LoadSpriteAsset(string fileName)
        {
            string path = MinimapIconFolder + "/" + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing minimap icon sprite: {path}");
            }
            return sprite;
        }

        private static Sprite LoadSpriteSubAsset(
            string assetPath,
            string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault(candidate => string.Equals(
                    candidate.name,
                    spriteName,
                    StringComparison.Ordinal));
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing sprite {spriteName} in {assetPath}.");
            }
            return sprite;
        }

        private static T EnsurePrefab<T>(
            string assetPath,
            Func<T> create)
            where T : MonoBehaviour
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                ValidatePrefabView(existing, assetPath);
                return existing;
            }
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                throw new InvalidOperationException(
                    $"An incompatible asset already exists at {assetPath}.");
            }

            T created = CreateInTemporaryScene(create);
            ValidatePrefabView(created, assetPath);
            return created;
        }

        private static T CreateInTemporaryScene<T>(Func<T> create)
            where T : MonoBehaviour
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene authoringScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(authoringScene);
            try
            {
                return create();
            }
            finally
            {
                if (previousActiveScene.IsValid() &&
                    previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
                if (authoringScene.IsValid())
                {
                    EditorSceneManager.CloseScene(authoringScene, true);
                }
            }
        }

        private static T SavePrefabView<T>(
            GameObject root,
            string assetPath,
            T temporaryView)
            where T : MonoBehaviour
        {
            if (temporaryView == null)
            {
                throw new ArgumentNullException(nameof(temporaryView));
            }
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            T saved = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    $"Unity failed to create UI prefab {assetPath}.");
            }
            return saved;
        }

        private static void ValidatePrefabView(
            MonoBehaviour view,
            string assetPath)
        {
            bool isValid =
                (view is PrototypeWeaponHudView weapon &&
                    weapon.HasRequiredReferences) ||
                (view is PrototypeHealthHeartView heart &&
                    heart.HasRequiredReferences) ||
                (view is PrototypeHealthHudView health &&
                    health.HasRequiredReferences) ||
                (view is PrototypeDungeonMinimapRoomView minimapRoom &&
                    minimapRoom.HasRequiredReferences) ||
                (view is PrototypeDungeonMinimapConnectionView minimapConnection &&
                    minimapConnection.HasRequiredReferences) ||
                (view is PrototypeDungeonMinimapView minimap &&
                    minimap.HasRequiredReferences) ||
                (view is PrototypePauseView pause &&
                    pause.HasRequiredReferences) ||
                (view is PrototypeRunCompletionView completion &&
                    completion.HasRequiredReferences);
            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"UI prefab has missing authored references: {assetPath}");
            }
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            string[] segments = assetFolder.Split('/');
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

        private static void DestroyTemporaryRoot(GameObject root)
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
