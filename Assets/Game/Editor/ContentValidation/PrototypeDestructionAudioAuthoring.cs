using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    internal static class PrototypeDestructionAudioAuthoring
    {
        public const string BoxBreakClip1Path =
            "Assets/Arts/Sound/Destroy/Box/Box_break_1.wav";
        public const string BoxBreakClip2Path =
            "Assets/Arts/Sound/Destroy/Box/Box_break_2.wav";
        public const string BrickBreakClip1Path =
            "Assets/Arts/Sound/Destroy/Brick/Brick_break_1.wav";
        public const string BrickBreakClip2Path =
            "Assets/Arts/Sound/Destroy/Brick/Brick_break_2.wav";

        public static void Synchronize(Scene scene)
        {
            PrototypeDestructibleWallPresenter wallPresenter =
                FindExactlyOne<PrototypeDestructibleWallPresenter>(scene);
            PrototypeDungeonDoorPresenter doorPresenter =
                FindZeroOrOne<PrototypeDungeonDoorPresenter>(scene);
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                PrototypeContentValidator.AudioMixerPath);
            AudioMixerGroup sfxGroup = mixer != null
                ? mixer.FindMatchingGroups("SFX").FirstOrDefault()
                : null;
            if (sfxGroup == null)
            {
                throw new InvalidOperationException(
                    "Destruction audio requires the SFX mixer group.");
            }

            PrototypeRandomOneShotAudio boxAudio = EnsureAudio(
                wallPresenter.transform,
                "BoxBreakAudio",
                new[]
                {
                    LoadClip(BoxBreakClip1Path),
                    LoadClip(BoxBreakClip2Path),
                },
                sfxGroup,
                3f);
            wallPresenter.ConfigureBreakAudio(boxAudio);
            EditorUtility.SetDirty(wallPresenter);
            if (doorPresenter != null)
            {
                PrototypeRandomOneShotAudio brickAudio = EnsureAudio(
                    doorPresenter.transform,
                    "BrickBreakAudio",
                    new[]
                    {
                        LoadClip(BrickBreakClip1Path),
                        LoadClip(BrickBreakClip2Path),
                    },
                    sfxGroup,
                    8f);
                doorPresenter.ConfigureSecretWallBreakAudio(brickAudio);
                EditorUtility.SetDirty(doorPresenter);
            }
        }

        private static PrototypeRandomOneShotAudio EnsureAudio(
            Transform parent,
            string objectName,
            AudioClip[] clips,
            AudioMixerGroup outputGroup,
            float minDistance)
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
            audio.Configure(clips, outputGroup, 4, 1f, minDistance, 20f);
            EditorUtility.SetDirty(audio);
            return audio;
        }

        private static AudioClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path) ??
                throw new InvalidOperationException(
                    $"Destruction audio clip is missing at '{path}'.");
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

        private static T FindZeroOrOne<T>(Scene scene) where T : Component
        {
            T[] components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' allows at most one {typeof(T).Name}; found {components.Length}.");
            }
            return components.SingleOrDefault();
        }
    }
}
