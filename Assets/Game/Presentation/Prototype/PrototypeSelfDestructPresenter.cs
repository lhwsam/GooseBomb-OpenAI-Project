using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSelfDestructPresenter : MonoBehaviour
    {
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int TelegraphParameterId = Animator.StringToHash("Telegraph");
        private static readonly int DetonateParameterId = Animator.StringToHash("Detonate");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Color telegraphColor = new Color(1f, 0.2f, 0.04f, 1f);

        [SerializeField]
        private Color deathColor = new Color(0.18f, 0.01f, 0.01f, 1f);

        [SerializeField]
        private float warningPulseHz = 3f;

        [SerializeField]
        private float telegraphPulseHz = 8f;

        [SerializeField]
        private float warningScaleMultiplier = 1.08f;

        [SerializeField]
        private float telegraphScaleMultiplier = 1.18f;

        private readonly List<GameObject> telegraphCells = new List<GameObject>();
        private GameObject instance;
        private Renderer instanceRenderer;
        private Animator animator;
        private MaterialPropertyBlock propertyBlock;
        private int colorPropertyId;
        private Color normalColor;
        private Vector3 baseScale;
        private float deathRemaining;
        private float pulsePhase;
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

        public int ActiveTelegraphCellCount { get; private set; }

        public SelfDestructEnemyState CurrentState { get; private set; }

        public Color CurrentColor { get; private set; }

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

            session.SelfDestructAdvanced += OnSelfDestructAdvanced;
            session.SelfDestructSpawned += OnSelfDestructSpawned;
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
                session.EnemyDied -= OnEnemyDied;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }
            if (instance != null)
            {
                Destroy(instance);
            }
            for (int index = 0; index < telegraphCells.Count; index++)
            {
                if (telegraphCells[index] != null)
                {
                    Destroy(telegraphCells[index]);
                }
            }

            instance = null;
            instanceRenderer = null;
            if (animator != null)
            {
                animator.speed = 1f;
            }
            animator = null;
            telegraphCells.Clear();
            ActiveTelegraphCellCount = 0;
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

            if (!isShowingDeath &&
                instance != null &&
                session != null &&
                (CurrentState == SelfDestructEnemyState.WarningChase ||
                    CurrentState == SelfDestructEnemyState.Telegraph))
            {
                ApplyPulse(Time.deltaTime);
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
            instanceRenderer = instance.GetComponentInChildren<Renderer>(true);
            animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.speed = session.IsPaused ? 0f : 1f;
                animator.SetBool(IsMovingParameterId, false);
            }
            baseScale = instance.transform.localScale;
            InitializeColor();
            instance.transform.position = ToPresentationPosition(
                session.CurrentSelfDestructGridPosition);
            instance.SetActive(session.IsSelfDestructAlive);
            CurrentState = session.CurrentSelfDestructState;
            ApplyAnimationState(CurrentState);
            ApplyStateVisual(CurrentState);
            if (CurrentState == SelfDestructEnemyState.Telegraph)
            {
                ShowTelegraph();
            }
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
                ApplyStateVisual(CurrentState);
                if (CurrentState == SelfDestructEnemyState.Telegraph)
                {
                    ShowTelegraph();
                }
                else
                {
                    HideTelegraph();
                }
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
            HideTelegraph();
            instance.transform.localScale = baseScale;
            ApplyColor(deathColor);
            deathRemaining = session.SelfDestructDefinition.DeathVisualSeconds;
            isShowingDeath = true;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (animator != null)
            {
                animator.speed = isPaused ? 0f : 1f;
            }
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

        private void ShowTelegraph()
        {
            HideTelegraph();
            IReadOnlyList<GridPosition> cells = session.CurrentSelfDestructTelegraphCells;
            EnsureTelegraphCapacity(cells.Count);
            for (int index = 0; index < cells.Count; index++)
            {
                GameObject visual = telegraphCells[index];
                visual.transform.position = session.GridSpace.GridToWorld(cells[index]) +
                    (Vector3.up * 0.03f);
                visual.SetActive(true);
            }

            ActiveTelegraphCellCount = cells.Count;
        }

        private void HideTelegraph()
        {
            for (int index = 0; index < ActiveTelegraphCellCount; index++)
            {
                if (telegraphCells[index] != null)
                {
                    telegraphCells[index].SetActive(false);
                }
            }

            ActiveTelegraphCellCount = 0;
        }

        private void EnsureTelegraphCapacity(int required)
        {
            while (telegraphCells.Count < required)
            {
                GameObject visual = Instantiate(
                    session.SelfDestructDefinition.TelegraphCellPrefab,
                    presentationRoot);
                visual.name = $"PrototypeSelfDestructTelegraphCell{telegraphCells.Count}";
                visual.SetActive(false);
                telegraphCells.Add(visual);
            }
        }

        private void InitializeColor()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            Material material = instanceRenderer.sharedMaterial;
            if (material == null)
            {
                throw new InvalidOperationException(
                    "Self-destruct enemy prefab renderer requires a material.");
            }
            if (material.HasProperty(BaseColorId))
            {
                colorPropertyId = BaseColorId;
            }
            else if (material.HasProperty(ColorId))
            {
                colorPropertyId = ColorId;
            }
            else
            {
                throw new InvalidOperationException(
                    "Self-destruct enemy material requires a supported color property.");
            }

            normalColor = material.GetColor(colorPropertyId);
            CurrentColor = normalColor;
        }

        private void ApplyStateVisual(SelfDestructEnemyState state)
        {
            pulsePhase = 0f;
            instance.transform.localScale = baseScale;
            switch (state)
            {
                case SelfDestructEnemyState.Chase:
                    ApplyColor(normalColor);
                    break;
                case SelfDestructEnemyState.WarningChase:
                    ApplyColor(Color.Lerp(normalColor, telegraphColor, 0.5f));
                    break;
                case SelfDestructEnemyState.Telegraph:
                    ApplyColor(telegraphColor);
                    break;
                case SelfDestructEnemyState.Detonated:
                    ApplyColor(deathColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ApplyPulse(float elapsedSeconds)
        {
            float warningProgress = CurrentState == SelfDestructEnemyState.WarningChase
                ? Mathf.Clamp01(session.CurrentSelfDestructWarningProgress)
                : 1f;
            float frequency = Mathf.Lerp(
                warningPulseHz,
                telegraphPulseHz,
                warningProgress);
            float scaleMultiplier = Mathf.Lerp(
                warningScaleMultiplier,
                telegraphScaleMultiplier,
                warningProgress);
            pulsePhase = Mathf.Repeat(
                pulsePhase + (elapsedSeconds * frequency),
                1f);
            float wave = 0.5f +
                (Mathf.Sin(pulsePhase * Mathf.PI * 2f) * 0.5f);
            ApplyColor(Color.Lerp(normalColor, telegraphColor, wave));
            instance.transform.localScale = baseScale *
                Mathf.Lerp(1f, scaleMultiplier, wave);
        }

        private void ApplyColor(Color color)
        {
            instanceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, color);
            instanceRenderer.SetPropertyBlock(propertyBlock);
            CurrentColor = color;
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.SelfDestructDefinition.VisualHeight);
        }
    }
}
