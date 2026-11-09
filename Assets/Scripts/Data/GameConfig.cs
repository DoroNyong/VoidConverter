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
        [Tooltip("Added to the player's Magic attack.")]
        public int magicBonus;

        public ItemDefinition ToDefinition() => new ItemDefinition(id, displayName, attackBonus, maxHpBonus, magicBonus);
    }

    /// <summary>One kind of monster: beaten when the player's Attack (physical) or Magic is at least its power.</summary>
    [Serializable]
    public class MonsterTypeData
    {
        public string name;
        [Tooltip("On: a magic monster (beaten by Magic). Off: a physical one (beaten by Attack).")]
        public bool magic;
        [Tooltip("The attack / magic needed to beat it. A lower value costs HP equal to the difference.")]
        public int power = 3;
        [Tooltip("How often it shows up among the monsters that may appear.")]
        public float weight = 1f;
        [Tooltip("The first stage (1-based position in the run) in which it can appear.")]
        public int firstStage = 1;
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
        [Tooltip("TileWeight only: the kind of cell made likelier / rarer. Amount is the percent (30 = +30%, -30 = -30%, compounding).")]
        public CellType cellType;
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
                    effects.Add(new EventEffect(e.kind, e.amount, item, e.cellType));
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
        [Tooltip("Height of one diamond tile (its top-to-bottom diagonal).")]
        public float cellSize = 1f;
        public float cellGap = 0.06f;
        [Tooltip("Width of a diamond tile divided by its height. 1 = a square turned 45 degrees, 1.7 = wide and low so the board fills a wide screen.")]
        public float tileAspect = 1.7f;

        // The board is drawn as a diamond: the cell (x, y) sits at ((x - y) * StepX / 2, (x + y) * StepY / 2)
        // from the board center, so x runs to the upper right and y to the upper left.
        /// <summary>Height / width of one diagonal step between neighbouring cells (their centers are StepX/2 apart sideways, StepY/2 up).</summary>
        public float StepY => cellSize + cellGap;
        public float StepX => StepY * tileAspect;
        public float TileWidth => cellSize * tileAspect;
        /// <summary>Half the width of the whole board, vertex to vertex.</summary>
        public float GridHalfWidth => (gridWidth + gridHeight - 2) * StepX * 0.25f + TileWidth * 0.5f;
        /// <summary>Half the height of the whole board, vertex to vertex.</summary>
        public float GridHalfHeight => (gridWidth + gridHeight - 2) * StepY * 0.25f + cellSize * 0.5f;

        /// <summary>Screen offset (relative to the board center) of a cell given in fractional cell coordinates.</summary>
        public Vector2 CellOffset(float x, float y)
        {
            float u = x - (gridWidth - 1) * 0.5f;
            float v = y - (gridHeight - 1) * 0.5f;
            return new Vector2((u - v) * StepX * 0.5f, (u + v) * StepY * 0.5f);
        }

        /// <summary>The reverse of <see cref="CellOffset"/>: fractional cell coordinates (cell centers on whole numbers).</summary>
        public Vector2 OffsetToCell(Vector2 offset)
        {
            float diff = offset.x * 2f / StepX;
            float sum = offset.y * 2f / StepY;
            return new Vector2((sum + diff) * 0.5f + (gridWidth - 1) * 0.5f, (sum - diff) * 0.5f + (gridHeight - 1) * 0.5f);
        }

        [Header("Cell Counts")]
        [Tooltip("Exactly this many void cells per board.")]
        public int voidCount = 0;
        [Tooltip("Every board has at least this many monster cells (used when Monster Count is 0).")]
        public int monsterMin = 10;
        [Tooltip("Above 0 = the Pick-One layout: traps are exact, gold and heal are drawn from their min..max, and every other cell is a monster (the number itself is not used). 0 = the older rule (Monster Min plus a weighted share of the leftover cells).")]
        public int monsterCount = 12;
        [Tooltip("Pick-One rule: fewest trap cells per board (a trap only costs HP when it fires).")]
        public int trapCount = 4;
        [Tooltip("Pick-One rule: most trap cells per board.")]
        public int trapMax = 5;
        [Tooltip("Every board has at least this many gold cells.")]
        public int goldMin = 3;
        [Tooltip("Pick-One: most gold cells per board.")]
        public int goldMax = 4;
        [Tooltip("Every board has at least this many heal cells.")]
        public int healMin = 2;
        [Tooltip("Pick-One: most heal cells per board.")]
        public int healMax = 3;
        [Tooltip("Gold and heal cells together never exceed this per board (0 = no cap). Cells over the cap become monsters.")]
        public int rewardMax = 9;
        [Tooltip("The cells left after the void count and the minimums are split among monster/gold/heal by these weights.")]
        public float monsterWeight = 5f;
        public float goldWeight = 3f;
        public float healWeight = 1f;

        [Header("Cell Spacing")]
        [Tooltip("Void cells keep this Chebyshev distance from each other. 2 = not even diagonally touching. 1 = no rule.")]
        public int voidMinSpacing = 2;
        [Tooltip("Gold/heal cells keep this Chebyshev distance from each other. 2 = not even diagonally touching. 1 = no rule.")]
        public int rewardMinSpacing = 1;
        [Tooltip("Above 0: no group of edge-connected cells of one kind (monster, trap, gold, heal) is larger than this, and traps never touch heal cells. Replaces the monster cluster cap. 0 = off.")]
        public int maxCluster = 2;
        [Tooltip("Same limit for monster cells only, since they fill most of the board. 0 = use Max Cluster.")]
        public int maxMonsterCluster = 3;
        [Tooltip("The largest group of edge-connected monster cells may not exceed this. 0 = no cap.")]
        public int monsterMaxCluster = 8;

        [Header("Monsters")]
        public int monsterPowerMin = 1;
        public int monsterPowerMax = 6;

        [Header("Player")]
        public int playerMaxHp = 6;
        [Tooltip("The player's attack: one fixed number (no roll). A monster is beaten when the attack is at least its power.")]
        public int playerAttack = 2;

        [Tooltip("The player's Magic attack: the number that must reach a magic monster's power. Items add to it.")]
        public int playerMagic = 1;

        [Header("Monster Types (Pick-One rule)")]
        [Tooltip("Monster cells take one of these types (by weight) among those whose First Stage has been reached. Empty = random physical power between Monster Power Min and Max.")]
        public List<MonsterTypeData> monsterTypes = new List<MonsterTypeData>
        {
            new MonsterTypeData { name = "Slime", magic = true, power = 1, weight = 1f, firstStage = 1 },
            new MonsterTypeData { name = "Beast", magic = false, power = 3, weight = 1f, firstStage = 1 },
            new MonsterTypeData { name = "Wisp", magic = true, power = 2, weight = 1f, firstStage = 3 },
            new MonsterTypeData { name = "Brute", magic = false, power = 4, weight = 1f, firstStage = 3 },
            new MonsterTypeData { name = "Mage", magic = true, power = 3, weight = 1f, firstStage = 5 },
            new MonsterTypeData { name = "Ogre", magic = false, power = 5, weight = 1f, firstStage = 5 },
        };

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
        public int voidBuffCount = 0;
        public int voidSpreadCount = 0;
        public int voidDangerCount = 0;
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
        public int goldCellReward = 2;
        [Tooltip("HP restored by a heal cell.")]
        public int healCellAmount = 1;

        [Header("Clear Rule")]
        [Tooltip("On: a Combat round ends when every monster cell is covered (a lost fight still removes the monster), blocks may overlap spent cells, and Max Placements / manual end do not apply. Off: the older round rules below.")]
        public bool clearAllRule = false;
        [Tooltip("Clear Rule: the blocks placed in one round before overtime starts.")]
        public int clearTurnLimit = 5;
        [Tooltip("Clear Rule: after every block placed at or past the turn limit, each monster left gets this much stronger.")]
        public int overtimeGrowth = 1;

        [Header("Pick-One Rule (v0.3)")]
        [Tooltip("On: a block lets only ONE random covered fresh cell fire (all cells are shown, the odds are shown before placing). A cell a block covered cannot take another block. A round has no gold goal: place the minimum number of blocks to be allowed to leave, or keep going for more gold at the cost of HP. Replaces the Clear Rule and the older round rules.")]
        public bool pickOneRule = true;
        [Tooltip("Pick-One: blocks that must be placed in a Combat round before the player may leave it.")]
        public int pickMinPlacements = 4;
        [Tooltip("Pick-One: blocks that must be placed in the Boss round before it can be defeated.")]
        public int bossMinPlacements = 6;
        [Tooltip("Pick-One: blocks that may be placed in one round at most (the round ends by itself after this many).")]
        public int goalTurns = 7;
        [Tooltip("Pick-One: HP lost when a trap fires.")]
        public int trapDamage = 1;
        [Tooltip("Pick-One: how many times per round the player may throw away the block in hand and draw a new one.")]
        public int rerollsPerRound = 2;
        [Tooltip("Pick-One: a cell a block covered cannot take another block (the covered cells that did not fire are lost). The round also ends when no block in the hand fits any more. Off: blocks may overlap spent cells.")]
        public bool coveredCellsBlock = true;

        [Header("Round")]
        [Tooltip("How many blocks the player may place in one round (older rule, ignored by the Clear Rule).")]
        public int maxPlacements = 6;
        [Tooltip("The run is cleared after the last stage. Combat and Boss stages play a grid round.")]
        public List<StageType> stages = new List<StageType>
        {
            // One cycle is two short fights and an event, three times, then the shop and the boss.
            StageType.Combat, StageType.Combat, StageType.Event,
            StageType.Combat, StageType.Combat, StageType.Event,
            StageType.Combat, StageType.Combat, StageType.Event,
            StageType.Shop, StageType.Boss,
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
            new ItemData { id = "arcane_ring", displayName = "Arcane Ring", magicBonus = 1 },
        };

        [Header("Tile Weights")]
        [Tooltip("Pick-One: no kind of cell may become likelier than this share of a block's pick because of event tile weights (a kind that is already above it without weights stays as it is). 0.75 = 75%.")]
        [Range(0.5f, 1f)] public float maxTileChance = 0.75f;

        [Header("Events")]
        [Tooltip("The pool the Event stages draw from, without repeats until every event was seen.")]
        public List<EventData> events = new List<EventData>
        {
            new EventData
            {
                id = "lucky_shrine",
                title = "Lucky Shrine",
                description = "A small shrine glows faintly.\nIts blessing bends luck your way.",
                choices = new List<EventChoiceData>
                {
                    Choice("Pray for health", TileWeight(CellType.Heal, 30)),
                    Choice("Pray for fortune", TileWeight(CellType.Gold, 30)),
                    Choice("Pray for safety", TileWeight(CellType.Trap, -30)),
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
            new EventData
            {
                id = "whetstone_rune",
                title = "Whetstone & Rune Stone",
                description = "Two tools lie on a stump.\nYou can take only one.",
                choices = new List<EventChoiceData>
                {
                    Choice("Sharpen your blade", Effect(EventEffectKind.Attack, 1)),
                    Choice("Carve the rune", Effect(EventEffectKind.Magic, 1)),
                    Choice("Walk away"),
                },
            },
            new EventData
            {
                id = "twin_altar",
                title = "Twin Altar",
                description = "Two altars, blade and flame.\nOne gives, the other takes.",
                choices = new List<EventChoiceData>
                {
                    Choice("Kneel at the blade", Effect(EventEffectKind.Attack, 2), Effect(EventEffectKind.Magic, -1)),
                    Choice("Kneel at the flame", Effect(EventEffectKind.Magic, 2), Effect(EventEffectKind.Attack, -1)),
                    Choice("Leave"),
                },
            },
        };

        static EventEffectData Effect(EventEffectKind kind, int amount) => new EventEffectData { kind = kind, amount = amount };

        static EventEffectData TileWeight(CellType cell, int percent) =>
            new EventEffectData { kind = EventEffectKind.TileWeight, amount = percent, cellType = cell };

        static EventEffectData GainItem(string itemId) => new EventEffectData { kind = EventEffectKind.GainItem, itemId = itemId };

        static EventChoiceData Choice(string label, params EventEffectData[] effects) =>
            new EventChoiceData { label = label, effects = new List<EventEffectData>(effects) };

        [Header("Shop")]
        [Tooltip("The fixed shelf shown at every Shop stage.")]
        public List<ShopOfferData> shopOffers = new List<ShopOfferData>
        {
            new ShopOfferData { itemId = "power_ring", price = 20 },
            new ShopOfferData { itemId = "arcane_ring", price = 20 },
            new ShopOfferData { itemId = "life_charm", price = 15 },
            new ShopOfferData { itemId = "", healAmount = 4, price = 14 },
        };

        [Header("Blocks")]
        public int handSize = 1;
        [Tooltip("On: slot i always holds a shape with Fixed Hand Sizes[i] cells (a used slot refills with the same size). Off: the weights below decide.")]
        public bool fixedHand = false;
        public List<int> fixedHandSizes = new List<int> { 2, 3, 4 };
        [Tooltip("Attack added to the roll of a block with this many cells (a bigger block hits harder).")]
        public int attackBonus2 = 0;
        public int attackBonus3 = 0;
        public int attackBonus4 = 0;
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

        /// <summary>The monster types that can appear at this stage (1-based; 0 = all).</summary>
        MonsterSpec[] MonsterSpecsFor(int stage)
        {
            var specs = new List<MonsterSpec>();
            if (monsterTypes != null)
            {
                foreach (var t in monsterTypes)
                {
                    if (stage > 0 && t.firstStage > stage) continue;
                    specs.Add(new MonsterSpec { Magic = t.magic, Power = t.power, Weight = t.weight });
                }
            }
            return specs.Count > 0 ? specs.ToArray() : null;
        }

        public GridSettings ToGridSettings(int stage = 0) => new GridSettings
        {
            MonsterSpecs = pickOneRule ? MonsterSpecsFor(stage) : null,
            VoidCount = voidCount,
            MonsterMin = monsterMin,
            MonsterCount = monsterCount,
            TrapCount = trapCount,
            TrapMax = trapMax,
            GoldMin = goldMin,
            HealMin = healMin,
            GoldMax = goldMax,
            HealMax = healMax,
            RewardMax = rewardMax,
            MonsterWeight = monsterWeight,
            GoldWeight = goldWeight,
            HealWeight = healWeight,
            VoidMinSpacing = voidMinSpacing,
            RewardMinSpacing = rewardMinSpacing,
            MonsterMaxCluster = monsterMaxCluster,
            MaxCluster = maxCluster,
            MaxMonsterCluster = maxMonsterCluster,
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

        public PlayerStats CreatePlayer() =>
            new PlayerStats(playerMaxHp, playerAttack, playerAttack, maxCorruption, playerMagic) { MaxTileChance = maxTileChance };

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
