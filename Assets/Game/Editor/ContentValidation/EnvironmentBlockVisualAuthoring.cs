using System;
using BombSwap.Core;
using UnityEditor;
using UnityEngine;

namespace BombSwap.Editor.ContentValidation
{
    internal static class EnvironmentBlockVisualAuthoring
    {
        public const float FloorVisualRootY = -1f;

        public const string BrickBlockPrefabPath =
            "Assets/Game/Content/Prefabs/Environment/BrickBlock.prefab";
        public const string BrickCornerPrefabPath =
            "Assets/Game/Content/Prefabs/Environment/BrickCorner.prefab";
        public const string WoodBoxPrefabPath =
            "Assets/Game/Content/Prefabs/Environment/WoodBox.prefab";
        public const string DoorPrefabPath =
            "Assets/Game/Content/Prefabs/Environment/Door.prefab";
        public const string CrackedBrickBlockPrefabPath =
            "Assets/Game/Content/Prefabs/Environment/CrackedBrickBlock.prefab";

        public static void Synchronize(
            Transform gridRoot,
            PrototypeCombatRoomDefinitionAsset definition,
            CombatRoomDefinition room)
        {
            GameObject brick = LoadPrefab(BrickBlockPrefabPath);
            GameObject corner = LoadPrefab(BrickCornerPrefabPath);
            GameObject woodBox = LoadPrefab(WoodBoxPrefabPath);
            Transform environment = gridRoot.Find("Environment") ??
                throw new InvalidOperationException("GridRoot is missing Environment.");

            SynchronizeFloor(environment, room, definition.CellSize, brick);
            SynchronizeBoundary(environment, room, definition.CellSize, corner);
            SynchronizeInterior(environment, corner);
            SynchronizeDestructibles(environment, woodBox);
        }

        private static void SynchronizeFloor(
            Transform environment,
            CombatRoomDefinition room,
            float cellSize,
            GameObject prefab)
        {
            Transform floor = environment.Find("Floor");
            if (floor == null)
            {
                throw new InvalidOperationException("Environment is missing its logical Floor.");
            }
            SetRenderersEnabled(floor, false, includeChildren: false);

            Transform visuals = GetOrCreateRoot(environment, "FloorVisuals");
            Vector3 floorVisualRootPosition = visuals.localPosition;
            floorVisualRootPosition.y = FloorVisualRootY;
            visuals.localPosition = floorVisualRootPosition;
            var expectedNames = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            int halfWidth = room.Width / 2;
            int halfDepth = room.Depth / 2;
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int z = -halfDepth; z <= halfDepth; z++)
                {
                    string name = $"Floor_{x}_{z}";
                    expectedNames.Add(name);
                    EnsureVisual(
                        prefab,
                        visuals,
                        name,
                        new Vector3(x * cellSize, 0f, z * cellSize),
                        Quaternion.identity);
                }
            }
            int edgeX = halfWidth + 1;
            int edgeZ = halfDepth + 1;
            EnsureExpectedVisual(prefab, visuals, expectedNames, "Floor_Door_North", new Vector3(0f, 0f, edgeZ * cellSize), Quaternion.identity);
            EnsureExpectedVisual(prefab, visuals, expectedNames, "Floor_Door_East", new Vector3(edgeX * cellSize, 0f, 0f), Quaternion.identity);
            EnsureExpectedVisual(prefab, visuals, expectedNames, "Floor_Door_South", new Vector3(0f, 0f, -edgeZ * cellSize), Quaternion.identity);
            EnsureExpectedVisual(prefab, visuals, expectedNames, "Floor_Door_West", new Vector3(-edgeX * cellSize, 0f, 0f), Quaternion.identity);
            RemoveUnexpectedChildren(visuals, expectedNames);
        }

        private static void SynchronizeBoundary(
            Transform environment,
            CombatRoomDefinition room,
            float cellSize,
            GameObject corner)
        {
            Transform logicalBoundary = environment.Find("BoundaryWalls");
            if (logicalBoundary == null)
            {
                throw new InvalidOperationException("Environment is missing BoundaryWalls.");
            }
            for (int index = 0; index < logicalBoundary.childCount; index++)
            {
                Transform child = logicalBoundary.GetChild(index);
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null && child.name.Contains("Wall"))
                {
                    renderer.enabled = false;
                }
            }

            Transform visuals = GetOrCreateRoot(environment, "BoundaryVisuals");
            var expectedNames = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            RemoveChild(environment, "BoundaryBaseVisuals");
            int edgeX = (room.Width / 2) + 1;
            int edgeZ = (room.Depth / 2) + 1;
            EnsureExpectedVisual(corner, visuals, expectedNames, "Corner_NorthWest", new Vector3(-edgeX * cellSize, 0f, edgeZ * cellSize), Quaternion.identity);
            EnsureExpectedVisual(corner, visuals, expectedNames, "Corner_NorthEast", new Vector3(edgeX * cellSize, 0f, edgeZ * cellSize), Quaternion.Euler(0f, 90f, 0f));
            EnsureExpectedVisual(corner, visuals, expectedNames, "Corner_SouthEast", new Vector3(edgeX * cellSize, 0f, -edgeZ * cellSize), Quaternion.Euler(0f, 180f, 0f));
            EnsureExpectedVisual(corner, visuals, expectedNames, "Corner_SouthWest", new Vector3(-edgeX * cellSize, 0f, -edgeZ * cellSize), Quaternion.Euler(0f, 270f, 0f));

            for (int x = -edgeX + 1; x < edgeX; x++)
            {
                if (x == 0)
                {
                    continue;
                }
                EnsureExpectedVisual(corner, visuals, expectedNames, $"NorthWall_{x}", new Vector3(x * cellSize, 0f, edgeZ * cellSize), Quaternion.identity);
                EnsureExpectedVisual(corner, visuals, expectedNames, $"SouthWall_{x}", new Vector3(x * cellSize, 0f, -edgeZ * cellSize), Quaternion.Euler(0f, 180f, 0f));
            }
            for (int z = -edgeZ + 1; z < edgeZ; z++)
            {
                if (z == 0)
                {
                    continue;
                }
                EnsureExpectedVisual(corner, visuals, expectedNames, $"EastWall_{z}", new Vector3(edgeX * cellSize, 0f, z * cellSize), Quaternion.Euler(0f, 90f, 0f));
                EnsureExpectedVisual(corner, visuals, expectedNames, $"WestWall_{z}", new Vector3(-edgeX * cellSize, 0f, z * cellSize), Quaternion.Euler(0f, 270f, 0f));
            }
            RemoveUnexpectedChildren(visuals, expectedNames);
        }

        private static void SynchronizeInterior(Transform environment, GameObject prefab)
        {
            Transform obstacles = environment.Find("InteriorObstacles") ??
                throw new InvalidOperationException("Environment is missing InteriorObstacles.");
            for (int index = 0; index < obstacles.childCount; index++)
            {
                Transform obstacle = obstacles.GetChild(index);
                SetRenderersEnabled(obstacle, false, includeChildren: false);
                EnsureVisual(prefab, obstacle, "Visual", Vector3.zero, Quaternion.identity);
            }
        }

        private static void SynchronizeDestructibles(Transform environment, GameObject prefab)
        {
            Transform obstacles = environment.Find("DestructibleObstacles") ??
                throw new InvalidOperationException("Environment is missing DestructibleObstacles.");
            for (int index = 0; index < obstacles.childCount; index++)
            {
                Transform obstacle = obstacles.GetChild(index);
                EnsureVisual(prefab, obstacle, "Visual", Vector3.zero, Quaternion.identity);
            }
        }

        private static Transform GetOrCreateRoot(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private static void EnsureExpectedVisual(
            GameObject prefab,
            Transform parent,
            System.Collections.Generic.ISet<string> expectedNames,
            string name,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            expectedNames.Add(name);
            EnsureVisual(prefab, parent, name, localPosition, localRotation);
        }

        private static void RemoveUnexpectedChildren(
            Transform parent,
            System.Collections.Generic.ISet<string> expectedNames)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (!expectedNames.Contains(child.name))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void EnsureVisual(
            GameObject prefab,
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            Transform existing = parent.Find(name);
            GameObject instance = existing != null ? existing.gameObject : null;
            if (instance != null &&
                PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefab)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                instance = null;
            }
            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            }
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to instantiate '{prefab.name}'.");
            }
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException($"Environment visual prefab is missing at '{path}'.");
        }

        private static void SetRenderersEnabled(
            Transform root,
            bool enabled,
            bool includeChildren)
        {
            Renderer[] renderers = includeChildren
                ? root.GetComponentsInChildren<Renderer>(true)
                : root.GetComponents<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enabled;
            }
        }
    }
}
