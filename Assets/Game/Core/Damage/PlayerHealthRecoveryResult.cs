namespace BombSwap.Core
{
    public enum PlayerHealthRecoveryStatus
    {
        Applied = 0,
        IgnoredAtFullHealth = 1,
        IgnoredDead = 2,
    }

    public readonly struct PlayerHealthRecoveryResult
    {
        internal PlayerHealthRecoveryResult(
            int requestedHealth,
            int previousHealth,
            int currentHealth,
            PlayerHealthRecoveryStatus status)
        {
            RequestedHealth = requestedHealth;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            Status = status;
        }

        public int RequestedHealth { get; }

        public int PreviousHealth { get; }

        public int CurrentHealth { get; }

        public int RestoredHealth => CurrentHealth - PreviousHealth;

        public PlayerHealthRecoveryStatus Status { get; }

        public bool WasApplied => Status == PlayerHealthRecoveryStatus.Applied;
    }
}
