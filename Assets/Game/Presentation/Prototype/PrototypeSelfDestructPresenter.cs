using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSelfDestructPresenter : MonoBehaviour
    {
        private const float SummonVfxCleanupPaddingSeconds = 0.25f;
        private const float WarningHologramToggleSeconds = 0.14f;
        private const float TelegraphHologramToggleSeconds = 0.065f;
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int TelegraphParameterId = Animator.StringToHash("Telegraph");
        private static readonly int DetonateParameterId = Animator.StringToHash("Detonate");
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        private GameObject instance;
        private Animator animator;
        private PrototypeHologramFeedback hologramFeedback;
        private PrototypeLocalVfxOverrides localVfxOverrides;
        private GameObject summonVfxAnchor;
        private ParticleSystem[] summonVfxSystems = Array.Empty<ParticleSystem>();
        private float summonVfxRemaining;
        private PigCharacterVocalAudio vocalAudio;
        private float deathRemaining;
        private bool isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => instance;

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => instance != null && instance.activeSelf;

        public int MoveCount { get; private set; }

        public int StateChangeCount { get; private set; }

        public int DeathCount { get; private set; }

        public Animator Animator => animator;

        public PrototypeHologramFeedback HologramFeedback => hologramFeedback;

        public SelfDestructEnemyState CurrentState { get; private set; }

        public int SummonVfxPlayCount { get; private set; }

        public bool IsSummonVfxActive =>
            summonVfxAnchor != null && summonVfxAnchor.activeSelf;

        public void Configure(PrototypeGameSession gameSession, Transform visualRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeSelfDestructPresenter before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (visualRoot == null)
            {
                throw new ArgumentNullException(nameof(visualRoot));
            }

            session = gameSession;
            presentationRoot = visualRoot;
        }

        public void ConfigureLocalVfxOverrides(
            PrototypeLocalVfxOverrides authoredLocalVfxOverrides)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeSelfDestructPresenter before changing its local VFX overrides.");
            }

            localVfxOverrides = authoredLocalVfxOverrides;
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
                    "PrototypeSelfDestructPresenter requires session and presentation-root references.");
            }

            localVfxOverrides ??= PrototypeLocalVfxOverrides.LoadOptional();
            session.SelfDestructAdvanced += OnSelfDestructAdvanced;
            session.SelfDestructSpawned += OnSelfDestructSpawned;
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
                session.SelfDestructAdvanced -= OnSelfDestructAdvanced;
                session.SelfDestructSpawned -= OnSelfDestructSpawned;
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
            DestroySummonVfx();
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

            if (summonVfxAnchor != null)
            {
                summonVfxRemaining -= Time.deltaTime;
                if (summonVfxRemaining <= 0f)
                {
                    DestroySummonVfx();
                }
            }

            if (instance != null && !isShowingDeath)
            {
                bool usesMovementTransition =
                    CurrentState == SelfDestructEnemyState.Chase ||
                    CurrentState == SelfDestructEnemyState.WarningChase;
                instance.transform.position = usesMovementTransition
                    ? session.GridSpace.GridToWorld(
                        session.CurrentSelfDestructMovementPosition) +
                        (Vector3.up * session.SelfDestructDefinition.VisualHeight)
                    : ToPresentationPosition(session.CurrentSelfDestructGridPosition);
            }

            SyncLocomotionAnimation();

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
            if (!session.HasSelfDestruct)
            {
                IsInitialized = true;
                CurrentState = SelfDestructEnemyState.Chase;
                return;
            }

            CreatePresentation();
        }

        private void OnSelfDestructSpawned(ActorId actorId)
        {
            if (actorId != session.SelfDestructActorId)
            {
                throw new InvalidOperationException(
                    "Prototype self-destruct presenter received another actor's spawn.");
            }
            if (instance == null)
            {
                CreatePresentation();
            }
            PlayBossSummonVfx();
        }

        private void CreatePresentation()
        {
            if (instance != null)
            {
                return;
            }

            PrototypeSelfDestructDefinitionAsset definition =
                session.SelfDestructDefinition;
            definition.ValidatePresentationReferences();
            instance = Instantiate(definition.EnemyPrefab, presentationRoot);
            instance.name = "PrototypeSelfDestructVisual";
            hologramFeedback =
                PrototypeHologramFeedback.CreateWarningFeedback(instance);
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
            instance.transform.position = ToPresentationPosition(
                session.CurrentSelfDestructGridPosition);
            instance.SetActive(session.IsSelfDestructAlive);
            CurrentState = session.CurrentSelfDestructState;
            ApplyAnimationState(CurrentState);
            RefreshWarningHologram();
            IsInitialized = true;
        }

        private void OnSelfDestructAdvanced(SelfDestructEnemyAdvanceResult result)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (result.ActorId != session.SelfDestructActorId)
            {
                throw new InvalidOperationException(
                    "Prototype self-destruct presenter received another actor's update.");
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
            if (result.HasStateTransition)
            {
                StateChangeCount++;
                CurrentState = result.State;
                if (CurrentState == SelfDestructEnemyState.Telegraph)
                {
                    instance.transform.position = ToPresentationPosition(
                        session.CurrentSelfDestructGridPosition);
                }
                ApplyAnimationState(CurrentState);
                RefreshWarningHologram();
            }
        }

        private void OnEnemyDamaged(EnemyDamageResult damage)
        {
            if (damage.ActorId == session.SelfDestructActorId &&
                hologramFeedback != null)
            {
                hologramFeedback.TriggerHitBlink();
            }
        }

        private void OnEnemyDied(EnemyDamageResult damage)
        {
            if (!session.HasSelfDestruct || damage.ActorId != session.SelfDestructActorId)
            {
                return;
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }

            DeathCount++;
            vocalAudio?.PlayDeathVocal();
            deathRemaining = session.SelfDestructDefinition.DeathVisualSeconds;
            isShowingDeath = true;
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
            SetSummonVfxPaused(isPaused);
        }

        private void ApplyAnimationState(SelfDestructEnemyState state)
        {
            if (animator == null)
            {
                return;
            }

            switch (state)
            {
                case SelfDestructEnemyState.Chase:
                case SelfDestructEnemyState.WarningChase:
                    break;
                case SelfDestructEnemyState.Telegraph:
                    animator.SetBool(IsMovingParameterId, false);
                    animator.ResetTrigger(DetonateParameterId);
                    animator.SetTrigger(TelegraphParameterId);
                    break;
                case SelfDestructEnemyState.Detonated:
                    animator.SetBool(IsMovingParameterId, false);
                    animator.ResetTrigger(TelegraphParameterId);
                    animator.SetTrigger(DetonateParameterId);
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
                (CurrentState == SelfDestructEnemyState.Chase ||
                    CurrentState == SelfDestructEnemyState.WarningChase) &&
                session.CurrentSelfDestructLocomotionState ==
                EnemyLocomotionState.Moving);
        }

        private void RefreshWarningHologram()
        {
            if (hologramFeedback == null)
            {
                return;
            }

            switch (CurrentState)
            {
                case SelfDestructEnemyState.WarningChase:
                    hologramFeedback.StartLooping(WarningHologramToggleSeconds);
                    break;
                case SelfDestructEnemyState.Telegraph:
                    hologramFeedback.StartLooping(TelegraphHologramToggleSeconds);
                    break;
                default:
                    hologramFeedback.StopAndRestore();
                    break;
            }
        }

        private void PlayBossSummonVfx()
        {
            GameObject prefab = localVfxOverrides != null
                ? localVfxOverrides.BossIntroSpawnVfxPrefab
                : null;
            if (prefab == null)
            {
                return;
            }

            DestroySummonVfx();
            summonVfxAnchor = new GameObject("PrototypeBossSelfDestructSummonVfx");
            summonVfxAnchor.transform.SetParent(presentationRoot, false);
            summonVfxAnchor.transform.position = session.GridSpace.GridToWorld(
                session.CurrentSelfDestructGridPosition);
            GameObject vfxInstance = Instantiate(
                prefab,
                summonVfxAnchor.transform,
                false);
            vfxInstance.name = prefab.name;
            vfxInstance.SetActive(true);
            summonVfxSystems =
                vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            RestartSummonVfxParticles();
            summonVfxRemaining = GetParticleLifetime(summonVfxSystems) +
                SummonVfxCleanupPaddingSeconds;
            session.BeginBossSelfDestructSpawnProtection(
                session.SelfDestructActorId,
                summonVfxRemaining);
            SummonVfxPlayCount++;
        }

        private void RestartSummonVfxParticles()
        {
            for (int index = 0; index < summonVfxSystems.Length; index++)
            {
                ParticleSystem system = summonVfxSystems[index];
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(false);
                if (session.IsPaused)
                {
                    system.Pause(false);
                }
            }
        }

        private void SetSummonVfxPaused(bool isPaused)
        {
            for (int index = 0; index < summonVfxSystems.Length; index++)
            {
                ParticleSystem system = summonVfxSystems[index];
                if (isPaused)
                {
                    if (system.isPlaying)
                    {
                        system.Pause(false);
                    }
                }
                else if (system.isPaused)
                {
                    system.Play(false);
                }
            }
        }

        private void DestroySummonVfx()
        {
            if (summonVfxAnchor != null)
            {
                summonVfxAnchor.SetActive(false);
                Destroy(summonVfxAnchor);
            }
            summonVfxAnchor = null;
            summonVfxSystems = Array.Empty<ParticleSystem>();
            summonVfxRemaining = 0f;
        }

        private static float GetParticleLifetime(ParticleSystem[] systems)
        {
            float lifetime = 0.1f;
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem.MainModule main = systems[index].main;
                lifetime = Mathf.Max(
                    lifetime,
                    main.startDelay.constantMax +
                    main.duration +
                    main.startLifetime.constantMax);
            }
            return lifetime;
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.SelfDestructDefinition.VisualHeight);
        }
    }
}
