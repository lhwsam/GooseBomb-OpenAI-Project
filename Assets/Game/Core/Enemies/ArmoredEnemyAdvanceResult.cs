namespace BombSwap.Core
{
    public readonly struct ArmoredEnemyAdvanceResult
    {
        internal ArmoredEnemyAdvanceResult(
            ActorId actorId,
            ArmoredEnemyBehaviorState previousState,
            ArmoredEnemyBehaviorState state,
            CardinalDirection panicDirection,
            GridPosition panicDestination,
            int panicPathCellCount,
            EnemyMovementStep movement,
            bool hasMovement)
        {
            ActorId = actorId;
            PreviousState = previousState;
            State = state;
            PanicDirection = panicDirection;
            PanicDestination = panicDestination;
            PanicPathCellCount = panicPathCellCount;
            Movement = movement;
            HasMovement = hasMovement;
        }

        public ActorId ActorId { get; }

        public ArmoredEnemyBehaviorState PreviousState { get; }

        public ArmoredEnemyBehaviorState State { get; }

        public CardinalDirection PanicDirection { get; }

        public GridPosition PanicDestination { get; }

        public int PanicPathCellCount { get; }

        public EnemyMovementStep Movement { get; }

        public bool HasMovement { get; }

        public bool HasStateTransition => PreviousState != State;

        public bool HasActivity => HasStateTransition || HasMovement;
    }
}
