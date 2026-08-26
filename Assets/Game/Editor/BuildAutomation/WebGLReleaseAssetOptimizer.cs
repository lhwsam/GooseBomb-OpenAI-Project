using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BombSwap.Editor.Verification
{
    public static class WebGLReleaseAssetOptimizer
    {
        private const string WebGlPlatformName = "WebGL";

        private static readonly TextureImportRule[] TextureRules =
        {
            new("Assets/Game/Content/UI/Sprites/Common/Bomb/Bomb_Area.png", 256),
            new("Assets/Game/Content/UI/Sprites/Common/Bomb/Bomb_Line.png", 256),
            new("Assets/Game/Content/UI/Sprites/Common/Bomb/Bomb_Cross.png", 256),

            new("Assets/Arts/Character/PlayerDuck/material_0_Pbr_Diffuse.png", 1024),
            new("Assets/Arts/Character/PlayerDuck/material_0_Pbr_Normal.png", 1024),
            new("Assets/Arts/Character/Pig/Boss/material_0_Pbr_Diffuse.png", 1024),
            new("Assets/Arts/Character/Pig/Boss/material_0_Pbr_Normal.png", 1024),
            new("Assets/Arts/Character/Pig/Chaser/material_0_Pbr_Diffuse.png", 1024),
            new("Assets/Arts/Character/Pig/Chaser/material_0_Pbr_Normal.png", 1024),
            new("Assets/Arts/Character/Pig/Charger/material_0_Pbr_Diffuse.png", 1024),
            new("Assets/Arts/Character/Pig/Charger/material_0_Pbr_Normal.png", 1024),
            new("Assets/Arts/Character/Pig/SelfDestruct/material_0_Pbr_Diffuse.png", 1024),
            new("Assets/Arts/Character/Pig/SelfDestruct/material_0_Pbr_Normal.png", 1024),
            new("Assets/Arts/Character/Pig/Thrower/material_0_Pbr_Diffuse.png", 1024),
            new("Assets/Arts/Character/Pig/Thrower/material_0_Pbr_Normal.png", 1024),

            new("Assets/Arts/Bomb/Boss/diffuse.png", 512),
            new("Assets/Arts/Bomb/Boss/normal.png", 512),
            new("Assets/Arts/Bomb/Player/Normal/diffuse.png", 512),
            new("Assets/Arts/Bomb/Player/Normal/normal.png", 512),
            new("Assets/Arts/Bomb/Player/Range/diffuse.png", 512),
            new("Assets/Arts/Bomb/Player/Range/normal.png", 512),
            new("Assets/Arts/Bomb/Player/Straight/diffuse.png", 512),
            new("Assets/Arts/Bomb/Player/Straight/normal.png", 512),
            new("Assets/Arts/Bomb/ThrowerPig/diffuse.png", 512),
            new("Assets/Arts/Bomb/ThrowerPig/normal.png", 512),

            new("Assets/Arts/Environments/Block/벽돌코너/diffuse.png", 512),
            new("Assets/Arts/Environments/Block/벽돌코너/normal.png", 512),
            new("Assets/Arts/Environments/Block/벽돌블럭/diffuse.png", 512),
            new("Assets/Arts/Environments/Block/벽돌블럭/normal.png", 512),
            new("Assets/Arts/Environments/Block/금간 블럭/diffuse.png", 512),
            new("Assets/Arts/Environments/Block/금간 블럭/normal.png", 512),
            new("Assets/Arts/Environments/Block/나무상자/diffuse.png", 512),
            new("Assets/Arts/Environments/Block/나무상자/normal.png", 512),
            new("Assets/Arts/Environments/Chest/Closed/diffuse.png", 512),
            new("Assets/Arts/Environments/Chest/Closed/normal.png", 512),
            new("Assets/Arts/Environments/Chest/Opened/diffuse.png", 512),
            new("Assets/Arts/Environments/Chest/Opened/normal.png", 512),
            new("Assets/Arts/Environments/Door/diffuse.png", 512),
            new("Assets/Arts/Environments/Door/normal.png", 512),
            new("Assets/Arts/Environments/HealStruct/diffuse.png", 512),
            new("Assets/Arts/Environments/HealStruct/normal.png", 512),
            new("Assets/Arts/Environments/Torch/diffuse.png", 512),
            new("Assets/Arts/Environments/Torch/normal.png", 512),

            new("Assets/Arts/VFX/GabrielAguiarProductions/FreeQuickEffectsVol1/Textures/Circle01_v1.png", 512),
            new("Assets/Arts/VFX/GabrielAguiarProductions/FreeQuickEffectsVol1/Textures/DistortedFlare01.png", 512),
            new("Assets/Arts/VFX/GabrielAguiarProductions/FreeQuickEffectsVol1/Textures/Flame02.png", 512),
            new("Assets/Arts/VFX/GabrielAguiarProductions/FreeQuickEffectsVol1/Textures/Flare00.PNG", 512),
            new("Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Textures/Explosion.tif", 512),
            new("Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Textures/ExplosionEmission.tif", 512),
            new("Assets/Arts/VFX/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Textures/SmokePuff02.png", 512),
        };

        private static readonly string[] BaseRigModelPaths =
        {
            "Assets/Arts/Character/PlayerDuck/player-duck-rigged.fbx",
            "Assets/Arts/Character/Pig/Boss/boss-pig-rigged.fbx",
            "Assets/Arts/Character/Pig/Chaser/normal-pig-rigged.fbx",
            "Assets/Arts/Character/Pig/Charger/charger-pig-rigged.fbx",
            "Assets/Arts/Character/Pig/SelfDestruct/bomber-pig-rigged.fbx",
            "Assets/Arts/Character/Pig/Thrower/ranged-pig-rigged.fbx",
        };

        [MenuItem("Bomb Swap/Build/Apply WebGL Release Import Settings")]
        public static void Apply()
        {
            var changedAssets = new List<string>();
            foreach (TextureImportRule rule in TextureRules)
            {
                ApplyTextureRule(rule, changedAssets);
            }

            foreach (string modelPath in BaseRigModelPaths)
            {
                DisableUnusedBaseRigAnimation(modelPath, changedAssets);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"BOMBSWAP_WEBGL_RELEASE_IMPORT_SETTINGS applied={changedAssets.Count}" +
                (changedAssets.Count == 0
                    ? string.Empty
                    : Environment.NewLine + string.Join(Environment.NewLine, changedAssets)));
        }

        private static void ApplyTextureRule(
            TextureImportRule rule,
            ICollection<string> changedAssets)
        {
            var importer = AssetImporter.GetAtPath(rule.AssetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Texture importer was not found for '{rule.AssetPath}'.");
            }

            TextureImporterPlatformSettings settings =
                importer.GetPlatformTextureSettings(WebGlPlatformName);
            if (settings.overridden &&
                settings.maxTextureSize == rule.MaxTextureSize &&
                settings.format == TextureImporterFormat.Automatic &&
                settings.textureCompression == TextureImporterCompression.Compressed &&
                !settings.crunchedCompression)
            {
                return;
            }

            Undo.RecordObject(importer, "Apply WebGL release texture settings");
            settings.name = WebGlPlatformName;
            settings.overridden = true;
            settings.maxTextureSize = rule.MaxTextureSize;
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            changedAssets.Add(
                $"texture {rule.MaxTextureSize}: {rule.AssetPath}");
        }

        private static void DisableUnusedBaseRigAnimation(
            string modelPath,
            ICollection<string> changedAssets)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Model importer was not found for '{modelPath}'.");
            }
            if (!importer.importAnimation)
            {
                return;
            }

            Undo.RecordObject(importer, "Disable unused base-rig animation import");
            importer.importAnimation = false;
            importer.SaveAndReimport();
            changedAssets.Add("animation off: " + modelPath);
        }

        private readonly struct TextureImportRule
        {
            internal TextureImportRule(string assetPath, int maxTextureSize)
            {
                AssetPath = assetPath;
                MaxTextureSize = maxTextureSize;
            }

            internal string AssetPath { get; }

            internal int MaxTextureSize { get; }
        }
    }
}
