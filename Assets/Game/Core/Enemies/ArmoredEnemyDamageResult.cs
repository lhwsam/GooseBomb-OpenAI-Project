namespace BombSwap.Core
{
    public readonly struct ArmoredEnemyDamageResult
    {
        internal ArmoredEnemyDamageResult(
            EnemyDamageResult damage,
            ArmoredEnemyState previousState,
            ArmoredEnemyState currentState,
            ArmoredEnemyBehaviorState previousBehaviorState,
            ArmoredEnemyBehaviorState currentBehaviorState)
        {
            Damage = damage;
            PreviousState = previousState;
            CurrentState = currentState;
            PreviousBehaviorState = previousBehaviorState;
            CurrentBehaviorState = currentBehaviorState;
        }

        public EnemyDamageResult Damage { get; }

        public ArmoredEnemyState PreviousState { get; }

        public ArmoredEnemyState CurrentState { get; }

        public ArmoredEnemyBehaviorState PreviousBehaviorState { get; }

        public ArmoredEnemyBehaviorState CurrentBehaviorState { get; }

        public bool HasStateTransition => Damage.WasApplied && PreviousState != CurrentState;

        public bool HasBehaviorTransition =>
            Damage.WasApplied && PreviousBehaviorState != CurrentBehaviorState;

        public bool ArmorWasBroken =>
            HasStateTransition &&
            PreviousState == ArmoredEnemyState.Armored &&
            CurrentState == ArmoredEnemyState.Broken;

        public bool WasFatal => Damage.WasFatal && CurrentState == ArmoredEnemyState.Dead;
    }
}
