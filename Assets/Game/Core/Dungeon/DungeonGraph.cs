using System;
using System.Collections.Generic;

namespace BombSwap.Core
{
    public sealed class DungeonGraph
    {
        private readonly DungeonRoomNode[] _rooms;
        private readonly DungeonRoomConnection[] _connections;
        private readonly IReadOnlyList<DungeonRoomNodeId>[] _neighbors;
        private DungeonRoomNodeId _startRoomId;
        private DungeonRoomNodeId _bombRewardRoomId;
        private DungeonRoomNodeId _bossAntechamberRoomId;
        private DungeonRoomNodeId _bossRoomId;
        private DungeonRoomNodeId _recoveryRoomId;
        private DungeonRoomNodeId _secretRoomId;

        internal DungeonGraph(
            int seed,
            string generationVersion,
            DungeonGenerationDefinition definition,
            DungeonRoomNode[] rooms,
            DungeonRoomConnection[] connections)
        {
            if (string.IsNullOrWhiteSpace(generationVersion))
            {
                throw new ArgumentException(
                    "Dungeon generation version cannot be empty.",
                    nameof(generationVersion));
            }
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (rooms == null)
            {
                throw new ArgumentNullException(nameof(rooms));
            }
            if (connections == null)
            {
                throw new ArgumentNullException(nameof(connections));
            }

            Seed = seed;
            GenerationVersion = generationVersion;
            _rooms = (DungeonRoomNode[])rooms.Clone();
            _connections = (DungeonRoomConnection[])connections.Clone();
            Rooms = Array.AsReadOnly(_rooms);
            Connections = Array.AsReadOnly(_connections);
            _neighbors = ValidateAndBuildNeighbors();
            ValidateRequiredTopology();
        }

        public int Seed { get; }

        public string GenerationVersion { get; }

        public DungeonGenerationDefinition Definition { get; }

        public IReadOnlyList<DungeonRoomNode> Rooms { get; }

        public IReadOnlyList<DungeonRoomConnection> Connections { get; }

        public DungeonRoomNodeId StartRoomId => _startRoomId;

        public DungeonRoomNodeId BombRewardRoomId => _bombRewardRoomId;

        public DungeonRoomNodeId BossAntechamberRoomId => _bossAntechamberRoomId;

        public DungeonRoomNodeId BossRoomId => _bossRoomId;

        public DungeonRoomNodeId RecoveryRoomId => _recoveryRoomId;

        public DungeonRoomNodeId SecretRoomId => _secretRoomId;

        public bool HasSecretRoom => _secretRoomId.IsValid;

        public int CombatRoomCount { get; private set; }

        public DungeonRoomNode GetRoom(DungeonRoomNodeId roomId)
        {
            int index = GetRoomIndex(roomId);
            return _rooms[index];
        }

        public IReadOnlyList<DungeonRoomNodeId> GetNeighbors(DungeonRoomNodeId roomId)
        {
            return _neighbors[GetRoomIndex(roomId)];
        }

        public bool TryGetConnection(
            DungeonRoomNodeId first,
            DungeonRoomNodeId second,
            out DungeonRoomConnection connection)
        {
            GetRoomIndex(first);
            GetRoomIndex(second);
            for (int index = 0; index < _connections.Length; index++)
            {
                DungeonRoomConnection candidate = _connections[index];
                if (candidate.Contains(first) && candidate.Contains(second))
                {
                    connection = candidate;
                    return true;
                }
            }

            connection = default;
            return false;
        }

        public bool TryGetNeighbor(
            DungeonRoomNodeId from,
            RoomExitDirection direction,
            out DungeonRoomNodeId neighbor)
        {
            ValidateExitDirection(direction);
            IReadOnlyList<DungeonRoomNodeId> neighbors = GetNeighbors(from);
            for (int index = 0; index < neighbors.Count; index++)
            {
                DungeonRoomNodeId candidate = neighbors[index];
                if (GetExitDirection(from, candidate) == direction)
                {
                    neighbor = candidate;
                    return true;
                }
            }

            neighbor = default;
            return false;
        }

        public RoomExitDirection GetExitDirection(
            DungeonRoomNodeId from,
            DungeonRoomNodeId to)
        {
            DungeonRoomNode fromRoom = GetRoom(from);
            DungeonRoomNode toRoom = GetRoom(to);
            IReadOnlyList<DungeonRoomNodeId> neighbors = _neighbors[from.Value - 1];
            bool connected = false;
            for (int index = 0; index < neighbors.Count; index++)
            {
                if (neighbors[index] == to)
                {
                    connected = true;
                    break;
                }
            }
            if (!connected)
            {
                throw new InvalidOperationException(
                    $"Dungeon rooms {from} and {to} are not directly connected.");
            }

            long deltaX = (long)toRoom.Position.X - fromRoom.Position.X;
            long deltaZ = (long)toRoom.Position.Z - fromRoom.Position.Z;
            if (deltaX == 0L && deltaZ == 1L)
            {
                return RoomExitDirection.North;
            }
            if (deltaX == 1L && deltaZ == 0L)
            {
                return RoomExitDirection.East;
            }
            if (deltaX == 0L && deltaZ == -1L)
            {
                return RoomExitDirection.South;
            }
            if (deltaX == -1L && deltaZ == 0L)
            {
                return RoomExitDirection.West;
            }

            throw new InvalidOperationException(
                $"Connected dungeon rooms {from} and {to} are not cardinally adjacent.");
        }

        public int GetDistance(DungeonRoomNodeId from, DungeonRoomNodeId to)
        {
            IReadOnlyList<DungeonRoomNodeId> path = GetShortestPath(from, to);
            return path.Count - 1;
        }

        public IReadOnlyList<DungeonRoomNodeId> GetShortestPath(
            DungeonRoomNodeId from,
            DungeonRoomNodeId to)
        {
            int fromIndex = GetRoomIndex(from);
            int toIndex = GetRoomIndex(to);
            var previous = new int[_rooms.Length];
            for (int index = 0; index < previous.Length; index++)
            {
                previous[index] = -1;
            }

            var queue = new Queue<int>();
            bool includeSecretConnections =
                _rooms[fromIndex].RoomType == RoomType.Secret ||
                _rooms[toIndex].RoomType == RoomType.Secret;
            previous[fromIndex] = fromIndex;
            queue.Enqueue(fromIndex);
            while (queue.Count > 0 && previous[toIndex] < 0)
            {
                int current = queue.Dequeue();
                IReadOnlyList<DungeonRoomNodeId> neighbors = _neighbors[current];
                for (int index = 0; index < neighbors.Count; index++)
                {
                    DungeonRoomNodeId neighborId = neighbors[index];
                    if (!TryGetConnection(
                            _rooms[current].Id,
                            neighborId,
                            out DungeonRoomConnection connection) ||
                        (!includeSecretConnections &&
                            connection.Kind == DungeonRoomConnectionKind.Secret))
                    {
                        continue;
                    }

                    int neighbor = neighborId.Value - 1;
                    if (previous[neighbor] >= 0)
                    {
                        continue;
                    }

                    previous[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            if (previous[toIndex] < 0)
            {
                throw new InvalidOperationException(
                    $"Dungeon rooms {from} and {to} are not connected.");
            }

            var reversed = new List<DungeonRoomNodeId>();
            int cursor = toIndex;
            while (cursor != fromIndex)
            {
                reversed.Add(_rooms[cursor].Id);
                cursor = previous[cursor];
            }
            reversed.Add(_rooms[fromIndex].Id);
            reversed.Reverse();
            return Array.AsReadOnly(reversed.ToArray());
        }

        private IReadOnlyList<DungeonRoomNodeId>[] ValidateAndBuildNeighbors()
        {
            if (_rooms.Length == 0)
            {
                throw new ArgumentException("Dungeon graph requires rooms.");
            }

            var positions = new HashSet<RoomGraphPosition>();
            var mutableNeighbors = new List<DungeonRoomNodeId>[_rooms.Length];
            int secretRoomCount = 0;
            for (int index = 0; index < _rooms.Length; index++)
            {
                DungeonRoomNode room = _rooms[index] ??
                    throw new ArgumentException("Dungeon graph cannot contain null rooms.");
                if (room.Id.Value != index + 1)
                {
                    throw new ArgumentException(
                        "Dungeon room IDs must be contiguous and ordered from one.");
                }
                DungeonRoomNode.ValidateRoomType(room.RoomType);
                if (!positions.Add(room.Position))
                {
                    throw new ArgumentException(
                        $"Dungeon room position {room.Position} is duplicated.");
                }
                if (room.RoomType == RoomType.Secret)
                {
                    secretRoomCount++;
                }
                mutableNeighbors[index] = new List<DungeonRoomNodeId>();
            }

            if (secretRoomCount > 1)
            {
                throw new ArgumentException(
                    "Dungeon graph supports at most one secret room.");
            }

            var connectionEndpoints = new HashSet<long>();
            int normalConnectionCount = 0;
            int secretConnectionCount = 0;
            for (int index = 0; index < _connections.Length; index++)
            {
                DungeonRoomConnection connection = _connections[index];
                int first = GetRoomIndex(connection.First);
                int second = GetRoomIndex(connection.Second);
                if (!connectionEndpoints.Add(CreateEndpointKey(connection.First, connection.Second)))
                {
                    throw new ArgumentException(
                        $"Dungeon connection endpoints {connection.First} and " +
                        $"{connection.Second} are duplicated.");
                }
                if (!_rooms[first].Position.IsCardinallyAdjacentTo(_rooms[second].Position))
                {
                    throw new ArgumentException(
                        $"Connected rooms {connection} must be cardinally adjacent.");
                }

                bool firstSecret = _rooms[first].RoomType == RoomType.Secret;
                bool secondSecret = _rooms[second].RoomType == RoomType.Secret;
                switch (connection.Kind)
                {
                    case DungeonRoomConnectionKind.Normal:
                        normalConnectionCount++;
                        if (firstSecret || secondSecret)
                        {
                            throw new ArgumentException(
                                "Normal dungeon connections cannot include a secret room.");
                        }
                        break;
                    case DungeonRoomConnectionKind.Secret:
                        secretConnectionCount++;
                        if (firstSecret == secondSecret ||
                            (!firstSecret && _rooms[first].RoomType != RoomType.Combat) ||
                            (!secondSecret && _rooms[second].RoomType != RoomType.Combat))
                        {
                            throw new ArgumentException(
                                "Secret connections must connect one secret room to one combat room.");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(connection.Kind),
                            connection.Kind,
                            "Unsupported dungeon connection kind.");
                }

                mutableNeighbors[first].Add(connection.Second);
                mutableNeighbors[second].Add(connection.First);
            }

            int normalRoomCount = _rooms.Length - secretRoomCount;
            if (normalConnectionCount != normalRoomCount - 1)
            {
                throw new ArgumentException(
                    "Normal dungeon rooms must form a tree with room-count minus one connections.");
            }
            if ((secretRoomCount == 0 && secretConnectionCount != 0) ||
                (secretRoomCount == 1 &&
                    (secretConnectionCount < 2 || secretConnectionCount > 3)))
            {
                throw new ArgumentException(
                    "A secret room must have exactly two or three secret connections.");
            }

            for (int left = 0; left < _rooms.Length; left++)
            {
                for (int right = left + 1; right < _rooms.Length; right++)
                {
                    if (!_rooms[left].Position.IsCardinallyAdjacentTo(_rooms[right].Position))
                    {
                        continue;
                    }

                    if (!connectionEndpoints.Contains(
                        CreateEndpointKey(_rooms[left].Id, _rooms[right].Id)))
                    {
                        throw new ArgumentException(
                            $"Unconnected rooms {_rooms[left].Id} and {_rooms[right].Id} " +
                            "cannot be cardinally adjacent.");
                    }
                }
            }

            var neighbors = new IReadOnlyList<DungeonRoomNodeId>[_rooms.Length];
            for (int index = 0; index < mutableNeighbors.Length; index++)
            {
                mutableNeighbors[index].Sort();
                neighbors[index] = Array.AsReadOnly(mutableNeighbors[index].ToArray());
            }

            ValidateNormalRoomsConnected(neighbors);
            ValidateConnected(neighbors);
            return neighbors;
        }

        private void ValidateNormalRoomsConnected(
            IReadOnlyList<DungeonRoomNodeId>[] neighbors)
        {
            int firstNormalIndex = -1;
            int expectedCount = 0;
            for (int index = 0; index < _rooms.Length; index++)
            {
                if (_rooms[index].RoomType == RoomType.Secret)
                {
                    continue;
                }
                expectedCount++;
                if (firstNormalIndex < 0)
                {
                    firstNormalIndex = index;
                }
            }

            var visited = new bool[_rooms.Length];
            var queue = new Queue<int>();
            visited[firstNormalIndex] = true;
            queue.Enqueue(firstNormalIndex);
            int visitedCount = 0;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                visitedCount++;
                IReadOnlyList<DungeonRoomNodeId> currentNeighbors = neighbors[current];
                for (int index = 0; index < currentNeighbors.Count; index++)
                {
                    DungeonRoomNodeId neighborId = currentNeighbors[index];
                    int neighbor = neighborId.Value - 1;
                    if (_rooms[neighbor].RoomType == RoomType.Secret || visited[neighbor])
                    {
                        continue;
                    }
                    if (!TryGetConnection(
                            _rooms[current].Id,
                            neighborId,
                            out DungeonRoomConnection connection) ||
                        connection.Kind != DungeonRoomConnectionKind.Normal)
                    {
                        continue;
                    }

                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }

            if (visitedCount != expectedCount)
            {
                throw new ArgumentException(
                    "Normal dungeon rooms must remain connected without secret-room shortcuts.");
            }
        }

        private void ValidateConnected(IReadOnlyList<DungeonRoomNodeId>[] neighbors)
        {
            var visited = new bool[_rooms.Length];
            var queue = new Queue<int>();
            visited[0] = true;
            queue.Enqueue(0);
            int visitedCount = 0;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                visitedCount++;
                IReadOnlyList<DungeonRoomNodeId> currentNeighbors = neighbors[current];
                for (int index = 0; index < currentNeighbors.Count; index++)
                {
                    int neighbor = currentNeighbors[index].Value - 1;
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (visitedCount != _rooms.Length)
            {
                throw new ArgumentException("Dungeon graph must be connected.");
            }
        }

        private void ValidateRequiredTopology()
        {
            for (int index = 0; index < _rooms.Length; index++)
            {
                DungeonRoomNode room = _rooms[index];
                switch (room.RoomType)
                {
                    case RoomType.Start:
                        AssignUnique(ref _startRoomId, room.Id, RoomType.Start);
                        break;
                    case RoomType.BombReward:
                        AssignUnique(
                            ref _bombRewardRoomId,
                            room.Id,
                            RoomType.BombReward);
                        break;
                    case RoomType.BossAntechamber:
                        AssignUnique(
                            ref _bossAntechamberRoomId,
                            room.Id,
                            RoomType.BossAntechamber);
                        break;
                    case RoomType.Boss:
                        AssignUnique(ref _bossRoomId, room.Id, RoomType.Boss);
                        break;
                    case RoomType.Recovery:
                        AssignUnique(
                            ref _recoveryRoomId,
                            room.Id,
                            RoomType.Recovery);
                        break;
                    case RoomType.Secret:
                        AssignUnique(ref _secretRoomId, room.Id, RoomType.Secret);
                        break;
                    case RoomType.Combat:
                        CombatRoomCount++;
                        break;
                }
            }

            RequireAssigned(StartRoomId, RoomType.Start);
            RequireAssigned(BombRewardRoomId, RoomType.BombReward);
            RequireAssigned(BossAntechamberRoomId, RoomType.BossAntechamber);
            RequireAssigned(BossRoomId, RoomType.Boss);
            RequireAssigned(RecoveryRoomId, RoomType.Recovery);
            if (CombatRoomCount < Definition.MinimumCombatRooms ||
                CombatRoomCount > Definition.MaximumCombatRooms)
            {
                throw new ArgumentException(
                    $"Combat room count {CombatRoomCount} is outside the generation definition.");
            }

            if (_neighbors[StartRoomId.Value - 1].Count != 1)
            {
                throw new ArgumentException("Start room must have exactly one connection.");
            }
            if (_neighbors[BossRoomId.Value - 1].Count != 1 ||
                _neighbors[BossRoomId.Value - 1][0] != BossAntechamberRoomId)
            {
                throw new ArgumentException(
                    "Boss room must connect only to the boss antechamber.");
            }
            if (_neighbors[BossAntechamberRoomId.Value - 1].Count != 2)
            {
                throw new ArgumentException(
                    "Boss antechamber must connect the combat path and boss only.");
            }

            if (HasSecretRoom)
            {
                IReadOnlyList<DungeonRoomNodeId> secretNeighbors =
                    _neighbors[SecretRoomId.Value - 1];
                if (secretNeighbors.Count < 2 || secretNeighbors.Count > 3)
                {
                    throw new ArgumentException(
                        "Secret room must connect to two or three combat rooms.");
                }
                for (int index = 0; index < secretNeighbors.Count; index++)
                {
                    DungeonRoomNodeId neighbor = secretNeighbors[index];
                    if (GetRoom(neighbor).RoomType != RoomType.Combat ||
                        !TryGetConnection(
                            SecretRoomId,
                            neighbor,
                            out DungeonRoomConnection connection) ||
                        connection.Kind != DungeonRoomConnectionKind.Secret)
                    {
                        throw new ArgumentException(
                            "Secret room connections must lead only to combat rooms.");
                    }
                }
            }

            IReadOnlyList<DungeonRoomNodeId> rewardPath =
                GetShortestPath(StartRoomId, BombRewardRoomId);
            if (rewardPath.Count != 3 ||
                GetRoom(rewardPath[0]).RoomType != RoomType.Start ||
                GetRoom(rewardPath[1]).RoomType != RoomType.Combat ||
                GetRoom(rewardPath[2]).RoomType != RoomType.BombReward)
            {
                throw new ArgumentException(
                    "Bomb reward must follow the first combat room from the start.");
            }

            IReadOnlyList<DungeonRoomNodeId> bossPath =
                GetShortestPath(StartRoomId, BossRoomId);
            int bossPathCombatCount = 0;
            bool rewardFound = false;
            for (int index = 0; index < bossPath.Count; index++)
            {
                RoomType type = GetRoom(bossPath[index]).RoomType;
                if (type == RoomType.Combat)
                {
                    bossPathCombatCount++;
                }
                else if (type == RoomType.BombReward)
                {
                    rewardFound = true;
                }
            }

            if (!rewardFound ||
                bossPathCombatCount != Definition.BossPathCombatRooms ||
                bossPath[bossPath.Count - 2] != BossAntechamberRoomId)
            {
                throw new ArgumentException(
                    "Boss path must contain the reward, configured combat count, and antechamber.");
            }

            DungeonRoomNodeId lastBossPathCombatRoomId =
                bossPath[bossPath.Count - 3];
            if (GetRoom(lastBossPathCombatRoomId).RoomType != RoomType.Combat ||
                _neighbors[RecoveryRoomId.Value - 1].Count != 1 ||
                _neighbors[RecoveryRoomId.Value - 1][0] != lastBossPathCombatRoomId)
            {
                throw new ArgumentException(
                    "Recovery room must be a leaf attached to the final boss-path combat room.");
            }

            var bossPathSet = new HashSet<DungeonRoomNodeId>();
            for (int index = 0; index < bossPath.Count; index++)
            {
                bossPathSet.Add(bossPath[index]);
            }
            int branchCombatCount = 0;
            int branchAttachmentCount = 0;
            for (int index = 0; index < _rooms.Length; index++)
            {
                if (_rooms[index].RoomType == RoomType.Combat &&
                    !bossPathSet.Contains(_rooms[index].Id))
                {
                    branchCombatCount++;
                    IReadOnlyList<DungeonRoomNodeId> branchPath =
                        GetShortestPath(StartRoomId, _rooms[index].Id);
                    bool rewardOnBranchPath = false;
                    for (int pathIndex = 0; pathIndex < branchPath.Count; pathIndex++)
                    {
                        if (branchPath[pathIndex] == BombRewardRoomId)
                        {
                            rewardOnBranchPath = true;
                            break;
                        }
                    }
                    if (!rewardOnBranchPath)
                    {
                        throw new ArgumentException(
                            "Optional combat branches must be reachable after the bomb reward.");
                    }
                }
            }
            if (branchCombatCount <= 0)
            {
                throw new ArgumentException(
                    "Dungeon graph requires at least one optional combat branch room.");
            }

            for (int index = 0; index < _connections.Length; index++)
            {
                if (_connections[index].Kind != DungeonRoomConnectionKind.Normal)
                {
                    continue;
                }
                DungeonRoomNode firstRoom = GetRoom(_connections[index].First);
                DungeonRoomNode secondRoom = GetRoom(_connections[index].Second);
                bool firstOnBossPath = bossPathSet.Contains(firstRoom.Id);
                bool secondOnBossPath = bossPathSet.Contains(secondRoom.Id);
                bool firstOptionalCombat =
                    firstRoom.RoomType == RoomType.Combat && !firstOnBossPath;
                bool secondOptionalCombat =
                    secondRoom.RoomType == RoomType.Combat && !secondOnBossPath;
                if ((firstOnBossPath && secondOptionalCombat) ||
                    (secondOnBossPath && firstOptionalCombat))
                {
                    branchAttachmentCount++;
                }
            }
            if (branchAttachmentCount != 1)
            {
                throw new ArgumentException(
                    "Optional combat rooms must form one branch from the boss path.");
            }
        }

        private int GetRoomIndex(DungeonRoomNodeId roomId)
        {
            if (!roomId.IsValid)
            {
                throw new ArgumentException("Dungeon room node ID must be valid.", nameof(roomId));
            }

            int index = roomId.Value - 1;
            if (index < 0 || index >= _rooms.Length)
            {
                throw new KeyNotFoundException($"Dungeon room {roomId} does not exist.");
            }
            return index;
        }

        private static void AssignUnique(
            ref DungeonRoomNodeId target,
            DungeonRoomNodeId value,
            RoomType roomType)
        {
            if (target.IsValid)
            {
                throw new ArgumentException($"Dungeon graph has multiple {roomType} rooms.");
            }
            target = value;
        }

        private static void RequireAssigned(DungeonRoomNodeId roomId, RoomType roomType)
        {
            if (!roomId.IsValid)
            {
                throw new ArgumentException($"Dungeon graph requires one {roomType} room.");
            }
        }

        private static long CreateEndpointKey(
            DungeonRoomNodeId first,
            DungeonRoomNodeId second)
        {
            int lower = first.Value < second.Value ? first.Value : second.Value;
            int upper = first.Value < second.Value ? second.Value : first.Value;
            return ((long)lower << 32) | (uint)upper;
        }

        private static void ValidateExitDirection(RoomExitDirection direction)
        {
            switch (direction)
            {
                case RoomExitDirection.North:
                case RoomExitDirection.East:
                case RoomExitDirection.South:
                case RoomExitDirection.West:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unknown room exit direction.");
            }
        }
    }
}
