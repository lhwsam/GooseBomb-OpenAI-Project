using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public static class CombatRoomRotationUtility
    {
        public static CombatRoomDefinition Rotate(
            CombatRoomDefinition definition,
            RoomRotation rotation)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            RoomRotationUtility.GetClockwiseDegrees(rotation);
            if (rotation == RoomRotation.None)
            {
                return definition;
            }

            bool swapsDimensions = rotation == RoomRotation.Clockwise90 ||
                rotation == RoomRotation.Clockwise270;
            return new CombatRoomDefinition(
                definition.Id,
                definition.RoomType,
                swapsDimensions ? definition.Depth : definition.Width,
                swapsDimensions ? definition.Width : definition.Depth,
                Rotate(definition.PlayerSpawn, rotation),
                Rotate(definition.ChaserSpawn, rotation),
                RotatePositions(definition.IndestructibleWalls, rotation),
                RotatePositions(definition.SafePlayerCells, rotation),
                RotatePositions(definition.RetreatAnchors, rotation),
                RotatePositions(definition.LureLoop, rotation),
                RotateExits(definition.Exits, rotation),
                RotatePositions(definition.DestructibleWalls, rotation),
                RotateOptional(definition.ChargerSpawn, rotation),
                RotateOptional(definition.ArmoredSpawn, rotation),
                RotateOptional(definition.SelfDestructSpawn, rotation),
                RotatePositions(definition.SelfDestructAnchors, rotation),
                RotateOptional(definition.ThrowerSpawn, rotation),
                RotatePositions(definition.ThrowerFiringAnchors, rotation),
                RotatePositions(definition.ThrowerTargetAnchors, rotation));
        }

        public static GridPosition Rotate(
            GridPosition position,
            RoomRotation rotation)
        {
            switch (rotation)
            {
                case RoomRotation.None:
                    return position;
                case RoomRotation.Clockwise90:
                    return new GridPosition(position.Z, -position.X);
                case RoomRotation.Clockwise180:
                    return new GridPosition(-position.X, -position.Z);
                case RoomRotation.Clockwise270:
                    return new GridPosition(-position.Z, position.X);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(rotation),
                        rotation,
                        "Unsupported room rotation.");
            }
        }

        private static GridPosition? RotateOptional(
            GridPosition? position,
            RoomRotation rotation)
        {
            return position.HasValue
                ? Rotate(position.Value, rotation)
                : (GridPosition?)null;
        }

        private static GridPosition[] RotatePositions(
            IReadOnlyList<GridPosition> positions,
            RoomRotation rotation)
        {
            var result = new GridPosition[positions.Count];
            for (int index = 0; index < positions.Count; index++)
            {
                result[index] = Rotate(positions[index], rotation);
            }
            return result;
        }

        private static RoomExit[] RotateExits(
            IReadOnlyList<RoomExit> exits,
            RoomRotation rotation)
        {
            var result = new RoomExit[exits.Count];
            for (int index = 0; index < exits.Count; index++)
            {
                result[index] = new RoomExit(
                    Rotate(exits[index].Cell, rotation),
                    RoomRotationUtility.Rotate(exits[index].Direction, rotation));
            }
            return result;
        }
    }
}
