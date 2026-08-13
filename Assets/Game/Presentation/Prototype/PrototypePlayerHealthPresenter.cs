using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerHealthPresenter : MonoBehaviour
    {
        public const float DefaultDamagePulseSeconds = 0.12f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Renderer targetRenderer;

        [SerializeField]
        private float damagePulseSeconds = DefaultDamagePulseSeconds;

        [SerializeField]
        private Color damageColor = new Color(1f, 0.12f, 0.04f, 1f);

        [SerializeField]
        private Color deadColor = new Color(0.16f, 0.17f, 0.2f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;
        private Color _normalColor;
        private float _damagePulseEndsAt;
        private bool _initialized;

        public PrototypeGameSession Session => session;

        public Renderer TargetRenderer => targetRenderer;

        public float DamagePulseSeconds => damagePulseSeconds;

        public int DisplayedHealth { get; private set; }

        public int DamagePulseCount { get; private set; }

        public bool IsDisplayingDeath { get; private set; }

        public Color CurrentColor { get; private set; }

        public void Configure(
            PrototypeGameSession gameSession,
            Renderer playerRenderer,
            float hitPulseSeconds = DefaultDamagePulseSeconds)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerHealthPresenter before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (playerRenderer == null)
            {
                throw new ArgumentNullException(nameof(playerRenderer));
            }
            ValidateFinitePositive(hitPulseSeconds, nameof(hitPulseSeconds));

            session = gameSession;
            targetRenderer = playerRenderer;
            damagePulseSeconds = hitPulseSeconds;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || targetRenderer == null)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerHealthPresenter requires session and renderer references.");
            }

            InitializeRenderer();
            session.PlayerDamaged += OnPlayerDamaged;
            session.PlayerDied += OnPlayerDied;
            session.Ready += OnSessionReady;
            if (session.IsInitialized)
            {
                SyncFromSession();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.PlayerDamaged -= OnPlayerDamaged;
                session.PlayerDied -= OnPlayerDied;
                session.Ready -= OnSessionReady;
            }
            if (_initialized && targetRenderer != null)
            {
                ApplyColor(_normalColor);
            }

            _initialized = false;
            IsDisplayingDeath = false;
        }

        private void Update()
        {
            if (!_initialized || IsDisplayingDeath || Time.unscaledTime < _damagePulseEndsAt)
            {
                return;
            }

            if (CurrentColor != _normalColor)
            {
                ApplyColor(_normalColor);
            }
        }

        private void OnSessionReady()
        {
            SyncFromSession();
        }

        private void OnPlayerDamaged(PlayerDamageResult result)
        {
            DisplayedHealth = result.CurrentHealth;
            DamagePulseCount++;
            _damagePulseEndsAt = Time.unscaledTime + damagePulseSeconds;
            ApplyColor(damageColor);
        }

        private void OnPlayerDied(PlayerDamageResult result)
        {
            DisplayedHealth = result.CurrentHealth;
            IsDisplayingDeath = true;
            ApplyColor(deadColor);
        }

        private void SyncFromSession()
        {
            DisplayedHealth = session.CurrentHealth;
            if (session.IsPlayerDead)
            {
                IsDisplayingDeath = true;
                ApplyColor(deadColor);
            }
        }

        private void InitializeRenderer()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            Material material = targetRenderer.sharedMaterial;
            if (material == null)
            {
                throw new InvalidOperationException(
                    "Player health presenter target requires a shared material.");
            }

            if (material.HasProperty(BaseColorId))
            {
                _colorPropertyId = BaseColorId;
            }
            else if (material.HasProperty(ColorId))
            {
                _colorPropertyId = ColorId;
            }
            else
            {
                throw new InvalidOperationException(
                    "Player material requires a supported color property.");
            }

            _normalColor = material.GetColor(_colorPropertyId);
            CurrentColor = _normalColor;
            _initialized = true;
        }

        private void ApplyColor(Color color)
        {
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_colorPropertyId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
            CurrentColor = color;
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
