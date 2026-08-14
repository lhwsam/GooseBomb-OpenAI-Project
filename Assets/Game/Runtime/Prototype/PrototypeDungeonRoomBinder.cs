using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap
{
    [DefaultExecutionOrder(-1500)]
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonRoomBinder : MonoBehaviour
    {
        [SerializeField]
        private PrototypeGameSession roomSession;

        [SerializeField]
        private PrototypeDungeonDoorPresenter doorPresenter;

        [SerializeField]
        private Transform gridRoot;

        private PrototypeDungeonRunHost _runHost;
        private CombatRoomDefinition _runtimeRoomDefinition;
        private DungeonRoomNodeId _runtimeRoomId;
        private RoomType _runtimeRoomType;
        private RoomRotation _roomRotation;
        private bool _transitionRequested;

        public PrototypeDungeonRunHost RunHost => _runHost;

        public PrototypeGameSession RoomSession => roomSession;

        public PrototypeDungeonDoorPresenter DoorPresenter => doorPresenter;

        public Transform GridRoot => gridRoot;

        public RoomRotation RoomRotation => _roomRotation;

        public DungeonRoomNodeId RuntimeRoomId => _runtimeRoomId;

        public RoomType RuntimeRoomType => _runtimeRoomType;

        public DungeonBombRewardSelectionStatus TrySelectBombReward(
            BombDefinitionId candidateId)
        {
            if (_runHost == null || roomSession == null || !roomSession.IsReady)
            {
                throw new InvalidOperationException(
                    "Dungeon room is not ready to select a bomb reward.");
            }

            DungeonBombRewardSelectionStatus status =
                _runHost.RunSession.TrySelectBombReward(candidateId);
            if (status != DungeonBombRewardSelectionStatus.Selected)
            {
                return status;
            }

            PrototypeBombDefinitionAsset selected =
                _runHost.BombRewardCatalog.GetDefinition(candidateId);
            if (!roomSession.TryEquipSecondBomb(selected))
            {
                throw new InvalidOperationException(
                    $"Selected bomb reward '{candidateId}' could not fill the empty second slot.");
            }
            WebGlHarnessReporter.Report("bomb-reward-selected-" + candidateId.Value);
            return status;
        }

        public void Configure(
            PrototypeGameSession authoredRoomSession,
            PrototypeDungeonDoorPresenter authoredDoorPresenter,
            Transform authoredGridRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeDungeonRoomBinder before changing its configuration.");
            }
            roomSession = authoredRoomSession ??
                throw new ArgumentNullException(nameof(authoredRoomSession));
            doorPresenter = authoredDoorPresenter ??
                throw new ArgumentNullException(nameof(authoredDoorPresenter));
            gridRoot = authoredGridRoot ??
                throw new ArgumentNullException(nameof(authoredGridRoot));
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (roomSession == null || doorPresenter == null || gridRoot == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonRoomBinder requires session, door presenter, and grid root references.");
            }

            _runHost = FindPrimaryRunHost();
            PrepareRoomBeforeSessionAwake();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (_runHost == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonRoomBinder did not resolve a primary run host.");
            }

            _runHost.RoomCommitted += OnRoomCommitted;
            roomSession.RoomCleared += OnRoomCleared;
            roomSession.PlayerMoved += OnPlayerMoved;
        }

        private void Start()
        {
            if (Application.isPlaying && !_runHost.HasPendingTransition)
            {
                RefreshDoors();
            }
        }

        private void Update()
        {
            if (_transitionRequested || roomSession == null ||
                roomSession.InputReader == null)
            {
                return;
            }

            TryRequestExit(
                roomSession.CurrentGridPosition,
                roomSession.InputReader.CurrentMoveDirection);
        }

        private void OnDisable()
        {
            if (_runHost != null)
            {
                _runHost.RoomCommitted -= OnRoomCommitted;
            }
            if (roomSession != null)
            {
                roomSession.RoomCleared -= OnRoomCleared;
                roomSession.PlayerMoved -= OnPlayerMoved;
            }
        }

        private void PrepareRoomBeforeSessionAwake()
        {
            PrototypeDungeonRunSession run = _runHost.RunSession;
            bool hasPending = _runHost.HasPendingTransition;
            DungeonRoomNodeId roomId = hasPending
                ? _runHost.PendingTransition.TargetRoomId
                : run.CurrentRoomId;
            string expectedSceneName;
            if (!run.TryGetSceneName(roomId, out expectedSceneName) ||
                !string.Equals(
                    SceneManager.GetActiveScene().name,
                    expectedSceneName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Active scene '{SceneManager.GetActiveScene().name}' does not match dungeon room {roomId} scene '{expectedSceneName}'.");
            }

            DungeonRoomNode room = run.Graph.GetRoom(roomId);
            _runtimeRoomId = roomId;
            _runtimeRoomType = room.RoomType;
            CombatRoomDefinition authoredDefinition =
                roomSession.Context.RoomDefinition.CreateCoreDefinition();
            bool roomRequiresCombat = DungeonRunState.RequiresClear(room.RoomType);
            bool roomRequiresBoss = room.RoomType == RoomType.Boss;
            if (roomSession.IsCombatEnabledByDefault != roomRequiresCombat)
            {
                throw new InvalidOperationException(
                    roomRequiresCombat
                        ? $"Dungeon room {room.RoomType} requires an authored combat session."
                        : $"Safe dungeon room {room.RoomType} must disable authored combat.");
            }
            if (roomSession.IsBossEnabledByDefault != roomRequiresBoss)
            {
                throw new InvalidOperationException(
                    roomRequiresBoss
                        ? "Dungeon boss room requires an authored boss session."
                        : $"Dungeon room {room.RoomType} must not enable the boss encounter.");
            }
            if (room.RoomType == RoomType.Combat)
            {
                if (!run.TryGetCombatRoom(
                    roomId,
                    out PrototypeDungeonCombatRoomSelection selection) ||
                    selection.RoomDefinition != roomSession.Context.RoomDefinition)
                {
                    throw new InvalidOperationException(
                        $"Combat scene '{expectedSceneName}' does not use its assigned room asset.");
                }
                _roomRotation = selection.Assignment.Rotation;
                _runtimeRoomDefinition = CombatRoomRotationUtility.Rotate(
                    authoredDefinition,
                    _roomRotation);
            }
            else
            {
                _roomRotation = RoomRotation.None;
                _runtimeRoomDefinition = authoredDefinition;
            }

            gridRoot.localRotation = Quaternion.Euler(
                0f,
                RoomRotationUtility.GetClockwiseDegrees(_roomRotation),
                0f);
            GridPosition playerStart = hasPending
                ? FindExitCell(
                    _runtimeRoomDefinition.Exits,
                    _runHost.PendingTransition.EntryDirection)
                : _runtimeRoomDefinition.PlayerSpawn;
            bool combatEnabledForVisit =
                roomRequiresCombat && !run.RunState.IsCleared(roomId);
            bool bossEnabledForVisit =
                roomRequiresBoss && combatEnabledForVisit;
            PrototypeBombRewardCatalogAsset rewardCatalog =
                _runHost.BombRewardCatalog;
            DungeonBombLoadoutState runLoadout = run.BombLoadoutState ??
                throw new InvalidOperationException(
                    "Dungeon run requires a persistent bomb loadout state.");
            PrototypeBombDefinitionAsset secondSlot = runLoadout.SecondSlot.HasValue
                ? rewardCatalog.GetDefinition(runLoadout.SecondSlot.Value)
                : null;
            roomSession.PrepareRuntimeBombLoadout(
                rewardCatalog.FirstSlot,
                secondSlot,
                rewardCatalog.GetAvailableDefinitions(),
                rewardCatalog.SwapCooldownSeconds);
            roomSession.PrepareRuntimeRoom(
                _runtimeRoomDefinition,
                playerStart,
                combatEnabledForVisit,
                bossEnabledForVisit);
            WebGlHarnessReporter.ReportDungeonRoomReady(
                roomId,
                room.RoomType,
                combatEnabledForVisit,
                run.RunState.IsCleared(roomId));
        }

        private void OnRoomCommitted()
        {
            _transitionRequested = false;
            RefreshDoors();
        }

        private void OnRoomCleared()
        {
            DungeonRoomClearStatus status = _runHost.TryClearCurrentRoom();
            if (status != DungeonRoomClearStatus.Cleared &&
                status != DungeonRoomClearStatus.AlreadyCleared)
            {
                throw new InvalidOperationException(
                    $"Room session cleared a non-clearable dungeon room: {status}.");
            }
            RefreshDoors();
        }

        private void OnPlayerMoved(PlayerMovementStep step)
        {
            TryRequestExit(step.To, step.Direction);
        }

        private void TryRequestExit(
            GridPosition playerPosition,
            CardinalDirection moveDirection)
        {
            if (_transitionRequested || moveDirection == CardinalDirection.None)
            {
                return;
            }

            RoomExitDirection exitDirection = ToExitDirection(moveDirection);
            GridPosition exitCell = FindExitCell(
                _runtimeRoomDefinition.Exits,
                exitDirection);
            if (playerPosition != exitCell)
            {
                return;
            }

            PrototypeDungeonTransitionStartResult result =
                _runHost.RequestTravel(exitDirection);
            _transitionRequested = result.Started;
        }

        private void RefreshDoors()
        {
            doorPresenter.Apply(
                _runHost.RunSession.GetCurrentExitStates(),
                _roomRotation);
        }

        private static PrototypeDungeonRunHost FindPrimaryRunHost()
        {
            PrototypeDungeonRunHost[] hosts =
                FindObjectsByType<PrototypeDungeonRunHost>(
                    FindObjectsInactive.Include);
            PrototypeDungeonRunHost primary = null;
            for (int index = 0; index < hosts.Length; index++)
            {
                if (!hosts[index].IsPrimary)
                {
                    continue;
                }
                if (primary != null)
                {
                    throw new InvalidOperationException(
                        "Multiple primary dungeon run hosts are active.");
                }
                primary = hosts[index];
            }
            return primary ?? throw new InvalidOperationException(
                "Dungeon room requires one primary run host.");
        }

        private static GridPosition FindExitCell(
            IReadOnlyList<RoomExit> exits,
            RoomExitDirection direction)
        {
            for (int index = 0; index < exits.Count; index++)
            {
                if (exits[index].Direction == direction)
                {
                    return exits[index].Cell;
                }
            }
            throw new InvalidOperationException(
                $"Runtime room definition has no {direction} potential exit.");
        }

        private static RoomExitDirection ToExitDirection(
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return RoomExitDirection.North;
                case CardinalDirection.East:
                    return RoomExitDirection.East;
                case CardinalDirection.South:
                    return RoomExitDirection.South;
                case CardinalDirection.West:
                    return RoomExitDirection.West;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Movement direction must be cardinal.");
            }
        }
    }
}
