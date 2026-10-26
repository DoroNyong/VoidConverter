using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;
using Grid = VoidConverter.Core.Grid;

namespace VoidConverter.Game
{
    /// <summary>Owns the board and the hand. Views read from here and redraw when Changed fires.</summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        public GameConfig Config => config;
        public Grid Grid { get; private set; }
        public Hand Hand { get; private set; }

        /// <summary>Raised after the board or hand changed. Views must re-read Grid and Hand, never cache them.</summary>
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
            // Debug: regenerate the board and hand.
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) NewBoard();
        }

        public void NewBoard()
        {
            Grid = new Grid(config.gridWidth, config.gridHeight);
            Grid.Generate(config.ToGridSettings(), rng);
            Hand = new Hand(config.handSize);
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Changed?.Invoke();
        }

        /// <summary>Places the selected block with its lower-left corner at the given cell.</summary>
        public bool TryPlace(int originX, int originY)
        {
            var cells = Hand.SelectedCells();
            if (cells == null) return false;

            var result = Grid.Place(cells, originX, originY);
            if (result == null) return false;

            Hand.UseSelected();
            Hand.Fill(config.ToBlockSettings(), rng, Grid.CanPlaceAnywhere);
            Log(result);
            Changed?.Invoke();
            return true;
        }

        public void SelectSlot(int index)
        {
            if (Hand.Select(index)) Changed?.Invoke();
        }

        public void RotateSelected()
        {
            if (Hand.Selected < 0) return;
            Hand.Rotate();
            Changed?.Invoke();
        }

        static void Log(PlacementResult result)
        {
            int monsters = 0, rewards = 0, voids = 0;
            foreach (var a in result.Activated)
            {
                if (a.Type == CellType.Monster) monsters++;
                else if (a.Type == CellType.Reward) rewards++;
                else voids++;
            }
            Debug.Log($"Placed block: monster x{monsters}, reward x{rewards}, void x{voids}");
        }
    }
}
