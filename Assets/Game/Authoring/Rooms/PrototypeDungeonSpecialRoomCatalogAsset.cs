using System;
using System.Collections.Generic;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    [Serializable]
    public struct PrototypeDungeonSpecialRoomEntry
    {
        [SerializeField]
        private RoomType roomType;

        [SerializeField]
        private string sceneName;

        public PrototypeDungeonSpecialRoomEntry(
            RoomType authoredRoomType,
            string authoredSceneName)
        {
            roomType = authoredRoomType;
            sceneName = authoredSceneName;
        }

        public RoomType RoomType => roomType;

        public string SceneName => sceneName;
    }

    [CreateAssetMenu(
        fileName = "PrototypeDungeonSpecialRoomCatalog",
        menuName = "Bomb Swap/Prototype/Dungeon Special Room Catalog")]
    public sealed class PrototypeDungeonSpecialRoomCatalogAsset : ScriptableObject
    {
        private static readonly RoomType[] RequiredRoomTypes =
        {
            RoomType.Start,
            RoomType.BombReward,
            RoomType.BossAntechamber,
            RoomType.Boss,
        };

        [SerializeField]
        private PrototypeDungeonSpecialRoomEntry[] entries =
            Array.Empty<PrototypeDungeonSpecialRoomEntry>();

        public IReadOnlyList<PrototypeDungeonSpecialRoomEntry> Entries => entries;

        public void Configure(PrototypeDungeonSpecialRoomEntry[] authoredEntries)
        {
            PrototypeDungeonSpecialRoomEntry[] copy = ValidateAndCopy(authoredEntries);
            entries = copy;
        }

        public void Validate()
        {
            ValidateAndCopy(entries);
        }

        public string GetSceneName(RoomType roomType)
        {
            ValidateSpecialRoomType(roomType);
            ValidateAndCopy(entries);
            for (int index = 0; index < entries.Length; index++)
            {
                if (entries[index].RoomType == roomType)
                {
                    return entries[index].SceneName;
                }
            }

            throw new KeyNotFoundException(
                $"Dungeon special room type {roomType} is not in the catalog.");
        }

        private static PrototypeDungeonSpecialRoomEntry[] ValidateAndCopy(
            PrototypeDungeonSpecialRoomEntry[] source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Length != RequiredRoomTypes.Length)
            {
                throw new ArgumentException(
                    $"Dungeon special room catalog requires exactly {RequiredRoomTypes.Length} entries.",
                    nameof(source));
            }

            var copy = new PrototypeDungeonSpecialRoomEntry[source.Length];
            var roomTypes = new HashSet<RoomType>();
            var sceneNames = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < source.Length; index++)
            {
                PrototypeDungeonSpecialRoomEntry entry = source[index];
                ValidateSpecialRoomType(entry.RoomType);
                if (string.IsNullOrWhiteSpace(entry.SceneName))
                {
                    throw new ArgumentException(
                        "Dungeon special room scene name cannot be empty.",
                        nameof(source));
                }
                if (!roomTypes.Add(entry.RoomType))
                {
                    throw new ArgumentException(
                        $"Dungeon special room type {entry.RoomType} is duplicated.",
                        nameof(source));
                }
                if (!sceneNames.Add(entry.SceneName))
                {
                    throw new ArgumentException(
                        $"Dungeon special room scene '{entry.SceneName}' is duplicated.",
                        nameof(source));
                }
                copy[index] = entry;
            }

            for (int index = 0; index < RequiredRoomTypes.Length; index++)
            {
                if (!roomTypes.Contains(RequiredRoomTypes[index]))
                {
                    throw new ArgumentException(
                        $"Dungeon special room catalog is missing {RequiredRoomTypes[index]}.",
                        nameof(source));
                }
            }
            return copy;
        }

        private static void ValidateSpecialRoomType(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Start:
                case RoomType.BombReward:
                case RoomType.BossAntechamber:
                case RoomType.Boss:
                    return;
                case RoomType.Combat:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        "Combat rooms belong to the combat room catalog.");
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roomType),
                        roomType,
                        "Unsupported dungeon special room type.");
            }
        }
    }
}
