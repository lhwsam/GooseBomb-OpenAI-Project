using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private Material pickupMaterial;

        private GameObject _pickupVisual;
        private TextMeshProUGUI _instructionLabel;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public int RecoveryAmount => recoveryAmount;

        public Vector2Int PickupCell => pickupCell;

        public Material PickupMaterial => pickupMaterial;

        public static Color DefaultPickupColor => PickupColor;

        public bool IsInitialized { get; private set; }

        public bool IsConsumed { get; private set; }

        public bool IsVisualVisible =>
            _pickupVisual != null && _pickupVisual.activeSelf;

        public DungeonRecoveryUseStatus LastStatus { get; private set; }

        public string InstructionText =>
            _instructionLabel != null ? _instructionLabel.text : string.Empty;

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            Material authoredPickupMaterial,
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
            pickupMaterial = authoredPickupMaterial ??
                throw new ArgumentNullException(nameof(authoredPickupMaterial));
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
            if (pickupMaterial == null)
            {
                throw new InvalidOperationException(
                    "Recovery pickup requires a WebGL-compatible shared material.");
            }

            GridPosition corePickupCell = ToCorePosition(pickupCell);
            if (!roomBinder.RoomSession.GetCell(corePickupCell).IsWalkableTerrain)
            {
                throw new InvalidOperationException(
                    $"Recovery pickup cell {corePickupCell} must be walkable floor.");
            }

            CreateInstructionUi();
            IsConsumed = roomBinder.IsCurrentRecoveryConsumed;
            if (IsConsumed)
            {
                _instructionLabel.text = "RECOVERY USED";
            }
            else
            {
                CreatePickupVisual(corePickupCell);
                _instructionLabel.text =
                    "RECOVERY +" + recoveryAmount +
                    " — WALK ONTO THE GREEN CAPSULE";
            }
            IsInitialized = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            TryCollectAt(step.To);
        }

        public bool TryCollectAt(GridPosition playerCell)
        {
            if (!IsInitialized || IsConsumed ||
                playerCell != ToCorePosition(pickupCell))
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
                    if (_pickupVisual != null)
                    {
                        _pickupVisual.SetActive(false);
                    }
                    _instructionLabel.text =
                        "RECOVERED +" + result.RestoredHealth +
                        "  |  HP " + result.CurrentHealth + " / " +
                        roomBinder.RoomSession.MaxHealth;
                    return true;
                case DungeonRecoveryUseStatus.AtFullHealth:
                    _instructionLabel.text =
                        "HP FULL — RECOVERY SAVED FOR A LATER VISIT";
                    return false;
                case DungeonRecoveryUseStatus.AlreadyConsumed:
                    IsConsumed = true;
                    if (_pickupVisual != null)
                    {
                        _pickupVisual.SetActive(false);
                    }
                    _instructionLabel.text = "RECOVERY USED";
                    return false;
                default:
                    return false;
            }
        }

        private void CreatePickupVisual(GridPosition cell)
        {
            _pickupVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _pickupVisual.name = "RecoveryPickupVisual";
            _pickupVisual.transform.SetParent(transform, true);
            _pickupVisual.transform.position =
                roomBinder.RoomSession.GridSpace.GridToWorld(cell) +
                Vector3.up * 0.55f;
            _pickupVisual.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            Collider pickupCollider = _pickupVisual.GetComponent<Collider>();
            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }
            Renderer pickupRenderer = _pickupVisual.GetComponent<Renderer>();
            if (pickupRenderer == null)
            {
                throw new InvalidOperationException(
                    "Recovery pickup primitive requires a renderer.");
            }
            pickupRenderer.sharedMaterial = pickupMaterial;
        }

        private void CreateInstructionUi()
        {
            GameObject canvasObject = new GameObject(
                "RecoveryPickupCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            PrototypeUiFactory.ConfigureCanvasScaler(scaler);

            _instructionLabel = PrototypeUiFactory.CreateText(
                "RecoveryPickupInstruction",
                canvasObject.transform,
                22f,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RectTransform rect = _instructionLabel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(1100f, 58f);
            _instructionLabel.color = PickupColor;
        }

        private static GridPosition ToCorePosition(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
