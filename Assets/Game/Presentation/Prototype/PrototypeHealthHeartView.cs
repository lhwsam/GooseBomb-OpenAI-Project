using System;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHealthHeartView : MonoBehaviour
    {
        [SerializeField]
        private Image fullVisual;

        [SerializeField]
        private Image emptyVisual;

        [SerializeField]
        private bool previewFilled = true;

        public Image FullVisual => fullVisual;

        public Image EmptyVisual => emptyVisual;

        public bool PreviewFilled => previewFilled;

        public bool IsFilled { get; private set; }

        public bool HasRequiredReferences =>
            fullVisual != null &&
            emptyVisual != null &&
            fullVisual != emptyVisual;

        public void BindAuthoredView(
            Image authoredFullVisual,
            Image authoredEmptyVisual,
            bool authoredPreviewFilled = true)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Health heart view can only be authored outside Play Mode.");
            }

            fullVisual = authoredFullVisual ??
                throw new ArgumentNullException(nameof(authoredFullVisual));
            emptyVisual = authoredEmptyVisual ??
                throw new ArgumentNullException(nameof(authoredEmptyVisual));
            previewFilled = authoredPreviewFilled;
            ApplyState(previewFilled);
        }

        public void SetFilled(bool filled)
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Health heart view is missing its full or empty visual.");
            }

            ApplyState(filled);
        }

        public void SetAuthoredPreviewFilled(bool filled)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Health heart preview can only be authored outside Play Mode.");
            }

            previewFilled = filled;
            ApplyState(previewFilled);
        }

        private void Awake()
        {
            if (HasRequiredReferences)
            {
                ApplyState(previewFilled);
            }
        }

        private void ApplyState(bool filled)
        {
            IsFilled = filled;
            if (fullVisual != null)
            {
                fullVisual.gameObject.SetActive(filled);
            }
            if (emptyVisual != null)
            {
                emptyVisual.gameObject.SetActive(!filled);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && HasRequiredReferences)
            {
                ApplyState(previewFilled);
            }
        }
#endif
    }
}
