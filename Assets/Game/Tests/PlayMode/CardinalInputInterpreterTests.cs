using System;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;

namespace BombSwap.Tests.PlayMode
{
    public sealed class CardinalInputInterpreterTests
    {
        [TestCase(0f, 1f, CardinalDirection.North)]
        [TestCase(1f, 0f, CardinalDirection.East)]
        [TestCase(0f, -1f, CardinalDirection.South)]
        [TestCase(-1f, 0f, CardinalDirection.West)]
        [TestCase(0.49f, 0.49f, CardinalDirection.None)]
        public void Resolve_MapsActuatedVectorToCardinalDirection(
            float x,
            float y,
            CardinalDirection expected)
        {
            CardinalDirection direction = CardinalInputInterpreter.Resolve(
                new Vector2(x, y),
                CardinalDirection.None);

            Assert.That(direction, Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_PrefersDominantAxis()
        {
            Assert.That(
                CardinalInputInterpreter.Resolve(new Vector2(0.8f, 0.6f), CardinalDirection.North),
                Is.EqualTo(CardinalDirection.East));
            Assert.That(
                CardinalInputInterpreter.Resolve(new Vector2(-0.6f, -0.9f), CardinalDirection.West),
                Is.EqualTo(CardinalDirection.South));
        }

        [TestCase(1f, 1f, CardinalDirection.North, CardinalDirection.East)]
        [TestCase(-1f, 1f, CardinalDirection.North, CardinalDirection.West)]
        [TestCase(1f, -1f, CardinalDirection.South, CardinalDirection.East)]
        [TestCase(-1f, -1f, CardinalDirection.South, CardinalDirection.West)]
        [TestCase(1f, 1f, CardinalDirection.East, CardinalDirection.North)]
        [TestCase(1f, -1f, CardinalDirection.East, CardinalDirection.South)]
        [TestCase(-1f, 1f, CardinalDirection.West, CardinalDirection.North)]
        [TestCase(-1f, -1f, CardinalDirection.West, CardinalDirection.South)]
        public void Resolve_EqualDiagonalPrioritizesNewPerpendicularDirection(
            float x,
            float y,
            CardinalDirection previous,
            CardinalDirection expected)
        {
            Assert.That(
                CardinalInputInterpreter.Resolve(
                    new Vector2(x, y),
                    previous),
                Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_EqualDiagonalWithoutMatchingPreviousUsesVerticalTieBreak()
        {
            CardinalDirection direction = CardinalInputInterpreter.Resolve(
                new Vector2(-1f, 1f),
                CardinalDirection.South);

            Assert.That(direction, Is.EqualTo(CardinalDirection.North));
        }

        [Test]
        public void Resolve_RejectsNonFiniteVectorAndInvalidThreshold()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CardinalInputInterpreter.Resolve(
                    new Vector2(float.NaN, 0f),
                    CardinalDirection.None));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CardinalInputInterpreter.Resolve(
                    Vector2.one,
                    CardinalDirection.None,
                    0f));
        }
    }
}
