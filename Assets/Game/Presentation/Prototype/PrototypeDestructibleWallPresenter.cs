using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDestructibleWallPresenter : MonoBehaviour
    {
        [SerializeField]
        private PrototypeGameSession session;

        [SerializeField]
        private Transform wallRoot;

        private readonly Dictionary<GridPosition, GameObject> _wallVisuals =
            new Dictionary<GridPosition, GameObject>();
        private bool _initialized;

        public PrototypeGameSession Session => session;

        public Transform WallRoot => wallRoot;

        public int ActiveWallVisualCount
        {
            get
            {
                int count = 0;
                foreach (GameObject visual in _wallVisuals.Values)
                {
                    if (visual.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Configure(PrototypeGameSession gameSession, Transform authoredWallRoot)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeDestructibleWallPresenter before changing its runtime configuration.");
            }
            if (gameSession == null)
            {
                throw new ArgumentNullException(nameof(gameSession));
            }
            if (authoredWallRoot == null)
            {
                throw new ArgumentNullException(nameof(authoredWallRoot));
            }

            session = gameSession;
            wallRoot = authoredWallRoot;
        }

        public bool HasWallVisual(GridPosition position)
        {
            return _wallVisuals.TryGetValue(position, out GameObject visual) &&
                   visual.activeSelf;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (session == null || wallRoot == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDestructibleWallPresenter requires session and wall-root references.");
            }

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
                session.BombExploded -= OnBombExploded;
                session.Ready -= OnSessionReady;
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

            for (int index = 0; index < wallRoot.childCount; index++)
            {
                GameObject visual = wallRoot.GetChild(index).gameObject;
                GridPosition position = session.GridSpace.WorldToGrid(visual.transform.position);
                if (session.GetCell(position).Terrain != GridTerrain.DestructibleWall)
                {
                    throw new InvalidOperationException(
                        $"Destructible wall visual '{visual.name}' has no logical wall at {position}.");
                }
                if (_wallVisuals.ContainsKey(position))
                {
                    throw new InvalidOperationException(
                        $"Multiple destructible wall visuals occupy {position}.");
                }
                _wallVisuals.Add(position, visual);
            }

            _initialized = true;
        }

        private void OnBombExploded(BombExplosion explosion)
        {
            Initialize();
            for (int index = 0; index < explosion.DestroyedWalls.Count; index++)
            {
                GridPosition position = explosion.DestroyedWalls[index];
                if (!_wallVisuals.TryGetValue(position, out GameObject visual))
                {
                    if (session.IsRuntimeDestructibleWall(position))
                    {
                        if (session.GetCell(position).Terrain != GridTerrain.Floor)
                        {
                            throw new InvalidOperationException(
                                $"Destroyed runtime exit wall {position} must be logical floor.");
                        }
                        continue;
                    }
                    throw new InvalidOperationException(
                        $"Destroyed wall {position} has no authored presentation visual.");
                }
                if (session.GetCell(position).Terrain != GridTerrain.Floor)
                {
                    throw new InvalidOperationException(
                        $"Destroyed wall {position} must be logical floor before presentation updates.");
                }

                visual.SetActive(false);
            }
        }
    }
}
