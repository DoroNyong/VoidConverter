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
        static readonly Color RewardColor = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color VoidColor = new Color(0.55f, 0.3f, 0.85f);
        const float UsedDim = 0.3f;

        const float CameraMargin = 0.8f;
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
                    SpriteUtil.AddSquare(transform, $"Cell {x},{y}", origin + new Vector2(x * Step, y * Step),
                        config.cellSize, color, 0);
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

            float top = gridHalfHeight + CameraMargin;
            float bottom = gridHalfHeight + HandGapBelowGrid + config.handSlotSize + CameraMargin;
            float halfWidth = Mathf.Max(gridHalfWidth, handWidth * 0.5f) + CameraMargin;

            // orthographicSize is the vertical half-extent, so widen it when the view is narrower than the content.
            cam.orthographicSize = Mathf.Max((top + bottom) * 0.5f, halfWidth / cam.aspect);
            var pos = cam.transform.position;
            cam.transform.position = new Vector3(0f, (top - bottom) * 0.5f, pos.z);
        }

        static Color ColorOf(CellType type)
        {
            switch (type)
            {
                case CellType.Monster: return MonsterColor;
                case CellType.Reward: return RewardColor;
                default: return VoidColor;
            }
        }
    }
}
