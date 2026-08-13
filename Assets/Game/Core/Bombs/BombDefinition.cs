using System;

namespace BombSwap.Core
{
    public sealed class BombDefinition
    {
        public BombDefinition(
            BombDefinitionId id,
            BombExplosionShape explosionShape,
            TimeSpan fuseDuration,
            int range)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Bomb definition ID must be valid.", nameof(id));
            }

            if (explosionShape != BombExplosionShape.Cross)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(explosionShape),
                    explosionShape,
                    "Unsupported bomb explosion shape.");
            }

            if (fuseDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fuseDuration),
                    fuseDuration,
                    "Bomb fuse duration must be greater than zero.");
            }

            if (range < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(range),
                    range,
                    "Bomb range cannot be negative.");
            }

            Id = id;
            ExplosionShape = explosionShape;
            FuseDuration = fuseDuration;
            Range = range;
        }

        public BombDefinitionId Id { get; }

        public BombExplosionShape ExplosionShape { get; }

        public TimeSpan FuseDuration { get; }

        public int Range { get; }
    }
}
