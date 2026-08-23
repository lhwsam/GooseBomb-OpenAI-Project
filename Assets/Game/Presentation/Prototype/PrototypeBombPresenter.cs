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
        private readonly List<TimedExplosionVisual> _activeExplosions =
            new List<TimedExplosionVisual>();
        private readonly List<ActiveBossFlightVisual> _activeBossFlights =
            new List<ActiveBossFlightVisual>(4);
        private readonly List<ActiveThrowerFlightVisual> _activeThrowerFlights =
            new List<ActiveThrowerFlightVisual>(3);
        private bool _initialized;

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
                GetExplosionPool(visual.DefinitionId).Push(visual.Instance);
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
            for (int index = 0; index < explosion.AffectedCells.Count; index++)
            {
                GameObject instance = AcquireExplosion(explosion.DefinitionId, definition);
                instance.transform.position = session.GridSpace.GridToWorld(
                    explosion.AffectedCells[index]);
                instance.SetActive(true);
                _activeExplosions.Add(new TimedExplosionVisual(
                    instance,
                    explosion.DefinitionId,
                    definition.ExplosionVisualSeconds));
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
                float remainingSeconds)
            {
                Instance = instance;
                DefinitionId = definitionId;
                RemainingSeconds = remainingSeconds;
            }

            public GameObject Instance { get; }

            public BombDefinitionId DefinitionId { get; }

            public float RemainingSeconds { get; set; }
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
