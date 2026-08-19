using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public readonly struct ThrowerEnemyAdvanceResult
    {
        public ThrowerEnemyAdvanceResult(
            ActorId actorId,
            ThrowerEnemyState previousState,
            ThrowerEnemyState state,
            IReadOnlyList<GridPosition> lockedTargets,
            EnemyMovementStep movement,
            TimeSpan movementDuration,
            bool hasMovement,
            bool shouldLaunch)
        {
            ActorId = actorId;
            PreviousState = previousState;
            State = state;
            LockedTargets = lockedTargets ??
                throw new ArgumentNullException(nameof(lockedTargets));
            LockedTarget = lockedTargets.Count > 0 ? lockedTargets[0] : default;
            Movement = movement;
            MovementDuration = movementDuration;
            HasMovement = hasMovement;
            ShouldLaunch = shouldLaunch;
        }

        public ActorId ActorId { get; }

        public ThrowerEnemyState PreviousState { get; }

        public ThrowerEnemyState State { get; }

        public GridPosition LockedTarget { get; }

        public IReadOnlyList<GridPosition> LockedTargets { get; }

        public EnemyMovementStep Movement { get; }

        public TimeSpan MovementDuration { get; }

        public bool HasMovement { get; }

        public bool ShouldLaunch { get; }

        public bool HasStateTransition => PreviousState != State;

        public bool HasActivity => HasMovement || ShouldLaunch || HasStateTransition;
    }
}
