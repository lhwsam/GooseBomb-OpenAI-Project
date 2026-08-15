using System;

namespace BombSwap.Core
{
    public sealed class DungeonRoomNode
    {
        internal DungeonRoomNode(
            DungeonRoomNodeId id,
            RoomType roomType,
            RoomGraphPosition position)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Dungeon room node ID must be valid.", nameof(id));
            }
            ValidateRoomType(roomType);

            Id = id;
            RoomType = roomType;
            Position = position;
        }

        public DungeonRoomNodeId Id { get; }

        public RoomType RoomType { get; }

        public RoomGraphPosition Position { get; }

        public override string ToString()
        {
            return $"{Id}:{RoomType}@{Position}";
        }

        internal static void ValidateRoomType(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat:
                case RoomType.Start:
                case RoomType.BombReward:
                case RoomType.BossAntechamber:
                case RoomType.Boss:
                case RoomType.Recovery:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        "Unsupported dungeon room type.");
            }
        }
    }
}
