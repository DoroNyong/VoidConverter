namespace VoidConverter.Core
{
    /// <summary>What a void cell does. Cells of other types always have Normal.</summary>
    public enum VoidKind
    {
        /// <summary>Rolls monster or reward (the original void).</summary>
        Normal,
        /// <summary>While it stands, monsters in the 8 cells around it are stronger.</summary>
        Buff,
        /// <summary>Every turn it contaminates one of the 8 cells around it.</summary>
        Spread,
        /// <summary>Has HP. Each block placed on it hits it for the player's attack roll; it pays a reward when it breaks.</summary>
        Danger,
    }

    public class Cell
    {
        public CellType Type;
        // Every cell locks once activated (a Danger void only when it breaks).
        public bool Used;
        // Monster cells only: their attack. Normal void cells share Grid.VoidPower instead.
        public int Power;
        // Void cells only.
        public VoidKind Kind;
        // Danger voids only: hit points left.
        public int Hp;
        // Set by a Spread void. Placing a block on a contaminated cell raises corruption (the cell otherwise acts as before).
        public bool Contaminated;
        // How many times a Danger void has been hit this round.
        public int ActivationCount;
    }
}
