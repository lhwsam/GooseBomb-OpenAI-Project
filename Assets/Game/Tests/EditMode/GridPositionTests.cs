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

        [TestCase(0, 1, true)]
        [TestCase(1, 0, true)]
        [TestCase(0, -1, true)]
        [TestCase(-1, 0, true)]
        [TestCase(0, 0, false)]
        [TestCase(1, 1, false)]
        [TestCase(0, 2, false)]
        public void IsCardinallyAdjacentTo_UsesOneLogicalXZStep(
            int otherX,
            int otherZ,
            bool expected)
        {
            var origin = new GridPosition(0, 0);

            Assert.That(
                origin.IsCardinallyAdjacentTo(new GridPosition(otherX, otherZ)),
                Is.EqualTo(expected));
        }

        [Test]
        public void IsCardinallyAdjacentTo_ExtremeCoordinatesDoesNotOverflow()
        {
            var minimum = new GridPosition(int.MinValue, int.MinValue);
            var maximum = new GridPosition(int.MaxValue, int.MaxValue);

            Assert.That(minimum.IsCardinallyAdjacentTo(maximum), Is.False);
            Assert.That(
                minimum.IsCardinallyAdjacentTo(new GridPosition(int.MinValue + 1, int.MinValue)),
                Is.True);
        }
    }
}
