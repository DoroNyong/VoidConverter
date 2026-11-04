namespace VoidConverter.Core
{
    public enum CellType
    {
        Monster,
        // Gives gold when activated.
        Gold,
        // Restores HP when activated.
        Heal,
        Void,
        // Pick-One rule: only hurts. When it fires the player loses HP (GameConfig.trapDamage).
        Trap,
        // Does nothing. Blocks may overlap it once the board allows overlapping (see Grid.AllowOverlap).
        Empty,
    }
}
