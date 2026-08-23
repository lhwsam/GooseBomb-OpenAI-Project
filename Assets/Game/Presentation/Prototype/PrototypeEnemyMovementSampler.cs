using BombSwap.Core;
using UnityEngine;

namespace BombSwap
{
    internal static class PrototypeEnemyMovementSampler
    {
        public static bool IsActive(
            EnemyMovementTransition transition,
            System.TimeSpan gameTime)
        {
            return transition.IsValid && transition.GetProgress(gameTime) < 1d;
        }

        public static Vector3 Sample(
            EnemyMovementTransition transition,
            System.TimeSpan gameTime,
            GridSpace gridSpace,
            float visualHeight,
            GridPosition fallbackPosition)
        {
            if (!transition.IsValid)
            {
                return ToPresentationPosition(gridSpace, visualHeight, fallbackPosition);
            }

            Vector3 from = ToPresentationPosition(
                gridSpace,
                visualHeight,
                transition.Movement.From);
            Vector3 to = ToPresentationPosition(
                gridSpace,
                visualHeight,
                transition.Movement.To);
            return Vector3.LerpUnclamped(
                from,
                to,
                (float)transition.GetProgress(gameTime));
        }

        private static Vector3 ToPresentationPosition(
            GridSpace gridSpace,
            float visualHeight,
            GridPosition position)
        {
            return gridSpace.GridToWorld(position) + (Vector3.up * visualHeight);
        }
    }
}
