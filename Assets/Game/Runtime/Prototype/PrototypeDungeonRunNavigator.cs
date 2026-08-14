using System;
using BombSwap.Core;

namespace BombSwap
{
    public enum PrototypeDungeonTransitionStartStatus
    {
        Started = 0,
        NotConnected = 1,
        Locked = 2,
        TransitionAlreadyPending = 3,
        ContentUnavailable = 4,
        SceneNotLoadable = 5,
    }

    public enum PrototypeDungeonTransitionCommitStatus
    {
        Committed = 0,
        NoTransitionPending = 1,
        SceneMismatch = 2,
        CoreRejected = 3,
    }

    public readonly struct PrototypeDungeonPendingTransition
    {
        internal PrototypeDungeonPendingTransition(
            DungeonRoomNodeId fromRoomId,
            DungeonRoomNodeId targetRoomId,
            RoomExitDirection travelDirection,
            string targetSceneName)
        {
            FromRoomId = fromRoomId;
            TargetRoomId = targetRoomId;
            TravelDirection = travelDirection;
            EntryDirection = RoomRotationUtility.Rotate(
                travelDirection,
                RoomRotation.Clockwise180);
            TargetSceneName = targetSceneName;
        }

        public DungeonRoomNodeId FromRoomId { get; }

        public DungeonRoomNodeId TargetRoomId { get; }

        public RoomExitDirection TravelDirection { get; }

        public RoomExitDirection EntryDirection { get; }

        public string TargetSceneName { get; }
    }

    public readonly struct PrototypeDungeonTransitionStartResult
    {
        internal PrototypeDungeonTransitionStartResult(
            PrototypeDungeonTransitionStartStatus status,
            PrototypeDungeonPendingTransition transition)
        {
            Status = status;
            Transition = transition;
        }

        public PrototypeDungeonTransitionStartStatus Status { get; }

        public PrototypeDungeonPendingTransition Transition { get; }

        public bool Started => Status == PrototypeDungeonTransitionStartStatus.Started;
    }

    public sealed class PrototypeDungeonRunNavigator
    {
        private PrototypeDungeonPendingTransition _pendingTransition;

        public PrototypeDungeonRunNavigator(PrototypeDungeonRunSession runSession)
        {
            RunSession = runSession ?? throw new ArgumentNullException(nameof(runSession));
        }

        public PrototypeDungeonRunSession RunSession { get; }

        public bool HasPendingTransition { get; private set; }

        public PrototypeDungeonPendingTransition PendingTransition
        {
            get
            {
                if (!HasPendingTransition)
                {
                    throw new InvalidOperationException(
                        "No dungeon scene transition is pending.");
                }
                return _pendingTransition;
            }
        }

        public PrototypeDungeonTransitionStartResult TryBeginTravel(
            RoomExitDirection direction,
            Predicate<string> canLoadScene)
        {
            RoomRotationUtility.Rotate(direction, RoomRotation.None);
            if (canLoadScene == null)
            {
                throw new ArgumentNullException(nameof(canLoadScene));
            }
            if (HasPendingTransition)
            {
                return StartResult(
                    PrototypeDungeonTransitionStartStatus.TransitionAlreadyPending);
            }

            DungeonRoomExitState exit = RunSession.RunState.GetCurrentExitState(direction);
            if (!exit.IsConnected)
            {
                return StartResult(PrototypeDungeonTransitionStartStatus.NotConnected);
            }
            if (!exit.CanTravel)
            {
                return StartResult(PrototypeDungeonTransitionStartStatus.Locked);
            }
            if (!RunSession.TryGetSceneName(exit.TargetRoomId, out string sceneName))
            {
                return StartResult(PrototypeDungeonTransitionStartStatus.ContentUnavailable);
            }
            if (!canLoadScene(sceneName))
            {
                return StartResult(PrototypeDungeonTransitionStartStatus.SceneNotLoadable);
            }

            _pendingTransition = new PrototypeDungeonPendingTransition(
                RunSession.CurrentRoomId,
                exit.TargetRoomId,
                direction,
                sceneName);
            HasPendingTransition = true;
            return new PrototypeDungeonTransitionStartResult(
                PrototypeDungeonTransitionStartStatus.Started,
                _pendingTransition);
        }

        public PrototypeDungeonTransitionCommitStatus CommitLoadedScene(
            string loadedSceneName)
        {
            if (string.IsNullOrWhiteSpace(loadedSceneName))
            {
                throw new ArgumentException(
                    "Loaded dungeon scene name cannot be empty.",
                    nameof(loadedSceneName));
            }
            if (!HasPendingTransition)
            {
                return PrototypeDungeonTransitionCommitStatus.NoTransitionPending;
            }
            if (!string.Equals(
                loadedSceneName,
                _pendingTransition.TargetSceneName,
                StringComparison.Ordinal))
            {
                return PrototypeDungeonTransitionCommitStatus.SceneMismatch;
            }
            if (RunSession.CurrentRoomId != _pendingTransition.FromRoomId)
            {
                return PrototypeDungeonTransitionCommitStatus.CoreRejected;
            }

            DungeonTravelResult travel = RunSession.TryTravel(
                _pendingTransition.TravelDirection);
            if (!travel.Moved || travel.TargetRoomId != _pendingTransition.TargetRoomId)
            {
                return PrototypeDungeonTransitionCommitStatus.CoreRejected;
            }

            HasPendingTransition = false;
            _pendingTransition = default;
            return PrototypeDungeonTransitionCommitStatus.Committed;
        }

        public void CancelPendingTransition()
        {
            HasPendingTransition = false;
            _pendingTransition = default;
        }

        private static PrototypeDungeonTransitionStartResult StartResult(
            PrototypeDungeonTransitionStartStatus status)
        {
            return new PrototypeDungeonTransitionStartResult(status, default);
        }
    }
}
