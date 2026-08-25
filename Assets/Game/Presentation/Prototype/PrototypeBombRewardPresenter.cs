using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

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

        [SerializeField]
        private PrototypeWorldInteractableView[] candidateViews =
            Array.Empty<PrototypeWorldInteractableView>();

        private readonly List<GameObject> _candidateVisuals =
            new List<GameObject>();
        private readonly List<PrototypeWorldInteractableView> _activeCandidateViews =
            new List<PrototypeWorldInteractableView>();
        private readonly HashSet<GridPosition> _registeredCandidateCells =
            new HashSet<GridPosition>();
        private PrototypeBombDefinitionAsset[] _candidates;
        private IReadOnlyList<GridPosition> _candidateCells;

        public PrototypeDungeonRoomBinder RoomBinder => roomBinder;

        public IReadOnlyList<PrototypeWorldInteractableView> CandidateViews =>
            candidateViews;

        public bool IsInitialized { get; private set; }

        public bool CanInteract { get; private set; }

        public int CandidateVisualCount => _candidateVisuals.Count;

        public BombDefinitionId? SelectedDefinitionId { get; private set; }

        public void Configure(
            PrototypeDungeonRoomBinder authoredRoomBinder,
            PrototypeWorldInteractableView[] authoredCandidateViews)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBombRewardPresenter before changing its configuration.");
            }
            if (authoredCandidateViews == null || authoredCandidateViews.Length != 3)
            {
                throw new ArgumentException(
                    "Bomb reward presenter requires left, center, and right authored choice views.",
                    nameof(authoredCandidateViews));
            }
            for (int index = 0; index < authoredCandidateViews.Length; index++)
            {
                if (authoredCandidateViews[index] == null)
                {
                    throw new ArgumentException(
                        $"Bomb reward choice view {index} is missing.",
                        nameof(authoredCandidateViews));
                }
            }

            roomBinder = authoredRoomBinder ??
                throw new ArgumentNullException(nameof(authoredRoomBinder));
            candidateViews = (PrototypeWorldInteractableView[])
                authoredCandidateViews.Clone();
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
                UnregisterCandidateBlockers();
            }
            CanInteract = false;
            UpdateWorldViews(-1);
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
            if (candidateViews == null || candidateViews.Length != 3)
            {
                throw new InvalidOperationException(
                    "Bomb reward presenter requires three authored choice views.");
            }

            IReadOnlyList<PrototypeBombDefinitionAsset> candidates =
                host.BombRewardCatalog.RewardCandidates;
            _candidates = new PrototypeBombDefinitionAsset[candidates.Count];
            for (int index = 0; index < candidates.Count; index++)
            {
                _candidates[index] = candidates[index];
            }
            _candidateCells = GetCandidateCells(_candidates.Length);
            SelectActiveCandidateViews(_candidates.Length);

            DungeonBombLoadoutState loadout = host.RunSession.BombLoadoutState;
            if (loadout.HasSelectedReward)
            {
                SelectedDefinitionId = loadout.SecondSlot;
            }

            for (int index = 0; index < _candidates.Length; index++)
            {
                PrototypeBombDefinitionAsset candidate = _candidates[index];
                PrototypeWorldInteractableView view = _activeCandidateViews[index];
                candidate.ValidatePresentationReferences();
                if (!view.HasRequiredReferences || view.DynamicContentAnchor == null)
                {
                    throw new InvalidOperationException(
                        $"Bomb reward choice view {index} is missing its visual, effect, prompt, or content anchor.");
                }
                if (!roomBinder.RoomSession.GetCell(
                        _candidateCells[index]).IsWalkableTerrain)
                {
                    throw new InvalidOperationException(
                        $"Bomb reward candidate cell {_candidateCells[index]} must be walkable floor.");
                }

                view.transform.position = roomBinder.RoomSession.GridSpace.GridToWorld(
                    _candidateCells[index]);
                GameObject visual = Instantiate(
                    candidate.BombPrefab,
                    view.DynamicContentAnchor,
                    false);
                visual.name = "RewardChoice-" + candidate.DefinitionId;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.SetActive(true);
                _candidateVisuals.Add(visual);
            }

            IsInitialized = true;
            GridPosition playerCell = roomBinder.RoomSession.CurrentGridPosition;
            EnsureCandidateBlockersRegistered(playerCell);
            UpdateInteractionAvailability(playerCell);
        }

        private void SelectActiveCandidateViews(int candidateCount)
        {
            _activeCandidateViews.Clear();
            for (int index = 0; index < candidateViews.Length; index++)
            {
                candidateViews[index].gameObject.SetActive(false);
            }

            switch (candidateCount)
            {
                case 2:
                    _activeCandidateViews.Add(candidateViews[0]);
                    _activeCandidateViews.Add(candidateViews[2]);
                    break;
                case 3:
                    _activeCandidateViews.AddRange(candidateViews);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(candidateCount),
                        candidateCount,
                        "Bomb reward presenter supports two or three candidates.");
            }

            for (int index = 0; index < _activeCandidateViews.Count; index++)
            {
                _activeCandidateViews[index].gameObject.SetActive(true);
            }
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            EnsureCandidateBlockersRegistered(step.To);
            UpdateInteractionAvailability(step.To);
        }

        private void OnInteractionRequested()
        {
            if (CanInteract)
            {
                TryInteractAt(roomBinder.RoomSession.CurrentGridPosition);
            }
        }

        public bool TryInteractAt(GridPosition playerCell)
        {
            if (!IsInitialized || SelectedDefinitionId.HasValue)
            {
                return false;
            }

            int candidateIndex = GetSingleAdjacentCandidateIndex(playerCell);
            if (candidateIndex < 0)
            {
                return false;
            }

            BombDefinitionId candidateId = new BombDefinitionId(
                _candidates[candidateIndex].DefinitionId);
            DungeonBombRewardSelectionStatus status =
                roomBinder.TrySelectBombReward(candidateId);
            if (status != DungeonBombRewardSelectionStatus.Selected)
            {
                return false;
            }

            SelectedDefinitionId = candidateId;
            CanInteract = false;
            UnregisterCandidateBlocker(candidateIndex);
            UpdateWorldViews(-1);
            return true;
        }

        public bool IsCandidateVisualVisible(int candidateIndex)
        {
            return _candidateVisuals[candidateIndex].activeSelf;
        }

        public bool IsCandidateAvailabilityEffectVisible(int candidateIndex)
        {
            return _activeCandidateViews[candidateIndex]
                .IsAvailabilityEffectVisible;
        }

        public bool IsCandidateInteractionPromptVisible(int candidateIndex)
        {
            return _activeCandidateViews[candidateIndex]
                .IsInteractionPromptVisible;
        }

        private void RefreshInteractionAvailability()
        {
            if (!IsInitialized)
            {
                return;
            }
            GridPosition playerCell = roomBinder.RoomSession.CurrentGridPosition;
            EnsureCandidateBlockersRegistered(playerCell);
            UpdateInteractionAvailability(playerCell);
        }

        private void UpdateInteractionAvailability(GridPosition playerCell)
        {
            int candidateIndex = SelectedDefinitionId.HasValue
                ? -1
                : GetSingleAdjacentCandidateIndex(playerCell);
            CanInteract = candidateIndex >= 0;
            UpdateWorldViews(candidateIndex);
        }

        private int GetSingleAdjacentCandidateIndex(GridPosition playerCell)
        {
            int candidateIndex = -1;
            for (int index = 0; index < _candidateCells.Count; index++)
            {
                if (!playerCell.IsCardinallyAdjacentTo(_candidateCells[index]))
                {
                    continue;
                }
                if (candidateIndex >= 0)
                {
                    return -1;
                }
                candidateIndex = index;
            }
            return candidateIndex;
        }

        private void UpdateWorldViews(int interactableCandidateIndex)
        {
            bool isAvailable = IsInitialized && !SelectedDefinitionId.HasValue;
            int selectedCandidateIndex = GetSelectedCandidateIndex();
            for (int index = 0; index < _activeCandidateViews.Count; index++)
            {
                _activeCandidateViews[index].SetInteractionState(
                    isAvailable,
                    isAvailable && index == interactableCandidateIndex);
                if (index < _candidateVisuals.Count)
                {
                    _candidateVisuals[index].SetActive(
                        index != selectedCandidateIndex);
                }
            }
        }

        private int GetSelectedCandidateIndex()
        {
            if (!SelectedDefinitionId.HasValue || _candidates == null)
            {
                return -1;
            }

            for (int index = 0; index < _candidates.Length; index++)
            {
                var candidateId = new BombDefinitionId(
                    _candidates[index].DefinitionId);
                if (candidateId.Equals(SelectedDefinitionId.Value))
                {
                    return index;
                }
            }
            return -1;
        }

        private void EnsureCandidateBlockersRegistered(GridPosition playerCell)
        {
            for (int index = 0; index < _candidateCells.Count; index++)
            {
                GridPosition candidateCell = _candidateCells[index];
                if (index == GetSelectedCandidateIndex() ||
                    candidateCell == playerCell ||
                    _registeredCandidateCells.Contains(candidateCell))
                {
                    continue;
                }
                if (!roomBinder.RoomSession.TryRegisterInteractable(candidateCell))
                {
                    throw new InvalidOperationException(
                        $"Bomb reward choice cell {candidateCell} could not be reserved.");
                }
                _registeredCandidateCells.Add(candidateCell);
            }
        }

        private void UnregisterCandidateBlocker(int candidateIndex)
        {
            GridPosition candidateCell = _candidateCells[candidateIndex];
            if (!_registeredCandidateCells.Contains(candidateCell))
            {
                return;
            }
            if (!roomBinder.RoomSession.TryUnregisterInteractable(candidateCell))
            {
                throw new InvalidOperationException(
                    $"Bomb reward choice blocker {candidateCell} could not be removed.");
            }
            _registeredCandidateCells.Remove(candidateCell);
        }

        private void UnregisterCandidateBlockers()
        {
            foreach (GridPosition candidateCell in _registeredCandidateCells)
            {
                if (!roomBinder.RoomSession.TryUnregisterInteractable(candidateCell))
                {
                    throw new InvalidOperationException(
                        $"Bomb reward choice blocker {candidateCell} could not be removed.");
                }
            }
            _registeredCandidateCells.Clear();
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
