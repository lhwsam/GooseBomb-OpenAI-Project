using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BombSwap.Tests
{
    public sealed class CharacterFootstepAudioTests
    {
        private GameObject _root;
        private AudioClip _firstClip;
        private AudioClip _secondClip;
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _root = new GameObject("FootstepAudioTest");
            _firstClip = AudioClip.Create("FootstepA", 128, 1, 44100, false);
            _secondClip = AudioClip.Create("FootstepB", 128, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_firstClip);
            Object.DestroyImmediate(_secondClip);
        }

        [Test]
        public void PlayFootstep_AvoidsRepeatingThePreviousClip()
        {
            CharacterFootstepAudio footsteps = CreateFootsteps();

            footsteps.PlayFootstep();
            AudioClip firstPlayed = footsteps.LastPlayedClip;
            footsteps.PlayFootstep();

            Assert.That(footsteps.PlayCount, Is.EqualTo(2));
            Assert.That(firstPlayed, Is.Not.Null);
            Assert.That(footsteps.LastPlayedClip, Is.Not.SameAs(firstPlayed));
        }

        [Test]
        public void Awake_AttachesRelayThatForwardsAnimationEventFromChildAnimator()
        {
            GameObject animatorObject = new("Animator");
            animatorObject.transform.SetParent(_root.transform);
            animatorObject.AddComponent<Animator>();

            CharacterFootstepAudio footsteps = CreateFootsteps();
            CharacterFootstepAnimationEventRelay relay =
                animatorObject.GetComponent<CharacterFootstepAnimationEventRelay>();

            Assert.That(relay, Is.Not.Null);
            relay.PlayFootstep();

            Assert.That(footsteps.PlayCount, Is.EqualTo(1));
            Assert.That(footsteps.LastPlayedClip, Is.Not.Null);
        }

        [Test]
        public void PlayFootstep_DoesNotPlayWhileGameIsPaused()
        {
            CharacterFootstepAudio footsteps = CreateFootsteps();
            Time.timeScale = 0f;

            footsteps.PlayFootstep();

            Assert.That(footsteps.PlayCount, Is.Zero);
            Assert.That(footsteps.LastPlayedClip, Is.Null);
        }

        [Test]
        public void StopPlayback_StopsAnActiveFootstepVoice()
        {
            CharacterFootstepAudio footsteps = CreateFootsteps();
            AudioSource source = _root.GetComponent<AudioSource>();

            footsteps.PlayFootstep();
            Assert.That(source.isPlaying, Is.True);

            footsteps.StopPlayback();

            Assert.That(source.isPlaying, Is.False);
        }

        private CharacterFootstepAudio CreateFootsteps()
        {
            CharacterFootstepAudio footsteps = _root.AddComponent<CharacterFootstepAudio>();
            SetField(footsteps, "footstepClips", new[] { _firstClip, _secondClip });
            SetField(footsteps, "minimumInterval", 0f);
            return footsteps;
        }

        private static void SetField<T>(CharacterFootstepAudio target, string fieldName, T value)
        {
            FieldInfo field = typeof(CharacterFootstepAudio).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
