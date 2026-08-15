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
        private GameObject northSecretCracks;

        [SerializeField]
        private GameObject eastSecretCracks;

        [SerializeField]
        private GameObject southSecretCracks;

        [SerializeField]
        private GameObject westSecretCracks;

        [SerializeField]
        private Color inactiveColor = new Color(0.18f, 0.22f, 0.27f, 1f);

        [SerializeField]
        private Color lockedColor = new Color(0.92f, 0.16f, 0.08f, 1f);

        [SerializeField]
        private Color openColor = new Color(0.08f, 0.82f, 0.45f, 1f);

        [SerializeField]
        private Color secretWallColor = new Color(0.64f, 0.38f, 0.16f, 1f);

        private readonly DungeonRoomExitStatus[] _localStatuses =
            new DungeonRoomExitStatus[4];
        private MaterialPropertyBlock _propertyBlock;

        public bool IsConfigured =>
            northDoor != null && eastDoor != null && southDoor != null && westDoor != null &&
            northSecretCracks != null && eastSecretCracks != null &&
            southSecretCracks != null && westSecretCracks != null;

        public Renderer NorthDoor => northDoor;

        public Renderer EastDoor => eastDoor;

        public Renderer SouthDoor => southDoor;

        public Renderer WestDoor => westDoor;

        public GameObject NorthSecretCracks => northSecretCracks;

        public GameObject EastSecretCracks => eastSecretCracks;

        public GameObject SouthSecretCracks => southSecretCracks;

        public GameObject WestSecretCracks => westSecretCracks;

        public void Configure(
            Renderer authoredNorthDoor,
            Renderer authoredEastDoor,
            Renderer authoredSouthDoor,
            Renderer authoredWestDoor,
            GameObject authoredNorthSecretCracks,
            GameObject authoredEastSecretCracks,
            GameObject authoredSouthSecretCracks,
            GameObject authoredWestSecretCracks)
        {
            northDoor = authoredNorthDoor ??
                throw new ArgumentNullException(nameof(authoredNorthDoor));
            eastDoor = authoredEastDoor ??
                throw new ArgumentNullException(nameof(authoredEastDoor));
            southDoor = authoredSouthDoor ??
                throw new ArgumentNullException(nameof(authoredSouthDoor));
            westDoor = authoredWestDoor ??
                throw new ArgumentNullException(nameof(authoredWestDoor));
            northSecretCracks = authoredNorthSecretCracks ??
                throw new ArgumentNullException(nameof(authoredNorthSecretCracks));
            eastSecretCracks = authoredEastSecretCracks ??
                throw new ArgumentNullException(nameof(authoredEastSecretCracks));
            southSecretCracks = authoredSouthSecretCracks ??
                throw new ArgumentNullException(nameof(authoredSouthSecretCracks));
            westSecretCracks = authoredWestSecretCracks ??
                throw new ArgumentNullException(nameof(authoredWestSecretCracks));
            ValidateUniqueRenderers();
            ValidateUniqueCrackRoots();
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
                RoomExitDirection localDirection = LocalDirectionOrder[localIndex];
                ApplyColor(GetRenderer(localDirection), state.Status);
                GetSecretCracks(localDirection).SetActive(
                    state.Status == DungeonRoomExitStatus.SecretWall);
            }
        }

        public DungeonRoomExitStatus GetDisplayedStatus(
            RoomExitDirection localDirection)
        {
            ValidateDirection(localDirection);
            return _localStatuses[(int)localDirection];
        }

        public bool IsSecretCrackVisible(RoomExitDirection localDirection)
        {
            ValidateDirection(localDirection);
            ValidateConfiguration();
            return GetSecretCracks(localDirection).activeSelf;
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
                case DungeonRoomExitStatus.SecretWall:
                    color = secretWallColor;
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

        private GameObject GetSecretCracks(RoomExitDirection direction)
        {
            switch (direction)
            {
                case RoomExitDirection.North:
                    return northSecretCracks;
                case RoomExitDirection.East:
                    return eastSecretCracks;
                case RoomExitDirection.South:
                    return southSecretCracks;
                case RoomExitDirection.West:
                    return westSecretCracks;
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
                    "PrototypeDungeonDoorPresenter requires four door renderers " +
                    "and four secret-crack roots.");
            }
            ValidateUniqueRenderers();
            ValidateUniqueCrackRoots();
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

        private void ValidateUniqueCrackRoots()
        {
            var crackRoots = new HashSet<GameObject>
            {
                northSecretCracks,
                eastSecretCracks,
                southSecretCracks,
                westSecretCracks,
            };
            if (crackRoots.Contains(null) || crackRoots.Count != LocalDirectionOrder.Length)
            {
                throw new InvalidOperationException(
                    "Each dungeon direction requires a distinct secret-crack root.");
            }
        }

        private static void ValidateDirection(RoomExitDirection direction)
        {
            RoomRotationUtility.Rotate(direction, RoomRotation.None);
        }
    }
}
