using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class BombDefinitionTests
    {
        [Test]
        public void DefinitionId_UsesOrdinalValueEquality()
        {
            var first = new BombDefinitionId("basic-cross");
            var same = new BombDefinitionId("basic-cross");
            var differentCase = new BombDefinitionId("Basic-Cross");

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(differentCase));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void DefinitionId_RejectsMissingValue(string value)
        {
            Assert.Throws<ArgumentException>(() => new BombDefinitionId(value));
        }

        [TestCase(BombExplosionShape.Cross)]
        [TestCase(BombExplosionShape.SquareArea)]
        [TestCase(BombExplosionShape.ForwardLine)]
        public void Definition_AcceptsSupportedShapesWithPositiveFuseAndZeroRange(
            BombExplosionShape shape)
        {
            var definition = new BombDefinition(
                new BombDefinitionId("basic-cross"),
                shape,
                TimeSpan.FromSeconds(1),
                0);

            Assert.That(definition.Id, Is.EqualTo(new BombDefinitionId("basic-cross")));
            Assert.That(definition.ExplosionShape, Is.EqualTo(shape));
            Assert.That(definition.FuseDuration, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(definition.Range, Is.Zero);
        }

        [Test]
        public void Definition_RejectsDefaultId()
        {
            Assert.Throws<ArgumentException>(() => new BombDefinition(
                default,
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(1),
                1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveFuse(long ticks)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BombDefinition(
                new BombDefinitionId("basic-cross"),
                BombExplosionShape.Cross,
                TimeSpan.FromTicks(ticks),
                1));
        }

        [Test]
        public void Definition_RejectsNegativeRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BombDefinition(
                new BombDefinitionId("basic-cross"),
                BombExplosionShape.Cross,
                TimeSpan.FromSeconds(1),
                -1));
        }

        [Test]
        public void Definition_RejectsUnsupportedShape()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BombDefinition(
                new BombDefinitionId("unsupported"),
                (BombExplosionShape)99,
                TimeSpan.FromSeconds(1),
                1));
        }
    }
}
