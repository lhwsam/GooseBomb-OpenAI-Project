using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class CombatRoomDefinition
    {
        private static readonly GridPosition[] CardinalOffsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0),
        };

        private readonly HashSet<GridPosition> _indestructibleWallSet;
        private readonly HashSet<GridPosition> _destructibleWallSet;
        private readonly HashSet<GridPosition> _blockedSet;

        public CombatRoomDefinition(
            RoomDefinitionId id,
            RoomType roomType,
            int width,
            int depth,
            GridPosition playerSpawn,
            GridPosition chaserSpawn,
            IReadOnlyList<GridPosition> indestructibleWalls,
            IReadOnlyList<GridPosition> safePlayerCells,
            IReadOnlyList<GridPosition> retreatAnchors,
            IReadOnlyList<GridPosition> lureLoop,
            IReadOnlyList<RoomExit> exits,
            IReadOnlyList<GridPosition> destructibleWalls = null,
            GridPosition? chargerSpawn = null,
            GridPosition? armoredSpawn = null,
            GridPosition? selfDestructSpawn = null,
            IReadOnlyList<GridPosition> selfDestructAnchors = null)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Room definition ID must be valid.", nameof(id));
            }
            if (roomType != RoomType.Combat)
            {
                throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type.");
            }
            ValidateDimension(width, nameof(width));
            ValidateDimension(depth, nameof(depth));

            Id = id;
            RoomType = roomType;
            Width = width;
            Depth = depth;
            PlayerSpawn = playerSpawn;
            ChaserSpawn = chaserSpawn;

            GridPosition[] wallArray = CopyUniquePositions(
                indestructibleWalls,
                nameof(indestructibleWalls),
                false);
            GridPosition[] destructibleWallArray = CopyUniquePositions(
                destructibleWalls ?? Array.Empty<GridPosition>(),
                nameof(destructibleWalls),
                false);
            _indestructibleWallSet = new HashSet<GridPosition>(wallArray);
            _destructibleWallSet = new HashSet<GridPosition>(destructibleWallArray);
            foreach (GridPosition position in destructibleWallArray)
            {
                if (_indestructibleWallSet.Contains(position))
                {
                    throw new ArgumentException(
                        $"Destructible wall {position} overlaps an indestructible wall.",
                        nameof(destructibleWalls));
                }
            }
            _blockedSet = new HashSet<GridPosition>(_indestructibleWallSet);
            _blockedSet.UnionWith(_destructibleWallSet);
            GridPosition[] safeArray = CopyUniquePositions(
                safePlayerCells,
                nameof(safePlayerCells),
                true);
            GridPosition[] retreatArray = CopyUniquePositions(
                retreatAnchors,
                nameof(retreatAnchors),
                true);
            GridPosition[] lureArray = CopyUniquePositions(lureLoop, nameof(lureLoop), true);
            GridPosition[] selfDestructAnchorArray = CopyUniquePositions(
                selfDestructAnchors ?? Array.Empty<GridPosition>(),
                nameof(selfDestructAnchors),
                selfDestructSpawn.HasValue);
            RoomExit[] exitArray = CopyExits(exits);

            ValidateTraversableCell(playerSpawn, nameof(playerSpawn));
            ValidateTraversableCell(chaserSpawn, nameof(chaserSpawn));
            if (playerSpawn == chaserSpawn)
            {
                throw new ArgumentException("Player and chaser spawn cells must be different.");
            }
            if (playerSpawn.IsCardinallyAdjacentTo(chaserSpawn))
            {
                throw new ArgumentException("Player and chaser cannot begin in cardinal contact.");
            }
            if (chargerSpawn.HasValue)
            {
                GridPosition authoredChargerSpawn = chargerSpawn.Value;
                ValidateTraversableCell(authoredChargerSpawn, nameof(chargerSpawn));
                if (authoredChargerSpawn == playerSpawn || authoredChargerSpawn == chaserSpawn)
                {
                    throw new ArgumentException(
                        "Charger spawn must be distinct from player and chaser spawns.",
                        nameof(chargerSpawn));
                }
                if (playerSpawn.IsCardinallyAdjacentTo(authoredChargerSpawn))
                {
                    throw new ArgumentException(
                        "Player and charger cannot begin in cardinal contact.",
                        nameof(chargerSpawn));
                }
            }
            if (armoredSpawn.HasValue)
            {
                GridPosition authoredArmoredSpawn = armoredSpawn.Value;
                ValidateTraversableCell(authoredArmoredSpawn, nameof(armoredSpawn));
                if (authoredArmoredSpawn == playerSpawn ||
                    authoredArmoredSpawn == chaserSpawn ||
                    (chargerSpawn.HasValue && authoredArmoredSpawn == chargerSpawn.Value))
                {
                    throw new ArgumentException(
                        "Armored spawn must be distinct from all other actor spawns.",
                        nameof(armoredSpawn));
                }
                if (playerSpawn.IsCardinallyAdjacentTo(authoredArmoredSpawn))
                {
                    throw new ArgumentException(
                        "Player and armored enemy cannot begin in cardinal contact.",
                        nameof(armoredSpawn));
                }
            }
            if (selfDestructSpawn.HasValue)
            {
                GridPosition authoredSelfDestructSpawn = selfDestructSpawn.Value;
                ValidateTraversableCell(
                    authoredSelfDestructSpawn,
                    nameof(selfDestructSpawn));
                if (authoredSelfDestructSpawn == playerSpawn ||
                    authoredSelfDestructSpawn == chaserSpawn ||
                    (chargerSpawn.HasValue &&
                     authoredSelfDestructSpawn == chargerSpawn.Value) ||
                    (armoredSpawn.HasValue &&
                     authoredSelfDestructSpawn == armoredSpawn.Value))
                {
                    throw new ArgumentException(
                        "Self-destruct spawn must be distinct from all other actor spawns.",
                        nameof(selfDestructSpawn));
                }
                if (playerSpawn.IsCardinallyAdjacentTo(authoredSelfDestructSpawn))
                {
                    throw new ArgumentException(
                        "Player and self-destruct enemy cannot begin in cardinal contact.",
                        nameof(selfDestructSpawn));
                }
            }
            else if (selfDestructAnchorArray.Length > 0)
            {
                throw new ArgumentException(
                    "Self-destruct anchors require a self-destruct spawn.",
                    nameof(selfDestructAnchors));
            }

            ValidateTraversableCells(
                selfDestructAnchorArray,
                nameof(selfDestructAnchors));
            for (int index = 0; index < selfDestructAnchorArray.Length; index++)
            {
                GridPosition anchor = selfDestructAnchorArray[index];
                if (anchor == playerSpawn || anchor == chaserSpawn ||
                    (chargerSpawn.HasValue && anchor == chargerSpawn.Value) ||
                    (armoredSpawn.HasValue && anchor == armoredSpawn.Value) ||
                    (selfDestructSpawn.HasValue && anchor == selfDestructSpawn.Value))
                {
                    throw new ArgumentException(
                        $"Self-destruct anchor {anchor} cannot overlap an actor spawn.",
                        nameof(selfDestructAnchors));
                }
            }

            ValidateTraversableCells(safeArray, nameof(safePlayerCells));
            ValidateTraversableCells(retreatArray, nameof(retreatAnchors));
            ValidateTraversableCells(lureArray, nameof(lureLoop));
            if (Array.IndexOf(safeArray, playerSpawn) < 0)
            {
                throw new ArgumentException(
                    "Safe player cells must include the player spawn.",
                    nameof(safePlayerCells));
            }
            if (Array.IndexOf(safeArray, chaserSpawn) >= 0)
            {
                throw new ArgumentException(
                    "Safe player cells cannot include the chaser spawn.",
                    nameof(safePlayerCells));
            }
            if (chargerSpawn.HasValue && Array.IndexOf(safeArray, chargerSpawn.Value) >= 0)
            {
                throw new ArgumentException(
                    "Safe player cells cannot include the charger spawn.",
                    nameof(safePlayerCells));
            }
            if (armoredSpawn.HasValue && Array.IndexOf(safeArray, armoredSpawn.Value) >= 0)
            {
                throw new ArgumentException(
                    "Safe player cells cannot include the armored enemy spawn.",
                    nameof(safePlayerCells));
            }
            if (selfDestructSpawn.HasValue &&
                Array.IndexOf(safeArray, selfDestructSpawn.Value) >= 0)
            {
                throw new ArgumentException(
                    "Safe player cells cannot include the self-destruct enemy spawn.",
                    nameof(safePlayerCells));
            }
            if (retreatArray.Length < 2)
            {
                throw new ArgumentException(
                    "A combat room requires at least two retreat anchors.",
                    nameof(retreatAnchors));
            }
            ValidateClosedLureLoop(lureArray);
            ValidateExits(exitArray);
            ValidatePlayableAreaConnected(playerSpawn);
            ValidateDistinctRetreatRoutes(playerSpawn, retreatArray);

            IndestructibleWalls = Array.AsReadOnly(wallArray);
            DestructibleWalls = Array.AsReadOnly(destructibleWallArray);
            ChargerSpawn = chargerSpawn;
            ArmoredSpawn = armoredSpawn;
            SelfDestructSpawn = selfDestructSpawn;
            SelfDestructAnchors = Array.AsReadOnly(selfDestructAnchorArray);
            SafePlayerCells = Array.AsReadOnly(safeArray);
            RetreatAnchors = Array.AsReadOnly(retreatArray);
            LureLoop = Array.AsReadOnly(lureArray);
            Exits = Array.AsReadOnly(exitArray);
        }

        public RoomDefinitionId Id { get; }

        public RoomType RoomType { get; }

        public int Width { get; }

        public int Depth { get; }

        public GridPosition PlayerSpawn { get; }

        public GridPosition ChaserSpawn { get; }

        public GridPosition? ChargerSpawn { get; }

        public GridPosition? ArmoredSpawn { get; }

        public GridPosition? SelfDestructSpawn { get; }

        public IReadOnlyList<GridPosition> SelfDestructAnchors { get; }

        public IReadOnlyList<GridPosition> IndestructibleWalls { get; }

        public IReadOnlyList<GridPosition> DestructibleWalls { get; }

        public IReadOnlyList<GridPosition> SafePlayerCells { get; }

        public IReadOnlyList<GridPosition> RetreatAnchors { get; }

        public IReadOnlyList<GridPosition> LureLoop { get; }

        public IReadOnlyList<RoomExit> Exits { get; }

        public bool IsInside(GridPosition position)
        {
            int halfWidth = Width / 2;
            int halfDepth = Depth / 2;
            return position.X >= -halfWidth && position.X <= halfWidth &&
                   position.Z >= -halfDepth && position.Z <= halfDepth;
        }

        public bool IsBlocked(GridPosition position)
        {
            return _blockedSet.Contains(position);
        }

        public bool IsIndestructibleWall(GridPosition position)
        {
            return _indestructibleWallSet.Contains(position);
        }

        public bool IsDestructibleWall(GridPosition position)
        {
            return _destructibleWallSet.Contains(position);
        }

        private static void ValidateDimension(int value, string parameterName)
        {
            if (value <= 0 || (value & 1) == 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Room dimensions must be positive odd numbers.");
            }
        }

        private GridPosition[] CopyUniquePositions(
            IReadOnlyList<GridPosition> source,
            string parameterName,
            bool requireAny)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (requireAny && source.Count == 0)
            {
                throw new ArgumentException("Authored cell list cannot be empty.", parameterName);
            }

            var copy = new GridPosition[source.Count];
            var seen = new HashSet<GridPosition>();
            for (int index = 0; index < source.Count; index++)
            {
                GridPosition position = source[index];
                if (!IsInside(position))
                {
                    throw new ArgumentOutOfRangeException(
                        parameterName,
                        position,
                        "Authored cell must be inside the room grid.");
                }
                if (!seen.Add(position))
                {
                    throw new ArgumentException($"Duplicate authored cell: {position}.", parameterName);
                }
                copy[index] = position;
            }

            return copy;
        }

        private RoomExit[] CopyExits(IReadOnlyList<RoomExit> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Count < 2)
            {
                throw new ArgumentException("A combat room requires at least two exits.", nameof(source));
            }

            var copy = new RoomExit[source.Count];
            var seenCells = new HashSet<GridPosition>();
            var seenDirections = new HashSet<RoomExitDirection>();
            for (int index = 0; index < source.Count; index++)
            {
                RoomExit roomExit = source[index];
                if (!seenCells.Add(roomExit.Cell))
                {
                    throw new ArgumentException(
                        $"Duplicate room exit cell: {roomExit.Cell}.",
                        nameof(source));
                }
                if (!seenDirections.Add(roomExit.Direction))
                {
                    throw new ArgumentException(
                        $"Duplicate room exit direction: {roomExit.Direction}.",
                        nameof(source));
                }
                copy[index] = roomExit;
            }

            return copy;
        }

        private void ValidateTraversableCells(GridPosition[] positions, string parameterName)
        {
            foreach (GridPosition position in positions)
            {
                try
                {
                    ValidateTraversableCell(position, parameterName);
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException(exception.Message, parameterName, exception);
                }
            }
        }

        private void ValidateTraversableCell(GridPosition position, string parameterName)
        {
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    position,
                    "Cell must be inside the room grid.");
            }
            if (_blockedSet.Contains(position))
            {
                throw new ArgumentException($"Cell {position} overlaps a wall.", parameterName);
            }
        }

        private void ValidateClosedLureLoop(GridPosition[] positions)
        {
            if (positions.Length < 4)
            {
                throw new ArgumentException(
                    "The lure loop requires at least four unique cells.",
                    nameof(positions));
            }

            for (int index = 0; index < positions.Length; index++)
            {
                GridPosition current = positions[index];
                GridPosition next = positions[(index + 1) % positions.Length];
                if (!current.IsCardinallyAdjacentTo(next))
                {
                    throw new ArgumentException(
                        $"Lure loop cells must form a closed cardinal path: {current} to {next}.",
                        nameof(positions));
                }
            }
        }

        private void ValidateExits(RoomExit[] exits)
        {
            int halfWidth = Width / 2;
            int halfDepth = Depth / 2;
            foreach (RoomExit roomExit in exits)
            {
                ValidateTraversableCell(roomExit.Cell, nameof(exits));
                bool matchesBoundary;
                switch (roomExit.Direction)
                {
                    case RoomExitDirection.North:
                        matchesBoundary = roomExit.Cell.Z == halfDepth;
                        break;
                    case RoomExitDirection.East:
                        matchesBoundary = roomExit.Cell.X == halfWidth;
                        break;
                    case RoomExitDirection.South:
                        matchesBoundary = roomExit.Cell.Z == -halfDepth;
                        break;
                    case RoomExitDirection.West:
                        matchesBoundary = roomExit.Cell.X == -halfWidth;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(exits),
                            roomExit.Direction,
                            "Unknown room exit direction.");
                }

                if (!matchesBoundary)
                {
                    throw new ArgumentException(
                        $"Room exit {roomExit} does not match its room boundary.",
                        nameof(exits));
                }
            }
        }

        private void ValidatePlayableAreaConnected(GridPosition origin)
        {
            HashSet<GridPosition> reachable = CollectReachable(origin, default, false);
            int playableCellCount = (Width * Depth) - _blockedSet.Count;
            if (reachable.Count != playableCellCount)
            {
                throw new ArgumentException("All playable room cells must be connected.");
            }
        }

        private void ValidateDistinctRetreatRoutes(
            GridPosition origin,
            IReadOnlyCollection<GridPosition> retreatAnchors)
        {
            int routeCount = 0;
            foreach (GridPosition offset in CardinalOffsets)
            {
                GridPosition firstStep = origin.Offset(offset.X, offset.Z);
                if (!IsInside(firstStep) || IsBlocked(firstStep))
                {
                    continue;
                }

                HashSet<GridPosition> reachable = CollectReachable(firstStep, origin, true);
                foreach (GridPosition anchor in retreatAnchors)
                {
                    if (reachable.Contains(anchor))
                    {
                        routeCount++;
                        break;
                    }
                }
            }

            if (routeCount < 2)
            {
                throw new ArgumentException(
                    "Player spawn requires retreat routes with at least two distinct first steps.",
                    nameof(retreatAnchors));
            }
        }

        private HashSet<GridPosition> CollectReachable(
            GridPosition origin,
            GridPosition excluded,
            bool hasExcluded)
        {
            var reachable = new HashSet<GridPosition>();
            var frontier = new Queue<GridPosition>();
            reachable.Add(origin);
            frontier.Enqueue(origin);
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                foreach (GridPosition offset in CardinalOffsets)
                {
                    GridPosition next = current.Offset(offset.X, offset.Z);
                    if (!IsInside(next) || IsBlocked(next) ||
                        (hasExcluded && next == excluded) || !reachable.Add(next))
                    {
                        continue;
                    }
                    frontier.Enqueue(next);
                }
            }

            return reachable;
        }
    }
}
