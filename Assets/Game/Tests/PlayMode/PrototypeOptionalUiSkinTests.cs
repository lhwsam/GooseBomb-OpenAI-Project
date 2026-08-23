using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeOptionalUiSkinTests
    {
        private GameObject _root;
        private Texture2D _texture;
        private Sprite _sprite;
        private PrototypeOptionalUiSkin _skin;

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
            if (_skin != null)
            {
                Object.DestroyImmediate(_skin);
            }
        }

        [Test]
        public void Applicator_UsesFunctionalFallbackWhenPackageIsMissing()
        {
            PrototypeOptionalUiSkinApplicator applicator =
                CreateApplicator(
                    out Image background,
                    out Image navigationArrow);

            applicator.ApplyPublicFallback();

            Assert.That(background.sprite, Is.Null);
            Assert.That(background.enabled, Is.True);
            Assert.That(navigationArrow.sprite, Is.Null);
            Assert.That(navigationArrow.enabled, Is.False);
            Assert.That(applicator.HasValidBindings, Is.True);
            Assert.That(applicator.UsesPublicFallback, Is.True);
        }

        [Test]
        public void Applicator_AppliesInstalledRoleWithoutHierarchyLookup()
        {
            PrototypeOptionalUiSkinApplicator applicator =
                CreateApplicator(
                    out Image background,
                    out Image navigationArrow);
            _texture = new Texture2D(2, 2);
            _sprite = Sprite.Create(
                _texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            _skin = ScriptableObject.CreateInstance<PrototypeOptionalUiSkin>();
            _skin.Configure(new[]
            {
                new PrototypeOptionalUiSkin.SpriteEntry(
                    PrototypeUiSpriteRole.NavigationArrowLeft,
                    _sprite)
            });

            applicator.ApplySkin(_skin);

            Assert.That(background.sprite, Is.Null);
            Assert.That(background.enabled, Is.True);
            Assert.That(navigationArrow.sprite, Is.SameAs(_sprite));
            Assert.That(navigationArrow.enabled, Is.True);
            Assert.That(applicator.UsesPublicFallback, Is.False);
        }

        private PrototypeOptionalUiSkinApplicator CreateApplicator(
            out Image background,
            out Image navigationArrow)
        {
            _root = new GameObject(
                "OptionalSkinRoot",
                typeof(RectTransform),
                typeof(PrototypeOptionalUiSkinApplicator));
            background = CreateImage("Background");
            navigationArrow = CreateImage("NavigationArrow");
            PrototypeOptionalUiSkinApplicator applicator =
                _root.GetComponent<PrototypeOptionalUiSkinApplicator>();
            applicator.ConfigureBindings(new[]
            {
                new PrototypeOptionalUiSkinApplicator.SpriteBinding(
                    PrototypeUiSpriteRole.LobbyBackground,
                    background,
                    false),
                new PrototypeOptionalUiSkinApplicator.SpriteBinding(
                    PrototypeUiSpriteRole.NavigationArrowLeft,
                    navigationArrow,
                    true)
            });
            return applicator;
        }

        private Image CreateImage(string objectName)
        {
            var child = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(_root.transform, false);
            return child.GetComponent<Image>();
        }
    }
}
