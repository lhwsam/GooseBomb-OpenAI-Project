using System;
using System.Collections.Generic;
using BombSwap.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBombRewardPresenter : MonoBehaviour
    {
        private static readonly GridPosition[] TwoCandidateCells =
        {
            new GridPosition(-1, 0),
            new GridPosition(1, 0),
        };

        private static readonly GridPosition[] ThreeCandidateCells =
        {
            new GridPosition(-1, 0),
            new GridPosition(0, 0),
            new GridPosition(1, 0),
        };

        [SerializeField]
        private PrototypeDungeonRoomBinder roomBinder;

        private readonly List<GameObject> _candidateVisuals = new List<GameObject>();
        private PrototypeBombDefinitionAsset[] _candidates;
        private IReadOnlyList<GridPosition> _candidateCells;
        private TextMeshProUGUI _instructionLabel;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public bool IsInitialized { get; private set; }

        public int CandidateVisualCount => _candidateVisuals.Count;

        public BombDefinitionId? SelectedDefinitionId { get; private set; }

        public void Configure(PrototypeDungeonRoomBinder authoredRoomBinder)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBombRewardPresenter before changing its configuration.");
            }
            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
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
                    "PrototypeBombRewardPresenter requires a dungeon room binder.");
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
            PrototypeDungeonRunHost host = roomBinder.RunHost;
            if (host == null || host.RunSession == null ||
                host.BombRewardCatalog == null)
            {
                throw new InvalidOperationException(
                    "Bomb reward presenter requires an initialized dungeon run host and reward catalog.");
            }
            if (roomBinder.RuntimeRoomType != RoomType.BombReward)
            {
                throw new InvalidOperationException(
                    "PrototypeBombRewardPresenter can only run in the BombReward room.");
            }

            IReadOnlyList<PrototypeBombDefinitionAsset> candidates =
                host.BombRewardCatalog.RewardCandidates;
            _candidates = new PrototypeBombDefinitionAsset[candidates.Count];
            for (int index = 0; index < candidates.Count; index++)
            {
                _candidates[index] = candidates[index];
            }
            _candidateCells = GetCandidateCells(_candidates.Length);
            CreateInstructionUi();

            DungeonBombLoadoutState loadout = host.RunSession.BombLoadoutState;
            if (loadout.HasSelectedReward)
            {
                SelectedDefinitionId = loadout.SecondSlot;
                _instructionLabel.text =
                    "BOMB REWARD EQUIPPED: " + loadout.SecondSlot.Value.Value;
                IsInitialized = true;
                return;
            }

            for (int index = 0; index < _candidates.Length; index++)
            {
                PrototypeBombDefinitionAsset candidate = _candidates[index];
                candidate.ValidatePresentationReferences();
                if (!roomBinder.RoomSession.GetCell(
                        _candidateCells[index]).IsWalkableTerrain)
                {
                    throw new InvalidOperationException(
                        $"Bomb reward candidate cell {_candidateCells[index]} must be walkable floor.");
                }
                GameObject visual = Instantiate(candidate.BombPrefab, transform);
                visual.name = "RewardChoice-" + candidate.DefinitionId;
                visual.transform.position = roomBinder.RoomSession.GridSpace.GridToWorld(
                    _candidateCells[index]);
                visual.SetActive(true);
                _candidateVisuals.Add(visual);
            }
            _instructionLabel.text = BuildInstructionText();
            IsInitialized = true;
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            TryCollectAt(step.To);
        }

        public bool TryCollectAt(GridPosition playerCell)
        {
            if (!IsInitialized || SelectedDefinitionId.HasValue)
            {
                return false;
            }
            for (int index = 0; index < _candidateCells.Count; index++)
            {
                if (_candidateCells[index] != playerCell)
                {
                    continue;
                }

                BombDefinitionId candidateId = new BombDefinitionId(
                    _candidates[index].DefinitionId);
                DungeonBombRewardSelectionStatus status =
                    roomBinder.TrySelectBombReward(candidateId);
                if (status != DungeonBombRewardSelectionStatus.Selected)
                {
                    return false;
                }

                SelectedDefinitionId = candidateId;
                for (int visualIndex = 0;
                    visualIndex < _candidateVisuals.Count;
                    visualIndex++)
                {
                    _candidateVisuals[visualIndex].SetActive(visualIndex == index);
                }
                _instructionLabel.text = "EQUIPPED SLOT 2: " + candidateId.Value;
                return true;
            }
            return false;
        }

        private string BuildInstructionText()
        {
            if (_candidates.Length == 2)
            {
                return "BOMB REWARD — WALK LEFT FOR " +
                    _candidates[0].DefinitionId + "  /  RIGHT FOR " +
                    _candidates[1].DefinitionId;
            }
            return "BOMB REWARD — WALK ONTO: " +
                _candidates[0].DefinitionId + " / " +
                _candidates[1].DefinitionId + " / " +
                _candidates[2].DefinitionId;
        }

        private void CreateInstructionUi()
        {
            GameObject canvasObject = new GameObject(
                "BombRewardCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            _instructionLabel = PrototypeUiFactory.CreateText(
                "BombRewardInstruction",
                canvasObject.transform,
                22f,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RectTransform rect = _instructionLabel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(1100f, 58f);
            _instructionLabel.color = Color.white;
        }

        private static IReadOnlyList<GridPosition> GetCandidateCells(int candidateCount)
        {
            switch (candidateCount)
            {
                case 2:
                    return TwoCandidateCells;
                case 3:
                    return ThreeCandidateCells;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(candidateCount),
                        candidateCount,
                        "Bomb reward presenter supports two or three candidates.");
            }
        }
    }
}
