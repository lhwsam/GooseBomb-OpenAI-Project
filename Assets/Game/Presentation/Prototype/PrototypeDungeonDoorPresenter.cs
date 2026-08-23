using System;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonDoorPresenter : MonoBehaviour
    {
        public const float SecretWallBreakVfxHeightOffset = 0.5f;

        private static readonly int IsOpenId = Animator.StringToHash("IsOpen");

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
        private Animator northDoorAnimator;

        [SerializeField]
        private Animator eastDoorAnimator;

        [SerializeField]
        private Animator southDoorAnimator;

        [SerializeField]
        private Animator westDoorAnimator;

        [SerializeField]
        private GameObject northSecretCracks;

        [SerializeField]
        private GameObject eastSecretCracks;

        [SerializeField]
        private GameObject southSecretCracks;

        [SerializeField]
        private GameObject westSecretCracks;

        [SerializeField]
        private GameObject secretWallBreakVfxPrefab;

        private PrototypeLocalVfxOverrides _localVfxOverrides;

        private readonly DungeonRoomExitStatus[] _localStatuses =
            new DungeonRoomExitStatus[4];

        public bool IsConfigured =>
            northDoor != null && eastDoor != null && southDoor != null && westDoor != null &&
            northSecretCracks != null && eastSecretCracks != null &&
            southSecretCracks != null && westSecretCracks != null &&
            HasConsistentAnimatorConfiguration;

        public bool HasAnimatedDoors =>
            northDoorAnimator != null && eastDoorAnimator != null &&
            southDoorAnimator != null && westDoorAnimator != null;

        public Renderer NorthDoor => northDoor;

        public Renderer EastDoor => eastDoor;

        public Renderer SouthDoor => southDoor;

        public Renderer WestDoor => westDoor;

        public Animator NorthDoorAnimator => northDoorAnimator;

        public Animator EastDoorAnimator => eastDoorAnimator;

        public Animator SouthDoorAnimator => southDoorAnimator;

        public Animator WestDoorAnimator => westDoorAnimator;

        public GameObject NorthSecretCracks => northSecretCracks;

        public GameObject EastSecretCracks => eastSecretCracks;

        public GameObject SouthSecretCracks => southSecretCracks;

        public GameObject WestSecretCracks => westSecretCracks;

        public GameObject SecretWallBreakVfxPrefab => secretWallBreakVfxPrefab;

        public void Configure(
            Renderer authoredNorthDoor,
            Renderer authoredEastDoor,
            Renderer authoredSouthDoor,
            Renderer authoredWestDoor,
            Animator authoredNorthDoorAnimator,
            Animator authoredEastDoorAnimator,
            Animator authoredSouthDoorAnimator,
            Animator authoredWestDoorAnimator,
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
            northDoorAnimator = authoredNorthDoorAnimator;
            eastDoorAnimator = authoredEastDoorAnimator;
            southDoorAnimator = authoredSouthDoorAnimator;
            westDoorAnimator = authoredWestDoorAnimator;
            northSecretCracks = authoredNorthSecretCracks ??
                throw new ArgumentNullException(nameof(authoredNorthSecretCracks));
            eastSecretCracks = authoredEastSecretCracks ??
                throw new ArgumentNullException(nameof(authoredEastSecretCracks));
            southSecretCracks = authoredSouthSecretCracks ??
                throw new ArgumentNullException(nameof(authoredSouthSecretCracks));
            westSecretCracks = authoredWestSecretCracks ??
                throw new ArgumentNullException(nameof(authoredWestSecretCracks));
            ValidateUniqueRenderers();
            ValidateAnimators();
            ValidateUniqueCrackRoots();
        }

        public void ConfigureSecretWallBreakVfx(GameObject authoredVfxPrefab)
        {
            if (authoredVfxPrefab != null &&
                authoredVfxPrefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
            {
                throw new InvalidOperationException(
                    "Secret wall break VFX prefab requires at least one ParticleSystem.");
            }
            secretWallBreakVfxPrefab = authoredVfxPrefab;
        }

        public void ConfigureLocalVfxOverrides(PrototypeLocalVfxOverrides localVfxOverrides)
        {
            _localVfxOverrides = localVfxOverrides ??
                throw new ArgumentNullException(nameof(localVfxOverrides));
            _localVfxOverrides.ValidateConfiguration();
        }

        public GameObject PlaySecretWallBreak(
            RoomExitDirection graphDirection,
            RoomRotation roomRotation)
        {
            ValidateConfiguration();
            RoomExitDirection localDirection = FindLocalDirection(
                graphDirection,
                roomRotation);
            Vector3 position =
                GetSecretCracks(localDirection).transform.position +
                (Vector3.up * SecretWallBreakVfxHeightOffset);
            GameObject resolvedVfxPrefab = ResolveSecretWallBreakVfxPrefab();
            GameObject instance = resolvedVfxPrefab != null
                ? Instantiate(resolvedVfxPrefab)
                : CreateFallbackSecretWallBreakVfx();
            instance.transform.position = position;
            ParticleSystem[] particleSystems =
                instance.GetComponentsInChildren<ParticleSystem>(true);
            float lifetime = 0f;
            for (int index = 0; index < particleSystems.Length; index++)
            {
                ParticleSystem particleSystem = particleSystems[index];
                if (!particleSystem.gameObject.activeInHierarchy)
                {
                    continue;
                }
                particleSystem.Clear(true);
                particleSystem.Play(true);
                ParticleSystem.MainModule main = particleSystem.main;
                lifetime = Mathf.Max(
                    lifetime,
                    main.startDelay.constantMax + main.duration +
                    main.startLifetime.constantMax);
            }
            Destroy(instance, Mathf.Max(lifetime, 0.1f));
            return instance;
        }

        private GameObject ResolveSecretWallBreakVfxPrefab()
        {
            if (secretWallBreakVfxPrefab != null)
            {
                return secretWallBreakVfxPrefab;
            }

            _localVfxOverrides ??= PrototypeLocalVfxOverrides.LoadOptional();
            return _localVfxOverrides != null
                ? _localVfxOverrides.SecretWallBreakVfxPrefab
                : null;
        }

        private static GameObject CreateFallbackSecretWallBreakVfx()
        {
            var instance = new GameObject("SecretWallBreakVfxFallback");
            ParticleSystem particleSystem = instance.AddComponent<ParticleSystem>();
            particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = 0.35f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.25f, 2.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.38f, 0.3f, 0.2f, 0.9f),
                new Color(0.72f, 0.62f, 0.46f, 0.75f));
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 14),
            });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f;

            return instance;
        }

        public void Apply(
            IReadOnlyList<DungeonRoomExitState> graphExitStates,
            RoomRotation roomRotation,
            IReadOnlyList<RoomExitDirection> graphSecretExitDirections)
        {
            if (graphExitStates == null)
            {
                throw new ArgumentNullException(nameof(graphExitStates));
            }
            if (graphSecretExitDirections == null)
            {
                throw new ArgumentNullException(nameof(graphSecretExitDirections));
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
                bool isSecretConnection = ContainsDirection(
                    graphSecretExitDirections,
                    graphDirection);
                _localStatuses[localIndex] = state.Status;
                RoomExitDirection localDirection = LocalDirectionOrder[localIndex];
                Renderer door = GetRenderer(localDirection);
                Animator animator = GetAnimator(localDirection);
                if (animator != null)
                {
                    animator.SetBool(
                        IsOpenId,
                        state.Status == DungeonRoomExitStatus.Open &&
                        !isSecretConnection);
                }
                // TODO: Add a Locked-specific presentation only if design requires
                // a state beyond the existing closed animation.
                door.enabled = !isSecretConnection;
                GetSecretCracks(localDirection).SetActive(
                    state.Status == DungeonRoomExitStatus.SecretWall);
            }
        }

        private static bool ContainsDirection(
            IReadOnlyList<RoomExitDirection> directions,
            RoomExitDirection target)
        {
            for (int index = 0; index < directions.Count; index++)
            {
                if (directions[index] == target)
                {
                    return true;
                }
            }

            return false;
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

        public bool IsDoorPanelVisible(RoomExitDirection localDirection)
        {
            ValidateDirection(localDirection);
            ValidateConfiguration();
            return GetRenderer(localDirection).enabled;
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

        private Animator GetAnimator(RoomExitDirection direction)
        {
            switch (direction)
            {
                case RoomExitDirection.North:
                    return northDoorAnimator;
                case RoomExitDirection.East:
                    return eastDoorAnimator;
                case RoomExitDirection.South:
                    return southDoorAnimator;
                case RoomExitDirection.West:
                    return westDoorAnimator;
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

        private static RoomExitDirection FindLocalDirection(
            RoomExitDirection graphDirection,
            RoomRotation roomRotation)
        {
            for (int index = 0; index < LocalDirectionOrder.Length; index++)
            {
                RoomExitDirection localDirection = LocalDirectionOrder[index];
                if (RoomRotationUtility.Rotate(localDirection, roomRotation) ==
                    graphDirection)
                {
                    return localDirection;
                }
            }
            throw new ArgumentOutOfRangeException(
                nameof(graphDirection),
                graphDirection,
                "Unknown room exit direction.");
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
            ValidateAnimators();
            ValidateUniqueCrackRoots();
        }

        private bool HasConsistentAnimatorConfiguration
        {
            get
            {
                int count = 0;
                count += northDoorAnimator != null ? 1 : 0;
                count += eastDoorAnimator != null ? 1 : 0;
                count += southDoorAnimator != null ? 1 : 0;
                count += westDoorAnimator != null ? 1 : 0;
                return count == 0 || count == LocalDirectionOrder.Length;
            }
        }

        private void ValidateAnimators()
        {
            if (!HasConsistentAnimatorConfiguration)
            {
                throw new InvalidOperationException(
                    "Dungeon doors require either four direction animators or no animators during the pilot rollout.");
            }
            if (!HasAnimatedDoors)
            {
                return;
            }

            var animators = new HashSet<Animator>
            {
                northDoorAnimator,
                eastDoorAnimator,
                southDoorAnimator,
                westDoorAnimator,
            };
            if (animators.Count != LocalDirectionOrder.Length)
            {
                throw new InvalidOperationException(
                    "Each dungeon direction requires a distinct door animator.");
            }

            Animator[] orderedAnimators =
            {
                northDoorAnimator,
                eastDoorAnimator,
                southDoorAnimator,
                westDoorAnimator,
            };
            Renderer[] orderedRenderers =
            {
                northDoor,
                eastDoor,
                southDoor,
                westDoor,
            };
            for (int index = 0; index < orderedAnimators.Length; index++)
            {
                Renderer[] childRenderers =
                    orderedAnimators[index].GetComponentsInChildren<Renderer>(true);
                bool hasIsOpen = orderedAnimators[index].parameters.Any(parameter =>
                    parameter.type == AnimatorControllerParameterType.Bool &&
                    parameter.nameHash == IsOpenId);
                if (childRenderers.Length != 1 ||
                    childRenderers[0] != orderedRenderers[index] ||
                    !hasIsOpen)
                {
                    throw new InvalidOperationException(
                        "Each animated dungeon door requires its matching Renderer and an IsOpen bool parameter.");
                }
            }
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
