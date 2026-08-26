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
        private PrototypeBombRewardCatalogAsset bombRewardCatalog;

        [SerializeField]
        private PrototypePlayerVitalsAsset playerVitals;

        [SerializeField]
        private bool requireInitialSceneMatch = true;

        private PrototypeDungeonRunNavigator _navigator;
        private int _releaseSeedSequence;

        public event Action RoomCommitted;

        public event Action<PrototypeDungeonPendingTransition> TransitionStarted;

        public bool IsPrimary { get; private set; }

        public int Seed => seed;

        public PrototypeDungeonCombatRoomCatalogAsset CombatRoomCatalog =>
            combatRoomCatalog;

        public PrototypeDungeonSpecialRoomCatalogAsset SpecialRoomCatalog =>
            specialRoomCatalog;

        public PrototypeBombRewardCatalogAsset BombRewardCatalog =>
            bombRewardCatalog;

        public PrototypePlayerVitalsAsset PlayerVitals => playerVitals;

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
            PrototypeBombRewardCatalogAsset authoredBombRewardCatalog,
            PrototypePlayerVitalsAsset authoredPlayerVitals,
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
            bombRewardCatalog = authoredBombRewardCatalog ??
                throw new ArgumentNullException(nameof(authoredBombRewardCatalog));
            playerVitals = authoredPlayerVitals ??
                throw new ArgumentNullException(nameof(authoredPlayerVitals));
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

        public bool TryFailCurrentRun(PlayerDamageResult fatalDamage)
        {
            RequirePrimary();
            return RunSession.TryFail(fatalDamage);
        }

        public void RestartFinishedRun()
        {
            RequirePrimary();
            if (_navigator.HasPendingTransition)
            {
                throw new InvalidOperationException(
                    "A dungeon run cannot restart during a pending room transition.");
            }
            if (!RunSession.IsFinished)
            {
                throw new InvalidOperationException(
                    "Only a completed or failed dungeon run can restart.");
            }

            PrototypeDungeonRunSession restartedSession = CreateRunSession(
                SelectNextRunSeed());
            if (!restartedSession.TryGetCurrentSceneName(out string startSceneName) ||
                !Application.CanStreamedLevelBeLoaded(startSceneName))
            {
                throw new InvalidOperationException(
                    $"Restart scene '{startSceneName}' is not loadable.");
            }

            PrototypeDungeonRunNavigator previousNavigator = _navigator;
            _navigator = new PrototypeDungeonRunNavigator(restartedSession);
            try
            {
                LogActiveSeed(restartedSession);
                WebGlHarnessReporter.Report("dungeon-run-restarted");
                SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
            }
            catch
            {
                _navigator = previousNavigator;
                throw;
            }
        }

        public void ExitFinishedRunToScene(string sceneName)
        {
            RequirePrimary();
            if (_navigator.HasPendingTransition)
            {
                throw new InvalidOperationException(
                    "A dungeon run cannot exit during a pending room transition.");
            }
            if (!RunSession.IsFinished)
            {
                throw new InvalidOperationException(
                    "Only a completed or failed dungeon run can exit to the lobby.");
            }
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException(
                    "Exit scene name cannot be empty.",
                    nameof(sceneName));
            }
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException(
                    $"Exit scene '{sceneName}' is not loadable.");
            }

            WebGlHarnessReporter.Report("run-lobby-requested");
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            Destroy(gameObject);
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

            if (combatRoomCatalog == null || specialRoomCatalog == null ||
                bombRewardCatalog == null || playerVitals == null)
            {
                throw new InvalidOperationException(
                    "PrototypeDungeonRunHost requires combat, special-room, bomb-reward, and player-vitals assets.");
            }

            PrototypeDungeonRunSession runSession = CreateRunSession(
                SelectNextRunSeed());
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
            LogActiveSeed(runSession);
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

        private int SelectNextRunSeed()
        {
            if (Debug.isDebugBuild)
            {
                return seed;
            }

            _releaseSeedSequence++;
            return PrototypeDungeonRunSeedPolicy.CreateReleaseSeed(
                DateTime.UtcNow.Ticks,
                Environment.TickCount,
                _releaseSeedSequence);
        }

        private void LogActiveSeed(PrototypeDungeonRunSession runSession)
        {
            Debug.Log($"[DungeonRun] Active seed: {runSession.Seed}.", this);
        }

        private PrototypeDungeonRunSession CreateRunSession(int runSeed)
        {
            return new PrototypeDungeonRunSession(
                runSeed,
                combatRoomCatalog,
                specialRoomCatalog,
                bombRewardCatalog,
                playerVitals.CreateCoreDefinition());
        }
    }

    internal static class PrototypeDungeonRunSeedPolicy
    {
        internal static int CreateReleaseSeed(
            long utcTicks,
            int environmentTickCount,
            int sequence)
        {
            unchecked
            {
                ulong ticks = (ulong)utcTicks;
                uint mixed = (uint)ticks ^ (uint)(ticks >> 32);
                mixed ^= (uint)environmentTickCount;
                mixed ^= (uint)sequence * 0x9E3779B9u;
                mixed ^= mixed >> 16;
                mixed *= 0x7FEB352Du;
                mixed ^= mixed >> 15;
                mixed *= 0x846CA68Bu;
                mixed ^= mixed >> 16;
                return mixed == 0u ? 1 : (int)mixed;
            }
        }
    }
}
