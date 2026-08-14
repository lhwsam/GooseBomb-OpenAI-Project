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
        private Color deathColor = new Color(0.12f, 0.01f, 0.02f, 1f);

        private readonly List<GameObject> _dangerCellInstances = new List<GameObject>();
        private readonly List<Renderer> _dangerCellRenderers = new List<Renderer>();
        private GameObject _bossInstance;
        private Renderer _bossRenderer;
        private MaterialPropertyBlock _bossPropertyBlock;
        private MaterialPropertyBlock _dangerCellPropertyBlock;
        private int _bossColorPropertyId;
        private Color _baseBossColor;
        private float _deathEndsAt;
        private bool _isShowingDeath;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public GameObject BossInstance => _bossInstance;

        public int DangerCellPoolCount => _dangerCellInstances.Count;

        public int VisibleDangerCellCount { get; private set; }

        public int PatternTransitionCount { get; private set; }

        public int DamageCount { get; private set; }

        public int DeathCount { get; private set; }

        public int DisplayedHealth { get; private set; }

        public BossBattleState CurrentState { get; private set; }

        public BossPhase CurrentPhase { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsBossVisible => _bossInstance != null && _bossInstance.activeSelf;

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
                session.BossDamaged -= OnBossDamaged;
                session.Ready -= OnSessionReady;
            }
            if (_bossInstance != null)
            {
                Destroy(_bossInstance);
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
            _dangerCellInstances.Clear();
            _dangerCellRenderers.Clear();
            VisibleDangerCellCount = 0;
            IsInitialized = false;
            _isShowingDeath = false;
        }

        private void Update()
        {
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
            _dangerCellPropertyBlock = new MaterialPropertyBlock();
            _baseBossColor = _bossRenderer.sharedMaterial.GetColor(_bossColorPropertyId);

            DisplayedHealth = session.CurrentBossHealth;
            CurrentState = session.CurrentBossState;
            CurrentPhase = session.CurrentBossPhase;
            ApplyBossState(CurrentState, CurrentPhase);
            ApplyDangerCells(CurrentState, session.CurrentBossDangerCells);
            _bossInstance.SetActive(session.IsBossAlive);
            IsInitialized = true;
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
            ApplyBossState(CurrentState, CurrentPhase);
            ApplyDangerCells(CurrentState, transition.DangerCells);
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
            ApplyDangerCells(CurrentState, Array.Empty<GridPosition>());
            ApplyBossColor(deathColor);
            _deathEndsAt = Time.unscaledTime + session.BossDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void ApplyBossState(BossBattleState state, BossPhase phase)
        {
            switch (state)
            {
                case BossBattleState.Telegraph:
                    ApplyBossColor(phase == BossPhase.Two ? phaseTwoColor : _baseBossColor);
                    break;
                case BossBattleState.Execute:
                    ApplyBossColor(executeColor);
                    break;
                case BossBattleState.Recovery:
                    ApplyBossColor(recoveryColor);
                    break;
                case BossBattleState.Defeated:
                    ApplyBossColor(deathColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
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

        private void ApplyBossColor(Color color)
        {
            _bossRenderer.GetPropertyBlock(_bossPropertyBlock);
            _bossPropertyBlock.SetColor(_bossColorPropertyId, color);
            _bossRenderer.SetPropertyBlock(_bossPropertyBlock);
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
