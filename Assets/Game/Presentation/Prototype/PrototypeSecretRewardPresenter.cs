using System;
using BombSwap.Core;
using TMPro;
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
        private PrototypeInstructionView viewPrefab;

        [SerializeField]
        private int tokenReward = DefaultTokenReward;

        [SerializeField]
        private Vector2Int pickupCell = Vector2Int.zero;

        [SerializeField]
        private Material pickupMaterial;

        private GameObject _cacheVisual;
        private TextMeshProUGUI _instructionLabel;
        private PrototypeInstructionView _viewInstance;
        private GridPosition _corePickupCell;
        private bool _isBlockerRegistered;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public PrototypeInstructionView ViewPrefab => viewPrefab;

        public PrototypeInstructionView ViewInstance => _viewInstance;

        public int TokenReward => tokenReward;

        public Vector2Int PickupCell => pickupCell;

        public Material PickupMaterial => pickupMaterial;

        public static Color DefaultRewardColor => RewardColor;

        public bool IsInitialized { get; private set; }

        public bool IsCollected { get; private set; }

        public bool IsVisualVisible =>
            _cacheVisual != null && _cacheVisual.activeSelf;

        public bool CanInteract { get; private set; }

        public DungeonSecretRewardCollectStatus LastStatus { get; private set; }

        public string InstructionText =>
            _instructionLabel != null ? _instructionLabel.text : string.Empty;

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

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            Material authoredPickupMaterial,
            PrototypeInstructionView authoredViewPrefab,
            int authoredTokenReward = DefaultTokenReward,
            Vector2Int? authoredPickupCell = null)
        {
            Configure(
                authoredRoomBinder,
                authoredPickupMaterial,
                authoredTokenReward,
                authoredPickupCell);
            BindViewPrefab(authoredViewPrefab);
        }

        public void BindViewPrefab(PrototypeInstructionView authoredViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeSecretRewardPresenter before changing its view prefab.");
            }

            viewPrefab = authoredViewPrefab ??
                throw new ArgumentNullException(nameof(authoredViewPrefab));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (roomBinder == null || roomBinder.RoomSession == null ||
                viewPrefab == null || !viewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypeSecretRewardPresenter requires a dungeon room binder and instruction view prefab.");
            }

            roomBinder.RoomSession.Ready += OnSessionReady;
            roomBinder.RoomSession.PlayerMoved += OnPlayerMoved;
            roomBinder.RoomSession.InteractionRequested += OnInteractionRequested;
            if (roomBinder.RoomSession.IsReady)
            {
                Initialize();
                RefreshInteractionAvailability();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                Initialize();
                RefreshInteractionAvailability();
            }
        }

        private void OnDisable()
        {
            if (roomBinder != null && roomBinder.RoomSession != null)
            {
                roomBinder.RoomSession.Ready -= OnSessionReady;
                roomBinder.RoomSession.PlayerMoved -= OnPlayerMoved;
                roomBinder.RoomSession.InteractionRequested -= OnInteractionRequested;
                if (_isBlockerRegistered)
                {
                    roomBinder.RoomSession.TryUnregisterInteractable(_corePickupCell);
                    _isBlockerRegistered = false;
                }
            }
        }

        private void OnSessionReady()
        {
            Initialize();
            RefreshInteractionAvailability();
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

            _corePickupCell = ToCorePosition(pickupCell);
            if (!roomBinder.RoomSession.GetCell(_corePickupCell).IsWalkableTerrain)
            {
                throw new InvalidOperationException(
                    $"Secret reward cell {_corePickupCell} must be walkable floor.");
            }

            CreateInstructionUi();
            IsCollected = roomBinder.IsCurrentSecretRewardCollected;
            if (IsCollected)
            {
                _instructionLabel.text = "SECRET CACHE COLLECTED";
            }
            else
            {
                CreateCacheVisual(_corePickupCell);
                _instructionLabel.text = string.Empty;
            }
            IsInitialized = true;
            GridPosition playerCell = roomBinder.RoomSession.CurrentGridPosition;
            EnsureBlockerRegistered(playerCell);
            UpdateInteractionAvailability(playerCell);
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            EnsureBlockerRegistered(step.To);
            UpdateInteractionAvailability(step.To);
        }

        private void OnInteractionRequested()
        {
            if (CanInteract)
            {
                TryInteract();
            }
        }

        public bool TryInteract()
        {
            if (!IsInitialized || IsCollected || !CanInteract)
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
                    CanInteract = false;
                    UnregisterBlocker();
                    if (_cacheVisual != null)
                    {
                        _cacheVisual.SetActive(false);
                    }
                    _instructionLabel.text =
                        "SECRET CACHE +" + result.AwardedTokens +
                        "  |  ROOM TOKENS " + result.CurrentTokens;
                    return true;
                case DungeonSecretRewardCollectStatus.AlreadyCollected:
                    IsCollected = true;
                    CanInteract = false;
                    UnregisterBlocker();
                    if (_cacheVisual != null)
                    {
                        _cacheVisual.SetActive(false);
                    }
                    _instructionLabel.text = "SECRET CACHE COLLECTED";
                    return false;
                default:
                    return false;
            }
        }

        public bool TryInteractAt(GridPosition playerCell)
        {
            UpdateInteractionAvailability(playerCell);
            return TryInteract();
        }

        private void UpdateInteractionAvailability(GridPosition playerCell)
        {
            CanInteract = IsInitialized && !IsCollected &&
                playerCell.IsCardinallyAdjacentTo(_corePickupCell);
            if (_instructionLabel != null && !IsCollected)
            {
                _instructionLabel.text = CanInteract
                    ? "E — OPEN SECRET CACHE"
                    : string.Empty;
            }
        }

        private void UnregisterBlocker()
        {
            if (!_isBlockerRegistered)
            {
                return;
            }
            if (!roomBinder.RoomSession.TryUnregisterInteractable(_corePickupCell))
            {
                throw new InvalidOperationException(
                    $"Secret reward blocker {_corePickupCell} could not be removed.");
            }
            _isBlockerRegistered = false;
        }

        private void EnsureBlockerRegistered(GridPosition playerCell)
        {
            if (_isBlockerRegistered || IsCollected || playerCell == _corePickupCell)
            {
                return;
            }
            if (!roomBinder.RoomSession.TryRegisterInteractable(_corePickupCell))
            {
                throw new InvalidOperationException(
                    $"Secret reward cell {_corePickupCell} could not be reserved.");
            }
            _isBlockerRegistered = true;
        }

        private void RefreshInteractionAvailability()
        {
            if (!IsInitialized)
            {
                return;
            }
            GridPosition playerCell = roomBinder.RoomSession.CurrentGridPosition;
            EnsureBlockerRegistered(playerCell);
            UpdateInteractionAvailability(playerCell);
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

        private void CreateInstructionUi()
        {
            _viewInstance = Instantiate(viewPrefab, transform, false);
            _viewInstance.name = viewPrefab.name;
            if (!_viewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated secret reward instruction view is missing required references.");
            }

            _instructionLabel = _viewInstance.InstructionLabel;
        }

        private static GridPosition ToCorePosition(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
