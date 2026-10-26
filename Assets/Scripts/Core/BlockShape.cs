using System;

namespace VoidConverter.Core
{
    /// <summary>A connected group of cells. Rotation is allowed, flipping is not.</summary>
    public class BlockShape
    {
        public string Name { get; }
        public Int2[] Cells { get; }
        public int Size => Cells.Length;

        /// <param name="rows">Rows from top to bottom; 'X' marks a filled cell.</param>
        public BlockShape(string name, params string[] rows)
        {
            Name = name;
            int height = rows.Length;
            var list = new System.Collections.Generic.List<Int2>();
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (rows[r][c] == 'X') list.Add(new Int2(c, height - 1 - r));
                }
            }
            Cells = list.ToArray();
        }

        /// <summary>Cells after rotating clockwise by 90 degrees <paramref name="quarterTurns"/> times, shifted so the minimum is (0,0).</summary>
        public Int2[] Rotated(int quarterTurns)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            var result = (Int2[])Cells.Clone();
            for (int t = 0; t < turns; t++)
            {
                for (int i = 0; i < result.Length; i++) result[i] = new Int2(result[i].Y, -result[i].X);
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in result)
            {
                minX = Math.Min(minX, c.X);
                minY = Math.Min(minY, c.Y);
            }
            for (int i = 0; i < result.Length; i++) result[i] = new Int2(result[i].X - minX, result[i].Y - minY);
            return result;
        }
    }
}
