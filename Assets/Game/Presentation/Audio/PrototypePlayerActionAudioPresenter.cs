using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerActionAudioPresenter : MonoBehaviour
    {
        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private PrototypeRandomOneShotAudio damageAudio;
        [SerializeField] private PrototypeRandomOneShotAudio bombPlacementAudio;

        public PrototypeGameSession Session => session;
        public PrototypeRandomOneShotAudio DamageAudio => damageAudio;
        public PrototypeRandomOneShotAudio BombPlacementAudio => bombPlacementAudio;
        public int DamageSoundCount { get; private set; }
        public int BombPlacementSoundCount { get; private set; }

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypeRandomOneShotAudio authoredDamageAudio,
            PrototypeRandomOneShotAudio authoredBombPlacementAudio)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerActionAudioPresenter before changing its runtime configuration.");
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            damageAudio = authoredDamageAudio ??
                throw new ArgumentNullException(nameof(authoredDamageAudio));
            bombPlacementAudio = authoredBombPlacementAudio ??
                throw new ArgumentNullException(nameof(authoredBombPlacementAudio));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || damageAudio == null || bombPlacementAudio == null ||
                !damageAudio.IsConfigured || !bombPlacementAudio.IsConfigured)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerActionAudioPresenter requires a session and configured damage/bomb audio.");
            }

            session.PlayerDamaged += OnPlayerDamaged;
            session.BombPlaced += OnBombPlaced;
        }

        private void OnDisable()
        {
            if (session == null)
            {
                return;
            }

            session.PlayerDamaged -= OnPlayerDamaged;
            session.BombPlaced -= OnBombPlaced;
        }

        private void OnPlayerDamaged(PlayerDamageResult result)
        {
            if (!result.WasApplied)
            {
                return;
            }

            DamageSoundCount++;
            damageAudio.Play(Vector3.zero);
        }

        private void OnBombPlaced(BombSnapshot _)
        {
            BombPlacementSoundCount++;
            bombPlacementAudio.Play(Vector3.zero);
        }
    }
}
