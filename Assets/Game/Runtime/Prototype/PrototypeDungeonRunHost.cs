using System;
using BombSwap.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombSwap
{
    [DefaultExecutionOrder(-2000)]
    [DisallowMultipleComponent]
    public sealed class PrototypeDungeonRunHost : MonoBehaviour
    {
        [SerializeField]
        private int seed;

        [SerializeField]
        private PrototypeDungeonCombatRoomCatalogAsset combatRoomCatalog;

        [SerializeField]
        private PrototypeDungeonSpecialRoomCatalogAsset specialRoomCatalog;

        [SerializeField]
        private bool requireInitialSceneMatch = true;

        private PrototypeDungeonRunNavigator _navigator;

        public event Action RoomCommitted;

        public event Action<PrototypeDungeonPendingTransition> TransitionStarted;

        public bool IsPrimary { get; private set; }

        public int Seed => seed;

        public PrototypeDungeonCombatRoomCatalogAsset CombatRoomCatalog =>
            combatRoomCatalog;

        public PrototypeDungeonSpecialRoomCatalogAsset SpecialRoomCatalog =>
            specialRoomCatalog;

        public bool RequireInitialSceneMatch => requireInitialSceneMatch;

        public PrototypeDungeonRunSession RunSession =>
            _navigator != null ? _navigator.RunSession : null;

        public bool HasPendingTransition =>
            _navigator != null && _navigator.HasPendingTransition;

        public PrototypeDungeonPendingTransition PendingTransition =>
            _navigator != null
                ? _navigator.PendingTransition
                : throw new InvalidOperationException("Dungeon run host is not initialized.");

        public void Configure(
            int authoredSeed,
            PrototypeDungeonCombatRoomCatalogAsset authoredCombatRoomCatalog,
            PrototypeDungeonSpecialRoomCatalogAsset authoredSpecialRoomCatalog,
            bool authoredRequireInitialSceneMatch = true)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "Disable PrototypeDungeonRunHost before changing its configuration.");
            }
            combatRoomCatalog = authoredCombatRoomCatalog ??
                throw new ArgumentNullException(nameof(authoredCombatRoomCatalog));
            specialRoomCatalog = authoredSpecialRoomCatalog ??
                throw new ArgumentNullException(nameof(authoredSpecialRoomCatalog));
            seed = authoredSeed;
            requireInitialSceneMatch = authoredRequireInitialSceneMatch;
        }

        public PrototypeDungeonTransitionStartResult RequestTravel(
            RoomExitDirection direction)
        {
            RequirePrimary();
            PrototypeDungeonTransitionStartResult result = _navigator.TryBeginTravel(
                direction,
                Application.CanStreamedLevelBeLoaded);
            if (!result.Started)
            {
                return result;
            }

            try
            {
                TransitionStarted?.Invoke(result.Transition);
                WebGlHarnessReporter.Report("dungeon-transition-started");
                SceneManager.LoadScene(
                    result.Transition.TargetSceneName,
                    LoadSceneMode.Single);
            }
            catch
            {
                _navigator.CancelPendingTransition();
                throw;
            }
            return result;
        }

        public DungeonRoomClearStatus TryClearCurrentRoom()
        {
            RequirePrimary();
            return RunSession.TryClearCurrentRoom();
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (transform.parent != null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonRunHost must be on a root GameObject.");
            }

            PrototypeDungeonRunHost[] hosts = FindObjectsByType<PrototypeDungeonRunHost>(
                FindObjectsInactive.Include);
            for (int index = 0; index < hosts.Length; index++)
            {
                PrototypeDungeonRunHost host = hosts[index];
                if (host != this && host.IsPrimary)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (combatRoomCatalog == null || specialRoomCatalog == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonRunHost requires combat and special room catalogs.");
            }

            var runSession = new PrototypeDungeonRunSession(
                seed,
                combatRoomCatalog,
                specialRoomCatalog);
            _navigator = new PrototypeDungeonRunNavigator(runSession);
            if (requireInitialSceneMatch)
            {
                string activeSceneName = SceneManager.GetActiveScene().name;
                if (!runSession.TryGetCurrentSceneName(out string expectedSceneName) ||
                    !string.Equals(activeSceneName, expectedSceneName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Initial dungeon scene '{activeSceneName}' must match Start scene '{expectedSceneName}'.");
                }
            }

            IsPrimary = true;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (IsPrimary)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
            IsPrimary = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (!IsPrimary || !_navigator.HasPendingTransition)
            {
                return;
            }

            PrototypeDungeonTransitionCommitStatus status =
                _navigator.CommitLoadedScene(scene.name);
            if (status != PrototypeDungeonTransitionCommitStatus.Committed)
            {
                Debug.LogError(
                    $"Dungeon transition commit failed for scene '{scene.name}': {status}.",
                    this);
                return;
            }

            WebGlHarnessReporter.Report("dungeon-room-committed");
            RoomCommitted?.Invoke();
        }

        private void RequirePrimary()
        {
            if (!IsPrimary || _navigator == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonRunHost is not the active run host.");
            }
        }
    }
}
