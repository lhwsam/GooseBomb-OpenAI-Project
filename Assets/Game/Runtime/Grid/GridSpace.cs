using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    public readonly struct GridSpace
    {
        public GridSpace(Vector3 origin, float cellSize)
        {
            if (!IsFinite(origin.x) || !IsFinite(origin.y) || !IsFinite(origin.z))
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "Grid origin must be finite.");
            }

            if (!IsFinite(cellSize) || cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellSize),
                    cellSize,
                    "Grid cell size must be finite and greater than zero.");
            }

            Origin = origin;
            CellSize = cellSize;
        }

        public Vector3 Origin { get; }

        public float CellSize { get; }

        public Vector3 GridToWorld(GridPosition position)
        {
            return GridToWorld(GridSubcellPosition.AtCellCenter(position));
        }

        public Vector3 GridToWorld(GridSubcellPosition position)
        {
            double worldX = Origin.x + (position.X * CellSize);
            double worldZ = Origin.z + (position.Z * CellSize);

            if (worldX < -float.MaxValue || worldX > float.MaxValue ||
                worldZ < -float.MaxValue || worldZ > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position,
                    "Grid subcell position maps outside the supported Unity world coordinate range.");
            }

            return new Vector3((float)worldX, Origin.y, (float)worldZ);
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            ValidateWorldPosition(worldPosition);

            double normalizedX = ((double)worldPosition.x - Origin.x) / CellSize;
            double normalizedZ = ((double)worldPosition.z - Origin.z) / CellSize;

            return new GridPosition(
                RoundToNearestCell(normalizedX, nameof(worldPosition)),
                RoundToNearestCell(normalizedZ, nameof(worldPosition)));
        }

        private static int RoundToNearestCell(double normalizedCoordinate, string parameterName)
        {
            double shifted = normalizedCoordinate + 0.5d;
            double rounded = Math.Floor(shifted);

            if (rounded < int.MinValue || rounded > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    normalizedCoordinate,
                    "World position maps outside the supported grid coordinate range.");
            }

            return (int)rounded;
        }

        private static void ValidateWorldPosition(Vector3 worldPosition)
        {
            if (!IsFinite(worldPosition.x) ||
                !IsFinite(worldPosition.y) ||
                !IsFinite(worldPosition.z))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldPosition),
                    worldPosition,
                    "World position must be finite.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
