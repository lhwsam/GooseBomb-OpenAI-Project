using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeRecoveryPickupPresenter : MonoBehaviour
    {
        public const int DefaultRecoveryAmount = 2;

        private static readonly Color PickupColor =
            new Color(0.22f, 0.95f, 0.48f, 1f);

        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        [SerializeField]
        private int recoveryAmount = DefaultRecoveryAmount;

        [SerializeField]
        private Vector2Int pickupCell = Vector2Int.zero;

        [SerializeField]
        private PrototypeWorldInteractableView worldView;

        private GridPosition _corePickupCell;
        private bool _isBlockerRegistered;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public int RecoveryAmount => recoveryAmount;

        public Vector2Int PickupCell => pickupCell;

        public PrototypeWorldInteractableView WorldView => worldView;

        public static Color DefaultPickupColor => PickupColor;

        public bool IsInitialized { get; private set; }

        public bool IsConsumed { get; private set; }

        public bool IsVisualVisible =>
            worldView != null && worldView.IsVisualVisible;

        public bool IsAvailabilityEffectVisible =>
            worldView != null && worldView.IsAvailabilityEffectVisible;

        public bool IsInteractionPromptVisible =>
            worldView != null && worldView.IsInteractionPromptVisible;

        public bool CanInteract { get; private set; }

        public DungeonRecoveryUseStatus LastStatus { get; private set; }

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            PrototypeWorldInteractableView authoredWorldView,
            int authoredRecoveryAmount = DefaultRecoveryAmount,
            Vector2Int? authoredPickupCell = null)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeRecoveryPickupPresenter before changing its configuration.");
            }
            if (authoredRecoveryAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredRecoveryAmount),
                    authoredRecoveryAmount,
                    "Recovery amount must be positive.");
            }

            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
            worldView = authoredWorldView ??
                throw new ArgumentNullException(nameof(authoredWorldView));
            recoveryAmount = authoredRecoveryAmount;
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
                    "PrototypeRecoveryPickupPresenter requires a dungeon room binder.");
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
                    "Recovery pickup requires an initialized dungeon run host.");
            }
            if (roomBinder.RuntimeRoomType != RoomType.Recovery)
            {
                throw new InvalidOperationException(
                    "PrototypeRecoveryPickupPresenter can only run in the Recovery room.");
            }
            if (recoveryAmount <= 0)
            {
                throw new InvalidOperationException(
                    "Recovery pickup amount must be positive.");
            }
            if (worldView == null || !worldView.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Recovery shrine requires a configured world interaction view.");
            }

            _corePickupCell = ToCorePosition(pickupCell);
            if (!roomBinder.RoomSession.GetCell(_corePickupCell).IsWalkableTerrain)
            {
                throw new InvalidOperationException(
                    $"Recovery pickup cell {_corePickupCell} must be walkable floor.");
            }

            IsConsumed = roomBinder.IsCurrentRecoveryConsumed;
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
            if (!IsInitialized || IsConsumed || !CanInteract)
            {
                return false;
            }

            DungeonRecoveryUseResult result =
                roomBinder.TryUseRecovery(recoveryAmount);
            LastStatus = result.Status;
            switch (result.Status)
            {
                case DungeonRecoveryUseStatus.Restored:
                    IsConsumed = true;
                    CanInteract = false;
                    UpdateWorldView();
                    return true;
                case DungeonRecoveryUseStatus.AtFullHealth:
                    return false;
                case DungeonRecoveryUseStatus.AlreadyConsumed:
                    IsConsumed = true;
                    CanInteract = false;
                    UpdateWorldView();
                    return false;
                default:
                    return false;
            }
        }

        private void UpdateInteractionAvailability(GridPosition playerCell)
        {
            CanInteract = IsInitialized && !IsConsumed &&
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
                    $"Recovery pickup cell {_corePickupCell} could not be reserved.");
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
                worldView.SetInteractionState(!IsConsumed, CanInteract);
            }
        }

        private static GridPosition ToCorePosition(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
