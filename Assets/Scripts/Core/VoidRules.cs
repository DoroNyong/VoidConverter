using System;
using System.Collections.Generic;

namespace VoidConverter.Core
{
    public struct VoidSettings
    {
        // Base range of the monster power a void cell would spawn.
        public int PowerMin;
        public int PowerMax;
        // The range shifts up by floor(corruption * PowerPerCorruption).
        public float PowerPerCorruption;
    }

    public static class VoidRules
    {
        /// <summary>Chance that an activated void cell becomes a monster instead of a reward.</summary>
        public static float MonsterChance(PlayerStats player)
        {
            if (player.MaxCorruption <= 0) return 0f;
            float chance = (float)player.Corruption / player.MaxCorruption;
            return Math.Max(0f, Math.Min(1f, chance));
        }

        public static int PowerShift(PlayerStats player, VoidSettings settings)
        {
            return (int)Math.Floor(player.Corruption * settings.PowerPerCorruption);
        }

        public static int RollPower(PlayerStats player, VoidSettings settings, Random rng)
        {
            int shift = PowerShift(player, settings);
            return rng.Next(settings.PowerMin + shift, settings.PowerMax + shift + 1);
        }
    }

    /// <summary>One possible result of the covered void cells: how many became monsters, how likely, and the win chance then.</summary>
    public struct OutcomeGroup
    {
        public int Monsters;
        public float Probability;
        public float WinChance;
    }

    public static class Odds
    {
        /// <summary>
        /// Splits the contest by how many covered void cells became monsters (0..k). Each group reports its
        /// probability and the win chance given that group. Void cells share one power, so every outcome inside a
        /// group has the same total and the group's win chance is exact (with differing powers it would be an average).
        /// Groups that cannot happen are left out. The probabilities add up to 1.
        /// </summary>
        public static List<OutcomeGroup> Breakdown(PlayerStats player, int fixedPower, IReadOnlyList<int> voidPowers, float monsterChance)
        {
            int k = voidPowers.Count;
            var probability = new float[k + 1];
            var winWeighted = new float[k + 1];
            for (int mask = 0; mask < (1 << k); mask++)
            {
                float p = 1f;
                int sum = fixedPower, monsters = 0;
                for (int i = 0; i < k; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        p *= monsterChance;
                        sum += voidPowers[i];
                        monsters++;
                    }
                    else
                    {
                        p *= 1f - monsterChance;
                    }
                }
                probability[monsters] += p;
                winWeighted[monsters] += p * (sum <= 0 ? 1f : player.WinChance(sum));
            }

            var groups = new List<OutcomeGroup>();
            for (int m = 0; m <= k; m++)
            {
                if (probability[m] <= 1e-6f) continue;
                groups.Add(new OutcomeGroup { Monsters = m, Probability = probability[m], WinChance = winWeighted[m] / probability[m] });
            }
            return groups;
        }

        /// <summary>
        /// Exact chance to win a block's contest. Each covered void cell independently becomes a monster with
        /// <paramref name="monsterChance"/> (adding its power) or a reward, so every combination is weighed.
        /// With no monsters at all there is no fight, which counts as a win.
        /// </summary>
        public static float WinChance(PlayerStats player, int fixedPower, IReadOnlyList<int> voidPowers, float monsterChance)
        {
            int k = voidPowers.Count;
            float win = 0f;
            for (int mask = 0; mask < (1 << k); mask++)
            {
                float probability = 1f;
                int sum = fixedPower;
                for (int i = 0; i < k; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        probability *= monsterChance;
                        sum += voidPowers[i];
                    }
                    else
                    {
                        probability *= 1f - monsterChance;
                    }
                }
                win += probability * (sum <= 0 ? 1f : player.WinChance(sum));
            }
            return win;
        }
    }
}
