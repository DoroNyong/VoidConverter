using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;
using Grid = VoidConverter.Core.Grid;

namespace VoidConverter.Game
{
    public enum RunPhase
    {
        // A Combat or Boss stage: the grid round is being played.
        Playing,
        // The Event stage (placeholder screen) and the Shop stage wait for the player to press Continue.
        Event,
        Shop,
        Cleared,
        Defeated,
    }

    /// <summary>Owns the run: board, hand, player and the round loop. Views read from here and redraw when Changed fires.</summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        public GameConfig Config => config;
        public Grid Grid { get; private set; }
        public Hand Hand { get; private set; }
        public PlayerStats Player { get; private set; }
        public RunPhase Phase { get; private set; }

        /// <summary>The 1-based position in the run's stage list.</summary>
        public int Stage { get; private set; }
        public int StageCount => config.stages.Count;
        public StageType CurrentStage => config.stages[Stage - 1];

        /// <summary>The shelf of the current Shop stage (empty outside it).</summary>
        public IReadOnlyList<ShopOffer> ShopOffers => shopOffers;

        public int PlacementsUsed { get; private set; }
        public int MaxPlacements => config.maxPlacements;
        public int MinPlacementsToEnd => config.minPlacementsToEnd;
        public bool OutOfPlacements => PlacementsUsed >= MaxPlacements;

        /// <summary>The round can be ended by hand after enough blocks were placed.</summary>
        public bool CanEndRound => Phase == RunPhase.Playing && !IsBossStage && PlacementsUsed >= MinPlacementsToEnd;

        /// <summary>True while the boss stage's board is being played.</summary>
        public bool IsBossStage => Phase == RunPhase.Playing && CurrentStage == StageType.Boss;
        /// <summary>Blocks the player has to place in the boss stage before the boss can be defeated.</summary>
        public int BossHoldTarget => config.bossHoldPlacements;
        /// <summary>The boss can be defeated: enough blocks were placed (or nothing more fits).</summary>
        public bool BossReady => IsBossStage && !Player.IsDead &&
            (PlacementsUsed >= BossHoldTarget || OutOfPlacements || !Hand.AnyPlaceable(Grid.CanPlaceAnywhere));

        /// <summary>Summary of what the last action did, shown in the HUD.</summary>
        public string Message { get; private set; } = "";

        /// <summary>Raised after the board, hand, player or phase changed. Views must re-read state, never cache it.</summary>
        public event Action Changed;

        /// <summary>
        /// Plays a screen transition around a state change: it must call <paramref name="change"/> once while the
        /// screen is hidden (after <paramref name="delay"/> seconds and the fade out), then fade in and call
        /// <paramref name="finished"/>. Set by a view (ScreenFader); without one the changes happen at once.
        /// </summary>
        public delegate void TransitionRunner(Action change, float delay, Action finished);

        public TransitionRunner Transitioner { get; set; }

        /// <summary>True while a transition runs. Every player action is ignored until it ends.</summary>
        public bool InputLocked { get; private set; }

        /// <summary>The event of the current Event stage, or null outside it.</summary>
        public EventDefinition CurrentEvent { get; private set; }
        /// <summary>Index of the choice taken in the current event, or -1 while none was taken yet.</summary>
        public int EventChoiceTaken { get; private set; } = -1;
        public bool EventResolved => EventChoiceTaken >= 0;

        System.Random rng;
        List<ShopOffer> shopOffers = new List<ShopOffer>();
        List<EventDefinition> eventPool;
        readonly HashSet<string> seenEvents = new HashSet<string>();

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            rng = new System.Random();
            NewRun();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (InputLocked) return;

            // Debug: restart the whole run.
            if (keyboard.gKey.wasPressedThisFrame) NewRun();
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) PressMainButton();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug: 1 / 2 give the first two catalog items, Backspace takes back the last one.
            if (keyboard.digit1Key.wasPressedThisFrame && config.items.Count > 0) AddItem(config.items[0].id);
            if (keyboard.digit2Key.wasPressedThisFrame && config.items.Count > 1) AddItem(config.items[1].id);
            if (keyboard.backspaceKey.wasPressedThisFrame && Player.Items.Count > 0) RemoveItemAt(Player.Items.Count - 1);
#endif
        }

        /// <summary>The items the player carries, in the order they were taken.</summary>
        public IReadOnlyList<ItemDefinition> Items => Player.Items;

        /// <summary>Gives the player the catalog item with this id. Returns false if there is no such item.</summary>
        public bool AddItem(string itemId)
        {
            var item = config.FindItem(itemId);
            if (item == null) return false;
            Player.AddItem(item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Takes an item away (for events and boss patterns; the shop does not buy items back).</summary>
        public bool RemoveItemAt(int index)
        {
            if (!Player.RemoveItemAt(index)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Starts a fresh run: first stage, full HP, no gold, no corruption.</summary>
        public void NewRun()
        {
            Transition(() =>
            {
                Player = config.CreatePlayer();
                Stage = 1;
                Message = "";
                eventPool = config.CreateEvents();
                seenEvents.Clear();
                EnterStage();
            }, 0f);
        }

        /// <summary>
        /// Runs a state change behind the screen transition (if a view provides one) and then raises Changed.
        /// Input stays locked from now until the transition has faded back in.
        /// </summary>
        void Transition(Action change, float delay)
        {
            if (Transitioner == null || config.fadeSeconds <= 0f)
            {
                change();
                Changed?.Invoke();
                return;
            }

            InputLocked = true;
            Transitioner(() =>
            {
                change();
                Changed?.Invoke();
            }, delay, () => InputLocked = false);
        }

        /// <summary>Sets up the current stage: a grid round, the event placeholder or a fresh shop shelf.</summary>
        void EnterStage()
        {
            shopOffers = new List<ShopOffer>();
            CurrentEvent = null;
            EventChoiceTaken = -1;
            switch (CurrentStage)
            {
                case StageType.Event:
                    CurrentEvent = EventRules.Pick(eventPool, seenEvents, rng);
                    // With no events configured the stage is just a pause with a Continue button.
                    EventChoiceTaken = CurrentEvent == null ? 0 : -1;
                    Phase = RunPhase.Event;
                    break;
                case StageType.Shop:
                    Phase = RunPhase.Shop;
                    shopOffers = config.CreateShopOffers();
                    break;
                default:
                    Phase = RunPhase.Playing;
                    StartRound();
                    break;
            }
        }

        void StartRound()
        {
            Grid = new Grid(config.gridWidth, config.gridHeight);
            Grid.Generate(config.ToGridSettings(), rng);
            if (CurrentStage == StageType.Boss) Grid.PlaceStrongMonsters(config.bossStrongMonsterCount, config.bossStrongMonsterPower, rng);
            // The first void power comes from the current corruption, which carries over between rounds.
            Grid.RerollVoidPower(Player, config.ToVoidSettings(), rng);
            Hand = new Hand(config.handSize);
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            PlacementsUsed = 0;
        }

        /// <summary>
        /// The one button: ends the round by hand while playing, moves on from an event or the shop, or restarts
        /// after the run is over.
        /// </summary>
        public void PressMainButton()
        {
            if (InputLocked) return;
            if (Phase == RunPhase.Event || Phase == RunPhase.Shop)
            {
                // An event must be answered before moving on.
                if (Phase == RunPhase.Event && !EventResolved) return;
                AdvanceStage(0f, true);
                return;
            }
            if (Phase != RunPhase.Playing)
            {
                NewRun();
                return;
            }
            if (IsBossStage)
            {
                DefeatBoss();
                return;
            }
            if (!CanEndRound) return;

            Message = $"Stage {Stage} ended early";
            FinishRound();
            Changed?.Invoke();
        }

        /// <summary>Corruption fades, then the run moves on to the next stage.</summary>
        void FinishRound()
        {
            Player.ReduceCorruption(config.corruptionDecayPerRound);
            // A short pause first, so the result of the last block can be read before the screen fades.
            AdvanceStage(config.fadeDelaySeconds, false);
        }

        /// <summary>Enters the next stage, or clears the run after the last one.</summary>
        void AdvanceStage(float delay, bool clearMessage)
        {
            if (Stage >= StageCount)
            {
                Transition(() =>
                {
                    Phase = RunPhase.Cleared;
                    Message += (Message.Length > 0 ? "\n" : "") + $"CLEARED! Gold {Player.Gold}   (Space: restart)";
                }, delay);
                return;
            }

            Transition(() =>
            {
                if (clearMessage) Message = "";
                Stage++;
                EnterStage();
            }, delay);
        }

        /// <summary>
        /// Boss stage: defeats the boss once enough blocks were placed. The run is cleared behind a screen transition.
        /// Returns false if the boss cannot be defeated yet.
        /// </summary>
        public bool DefeatBoss()
        {
            if (InputLocked || !BossReady) return false;
            Message = "Boss defeated!";
            AdvanceStage(0f, false);
            return true;
        }

        /// <summary>Takes the choice at this position in the current event (once). Returns whether it was applied.</summary>
        public bool ChooseEventOption(int index)
        {
            if (InputLocked || Phase != RunPhase.Event || CurrentEvent == null || EventResolved) return false;
            if (index < 0 || index >= CurrentEvent.Choices.Count) return false;

            var choice = CurrentEvent.Choices[index];
            if (!choice.IsAvailable(Player))
            {
                Message = $"Cannot choose: {choice.Label}";
                Changed?.Invoke();
                return false;
            }

            string result = EventRules.Apply(Player, choice, rng);
            EventChoiceTaken = index;
            Message = $"{choice.Label}: {result}";
            Changed?.Invoke();
            return true;
        }

        /// <summary>Buys the offer at this shelf position during the Shop stage. Returns whether it was bought.</summary>
        public bool TryBuy(int index)
        {
            if (InputLocked || Phase != RunPhase.Shop || index < 0 || index >= shopOffers.Count) return false;

            var offer = shopOffers[index];
            var result = Shop.Buy(Player, offer);
            Message = result switch
            {
                Shop.BuyResult.Bought => $"Bought {offer.Name} ({offer.Summary}) for {offer.Price}",
                Shop.BuyResult.NotEnoughGold => $"Not enough gold for {offer.Name} ({offer.Price})",
                Shop.BuyResult.FullHealth => "HP is already full",
                _ => "Already sold",
            };
            Changed?.Invoke();
            return result == Shop.BuyResult.Bought;
        }

        /// <summary>
        /// Places the selected block with its lower-left corner at the given cell. In order:
        /// void cells decide monster or reward (using the corruption from before this block), every monster is
        /// summed into one contest, corruption rises, then if the player survived: heal cells heal, gold cells
        /// and void rewards pay gold, and a won contest pays gold for the beaten monsters. Finally the shared void
        /// power is re-rolled and the round may end.
        /// </summary>
        public bool TryPlace(int originX, int originY)
        {
            if (InputLocked || Phase != RunPhase.Playing || OutOfPlacements) return false;

            var cells = Hand.SelectedCells();
            if (cells == null) return false;

            var result = Grid.Place(cells, originX, originY);
            if (result == null) return false;

            var voidSettings = config.ToVoidSettings();
            float monsterChance = VoidRules.MonsterChance(Player);

            int totalPower = 0, monsters = 0, goldCells = 0, healCells = 0, voidCells = 0, voidMonsters = 0, voidRewards = 0;
            foreach (var cell in result.Activated)
            {
                switch (cell.Type)
                {
                    case CellType.Monster:
                        monsters++;
                        totalPower += cell.Power;
                        break;
                    case CellType.Gold:
                        goldCells++;
                        break;
                    case CellType.Heal:
                        healCells++;
                        break;
                    case CellType.Void:
                        voidCells++;
                        if (rng.NextDouble() < monsterChance)
                        {
                            voidMonsters++;
                            totalPower += cell.Power;
                        }
                        else
                        {
                            voidRewards++;
                        }
                        break;
                }
            }

            Player.AddCorruption(voidCells);

            int monstersTotal = monsters + voidMonsters;
            FightResult? fight = monstersTotal > 0 ? Combat.Fight(Player, totalPower, rng) : (FightResult?)null;

            int heal = 0, rewardGold = 0, killGold = 0;
            if (!Player.IsDead)
            {
                int before = Player.Hp;
                Player.Heal(healCells * config.healCellAmount);
                heal = Player.Hp - before;

                rewardGold = (goldCells + voidRewards) * config.goldCellReward;
                if (fight.HasValue && fight.Value.Won) killGold = Mathf.RoundToInt(totalPower * config.goldPerMonsterPower);
                Player.AddGold(rewardGold + killGold);
            }

            Grid.RerollVoidPower(Player, voidSettings, rng);
            // The longer the player takes to hold out in the boss stage, the stronger the monsters left on the board.
            if (CurrentStage == StageType.Boss && !Player.IsDead) Grid.GrowMonsters(config.bossMonsterGrowth);

            PlacementsUsed++;
            Hand.UseSelected();
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Message = Describe(fight, monstersTotal, voidCells, voidMonsters, voidRewards, heal, rewardGold, killGold);

            // The round ends when the player falls, the placements run out, or nothing in the hand fits any more.
            if (Player.IsDead)
            {
                Phase = RunPhase.Defeated;
                Message += $"\nDEFEATED in stage {Stage}/{StageCount}   (Space: restart)";
            }
            else if (CurrentStage == StageType.Boss)
            {
                // The boss stage has no round end: it ends when the player defeats the boss.
                if (BossReady) Message += "\nThe boss is open! Defeat it.";
            }
            else if (OutOfPlacements || !Hand.AnyPlaceable(Grid.CanPlaceAnywhere))
            {
                Message += OutOfPlacements ? $"\nStage {Stage} complete" : $"\nStage {Stage} complete (no room left)";
                FinishRound();
            }

            Changed?.Invoke();
            return true;
        }

        public void SelectSlot(int index)
        {
            if (!InputLocked && Hand.Select(index)) Changed?.Invoke();
        }

        public void RotateSlot(int index)
        {
            if (InputLocked || index < 0 || index >= Hand.Size || Hand.Slots[index] == null) return;
            Hand.Rotate(index);
            Changed?.Invoke();
        }

        /// <summary>Puts the held block back after a drop that did not place it.</summary>
        public void ClearSelection()
        {
            if (Hand.Selected < 0) return;
            Hand.Deselect();
            Changed?.Invoke();
        }

        string Describe(FightResult? fight, int monsters, int voidCells, int voidMonsters, int voidRewards,
            int heal, int rewardGold, int killGold)
        {
            var lines = new List<string>();

            if (fight.HasValue)
            {
                var f = fight.Value;
                string outcome = f.Won ? "WIN" : $"LOSE -{f.Damage}";
                lines.Add($"ATK {f.Roll} vs {f.MonsterPower} ({monsters} monster{(monsters > 1 ? "s" : "")}): {outcome}");
            }
            else
            {
                lines.Add("No fight");
            }

            var extras = new List<string>();
            if (voidCells > 0) extras.Add($"Void: {voidMonsters} monster, {voidRewards} reward");
            if (heal > 0) extras.Add($"Heal +{heal}");
            if (rewardGold + killGold > 0) extras.Add($"Gold +{rewardGold + killGold}");
            if (extras.Count > 0) lines.Add(string.Join("   ", extras));

            return string.Join("\n", lines);
        }
    }
}
