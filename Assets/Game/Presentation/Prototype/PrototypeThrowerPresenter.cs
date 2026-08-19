using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeThrowerPresenter : MonoBehaviour
    {
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
        private MaterialPropertyBlock propertyBlock;
        private int colorPropertyId;
        private Color normalColor;
        private Vector3 baseScale;
        private Vector3 movementStart;
        private Vector3 movementTarget;
        private float movementElapsed;
        private float movementDuration;
        private float pulsePhase;
        private float deathEndsAt;
        private bool isInterpolating;
        private bool isShowingDeath;

        public PrototypeGameSession Session => session;

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
            telegraphCells.Clear();
            IsInitialized = false;
        }

        private void Update()
        {
            if (isInterpolating && instance != null)
            {
                movementElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(movementElapsed / movementDuration);
                instance.transform.position = Vector3.LerpUnclamped(
                    movementStart,
                    movementTarget,
                    progress);
                if (progress >= 1f)
                {
                    instance.transform.position = movementTarget;
                    isInterpolating = false;
                }
            }
            if (!isShowingDeath && IsTelegraphVisible && !session.IsPaused)
            {
                pulsePhase = Mathf.Repeat(pulsePhase + (Time.deltaTime * pulseHz), 1f);
                float wave = 0.5f + (Mathf.Sin(pulsePhase * Mathf.PI * 2f) * 0.5f);
                ApplyColor(Color.Lerp(normalColor, telegraphColor, wave));
                instance.transform.localScale = baseScale * Mathf.Lerp(1f, 1.12f, wave);
            }
            if (isShowingDeath && instance != null && Time.unscaledTime >= deathEndsAt)
            {
                instance.SetActive(false);
                isShowingDeath = false;
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
            baseScale = instance.transform.localScale;
            InitializeColor();
            movementTarget = ToPresentationPosition(session.CurrentThrowerGridPosition);
            movementStart = movementTarget;
            instance.transform.position = movementTarget;
            instance.SetActive(session.IsThrowerAlive);

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
                movementStart = instance.transform.position;
                movementTarget = ToPresentationPosition(result.Movement.To);
                movementElapsed = 0f;
                movementDuration = Mathf.Max(
                    (float)result.MovementDuration.TotalSeconds,
                    Mathf.Epsilon);
                isInterpolating = true;
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
            isInterpolating = false;
            HideTelegraph();
            instance.transform.localScale = baseScale;
            ApplyColor(deathColor);
            deathEndsAt = Time.unscaledTime + session.ThrowerDefinition.DeathVisualSeconds;
            isShowingDeath = true;
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
