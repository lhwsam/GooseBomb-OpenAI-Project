namespace BombSwap.Core
{
    public readonly struct GridCellState
    {
        internal GridCellState(GridTerrain terrain, GridOccupancy occupancy)
        {
            Terrain = terrain;
            Occupancy = occupancy;
        }

        public GridTerrain Terrain { get; }

        public GridOccupancy Occupancy { get; }

        public bool IsWalkableTerrain => Terrain == GridTerrain.Floor;

        public bool HasActor => (Occupancy & GridOccupancy.Actor) != 0;

        public bool HasBomb => (Occupancy & GridOccupancy.Bomb) != 0;
    }
}
