using System;
using System.Collections.Generic;
using BombSwap.Core;

namespace BombSwap
{
    public readonly struct PrototypeDungeonCombatRoomSelection
    {
        internal PrototypeDungeonCombatRoomSelection(
            PrototypeCombatRoomDefinitionAsset roomDefinition,
            string sceneName,
            DungeonCombatRoomAssignment assignment)
        {
            RoomDefinition = roomDefinition ??
                throw new ArgumentNullException(nameof(roomDefinition));
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException(
                    "Dungeon combat room scene name cannot be empty.",
                    nameof(sceneName));
            }
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            SceneName = sceneName;
        }

        public PrototypeCombatRoomDefinitionAsset RoomDefinition { get; }

        public string SceneName { get; }

        public DungeonCombatRoomAssignment Assignment { get; }
    }

    public sealed class PrototypeDungeonRunSession
    {
        private readonly PrototypeDungeonCombatRoomCatalogAsset _catalog;
        private readonly PrototypeDungeonSpecialRoomCatalogAsset _specialRoomCatalog;

        public PrototypeDungeonRunSession(
            int seed,
            PrototypeDungeonCombatRoomCatalogAsset catalog)
            : this(seed, catalog, null)
        {
        }

        public PrototypeDungeonRunSession(
            int seed,
            PrototypeDungeonCombatRoomCatalogAsset catalog,
            PrototypeDungeonSpecialRoomCatalogAsset specialRoomCatalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _specialRoomCatalog = specialRoomCatalog;
            _specialRoomCatalog?.Validate();
            Seed = seed;
            Graph = DungeonGenerator.Generate(seed);
            CombatRoomLayout = DungeonCombatRoomAssigner.Assign(
                Graph,
                catalog.CreateCoreDefinitions());
            RunState = new DungeonRunState(Graph);
        }

        public int Seed { get; }

        public DungeonGraph Graph { get; }

        public DungeonCombatRoomLayout CombatRoomLayout { get; }

        public DungeonRunState RunState { get; }

        public DungeonRoomNodeId CurrentRoomId => RunState.CurrentRoomId;

        public DungeonTravelResult TryTravel(RoomExitDirection direction)
        {
            return RunState.TryTravel(direction);
        }

        public DungeonTravelResult TryTravelTo(DungeonRoomNodeId roomId)
        {
            return RunState.TryTravelTo(roomId);
        }

        public DungeonRoomClearStatus TryClearCurrentRoom()
        {
            return RunState.TryClearCurrentRoom();
        }

        public IReadOnlyList<DungeonRoomExitState> GetCurrentExitStates()
        {
            return RunState.GetCurrentExitStates();
        }

        public bool TryGetCurrentSceneName(out string sceneName)
        {
            return TryGetSceneName(CurrentRoomId, out sceneName);
        }

        public bool TryGetSceneName(
            DungeonRoomNodeId roomId,
            out string sceneName)
        {
            DungeonRoomNode room = Graph.GetRoom(roomId);
            if (room.RoomType == RoomType.Combat)
            {
                TryGetCombatRoom(roomId, out PrototypeDungeonCombatRoomSelection selection);
                sceneName = selection.SceneName;
                return true;
            }
            if (_specialRoomCatalog == null)
            {
                sceneName = string.Empty;
                return false;
            }

            sceneName = _specialRoomCatalog.GetSceneName(room.RoomType);
            return true;
        }

        public bool TryGetCurrentCombatRoom(
            out PrototypeDungeonCombatRoomSelection selection)
        {
            return TryGetCombatRoom(CurrentRoomId, out selection);
        }

        public bool TryGetCombatRoom(
            DungeonRoomNodeId roomId,
            out PrototypeDungeonCombatRoomSelection selection)
        {
            DungeonRoomNode room = Graph.GetRoom(roomId);
            if (room.RoomType != RoomType.Combat)
            {
                selection = default;
                return false;
            }

            DungeonCombatRoomAssignment assignment =
                CombatRoomLayout.GetAssignment(roomId);
            PrototypeDungeonCombatRoomEntry entry =
                _catalog.GetEntry(assignment.DefinitionId);
            selection = new PrototypeDungeonCombatRoomSelection(
                entry.RoomDefinition,
                entry.SceneName,
                assignment);
            return true;
        }
    }
}
