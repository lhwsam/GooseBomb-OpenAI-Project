using System;
using BombSwap.Core;
using NUnit.Framework;

namespace BombSwap.Tests.EditMode
{
    public sealed class PlayerCommandTests
    {
        [TestCase(CardinalDirection.None)]
        [TestCase(CardinalDirection.North)]
        [TestCase(CardinalDirection.East)]
        [TestCase(CardinalDirection.South)]
        [TestCase(CardinalDirection.West)]
        public void Move_PreservesDefinedDirection(CardinalDirection direction)
        {
            PlayerCommand command = PlayerCommand.Move(direction);

            Assert.That(command.Kind, Is.EqualTo(PlayerCommandKind.Move));
            Assert.That(command.MoveDirection, Is.EqualTo(direction));
            Assert.That(command.IsValid, Is.True);
        }

        [Test]
        public void Move_RejectsUndefinedDirection()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PlayerCommand.Move((CardinalDirection)99));
        }

        [Test]
        public void ButtonFactories_CreateSemanticCommandsWithoutMoveDirection()
        {
            PlayerCommand[] commands =
            {
                PlayerCommand.PlaceBomb(),
                PlayerCommand.SwapBomb(),
                PlayerCommand.Pause(),
            };

            Assert.That(commands[0].Kind, Is.EqualTo(PlayerCommandKind.PlaceBomb));
            Assert.That(commands[1].Kind, Is.EqualTo(PlayerCommandKind.SwapBomb));
            Assert.That(commands[2].Kind, Is.EqualTo(PlayerCommandKind.Pause));
            foreach (PlayerCommand command in commands)
            {
                Assert.That(command.MoveDirection, Is.EqualTo(CardinalDirection.None));
                Assert.That(command.IsValid, Is.True);
            }
        }

        [Test]
        public void DefaultCommand_IsNotValid()
        {
            Assert.That(default(PlayerCommand).Kind, Is.EqualTo(PlayerCommandKind.Unknown));
            Assert.That(default(PlayerCommand).IsValid, Is.False);
        }

        [Test]
        public void SameSemanticCommand_UsesValueEqualityAndSameHashCode()
        {
            PlayerCommand first = PlayerCommand.Move(CardinalDirection.West);
            PlayerCommand second = PlayerCommand.Move(CardinalDirection.West);

            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
    }
}
