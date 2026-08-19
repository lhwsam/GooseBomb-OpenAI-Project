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
        private Transform chargerSpawn;

        [SerializeField]
        private Transform armoredSpawn;

        [SerializeField]
        private Transform selfDestructSpawn;

        [SerializeField]
        private Transform throwerSpawn;

        [SerializeField]
        private PrototypeCombatRoomDefinitionAsset roomDefinition;

        public BombSwapInputReader InputReader => inputReader;

        public Transform GridRoot => gridRoot;

        public Transform PlayerSpawn => playerSpawn;

        public Transform PlayerPlaceholder => playerPlaceholder;

        public Transform ChaserSpawn => chaserSpawn;

        public Transform ChargerSpawn => chargerSpawn;

        public Transform ArmoredSpawn => armoredSpawn;

        public Transform SelfDestructSpawn => selfDestructSpawn;

        public Transform ThrowerSpawn => throwerSpawn;

        public PrototypeCombatRoomDefinitionAsset RoomDefinition => roomDefinition;

        public int GridWidth => roomDefinition == null ? 0 : roomDefinition.GridWidth;

        public int GridDepth => roomDefinition == null ? 0 : roomDefinition.GridDepth;

        public float CellSize => roomDefinition == null ? 0f : roomDefinition.CellSize;

        public IReadOnlyList<Vector2Int> BlockedCells =>
            roomDefinition == null ? Array.Empty<Vector2Int>() : roomDefinition.IndestructibleWalls;

        public IReadOnlyList<Vector2Int> DestructibleCells =>
            roomDefinition == null ? Array.Empty<Vector2Int>() : roomDefinition.DestructibleWalls;

        public GridSpace GridSpace => new GridSpace(gridRoot.position, CellSize);

        public void Configure(
            BombSwapInputReader reader,
            Transform grid,
            Transform spawn,
            Transform player,
            Transform enemySpawn,
            PrototypeCombatRoomDefinitionAsset authoredRoomDefinition,
            Transform authoredChargerSpawn = null,
            Transform authoredArmoredSpawn = null,
            Transform authoredSelfDestructSpawn = null,
            Transform authoredThrowerSpawn = null)
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
            if (authoredRoomDefinition == null)
            {
                throw new ArgumentNullException(nameof(authoredRoomDefinition));
            }

            CombatRoomDefinition coreRoom = authoredRoomDefinition.CreateCoreDefinition();
            var gridSpace = new GridSpace(grid.position, authoredRoomDefinition.CellSize);
            ValidateTransformCell(gridSpace, spawn, coreRoom.PlayerSpawn, nameof(spawn));
            ValidateTransformCell(gridSpace, player, coreRoom.PlayerSpawn, nameof(player));
            ValidateTransformCell(gridSpace, enemySpawn, coreRoom.ChaserSpawn, nameof(enemySpawn));
            if (coreRoom.ChargerSpawn.HasValue)
            {
                if (authoredChargerSpawn == null)
                {
                    throw new ArgumentNullException(nameof(authoredChargerSpawn));
                }
                ValidateTransformCell(
                    gridSpace,
                    authoredChargerSpawn,
                    coreRoom.ChargerSpawn.Value,
                    nameof(authoredChargerSpawn));
            }
            else if (authoredChargerSpawn != null)
            {
                throw new ArgumentException(
                    "A charger spawn Transform requires an authored charger cell.",
                    nameof(authoredChargerSpawn));
            }
            if (coreRoom.ArmoredSpawn.HasValue)
            {
                if (authoredArmoredSpawn == null)
                {
                    throw new ArgumentNullException(nameof(authoredArmoredSpawn));
                }
                ValidateTransformCell(
                    gridSpace,
                    authoredArmoredSpawn,
                    coreRoom.ArmoredSpawn.Value,
                    nameof(authoredArmoredSpawn));
            }
            else if (authoredArmoredSpawn != null)
            {
                throw new ArgumentException(
                    "An armored spawn Transform requires an authored armored cell.",
                    nameof(authoredArmoredSpawn));
            }
            if (coreRoom.SelfDestructSpawn.HasValue)
            {
                if (authoredSelfDestructSpawn == null)
                {
                    throw new ArgumentNullException(nameof(authoredSelfDestructSpawn));
                }
                ValidateTransformCell(
                    gridSpace,
                    authoredSelfDestructSpawn,
                    coreRoom.SelfDestructSpawn.Value,
                    nameof(authoredSelfDestructSpawn));
            }
            else if (authoredSelfDestructSpawn != null)
            {
                throw new ArgumentException(
                    "A self-destruct spawn Transform requires an authored self-destruct cell.",
                    nameof(authoredSelfDestructSpawn));
            }
            if (coreRoom.ThrowerSpawn.HasValue)
            {
                if (authoredThrowerSpawn == null)
                {
                    throw new ArgumentNullException(nameof(authoredThrowerSpawn));
                }
                ValidateTransformCell(
                    gridSpace,
                    authoredThrowerSpawn,
                    coreRoom.ThrowerSpawn.Value,
                    nameof(authoredThrowerSpawn));
            }
            else if (authoredThrowerSpawn != null)
            {
                throw new ArgumentException(
                    "A thrower spawn Transform requires an authored thrower cell.",
                    nameof(authoredThrowerSpawn));
            }

            inputReader = reader;
            gridRoot = grid;
            playerSpawn = spawn;
            playerPlaceholder = player;
            chaserSpawn = enemySpawn;
            chargerSpawn = authoredChargerSpawn;
            armoredSpawn = authoredArmoredSpawn;
            selfDestructSpawn = authoredSelfDestructSpawn;
            throwerSpawn = authoredThrowerSpawn;
            roomDefinition = authoredRoomDefinition;
        }

        private static void ValidateTransformCell(
            GridSpace gridSpace,
            Transform target,
            GridPosition expected,
            string parameterName)
        {
            GridPosition actual = gridSpace.WorldToGrid(target.position);
            if (actual != expected)
            {
                throw new ArgumentException(
                    $"Transform cell {actual} must match authored room cell {expected}.",
                    parameterName);
            }
        }
    }
}
