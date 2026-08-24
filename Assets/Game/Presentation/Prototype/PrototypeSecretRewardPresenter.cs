using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSecretRewardPresenter : MonoBehaviour
    {
        public const int DefaultTokenReward = 3;

        private static readonly Color RewardColor =
            new Color(1f, 0.68f, 0.12f, 1f);

        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        [SerializeField]
        private int tokenReward = DefaultTokenReward;

        [SerializeField]
        private Vector2Int pickupCell = Vector2Int.zero;

        [SerializeField]
        private Material pickupMaterial;

        private GameObject _cacheVisual;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public int TokenReward => tokenReward;

        public Vector2Int PickupCell => pickupCell;

        public Material PickupMaterial => pickupMaterial;

        public static Color DefaultRewardColor => RewardColor;

        public bool IsInitialized { get; private set; }

        public bool IsCollected { get; private set; }

        public bool IsVisualVisible =>
            _cacheVisual != null && _cacheVisual.activeSelf;

        public DungeonSecretRewardCollectStatus LastStatus { get; private set; }

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            Material authoredPickupMaterial,
            int authoredTokenReward = DefaultTokenReward,
            Vector2Int? authoredPickupCell = null)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeSecretRewardPresenter before changing its configuration.");
            }
            if (authoredTokenReward <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredTokenReward),
                    authoredTokenReward,
                    "Secret-room token reward must be positive.");
            }

            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
            pickupMaterial = authoredPickupMaterial ??
                throw new ArgumentNullException(nameof(authoredPickupMaterial));
            tokenReward = authoredTokenReward;
            pickupCell = authoredPickupCell ?? Vector2Int.zero;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (roomBinder == null || roomBinder.RoomSession == null)
            {
                throw new InvalidOperationException(
                    "PrototypeSecretRewardPresenter requires a dungeon room binder.");
            }

            roomBinder.RoomSession.Ready += OnSessionReady;
            roomBinder.RoomSession.PlayerMoved += OnPlayerMoved;
            if (roomBinder.RoomSession.IsReady)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            if (roomBinder != null && roomBinder.RoomSession != null)
            {
                roomBinder.RoomSession.Ready -= OnSessionReady;
                roomBinder.RoomSession.PlayerMoved -= OnPlayerMoved;
            }
        }

        private void OnSessionReady()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            if (roomBinder.RunHost == null ||
                roomBinder.RunHost.RunSession == null)
            {
                throw new InvalidOperationException(
                    "Secret reward requires an initialized dungeon run host.");
            }
            if (roomBinder.RuntimeRoomType != RoomType.Secret)
            {
                throw new InvalidOperationException(
                    "PrototypeSecretRewardPresenter can only run in the Secret room.");
            }
            if (tokenReward <= 0 || pickupMaterial == null)
            {
                throw new InvalidOperationException(
                    "Secret reward requires a positive token value and shared material.");
            }

            GridPosition corePickupCell = ToCorePosition(pickupCell);
            if (!roomBinder.RoomSession.GetCell(corePickupCell).IsWalkableTerrain)
            {
                throw new InvalidOperationException(
                    $"Secret reward cell {corePickupCell} must be walkable floor.");
            }

            IsCollected = roomBinder.IsCurrentSecretRewardCollected;
            if (!IsCollected)
            {
                CreateCacheVisual(corePickupCell);
            }
            IsInitialized = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            TryCollectAt(step.To);
        }

        public bool TryCollectAt(GridPosition playerCell)
        {
            if (!IsInitialized || IsCollected ||
                playerCell != ToCorePosition(pickupCell))
            {
                return false;
            }

            DungeonSecretRewardCollectResult result =
                roomBinder.TryCollectSecretReward(tokenReward);
            LastStatus = result.Status;
            switch (result.Status)
            {
                case DungeonSecretRewardCollectStatus.Collected:
                    IsCollected = true;
                    if (_cacheVisual != null)
                    {
                        _cacheVisual.SetActive(false);
                    }
                    return true;
                case DungeonSecretRewardCollectStatus.AlreadyCollected:
                    IsCollected = true;
                    if (_cacheVisual != null)
                    {
                        _cacheVisual.SetActive(false);
                    }
                    return false;
                default:
                    return false;
            }
        }

        private void CreateCacheVisual(GridPosition cell)
        {
            _cacheVisual = new GameObject("SecretRewardCacheVisual");
            _cacheVisual.transform.SetParent(transform, true);
            _cacheVisual.transform.position =
                roomBinder.RoomSession.GridSpace.GridToWorld(cell);

            CreateCachePart(
                "Base",
                new Vector3(0f, 0.35f, 0f),
                new Vector3(0.9f, 0.55f, 0.7f));
            CreateCachePart(
                "Lid",
                new Vector3(0f, 0.7f, -0.04f),
                new Vector3(0.96f, 0.22f, 0.76f));
        }

        private void CreateCachePart(
            string objectName,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = objectName;
            part.transform.SetParent(_cacheVisual.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    "Secret reward cache primitive requires a renderer.");
            }
            renderer.sharedMaterial = pickupMaterial;
        }

        private static GridPosition ToCorePosition(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
