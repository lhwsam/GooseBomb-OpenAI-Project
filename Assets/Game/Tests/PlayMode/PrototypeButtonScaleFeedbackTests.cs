using System.Collections;
using System.Linq;
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
        private const string ClickVoiceObjectName =
            "Prototype UI Button Click Voice";

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
                AssertColorApproximately(label.color, targetColor);
                Assert.That(background.color, Is.EqualTo(backgroundColor));

                feedback.OnPointerDown(pointer);
                yield return new WaitForSecondsRealtime(0.04f);
                AssertColorApproximately(label.color, targetColor);
                Assert.That(background.color, Is.EqualTo(backgroundColor));

                feedback.enabled = false;
                AssertColorApproximately(label.color, startColor);
                Assert.That(background.color, Is.EqualTo(backgroundColor));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static void AssertColorApproximately(Color actual, Color expected)
        {
            const float tolerance = 0.0001f;
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        [UnityTest]
        public IEnumerator HoverVisuals_FollowHoverSelectionAndDisabledStates()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out _);
            var leftArrow = new GameObject("Arrow_Left", typeof(RectTransform));
            var rightArrow = new GameObject("Arrow_Right", typeof(RectTransform));
            leftArrow.transform.SetParent(root.transform, false);
            rightArrow.transform.SetParent(root.transform, false);
            leftArrow.SetActive(false);
            rightArrow.SetActive(false);
            feedback.ConfigureHoverVisuals(new[] { leftArrow, rightArrow });

            try
            {
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };

                feedback.OnPointerEnter(pointer);
                Assert.That(leftArrow.activeSelf, Is.True);
                Assert.That(rightArrow.activeSelf, Is.True);

                feedback.OnPointerDown(pointer);
                Assert.That(leftArrow.activeSelf, Is.True);
                Assert.That(rightArrow.activeSelf, Is.True);

                feedback.OnPointerExit(pointer);
                Assert.That(leftArrow.activeSelf, Is.False);
                Assert.That(rightArrow.activeSelf, Is.False);

                feedback.OnSelect(new BaseEventData(null));
                Assert.That(leftArrow.activeSelf, Is.True);
                Assert.That(rightArrow.activeSelf, Is.True);

                root.GetComponent<Button>().interactable = false;
                yield return null;
                Assert.That(leftArrow.activeSelf, Is.False);
                Assert.That(rightArrow.activeSelf, Is.False);

                feedback.enabled = false;
                Assert.That(leftArrow.activeSelf, Is.False);
                Assert.That(rightArrow.activeSelf, Is.False);
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator PointerHoverAndConfirmedClick_UseSharedCanvasAudio()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out _);
            var audioRoot = new GameObject(
                "ButtonAudio",
                typeof(AudioSource),
                typeof(PrototypeUiButtonAudioPlayer));
            AudioClip hoverClip = AudioClip.Create(
                "HoverClip",
                4410,
                1,
                44100,
                false);
            AudioClip clickClip = AudioClip.Create(
                "ClickClip",
                4410,
                1,
                44100,
                false);
            PrototypeUiButtonAudioPlayer audioPlayer =
                audioRoot.GetComponent<PrototypeUiButtonAudioPlayer>();
            audioPlayer.Configure(
                audioRoot.GetComponent<AudioSource>(),
                hoverClip,
                clickClip);
            feedback.ConfigureAudio(audioPlayer);

            try
            {
                var pointer = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };
                Button button = root.GetComponent<Button>();

                feedback.OnPointerEnter(pointer);
                Assert.That(audioPlayer.HoverPlayCount, Is.EqualTo(1));
                Assert.That(audioPlayer.LastPlayedClip, Is.SameAs(hoverClip));

                feedback.OnPointerEnter(pointer);
                Assert.That(
                    audioPlayer.HoverPlayCount,
                    Is.EqualTo(1),
                    "Repeated enter callbacks must not restart the same hover cue.");

                feedback.OnPointerExit(pointer);
                feedback.OnPointerEnter(pointer);
                Assert.That(audioPlayer.HoverPlayCount, Is.EqualTo(2));

                int confirmedClickCount = 0;
                button.onClick.AddListener(audioPlayer.PlayClick);
                button.onClick.AddListener(() =>
                {
                    confirmedClickCount++;
                    audioRoot.SetActive(false);
                });

                ExecuteEvents.Execute<IPointerClickHandler>(
                    root,
                    pointer,
                    ExecuteEvents.pointerClickHandler);
                Assert.That(confirmedClickCount, Is.EqualTo(1));
                Assert.That(audioPlayer.ClickPlayCount, Is.EqualTo(1));
                Assert.That(audioPlayer.LastPlayedClip, Is.SameAs(clickClip));
                AudioSource clickVoice = FindClickVoices().Single();
                Assert.That(clickVoice, Is.Not.SameAs(audioPlayer.AudioSource));
                Assert.That(clickVoice.gameObject.activeInHierarchy, Is.True);

                Assert.That(
                    clickVoice.gameObject.activeInHierarchy,
                    Is.True,
                    "A click voice must survive the next listener disabling its Canvas audio player.");
                audioRoot.SetActive(true);

                button.interactable = false;
                feedback.OnPointerExit(pointer);
                feedback.OnPointerEnter(pointer);
                ExecuteEvents.Execute<IPointerClickHandler>(
                    root,
                    pointer,
                    ExecuteEvents.pointerClickHandler);
                Assert.That(audioPlayer.HoverPlayCount, Is.EqualTo(2));
                Assert.That(audioPlayer.ClickPlayCount, Is.EqualTo(1));
                Assert.That(confirmedClickCount, Is.EqualTo(1));

                feedback.enabled = false;
                button.interactable = true;
                ExecuteEvents.Execute<IPointerClickHandler>(
                    root,
                    pointer,
                    ExecuteEvents.pointerClickHandler);
                Assert.That(
                    audioPlayer.ClickPlayCount,
                    Is.EqualTo(2),
                    "Click audio must not depend on the optional visual feedback component.");
                Assert.That(
                    confirmedClickCount,
                    Is.EqualTo(2),
                    "Disabling feedback must not disable the Button action.");

                audioRoot.SetActive(true);
                feedback.enabled = true;
                ExecuteEvents.Execute<IPointerClickHandler>(
                    root,
                    pointer,
                    ExecuteEvents.pointerClickHandler);
                Assert.That(audioPlayer.ClickPlayCount, Is.EqualTo(3));
                Assert.That(confirmedClickCount, Is.EqualTo(3));
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(audioRoot);
                Object.Destroy(hoverClip);
                Object.Destroy(clickClip);
                AudioSource[] clickVoices = FindClickVoices();
                for (int index = 0; index < clickVoices.Length; index++)
                {
                    Object.Destroy(clickVoices[index].gameObject);
                }
            }

            yield return null;
        }

        private static AudioSource[] FindClickVoices()
        {
            return Object.FindObjectsByType<AudioSource>(
                    FindObjectsInactive.Include)
                .Where(source =>
                    source.name == ClickVoiceObjectName)
                .ToArray();
        }

        [UnityTest]
        public IEnumerator UnconfiguredDirectArrowChildren_AreNotDiscoveredByName()
        {
            PrototypeButtonScaleFeedback feedback = CreateFeedback(
                out GameObject root,
                out _);
            var arrow = new GameObject("Arrow_Left", typeof(RectTransform));
            arrow.transform.SetParent(root.transform, false);
            arrow.SetActive(false);

            try
            {
                feedback.enabled = false;
                feedback.enabled = true;
                feedback.OnPointerEnter(new PointerEventData(null));

                Assert.That(arrow.activeSelf, Is.False);
            }
            finally
            {
                Object.Destroy(root);
            }

            yield return null;
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
