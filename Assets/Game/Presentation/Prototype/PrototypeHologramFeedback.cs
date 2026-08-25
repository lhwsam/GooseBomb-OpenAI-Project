using System;
using System.Linq;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHologramFeedback : MonoBehaviour
    {
        public const int DefaultHitBlinkCount = 2;
        public const float DefaultHitToggleSeconds = 0.065f;

        public static readonly Color HitColor =
            new Color(1f, 0.015f, 0.025f, 0.48f);
        public static readonly Color HitTextureTint =
            new Color(1f, 0f, 0.01f, 0.25f);
        public static readonly Color WarningColor =
            new Color(1f, 0.055f, 0.01f, 0.34f);
        public static readonly Color WarningTextureTint =
            new Color(1f, 0.01f, 0f, 0.17f);

        private static readonly int HologramColorId =
            Shader.PropertyToID("_Hologram_Color");
        private static readonly int TextureTintColorId =
            Shader.PropertyToID("_Texture_Tint_Color");
        private static readonly int EmissionScaleId =
            Shader.PropertyToID("_Emission_Scale");

        private RendererState[] _rendererStates = Array.Empty<RendererState>();
        private float _toggleSeconds;
        private float _loopToggleSeconds;
        private float _phaseRemaining;
        private int _blinkCount;
        private int _phaseIndex;
        private FeedbackMode _mode;
        private bool _isPaused;
        private bool _isShowingHologram;
        private bool _resumeLoopAfterHit;
        private bool _configured;

        public bool IsHitBlinkActive => _mode == FeedbackMode.HitBlink;

        public bool IsLooping => _mode == FeedbackMode.Looping;

        public bool IsShowingHologram => _isShowingHologram;

        public int HitBlinkTriggerCount { get; private set; }

        public static PrototypeHologramFeedback CreateHitFeedback(
            GameObject visualRoot)
        {
            PrototypeLocalHologramOverrides overrides =
                PrototypeLocalHologramOverrides.LoadOptional();
            return overrides == null
                ? null
                : CreateForRoot(
                    visualRoot,
                    overrides.ActorHologramMaterial,
                    HitColor,
                    HitTextureTint,
                    6f);
        }

        public static PrototypeHologramFeedback CreateWarningFeedback(
            GameObject visualRoot)
        {
            PrototypeLocalHologramOverrides overrides =
                PrototypeLocalHologramOverrides.LoadOptional();
            return overrides == null
                ? null
                : CreateForRoot(
                    visualRoot,
                    overrides.ActorHologramMaterial,
                    WarningColor,
                    WarningTextureTint,
                    5f);
        }

        public static PrototypeHologramFeedback CreateForRoot(
            GameObject visualRoot,
            Material hologramMaterial,
            Color hologramColor,
            Color textureTintColor,
            float emissionScale)
        {
            if (visualRoot == null)
            {
                throw new ArgumentNullException(nameof(visualRoot));
            }
            if (hologramMaterial == null)
            {
                return null;
            }

            Renderer[] renderers = visualRoot
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToArray();
            return Create(
                visualRoot,
                renderers,
                hologramMaterial,
                hologramColor,
                textureTintColor,
                emissionScale);
        }

        public static PrototypeHologramFeedback CreateForRenderer(
            Renderer targetRenderer,
            Material hologramMaterial,
            Color hologramColor,
            Color textureTintColor,
            float emissionScale)
        {
            if (targetRenderer == null)
            {
                throw new ArgumentNullException(nameof(targetRenderer));
            }
            if (hologramMaterial == null)
            {
                return null;
            }

            return Create(
                targetRenderer.gameObject,
                new[] { targetRenderer },
                hologramMaterial,
                hologramColor,
                textureTintColor,
                emissionScale);
        }

        public void TriggerHitBlink(
            int blinkCount = DefaultHitBlinkCount,
            float toggleSeconds = DefaultHitToggleSeconds)
        {
            ValidateTiming(blinkCount, toggleSeconds);
            EnsureConfigured();

            _resumeLoopAfterHit =
                _mode == FeedbackMode.Looping || _resumeLoopAfterHit;
            _mode = FeedbackMode.HitBlink;
            _blinkCount = blinkCount;
            _phaseIndex = 0;
            _toggleSeconds = toggleSeconds;
            _phaseRemaining = toggleSeconds;
            HitBlinkTriggerCount++;
            SetHologramVisible(true);
        }

        public void StartLooping(float toggleSeconds)
        {
            ValidateFinitePositive(toggleSeconds, nameof(toggleSeconds));
            EnsureConfigured();

            _loopToggleSeconds = toggleSeconds;
            _resumeLoopAfterHit = true;
            if (_mode != FeedbackMode.HitBlink)
            {
                BeginLooping();
            }
        }

        public void StopAndRestore()
        {
            _mode = FeedbackMode.None;
            _phaseRemaining = 0f;
            _phaseIndex = 0;
            _resumeLoopAfterHit = false;
            SetHologramVisible(false);
        }

        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
        }

        private static PrototypeHologramFeedback Create(
            GameObject owner,
            Renderer[] renderers,
            Material hologramMaterial,
            Color hologramColor,
            Color textureTintColor,
            float emissionScale)
        {
            if (renderers == null || renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Hologram feedback root '{owner.name}' requires a mesh renderer.");
            }
            ValidateFinitePositive(emissionScale, nameof(emissionScale));

            PrototypeHologramFeedback feedback =
                owner.GetComponent<PrototypeHologramFeedback>();
            if (feedback == null)
            {
                feedback = owner.AddComponent<PrototypeHologramFeedback>();
            }
            feedback.Configure(
                renderers,
                hologramMaterial,
                hologramColor,
                textureTintColor,
                emissionScale);
            return feedback;
        }

        private void Configure(
            Renderer[] renderers,
            Material hologramMaterial,
            Color hologramColor,
            Color textureTintColor,
            float emissionScale)
        {
            if (_configured)
            {
                throw new InvalidOperationException(
                    "PrototypeHologramFeedback is already configured.");
            }
            if (hologramMaterial == null)
            {
                throw new ArgumentNullException(nameof(hologramMaterial));
            }

            _rendererStates = new RendererState[renderers.Length];
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                {
                    throw new InvalidOperationException(
                        $"Hologram feedback renderer {index} is missing.");
                }

                Material[] originals = renderer.sharedMaterials;
                if (originals.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Hologram feedback renderer '{renderer.name}' has no materials.");
                }

                Material[] holograms = new Material[originals.Length];
                for (int materialIndex = 0;
                     materialIndex < holograms.Length;
                     materialIndex++)
                {
                    holograms[materialIndex] = hologramMaterial;
                }

                var originalBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(originalBlock);
                var hologramBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(hologramBlock);
                hologramBlock.SetColor(HologramColorId, hologramColor);
                hologramBlock.SetColor(TextureTintColorId, textureTintColor);
                hologramBlock.SetFloat(EmissionScaleId, emissionScale);
                _rendererStates[index] = new RendererState(
                    renderer,
                    originals,
                    holograms,
                    originalBlock,
                    hologramBlock);
            }

            _configured = true;
        }

        private void Update()
        {
            if (_isPaused || _mode == FeedbackMode.None)
            {
                return;
            }

            Advance(Time.unscaledDeltaTime);
        }

        internal void Advance(float elapsedSeconds)
        {
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) ||
                float.IsInfinity(elapsedSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "Elapsed time must be finite and non-negative.");
            }
            if (_isPaused || _mode == FeedbackMode.None)
            {
                return;
            }

            _phaseRemaining -= elapsedSeconds;
            while (_mode != FeedbackMode.None && _phaseRemaining <= 0f)
            {
                if (_mode == FeedbackMode.HitBlink)
                {
                    _phaseIndex++;
                    if (_phaseIndex >= _blinkCount * 2)
                    {
                        if (_resumeLoopAfterHit)
                        {
                            BeginLooping();
                            return;
                        }
                        StopAndRestore();
                        return;
                    }
                    SetHologramVisible((_phaseIndex & 1) == 0);
                }
                else
                {
                    SetHologramVisible(!_isShowingHologram);
                }
                _phaseRemaining += _toggleSeconds;
            }
        }

        private void BeginLooping()
        {
            _mode = FeedbackMode.Looping;
            _toggleSeconds = _loopToggleSeconds;
            _phaseRemaining = _loopToggleSeconds;
            SetHologramVisible(true);
        }

        private void OnDisable()
        {
            if (_configured)
            {
                StopAndRestore();
            }
        }

        private void SetHologramVisible(bool isVisible)
        {
            if (!_configured || _isShowingHologram == isVisible)
            {
                return;
            }

            for (int index = 0; index < _rendererStates.Length; index++)
            {
                RendererState state = _rendererStates[index];
                if (state.Renderer == null)
                {
                    continue;
                }

                state.Renderer.sharedMaterials = isVisible
                    ? state.HologramMaterials
                    : state.OriginalMaterials;
                state.Renderer.SetPropertyBlock(
                    isVisible ? state.HologramBlock : state.OriginalBlock);
            }
            _isShowingHologram = isVisible;
        }

        private void EnsureConfigured()
        {
            if (!_configured)
            {
                throw new InvalidOperationException(
                    "PrototypeHologramFeedback must be configured before use.");
            }
        }

        private static void ValidateTiming(int blinkCount, float toggleSeconds)
        {
            if (blinkCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blinkCount),
                    blinkCount,
                    "Blink count must be positive.");
            }
            ValidateFinitePositive(toggleSeconds, nameof(toggleSeconds));
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

        private sealed class RendererState
        {
            public RendererState(
                Renderer renderer,
                Material[] originalMaterials,
                Material[] hologramMaterials,
                MaterialPropertyBlock originalBlock,
                MaterialPropertyBlock hologramBlock)
            {
                Renderer = renderer;
                OriginalMaterials = originalMaterials;
                HologramMaterials = hologramMaterials;
                OriginalBlock = originalBlock;
                HologramBlock = hologramBlock;
            }

            public Renderer Renderer { get; }

            public Material[] OriginalMaterials { get; }

            public Material[] HologramMaterials { get; }

            public MaterialPropertyBlock OriginalBlock { get; }

            public MaterialPropertyBlock HologramBlock { get; }
        }

        private enum FeedbackMode
        {
            None = 0,
            HitBlink = 1,
            Looping = 2,
        }
    }
}
