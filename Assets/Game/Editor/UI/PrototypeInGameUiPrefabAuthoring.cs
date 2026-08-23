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
        public const string MinimapPrefabPath =
            PrefabFolder + "/PrototypeDungeonMinimapCanvas.prefab";
        public const string PausePrefabPath =
            PrefabFolder + "/PrototypePauseCanvas.prefab";

        public const string WeaponHudResourcePath =
            "UI/PrototypeWeaponHudCanvas";
        public const string HealthHudResourcePath =
            "UI/PrototypeHealthHudCanvas";
        public const string MinimapResourcePath =
            "UI/PrototypeDungeonMinimapCanvas";
        public const string PauseResourcePath =
            "UI/PrototypePauseCanvas";

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
                PrototypePauseView pause)
            {
                WeaponHud = weaponHud;
                HealthHud = healthHud;
                Minimap = minimap;
                Pause = pause;
            }

            public PrototypeWeaponHudView WeaponHud { get; }

            public PrototypeHealthHudView HealthHud { get; }

            public PrototypeDungeonMinimapView Minimap { get; }

            public PrototypePauseView Pause { get; }
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
                $"Four prefab views are valid; {changedSceneCount} scenes changed.");
        }

        public static PrefabSet EnsurePrefabAssets()
        {
            EnsureAssetFolder(PrefabFolder);
            PrototypeWeaponHudView weapon = EnsurePrefab(
                WeaponHudPrefabPath,
                CreateWeaponHudPrefab);
            PrototypeHealthHudView health = EnsurePrefab(
                HealthHudPrefabPath,
                CreateHealthHudPrefab);
            PrototypeDungeonMinimapView minimap = EnsurePrefab(
                MinimapPrefabPath,
                CreateMinimapPrefab);
            PrototypePauseView pause = EnsurePrefab(
                PausePrefabPath,
                CreatePausePrefab);
            return new PrefabSet(weapon, health, minimap, pause);
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

                var slotBackgrounds = new Image[BombWeaponLoadout.SlotCount];
                var slotFills = new Image[BombWeaponLoadout.SlotCount];
                var slotLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
                var cooldownLabels = new TextMeshProUGUI[BombWeaponLoadout.SlotCount];
                for (int slotIndex = 0;
                     slotIndex < BombWeaponLoadout.SlotCount;
                     slotIndex++)
                {
                    CreateWeaponSlot(
                        panel,
                        slotIndex,
                        out slotBackgrounds[slotIndex],
                        out slotFills[slotIndex],
                        out slotLabels[slotIndex],
                        out cooldownLabels[slotIndex]);
                }

                TextMeshProUGUI swapLabel = PrototypeUiFactory.CreateText(
                    "SwapStatus",
                    panel,
                    18f,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
                SetRect(
                    swapLabel.rectTransform,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 6f),
                    new Vector2(-16f, 24f));
                swapLabel.text = "X  SWAP READY";
                swapLabel.color = Color.white;

                PrototypeWeaponHudView view =
                    root.AddComponent<PrototypeWeaponHudView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    slotBackgrounds,
                    slotFills,
                    slotLabels,
                    cooldownLabels,
                    swapLabel);
                return SavePrefabView(root, WeaponHudPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeHealthHudView CreateHealthHudPrefab()
        {
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
                TextMeshProUGUI playerLabel = CreateHealthLabel(
                    playerPanel,
                    21f,
                    "PLAYER HP  10 / 10");
                Image playerFill = CreateHealthBar(
                    playerPanel,
                    PlayerHealthColor);

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
                    "ROOM TOKENS  0");

                RectTransform bossPanel = CreatePanel(
                    "BossHealthPanel",
                    root.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -24f),
                    new Vector2(560f, 82f));
                TextMeshProUGUI bossLabel = CreateHealthLabel(
                    bossPanel,
                    22f,
                    "BOSS  |  PHASE 1  |  10 / 10");
                Image bossFill = CreateHealthBar(
                    bossPanel,
                    BossHealthColor);

                PrototypeHealthHudView view =
                    root.AddComponent<PrototypeHealthHudView>();
                view.BindAuthoredView(
                    root.GetComponent<Canvas>(),
                    bossPanel.gameObject,
                    playerFill,
                    bossFill,
                    playerLabel,
                    bossLabel,
                    rewardLabel);
                return SavePrefabView(root, HealthHudPrefabPath, view);
            }
            finally
            {
                DestroyTemporaryRoot(root);
            }
        }

        private static PrototypeDungeonMinimapView CreateMinimapPrefab()
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
                view.BindAuthoredView(root.GetComponent<Canvas>(), mapRoot);
                return SavePrefabView(root, MinimapPrefabPath, view);
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
            out Image background,
            out Image fill,
            out TextMeshProUGUI definitionLabel,
            out TextMeshProUGUI cooldownLabel)
        {
            RectTransform slot = CreateRect("Slot" + (slotIndex + 1), panel);
            SetRect(
                slot,
                new Vector2(slotIndex * 0.5f, 1f),
                new Vector2((slotIndex + 1) * 0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(slotIndex == 0 ? 4f : -4f, -8f),
                new Vector2(-16f, 82f));
            background = slot.gameObject.AddComponent<Image>();
            background.color = InactiveSlotColor;
            background.raycastTarget = false;
            Outline outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform bar = CreateRect("CooldownBar", slot);
            SetRect(
                bar,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(0f, 12f));
            Image barBackground = bar.gameObject.AddComponent<Image>();
            barBackground.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);
            barBackground.raycastTarget = false;

            RectTransform fillRect = CreateRect("ReadyFill", bar);
            SetAnchors(fillRect, Vector2.zero, Vector2.one);
            fill = fillRect.gameObject.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            definitionLabel = PrototypeUiFactory.CreateText(
                "Definition",
                slot,
                18f,
                TextAlignmentOptions.MidlineLeft);
            definitionLabel.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            definitionLabel.rectTransform.anchorMax = Vector2.one;
            definitionLabel.rectTransform.offsetMin = new Vector2(10f, 0f);
            definitionLabel.rectTransform.offsetMax = new Vector2(-10f, 0f);
            definitionLabel.text = slotIndex == 0 ? "1  BOMB" : "2  EMPTY";
            definitionLabel.color = Color.white;

            cooldownLabel = PrototypeUiFactory.CreateText(
                "Cooldown",
                slot,
                16f,
                TextAlignmentOptions.MidlineLeft);
            cooldownLabel.rectTransform.anchorMin = new Vector2(0f, 0.14f);
            cooldownLabel.rectTransform.anchorMax = new Vector2(1f, 0.46f);
            cooldownLabel.rectTransform.offsetMin = new Vector2(10f, 0f);
            cooldownLabel.rectTransform.offsetMax = new Vector2(-10f, 0f);
            cooldownLabel.text = "Z  PLACE READY";
            cooldownLabel.color = Color.white;
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
                (view is PrototypeHealthHudView health &&
                    health.HasRequiredReferences) ||
                (view is PrototypeDungeonMinimapView minimap &&
                    minimap.HasRequiredReferences) ||
                (view is PrototypePauseView pause &&
                    pause.HasRequiredReferences);
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
