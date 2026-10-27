using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>An item the player can carry for the rest of the run. Its bonuses add up with the other items'.</summary>
    public class ItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        /// <summary>Added to both ends of the player's attack range.</summary>
        public int AttackBonus { get; }
        public int MaxHpBonus { get; }

        public ItemDefinition(string id, string name, int attackBonus, int maxHpBonus)
        {
            Id = id;
            Name = name;
            AttackBonus = attackBonus;
            MaxHpBonus = maxHpBonus;
        }

        /// <summary>Short effect text such as "ATK +1" or "HP +5" (ASCII, the HUD font has no Korean).</summary>
        public string Summary
        {
            get
            {
                var parts = new List<string>();
                if (AttackBonus != 0) parts.Add($"ATK {AttackBonus:+#;-#}");
                if (MaxHpBonus != 0) parts.Add($"HP {MaxHpBonus:+#;-#}");
                return parts.Count > 0 ? string.Join(" ", parts) : "-";
            }
        }
    }
}
