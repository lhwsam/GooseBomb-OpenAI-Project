using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypePauseTitleWaveTests
    {
        [UnityTest]
        public IEnumerator PausePrefab_WaveUsesUnscaledTimeAndRestoresOnDisable()
        {
            float previousTimeScale = Time.timeScale;
            PrototypePauseView instance = null;
            try
            {
                PrototypePauseView prefab =
                    Resources.Load<PrototypePauseView>("UI/PrototypePauseCanvas");
                Assert.That(prefab, Is.Not.Null);

                instance = Object.Instantiate(prefab);
                PrototypePauseTitleWave wave =
                    instance.GetComponentInChildren<PrototypePauseTitleWave>(true);
                Assert.That(wave, Is.Not.Null);
                Assert.That(wave.Target, Is.Not.Null);
                Assert.That(wave.Target.gameObject, Is.SameAs(wave.gameObject));
                Assert.That(wave.CycleDuration, Is.EqualTo(2f).Within(0.001f));
                Assert.That(wave.TrailingPauseSteps, Is.EqualTo(1f).Within(0.001f));

                wave.enabled = false;
                wave.Target.ForceMeshUpdate();
                float authoredY = GetFirstVisibleVertexY(wave.Target);
                float[] authoredVisibleVertexYs =
                    GetVisibleVertexYs(wave.Target);

                Time.timeScale = 0f;
                wave.enabled = true;
                float deadline = Time.realtimeSinceStartup + 0.75f;
                bool observedMovement = false;
                do
                {
                    yield return null;
                    observedMovement = HasVisibleVertexMoved(
                        wave.Target,
                        authoredVisibleVertexYs,
                        0.1f);
                }
                while (!observedMovement && Time.realtimeSinceStartup < deadline);

                Assert.That(wave.IsAnimating, Is.True);
                Assert.That(observedMovement, Is.True);

                wave.enabled = false;
                wave.Target.ForceMeshUpdate();
                Assert.That(wave.IsAnimating, Is.False);
                Assert.That(
                    GetFirstVisibleVertexY(wave.Target),
                    Is.EqualTo(authoredY).Within(0.01f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                if (instance != null)
                {
                    Object.DestroyImmediate(instance.gameObject);
                }
            }
        }

        private static float GetFirstVisibleVertexY(TextMeshProUGUI target)
        {
            TMP_TextInfo textInfo = target.textInfo;
            for (int index = 0; index < textInfo.characterCount; index++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[index];
                if (!character.isVisible)
                {
                    continue;
                }

                return textInfo
                    .meshInfo[character.materialReferenceIndex]
                    .vertices[character.vertexIndex]
                    .y;
            }

            Assert.Fail("Expected at least one visible pause-title character.");
            return 0f;
        }

        private static float[] GetVisibleVertexYs(TextMeshProUGUI target)
        {
            TMP_TextInfo textInfo = target.textInfo;
            var values = new float[textInfo.characterCount];
            int visibleCount = 0;
            for (int index = 0; index < textInfo.characterCount; index++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[index];
                if (!character.isVisible)
                {
                    continue;
                }

                values[visibleCount++] = textInfo
                    .meshInfo[character.materialReferenceIndex]
                    .vertices[character.vertexIndex]
                    .y;
            }

            Assert.That(visibleCount, Is.GreaterThan(0));
            if (visibleCount == values.Length)
            {
                return values;
            }

            var visibleValues = new float[visibleCount];
            System.Array.Copy(values, visibleValues, visibleCount);
            return visibleValues;
        }

        private static bool HasVisibleVertexMoved(
            TextMeshProUGUI target,
            float[] authoredValues,
            float minimumOffset)
        {
            TMP_TextInfo textInfo = target.textInfo;
            int visibleIndex = 0;
            for (int index = 0; index < textInfo.characterCount; index++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[index];
                if (!character.isVisible)
                {
                    continue;
                }

                Assert.That(visibleIndex, Is.LessThan(authoredValues.Length));
                float currentY = textInfo
                    .meshInfo[character.materialReferenceIndex]
                    .vertices[character.vertexIndex]
                    .y;
                if (currentY > authoredValues[visibleIndex] + minimumOffset)
                {
                    return true;
                }

                visibleIndex++;
            }

            Assert.That(visibleIndex, Is.EqualTo(authoredValues.Length));
            return false;
        }
    }
}
