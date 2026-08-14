using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeRoomAdvanceController : MonoBehaviour
    {
        public const float DefaultTransitionDelaySeconds = 1.25f;

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private string nextSceneName = string.Empty;

        [SerializeField]
        private float transitionDelaySeconds = DefaultTransitionDelaySeconds;

        private Coroutine _transitionCoroutine;

        public PrototypeGameSession Session => session;

        public string NextSceneName => nextSceneName;

        public float TransitionDelaySeconds => transitionDelaySeconds;

        public bool IsTransitionPending { get; private set; }

        public void Configure(
            PrototypeGameSession gameSession,
            string authoredNextSceneName,
            float authoredTransitionDelaySeconds = DefaultTransitionDelaySeconds)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeRoomAdvanceController before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (authoredTransitionDelaySeconds <= 0f ||
                float.IsNaN(authoredTransitionDelaySeconds) ||
                float.IsInfinity(authoredTransitionDelaySeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredTransitionDelaySeconds),
                    authoredTransitionDelaySeconds,
                    "Room transition delay must be finite and positive.");
            }

            session = gameSession;
            nextSceneName = authoredNextSceneName ?? string.Empty;
            transitionDelaySeconds = authoredTransitionDelaySeconds;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null)
            {
                throw new InvalidOperationException(
                    "PrototypeRoomAdvanceController requires a game session reference.");
            }

            session.RoomCleared += OnRoomCleared;
            if (session.IsRoomCleared)
            {
                OnRoomCleared();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.RoomCleared -= OnRoomCleared;
            }
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            IsTransitionPending = false;
        }

        private void OnRoomCleared()
        {
            if (IsTransitionPending || string.IsNullOrWhiteSpace(nextSceneName))
            {
                return;
            }

            IsTransitionPending = true;
            WebGlHarnessReporter.Report("room-transition-started");
            _transitionCoroutine = StartCoroutine(LoadNextSceneAfterDelay());
        }

        private IEnumerator LoadNextSceneAfterDelay()
        {
            yield return new WaitForSecondsRealtime(transitionDelaySeconds);
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
    }
}
