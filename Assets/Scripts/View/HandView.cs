using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// Shows the offered blocks below the grid. Press a slot to pick the block up, drag it over the grid
    /// (a ghost shows where it would land), release to place. R or right click rotates the picked block.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        static readonly Color SlotColor = new Color(0.12f, 0.17f, 0.28f);
        static readonly Color SlotSelectedColor = new Color(0.25f, 0.4f, 0.65f);
        static readonly Color BlockColor = new Color(0.9f, 0.85f, 0.6f);
        static readonly Color GhostValidColor = new Color(0.95f, 0.9f, 0.4f, 0.65f);
        static readonly Color GhostInvalidColor = new Color(1f, 0.3f, 0.3f, 0.55f);
        const int MaxBlockCells = 4;

        GameController controller;
        GameConfig config;
        GridView gridView;
        Camera cam;

        bool dragging;
        int hoveredSlot = -1;
        Transform ghostRoot;
        SpriteRenderer[] ghostCells;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            gridView = FindFirstObjectByType<GridView>();
            cam = Camera.main;

            var ghost = new GameObject("Ghost");
            ghostRoot = ghost.transform;
            ghostCells = new SpriteRenderer[MaxBlockCells];
            for (int i = 0; i < MaxBlockCells; i++)
            {
                ghostCells[i] = SpriteUtil.AddSquare(ghostRoot, "Ghost Cell", Vector2.zero, config.cellSize * 0.92f, GhostValidColor, 5);
            }
            HideGhost();

            controller.Changed += Rebuild;
            Rebuild();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Rebuild;
            if (ghostRoot != null) Destroy(ghostRoot.gameObject);
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null) return;

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());

            // The block under the cursor is the one that rotates; while one is held, the held block is.
            int hover = dragging ? -1 : SlotAt(world);
            if (hover != hoveredSlot)
            {
                hoveredSlot = hover;
                Rebuild();
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                int slot = SlotAt(world);
                if (slot >= 0 && controller.Hand.Select(slot))
                {
                    dragging = true;
                    hoveredSlot = -1;
                    Rebuild();
                }
            }

            bool rotatePressed = (keyboard != null && keyboard.rKey.wasPressedThisFrame) || mouse.rightButton.wasPressedThisFrame;
            if (rotatePressed) controller.RotateSlot(dragging ? controller.Hand.Selected : hoveredSlot);

            if (!dragging) return;

            UpdateGhost(world);

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                HideGhost();
                var origin = DropOrigin(world);
                controller.TryPlace(origin.X, origin.Y);
                // A drop that did not place the block puts it back; it is no longer "held".
                controller.ClearSelection();
                hoveredSlot = SlotAt(world);
                Rebuild();
            }
        }

        /// <summary>Lower-left cell of the block when its center is held at the cursor.</summary>
        Int2 DropOrigin(Vector3 world)
        {
            var cells = controller.Hand.SelectedCells();
            if (cells == null) return new Int2(0, 0);

            int maxX = 0, maxY = 0;
            foreach (var c in cells)
            {
                maxX = Mathf.Max(maxX, c.X);
                maxY = Mathf.Max(maxY, c.Y);
            }
            Vector2 g = gridView.WorldToGrid(world);
            return new Int2(Mathf.RoundToInt(g.x - maxX * 0.5f), Mathf.RoundToInt(g.y - maxY * 0.5f));
        }

        void UpdateGhost(Vector3 world)
        {
            var cells = controller.Hand.SelectedCells();
            if (cells == null)
            {
                HideGhost();
                return;
            }

            var origin = DropOrigin(world);
            bool valid = controller.Grid.CanPlace(cells, origin.X, origin.Y);
            for (int i = 0; i < ghostCells.Length; i++)
            {
                bool used = i < cells.Length;
                ghostCells[i].gameObject.SetActive(used);
                if (!used) continue;
                Vector2 pos = gridView.CellToWorld(origin.X + cells[i].X, origin.Y + cells[i].Y);
                ghostCells[i].transform.position = new Vector3(pos.x, pos.y, 0f);
                ghostCells[i].color = valid ? GhostValidColor : GhostInvalidColor;
            }
        }

        void HideGhost()
        {
            foreach (var cell in ghostCells) cell.gameObject.SetActive(false);
        }

        Vector2 SlotCenter(int index)
        {
            float step = config.cellSize + config.cellGap;
            float gridHalfHeight = config.gridHeight * step * 0.5f;
            float pitch = config.handSlotSize + config.handSlotGap;
            float x = (index - (controller.Hand.Size - 1) * 0.5f) * pitch;
            float y = -(gridHalfHeight + 0.6f + config.handSlotSize * 0.5f);
            return new Vector2(x, y);
        }

        int SlotAt(Vector3 world)
        {
            float half = config.handSlotSize * 0.5f;
            for (int i = 0; i < controller.Hand.Size; i++)
            {
                var c = SlotCenter(i);
                if (Mathf.Abs(world.x - c.x) <= half && Mathf.Abs(world.y - c.y) <= half) return i;
            }
            return -1;
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            var hand = controller.Hand;
            for (int i = 0; i < hand.Size; i++)
            {
                Vector2 center = SlotCenter(i);
                bool highlighted = dragging ? i == hand.Selected : i == hoveredSlot;
                SpriteUtil.AddSquare(transform, $"Slot {i}", center, config.handSlotSize,
                    highlighted ? SlotSelectedColor : SlotColor, -1);

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
