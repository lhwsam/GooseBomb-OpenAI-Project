using System;
using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    public static class CardinalInputInterpreter
    {
        public const float DefaultActuationThreshold = 0.5f;

        public static CardinalDirection Resolve(
            Vector2 value,
            CardinalDirection previousDirection,
            float actuationThreshold = DefaultActuationThreshold)
        {
            if (!IsDefinedDirection(previousDirection))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(previousDirection),
                    previousDirection,
                    "Previous direction is not defined.");
            }

            if (float.IsNaN(actuationThreshold) ||
                float.IsInfinity(actuationThreshold) ||
                actuationThreshold <= 0f ||
                actuationThreshold > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actuationThreshold),
                    actuationThreshold,
                    "Actuation threshold must be finite and in the range (0, 1].");
            }

            if (float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Input value must be finite.");
            }

            float absoluteX = Mathf.Abs(value.x);
            float absoluteY = Mathf.Abs(value.y);
            bool horizontalActuated = absoluteX >= actuationThreshold;
            bool verticalActuated = absoluteY >= actuationThreshold;

            if (!horizontalActuated && !verticalActuated)
            {
                return CardinalDirection.None;
            }

            if (horizontalActuated && !verticalActuated)
            {
                return value.x > 0f ? CardinalDirection.East : CardinalDirection.West;
            }

            if (verticalActuated && !horizontalActuated)
            {
                return value.y > 0f ? CardinalDirection.North : CardinalDirection.South;
            }

            if (absoluteX > absoluteY)
            {
                return value.x > 0f ? CardinalDirection.East : CardinalDirection.West;
            }

            if (absoluteY > absoluteX)
            {
                return value.y > 0f ? CardinalDirection.North : CardinalDirection.South;
            }

            if (Matches(previousDirection, value))
            {
                return ResolvePerpendicularTurn(previousDirection, value);
            }

            return value.y > 0f ? CardinalDirection.North : CardinalDirection.South;
        }

        private static CardinalDirection ResolvePerpendicularTurn(
            CardinalDirection previousDirection,
            Vector2 value)
        {
            if (previousDirection == CardinalDirection.North ||
                previousDirection == CardinalDirection.South)
            {
                return value.x > 0f ? CardinalDirection.East : CardinalDirection.West;
            }

            return value.y > 0f ? CardinalDirection.North : CardinalDirection.South;
        }

        private static bool Matches(CardinalDirection direction, Vector2 value)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return value.y > 0f;
                case CardinalDirection.East:
                    return value.x > 0f;
                case CardinalDirection.South:
                    return value.y < 0f;
                case CardinalDirection.West:
                    return value.x < 0f;
                default:
                    return false;
            }
        }

        private static bool IsDefinedDirection(CardinalDirection direction)
        {
            return direction >= CardinalDirection.None && direction <= CardinalDirection.West;
        }
    }
}
