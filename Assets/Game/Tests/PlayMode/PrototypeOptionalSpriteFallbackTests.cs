using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeOptionalSpriteFallbackTests
    {
        private GameObject _root;
        private Texture2D _texture;
        private Sprite _sprite;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
            if (_sprite != null)
            {
                Object.DestroyImmediate(_sprite);
            }
            if (_texture != null)
            {
                Object.DestroyImmediate(_texture);
            }
        }

        [Test]
        public void MissingOptionalSprite_KeepsFunctionalImageEnabled()
        {
            PrototypeOptionalSpriteFallback fallback =
                CreateFallback(false, out Image image);

            fallback.ApplyFallbackIfMissing();

            Assert.That(image.sprite, Is.Null);
            Assert.That(image.enabled, Is.True);
            Assert.That(fallback.HasValidConfiguration, Is.True);
        }

        [Test]
        public void MissingOptionalSprite_HidesDecorativeImage()
        {
            PrototypeOptionalSpriteFallback fallback =
                CreateFallback(true, out Image image);

            fallback.ApplyFallbackIfMissing();

            Assert.That(image.sprite, Is.Null);
            Assert.That(image.enabled, Is.False);
        }

        [Test]
        public void ResolvedOptionalSprite_OverridesPreviousHiddenState()
        {
            PrototypeOptionalSpriteFallback fallback =
                CreateFallback(true, out Image image);
            _texture = new Texture2D(2, 2);
            _sprite = Sprite.Create(
                _texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            image.sprite = _sprite;
            image.enabled = false;

            fallback.ApplyFallbackIfMissing();

            Assert.That(image.sprite, Is.SameAs(_sprite));
            Assert.That(image.enabled, Is.True);
        }

        [Test]
        public void AuthoredFallbackSprite_IsUsedWhenOptionalSpriteIsMissing()
        {
            PrototypeOptionalSpriteFallback fallback =
                CreateFallback(false, out Image image);
            _texture = new Texture2D(2, 2);
            _sprite = Sprite.Create(
                _texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            fallback.Configure(_sprite, false);

            fallback.ApplyFallbackIfMissing();

            Assert.That(image.sprite, Is.SameAs(_sprite));
            Assert.That(image.enabled, Is.True);
        }

        private PrototypeOptionalSpriteFallback CreateFallback(
            bool hideWhenMissing,
            out Image image)
        {
            _root = new GameObject(
                "OptionalSprite",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(PrototypeOptionalSpriteFallback));
            image = _root.GetComponent<Image>();
            PrototypeOptionalSpriteFallback fallback =
                _root.GetComponent<PrototypeOptionalSpriteFallback>();
            fallback.Configure(null, hideWhenMissing);
            return fallback;
        }
    }
}
