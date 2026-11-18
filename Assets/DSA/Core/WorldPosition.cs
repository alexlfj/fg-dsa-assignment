namespace FrontierDispatch.Core
{
    public readonly struct WorldPosition
    {
        public WorldPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}
