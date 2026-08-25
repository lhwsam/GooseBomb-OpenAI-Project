using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBossPresenter : MonoBehaviour
    {
        public const float DefaultChargeShakeAmplitude = 0.2f;
        public const float DefaultChargeShakeDuration = 0.22f;
        public const float DefaultChargeShakeFrequency = 22f;
        public const float DefaultParityShakeAmplitude = 0.11f;
        public const float DefaultParityShakeDuration = 0.13f;
        public const float DefaultParityShakeFrequency = 26f;
        public const float DefaultBossBombShakeAmplitude = 0.13f;
        public const float DefaultBossBombShakeDuration = 0.16f;
        public const float DefaultBossBombShakeFrequency = 24f;

        private static readonly int AliveParameterId = Animator.StringToHash("Alive");
        private static readonly int IsMovingParameterId = Animator.StringToHash("IsMoving");
        private static readonly int TelegraphParameterId = Animator.StringToHash("Telegraph");
        private static readonly int ChargeParameterId = Animator.StringToHash("Charge");
        private static readonly int SummonParameterId = Animator.StringToHash("Summon");
        private static readonly int ThrowLeftParameterId = Animator.StringToHash("ThrowLeft");
        private static readonly int ThrowRightParameterId = Animator.StringToHash("ThrowRight");
        private static readonly int DieParameterId = Animator.StringToHash("Die");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly IReadOnlyList<GridPosition> NoDangerCells =
            Array.Empty<GridPosition>();

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private PrototypeUserSettingsRuntime attackFeedbackSettings;

        [SerializeField]
        private PrototypeCameraShake attackCameraShake;

        [SerializeField]
        private PrototypeLocalVfxOverrides localVfxOverrides;

        [SerializeField]
        [Min(0f)]
        private float chargeShakeAmplitude = DefaultChargeShakeAmplitude;

        [SerializeField]
        [Min(0f)]
        private float chargeShakeDuration = DefaultChargeShakeDuration;

        [SerializeField]
        [Min(0f)]
        private float chargeShakeFrequency = DefaultChargeShakeFrequency;

        [SerializeField]
        [Min(0f)]
        private float parityShakeAmplitude = DefaultParityShakeAmplitude;

        [SerializeField]
        [Min(0f)]
        private float parityShakeDuration = DefaultParityShakeDuration;

        [SerializeField]
        [Min(0f)]
        private float parityShakeFrequency = DefaultParityShakeFrequency;

        [SerializeField]
        [Min(0f)]
        private float bossBombShakeAmplitude = DefaultBossBombShakeAmplitude;

        [SerializeField]
        [Min(0f)]
        private float bossBombShakeDuration = DefaultBossBombShakeDuration;

        [SerializeField]
        [Min(0f)]
        private float bossBombShakeFrequency = DefaultBossBombShakeFrequency;

        [SerializeField]
        private Color telegraphColor = new Color(1f, 0.72f, 0.08f, 0.68f);

        [SerializeField]
        private Color executeColor = new Color(1f, 0.08f, 0.04f, 0.9f);

        [SerializeField]
        private Color moveTargetColor = new Color(0.12f, 0.86f, 1f, 1f);

        private readonly List<GameObject> _dangerCellInstances = new List<GameObject>();
        private readonly List<Renderer> _dangerCellRenderers = new List<Renderer>();
        private readonly List<GridPosition> _visibleDangerCells =
            new List<GridPosition>();
        private readonly HashSet<GridPosition> _visibleDangerCellSet =
            new HashSet<GridPosition>();
        private readonly HashSet<GridPosition> _executingPatternDangerCells =
            new HashSet<GridPosition>();
        private readonly List<GameObject> _parityLightningInstances =
            new List<GameObject>();
        private readonly List<ParticleSystem[]> _parityLightningSystems =
            new List<ParticleSystem[]>();
        private readonly List<float> _parityLightningRemaining =
            new List<float>();
        private readonly List<float> _parityLightningLifetimes =
            new List<float>();
        private readonly Queue<GridPosition> _movementTargets = new Queue<GridPosition>();
        private GameObject _bossInstance;
        private Animator _animator;
        private PrototypeHologramFeedback _hologramFeedback;
        private PrototypeLocalHologramOverrides _localHologramOverrides;
        private GameObject _moveTargetInstance;
        private Renderer _moveTargetRenderer;
        private MaterialPropertyBlock _moveTargetPropertyBlock;
        private MaterialPropertyBlock _dangerCellPropertyBlock;
        private int _moveTargetColorPropertyId;
        private Vector3 _moveVisualFrom;
        private Vector3 _moveVisualTo;
        private float _moveVisualElapsed;
        private float _moveVisualDuration;
        private float _deathRemaining;
        private bool _isMoving;
        private bool _isShowingDeath;
        private bool _isBossClearPresentationActive;
        private bool _isParityWaveTelegraphActive;
        private bool _isIntroPrepared;
        private bool _isIntroLanded;
        private Vector3 _introStartWorldPosition;
        private Vector3 _introLandingWorldPosition;
        private GameObject _parityLightningPrefab;
        private IReadOnlyList<GridPosition> _currentPatternDangerCells =
            NoDangerCells;
        private BossBattleState _currentDangerState;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public PrototypeUserSettingsRuntime AttackFeedbackSettings =>
            attackFeedbackSettings;

        public PrototypeCameraShake AttackCameraShake => attackCameraShake;

        public GameObject BossInstance => _bossInstance;

        public int DangerCellPoolCount => _dangerCellInstances.Count;

        public int VisibleDangerCellCount { get; private set; }

        public bool UsesHologramDangerCells { get; private set; }

        public int PatternTransitionCount { get; private set; }

        public int DamageCount { get; private set; }

        public int MovementCount { get; private set; }

        public int DeathCount { get; private set; }

        public int ThrowAnimationCount { get; private set; }

        public int ChargeAttackFeedbackCount { get; private set; }

        public int ParityAttackFeedbackCount { get; private set; }

        public int ParityLightningVfxPlayCount { get; private set; }

        public int BossBombExplosionFeedbackCount { get; private set; }

        public int AttackShakePlayCount { get; private set; }

        public bool LastThrowWasLeft { get; private set; }

        public Animator Animator => _animator;

        public PrototypeHologramFeedback HologramFeedback => _hologramFeedback;

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

        public bool IsIntroPrepared => _isIntroPrepared;

        public bool IsIntroLanded => _isIntroLanded;

        public bool IsBossClearPresentationActive =>
            _isBossClearPresentationActive;

        public Vector3 IntroStartWorldPosition => _introStartWorldPosition;

        public Vector3 IntroLandingWorldPosition => _introLandingWorldPosition;

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

        public void ConfigureAttackFeedback(
            PrototypeUserSettingsRuntime settingsRuntime,
            PrototypeCameraShake cameraShake,
            PrototypeLocalVfxOverrides authoredLocalVfxOverrides = null)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBossPresenter before changing its attack feedback configuration.");
            }

            attackFeedbackSettings = settingsRuntime ??
                throw new ArgumentNullException(nameof(settingsRuntime));
            attackCameraShake = cameraShake ??
                throw new ArgumentNullException(nameof(cameraShake));
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
                    "PrototypeBossPresenter requires session and presentation-root references.");
            }

            session.BossPatternTransitioned += OnBossPatternTransitioned;
            session.BossMoved += OnBossMoved;
            session.BossDamaged += OnBossDamaged;
            session.BossBombLaunched += OnBossBombLaunched;
            session.BombExploded += OnBombExploded;
            session.PauseStateChanged += OnPauseStateChanged;
            session.BossCombatStarted += OnBossCombatStarted;
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
                session.BossBombLaunched -= OnBossBombLaunched;
                session.BombExploded -= OnBombExploded;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.BossCombatStarted -= OnBossCombatStarted;
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
            DestroyParityLightningPool();
            if (attackCameraShake != null)
            {
                attackCameraShake.Stop();
            }

            _bossInstance = null;
            _hologramFeedback = null;
            _localHologramOverrides = null;
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
            _animator = null;
            _moveTargetInstance = null;
            _moveTargetRenderer = null;
            _dangerCellInstances.Clear();
            _dangerCellRenderers.Clear();
            _visibleDangerCells.Clear();
            _visibleDangerCellSet.Clear();
            _executingPatternDangerCells.Clear();
            _currentPatternDangerCells = NoDangerCells;
            VisibleDangerCellCount = 0;
            UsesHologramDangerCells = false;
            IsInitialized = false;
            _isMoving = false;
            _isShowingDeath = false;
            _isBossClearPresentationActive = false;
            _isParityWaveTelegraphActive = false;
            _isIntroPrepared = false;
            _isIntroLanded = false;
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
            if (_isShowingDeath && _bossInstance != null && !session.IsPaused)
            {
                _deathRemaining -= Time.unscaledDeltaTime;
                if (_deathRemaining <= 0f)
                {
                    _bossInstance.SetActive(false);
                    _isShowingDeath = false;
                }
            }
            UpdateParityLightningVfx();
        }

        private void OnValidate()
        {
            chargeShakeAmplitude = Mathf.Max(0f, chargeShakeAmplitude);
            chargeShakeDuration = Mathf.Max(0f, chargeShakeDuration);
            chargeShakeFrequency = Mathf.Max(0f, chargeShakeFrequency);
            parityShakeAmplitude = Mathf.Max(0f, parityShakeAmplitude);
            parityShakeDuration = Mathf.Max(0f, parityShakeDuration);
            parityShakeFrequency = Mathf.Max(0f, parityShakeFrequency);
            bossBombShakeAmplitude = Mathf.Max(0f, bossBombShakeAmplitude);
            bossBombShakeDuration = Mathf.Max(0f, bossBombShakeDuration);
            bossBombShakeFrequency = Mathf.Max(0f, bossBombShakeFrequency);
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

            localVfxOverrides ??= PrototypeLocalVfxOverrides.LoadOptional();
            _localHologramOverrides ??=
                PrototypeLocalHologramOverrides.LoadOptional();

            PrototypeBossDefinitionAsset definition = session.BossDefinition;
            definition.ValidatePresentationReferences();
            _bossInstance = Instantiate(definition.BossPrefab, presentationRoot);
            _bossInstance.name = "PrototypeBossVisual";
            _hologramFeedback =
                PrototypeHologramFeedback.CreateHitFeedback(_bossInstance);
            if (_hologramFeedback != null)
            {
                _hologramFeedback.SetPaused(session.IsPaused);
            }
            _bossInstance.transform.position =
                session.GridSpace.GridToWorld(session.CurrentBossGridPosition) +
                (Vector3.up * definition.VisualHeight);
            _animator = _bossInstance.GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _animator.speed = session.IsPaused ? 0f : 1f;
                _animator.SetBool(AliveParameterId, session.IsBossAlive);
                _animator.SetBool(IsMovingParameterId, false);
            }
            _moveTargetPropertyBlock = new MaterialPropertyBlock();
            _dangerCellPropertyBlock = new MaterialPropertyBlock();
            DisplayedHealth = session.CurrentBossHealth;
            DisplayedBossPosition = session.CurrentBossGridPosition;
            CurrentState = session.CurrentBossState;
            CurrentPhase = session.CurrentBossPhase;
            CurrentPattern = session.CurrentBossPattern;
            if (session.IsBossIntroPending)
            {
                ApplyDangerCells(BossBattleState.Recovery, Array.Empty<GridPosition>());
                ApplyMoveTarget(BossBattleState.Recovery, default);
                _bossInstance.SetActive(false);
            }
            else
            {
                ApplyBossAnimation(CurrentState, CurrentPattern);
                ApplyDangerCells(CurrentState, session.CurrentBossDangerCells);
                ApplyMoveTarget(CurrentState, session.NextBossGridPosition);
                _bossInstance.SetActive(session.IsBossAlive);
            }
            IsInitialized = true;
        }

        public void PrepareBossIntro(float dropHeight)
        {
            if (dropHeight <= 0f || float.IsNaN(dropHeight) ||
                float.IsInfinity(dropHeight))
            {
                throw new ArgumentOutOfRangeException(nameof(dropHeight));
            }
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (!session.HasBoss || !session.IsBossIntroPending || _bossInstance == null)
            {
                throw new InvalidOperationException(
                    "Boss intro preparation requires a pending boss encounter.");
            }

            _introLandingWorldPosition =
                session.GridSpace.GridToWorld(session.CurrentBossGridPosition) +
                (Vector3.up * session.BossDefinition.VisualHeight);
            _introStartWorldPosition =
                _introLandingWorldPosition + (Vector3.up * dropHeight);
            _bossInstance.transform.position = _introStartWorldPosition;
            _bossInstance.SetActive(false);
            ApplyDangerCells(BossBattleState.Recovery, Array.Empty<GridPosition>());
            ApplyMoveTarget(BossBattleState.Recovery, default);
            if (_animator != null)
            {
                _animator.SetBool(IsMovingParameterId, false);
                _animator.speed = 0f;
            }
            _isIntroPrepared = true;
            _isIntroLanded = false;
        }

        public void RevealBossForIntro()
        {
            if (!_isIntroPrepared || _bossInstance == null)
            {
                throw new InvalidOperationException(
                    "Prepare the boss intro before revealing its visual.");
            }

            _bossInstance.SetActive(session.IsBossAlive);
        }

        public void SetBossIntroDropProgress(float progress)
        {
            if (!_isIntroPrepared || _bossInstance == null)
            {
                throw new InvalidOperationException(
                    "Prepare the boss intro before moving its visual.");
            }

            _bossInstance.transform.position = Vector3.Lerp(
                _introStartWorldPosition,
                _introLandingWorldPosition,
                Mathf.Clamp01(progress));
        }

        public void CompleteBossIntroLanding()
        {
            if (!_isIntroPrepared || _bossInstance == null)
            {
                throw new InvalidOperationException(
                    "Prepare the boss intro before completing its landing.");
            }

            _bossInstance.transform.position = _introLandingWorldPosition;
            _bossInstance.SetActive(session.IsBossAlive);
            if (_animator != null)
            {
                _animator.SetBool(AliveParameterId, session.IsBossAlive);
                _animator.speed = session.IsPaused ? 0f : 1f;
            }
            _isIntroLanded = true;
        }

        public void BeginBossClearPresentation(float animatorPlaybackSpeed)
        {
            if (animatorPlaybackSpeed <= 0f || animatorPlaybackSpeed > 1f ||
                float.IsNaN(animatorPlaybackSpeed) ||
                float.IsInfinity(animatorPlaybackSpeed))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(animatorPlaybackSpeed),
                    animatorPlaybackSpeed,
                    "Boss-clear animator speed must be finite and in (0, 1].");
            }
            if (!IsInitialized || _bossInstance == null || session.IsBossAlive)
            {
                throw new InvalidOperationException(
                    "Boss-clear presentation requires an initialized defeated boss visual.");
            }

            _isMoving = false;
            _movementTargets.Clear();
            _isShowingDeath = false;
            _isBossClearPresentationActive = true;
            _bossInstance.SetActive(true);
            if (_animator != null)
            {
                _animator.speed = animatorPlaybackSpeed;
            }
        }

        public void CompleteBossClearPresentation()
        {
            if (!_isBossClearPresentationActive)
            {
                return;
            }

            _isBossClearPresentationActive = false;
            _isShowingDeath = false;
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
            if (_bossInstance != null)
            {
                _bossInstance.SetActive(false);
            }
        }

        public void CancelBossClearPresentation()
        {
            if (!_isBossClearPresentationActive)
            {
                return;
            }

            _isBossClearPresentationActive = false;
            if (_animator != null)
            {
                _animator.speed = session != null && session.IsPaused ? 0f : 1f;
            }
            if (_bossInstance != null && session != null && !session.IsBossAlive)
            {
                _bossInstance.SetActive(false);
            }
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
            ApplyBossAnimation(CurrentState, CurrentPattern);
            ApplyDangerCells(CurrentState, transition.DangerCells);
            ApplyMoveTarget(CurrentState, transition.NextBossPosition);
            ApplyAttackFeedback(transition);
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
            if (_hologramFeedback != null)
            {
                _hologramFeedback.TriggerHitBlink();
            }
            if (!result.WasFatal)
            {
                return;
            }

            DeathCount++;
            CurrentState = BossBattleState.Defeated;
            _isMoving = false;
            if (_animator != null)
            {
                _animator.SetBool(IsMovingParameterId, false);
                _animator.SetBool(AliveParameterId, false);
                ResetLivingAnimationTriggers();
                _animator.SetTrigger(DieParameterId);
            }
            ApplyDangerCells(CurrentState, Array.Empty<GridPosition>());
            ApplyMoveTarget(CurrentState, default);
            _deathRemaining = session.BossDefinition.DeathVisualSeconds;
            _isShowingDeath = true;
        }

        private void OnBossBombLaunched(BossBombFlight flight)
        {
            if (!IsInitialized)
            {
                InitializePresentation();
            }
            if (_animator == null || _isShowingDeath)
            {
                return;
            }

            bool useLeft = flight.Sequence % 2 == 0;
            LastThrowWasLeft = useLeft;
            ThrowAnimationCount++;
            _animator.ResetTrigger(
                useLeft ? ThrowRightParameterId : ThrowLeftParameterId);
            _animator.SetTrigger(
                useLeft ? ThrowLeftParameterId : ThrowRightParameterId);
        }

        private void OnBombExploded(BombExplosion explosion)
        {
            if (!session.HasBoss || explosion.OwnerId != session.BossActorId)
            {
                return;
            }

            BossBombExplosionFeedbackCount++;
            RequestAttackShake(
                bossBombShakeAmplitude,
                bossBombShakeDuration,
                bossBombShakeFrequency);
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (_animator != null)
            {
                _animator.speed = isPaused ||
                    (_isIntroPrepared && !_isIntroLanded)
                        ? 0f
                        : 1f;
            }
            if (_hologramFeedback != null)
            {
                _hologramFeedback.SetPaused(isPaused);
            }
            SetParityLightningPaused(isPaused);
            if (isPaused && attackCameraShake != null)
            {
                attackCameraShake.Stop();
            }
        }

        private void OnBossCombatStarted()
        {
            if (!session.HasBoss)
            {
                return;
            }

            localVfxOverrides ??= PrototypeLocalVfxOverrides.LoadOptional();
            if (_isIntroPrepared && !_isIntroLanded)
            {
                CompleteBossIntroLanding();
            }

            if (_animator != null)
            {
                _animator.SetBool(AliveParameterId, session.IsBossAlive);
            }

            ApplyBossAnimation(
                session.CurrentBossState,
                session.CurrentBossPattern);
            ApplyDangerCells(
                session.CurrentBossState,
                session.CurrentBossDangerCells);
            ApplyMoveTarget(
                session.CurrentBossState,
                session.NextBossGridPosition);
            _isIntroPrepared = false;
        }

        private void ApplyAttackFeedback(BossPatternTransition transition)
        {
            if (transition.State != BossBattleState.Execute)
            {
                return;
            }

            switch (transition.Pattern)
            {
                case BossPatternKind.FixedCharge:
                    ChargeAttackFeedbackCount++;
                    RequestAttackShake(
                        chargeShakeAmplitude,
                        chargeShakeDuration,
                        chargeShakeFrequency);
                    break;
                case BossPatternKind.ParityWave:
                    ParityAttackFeedbackCount++;
                    PlayParityLightning(transition.DangerCells);
                    RequestAttackShake(
                        parityShakeAmplitude,
                        parityShakeDuration,
                        parityShakeFrequency);
                    break;
            }
        }

        private bool RequestAttackShake(
            float amplitude,
            float duration,
            float frequency)
        {
            if (attackFeedbackSettings == null || attackCameraShake == null)
            {
                return false;
            }

            float effectiveAmplitude =
                attackFeedbackSettings.ScaleScreenShake(amplitude);
            if (!attackCameraShake.Play(effectiveAmplitude, duration, frequency))
            {
                return false;
            }

            AttackShakePlayCount++;
            return true;
        }

        private void PlayParityLightning(
            IReadOnlyList<GridPosition> dangerCells)
        {
            GameObject prefab = localVfxOverrides != null
                ? localVfxOverrides.BossIntroLightningVfxPrefab
                : null;
            if (prefab == null || dangerCells == null || dangerCells.Count == 0)
            {
                return;
            }

            EnsureParityLightningPool(prefab, dangerCells.Count);
            for (int index = 0; index < dangerCells.Count; index++)
            {
                GameObject instance = _parityLightningInstances[index];
                instance.transform.position =
                    session.GridSpace.GridToWorld(dangerCells[index]);
                instance.SetActive(true);
                RestartParticleSystems(_parityLightningSystems[index]);
                _parityLightningRemaining[index] =
                    _parityLightningLifetimes[index] + 0.1f;
                ParityLightningVfxPlayCount++;
            }
        }

        private void EnsureParityLightningPool(GameObject prefab, int requiredCount)
        {
            if (_parityLightningPrefab != null && _parityLightningPrefab != prefab)
            {
                DestroyParityLightningPool();
            }
            _parityLightningPrefab = prefab;

            while (_parityLightningInstances.Count < requiredCount)
            {
                GameObject instance = Instantiate(prefab, presentationRoot, false);
                instance.name =
                    "PrototypeBossParityLightning_" +
                    _parityLightningInstances.Count;
                instance.SetActive(false);
                ParticleSystem[] systems =
                    instance.GetComponentsInChildren<ParticleSystem>(true);
                _parityLightningInstances.Add(instance);
                _parityLightningSystems.Add(systems);
                _parityLightningRemaining.Add(0f);
                _parityLightningLifetimes.Add(GetParticleLifetime(systems));
            }
        }

        private void UpdateParityLightningVfx()
        {
            if (session == null || session.IsPaused)
            {
                return;
            }

            for (int index = 0; index < _parityLightningInstances.Count; index++)
            {
                GameObject instance = _parityLightningInstances[index];
                if (instance == null || !instance.activeSelf)
                {
                    continue;
                }

                float remaining =
                    _parityLightningRemaining[index] - Time.unscaledDeltaTime;
                _parityLightningRemaining[index] = remaining;
                if (remaining <= 0f)
                {
                    instance.SetActive(false);
                }
            }
        }

        private void SetParityLightningPaused(bool isPaused)
        {
            for (int index = 0; index < _parityLightningInstances.Count; index++)
            {
                GameObject instance = _parityLightningInstances[index];
                if (instance == null || !instance.activeSelf)
                {
                    continue;
                }

                ParticleSystem[] systems = _parityLightningSystems[index];
                for (int systemIndex = 0;
                     systemIndex < systems.Length;
                     systemIndex++)
                {
                    if (isPaused)
                    {
                        systems[systemIndex].Pause(true);
                    }
                    else
                    {
                        systems[systemIndex].Play(true);
                    }
                }
            }
        }

        private void DestroyParityLightningPool()
        {
            for (int index = 0; index < _parityLightningInstances.Count; index++)
            {
                if (_parityLightningInstances[index] != null)
                {
                    Destroy(_parityLightningInstances[index]);
                }
            }
            _parityLightningInstances.Clear();
            _parityLightningSystems.Clear();
            _parityLightningRemaining.Clear();
            _parityLightningLifetimes.Clear();
            _parityLightningPrefab = null;
        }

        private static void RestartParticleSystems(ParticleSystem[] systems)
        {
            for (int index = 0; index < systems.Length; index++)
            {
                systems[index].Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[index].Play(true);
            }
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

        private void ApplyBossAnimation(BossBattleState state, BossPatternKind pattern)
        {
            if (_animator == null || state == BossBattleState.Defeated)
            {
                return;
            }

            switch (state)
            {
                case BossBattleState.Telegraph:
                {
                    _animator.SetBool(IsMovingParameterId, false);
                    ResetLivingAnimationTriggers();
                    bool shouldTriggerTelegraph = false;
                    if (pattern == BossPatternKind.ParityWave)
                    {
                        shouldTriggerTelegraph = !_isParityWaveTelegraphActive;
                        _isParityWaveTelegraphActive = true;
                    }
                    else
                    {
                        _isParityWaveTelegraphActive = false;
                    }
                    if (shouldTriggerTelegraph)
                    {
                        _animator.SetTrigger(TelegraphParameterId);
                    }
                    break;
                }
                case BossBattleState.Execute:
                    if (pattern == BossPatternKind.FixedCharge)
                    {
                        _animator.SetBool(IsMovingParameterId, false);
                        _animator.SetTrigger(ChargeParameterId);
                    }
                    else if (pattern == BossPatternKind.SummonSelfDestruct)
                    {
                        _animator.SetBool(IsMovingParameterId, false);
                        _animator.SetTrigger(SummonParameterId);
                    }
                    break;
                case BossBattleState.Recovery:
                    _animator.SetBool(IsMovingParameterId, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ResetLivingAnimationTriggers()
        {
            if (_animator == null)
            {
                return;
            }
            _animator.ResetTrigger(TelegraphParameterId);
            _animator.ResetTrigger(ChargeParameterId);
            _animator.ResetTrigger(SummonParameterId);
            _animator.ResetTrigger(ThrowLeftParameterId);
            _animator.ResetTrigger(ThrowRightParameterId);
        }

        private void ApplyDangerCells(
            BossBattleState state,
            IReadOnlyList<GridPosition> dangerCells)
        {
            _currentDangerState = state;
            _currentPatternDangerCells =
                ShouldShowPreImpactDangerCells(CurrentPattern) &&
                (state == BossBattleState.Telegraph ||
                 state == BossBattleState.Execute)
                    ? dangerCells
                    : NoDangerCells;
            RefreshDangerCells();
        }

        private void RefreshDangerCells()
        {
            _visibleDangerCells.Clear();
            _visibleDangerCellSet.Clear();
            _executingPatternDangerCells.Clear();

            AddUniqueDangerCells(_currentPatternDangerCells);
            if (_currentDangerState == BossBattleState.Execute)
            {
                for (int index = 0;
                     index < _currentPatternDangerCells.Count;
                     index++)
                {
                    _executingPatternDangerCells.Add(
                        _currentPatternDangerCells[index]);
                }
            }

            _visibleDangerCells.Sort(CompareGridPositions);
            int visibleCount = _visibleDangerCells.Count;
            EnsureDangerCellPool(visibleCount);
            for (int index = 0; index < _dangerCellInstances.Count; index++)
            {
                bool visible = index < visibleCount;
                GameObject instance = _dangerCellInstances[index];
                instance.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                GridPosition position = _visibleDangerCells[index];
                instance.transform.position =
                    session.GridSpace.GridToWorld(position) +
                    (Vector3.up * session.BossDefinition.DangerCellVisualHeight);
                if (!UsesHologramDangerCells)
                {
                    ApplyDangerCellColor(
                        _dangerCellRenderers[index],
                        _executingPatternDangerCells.Contains(position)
                            ? executeColor
                            : telegraphColor);
                }
            }
            VisibleDangerCellCount = visibleCount;
        }

        private void AddUniqueDangerCells(IReadOnlyList<GridPosition> cells)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                if (_visibleDangerCellSet.Add(cells[index]))
                {
                    _visibleDangerCells.Add(cells[index]);
                }
            }
        }

        private static int CompareGridPositions(
            GridPosition left,
            GridPosition right)
        {
            int xComparison = left.X.CompareTo(right.X);
            return xComparison != 0
                ? xComparison
                : left.Z.CompareTo(right.Z);
        }

        private static bool ShouldShowPreImpactDangerCells(
            BossPatternKind pattern)
        {
            return pattern != BossPatternKind.BombVolley &&
                   pattern != BossPatternKind.LastStandBombChain;
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
                Material hologramMaterial = _localHologramOverrides != null
                    ? _localHologramOverrides.BombRangeHologramMaterial
                    : null;
                bool usesHologram = PrototypeHologramTelegraphStyle.Apply(
                    instance,
                    hologramMaterial);
                if (!usesHologram)
                {
                    ResolveColorProperty(renderer, "Boss danger-cell");
                }
                UsesHologramDangerCells |= usesHologram;
                _dangerCellInstances.Add(instance);
                _dangerCellRenderers.Add(renderer);
            }
        }

        private void StartNextMovementSegment()
        {
            if (_movementTargets.Count == 0 || _bossInstance == null)
            {
                _isMoving = false;
                if (_animator != null)
                {
                    _animator.SetBool(IsMovingParameterId, false);
                }
                return;
            }

            GridPosition target = _movementTargets.Dequeue();
            _moveVisualFrom = _bossInstance.transform.position;
            _moveVisualTo = session.GridSpace.GridToWorld(target) +
                (Vector3.up * session.BossDefinition.VisualHeight);
            _moveVisualElapsed = 0f;
            _isMoving = true;
            Vector3 facing = _moveVisualTo - _moveVisualFrom;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                _bossInstance.transform.rotation =
                    Quaternion.LookRotation(facing.normalized, Vector3.up);
            }
            if (_animator != null)
            {
                _animator.SetBool(IsMovingParameterId, true);
            }
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
