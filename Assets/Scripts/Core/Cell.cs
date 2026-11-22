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

    /// <summary>Which monster a monster cell is. Only the picture depends on it; the rules use Magic, Power and BuffAmount.</summary>
    public enum MonsterSpecies
    {
        None,
        Slime,
        Beast,
        Wisp,
        Brute,
    }

    /// <summary>
    /// Whether the floor under a cell is there (2026-11-22, idea 34). Only the mechanism: which boss or monster cracks
    /// which cells is not decided yet. See Grid.MarkCrack / Grid.AdvanceFloor and BoardShape.
    /// </summary>
    public enum CellFloor
    {
        /// <summary>A normal cell.</summary>
        Solid,
        /// <summary>Marked to break: still a normal cell for every rule, it breaks after the next placement.</summary>
        Cracked,
        /// <summary>Not part of the board: never there (BoardShape) or broken. Nothing is drawn and no block may cover it.</summary>
        Hole,
    }

    public class Cell
    {
        public CellType Type;
        // Solid, cracked (breaks after the next placement) or a hole. A hole also counts as Used, so every rule that
        // skips spent cells (odds, buffs, contamination, the round end) skips it without a check of its own.
        public CellFloor Floor;
        public bool IsHole => Floor == CellFloor.Hole;
        // Monster cells only: which monster it is (for its picture).
        public MonsterSpecies Species;
        // Pick-One rule: a block was placed over this cell. A covered cell never fires and (with BlockCovered) takes no block again.
        public bool Covered;
        // Every cell locks once activated (a Danger void only when it breaks).
        public bool Used;
        // Monster cells only: their attack. Normal void cells share Grid.VoidPower instead.
        public int Power;
        // Monster cells only: true = a magic monster (beaten by the player's Magic), false = physical (beaten by Attack).
        public bool Magic;
        // Monster cells that buff a neighbour (the Wisp): how much attack the buffed monster gains while this cell
        // stands (not fired, not covered), and which cell it is (-1 = none). See Grid.EffectivePower.
        public int BuffAmount;
        public int BuffX = -1;
        public int BuffY = -1;
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
