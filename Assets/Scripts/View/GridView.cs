using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;
using Grid = VoidConverter.Core.Grid;

namespace VoidConverter.View
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        static readonly Color MonsterColor = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color RewardColor = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color VoidColor = new Color(0.55f, 0.3f, 0.85f);

        const float CameraMargin = 0.8f;
        const float HandGapBelowGrid = 0.6f;

        Grid grid;
        SpriteRenderer[,] renderers;

        void Start()
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            FitCamera();
            Rebuild();
        }

        void Update()
        {
            // Debug: regenerate the board.
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) Rebuild();
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            grid = new Grid(config.gridWidth, config.gridHeight);
            grid.Generate(config.ToGridSettings(), new System.Random());
            renderers = new SpriteRenderer[grid.Width, grid.Height];

            float step = config.cellSize + config.cellGap;
            Vector2 origin = new Vector2(-(grid.Width - 1) * step * 0.5f, -(grid.Height - 1) * step * 0.5f);

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    renderers[x, y] = SpriteUtil.AddSquare(
                        transform, $"Cell {x},{y}", origin + new Vector2(x * step, y * step),
                        config.cellSize, ColorOf(grid[x, y].Type), 0);
                }
            }
        }

        /// <summary>Frames the grid with the hand area below it, whatever the screen aspect ratio.</summary>
        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;

            float step = config.cellSize + config.cellGap;
            float gridHalfWidth = config.gridWidth * step * 0.5f;
            float gridHalfHeight = config.gridHeight * step * 0.5f;
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
