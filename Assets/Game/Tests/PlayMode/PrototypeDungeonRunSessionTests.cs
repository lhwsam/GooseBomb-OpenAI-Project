using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BombSwap.Tests.PlayMode
{
    public sealed class PrototypeDungeonRunSessionTests
    {
        private readonly List<ScriptableObject> _createdAssets =
            new List<ScriptableObject>();
        private readonly List<GameObject> _createdGameObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdGameObjects.Count - 1; index >= 0; index--)
            {
                if (_createdGameObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdGameObjects[index]);
                }
            }
            _createdGameObjects.Clear();
            for (int index = _createdAssets.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(_createdAssets[index]);
            }
            _createdAssets.Clear();
        }

        [Test]
        public void Session_CreatesVersionedCoreRunAndResolvesEveryCombatSelection()
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalog();
            var session = new PrototypeDungeonRunSession(0, catalog);

            Assert.That(session.Seed, Is.Zero);
            Assert.That(session.Graph.Seed, Is.Zero);
            Assert.That(session.RunState.Graph, Is.SameAs(session.Graph));
            Assert.That(session.Outcome, Is.EqualTo(DungeonRunOutcome.InProgress));
            Assert.That(session.IsFinished, Is.False);
            Assert.That(
                session.CombatRoomLayout.AssignmentVersion,
                Is.EqualTo(DungeonCombatRoomAssigner.AssignmentVersion));
            foreach (DungeonRoomNode room in session.Graph.Rooms)
            {
                bool found = session.TryGetCombatRoom(
                    room.Id,
                    out PrototypeDungeonCombatRoomSelection selection);
                if (room.RoomType != RoomType.Combat)
                {
                    Assert.That(found, Is.False);
                    continue;
                }

                Assert.That(found, Is.True);
                Assert.That(selection.RoomDefinition, Is.Not.Null);
                Assert.That(selection.SceneName, Is.Not.Empty);
                Assert.That(selection.Assignment.RoomId, Is.EqualTo(room.Id));
                Assert.That(
                    selection.RoomDefinition.CreateCoreDefinition().Id,
                    Is.EqualTo(selection.Assignment.DefinitionId));
            }
        }

        [Test]
        public void Session_DelegatesLockedClearAndBidirectionalTravelToCoreState()
        {
            var session = new PrototypeDungeonRunSession(12, CreateCatalog());
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];

            Assert.That(session.TryGetCurrentCombatRoom(out _), Is.False);
            Assert.That(session.TryTravelTo(firstCombat).Moved, Is.True);
            Assert.That(session.TryGetCurrentCombatRoom(out var selection), Is.True);
            Assert.That(selection.Assignment.RoomId, Is.EqualTo(firstCombat));
            Assert.That(
                session.TryTravelTo(session.Graph.StartRoomId).Status,
                Is.EqualTo(DungeonTravelStatus.BlockedByUnclearedRoom));
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                session.TryTravelTo(session.Graph.StartRoomId).Moved,
                Is.True);
            Assert.That(session.RunState.IsCleared(firstCombat), Is.True);
        }

        [Test]
        public void Session_CompletesOnlyAfterBossRoomIsCleared()
        {
            var session = new PrototypeDungeonRunSession(
                19,
                CreateCatalog(),
                CreateSpecialCatalog(),
                CreateBombRewardCatalog());
            IReadOnlyList<DungeonRoomNodeId> path = session.Graph.GetShortestPath(
                session.Graph.StartRoomId,
                session.Graph.BossRoomId);

            Assert.That(session.IsComplete, Is.False);
            for (int index = 1; index < path.Count; index++)
            {
                DungeonRoomNode current = session.Graph.GetRoom(session.CurrentRoomId);
                if (DungeonRunState.RequiresClear(current.RoomType))
                {
                    Assert.That(
                        session.TryClearCurrentRoom(),
                        Is.EqualTo(DungeonRoomClearStatus.Cleared));
                }
                Assert.That(session.TryTravelTo(path[index]).Moved, Is.True);
                Assert.That(session.IsComplete, Is.False);
            }

            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.BossRoomId));
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.Outcome, Is.EqualTo(DungeonRunOutcome.Completed));
        }

        [Test]
        public void Session_FailureIsTerminalAndCannotBecomeCompletion()
        {
            var session = new PrototypeDungeonRunSession(
                31,
                CreateCatalog(),
                CreateSpecialCatalog(),
                CreateBombRewardCatalog());

            PlayerDamageResult fatal = CreateFatalContactDamage();
            Assert.That(session.TryFail(fatal), Is.True);
            Assert.That(session.TryFail(fatal), Is.False);
            Assert.That(session.IsFailed, Is.True);
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.IsComplete, Is.False);
            Assert.That(session.FailureDamage, Is.EqualTo(fatal));
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.RunFinished));
            Assert.That(
                session.TryTravelTo(session.Graph.GetNeighbors(session.CurrentRoomId)[0]).Status,
                Is.EqualTo(DungeonTravelStatus.RunFinished));
        }

        [Test]
        public void DeathCauseFormatter_MapsCoreSourceAndPrototypeEnemyActors()
        {
            ActorId chaser = new ActorId(2);
            ActorId charger = new ActorId(3);
            ActorId armored = new ActorId(4);

            Assert.That(
                PrototypePlayerDeathCauseFormatter.Resolve(
                    PlayerDamageSourceKind.Explosion,
                    default,
                    chaser,
                    charger,
                    armored),
                Is.EqualTo(PrototypePlayerDeathCause.BombExplosion));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.Resolve(
                    PlayerDamageSourceKind.EnemyContact,
                    chaser,
                    chaser,
                    charger,
                    armored),
                Is.EqualTo(PrototypePlayerDeathCause.ChaserContact));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.Resolve(
                    PlayerDamageSourceKind.EnemyContact,
                    charger,
                    chaser,
                    charger,
                    armored),
                Is.EqualTo(PrototypePlayerDeathCause.ChargerCharge));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.Resolve(
                    PlayerDamageSourceKind.EnemyContact,
                    armored,
                    chaser,
                    charger,
                    armored),
                Is.EqualTo(PrototypePlayerDeathCause.ArmoredContact));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.Resolve(
                    PlayerDamageSourceKind.EnemyContact,
                    new ActorId(99),
                    chaser,
                    charger,
                    armored),
                Is.EqualTo(PrototypePlayerDeathCause.EnemyContact));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.Resolve(
                    PlayerDamageSourceKind.BossPattern,
                    new ActorId(5),
                    chaser,
                    charger,
                    armored),
                Is.EqualTo(PrototypePlayerDeathCause.BossAttack));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.GetDisplayText(
                    PrototypePlayerDeathCause.BombExplosion),
                Is.EqualTo("CAUSE: BOMB EXPLOSION"));
            Assert.That(
                PrototypePlayerDeathCauseFormatter.GetHarnessEvent(
                    PrototypePlayerDeathCause.BombExplosion),
                Is.EqualTo("run-failed-cause-bomb-explosion"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PrototypePlayerDeathCauseFormatter.Resolve(
                    (PlayerDamageSourceKind)999,
                    default,
                    chaser,
                    charger,
                    armored));
        }

        [Test]
        public void Session_ExposesGraphDoorsThatMatchCombatAssignmentAndClearState()
        {
            var session = new PrototypeDungeonRunSession(41, CreateCatalog());
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];
            Assert.That(session.TryTravelTo(firstCombat).Moved, Is.True);
            Assert.That(
                session.TryGetCurrentCombatRoom(out var selection),
                Is.True);

            IReadOnlyList<DungeonRoomExitState> locked =
                session.GetCurrentExitStates();
            Assert.That(
                locked.Where(exit => exit.IsConnected).Select(exit => exit.Direction),
                Is.EqualTo(selection.Assignment.ActiveExitDirections));
            Assert.That(
                locked.Where(exit => exit.IsConnected).Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Locked));

            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                session.GetCurrentExitStates()
                    .Where(exit => exit.IsConnected)
                    .Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Open));
        }

        [Test]
        public void DoorPresenter_MapsGraphExitStatesBackToRotatedAuthoredDoors()
        {
            var session = new PrototypeDungeonRunSession(
                41,
                CreateCatalog(),
                CreateSpecialCatalog());
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];
            Assert.That(session.TryTravelTo(firstCombat).Moved, Is.True);
            Assert.That(
                session.TryGetCurrentCombatRoom(out var selection),
                Is.True);

            PrototypeDungeonDoorPresenter presenter = CreateDoorPresenter();
            presenter.Apply(
                session.GetCurrentExitStates(),
                selection.Assignment.Rotation);

            AssertDisplayedStatusesMatchGraph(
                presenter,
                session,
                selection.Assignment.Rotation,
                DungeonRoomExitStatus.Locked);
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));

            presenter.Apply(
                session.GetCurrentExitStates(),
                selection.Assignment.Rotation);

            AssertDisplayedStatusesMatchGraph(
                presenter,
                session,
                selection.Assignment.Rotation,
                DungeonRoomExitStatus.Open);
        }

        [Test]
        public void DoorPresenter_RejectsSharedOrMissingDirectionRenderers()
        {
            GameObject root = CreateGameObject("DungeonDoorPresenter");
            PrototypeDungeonDoorPresenter presenter =
                root.AddComponent<PrototypeDungeonDoorPresenter>();
            Renderer north = CreateDoorRenderer("NorthDoor");
            Renderer east = CreateDoorRenderer("EastDoor");
            Renderer south = CreateDoorRenderer("SouthDoor");

            Assert.Throws<ArgumentNullException>(() =>
                presenter.Configure(north, east, south, null));
            Assert.Throws<InvalidOperationException>(() =>
                presenter.Configure(north, east, south, north));
        }

        [UnityTest]
        public IEnumerator ClearedCombatScene_ReentryDoesNotRespawnEnemiesOrRelockDoors()
        {
            Scene loadedDungeonScene = default;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "DungeonStart",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRunHost host =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                            FindObjectsInactive.Include)
                        .Single(candidate => candidate.IsPrimary);
                PrototypeDungeonRunSession run = host.RunSession;
                DungeonRoomNodeId startRoom = run.Graph.StartRoomId;
                DungeonRoomNodeId firstCombat =
                    run.Graph.GetNeighbors(startRoom)[0];
                DungeonTravelResult firstEntry = run.TryTravelTo(firstCombat);
                Assert.That(firstEntry.Moved, Is.True);
                Assert.That(firstEntry.EnteredFirstTime, Is.True);
                Assert.That(
                    run.TryGetSceneName(firstCombat, out string combatSceneName),
                    Is.True);

                yield return SceneManager.LoadSceneAsync(
                    combatSceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder firstBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(firstBinder.RoomSession.IsCombatEnabledByDefault, Is.True);
                Assert.That(firstBinder.RoomSession.HasChaser, Is.True);
                Assert.That(firstBinder.RoomSession.EnemyActiveCount, Is.GreaterThan(0));
                Assert.That(firstBinder.RoomSession.IsRoomCleared, Is.False);
                AssertDisplayedStatusesMatchGraph(
                    firstBinder.DoorPresenter,
                    run,
                    firstBinder.RoomRotation,
                    DungeonRoomExitStatus.Locked);

                Assert.That(
                    run.TryClearCurrentRoom(),
                    Is.EqualTo(DungeonRoomClearStatus.Cleared));
                Assert.That(run.TryTravelTo(startRoom).Moved, Is.True);
                yield return SceneManager.LoadSceneAsync(
                    "DungeonStart",
                    LoadSceneMode.Single);
                yield return null;

                DungeonTravelResult reentry = run.TryTravelTo(firstCombat);
                Assert.That(reentry.Moved, Is.True);
                Assert.That(reentry.EnteredFirstTime, Is.False);
                Assert.That(run.RunState.IsCleared(firstCombat), Is.True);
                yield return SceneManager.LoadSceneAsync(
                    combatSceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder reentryBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession reentrySession = reentryBinder.RoomSession;
                Assert.That(reentrySession.IsCombatEnabledByDefault, Is.True);
                Assert.That(reentrySession.HasChaser, Is.False);
                Assert.That(reentrySession.IsChaserAlive, Is.False);
                Assert.That(reentrySession.HasCharger, Is.False);
                Assert.That(reentrySession.HasArmored, Is.False);
                Assert.That(reentrySession.EnemyActiveCount, Is.Zero);
                Assert.That(reentrySession.IsRoomCleared, Is.True);
                Assert.That(run.RunState.IsRoomLocked(firstCombat), Is.False);
                PrototypeChaserPresenter reentryChaser =
                    UnityEngine.Object.FindObjectsByType<PrototypeChaserPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(reentryChaser.IsInitialized, Is.True);
                Assert.That(reentryChaser.Instance, Is.Null);
                AssertDisplayedStatusesMatchGraph(
                    reentryBinder.DoorPresenter,
                    run,
                    reentryBinder.RoomRotation,
                    DungeonRoomExitStatus.Open);

                loadedDungeonScene = SceneManager.GetActiveScene();
            }
            finally
            {
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedDungeonScene.IsValid())
                {
                    loadedDungeonScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene(
                    "DungeonReentryPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GatesCombatScene_LoadsAuthoredLogicalAndVisualGatePair()
        {
            Scene loadedDungeonScene = default;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "DungeonStart",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRunHost host =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                            FindObjectsInactive.Include)
                        .Single(candidate => candidate.IsPrimary);
                PrototypeDungeonRunSession run = host.RunSession;
                DungeonCombatRoomAssignment gatesAssignment =
                    run.CombatRoomLayout.Assignments.Single(assignment =>
                        assignment.DefinitionId ==
                        new RoomDefinitionId("prototype-combat-gates"));
                Assert.That(gatesAssignment.Rotation, Is.EqualTo(RoomRotation.None));

                IReadOnlyList<DungeonRoomNodeId> path =
                    run.Graph.GetShortestPath(
                        run.Graph.StartRoomId,
                        gatesAssignment.RoomId);
                for (int index = 1; index < path.Count; index++)
                {
                    DungeonRoomNode current = run.Graph.GetRoom(run.CurrentRoomId);
                    if (DungeonRunState.RequiresClear(current.RoomType) &&
                        !run.RunState.IsCleared(current.Id))
                    {
                        Assert.That(
                            run.TryClearCurrentRoom(),
                            Is.EqualTo(DungeonRoomClearStatus.Cleared));
                    }
                    Assert.That(run.TryTravelTo(path[index]).Moved, Is.True);
                }

                Assert.That(
                    run.TryGetCurrentSceneName(out string sceneName),
                    Is.True);
                Assert.That(sceneName, Is.EqualTo("TestSandboxGates"));
                yield return SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder binder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession session = binder.RoomSession;
                TestSandboxContext context = session.Context;
                CombatRoomDefinition room = context.RoomDefinition.CreateCoreDefinition();
                PrototypeDestructibleWallPresenter wallPresenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeDestructibleWallPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeHealthHud healthHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();

                Assert.That(session.IsReady, Is.True);
                Assert.That(session.HasChaser, Is.True);
                Assert.That(session.HasCharger, Is.False);
                Assert.That(session.HasArmored, Is.False);
                Assert.That(room.Id, Is.EqualTo(gatesAssignment.DefinitionId));
                Assert.That(room.PlayerSpawn, Is.EqualTo(new GridPosition(0, -3)));
                Assert.That(room.ChaserSpawn, Is.EqualTo(new GridPosition(0, 3)));
                Assert.That(room.IndestructibleWalls, Has.Count.EqualTo(8));
                Assert.That(room.DestructibleWalls, Has.Count.EqualTo(2));
                Assert.That(
                    session.GetCell(new GridPosition(0, -1)).Terrain,
                    Is.EqualTo(GridTerrain.DestructibleWall));
                Assert.That(
                    session.GetCell(new GridPosition(0, 1)).Terrain,
                    Is.EqualTo(GridTerrain.DestructibleWall));
                Assert.That(wallPresenter.ActiveWallVisualCount, Is.EqualTo(2));
                Assert.That(healthHud.IsInitialized, Is.True);
                Assert.That(healthHud.IsBossPanelVisible, Is.False);
                Assert.That(run.CombatRewardTokenCount, Is.GreaterThan(0));
                Assert.That(
                    healthHud.DisplayedCombatRewardTokenCount,
                    Is.EqualTo(run.CombatRewardTokenCount));
                Assert.That(
                    healthHud.CombatRewardText,
                    Is.EqualTo("ROOM TOKENS  " + run.CombatRewardTokenCount));

                loadedDungeonScene = SceneManager.GetActiveScene();
            }
            finally
            {
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedDungeonScene.IsValid())
                {
                    loadedDungeonScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene(
                    "GatesCombatPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DungeonBossScene_AuthorsBossInsteadOfRegularCombatEnemies()
        {
            Scene loadedDungeonScene = default;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "DungeonStart",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRunHost host =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                            FindObjectsInactive.Include)
                        .Single(candidate => candidate.IsPrimary);
                PrototypeDungeonRunSession run = host.RunSession;
                IReadOnlyList<DungeonRoomNodeId> path = run.Graph.GetShortestPath(
                    run.Graph.StartRoomId,
                    run.Graph.BossRoomId);
                for (int index = 1; index < path.Count; index++)
                {
                    DungeonRoomNode current = run.Graph.GetRoom(run.CurrentRoomId);
                    if (DungeonRunState.RequiresClear(current.RoomType) &&
                        !run.RunState.IsCleared(current.Id))
                    {
                        Assert.That(
                            run.TryClearCurrentRoom(),
                            Is.EqualTo(DungeonRoomClearStatus.Cleared));
                    }
                    Assert.That(run.TryTravelTo(path[index]).Moved, Is.True);
                }

                Assert.That(run.CurrentRoomId, Is.EqualTo(run.Graph.BossRoomId));
                yield return SceneManager.LoadSceneAsync(
                    "DungeonBoss",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder binder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession session = binder.RoomSession;
                PrototypeBossPresenter presenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeBossPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeRunCompletionPresenter completionPresenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeRunCompletionPresenter>(
                            FindObjectsInactive.Include)
                        .Single();

                Assert.That(binder.RuntimeRoomType, Is.EqualTo(RoomType.Boss));
                Assert.That(session.IsCombatEnabledByDefault, Is.True);
                Assert.That(session.IsBossEnabledByDefault, Is.True);
                Assert.That(session.HasBoss, Is.True);
                Assert.That(session.HasChaser, Is.False);
                Assert.That(session.HasCharger, Is.False);
                Assert.That(session.HasArmored, Is.False);
                Assert.That(session.BossActorId, Is.EqualTo(new ActorId(5)));
                Assert.That(session.EnemyActiveCount, Is.EqualTo(1));
                Assert.That(session.IsRoomCleared, Is.False);
                Assert.That(presenter.IsInitialized, Is.True);
                Assert.That(presenter.IsBossVisible, Is.True);
                Assert.That(presenter.VisibleDangerCellCount, Is.GreaterThan(0));
                Assert.That(completionPresenter.RoomBinder, Is.SameAs(binder));
                Assert.That(completionPresenter.InputReader, Is.SameAs(session.InputReader));
                Assert.That(completionPresenter.IsVisible, Is.False);
                AssertDisplayedStatusesMatchGraph(
                    binder.DoorPresenter,
                    run,
                    binder.RoomRotation,
                    DungeonRoomExitStatus.Locked);

                loadedDungeonScene = SceneManager.GetActiveScene();
            }
            finally
            {
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedDungeonScene.IsValid())
                {
                    loadedDungeonScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene(
                    "DungeonBossPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BombRewardScene_CollectsEmptySecondSlotAndPersistsAcrossSceneLoad()
        {
            Scene loadedDungeonScene = default;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "DungeonStart",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRunHost host =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                            FindObjectsInactive.Include)
                        .Single(candidate => candidate.IsPrimary);
                PrototypeDungeonRunSession run = host.RunSession;
                IReadOnlyList<DungeonRoomNodeId> path = run.Graph.GetShortestPath(
                    run.Graph.StartRoomId,
                    run.Graph.BombRewardRoomId);
                DungeonRoomNodeId firstCombat = path[1];
                Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);
                Assert.That(
                    run.TryClearCurrentRoom(),
                    Is.EqualTo(DungeonRoomClearStatus.Cleared));
                Assert.That(run.TryTravelTo(path[2]).Moved, Is.True);

                yield return SceneManager.LoadSceneAsync(
                    "DungeonReward",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder rewardBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeBombRewardPresenter rewardPresenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeBombRewardPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession rewardSession = rewardBinder.RoomSession;
                Assert.That(rewardPresenter.IsInitialized, Is.True);
                Assert.That(rewardPresenter.CandidateVisualCount, Is.EqualTo(2));
                Assert.That(rewardSession.GetBombSlot(0).HasDefinition, Is.True);
                Assert.That(
                    rewardSession.GetBombSlot(0).DefinitionId.Value,
                    Is.EqualTo("prototype-cross"));
                Assert.That(rewardSession.GetBombSlot(1).HasDefinition, Is.False);
                Assert.That(rewardSession.HasSecondBombSlot, Is.False);

                Assert.That(
                    rewardPresenter.TryCollectAt(new GridPosition(-1, 0)),
                    Is.True);

                BombDefinitionId selected = new BombDefinitionId("prototype-area");
                Assert.That(rewardPresenter.SelectedDefinitionId, Is.EqualTo(selected));
                Assert.That(run.BombLoadoutState.SecondSlot, Is.EqualTo(selected));
                Assert.That(rewardSession.HasSecondBombSlot, Is.True);
                Assert.That(
                    rewardSession.GetBombSlot(1).DefinitionId,
                    Is.EqualTo(selected));

                Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);
                Assert.That(
                    run.TryGetSceneName(firstCombat, out string combatSceneName),
                    Is.True);
                yield return SceneManager.LoadSceneAsync(
                    combatSceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeGameSession nextSession =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single()
                        .RoomSession;
                Assert.That(nextSession.HasSecondBombSlot, Is.True);
                Assert.That(nextSession.GetBombSlot(1).DefinitionId, Is.EqualTo(selected));
                loadedDungeonScene = SceneManager.GetActiveScene();
            }
            finally
            {
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedDungeonScene.IsValid())
                {
                    loadedDungeonScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene(
                    "DungeonBombRewardPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [Test]
        public void SpecialCatalog_RequiresEveryUniqueNonCombatTypeAndScene()
        {
            PrototypeDungeonSpecialRoomCatalogAsset catalog =
                CreateSpecialCatalogAsset();
            PrototypeDungeonSpecialRoomEntry[] valid = CreateSpecialEntries();

            catalog.Configure(valid);
            valid[0] = default;

            Assert.That(catalog.GetSceneName(RoomType.Start), Is.EqualTo("DungeonStart"));
            Assert.That(
                catalog.GetSceneName(RoomType.BombReward),
                Is.EqualTo("DungeonReward"));
            Assert.That(
                catalog.GetSceneName(RoomType.BossAntechamber),
                Is.EqualTo("DungeonBossAnte"));
            Assert.That(catalog.GetSceneName(RoomType.Boss), Is.EqualTo("DungeonBoss"));
            Assert.Throws<ArgumentNullException>(() => catalog.Configure(null));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(Array.Empty<PrototypeDungeonSpecialRoomEntry>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Combat, "Combat"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.BombReward, "Reward"),
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.BossAntechamber,
                        "Ante"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "Boss"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "Same"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.BombReward, "Same"),
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.BossAntechamber,
                        "Ante"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "Boss"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "Start"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "OtherStart"),
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.BossAntechamber,
                        "Ante"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "Boss"),
                }));
        }

        [Test]
        public void Session_ResolvesCombatAndEveryRequiredSpecialScene()
        {
            var session = new PrototypeDungeonRunSession(
                73,
                CreateCatalog(),
                CreateSpecialCatalog());

            foreach (DungeonRoomNode room in session.Graph.Rooms)
            {
                Assert.That(
                    session.TryGetSceneName(room.Id, out string sceneName),
                    Is.True,
                    room.ToString());
                Assert.That(sceneName, Is.Not.Empty);
                if (room.RoomType != RoomType.Combat)
                {
                    Assert.That(
                        sceneName,
                        Is.EqualTo(ExpectedSpecialScene(room.RoomType)));
                }
            }
            Assert.That(
                session.TryGetCurrentSceneName(out string currentScene),
                Is.True);
            Assert.That(currentScene, Is.EqualTo("DungeonStart"));

            var withoutSpecialCatalog = new PrototypeDungeonRunSession(73, CreateCatalog());
            Assert.That(withoutSpecialCatalog.TryGetCurrentSceneName(out _), Is.False);
        }

        [Test]
        public void BombRewardCatalog_ClonesCandidatesAndBuildsSingleSlotRunState()
        {
            PrototypeBombDefinitionAsset starter = CreateBombDefinition("starter");
            PrototypeBombDefinitionAsset area = CreateBombDefinition(
                "area",
                BombExplosionShape.SquareArea);
            PrototypeBombDefinitionAsset line = CreateBombDefinition(
                "line",
                BombExplosionShape.ForwardLine);
            PrototypeBombRewardCatalogAsset catalog = CreateBombRewardCatalogAsset();
            var source = new[] { area, line };

            catalog.Configure(starter, source, 2f);
            source[0] = starter;
            DungeonBombLoadoutState state = catalog.CreateRunLoadoutState();

            Assert.That(catalog.FirstSlot, Is.SameAs(starter));
            Assert.That(catalog.RewardCandidates, Is.EqualTo(new[] { area, line }));
            Assert.That(state.FirstSlot.Value, Is.EqualTo("starter"));
            Assert.That(state.SecondSlot.HasValue, Is.False);
            Assert.That(
                catalog.GetDefinition(new BombDefinitionId("area")),
                Is.SameAs(area));
            Assert.That(catalog.GetAvailableDefinitions().Length, Is.EqualTo(3));
        }

        [Test]
        public void Session_SelectsBombRewardOnlyInRewardRoomAndPersistsIt()
        {
            PrototypeBombRewardCatalogAsset rewardCatalog = CreateBombRewardCatalog();
            var session = new PrototypeDungeonRunSession(
                0,
                CreateCatalog(),
                CreateSpecialCatalog(),
                rewardCatalog);
            BombDefinitionId selected = new BombDefinitionId(
                rewardCatalog.RewardCandidates[1].DefinitionId);

            Assert.That(
                session.TrySelectBombReward(selected),
                Is.EqualTo(DungeonBombRewardSelectionStatus.NotInBombRewardRoom));
            IReadOnlyList<DungeonRoomNodeId> rewardPath = session.Graph.GetShortestPath(
                session.Graph.StartRoomId,
                session.Graph.BombRewardRoomId);
            Assert.That(session.TryTravelTo(rewardPath[1]).Moved, Is.True);
            Assert.That(session.TryClearCurrentRoom(), Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(session.TryTravelTo(rewardPath[2]).Moved, Is.True);
            Assert.That(
                session.TrySelectBombReward(new BombDefinitionId("unknown")),
                Is.EqualTo(DungeonBombRewardSelectionStatus.NotCandidate));

            Assert.That(
                session.TrySelectBombReward(selected),
                Is.EqualTo(DungeonBombRewardSelectionStatus.Selected));
            Assert.That(session.BombLoadoutState.SecondSlot, Is.EqualTo(selected));
            Assert.That(session.TryTravelTo(rewardPath[1]).Moved, Is.True);
            Assert.That(session.TryTravelTo(rewardPath[2]).Moved, Is.True);
            Assert.That(session.BombLoadoutState.SecondSlot, Is.EqualTo(selected));
            Assert.That(
                session.TrySelectBombReward(
                    new BombDefinitionId(rewardCatalog.RewardCandidates[0].DefinitionId)),
                Is.EqualTo(DungeonBombRewardSelectionStatus.AlreadySelected));
        }

        [Test]
        public void BombRewardCatalog_RejectsDuplicateStarterAndInvalidCandidateCounts()
        {
            PrototypeBombDefinitionAsset starter = CreateBombDefinition("starter");
            PrototypeBombDefinitionAsset area = CreateBombDefinition(
                "area",
                BombExplosionShape.SquareArea);
            PrototypeBombRewardCatalogAsset catalog = CreateBombRewardCatalogAsset();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                catalog.Configure(starter, new[] { area }, 2f));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(starter, new[] { starter, area }, 2f));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(starter, new[] { area, area }, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                catalog.Configure(starter, new[] { area, CreateBombDefinition("other") }, 0f));
        }

        [Test]
        public void Navigator_CommitsCoreTravelOnlyAfterExpectedSceneLoads()
        {
            var session = new PrototypeDungeonRunSession(
                17,
                CreateCatalog(),
                CreateSpecialCatalog());
            var navigator = new PrototypeDungeonRunNavigator(session);
            DungeonRoomNodeId firstCombat =
                session.Graph.GetNeighbors(session.Graph.StartRoomId)[0];
            RoomExitDirection direction = session.Graph.GetExitDirection(
                session.Graph.StartRoomId,
                firstCombat);

            PrototypeDungeonTransitionStartResult unavailable =
                navigator.TryBeginTravel(direction, _ => false);

            Assert.That(
                unavailable.Status,
                Is.EqualTo(PrototypeDungeonTransitionStartStatus.SceneNotLoadable));
            Assert.That(navigator.HasPendingTransition, Is.False);
            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.StartRoomId));

            PrototypeDungeonTransitionStartResult started =
                navigator.TryBeginTravel(direction, _ => true);

            Assert.That(started.Started, Is.True);
            Assert.That(navigator.HasPendingTransition, Is.True);
            Assert.That(started.Transition.FromRoomId, Is.EqualTo(session.Graph.StartRoomId));
            Assert.That(started.Transition.TargetRoomId, Is.EqualTo(firstCombat));
            Assert.That(
                started.Transition.EntryDirection,
                Is.EqualTo(Opposite(direction)));
            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.StartRoomId));
            Assert.That(
                navigator.TryBeginTravel(direction, _ => true).Status,
                Is.EqualTo(
                    PrototypeDungeonTransitionStartStatus.TransitionAlreadyPending));
            Assert.That(
                navigator.CommitLoadedScene("WrongScene"),
                Is.EqualTo(PrototypeDungeonTransitionCommitStatus.SceneMismatch));
            Assert.That(session.CurrentRoomId, Is.EqualTo(session.Graph.StartRoomId));
            Assert.That(navigator.HasPendingTransition, Is.True);

            Assert.That(
                navigator.CommitLoadedScene(started.Transition.TargetSceneName),
                Is.EqualTo(PrototypeDungeonTransitionCommitStatus.Committed));
            Assert.That(session.CurrentRoomId, Is.EqualTo(firstCombat));
            Assert.That(navigator.HasPendingTransition, Is.False);
            Assert.That(
                navigator.CommitLoadedScene(started.Transition.TargetSceneName),
                Is.EqualTo(
                    PrototypeDungeonTransitionCommitStatus.NoTransitionPending));

            RoomExitDirection reverse = session.Graph.GetExitDirection(
                firstCombat,
                session.Graph.StartRoomId);
            Assert.That(
                navigator.TryBeginTravel(reverse, _ => true).Status,
                Is.EqualTo(PrototypeDungeonTransitionStartStatus.Locked));
            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(navigator.TryBeginTravel(reverse, _ => true).Started, Is.True);
            navigator.CancelPendingTransition();
            Assert.That(navigator.HasPendingTransition, Is.False);
            Assert.That(session.CurrentRoomId, Is.EqualTo(firstCombat));
            Assert.Throws<ArgumentNullException>(() =>
                navigator.TryBeginTravel(reverse, null));
            Assert.Throws<ArgumentException>(() => navigator.CommitLoadedScene(" "));
        }

        [UnityTest]
        public IEnumerator RunHost_KeepsOneExplicitPersistentPrimary()
        {
            PrototypeDungeonCombatRoomCatalogAsset combatCatalog = CreateCatalog();
            PrototypeDungeonSpecialRoomCatalogAsset specialCatalog =
                CreateSpecialCatalog();
            PrototypeBombRewardCatalogAsset rewardCatalog = CreateBombRewardCatalog();
            GameObject firstRoot = CreateGameObject("FirstDungeonRunHost");
            firstRoot.SetActive(false);
            PrototypeDungeonRunHost first =
                firstRoot.AddComponent<PrototypeDungeonRunHost>();
            first.Configure(5, combatCatalog, specialCatalog, rewardCatalog, false);
            firstRoot.SetActive(true);

            GameObject duplicateRoot = CreateGameObject("DuplicateDungeonRunHost");
            duplicateRoot.SetActive(false);
            PrototypeDungeonRunHost duplicate =
                duplicateRoot.AddComponent<PrototypeDungeonRunHost>();
            duplicate.Configure(5, combatCatalog, specialCatalog, rewardCatalog, false);
            duplicateRoot.SetActive(true);

            yield return null;

            Assert.That(first, Is.Not.Null);
            Assert.That(first.IsPrimary, Is.True);
            Assert.That(first.RunSession, Is.Not.Null);
            Assert.That(first.RunSession.CurrentRoomId, Is.EqualTo(first.RunSession.Graph.StartRoomId));
            Assert.That(duplicate == null || !duplicate.IsPrimary, Is.True);
            Assert.That(
                UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                    FindObjectsInactive.Include).Count(host => host.IsPrimary),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RunHost_RestartsCompletedRunAtStartScene()
        {
            Scene loadedDungeonScene = default;
            PrototypeDungeonRunHost host = null;
            try
            {
                GameObject hostRoot = CreateGameObject("RestartingDungeonRunHost");
                hostRoot.SetActive(false);
                host = hostRoot.AddComponent<PrototypeDungeonRunHost>();
                host.Configure(
                    23,
                    CreateCatalog(),
                    CreateSpecialCatalog(),
                    CreateBombRewardCatalog(),
                    false);
                hostRoot.SetActive(true);
                yield return null;

                PrototypeDungeonRunSession originalSession = host.RunSession;
                IReadOnlyList<DungeonRoomNodeId> path =
                    originalSession.Graph.GetShortestPath(
                        originalSession.Graph.StartRoomId,
                        originalSession.Graph.BossRoomId);
                for (int index = 1; index < path.Count; index++)
                {
                    DungeonRoomNode current =
                        originalSession.Graph.GetRoom(originalSession.CurrentRoomId);
                    if (DungeonRunState.RequiresClear(current.RoomType))
                    {
                        Assert.That(
                            originalSession.TryClearCurrentRoom(),
                            Is.EqualTo(DungeonRoomClearStatus.Cleared));
                    }
                    Assert.That(originalSession.TryTravelTo(path[index]).Moved, Is.True);
                }
                Assert.That(
                    originalSession.TryClearCurrentRoom(),
                    Is.EqualTo(DungeonRoomClearStatus.Cleared));
                Assert.That(originalSession.IsComplete, Is.True);
                Assert.That(originalSession.CombatRewardTokenCount, Is.GreaterThan(0));

                host.RestartFinishedRun();
                yield return null;

                loadedDungeonScene = SceneManager.GetActiveScene();
                Assert.That(loadedDungeonScene.name, Is.EqualTo("DungeonStart"));
                Assert.That(host, Is.Not.Null);
                Assert.That(host.IsPrimary, Is.True);
                Assert.That(host.RunSession, Is.Not.SameAs(originalSession));
                Assert.That(host.RunSession.Seed, Is.EqualTo(23));
                Assert.That(
                    host.RunSession.CurrentRoomId,
                    Is.EqualTo(host.RunSession.Graph.StartRoomId));
                Assert.That(host.RunSession.IsComplete, Is.False);
                Assert.That(host.RunSession.IsFinished, Is.False);
                Assert.That(host.RunSession.CombatRewardTokenCount, Is.Zero);
            }
            finally
            {
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedDungeonScene.IsValid())
                {
                    loadedDungeonScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene("RunRestartPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RunHost_RestartsFailedRunAtStartScene()
        {
            Scene loadedDungeonScene = default;
            try
            {
                GameObject hostRoot = CreateGameObject("FailedDungeonRunHost");
                hostRoot.SetActive(false);
                PrototypeDungeonRunHost host =
                    hostRoot.AddComponent<PrototypeDungeonRunHost>();
                host.Configure(
                    29,
                    CreateCatalog(),
                    CreateSpecialCatalog(),
                    CreateBombRewardCatalog(),
                    false);
                hostRoot.SetActive(true);
                yield return null;

                PrototypeDungeonRunSession originalSession = host.RunSession;
                PlayerDamageResult fatal = CreateFatalContactDamage();
                Assert.That(host.TryFailCurrentRun(fatal), Is.True);
                Assert.That(host.TryFailCurrentRun(fatal), Is.False);
                Assert.That(originalSession.IsFailed, Is.True);
                Assert.That(originalSession.FailureDamage, Is.EqualTo(fatal));

                host.RestartFinishedRun();
                yield return null;

                loadedDungeonScene = SceneManager.GetActiveScene();
                Assert.That(loadedDungeonScene.name, Is.EqualTo("DungeonStart"));
                Assert.That(host, Is.Not.Null);
                Assert.That(host.IsPrimary, Is.True);
                Assert.That(host.RunSession, Is.Not.SameAs(originalSession));
                Assert.That(host.RunSession.Seed, Is.EqualTo(29));
                Assert.That(host.RunSession.Outcome, Is.EqualTo(DungeonRunOutcome.InProgress));
                Assert.That(host.RunSession.CombatRewardTokenCount, Is.Zero);
            }
            finally
            {
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedDungeonScene.IsValid())
                {
                    loadedDungeonScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene("RunFailureRestartPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [Test]
        public void Catalog_ClonesConfigurationAndLooksUpStableRoomId()
        {
            PrototypeCombatRoomDefinitionAsset room = CreateRoom("room-one");
            var source = new[]
            {
                new PrototypeDungeonCombatRoomEntry(room, "SceneOne"),
            };
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalogAsset();

            catalog.Configure(source);
            source[0] = default;
            PrototypeDungeonCombatRoomEntry stored =
                catalog.GetEntry(new RoomDefinitionId("room-one"));

            Assert.That(stored.RoomDefinition, Is.SameAs(room));
            Assert.That(stored.SceneName, Is.EqualTo("SceneOne"));
            Assert.That(catalog.CreateCoreDefinitions().Single().Id.Value, Is.EqualTo("room-one"));
            Assert.Throws<ArgumentException>(() => catalog.GetEntry(default));
            Assert.Throws<KeyNotFoundException>(() =>
                catalog.GetEntry(new RoomDefinitionId("missing")));
        }

        [Test]
        public void Catalog_RejectsMissingDuplicateOrAmbiguousEntries()
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalogAsset();
            PrototypeCombatRoomDefinitionAsset first = CreateRoom("room-one");
            PrototypeCombatRoomDefinitionAsset duplicateId = CreateRoom("room-one");
            PrototypeCombatRoomDefinitionAsset second = CreateRoom("room-two");

            Assert.Throws<ArgumentNullException>(() => catalog.Configure(null));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(Array.Empty<PrototypeDungeonCombatRoomEntry>()));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(null, "SceneOne"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(first, " "),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(first, "SceneOne"),
                    new PrototypeDungeonCombatRoomEntry(duplicateId, "SceneTwo"),
                }));
            Assert.Throws<ArgumentException>(() =>
                catalog.Configure(new[]
                {
                    new PrototypeDungeonCombatRoomEntry(first, "SceneOne"),
                    new PrototypeDungeonCombatRoomEntry(second, "SceneOne"),
                }));
        }

        [Test]
        public void Session_RejectsMissingOrInvalidCatalogAtConstruction()
        {
            PrototypeDungeonCombatRoomCatalogAsset empty = CreateCatalogAsset();
            PrototypeDungeonSpecialRoomCatalogAsset emptySpecial =
                CreateSpecialCatalogAsset();

            Assert.Throws<ArgumentNullException>(() =>
                new PrototypeDungeonRunSession(0, null));
            Assert.Throws<ArgumentException>(() =>
                new PrototypeDungeonRunSession(0, empty));
            Assert.Throws<ArgumentException>(() =>
                new PrototypeDungeonRunSession(0, CreateCatalog(), emptySpecial));
        }

        private PrototypeDungeonCombatRoomCatalogAsset CreateCatalog()
        {
            PrototypeDungeonCombatRoomCatalogAsset catalog = CreateCatalogAsset();
            catalog.Configure(new[]
            {
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-alpha"), "SceneAlpha"),
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-bravo"), "SceneBravo"),
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-charlie"), "SceneCharlie"),
                new PrototypeDungeonCombatRoomEntry(CreateRoom("room-delta"), "SceneDelta"),
            });
            return catalog;
        }

        private PrototypeDungeonSpecialRoomCatalogAsset CreateSpecialCatalog()
        {
            PrototypeDungeonSpecialRoomCatalogAsset catalog =
                CreateSpecialCatalogAsset();
            catalog.Configure(CreateSpecialEntries());
            return catalog;
        }

        private PrototypeDungeonSpecialRoomCatalogAsset CreateSpecialCatalogAsset()
        {
            var catalog = ScriptableObject.CreateInstance<
                PrototypeDungeonSpecialRoomCatalogAsset>();
            _createdAssets.Add(catalog);
            return catalog;
        }

        private PrototypeBombRewardCatalogAsset CreateBombRewardCatalog()
        {
            PrototypeBombDefinitionAsset starter = CreateBombDefinition("starter");
            PrototypeBombDefinitionAsset area = CreateBombDefinition(
                "area",
                BombExplosionShape.SquareArea);
            PrototypeBombDefinitionAsset line = CreateBombDefinition(
                "line",
                BombExplosionShape.ForwardLine);
            PrototypeBombRewardCatalogAsset catalog = CreateBombRewardCatalogAsset();
            catalog.Configure(starter, new[] { area, line }, 2f);
            return catalog;
        }

        private PrototypeBombRewardCatalogAsset CreateBombRewardCatalogAsset()
        {
            var catalog = ScriptableObject.CreateInstance<
                PrototypeBombRewardCatalogAsset>();
            _createdAssets.Add(catalog);
            return catalog;
        }

        private PrototypeBombDefinitionAsset CreateBombDefinition(
            string id,
            BombExplosionShape shape = BombExplosionShape.Cross)
        {
            var definition = ScriptableObject.CreateInstance<
                PrototypeBombDefinitionAsset>();
            GameObject bombVisual = CreateGameObject(id + "-bomb");
            GameObject explosionVisual = CreateGameObject(id + "-explosion");
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(bombVisual);
                UnityEngine.Object.DontDestroyOnLoad(explosionVisual);
            }
            definition.Configure(
                id,
                2f,
                shape == BombExplosionShape.SquareArea ? 1 : 2,
                bombVisual,
                explosionVisual,
                0.25f,
                1.5f,
                shape);
            _createdAssets.Add(definition);
            return definition;
        }

        private static PrototypeDungeonSpecialRoomEntry[] CreateSpecialEntries()
        {
            return new[]
            {
                new PrototypeDungeonSpecialRoomEntry(RoomType.Start, "DungeonStart"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.BombReward,
                    "DungeonReward"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.BossAntechamber,
                    "DungeonBossAnte"),
                new PrototypeDungeonSpecialRoomEntry(RoomType.Boss, "DungeonBoss"),
            };
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdGameObjects.Add(gameObject);
            return gameObject;
        }

        private PrototypeDungeonDoorPresenter CreateDoorPresenter()
        {
            GameObject root = CreateGameObject("DungeonDoorPresenter");
            PrototypeDungeonDoorPresenter presenter =
                root.AddComponent<PrototypeDungeonDoorPresenter>();
            presenter.Configure(
                CreateDoorRenderer("NorthDoor"),
                CreateDoorRenderer("EastDoor"),
                CreateDoorRenderer("SouthDoor"),
                CreateDoorRenderer("WestDoor"));
            return presenter;
        }

        private Renderer CreateDoorRenderer(string name)
        {
            GameObject door = CreateGameObject(name);
            return door.AddComponent<MeshRenderer>();
        }

        private static void AssertDisplayedStatusesMatchGraph(
            PrototypeDungeonDoorPresenter presenter,
            PrototypeDungeonRunSession session,
            RoomRotation roomRotation,
            DungeonRoomExitStatus expectedConnectedStatus)
        {
            RoomExitDirection[] localDirections =
            {
                RoomExitDirection.North,
                RoomExitDirection.East,
                RoomExitDirection.South,
                RoomExitDirection.West,
            };
            int connectedCount = 0;
            for (int index = 0; index < localDirections.Length; index++)
            {
                RoomExitDirection graphDirection = RoomRotationUtility.Rotate(
                    localDirections[index],
                    roomRotation);
                DungeonRoomExitState graphState =
                    session.RunState.GetCurrentExitState(graphDirection);
                Assert.That(
                    presenter.GetDisplayedStatus(localDirections[index]),
                    Is.EqualTo(graphState.Status),
                    $"Authored {localDirections[index]} maps to graph {graphDirection}.");
                if (graphState.IsConnected)
                {
                    connectedCount++;
                    Assert.That(graphState.Status, Is.EqualTo(expectedConnectedStatus));
                }
            }
            Assert.That(connectedCount, Is.GreaterThan(0));
        }

        private static string ExpectedSpecialScene(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Start:
                    return "DungeonStart";
                case RoomType.BombReward:
                    return "DungeonReward";
                case RoomType.BossAntechamber:
                    return "DungeonBossAnte";
                case RoomType.Boss:
                    return "DungeonBoss";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        null);
            }
        }

        private static PlayerDamageResult CreateFatalContactDamage()
        {
            var health = new PlayerHealthSimulation(
                new ActorId(1),
                new ManualGameClock(),
                new PlayerHealthDefinition(1, TimeSpan.FromSeconds(0.75)));
            return health.ApplyContactDamage(new ActorId(2), 1);
        }

        private static RoomExitDirection Opposite(RoomExitDirection direction)
        {
            return RoomRotationUtility.Rotate(direction, RoomRotation.Clockwise180);
        }

        private PrototypeDungeonCombatRoomCatalogAsset CreateCatalogAsset()
        {
            var catalog = ScriptableObject.CreateInstance<
                PrototypeDungeonCombatRoomCatalogAsset>();
            _createdAssets.Add(catalog);
            return catalog;
        }

        private PrototypeCombatRoomDefinitionAsset CreateRoom(string id)
        {
            var room = ScriptableObject.CreateInstance<PrototypeCombatRoomDefinitionAsset>();
            room.Configure(
                id,
                RoomType.Combat,
                11,
                9,
                1f,
                Vector2Int.zero,
                new Vector2Int(1, -1),
                Array.Empty<Vector2Int>(),
                new[]
                {
                    Vector2Int.zero,
                    new Vector2Int(0, 1),
                    new Vector2Int(-1, 0),
                },
                new[]
                {
                    new Vector2Int(-3, 1),
                    new Vector2Int(3, 1),
                },
                new[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, -1),
                    new Vector2Int(0, -1),
                },
                new[]
                {
                    new PrototypeRoomExitData(
                        new Vector2Int(0, 4),
                        RoomExitDirection.North),
                    new PrototypeRoomExitData(
                        new Vector2Int(5, 0),
                        RoomExitDirection.East),
                    new PrototypeRoomExitData(
                        new Vector2Int(0, -4),
                        RoomExitDirection.South),
                    new PrototypeRoomExitData(
                        new Vector2Int(-5, 0),
                        RoomExitDirection.West),
                });
            _createdAssets.Add(room);
            return room;
        }
    }
}
