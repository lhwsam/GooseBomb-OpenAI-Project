using System;
using System.Collections.Generic;
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
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Color telegraphColor = new Color(1f, 0.12f, 0.85f, 1f);

        [SerializeField]
        private Color deathColor = new Color(0.12f, 0.01f, 0.1f, 1f);

        [SerializeField]
        private float pulseHz = 6f;

        private GameObject instance;
        private readonly List<GameObject> telegraphCells = new List<GameObject>(3);
        private Renderer instanceRenderer;
        private Animator animator;
        private MaterialPropertyBlock propertyBlock;
        private int colorPropertyId;
        private Color normalColor;
        private Vector3 baseScale;
        private float pulsePhase;
        private float deathRemaining;
        private bool isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject Instance => instance;

        public bool IsInitialized { get; private set; }

        public bool IsEnemyVisible => instance != null && instance.activeSelf;

        public bool IsTelegraphVisible => ActiveTelegraphCellCount > 0;

        public int ActiveTelegraphCellCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < telegraphCells.Count; index++)
                {
                    if (telegraphCells[index] != null && telegraphCells[index].activeSelf)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public int MoveCount { get; private set; }

        public int TelegraphCount { get; private set; }

        public int DeathCount { get; private set; }

        public Animator Animator => animator;

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
            if (animator != null)
            {
                animator.speed = 1f;
            }
            animator = null;
            telegraphCells.Clear();
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
                instance.transform.position = PrototypeEnemyMovementSampler.Sample(
                    session.CurrentThrowerMovementTransition,
                    session.CurrentGameTime,
                    session.GridSpace,
                    session.ThrowerDefinition.VisualHeight,
                    session.CurrentThrowerGridPosition);
            }
            SyncLocomotionAnimation();
            if (!isShowingDeath && IsTelegraphVisible && !session.IsPaused)
            {
                pulsePhase = Mathf.Repeat(pulsePhase + (Time.deltaTime * pulseHz), 1f);
                float wave = 0.5f + (Mathf.Sin(pulsePhase * Mathf.PI * 2f) * 0.5f);
                ApplyColor(Color.Lerp(normalColor, telegraphColor, wave));
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
                session.CurrentThrowerGridPosition);
            instance.SetActive(session.IsThrowerAlive);

            ApplyAnimationState(session.CurrentThrowerState);

            EnsureTelegraphCapacity(definition.BombsPerVolley);
            HideTelegraph();
            if (session.CurrentThrowerState == ThrowerEnemyState.Telegraph)
            {
                ShowTelegraphs(session.CurrentThrowerLockedTargets);
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
                ShowTelegraphs(result.LockedTargets);
            }
            else
            {
                HideTelegraph();
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
            if (animator != null)
            {
                animator.SetBool(IsMovingParameterId, false);
                animator.ResetTrigger(ThrowParameterId);
                animator.ResetTrigger(RecoverParameterId);
                animator.SetTrigger(DieParameterId);
            }
            HideTelegraph();
            instance.transform.localScale = baseScale;
            ApplyColor(deathColor);
            deathRemaining = session.ThrowerDefinition.DeathVisualSeconds;
            isShowingDeath = true;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (animator != null)
            {
                animator.speed = isPaused ? 0f : 1f;
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
                (PrototypeEnemyMovementSampler.IsActive(
                        session.CurrentThrowerMovementTransition,
                        session.CurrentGameTime) ||
                    session.CurrentThrowerLocomotionState == EnemyLocomotionState.Moving));
        }

        private void ShowTelegraphs(IReadOnlyList<GridPosition> targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }
            EnsureTelegraphCapacity(targets.Count);
            for (int index = 0; index < telegraphCells.Count; index++)
            {
                GameObject cell = telegraphCells[index];
                bool shouldShow = index < targets.Count;
                if (shouldShow)
                {
                    cell.transform.position = session.GridSpace.GridToWorld(targets[index]) +
                        (Vector3.up * 0.03f);
                }
                cell.SetActive(shouldShow);
            }
            pulsePhase = 0f;
        }

        private void HideTelegraph()
        {
            for (int index = 0; index < telegraphCells.Count; index++)
            {
                if (telegraphCells[index] != null)
                {
                    telegraphCells[index].SetActive(false);
                }
            }
            if (instance != null)
            {
                instance.transform.localScale = baseScale;
                ApplyColor(normalColor);
            }
        }

        private void EnsureTelegraphCapacity(int count)
        {
            PrototypeThrowerDefinitionAsset definition = session.ThrowerDefinition;
            while (telegraphCells.Count < count)
            {
                GameObject cell = Instantiate(
                    definition.TelegraphCellPrefab,
                    presentationRoot);
                cell.name = "PrototypeThrowerTargetTelegraph" + telegraphCells.Count;
                cell.SetActive(false);
                telegraphCells.Add(cell);
            }
        }

        private void InitializeColor()
        {
            propertyBlock = new MaterialPropertyBlock();
            Material material = instanceRenderer.sharedMaterial;
            if (material == null)
            {
                throw new InvalidOperationException(
                    "Thrower enemy prefab renderer requires a material.");
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
                    "Thrower enemy material requires a supported color property.");
            }
            normalColor = material.GetColor(colorPropertyId);
        }

        private void ApplyColor(Color color)
        {
            instanceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, color);
            instanceRenderer.SetPropertyBlock(propertyBlock);
        }

        private Vector3 ToPresentationPosition(GridPosition position)
        {
            return session.GridSpace.GridToWorld(position) +
                (Vector3.up * session.ThrowerDefinition.VisualHeight);
        }
    }
}
