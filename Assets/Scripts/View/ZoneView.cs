using UnityEngine;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// Draws the boss stage's danger zone over the board: nine red cells with a pulsing frame, the middle cell darker
    /// with an X. The zone slides to its new place after every block. Placeholder art until the boss picture exists.
    /// </summary>
    public class ZoneView : MonoBehaviour
    {
        // Above the cell numbers (order 11), below the fire pulse (15) and the percent badges.
        const int FillOrder = 12;
        const int FrameOrder = 13;
        const int MarkOrder = 14;
        // A dark shade reads on every cell color (a red tint vanished on the red and orange cells).
        // The boss's attack is purple, so it never mixes with the red of damage.
        static readonly Color FillColor = new Color(0.35f, 0.05f, 0.6f, 0.42f);
        static readonly Color MiddleFillColor = new Color(0.3f, 0f, 0.5f, 0.75f);
        static readonly Color FrameColor = new Color(0.78f, 0.35f, 1f, 1f);
        const float InnerFrameScale = 0.9f;
        const float SlideSpeed = 6f;
        const float PulseSpeed = 4f;

        GameController controller;
        GameConfig config;
        GridView gridView;
        GameObject root;
        SpriteRenderer[] fills = new SpriteRenderer[9];
        SpriteRenderer[] frames = new SpriteRenderer[9];
        SpriteRenderer[] innerFrames = new SpriteRenderer[9];
        TextMesh mark;
        // The zone's middle on screen. It slides toward its cell so a move can be followed by eye.
        Vector2 shown;
        bool placed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<GameController>() == null || FindFirstObjectByType<ZoneView>() != null) return;
            new GameObject("Danger Zone View").AddComponent<ZoneView>();
        }

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            gridView = FindFirstObjectByType<GridView>();
            config = controller.Config;

            root = new GameObject("Danger Zone");
            root.transform.SetParent(transform, false);
            float w = config.TileWidth, h = config.cellSize;
            for (int i = 0; i < 9; i++)
            {
                bool middle = i == 4;
                fills[i] = SpriteUtil.AddDiamond(root.transform, "Fill " + i, Vector2.zero, w, h, middle ? MiddleFillColor : FillColor, FillOrder);
                frames[i] = SpriteUtil.AddDiamondOutline(root.transform, "Frame " + i, Vector2.zero, w, h, FrameColor, FrameOrder);
                // A second, smaller frame makes the line twice as thick.
                innerFrames[i] = SpriteUtil.AddDiamondOutline(root.transform, "Inner Frame " + i, Vector2.zero, w * InnerFrameScale, h * InnerFrameScale, FrameColor, FrameOrder);
            }
            mark = TextUtil.AddText(root.transform, "Middle Mark", Vector2.zero, "X", 64, 0.07f, new Color(0.95f, 0.88f, 1f, 1f), MarkOrder);
            root.SetActive(false);
        }

        void Update()
        {
            var zone = controller != null ? controller.Zone : null;
            bool visible = zone != null && controller.IsBossStage && gridView != null;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible)
            {
                placed = false;
                return;
            }

            Vector2 target = gridView.CellToWorld(zone.CenterX, zone.CenterY);
            if (!placed)
            {
                shown = target;
                placed = true;
            }
            shown = Vector2.Lerp(shown, target, 1f - Mathf.Exp(-SlideSpeed * Time.unscaledDeltaTime));

            // The board is linear, so the zone's other cells keep their offsets from its middle cell.
            Vector2 origin = gridView.CellToWorld(zone.CenterX, zone.CenterY);
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
            int i = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++, i++)
                {
                    // The part of the zone that hangs over the edge of the board is not drawn.
                    int cx = zone.CenterX + dx, cy = zone.CenterY + dy;
                    bool onBoard = cx >= 0 && cy >= 0 && cx < controller.Grid.Width && cy < controller.Grid.Height;
                    fills[i].enabled = onBoard;
                    frames[i].enabled = onBoard;
                    innerFrames[i].enabled = onBoard;
                    Vector2 pos = shown + (gridView.CellToWorld(cx, cy) - origin);
                    fills[i].transform.position = pos;
                    frames[i].transform.position = pos;
                    innerFrames[i].transform.position = pos;
                    var c = FrameColor;
                    c.a = i == 4 ? 1f : pulse;
                    frames[i].color = c;
                    innerFrames[i].color = c;
                }
            }
            mark.transform.position = shown;
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root);
        }
    }
}
