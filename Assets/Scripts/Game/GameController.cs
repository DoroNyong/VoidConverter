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
        Playing,
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

        public int Round { get; private set; }
        public int MaxRounds => config.maxRounds;

        public int PlacementsUsed { get; private set; }
        public int MaxPlacements => config.maxPlacements;
        public int MinPlacementsToEnd => config.minPlacementsToEnd;
        public bool OutOfPlacements => PlacementsUsed >= MaxPlacements;

        /// <summary>The round can be ended by hand after enough blocks were placed.</summary>
        public bool CanEndRound => Phase == RunPhase.Playing && PlacementsUsed >= MinPlacementsToEnd;

        /// <summary>Summary of what the last action did, shown in the HUD.</summary>
        public string Message { get; private set; } = "";

        /// <summary>Raised after the board, hand, player or phase changed. Views must re-read state, never cache it.</summary>
        public event Action Changed;

        System.Random rng;

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

            // Debug: restart the whole run.
            if (keyboard.gKey.wasPressedThisFrame) NewRun();
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) PressMainButton();
        }

        /// <summary>Starts a fresh run: round 1, full HP, no gold, no corruption.</summary>
        public void NewRun()
        {
            Player = config.CreatePlayer();
            Round = 1;
            Phase = RunPhase.Playing;
            Message = "";
            StartRound();
            Changed?.Invoke();
        }

        void StartRound()
        {
            Grid = new Grid(config.gridWidth, config.gridHeight);
            Grid.Generate(config.ToGridSettings(), rng);
            // The first void power comes from the current corruption, which carries over between rounds.
            Grid.RerollVoidPower(Player, config.ToVoidSettings(), rng);
            Hand = new Hand(config.handSize);
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            PlacementsUsed = 0;
        }

        /// <summary>The one button: ends the round by hand while playing, or restarts after the run is over.</summary>
        public void PressMainButton()
        {
            if (Phase != RunPhase.Playing)
            {
                NewRun();
                return;
            }
            if (!CanEndRound) return;

            Message = $"Round {Round} ended early";
            FinishRound();
            Changed?.Invoke();
        }

        /// <summary>Corruption fades, then the next round starts, or the run is cleared after the last one.</summary>
        void FinishRound()
        {
            Player.ReduceCorruption(config.corruptionDecayPerRound);

            if (Round >= MaxRounds)
            {
                Phase = RunPhase.Cleared;
                Message += (Message.Length > 0 ? "\n" : "") + $"CLEARED! Gold {Player.Gold}   (Space: restart)";
                return;
            }

            Round++;
            StartRound();
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
            if (Phase != RunPhase.Playing || OutOfPlacements) return false;

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

            PlacementsUsed++;
            Hand.UseSelected();
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Message = Describe(fight, monstersTotal, voidCells, voidMonsters, voidRewards, heal, rewardGold, killGold);

            // The round ends when the player falls, the placements run out, or nothing in the hand fits any more.
            if (Player.IsDead)
            {
                Phase = RunPhase.Defeated;
                Message += $"\nDEFEATED in round {Round}/{MaxRounds}   (Space: restart)";
            }
            else if (OutOfPlacements || !Hand.AnyPlaceable(Grid.CanPlaceAnywhere))
            {
                Message += OutOfPlacements ? $"\nRound {Round} complete" : $"\nRound {Round} complete (no room left)";
                FinishRound();
            }

            Changed?.Invoke();
            return true;
        }

        public void SelectSlot(int index)
        {
            if (Hand.Select(index)) Changed?.Invoke();
        }

        public void RotateSlot(int index)
        {
            if (index < 0 || index >= Hand.Size || Hand.Slots[index] == null) return;
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
