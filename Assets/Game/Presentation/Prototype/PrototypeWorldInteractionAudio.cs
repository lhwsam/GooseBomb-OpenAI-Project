using System;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeWorldInteractionAudio : MonoBehaviour
    {
        [SerializeField]
        private AudioSource successAudioSource;

        public AudioSource SuccessAudioSource => successAudioSource;

        public bool HasRequiredReferences =>
            successAudioSource != null && successAudioSource.clip != null;

        public void Configure(AudioSource authoredSuccessAudioSource)
        {
            successAudioSource = authoredSuccessAudioSource ??
                throw new ArgumentNullException(nameof(authoredSuccessAudioSource));
            if (successAudioSource.clip == null)
            {
                throw new ArgumentException(
                    "World interaction success AudioSource requires an AudioClip.",
                    nameof(authoredSuccessAudioSource));
            }
        }

        public void PlaySuccess()
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "World interaction audio requires an AudioSource with a success clip.");
            }

            successAudioSource.Play();
        }
    }
}
