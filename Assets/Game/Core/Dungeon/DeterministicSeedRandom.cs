using System;

namespace BombSwap.Core
{
    internal sealed class DeterministicSeedRandom
    {
        private uint _state;

        public DeterministicSeedRandom(int seed)
        {
            uint mixed = unchecked((uint)seed);
            mixed ^= mixed >> 16;
            mixed = unchecked(mixed * 0x7feb352du);
            mixed ^= mixed >> 15;
            mixed = unchecked(mixed * 0x846ca68bu);
            mixed ^= mixed >> 16;
            _state = mixed;
        }

        public int Next(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumExclusive),
                    maximumExclusive,
                    "Maximum must be greater than minimum.");
            }

            uint range = unchecked((uint)(maximumExclusive - minimumInclusive));
            uint sample;
            ulong product;
            uint low;
            sample = NextUInt32();
            product = (ulong)sample * range;
            low = unchecked((uint)product);
            if (low < range)
            {
                uint threshold = unchecked(0u - range) % range;
                while (low < threshold)
                {
                    sample = NextUInt32();
                    product = (ulong)sample * range;
                    low = unchecked((uint)product);
                }
            }

            return minimumInclusive + unchecked((int)(product >> 32));
        }

        private uint NextUInt32()
        {
            _state = unchecked((_state * 1664525u) + 1013904223u);
            return _state;
        }
    }
}
