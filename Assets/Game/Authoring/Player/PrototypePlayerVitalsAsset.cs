using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypePlayerVitals",
        menuName = "Bomb Swap/Prototype/Player Vitals")]
    public sealed class PrototypePlayerVitalsAsset : ScriptableObject
    {
        [SerializeField]
        private int maxHealth = 5;

        [SerializeField]
        private float invulnerabilitySeconds = 0.75f;

        public int MaxHealth => maxHealth;

        public float InvulnerabilitySeconds => invulnerabilitySeconds;

        public void Configure(int authoredMaxHealth, float authoredInvulnerabilitySeconds)
        {
            if (authoredMaxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredMaxHealth),
                    authoredMaxHealth,
                    "Maximum health must be positive.");
            }
            ValidateFinitePositive(
                authoredInvulnerabilitySeconds,
                nameof(authoredInvulnerabilitySeconds));

            maxHealth = authoredMaxHealth;
            invulnerabilitySeconds = authoredInvulnerabilitySeconds;
        }

        public PlayerHealthDefinition CreateCoreDefinition()
        {
            if (maxHealth <= 0)
            {
                throw new InvalidOperationException("Authored maximum health must be positive.");
            }
            ValidateFinitePositive(invulnerabilitySeconds, nameof(invulnerabilitySeconds));

            return new PlayerHealthDefinition(
                maxHealth,
                TimeSpan.FromSeconds(invulnerabilitySeconds));
        }

        private static void ValidateFinitePositive(float value, string parameterName)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and positive.");
            }
        }
    }
}
