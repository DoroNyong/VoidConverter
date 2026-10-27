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
    public class EventChoiceData
    {
        [Tooltip("Button text. ASCII only for now.")]
        public string label;
        public EventEffectKind effect;
        [Tooltip("Gold gained / HP restored (negative gold takes gold away).")]
        public int amount;
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

        public EventDefinition ToDefinition()
        {
            var list = new List<EventChoice>();
            foreach (var c in choices) list.Add(new EventChoice(c.label, c.effect, c.amount));
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
        [Tooltip("Every board has at least this many monster cells.")]
        public int monsterMin = 10;
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
        public int monsterPowerMin = 3;
        public int monsterPowerMax = 9;

        [Header("Player")]
        public int playerMaxHp = 30;
        public int playerAttackMin = 5;
        public int playerAttackMax = 12;

        [Header("Void")]
        [Tooltip("Corruption cap. Monster chance of a void cell = corruption / this.")]
        public int maxCorruption = 10;
        [Tooltip("Base range of the monster power a void cell would spawn.")]
        public int voidPowerMin = 3;
        public int voidPowerMax = 9;
        [Tooltip("The void power range shifts up by floor(corruption * this).")]
        public float voidPowerPerCorruption = 0.5f;

        [Header("Rewards")]
        [Tooltip("Gold gained for beating monsters = their summed power * this (only when the block's contest is won).")]
        public float goldPerMonsterPower = 1f;
        [Tooltip("Gold from a gold cell, and from a void cell that turned out to be a reward.")]
        public int goldCellReward = 2;
        [Tooltip("HP restored by a heal cell.")]
        public int healCellAmount = 2;

        [Header("Round")]
        [Tooltip("How many blocks the player may place in one round.")]
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

        [Header("Screen Transition")]
        [Tooltip("Seconds for each half of a screen transition (fade out, then fade in). 0 = switch screens at once.")]
        public float fadeSeconds = 0.3f;
        [Tooltip("Seconds to wait after a round ends before the fade starts, so its last result can be read.")]
        public float fadeDelaySeconds = 0.5f;

        [Header("Items")]
        public List<ItemData> items = new List<ItemData>
        {
            new ItemData { id = "power_ring", displayName = "Power Ring", attackBonus = 1, maxHpBonus = 0 },
            new ItemData { id = "life_charm", displayName = "Life Charm", attackBonus = 0, maxHpBonus = 5 },
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
                    new EventChoiceData { label = "Take the coins", effect = EventEffectKind.Gold, amount = 10 },
                    new EventChoiceData { label = "Rest by the fire", effect = EventEffectKind.Heal, amount = 5 },
                },
            },
        };

        [Header("Shop")]
        [Tooltip("The fixed shelf shown at every Shop stage.")]
        public List<ShopOfferData> shopOffers = new List<ShopOfferData>
        {
            new ShopOfferData { itemId = "power_ring", price = 60 },
            new ShopOfferData { itemId = "life_charm", price = 45 },
            new ShopOfferData { itemId = "", healAmount = 10, price = 40 },
        };

        [Header("Blocks")]
        public int handSize = 3;
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
            foreach (var data in events) list.Add(data.ToDefinition());
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

        public BlockSettings ToBlockSettings() => new BlockSettings
        {
            Size2Weight = blockWeight2,
            Size3Weight = blockWeight3,
            Size4Weight = blockWeight4,
        };
    }
}
