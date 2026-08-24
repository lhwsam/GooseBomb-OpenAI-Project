using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BombSwap
{
    [CreateAssetMenu(
        fileName = "PrototypeBgmCatalog",
        menuName = "Bomb Swap/Audio/Prototype BGM Catalog")]
    public sealed class PrototypeBgmCatalogAsset : ScriptableObject
    {
        public const int ExpectedFrequency = 44100;
        public const int ExpectedChannels = 2;
        public const int ExpectedLobbySamples = 3528000;
        public const int ExpectedDungeonSamples = 2919724;
        public const int ExpectedBossSamples = 2646000;

        [SerializeField]
        private AudioMixerGroup bgmOutputGroup;

        [Header("Lobby")]
        [SerializeField]
        private AudioClip lobby;

        [Header("Dungeon stems")]
        [SerializeField]
        private AudioClip dungeonBase;

        [SerializeField]
        private AudioClip dungeonCombat;

        [SerializeField]
        private AudioClip dungeonDanger;

        [SerializeField]
        private AudioClip dungeonSanctuary;

        [Header("Boss stems")]
        [SerializeField]
        private AudioClip bossBase;

        [SerializeField]
        private AudioClip bossGrand;

        [SerializeField]
        private AudioClip bossDanger;

        [Header("Playback")]
        [SerializeField, Min(0.01f)]
        private float crossfadeSeconds = 1f;

        [SerializeField, Range(0f, 1f)]
        private float pauseDuckGain = 0.5f;

        [SerializeField, Min(0.01f)]
        private float pauseDuckFadeSeconds = 0.25f;

        [SerializeField, Min(0.01f)]
        private float scheduleLeadSeconds = 0.1f;

        public AudioMixerGroup BgmOutputGroup => bgmOutputGroup;

        public AudioClip Lobby => lobby;

        public AudioClip DungeonBase => dungeonBase;

        public AudioClip DungeonCombat => dungeonCombat;

        public AudioClip DungeonDanger => dungeonDanger;

        public AudioClip DungeonSanctuary => dungeonSanctuary;

        public AudioClip BossBase => bossBase;

        public AudioClip BossGrand => bossGrand;

        public AudioClip BossDanger => bossDanger;

        public float CrossfadeSeconds => crossfadeSeconds;

        public float PauseDuckGain => pauseDuckGain;

        public float PauseDuckFadeSeconds => pauseDuckFadeSeconds;

        public float ScheduleLeadSeconds => scheduleLeadSeconds;

        public bool HasRequiredReferences =>
            bgmOutputGroup != null &&
            lobby != null &&
            dungeonBase != null &&
            dungeonCombat != null &&
            dungeonDanger != null &&
            dungeonSanctuary != null &&
            bossBase != null &&
            bossGrand != null &&
            bossDanger != null;

        public void Configure(
            AudioMixerGroup authoredBgmOutputGroup,
            AudioClip authoredLobby,
            AudioClip authoredDungeonBase,
            AudioClip authoredDungeonCombat,
            AudioClip authoredDungeonDanger,
            AudioClip authoredDungeonSanctuary,
            AudioClip authoredBossBase,
            AudioClip authoredBossGrand,
            AudioClip authoredBossDanger)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "The BGM catalog can only be authored outside Play Mode.");
            }

            bgmOutputGroup = authoredBgmOutputGroup;
            lobby = authoredLobby;
            dungeonBase = authoredDungeonBase;
            dungeonCombat = authoredDungeonCombat;
            dungeonDanger = authoredDungeonDanger;
            dungeonSanctuary = authoredDungeonSanctuary;
            bossBase = authoredBossBase;
            bossGrand = authoredBossGrand;
            bossDanger = authoredBossDanger;
        }

        public AudioClip[] GetRuntimeClips()
        {
            return new[]
            {
                lobby,
                dungeonBase,
                dungeonCombat,
                dungeonDanger,
                dungeonSanctuary,
                bossBase,
                bossGrand,
                bossDanger,
            };
        }

        public void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }
            if (!HasRequiredReferences)
            {
                errors.Add("Prototype BGM catalog requires its BGM mixer group and eight runtime clips.");
                return;
            }
            if (!string.Equals(bgmOutputGroup.name, "BGM", StringComparison.Ordinal))
            {
                errors.Add("Prototype BGM catalog output must use the BGM AudioMixer group.");
            }
            if (crossfadeSeconds <= 0f || pauseDuckFadeSeconds <= 0f || scheduleLeadSeconds <= 0f)
            {
                errors.Add("Prototype BGM playback durations must be positive.");
            }

            ValidateClip(lobby, "Lobby", ExpectedLobbySamples, errors);
            ValidateClip(dungeonBase, "Dungeon Base", ExpectedDungeonSamples, errors);
            ValidateClip(dungeonCombat, "Dungeon Combat", ExpectedDungeonSamples, errors);
            ValidateClip(dungeonDanger, "Dungeon Danger", ExpectedDungeonSamples, errors);
            ValidateClip(dungeonSanctuary, "Dungeon Sanctuary", ExpectedDungeonSamples, errors);
            ValidateClip(bossBase, "Boss Base", ExpectedBossSamples, errors);
            ValidateClip(bossGrand, "Boss Grand", ExpectedBossSamples, errors);
            ValidateClip(bossDanger, "Boss Danger", ExpectedBossSamples, errors);

            var distinctClips = new HashSet<AudioClip>(GetRuntimeClips());
            if (distinctClips.Count != 8)
            {
                errors.Add("Prototype BGM catalog must reference eight distinct runtime clips.");
            }
        }

        private static void ValidateClip(
            AudioClip clip,
            string label,
            int expectedSamples,
            ICollection<string> errors)
        {
            if (clip == null)
            {
                return;
            }
            if (clip.frequency != ExpectedFrequency || clip.channels != ExpectedChannels)
            {
                errors.Add(
                    $"{label} BGM must be {ExpectedFrequency} Hz stereo, found {clip.frequency} Hz/{clip.channels} channels.");
            }
            if (clip.samples != expectedSamples)
            {
                errors.Add(
                    $"{label} BGM must contain {expectedSamples} samples, found {clip.samples}.");
            }
        }
    }
}
