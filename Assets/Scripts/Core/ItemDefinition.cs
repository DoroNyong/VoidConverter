using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>An item the player can carry for the rest of the run. Its bonuses add up with the other items'.</summary>
    public class ItemDefinition
    {
        public string Id { get; }
        readonly string baseName;
        /// <summary>The display name in the current language (the config name, unless a translation exists for this id).</summary>
        public string Name => Loc.TOr("item." + Id + ".name", baseName);
        /// <summary>Added to both ends of the player's attack range.</summary>
        public int AttackBonus { get; }
        public int MaxHpBonus { get; }
        /// <summary>Added to the player's Magic attack.</summary>
        public int MagicBonus { get; }

        public ItemDefinition(string id, string name, int attackBonus, int maxHpBonus, int magicBonus = 0)
        {
            MagicBonus = magicBonus;
            Id = id;
            baseName = name;
            AttackBonus = attackBonus;
            MaxHpBonus = maxHpBonus;
        }

        /// <summary>Short effect text such as "ATK +1" or "HP +5", in the current language.</summary>
        public string Summary
        {
            get
            {
                var parts = new List<string>();
                if (AttackBonus != 0) parts.Add($"{Loc.T("stat.atk")} {Loc.Signed(AttackBonus)}");
                if (MagicBonus != 0) parts.Add($"{Loc.T("stat.mag")} {Loc.Signed(MagicBonus)}");
                if (MaxHpBonus != 0) parts.Add($"{Loc.T("stat.hp")} {Loc.Signed(MaxHpBonus)}");
                return parts.Count > 0 ? string.Join(" ", parts) : "-";
            }
        }
    }
}
