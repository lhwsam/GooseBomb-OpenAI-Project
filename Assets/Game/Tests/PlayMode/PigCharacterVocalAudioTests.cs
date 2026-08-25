using System.Reflection;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;

namespace BombSwap.Tests
{
    public sealed class PigCharacterVocalAudioTests
    {
        private GameObject _root;
        private AudioClip _shortA;
        private AudioClip _shortB;
        private AudioClip _longA;
        private AudioClip _skillA;
        private AudioClip _skillB;
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _root = new GameObject("PigVocalAudioTest");
            _shortA = AudioClip.Create("ShortA", 128, 1, 44100, false);
            _shortB = AudioClip.Create("ShortB", 128, 1, 44100, false);
            _longA = AudioClip.Create("LongA", 128, 1, 44100, false);
            _skillA = AudioClip.Create("SkillA", 128, 1, 44100, false);
            _skillB = AudioClip.Create("SkillB", 128, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_shortA);
            Object.DestroyImmediate(_shortB);
            Object.DestroyImmediate(_longA);
            Object.DestroyImmediate(_skillA);
            Object.DestroyImmediate(_skillB);
        }

        [Test]
        public void MovementVocal_UsesChanceAndMinimumInterval()
        {
            PigCharacterVocalAudio vocals = CreateVocals();

            vocals.TryPlayMovementVocal();
            vocals.TryPlayMovementVocal();

            Assert.That(vocals.ShortPlayCount, Is.EqualTo(1));
        }

        [Test]
        public void AttackVocal_BypassesMovementIntervalAndAvoidsImmediateRepeat()
        {
            PigCharacterVocalAudio vocals = CreateVocals();

            vocals.PlayAttackVocal();
            AudioClip firstClip = vocals.LastPlayedClip;
            vocals.PlayAttackVocal();

            Assert.That(vocals.ShortPlayCount, Is.EqualTo(2));
            Assert.That(vocals.LastPlayedClip, Is.Not.SameAs(firstClip));
        }

        [Test]
        public void DeathVocal_PlaysLongClip()
        {
            PigCharacterVocalAudio vocals = CreateVocals();

            vocals.PlayDeathVocal();

            Assert.That(vocals.LongPlayCount, Is.EqualTo(1));
            Assert.That(vocals.LastPlayedClip, Is.SameAs(_longA));
        }

        [Test]
        public void SkillVocal_UsesDedicatedClipsWithoutImmediateRepeat()
        {
            PigCharacterVocalAudio vocals = CreateVocals();

            vocals.PlaySkillVocal();
            AudioClip firstClip = vocals.LastPlayedClip;
            vocals.PlaySkillVocal();

            Assert.That(vocals.SkillPlayCount, Is.EqualTo(2));
            Assert.That(vocals.LastPlayedClip, Is.Not.SameAs(firstClip));
            Assert.That(
                vocals.LastPlayedClip == _skillA || vocals.LastPlayedClip == _skillB,
                Is.True);
        }

        [Test]
        public void BossParityWaveVocal_PlaysOnlyFirstExecutePerSequence()
        {
            var gate = new BossSkillVocalGate();

            Assert.That(
                gate.ShouldPlay(BossBattleState.Telegraph, BossPatternKind.ParityWave),
                Is.False);
            Assert.That(
                gate.ShouldPlay(BossBattleState.Execute, BossPatternKind.ParityWave),
                Is.True);
            Assert.That(
                gate.ShouldPlay(BossBattleState.Recovery, BossPatternKind.ParityWave),
                Is.False);
            Assert.That(
                gate.ShouldPlay(BossBattleState.Telegraph, BossPatternKind.ParityWave),
                Is.False);
            Assert.That(
                gate.ShouldPlay(BossBattleState.Execute, BossPatternKind.ParityWave),
                Is.False);

            gate.ShouldPlay(BossBattleState.Telegraph, BossPatternKind.FixedCharge);
            gate.ShouldPlay(BossBattleState.Telegraph, BossPatternKind.ParityWave);

            Assert.That(
                gate.ShouldPlay(BossBattleState.Execute, BossPatternKind.ParityWave),
                Is.True);
        }

        private PigCharacterVocalAudio CreateVocals()
        {
            _root.AddComponent<AudioSource>();
            PigCharacterVocalAudio vocals = _root.AddComponent<PigCharacterVocalAudio>();
            SetField(vocals, "shortClips", new[] { _shortA, _shortB });
            SetField(vocals, "longClips", new[] { _longA });
            SetField(vocals, "skillClips", new[] { _skillA, _skillB });
            SetField(vocals, "movementChance", 1f);
            SetField(vocals, "movementMinimumInterval", 10f);
            return vocals;
        }

        private static void SetField<T>(PigCharacterVocalAudio target, string name, T value)
        {
            FieldInfo field = typeof(PigCharacterVocalAudio).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field '{name}'.");
            field.SetValue(target, value);
        }
    }
}
