using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class DungeonMinimapSnapshot
    {
        private readonly DungeonMinimapRoomSnapshot[] _rooms;
        private readonly DungeonRoomConnection[] _connections;

        internal DungeonMinimapSnapshot(
            DungeonRoomNodeId currentRoomId,
            DungeonMinimapRoomSnapshot[] rooms,
            DungeonRoomConnection[] connections)
        {
            if (!currentRoomId.IsValid)
            {
                throw new ArgumentException(
                    "Current minimap room ID must be valid.",
                    nameof(currentRoomId));
            }
            if (rooms == null)
            {
                throw new ArgumentNullException(nameof(rooms));
            }
            if (connections == null)
            {
                throw new ArgumentNullException(nameof(connections));
            }

            CurrentRoomId = currentRoomId;
            _rooms = (DungeonMinimapRoomSnapshot[])rooms.Clone();
            _connections = (DungeonRoomConnection[])connections.Clone();
            Validate();
            Rooms = Array.AsReadOnly(_rooms);
            Connections = Array.AsReadOnly(_connections);
        }

        public DungeonRoomNodeId CurrentRoomId { get; }

        public IReadOnlyList<DungeonMinimapRoomSnapshot> Rooms { get; }

        public IReadOnlyList<DungeonRoomConnection> Connections { get; }

        public DungeonMinimapRoomSnapshot GetRoom(DungeonRoomNodeId roomId)
        {
            for (int index = 0; index < _rooms.Length; index++)
            {
                if (_rooms[index].RoomId == roomId)
                {
                    return _rooms[index];
                }
            }

            throw new KeyNotFoundException(
                $"Dungeon room {roomId} is not visible on the minimap.");
        }

        private void Validate()
        {
            if (_rooms.Length == 0)
            {
                throw new ArgumentException(
                    "Minimap snapshot requires at least the current room.",
                    nameof(_rooms));
            }

            var roomIds = new HashSet<DungeonRoomNodeId>();
            int currentCount = 0;
            for (int index = 0; index < _rooms.Length; index++)
            {
                DungeonMinimapRoomSnapshot room = _rooms[index];
                if (!roomIds.Add(room.RoomId))
                {
                    throw new ArgumentException(
                        $"Minimap room {room.RoomId} is duplicated.",
                        nameof(_rooms));
                }
                if (room.IsCurrent)
                {
                    currentCount++;
                    if (room.RoomId != CurrentRoomId)
                    {
                        throw new ArgumentException(
                            "Minimap current state does not match current room ID.",
                            nameof(_rooms));
                    }
                }
            }
            if (currentCount != 1)
            {
                throw new ArgumentException(
                    "Minimap snapshot requires exactly one current room.",
                    nameof(_rooms));
            }

            var uniqueConnections = new HashSet<DungeonRoomConnection>();
            for (int index = 0; index < _connections.Length; index++)
            {
                DungeonRoomConnection connection = _connections[index];
                if (!roomIds.Contains(connection.First) ||
                    !roomIds.Contains(connection.Second) ||
                    !uniqueConnections.Add(connection))
                {
                    throw new ArgumentException(
                        "Minimap connection must be unique and join visible rooms.",
                        nameof(_connections));
                }
            }
        }
    }
}
