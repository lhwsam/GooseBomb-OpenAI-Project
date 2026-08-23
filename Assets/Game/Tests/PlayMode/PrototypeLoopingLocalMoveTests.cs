using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeLoopingLocalMoveTests
    {
        [UnityTest]
        public IEnumerator LoopingMove_UsesCoreTweenAndRestoresAuthoredPosition()
        {
            var target = new GameObject("LoopingLocalMoveTarget");
            try
            {
                target.SetActive(false);
                var authoredPosition = new Vector3(0f, 100f, 0f);
                target.transform.localPosition = authoredPosition;
                PrototypeLoopingLocalMove move =
                    target.AddComponent<PrototypeLoopingLocalMove>();

                SetPrivateField(
                    move,
                    "endLocalPosition",
                    new Vector3(0f, 110f, 0f));
                SetPrivateField(move, "duration", 0.1f);
                SetPrivateField(move, "useUnscaledTime", true);
                target.SetActive(true);

                yield return new WaitForSecondsRealtime(0.03f);

                Assert.That(move.IsAnimating, Is.True);
                Assert.That(
                    target.transform.localPosition.y,
                    Is.GreaterThan(authoredPosition.y));

                move.enabled = false;

                Assert.That(move.IsAnimating, Is.False);
                Assert.That(target.transform.localPosition, Is.EqualTo(authoredPosition));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void SetPrivateField<T>(
            PrototypeLoopingLocalMove target,
            string fieldName,
            T value)
        {
            FieldInfo field = typeof(PrototypeLoopingLocalMove).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
