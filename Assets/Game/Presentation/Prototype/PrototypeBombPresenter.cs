using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBombPresenter : MonoBehaviour
    {
        public const int DefaultBombPoolSize = 8;
        public const int DefaultExplosionPoolSize = 32;
        public const float CrossExplosionVisualSeconds = 1f;
        public const float CrossExplosionVisualHeight = 0.5f;
        public const string PlayerCrossBombDefinitionId = "prototype-cross";
        public const string PlayerLineBombDefinitionId = "prototype-line";
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
        private bool _initialized;
        private PrototypeLocalVfxOverrides _localVfxOverrides;
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

        public bool HasBombVisual(BombId bombId)
        {
            return _activeBombs.ContainsKey(bombId);
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

            session.BombPlaced += OnBombPlaced;
            session.BossBombPlaced += OnBombPlaced;
            session.BossBombLaunched += OnBossBombLaunched;
            session.ThrowerBombPlaced += OnBombPlaced;
            session.ThrowerBombLaunched += OnThrowerBombLaunched;
            session.BombExploded += OnBombExploded;
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
                session.BombPlaced -= OnBombPlaced;
                session.BossBombPlaced -= OnBombPlaced;
                session.BossBombLaunched -= OnBossBombLaunched;
                session.ThrowerBombPlaced -= OnBombPlaced;
                session.ThrowerBombLaunched -= OnThrowerBombLaunched;
                session.BombExploded -= OnBombExploded;
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

            _localVfxOverrides = PrototypeLocalVfxOverrides.LoadOptional();
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
                instance = AcquireBomb(snapshot.DefinitionId, definition);
            }
            instance.transform.position = session.GridSpace.GridToWorld(snapshot.Position);
            instance.transform.rotation = definition.ExplosionShape == BombExplosionShape.ForwardLine
                ? GetPlacementRotation(snapshot.PlacementDirection)
                : Quaternion.identity;
            instance.SetActive(true);
            SetBombAnimatorsEnabled(instance, true);
            _activeBombs.Add(
                snapshot.Id,
                new ActiveBombVisual(instance, snapshot.DefinitionId));
        }

        private void OnBossBombLaunched(BossBombFlight flight)
        {
            Initialize();
            BombDefinitionId definitionId = flight.Definition.Id;
            PrototypeBombDefinitionAsset definition = session.GetBombDefinition(definitionId);
            GameObject instance = AcquireBomb(definitionId, definition);
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
            GameObject instance = AcquireBomb(definitionId, definition);
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
                definition.Range <= 4 &&
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
            PrototypeBombDefinitionAsset definition)
        {
            Stack<GameObject> pool = GetBombPool(definitionId);
            return pool.Count > 0
                ? pool.Pop()
                : CreatePooledInstance(definition.BombPrefab, "BombVisual");
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

        private void SetBombAnimatorsEnabled(GameObject instance, bool enabled)
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
            }
        }

        private readonly struct ActiveBombVisual
        {
            public ActiveBombVisual(GameObject instance, BombDefinitionId definitionId)
            {
                Instance = instance;
                DefinitionId = definitionId;
            }

            public GameObject Instance { get; }

            public BombDefinitionId DefinitionId { get; }
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
