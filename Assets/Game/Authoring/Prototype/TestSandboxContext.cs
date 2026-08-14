using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class TestSandboxContext : MonoBehaviour
    {
        [SerializeField]
        private BombSwapInputReader inputReader;

        [SerializeField]
        private Transform gridRoot;

        [SerializeField]
        private Transform playerSpawn;

        [SerializeField]
        private Transform playerPlaceholder;

        [SerializeField]
        private Transform chaserSpawn;

        [SerializeField]
        private int gridWidth = 11;

        [SerializeField]
        private int gridDepth = 9;

        [SerializeField]
        private float cellSize = 1f;

        [SerializeField]
        private Vector2Int[] blockedCells = Array.Empty<Vector2Int>();

        public BombSwapInputReader InputReader => inputReader;

        public Transform GridRoot => gridRoot;

        public Transform PlayerSpawn => playerSpawn;

        public Transform PlayerPlaceholder => playerPlaceholder;

        public Transform ChaserSpawn => chaserSpawn;

        public int GridWidth => gridWidth;

        public int GridDepth => gridDepth;

        public float CellSize => cellSize;

        public IReadOnlyList<Vector2Int> BlockedCells => blockedCells;

        public GridSpace GridSpace => new GridSpace(gridRoot.position, cellSize);

        public void Configure(
            BombSwapInputReader reader,
            Transform grid,
            Transform spawn,
            Transform player,
            Transform enemySpawn,
            int width,
            int depth,
            float size,
            Vector2Int[] blockers)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }
            if (spawn == null)
            {
                throw new ArgumentNullException(nameof(spawn));
            }
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }
            if (enemySpawn == null)
            {
                throw new ArgumentNullException(nameof(enemySpawn));
            }
            if (width <= 0 || (width & 1) == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Grid width must be a positive odd number.");
            }
            if (depth <= 0 || (depth & 1) == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Grid depth must be a positive odd number.");
            }
            if (float.IsNaN(size) || float.IsInfinity(size) || size <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Cell size must be finite and positive.");
            }
            if (blockers == null)
            {
                throw new ArgumentNullException(nameof(blockers));
            }

            int halfWidth = width / 2;
            int halfDepth = depth / 2;
            var uniqueBlockers = new HashSet<Vector2Int>();
            var gridSpace = new GridSpace(grid.position, size);
            GridPosition spawnCell = gridSpace.WorldToGrid(spawn.position);
            GridPosition enemySpawnCell = gridSpace.WorldToGrid(enemySpawn.position);
            if (enemySpawnCell.X < -halfWidth || enemySpawnCell.X > halfWidth ||
                enemySpawnCell.Z < -halfDepth || enemySpawnCell.Z > halfDepth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemySpawn),
                    enemySpawnCell,
                    "Chaser spawn cell must be inside the TestSandbox grid.");
            }
            if (enemySpawnCell == spawnCell)
            {
                throw new ArgumentException(
                    "Player and chaser cannot share a spawn cell.",
                    nameof(enemySpawn));
            }
            foreach (Vector2Int blocker in blockers)
            {
                if (blocker.x < -halfWidth || blocker.x > halfWidth ||
                    blocker.y < -halfDepth || blocker.y > halfDepth)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(blockers),
                        blocker,
                        "Blocked cell must be inside the TestSandbox grid.");
                }
                if (!uniqueBlockers.Add(blocker))
                {
                    throw new ArgumentException($"Duplicate blocked cell: {blocker}.", nameof(blockers));
                }
                if (spawnCell == new GridPosition(blocker.x, blocker.y))
                {
                    throw new ArgumentException(
                        $"Player spawn cell cannot also be blocked: {blocker}.",
                        nameof(blockers));
                }
                if (enemySpawnCell == new GridPosition(blocker.x, blocker.y))
                {
                    throw new ArgumentException(
                        $"Chaser spawn cell cannot also be blocked: {blocker}.",
                        nameof(blockers));
                }
            }

            inputReader = reader;
            gridRoot = grid;
            playerSpawn = spawn;
            playerPlaceholder = player;
            chaserSpawn = enemySpawn;
            gridWidth = width;
            gridDepth = depth;
            cellSize = size;
            blockedCells = (Vector2Int[])blockers.Clone();
        }
    }
}
