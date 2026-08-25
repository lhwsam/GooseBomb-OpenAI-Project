using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombSwap.Editor.ContentValidation
{
    public static class PrototypeWorldInteractionAuthoring
    {
        public const string PrefabFolderPath =
            "Assets/Game/Content/Prefabs/Interaction";
        public const string InteractionPromptPrefabPath =
            PrefabFolderPath + "/InteractionPrompt.prefab";
        public const string RecoveryShrinePrefabPath =
            PrefabFolderPath + "/RecoveryShrine.prefab";
        public const string RewardChestPrefabPath =
            PrefabFolderPath + "/RewardChest.prefab";
        public const string BombRewardChoicePrefabPath =
            PrefabFolderPath + "/BombRewardChoice.prefab";
        public const string InteractionKeyAtlasPath =
            "Assets/Game/Content/UI/Sprites/CC0/Game Prompts/Transparent/Letters.png";
        public const string InteractionKeySpriteName = "Letters_31";
        public const string RecoveryWellModelPath =
            "Assets/Arts/Environments/HealStruct/Stone Water Well.fbx";
        public const string RecoveryDrinkClipPath =
            "Assets/Arts/Sound/WaterDrink/Gulp_water.wav";
        public const string RewardChestModelPath =
            "Assets/Arts/Environments/Chest/Closed/Chest.fbx";
        public const string RewardChestOpenModelPath =
            "Assets/Arts/Environments/Chest/Opened/Chest_Open.fbx";
        public const string RewardChestHingeClipPath =
            "Assets/Arts/Sound/ChestHinge/Chest_hinge.wav";
        public const string RecoveryGlowCoreMaterialPath =
            "Assets/Game/Content/Materials/Prototype/RecoveryShrineGlowCore.mat";
        public const string RecoveryGlowHaloMaterialPath =
            "Assets/Game/Content/Materials/Prototype/RecoveryShrineGlowHalo.mat";
        public const string BombRewardGlowCoreMaterialPath =
            "Assets/Game/Content/Materials/Prototype/BombRewardGlowCore.mat";
        public const string BombRewardGlowHaloMaterialPath =
            "Assets/Game/Content/Materials/Prototype/BombRewardGlowHalo.mat";

        private const string WorldInteractablesRootName = "WorldInteractables";
        private const string RecoveryShrineName = "RecoveryShrine";
        private const string SecretRewardCacheName = "SecretRewardCache";
        private static readonly string[] BombRewardChoiceNames =
        {
            "BombRewardChoiceLeft",
            "BombRewardChoiceCenter",
            "BombRewardChoiceRight",
        };
        private static readonly string[] LegacyRewardChestNames =
        {
            "RewardChestLeft",
            "RewardChestCenter",
            "RewardChestRight",
        };

        [MenuItem("Bomb Swap/Prototype/Synchronize World Interactions")]
        public static void SynchronizeWorldInteractionsMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before synchronizing world interactions.");
            }

            EnsurePrefabAssets();
            SynchronizeScene(PrototypeContentValidator.DungeonRewardScenePath);
            SynchronizeScene(PrototypeContentValidator.DungeonRecoveryScenePath);
            SynchronizeScene(PrototypeContentValidator.DungeonSecretScenePath);
            PrototypeContentBuilder.RefreshInputActionsMenu();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Synchronized bomb reward choices, persistent reward chest, recovery shrine, shared F prompts, and presenter references.");
        }

        public static void EnsurePrefabAssets()
        {
            EnsureFolder(PrefabFolderPath);
            EnsureInteractionPromptPrefab();
            Material recoveryGlowCore = EnsureGlowMaterial(
                RecoveryGlowCoreMaterialPath,
                new Color(0.18f, 1.8f, 0.85f, 0.72f));
            Material recoveryGlowHalo = EnsureGlowMaterial(
                RecoveryGlowHaloMaterialPath,
                new Color(0.08f, 1.15f, 0.55f, 0.16f));
            Material bombRewardGlowCore = EnsureGlowMaterial(
                BombRewardGlowCoreMaterialPath,
                new Color(2.1f, 1.35f, 0.18f, 0.72f));
            Material bombRewardGlowHalo = EnsureGlowMaterial(
                BombRewardGlowHaloMaterialPath,
                new Color(1.3f, 0.62f, 0.08f, 0.16f));
            EnsureWorldInteractablePrefab(
                RecoveryShrinePrefabPath,
                "RecoveryShrine",
                RecoveryWellModelPath,
                0.01f,
                PrototypeContentValidator.RecoveryPickupMaterialPath,
                1.45f,
                false);
            SynchronizeRecoveryShrineGlow(
                recoveryGlowCore,
                recoveryGlowHalo);
            SynchronizeRecoveryShrineAudio();
            EnsureBombRewardChoicePrefab(
                bombRewardGlowCore,
                bombRewardGlowHalo);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    RewardChestPrefabPath) == null)
            {
                EnsureWorldInteractablePrefab(
                    RewardChestPrefabPath,
                    "RewardChest",
                    RewardChestModelPath,
                    1.1f,
                    PrototypeContentValidator.SecretRewardMaterialPath,
                    1.65f,
                    true);
            }
            SynchronizeRewardChestPresentation();
            SynchronizeRewardChestGlow(
                bombRewardGlowCore,
                bombRewardGlowHalo);
        }

        public static PrototypeWorldInteractableView EnsureRecoveryView(Scene scene)
        {
            EnsurePrefabAssets();
            Transform parent = EnsureWorldInteractablesRoot(scene);
            return EnsureSceneView(
                scene,
                parent,
                RecoveryShrineName,
                RecoveryShrinePrefabPath,
                Vector3.zero);
        }

        public static PrototypeWorldInteractableView EnsureSecretRewardView(Scene scene)
        {
            EnsurePrefabAssets();
            Transform parent = EnsureWorldInteractablesRoot(scene);
            return EnsureSceneView(
                scene,
                parent,
                SecretRewardCacheName,
                RewardChestPrefabPath,
                Vector3.zero);
        }

        public static PrototypeWorldInteractableView[] EnsureBombRewardViews(
            Scene scene)
        {
            EnsurePrefabAssets();
            Transform parent = EnsureWorldInteractablesRoot(scene);
            RemoveLegacyBombRewardViews(parent);
            Vector3[] positions =
            {
                new Vector3(-1f, 0f, 0f),
                Vector3.zero,
                new Vector3(1f, 0f, 0f),
            };
            var views = new PrototypeWorldInteractableView[
                BombRewardChoiceNames.Length];
            for (int index = 0; index < views.Length; index++)
            {
                views[index] = EnsureSceneView(
                    scene,
                    parent,
                    BombRewardChoiceNames[index],
                    BombRewardChoicePrefabPath,
                    positions[index]);
            }
            return views;
        }

        private static void SynchronizeScene(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForSynchronization = !scene.IsValid() || !scene.isLoaded;
            if (openedForSynchronization)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                PrototypeDungeonRoomBinder binder =
                    FindExactlyOneInScene<PrototypeDungeonRoomBinder>(scene);
                if (string.Equals(
                        scenePath,
                        PrototypeContentValidator.DungeonRewardScenePath,
                        StringComparison.Ordinal))
                {
                    PrototypeBombRewardPresenter presenter =
                        FindExactlyOneInScene<PrototypeBombRewardPresenter>(scene);
                    presenter.Configure(binder, EnsureBombRewardViews(scene));
                    EditorUtility.SetDirty(presenter);
                }
                else if (string.Equals(
                             scenePath,
                             PrototypeContentValidator.DungeonRecoveryScenePath,
                             StringComparison.Ordinal))
                {
                    PrototypeRecoveryPickupPresenter presenter =
                        FindExactlyOneInScene<PrototypeRecoveryPickupPresenter>(scene);
                    presenter.Configure(
                        binder,
                        EnsureRecoveryView(scene),
                        PrototypeRecoveryPickupPresenter.DefaultRecoveryAmount,
                        Vector2Int.zero);
                    EditorUtility.SetDirty(presenter);
                }
                else if (string.Equals(
                             scenePath,
                             PrototypeContentValidator.DungeonSecretScenePath,
                             StringComparison.Ordinal))
                {
                    PrototypeSecretRewardPresenter presenter =
                        FindExactlyOneInScene<PrototypeSecretRewardPresenter>(scene);
                    presenter.Configure(
                        binder,
                        EnsureSecretRewardView(scene),
                        PrototypeSecretRewardPresenter.DefaultTokenReward,
                        Vector2Int.zero);
                    EditorUtility.SetDirty(presenter);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save world interaction scene '{scenePath}'.");
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

        private static void EnsureInteractionPromptPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                InteractionPromptPrefabPath);
            if (existing != null)
            {
                Image existingImage = existing.GetComponentInChildren<Image>(true);
                if (existingImage == null ||
                    existingImage.sprite == null ||
                    !string.Equals(
                        existingImage.sprite.name,
                        InteractionKeySpriteName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Existing interaction prompt prefab is missing the authored F key sprite.");
                }
                return;
            }

            Sprite keySprite = LoadRequiredSprite(
                InteractionKeyAtlasPath,
                InteractionKeySpriteName);
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var prompt = new GameObject(
                    "InteractionPrompt",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler));
                SceneManager.MoveGameObjectToScene(prompt, previewScene);
                RectTransform promptRect = prompt.GetComponent<RectTransform>();
                promptRect.sizeDelta = new Vector2(64f, 64f);
                promptRect.localScale = Vector3.one * 0.01f;

                Canvas canvas = prompt.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 50;

                CanvasScaler scaler = prompt.GetComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 16f;

                var iconObject = new GameObject(
                    "KeyIcon",
                    typeof(RectTransform),
                    typeof(Image));
                iconObject.transform.SetParent(prompt.transform, false);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                Image image = iconObject.GetComponent<Image>();
                image.sprite = keySprite;
                image.preserveAspect = true;
                image.raycastTarget = false;

                prompt.SetActive(false);
                if (PrefabUtility.SaveAsPrefabAsset(
                        prompt,
                        InteractionPromptPrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the interaction prompt prefab.");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void EnsureWorldInteractablePrefab(
            string prefabPath,
            string prefabName,
            string modelPath,
            float modelScaleMultiplier,
            string availabilityMaterialPath,
            float promptHeight,
            bool requiresDynamicContentAnchor)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                throw new InvalidOperationException(
                    $"Cannot configure '{prefabPath}' because model '{modelPath}' is missing.");
            }

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);
            if (existing != null)
            {
                SynchronizeExistingWorldInteractablePrefab(
                    prefabPath,
                    model,
                    modelScaleMultiplier,
                    requiresDynamicContentAnchor);
                return;
            }

            Material availabilityMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(availabilityMaterialPath);
            GameObject promptPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    InteractionPromptPrefabPath);
            if (availabilityMaterial == null || promptPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Cannot create '{prefabPath}' because its material or prompt is missing.");
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject(prefabName);
                SceneManager.MoveGameObjectToScene(root, previewScene);
                PrototypeWorldInteractableView view =
                    root.AddComponent<PrototypeWorldInteractableView>();

                var persistentVisual = new GameObject("PersistentVisual");
                SceneManager.MoveGameObjectToScene(persistentVisual, previewScene);
                persistentVisual.transform.SetParent(root.transform, false);
                GameObject modelInstance = (GameObject)
                    PrefabUtility.InstantiatePrefab(model, previewScene);
                modelInstance.name = "Model";
                modelInstance.transform.SetParent(
                    persistentVisual.transform,
                    false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                ApplyImportedModelScale(
                    modelInstance.transform,
                    model.transform,
                    modelScaleMultiplier);

                GameObject availabilityEffect =
                    GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                SceneManager.MoveGameObjectToScene(
                    availabilityEffect,
                    previewScene);
                availabilityEffect.name = "AvailabilityEffect";
                availabilityEffect.transform.SetParent(root.transform, false);
                availabilityEffect.transform.localPosition =
                    new Vector3(0f, 0.025f, 0f);
                availabilityEffect.transform.localScale =
                    new Vector3(0.72f, 0.025f, 0.72f);
                UnityEngine.Object.DestroyImmediate(
                    availabilityEffect.GetComponent<Collider>());
                availabilityEffect.GetComponent<Renderer>().sharedMaterial =
                    availabilityMaterial;

                GameObject prompt = (GameObject)
                    PrefabUtility.InstantiatePrefab(promptPrefab, previewScene);
                prompt.name = "InteractionPrompt";
                prompt.transform.SetParent(root.transform, false);
                prompt.transform.localPosition =
                    new Vector3(0f, promptHeight, 0f);
                prompt.transform.localRotation = Quaternion.identity;
                prompt.SetActive(false);

                Transform contentAnchor = null;
                if (requiresDynamicContentAnchor)
                {
                    var anchorObject = new GameObject("DynamicContentAnchor");
                    SceneManager.MoveGameObjectToScene(anchorObject, previewScene);
                    contentAnchor = anchorObject.transform;
                    contentAnchor.SetParent(root.transform, false);
                    contentAnchor.localPosition = new Vector3(0f, 1.15f, 0f);
                }

                view.Configure(
                    persistentVisual,
                    availabilityEffect,
                    prompt,
                    contentAnchor);
                if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save world interactable prefab '{prefabPath}'.");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void EnsureBombRewardChoicePrefab(
            Material coreMaterial,
            Material haloMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                BombRewardChoicePrefabPath);
            if (existing == null)
            {
                GameObject promptPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        InteractionPromptPrefabPath);
                if (promptPrefab == null)
                {
                    throw new InvalidOperationException(
                        "Cannot create the bomb reward choice prefab because the shared prompt is missing.");
                }

                Scene previewScene = EditorSceneManager.NewPreviewScene();
                try
                {
                    var root = new GameObject("BombRewardChoice");
                    SceneManager.MoveGameObjectToScene(root, previewScene);
                    PrototypeWorldInteractableView view =
                        root.AddComponent<PrototypeWorldInteractableView>();

                    var persistentVisual = new GameObject("PersistentVisual");
                    SceneManager.MoveGameObjectToScene(
                        persistentVisual,
                        previewScene);
                    persistentVisual.transform.SetParent(root.transform, false);

                    var availabilityEffect =
                        new GameObject("BombRewardChoiceGlow");
                    SceneManager.MoveGameObjectToScene(
                        availabilityEffect,
                        previewScene);
                    availabilityEffect.transform.SetParent(root.transform, false);

                    GameObject prompt = (GameObject)
                        PrefabUtility.InstantiatePrefab(
                            promptPrefab,
                            previewScene);
                    prompt.name = "InteractionPrompt";
                    prompt.transform.SetParent(root.transform, false);
                    prompt.transform.localPosition =
                        new Vector3(0f, 1.55f, 0f);
                    prompt.transform.localRotation = Quaternion.identity;
                    prompt.SetActive(false);

                    var anchorObject = new GameObject("DynamicContentAnchor");
                    SceneManager.MoveGameObjectToScene(
                        anchorObject,
                        previewScene);
                    anchorObject.transform.SetParent(root.transform, false);
                    anchorObject.transform.localPosition =
                        new Vector3(0f, 0.1f, 0f);

                    view.Configure(
                        persistentVisual,
                        availabilityEffect,
                        prompt,
                        anchorObject.transform);
                    if (PrefabUtility.SaveAsPrefabAsset(
                            root,
                            BombRewardChoicePrefabPath) == null)
                    {
                        throw new InvalidOperationException(
                            "Unity failed to save the bomb reward choice prefab.");
                    }
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(previewScene);
                }
            }

            SynchronizeBombRewardChoiceGlow(
                coreMaterial,
                haloMaterial);
        }

        private static Material EnsureGlowMaterial(
            string materialPath,
            Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "World interaction glow requires the URP Unlit shader.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(materialPath),
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SynchronizeRecoveryShrineGlow(
            Material coreMaterial,
            Material haloMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                RecoveryShrinePrefabPath);
            try
            {
                PrototypeWorldInteractableView view =
                    root.GetComponent<PrototypeWorldInteractableView>();
                if (view == null || !view.HasRequiredReferences)
                {
                    throw new InvalidOperationException(
                        "Recovery shrine prefab is missing its world interaction view references.");
                }

                GameObject glowRoot = view.AvailabilityEffectRoot;
                if (glowRoot == null ||
                    !string.Equals(
                        glowRoot.name,
                        "RecoveryShrineGlow",
                        StringComparison.Ordinal))
                {
                    if (glowRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(glowRoot);
                    }
                    glowRoot = new GameObject("RecoveryShrineGlow");
                    glowRoot.transform.SetParent(root.transform, false);
                }
                glowRoot.transform.localPosition = Vector3.zero;
                glowRoot.transform.localRotation = Quaternion.identity;
                glowRoot.transform.localScale = Vector3.one;
                glowRoot.SetActive(true);

                GameObject core = EnsureGlowSphere(
                    glowRoot.transform,
                    "GlowCore",
                    new Vector3(0f, 0.46f, 0f),
                    Vector3.one * 0.18f,
                    coreMaterial);
                GameObject halo = EnsureGlowSphere(
                    glowRoot.transform,
                    "GlowHalo",
                    new Vector3(0f, 0.44f, 0f),
                    new Vector3(0.44f, 0.3f, 0.44f),
                    haloMaterial);
                EnsureGlowMotes(
                    glowRoot.transform,
                    core.GetComponent<MeshFilter>().sharedMesh,
                    coreMaterial,
                    new Vector3(0f, 0.4f, 0f),
                    0.16f,
                    new Color(0.38f, 1f, 0.72f, 0.85f),
                    new Color(0.12f, 0.8f, 0.48f, 0.5f));

                PrototypeRecoveryShrineGlow pulse =
                    glowRoot.GetComponent<PrototypeRecoveryShrineGlow>();
                if (pulse == null)
                {
                    pulse = glowRoot.AddComponent<PrototypeRecoveryShrineGlow>();
                }
                pulse.Configure(core.transform, halo.transform);

                view.Configure(
                    view.PersistentVisualRoot,
                    glowRoot,
                    view.InteractionPromptRoot,
                    view.DynamicContentAnchor);
                if (PrefabUtility.SaveAsPrefabAsset(
                        root,
                        RecoveryShrinePrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the recovery shrine glow prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SynchronizeBombRewardChoiceGlow(
            Material coreMaterial,
            Material haloMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                BombRewardChoicePrefabPath);
            try
            {
                PrototypeWorldInteractableView view =
                    root.GetComponent<PrototypeWorldInteractableView>();
                if (view == null ||
                    !view.HasRequiredReferences ||
                    view.DynamicContentAnchor == null)
                {
                    throw new InvalidOperationException(
                        "Bomb reward choice prefab is missing its world interaction view references.");
                }

                Transform persistentVisual = view.PersistentVisualRoot.transform;
                for (int index = persistentVisual.childCount - 1;
                    index >= 0;
                    index--)
                {
                    UnityEngine.Object.DestroyImmediate(
                        persistentVisual.GetChild(index).gameObject);
                }
                persistentVisual.gameObject.SetActive(true);

                GameObject glowRoot = view.AvailabilityEffectRoot;
                if (glowRoot == null ||
                    !string.Equals(
                        glowRoot.name,
                        "BombRewardChoiceGlow",
                        StringComparison.Ordinal))
                {
                    if (glowRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(glowRoot);
                    }
                    glowRoot = new GameObject("BombRewardChoiceGlow");
                    glowRoot.transform.SetParent(root.transform, false);
                }
                glowRoot.transform.localPosition = Vector3.zero;
                glowRoot.transform.localRotation = Quaternion.identity;
                glowRoot.transform.localScale = Vector3.one;
                glowRoot.SetActive(true);

                GameObject core = EnsureGlowSphere(
                    glowRoot.transform,
                    "GlowCore",
                    new Vector3(0f, 0.16f, 0f),
                    new Vector3(0.72f, 0.12f, 0.72f),
                    coreMaterial);
                GameObject halo = EnsureGlowSphere(
                    glowRoot.transform,
                    "GlowHalo",
                    new Vector3(0f, 0.1f, 0f),
                    new Vector3(1.16f, 0.08f, 1.16f),
                    haloMaterial);
                EnsureGlowMotes(
                    glowRoot.transform,
                    core.GetComponent<MeshFilter>().sharedMesh,
                    coreMaterial,
                    new Vector3(0f, 0.14f, 0f),
                    0.42f,
                    new Color(1f, 0.76f, 0.28f, 0.9f),
                    new Color(1f, 0.38f, 0.08f, 0.52f));

                PrototypeWorldInteractionGlow pulse =
                    glowRoot.GetComponent<PrototypeWorldInteractionGlow>();
                if (pulse == null)
                {
                    pulse = glowRoot.AddComponent<PrototypeWorldInteractionGlow>();
                }
                pulse.Configure(
                    core.transform,
                    halo.transform,
                    0.75f,
                    0.08f,
                    0.14f);

                view.DynamicContentAnchor.localPosition =
                    new Vector3(0f, 0.1f, 0f);
                view.InteractionPromptRoot.transform.localPosition =
                    new Vector3(0f, 1.55f, 0f);
                view.Configure(
                    view.PersistentVisualRoot,
                    glowRoot,
                    view.InteractionPromptRoot,
                    view.DynamicContentAnchor);
                if (PrefabUtility.SaveAsPrefabAsset(
                        root,
                        BombRewardChoicePrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the bomb reward choice glow prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SynchronizeRewardChestGlow(
            Material coreMaterial,
            Material haloMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                RewardChestPrefabPath);
            try
            {
                PrototypeWorldInteractableView view =
                    root.GetComponent<PrototypeWorldInteractableView>();
                if (view == null ||
                    !view.HasRequiredReferences ||
                    view.DynamicContentAnchor == null)
                {
                    throw new InvalidOperationException(
                        "Reward chest prefab is missing its world interaction view references.");
                }

                Bounds modelBounds = GetCombinedRendererBounds(
                    view.PersistentVisualRoot);
                float modelFootprint = Mathf.Max(
                    modelBounds.size.x,
                    modelBounds.size.z);
                float coreDiameter = modelFootprint * 0.95f;
                float haloDiameter = modelFootprint * 1.4f;
                float moteRadius = modelFootprint * 0.55f;

                GameObject glowRoot = view.AvailabilityEffectRoot;
                if (glowRoot == null ||
                    !string.Equals(
                        glowRoot.name,
                        "RewardChestGlow",
                        StringComparison.Ordinal))
                {
                    if (glowRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(glowRoot);
                    }
                    glowRoot = new GameObject("RewardChestGlow");
                    glowRoot.transform.SetParent(root.transform, false);
                }
                glowRoot.transform.localPosition = Vector3.zero;
                glowRoot.transform.localRotation = Quaternion.identity;
                glowRoot.transform.localScale = Vector3.one;
                glowRoot.SetActive(true);

                GameObject core = EnsureGlowSphere(
                    glowRoot.transform,
                    "GlowCore",
                    new Vector3(0f, 0.1f, 0f),
                    new Vector3(coreDiameter, 0.12f, coreDiameter),
                    coreMaterial);
                GameObject halo = EnsureGlowSphere(
                    glowRoot.transform,
                    "GlowHalo",
                    new Vector3(0f, 0.06f, 0f),
                    new Vector3(haloDiameter, 0.08f, haloDiameter),
                    haloMaterial);
                EnsureGlowMotes(
                    glowRoot.transform,
                    core.GetComponent<MeshFilter>().sharedMesh,
                    coreMaterial,
                    new Vector3(0f, 0.1f, 0f),
                    moteRadius,
                    new Color(1f, 0.76f, 0.28f, 0.9f),
                    new Color(1f, 0.38f, 0.08f, 0.52f));

                PrototypeWorldInteractionGlow pulse =
                    glowRoot.GetComponent<PrototypeWorldInteractionGlow>();
                if (pulse == null)
                {
                    pulse = glowRoot.AddComponent<PrototypeWorldInteractionGlow>();
                }
                pulse.Configure(
                    core.transform,
                    halo.transform,
                    0.72f,
                    0.08f,
                    0.12f);

                view.Configure(
                    view.PersistentVisualRoot,
                    glowRoot,
                    view.InteractionPromptRoot,
                    view.DynamicContentAnchor);
                if (PrefabUtility.SaveAsPrefabAsset(
                        root,
                        RewardChestPrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the reward chest glow prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void SynchronizeRewardChestPresentation()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                RewardChestPrefabPath);
            try
            {
                PrototypeWorldInteractableView worldView =
                    root.GetComponent<PrototypeWorldInteractableView>();
                if (worldView == null || !worldView.HasRequiredReferences)
                {
                    throw new InvalidOperationException(
                        "Reward chest prefab is missing its world interaction view references.");
                }

                Transform closed = worldView.PersistentVisualRoot.transform.Find("Chest");
                Transform opened = worldView.PersistentVisualRoot.transform.Find("Chest_Open");
                if (closed == null)
                {
                    closed = worldView.PersistentVisualRoot.transform.Find("Model");
                    if (closed != null)
                    {
                        closed.name = "Chest";
                    }
                }
                if (opened == null)
                {
                    GameObject openModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                        RewardChestOpenModelPath);
                    if (openModel == null || closed == null)
                    {
                        throw new InvalidOperationException(
                            $"Reward chest open model '{RewardChestOpenModelPath}' is missing.");
                    }
                    GameObject openInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                        openModel,
                        root.scene);
                    openInstance.name = "Chest_Open";
                    openInstance.transform.SetParent(
                        worldView.PersistentVisualRoot.transform,
                        false);
                    openInstance.transform.localPosition = closed.localPosition;
                    openInstance.transform.localRotation = closed.localRotation;
                    openInstance.transform.localScale = closed.localScale;
                    opened = openInstance.transform;
                }
                if (closed == null || opened == null)
                {
                    throw new InvalidOperationException(
                        "Reward chest presentation requires Chest and Chest_Open.");
                }

                PrototypeRewardChestView chestView =
                    root.GetComponent<PrototypeRewardChestView>();
                if (chestView == null)
                {
                    chestView = root.AddComponent<PrototypeRewardChestView>();
                }
                chestView.Configure(closed.gameObject, opened.gameObject);
                chestView.SetOpen(false);
                ConfigureWorldInteractionAudio(root, RewardChestHingeClipPath);

                if (PrefabUtility.SaveAsPrefabAsset(root, RewardChestPrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the reward chest presentation prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void SynchronizeRecoveryShrineAudio()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                RecoveryShrinePrefabPath);
            try
            {
                ConfigureWorldInteractionAudio(root, RecoveryDrinkClipPath);
                if (PrefabUtility.SaveAsPrefabAsset(
                        root,
                        RecoveryShrinePrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Unity failed to save the recovery shrine interaction audio.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureWorldInteractionAudio(
            GameObject root,
            string clipPath)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                PrototypeContentValidator.AudioMixerPath);
            AudioMixerGroup sfxGroup = mixer != null
                ? mixer.FindMatchingGroups("SFX").FirstOrDefault()
                : null;
            if (clip == null || sfxGroup == null)
            {
                throw new InvalidOperationException(
                    $"World interaction audio requires clip '{clipPath}' and the SFX mixer group.");
            }

            PrototypeWorldInteractionAudio interactionAudio =
                root.GetComponent<PrototypeWorldInteractionAudio>();
            if (interactionAudio == null)
            {
                interactionAudio = root.AddComponent<PrototypeWorldInteractionAudio>();
            }
            AudioSource source = root.GetComponent<AudioSource>();
            if (source == null)
            {
                source = root.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = false;
            source.clip = clip;
            source.outputAudioMixerGroup = sfxGroup;
            source.spatialBlend = 0f;
            interactionAudio.Configure(source);
        }

        private static Bounds GetCombinedRendererBounds(
            GameObject visualRoot)
        {
            Renderer[] renderers =
                visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"World interaction visual '{visualRoot.name}' has no renderer bounds.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static GameObject EnsureGlowSphere(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            GameObject sphere = existing != null
                ? existing.gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = objectName;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = localScale;
            sphere.SetActive(true);

            Collider collider = sphere.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"World interaction {objectName} requires a mesh renderer.");
            }
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return sphere;
        }

        private static void EnsureGlowMotes(
            Transform parent,
            Mesh particleMesh,
            Material material,
            Vector3 localPosition,
            float shapeRadius,
            Color minimumColor,
            Color maximumColor)
        {
            if (shapeRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shapeRadius),
                    shapeRadius,
                    "Glow mote shape radius must be positive.");
            }

            Transform existing = parent.Find("GlowMotes");
            GameObject motes = existing != null
                ? existing.gameObject
                : new GameObject("GlowMotes");
            motes.transform.SetParent(parent, false);
            motes.transform.localPosition = localPosition;
            motes.transform.localRotation = Quaternion.identity;
            motes.transform.localScale = Vector3.one;
            motes.SetActive(true);

            ParticleSystem particles = motes.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = motes.AddComponent<ParticleSystem>();
            }
            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.duration = 2f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                minimumColor,
                maximumColor);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 12;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 5f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = shapeRadius;
            shape.radiusThickness = 1f;

            ParticleSystem.VelocityOverLifetimeModule velocity =
                particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            var zeroVelocity = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.x = zeroVelocity;
            velocity.y = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            velocity.z = zeroVelocity;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alpha = new Gradient();
            alpha.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = alpha;

            ParticleSystemRenderer particleRenderer =
                motes.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = particleMesh;
            particleRenderer.sharedMaterial = material;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.lightProbeUsage = LightProbeUsage.Off;
            particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void SynchronizeExistingWorldInteractablePrefab(
            string prefabPath,
            GameObject sourceModel,
            float modelScaleMultiplier,
            bool requiresDynamicContentAnchor)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PrototypeWorldInteractableView view =
                    root.GetComponent<PrototypeWorldInteractableView>();
                if (view == null ||
                    !view.HasRequiredReferences ||
                    (requiresDynamicContentAnchor &&
                     view.DynamicContentAnchor == null))
                {
                    throw new InvalidOperationException(
                        $"Existing world interactable prefab '{prefabPath}' has incomplete references.");
                }

                Transform modelTransform =
                    view.PersistentVisualRoot.transform.Find("Model");
                if (modelTransform == null)
                {
                    throw new InvalidOperationException(
                        $"Existing world interactable prefab '{prefabPath}' is missing its Model child.");
                }

                ApplyImportedModelScale(
                    modelTransform,
                    sourceModel.transform,
                    modelScaleMultiplier);
                if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
                {
                    throw new InvalidOperationException(
                        $"Unity failed to update world interactable prefab '{prefabPath}'.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyImportedModelScale(
            Transform modelTransform,
            Transform sourceModelTransform,
            float scaleMultiplier)
        {
            if (scaleMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scaleMultiplier),
                    scaleMultiplier,
                    "World interactable model scale multiplier must be positive.");
            }

            modelTransform.localScale =
                sourceModelTransform.localScale * scaleMultiplier;
        }

        private static PrototypeWorldInteractableView EnsureSceneView(
            Scene scene,
            Transform parent,
            string objectName,
            string prefabPath,
            Vector3 localPosition)
        {
            Transform existingTransform = parent.Find(objectName);
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Missing world interactable prefab '{prefabPath}'.");
            }

            GameObject instance = existingTransform != null
                ? existingTransform.gameObject
                : null;
            if (instance != null &&
                PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefab)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                instance = null;
            }
            if (instance == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    prefab,
                    scene);
                instance.name = objectName;
                instance.transform.SetParent(parent, false);
            }

            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);

            PrototypeWorldInteractableView view =
                instance.GetComponent<PrototypeWorldInteractableView>();
            if (view == null || !view.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    $"World interactable scene object '{objectName}' is not configured.");
            }
            EditorUtility.SetDirty(instance);
            return view;
        }

        private static void RemoveLegacyBombRewardViews(Transform parent)
        {
            for (int index = 0;
                index < LegacyRewardChestNames.Length;
                index++)
            {
                Transform legacy = parent.Find(LegacyRewardChestNames[index]);
                if (legacy != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacy.gameObject);
                }
            }
        }

        private static Transform EnsureWorldInteractablesRoot(Scene scene)
        {
            TestSandboxContext context =
                FindExactlyOneInScene<TestSandboxContext>(scene);
            Transform environment = context.GridRoot.Find("Environment");
            if (environment == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' is missing GridRoot/Environment.");
            }

            Transform root = environment.Find(WorldInteractablesRootName);
            if (root != null)
            {
                return root;
            }

            var rootObject = new GameObject(WorldInteractablesRootName);
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            rootObject.transform.SetParent(environment, false);
            return rootObject.transform;
        }

        private static T FindExactlyOneInScene<T>(Scene scene)
            where T : Component
        {
            T[] components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' must contain exactly one {typeof(T).Name}; found {components.Length}.");
            }
            return components[0];
        }

        private static Sprite LoadRequiredSprite(
            string atlasPath,
            string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(atlasPath)
                .OfType<Sprite>()
                .SingleOrDefault(candidate => string.Equals(
                    candidate.name,
                    spriteName,
                    StringComparison.Ordinal));
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Sprite '{spriteName}' is missing from '{atlasPath}'.");
            }
            return sprite;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = folderPath.Substring(
                0,
                folderPath.LastIndexOf('/'));
            string name = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
