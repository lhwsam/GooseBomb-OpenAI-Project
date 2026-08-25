using NUnit.Framework;

namespace BombSwap.Tests
{
    public sealed class PrototypePlayerDamageChromaticPresenterTests
    {
        [TestCase(0f, 0f)]
        [TestCase(0.25f, 1f)]
        [TestCase(0.5f, 0f)]
        [TestCase(0.75f, 1f)]
        [TestCase(1f, 0f)]
        public void EvaluateIntensity_ProducesDoubleLinearPulse(
            float normalizedTime,
            float expected)
        {
            Assert.That(
                PrototypePlayerDamageChromaticPresenter.EvaluateIntensity(normalizedTime),
                Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
