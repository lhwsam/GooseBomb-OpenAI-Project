using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class VerificationHarnessSmokeTests
    {
        [Test]
        public void CoreAssembly_IsDiscoverableByEditModeHarness()
        {
            Assert.That(typeof(VerificationHarnessSmokeTests).Assembly.GetName().Name, Is.EqualTo("BombSwap.Core.Tests"));
        }
    }
}

