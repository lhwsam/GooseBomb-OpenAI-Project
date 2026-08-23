using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BombSwap.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
                locked.Where(exit => exit.IsConnected &&
                    exit.Status != DungeonRoomExitStatus.SecretWall)
                    .Select(exit => exit.Status),
                Is.All.EqualTo(DungeonRoomExitStatus.Locked));

            Assert.That(
                session.TryClearCurrentRoom(),
                Is.EqualTo(DungeonRoomClearStatus.Cleared));
            Assert.That(
                session.GetCurrentExitStates()
                    .Where(exit => exit.IsConnected &&
                        exit.Status != DungeonRoomExitStatus.SecretWall)
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
            GameObject northCracks = CreateCrackRoot("NorthSecretCracks");
            GameObject eastCracks = CreateCrackRoot("EastSecretCracks");
            GameObject southCracks = CreateCrackRoot("SouthSecretCracks");
            GameObject westCracks = CreateCrackRoot("WestSecretCracks");

            Assert.Throws<ArgumentNullException>(() =>
                presenter.Configure(
                    north,
                    east,
                    south,
                    null,
                    northCracks,
                    eastCracks,
                    southCracks,
                    westCracks));
            Assert.Throws<InvalidOperationException>(() =>
                presenter.Configure(
                    north,
                    east,
                    south,
                    north,
                    northCracks,
                    eastCracks,
                    southCracks,
                    westCracks));
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
        public IEnumerator PlayerHealth_PersistsAcrossForwardTravelAndRoomReentry()
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
                PrototypeDungeonRoomBinder startBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession startSession = startBinder.RoomSession;

                Assert.That(run.PlayerHealthState, Is.Not.Null);
                Assert.That(run.PlayerHealthState.CurrentHealth, Is.EqualTo(5));
                Assert.That(startSession.CurrentHealth, Is.EqualTo(5));
                Assert.That(startSession.TryPlaceBomb(), Is.True);

                float damageDeadline = Time.realtimeSinceStartup + 5f;
                while (startSession.CurrentHealth == 5 &&
                       Time.realtimeSinceStartup < damageDeadline)
                {
                    yield return null;
                }

                Assert.That(startSession.CurrentHealth, Is.EqualTo(4));
                Assert.That(run.PlayerHealthState.CurrentHealth, Is.EqualTo(4));

                DungeonRoomNodeId startRoom = run.Graph.StartRoomId;
                DungeonRoomNodeId firstCombat = run.Graph.GetNeighbors(startRoom)[0];
                Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);
                Assert.That(
                    run.TryGetSceneName(firstCombat, out string combatSceneName),
                    Is.True);
                yield return SceneManager.LoadSceneAsync(
                    combatSceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder combatBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeHealthHud combatHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(combatBinder.RoomSession.CurrentHealth, Is.EqualTo(4));
                Assert.That(combatHud.DisplayedPlayerHealth, Is.EqualTo(4));
                Assert.That(run.PlayerHealthState.CurrentHealth, Is.EqualTo(4));

                Assert.That(
                    run.TryClearCurrentRoom(),
                    Is.EqualTo(DungeonRoomClearStatus.Cleared));
                Assert.That(run.TryTravelTo(startRoom).Moved, Is.True);
                yield return SceneManager.LoadSceneAsync(
                    "DungeonStart",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder reentryBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeHealthHud reentryHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(reentryBinder.RoomSession.CurrentHealth, Is.EqualTo(4));
                Assert.That(reentryHud.DisplayedPlayerHealth, Is.EqualTo(4));
                Assert.That(run.PlayerHealthState.CurrentHealth, Is.EqualTo(4));

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
                    "DungeonHealthPersistencePlayModeCleanup");
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
                Assert.That(session.CurrentBossPattern, Is.EqualTo(BossPatternKind.LimitedChase));
                Assert.That(presenter.VisibleDangerCellCount, Is.EqualTo(0));
                Assert.That(presenter.IsMoveTargetVisible, Is.False);
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
                Assert.That(run.BombLoadoutState.ActiveSlotIndex, Is.Zero);
                Assert.That(rewardSession.TrySwapActiveBomb(), Is.True);
                Assert.That(rewardSession.ActiveBombSlotIndex, Is.EqualTo(1));
                Assert.That(run.BombLoadoutState.ActiveSlotIndex, Is.EqualTo(1));

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
                Assert.That(nextSession.ActiveBombSlotIndex, Is.EqualTo(1));
                Assert.That(
                    nextSession.GetBombDefinitionForSlot(nextSession.ActiveBombSlotIndex).DefinitionId,
                    Is.EqualTo(selected.Value));
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

        [UnityTest]
        public IEnumerator DungeonScenes_MinimapShowsCurrentVisitedAndDiscoveredFrontier()
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
                PrototypeDungeonMinimapPresenter startMinimap =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonMinimapPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(startMinimap.IsInitialized, Is.True);
                Assert.That(startMinimap.ViewPrefab, Is.Not.Null);
                Assert.That(startMinimap.ViewInstance, Is.Not.Null);
                Assert.That(
                    startMinimap.ViewInstance,
                    Is.Not.SameAs(startMinimap.ViewPrefab));
                Assert.That(
                    startMinimap.DisplayedCurrentRoomId,
                    Is.EqualTo(run.Graph.StartRoomId));
                Assert.That(startMinimap.DisplayedRoomCount, Is.EqualTo(2));
                Assert.That(startMinimap.DisplayedConnectionCount, Is.EqualTo(1));
                Assert.That(
                    startMinimap.DisplayedSnapshot.GetRoom(run.Graph.StartRoomId).State,
                    Is.EqualTo(DungeonMinimapRoomState.Current));

                RectTransform minimapPanel =
                    (RectTransform)startMinimap.ViewInstance.MapRoot.parent;
                PrototypeHealthHud startHealthHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();
                RectTransform rewardPanel = (RectTransform)startHealthHud
                    .ViewInstance
                    .CombatRewardLabel
                    .transform
                    .parent;
                Assert.That(
                    minimapPanel.anchoredPosition.y,
                    Is.LessThanOrEqualTo(
                        rewardPanel.anchoredPosition.y -
                        rewardPanel.sizeDelta.y - 10f));

                DungeonRoomNodeId firstCombat =
                    run.Graph.GetNeighbors(run.Graph.StartRoomId).Single();
                Assert.That(run.TryTravelTo(firstCombat).Moved, Is.True);
                Assert.That(
                    run.TryGetSceneName(firstCombat, out string combatSceneName),
                    Is.True);
                yield return SceneManager.LoadSceneAsync(
                    combatSceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonMinimapPresenter combatMinimap =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonMinimapPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(combatMinimap.IsInitialized, Is.True);
                Assert.That(combatMinimap.ViewPrefab, Is.Not.Null);
                Assert.That(combatMinimap.ViewInstance, Is.Not.Null);
                Assert.That(
                    combatMinimap.DisplayedCurrentRoomId,
                    Is.EqualTo(firstCombat));
                Assert.That(combatMinimap.DisplayedRoomCount, Is.EqualTo(3));
                Assert.That(combatMinimap.DisplayedConnectionCount, Is.EqualTo(2));
                Assert.That(
                    combatMinimap.DisplayedSnapshot.GetRoom(run.Graph.StartRoomId).State,
                    Is.EqualTo(DungeonMinimapRoomState.Visited));
                Assert.That(
                    combatMinimap.DisplayedSnapshot.GetRoom(
                        run.Graph.BombRewardRoomId).State,
                    Is.EqualTo(DungeonMinimapRoomState.Discovered));
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
                    "DungeonMinimapPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SecretRoom_ExplosionRevealsEntranceAndCachePaysOnceAcrossReentry()
        {
            Scene loadedDungeonScene = default;
            Keyboard keyboard = null;
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
                Assert.That(run.Graph.HasSecretRoom, Is.True);
                DungeonRoomNodeId secretRoom = run.Graph.SecretRoomId;
                DungeonRoomNodeId[] secretNeighbors = run.Graph.GetNeighbors(secretRoom)
                    .OrderBy(roomId => roomId.Value)
                    .ToArray();
                Assert.That(secretNeighbors, Has.Length.InRange(2, 3));
                Assert.That(
                    secretNeighbors.Select(run.Graph.GetRoom)
                        .Select(room => room.RoomType),
                    Is.All.EqualTo(RoomType.Combat));

                DungeonRoomNodeId entranceCombat = secretNeighbors[0];
                DungeonRoomNodeId hiddenAlternateCombat = secretNeighbors[1];
                TraverseRunTo(run, entranceCombat);
                if (run.RunState.IsCurrentRoomLocked)
                {
                    Assert.That(
                        run.TryClearCurrentRoom(),
                        Is.EqualTo(DungeonRoomClearStatus.Cleared));
                }
                int tokensBeforeCache = run.RoomRewardTokenCount;
                Assert.That(
                    run.RunState.CreateMinimapSnapshot().ContainsRoom(secretRoom),
                    Is.False);
                Assert.That(
                    run.TryGetSceneName(entranceCombat, out string combatSceneName),
                    Is.True);

                yield return SceneManager.LoadSceneAsync(
                    combatSceneName,
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder entranceBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession entranceSession = entranceBinder.RoomSession;
                PrototypeDungeonMinimapPresenter entranceMinimap =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonMinimapPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(entranceBinder.RuntimeRoomId, Is.EqualTo(entranceCombat));
                Assert.That(entranceSession.EnemyActiveCount, Is.Zero);
                Assert.That(
                    entranceBinder.RuntimeSecretDoorImpactCells,
                    Has.Count.EqualTo(1));
                GridPosition secretDoorImpactCell =
                    entranceBinder.RuntimeSecretDoorImpactCells[0];
                Assert.That(
                    entranceSession.GetCell(secretDoorImpactCell).Terrain,
                    Is.EqualTo(GridTerrain.Floor));
                Assert.That(entranceMinimap.DisplayedSnapshot.ContainsRoom(secretRoom), Is.False);
                Assert.That(
                    CountDisplayedDoorStatuses(
                        entranceBinder.DoorPresenter,
                        DungeonRoomExitStatus.SecretWall),
                    Is.EqualTo(1));
                Assert.That(
                    CountVisibleSecretCracks(entranceBinder.DoorPresenter),
                    Is.EqualTo(1));
                GameObject visibleSecretWall = new[]
                    {
                        entranceBinder.DoorPresenter.NorthSecretCracks,
                        entranceBinder.DoorPresenter.EastSecretCracks,
                        entranceBinder.DoorPresenter.SouthSecretCracks,
                        entranceBinder.DoorPresenter.WestSecretCracks,
                    }
                    .Single(root => root.activeSelf);
                Renderer hiddenSecretDoor = new[]
                    {
                        entranceBinder.DoorPresenter.NorthDoor,
                        entranceBinder.DoorPresenter.EastDoor,
                        entranceBinder.DoorPresenter.SouthDoor,
                        entranceBinder.DoorPresenter.WestDoor,
                    }
                    .Single(renderer => !renderer.enabled);
                Assert.That(
                    Vector3.Distance(
                        visibleSecretWall.transform.position,
                        hiddenSecretDoor.transform.position),
                    Is.LessThan(0.001f),
                    "The cracked secret door must occupy the normal door position.");

                keyboard = InputSystem.AddDevice<Keyboard>();
                yield return MoveSessionTo(
                    entranceSession,
                    keyboard,
                    secretDoorImpactCell);
                Assert.That(
                    entranceSession.CurrentGridPosition,
                    Is.EqualTo(secretDoorImpactCell));
                Assert.That(run.CurrentRoomId, Is.EqualTo(entranceCombat));
                Assert.That(
                    run.TryTravelTo(secretRoom).Status,
                    Is.EqualTo(DungeonTravelStatus.BlockedBySecretWall));
                bool explosionObserved = false;
                BombExplosion observedExplosion = default;
                entranceSession.BombExploded += explosion =>
                {
                    explosionObserved = true;
                    observedExplosion = explosion;
                };
                Assert.That(entranceSession.TryPlaceBomb(), Is.True);
                GridPosition evadeCell = FindWalkableNeighbor(
                    entranceSession,
                    secretDoorImpactCell);
                yield return MoveSessionTo(entranceSession, keyboard, evadeCell);

                float revealDeadline = Time.realtimeSinceStartup + 5f;
                while (!explosionObserved &&
                       Time.realtimeSinceStartup < revealDeadline)
                {
                    yield return null;
                }

                Assert.That(
                    explosionObserved,
                    Is.True,
                    $"Bomb did not explode; active={entranceSession.ActiveBombCount}.");
                Assert.That(
                    observedExplosion.Affects(secretDoorImpactCell),
                    Is.True,
                    $"Bomb {observedExplosion.DefinitionId} at {observedExplosion.Origin} " +
                    $"did not reach secret-door impact cell {secretDoorImpactCell}.");
                Assert.That(
                    observedExplosion.DestroyedWalls,
                    Has.No.Member(secretDoorImpactCell));

                Assert.That(
                    entranceSession.GetCell(secretDoorImpactCell).Terrain,
                    Is.EqualTo(GridTerrain.Floor));
                Assert.That(
                    entranceBinder.RuntimeSecretDoorImpactCells,
                    Is.Empty);
                Assert.That(
                    run.RunState.CreateMinimapSnapshot().ContainsRoom(secretRoom),
                    Is.True);
                Assert.That(
                    entranceMinimap.DisplayedSnapshot.ContainsRoom(secretRoom),
                    Is.True);
                Assert.That(
                    CountDisplayedDoorStatuses(
                        entranceBinder.DoorPresenter,
                        DungeonRoomExitStatus.SecretWall),
                    Is.Zero);
                Assert.That(
                    CountVisibleSecretCracks(entranceBinder.DoorPresenter),
                    Is.Zero);

                PrototypeDungeonTransitionStartResult secretEntry = host.RequestTravel(
                    run.Graph.GetExitDirection(entranceCombat, secretRoom));
                Assert.That(secretEntry.Started, Is.True);
                Assert.That(secretEntry.Transition.TargetSceneName, Is.EqualTo("DungeonSecret"));
                yield return null;

                PrototypeDungeonRoomBinder secretBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeGameSession secretSession = secretBinder.RoomSession;
                PrototypeSecretRewardPresenter reward =
                    UnityEngine.Object.FindObjectsByType<PrototypeSecretRewardPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeHealthHud secretHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(secretBinder.RuntimeRoomType, Is.EqualTo(RoomType.Secret));
                Assert.That(secretSession.EnemyActiveCount, Is.Zero);
                Assert.That(run.RunState.IsCurrentRoomLocked, Is.False);
                Assert.That(
                    secretBinder.RuntimeSecretDoorImpactCells,
                    Has.Count.EqualTo(1));
                Assert.That(
                    CountDisplayedDoorStatuses(
                        secretBinder.DoorPresenter,
                        DungeonRoomExitStatus.SecretWall),
                    Is.EqualTo(1));
                Assert.That(
                    run.TryTravelTo(hiddenAlternateCombat).Status,
                    Is.EqualTo(DungeonTravelStatus.BlockedBySecretWall));
                Assert.That(reward.IsInitialized, Is.True);
                Assert.That(reward.IsCollected, Is.False);
                Assert.That(reward.IsVisualVisible, Is.True);
                Assert.That(reward.PickupCell, Is.EqualTo(Vector2Int.zero));

                yield return MoveSessionTo(
                    secretSession,
                    keyboard,
                    new GridPosition(0, 0));
                yield return null;

                Assert.That(reward.IsCollected, Is.True);
                Assert.That(reward.IsVisualVisible, Is.False);
                Assert.That(
                    reward.LastStatus,
                    Is.EqualTo(DungeonSecretRewardCollectStatus.Collected));
                Assert.That(
                    run.RoomRewardTokenCount,
                    Is.EqualTo(tokensBeforeCache + PrototypeSecretRewardPresenter.DefaultTokenReward));
                Assert.That(
                    secretHud.DisplayedCombatRewardTokenCount,
                    Is.EqualTo(run.RoomRewardTokenCount));

                PrototypeDungeonTransitionStartResult secretExit = host.RequestTravel(
                    run.Graph.GetExitDirection(secretRoom, entranceCombat));
                Assert.That(secretExit.Started, Is.True);
                Assert.That(
                    secretExit.Transition.TargetSceneName,
                    Is.EqualTo(combatSceneName));
                yield return null;
                PrototypeDungeonTransitionStartResult secretReentry = host.RequestTravel(
                    run.Graph.GetExitDirection(entranceCombat, secretRoom));
                Assert.That(secretReentry.Started, Is.True);
                Assert.That(
                    secretReentry.Transition.TargetSceneName,
                    Is.EqualTo("DungeonSecret"));
                yield return null;

                PrototypeSecretRewardPresenter reentryReward =
                    UnityEngine.Object.FindObjectsByType<PrototypeSecretRewardPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(reentryReward.IsCollected, Is.True);
                Assert.That(reentryReward.IsVisualVisible, Is.False);
                Assert.That(
                    reentryReward.InstructionText,
                    Is.EqualTo("SECRET CACHE COLLECTED"));
                Assert.That(
                    run.RoomRewardTokenCount,
                    Is.EqualTo(tokensBeforeCache + PrototypeSecretRewardPresenter.DefaultTokenReward));

                loadedDungeonScene = SceneManager.GetActiveScene();
            }
            finally
            {
                if (keyboard != null && keyboard.added)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.Update();
                    InputSystem.RemoveDevice(keyboard);
                }

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
                    "DungeonSecretPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedDungeonScene.IsValid() && loadedDungeonScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedDungeonScene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RecoveryScene_SavesAtFullHealthRestoresOnceAndPersistsAcrossReentry()
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
                    run.Graph.RecoveryRoomId);
                for (int index = 1; index < path.Count; index++)
                {
                    if (run.RunState.IsCurrentRoomLocked)
                    {
                        Assert.That(
                            run.TryClearCurrentRoom(),
                            Is.EqualTo(DungeonRoomClearStatus.Cleared));
                    }
                    Assert.That(run.TryTravelTo(path[index]).Moved, Is.True);
                }

                int tokensBeforeRecovery = run.CombatRewardTokenCount;
                yield return SceneManager.LoadSceneAsync(
                    "DungeonRecovery",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeDungeonRoomBinder fullBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeRecoveryPickupPresenter fullPresenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeRecoveryPickupPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeHealthHud fullHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(fullBinder.RuntimeRoomType, Is.EqualTo(RoomType.Recovery));
                Assert.That(fullBinder.RoomSession.EnemyActiveCount, Is.Zero);
                Assert.That(run.RunState.IsCurrentRoomLocked, Is.False);
                Assert.That(fullPresenter.IsInitialized, Is.True);
                Assert.That(fullPresenter.IsVisualVisible, Is.True);
                Assert.That(
                    fullPresenter.TryCollectAt(new GridPosition(0, 0)),
                    Is.False);
                Assert.That(
                    fullPresenter.LastStatus,
                    Is.EqualTo(DungeonRecoveryUseStatus.AtFullHealth));
                Assert.That(fullPresenter.IsVisualVisible, Is.True);
                Assert.That(
                    run.RunState.IsRecoveryConsumed(run.Graph.RecoveryRoomId),
                    Is.False);
                Assert.That(fullHud.DisplayedPlayerHealth, Is.EqualTo(5));

                DungeonRoomNodeId recoveryParent =
                    run.Graph.GetNeighbors(run.Graph.RecoveryRoomId).Single();
                Assert.That(run.TryTravelTo(recoveryParent).Moved, Is.True);
                var roomHealth = new PlayerHealthSimulation(
                    new ActorId(1),
                    new ManualGameClock(),
                    new PlayerHealthDefinition(5, TimeSpan.FromSeconds(0.75)),
                    run.PlayerHealthState.CurrentHealth);
                PlayerDamageResult damage =
                    roomHealth.ApplyContactDamage(new ActorId(2), 2);
                run.PlayerHealthState.RecordAppliedDamage(damage);
                Assert.That(run.TryTravelTo(run.Graph.RecoveryRoomId).Moved, Is.True);

                yield return SceneManager.LoadSceneAsync(
                    "DungeonRecovery",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeRecoveryPickupPresenter damagedPresenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeRecoveryPickupPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeDungeonRoomBinder damagedBinder =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRoomBinder>(
                            FindObjectsInactive.Include)
                        .Single();
                PrototypeHealthHud damagedHud =
                    UnityEngine.Object.FindObjectsByType<PrototypeHealthHud>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(damagedBinder.RoomSession.CurrentHealth, Is.EqualTo(3));
                Assert.That(damagedHud.DisplayedPlayerHealth, Is.EqualTo(3));
                Assert.That(
                    damagedPresenter.TryCollectAt(new GridPosition(0, 0)),
                    Is.True);
                Assert.That(
                    damagedPresenter.LastStatus,
                    Is.EqualTo(DungeonRecoveryUseStatus.Restored));
                Assert.That(damagedPresenter.IsConsumed, Is.True);
                Assert.That(damagedPresenter.IsVisualVisible, Is.False);
                Assert.That(damagedBinder.RoomSession.CurrentHealth, Is.EqualTo(5));
                Assert.That(run.PlayerHealthState.CurrentHealth, Is.EqualTo(5));
                Assert.That(damagedHud.DisplayedPlayerHealth, Is.EqualTo(5));
                Assert.That(damagedHud.PlayerHealthFillFraction, Is.EqualTo(1f));
                Assert.That(run.CombatRewardTokenCount, Is.EqualTo(tokensBeforeRecovery));

                Assert.That(run.TryTravelTo(recoveryParent).Moved, Is.True);
                Assert.That(run.TryTravelTo(run.Graph.RecoveryRoomId).Moved, Is.True);
                yield return SceneManager.LoadSceneAsync(
                    "DungeonRecovery",
                    LoadSceneMode.Single);
                yield return null;

                PrototypeRecoveryPickupPresenter consumedPresenter =
                    UnityEngine.Object.FindObjectsByType<PrototypeRecoveryPickupPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(consumedPresenter.IsConsumed, Is.True);
                Assert.That(consumedPresenter.IsVisualVisible, Is.False);
                Assert.That(consumedPresenter.InstructionText, Is.EqualTo("RECOVERY USED"));
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
                    "DungeonRecoveryPlayModeCleanup");
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
            Assert.That(
                catalog.GetSceneName(RoomType.Recovery),
                Is.EqualTo("DungeonRecovery"));
            Assert.That(
                catalog.GetSceneName(RoomType.Secret),
                Is.EqualTo("DungeonSecret"));
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
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.Recovery,
                        "Recovery"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Secret, "Secret"),
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
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.Recovery,
                        "Recovery"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Secret, "Secret"),
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
                    new PrototypeDungeonSpecialRoomEntry(
                        RoomType.Recovery,
                        "Recovery"),
                    new PrototypeDungeonSpecialRoomEntry(RoomType.Secret, "Secret"),
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
        public void Session_RecoveryPersistsHealthAndConsumptionForTheRun()
        {
            var session = new PrototypeDungeonRunSession(
                0,
                CreateCatalog(),
                CreateSpecialCatalog(),
                CreateBombRewardCatalog(),
                new PlayerHealthDefinition(5, TimeSpan.FromSeconds(0.75)));
            TraverseRunTo(session, session.Graph.RecoveryRoomId);

            DungeonRecoveryUseResult atFull = session.TryUseRecovery(2);
            Assert.That(atFull.Status, Is.EqualTo(
                DungeonRecoveryUseStatus.AtFullHealth));
            Assert.That(
                session.RunState.IsRecoveryConsumed(session.Graph.RecoveryRoomId),
                Is.False);

            var roomHealth = new PlayerHealthSimulation(
                new ActorId(1),
                new ManualGameClock(),
                new PlayerHealthDefinition(5, TimeSpan.FromSeconds(0.75)),
                session.PlayerHealthState.CurrentHealth);
            PlayerDamageResult damage =
                roomHealth.ApplyContactDamage(new ActorId(2), 3);
            session.PlayerHealthState.RecordAppliedDamage(damage);

            DungeonRecoveryUseResult restored = session.TryUseRecovery(2);
            Assert.That(restored.Status, Is.EqualTo(
                DungeonRecoveryUseStatus.Restored));
            Assert.That(restored.RestoredHealth, Is.EqualTo(2));
            Assert.That(session.PlayerHealthState.CurrentHealth, Is.EqualTo(4));
            Assert.That(
                session.RunState.IsRecoveryConsumed(session.Graph.RecoveryRoomId),
                Is.True);
            Assert.That(
                session.TryUseRecovery(2).Status,
                Is.EqualTo(DungeonRecoveryUseStatus.AlreadyConsumed));
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
            PrototypePlayerVitalsAsset playerVitals = CreatePlayerVitals();
            GameObject firstRoot = CreateGameObject("FirstDungeonRunHost");
            firstRoot.SetActive(false);
            PrototypeDungeonRunHost first =
                firstRoot.AddComponent<PrototypeDungeonRunHost>();
            first.Configure(
                5,
                combatCatalog,
                specialCatalog,
                rewardCatalog,
                playerVitals,
                false);
            firstRoot.SetActive(true);

            GameObject duplicateRoot = CreateGameObject("DuplicateDungeonRunHost");
            duplicateRoot.SetActive(false);
            PrototypeDungeonRunHost duplicate =
                duplicateRoot.AddComponent<PrototypeDungeonRunHost>();
            duplicate.Configure(
                5,
                combatCatalog,
                specialCatalog,
                rewardCatalog,
                playerVitals,
                false);
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
                    CreatePlayerVitals(),
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
                Assert.That(host.RunSession.PlayerHealthState.CurrentHealth, Is.EqualTo(5));
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
                    CreatePlayerVitals(),
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
                Assert.That(host.RunSession.PlayerHealthState.CurrentHealth, Is.EqualTo(5));
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

        [UnityTest]
        public IEnumerator RunHost_ExitsFinishedRunToLobbyAndLobbyStartsCleanRun()
        {
            Scene loadedScene = default;
            Keyboard pauseKeyboard = null;
            Keyboard feedbackKeyboard = null;
            InputAction pauseAction = null;
            InputActionAsset lobbyInputActions = null;
            int pauseBindingIndex = -1;
            string previousPauseOverride = null;
            bool hadInputOverrides = PlayerPrefs.HasKey(
                PrototypeUserSettingsStorage.InputOverridesKey);
            string previousInputOverrides = PlayerPrefs.GetString(
                PrototypeUserSettingsStorage.InputOverridesKey,
                string.Empty);
            try
            {
                GameObject hostRoot = CreateGameObject("LobbyExitDungeonRunHost");
                hostRoot.SetActive(false);
                PrototypeDungeonRunHost host =
                    hostRoot.AddComponent<PrototypeDungeonRunHost>();
                host.Configure(
                    31,
                    CreateCatalog(),
                    CreateSpecialCatalog(),
                    CreateBombRewardCatalog(),
                    CreatePlayerVitals(),
                    false);
                hostRoot.SetActive(true);
                yield return null;

                Assert.That(
                    host.TryFailCurrentRun(CreateFatalContactDamage()),
                    Is.True);
                host.ExitFinishedRunToScene(
                    PrototypeLobbyPresenter.DefaultLobbySceneName);
                yield return null;

                loadedScene = SceneManager.GetActiveScene();
                Assert.That(
                    loadedScene.name,
                    Is.EqualTo(PrototypeLobbyPresenter.DefaultLobbySceneName));
                Assert.That(
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include),
                    Is.Empty);

                PrototypeLobbyPresenter lobby =
                    UnityEngine.Object.FindObjectsByType<PrototypeLobbyPresenter>(
                            FindObjectsInactive.Include)
                        .Single();
                Assert.That(
                    lobby.TitleText,
                    Does.Contain(PrototypeLobbyPresenter.GameTitle));
                Assert.That(lobby.HasAuthoredViewReferences, Is.True);
                Assert.That(lobby.HasVersionLabelReference, Is.True);
                Assert.That(lobby.VersionLabel, Is.Not.Null);
                Assert.That(
                    lobby.VersionText,
                    Is.EqualTo(
                        PrototypeLobbyPresenter.FormatVersionText(
                            Application.version)));
                Assert.That(lobby.LobbyCanvas.gameObject.scene, Is.EqualTo(loadedScene));
                Assert.That(lobby.LobbyCanvas.name, Is.EqualTo("LobbyCanvas"));
                CanvasScaler lobbyScaler = lobby.LobbyCanvas.GetComponent<CanvasScaler>();
                Assert.That(
                    PrototypeUiFactory.HasReferenceCanvasScale(lobbyScaler),
                    Is.True);
                Assert.That(lobby.LobbyEventSystem.gameObject.scene, Is.EqualTo(loadedScene));
                Assert.That(lobby.StartButton, Is.Not.Null);
                Assert.That(lobby.ControlsButton, Is.Not.Null);
                Assert.That(lobby.BackButton, Is.Not.Null);
                PrototypeButtonScaleFeedback startFeedback =
                    lobby.StartButton.GetComponent<PrototypeButtonScaleFeedback>();
                PrototypeButtonScaleFeedback settingsFeedback =
                    lobby.ControlsButton.GetComponent<PrototypeButtonScaleFeedback>();
                Assert.That(
                    startFeedback.ColorTarget,
                    Is.SameAs(
                        lobby.StartButton.GetComponentInChildren<
                            TextMeshProUGUI>(true)));
                Assert.That(
                    settingsFeedback.ColorTarget,
                    Is.SameAs(
                        lobby.ControlsButton.GetComponentInChildren<
                            TextMeshProUGUI>(true)));
                Assert.That(startFeedback.HoverVisualTargetCount, Is.EqualTo(2));
                Assert.That(settingsFeedback.HoverVisualTargetCount, Is.EqualTo(2));
                for (int hoverIndex = 0; hoverIndex < 2; hoverIndex++)
                {
                    Assert.That(
                        startFeedback.GetHoverVisualTarget(hoverIndex).transform
                            .IsChildOf(lobby.StartButton.transform),
                        Is.True);
                    Assert.That(
                        settingsFeedback.GetHoverVisualTarget(hoverIndex).transform
                            .IsChildOf(lobby.ControlsButton.transform),
                        Is.True);
                }
                Assert.That(
                    lobby.LobbyEventSystem.currentSelectedGameObject,
                    Is.SameAs(lobby.StartButton.gameObject));
                Assert.That(
                    startFeedback.VisualTarget.localScale,
                    Is.EqualTo(Vector3.one));
                Assert.That(
                    startFeedback.ColorTarget.color,
                    Is.EqualTo(startFeedback.StartColor));
                Assert.That(lobby.IsControlsVisible, Is.False);
                Assert.That(lobby.SettingsRuntime, Is.Not.Null);
                Assert.That(lobby.SettingsRuntime.HasRequiredReferences, Is.True);
                Assert.That(lobby.SettingsPanel, Is.Not.Null);
                Assert.That(
                    lobby.SettingsPanel.KeyboardBindingCount,
                    Is.EqualTo(PrototypeSettingsPanelFactory.KeyboardBindingCount));
                Assert.That(lobby.SettingsPanel.KeyboardResetButton, Is.Not.Null);
                Assert.That(
                    lobby.SettingsPanel.KeyboardResetButton.transform
                        .IsChildOf(lobby.SettingsPanel.transform),
                    Is.True);
                Assert.That(
                    lobby.SettingsPanel
                        .GetComponentsInChildren<TextMeshProUGUI>(true)
                        .Any(label => label.name == "SettingsStatusText"),
                    Is.False);
                Assert.That(
                    lobby.SettingsRuntime.AudioMixer.GetFloat(
                        PrototypeUserSettingsRuntime.MasterVolumeParameter,
                        out _),
                    Is.True);
                Assert.That(
                    lobby.SettingsRuntime.AudioMixer.GetFloat(
                        PrototypeUserSettingsRuntime.BgmVolumeParameter,
                        out _),
                    Is.True);
                Assert.That(
                    lobby.SettingsRuntime.AudioMixer.GetFloat(
                        PrototypeUserSettingsRuntime.SfxVolumeParameter,
                        out _),
                    Is.True);

                lobby.ShowControls();
                Assert.That(lobby.IsControlsVisible, Is.True);
                Assert.That(lobby.SettingsPanel.IsControlsPageVisible, Is.True);
                lobbyInputActions = lobby.SettingsRuntime.InputActions;
                lobby.SettingsPanel.KeyboardResetButton.onClick.Invoke();
                PrototypeSettingsPanelPresenter.KeyboardBindingView upBinding =
                    lobby.SettingsPanel.GetKeyboardBinding(0);
                feedbackKeyboard = InputSystem.AddDevice<Keyboard>();
                upBinding.Button.onClick.Invoke();
                Assert.That(lobby.SettingsPanel.IsRebinding, Is.True);

                InputSystem.QueueStateEvent(
                    feedbackKeyboard,
                    new KeyboardState(Key.S));
                InputSystem.Update();
                InputSystem.QueueStateEvent(
                    feedbackKeyboard,
                    new KeyboardState());
                InputSystem.Update();
                yield return null;

                Assert.That(lobby.SettingsPanel.IsRebinding, Is.False);
                Assert.That(
                    lobby.SettingsPanel.IsDuplicateBindingFeedbackPlaying,
                    Is.True);
                Assert.That(upBinding.ValueLabel.text, Is.EqualTo("이미 사용 중"));
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(
                    lobby.SettingsPanel.IsDuplicateBindingFeedbackPlaying,
                    Is.False);
                Assert.That(upBinding.ValueLabel.text, Is.EqualTo("W"));
                Assert.That(
                    lobby.SettingsPanel
                        .GetComponentsInChildren<TextMeshProUGUI>(true)
                        .Any(label =>
                            label.text.Contains("게임패드") ||
                            label.text.Contains("Gamepad")),
                    Is.False);
                lobby.SettingsPanel.ShowAudioPage();
                Assert.That(lobby.SettingsPanel.IsControlsPageVisible, Is.False);
                lobby.HideControls();
                Assert.That(lobby.IsControlsVisible, Is.False);

                TextMeshProUGUI[] labels =
                    UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                        FindObjectsInactive.Include);
                Assert.That(labels, Is.Not.Empty);
                Assert.That(
                    labels.All(label =>
                        PrototypeUiFactory.IsSupportedGameFont(label.font)),
                    Is.True);

                lobby.StartNewRun();
                yield return null;

                loadedScene = SceneManager.GetActiveScene();
                Assert.That(
                    loadedScene.name,
                    Is.EqualTo(PrototypeLobbyPresenter.DefaultStartSceneName));
                PrototypeDungeonRunHost startedHost =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                            FindObjectsInactive.Include)
                        .Single(candidate => candidate.IsPrimary);
                Assert.That(startedHost.RunSession, Is.Not.Null);
                Assert.That(startedHost.RunSession.IsFinished, Is.False);
                Assert.That(
                    startedHost.RunSession.CurrentRoomId,
                    Is.EqualTo(startedHost.RunSession.Graph.StartRoomId));

                PrototypeGameSession gameSession =
                    UnityEngine.Object.FindObjectsByType<PrototypeGameSession>(
                            FindObjectsInactive.Include)
                        .Single();
                pauseAction = gameSession.InputReader.InputActions.FindAction(
                    BombSwapInputActionNames.Pause,
                    true);
                Guid pauseBindingId = Guid.Parse(
                    "afedc0a1-6906-45b8-90ce-f0eebf188ab2");
                for (int index = 0; index < pauseAction.bindings.Count; index++)
                {
                    if (pauseAction.bindings[index].id == pauseBindingId)
                    {
                        pauseBindingIndex = index;
                        break;
                    }
                }
                Assert.That(pauseBindingIndex, Is.GreaterThanOrEqualTo(0));
                previousPauseOverride =
                    pauseAction.bindings[pauseBindingIndex].overridePath;
                pauseAction.ApplyBindingOverride(
                    pauseBindingIndex,
                    "<Keyboard>/escape");
                pauseKeyboard = InputSystem.AddDevice<Keyboard>();

                PressAndRelease(pauseKeyboard, Key.Escape);
                yield return null;

                Assert.That(gameSession.IsPaused, Is.True);
                PrototypePausePresenter pausePresenter =
                    gameSession.GetComponent<PrototypePausePresenter>();
                Assert.That(pausePresenter.IsVisible, Is.True);
                Button pauseSettingsButton =
                    pausePresenter.ViewInstance.SettingsButton;
                pauseSettingsButton.onClick.Invoke();
                Assert.That(pausePresenter.IsSettingsOpen, Is.True);
                Assert.That(pausePresenter.SettingsPanel, Is.Not.Null);
                Assert.That(
                    pausePresenter.SettingsPanel.KeyboardBindingCount,
                    Is.EqualTo(PrototypeSettingsPanelFactory.KeyboardBindingCount));
                Assert.That(
                    pausePresenter.SettingsPanel.KeyboardResetButton,
                    Is.Not.Null);

                PressAndRelease(pauseKeyboard, Key.Escape);
                yield return null;
                Assert.That(gameSession.IsPaused, Is.True);
                Assert.That(pausePresenter.IsSettingsOpen, Is.False);

                PressAndRelease(pauseKeyboard, Key.Escape);
                yield return null;
                Assert.That(gameSession.IsPaused, Is.False);
            }
            finally
            {
                if (pauseAction != null && pauseBindingIndex >= 0)
                {
                    if (string.IsNullOrEmpty(previousPauseOverride))
                    {
                        pauseAction.RemoveBindingOverride(pauseBindingIndex);
                    }
                    else
                    {
                        pauseAction.ApplyBindingOverride(
                            pauseBindingIndex,
                            previousPauseOverride);
                    }
                }
                if (pauseKeyboard != null && pauseKeyboard.added)
                {
                    InputSystem.RemoveDevice(pauseKeyboard);
                }
                if (feedbackKeyboard != null && feedbackKeyboard.added)
                {
                    InputSystem.RemoveDevice(feedbackKeyboard);
                }
                if (lobbyInputActions != null)
                {
                    lobbyInputActions.RemoveAllBindingOverrides();
                    if (hadInputOverrides &&
                        !string.IsNullOrWhiteSpace(previousInputOverrides))
                    {
                        lobbyInputActions.LoadBindingOverridesFromJson(
                            previousInputOverrides);
                    }
                }
                if (hadInputOverrides)
                {
                    PlayerPrefs.SetString(
                        PrototypeUserSettingsStorage.InputOverridesKey,
                        previousInputOverrides);
                }
                else
                {
                    PlayerPrefs.DeleteKey(
                        PrototypeUserSettingsStorage.InputOverridesKey);
                }
                PlayerPrefs.Save();
                PrototypeDungeonRunHost[] hosts =
                    UnityEngine.Object.FindObjectsByType<PrototypeDungeonRunHost>(
                        FindObjectsInactive.Include);
                for (int index = 0; index < hosts.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(hosts[index].gameObject);
                }

                if (!loadedScene.IsValid())
                {
                    loadedScene = SceneManager.GetActiveScene();
                }
                Scene cleanup = SceneManager.CreateScene("LobbyFlowPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedScene);
                }
            }

            yield return null;
        }

        private static void PressAndRelease(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
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
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.Recovery,
                    "DungeonRecovery"),
                new PrototypeDungeonSpecialRoomEntry(
                    RoomType.Secret,
                    "DungeonSecret"),
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
                CreateDoorRenderer("WestDoor"),
                CreateCrackRoot("NorthSecretCracks"),
                CreateCrackRoot("EastSecretCracks"),
                CreateCrackRoot("SouthSecretCracks"),
                CreateCrackRoot("WestSecretCracks"));
            return presenter;
        }

        private Renderer CreateDoorRenderer(string name)
        {
            GameObject door = CreateGameObject(name);
            return door.AddComponent<MeshRenderer>();
        }

        private GameObject CreateCrackRoot(string name)
        {
            GameObject root = CreateGameObject(name);
            root.SetActive(false);
            return root;
        }

        private static int CountDisplayedDoorStatuses(
            PrototypeDungeonDoorPresenter presenter,
            DungeonRoomExitStatus status)
        {
            RoomExitDirection[] directions =
            {
                RoomExitDirection.North,
                RoomExitDirection.East,
                RoomExitDirection.South,
                RoomExitDirection.West,
            };
            return directions.Count(direction =>
                presenter.GetDisplayedStatus(direction) == status);
        }

        private static int CountVisibleSecretCracks(
            PrototypeDungeonDoorPresenter presenter)
        {
            RoomExitDirection[] directions =
            {
                RoomExitDirection.North,
                RoomExitDirection.East,
                RoomExitDirection.South,
                RoomExitDirection.West,
            };
            return directions.Count(presenter.IsSecretCrackVisible);
        }

        private static GridPosition FindWalkableNeighbor(
            PrototypeGameSession session,
            GridPosition wall)
        {
            CardinalDirection[] directions =
            {
                CardinalDirection.North,
                CardinalDirection.East,
                CardinalDirection.South,
                CardinalDirection.West,
            };
            for (int index = 0; index < directions.Length; index++)
            {
                GridPosition candidate = Offset(wall, directions[index]);
                if (session.GetCell(candidate).IsWalkableTerrain)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"Runtime secret wall {wall} has no walkable interior neighbor.");
        }

        private static IEnumerator MoveSessionTo(
            PrototypeGameSession session,
            Keyboard keyboard,
            GridPosition destination)
        {
            int movementGuard = 0;
            while (session.CurrentGridPosition != destination &&
                   movementGuard++ < 256)
            {
                IReadOnlyList<CardinalDirection> path = FindWalkablePath(
                    session,
                    session.CurrentGridPosition,
                    destination);
                Assert.That(
                    path.Count,
                    Is.GreaterThan(0),
                    $"No walkable path remains toward {destination}.");
                CardinalDirection direction = path[0];
                GridPosition start = session.CurrentGridPosition;
                session.InputReader.SetInputFocus(true);
                InputSystem.QueueStateEvent(
                    keyboard,
                    new KeyboardState(ToKey(direction)));
                InputSystem.Update();

                float deadline = Time.realtimeSinceStartup + 1.5f;
                while (session.CurrentGridPosition == start &&
                       Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(
                    session.CurrentGridPosition,
                    Is.Not.EqualTo(start),
                    $"Timed out moving {direction} toward {destination}.");
                yield return null;
            }

            Assert.That(
                session.CurrentGridPosition,
                Is.EqualTo(destination),
                $"Movement guard exhausted before reaching {destination}.");
        }

        private static IReadOnlyList<CardinalDirection> FindWalkablePath(
            PrototypeGameSession session,
            GridPosition start,
            GridPosition destination)
        {
            if (start == destination)
            {
                return Array.Empty<CardinalDirection>();
            }

            CardinalDirection[] directions =
            {
                CardinalDirection.North,
                CardinalDirection.East,
                CardinalDirection.South,
                CardinalDirection.West,
            };
            var frontier = new Queue<GridPosition>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            var arrivalDirection =
                new Dictionary<GridPosition, CardinalDirection>();
            frontier.Enqueue(start);
            previous.Add(start, start);

            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                for (int index = 0; index < directions.Length; index++)
                {
                    CardinalDirection direction = directions[index];
                    GridPosition next = Offset(current, direction);
                    if (previous.ContainsKey(next) ||
                        !session.GetCell(next).IsWalkableTerrain)
                    {
                        continue;
                    }

                    previous.Add(next, current);
                    arrivalDirection.Add(next, direction);
                    if (next == destination)
                    {
                        var reversed = new List<CardinalDirection>();
                        GridPosition step = destination;
                        while (step != start)
                        {
                            reversed.Add(arrivalDirection[step]);
                            step = previous[step];
                        }
                        reversed.Reverse();
                        return reversed;
                    }
                    frontier.Enqueue(next);
                }
            }

            throw new InvalidOperationException(
                $"No walkable path from {start} to {destination}.");
        }

        private static GridPosition Offset(
            GridPosition position,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return new GridPosition(position.X, position.Z + 1);
                case CardinalDirection.East:
                    return new GridPosition(position.X + 1, position.Z);
                case CardinalDirection.South:
                    return new GridPosition(position.X, position.Z - 1);
                case CardinalDirection.West:
                    return new GridPosition(position.X - 1, position.Z);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        null);
            }
        }

        private static Key ToKey(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return Key.W;
                case CardinalDirection.East:
                    return Key.D;
                case CardinalDirection.South:
                    return Key.S;
                case CardinalDirection.West:
                    return Key.A;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        null);
            }
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
                Assert.That(
                    presenter.IsDoorPanelVisible(localDirections[index]),
                    Is.EqualTo(graphState.Status != DungeonRoomExitStatus.SecretWall),
                    $"Authored {localDirections[index]} door visibility must match {graphState.Status}.");
                if (graphState.IsConnected)
                {
                    connectedCount++;
                    if (graphState.Status == DungeonRoomExitStatus.SecretWall)
                    {
                        Assert.That(
                            presenter.IsSecretCrackVisible(localDirections[index]),
                            Is.True);
                    }
                    else
                    {
                        Assert.That(graphState.Status, Is.EqualTo(expectedConnectedStatus));
                        Assert.That(
                            presenter.IsSecretCrackVisible(localDirections[index]),
                            Is.False);
                    }
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
                case RoomType.Recovery:
                    return "DungeonRecovery";
                case RoomType.Secret:
                    return "DungeonSecret";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        null);
            }
        }

        private static void TraverseRunTo(
            PrototypeDungeonRunSession session,
            DungeonRoomNodeId targetRoomId)
        {
            IReadOnlyList<DungeonRoomNodeId> path = session.Graph.GetShortestPath(
                session.CurrentRoomId,
                targetRoomId);
            for (int index = 1; index < path.Count; index++)
            {
                if (session.RunState.IsCurrentRoomLocked)
                {
                    Assert.That(
                        session.TryClearCurrentRoom(),
                        Is.EqualTo(DungeonRoomClearStatus.Cleared));
                }
                Assert.That(session.TryTravelTo(path[index]).Moved, Is.True);
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

        private PrototypePlayerVitalsAsset CreatePlayerVitals()
        {
            var vitals = ScriptableObject.CreateInstance<PrototypePlayerVitalsAsset>();
            vitals.Configure(5, 0.75f);
            _createdAssets.Add(vitals);
            return vitals;
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
