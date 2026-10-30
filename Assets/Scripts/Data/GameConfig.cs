using System;
using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Core;

namespace VoidConverter.Data
{
    /// <summary>One item as edited in the inspector. Add rows to GameConfig.items to create more items.</summary>
    [Serializable]
    public class ItemData
    {
        [Tooltip("Unique key, e.g. power_ring.")]
        public string id;
        [Tooltip("Shown in the item list. ASCII only for now.")]
        public string displayName;
        [Tooltip("Added to both ends of the attack range.")]
        public int attackBonus;
        [Tooltip("Added to the max HP. Current HP rises by the same amount.")]
        public int maxHpBonus;

        public ItemDefinition ToDefinition() => new ItemDefinition(id, displayName, attackBonus, maxHpBonus);
    }

    /// <summary>One shelf slot as edited in the inspector: an item (by id) or a heal, with its price.</summary>
    [Serializable]
    public class ShopOfferData
    {
        [Tooltip("Id of a catalog item to sell. Leave empty to sell a heal instead.")]
        public string itemId;
        [Tooltip("HP restored when itemId is empty.")]
        public int healAmount = 10;
        public int price = 30;
    }

    [Serializable]
    public class EventEffectData
    {
        public EventEffectKind kind;
        [Tooltip("Gold (negative pays gold), HP healed / lost, max HP gained. Ignored by the item effects.")]
        public int amount;
        [Tooltip("GainItem only: id of the catalog item to give.")]
        public string itemId;
    }

    [Serializable]
    public class EventChoiceData
    {
        [Tooltip("Button text. ASCII only for now.")]
        public string label;
        [Tooltip("Applied in order. Leave empty for a choice that does nothing (walk away).")]
        public List<EventEffectData> effects = new List<EventEffectData>();
    }

    /// <summary>One event as edited in the inspector. Add rows to GameConfig.events to create more events.</summary>
    [Serializable]
    public class EventData
    {
        [Tooltip("Unique key, e.g. abandoned_camp.")]
        public string id;
        public string title;
        [TextArea] public string description;
        public List<EventChoiceData> choices = new List<EventChoiceData>();

        /// <summary><paramref name="findItem"/> turns an item id into the catalog item (null if unknown).</summary>
        public EventDefinition ToDefinition(Func<string, ItemDefinition> findItem)
        {
            var list = new List<EventChoice>();
            foreach (var c in choices)
            {
                var effects = new List<EventEffect>();
                foreach (var e in c.effects)
                {
                    var item = e.kind == EventEffectKind.GainItem ? findItem(e.itemId) : null;
                    effects.Add(new EventEffect(e.kind, e.amount, item));
                }
                list.Add(new EventChoice(c.label, effects));
            }
            return new EventDefinition(id, title, description, list);
        }
    }

    /// <summary>Tunable numbers live here so they can be changed without touching code.</summary>
    [CreateAssetMenu(menuName = "VoidConverter/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth = 5;
        public int gridHeight = 5;
        public float cellSize = 1f;
        public float cellGap = 0.06f;
        [Header("Cell Counts")]
        [Tooltip("Exactly this many void cells per board.")]
        public int voidCount = 5;
        [Tooltip("Every board has at least this many monster cells (used when Monster Count is 0).")]
        public int monsterMin = 10;
        [Tooltip("Above 0: exactly this many monster cells. Gold and heal fill up to Reward Max and the rest of the board is empty. 0 = the older rule (Monster Min plus a weighted share of the leftover cells).")]
        public int monsterCount = 10;
        [Tooltip("Every board has at least this many gold cells.")]
        public int goldMin = 2;
        [Tooltip("Every board has at least this many heal cells.")]
        public int healMin = 2;
        [Tooltip("Gold and heal cells together never exceed this per board (0 = no cap). Cells over the cap become monsters.")]
        public int rewardMax = 6;
        [Tooltip("The cells left after the void count and the minimums are split among monster/gold/heal by these weights.")]
        public float monsterWeight = 5f;
        public float goldWeight = 1f;
        public float healWeight = 1f;

        [Header("Cell Spacing")]
        [Tooltip("Void cells keep this Chebyshev distance from each other. 2 = not even diagonally touching. 1 = no rule.")]
        public int voidMinSpacing = 2;
        [Tooltip("Gold/heal cells keep this Chebyshev distance from each other. 2 = not even diagonally touching. 1 = no rule.")]
        public int rewardMinSpacing = 2;
        [Tooltip("The largest group of edge-connected monster cells may not exceed this. 0 = no cap.")]
        public int monsterMaxCluster = 8;

        [Header("Monsters")]
        public int monsterPowerMin = 2;
        public int monsterPowerMax = 4;

        [Header("Player")]
        public int playerMaxHp = 10;
        public int playerAttackMin = 2;
        public int playerAttackMax = 4;

        [Header("Void")]
        [Tooltip("Corruption cap. Monster chance of a void cell = corruption / this.")]
        public int maxCorruption = 10;
        [Tooltip("Base range of the monster power a void cell would spawn.")]
        public int voidPowerMin = 2;
        public int voidPowerMax = 4;
        [Tooltip("The void power range shifts up by floor(corruption * this).")]
        public float voidPowerPerCorruption = 0.2f;

        [Header("Void Kinds")]
        [Tooltip("How many of the void cells on a board are each special kind. The rest are Normal.")]
        public int voidBuffCount = 1;
        public int voidSpreadCount = 1;
        public int voidDangerCount = 1;
        [Tooltip("Buff void: each monster next to it gets this much stronger while it stands.")]
        public int voidBuffPower = 1;
        [Tooltip("Danger void: hit points. Each block placed on it hits for the player's attack roll.")]
        public int voidDangerHp = 6;
        [Tooltip("Danger void: gold paid when it breaks.")]
        public int voidDangerRewardGold = 3;

        [Header("Rewards")]
        [Tooltip("Gold gained for beating monsters = their summed power * this (only when the block's contest is won).")]
        public float goldPerMonsterPower = 1f;
        [Tooltip("Gold from a gold cell, and from a void cell that turned out to be a reward.")]
        public int goldCellReward = 1;
        [Tooltip("HP restored by a heal cell.")]
        public int healCellAmount = 1;

        [Header("Clear Rule")]
        [Tooltip("On: a Combat round ends when every monster cell is covered (a lost fight still removes the monster), blocks may overlap spent cells, and Max Placements / manual end do not apply. Off: the older round rules below.")]
        public bool clearAllRule = true;
        [Tooltip("Clear Rule: the blocks placed in one round before overtime starts.")]
        public int clearTurnLimit = 5;
        [Tooltip("Clear Rule: after every block placed at or past the turn limit, each monster left gets this much stronger.")]
        public int overtimeGrowth = 1;

        [Header("Round")]
        [Tooltip("How many blocks the player may place in one round (older rule, ignored by the Clear Rule).")]
        public int maxPlacements = 6;
        [Tooltip("The run is cleared after the last stage. Combat and Boss stages play a grid round.")]
        public List<StageType> stages = new List<StageType>
        {
            StageType.Combat, StageType.Event, StageType.Combat, StageType.Event,
            StageType.Combat, StageType.Shop, StageType.Boss,
        };
        [Tooltip("The round can be ended by hand once this many blocks were placed.")]
        public int minPlacementsToEnd = 3;
        [Tooltip("Corruption lost when a round ends.")]
        public int corruptionDecayPerRound = 2;

        [Header("Boss (MVP, to be redesigned)")]
        [Tooltip("The boss can be defeated once this many blocks were placed in the boss stage.")]
        public int bossHoldPlacements = 5;
        [Tooltip("Every monster still on the board gets this much stronger after each block placed in the boss stage (on top of the Clear Rule's overtime). 1 made the boss unwinnable with the Clear Rule, so it is 0.")]
        public int bossMonsterGrowth = 0;
        [Tooltip("This many monsters on the boss board are replaced by strong ones.")]
        public int bossStrongMonsterCount = 3;
        public int bossStrongMonsterPower = 5;

        [Header("Screen Transition")]
        [Tooltip("Seconds for each half of a screen transition (fade out, then fade in). 0 = switch screens at once.")]
        public float fadeSeconds = 0.3f;
        [Tooltip("Seconds to wait after a round ends before the fade starts, so its last result can be read.")]
        public float fadeDelaySeconds = 0.5f;

        [Header("Items")]
        public List<ItemData> items = new List<ItemData>
        {
            new ItemData { id = "power_ring", displayName = "Power Ring", attackBonus = 1, maxHpBonus = 0 },
            new ItemData { id = "life_charm", displayName = "Life Charm", attackBonus = 0, maxHpBonus = 2 },
        };

        [Header("Events")]
        [Tooltip("The pool the Event stages draw from, without repeats until every event was seen.")]
        public List<EventData> events = new List<EventData>
        {
            new EventData
            {
                id = "abandoned_camp",
                title = "Abandoned Camp",
                description = "You find a deserted camp.\nChoose one.",
                choices = new List<EventChoiceData>
                {
                    Choice("Take the coins", Effect(EventEffectKind.Gold, 4)),
                    Choice("Rest by the fire", Effect(EventEffectKind.Heal, 2)),
                },
            },
            new EventData
            {
                id = "shady_peddler",
                title = "Shady Peddler",
                description = "A peddler grins at you.\n\"Everything has a price.\"",
                choices = new List<EventChoiceData>
                {
                    Choice("Sell your blood", Effect(EventEffectKind.Damage, 2), Effect(EventEffectKind.Gold, 8)),
                    Choice("Buy a tonic", Effect(EventEffectKind.Gold, -5), Effect(EventEffectKind.Heal, 4)),
                    Choice("Walk away"),
                },
            },
            new EventData
            {
                id = "spring",
                title = "Clear Spring",
                description = "Clean water bubbles from the ground.",
                choices = new List<EventChoiceData>
                {
                    Choice("Drink", Effect(EventEffectKind.Heal, 3)),
                    Choice("Bottle some water", Effect(EventEffectKind.Gold, 2)),
                },
            },
            new EventData
            {
                id = "wandering_trader",
                title = "Wandering Trader",
                description = "A trader opens her pack.\n\"A charm for a fair price?\"",
                choices = new List<EventChoiceData>
                {
                    Choice("Buy the charm", Effect(EventEffectKind.Gold, -12), GainItem("life_charm")),
                    Choice("Walk away"),
                },
            },
            new EventData
            {
                id = "old_chest",
                title = "Old Chest",
                description = "A rusty chest, and the lock bites.\nSomething glints inside.",
                choices = new List<EventChoiceData>
                {
                    Choice("Pry it open", Effect(EventEffectKind.Damage, 3), GainItem("power_ring")),
                    Choice("Leave it"),
                },
            },
            new EventData
            {
                id = "cursed_altar",
                title = "Cursed Altar",
                description = "The altar hungers for an offering.\nIt will take one of your items.",
                choices = new List<EventChoiceData>
                {
                    Choice("Make an offering", Effect(EventEffectKind.LoseItem, 1), Effect(EventEffectKind.MaxHp, 3)),
                    Choice("Leave"),
                },
            },
        };

        static EventEffectData Effect(EventEffectKind kind, int amount) => new EventEffectData { kind = kind, amount = amount };

        static EventEffectData GainItem(string itemId) => new EventEffectData { kind = EventEffectKind.GainItem, itemId = itemId };

        static EventChoiceData Choice(string label, params EventEffectData[] effects) =>
            new EventChoiceData { label = label, effects = new List<EventEffectData>(effects) };

        [Header("Shop")]
        [Tooltip("The fixed shelf shown at every Shop stage.")]
        public List<ShopOfferData> shopOffers = new List<ShopOfferData>
        {
            new ShopOfferData { itemId = "power_ring", price = 20 },
            new ShopOfferData { itemId = "life_charm", price = 15 },
            new ShopOfferData { itemId = "", healAmount = 4, price = 14 },
        };

        [Header("Blocks")]
        public int handSize = 3;
        [Tooltip("On: slot i always holds a shape with Fixed Hand Sizes[i] cells (a used slot refills with the same size). Off: the weights below decide.")]
        public bool fixedHand = true;
        public List<int> fixedHandSizes = new List<int> { 2, 3, 4 };
        [Tooltip("Attack added to the roll of a block with this many cells (a bigger block hits harder).")]
        public int attackBonus2 = 0;
        public int attackBonus3 = 1;
        public int attackBonus4 = 2;
        [Tooltip("Probability weight of 2-cell blocks (split evenly among that size's shapes).")]
        public float blockWeight2 = 4f;
        [Tooltip("Probability weight of 3-cell blocks.")]
        public float blockWeight3 = 4f;
        [Tooltip("Probability weight of 4-cell blocks.")]
        public float blockWeight4 = 2f;

        [Header("Hand Layout")]
        public float handSlotSize = 2.4f;
        public float handSlotGap = 0.2f;
        public float handMiniCellSize = 0.55f;

        public GridSettings ToGridSettings() => new GridSettings
        {
            VoidCount = voidCount,
            MonsterMin = monsterMin,
            MonsterCount = monsterCount,
            GoldMin = goldMin,
            HealMin = healMin,
            RewardMax = rewardMax,
            MonsterWeight = monsterWeight,
            GoldWeight = goldWeight,
            HealWeight = healWeight,
            VoidMinSpacing = voidMinSpacing,
            RewardMinSpacing = rewardMinSpacing,
            MonsterMaxCluster = monsterMaxCluster,
            MonsterPowerMin = monsterPowerMin,
            MonsterPowerMax = monsterPowerMax,
            VoidPowerMin = voidPowerMin,
            VoidPowerMax = voidPowerMax,
            BuffCount = voidBuffCount,
            SpreadCount = voidSpreadCount,
            DangerCount = voidDangerCount,
            BuffPower = voidBuffPower,
            DangerHp = voidDangerHp,
        };

        public VoidSettings ToVoidSettings() => new VoidSettings
        {
            PowerMin = voidPowerMin,
            PowerMax = voidPowerMax,
            PowerPerCorruption = voidPowerPerCorruption,
        };

        /// <summary>The item with this id, or null.</summary>
        public ItemDefinition FindItem(string id)
        {
            foreach (var data in items)
            {
                if (data.id == id) return data.ToDefinition();
            }
            return null;
        }

        public List<EventDefinition> CreateEvents()
        {
            var list = new List<EventDefinition>();
            foreach (var data in events) list.Add(data.ToDefinition(FindItem));
            return list;
        }

        /// <summary>A fresh shelf for one shop visit. Rows naming an unknown item are skipped.</summary>
        public List<ShopOffer> CreateShopOffers()
        {
            var offers = new List<ShopOffer>();
            foreach (var data in shopOffers)
            {
                if (string.IsNullOrEmpty(data.itemId))
                {
                    offers.Add(ShopOffer.ForHeal(data.healAmount, data.price));
                    continue;
                }
                var item = FindItem(data.itemId);
                if (item != null) offers.Add(ShopOffer.ForItem(item, data.price));
            }
            return offers;
        }

        public PlayerStats CreatePlayer() => new PlayerStats(playerMaxHp, playerAttackMin, playerAttackMax, maxCorruption);

        /// <summary>The attack bonus of a block with this many cells.</summary>
        public int AttackBonusFor(int cellCount)
        {
            switch (cellCount)
            {
                case 2: return attackBonus2;
                case 3: return attackBonus3;
                case 4: return attackBonus4;
                default: return 0;
            }
        }

        /// <summary>The per-slot block sizes of a fixed hand, or null when the hand is drawn at random.</summary>
        public int[] FixedHandSizesOrNull() => fixedHand && fixedHandSizes != null && fixedHandSizes.Count > 0 ? fixedHandSizes.ToArray() : null;

        public BlockSettings ToBlockSettings() => new BlockSettings
        {
            Size2Weight = blockWeight2,
            Size3Weight = blockWeight3,
            Size4Weight = blockWeight4,
        };
    }
}
