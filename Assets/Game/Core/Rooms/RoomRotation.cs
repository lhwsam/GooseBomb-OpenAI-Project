using System;

namespace BombSwap.Core
{
    public enum RoomRotation
    {
        None = 0,
        Clockwise90 = 1,
        Clockwise180 = 2,
        Clockwise270 = 3,
    }

    public static class RoomRotationUtility
    {
        public static RoomExitDirection Rotate(
            RoomExitDirection direction,
            RoomRotation rotation)
        {
            ValidateDirection(direction);
            int quarterTurns = GetQuarterTurns(rotation);
            return (RoomExitDirection)(((int)direction + quarterTurns) % 4);
        }

        public static int GetClockwiseDegrees(RoomRotation rotation)
        {
            return GetQuarterTurns(rotation) * 90;
        }

        private static int GetQuarterTurns(RoomRotation rotation)
        {
            switch (rotation)
            {
                case RoomRotation.None:
                case RoomRotation.Clockwise90:
                case RoomRotation.Clockwise180:
                case RoomRotation.Clockwise270:
                    return (int)rotation;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(rotation),
                        rotation,
                        "Unsupported room rotation.");
            }
        }

        private static void ValidateDirection(RoomExitDirection direction)
        {
            switch (direction)
            {
                case RoomExitDirection.North:
                case RoomExitDirection.East:
                case RoomExitDirection.South:
                case RoomExitDirection.West:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unknown room exit direction.");
            }
        }
    }
}
