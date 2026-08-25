using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BombSwap.Editor.ContentValidation
{
    internal static class PrototypePlayerDamageChromaticAuthoring
    {
        public static void Synchronize(Scene scene)
        {
            PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
            Volume volume = FindExactlyOne<Volume>(scene);
            Synchronize(session, volume);
        }

        public static void SynchronizeIfPresent(Scene scene)
        {
            Volume[] volumes = FindComponents<Volume>(scene);
            if (volumes.Length == 0)
            {
                return;
            }
            if (volumes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' requires exactly one {nameof(Volume)} when player damage chromatic feedback is configured; found {volumes.Length}.");
            }

            PrototypeGameSession session = FindExactlyOne<PrototypeGameSession>(scene);
            Synchronize(session, volumes[0]);
        }

        private static void Synchronize(PrototypeGameSession session, Volume volume)
        {
            PrototypePlayerDamageChromaticPresenter presenter =
                session.GetComponent<PrototypePlayerDamageChromaticPresenter>();
            if (presenter == null)
            {
                presenter = session.gameObject.AddComponent<PrototypePlayerDamageChromaticPresenter>();
            }

            presenter.Configure(session, volume);
            EditorUtility.SetDirty(presenter);
        }

        private static T FindExactlyOne<T>(Scene scene) where T : Component
        {
            T[] components = FindComponents<T>(scene);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' requires exactly one {typeof(T).Name}; found {components.Length}.");
            }
            return components[0];
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
    }
}
