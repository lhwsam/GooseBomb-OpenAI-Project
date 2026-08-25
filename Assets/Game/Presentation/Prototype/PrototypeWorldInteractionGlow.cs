using System;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public class PrototypeWorldInteractionGlow : MonoBehaviour
    {
        [SerializeField]
        private Transform core;

        [SerializeField]
        private Transform halo;

        [SerializeField]
        [Min(0.01f)]
        private float cyclesPerSecond = 0.65f;

        [SerializeField]
        [Range(0f, 0.5f)]
        private float coreScaleAmplitude = 0.06f;

        [SerializeField]
        [Range(0f, 0.5f)]
        private float haloScaleAmplitude = 0.12f;

        private Vector3 _coreBaseScale;
        private Vector3 _haloBaseScale;
        private bool _hasCapturedScale;

        public Transform Core => core;

        public Transform Halo => halo;

        public bool HasRequiredReferences => core != null && halo != null;

        public void Configure(
            Transform authoredCore,
            Transform authoredHalo,
            float authoredCyclesPerSecond = 0.65f,
            float authoredCoreScaleAmplitude = 0.06f,
            float authoredHaloScaleAmplitude = 0.12f)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable the world interaction glow before changing its configuration.");
            }
            if (authoredCyclesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredCyclesPerSecond),
                    authoredCyclesPerSecond,
                    "Glow cycles per second must be positive.");
            }
            if (authoredCoreScaleAmplitude < 0f ||
                authoredCoreScaleAmplitude > 0.5f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredCoreScaleAmplitude));
            }
            if (authoredHaloScaleAmplitude < 0f ||
                authoredHaloScaleAmplitude > 0.5f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredHaloScaleAmplitude));
            }

            core = authoredCore ??
                throw new ArgumentNullException(nameof(authoredCore));
            halo = authoredHalo ??
                throw new ArgumentNullException(nameof(authoredHalo));
            cyclesPerSecond = authoredCyclesPerSecond;
            coreScaleAmplitude = authoredCoreScaleAmplitude;
            haloScaleAmplitude = authoredHaloScaleAmplitude;
            CaptureBaseScale();
        }

        protected virtual void OnEnable()
        {
            CaptureBaseScale();
        }

        protected virtual void Update()
        {
            if (!_hasCapturedScale || !HasRequiredReferences)
            {
                return;
            }

            float phase = Time.unscaledTime * cyclesPerSecond * Mathf.PI * 2f;
            float wave = Mathf.Sin(phase);
            core.localScale = _coreBaseScale *
                (1f + wave * coreScaleAmplitude);
            halo.localScale = _haloBaseScale *
                (1f - wave * haloScaleAmplitude);
        }

        protected virtual void OnDisable()
        {
            if (!_hasCapturedScale)
            {
                return;
            }
            if (core != null)
            {
                core.localScale = _coreBaseScale;
            }
            if (halo != null)
            {
                halo.localScale = _haloBaseScale;
            }
        }

        private void CaptureBaseScale()
        {
            if (!HasRequiredReferences)
            {
                _hasCapturedScale = false;
                return;
            }

            _coreBaseScale = core.localScale;
            _haloBaseScale = halo.localScale;
            _hasCapturedScale = true;
        }
    }
}
