using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBombPresenter : MonoBehaviour
    {
        public const int DefaultBombPoolSize = 8;
        public const int DefaultExplosionPoolSize = 32;
        public const float BombFuseVisualReferenceSeconds = 2f;
        public const float CrossExplosionVisualSeconds = 1f;
        public const float CrossExplosionVisualHeight = 0.5f;
        public const float BombDangerCellVisualHeight = 0.03f;
        public const float DefaultFuseAudioVolume = 1f;
        public const float DefaultExplosionAudioVolume = 0.9f;
        public const float DefaultBombAudioMinDistance = 18f;
        public const float DefaultBombAudioMaxDistance = 30f;
        public const string PlayerCrossBombDefinitionId = "prototype-cross";
        public const string PlayerLineBombDefinitionId = "prototype-line";
        public const string SelfDestructBombDefinitionId = "prototype-self-destruct-blast";
        public const string ThrowerBombDefinitionId = "prototype-thrower-blocker";
        public const string BossThrowBombDefinitionId = "prototype-boss-throw";
        public const string BossChainBombDefinitionId = "prototype-boss-chain";
        public const string PlayerAreaBombDefinitionId = "prototype-area";

        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private int bombPoolSize = DefaultBombPoolSize;

        [SerializeField]
        private int explosionPoolSize = DefaultExplosionPoolSize;

        [SerializeField]
        private float bossThrowArcHeight = 2.2f;

        [Header("Bomb Audio")]
        [SerializeField]
        private AudioClip fuseAudioClip;

        [SerializeField]
        private AudioClip[] explosionAudioClips = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioMixerGroup bombAudioMixerGroup;

        [SerializeField, Range(0f, 1f)]
        private float fuseAudioVolume = DefaultFuseAudioVolume;

        [SerializeField, Range(0f, 1f)]
        private float explosionAudioVolume = DefaultExplosionAudioVolume;

        [SerializeField, Min(0.01f)]
        private float bombAudioMinDistance = DefaultBombAudioMinDistance;

        [SerializeField, Min(0.01f)]
        private float bombAudioMaxDistance = DefaultBombAudioMaxDistance;

        private readonly Dictionary<BombId, ActiveBombVisual> _activeBombs =
            new Dictionary<BombId, ActiveBombVisual>();
        private readonly Dictionary<BombDefinitionId, Stack<GameObject>> _availableBombs =
            new Dictionary<BombDefinitionId, Stack<GameObject>>();
        private readonly Dictionary<BombDefinitionId, Stack<GameObject>> _availableExplosions =
            new Dictionary<BombDefinitionId, Stack<GameObject>>();
        private readonly Dictionary<GameObject, Animator[]> _bombAnimators =
            new Dictionary<GameObject, Animator[]>();
        private readonly Dictionary<GameObject, ParticleSystem> _straightFlames =
            new Dictionary<GameObject, ParticleSystem>();
        private readonly Dictionary<GameObject, ParticleSystem[]> _particleSystems =
            new Dictionary<GameObject, ParticleSystem[]>();
        private readonly Dictionary<GameObject, float[]> _particleSimulationSpeeds =
            new Dictionary<GameObject, float[]>();
        private readonly HashSet<GameObject> _configuredBombReadyVfx =
            new HashSet<GameObject>();
        private readonly Stack<GameObject> _availableCrossCenters =
            new Stack<GameObject>();
        private readonly Stack<GameObject> _availableCrossStraights =
            new Stack<GameObject>();
        private readonly Stack<GameObject> _availableAreaGridExplosions =
            new Stack<GameObject>();
        private readonly List<TimedExplosionVisual> _activeExplosions =
            new List<TimedExplosionVisual>();
        private readonly List<ActiveBossFlightVisual> _activeBossFlights =
            new List<ActiveBossFlightVisual>(4);
        private readonly List<ActiveThrowerFlightVisual> _activeThrowerFlights =
            new List<ActiveThrowerFlightVisual>(3);
        private readonly Dictionary<BombId, IReadOnlyList<GridPosition>>
            _activeBombDangerCells =
                new Dictionary<BombId, IReadOnlyList<GridPosition>>();
        private readonly List<GridPosition> _visibleBombDangerCells =
            new List<GridPosition>();
        private readonly HashSet<GridPosition> _visibleBombDangerCellSet =
            new HashSet<GridPosition>();
        private readonly List<GameObject> _bombDangerCellInstances =
            new List<GameObject>();
        private readonly Dictionary<BombId, AudioSource> _activeFuseAudio =
            new Dictionary<BombId, AudioSource>();
        private readonly Stack<AudioSource> _availableFuseAudio =
            new Stack<AudioSource>();
        private readonly Stack<AudioSource> _availableExplosionAudio =
            new Stack<AudioSource>();
        private readonly List<TimedBombAudio> _activeExplosionAudio =
            new List<TimedBombAudio>();
        private bool _initialized;
        private int _lastExplosionAudioClipIndex = -1;
        private GameObject _bombDangerCellPrefab;
        private PrototypeLocalVfxOverrides _localVfxOverrides;
        private PrototypeLocalHologramOverrides _localHologramOverrides;
        private GameObject _crossCenterExplosionPrefab;
        private GameObject _crossStraightExplosionPrefab;
        private bool _crossExplosionVfxConfiguredExplicitly;
        private GameObject _areaGridExplosionPrefab;
        private bool _areaExplosionVfxConfiguredExplicitly;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public int BombPoolSize => bombPoolSize;

        public int ExplosionPoolSize => explosionPoolSize;

        public int ActiveBombVisualCount => _activeBombs.Count;

        public int ActiveExplosionVisualCount => _activeExplosions.Count;

        public int ActiveBossFlightVisualCount => _activeBossFlights.Count;

        public int ActiveThrowerFlightVisualCount => _activeThrowerFlights.Count;

        public int VisibleBombDangerCellCount => _visibleBombDangerCells.Count;

        public AudioClip FuseAudioClip => fuseAudioClip;

        public IReadOnlyList<AudioClip> ExplosionAudioClips => explosionAudioClips;

        public AudioMixerGroup BombAudioMixerGroup => bombAudioMixerGroup;

        public float FuseAudioVolume => fuseAudioVolume;

        public float ExplosionAudioVolume => explosionAudioVolume;

        public float BombAudioMinDistance => bombAudioMinDistance;

        public float BombAudioMaxDistance => bombAudioMaxDistance;

        public int ActiveFuseAudioCount => _activeFuseAudio.Count;

        public int ActiveExplosionAudioCount => _activeExplosionAudio.Count;

        public int FuseAudioPlayCount { get; private set; }

        public int ExplosionAudioPlayCount { get; private set; }

        public bool HasBombAudioConfiguration =>
            fuseAudioClip != null &&
            explosionAudioClips != null &&
            explosionAudioClips.Length > 0 &&
            Array.TrueForAll(explosionAudioClips, clip => clip != null) &&
            bombAudioMixerGroup != null;

        public bool UsesHologramBombDanger =>
            _localHologramOverrides != null &&
            _localHologramOverrides.BombRangeHologramMaterial != null;

        public bool HasBombDanger(BombId bombId)
        {
            return _activeBombDangerCells.ContainsKey(bombId);
        }

        public void Configure(
            PrototypeGameSession gameSession,
            Transform visualRoot,
            int initialBombPoolSize = DefaultBombPoolSize,
            int initialExplosionPoolSize = DefaultExplosionPoolSize)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBombPresenter before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (visualRoot == null)
            {
                throw new ArgumentNullException(nameof(visualRoot));
            }
            if (initialBombPoolSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBombPoolSize));
            }
            if (initialExplosionPoolSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialExplosionPoolSize));
            }

            session = gameSession;
            presentationRoot = visualRoot;
            bombPoolSize = initialBombPoolSize;
            explosionPoolSize = initialExplosionPoolSize;
        }

        public void ConfigureLocalVfxOverrides(
            PrototypeLocalVfxOverrides authoredLocalVfxOverrides)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBombPresenter before changing its local VFX configuration.");
            }

            _localVfxOverrides = authoredLocalVfxOverrides ??
                throw new ArgumentNullException(nameof(authoredLocalVfxOverrides));
            _localVfxOverrides.ValidateConfiguration();
        }

        public void ConfigureBombAudio(
            AudioClip authoredFuseAudioClip,
            AudioClip[] authoredExplosionAudioClips,
            AudioMixerGroup authoredMixerGroup,
            float authoredFuseAudioVolume = DefaultFuseAudioVolume,
            float authoredExplosionAudioVolume = DefaultExplosionAudioVolume,
            float authoredMinDistance = DefaultBombAudioMinDistance,
            float authoredMaxDistance = DefaultBombAudioMaxDistance)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeBombPresenter before changing its audio configuration.");
            }
            if (authoredFuseAudioClip == null)
            {
                throw new ArgumentNullException(nameof(authoredFuseAudioClip));
            }
            if (authoredExplosionAudioClips == null ||
                authoredExplosionAudioClips.Length == 0)
            {
                throw new ArgumentException(
                    "Bomb audio requires at least one explosion clip.",
                    nameof(authoredExplosionAudioClips));
            }
            for (int index = 0; index < authoredExplosionAudioClips.Length; index++)
            {
                if (authoredExplosionAudioClips[index] == null)
                {
                    throw new ArgumentException(
                        "Bomb explosion clips cannot contain null entries.",
                        nameof(authoredExplosionAudioClips));
                }
            }
            ValidateNormalizedAudioVolume(
                authoredFuseAudioVolume,
                nameof(authoredFuseAudioVolume));
            ValidateNormalizedAudioVolume(
                authoredExplosionAudioVolume,
                nameof(authoredExplosionAudioVolume));
            if (float.IsNaN(authoredMinDistance) ||
                float.IsInfinity(authoredMinDistance) ||
                authoredMinDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredMinDistance),
                    authoredMinDistance,
                    "Bomb audio minimum distance must be finite and positive.");
            }
            if (float.IsNaN(authoredMaxDistance) ||
                float.IsInfinity(authoredMaxDistance) ||
                authoredMaxDistance <= authoredMinDistance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredMaxDistance),
                    authoredMaxDistance,
                    "Bomb audio maximum distance must be finite and greater than the minimum distance.");
            }

            fuseAudioClip = authoredFuseAudioClip;
            explosionAudioClips = (AudioClip[])authoredExplosionAudioClips.Clone();
            bombAudioMixerGroup = authoredMixerGroup ??
                throw new ArgumentNullException(nameof(authoredMixerGroup));
            fuseAudioVolume = authoredFuseAudioVolume;
            explosionAudioVolume = authoredExplosionAudioVolume;
            bombAudioMinDistance = authoredMinDistance;
            bombAudioMaxDistance = authoredMaxDistance;
        }

        public bool HasBombVisual(BombId bombId)
        {
            return _activeBombs.ContainsKey(bombId);
        }

        public void HideAllForBossClear()
        {
            if (session == null || !session.HasBoss || session.IsBossAlive)
            {
                throw new InvalidOperationException(
                    "Boss-clear visual cleanup requires a defeated boss session.");
            }

            foreach (KeyValuePair<BombId, ActiveBombVisual> entry in _activeBombs)
            {
                ActiveBombVisual visual = entry.Value;
                SetBombAnimatorsEnabled(visual.Instance, false);
                visual.Instance.SetActive(false);
                GetBombPool(visual.DefinitionId).Push(visual.Instance);
            }
            _activeBombs.Clear();

            for (int index = 0; index < _activeBossFlights.Count; index++)
            {
                ActiveBossFlightVisual flight = _activeBossFlights[index];
                SetBombAnimatorsEnabled(flight.Instance, false);
                flight.Instance.SetActive(false);
                GetBombPool(flight.DefinitionId).Push(flight.Instance);
            }
            _activeBossFlights.Clear();

            for (int index = 0; index < _activeThrowerFlights.Count; index++)
            {
                ActiveThrowerFlightVisual flight = _activeThrowerFlights[index];
                SetBombAnimatorsEnabled(flight.Instance, false);
                flight.Instance.SetActive(false);
                GetBombPool(flight.DefinitionId).Push(flight.Instance);
            }
            _activeThrowerFlights.Clear();

            for (int index = 0; index < _activeExplosions.Count; index++)
            {
                ReleaseExplosion(_activeExplosions[index]);
            }
            _activeExplosions.Clear();
            _activeBombDangerCells.Clear();
            StopAllBombAudio();
            RefreshBombDangerCells();
        }

        public void ConfigureCrossExplosionVfx(
            GameObject centerExplosionPrefab,
            GameObject straightExplosionPrefab)
        {
            ValidateParticlePrefab(centerExplosionPrefab, nameof(centerExplosionPrefab));
            ValidateParticlePrefab(straightExplosionPrefab, nameof(straightExplosionPrefab));
            if (_activeExplosions.Count > 0)
            {
                throw new InvalidOperationException(
                    "Cross-explosion VFX cannot change while explosion visuals are active.");
            }

            _crossCenterExplosionPrefab = centerExplosionPrefab;
            _crossStraightExplosionPrefab = straightExplosionPrefab;
            _crossExplosionVfxConfiguredExplicitly = true;
        }

        public void ConfigureAreaExplosionVfx(GameObject gridExplosionPrefab)
        {
            ValidateParticlePrefab(gridExplosionPrefab, nameof(gridExplosionPrefab));
            if (_activeExplosions.Count > 0)
            {
                throw new InvalidOperationException(
                    "Area-explosion VFX cannot change while explosion visuals are active.");
            }

            _areaGridExplosionPrefab = gridExplosionPrefab;
            _areaExplosionVfxConfiguredExplicitly = true;
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
                    "PrototypeBombPresenter requires session and presentation-root references.");
            }

            session.BombActivated += OnBombActivated;
            session.BombPlaced += OnBombPlaced;
            session.BossBombPlaced += OnBombPlaced;
            session.BossBombLaunched += OnBossBombLaunched;
            session.ThrowerBombPlaced += OnBombPlaced;
            session.ThrowerBombLaunched += OnThrowerBombLaunched;
            session.BombExploded += OnBombExploded;
            session.PauseStateChanged += OnPauseStateChanged;
            session.Ready += OnSessionReady;
            if (session.IsReady)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.BombActivated -= OnBombActivated;
                session.BombPlaced -= OnBombPlaced;
                session.BossBombPlaced -= OnBombPlaced;
                session.BossBombLaunched -= OnBossBombLaunched;
                session.ThrowerBombPlaced -= OnBombPlaced;
                session.ThrowerBombLaunched -= OnThrowerBombLaunched;
                session.BombExploded -= OnBombExploded;
                session.PauseStateChanged -= OnPauseStateChanged;
                session.Ready -= OnSessionReady;
            }
            for (int index = 0; index < _activeBossFlights.Count; index++)
            {
                ActiveBossFlightVisual flight = _activeBossFlights[index];
                SetBombAnimatorsEnabled(flight.Instance, false);
                flight.Instance.SetActive(false);
                GetBombPool(flight.DefinitionId).Push(flight.Instance);
            }
            _activeBossFlights.Clear();
            for (int index = 0; index < _activeThrowerFlights.Count; index++)
            {
                ActiveThrowerFlightVisual flight = _activeThrowerFlights[index];
                SetBombAnimatorsEnabled(flight.Instance, false);
                flight.Instance.SetActive(false);
                GetBombPool(flight.DefinitionId).Push(flight.Instance);
            }
            _activeThrowerFlights.Clear();
            _activeBombDangerCells.Clear();
            _visibleBombDangerCells.Clear();
            _visibleBombDangerCellSet.Clear();
            for (int index = 0; index < _bombDangerCellInstances.Count; index++)
            {
                if (_bombDangerCellInstances[index] != null)
                {
                    _bombDangerCellInstances[index].SetActive(false);
                }
            }
            StopAllBombAudio();
        }

        private void Update()
        {
            float elapsedSeconds = Time.deltaTime;
            if (!session.IsPaused)
            {
                UpdateBossFlights(Time.unscaledDeltaTime);
                UpdateThrowerFlights(Time.unscaledDeltaTime);
            }
            for (int index = _activeExplosions.Count - 1; index >= 0; index--)
            {
                TimedExplosionVisual visual = _activeExplosions[index];
                visual.RemainingSeconds -= elapsedSeconds;
                if (visual.RemainingSeconds > 0f)
                {
                    _activeExplosions[index] = visual;
                    continue;
                }

                visual.Instance.SetActive(false);
                ReleaseExplosion(visual);
                _activeExplosions.RemoveAt(index);
            }

            if (!session.IsPaused)
            {
                UpdateExplosionAudio(Time.deltaTime);
            }
        }

        private void OnSessionReady()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (_localVfxOverrides == null)
            {
                _localVfxOverrides = PrototypeLocalVfxOverrides.LoadOptional();
            }
            _localHologramOverrides =
                PrototypeLocalHologramOverrides.LoadOptional();
            if (_crossCenterExplosionPrefab == null && _localVfxOverrides != null)
            {
                _crossCenterExplosionPrefab =
                    _localVfxOverrides.CrossBombCenterExplosionVfxPrefab;
                _crossStraightExplosionPrefab =
                    _localVfxOverrides.CrossBombStraightExplosionVfxPrefab;
            }
            if (_areaGridExplosionPrefab == null && _localVfxOverrides != null)
            {
                _areaGridExplosionPrefab =
                    _localVfxOverrides.AreaBombGridExplosionVfxPrefab;
            }

            for (int slotIndex = 0; slotIndex < BombWeaponLoadout.SlotCount; slotIndex++)
            {
                PrototypeBombDefinitionAsset definition =
                    session.GetBombDefinitionForSlot(slotIndex);
                if (definition == null)
                {
                    continue;
                }
                definition.ValidatePresentationReferences();
                BombDefinitionId definitionId = new BombDefinitionId(definition.DefinitionId);
                Stack<GameObject> bombPool = GetBombPool(definitionId);
                Stack<GameObject> explosionPool = GetExplosionPool(definitionId);
                for (int index = 0; index < bombPoolSize; index++)
                {
                    bombPool.Push(CreatePooledInstance(definition.BombPrefab, "BombVisual"));
                }
                for (int index = 0; index < explosionPoolSize; index++)
                {
                    explosionPool.Push(
                        CreatePooledInstance(definition.ExplosionCellPrefab, "ExplosionCellVisual"));
                }
            }

            _initialized = true;
        }

        private void OnBombPlaced(BombSnapshot snapshot)
        {
            Initialize();
            if (_activeBombs.ContainsKey(snapshot.Id))
            {
                throw new InvalidOperationException($"Bomb {snapshot.Id} already has a visual.");
            }

            PrototypeBombDefinitionAsset definition =
                session.GetBombDefinition(snapshot.DefinitionId);
            GameObject instance = snapshot.OwnerId == session.BossActorId
                ? TakeLandedBossFlight(snapshot)
                : snapshot.OwnerId == session.ThrowerActorId
                    ? TakeLandedThrowerFlight(snapshot)
                    : null;
            if (instance == null)
            {
                instance = AcquireBomb(
                    snapshot.DefinitionId,
                    definition,
                    snapshot.OwnerId == session.PlayerActorId);
            }
            instance.transform.position = session.GridSpace.GridToWorld(snapshot.Position);
            instance.transform.rotation = definition.ExplosionShape == BombExplosionShape.ForwardLine
                ? GetPlacementRotation(snapshot.PlacementDirection)
                : Quaternion.identity;
            instance.SetActive(true);
            float fuseAnimationSpeed =
                BombFuseVisualReferenceSeconds / definition.FuseSeconds;
            SetBombAnimatorsEnabled(
                instance,
                true,
                fuseAnimationSpeed,
                session.IsPaused);
            SetBombParticlePlayback(
                instance,
                fuseAnimationSpeed,
                session.IsPaused);
            _activeBombs.Add(
                snapshot.Id,
                new ActiveBombVisual(
                    instance,
                    snapshot.DefinitionId,
                    fuseAnimationSpeed));
        }

        private void OnBombActivated(BombSnapshot snapshot)
        {
            if (!session.TryGetBombExplosionPreview(
                    snapshot.Id,
                    out IReadOnlyList<GridPosition> affectedCells))
            {
                throw new InvalidOperationException(
                    $"Activated bomb {snapshot.Id} has no explosion preview.");
            }

            _activeBombDangerCells[snapshot.Id] = affectedCells;
            StartFuseAudio(snapshot);
            if (_bombDangerCellPrefab == null)
            {
                _bombDangerCellPrefab = ResolveBombDangerCellPrefab(snapshot.DefinitionId);
            }
            RefreshBombDangerCells();
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            foreach (KeyValuePair<BombId, ActiveBombVisual> entry in _activeBombs)
            {
                ActiveBombVisual visual = entry.Value;
                SetBombAnimatorPlayback(
                    visual.Instance,
                    isPaused ? 0f : visual.FuseAnimationSpeed);
                SetBombParticlesPaused(visual.Instance, isPaused);
            }
            SetBombAudioPaused(isPaused);
        }

        private void OnBossBombLaunched(BossBombFlight flight)
        {
            Initialize();
            BombDefinitionId definitionId = flight.Definition.Id;
            PrototypeBombDefinitionAsset definition = session.GetBombDefinition(definitionId);
            GameObject instance = AcquireBomb(
                definitionId,
                definition,
                applyPlayerFuseOffset: false);
            instance.transform.position = ToFlightPoint(flight.Origin, 0f);
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);
            _activeBossFlights.Add(new ActiveBossFlightVisual(
                instance,
                definitionId,
                flight,
                0f));
        }

        private void OnThrowerBombLaunched(ThrowerBombFlight flight)
        {
            Initialize();
            BombDefinitionId definitionId = flight.Definition.Id;
            PrototypeBombDefinitionAsset definition = session.GetBombDefinition(definitionId);
            GameObject instance = AcquireBomb(
                definitionId,
                definition,
                applyPlayerFuseOffset: false);
            instance.transform.position = ToFlightPoint(flight.Origin, 0f);
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);
            _activeThrowerFlights.Add(new ActiveThrowerFlightVisual(
                instance,
                definitionId,
                flight,
                0f));
        }

        private void UpdateBossFlights(float elapsedSeconds)
        {
            for (int index = 0; index < _activeBossFlights.Count; index++)
            {
                ActiveBossFlightVisual visual = _activeBossFlights[index];
                visual.ElapsedSeconds = Mathf.Min(
                    visual.ElapsedSeconds + elapsedSeconds,
                    (float)visual.Flight.Duration.TotalSeconds);
                float progress = Mathf.Clamp01(
                    visual.ElapsedSeconds / (float)visual.Flight.Duration.TotalSeconds);
                Vector3 origin = session.GridSpace.GridToWorld(visual.Flight.Origin);
                Vector3 target = session.GridSpace.GridToWorld(visual.Flight.Target);
                Vector3 position = Vector3.LerpUnclamped(origin, target, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * bossThrowArcHeight;
                visual.Instance.transform.position = position;
                visual.Instance.transform.Rotate(
                    Vector3.up,
                    elapsedSeconds * 540f,
                    Space.World);
                _activeBossFlights[index] = visual;
            }
        }

        private GameObject TakeLandedBossFlight(BombSnapshot snapshot)
        {
            for (int index = 0; index < _activeBossFlights.Count; index++)
            {
                ActiveBossFlightVisual flight = _activeBossFlights[index];
                if (flight.DefinitionId != snapshot.DefinitionId ||
                    flight.Flight.Target != snapshot.Position)
                {
                    continue;
                }

                _activeBossFlights.RemoveAt(index);
                return flight.Instance;
            }
            return null;
        }

        private void UpdateThrowerFlights(float elapsedSeconds)
        {
            for (int index = _activeThrowerFlights.Count - 1; index >= 0; index--)
            {
                ActiveThrowerFlightVisual visual = _activeThrowerFlights[index];
                visual.ElapsedSeconds = Mathf.Min(
                    visual.ElapsedSeconds + elapsedSeconds,
                    (float)visual.Flight.Duration.TotalSeconds);
                float progress = Mathf.Clamp01(
                    visual.ElapsedSeconds / (float)visual.Flight.Duration.TotalSeconds);
                Vector3 origin = session.GridSpace.GridToWorld(visual.Flight.Origin);
                Vector3 target = session.GridSpace.GridToWorld(visual.Flight.Target);
                Vector3 position = Vector3.LerpUnclamped(origin, target, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * bossThrowArcHeight;
                visual.Instance.transform.position = position;
                visual.Instance.transform.Rotate(
                    Vector3.up,
                    elapsedSeconds * 540f,
                    Space.World);
                if (progress >= 1f)
                {
                    SetBombAnimatorsEnabled(visual.Instance, false);
                    visual.Instance.SetActive(false);
                    GetBombPool(visual.DefinitionId).Push(visual.Instance);
                    _activeThrowerFlights.RemoveAt(index);
                    continue;
                }
                _activeThrowerFlights[index] = visual;
            }
        }

        private GameObject TakeLandedThrowerFlight(BombSnapshot snapshot)
        {
            for (int index = 0; index < _activeThrowerFlights.Count; index++)
            {
                ActiveThrowerFlightVisual flight = _activeThrowerFlights[index];
                if (flight.DefinitionId != snapshot.DefinitionId ||
                    flight.Flight.Target != snapshot.Position)
                {
                    continue;
                }

                _activeThrowerFlights.RemoveAt(index);
                return flight.Instance;
            }
            return null;
        }

        private Vector3 ToFlightPoint(GridPosition position, float height)
        {
            return session.GridSpace.GridToWorld(position) + (Vector3.up * height);
        }

        private void OnBombExploded(BombExplosion explosion)
        {
            StopFuseAudio(explosion.BombId);
            PlayExplosionAudio(explosion);
            if (_activeBombDangerCells.Remove(explosion.BombId))
            {
                RefreshBombDangerCells();
            }

            if (_activeBombs.TryGetValue(
                    explosion.BombId,
                    out ActiveBombVisual bombVisual))
            {
                _activeBombs.Remove(explosion.BombId);
                SetBombAnimatorsEnabled(bombVisual.Instance, false);
                bombVisual.Instance.SetActive(false);
                GetBombPool(bombVisual.DefinitionId).Push(bombVisual.Instance);
            }

            PrototypeBombDefinitionAsset definition =
                session.GetBombDefinition(explosion.DefinitionId);
            if (CanPresentCrossExplosion(explosion, definition))
            {
                PresentCrossExplosion(explosion, definition);
                return;
            }
            if (CanPresentLineExplosion(explosion, definition))
            {
                PresentLineExplosion(explosion, definition);
                return;
            }
            if (CanPresentAreaExplosion(explosion, definition))
            {
                PresentAreaExplosion(explosion, definition);
                return;
            }

            for (int index = 0; index < explosion.AffectedCells.Count; index++)
            {
                GameObject instance = AcquireExplosion(explosion.DefinitionId, definition);
                instance.transform.position = session.GridSpace.GridToWorld(
                    explosion.AffectedCells[index]);
                instance.SetActive(true);
                _activeExplosions.Add(new TimedExplosionVisual(
                    instance,
                    explosion.DefinitionId,
                    definition.ExplosionVisualSeconds,
                    ExplosionVisualKind.Cell));
            }
        }

        private bool CanPresentCrossExplosion(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            return definition.ExplosionShape == BombExplosionShape.Cross &&
                definition.Range <= 4 &&
                IsSupportedCrossExplosionOwner(explosion, definition) &&
                _crossCenterExplosionPrefab != null &&
                _crossStraightExplosionPrefab != null;
        }

        private bool IsSupportedCrossExplosionOwner(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            if (explosion.OwnerId == session.PlayerActorId)
            {
                return _crossExplosionVfxConfiguredExplicitly ||
                    definition.DefinitionId == PlayerCrossBombDefinitionId;
            }
            if (explosion.OwnerId == session.SelfDestructActorId)
            {
                return _crossExplosionVfxConfiguredExplicitly ||
                    definition.DefinitionId == SelfDestructBombDefinitionId;
            }
            if (explosion.OwnerId == session.ThrowerActorId)
            {
                return _crossExplosionVfxConfiguredExplicitly ||
                    definition.DefinitionId == ThrowerBombDefinitionId;
            }
            if (explosion.OwnerId == session.BossActorId)
            {
                return _crossExplosionVfxConfiguredExplicitly ||
                    definition.DefinitionId == BossThrowBombDefinitionId ||
                    definition.DefinitionId == BossChainBombDefinitionId;
            }
            return false;
        }

        private void PresentCrossExplosion(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            PresentExplosionCenter(explosion, definition);

            PresentStraightDirection(explosion, definition, CardinalDirection.North, 0, 1);
            PresentStraightDirection(explosion, definition, CardinalDirection.East, 1, 0);
            PresentStraightDirection(explosion, definition, CardinalDirection.South, 0, -1);
            PresentStraightDirection(explosion, definition, CardinalDirection.West, -1, 0);
        }

        private bool CanPresentLineExplosion(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            return explosion.OwnerId == session.PlayerActorId &&
                definition.ExplosionShape == BombExplosionShape.ForwardLine &&
                definition.Range <= 5 &&
                (_crossExplosionVfxConfiguredExplicitly ||
                 definition.DefinitionId == PlayerLineBombDefinitionId) &&
                _crossCenterExplosionPrefab != null &&
                _crossStraightExplosionPrefab != null;
        }

        private bool CanPresentAreaExplosion(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            return explosion.OwnerId == session.PlayerActorId &&
                definition.ExplosionShape == BombExplosionShape.SquareArea &&
                (_areaExplosionVfxConfiguredExplicitly ||
                 definition.DefinitionId == PlayerAreaBombDefinitionId) &&
                _areaGridExplosionPrefab != null;
        }

        private void PresentAreaExplosion(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            float visualSeconds = Mathf.Max(
                definition.ExplosionVisualSeconds,
                CrossExplosionVisualSeconds);
            for (int index = 0; index < explosion.AffectedCells.Count; index++)
            {
                GameObject instance = AcquireAreaGridExplosion();
                PrepareParticleInstance(
                    instance,
                    GetCrossExplosionWorldPosition(explosion.AffectedCells[index]),
                    Quaternion.identity);
                _activeExplosions.Add(new TimedExplosionVisual(
                    instance,
                    explosion.DefinitionId,
                    visualSeconds,
                    ExplosionVisualKind.AreaGrid));
            }
        }

        private void PresentLineExplosion(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            PresentExplosionCenter(explosion, definition);
            GetDirectionStep(
                explosion.PlacementDirection,
                out int stepX,
                out int stepZ);
            PresentStraightDirection(
                explosion,
                definition,
                explosion.PlacementDirection,
                stepX,
                stepZ);
        }

        private void PresentExplosionCenter(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition)
        {
            GameObject center = AcquireCrossCenter();
            PrepareParticleInstance(
                center,
                GetCrossExplosionWorldPosition(explosion.Origin),
                Quaternion.identity);
            _activeExplosions.Add(new TimedExplosionVisual(
                center,
                explosion.DefinitionId,
                Mathf.Max(
                    definition.ExplosionVisualSeconds,
                    CrossExplosionVisualSeconds),
                ExplosionVisualKind.CrossCenter));
        }

        private void PresentStraightDirection(
            BombExplosion explosion,
            PrototypeBombDefinitionAsset definition,
            CardinalDirection direction,
            int stepX,
            int stepZ)
        {
            int cellCount = 0;
            for (int distance = 1; distance <= definition.Range; distance++)
            {
                var position = new GridPosition(
                    explosion.Origin.X + (stepX * distance),
                    explosion.Origin.Z + (stepZ * distance));
                if (!explosion.Affects(position))
                {
                    break;
                }
                cellCount++;
            }
            if (cellCount == 0)
            {
                return;
            }

            GameObject straight = AcquireCrossStraight();
            SetStraightSpeedModifier(straight, cellCount * 0.25f);
            PrepareParticleInstance(
                straight,
                GetCrossExplosionWorldPosition(explosion.Origin),
                GetPlacementRotation(direction));
            _activeExplosions.Add(new TimedExplosionVisual(
                straight,
                explosion.DefinitionId,
                Mathf.Max(
                    definition.ExplosionVisualSeconds,
                    CrossExplosionVisualSeconds),
                ExplosionVisualKind.CrossStraight));
        }

        private static void GetDirectionStep(
            CardinalDirection direction,
            out int stepX,
            out int stepZ)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    stepX = 0;
                    stepZ = 1;
                    return;
                case CardinalDirection.East:
                    stepX = 1;
                    stepZ = 0;
                    return;
                case CardinalDirection.South:
                    stepX = 0;
                    stepZ = -1;
                    return;
                case CardinalDirection.West:
                    stepX = -1;
                    stepZ = 0;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "A forward-line explosion VFX requires a cardinal direction.");
            }
        }

        private GameObject AcquireCrossCenter()
        {
            return _availableCrossCenters.Count > 0
                ? _availableCrossCenters.Pop()
                : CreatePooledInstance(_crossCenterExplosionPrefab, "CrossExplosionCenterVisual");
        }

        private Vector3 GetCrossExplosionWorldPosition(GridPosition origin)
        {
            return session.GridSpace.GridToWorld(origin) +
                (Vector3.up * CrossExplosionVisualHeight);
        }

        private GameObject AcquireCrossStraight()
        {
            GameObject instance = _availableCrossStraights.Count > 0
                ? _availableCrossStraights.Pop()
                : CreatePooledInstance(_crossStraightExplosionPrefab, "CrossExplosionStraightVisual");
            if (!_straightFlames.ContainsKey(instance))
            {
                ParticleSystem flames = FindParticleSystem(instance, "Flames_F");
                if (flames == null)
                {
                    throw new InvalidOperationException(
                        "Cross straight explosion VFX requires a child ParticleSystem named 'Flames_F'.");
                }
                _straightFlames.Add(instance, flames);
            }
            return instance;
        }

        private GameObject AcquireAreaGridExplosion()
        {
            return _availableAreaGridExplosions.Count > 0
                ? _availableAreaGridExplosions.Pop()
                : CreatePooledInstance(
                    _areaGridExplosionPrefab,
                    "AreaGridExplosionVisual");
        }

        private void SetStraightSpeedModifier(GameObject instance, float speedModifier)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity =
                _straightFlames[instance].velocityOverLifetime;
            velocity.speedModifier = speedModifier;
        }

        private ParticleSystem FindParticleSystem(GameObject instance, string objectName)
        {
            ParticleSystem[] systems = GetParticleSystems(instance);
            for (int index = 0; index < systems.Length; index++)
            {
                if (systems[index].name == objectName)
                {
                    return systems[index];
                }
            }
            return null;
        }

        private void PrepareParticleInstance(
            GameObject instance,
            Vector3 position,
            Quaternion rotation)
        {
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            ParticleSystem[] systems = GetParticleSystems(instance);
            for (int index = 0; index < systems.Length; index++)
            {
                systems[index].Clear(true);
                systems[index].Play(true);
            }
        }

        private void ReleaseExplosion(TimedExplosionVisual visual)
        {
            ParticleSystem[] systems = GetParticleSystems(visual.Instance);
            for (int index = 0; index < systems.Length; index++)
            {
                systems[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            visual.Instance.SetActive(false);
            switch (visual.Kind)
            {
                case ExplosionVisualKind.Cell:
                    GetExplosionPool(visual.DefinitionId).Push(visual.Instance);
                    break;
                case ExplosionVisualKind.CrossCenter:
                    _availableCrossCenters.Push(visual.Instance);
                    break;
                case ExplosionVisualKind.CrossStraight:
                    _availableCrossStraights.Push(visual.Instance);
                    break;
                case ExplosionVisualKind.AreaGrid:
                    _availableAreaGridExplosions.Push(visual.Instance);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ParticleSystem[] GetParticleSystems(GameObject instance)
        {
            if (!_particleSystems.TryGetValue(instance, out ParticleSystem[] systems))
            {
                systems = instance.GetComponentsInChildren<ParticleSystem>(true);
                _particleSystems.Add(instance, systems);
            }
            return systems;
        }

        private static void ValidateParticlePrefab(GameObject prefab, string parameterName)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
            {
                throw new ArgumentException(
                    "Explosion VFX prefab requires at least one ParticleSystem.",
                    parameterName);
            }
        }

        private GameObject AcquireBomb(
            BombDefinitionId definitionId,
            PrototypeBombDefinitionAsset definition,
            bool applyPlayerFuseOffset)
        {
            Stack<GameObject> pool = GetBombPool(definitionId);
            GameObject instance = pool.Count > 0
                ? pool.Pop()
                : CreatePooledInstance(definition.BombPrefab, "BombVisual");
            ConfigureBombReadyVfx(instance, applyPlayerFuseOffset);
            return instance;
        }

        private void ConfigureBombReadyVfx(
            GameObject instance,
            bool applyPlayerFuseOffset)
        {
            if (_localVfxOverrides == null || !_configuredBombReadyVfx.Add(instance))
            {
                return;
            }

            Transform anchor = instance.transform.Find("SparksEffect");
            if (anchor == null)
            {
                return;
            }

            if (applyPlayerFuseOffset)
            {
                anchor.localPosition = _localVfxOverrides.BombReadyLocalPosition;
                anchor.localRotation = _localVfxOverrides.BombReadyLocalRotation;
                anchor.localScale = Vector3.one;
            }

            ParticleSystem[] systems = anchor.GetComponentsInChildren<ParticleSystem>(true);
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem.MainModule main = systems[index].main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;

                ParticleSystem.CollisionModule collision = systems[index].collision;
                collision.enabled = false;
            }
        }

        private GameObject AcquireExplosion(
            BombDefinitionId definitionId,
            PrototypeBombDefinitionAsset definition)
        {
            Stack<GameObject> pool = GetExplosionPool(definitionId);
            return pool.Count > 0
                ? pool.Pop()
                : CreatePooledInstance(definition.ExplosionCellPrefab, "ExplosionCellVisual");
        }

        private static Quaternion GetPlacementRotation(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return Quaternion.identity;
                case CardinalDirection.East:
                    return Quaternion.Euler(0f, 90f, 0f);
                case CardinalDirection.South:
                    return Quaternion.Euler(0f, 180f, 0f);
                case CardinalDirection.West:
                    return Quaternion.Euler(0f, 270f, 0f);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "A forward-line bomb visual requires a cardinal placement direction.");
            }
        }

        private Stack<GameObject> GetBombPool(BombDefinitionId definitionId)
        {
            if (!_availableBombs.TryGetValue(definitionId, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _availableBombs.Add(definitionId, pool);
            }

            return pool;
        }

        private Stack<GameObject> GetExplosionPool(BombDefinitionId definitionId)
        {
            if (!_availableExplosions.TryGetValue(definitionId, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _availableExplosions.Add(definitionId, pool);
            }

            return pool;
        }

        private GameObject CreatePooledInstance(GameObject prefab, string instanceName)
        {
            GameObject instance = Instantiate(prefab, presentationRoot);
            instance.name = instanceName;
            instance.SetActive(false);
            return instance;
        }

        private void SetBombAnimatorsEnabled(
            GameObject instance,
            bool enabled,
            float playbackSpeed = 1f,
            bool paused = false)
        {
            if (!_bombAnimators.TryGetValue(instance, out Animator[] animators))
            {
                animators = instance.GetComponentsInChildren<Animator>(true);
                _bombAnimators.Add(instance, animators);
            }
            for (int index = 0; index < animators.Length; index++)
            {
                Animator animator = animators[index];
                animator.enabled = enabled;
                if (enabled && animator.runtimeAnimatorController != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
                animator.speed = enabled && !paused ? playbackSpeed : 0f;
            }
        }

        private void StartFuseAudio(BombSnapshot snapshot)
        {
            if (!HasBombAudioConfiguration)
            {
                return;
            }
            if (_activeFuseAudio.ContainsKey(snapshot.Id))
            {
                throw new InvalidOperationException(
                    $"Bomb {snapshot.Id} already has active fuse audio.");
            }

            AudioSource source = AcquireBombAudioSource(
                _availableFuseAudio,
                "BombFuseAudio");
            source.transform.position = session.GridSpace.GridToWorld(snapshot.Position);
            source.clip = fuseAudioClip;
            source.loop = true;
            source.volume = fuseAudioVolume;
            source.Play();
            if (session.IsPaused)
            {
                source.Pause();
            }
            _activeFuseAudio.Add(snapshot.Id, source);
            FuseAudioPlayCount++;
        }

        private void StopFuseAudio(BombId bombId)
        {
            if (!_activeFuseAudio.TryGetValue(bombId, out AudioSource source))
            {
                return;
            }

            _activeFuseAudio.Remove(bombId);
            ReleaseBombAudioSource(source, _availableFuseAudio);
        }

        private void PlayExplosionAudio(BombExplosion explosion)
        {
            if (!HasBombAudioConfiguration)
            {
                return;
            }

            int clipIndex = SelectExplosionAudioClipIndex();
            AudioClip clip = explosionAudioClips[clipIndex];
            AudioSource source = AcquireBombAudioSource(
                _availableExplosionAudio,
                "BombExplosionAudio");
            source.transform.position = session.GridSpace.GridToWorld(explosion.Origin);
            source.clip = clip;
            source.loop = false;
            source.volume = explosionAudioVolume;
            source.Play();
            if (session.IsPaused)
            {
                source.Pause();
            }
            _activeExplosionAudio.Add(new TimedBombAudio(source, clip.length));
            _lastExplosionAudioClipIndex = clipIndex;
            ExplosionAudioPlayCount++;
        }

        private void UpdateExplosionAudio(float elapsedSeconds)
        {
            for (int index = _activeExplosionAudio.Count - 1; index >= 0; index--)
            {
                TimedBombAudio audio = _activeExplosionAudio[index];
                audio.RemainingSeconds -= elapsedSeconds;
                if (audio.RemainingSeconds > 0f)
                {
                    _activeExplosionAudio[index] = audio;
                    continue;
                }

                ReleaseBombAudioSource(audio.Source, _availableExplosionAudio);
                _activeExplosionAudio.RemoveAt(index);
            }
        }

        private void SetBombAudioPaused(bool isPaused)
        {
            foreach (KeyValuePair<BombId, AudioSource> entry in _activeFuseAudio)
            {
                SetAudioSourcePaused(entry.Value, isPaused);
            }
            for (int index = 0; index < _activeExplosionAudio.Count; index++)
            {
                SetAudioSourcePaused(_activeExplosionAudio[index].Source, isPaused);
            }
        }

        private void StopAllBombAudio()
        {
            foreach (KeyValuePair<BombId, AudioSource> entry in _activeFuseAudio)
            {
                ReleaseBombAudioSource(entry.Value, _availableFuseAudio);
            }
            _activeFuseAudio.Clear();

            for (int index = 0; index < _activeExplosionAudio.Count; index++)
            {
                ReleaseBombAudioSource(
                    _activeExplosionAudio[index].Source,
                    _availableExplosionAudio);
            }
            _activeExplosionAudio.Clear();
        }

        private AudioSource AcquireBombAudioSource(
            Stack<AudioSource> pool,
            string objectName)
        {
            AudioSource source;
            if (pool.Count > 0)
            {
                source = pool.Pop();
            }
            else
            {
                var audioObject = new GameObject(objectName);
                audioObject.transform.SetParent(presentationRoot, false);
                source = audioObject.AddComponent<AudioSource>();
            }

            source.gameObject.SetActive(true);
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = bombAudioMinDistance;
            source.maxDistance = bombAudioMaxDistance;
            source.dopplerLevel = 0f;
            source.outputAudioMixerGroup = bombAudioMixerGroup;
            return source;
        }

        private static void ValidateNormalizedAudioVolume(float volume, string parameterName)
        {
            if (float.IsNaN(volume) ||
                float.IsInfinity(volume) ||
                volume < 0f ||
                volume > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    volume,
                    "Bomb audio volume must be finite and between zero and one.");
            }
        }

        private static void ReleaseBombAudioSource(
            AudioSource source,
            Stack<AudioSource> pool)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.gameObject.SetActive(false);
            pool.Push(source);
        }

        private static void SetAudioSourcePaused(AudioSource source, bool isPaused)
        {
            if (source == null)
            {
                return;
            }
            if (isPaused)
            {
                source.Pause();
            }
            else
            {
                source.UnPause();
            }
        }

        private int SelectExplosionAudioClipIndex()
        {
            if (explosionAudioClips.Length == 1 ||
                _lastExplosionAudioClipIndex < 0 ||
                _lastExplosionAudioClipIndex >= explosionAudioClips.Length)
            {
                return UnityEngine.Random.Range(0, explosionAudioClips.Length);
            }

            int candidate = UnityEngine.Random.Range(0, explosionAudioClips.Length - 1);
            return candidate >= _lastExplosionAudioClipIndex
                ? candidate + 1
                : candidate;
        }

        private void RefreshBombDangerCells()
        {
            _visibleBombDangerCells.Clear();
            _visibleBombDangerCellSet.Clear();
            foreach (KeyValuePair<BombId, IReadOnlyList<GridPosition>> entry in
                     _activeBombDangerCells)
            {
                IReadOnlyList<GridPosition> cells = entry.Value;
                for (int index = 0; index < cells.Count; index++)
                {
                    if (_visibleBombDangerCellSet.Add(cells[index]))
                    {
                        _visibleBombDangerCells.Add(cells[index]);
                    }
                }
            }
            _visibleBombDangerCells.Sort(CompareGridPositions);

            EnsureBombDangerCellCapacity(_visibleBombDangerCells.Count);
            for (int index = 0; index < _bombDangerCellInstances.Count; index++)
            {
                GameObject instance = _bombDangerCellInstances[index];
                bool shouldShow = index < _visibleBombDangerCells.Count;
                if (shouldShow)
                {
                    instance.transform.position = session.GridSpace.GridToWorld(
                            _visibleBombDangerCells[index]) +
                        (Vector3.up * BombDangerCellVisualHeight);
                }
                instance.SetActive(shouldShow);
            }
        }

        private void EnsureBombDangerCellCapacity(int required)
        {
            while (_bombDangerCellInstances.Count < required)
            {
                GameObject instance = CreatePooledInstance(
                    _bombDangerCellPrefab,
                    $"BombDangerCellVisual{_bombDangerCellInstances.Count}");
                ApplyBombDangerHologram(instance);
                _bombDangerCellInstances.Add(instance);
            }
        }

        private void ApplyBombDangerHologram(GameObject instance)
        {
            Material hologramMaterial = _localHologramOverrides != null
                ? _localHologramOverrides.BombRangeHologramMaterial
                : null;
            PrototypeHologramTelegraphStyle.Apply(instance, hologramMaterial);
        }

        private GameObject ResolveBombDangerCellPrefab(BombDefinitionId definitionId)
        {
            if (session.SelfDestructDefinition != null &&
                session.SelfDestructDefinition.TelegraphCellPrefab != null)
            {
                return session.SelfDestructDefinition.TelegraphCellPrefab;
            }
            if (session.BossDefinition != null &&
                session.BossDefinition.DangerCellPrefab != null)
            {
                return session.BossDefinition.DangerCellPrefab;
            }
            if (session.ThrowerDefinition != null &&
                session.ThrowerDefinition.TelegraphCellPrefab != null)
            {
                return session.ThrowerDefinition.TelegraphCellPrefab;
            }

            PrototypeBombDefinitionAsset definition =
                session.GetBombDefinition(definitionId);
            if (definition.ExplosionCellPrefab == null)
            {
                throw new InvalidOperationException(
                    "A bomb danger cell visual requires a presentation prefab.");
            }
            return definition.ExplosionCellPrefab;
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

        private void SetBombAnimatorPlayback(GameObject instance, float playbackSpeed)
        {
            if (!_bombAnimators.TryGetValue(instance, out Animator[] animators))
            {
                return;
            }
            for (int index = 0; index < animators.Length; index++)
            {
                if (animators[index].enabled)
                {
                    animators[index].speed = playbackSpeed;
                }
            }
        }

        private void SetBombParticlesPaused(GameObject instance, bool paused)
        {
            ParticleSystem[] systems = GetParticleSystems(instance);
            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem system = systems[index];
                if (paused && system.isPlaying)
                {
                    system.Pause(true);
                }
                else if (!paused && system.isPaused)
                {
                    system.Play(true);
                }
            }
        }

        private void SetBombParticlePlayback(
            GameObject instance,
            float playbackSpeed,
            bool paused)
        {
            ParticleSystem[] systems = GetParticleSystems(instance);
            if (!_particleSimulationSpeeds.TryGetValue(instance, out float[] baseSpeeds))
            {
                baseSpeeds = new float[systems.Length];
                for (int index = 0; index < systems.Length; index++)
                {
                    baseSpeeds[index] = systems[index].main.simulationSpeed;
                }
                _particleSimulationSpeeds.Add(instance, baseSpeeds);
            }

            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem.MainModule main = systems[index].main;
                main.simulationSpeed = baseSpeeds[index] * playbackSpeed;
            }
            SetBombParticlesPaused(instance, paused);
        }

        private readonly struct ActiveBombVisual
        {
            public ActiveBombVisual(
                GameObject instance,
                BombDefinitionId definitionId,
                float fuseAnimationSpeed)
            {
                Instance = instance;
                DefinitionId = definitionId;
                FuseAnimationSpeed = fuseAnimationSpeed;
            }

            public GameObject Instance { get; }

            public BombDefinitionId DefinitionId { get; }

            public float FuseAnimationSpeed { get; }
        }

        private struct TimedExplosionVisual
        {
            public TimedExplosionVisual(
                GameObject instance,
                BombDefinitionId definitionId,
                float remainingSeconds,
                ExplosionVisualKind kind)
            {
                Instance = instance;
                DefinitionId = definitionId;
                RemainingSeconds = remainingSeconds;
                Kind = kind;
            }

            public GameObject Instance { get; }

            public BombDefinitionId DefinitionId { get; }

            public float RemainingSeconds { get; set; }

            public ExplosionVisualKind Kind { get; }
        }

        private struct TimedBombAudio
        {
            public TimedBombAudio(AudioSource source, float remainingSeconds)
            {
                Source = source;
                RemainingSeconds = remainingSeconds;
            }

            public AudioSource Source { get; }

            public float RemainingSeconds { get; set; }
        }

        private enum ExplosionVisualKind
        {
            Cell = 0,
            CrossCenter = 1,
            CrossStraight = 2,
            AreaGrid = 3,
        }

        private struct ActiveBossFlightVisual
        {
            public ActiveBossFlightVisual(
                GameObject instance,
                BombDefinitionId definitionId,
                BossBombFlight flight,
                float elapsedSeconds)
            {
                Instance = instance;
                DefinitionId = definitionId;
                Flight = flight;
                ElapsedSeconds = elapsedSeconds;
            }

            public GameObject Instance { get; }
            public BombDefinitionId DefinitionId { get; }
            public BossBombFlight Flight { get; }
            public float ElapsedSeconds { get; set; }
        }

        private struct ActiveThrowerFlightVisual
        {
            public ActiveThrowerFlightVisual(
                GameObject instance,
                BombDefinitionId definitionId,
                ThrowerBombFlight flight,
                float elapsedSeconds)
            {
                Instance = instance;
                DefinitionId = definitionId;
                Flight = flight;
                ElapsedSeconds = elapsedSeconds;
            }

            public GameObject Instance { get; }
            public BombDefinitionId DefinitionId { get; }
            public ThrowerBombFlight Flight { get; }
            public float ElapsedSeconds { get; set; }
        }
    }
}
