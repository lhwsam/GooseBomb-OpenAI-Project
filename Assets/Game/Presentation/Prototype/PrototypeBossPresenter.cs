using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBossPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Color telegraphColor = new Color(1f, 0.72f, 0.08f, 0.68f);

        [SerializeField]
        private Color executeColor = new Color(1f, 0.08f, 0.04f, 0.9f);

        [SerializeField]
        private Color recoveryColor = new Color(0.16f, 1f, 0.34f, 1f);

        [SerializeField]
        private Color phaseTwoColor = new Color(0.72f, 0.18f, 1f, 1f);

        [SerializeField]
        private Color lastStandColor = new Color(1f, 0.12f, 0.42f, 1f);

        [SerializeField]
        private Color deathColor = new Color(0.12f, 0.01f, 0.02f, 1f);

        [SerializeField]
        private Color moveTargetColor = new Color(0.12f, 0.86f, 1f, 1f);

        private readonly List<GameObject> _dangerCellInstances = new List<GameObject>();
        private readonly List<Renderer> _dangerCellRenderers = new List<Renderer>();
        private readonly Queue<GridPosition> _movementTargets = new Queue<GridPosition>();
        private GameObject _bossInstance;
        private Renderer _bossRenderer;
        private GameObject _moveTargetInstance;
        private Renderer _moveTargetRenderer;
        private MaterialPropertyBlock _bossPropertyBlock;
        private MaterialPropertyBlock _moveTargetPropertyBlock;
        private MaterialPropertyBlock _dangerCellPropertyBlock;
        private int _bossColorPropertyId;
        private int _moveTargetColorPropertyId;
        private Color _baseBossColor;
        private Vector3 _moveVisualFrom;
        private Vector3 _moveVisualTo;
        private float _moveVisualElapsed;
        private float _moveVisualDuration;
        private float _deathEndsAt;
        private bool _isMoving;
        private bool _isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject BossInstance => _bossInstance;

        public int DangerCellPoolCount => _dangerCellInstances.Count;

        public int VisibleDangerCellCount { get; private set; }

        public int PatternTransitionCount { get; private set; }

        public int DamageCount { get; private set; }

        public int MovementCount { get; private set; }

        public int DeathCount { get; private set; }

        public int DisplayedHealth { get; private set; }

        public BossBattleState CurrentState { get; private set; }

        public BossPhase CurrentPhase { get; private set; }

        public BossPatternKind CurrentPattern { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsBossVisible => _bossInstance != null && _bossInstance.activeSelf;

        public bool IsMoveTargetVisible =>
            _moveTargetInstance != null && _moveTargetInstance.activeSelf;

        public GridPosition DisplayedBossPosition { get; private set; }

        public GridPosition DisplayedMoveTarget { get; private set; }

        public void Configure(PrototypeGameSession gameSession, Transform visualRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBossPresenter before changing its runtime configuration.");
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
                    "PrototypeBossPresenter requires session and presentation-root references.");
            }

            session.BossPatternTransitioned += OnBossPatternTransitioned;
            session.BossMoved += OnBossMoved;
            session.BossDamaged += OnBossDamaged;
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
                session.BossPatternTransitioned -= OnBossPatternTransitioned;
                session.BossMoved -= OnBossMoved;
                session.BossDamaged -= OnBossDamaged;
                session.Ready -= OnSessionReady;
            }
            if (_bossInstance != null)
            {
                Destroy(_bossInstance);
            }
            if (_moveTargetInstance != null)
            {
                Destroy(_moveTargetInstance);
            }
            for (int index = 0; index < _dangerCellInstances.Count; index++)
            {
                if (_dangerCellInstances[index] != null)
                {
                    Destroy(_dangerCellInstances[index]);
                }
            }

            _bossInstance = null;
            _bossRenderer = null;
            _moveTargetInstance = null;
            _moveTargetRenderer = null;
            _dangerCellInstances.Clear();
            _dangerCellRenderers.Clear();
            VisibleDangerCellCount = 0;
            IsInitialized = false;
            _isMoving = false;
            _isShowingDeath = false;
            _movementTargets.Clear();
        }

        private void Update()
        {
            if (_isMoving && _bossInstance != null && !session.IsPaused)
            {
                _moveVisualElapsed = Mathf.Min(
                    _moveVisualElapsed + Time.unscaledDeltaTime,
                    _moveVisualDuration);
                float t = _moveVisualDuration > 0f
                    ? _moveVisualElapsed / _moveVisualDuration
                    : 1f;
                _bossInstance.transform.position = Vector3.Lerp(
                    _moveVisualFrom,
                    _moveVisualTo,
                    t);
                _isMoving = t < 1f;
                if (!_isMoving)
                {
                    StartNextMovementSegment();
                }
            }
            if (_isShowingDeath && _bossInstance != null &&
                Time.unscaledTime >= _deathEndsAt)
            {
                _bossInstance.SetActive(false);
                _isShowingDeath = false;
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
            CurrentState = BossBattleState.Telegraph;
            CurrentPhase = BossPhase.One;
            CurrentPattern = BossPatternKind.LimitedChase;
            if (!session.HasBoss)
            {
                IsInitialized = true;
                return;
            }

            PrototypeBossDefinitionAsset definition = session.BossDefinition;
            definition.ValidatePresentationReferences();
            _bossInstance = Instantiate(definition.BossPrefab, presentationRoot);
            _bossInstance.name = "PrototypeBossVisual";
            _bossInstance.transform.position =
                session.GridSpace.GridToWorld(session.CurrentBossGridPosition) +
                (Vector3.up * definition.VisualHeight);
            _bossRenderer = _bossInstance.GetComponentInChildren<Renderer>(true);
            _bossColorPropertyId = ResolveColorProperty(_bossRenderer, "Boss");
            _bossPropertyBlock = new MaterialPropertyBlock();
            _moveTargetPropertyBlock = new MaterialPropertyBlock();
            _dangerCellPropertyBlock = new MaterialPropertyBlock();
            _baseBossColor = _bossRenderer.sharedMaterial.GetColor(_bossColorPropertyId);
            DisplayedHealth = session.CurrentBossHealth;
            DisplayedBossPosition = session.CurrentBossGridPosition;
            CurrentState = session.CurrentBossState;
            CurrentPhase = session.CurrentBossPhase;
            CurrentPattern = session.CurrentBossPattern;
            ApplyBossState(CurrentState, CurrentPhase, CurrentPattern);
            ApplyDangerCells(CurrentState, session.CurrentBossDangerCells);
            ApplyMoveTarget(CurrentState, session.NextBossGridPosition);
            _bossInstance.SetActive(session.IsBossAlive);
            IsInitialized = true;
        }

        private void OnBossMoved(EnemyMovementStep step)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (!session.HasBoss || step.ActorId != session.BossActorId)
            {
                throw new InvalidOperationException(
                    "Prototype boss presenter received another actor's movement.");
            }

            MovementCount++;
            DisplayedBossPosition = step.To;
        }

        private void OnBossPatternTransitioned(BossPatternTransition transition)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (!session.HasBoss || transition.ActorId != session.BossActorId)
            {
                throw new InvalidOperationException(
                    "Prototype boss presenter received another actor's transition.");
            }

            PatternTransitionCount++;
            CurrentState = transition.State;
            CurrentPhase = transition.Phase;
            CurrentPattern = transition.Pattern;
            DisplayedBossPosition = transition.BossPosition;
            if (transition.Movements.Count > 0)
            {
                _movementTargets.Clear();
                for (int index = 0; index < transition.Movements.Count; index++)
                {
                    _movementTargets.Enqueue(transition.Movements[index].To);
                }
                _moveVisualDuration = Mathf.Max(
                    session.BossDefinition.GetPatternExecuteSeconds(
                        transition.Phase,
                        transition.Pattern) / transition.Movements.Count,
                    Mathf.Epsilon);
                StartNextMovementSegment();
            }
            ApplyBossState(CurrentState, CurrentPhase, CurrentPattern);
            ApplyDangerCells(CurrentState, transition.DangerCells);
            ApplyMoveTarget(CurrentState, transition.NextBossPosition);
        }

        private void OnBossDamaged(BossDamageResult result)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (!session.HasBoss || result.ActorId != session.BossActorId)
            {
                throw new InvalidOperationException(
                    "Prototype boss presenter received another actor's damage.");
            }

            DamageCount++;
            DisplayedHealth = result.CurrentHealth;
            if (!result.WasFatal)
            {
                return;
            }

            DeathCount++;
            CurrentState = BossBattleState.Defeated;
            _isMoving = false;
            ApplyDangerCells(CurrentState, Array.Empty<GridPosition>());
            ApplyMoveTarget(CurrentState, default);
            ApplyBossColor(deathColor);
            _deathEndsAt = Time.unscaledTime + session.BossDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void ApplyBossState(
            BossBattleState state,
            BossPhase phase,
            BossPatternKind pattern)
        {
            switch (state)
            {
                case BossBattleState.Telegraph:
                    ApplyBossColor(GetPhaseColor(phase));
                    break;
                case BossBattleState.Execute:
                    ApplyBossColor(executeColor);
                    break;
                case BossBattleState.Recovery:
                    ApplyBossColor(
                        pattern == BossPatternKind.Overheat
                            ? recoveryColor
                            : GetPhaseColor(phase));
                    break;
                case BossBattleState.Defeated:
                    ApplyBossColor(deathColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private Color GetPhaseColor(BossPhase phase)
        {
            return phase == BossPhase.LastStand
                ? lastStandColor
                : phase == BossPhase.Two
                    ? phaseTwoColor
                    : _baseBossColor;
        }

        private void ApplyDangerCells(
            BossBattleState state,
            IReadOnlyList<GridPosition> dangerCells)
        {
            int visibleCount = state == BossBattleState.Telegraph ||
                state == BossBattleState.Execute
                ? dangerCells.Count
                : 0;
            EnsureDangerCellPool(visibleCount);
            Color color = state == BossBattleState.Execute
                ? executeColor
                : telegraphColor;
            for (int index = 0; index < _dangerCellInstances.Count; index++)
            {
                bool visible = index < visibleCount;
                GameObject instance = _dangerCellInstances[index];
                instance.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                instance.transform.position =
                    session.GridSpace.GridToWorld(dangerCells[index]) +
                    (Vector3.up * session.BossDefinition.DangerCellVisualHeight);
                ApplyDangerCellColor(_dangerCellRenderers[index], color);
            }
            VisibleDangerCellCount = visibleCount;
        }

        private void ApplyMoveTarget(
            BossBattleState state,
            GridPosition target)
        {
            if (_moveTargetInstance == null)
            {
                return;
            }

            bool visible = false;
            _moveTargetInstance.SetActive(visible);
            if (!visible)
            {
                return;
            }

            DisplayedMoveTarget = target;
            _moveTargetInstance.transform.position =
                session.GridSpace.GridToWorld(target) +
                (Vector3.up * session.BossDefinition.VisualHeight);
        }

        private void EnsureDangerCellPool(int requiredCount)
        {
            while (_dangerCellInstances.Count < requiredCount)
            {
                GameObject instance = Instantiate(
                    session.BossDefinition.DangerCellPrefab,
                    presentationRoot);
                instance.name = "PrototypeBossDangerCell_" + _dangerCellInstances.Count;
                Renderer renderer = instance.GetComponentInChildren<Renderer>(true);
                ResolveColorProperty(renderer, "Boss danger-cell");
                _dangerCellInstances.Add(instance);
                _dangerCellRenderers.Add(renderer);
            }
        }

        private void StartNextMovementSegment()
        {
            if (_movementTargets.Count == 0 || _bossInstance == null)
            {
                _isMoving = false;
                return;
            }

            GridPosition target = _movementTargets.Dequeue();
            _moveVisualFrom = _bossInstance.transform.position;
            _moveVisualTo = session.GridSpace.GridToWorld(target) +
                (Vector3.up * session.BossDefinition.VisualHeight);
            _moveVisualElapsed = 0f;
            _isMoving = true;
        }

        private void ApplyBossColor(Color color)
        {
            _bossRenderer.GetPropertyBlock(_bossPropertyBlock);
            _bossPropertyBlock.SetColor(_bossColorPropertyId, color);
            _bossRenderer.SetPropertyBlock(_bossPropertyBlock);
        }

        private void ApplyMoveTargetColor()
        {
            _moveTargetRenderer.GetPropertyBlock(_moveTargetPropertyBlock);
            _moveTargetPropertyBlock.SetColor(
                _moveTargetColorPropertyId,
                moveTargetColor);
            _moveTargetRenderer.SetPropertyBlock(_moveTargetPropertyBlock);
        }

        private void ApplyDangerCellColor(Renderer renderer, Color color)
        {
            int propertyId = ResolveColorProperty(renderer, "Boss danger-cell");
            _dangerCellPropertyBlock.Clear();
            renderer.GetPropertyBlock(_dangerCellPropertyBlock);
            _dangerCellPropertyBlock.SetColor(propertyId, color);
            renderer.SetPropertyBlock(_dangerCellPropertyBlock);
        }

        private static int ResolveColorProperty(Renderer renderer, string label)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                throw new InvalidOperationException($"{label} visual requires a material renderer.");
            }
            if (renderer.sharedMaterial.HasProperty(BaseColorId))
            {
                return BaseColorId;
            }
            if (renderer.sharedMaterial.HasProperty(ColorId))
            {
                return ColorId;
            }
            throw new InvalidOperationException(
                $"{label} material requires a supported color property.");
        }
    }
}
