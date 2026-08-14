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

        private readonly Dictionary<BombId, ActiveBombVisual> _activeBombs =
            new Dictionary<BombId, ActiveBombVisual>();
        private readonly Dictionary<BombDefinitionId, Stack<GameObject>> _availableBombs =
            new Dictionary<BombDefinitionId, Stack<GameObject>>();
        private readonly Dictionary<BombDefinitionId, Stack<GameObject>> _availableExplosions =
            new Dictionary<BombDefinitionId, Stack<GameObject>>();
        private readonly List<TimedExplosionVisual> _activeExplosions =
            new List<TimedExplosionVisual>();
        private bool _initialized;

        public PrototypeGameSession Session => session;

        public Transform PresentationRoot => presentationRoot;

        public int BombPoolSize => bombPoolSize;

        public int ExplosionPoolSize => explosionPoolSize;

        public int ActiveBombVisualCount => _activeBombs.Count;

        public int ActiveExplosionVisualCount => _activeExplosions.Count;

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
                session.BombExploded -= OnBombExploded;
                session.Ready -= OnSessionReady;
            }
        }

        private void Update()
        {
            float elapsedSeconds = Time.deltaTime;
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
                PrototypeBombDefinitionAsset definition = session.BombLoadout.GetSlot(slotIndex);
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
            GameObject instance = AcquireBomb(snapshot.DefinitionId, definition);
            instance.transform.position = session.GridSpace.GridToWorld(snapshot.Position);
            instance.SetActive(true);
            _activeBombs.Add(
                snapshot.Id,
                new ActiveBombVisual(instance, snapshot.DefinitionId));
        }

        private void OnBombExploded(BombExplosion explosion)
        {
            if (_activeBombs.TryGetValue(
                    explosion.BombId,
                    out ActiveBombVisual bombVisual))
            {
                _activeBombs.Remove(explosion.BombId);
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
    }
}
