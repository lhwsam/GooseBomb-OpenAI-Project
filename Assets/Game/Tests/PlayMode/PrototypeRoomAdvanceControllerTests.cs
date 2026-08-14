using System;
using NUnit.Framework;
using UnityEngine;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeRoomAdvanceControllerTests
    {
        [Test]
        public void Configure_StoresSessionSceneAndPositiveDelay()
        {
            GameObject root = CreateInactiveRoot(
                out PrototypeGameSession session,
                out PrototypeRoomAdvanceController controller);
            try
            {
                controller.Configure(session, "TestSandboxLanes", 1.5f);

                Assert.That(controller.Session, Is.SameAs(session));
                Assert.That(controller.NextSceneName, Is.EqualTo("TestSandboxLanes"));
                Assert.That(controller.TransitionDelaySeconds, Is.EqualTo(1.5f));
                Assert.That(controller.IsTransitionPending, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Configure_NormalizesNullFinalSceneName()
        {
            GameObject root = CreateInactiveRoot(
                out PrototypeGameSession session,
                out PrototypeRoomAdvanceController controller);
            try
            {
                controller.Configure(session, null);

                Assert.That(controller.NextSceneName, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Configure_RejectsMissingSessionAndInvalidDelay()
        {
            GameObject root = CreateInactiveRoot(
                out PrototypeGameSession session,
                out PrototypeRoomAdvanceController controller);
            try
            {
                Assert.Throws<ArgumentNullException>(() =>
                    controller.Configure(null, "Next"));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    controller.Configure(session, "Next", 0f));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    controller.Configure(session, "Next", float.NaN));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    controller.Configure(session, "Next", float.PositiveInfinity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateInactiveRoot(
            out PrototypeGameSession session,
            out PrototypeRoomAdvanceController controller)
        {
            var root = new GameObject("PrototypeRoomAdvanceControllerTests");
            root.SetActive(false);
            session = root.AddComponent<PrototypeGameSession>();
            controller = root.AddComponent<PrototypeRoomAdvanceController>();
            return root;
        }
    }
}
