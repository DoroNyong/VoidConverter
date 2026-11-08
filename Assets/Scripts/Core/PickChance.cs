using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>
    /// The chance of each cell under a block to be the one that fires. Every cell starts equal; the player's tile
    /// weights (events) make a kind likelier or rarer. No kind may pass PlayerStats.MaxTileChance.
    /// </summary>
    public static class PickChance
    {
        /// <summary>One chance (0..1, sum 1) per candidate, in the same order. Empty for no candidates.</summary>
        public static float[] Probabilities(IReadOnlyList<ActivatedCell> candidates, PlayerStats player)
        {
            int n = candidates.Count;
            var p = new float[n];
            if (n == 0) return p;

            var weights = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                weights[i] = player != null ? player.TileWeight(candidates[i].Type) : 1f;
                total += weights[i];
            }
            for (int i = 0; i < n; i++) p[i] = weights[i] / total;
            if (player == null) return p;

            // The cap: a kind that the weights made likelier than its plain share is cut down to the cap (or to its
            // plain share if that is already higher), and the rest is shared by the other kinds by weight.
            // Only one kind can pass 50%, so one pass is enough.
            var share = new Dictionary<CellType, float>();
            var count = new Dictionary<CellType, int>();
            for (int i = 0; i < n; i++)
            {
                var t = candidates[i].Type;
                share[t] = (share.TryGetValue(t, out var s) ? s : 0f) + p[i];
                count[t] = (count.TryGetValue(t, out var c) ? c : 0) + 1;
            }
            foreach (var kv in share)
            {
                var type = kv.Key;
                float plain = (float)count[type] / n;
                float target = System.Math.Max(player.MaxTileChance, plain);
                if (kv.Value <= target) continue;

                float otherWeight = 0f;
                for (int i = 0; i < n; i++)
                {
                    if (candidates[i].Type != type) otherWeight += weights[i];
                }
                if (otherWeight <= 0f) break;
                for (int i = 0; i < n; i++)
                {
                    p[i] = candidates[i].Type == type ? target / count[type] : (1f - target) * weights[i] / otherWeight;
                }
                break;
            }
            return p;
        }

        /// <summary>Picks the index of the cell that fires.</summary>
        public static int Pick(IReadOnlyList<ActivatedCell> candidates, PlayerStats player, System.Random rng)
        {
            var p = Probabilities(candidates, player);
            float roll = (float)rng.NextDouble();
            for (int i = 0; i < p.Length; i++)
            {
                if (roll < p[i]) return i;
                roll -= p[i];
            }
            return p.Length - 1;
        }
    }
}
