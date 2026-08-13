using System;

namespace BombSwap.Core
{
    public interface IGameClock
    {
        TimeSpan Now { get; }
    }
}
