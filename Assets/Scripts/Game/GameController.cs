using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;
using Grid = VoidConverter.Core.Grid;

namespace VoidConverter.Game
{
    /// <summary>Owns the board, the hand and the player. Views read from here and redraw when Changed fires.</summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        public GameConfig Config => config;
        public Grid Grid { get; private set; }
        public Hand Hand { get; private set; }
        public PlayerStats Player { get; private set; }

        public int PlacementsUsed { get; private set; }
        public int MaxPlacements => config.maxPlacements;
        public bool OutOfPlacements => PlacementsUsed >= MaxPlacements;

        /// <summary>Summary of what the last placement did, shown in the HUD.</summary>
        public string Message { get; private set; } = "";

        /// <summary>Raised after the board, hand or player changed. Views must re-read state, never cache it.</summary>
        public event Action Changed;

        System.Random rng;

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            rng = new System.Random();
            NewBoard();
        }

        void Update()
        {
            // Debug: regenerate the board and hand, and restore the player.
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) NewBoard();
        }

        public void NewBoard()
        {
            Grid = new Grid(config.gridWidth, config.gridHeight);
            Grid.Generate(config.ToGridSettings(), rng);
            Hand = new Hand(config.handSize);
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Player = config.CreatePlayer();
            PlacementsUsed = 0;
            Message = "";
            Changed?.Invoke();
        }

        /// <summary>
        /// Places the selected block with its lower-left corner at the given cell. In order:
        /// void cells decide monster or reward (using the corruption from before this block), every monster is
        /// summed into one contest, corruption rises, rewards heal if the player survived, and the shared void
        /// power is re-rolled.
        /// </summary>
        public bool TryPlace(int originX, int originY)
        {
            if (Player.IsDead || OutOfPlacements) return false;

            var cells = Hand.SelectedCells();
            if (cells == null) return false;

            var result = Grid.Place(cells, originX, originY);
            if (result == null) return false;

            var voidSettings = config.ToVoidSettings();
            float monsterChance = VoidRules.MonsterChance(Player);

            int totalPower = 0, monsters = 0, rewards = 0, voidCells = 0, voidMonsters = 0, voidRewards = 0;
            foreach (var cell in result.Activated)
            {
                switch (cell.Type)
                {
                    case CellType.Monster:
                        monsters++;
                        totalPower += cell.Power;
                        break;
                    case CellType.Reward:
                        rewards++;
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

            int heal = 0;
            if (!Player.IsDead)
            {
                int before = Player.Hp;
                Player.Heal((rewards + voidRewards) * voidSettings.RewardHeal);
                heal = Player.Hp - before;
            }

            Grid.RerollVoidPower(Player, voidSettings, rng);

            PlacementsUsed++;
            Hand.UseSelected();
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Message = Describe(fight, monstersTotal, voidCells, voidMonsters, voidRewards, heal);
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

        string Describe(FightResult? fight, int monsters, int voidCells, int voidMonsters, int voidRewards, int heal)
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
            if (extras.Count > 0) lines.Add(string.Join("   ", extras));

            if (Player.IsDead) lines.Add("DEFEATED (G: restart)");
            else if (OutOfPlacements) lines.Add("No placements left (G: restart)");
            return string.Join("\n", lines);
        }
    }
}
