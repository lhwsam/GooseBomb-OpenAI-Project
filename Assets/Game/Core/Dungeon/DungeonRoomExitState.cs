using System;

namespace BombSwap.Core
{
    public enum DungeonRoomExitStatus
    {
        Inactive = 0,
        Locked = 1,
        Open = 2,
        SecretWall = 3,
    }

    public readonly struct DungeonRoomExitState : IEquatable<DungeonRoomExitState>
    {
        internal DungeonRoomExitState(
            RoomExitDirection direction,
            DungeonRoomNodeId targetRoomId,
            DungeonRoomExitStatus status)
        {
            ValidateStatus(status);
            if (status == DungeonRoomExitStatus.Inactive)
            {
                if (targetRoomId.IsValid)
                {
                    throw new ArgumentException(
                        "An inactive dungeon exit cannot have a target room.",
                        nameof(targetRoomId));
                }
            }
            else if (!targetRoomId.IsValid)
            {
                throw new ArgumentException(
                    "A connected dungeon exit requires a target room.",
                    nameof(targetRoomId));
            }

            Direction = direction;
            TargetRoomId = targetRoomId;
            Status = status;
        }

        public RoomExitDirection Direction { get; }

        public DungeonRoomNodeId TargetRoomId { get; }

        public DungeonRoomExitStatus Status { get; }

        public bool IsConnected => Status != DungeonRoomExitStatus.Inactive;

        public bool CanTravel => Status == DungeonRoomExitStatus.Open;

        public bool Equals(DungeonRoomExitState other)
        {
            return Direction == other.Direction &&
                TargetRoomId == other.TargetRoomId &&
                Status == other.Status;
        }

        public override bool Equals(object obj)
        {
            return obj is DungeonRoomExitState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Direction;
                hash = (hash * 397) ^ TargetRoomId.GetHashCode();
                hash = (hash * 397) ^ (int)Status;
                return hash;
            }
        }

        public static bool operator ==(
            DungeonRoomExitState left,
            DungeonRoomExitState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DungeonRoomExitState left,
            DungeonRoomExitState right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return IsConnected
                ? $"{Direction}:{Status}->{TargetRoomId}"
                : $"{Direction}:{Status}";
        }

        private static void ValidateStatus(DungeonRoomExitStatus status)
        {
            switch (status)
            {
                case DungeonRoomExitStatus.Inactive:
                case DungeonRoomExitStatus.Locked:
                case DungeonRoomExitStatus.Open:
                case DungeonRoomExitStatus.SecretWall:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(status),
                        status,
                        "Unknown dungeon room exit status.");
            }
        }
    }
}
