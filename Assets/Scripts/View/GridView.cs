using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;
using Grid = VoidConverter.Core.Grid;

namespace VoidConverter.View
{
    public class GridView : MonoBehaviour
    {
        static readonly Color MonsterColor = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color GoldColor = new Color(0.9f, 0.7f, 0.15f);
        static readonly Color HealColor = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color RewardLabelColor = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color VoidColor = new Color(0.55f, 0.3f, 0.85f);
        static readonly Color VoidPowerColor = new Color(1f, 1f, 1f, 0.32f);
        const float UsedDim = 0.3f;

        const float CameraMargin = 0.8f;
        const float HudSpace = 2.1f;
        const float HandGapBelowGrid = 0.6f;

        GameController controller;
        GameConfig config;
        float fittedAspect;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            FitCamera();
            controller.Changed += Rebuild;
            Rebuild();
        }

        void Update()
        {
            // The window can be resized at any time; keep the whole board and hand in view.
            if (Camera.main != null && !Mathf.Approximately(Camera.main.aspect, fittedAspect)) FitCamera();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Rebuild;
        }

        float Step => config.cellSize + config.cellGap;

        Vector2 GridOrigin(Grid grid)
        {
            return new Vector2(-(grid.Width - 1) * Step * 0.5f, -(grid.Height - 1) * Step * 0.5f);
        }

        /// <summary>World position of a cell's center.</summary>
        public Vector2 CellToWorld(int x, int y)
        {
            return (Vector2)transform.position + GridOrigin(controller.Grid) + new Vector2(x * Step, y * Step);
        }

        /// <summary>World position to fractional cell coordinates (cell centers sit on whole numbers).</summary>
        public Vector2 WorldToGrid(Vector3 world)
        {
            Vector2 local = (Vector2)(world - transform.position) - GridOrigin(controller.Grid);
            return local / Step;
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            var grid = controller.Grid;
            Vector2 origin = GridOrigin(grid);
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid[x, y];
                    Color color = ColorOf(cell.Type);
                    if (cell.Used && cell.Type != CellType.Void) color *= UsedDim;
                    color.a = 1f;
                    Vector2 center = origin + new Vector2(x * Step, y * Step);
                    SpriteUtil.AddSquare(transform, $"Cell {x},{y}", center, config.cellSize, color, 0);

                    // Monster power is public. Locked cells show nothing. The odds depend on which monsters a block
                    // covers together, so they are shown on the drag ghost rather than per cell.
                    if (cell.Type == CellType.Monster && !cell.Used)
                    {
                        TextUtil.AddText(transform, "Power", center, cell.Power.ToString(), 64, 0.07f, Color.white, 10);
                    }
                    // Reward cells say what they give. Locked cells show nothing.
                    else if (cell.Type == CellType.Gold && !cell.Used)
                    {
                        TextUtil.AddText(transform, "Gold Reward", center, "$" + config.goldCellReward, 64, 0.06f, RewardLabelColor, 10);
                    }
                    else if (cell.Type == CellType.Heal && !cell.Used)
                    {
                        TextUtil.AddText(transform, "Heal Amount", center, "+" + config.healCellAmount + " HP", 64, 0.045f, RewardLabelColor, 10);
                    }
                    // Every void cell shows, faintly, the same power: the monster it would spawn. It re-rolls after every placement.
                    else if (cell.Type == CellType.Void)
                    {
                        TextUtil.AddText(transform, "Void Power", center, grid.VoidPower.ToString(), 64, 0.07f, VoidPowerColor, 10);
                    }
                }
            }
        }

        /// <summary>Frames the grid with the hand area below it, whatever the screen aspect ratio.</summary>
        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            fittedAspect = cam.aspect;

            float gridHalfWidth = config.gridWidth * Step * 0.5f;
            float gridHalfHeight = config.gridHeight * Step * 0.5f;
            float handWidth = config.handSize * config.handSlotSize + (config.handSize - 1) * config.handSlotGap;

            float top = gridHalfHeight + HudSpace;
            float bottom = gridHalfHeight + HandGapBelowGrid + config.handSlotSize + CameraMargin;
            // Room on the right for the odds panel shown while a block is dragged; keep the board centered by
            // reserving the same room on the left.
            float panelReach = gridHalfWidth + HandView.SidePanelGap + HandView.SidePanelWidth;
            float halfWidth = Mathf.Max(panelReach, handWidth * 0.5f) + 0.3f;

            // orthographicSize is the vertical half-extent, so widen it when the view is narrower than the content.
            cam.orthographicSize = Mathf.Max((top + bottom) * 0.5f, halfWidth / cam.aspect);
            var pos = cam.transform.position;
            cam.transform.position = new Vector3(0f, (top - bottom) * 0.5f, pos.z);
        }

        public static Color ChanceColor(float chance)
        {
            if (chance >= 0.7f) return new Color(0.85f, 1f, 0.85f);
            if (chance >= 0.4f) return new Color(1f, 0.95f, 0.6f);
            return new Color(1f, 0.8f, 0.7f);
        }

        static Color ColorOf(CellType type)
        {
            switch (type)
            {
                case CellType.Monster: return MonsterColor;
                case CellType.Gold: return GoldColor;
                case CellType.Heal: return HealColor;
                default: return VoidColor;
            }
        }
    }
}
