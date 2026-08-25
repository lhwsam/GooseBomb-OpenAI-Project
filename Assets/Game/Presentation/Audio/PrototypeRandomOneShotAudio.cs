using System;
using UnityEngine;
using UnityEngine.Audio;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeRandomOneShotAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField] private AudioMixerGroup outputGroup;
        [SerializeField] private int voiceCount = 4;
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 20f;

        private AudioSource[] _voices;
        private int _nextVoice;

        public int ClipCount => clips?.Length ?? 0;
        public AudioMixerGroup OutputGroup => outputGroup;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public bool IsConfigured => ClipCount > 0 && outputGroup != null && voiceCount > 0;

        public void Configure(
            AudioClip[] authoredClips,
            AudioMixerGroup authoredOutputGroup,
            int authoredVoiceCount = 4,
            float authoredSpatialBlend = 1f,
            float authoredMinDistance = 3f,
            float authoredMaxDistance = 20f)
        {
            if (authoredClips == null || authoredClips.Length == 0 ||
                Array.Exists(authoredClips, clip => clip == null))
            {
                throw new ArgumentException("Random one-shot audio requires non-null clips.", nameof(authoredClips));
            }
            clips = (AudioClip[])authoredClips.Clone();
            outputGroup = authoredOutputGroup ?? throw new ArgumentNullException(nameof(authoredOutputGroup));
            voiceCount = Mathf.Max(1, authoredVoiceCount);
            spatialBlend = Mathf.Clamp01(authoredSpatialBlend);
            minDistance = Mathf.Max(0.01f, authoredMinDistance);
            maxDistance = Mathf.Max(minDistance, authoredMaxDistance);
        }

        public void Play(Vector3 worldPosition)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Random one-shot audio is not configured.");
            }
            EnsureVoices();
            AudioSource voice = FindVoice();
            voice.transform.position = worldPosition;
            voice.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            voice.Play();
        }

        private void EnsureVoices()
        {
            if (_voices != null && _voices.Length == voiceCount)
            {
                return;
            }
            _voices = new AudioSource[voiceCount];
            for (int index = 0; index < voiceCount; index++)
            {
                var voiceObject = new GameObject($"Voice_{index}");
                voiceObject.transform.SetParent(transform, false);
                AudioSource source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.outputAudioMixerGroup = outputGroup;
                source.spatialBlend = spatialBlend;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                _voices[index] = source;
            }
        }

        private AudioSource FindVoice()
        {
            for (int offset = 0; offset < _voices.Length; offset++)
            {
                int index = (_nextVoice + offset) % _voices.Length;
                if (!_voices[index].isPlaying)
                {
                    _nextVoice = (index + 1) % _voices.Length;
                    return _voices[index];
                }
            }
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return voice;
        }
    }
}
