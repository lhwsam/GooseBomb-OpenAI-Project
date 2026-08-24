using System;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeOptionalUiSkinApplicator : MonoBehaviour
    {
        public const string DefaultResourcesPath =
            "BombSwap/ThirdPartyUiSkin";

        [Serializable]
        public struct SpriteBinding
        {
            [SerializeField]
            private PrototypeUiSpriteRole role;

            [SerializeField]
            private Image target;

            [SerializeField]
            private bool hideWhenMissing;

            public SpriteBinding(
                PrototypeUiSpriteRole role,
                Image target,
                bool hideWhenMissing)
            {
                this.role = role;
                this.target = target;
                this.hideWhenMissing = hideWhenMissing;
            }

            public PrototypeUiSpriteRole Role => role;

            public Image Target => target;

            public bool HideWhenMissing => hideWhenMissing;
        }

        [SerializeField]
        private SpriteBinding[] bindings = Array.Empty<SpriteBinding>();

        public int BindingCount => bindings?.Length ?? 0;

        public bool HasValidBindings
        {
            get
            {
                if (bindings == null || bindings.Length == 0)
                {
                    return false;
                }

                for (int index = 0; index < bindings.Length; index++)
                {
                    Image target = bindings[index].Target;
                    if (target == null ||
                        !target.transform.IsChildOf(transform))
                    {
                        return false;
                    }

                    for (int comparison = index + 1;
                         comparison < bindings.Length;
                         comparison++)
                    {
                        if (target == bindings[comparison].Target)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public bool UsesPublicFallback
        {
            get
            {
                if (!HasValidBindings)
                {
                    return false;
                }

                for (int index = 0; index < bindings.Length; index++)
                {
                    SpriteBinding binding = bindings[index];
                    if (binding.Target.sprite != null ||
                        binding.Target.enabled == binding.HideWhenMissing)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public SpriteBinding GetBinding(int index)
        {
            if (bindings == null ||
                index < 0 ||
                index >= bindings.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return bindings[index];
        }

        public void ConfigureBindings(SpriteBinding[] authoredBindings)
        {
            bindings = authoredBindings == null
                ? Array.Empty<SpriteBinding>()
                : (SpriteBinding[])authoredBindings.Clone();
        }

        public void ApplyInstalledSkin()
        {
            PrototypeOptionalUiSkin skin =
                Resources.Load<PrototypeOptionalUiSkin>(
                    DefaultResourcesPath);
            ApplySkin(skin);
        }

        public void ApplySkin(PrototypeOptionalUiSkin skin)
        {
            if (bindings == null)
            {
                return;
            }

            for (int index = 0; index < bindings.Length; index++)
            {
                SpriteBinding binding = bindings[index];
                Image target = binding.Target;
                if (target == null)
                {
                    continue;
                }

                Sprite sprite = null;
                bool hasSprite = skin != null &&
                    skin.TryGetSprite(binding.Role, out sprite);
                target.sprite = hasSprite ? sprite : null;
                target.enabled = hasSprite || !binding.HideWhenMissing;
            }
        }

        public void ApplyPublicFallback()
        {
            ApplySkin(null);
        }

        private void Awake()
        {
            ApplyInstalledSkin();
        }
    }
}
