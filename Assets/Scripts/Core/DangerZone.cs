using System;

namespace VoidConverter.Core
{
    /// <summary>
    /// The boss stage's danger zone: a 3x3 block of cells. If the cell that fires is inside it the player takes a big
    /// hit, and if it is the middle cell the player is defeated. After every block the zone takes one step toward the
    /// cell that fired, so the safe cells run out over the turns.
    /// </summary>
    public class DangerZone
    {
        public int CenterX { get; private set; }
        public int CenterY { get; private set; }

        public DangerZone(int centerX, int centerY)
        {
            CenterX = centerX;
            CenterY = centerY;
        }

        /// <summary>The cell is one of the nine cells of the zone (the middle cell included).</summary>
        public bool Contains(int x, int y) => Math.Abs(x - CenterX) <= 1 && Math.Abs(y - CenterY) <= 1;

        /// <summary>The cell is the zone's middle cell.</summary>
        public bool IsCenter(int x, int y) => x == CenterX && y == CenterY;

        /// <summary>One step (also diagonal) toward the cell. The zone stays where it is if it is already there.</summary>
        public void MoveToward(int x, int y)
        {
            CenterX += Math.Sign(x - CenterX);
            CenterY += Math.Sign(y - CenterY);
        }
    }
}
