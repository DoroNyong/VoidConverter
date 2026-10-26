using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;

namespace VoidConverter.View
{
    /// <summary>Shows the three offered blocks below the grid. Click to select, R / right click to rotate.</summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        static readonly Color SlotColor = new Color(0.12f, 0.17f, 0.28f);
        static readonly Color SlotSelectedColor = new Color(0.25f, 0.4f, 0.65f);
        static readonly Color BlockColor = new Color(0.9f, 0.85f, 0.6f);

        System.Random rng;
        Hand hand;
        Camera cam;

        void Start()
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            cam = Camera.main;
            rng = new System.Random();
            hand = new Hand(config.handSize);
            hand.Fill(config.ToBlockSettings(), rng);
            Rebuild();
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool changed = false;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                int slot = SlotAt(cam.ScreenToWorldPoint(mouse.position.ReadValue()));
                if (slot >= 0 && hand.Select(slot)) changed = true;
            }

            bool rotatePressed = (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                              || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (rotatePressed && hand.Selected >= 0)
            {
                hand.Rotate();
                changed = true;
            }

            // Debug: consume the selected block and refill, to check the refill rule. Replaced by real placement later.
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && hand.Selected >= 0)
            {
                hand.UseSelected();
                hand.Fill(config.ToBlockSettings(), rng);
                changed = true;
            }

            if (changed) Rebuild();
        }

        Vector2 SlotCenter(int index)
        {
            float step = config.cellSize + config.cellGap;
            float gridHalfHeight = config.gridHeight * step * 0.5f;
            float pitch = config.handSlotSize + config.handSlotGap;
            float x = (index - (hand.Size - 1) * 0.5f) * pitch;
            float y = -(gridHalfHeight + 0.6f + config.handSlotSize * 0.5f);
            return new Vector2(x, y);
        }

        int SlotAt(Vector3 world)
        {
            float half = config.handSlotSize * 0.5f;
            for (int i = 0; i < hand.Size; i++)
            {
                var c = SlotCenter(i);
                if (Mathf.Abs(world.x - c.x) <= half && Mathf.Abs(world.y - c.y) <= half) return i;
            }
            return -1;
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            for (int i = 0; i < hand.Size; i++)
            {
                Vector2 center = SlotCenter(i);
                SpriteUtil.AddSquare(transform, $"Slot {i}", center, config.handSlotSize,
                    i == hand.Selected ? SlotSelectedColor : SlotColor, -1);

                var cells = hand.CellsOf(i);
                if (cells == null) continue;

                int maxX = 0, maxY = 0;
                foreach (var c in cells)
                {
                    maxX = Mathf.Max(maxX, c.X);
                    maxY = Mathf.Max(maxY, c.Y);
                }
                float mini = config.handMiniCellSize;
                Vector2 offset = center - new Vector2(maxX * mini * 0.5f, maxY * mini * 0.5f);
                foreach (var c in cells)
                {
                    SpriteUtil.AddSquare(transform, "Block Cell", offset + new Vector2(c.X * mini, c.Y * mini),
                        mini * 0.92f, BlockColor, 1);
                }
            }
        }
    }
}
