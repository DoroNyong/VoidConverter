using System;
using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>
    /// Decides which cell type goes where. Counts are fixed (an exact number of void cells, minimums for the
    /// rest), then positions are drawn so cells do not clump: void cells keep their distance, reward cells do not
    /// touch each other, and monsters may not form one giant block. If the constraints cannot be met after many
    /// tries, they are relaxed step by step so a board is always produced.
    /// </summary>
    public static class GridLayout
    {
        // A tight monster cluster cap can reject most random layouts (about 1 in 50 pass at a cap of 8), so allow
        // plenty of retries. Each try takes a few microseconds.
        const int MaxAttempts = 4000;

        public class Layout
        {
            public CellType[,] Types;
            /// <summary>0: every rule held. 1: the monster cluster cap was dropped. 2: all spacing rules were dropped.</summary>
            public int Relaxation;
        }

        public static Layout Build(int width, int height, GridSettings s, Random rng)
        {
            var counts = ResolveCounts(width * height, s, rng);

            for (int tier = 0; tier <= 2; tier++)
            {
                for (int attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    var types = TryBuild(width, height, counts, s, rng, tier);
                    if (types != null) return new Layout { Types = types, Relaxation = tier };
                }
            }

            // Unreachable in practice: tier 2 has no constraints and cannot fail.
            return new Layout { Types = TryBuild(width, height, counts, s, rng, 2), Relaxation = 2 };
        }

        struct Counts
        {
            public int Void, Monster, Gold, Heal, Empty;
        }

        // Exact void count, then each other type's minimum, then the leftover cells split by weight.
        static Counts ResolveCounts(int cellTotal, GridSettings s, Random rng)
        {
            var c = new Counts { Void = Math.Min(Math.Max(s.VoidCount, 0), cellTotal) };
            int rest = cellTotal - c.Void;

            if (s.MonsterCount > 0)
            {
                // Exact monster count; gold and heal reach RewardMax (at least their minimums); the rest stay empty.
                c.Monster = Math.Min(s.MonsterCount, rest);
                int room = rest - c.Monster;
                c.Gold = Math.Min(Math.Max(s.GoldMin, 0), room);
                c.Heal = Math.Min(Math.Max(s.HealMin, 0), room - c.Gold);
                int rewardTarget = Math.Min(room, Math.Max(s.RewardMax, c.Gold + c.Heal));
                float rewardWeight = s.GoldWeight + s.HealWeight;
                while (c.Gold + c.Heal < rewardTarget)
                {
                    bool gold = rewardWeight <= 0f || rng.NextDouble() * rewardWeight < s.GoldWeight;
                    if (gold) c.Gold++; else c.Heal++;
                }
                c.Empty = room - c.Gold - c.Heal;
                return c;
            }

            c.Monster = Math.Max(s.MonsterMin, 0);
            c.Gold = Math.Max(s.GoldMin, 0);
            c.Heal = Math.Max(s.HealMin, 0);

            // Minimums that do not fit are trimmed, largest first. A reward cap below the reward minimums trims them too.
            while (c.Monster + c.Gold + c.Heal > rest || (s.RewardMax > 0 && c.Gold + c.Heal > s.RewardMax))
            {
                bool rewardsOverCap = s.RewardMax > 0 && c.Gold + c.Heal > s.RewardMax;
                if (!rewardsOverCap && c.Monster >= c.Gold && c.Monster >= c.Heal) c.Monster--;
                else if (c.Gold >= c.Heal) c.Gold--;
                else c.Heal--;
            }

            int extra = rest - (c.Monster + c.Gold + c.Heal);
            float weightTotal = s.MonsterWeight + s.GoldWeight + s.HealWeight;
            for (int i = 0; i < extra; i++)
            {
                float roll = weightTotal > 0f ? (float)rng.NextDouble() * weightTotal : 0f;
                bool rewardsFull = s.RewardMax > 0 && c.Gold + c.Heal >= s.RewardMax;
                // Once the reward cap is reached, every remaining cell is a monster.
                if (weightTotal <= 0f || roll < s.MonsterWeight || rewardsFull) c.Monster++;
                else if (roll < s.MonsterWeight + s.GoldWeight) c.Gold++;
                else c.Heal++;
            }
            return c;
        }

        // tier 0: all rules. tier 1: no monster cluster cap. tier 2: no spacing rules at all.
        static CellType[,] TryBuild(int width, int height, Counts counts, GridSettings s, Random rng, int tier)
        {
            var positions = new List<(int x, int y)>();
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) positions.Add((x, y));
            Shuffle(positions, rng);

            var taken = new bool[width, height];
            var types = new CellType[width, height];

            // 1) Void cells, kept apart.
            int voidSpacing = tier >= 2 ? 1 : Math.Max(s.VoidMinSpacing, 1);
            var voids = new List<(int x, int y)>();
            foreach (var p in positions)
            {
                if (voids.Count >= counts.Void) break;
                if (FarEnough(p, voids, voidSpacing, chebyshev: true)) voids.Add(p);
            }
            if (voids.Count < counts.Void) return null;
            foreach (var p in voids) { taken[p.x, p.y] = true; types[p.x, p.y] = CellType.Void; }

            // 2) Reward cells (gold and heal together), kept apart like the void cells (not even diagonally touching).
            //    Otherwise one L- or S-shaped block could take two diagonal rewards at once.
            int rewardSpacing = tier >= 2 ? 1 : Math.Max(s.RewardMinSpacing, 1);
            int rewardCount = counts.Gold + counts.Heal;
            var rewards = new List<(int x, int y)>();
            Shuffle(positions, rng);
            foreach (var p in positions)
            {
                if (rewards.Count >= rewardCount) break;
                if (taken[p.x, p.y]) continue;
                if (FarEnough(p, rewards, rewardSpacing, chebyshev: true)) rewards.Add(p);
            }
            if (rewards.Count < rewardCount) return null;
            Shuffle(rewards, rng);
            for (int i = 0; i < rewards.Count; i++)
            {
                var p = rewards[i];
                taken[p.x, p.y] = true;
                types[p.x, p.y] = i < counts.Gold ? CellType.Gold : CellType.Heal;
            }

            // 3) Everything left is a monster, except the cells that are meant to stay empty (random ones).
            var leftover = new List<(int x, int y)>();
            foreach (var p in positions) if (!taken[p.x, p.y]) leftover.Add(p);
            Shuffle(leftover, rng);
            for (int i = 0; i < leftover.Count; i++)
            {
                types[leftover[i].x, leftover[i].y] = i < counts.Empty ? CellType.Empty : CellType.Monster;
            }

            if (tier == 0 && s.MonsterMaxCluster > 0 && LargestMonsterCluster(types, width, height) > s.MonsterMaxCluster) return null;
            return types;
        }

        // Chebyshev distance counts diagonals as touching; Manhattan distance does not.
        static bool FarEnough((int x, int y) p, List<(int x, int y)> others, int minSpacing, bool chebyshev)
        {
            if (minSpacing <= 1) return true;
            foreach (var o in others)
            {
                int dx = Math.Abs(p.x - o.x), dy = Math.Abs(p.y - o.y);
                int distance = chebyshev ? Math.Max(dx, dy) : dx + dy;
                if (distance < minSpacing) return false;
            }
            return true;
        }

        public static int LargestMonsterCluster(CellType[,] types, int width, int height)
        {
            var seen = new bool[width, height];
            int largest = 0;
            var stack = new Stack<(int x, int y)>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (seen[x, y] || types[x, y] != CellType.Monster) continue;
                    int size = 0;
                    stack.Push((x, y));
                    seen[x, y] = true;
                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();
                        size++;
                        foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            int nx = p.x + d.Item1, ny = p.y + d.Item2;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            if (seen[nx, ny] || types[nx, ny] != CellType.Monster) continue;
                            seen[nx, ny] = true;
                            stack.Push((nx, ny));
                        }
                    }
                    largest = Math.Max(largest, size);
                }
            }
            return largest;
        }

        static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
