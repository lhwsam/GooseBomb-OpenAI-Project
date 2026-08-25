using System;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PigCharacterVocalAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] shortClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] longClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] skillClips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float movementChance = 0.25f;
        [SerializeField, Min(0f)] private float movementMinimumInterval = 2f;
        [SerializeField, Range(0f, 1f)] private float shortVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float longVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float skillVolume = 1f;

        private int _lastShortClipIndex = -1;
        private int _lastLongClipIndex = -1;
        private int _lastSkillClipIndex = -1;
        private float _nextMovementVocalTime;

        public int ShortPlayCount { get; private set; }
        public int LongPlayCount { get; private set; }
        public int SkillPlayCount { get; private set; }
        public AudioClip LastPlayedClip { get; private set; }

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnDisable()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        public void TryPlayMovementVocal()
        {
            if (!CanPlay() || Time.unscaledTime < _nextMovementVocalTime ||
                movementChance <= 0f ||
                (movementChance < 1f && UnityEngine.Random.value >= movementChance))
            {
                return;
            }

            PlayShortInternal();
        }

        public void PlayAttackVocal()
        {
            if (!CanPlay())
            {
                return;
            }

            audioSource.Stop();
            PlayShortInternal();
        }

        public void PlayDeathVocal()
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || audioSource == null ||
                longClips == null || longClips.Length == 0)
            {
                return;
            }

            int clipIndex = SelectClipIndex(longClips, _lastLongClipIndex);
            AudioClip clip = longClips[clipIndex];
            if (clip == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.PlayOneShot(clip, longVolume);
            _lastLongClipIndex = clipIndex;
            LastPlayedClip = clip;
            LongPlayCount++;
        }

        public void PlaySkillVocal()
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || audioSource == null ||
                skillClips == null || skillClips.Length == 0)
            {
                return;
            }

            int clipIndex = SelectClipIndex(skillClips, _lastSkillClipIndex);
            AudioClip clip = skillClips[clipIndex];
            if (clip == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.PlayOneShot(clip, skillVolume);
            _lastSkillClipIndex = clipIndex;
            _nextMovementVocalTime = Time.unscaledTime + movementMinimumInterval;
            LastPlayedClip = clip;
            SkillPlayCount++;
        }

        private bool CanPlay()
        {
            return isActiveAndEnabled && Time.timeScale > 0f && audioSource != null &&
                shortClips != null && shortClips.Length > 0;
        }

        private void PlayShortInternal()
        {
            int clipIndex = SelectClipIndex(shortClips, _lastShortClipIndex);
            AudioClip clip = shortClips[clipIndex];
            if (clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, shortVolume);
            _lastShortClipIndex = clipIndex;
            _nextMovementVocalTime = Time.unscaledTime + movementMinimumInterval;
            LastPlayedClip = clip;
            ShortPlayCount++;
        }

        private static int SelectClipIndex(AudioClip[] clips, int lastClipIndex)
        {
            if (clips.Length == 1 || lastClipIndex < 0 || lastClipIndex >= clips.Length)
            {
                return UnityEngine.Random.Range(0, clips.Length);
            }

            int candidate = UnityEngine.Random.Range(0, clips.Length - 1);
            return candidate >= lastClipIndex ? candidate + 1 : candidate;
        }
    }
}
