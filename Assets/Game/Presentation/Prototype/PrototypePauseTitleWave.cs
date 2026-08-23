using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class PrototypePauseTitleWave : MonoBehaviour
    {
        public const float DefaultAmplitude = 8f;
        public const float DefaultCycleDuration = 1f;
        public const float DefaultTrailingPauseSteps = 1f;

        [SerializeField]
        private TextMeshProUGUI target;

        [SerializeField, Min(0f)]
        private float amplitude = DefaultAmplitude;

        [SerializeField, Min(0.1f)]
        private float cycleDuration = DefaultCycleDuration;

        [SerializeField, Min(0f)]
        private float trailingPauseSteps = DefaultTrailingPauseSteps;

        private Tween _phaseTween;
        private float _normalizedPhase;
        private bool _isSubscribed;

        public TextMeshProUGUI Target => target;

        public float Amplitude => amplitude;

        public float CycleDuration => cycleDuration;

        public float TrailingPauseSteps => trailingPauseSteps;

        public bool IsAnimating =>
            _phaseTween != null &&
            _phaseTween.IsActive() &&
            _phaseTween.IsPlaying();

        public void Configure(
            TextMeshProUGUI authoredTarget,
            float authoredAmplitude = DefaultAmplitude,
            float authoredCycleDuration = DefaultCycleDuration,
            float authoredTrailingPauseSteps = DefaultTrailingPauseSteps)
        {
            if (authoredTarget == null)
            {
                throw new ArgumentNullException(nameof(authoredTarget));
            }
            if (authoredTarget.gameObject != gameObject)
            {
                throw new ArgumentException(
                    "Pause title wave target must be on the same GameObject.",
                    nameof(authoredTarget));
            }

            StopWaveAndRestore();
            target = authoredTarget;
            amplitude = Mathf.Max(0f, authoredAmplitude);
            cycleDuration = Mathf.Max(0.1f, authoredCycleDuration);
            trailingPauseSteps = Mathf.Max(0f, authoredTrailingPauseSteps);
            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartWave();
            }
        }

        private void Reset()
        {
            target = GetComponent<TextMeshProUGUI>();
        }

        private void Awake()
        {
            EnsureTarget();
        }

        private void OnEnable()
        {
            EnsureTarget();
            if (!Application.isPlaying)
            {
                return;
            }
            StartWave();
        }

        private void OnDisable()
        {
            StopWaveAndRestore();
        }

        private void OnDestroy()
        {
            StopTween();
            Unsubscribe();
        }

        private void EnsureTarget()
        {
            if (target == null)
            {
                target = GetComponent<TextMeshProUGUI>();
            }
        }

        private void StartWave()
        {
            StopTween();
            Subscribe();
            _normalizedPhase = 0f;

            if (target == null || amplitude <= 0f)
            {
                RequestVertexRefresh();
                return;
            }

            _phaseTween = DOTween.To(
                    () => _normalizedPhase,
                    SetNormalizedPhase,
                    1f,
                    cycleDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetTarget(this);
            RequestVertexRefresh();
        }

        private void StopWaveAndRestore()
        {
            StopTween();
            Unsubscribe();
            _normalizedPhase = 0f;
            RequestVertexRefresh();
        }

        private void StopTween()
        {
            if (_phaseTween == null)
            {
                return;
            }

            _phaseTween.Kill(false);
            _phaseTween = null;
        }

        private void Subscribe()
        {
            if (_isSubscribed || target == null)
            {
                return;
            }

            target.OnPreRenderText += ApplyWave;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (target != null)
            {
                target.OnPreRenderText -= ApplyWave;
            }
            _isSubscribed = false;
        }

        private void SetNormalizedPhase(float value)
        {
            _normalizedPhase = value;
            RequestVertexRefresh();
        }

        private void RequestVertexRefresh()
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying && target.gameObject.activeInHierarchy)
            {
                target.ForceMeshUpdate();
                return;
            }

            target.SetVerticesDirty();
        }

        private void ApplyWave(TMP_TextInfo textInfo)
        {
            int visibleCharacterCount = CountVisibleCharacters(textInfo);
            if (visibleCharacterCount == 0 || amplitude <= 0f)
            {
                return;
            }

            float timeline = _normalizedPhase *
                (visibleCharacterCount + trailingPauseSteps);
            int visibleIndex = 0;
            for (int index = 0; index < textInfo.characterCount; index++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[index];
                if (!character.isVisible)
                {
                    continue;
                }

                float characterProgress = timeline - visibleIndex;
                visibleIndex++;
                if (characterProgress < 0f || characterProgress > 1f)
                {
                    continue;
                }

                float offsetY = Mathf.Sin(characterProgress * Mathf.PI) *
                    amplitude;
                Vector3[] vertices =
                    textInfo.meshInfo[character.materialReferenceIndex].vertices;
                int vertexIndex = character.vertexIndex;
                vertices[vertexIndex].y += offsetY;
                vertices[vertexIndex + 1].y += offsetY;
                vertices[vertexIndex + 2].y += offsetY;
                vertices[vertexIndex + 3].y += offsetY;
            }
        }

        private static int CountVisibleCharacters(TMP_TextInfo textInfo)
        {
            int count = 0;
            for (int index = 0; index < textInfo.characterCount; index++)
            {
                if (textInfo.characterInfo[index].isVisible)
                {
                    count++;
                }
            }

            return count;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            amplitude = Mathf.Max(0f, amplitude);
            cycleDuration = Mathf.Max(0.1f, cycleDuration);
            trailingPauseSteps = Mathf.Max(0f, trailingPauseSteps);
            if (target == null)
            {
                target = GetComponent<TextMeshProUGUI>();
            }
        }
#endif
    }
}
