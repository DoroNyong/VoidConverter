using UnityEngine;

namespace VoidConverter.View
{
    /// <summary>
    /// Where everything sits on the play screen (2026-11-16, from the developer's 1280x720 draft). Every panel is a
    /// rectangle given in the draft's pixels and turned into world units here, so the layout can be changed in one
    /// place. The screen is a fixed 16:9 area; the camera shows all of it (wider or taller windows get extra margin).
    ///
    ///  +-------------+                              +-----------+
    ///  | Info        |            (boss)            | Character |
    ///  +-------------+                              +-----------+
    ///          /\                                   | Stats     |
    ///         /  \  board                           +-----------+
    ///         \  /                                  | Items     |
    ///          \/                                   +-----------+
    ///  +-----+--------+-----------+---------------+ | Next      |
    ///  |     | Reroll | Block     | Tile weights  | | stage     |
    ///  +-----+--------+-----------+---------------+ +-----------+
    /// </summary>
    public static class HudLayout
    {
        const float RefWidth = 1280f;
        const float RefHeight = 720f;

        /// <summary>Middle of the screen area in world units. The title, event and shop screens are drawn around it.</summary>
        public static readonly Vector2 Center = new Vector2(0f, -0.85f);
        /// <summary>Half the height of the screen area in world units.</summary>
        public const float HalfHeight = 5.57f;
        public const float HalfWidth = HalfHeight * RefWidth / RefHeight;

        /// <summary>World units per draft pixel.</summary>
        public static float Scale => HalfWidth * 2f / RefWidth;

        /// <summary>A draft pixel (origin top left, y down) in world units.</summary>
        public static Vector2 Point(float px, float py)
        {
            return new Vector2(Center.x - HalfWidth + px * Scale, Center.y + HalfHeight - py * Scale);
        }

        /// <summary>A draft rectangle (left, top, right, bottom in pixels) in world units.</summary>
        public static Rect Box(float left, float top, float right, float bottom)
        {
            Vector2 a = Point(left, bottom);
            Vector2 b = Point(right, top);
            return new Rect(a, b - a);
        }

        // The draft's panels, in its pixels.
        public static Rect Info => Box(8, 8, 300, 165);
        public static Rect Character => Box(1000, 8, 1272, 184);
        public static Rect Stats => Box(1000, 190, 1272, 362);
        public static Rect Items => Box(1000, 368, 1272, 552);
        public static Rect NextStage => Box(1000, 558, 1272, 712);
        public static Rect Empty => Box(8, 562, 178, 712);
        public static Rect Reroll => Box(184, 562, 342, 712);
        public static Rect Hand => Box(348, 562, 616, 712);
        public static Rect Weights => Box(622, 562, 994, 712);
        /// <summary>The free corner above the board's right side, used by the boss panel in the boss stage.</summary>
        public static Rect Boss => Box(650, 14, 990, 168);

        /// <summary>Middle of the board: in the space left of the right column and above the bottom row.</summary>
        public static Vector2 BoardCenter => Point(497f, 282f);

        /// <summary>Fits the camera so the whole screen area shows, whatever the window's aspect ratio.</summary>
        public static void FitCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(HalfHeight, HalfWidth / cam.aspect);
            var pos = cam.transform.position;
            cam.transform.position = new Vector3(Center.x, Center.y, pos.z);
        }

        /// <summary>True if a world point is inside the rectangle.</summary>
        public static bool Contains(Rect r, Vector2 p) => r.Contains(p);

        /// <summary>A text's position at the top-left of a panel, with a margin.</summary>
        public static Vector2 TopLeft(Rect r, float margin = 0.25f) => new Vector2(r.xMin + margin, r.yMax - margin);

        /// <summary>A panel box filling the rectangle.</summary>
        public static SpriteRenderer AddPanel(Transform parent, string name, Rect r, Color color, int order)
        {
            return SpriteUtil.AddBox(parent, name, r.center, r.width, r.height, color, order);
        }

        public static readonly Color PanelColor = new Color(0.11f, 0.14f, 0.22f, 0.92f);
        public static readonly Color TitleColor = new Color(1f, 0.9f, 0.55f);
        public static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.45f);

        /// <summary>A left-aligned text whose top-left corner is at <paramref name="pos"/>.</summary>
        public static TextMesh AddLeftText(Transform parent, string name, Vector2 pos, string text, int fontSize, float characterSize, Color color, int order)
        {
            var tm = TextUtil.AddText(parent, name, pos, text, fontSize, characterSize, color, order);
            tm.anchor = TextAnchor.UpperLeft;
            tm.alignment = TextAlignment.Left;
            return tm;
        }
    }
}
