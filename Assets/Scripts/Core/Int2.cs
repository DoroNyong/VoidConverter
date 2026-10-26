namespace VoidConverter.Core
{
    /// <summary>Integer cell offset. Kept separate from UnityEngine so Core stays engine-free.</summary>
    public readonly struct Int2
    {
        public readonly int X;
        public readonly int Y;

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
