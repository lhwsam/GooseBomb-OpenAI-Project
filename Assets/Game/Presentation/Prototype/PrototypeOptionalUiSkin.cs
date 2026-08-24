using System;
using UnityEngine;

namespace BombSwap
{
    public enum PrototypeUiSpriteRole
    {
        LobbyBackground,
        NavigationArrowLeft,
        NavigationArrowRight,
        SettingsPanelFrame,
        SettingsButtonFrame,
        SettingsSliderBackground,
        SettingsSliderFill
    }

    [CreateAssetMenu(
        fileName = "ThirdPartyUiSkin",
        menuName = "Bomb Swap/UI/Optional UI Skin")]
    public sealed class PrototypeOptionalUiSkin : ScriptableObject
    {
        [Serializable]
        public struct SpriteEntry
        {
            [SerializeField]
            private PrototypeUiSpriteRole role;

            [SerializeField]
            private Sprite sprite;

            public SpriteEntry(PrototypeUiSpriteRole role, Sprite sprite)
            {
                this.role = role;
                this.sprite = sprite;
            }

            public PrototypeUiSpriteRole Role => role;

            public Sprite Sprite => sprite;
        }

        [SerializeField]
        private SpriteEntry[] entries = Array.Empty<SpriteEntry>();

        public int EntryCount => entries?.Length ?? 0;

        public bool HasValidEntries
        {
            get
            {
                if (entries == null || entries.Length == 0)
                {
                    return false;
                }

                for (int index = 0; index < entries.Length; index++)
                {
                    if (entries[index].Sprite == null)
                    {
                        return false;
                    }

                    for (int comparison = index + 1;
                         comparison < entries.Length;
                         comparison++)
                    {
                        if (entries[index].Role == entries[comparison].Role)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public bool TryGetSprite(
            PrototypeUiSpriteRole role,
            out Sprite sprite)
        {
            if (entries != null)
            {
                for (int index = 0; index < entries.Length; index++)
                {
                    if (entries[index].Role == role &&
                        entries[index].Sprite != null)
                    {
                        sprite = entries[index].Sprite;
                        return true;
                    }
                }
            }

            sprite = null;
            return false;
        }

        public void Configure(SpriteEntry[] authoredEntries)
        {
            entries = authoredEntries == null
                ? Array.Empty<SpriteEntry>()
                : (SpriteEntry[])authoredEntries.Clone();
        }
    }
}
