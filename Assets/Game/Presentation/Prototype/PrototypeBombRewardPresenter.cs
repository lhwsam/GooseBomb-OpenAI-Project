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

        private readonly List<GameObject> _candidateVisuals = new List<GameObject>();
        private PrototypeBombDefinitionAsset[] _candidates;
        private IReadOnlyList<GridPosition> _candidateCells;

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

            DungeonBombLoadoutState loadout = host.RunSession.BombLoadoutState;
            if (loadout.HasSelectedReward)
            {
                SelectedDefinitionId = loadout.SecondSlot;
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
                return true;
            }
            return false;
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
