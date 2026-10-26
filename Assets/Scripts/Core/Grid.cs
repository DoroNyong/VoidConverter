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

        /// <summary>
        /// How many spacing rules had to be dropped to lay out the last generated board (see GridLayout.Layout.Relaxation).
        /// 0 is the normal case.
        /// </summary>
        public int LastRelaxation { get; private set; }

        public void Generate(GridSettings settings, Random rng)
        {
            var layout = GridLayout.Build(Width, Height, settings, rng);
            LastRelaxation = layout.Relaxation;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var type = layout.Types[x, y];
                    int power = type == CellType.Monster ? rng.Next(settings.MonsterPowerMin, settings.MonsterPowerMax + 1) : 0;
                    cells[x, y] = new Cell { Type = type, Used = false, Power = power };
                }
            }

            VoidPower = rng.Next(settings.VoidPowerMin, settings.VoidPowerMax + 1);
        }

        /// <summary>
        /// The power of the monster any void cell would spawn right now. It is one shared value, shown faintly on
        /// every void cell, and re-rolled after every placement.
        /// </summary>
        public int VoidPower { get; private set; }

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

        /// <summary>How many not-yet-activated cells of the given type the block would cover.</summary>
        public int CountUnder(Int2[] blockCells, int originX, int originY, CellType type)
        {
            int count = 0;
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                var cell = cells[x, y];
                if (cell.Type == type && !cell.Used) count++;
            }
            return count;
        }

        /// <summary>The monster power of each void cell the block would cover (all equal to VoidPower), one entry per cell.</summary>
        public List<int> VoidPowersUnder(Int2[] blockCells, int originX, int originY)
        {
            var powers = new List<int>();
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                if (cells[x, y].Type == CellType.Void) powers.Add(VoidPower);
            }
            return powers;
        }

        /// <summary>Rolls a new shared void power from the player's current corruption.</summary>
        public void RerollVoidPower(PlayerStats player, VoidSettings settings, Random rng)
        {
            VoidPower = VoidRules.RollPower(player, settings, rng);
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
                int power = cell.Type == CellType.Void ? VoidPower : cell.Power;
                result.Activated.Add(new ActivatedCell { X = x, Y = y, Type = cell.Type, Power = power });
            }
            return result;
        }
    }

    public struct GridSettings
    {
        // Cell counts: an exact number of void cells, a minimum for each other type, and the leftover cells
        // split among monster/gold/heal by these weights.
        public int VoidCount;
        public int MonsterMin;
        public int GoldMin;
        public int HealMin;
        public float MonsterWeight;
        public float GoldWeight;
        public float HealWeight;
        // Spacing. VoidMinSpacing is a Chebyshev distance (2 = void cells may not even touch diagonally).
        // RewardMinSpacing is a Manhattan distance between any two reward cells (2 = never edge to edge).
        // MonsterMaxCluster caps the largest group of edge-connected monsters (0 = no cap). 1 disables a spacing rule.
        public int VoidMinSpacing;
        public int RewardMinSpacing;
        public int MonsterMaxCluster;
        public int MonsterPowerMin;
        public int MonsterPowerMax;
        public int VoidPowerMin;
        public int VoidPowerMax;
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
