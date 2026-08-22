using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerAnimationPresenter : MonoBehaviour
    {
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int PlaceBombParameterId = Animator.StringToHash("PlaceBomb");
        private static readonly int DieParameterId = Animator.StringToHash("Die");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Animator animator;

        public PrototypeGameSession Session => session;

        public Animator Animator => animator;

        public int BombAnimationCount { get; private set; }

        public int DeathAnimationCount { get; private set; }

        public void Configure(PrototypeGameSession gameSession, Animator playerAnimator)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerAnimationPresenter before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (playerAnimator == null)
            {
                throw new ArgumentNullException(nameof(playerAnimator));
            }

            session = gameSession;
            animator = playerAnimator;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || animator == null)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerAnimationPresenter requires session and Animator references.");
            }

            session.BombPlaced += OnBombPlaced;
            session.PlayerDied += OnPlayerDied;
            session.PauseStateChanged += OnPauseStateChanged;
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                SyncFromSession();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.BombPlaced -= OnBombPlaced;
                session.PlayerDied -= OnPlayerDied;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }

            if (animator != null)
            {
                animator.speed = 1f;
            }
        }

        private void Update()
        {
            if (session == null || animator == null ||
                !session.IsReady || session.IsPaused || session.IsPlayerDead)
            {
                return;
            }

            animator.SetBool(IsMovingParameterId, session.IsPlayerMoving);
        }

        private void OnSessionReady()
        {
            SyncFromSession();
        }

        private void OnBombPlaced(BombSnapshot _)
        {
            if (session.IsPlayerDead)
            {
                return;
            }

            BombAnimationCount++;
            animator.SetTrigger(PlaceBombParameterId);
        }

        private void OnPlayerDied(PlayerDamageResult _)
        {
            DeathAnimationCount++;
            animator.SetBool(IsMovingParameterId, false);
            animator.ResetTrigger(PlaceBombParameterId);
            animator.SetTrigger(DieParameterId);
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            animator.speed = isPaused ? 0f : 1f;
        }

        private void SyncFromSession()
        {
            animator.speed = session.IsPaused ? 0f : 1f;
            animator.SetBool(
                IsMovingParameterId,
                !session.IsPlayerDead && session.IsPlayerMoving);
            if (session.IsPlayerDead)
            {
                animator.ResetTrigger(PlaceBombParameterId);
                animator.SetTrigger(DieParameterId);
            }
        }
    }
}
