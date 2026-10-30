using System;
using System.Collections.Generic;

namespace VoidConverter.Core
{
    public struct BlockSettings
    {
        // Probability weight for each block size. Each size's weight is split evenly among its shapes.
        public float Size2Weight;
        public float Size3Weight;
        public float Size4Weight;

        public float WeightOfSize(int size)
        {
            switch (size)
            {
                case 2: return Size2Weight;
                case 3: return Size3Weight;
                case 4: return Size4Weight;
                default: return 0f;
            }
        }
    }

    /// <summary>All block shapes in the game: 1 domino, 2 trominoes, 7 tetrominoes.</summary>
    public static class BlockLibrary
    {
        public static readonly BlockShape[] All =
        {
            new BlockShape("Domino", "XX"),
            new BlockShape("I3", "XXX"),
            new BlockShape("L3", "X.", "XX"),
            new BlockShape("I4", "XXXX"),
            new BlockShape("O4", "XX", "XX"),
            new BlockShape("T4", ".X.", "XXX"),
            new BlockShape("S4", ".XX", "XX."),
            new BlockShape("Z4", "XX.", ".XX"),
            new BlockShape("L4", "X..", "XXX"),
            new BlockShape("J4", "..X", "XXX"),
        };

        /// <summary>
        /// Draws a shape by size weight. Shapes in <paramref name="exclude"/> are skipped so a hand never shows
        /// the same shape twice. If nothing is left to draw (or all remaining weights are zero), it falls back
        /// to drawing without the exclusion.
        /// </summary>
        public static BlockShape Pick(BlockSettings settings, Random rng, IEnumerable<BlockShape> exclude = null)
        {
            var excluded = exclude == null ? new HashSet<BlockShape>() : new HashSet<BlockShape>(exclude);
            var picked = PickWeighted(settings, rng, excluded);
            return picked ?? PickWeighted(settings, rng, new HashSet<BlockShape>());
        }

        /// <summary>A uniformly random shape of exactly this many cells (null if there is none).</summary>
        public static BlockShape PickOfSize(int size, Random rng)
        {
            var candidates = new List<BlockShape>();
            foreach (var shape in All) if (shape.Size == size) candidates.Add(shape);
            return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
        }

        static BlockShape PickWeighted(BlockSettings settings, Random rng, HashSet<BlockShape> excluded)
        {
            var weights = new float[All.Length];
            float total = 0f;
            for (int i = 0; i < All.Length; i++)
            {
                if (excluded.Contains(All[i])) continue;
                int sameSize = 0;
                foreach (var other in All) if (other.Size == All[i].Size) sameSize++;
                weights[i] = settings.WeightOfSize(All[i].Size) / sameSize;
                total += weights[i];
            }
            if (total <= 0f) return null;

            float roll = (float)rng.NextDouble() * total;
            for (int i = 0; i < All.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                if (roll < weights[i]) return All[i];
                roll -= weights[i];
            }
            // Floating point leftovers: return the last shape that was eligible.
            for (int i = All.Length - 1; i >= 0; i--)
            {
                if (weights[i] > 0f) return All[i];
            }
            return null;
        }
    }
}
