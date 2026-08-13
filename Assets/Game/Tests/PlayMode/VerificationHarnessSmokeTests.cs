using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class VerificationHarnessSmokeTests
    {
        [UnityTest]
        public IEnumerator UnityAssembly_IsDiscoverableByPlayModeHarness()
        {
            var gameObject = new GameObject("VerificationHarnessSmoke");

            yield return null;

            Assert.That(gameObject, Is.Not.Null);
            Object.Destroy(gameObject);
        }
    }
}

