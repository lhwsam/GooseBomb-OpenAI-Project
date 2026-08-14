using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [Serializable]
    public struct PrototypeRoomExitData
    {
        [SerializeField]
        private Vector2Int cell;

        [SerializeField]
        private RoomExitDirection direction;

        public PrototypeRoomExitData(Vector2Int authoredCell, RoomExitDirection authoredDirection)
        {
            cell = authoredCell;
            direction = authoredDirection;
        }

        public Vector2Int Cell => cell;

        public RoomExitDirection Direction => direction;

        public RoomExit CreateCoreExit()
        {
            return new RoomExit(new GridPosition(cell.x, cell.y), direction);
        }
    }

    [CreateAssetMenu(
        fileName = "PrototypeCombatRoom",
        menuName = "Bomb Swap/Prototype/Combat Room")]
    public sealed class PrototypeCombatRoomDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string roomId = "prototype-combat-loop";

        [SerializeField]
        private RoomType roomType = RoomType.Combat;

        [SerializeField]
        private int gridWidth = 11;

        [SerializeField]
        private int gridDepth = 9;

        [SerializeField]
        private float cellSize = 1f;

        [SerializeField]
        private Vector2Int playerSpawn;

        [SerializeField]
        private Vector2Int chaserSpawn = new Vector2Int(1, -1);

        [SerializeField]
        private bool hasCharger;

        [SerializeField]
        private Vector2Int chargerSpawn;

        [SerializeField]
        private Vector2Int[] indestructibleWalls = Array.Empty<Vector2Int>();

        [SerializeField]
        private Vector2Int[] destructibleWalls = Array.Empty<Vector2Int>();

        [SerializeField]
        private Vector2Int[] safePlayerCells = Array.Empty<Vector2Int>();

        [SerializeField]
        private Vector2Int[] retreatAnchors = Array.Empty<Vector2Int>();

        [SerializeField]
        private Vector2Int[] lureLoop = Array.Empty<Vector2Int>();

        [SerializeField]
        private PrototypeRoomExitData[] exits = Array.Empty<PrototypeRoomExitData>();

        public string RoomId => roomId;

        public RoomType RoomType => roomType;

        public int GridWidth => gridWidth;

        public int GridDepth => gridDepth;

        public float CellSize => cellSize;

        public Vector2Int PlayerSpawn => playerSpawn;

        public Vector2Int ChaserSpawn => chaserSpawn;

        public bool HasCharger => hasCharger;

        public Vector2Int ChargerSpawn => chargerSpawn;

        public IReadOnlyList<Vector2Int> IndestructibleWalls => indestructibleWalls;

        public IReadOnlyList<Vector2Int> DestructibleWalls => destructibleWalls;

        public IReadOnlyList<Vector2Int> SafePlayerCells => safePlayerCells;

        public IReadOnlyList<Vector2Int> RetreatAnchors => retreatAnchors;

        public IReadOnlyList<Vector2Int> LureLoop => lureLoop;

        public IReadOnlyList<PrototypeRoomExitData> Exits => exits;

        public void Configure(
            string authoredRoomId,
            RoomType authoredRoomType,
            int authoredGridWidth,
            int authoredGridDepth,
            float authoredCellSize,
            Vector2Int authoredPlayerSpawn,
            Vector2Int authoredChaserSpawn,
            Vector2Int[] authoredIndestructibleWalls,
            Vector2Int[] authoredSafePlayerCells,
            Vector2Int[] authoredRetreatAnchors,
            Vector2Int[] authoredLureLoop,
            PrototypeRoomExitData[] authoredExits,
            Vector2Int[] authoredDestructibleWalls = null,
            Vector2Int? authoredChargerSpawn = null)
        {
            ValidateFinitePositive(authoredCellSize, nameof(authoredCellSize));
            Vector2Int[] wallCopy = CloneRequired(
                authoredIndestructibleWalls,
                nameof(authoredIndestructibleWalls));
            Vector2Int[] safeCopy = CloneRequired(
                authoredSafePlayerCells,
                nameof(authoredSafePlayerCells));
            Vector2Int[] retreatCopy = CloneRequired(
                authoredRetreatAnchors,
                nameof(authoredRetreatAnchors));
            Vector2Int[] lureCopy = CloneRequired(authoredLureLoop, nameof(authoredLureLoop));
            PrototypeRoomExitData[] exitCopy = CloneRequired(authoredExits, nameof(authoredExits));
            Vector2Int[] destructibleWallCopy = CloneRequired(
                authoredDestructibleWalls ?? Array.Empty<Vector2Int>(),
                nameof(authoredDestructibleWalls));

            CreateCoreDefinition(
                authoredRoomId,
                authoredRoomType,
                authoredGridWidth,
                authoredGridDepth,
                authoredPlayerSpawn,
                authoredChaserSpawn,
                wallCopy,
                safeCopy,
                retreatCopy,
                lureCopy,
                exitCopy,
                destructibleWallCopy,
                authoredChargerSpawn);

            roomId = authoredRoomId;
            roomType = authoredRoomType;
            gridWidth = authoredGridWidth;
            gridDepth = authoredGridDepth;
            cellSize = authoredCellSize;
            playerSpawn = authoredPlayerSpawn;
            chaserSpawn = authoredChaserSpawn;
            hasCharger = authoredChargerSpawn.HasValue;
            chargerSpawn = authoredChargerSpawn ?? default;
            indestructibleWalls = wallCopy;
            destructibleWalls = destructibleWallCopy;
            safePlayerCells = safeCopy;
            retreatAnchors = retreatCopy;
            lureLoop = lureCopy;
            exits = exitCopy;
        }

        public CombatRoomDefinition CreateCoreDefinition()
        {
            ValidateFinitePositive(cellSize, nameof(cellSize));
            return CreateCoreDefinition(
                roomId,
                roomType,
                gridWidth,
                gridDepth,
                playerSpawn,
                chaserSpawn,
                indestructibleWalls,
                safePlayerCells,
                retreatAnchors,
                lureLoop,
                exits,
                destructibleWalls,
                hasCharger ? chargerSpawn : (Vector2Int?)null);
        }

        private static CombatRoomDefinition CreateCoreDefinition(
            string authoredRoomId,
            RoomType authoredRoomType,
            int authoredGridWidth,
            int authoredGridDepth,
            Vector2Int authoredPlayerSpawn,
            Vector2Int authoredChaserSpawn,
            IReadOnlyList<Vector2Int> authoredWalls,
            IReadOnlyList<Vector2Int> authoredSafeCells,
            IReadOnlyList<Vector2Int> authoredRetreatAnchors,
            IReadOnlyList<Vector2Int> authoredLureLoop,
            IReadOnlyList<PrototypeRoomExitData> authoredExits,
            IReadOnlyList<Vector2Int> authoredDestructibleWalls,
            Vector2Int? authoredChargerSpawn)
        {
            return new CombatRoomDefinition(
                new RoomDefinitionId(authoredRoomId),
                authoredRoomType,
                authoredGridWidth,
                authoredGridDepth,
                ToCorePosition(authoredPlayerSpawn),
                ToCorePosition(authoredChaserSpawn),
                ToCorePositions(authoredWalls),
                ToCorePositions(authoredSafeCells),
                ToCorePositions(authoredRetreatAnchors),
                ToCorePositions(authoredLureLoop),
                ToCoreExits(authoredExits),
                ToCorePositions(authoredDestructibleWalls),
                authoredChargerSpawn.HasValue
                    ? ToCorePosition(authoredChargerSpawn.Value)
                    : (GridPosition?)null);
        }

        private static GridPosition ToCorePosition(Vector2Int position)
        {
            return new GridPosition(position.x, position.y);
        }

        private static GridPosition[] ToCorePositions(IReadOnlyList<Vector2Int> positions)
        {
            if (positions == null)
            {
                throw new InvalidOperationException("Authored room cell list is missing.");
            }

            var result = new GridPosition[positions.Count];
            for (int index = 0; index < positions.Count; index++)
            {
                result[index] = ToCorePosition(positions[index]);
            }
            return result;
        }

        private static RoomExit[] ToCoreExits(IReadOnlyList<PrototypeRoomExitData> authoredExits)
        {
            if (authoredExits == null)
            {
                throw new InvalidOperationException("Authored room exit list is missing.");
            }

            var result = new RoomExit[authoredExits.Count];
            for (int index = 0; index < authoredExits.Count; index++)
            {
                result[index] = authoredExits[index].CreateCoreExit();
            }
            return result;
        }

        private static T[] CloneRequired<T>(T[] source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            return (T[])source.Clone();
        }

        private static void ValidateFinitePositive(float value, string parameterName)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and positive.");
            }
        }
    }
}
