using System;
using DG.Tweening;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerDeathPresenter : MonoBehaviour
    {
        public const string DeathAnimationClipName = "player_die";
        public const float DefaultFocusedOrthographicSize = 4.2f;
        public const float DefaultFocusSeconds = 1f;
        public const float DefaultDeathAnimatorSpeed = 0.75f;
        public const float DefaultDeathPoseHoldSeconds = 0.2f;
        public const float DefaultCoverSeconds = 0.4f;
        public const float DefaultCoverHoldSeconds = 0.18f;
        public const float DefaultRevealSeconds = 0.25f;

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private PrototypePlayerController playerController;

        [SerializeField]
        private PrototypePlayerAnimationPresenter playerAnimationPresenter;

        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private PrototypeCameraShake cameraShake;

        [SerializeField]
        private PrototypeBossClearTransitionView transitionViewPrefab;

        [SerializeField]
        [Min(0.01f)]
        private float focusedOrthographicSize = DefaultFocusedOrthographicSize;

        [SerializeField]
        [Range(0.05f, 1f)]
        private float deathAnimatorSpeed = DefaultDeathAnimatorSpeed;

        private Sequence _deathSequence;
        private PrototypeBossClearTransitionView _transitionViewInstance;
        private Vector3 _cameraRestingPosition;
        private float _cameraRestingOrthographicSize;
        private float _animatorRestingSpeed;
        private AnimatorUpdateMode _animatorRestingUpdateMode;
        private bool _cameraStateCaptured;
        private bool _animatorStateCaptured;
        private bool _started;

        public event Action Completed;

        public PrototypeGameSession Session => session;

        public PrototypePlayerController PlayerController => playerController;

        public PrototypePlayerAnimationPresenter PlayerAnimationPresenter =>
            playerAnimationPresenter;

        public Camera GameplayCamera => gameplayCamera;

        public PrototypeCameraShake CameraShake => cameraShake;

        public PrototypeBossClearTransitionView TransitionViewPrefab =>
            transitionViewPrefab;

        public PrototypeBossClearTransitionView TransitionViewInstance =>
            _transitionViewInstance;

        public float FocusedOrthographicSize => focusedOrthographicSize;

        public float DeathAnimatorSpeed => deathAnimatorSpeed;

        public float DeathAnimationDurationSeconds { get; private set; }

        public bool HasStarted => _started;

        public bool IsPlaying =>
            _deathSequence != null && _deathSequence.IsActive() && !IsCompleted;

        public bool IsCompleted { get; private set; }

        public int CompletionCount { get; private set; }

        public void Configure(
            PrototypeGameSession gameSession,
            PrototypePlayerController authoredPlayerController,
            PrototypePlayerAnimationPresenter authoredAnimationPresenter,
            Camera authoredCamera,
            PrototypeCameraShake authoredCameraShake,
            PrototypeBossClearTransitionView authoredTransitionViewPrefab)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypePlayerDeathPresenter before changing its configuration.");
            }

            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            playerController = authoredPlayerController ??
                throw new ArgumentNullException(nameof(authoredPlayerController));
            playerAnimationPresenter = authoredAnimationPresenter ??
                throw new ArgumentNullException(nameof(authoredAnimationPresenter));
            gameplayCamera = authoredCamera ??
                throw new ArgumentNullException(nameof(authoredCamera));
            cameraShake = authoredCameraShake ??
                throw new ArgumentNullException(nameof(authoredCameraShake));
            transitionViewPrefab = authoredTransitionViewPrefab ??
                throw new ArgumentNullException(nameof(authoredTransitionViewPrefab));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ValidateReferences();
            session.PlayerDied += OnPlayerDied;
            if (session.IsPlayerDead)
            {
                TryStartPresentation();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.PlayerDied -= OnPlayerDied;
            }

            KillSequence();
            RestoreCameraState();
            RestoreAnimatorState();
            DestroyTransitionView();
        }

        private void OnValidate()
        {
            focusedOrthographicSize = Mathf.Max(0.01f, focusedOrthographicSize);
            deathAnimatorSpeed = Mathf.Clamp(deathAnimatorSpeed, 0.05f, 1f);
        }

        private void OnPlayerDied(BombSwap.Core.PlayerDamageResult _)
        {
            TryStartPresentation();
        }

        private void TryStartPresentation()
        {
            if (_started || IsCompleted || !session.IsPlayerDead)
            {
                return;
            }
            if (!gameplayCamera.orthographic)
            {
                throw new InvalidOperationException(
                    "Prototype player death presentation requires an orthographic gameplay camera.");
            }

            _started = true;
            session.enabled = false;
            cameraShake.Stop();
            CaptureCameraState();
            CaptureAndSlowAnimator();
            CreateTransitionView();

            Vector3 focusedPosition =
                PrototypeCameraFramingUtility.CalculateGroundFocusPosition(
                    gameplayCamera,
                    playerController.PlayerTransform.position);
            float remainingDeathSeconds = Mathf.Max(
                0f,
                DeathAnimationDurationSeconds - DefaultFocusSeconds);

            _deathSequence = DOTween.Sequence().SetUpdate(true);
            _deathSequence.Join(DOTween.To(
                    () => gameplayCamera.transform.position,
                    value => gameplayCamera.transform.position = value,
                    focusedPosition,
                    DefaultFocusSeconds)
                .SetEase(Ease.InOutCubic));
            _deathSequence.Join(DOTween.To(
                    () => gameplayCamera.orthographicSize,
                    value => gameplayCamera.orthographicSize = value,
                    focusedOrthographicSize,
                    DefaultFocusSeconds)
                .SetEase(Ease.InOutCubic));
            if (remainingDeathSeconds > 0f)
            {
                _deathSequence.AppendInterval(remainingDeathSeconds);
            }
            _deathSequence.AppendInterval(DefaultDeathPoseHoldSeconds);
            _deathSequence.Append(
                _transitionViewInstance.CreateCloseTween(DefaultCoverSeconds));
            _deathSequence.AppendCallback(CompletePresentationUnderCover);
            _deathSequence.AppendInterval(DefaultCoverHoldSeconds);
            _deathSequence.Append(
                _transitionViewInstance.CreateRevealTween(DefaultRevealSeconds));
            _deathSequence.OnComplete(FinishSequence);
        }

        private void CaptureCameraState()
        {
            _cameraRestingPosition = gameplayCamera.transform.position;
            _cameraRestingOrthographicSize = gameplayCamera.orthographicSize;
            _cameraStateCaptured = true;
            WebGlHarnessReporter.Report("player-death-presentation-started");
        }

        private void CaptureAndSlowAnimator()
        {
            Animator animator = playerAnimationPresenter.Animator;
            _animatorRestingSpeed = animator.speed;
            _animatorRestingUpdateMode = animator.updateMode;
            _animatorStateCaptured = true;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = deathAnimatorSpeed;
            DeathAnimationDurationSeconds =
                ResolveDeathClipDuration(animator) / deathAnimatorSpeed;
        }

        private void CreateTransitionView()
        {
            _transitionViewInstance = Instantiate(
                transitionViewPrefab,
                transform,
                false);
            _transitionViewInstance.name = transitionViewPrefab.name;
            if (!_transitionViewInstance.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Instantiated player-death transition view is missing required references.");
            }
            _transitionViewInstance.PrepareForClosing();
        }

        private void CompletePresentationUnderCover()
        {
            RestoreCameraState();
            RestoreAnimatorState();
            IsCompleted = true;
            CompletionCount++;
            WebGlHarnessReporter.Report("player-death-presentation-completed");
            Completed?.Invoke();
        }

        private void FinishSequence()
        {
            _deathSequence = null;
            DestroyTransitionView();
        }

        private void RestoreCameraState()
        {
            if (!_cameraStateCaptured || gameplayCamera == null)
            {
                return;
            }

            gameplayCamera.transform.position = _cameraRestingPosition;
            gameplayCamera.orthographicSize = _cameraRestingOrthographicSize;
            _cameraStateCaptured = false;
        }

        private void RestoreAnimatorState()
        {
            if (!_animatorStateCaptured || playerAnimationPresenter == null ||
                playerAnimationPresenter.Animator == null)
            {
                return;
            }

            Animator animator = playerAnimationPresenter.Animator;
            animator.speed = _animatorRestingSpeed;
            animator.updateMode = _animatorRestingUpdateMode;
            _animatorStateCaptured = false;
        }

        private void KillSequence()
        {
            if (_deathSequence == null)
            {
                return;
            }

            _deathSequence.Kill(false);
            _deathSequence = null;
        }

        private void DestroyTransitionView()
        {
            if (_transitionViewInstance != null)
            {
                Destroy(_transitionViewInstance.gameObject);
                _transitionViewInstance = null;
            }
        }

        private void ValidateReferences()
        {
            if (session == null || playerController == null ||
                playerAnimationPresenter == null || gameplayCamera == null ||
                cameraShake == null || transitionViewPrefab == null ||
                !transitionViewPrefab.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "PrototypePlayerDeathPresenter requires session, player controller, player animation, camera, camera shake, and transition-view references.");
            }
            if (playerController.Session != session ||
                playerAnimationPresenter.Session != session ||
                playerController.PlayerTransform == null ||
                playerAnimationPresenter.Animator == null ||
                cameraShake.ShakeTarget != gameplayCamera.transform)
            {
                throw new InvalidOperationException(
                    "Prototype player-death references must share the same session, player, and gameplay camera.");
            }
            ResolveDeathClipDuration(playerAnimationPresenter.Animator);
        }

        private static float ResolveDeathClipDuration(Animator animator)
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "Player death presentation requires a runtime Animator controller.");
            }

            AnimationClip[] clips = controller.animationClips;
            for (int index = 0; index < clips.Length; index++)
            {
                AnimationClip clip = clips[index];
                if (clip != null && string.Equals(
                        clip.name,
                        DeathAnimationClipName,
                        StringComparison.Ordinal))
                {
                    return clip.length;
                }
            }

            throw new InvalidOperationException(
                $"Player Animator controller requires the '{DeathAnimationClipName}' clip.");
        }
    }
}
