using System;

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
    }

    public struct GridSettings
    {
        public float MonsterWeight;
        public float RewardWeight;
        public float VoidWeight;
        public int MonsterPowerMin;
        public int MonsterPowerMax;
    }
}
