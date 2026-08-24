using System;

namespace BombSwap.Core
{
    [Flags]
    public enum GridOccupancy
    {
        None = 0,
        Actor = 1 << 0,
        Bomb = 1 << 1,
        Interactable = 1 << 2
    }
}
