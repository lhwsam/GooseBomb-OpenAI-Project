using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class DungeonCombatRoomLayout
    {
        private readonly DungeonCombatRoomAssignment[] _assignments;

        internal DungeonCombatRoomLayout(
            DungeonGraph graph,
            string assignmentVersion,
            DungeonCombatRoomAssignment[] assignments)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (string.IsNullOrWhiteSpace(assignmentVersion))
            {
                throw new ArgumentException(
                    "Dungeon room assignment version cannot be empty.",
                    nameof(assignmentVersion));
            }
            if (assignments == null)
            {
                throw new ArgumentNullException(nameof(assignments));
            }
            if (assignments.Length != graph.CombatRoomCount)
            {
                throw new ArgumentException(
                    "Every combat graph node requires exactly one room assignment.",
                    nameof(assignments));
            }

            AssignmentVersion = assignmentVersion;
            _assignments = (DungeonCombatRoomAssignment[])assignments.Clone();
            var assignedRoomIds = new HashSet<DungeonRoomNodeId>();
            for (int index = 0; index < _assignments.Length; index++)
            {
                DungeonCombatRoomAssignment assignment = _assignments[index] ??
                    throw new ArgumentException(
                        "Dungeon combat room assignments cannot contain null.",
                        nameof(assignments));
                DungeonRoomNode room = graph.GetRoom(assignment.RoomId);
                if (room.RoomType != RoomType.Combat)
                {
                    throw new ArgumentException(
                        $"Dungeon room {room.Id} is not a combat room.",
                        nameof(assignments));
                }
                if (!assignedRoomIds.Add(room.Id))
                {
                    throw new ArgumentException(
                        $"Dungeon combat room {room.Id} was assigned more than once.",
                        nameof(assignments));
                }
                if (index > 0 && _assignments[index - 1].RoomId.Value >= room.Id.Value)
                {
                    throw new ArgumentException(
                        "Dungeon combat room assignments must be ordered by node ID.",
                        nameof(assignments));
                }
            }

            Assignments = Array.AsReadOnly(_assignments);
        }

        public DungeonGraph Graph { get; }

        public string AssignmentVersion { get; }

        public IReadOnlyList<DungeonCombatRoomAssignment> Assignments { get; }

        public DungeonCombatRoomAssignment GetAssignment(DungeonRoomNodeId roomId)
        {
            DungeonRoomNode room = Graph.GetRoom(roomId);
            if (room.RoomType != RoomType.Combat)
            {
                throw new InvalidOperationException(
                    $"Dungeon room {roomId} is not assigned combat content.");
            }

            for (int index = 0; index < _assignments.Length; index++)
            {
                if (_assignments[index].RoomId == roomId)
                {
                    return _assignments[index];
                }
            }

            throw new InvalidOperationException(
                $"Dungeon combat room {roomId} has no content assignment.");
        }
    }
}
