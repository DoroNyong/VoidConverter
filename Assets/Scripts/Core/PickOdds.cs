using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>What a block's pick-one placement is worth: the chance of each kind of cell and what it is worth on average.</summary>
    public struct PickOddsResult
    {
        /// <summary>How many fresh cells the block covers. 0 means the block would do nothing.</summary>
        public int Candidates;
        public int Monsters;
        public int Golds;
        public int Heals;
        public int Traps;
        /// <summary>How many of the covered monsters the player is sure to beat (all of them with a single attack number).</summary>
        public int MonsterWins;
        /// <summary>Average win chance (0..1) over the covered monsters, 0 if there are none.</summary>
        public float MonsterWinChance;
        /// <summary>Average gold of one placement, counting gold cells and won fights.</summary>
        public float ExpectedGold;
        /// <summary>Average HP lost in lost fights of one placement.</summary>
        public float ExpectedDamage;
        /// <summary>Average HP healed of one placement.</summary>
        public float ExpectedHeal;

        // Chance (0..1) that the cell which fires is of this kind. Equal per cell unless tile weights are set.
        public float MonsterChance;
        public float GoldChance;
        public float TrapChance;
        public float HealChance;
    }

    /// <summary>Pure math for the pick-one rule: a block lets one random covered fresh cell fire.</summary>
    public static class PickOdds
    {
        /// <summary>
        /// <paramref name="candidates"/> are the fresh cells under the block. Each one's chance comes from
        /// <see cref="PickChance"/> (equal unless the player has tile weights).
        /// A monster fight is the player's attack roll (uniform over their range) against its power.
        /// </summary>
        public static PickOddsResult Analyze(PlayerStats player, IReadOnlyList<ActivatedCell> candidates,
            int goldCellReward, int healCellAmount, float goldPerMonsterPower, int trapDamage)
        {
            var result = new PickOddsResult { Candidates = candidates.Count };
            if (candidates.Count == 0) return result;

            var chances = PickChance.Probabilities(candidates, player);
            float winSum = 0f, goldSum = 0f, damageSum = 0f, healSum = 0f;
            int min = player.AttackMin, max = player.AttackMax;
            int rolls = max - min + 1;
            for (int i = 0; i < candidates.Count; i++)
            {
                var cell = candidates[i];
                float chance = chances[i];
                switch (cell.Type)
                {
                    case CellType.Monster:
                        result.Monsters++;
                        result.MonsterChance += chance;
                        float win = cell.Magic ? (player.Magic >= cell.Power ? 1f : 0f) : player.WinChance(cell.Power);
                        winSum += win * chance;
                        if (win >= 1f) result.MonsterWins++;
                        goldSum += chance * win * cell.Power * goldPerMonsterPower;
                        float damage = 0f;
                        if (cell.Magic)
                        {
                            damageSum += chance * System.Math.Max(0, cell.Power - player.Magic);
                            break;
                        }
                        for (int roll = min; roll <= max; roll++)
                        {
                            if (roll < cell.Power) damage += cell.Power - roll;
                        }
                        damageSum += chance * damage / rolls;
                        break;
                    case CellType.Gold:
                        result.Golds++;
                        result.GoldChance += chance;
                        goldSum += chance * goldCellReward;
                        break;
                    case CellType.Trap:
                        result.Traps++;
                        result.TrapChance += chance;
                        damageSum += chance * trapDamage;
                        break;
                    case CellType.Heal:
                        result.Heals++;
                        result.HealChance += chance;
                        healSum += chance * healCellAmount;
                        break;
                }
            }

            result.MonsterWinChance = result.MonsterChance <= 0f ? 0f : winSum / result.MonsterChance;
            result.ExpectedGold = goldSum;
            result.ExpectedDamage = damageSum;
            result.ExpectedHeal = healSum;
            return result;
        }
    }
}
