namespace VoidConverter.Game
{
    /// <summary>What happened when a block was placed (Pick-One rule): the cell that fired and how it paid out.</summary>
    public enum FeedbackKind
    {
        // A monster fight the player won (Amount = gold).
        Win,
        // A monster fight the player lost (Amount = HP lost).
        Lose,
        // A trap fired (Amount = HP lost).
        Trap,
        // A gold cell (Amount = gold).
        Gold,
        // A heal cell (Amount = HP healed, 0 when already at full HP).
        Heal,
    }

    /// <summary>Raised by GameController.Fired once per block placement, for sound and visual effects only.</summary>
    public struct FeedbackEvent
    {
        public FeedbackKind Kind;
        // Grid cell that fired.
        public int X;
        public int Y;
        public int Amount;
        // For a monster: true when it was a magic monster.
        public bool Magic;
        // For a monster: its power (a stronger one gets a bigger effect).
        public int Power;
    }
}
