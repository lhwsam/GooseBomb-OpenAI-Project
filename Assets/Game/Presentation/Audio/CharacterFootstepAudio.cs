using System;
using System.Collections.Generic;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CharacterFootstepAudio : MonoBehaviour
    {
        private static readonly List<CharacterFootstepAudio> SharedEnemyEmitters = new();

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip[] footstepClips = Array.Empty<AudioClip>();

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.65f;

        [SerializeField]
        private Vector2 pitchRange = new(0.96f, 1.04f);

        [SerializeField, Min(0f)]
        private float minimumInterval = 0.08f;

        [SerializeField]
        private bool countsTowardSharedEnemyLimit;

        [SerializeField, Min(1)]
        private int maximumSharedEnemyVoices = 4;

        private int _lastClipIndex = -1;
        private float _nextAllowedTime;

        public int PlayCount { get; private set; }

        public AudioClip LastPlayedClip { get; private set; }

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int index = 0; index < animators.Length; index++)
            {
                CharacterFootstepAnimationEventRelay relay =
                    animators[index].GetComponent<CharacterFootstepAnimationEventRelay>();
                if (relay == null)
                {
                    relay = animators[index].gameObject.AddComponent<CharacterFootstepAnimationEventRelay>();
                }

                relay.Configure(this);
            }
        }

        private void OnEnable()
        {
            if (countsTowardSharedEnemyLimit && !SharedEnemyEmitters.Contains(this))
            {
                SharedEnemyEmitters.Add(this);
            }
        }

        private void OnDisable()
        {
            SharedEnemyEmitters.Remove(this);
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        public void PlayFootstep()
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || audioSource == null || footstepClips.Length == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextAllowedTime || HasReachedSharedEnemyVoiceLimit())
            {
                return;
            }

            int clipIndex = SelectClipIndex();
            AudioClip clip = footstepClips[clipIndex];
            if (clip == null)
            {
                return;
            }

            float minimumPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maximumPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            audioSource.pitch = UnityEngine.Random.Range(minimumPitch, maximumPitch);
            audioSource.PlayOneShot(clip, volume);

            _lastClipIndex = clipIndex;
            _nextAllowedTime = now + minimumInterval;
            LastPlayedClip = clip;
            PlayCount++;
        }

        private int SelectClipIndex()
        {
            if (footstepClips.Length == 1 || _lastClipIndex < 0 || _lastClipIndex >= footstepClips.Length)
            {
                return UnityEngine.Random.Range(0, footstepClips.Length);
            }

            int candidate = UnityEngine.Random.Range(0, footstepClips.Length - 1);
            return candidate >= _lastClipIndex ? candidate + 1 : candidate;
        }

        private bool HasReachedSharedEnemyVoiceLimit()
        {
            if (!countsTowardSharedEnemyLimit || audioSource.isPlaying)
            {
                return false;
            }

            int playingVoiceCount = 0;
            for (int index = 0; index < SharedEnemyEmitters.Count; index++)
            {
                CharacterFootstepAudio emitter = SharedEnemyEmitters[index];
                if (emitter != null && emitter.audioSource != null && emitter.audioSource.isPlaying)
                {
                    playingVoiceCount++;
                    if (playingVoiceCount >= maximumSharedEnemyVoices)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
