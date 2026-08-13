using System;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;

namespace BombSwap.Tests.PlayMode
{
    public sealed class GridSpaceTests
    {
        [Test]
        public void GridToWorld_MapsXZAndPreservesOriginHeight()
        {
            var gridSpace = new GridSpace(new Vector3(10f, 2.5f, -4f), 1.5f);

            Vector3 world = gridSpace.GridToWorld(new GridPosition(-2, 3));

            Assert.That(world, Is.EqualTo(new Vector3(7f, 2.5f, 0.5f)));
        }

        [Test]
        public void WorldToGrid_MapsCellCenterBackToGridPosition()
        {
            var gridSpace = new GridSpace(new Vector3(10f, 2.5f, -4f), 1.5f);
            var expected = new GridPosition(-2, 3);

            GridPosition position = gridSpace.WorldToGrid(gridSpace.GridToWorld(expected));

            Assert.That(position, Is.EqualTo(expected));
        }

        [TestCase(-0.5001f, -1)]
        [TestCase(-0.5f, 0)]
        [TestCase(-0.4999f, 0)]
        [TestCase(0.4999f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(1.4999f, 1)]
        [TestCase(1.5f, 2)]
        public void WorldToGrid_UsesHalfOpenCellBoundaries(float worldX, int expectedX)
        {
            var gridSpace = new GridSpace(Vector3.zero, 1f);

            GridPosition position = gridSpace.WorldToGrid(new Vector3(worldX, 99f, 0f));

            Assert.That(position.X, Is.EqualTo(expectedX));
            Assert.That(position.Z, Is.Zero);
        }

        [Test]
        public void WorldToGrid_IgnoresWorldHeightForLogicalXZ()
        {
            var gridSpace = new GridSpace(new Vector3(2f, 10f, -3f), 2f);

            GridPosition low = gridSpace.WorldToGrid(new Vector3(4f, -100f, 1f));
            GridPosition high = gridSpace.WorldToGrid(new Vector3(4f, 100f, 1f));

            Assert.That(low, Is.EqualTo(new GridPosition(1, 2)));
            Assert.That(high, Is.EqualTo(low));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_RejectsInvalidCellSize(float cellSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GridSpace(Vector3.zero, cellSize));
        }

        [Test]
        public void Constructor_RejectsNonFiniteOrigin()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GridSpace(new Vector3(float.NaN, 0f, 0f), 1f));
        }

        [Test]
        public void WorldToGrid_RejectsNonFinitePosition()
        {
            var gridSpace = new GridSpace(Vector3.zero, 1f);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => gridSpace.WorldToGrid(new Vector3(0f, float.PositiveInfinity, 0f)));
        }

        [Test]
        public void WorldToGrid_RejectsCoordinatesOutsideIntegerGridRange()
        {
            var gridSpace = new GridSpace(Vector3.zero, 0.25f);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => gridSpace.WorldToGrid(new Vector3(float.MaxValue, 0f, 0f)));
        }

        [Test]
        public void GridToWorld_RejectsCoordinatesOutsideFiniteUnityRange()
        {
            var gridSpace = new GridSpace(Vector3.zero, float.MaxValue);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => gridSpace.GridToWorld(new GridPosition(2, 0)));
        }
    }
}
