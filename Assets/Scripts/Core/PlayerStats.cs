namespace VoidConverter.Core
{
    public class PlayerStats
    {
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int AttackMin { get; }
        public int AttackMax { get; }

        /// <summary>Rises whenever void cells are activated. Higher corruption makes void cells spawn monsters.</summary>
        public int Corruption { get; private set; }
        public int MaxCorruption { get; }

        public int Gold { get; private set; }

        public bool IsDead => Hp <= 0;

        public PlayerStats(int maxHp, int attackMin, int attackMax, int maxCorruption)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            AttackMin = attackMin;
            AttackMax = attackMax;
            MaxCorruption = maxCorruption;
        }

        /// <summary>Chance (0..1) that a uniformly rolled attack is at least <paramref name="monsterPower"/>.</summary>
        public float WinChance(int monsterPower)
        {
            int total = AttackMax - AttackMin + 1;
            int winning = 0;
            for (int roll = AttackMin; roll <= AttackMax; roll++)
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
    }
}
