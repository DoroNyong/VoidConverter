using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>The kinds of stage a run is made of. The run walks through them in the order GameConfig.stages lists.</summary>
    public enum StageType
    {
        Combat,
        // Placeholder until events exist: a screen with a Continue button.
        Event,
        Shop,
        // Placeholder until the boss exists: plays like a Combat stage.
        Boss,
    }

    /// <summary>One product on the shop shelf: either an item to carry, or a heal. Each product can be bought once per visit.</summary>
    public class ShopOffer
    {
        public string Name { get; }
        /// <summary>Short effect text such as "ATK +1" or "HP +10".</summary>
        public string Summary { get; }
        public int Price { get; }
        /// <summary>The item given on purchase, or null for a heal offer.</summary>
        public ItemDefinition Item { get; }
        /// <summary>HP restored on purchase (0 for an item offer).</summary>
        public int HealAmount { get; }
        public bool Sold { get; set; }

        ShopOffer(string name, string summary, int price, ItemDefinition item, int healAmount)
        {
            Name = name;
            Summary = summary;
            Price = price;
            Item = item;
            HealAmount = healAmount;
        }

        public static ShopOffer ForItem(ItemDefinition item, int price) =>
            new ShopOffer(item.Name, item.Summary, price, item, 0);

        public static ShopOffer ForHeal(int amount, int price) =>
            new ShopOffer("Heal", $"HP +{amount}", price, null, amount);
    }

    /// <summary>The buying rules. There is no selling: items only leave the player through events and boss patterns.</summary>
    public static class Shop
    {
        public enum BuyResult
        {
            Bought,
            AlreadySold,
            NotEnoughGold,
            // Healing while already at full HP would waste the gold.
            FullHealth,
        }

        public static BuyResult CanBuy(PlayerStats player, ShopOffer offer)
        {
            if (offer.Sold) return BuyResult.AlreadySold;
            if (player.Gold < offer.Price) return BuyResult.NotEnoughGold;
            if (offer.Item == null && player.Hp >= player.MaxHp) return BuyResult.FullHealth;
            return BuyResult.Bought;
        }

        /// <summary>Pays and applies the offer if it can be bought. Returns why not otherwise; nothing changes then.</summary>
        public static BuyResult Buy(PlayerStats player, ShopOffer offer)
        {
            var check = CanBuy(player, offer);
            if (check != BuyResult.Bought) return check;

            player.SpendGold(offer.Price);
            if (offer.Item != null) player.AddItem(offer.Item);
            else player.Heal(offer.HealAmount);
            offer.Sold = true;
            return BuyResult.Bought;
        }
    }
}
