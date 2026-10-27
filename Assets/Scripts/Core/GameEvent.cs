using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>What an event choice does to the player. Add a kind here (and a case in EventRules.Apply) for a new effect.</summary>
    public enum EventEffectKind
    {
        Gold,
        Heal,
    }

    public class EventChoice
    {
        public string Label { get; }
        public EventEffectKind Kind { get; }
        public int Amount { get; }

        public EventChoice(string label, EventEffectKind kind, int amount)
        {
            Label = label;
            Kind = kind;
            Amount = amount;
        }

        /// <summary>Short effect text such as "Gold +10" or "HP +5" (ASCII, the HUD font has no Korean).</summary>
        public string Summary
        {
            get
            {
                switch (Kind)
                {
                    case EventEffectKind.Gold: return $"Gold {Amount:+#;-#}";
                    case EventEffectKind.Heal: return $"HP {Amount:+#;-#}";
                    default: return "-";
                }
            }
        }
    }

    /// <summary>One event: a prompt and the choices the player picks one of.</summary>
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
        /// <summary>Applies the choice to the player and returns a short result text such as "Gold +10".</summary>
        public static string Apply(PlayerStats player, EventChoice choice)
        {
            switch (choice.Kind)
            {
                case EventEffectKind.Gold:
                    if (choice.Amount >= 0) player.AddGold(choice.Amount);
                    else player.SpendGold(System.Math.Min(player.Gold, -choice.Amount));
                    return choice.Summary;
                case EventEffectKind.Heal:
                    int before = player.Hp;
                    player.Heal(choice.Amount);
                    // Healing at full HP gives nothing, so the text shows what was really gained.
                    return $"HP +{player.Hp - before}";
                default:
                    return "";
            }
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
