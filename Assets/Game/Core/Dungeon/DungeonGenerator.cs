using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public static class DungeonGenerator
    {
        public const string GenerationVersion = "prototype-tree-v2";

        private const int MaximumPlacementAttempts = 1000000;

        private static readonly RoomGraphPosition[] CardinalOffsets =
        {
            new RoomGraphPosition(0, 1),
            new RoomGraphPosition(1, 0),
            new RoomGraphPosition(0, -1),
            new RoomGraphPosition(-1, 0),
        };

        public static DungeonGraph Generate(int seed)
        {
            return Generate(seed, DungeonGenerationDefinition.CreatePrototype());
        }

        public static DungeonGraph Generate(
            int seed,
            DungeonGenerationDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var random = new DeterministicSeedRandom(seed);
            int combatRoomCount = random.Next(
                definition.MinimumCombatRooms,
                definition.MaximumCombatRooms + 1);
            int branchCombatCount = combatRoomCount - definition.BossPathCombatRooms;

            var roomTypes = new List<RoomType>();
            var parents = new List<int>();
            AddRoom(roomTypes, parents, RoomType.Start, -1);
            AddRoom(roomTypes, parents, RoomType.Combat, 0);
            AddRoom(roomTypes, parents, RoomType.BombReward, 1);
            for (int index = 1; index < definition.BossPathCombatRooms; index++)
            {
                AddRoom(roomTypes, parents, RoomType.Combat, roomTypes.Count - 1);
            }

            int lastBossPathCombatIndex = roomTypes.Count - 1;
            AddRoom(
                roomTypes,
                parents,
                RoomType.BossAntechamber,
                lastBossPathCombatIndex);
            AddRoom(roomTypes, parents, RoomType.Boss, roomTypes.Count - 1);
            AddRoom(
                roomTypes,
                parents,
                RoomType.Recovery,
                lastBossPathCombatIndex);
            int fixedRoomCount = roomTypes.Count;

            int branchAttachIndex = random.Next(2, lastBossPathCombatIndex + 1);
            int branchParent = branchAttachIndex;
            for (int index = 0; index < branchCombatCount; index++)
            {
                AddRoom(roomTypes, parents, RoomType.Combat, branchParent);
                branchParent = roomTypes.Count - 1;
            }

            if (fixedRoomCount + branchCombatCount != roomTypes.Count)
            {
                throw new InvalidOperationException("Dungeon topology room count is inconsistent.");
            }

            var directionStarts = new int[roomTypes.Count];
            var directionReversed = new bool[roomTypes.Count];
            for (int index = 1; index < roomTypes.Count; index++)
            {
                directionStarts[index] = random.Next(0, CardinalOffsets.Length);
                directionReversed[index] = random.Next(0, 2) == 1;
            }

            var positions = new RoomGraphPosition[roomTypes.Count];
            var assigned = new bool[roomTypes.Count];
            var occupied = new HashSet<RoomGraphPosition>();
            positions[0] = new RoomGraphPosition(0, 0);
            assigned[0] = true;
            occupied.Add(positions[0]);
            int placementAttempts = 0;
            if (!TryPlaceRooms(
                1,
                parents,
                positions,
                assigned,
                occupied,
                directionStarts,
                directionReversed,
                ref placementAttempts))
            {
                throw new InvalidOperationException(
                    $"Dungeon layout generation failed for seed {seed} using " +
                    $"version {GenerationVersion} after {placementAttempts} attempts.");
            }

            var rooms = new DungeonRoomNode[roomTypes.Count];
            var connections = new DungeonRoomConnection[roomTypes.Count - 1];
            for (int index = 0; index < roomTypes.Count; index++)
            {
                rooms[index] = new DungeonRoomNode(
                    new DungeonRoomNodeId(index + 1),
                    roomTypes[index],
                    positions[index]);
                if (index > 0)
                {
                    connections[index - 1] = new DungeonRoomConnection(
                        rooms[index].Id,
                        new DungeonRoomNodeId(parents[index] + 1));
                }
            }

            return new DungeonGraph(
                seed,
                GenerationVersion,
                definition,
                rooms,
                connections);
        }

        private static void AddRoom(
            ICollection<RoomType> roomTypes,
            ICollection<int> parents,
            RoomType roomType,
            int parent)
        {
            roomTypes.Add(roomType);
            parents.Add(parent);
        }

        private static bool TryPlaceRooms(
            int roomIndex,
            IReadOnlyList<int> parents,
            RoomGraphPosition[] positions,
            bool[] assigned,
            ISet<RoomGraphPosition> occupied,
            IReadOnlyList<int> directionStarts,
            IReadOnlyList<bool> directionReversed,
            ref int placementAttempts)
        {
            if (roomIndex >= positions.Length)
            {
                return true;
            }

            int parentIndex = parents[roomIndex];
            if (parentIndex < 0 || parentIndex >= roomIndex || !assigned[parentIndex])
            {
                throw new InvalidOperationException(
                    $"Dungeon room {roomIndex + 1} has an invalid placement parent.");
            }

            for (int offsetIndex = 0; offsetIndex < CardinalOffsets.Length; offsetIndex++)
            {
                placementAttempts++;
                if (placementAttempts > MaximumPlacementAttempts)
                {
                    return false;
                }

                int signedOffset = directionReversed[roomIndex]
                    ? -offsetIndex
                    : offsetIndex;
                int directionIndex =
                    (directionStarts[roomIndex] + signedOffset + CardinalOffsets.Length) %
                    CardinalOffsets.Length;
                RoomGraphPosition offset = CardinalOffsets[directionIndex];
                RoomGraphPosition candidate = positions[parentIndex].Offset(offset.X, offset.Z);
                if (!CanPlace(
                    candidate,
                    parentIndex,
                    positions,
                    assigned,
                    occupied))
                {
                    continue;
                }

                positions[roomIndex] = candidate;
                assigned[roomIndex] = true;
                occupied.Add(candidate);
                if (TryPlaceRooms(
                    roomIndex + 1,
                    parents,
                    positions,
                    assigned,
                    occupied,
                    directionStarts,
                    directionReversed,
                    ref placementAttempts))
                {
                    return true;
                }

                occupied.Remove(candidate);
                assigned[roomIndex] = false;
                positions[roomIndex] = default;
            }

            return false;
        }

        private static bool CanPlace(
            RoomGraphPosition candidate,
            int parentIndex,
            IReadOnlyList<RoomGraphPosition> positions,
            IReadOnlyList<bool> assigned,
            ISet<RoomGraphPosition> occupied)
        {
            if (occupied.Contains(candidate))
            {
                return false;
            }

            for (int index = 0; index < positions.Count; index++)
            {
                if (!assigned[index] || index == parentIndex)
                {
                    continue;
                }
                if (candidate.IsCardinallyAdjacentTo(positions[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
