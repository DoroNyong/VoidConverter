using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>What one effect of an event choice does. Add a kind here (and a case in EventEffect) for a new effect.</summary>
    public enum EventEffectKind
    {
        Gold,
        Heal,
        // Lose HP. A choice with this effect is not offered when it would kill the player.
        Damage,
        // Permanent max HP (also fills the same amount of current HP).
        MaxHp,
        // Lose one random carried item. Not offered when the player carries none.
        LoseItem,
        // Gain a specific item. Not offered when the item is unknown (a bad id in the data).
        GainItem,
    }

    /// <summary>One thing a choice does to the player.</summary>
    public class EventEffect
    {
        public EventEffectKind Kind { get; }
        public int Amount { get; }
        /// <summary>The item a GainItem effect gives.</summary>
        public ItemDefinition Item { get; }

        public EventEffect(EventEffectKind kind, int amount, ItemDefinition item = null)
        {
            Kind = kind;
            Amount = amount;
            Item = item;
        }

        /// <summary>Short effect text such as "Gold +10" or "HP +5" (ASCII, the HUD font has no Korean).</summary>
        public string Summary
        {
            get
            {
                switch (Kind)
                {
                    case EventEffectKind.Gold: return $"Gold {Amount:+#;-#}";
                    case EventEffectKind.Heal: return $"HP +{Amount}";
                    case EventEffectKind.Damage: return $"HP -{Amount}";
                    case EventEffectKind.MaxHp: return $"Max HP {Amount:+#;-#}";
                    case EventEffectKind.LoseItem: return "Lose an item";
                    case EventEffectKind.GainItem: return Item != null ? $"Get {Item.Name}" : "Get ?";
                    default: return "-";
                }
            }
        }

        /// <summary>False when the player cannot pay for this effect (gold to spend, HP to lose without dying, an item to lose).</summary>
        public bool CanApply(PlayerStats player)
        {
            switch (Kind)
            {
                case EventEffectKind.Gold: return Amount >= 0 || player.Gold >= -Amount;
                case EventEffectKind.Damage: return player.Hp > Amount;
                case EventEffectKind.LoseItem: return player.Items.Count > 0;
                case EventEffectKind.GainItem: return Item != null;
                default: return true;
            }
        }

        /// <summary>Applies the effect and returns what really happened, e.g. "HP +0" when healing at full HP.</summary>
        public string Apply(PlayerStats player, System.Random rng)
        {
            switch (Kind)
            {
                case EventEffectKind.Gold:
                    if (Amount >= 0) player.AddGold(Amount);
                    else player.SpendGold(System.Math.Min(player.Gold, -Amount));
                    return Summary;
                case EventEffectKind.Heal:
                    int before = player.Hp;
                    player.Heal(Amount);
                    // Healing at full HP gives nothing, so the text shows what was really gained.
                    return $"HP +{player.Hp - before}";
                case EventEffectKind.Damage:
                    player.TakeDamage(Amount);
                    return Summary;
                case EventEffectKind.MaxHp:
                    player.AddMaxHp(Amount);
                    return Summary;
                case EventEffectKind.LoseItem:
                    if (player.Items.Count == 0) return "No item";
                    var lost = player.Items[rng.Next(player.Items.Count)];
                    player.RemoveItem(lost);
                    return $"Lost {lost.Name}";
                case EventEffectKind.GainItem:
                    if (Item == null) return "No item";
                    player.AddItem(Item);
                    return Summary;
                default:
                    return "";
            }
        }
    }

    public class EventChoice
    {
        public string Label { get; }
        /// <summary>Every effect, applied in this order. Empty means the choice does nothing (walk away).</summary>
        public IReadOnlyList<EventEffect> Effects { get; }

        public EventChoice(string label, IReadOnlyList<EventEffect> effects)
        {
            Label = label;
            Effects = effects;
        }

        /// <summary>Effect texts joined, e.g. "HP -5  Gold +25". A choice without effects reads "Nothing".</summary>
        public string Summary
        {
            get
            {
                if (Effects.Count == 0) return "Nothing";
                var parts = new List<string>();
                foreach (var e in Effects) parts.Add(e.Summary);
                return string.Join("  ", parts);
            }
        }

        /// <summary>The player can pick this choice only if every effect can be paid for.</summary>
        public bool IsAvailable(PlayerStats player)
        {
            foreach (var e in Effects)
            {
                if (!e.CanApply(player)) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// One event: a prompt and the choices the player picks one of. Keep at least one choice without costs
    /// (such as walking away), so the player is never stuck.
    /// </summary>
    public class EventDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<EventChoice> Choices { get; }

        public EventDefinition(string id, string title, string description, IReadOnlyList<EventChoice> choices)
        {
            Id = id;
            Title = title;
            Description = description;
            Choices = choices;
        }
    }

    public static class EventRules
    {
        /// <summary>Applies every effect of the choice in order and returns a short result text such as "Gold +10".</summary>
        public static string Apply(PlayerStats player, EventChoice choice, System.Random rng)
        {
            if (choice.Effects.Count == 0) return "Nothing happened";

            var parts = new List<string>();
            foreach (var e in choice.Effects) parts.Add(e.Apply(player, rng));
            return string.Join("  ", parts);
        }

        /// <summary>
        /// Picks an event at random, preferring ones not in <paramref name="seenIds"/>. Once every event was seen
        /// it picks from all of them again. Returns null if there are no events.
        /// </summary>
        public static EventDefinition Pick(IReadOnlyList<EventDefinition> events, ICollection<string> seenIds, System.Random rng)
        {
            if (events.Count == 0) return null;

            var fresh = new List<EventDefinition>();
            foreach (var e in events)
            {
                if (!seenIds.Contains(e.Id)) fresh.Add(e);
            }
            var pool = fresh.Count > 0 ? fresh : new List<EventDefinition>(events);
            var picked = pool[rng.Next(pool.Count)];
            seenIds.Add(picked.Id);
            return picked;
        }
    }
}
