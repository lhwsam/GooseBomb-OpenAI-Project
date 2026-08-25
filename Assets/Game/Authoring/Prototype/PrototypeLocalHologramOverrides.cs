using System;
using UnityEngine;

namespace BombSwap
{
    public sealed class PrototypeLocalHologramOverrides : ScriptableObject
    {
        public const string ResourcesLoadPath = "BombSwapLocalHologramOverrides";

        [SerializeField]
        private Material actorHologramMaterial;

        [SerializeField]
        private Material bombRangeHologramMaterial;

        public Material ActorHologramMaterial => actorHologramMaterial;

        public Material BombRangeHologramMaterial => bombRangeHologramMaterial;

        public void Configure(
            Material authoredActorHologramMaterial,
            Material authoredBombRangeHologramMaterial)
        {
            ValidateMaterial(
                authoredActorHologramMaterial,
                nameof(authoredActorHologramMaterial));
            ValidateMaterial(
                authoredBombRangeHologramMaterial,
                nameof(authoredBombRangeHologramMaterial));

            actorHologramMaterial = authoredActorHologramMaterial;
            bombRangeHologramMaterial = authoredBombRangeHologramMaterial;
        }

        public void ValidateConfiguration()
        {
            ValidateMaterial(actorHologramMaterial, nameof(actorHologramMaterial));
            ValidateMaterial(
                bombRangeHologramMaterial,
                nameof(bombRangeHologramMaterial));
        }

        public static PrototypeLocalHologramOverrides LoadOptional()
        {
            return Resources.Load<PrototypeLocalHologramOverrides>(
                ResourcesLoadPath);
        }

        private static void ValidateMaterial(Material material, string parameterName)
        {
            if (material == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (material.shader == null)
            {
                throw new ArgumentException(
                    "A local hologram override requires a material with a shader.",
                    parameterName);
            }
        }
    }
}
