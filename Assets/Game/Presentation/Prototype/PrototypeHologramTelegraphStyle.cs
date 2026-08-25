using System;
using UnityEngine;

namespace BombSwap
{
    internal static class PrototypeHologramTelegraphStyle
    {
        public const float EmissionScale = 3.5f;

        public static readonly Color HologramColor =
            new Color(1f, 0.12f, 0.015f, 0.14f);
        public static readonly Color TextureTint =
            new Color(1f, 0.02f, 0f, 0.065f);

        private static readonly int HologramColorId =
            Shader.PropertyToID("_Hologram_Color");
        private static readonly int TextureTintColorId =
            Shader.PropertyToID("_Texture_Tint_Color");
        private static readonly int EmissionScaleId =
            Shader.PropertyToID("_Emission_Scale");

        public static bool Apply(GameObject visualRoot, Material hologramMaterial)
        {
            if (visualRoot == null)
            {
                throw new ArgumentNullException(nameof(visualRoot));
            }
            if (hologramMaterial == null)
            {
                return false;
            }

            Renderer[] renderers =
                visualRoot.GetComponentsInChildren<Renderer>(true);
            bool applied = false;
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (!(renderer is MeshRenderer) &&
                    !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    continue;
                }
                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    materials[materialIndex] = hologramMaterial;
                }
                renderer.sharedMaterials = materials;

                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(HologramColorId, HologramColor);
                propertyBlock.SetColor(TextureTintColorId, TextureTint);
                propertyBlock.SetFloat(EmissionScaleId, EmissionScale);
                renderer.SetPropertyBlock(propertyBlock);
                applied = true;
            }

            return applied;
        }
    }
}
