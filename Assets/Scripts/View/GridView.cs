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
        [SerializeField] float cellSize = 1f;
        [SerializeField] float gap = 0.06f;

        static readonly Color MonsterColor = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color RewardColor = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color VoidColor = new Color(0.55f, 0.3f, 0.85f);

        Grid grid;
        SpriteRenderer[,] renderers;
        Sprite square;

        void Start()
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            square = CreateSquareSprite();
            FitCamera();
            Rebuild();
        }

        void Update()
        {
            // Debug: regenerate the board.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Rebuild();
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            grid = new Grid(config.gridWidth, config.gridHeight);
            grid.Generate(config.ToGridSettings(), new System.Random());
            renderers = new SpriteRenderer[grid.Width, grid.Height];

            float step = cellSize + gap;
            Vector2 origin = new Vector2(-(grid.Width - 1) * step * 0.5f, -(grid.Height - 1) * step * 0.5f);

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var go = new GameObject($"Cell {x},{y}");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = origin + new Vector2(x * step, y * step);
                    go.transform.localScale = Vector3.one * cellSize;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = square;
                    sr.color = ColorOf(grid[x, y].Type);
                    renderers[x, y] = sr;
                }
            }
        }

        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            const float margin = 1.5f;
            float step = cellSize + gap;
            float halfWidth = config.gridWidth * step * 0.5f + margin;
            float halfHeight = config.gridHeight * step * 0.5f + margin;
            // orthographicSize is the vertical half-extent, so widen it when the view is narrower than the grid.
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
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

        static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
