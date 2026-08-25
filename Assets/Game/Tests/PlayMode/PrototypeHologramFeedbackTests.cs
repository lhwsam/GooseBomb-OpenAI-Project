using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeHologramFeedbackTests
    {
        [UnityTest]
        public IEnumerator HitBlink_ShowsTwoHologramPhasesAndRestoresMaterial()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = visual.GetComponent<Renderer>();
            Material original = CreateMaterial("Original");
            Material hologram = CreateMaterial("Hologram");
            renderer.sharedMaterial = original;

            PrototypeHologramFeedback feedback =
                PrototypeHologramFeedback.CreateForRenderer(
                    renderer,
                    hologram,
                    PrototypeHologramFeedback.HitColor,
                    PrototypeHologramFeedback.HitTextureTint,
                    1f);
            feedback.TriggerHitBlink(blinkCount: 2, toggleSeconds: 0.03f);

            Assert.That(feedback.IsHitBlinkActive, Is.True);
            Assert.That(feedback.IsShowingHologram, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(hologram));

            feedback.Advance(0.031f);

            Assert.That(feedback.IsShowingHologram, Is.False);
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));

            feedback.Advance(0.03f);

            Assert.That(feedback.IsShowingHologram, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(hologram));

            feedback.Advance(0.061f);

            Assert.That(feedback.IsHitBlinkActive, Is.False);
            Assert.That(feedback.IsShowingHologram, Is.False);
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));

            Object.Destroy(visual);
            Object.Destroy(original);
            Object.Destroy(hologram);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoopingWarning_DoesNotChangeScaleAndRestoresOnStop()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = visual.GetComponent<Renderer>();
            Material original = CreateMaterial("Original");
            Material hologram = CreateMaterial("Hologram");
            renderer.sharedMaterial = original;
            Vector3 authoredScale = new Vector3(1.2f, 0.8f, 1.4f);
            visual.transform.localScale = authoredScale;

            PrototypeHologramFeedback feedback =
                PrototypeHologramFeedback.CreateForRenderer(
                    renderer,
                    hologram,
                    PrototypeHologramFeedback.WarningColor,
                    PrototypeHologramFeedback.WarningTextureTint,
                    1f);
            feedback.StartLooping(0.03f);

            Assert.That(feedback.IsLooping, Is.True);
            Assert.That(feedback.IsShowingHologram, Is.True);
            feedback.Advance(0.031f);
            Assert.That(feedback.IsLooping, Is.True);
            Assert.That(feedback.IsShowingHologram, Is.False);
            Assert.That(visual.transform.localScale, Is.EqualTo(authoredScale));

            feedback.StopAndRestore();

            Assert.That(feedback.IsLooping, Is.False);
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));
            Assert.That(visual.transform.localScale, Is.EqualTo(authoredScale));

            Object.Destroy(visual);
            Object.Destroy(original);
            Object.Destroy(hologram);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HitBlink_ResumesLoopingWarningAtConfiguredCadence()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = visual.GetComponent<Renderer>();
            Material original = CreateMaterial("Original");
            Material hologram = CreateMaterial("Hologram");
            renderer.sharedMaterial = original;

            PrototypeHologramFeedback feedback =
                PrototypeHologramFeedback.CreateForRenderer(
                    renderer,
                    hologram,
                    PrototypeHologramFeedback.WarningColor,
                    PrototypeHologramFeedback.WarningTextureTint,
                    1f);
            feedback.StartLooping(0.08f);
            feedback.TriggerHitBlink(blinkCount: 2, toggleSeconds: 0.03f);
            feedback.Advance(0.121f);

            Assert.That(feedback.IsHitBlinkActive, Is.False);
            Assert.That(feedback.IsLooping, Is.True);
            Assert.That(feedback.IsShowingHologram, Is.True);

            feedback.Advance(0.081f);

            Assert.That(feedback.IsLooping, Is.True);
            Assert.That(feedback.IsShowingHologram, Is.False);

            feedback.StopAndRestore();
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));

            Object.Destroy(visual);
            Object.Destroy(original);
            Object.Destroy(hologram);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TelegraphStyle_UsesBombRangeMaterialAndProperties()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = visual.GetComponent<Renderer>();
            Material original = CreateMaterial("Original");
            Material hologram = CreateMaterial("Hologram");
            renderer.sharedMaterial = original;

            Assert.That(
                PrototypeHologramTelegraphStyle.Apply(visual, hologram),
                Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(hologram));

            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            AssertColorApproximately(
                propertyBlock.GetColor("_Hologram_Color"),
                PrototypeHologramTelegraphStyle.HologramColor);
            AssertColorApproximately(
                propertyBlock.GetColor("_Texture_Tint_Color"),
                PrototypeHologramTelegraphStyle.TextureTint);
            Assert.That(
                propertyBlock.GetFloat("_Emission_Scale"),
                Is.EqualTo(PrototypeHologramTelegraphStyle.EmissionScale));

            Object.Destroy(visual);
            Object.Destroy(original);
            Object.Destroy(hologram);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TelegraphStyle_SkipsRootsWithoutMeshRenderers()
        {
            var visual = new GameObject("RendererlessTelegraph");
            Material hologram = CreateMaterial("Hologram");

            Assert.That(
                PrototypeHologramTelegraphStyle.Apply(visual, hologram),
                Is.False);

            Object.Destroy(visual);
            Object.Destroy(hologram);
            yield return null;
        }

        private static void AssertColorApproximately(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

        private static Material CreateMaterial(string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            return new Material(shader)
            {
                name = name,
            };
        }
    }
}
