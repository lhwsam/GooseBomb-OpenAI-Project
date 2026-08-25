using System;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeWorldInteractableView : MonoBehaviour
    {
        [SerializeField]
        private GameObject persistentVisualRoot;

        [SerializeField]
        private GameObject availabilityEffectRoot;

        [SerializeField]
        private GameObject interactionPromptRoot;

        [SerializeField]
        private Transform dynamicContentAnchor;

        private Camera _cachedCamera;

        public GameObject PersistentVisualRoot => persistentVisualRoot;

        public GameObject AvailabilityEffectRoot => availabilityEffectRoot;

        public GameObject InteractionPromptRoot => interactionPromptRoot;

        public Transform DynamicContentAnchor => dynamicContentAnchor;

        public bool HasRequiredReferences =>
            persistentVisualRoot != null &&
            availabilityEffectRoot != null &&
            interactionPromptRoot != null;

        public bool IsVisualVisible =>
            persistentVisualRoot != null && persistentVisualRoot.activeSelf;

        public bool IsAvailabilityEffectVisible =>
            availabilityEffectRoot != null && availabilityEffectRoot.activeSelf;

        public bool IsInteractionPromptVisible =>
            interactionPromptRoot != null && interactionPromptRoot.activeSelf;

        public void Configure(
            GameObject authoredPersistentVisualRoot,
            GameObject authoredAvailabilityEffectRoot,
            GameObject authoredInteractionPromptRoot,
            Transform authoredDynamicContentAnchor = null)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeWorldInteractableView before changing its configuration.");
            }

            persistentVisualRoot = authoredPersistentVisualRoot ??
                throw new ArgumentNullException(nameof(authoredPersistentVisualRoot));
            availabilityEffectRoot = authoredAvailabilityEffectRoot ??
                throw new ArgumentNullException(nameof(authoredAvailabilityEffectRoot));
            interactionPromptRoot = authoredInteractionPromptRoot ??
                throw new ArgumentNullException(nameof(authoredInteractionPromptRoot));
            dynamicContentAnchor = authoredDynamicContentAnchor;
        }

        public void SetInteractionState(bool isAvailable, bool canInteract)
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "World interactable view is missing a persistent visual, availability effect, or prompt.");
            }

            persistentVisualRoot.SetActive(true);
            availabilityEffectRoot.SetActive(isAvailable);
            interactionPromptRoot.SetActive(isAvailable && canInteract);
        }

        private void LateUpdate()
        {
            if (interactionPromptRoot == null ||
                !interactionPromptRoot.activeInHierarchy)
            {
                return;
            }

            if (_cachedCamera == null || !_cachedCamera.isActiveAndEnabled)
            {
                _cachedCamera = Camera.main;
            }
            if (_cachedCamera != null)
            {
                interactionPromptRoot.transform.rotation =
                    _cachedCamera.transform.rotation;
            }
        }
    }
}
