using System;

namespace VoidConverter.Core
{
    public struct FightResult
    {
        public int MonsterPower;
        public int Roll;
        public bool Won;
        public int Damage;
    }

    public static class Combat
    {
        /// <summary>
        /// The player's attack is rolled from their range. An attack at least as high as the monster's power kills it;
        /// otherwise the player loses HP equal to the difference.
        /// </summary>
        public static FightResult Fight(PlayerStats player, int monsterPower, Random rng, bool magic = false)
        {
            // A magic monster is met with the Magic attack (one fixed number), a physical one with the attack roll.
            int roll = magic ? player.Magic : rng.Next(player.AttackMin, player.AttackMax + 1);
            bool won = roll >= monsterPower;
            int damage = won ? 0 : monsterPower - roll;
            player.TakeDamage(damage);
            return new FightResult { MonsterPower = monsterPower, Roll = roll, Won = won, Damage = damage };
        }
    }
}
