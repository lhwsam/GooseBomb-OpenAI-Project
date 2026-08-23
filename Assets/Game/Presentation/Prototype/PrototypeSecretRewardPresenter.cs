using System;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private TextMeshProUGUI _instructionLabel;

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

            CreateInstructionUi();
            IsCollected = roomBinder.IsCurrentSecretRewardCollected;
            if (IsCollected)
            {
                _instructionLabel.text = "SECRET CACHE COLLECTED";
            }
            else
            {
                CreateCacheVisual(corePickupCell);
                _instructionLabel.text =
                    "SECRET CACHE  |  ROOM TOKENS +" + tokenReward;
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
                    _instructionLabel.text =
                        "SECRET CACHE +" + result.AwardedTokens +
                        "  |  ROOM TOKENS " + result.CurrentTokens;
                    return true;
                case DungeonSecretRewardCollectStatus.AlreadyCollected:
                    IsCollected = true;
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
            GameObject canvasObject = new GameObject(
                "SecretRewardCanvas",
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
                "SecretRewardInstruction",
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
            _instructionLabel.color = RewardColor;
        }

        private static GridPosition ToCorePosition(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
