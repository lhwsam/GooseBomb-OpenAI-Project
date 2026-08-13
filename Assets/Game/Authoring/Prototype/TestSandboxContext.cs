using System;
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
        private int gridWidth = 11;

        [SerializeField]
        private int gridDepth = 9;

        [SerializeField]
        private float cellSize = 1f;

        public BombSwapInputReader InputReader => inputReader;

        public Transform GridRoot => gridRoot;

        public Transform PlayerSpawn => playerSpawn;

        public Transform PlayerPlaceholder => playerPlaceholder;

        public int GridWidth => gridWidth;

        public int GridDepth => gridDepth;

        public float CellSize => cellSize;

        public GridSpace GridSpace => new GridSpace(gridRoot.position, cellSize);

        public void Configure(
            BombSwapInputReader reader,
            Transform grid,
            Transform spawn,
            Transform player,
            int width,
            int depth,
            float size)
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

            inputReader = reader;
            gridRoot = grid;
            playerSpawn = spawn;
            playerPlaceholder = player;
            gridWidth = width;
            gridDepth = depth;
            cellSize = size;
        }
    }
}
