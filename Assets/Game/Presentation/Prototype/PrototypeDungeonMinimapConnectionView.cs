using System;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonMinimapConnectionView : MonoBehaviour
    {
        [SerializeField]
        private Image connectionImage;

        public Image ConnectionImage => connectionImage;

        public bool HasRequiredReferences => connectionImage != null;

        public void BindAuthoredView(Image authoredConnectionImage)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Minimap connection view can only be authored outside Play Mode.");
            }

            connectionImage = authoredConnectionImage ??
                throw new ArgumentNullException(nameof(authoredConnectionImage));
        }
    }
}
