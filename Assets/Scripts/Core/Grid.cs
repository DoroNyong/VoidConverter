using System;
using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>Pure rules/data for the board. No UnityEngine dependency.</summary>
    public class Grid
    {
        public int Width { get; }
        public int Height { get; }

        readonly Cell[,] cells;

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new Cell[width, height];
        }

        public Cell this[int x, int y] => cells[x, y];

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void Generate(GridSettings settings, Random rng)
        {
            float total = settings.MonsterWeight + settings.RewardWeight + settings.VoidWeight;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float roll = (float)rng.NextDouble() * total;
                    CellType type;
                    if (roll < settings.MonsterWeight) type = CellType.Monster;
                    else if (roll < settings.MonsterWeight + settings.RewardWeight) type = CellType.Reward;
                    else type = CellType.Void;

                    cells[x, y] = new Cell
                    {
                        Type = type,
                        Used = false,
                        Power = type == CellType.Monster
                            ? rng.Next(settings.MonsterPowerMin, settings.MonsterPowerMax + 1)
                            : 0,
                    };
                }
            }
        }

        /// <summary>
        /// A block fits when every cell is inside the grid and not already activated.
        /// Void cells can be covered again and again.
        /// </summary>
        public bool CanPlace(Int2[] blockCells, int originX, int originY)
        {
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) return false;
                var cell = cells[x, y];
                if (cell.Used && cell.Type != CellType.Void) return false;
            }
            return true;
        }

        /// <summary>Total power of the monster cells (not yet activated) that the block would cover.</summary>
        public int MonsterPowerUnder(Int2[] blockCells, int originX, int originY)
        {
            int total = 0;
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                var cell = cells[x, y];
                if (cell.Type == CellType.Monster && !cell.Used) total += cell.Power;
            }
            return total;
        }

        /// <summary>True if the shape fits somewhere in at least one of its four rotations.</summary>
        public bool CanPlaceAnywhere(BlockShape shape)
        {
            for (int r = 0; r < 4; r++)
            {
                var rotated = shape.Rotated(r);
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (CanPlace(rotated, x, y)) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Activates every covered cell and reports what was activated. Effects (combat, corruption, rewards)
        /// are applied by later systems from this result. Returns null if the block does not fit.
        /// </summary>
        public PlacementResult Place(Int2[] blockCells, int originX, int originY)
        {
            if (!CanPlace(blockCells, originX, originY)) return null;

            var result = new PlacementResult();
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                var cell = cells[x, y];
                if (cell.Type == CellType.Void) cell.ActivationCount++;
                else cell.Used = true;
                result.Activated.Add(new ActivatedCell { X = x, Y = y, Type = cell.Type, Power = cell.Power });
            }
            return result;
        }
    }

    public struct GridSettings
    {
        public float MonsterWeight;
        public float RewardWeight;
        public float VoidWeight;
        public int MonsterPowerMin;
        public int MonsterPowerMax;
    }

    public struct ActivatedCell
    {
        public int X;
        public int Y;
        public CellType Type;
        public int Power;
    }

    public class PlacementResult
    {
        public List<ActivatedCell> Activated { get; } = new List<ActivatedCell>();
    }
}
