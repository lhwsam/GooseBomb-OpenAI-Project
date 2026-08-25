using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeThrowerPresenter : MonoBehaviour
    {
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int ThrowParameterId = Animator.StringToHash("Throw");
        private static readonly int RecoverParameterId = Animator.StringToHash("Recover");
        private static readonly int DieParameterId = Animator.StringToHash("Die");
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private float pulseHz = 6f;

        private GameObject instance;
        private Animator animator;
        private PrototypeHologramFeedback hologramFeedback;
        private PigCharacterVocalAudio vocalAudio;
        private Vector3 baseScale;
        private float pulsePhase;
        private float deathRemaining;
        private bool isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => instance;

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => instance != null && instance.activeSelf;

        public int MoveCount { get; private set; }

        public int TelegraphCount { get; private set; }

        public int DeathCount { get; private set; }

        public Animator Animator => animator;

        public PrototypeHologramFeedback HologramFeedback => hologramFeedback;

        public void Configure(PrototypeGameSession gameSession, Transform visualRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeThrowerPresenter before changing its runtime configuration.");
            }
            session = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            presentationRoot = visualRoot ?? throw new ArgumentNullException(nameof(visualRoot));
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || presentationRoot == null)
            {
                throw new InvalidOperationException(
                    "PrototypeThrowerPresenter requires session and presentation-root references.");
            }

            session.ThrowerAdvanced += OnThrowerAdvanced;
            session.EnemyDamaged += OnEnemyDamaged;
            session.EnemyDied += OnEnemyDied;
            session.PauseStateChanged += OnPauseStateChanged;
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                InitializePresentation();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.ThrowerAdvanced -= OnThrowerAdvanced;
                session.EnemyDamaged -= OnEnemyDamaged;
                session.EnemyDied -= OnEnemyDied;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }
            if (instance != null)
            {
                Destroy(instance);
            }
            instance = null;
            hologramFeedback = null;
            if (animator != null)
            {
                animator.speed = 1f;
            }
            animator = null;
            IsInitialized = false;
            isShowingDeath = false;
        }

        private void Update()
        {
            if (session != null && session.IsPaused)
            {
                return;
            }

            if (instance != null && !isShowingDeath)
            {
                instance.transform.position = session.GridSpace.GridToWorld(
                    session.CurrentThrowerMovementPosition) +
                    (Vector3.up * session.ThrowerDefinition.VisualHeight);
            }
            SyncLocomotionAnimation();
            if (!isShowingDeath &&
                session.CurrentThrowerState == ThrowerEnemyState.Telegraph &&
                !session.IsPaused)
            {
                pulsePhase = Mathf.Repeat(pulsePhase + (Time.deltaTime * pulseHz), 1f);
                float wave = 0.5f + (Mathf.Sin(pulsePhase * Mathf.PI * 2f) * 0.5f);
                instance.transform.localScale = baseScale * Mathf.Lerp(1f, 1.12f, wave);
            }
            if (isShowingDeath && instance != null)
            {
                deathRemaining -= Time.unscaledDeltaTime;
                if (deathRemaining <= 0f)
                {
                    instance.SetActive(false);
                    isShowingDeath = false;
                }
            }
        }

        private void OnSessionReady()
        {
            InitializePresentation();
        }

        private void InitializePresentation()
        {
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;
            if (!session.HasThrower)
            {
                return;
            }

            PrototypeThrowerDefinitionAsset definition = session.ThrowerDefinition;
            definition.ValidatePresentationReferences();
            instance = Instantiate(definition.EnemyPrefab, presentationRoot);
            instance.name = "PrototypeThrowerVisual";
            hologramFeedback =
                PrototypeHologramFeedback.CreateHitFeedback(instance);
            if (hologramFeedback != null)
            {
                hologramFeedback.SetPaused(session.IsPaused);
            }
            animator = instance.GetComponentInChildren<Animator>(true);
            vocalAudio = instance.GetComponentInChildren<PigCharacterVocalAudio>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.speed = session.IsPaused ? 0f : 1f;
                animator.SetBool(IsMovingParameterId, false);
            }
            baseScale = instance.transform.localScale;
            instance.transform.position = ToPresentationPosition(
                session.CurrentThrowerGridPosition);
            instance.SetActive(session.IsThrowerAlive);

            ApplyAnimationState(session.CurrentThrowerState);

            if (session.CurrentThrowerState == ThrowerEnemyState.Telegraph)
            {
                pulsePhase = 0f;
            }
        }

        private void OnThrowerAdvanced(ThrowerEnemyAdvanceResult result)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (result.ActorId != session.ThrowerActorId)
            {
                throw new InvalidOperationException(
                    "Prototype thrower presenter received another actor's update.");
            }
            if (result.HasMovement)
            {
                MoveCount++;
                Vector3 facing = ToPresentationPosition(result.Movement.To) -
                    ToPresentationPosition(result.Movement.From);
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.0001f)
                {
                    instance.transform.rotation =
                        Quaternion.LookRotation(facing.normalized, Vector3.up);
                }
            }
            if (!result.HasStateTransition)
            {
                return;
            }

            if (result.State == ThrowerEnemyState.Telegraph)
            {
                TelegraphCount++;
                pulsePhase = 0f;
                vocalAudio?.PlayAttackVocal();
            }
            else if (instance != null)
            {
                instance.transform.localScale = baseScale;
            }
            ApplyAnimationState(result.State);
        }

        private void OnEnemyDied(EnemyDamageResult damage)
        {
            if (!session.HasThrower || damage.ActorId != session.ThrowerActorId)
            {
                return;
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            DeathCount++;
            vocalAudio?.PlayDeathVocal();
            if (animator != null)
            {
                animator.SetBool(IsMovingParameterId, false);
                animator.ResetTrigger(ThrowParameterId);
                animator.ResetTrigger(RecoverParameterId);
                animator.SetTrigger(DieParameterId);
            }
            instance.transform.localScale = baseScale;
            deathRemaining = session.ThrowerDefinition.DeathVisualSeconds;
            isShowingDeath = true;
        }

        private void OnEnemyDamaged(EnemyDamageResult damage)
        {
            if (damage.ActorId == session.ThrowerActorId &&
                hologramFeedback != null)
            {
                hologramFeedback.TriggerHitBlink();
            }
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (animator != null)
            {
                animator.speed = isPaused ? 0f : 1f;
            }
            if (hologramFeedback != null)
            {
                hologramFeedback.SetPaused(isPaused);
            }
        }

        private void ApplyAnimationState(ThrowerEnemyState state)
        {
            if (animator == null)
            {
                return;
            }

            switch (state)
            {
                case ThrowerEnemyState.Track:
                    animator.ResetTrigger(ThrowParameterId);
                    animator.ResetTrigger(RecoverParameterId);
                    break;
                case ThrowerEnemyState.Telegraph:
                    animator.SetBool(IsMovingParameterId, false);
                    animator.ResetTrigger(RecoverParameterId);
                    animator.SetTrigger(ThrowParameterId);
                    break;
                case ThrowerEnemyState.Recover:
                    animator.SetBool(IsMovingParameterId, false);
                    animator.ResetTrigger(ThrowParameterId);
                    animator.SetTrigger(RecoverParameterId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void SetMovingAnimation(bool isMoving)
        {
            if (animator != null)
            {
                animator.SetBool(IsMovingParameterId, isMoving);
            }
        }

        private void SyncLocomotionAnimation()
        {
            if (isShowingDeath)
            {
                SetMovingAnimation(false);
                return;
            }

            SetMovingAnimation(
                session.CurrentThrowerState == ThrowerEnemyState.Track &&
                session.CurrentThrowerLocomotionState == EnemyLocomotionState.Moving);
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.ThrowerDefinition.VisualHeight);
        }
    }
}
