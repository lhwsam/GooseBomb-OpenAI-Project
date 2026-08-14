using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class CombatRoomDefinitionTests
    {
        private static readonly GridPosition PlayerSpawn = new GridPosition(0, 0);
        private static readonly GridPosition ChaserSpawn = new GridPosition(3, -1);

        [Test]
        public void Definition_StoresValidatedAuthoringIntent()
        {
            CombatRoomDefinition room = CreateRoom();

            Assert.That(room.Id, Is.EqualTo(new RoomDefinitionId("test-combat-loop")));
            Assert.That(room.RoomType, Is.EqualTo(RoomType.Combat));
            Assert.That(room.Width, Is.EqualTo(11));
            Assert.That(room.Depth, Is.EqualTo(9));
            Assert.That(room.PlayerSpawn, Is.EqualTo(PlayerSpawn));
            Assert.That(room.ChaserSpawn, Is.EqualTo(ChaserSpawn));
            Assert.That(room.SafePlayerCells, Has.Count.EqualTo(3));
            Assert.That(room.RetreatAnchors, Has.Count.EqualTo(2));
            Assert.That(room.LureLoop, Has.Count.EqualTo(8));
            Assert.That(room.Exits, Has.Count.EqualTo(2));
        }

        [Test]
        public void DefinitionId_UsesOrdinalValueEquality()
        {
            var first = new RoomDefinitionId("prototype-combat-loop");
            var same = new RoomDefinitionId("prototype-combat-loop");
            var differentCase = new RoomDefinitionId("Prototype-Combat-Loop");

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(differentCase));
            Assert.That(first.ToString(), Is.EqualTo("prototype-combat-loop"));
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(-1)]
        public void Definition_RejectsNonPositiveOrEvenWidth(int width)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRoom(width: width));
        }

        [Test]
        public void Definition_RejectsOutOfBoundsAndDuplicateWalls()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateRoom(walls: new[] { new GridPosition(6, 0) }));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(walls: new[]
                {
                    new GridPosition(1, 1),
                    new GridPosition(1, 1),
                }));
        }

        [Test]
        public void Definition_StoresDestructibleWallsAsInitiallyBlockedCells()
        {
            var destructible = new GridPosition(0, -2);

            CombatRoomDefinition room = CreateRoom(
                destructibleWalls: new[] { destructible });

            Assert.That(room.DestructibleWalls, Is.EqualTo(new[] { destructible }));
            Assert.That(room.IsBlocked(destructible), Is.True);
            Assert.That(room.IsDestructibleWall(destructible), Is.True);
            Assert.That(room.IsIndestructibleWall(destructible), Is.False);
        }

        [Test]
        public void Definition_RejectsDuplicateOrOverlappingDestructibleWalls()
        {
            var wall = new GridPosition(1, 1);

            Assert.Throws<ArgumentException>(() =>
                CreateRoom(destructibleWalls: new[] { wall, wall }));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(
                    walls: new[] { wall },
                    destructibleWalls: new[] { wall }));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(destructibleWalls: new[] { PlayerSpawn }));
        }

        [Test]
        public void Definition_RejectsDestructibleWallThatDisconnectsInitialPlayableArea()
        {
            var separatingWall = new GridPosition[9];
            for (int index = 0; index < separatingWall.Length; index++)
            {
                separatingWall[index] = new GridPosition(1, index - 4);
            }

            Assert.Throws<ArgumentException>(() =>
                CreateRoom(destructibleWalls: separatingWall));
        }

        [Test]
        public void Definition_StoresOptionalChargerSpawnAsTraversableEnemyCell()
        {
            var chargerSpawn = new GridPosition(-3, 2);

            CombatRoomDefinition room = CreateRoom(chargerSpawn: chargerSpawn);

            Assert.That(room.ChargerSpawn, Is.EqualTo(chargerSpawn));
            Assert.That(room.IsBlocked(chargerSpawn), Is.False);
        }

        [Test]
        public void Definition_RejectsChargerSpawnOverlapsAndImmediatePlayerContact()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(chargerSpawn: PlayerSpawn));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(chargerSpawn: new GridPosition(0, 1)));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(chargerSpawn: ChaserSpawn));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(
                    walls: new[] { new GridPosition(-3, 2) },
                    chargerSpawn: new GridPosition(-3, 2)));
        }

        [Test]
        public void Definition_StoresOptionalArmoredSpawnAsTraversableEnemyCell()
        {
            var armoredSpawn = new GridPosition(-2, 2);

            CombatRoomDefinition room = CreateRoom(armoredSpawn: armoredSpawn);

            Assert.That(room.ArmoredSpawn, Is.EqualTo(armoredSpawn));
            Assert.That(room.IsBlocked(armoredSpawn), Is.False);
        }

        [Test]
        public void Definition_RejectsArmoredSpawnOverlapsContactAndSafeCells()
        {
            var chargerSpawn = new GridPosition(-3, 2);

            Assert.Throws<ArgumentException>(() =>
                CreateRoom(armoredSpawn: PlayerSpawn));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(armoredSpawn: new GridPosition(1, 0)));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(armoredSpawn: ChaserSpawn));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(chargerSpawn: chargerSpawn, armoredSpawn: chargerSpawn));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(
                    walls: new[] { new GridPosition(-2, 2) },
                    armoredSpawn: new GridPosition(-2, 2)));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(
                    safeCells: new[] { PlayerSpawn, new GridPosition(-2, 2) },
                    armoredSpawn: new GridPosition(-2, 2)));
        }

        [Test]
        public void Definition_RejectsSpawnOverlappingWallOrImmediateContact()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(walls: new[] { PlayerSpawn }));
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(chaserSpawn: new GridPosition(1, 0)));
        }

        [Test]
        public void Definition_RejectsSafeCellsWithoutPlayerSpawn()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(safeCells: new[] { new GridPosition(-1, 0) }));
        }

        [Test]
        public void Definition_RejectsExitThatDoesNotMatchBoundary()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(exits: new[]
                {
                    new RoomExit(new GridPosition(0, 3), RoomExitDirection.North),
                    new RoomExit(new GridPosition(0, -4), RoomExitDirection.South),
                }));
        }

        [Test]
        public void Definition_RejectsDuplicateExitDirectionEvenAtDifferentCells()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoom(exits: new[]
                {
                    new RoomExit(new GridPosition(0, 4), RoomExitDirection.North),
                    new RoomExit(new GridPosition(1, 4), RoomExitDirection.North),
                }));
        }

        [Test]
        public void Definition_RejectsDisconnectedPlayableArea()
        {
            var separatingWall = new GridPosition[9];
            for (int index = 0; index < separatingWall.Length; index++)
            {
                separatingWall[index] = new GridPosition(1, index - 4);
            }

            Assert.Throws<ArgumentException>(() => CreateRoom(walls: separatingWall));
        }

        [Test]
        public void Definition_RejectsSpawnWithOnlyOneRetreatDirection()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                CreateRoom(
                    walls: new[]
                    {
                        new GridPosition(0, 1),
                        new GridPosition(0, -1),
                        new GridPosition(-1, 0),
                    },
                    safeCells: new[] { PlayerSpawn }));

            Assert.That(exception.Message, Does.Contain("two distinct first steps"));
        }

        [Test]
        public void Definition_RejectsOpenOrDiscontinuousLureLoop()
        {
            GridPosition[] invalidLoop = CreateLureLoop();
            invalidLoop[invalidLoop.Length - 1] = new GridPosition(3, -1);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                CreateRoom(lureLoop: invalidLoop));

            Assert.That(exception.Message, Does.Contain("closed cardinal path"));
        }

        [Test]
        public void PrototypeCrossLayout_PreservesTwoRoutesAndClosedCentralLoop()
        {
            var room = new CombatRoomDefinition(
                new RoomDefinitionId("prototype-combat-loop"),
                RoomType.Combat,
                11,
                9,
                PlayerSpawn,
                new GridPosition(1, -1),
                new[]
                {
                    new GridPosition(-2, 0),
                    new GridPosition(2, 0),
                    new GridPosition(0, 2),
                    new GridPosition(0, -2),
                },
                new[]
                {
                    PlayerSpawn,
                    new GridPosition(0, 1),
                    new GridPosition(-1, 0),
                },
                new[]
                {
                    new GridPosition(-3, 1),
                    new GridPosition(3, 1),
                },
                new[]
                {
                    new GridPosition(-1, -1),
                    new GridPosition(-1, 0),
                    new GridPosition(-1, 1),
                    new GridPosition(0, 1),
                    new GridPosition(1, 1),
                    new GridPosition(1, 0),
                    new GridPosition(1, -1),
                    new GridPosition(0, -1),
                },
                CreateExits());

            Assert.That(room.IsBlocked(new GridPosition(2, 0)), Is.True);
            Assert.That(room.IsBlocked(PlayerSpawn), Is.False);
            Assert.That(room.LureLoop, Has.Count.EqualTo(8));
        }

        private static CombatRoomDefinition CreateRoom(
            int width = 11,
            GridPosition? chaserSpawn = null,
            GridPosition[] walls = null,
            GridPosition[] destructibleWalls = null,
            GridPosition? chargerSpawn = null,
            GridPosition? armoredSpawn = null,
            GridPosition[] safeCells = null,
            GridPosition[] lureLoop = null,
            RoomExit[] exits = null)
        {
            return new CombatRoomDefinition(
                new RoomDefinitionId("test-combat-loop"),
                RoomType.Combat,
                width,
                9,
                PlayerSpawn,
                chaserSpawn ?? ChaserSpawn,
                walls ?? Array.Empty<GridPosition>(),
                safeCells ?? new[]
                {
                    PlayerSpawn,
                    new GridPosition(0, 1),
                    new GridPosition(-1, 0),
                },
                new[]
                {
                    new GridPosition(-3, 1),
                    new GridPosition(3, 1),
                },
                lureLoop ?? CreateLureLoop(),
                exits ?? CreateExits(),
                destructibleWalls ?? Array.Empty<GridPosition>(),
                chargerSpawn,
                armoredSpawn);
        }

        private static GridPosition[] CreateLureLoop()
        {
            return new[]
            {
                new GridPosition(2, -2),
                new GridPosition(2, -1),
                new GridPosition(2, 0),
                new GridPosition(3, 0),
                new GridPosition(4, 0),
                new GridPosition(4, -1),
                new GridPosition(4, -2),
                new GridPosition(3, -2),
            };
        }

        private static RoomExit[] CreateExits()
        {
            return new[]
            {
                new RoomExit(new GridPosition(0, 4), RoomExitDirection.North),
                new RoomExit(new GridPosition(0, -4), RoomExitDirection.South),
            };
        }
    }
}
