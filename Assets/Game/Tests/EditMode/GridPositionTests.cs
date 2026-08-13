using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class GridPositionTests
    {
        [Test]
        public void Constructor_PreservesSignedXZCoordinates()
        {
            var position = new GridPosition(-3, 7);

            Assert.That(position.X, Is.EqualTo(-3));
            Assert.That(position.Z, Is.EqualTo(7));
        }

        [Test]
        public void Offset_ReturnsNewPositionWithoutChangingOriginal()
        {
            var original = new GridPosition(2, -4);

            GridPosition moved = original.Offset(-5, 9);

            Assert.That(moved, Is.EqualTo(new GridPosition(-3, 5)));
            Assert.That(original, Is.EqualTo(new GridPosition(2, -4)));
        }

        [Test]
        public void EqualCoordinates_UseValueEqualityAndSameHashCode()
        {
            var first = new GridPosition(-8, 11);
            var second = new GridPosition(-8, 11);

            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
    }
}
