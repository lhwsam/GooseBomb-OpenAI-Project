using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [Serializable]
    public struct PrototypeDungeonCombatRoomEntry
    {
        [SerializeField]
        private PrototypeCombatRoomDefinitionAsset roomDefinition;

        [SerializeField]
        private string sceneName;

        public PrototypeDungeonCombatRoomEntry(
            PrototypeCombatRoomDefinitionAsset authoredRoomDefinition,
            string authoredSceneName)
        {
            roomDefinition = authoredRoomDefinition;
            sceneName = authoredSceneName;
        }

        public PrototypeCombatRoomDefinitionAsset RoomDefinition => roomDefinition;

        public string SceneName => sceneName;
    }

    [CreateAssetMenu(
        fileName = "PrototypeDungeonCombatRoomCatalog",
        menuName = "Bomb Swap/Prototype/Dungeon Combat Room Catalog")]
    public sealed class PrototypeDungeonCombatRoomCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private PrototypeDungeonCombatRoomEntry[] entries =
            Array.Empty<PrototypeDungeonCombatRoomEntry>();

        public IReadOnlyList<PrototypeDungeonCombatRoomEntry> Entries => entries;

        public void Configure(PrototypeDungeonCombatRoomEntry[] authoredEntries)
        {
            PrototypeDungeonCombatRoomEntry[] copy = CopyAndValidate(authoredEntries);
            entries = copy;
        }

        public CombatRoomDefinition[] CreateCoreDefinitions()
        {
            PrototypeDungeonCombatRoomEntry[] validated = CopyAndValidate(entries);
            var definitions = new CombatRoomDefinition[validated.Length];
            for (int index = 0; index < validated.Length; index++)
            {
                definitions[index] = validated[index].RoomDefinition.CreateCoreDefinition();
            }
            return definitions;
        }

        public PrototypeDungeonCombatRoomEntry GetEntry(RoomDefinitionId definitionId)
        {
            if (!definitionId.IsValid)
            {
                throw new ArgumentException(
                    "Room definition ID must be valid.",
                    nameof(definitionId));
            }

            PrototypeDungeonCombatRoomEntry[] validated = CopyAndValidate(entries);
            for (int index = 0; index < validated.Length; index++)
            {
                if (validated[index].RoomDefinition.CreateCoreDefinition().Id == definitionId)
                {
                    return validated[index];
                }
            }

            throw new KeyNotFoundException(
                $"Dungeon combat room catalog has no definition '{definitionId}'.");
        }

        private static PrototypeDungeonCombatRoomEntry[] CopyAndValidate(
            PrototypeDungeonCombatRoomEntry[] source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Length == 0)
            {
                throw new ArgumentException(
                    "Dungeon combat room catalog cannot be empty.",
                    nameof(source));
            }

            var copy = (PrototypeDungeonCombatRoomEntry[])source.Clone();
            var roomIds = new HashSet<RoomDefinitionId>();
            var sceneNames = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < copy.Length; index++)
            {
                PrototypeCombatRoomDefinitionAsset room = copy[index].RoomDefinition;
                if (room == null)
                {
                    throw new ArgumentException(
                        $"Dungeon combat room catalog entry {index} has no room definition.",
                        nameof(source));
                }
                if (string.IsNullOrWhiteSpace(copy[index].SceneName))
                {
                    throw new ArgumentException(
                        $"Dungeon combat room catalog entry {index} has no scene name.",
                        nameof(source));
                }

                CombatRoomDefinition definition = room.CreateCoreDefinition();
                if (!roomIds.Add(definition.Id))
                {
                    throw new ArgumentException(
                        $"Dungeon combat room ID '{definition.Id}' is duplicated.",
                        nameof(source));
                }
                if (!sceneNames.Add(copy[index].SceneName))
                {
                    throw new ArgumentException(
                        $"Dungeon combat room scene '{copy[index].SceneName}' is duplicated.",
                        nameof(source));
                }
            }
            return copy;
        }
    }
}
