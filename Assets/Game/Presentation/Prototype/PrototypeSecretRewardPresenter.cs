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
        private PrototypeWorldInteractableView worldView;

        private GridPosition _corePickupCell;
        private bool _isBlockerRegistered;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public int TokenReward => tokenReward;

        public Vector2Int PickupCell => pickupCell;

        public PrototypeWorldInteractableView WorldView => worldView;

        public static Color DefaultRewardColor => RewardColor;

        public bool IsInitialized { get; private set; }

        public bool IsCollected { get; private set; }

        public bool IsVisualVisible =>
            worldView != null && worldView.IsVisualVisible;

        public bool IsAvailabilityEffectVisible =>
            worldView != null && worldView.IsAvailabilityEffectVisible;

        public bool IsInteractionPromptVisible =>
            worldView != null && worldView.IsInteractionPromptVisible;

        public bool CanInteract { get; private set; }

        public DungeonSecretRewardCollectStatus LastStatus { get; private set; }

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            PrototypeWorldInteractableView authoredWorldView,
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
            worldView = authoredWorldView ??
                throw new ArgumentNullException(nameof(authoredWorldView));
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
            if (!Application.isPlaying)
            {
                return;
            }
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
            CanInteract = false;
            UpdateWorldView();
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
            if (tokenReward <= 0 ||
                worldView == null ||
                !worldView.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Secret reward requires a positive token value and configured world interaction view.");
            }

            _corePickupCell = ToCorePosition(pickupCell);
            if (!roomBinder.RoomSession.GetCell(_corePickupCell).IsWalkableTerrain)
            {
                throw new InvalidOperationException(
                    $"Secret reward cell {_corePickupCell} must be walkable floor.");
            }

            IsCollected = roomBinder.IsCurrentSecretRewardCollected;
            worldView.transform.position =
                roomBinder.RoomSession.GridSpace.GridToWorld(_corePickupCell);
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
                    UpdateWorldView();
                    return true;
                case DungeonSecretRewardCollectStatus.AlreadyCollected:
                    IsCollected = true;
                    CanInteract = false;
                    UpdateWorldView();
                    return false;
                default:
                    return false;
            }
        }

        private void UpdateInteractionAvailability(GridPosition playerCell)
        {
            CanInteract = IsInitialized && !IsCollected &&
                playerCell.IsCardinallyAdjacentTo(_corePickupCell);
            UpdateWorldView();
        }

        private void EnsureBlockerRegistered(GridPosition playerCell)
        {
            if (_isBlockerRegistered || playerCell == _corePickupCell)
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

        private void UpdateWorldView()
        {
            if (worldView != null)
            {
                worldView.SetInteractionState(!IsCollected, CanInteract);
            }
        }

        private static GridPosition ToCorePosition(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
