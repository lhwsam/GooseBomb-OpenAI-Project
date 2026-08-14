using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class DungeonCombatRoomAssignment
    {
        private readonly IReadOnlyList<RoomExitDirection> _activeExitDirections;

        internal DungeonCombatRoomAssignment(
            DungeonRoomNodeId roomId,
            RoomDefinitionId definitionId,
            RoomRotation rotation,
            RoomExitDirection[] activeExitDirections)
        {
            if (!roomId.IsValid)
            {
                throw new ArgumentException(
                    "Dungeon room node ID must be valid.",
                    nameof(roomId));
            }
            if (!definitionId.IsValid)
            {
                throw new ArgumentException(
                    "Room definition ID must be valid.",
                    nameof(definitionId));
            }
            RoomRotationUtility.GetClockwiseDegrees(rotation);
            if (activeExitDirections == null || activeExitDirections.Length == 0)
            {
                throw new ArgumentException(
                    "A combat room assignment requires active exits.",
                    nameof(activeExitDirections));
            }

            RoomId = roomId;
            DefinitionId = definitionId;
            Rotation = rotation;
            _activeExitDirections = Array.AsReadOnly(
                (RoomExitDirection[])activeExitDirections.Clone());
        }

        public DungeonRoomNodeId RoomId { get; }

        public RoomDefinitionId DefinitionId { get; }

        public RoomRotation Rotation { get; }

        public IReadOnlyList<RoomExitDirection> ActiveExitDirections =>
            _activeExitDirections;

        public bool IsExitActive(RoomExitDirection direction)
        {
            RoomRotationUtility.Rotate(direction, RoomRotation.None);
            for (int index = 0; index < _activeExitDirections.Count; index++)
            {
                if (_activeExitDirections[index] == direction)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{RoomId}:{DefinitionId}@{RoomRotationUtility.GetClockwiseDegrees(Rotation)}";
        }
    }
}
