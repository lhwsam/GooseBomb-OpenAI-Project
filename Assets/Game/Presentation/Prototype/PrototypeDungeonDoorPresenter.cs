using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonDoorPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly RoomExitDirection[] LocalDirectionOrder =
        {
            RoomExitDirection.North,
            RoomExitDirection.East,
            RoomExitDirection.South,
            RoomExitDirection.West,
        };

        [SerializeField]
        private Renderer northDoor;

        [SerializeField]
        private Renderer eastDoor;

        [SerializeField]
        private Renderer southDoor;

        [SerializeField]
        private Renderer westDoor;

        [SerializeField]
        private Color inactiveColor = new Color(0.18f, 0.22f, 0.27f, 1f);

        [SerializeField]
        private Color lockedColor = new Color(0.92f, 0.16f, 0.08f, 1f);

        [SerializeField]
        private Color openColor = new Color(0.08f, 0.82f, 0.45f, 1f);

        private readonly DungeonRoomExitStatus[] _localStatuses =
            new DungeonRoomExitStatus[4];
        private MaterialPropertyBlock _propertyBlock;

        public bool IsConfigured =>
            northDoor != null && eastDoor != null && southDoor != null && westDoor != null;

        public Renderer NorthDoor => northDoor;

        public Renderer EastDoor => eastDoor;

        public Renderer SouthDoor => southDoor;

        public Renderer WestDoor => westDoor;

        public void Configure(
            Renderer authoredNorthDoor,
            Renderer authoredEastDoor,
            Renderer authoredSouthDoor,
            Renderer authoredWestDoor)
        {
            northDoor = authoredNorthDoor ??
                throw new ArgumentNullException(nameof(authoredNorthDoor));
            eastDoor = authoredEastDoor ??
                throw new ArgumentNullException(nameof(authoredEastDoor));
            southDoor = authoredSouthDoor ??
                throw new ArgumentNullException(nameof(authoredSouthDoor));
            westDoor = authoredWestDoor ??
                throw new ArgumentNullException(nameof(authoredWestDoor));
            ValidateUniqueRenderers();
        }

        public void Apply(
            IReadOnlyList<DungeonRoomExitState> graphExitStates,
            RoomRotation roomRotation)
        {
            if (graphExitStates == null)
            {
                throw new ArgumentNullException(nameof(graphExitStates));
            }
            RoomRotationUtility.GetClockwiseDegrees(roomRotation);
            ValidateConfiguration();
            if (graphExitStates.Count != LocalDirectionOrder.Length)
            {
                throw new ArgumentException(
                    "Dungeon door presenter requires four graph exit states.",
                    nameof(graphExitStates));
            }

            var seenGraphDirections = new bool[LocalDirectionOrder.Length];
            for (int index = 0; index < graphExitStates.Count; index++)
            {
                int directionIndex = (int)graphExitStates[index].Direction;
                if (directionIndex < 0 || directionIndex >= seenGraphDirections.Length ||
                    seenGraphDirections[directionIndex])
                {
                    throw new ArgumentException(
                        "Dungeon door graph directions must be unique cardinal values.",
                        nameof(graphExitStates));
                }
                seenGraphDirections[directionIndex] = true;
            }

            for (int localIndex = 0; localIndex < LocalDirectionOrder.Length; localIndex++)
            {
                RoomExitDirection graphDirection = RoomRotationUtility.Rotate(
                    LocalDirectionOrder[localIndex],
                    roomRotation);
                DungeonRoomExitState state = FindState(
                    graphExitStates,
                    graphDirection);
                _localStatuses[localIndex] = state.Status;
                ApplyColor(GetRenderer(LocalDirectionOrder[localIndex]), state.Status);
            }
        }

        public DungeonRoomExitStatus GetDisplayedStatus(
            RoomExitDirection localDirection)
        {
            ValidateDirection(localDirection);
            return _localStatuses[(int)localDirection];
        }

        private void ApplyColor(Renderer target, DungeonRoomExitStatus status)
        {
            Color color;
            switch (status)
            {
                case DungeonRoomExitStatus.Inactive:
                    color = inactiveColor;
                    break;
                case DungeonRoomExitStatus.Locked:
                    color = lockedColor;
                    break;
                case DungeonRoomExitStatus.Open:
                    color = openColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(status),
                        status,
                        "Unsupported dungeon door status.");
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            target.SetPropertyBlock(_propertyBlock);
        }

        private Renderer GetRenderer(RoomExitDirection direction)
        {
            switch (direction)
            {
                case RoomExitDirection.North:
                    return northDoor;
                case RoomExitDirection.East:
                    return eastDoor;
                case RoomExitDirection.South:
                    return southDoor;
                case RoomExitDirection.West:
                    return westDoor;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unknown room exit direction.");
            }
        }

        private static DungeonRoomExitState FindState(
            IReadOnlyList<DungeonRoomExitState> states,
            RoomExitDirection direction)
        {
            for (int index = 0; index < states.Count; index++)
            {
                if (states[index].Direction == direction)
                {
                    return states[index];
                }
            }
            throw new ArgumentException(
                $"Dungeon graph exit state is missing {direction}.",
                nameof(states));
        }

        private void ValidateConfiguration()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonDoorPresenter requires four door renderers.");
            }
            ValidateUniqueRenderers();
        }

        private void ValidateUniqueRenderers()
        {
            var renderers = new HashSet<Renderer>
            {
                northDoor,
                eastDoor,
                southDoor,
                westDoor,
            };
            if (renderers.Count != LocalDirectionOrder.Length)
            {
                throw new InvalidOperationException(
                    "Each dungeon direction requires a distinct door renderer.");
            }
        }

        private static void ValidateDirection(RoomExitDirection direction)
        {
            RoomRotationUtility.Rotate(direction, RoomRotation.None);
        }
    }
}
