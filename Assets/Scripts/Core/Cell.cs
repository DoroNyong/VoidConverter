namespace VoidConverter.Core
{
    public class Cell
    {
        public CellType Type;
        public bool Used;
        // Only meaningful for Monster cells.
        public int Power;
    }
}
