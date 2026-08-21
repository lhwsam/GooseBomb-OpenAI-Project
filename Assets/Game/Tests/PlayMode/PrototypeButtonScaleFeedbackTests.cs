using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeButtonScaleFeedbackTests
    {
        [UnityTest]
        public IEnumerator PointerHoverAndPress_AnimateUsingUnscaledTime()
        {
            float previousTimeScale = Time.timeScale;
            GameObject root = null;
            try
            {
                Time.timeScale = 0f;
                PrototypeButtonScaleFeedback feedback = CreateFeedback(
                    out root,
                    out RectTransform visualTarget);
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                feedback.OnPointerEnter(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(visualTarget.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

                feedback.OnPointerDown(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(visualTarget.localScale.x, Is.EqualTo(0.96f).Within(0.002f));

                feedback.OnPointerUp(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(visualTarget.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

                feedback.OnPointerExit(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(visualTarget.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator KeyboardSelectionAndSubmit_ShowHighlightAndPressPulse()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out RectTransform visualTarget);
            try
            {
                feedback.OnSelect(new BaseEventData(null));
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(visualTarget.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

                feedback.OnSubmit(new BaseEventData(null));
                yield return new WaitForSecondsRealtime(0.025f);
                Assert.That(visualTarget.localScale.x, Is.EqualTo(0.96f).Within(0.002f));

                yield return new WaitForSecondsRealtime(0.09f);
                Assert.That(visualTarget.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

                feedback.OnDeselect(new BaseEventData(null));
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(visualTarget.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator SuppressedInitialSelection_StaysNormalUntilInteraction()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out RectTransform visualTarget);
            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            Color startColor = new Color(0.74f, 0.74f, 0.74f, 1f);
            Color targetColor = Color.white;
            feedback.ConfigureColorFeedback(label, startColor, targetColor);

            try
            {
                feedback.OnSelect(new BaseEventData(null));
                feedback.SuppressSelectionVisualUntilInteraction();
                yield return new WaitForSecondsRealtime(0.04f);

                Assert.That(visualTarget.localScale, Is.EqualTo(Vector3.one));
                Assert.That(label.color, Is.EqualTo(startColor));

                feedback.OnSubmit(new BaseEventData(null));
                yield return new WaitForSecondsRealtime(0.025f);

                Assert.That(
                    visualTarget.localScale.x,
                    Is.EqualTo(0.96f).Within(0.002f));
                Assert.That(label.color, Is.EqualTo(targetColor));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator PointerExit_AfterHoveringSelectedButton_ReturnsToBaseScale()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out RectTransform visualTarget);
            try
            {
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                feedback.OnSelect(new BaseEventData(null));
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(
                    visualTarget.localScale.x,
                    Is.EqualTo(1.06f).Within(0.002f));

                feedback.OnPointerEnter(pointer);
                feedback.OnPointerExit(pointer);
                yield return new WaitForSecondsRealtime(0.04f);

                Assert.That(visualTarget.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator PointerHover_TransfersSelectionAndBothButtonsReturnCorrectly()
        {
            var eventSystemObject = new GameObject(
                "ButtonFeedbackEventSystem",
                typeof(EventSystem));
            PrototypeButtonScaleFeedback first = CreateFeedback(
                out GameObject firstRoot,
                out RectTransform firstVisual);
            PrototypeButtonScaleFeedback second = CreateFeedback(
                out GameObject secondRoot,
                out RectTransform secondVisual);
            try
            {
                EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
                eventSystem.SetSelectedGameObject(firstRoot);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(firstVisual.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

                var pointer = new PointerEventData(eventSystem);
                second.OnPointerEnter(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(firstVisual.localScale, Is.EqualTo(Vector3.one));
                Assert.That(secondVisual.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

                second.OnPointerExit(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(secondVisual.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.Destroy(firstRoot);
                Object.Destroy(secondRoot);
                Object.Destroy(eventSystemObject);
            }
        }

        [UnityTest]
        public IEnumerator SeparateVisualTarget_PreservesItsAuthoredBaseScale()
        {
            var root = new GameObject(
                "ButtonFeedbackRoot",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            var visual = new GameObject(
                "Visual",
                typeof(RectTransform));
            RectTransform visualTarget = visual.GetComponent<RectTransform>();
            visualTarget.SetParent(root.transform, false);
            visualTarget.localScale = new Vector3(2f, 3f, 1f);
            PrototypeButtonScaleFeedback feedback =
                root.AddComponent<PrototypeButtonScaleFeedback>();
            feedback.Configure(visualTarget, 1.1f, 0.9f, 0.01f, 0.08f);

            try
            {
                feedback.OnPointerEnter(new PointerEventData(null));
                yield return new WaitForSecondsRealtime(0.04f);

                Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(visualTarget.localScale.x, Is.EqualTo(2.2f).Within(0.002f));
                Assert.That(visualTarget.localScale.y, Is.EqualTo(3.3f).Within(0.002f));
                Assert.That(visualTarget.localScale.z, Is.EqualTo(1.1f).Within(0.002f));

                feedback.enabled = false;
                Assert.That(
                    visualTarget.localScale,
                    Is.EqualTo(new Vector3(2f, 3f, 1f)));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator RapidPointerChanges_KillPreviousTweenAndReturnToBaseScale()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out RectTransform visualTarget);
            try
            {
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                for (int index = 0; index < 6; index++)
                {
                    feedback.OnPointerEnter(pointer);
                    feedback.OnPointerDown(pointer);
                    feedback.OnPointerUp(pointer);
                    feedback.OnPointerExit(pointer);
                }

                yield return new WaitForSecondsRealtime(0.04f);

                Assert.That(visualTarget.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator TextColorFeedback_UsesConfiguredColorsAndLeavesBackgroundUnchanged()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out _);
            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            Image background = root.GetComponent<Image>();
            Color backgroundColor = new Color(0.94f, 0.55f, 0.12f, 0f);
            Color startColor = new Color(0.74f, 0.74f, 0.74f, 1f);
            Color targetColor = new Color(1f, 0.72f, 0.22f, 1f);
            background.color = backgroundColor;
            feedback.ConfigureColorFeedback(label, startColor, targetColor);

            try
            {
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                feedback.OnPointerEnter(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(label.color, Is.EqualTo(targetColor));
                Assert.That(background.color, Is.EqualTo(backgroundColor));

                feedback.OnPointerDown(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(label.color, Is.EqualTo(targetColor));
                Assert.That(background.color, Is.EqualTo(backgroundColor));

                feedback.enabled = false;
                Assert.That(label.color, Is.EqualTo(startColor));
                Assert.That(background.color, Is.EqualTo(backgroundColor));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static PrototypeButtonScaleFeedback CreateFeedback(
            out GameObject root,
            out RectTransform visualTarget)
        {
            root = new GameObject(
                "ButtonFeedbackRoot",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            visualTarget = root.GetComponent<RectTransform>();
            PrototypeButtonScaleFeedback feedback =
                root.AddComponent<PrototypeButtonScaleFeedback>();
            feedback.Configure(visualTarget, 1.06f, 0.96f, 0.01f, 0.08f);
            return feedback;
        }
    }
}
