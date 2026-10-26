using System;
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

        /// <summary>One-line summary of what the last placement did, shown in the HUD.</summary>
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

        /// <summary>Places the selected block with its lower-left corner at the given cell.</summary>
        public bool TryPlace(int originX, int originY)
        {
            if (Player.IsDead || OutOfPlacements) return false;

            var cells = Hand.SelectedCells();
            if (cells == null) return false;

            var result = Grid.Place(cells, originX, originY);
            if (result == null) return false;

            // Every monster the block covers is summed into one contest against the player's rolled attack.
            int monsters = 0, totalPower = 0;
            foreach (var activated in result.Activated)
            {
                if (activated.Type != CellType.Monster) continue;
                monsters++;
                totalPower += activated.Power;
            }
            FightResult? fight = monsters > 0 ? Combat.Fight(Player, totalPower, rng) : (FightResult?)null;

            PlacementsUsed++;
            Hand.UseSelected();
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Message = Describe(result, fight, monsters);
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

        string Describe(PlacementResult result, FightResult? fight, int monsters)
        {
            string text;
            if (fight.HasValue)
            {
                var f = fight.Value;
                string outcome = f.Won ? "WIN" : $"LOSE -{f.Damage}";
                text = $"ATK {f.Roll} vs {f.MonsterPower} ({monsters} monster{(monsters > 1 ? "s" : "")}): {outcome}";
            }
            else
            {
                int rewards = 0, voids = 0;
                foreach (var a in result.Activated)
                {
                    if (a.Type == CellType.Reward) rewards++;
                    else if (a.Type == CellType.Void) voids++;
                }
                text = $"Activated: reward x{rewards}, void x{voids}";
            }

            if (Player.IsDead) text += "\nDEFEATED (G: restart)";
            else if (OutOfPlacements) text += "\nNo placements left (G: restart)";
            return text;
        }
    }
}
