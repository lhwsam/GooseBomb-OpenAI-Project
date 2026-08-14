using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public static class DungeonCombatRoomAssigner
    {
        public const string AssignmentVersion = "prototype-combat-assignment-v1";

        private const int AssignmentSeedSalt = unchecked((int)0xC6A4A793u);

        private static readonly RoomRotation[] Rotations =
        {
            RoomRotation.None,
            RoomRotation.Clockwise90,
            RoomRotation.Clockwise180,
            RoomRotation.Clockwise270,
        };

        public static DungeonCombatRoomLayout Assign(
            DungeonGraph graph,
            IReadOnlyList<CombatRoomDefinition> definitions)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            CombatRoomDefinition[] catalog = CopyAndSortCatalog(definitions);
            var random = new DeterministicSeedRandom(
                unchecked(graph.Seed ^ AssignmentSeedSalt));
            var usageCounts = new int[catalog.Length];
            var assignments = new DungeonCombatRoomAssignment[graph.CombatRoomCount];
            int assignmentIndex = 0;
            for (int roomIndex = 0; roomIndex < graph.Rooms.Count; roomIndex++)
            {
                DungeonRoomNode room = graph.Rooms[roomIndex];
                if (room.RoomType != RoomType.Combat)
                {
                    continue;
                }

                RoomExitDirection[] requiredDirections =
                    GetRequiredExitDirections(graph, room.Id);
                int selectedDefinitionIndex = SelectDefinition(
                    catalog,
                    usageCounts,
                    requiredDirections,
                    random,
                    room.Id);
                RoomRotation selectedRotation = SelectRotation(
                    catalog[selectedDefinitionIndex],
                    requiredDirections,
                    random);
                usageCounts[selectedDefinitionIndex]++;
                assignments[assignmentIndex++] = new DungeonCombatRoomAssignment(
                    room.Id,
                    catalog[selectedDefinitionIndex].Id,
                    selectedRotation,
                    requiredDirections);
            }

            if (assignmentIndex != assignments.Length)
            {
                throw new InvalidOperationException(
                    "Dungeon combat room assignment count is inconsistent.");
            }

            return new DungeonCombatRoomLayout(
                graph,
                AssignmentVersion,
                assignments);
        }

        public static bool SupportsActiveExits(
            CombatRoomDefinition definition,
            RoomRotation rotation,
            IReadOnlyList<RoomExitDirection> activeDirections)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (activeDirections == null)
            {
                throw new ArgumentNullException(nameof(activeDirections));
            }
            RoomRotationUtility.GetClockwiseDegrees(rotation);

            for (int requiredIndex = 0; requiredIndex < activeDirections.Count; requiredIndex++)
            {
                RoomExitDirection required = activeDirections[requiredIndex];
                RoomRotationUtility.Rotate(required, RoomRotation.None);
                bool found = false;
                for (int exitIndex = 0; exitIndex < definition.Exits.Count; exitIndex++)
                {
                    if (RoomRotationUtility.Rotate(
                        definition.Exits[exitIndex].Direction,
                        rotation) == required)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static CombatRoomDefinition[] CopyAndSortCatalog(
            IReadOnlyList<CombatRoomDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }
            if (definitions.Count == 0)
            {
                throw new ArgumentException(
                    "Dungeon combat room catalog cannot be empty.",
                    nameof(definitions));
            }

            var copy = new CombatRoomDefinition[definitions.Count];
            for (int index = 0; index < definitions.Count; index++)
            {
                copy[index] = definitions[index] ??
                    throw new ArgumentException(
                        "Dungeon combat room catalog cannot contain null.",
                        nameof(definitions));
            }
            Array.Sort(
                copy,
                (left, right) => string.CompareOrdinal(left.Id.Value, right.Id.Value));
            for (int index = 1; index < copy.Length; index++)
            {
                if (copy[index - 1].Id == copy[index].Id)
                {
                    throw new ArgumentException(
                        $"Dungeon combat room ID '{copy[index].Id}' is duplicated.",
                        nameof(definitions));
                }
            }
            return copy;
        }

        private static RoomExitDirection[] GetRequiredExitDirections(
            DungeonGraph graph,
            DungeonRoomNodeId roomId)
        {
            IReadOnlyList<DungeonRoomNodeId> neighbors = graph.GetNeighbors(roomId);
            var directions = new RoomExitDirection[neighbors.Count];
            for (int index = 0; index < neighbors.Count; index++)
            {
                directions[index] = graph.GetExitDirection(roomId, neighbors[index]);
            }
            Array.Sort(directions);
            return directions;
        }

        private static int SelectDefinition(
            IReadOnlyList<CombatRoomDefinition> catalog,
            IReadOnlyList<int> usageCounts,
            IReadOnlyList<RoomExitDirection> requiredDirections,
            DeterministicSeedRandom random,
            DungeonRoomNodeId roomId)
        {
            var compatible = new List<int>();
            int minimumUsage = int.MaxValue;
            for (int definitionIndex = 0; definitionIndex < catalog.Count; definitionIndex++)
            {
                if (!HasCompatibleRotation(catalog[definitionIndex], requiredDirections))
                {
                    continue;
                }

                int usage = usageCounts[definitionIndex];
                if (usage < minimumUsage)
                {
                    compatible.Clear();
                    minimumUsage = usage;
                }
                if (usage == minimumUsage)
                {
                    compatible.Add(definitionIndex);
                }
            }

            if (compatible.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No authored combat room supports dungeon node {roomId} exits " +
                    $"[{string.Join(",", requiredDirections)}].");
            }

            return compatible[random.Next(0, compatible.Count)];
        }

        private static RoomRotation SelectRotation(
            CombatRoomDefinition definition,
            IReadOnlyList<RoomExitDirection> requiredDirections,
            DeterministicSeedRandom random)
        {
            var compatible = new List<RoomRotation>();
            for (int index = 0; index < Rotations.Length; index++)
            {
                if (SupportsActiveExits(definition, Rotations[index], requiredDirections))
                {
                    compatible.Add(Rotations[index]);
                }
            }
            if (compatible.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Room definition {definition.Id} has no compatible rotation.");
            }
            return compatible[random.Next(0, compatible.Count)];
        }

        private static bool HasCompatibleRotation(
            CombatRoomDefinition definition,
            IReadOnlyList<RoomExitDirection> requiredDirections)
        {
            for (int index = 0; index < Rotations.Length; index++)
            {
                if (SupportsActiveExits(definition, Rotations[index], requiredDirections))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
