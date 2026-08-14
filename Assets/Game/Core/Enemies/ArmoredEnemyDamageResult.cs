namespace BombSwap.Core
{
    public readonly struct ArmoredEnemyDamageResult
    {
        internal ArmoredEnemyDamageResult(
            EnemyDamageResult damage,
            ArmoredEnemyState previousState,
            ArmoredEnemyState currentState)
        {
            Damage = damage;
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public EnemyDamageResult Damage { get; }

        public ArmoredEnemyState PreviousState { get; }

        public ArmoredEnemyState CurrentState { get; }

        public bool HasStateTransition => Damage.WasApplied && PreviousState != CurrentState;

        public bool ArmorWasBroken =>
            HasStateTransition &&
            PreviousState == ArmoredEnemyState.Armored &&
            CurrentState == ArmoredEnemyState.Broken;

        public bool WasFatal => Damage.WasFatal && CurrentState == ArmoredEnemyState.Dead;
    }
}
