namespace BombSwap.Core
{
    public readonly struct ChargerEnemyAdvanceResult
    {
        internal ChargerEnemyAdvanceResult(
            ActorId actorId,
            ChargerEnemyState previousState,
            ChargerEnemyState state,
            CardinalDirection direction,
            bool hasStateTransition,
            EnemyMovementStep movement,
            bool hasMovement,
            bool impactedTarget)
        {
            ActorId = actorId;
            PreviousState = previousState;
            State = state;
            Direction = direction;
            HasStateTransition = hasStateTransition;
            Movement = movement;
            HasMovement = hasMovement;
            ImpactedTarget = impactedTarget;
        }

        public ActorId ActorId { get; }

        public ChargerEnemyState PreviousState { get; }

        public ChargerEnemyState State { get; }

        public CardinalDirection Direction { get; }

        public bool HasStateTransition { get; }

        public EnemyMovementStep Movement { get; }

        public bool HasMovement { get; }

        public bool ImpactedTarget { get; }

        public bool HasActivity => HasStateTransition || HasMovement || ImpactedTarget;
    }
}
