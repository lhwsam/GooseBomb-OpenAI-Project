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

        private readonly Dictionary<BombId, GameObject> _activeBombs =
            new Dictionary<BombId, GameObject>();
        private readonly Stack<GameObject> _availableBombs = new Stack<GameObject>();
        private readonly Stack<GameObject> _availableExplosions = new Stack<GameObject>();
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
                _availableExplosions.Push(visual.Instance);
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

            PrototypeBombDefinitionAsset definition = session.BombDefinition;
            definition.ValidatePresentationReferences();
            for (int index = 0; index < bombPoolSize; index++)
            {
                _availableBombs.Push(CreatePooledInstance(definition.BombPrefab, "BombVisual"));
            }
            for (int index = 0; index < explosionPoolSize; index++)
            {
                _availableExplosions.Push(
                    CreatePooledInstance(definition.ExplosionCellPrefab, "ExplosionCellVisual"));
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

            GameObject instance = AcquireBomb();
            instance.transform.position = session.GridSpace.GridToWorld(snapshot.Position);
            instance.SetActive(true);
            _activeBombs.Add(snapshot.Id, instance);
        }

        private void OnBombExploded(BombExplosion explosion)
        {
            if (_activeBombs.TryGetValue(explosion.BombId, out GameObject bombVisual))
            {
                _activeBombs.Remove(explosion.BombId);
                bombVisual.SetActive(false);
                _availableBombs.Push(bombVisual);
            }

            for (int index = 0; index < explosion.AffectedCells.Count; index++)
            {
                GameObject instance = AcquireExplosion();
                instance.transform.position = session.GridSpace.GridToWorld(
                    explosion.AffectedCells[index]);
                instance.SetActive(true);
                _activeExplosions.Add(new TimedExplosionVisual(
                    instance,
                    session.BombDefinition.ExplosionVisualSeconds));
            }
        }

        private GameObject AcquireBomb()
        {
            return _availableBombs.Count > 0
                ? _availableBombs.Pop()
                : CreatePooledInstance(session.BombDefinition.BombPrefab, "BombVisual");
        }

        private GameObject AcquireExplosion()
        {
            return _availableExplosions.Count > 0
                ? _availableExplosions.Pop()
                : CreatePooledInstance(
                    session.BombDefinition.ExplosionCellPrefab,
                    "ExplosionCellVisual");
        }

        private GameObject CreatePooledInstance(GameObject prefab, string instanceName)
        {
            GameObject instance = Instantiate(prefab, presentationRoot);
            instance.name = instanceName;
            instance.SetActive(false);
            return instance;
        }

        private struct TimedExplosionVisual
        {
            public TimedExplosionVisual(GameObject instance, float remainingSeconds)
            {
                Instance = instance;
                RemainingSeconds = remainingSeconds;
            }

            public GameObject Instance { get; }

            public float RemainingSeconds { get; set; }
        }
    }
}
