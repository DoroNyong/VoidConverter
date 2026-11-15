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
        // Permanent attack for the rest of the run (negative lowers it; the lowest attack stays at 1 or more).
        Attack,
        // Permanent magic for the rest of the run (negative lowers it, down to 0).
        Magic,
        // Makes one kind of cell likelier (+) or rarer (-) when a block picks the cell that fires. Amount is a
        // percent that compounds: +30 twice gives x1.69. Bounded by PlayerStats.MaxTileChance.
        TileWeight,
    }

    /// <summary>One thing a choice does to the player.</summary>
    public class EventEffect
    {
        public EventEffectKind Kind { get; }
        public int Amount { get; }
        /// <summary>The item a GainItem effect gives.</summary>
        public ItemDefinition Item { get; }
        /// <summary>The cell kind a TileWeight effect changes.</summary>
        public CellType Cell { get; }

        public EventEffect(EventEffectKind kind, int amount, ItemDefinition item = null, CellType cell = CellType.Monster)
        {
            Kind = kind;
            Amount = amount;
            Item = item;
            Cell = cell;
        }

        /// <summary>Short effect text such as "Gold +10" or "HP +5", in the current language.</summary>
        public string Summary
        {
            get
            {
                switch (Kind)
                {
                    case EventEffectKind.Gold: return Loc.T("eff.gold", Loc.Signed(Amount));
                    case EventEffectKind.Heal: return Loc.T("eff.heal", Amount);
                    case EventEffectKind.Damage: return Loc.T("eff.damage", Amount);
                    case EventEffectKind.MaxHp: return Loc.T("eff.maxhp", Loc.Signed(Amount));
                    case EventEffectKind.LoseItem: return Loc.T("eff.loseItem");
                    case EventEffectKind.GainItem: return Item != null ? Loc.T("eff.getItem", Item.Name) : Loc.T("eff.getUnknown");
                    case EventEffectKind.Attack: return $"{Loc.T("stat.atk")} {Loc.Signed(Amount)}";
                    case EventEffectKind.Magic: return $"{Loc.T("stat.mag")} {Loc.Signed(Amount)}";
                    case EventEffectKind.TileWeight: return Loc.T("eff.tile", Loc.CellName(Cell), Loc.Signed(Amount));
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
                case EventEffectKind.Attack: return player.CanAddAttack(Amount);
                case EventEffectKind.Magic: return player.CanAddMagic(Amount);
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
                    return Loc.T("eff.heal", player.Hp - before);
                case EventEffectKind.Damage:
                    player.TakeDamage(Amount);
                    return Summary;
                case EventEffectKind.MaxHp:
                    player.AddMaxHp(Amount);
                    return Summary;
                case EventEffectKind.LoseItem:
                    if (player.Items.Count == 0) return Loc.T("eff.noItem");
                    var lost = player.Items[rng.Next(player.Items.Count)];
                    player.RemoveItem(lost);
                    return Loc.T("eff.lost", lost.Name);
                case EventEffectKind.GainItem:
                    if (Item == null) return Loc.T("eff.noItem");
                    player.AddItem(Item);
                    return Summary;
                case EventEffectKind.Attack:
                    player.AddAttack(Amount);
                    return Summary;
                case EventEffectKind.Magic:
                    player.AddMagic(Amount);
                    return Summary;
                case EventEffectKind.TileWeight:
                    player.MultiplyTileWeight(Cell, 1f + Amount / 100f);
                    return Summary;
                default:
                    return "";
            }
        }
    }

    public class EventChoice
    {
        readonly string baseLabel;
        readonly string labelKey;
        /// <summary>The button text in the current language (the config text unless the key has a translation).</summary>
        public string Label => labelKey == null ? baseLabel : Loc.TOr(labelKey, baseLabel);
        /// <summary>Every effect, applied in this order. Empty means the choice does nothing (walk away).</summary>
        public IReadOnlyList<EventEffect> Effects { get; }

        /// <param name="labelKey">Key of the translated label (e.g. "event.old_chest.c0"), or null for none.</param>
        public EventChoice(string label, IReadOnlyList<EventEffect> effects, string labelKey = null)
        {
            baseLabel = label;
            this.labelKey = labelKey;
            Effects = effects;
        }

        /// <summary>Effect texts joined, e.g. "HP -5  Gold +25". A choice without effects reads "Nothing".</summary>
        public string Summary
        {
            get
            {
                if (Effects.Count == 0) return Loc.T("eff.nothing");
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
        readonly string baseTitle;
        readonly string baseDescription;

        public string Id { get; }
        /// <summary>The title in the current language (the config text unless a translation exists for this id).</summary>
        public string Title => Loc.TOr("event." + Id + ".title", baseTitle);
        public string Description => Loc.TOr("event." + Id + ".desc", baseDescription);
        public IReadOnlyList<EventChoice> Choices { get; }

        public EventDefinition(string id, string title, string description, IReadOnlyList<EventChoice> choices)
        {
            Id = id;
            baseTitle = title;
            baseDescription = description;
            Choices = choices;
        }
    }

    public static class EventRules
    {
        /// <summary>Applies every effect of the choice in order and returns a short result text such as "Gold +10".</summary>
        public static string Apply(PlayerStats player, EventChoice choice, System.Random rng)
        {
            if (choice.Effects.Count == 0) return Loc.T("eff.nothingHappened");

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
