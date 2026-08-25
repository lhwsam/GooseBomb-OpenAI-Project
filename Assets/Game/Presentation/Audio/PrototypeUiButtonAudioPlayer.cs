using System.Collections;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PrototypeUiButtonAudioPlayer : MonoBehaviour
    {
        private const string ClickVoiceObjectName =
            "Prototype UI Button Click Voice";

        private const float ClickVoiceCleanupPaddingSeconds = 0.1f;

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip hoverClip;

        [SerializeField]
        private AudioClip clickClip;

        public AudioSource AudioSource => audioSource;

        public AudioClip HoverClip => hoverClip;

        public AudioClip ClickClip => clickClip;

        public AudioClip LastPlayedClip { get; private set; }

        public int HoverPlayCount { get; private set; }

        public int ClickPlayCount { get; private set; }

        public void Configure(
            AudioSource authoredAudioSource,
            AudioClip authoredHoverClip,
            AudioClip authoredClickClip)
        {
            if (Application.isPlaying && audioSource != null)
            {
                audioSource.Stop();
            }

            audioSource = authoredAudioSource;
            hoverClip = authoredHoverClip;
            clickClip = authoredClickClip;
        }

        public bool HasConfiguration(
            AudioSource expectedAudioSource,
            AudioClip expectedHoverClip,
            AudioClip expectedClickClip)
        {
            return audioSource == expectedAudioSource &&
                   hoverClip == expectedHoverClip &&
                   clickClip == expectedClickClip;
        }

        public void PlayHover()
        {
            if (!TryPlay(hoverClip))
            {
                return;
            }

            HoverPlayCount++;
        }

        public void PlayClick()
        {
            if (!TryPlayClick())
            {
                return;
            }

            ClickPlayCount++;
        }

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

        private bool TryPlay(AudioClip clip)
        {
            if (!isActiveAndEnabled || audioSource == null || clip == null)
            {
                return false;
            }

            // The shared Canvas voice never stacks repeated hover transients.
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
            LastPlayedClip = clip;
            return true;
        }

        private bool TryPlayClick()
        {
            if (!isActiveAndEnabled || audioSource == null || clickClip == null)
            {
                return false;
            }

            // Navigation listeners can disable or destroy the owning Canvas in the
            // same Button.onClick invocation. Hand the confirmed cue to a tiny
            // scene-independent voice so that transition cannot cut its tail off.
            audioSource.Stop();
            var voiceObject = new GameObject(ClickVoiceObjectName)
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            DontDestroyOnLoad(voiceObject);

            AudioSource voice = voiceObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.volume = audioSource.volume;
            voice.pitch = audioSource.pitch;
            voice.panStereo = audioSource.panStereo;
            voice.spatialBlend = 0f;
            voice.reverbZoneMix = 0f;
            voice.dopplerLevel = 0f;
            voice.priority = audioSource.priority;
            voice.mute = audioSource.mute;
            voice.bypassEffects = audioSource.bypassEffects;
            voice.bypassListenerEffects = audioSource.bypassListenerEffects;
            voice.bypassReverbZones = audioSource.bypassReverbZones;
            voice.ignoreListenerPause = audioSource.ignoreListenerPause;
            voice.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            voice.PlayOneShot(clickClip);

            float pitchMagnitude = Mathf.Max(Mathf.Abs(voice.pitch), 0.01f);
            voiceObject
                .AddComponent<PrototypeUiButtonClickVoiceLifetime>()
                .Begin(
                    (clickClip.length / pitchMagnitude) +
                    ClickVoiceCleanupPaddingSeconds);
            LastPlayedClip = clickClip;
            return true;
        }
    }

    internal sealed class PrototypeUiButtonClickVoiceLifetime : MonoBehaviour
    {
        public void Begin(float lifetimeSeconds)
        {
            StartCoroutine(DestroyAfterRealtime(lifetimeSeconds));
        }

        private IEnumerator DestroyAfterRealtime(float lifetimeSeconds)
        {
            yield return new WaitForSecondsRealtime(lifetimeSeconds);
            Destroy(gameObject);
        }
    }
}
