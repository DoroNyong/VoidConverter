using System;

namespace VoidConverter.Core
{
    /// <summary>
    /// Which cells of a width x height board exist (2026-11-22, idea 34). Missing cells are holes from the start:
    /// nothing is generated or drawn there and no block may cover them. Only the mechanism: which stage uses which
    /// shape is not decided yet. A null shape in GridSettings means the full rectangle.
    /// </summary>
    public sealed class BoardShape
    {
        public int Width { get; }
        public int Height { get; }
        /// <summary>How many cells exist.</summary>
        public int Count { get; }

        readonly bool[,] cells;

        BoardShape(bool[,] cells)
        {
            this.cells = cells;
            Width = cells.GetLength(0);
            Height = cells.GetLength(1);
            int count = 0;
            foreach (bool c in cells) if (c) count++;
            Count = count;
        }

        /// <summary>True if the cell exists (false outside the board too).</summary>
        public bool Has(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && cells[x, y];

        /// <summary>Every cell of a width x height board.</summary>
        public static BoardShape Full(int width, int height)
        {
            var c = new bool[width, height];
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) c[x, y] = true;
            return new BoardShape(c);
        }

        /// <summary>
        /// A shape from text rows, one string per y (rows[0] is y = 0) and one character per x: '.' or ' ' is a hole,
        /// anything else a cell. Rows may differ in length (missing characters are holes); the widest row sets the width.
        /// Example (a 5x5 board without its corners): ".###.", "#####", "#####", "#####", ".###.".
        /// </summary>
        public static BoardShape FromRows(params string[] rows)
        {
            if (rows == null || rows.Length == 0) throw new ArgumentException("A board shape needs at least one row.");
            int width = 0;
            foreach (var r in rows) width = Math.Max(width, r?.Length ?? 0);
            var c = new bool[width, rows.Length];
            for (int y = 0; y < rows.Length; y++)
            {
                var row = rows[y] ?? "";
                for (int x = 0; x < row.Length; x++) c[x, y] = row[x] != '.' && row[x] != ' ';
            }
            return new BoardShape(c);
        }

        /// <summary>A shape from a mask (copied), [x, y] = true where a cell exists.</summary>
        public static BoardShape FromMask(bool[,] mask) => new BoardShape((bool[,])mask.Clone());
    }

    /// <summary>
    /// Something that cracks floor cells: a boss pattern or a monster's ability (2026-11-22, idea 34). None exists yet;
    /// which ones there are and what they mark is decided when monsters and maps are added.
    /// Grid.AdvanceFloor asks every gimmick once after each placement, right after the cells cracked earlier broke, so a
    /// cell marked here (Grid.MarkCrack) stays for one placement and breaks after the next one.
    /// </summary>
    public interface IFloorGimmick
    {
        /// <summary>Marks the cells to break after the next placement (Grid.MarkCrack). May mark none.</summary>
        void MarkCracks(Grid grid, Random rng);
    }
}
