using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeCameraShakeTests
    {
        [UnityTest]
        public IEnumerator Play_AppliesLocalOffsetAndRestoresAuthoredPosition()
        {
            float previousTimeScale = Time.timeScale;
            var root = new GameObject("CameraShakeTestRoot");
            root.SetActive(false);
            var target = new GameObject("CameraTarget").transform;
            target.SetParent(root.transform, false);
            Vector3 authoredPosition = new Vector3(2f, 3f, -4f);
            target.localPosition = authoredPosition;
            PrototypeCameraShake shake = root.AddComponent<PrototypeCameraShake>();
            shake.Configure(target, 0.25f);
            root.SetActive(true);

            try
            {
                Time.timeScale = 1f;
                Assert.That(shake.Play(0.16f, 0.12f, 24f), Is.True);

                float movementDeadline = Time.realtimeSinceStartup + 0.4f;
                bool observedMovement = false;
                while (!observedMovement && Time.realtimeSinceStartup < movementDeadline)
                {
                    yield return null;
                    observedMovement =
                        Vector3.Distance(target.localPosition, authoredPosition) > 0.001f;
                }

                Assert.That(observedMovement, Is.True);
                Assert.That(shake.AppliedOffset.z, Is.Zero.Within(0.000001f));

                float completionDeadline = Time.realtimeSinceStartup + 0.5f;
                while (shake.IsShaking && Time.realtimeSinceStartup < completionDeadline)
                {
                    yield return null;
                }
                yield return null;

                Assert.That(shake.IsShaking, Is.False);
                Assert.That(
                    Vector3.Distance(target.localPosition, authoredPosition),
                    Is.LessThan(0.0001f));
                Assert.That(shake.AppliedOffset, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator Disable_RestoresTargetWithoutTransformDrift()
        {
            float previousTimeScale = Time.timeScale;
            var root = new GameObject("CameraShakeDisableTestRoot");
            root.SetActive(false);
            var target = new GameObject("CameraTarget").transform;
            target.SetParent(root.transform, false);
            Vector3 authoredPosition = new Vector3(-1f, 5f, 2f);
            target.localPosition = authoredPosition;
            PrototypeCameraShake shake = root.AddComponent<PrototypeCameraShake>();
            shake.Configure(target, 0.25f);
            root.SetActive(true);

            try
            {
                Time.timeScale = 1f;
                shake.Play(0.16f, 1f, 24f);
                float deadline = Time.realtimeSinceStartup + 0.4f;
                while (shake.AppliedOffset == Vector3.zero &&
                       Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(shake.AppliedOffset, Is.Not.EqualTo(Vector3.zero));
                shake.enabled = false;

                Assert.That(shake.IsShaking, Is.False);
                Assert.That(
                    Vector3.Distance(target.localPosition, authoredPosition),
                    Is.LessThan(0.0001f));
                Assert.That(shake.AppliedOffset, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Play_ClampsAmplitudeAndIgnoresDisabledRequests()
        {
            var root = new GameObject("CameraShakeClampTestRoot");
            root.SetActive(false);
            var target = new GameObject("CameraTarget").transform;
            target.SetParent(root.transform, false);
            PrototypeCameraShake shake = root.AddComponent<PrototypeCameraShake>();
            shake.Configure(target, 0.2f);
            root.SetActive(true);

            try
            {
                Assert.That(shake.Play(1f, 0.1f, 20f), Is.True);
                Assert.That(shake.ActiveAmplitude, Is.EqualTo(0.2f).Within(0.0001f));
                shake.Stop();
                Assert.That(shake.Play(0f, 0.1f, 20f), Is.False);
                Assert.That(shake.Play(0.1f, 0f, 20f), Is.False);
                Assert.That(shake.Play(0.1f, 0.1f, 0f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

    }
}
