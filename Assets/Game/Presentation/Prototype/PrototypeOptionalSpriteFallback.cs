using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class PrototypeOptionalSpriteFallback : MonoBehaviour
    {
        [SerializeField]
        private Sprite fallbackSprite;

        [SerializeField]
        private bool hideWhenMissing;

        private Image _image;

        public Sprite FallbackSprite => fallbackSprite;

        public bool HideWhenMissing => hideWhenMissing;

        public Image TargetImage => ResolveImage();

        public bool HasValidConfiguration => ResolveImage() != null;

        public void Configure(Sprite authoredFallback, bool authoredHideWhenMissing)
        {
            fallbackSprite = authoredFallback;
            hideWhenMissing = authoredHideWhenMissing;
        }

        public void ApplyFallbackIfMissing()
        {
            Image image = ResolveImage();
            if (image == null)
            {
                return;
            }

            if (image.sprite != null)
            {
                image.enabled = true;
                return;
            }

            image.sprite = fallbackSprite;
            image.enabled = fallbackSprite != null || !hideWhenMissing;
        }

        private Image ResolveImage()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            return _image;
        }

        private void Awake()
        {
            ApplyFallbackIfMissing();
        }
    }
}
