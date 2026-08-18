namespace BombSwap.Core
{
    public readonly struct SelfDestructEnemyAdvanceResult
    {
        internal SelfDestructEnemyAdvanceResult(
            ActorId actorId,
            SelfDestructEnemyState previousState,
            SelfDestructEnemyState state,
            GridPosition targetPosition,
            EnemyMovementStep movement,
            bool hasMovement,
            bool shouldArm,
            BombId triggeringExplosionId)
        {
            ActorId = actorId;
            PreviousState = previousState;
            State = state;
            TargetPosition = targetPosition;
            Movement = movement;
            HasMovement = hasMovement;
            ShouldArm = shouldArm;
            TriggeringExplosionId = triggeringExplosionId;
        }

        public ActorId ActorId { get; }

        public SelfDestructEnemyState PreviousState { get; }

        public SelfDestructEnemyState State { get; }

        public GridPosition TargetPosition { get; }

        public EnemyMovementStep Movement { get; }

        public bool HasMovement { get; }

        public bool ShouldArm { get; }

        public BombId TriggeringExplosionId { get; }

        public bool HasStateTransition => PreviousState != State;

        public bool HasActivity => HasStateTransition || HasMovement || ShouldArm;
    }
}
