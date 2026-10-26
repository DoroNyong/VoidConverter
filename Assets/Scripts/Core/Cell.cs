namespace VoidConverter.Core
{
    public class Cell
    {
        public CellType Type;
        // Monster and Reward cells lock once activated. Void cells never lock.
        public bool Used;
        // Monster cells only: their attack. Void cells share Grid.VoidPower instead.
        public int Power;
        // How many times a Void cell has been activated this round.
        public int ActivationCount;
    }
}
