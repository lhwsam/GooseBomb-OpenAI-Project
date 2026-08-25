using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    internal static class PrototypePlayerActionAudioAuthoring
    {
        private const string PlayerSoundFolder = "Assets/Arts/Sound/Player/";

        public static void Synchronize(Scene scene)
        {
            PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                PrototypeContentValidator.AudioMixerPath);
            AudioMixerGroup sfxGroup = mixer != null
                ? mixer.FindMatchingGroups("SFX").SingleOrDefault()
                : null;
            if (sfxGroup == null)
            {
                throw new InvalidOperationException(
                    "Player action audio requires exactly one SFX mixer group.");
            }

            PrototypePlayerActionAudioPresenter presenter =
                session.GetComponent<PrototypePlayerActionAudioPresenter>();
            if (presenter == null)
            {
                presenter = session.gameObject.AddComponent<PrototypePlayerActionAudioPresenter>();
            }

            PrototypeRandomOneShotAudio damageAudio = EnsureAudio(
                session.transform,
                "PlayerDamageAudio",
                new[] { LoadClip(1), LoadClip(3), LoadClip(5) },
                sfxGroup);
            PrototypeRandomOneShotAudio bombAudio = EnsureAudio(
                session.transform,
                "PlayerBombPlacementAudio",
                new[] { LoadClip(2), LoadClip(4), LoadClip(6) },
                sfxGroup);
            presenter.Configure(session, damageAudio, bombAudio);
            EditorUtility.SetDirty(presenter);
        }

        private static PrototypeRandomOneShotAudio EnsureAudio(
            Transform parent,
            string objectName,
            AudioClip[] clips,
            AudioMixerGroup outputGroup)
        {
            Transform existing = parent.Find(objectName);
            GameObject audioObject = existing != null
                ? existing.gameObject
                : new GameObject(objectName);
            audioObject.transform.SetParent(parent, false);
            audioObject.transform.localPosition = Vector3.zero;

            PrototypeRandomOneShotAudio audio =
                audioObject.GetComponent<PrototypeRandomOneShotAudio>();
            if (audio == null)
            {
                audio = audioObject.AddComponent<PrototypeRandomOneShotAudio>();
            }
            audio.Configure(clips, outputGroup, 4, 0f, 1f, 1f);
            EditorUtility.SetDirty(audio);
            return audio;
        }

        private static AudioClip LoadClip(int number)
        {
            string path = $"{PlayerSoundFolder}Duck_call_{number}.wav";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path) ??
                throw new InvalidOperationException(
                    $"Player action audio clip is missing at '{path}'.");
        }

        private static T FindExactlyOne<T>(Scene scene) where T : Component
        {
            T[] components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' requires exactly one {typeof(T).Name}; found {components.Length}.");
            }
            return components[0];
        }
    }
}
