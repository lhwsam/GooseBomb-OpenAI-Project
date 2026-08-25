using System;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeRewardChestView : MonoBehaviour
    {
        [SerializeField]
        private GameObject closedRoot;

        [SerializeField]
        private GameObject openRoot;

        public GameObject ClosedRoot => closedRoot;

        public GameObject OpenRoot => openRoot;

        public bool HasRequiredReferences =>
            closedRoot != null &&
            openRoot != null;

        public bool IsOpen =>
            openRoot != null &&
            openRoot.activeSelf &&
            closedRoot != null &&
            !closedRoot.activeSelf;

        public void Configure(
            GameObject authoredClosedRoot,
            GameObject authoredOpenRoot)
        {
            closedRoot = authoredClosedRoot ??
                throw new ArgumentNullException(nameof(authoredClosedRoot));
            openRoot = authoredOpenRoot ??
                throw new ArgumentNullException(nameof(authoredOpenRoot));
        }

        public void SetOpen(bool isOpen)
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Reward chest view requires closed and open roots.");
            }

            closedRoot.SetActive(!isOpen);
            openRoot.SetActive(isOpen);
        }
    }
}
