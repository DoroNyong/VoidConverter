using System.Collections.Generic;

namespace VoidConverter.Core
{
    public class PlayerStats
    {
        readonly int baseMaxHp;
        readonly int baseAttackMin;
        readonly int baseAttackMax;
        readonly List<ItemDefinition> items = new List<ItemDefinition>();

        /// <summary>Items carried for the rest of the run. The same item can appear more than once.</summary>
        public IReadOnlyList<ItemDefinition> Items => items;

        // Permanent max HP from events, on top of the base and the items.
        int bonusMaxHp;

        public int MaxHp => baseMaxHp + bonusMaxHp + SumOf(i => i.MaxHpBonus);
        public int Hp { get; private set; }
        /// <summary>
        /// Attack bonus of the block being placed or considered (bigger blocks hit harder). The caller sets it just
        /// before a fight or an odds calculation and puts it back to 0 afterwards.
        /// </summary>
        public int BlockBonus { get; set; }

        public int AttackMin => baseAttackMin + SumOf(i => i.AttackBonus) + BlockBonus;
        public int AttackMax => baseAttackMax + SumOf(i => i.AttackBonus) + BlockBonus;

        /// <summary>Rises whenever void cells are activated. Higher corruption makes void cells spawn monsters.</summary>
        public int Corruption { get; private set; }
        public int MaxCorruption { get; }

        public int Gold { get; private set; }

        public bool IsDead => Hp <= 0;

        public PlayerStats(int maxHp, int attackMin, int attackMax, int maxCorruption)
        {
            baseMaxHp = maxHp;
            Hp = maxHp;
            baseAttackMin = attackMin;
            baseAttackMax = attackMax;
            MaxCorruption = maxCorruption;
        }

        int SumOf(System.Func<ItemDefinition, int> bonus)
        {
            int total = 0;
            foreach (var item in items) total += bonus(item);
            return total;
        }

        /// <summary>
        /// Takes the item. A max HP bonus raises current HP by the same amount, so the extra health can be used at once.
        /// </summary>
        public void AddItem(ItemDefinition item)
        {
            items.Add(item);
            if (!IsDead) Hp = System.Math.Min(MaxHp, Hp + item.MaxHpBonus);
        }

        /// <summary>
        /// Gives the item up (for selling). Current HP is cut down to the new max, but never below 1, so selling
        /// an item cannot kill the player. Returns false if the player does not carry that item.
        /// </summary>
        public bool RemoveItem(ItemDefinition item)
        {
            if (!items.Remove(item)) return false;
            if (!IsDead) Hp = System.Math.Max(1, System.Math.Min(Hp, MaxHp));
            return true;
        }

        public bool RemoveItemAt(int index)
        {
            if (index < 0 || index >= items.Count) return false;
            return RemoveItem(items[index]);
        }

        /// <summary>Raises max HP for the rest of the run, and current HP by the same amount (like a max HP item).</summary>
        public void AddMaxHp(int amount)
        {
            amount = System.Math.Max(0, amount);
            bonusMaxHp += amount;
            if (!IsDead) Hp = System.Math.Min(MaxHp, Hp + amount);
        }

        /// <summary>Chance (0..1) that a uniformly rolled attack is at least <paramref name="monsterPower"/>.</summary>
        public float WinChance(int monsterPower)
        {
            int min = AttackMin, max = AttackMax;
            int total = max - min + 1;
            int winning = 0;
            for (int roll = min; roll <= max; roll++)
            {
                if (roll >= monsterPower) winning++;
            }
            return (float)winning / total;
        }

        public void TakeDamage(int amount)
        {
            Hp = System.Math.Max(0, Hp - amount);
        }

        public void Heal(int amount)
        {
            Hp = System.Math.Min(MaxHp, Hp + amount);
        }

        public void AddCorruption(int amount)
        {
            Corruption = System.Math.Max(0, System.Math.Min(MaxCorruption, Corruption + amount));
        }

        public void ReduceCorruption(int amount)
        {
            AddCorruption(-amount);
        }

        public void AddGold(int amount)
        {
            Gold += System.Math.Max(0, amount);
        }

        /// <summary>Pays gold. Returns false (and takes nothing) if the player has less than that.</summary>
        public bool SpendGold(int amount)
        {
            if (amount < 0 || amount > Gold) return false;
            Gold -= amount;
            return true;
        }
    }
}
