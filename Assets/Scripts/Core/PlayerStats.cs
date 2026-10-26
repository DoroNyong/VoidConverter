namespace VoidConverter.Core
{
    public class PlayerStats
    {
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int AttackMin { get; }
        public int AttackMax { get; }

        public bool IsDead => Hp <= 0;

        public PlayerStats(int maxHp, int attackMin, int attackMax)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            AttackMin = attackMin;
            AttackMax = attackMax;
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
    }
}
