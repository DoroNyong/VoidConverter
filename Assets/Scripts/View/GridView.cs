using System.Collections.Generic;
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
        // Empty cells are spent from the start, so this is shown dimmed like any used cell.
        static readonly Color MagicMonsterColor = new Color(0.25f, 0.45f, 0.9f);
        static readonly Color TrapColor = new Color(0.8f, 0.4f, 0.1f);
        static readonly Color EmptyColor = new Color(0.6f, 0.62f, 0.7f);
        static readonly Color VoidPowerColor = new Color(1f, 1f, 1f, 0.32f);
        static readonly Color BuffVoidColor = new Color(1f, 0.95f, 0.3f);
        static readonly Color SpreadVoidColor = new Color(0.5f, 1f, 0.25f);
        static readonly Color DangerVoidColor = new Color(0.95f, 0.15f, 0.15f);
        // A special void is drawn as a purple frame around its kind color, so it still reads as a void.
        const float VoidInnerScale = 0.72f;
        const float ContaminationScale = 0.28f;
        const float UsedDim = 0.3f;
        // A cell a block covered cannot take another block: it gets a red frame.
        static readonly Color BlockedFrameColor = new Color(0.95f, 0.2f, 0.2f);
        const float BlockedFrameThickness = 0.08f;
        // The cell that fired in an earlier placement keeps a bright frame (not red, which means "covered, spent").
        static readonly Color FiredFrameColor = new Color(1f, 1f, 1f, 1f);
        // A border that flares out from the cell that just fired and fades.
        const float PulseSeconds = 0.7f;
        const float PulseGrowth = 0.6f;
        // How far (of the tile height) a cell's number sits below the middle when the cell has a picture.
        const float IconLabelDrop = 0.28f;
        const float IconLabelScale = 0.8f;


        GameController controller;
        GameConfig config;
        float fittedAspect;

        void Awake()
        {
            // The board sits left of the right-hand column and above the bottom row (HudLayout). Every cell position
            // goes through this transform (CellToWorld), so moving it moves the whole board. Set in Awake so the other
            // views see the final position in their Start.
            transform.position = HudLayout.BoardCenter;
        }

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            FitCamera();
            // Not a child of the board: it is placed in world space and outlives any board change.
            pulse = SpriteUtil.AddDiamondOutline(null, "Fire Pulse", Vector2.zero, config.TileWidth, config.cellSize, Color.white, 15);
            pulse.enabled = false;
            controller.Changed += Rebuild;
            controller.Fired += OnFired;
            Rebuild();
        }

        SpriteRenderer pulse;
        float pulseStart = -100f;

        void OnFired(FeedbackEvent e)
        {
            pulse.transform.position = CellToWorld(e.X, e.Y);
            pulseStart = Time.unscaledTime;
        }

        void Update()
        {
            float t = (Time.unscaledTime - pulseStart) / PulseSeconds;
            if (t >= 1f || t < 0f)
            {
                pulse.enabled = false;
            }
            else
            {
                pulse.enabled = true;
                float grow = 1f + PulseGrowth * t;
                pulse.transform.localScale = new Vector3(config.TileWidth * grow, config.cellSize * grow, 1f);
                pulse.color = new Color(1f, 1f, 1f, 1f - t);
            }

            if (cracksShown.Count > 0)
            {
                var c = CrackColor;
                c.a = 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * CrackBlinkSpeed);
                foreach (var crack in cracksShown) crack.color = c;
            }

            // The window can be resized at any time; keep the whole board and hand in view.
            if (Camera.main != null && !Mathf.Approximately(Camera.main.aspect, fittedAspect)) FitCamera();
        }

        void OnDestroy()
        {
            if (controller != null)
            {
                controller.Changed -= Rebuild;
                controller.Fired -= OnFired;
            }
            if (pulse != null) Destroy(pulse.gameObject);
        }

        // The board is a diamond (see GameConfig.CellOffset): this is the only place that turns cells into screen
        // positions and back, so the input and the effects follow whatever shape the board has.

        // The board is centered on the current grid's size, which a stage may set apart from gridWidth x gridHeight.
        int BoardWidth => controller != null && controller.Grid != null ? controller.Grid.Width : config.gridWidth;
        int BoardHeight => controller != null && controller.Grid != null ? controller.Grid.Height : config.gridHeight;

        /// <summary>A cell's center relative to the board's center.</summary>
        Vector2 CellOffset(float x, float y) => config.CellOffset(x, y, BoardWidth, BoardHeight);

        /// <summary>World position of a cell's center.</summary>
        public Vector2 CellToWorld(int x, int y)
        {
            return (Vector2)transform.position + CellOffset(x, y);
        }

        /// <summary>World position to fractional cell coordinates (cell centers sit on whole numbers).</summary>
        public Vector2 WorldToGrid(Vector3 world)
        {
            return config.OffsetToCell((Vector2)(world - transform.position), BoardWidth, BoardHeight);
        }

        // Where a monster's feet stand below the tile's center (as a fraction of the tile height), and how fast it idles.
        const float MonsterFootDrop = 0.28f;
        const float MonsterFps = 3f;

        static readonly Color BuffedLabelColor = new Color(1f, 0.92f, 0.3f);
        static readonly Color BuffLinkColor = new Color(1f, 0.92f, 0.3f, 0.9f);
        static readonly Color DeadTint = new Color(UsedDim, UsedDim, UsedDim, 1f);

        // Sorting orders of a cell's pieces (pictures use 1..9 by depth, see PictureOrder).
        const int FrameOrder = -1;
        const int TileOrder = 0;
        const int KindOrder = 1;
        const int ContaminationOrder = 2;
        const int BuffLinkOrder = 4;
        const int CrackOrder = 10;
        // A cell marked to break (CellFloor.Cracked) gets a blinking see-through warning cover. Temporary look: the picture for it
        // comes with the rule (idea 34).
        static readonly Color CrackColor = new Color(1f, 0.55f, 0.1f);
        const float CrackBlinkSpeed = 6f;
        const int LabelOrder = 11;
        const int LabelFontSize = 64;

        /// <summary>
        /// The pieces one board cell is drawn with. They are made once and reused (object pooling, 2026-11-22): a
        /// refresh only changes sprites, colors, sizes and texts and turns unused pieces off, instead of destroying and
        /// re-creating every object after each placement.
        /// </summary>
        sealed class CellPieces
        {
            public GameObject Root;
            public SpriteRenderer Frame;          // white frame (fired) or red frame (covered)
            public SpriteRenderer Tile;           // the colored diamond, or the tile's own picture
            public SpriteRenderer Picture;        // monster or icon standing on the tile
            public SpriteFrames Idle;             // the monster's idle animation
            public SpriteRenderer KindMark;       // inner diamond of a special void
            public SpriteRenderer Contamination;  // marker at the top corner
            public SpriteRenderer Crack;          // blinking cover: the cell breaks after the next placement
            public TextMesh Label;                // the cell's number
            public MeshRenderer LabelRenderer;
            public float LabelCharacterSize;
            public Font LabelFont;               // the snapping in TextUtil.SetSize depends on the font
        }

        // Pieces by cell coordinate. The array only grows: a smaller board (or a cell that is not on the board) just
        // switches the extra pieces off, so boards of different sizes and shapes reuse the same objects.
        CellPieces[,] pieces = new CellPieces[0, 0];
        // Buff lines are not tied to a cell: the first buffLinksUsed of these are shown, the rest are off.
        readonly List<SpriteRenderer> buffLinks = new List<SpriteRenderer>();
        int buffLinksUsed;

        /// <summary>
        /// Whether a cell is part of the board at all: false for a hole (left out by the board shape, or broken; see
        /// CellFloor). The cell's pieces are then switched off as a whole, so nothing is drawn there.
        /// </summary>
        static bool IsOnBoard(Grid grid, int x, int y) => !grid[x, y].IsHole;

        // Cracked cells shown right now (their covers blink in Update).
        readonly List<SpriteRenderer> cracksShown = new List<SpriteRenderer>();

        CellPieces CreatePieces(int x, int y)
        {
            var root = new GameObject($"Cell {x},{y}");
            root.transform.SetParent(transform, false);
            var p = new CellPieces { Root = root };
            var t = root.transform;
            p.Frame = SpriteUtil.AddDiamond(t, "Frame", Vector2.zero, 1f, 1f, Color.white, FrameOrder);
            p.Tile = SpriteUtil.AddDiamond(t, "Tile", Vector2.zero, 1f, 1f, Color.white, TileOrder);
            p.Picture = SpriteUtil.AddSquare(t, "Picture", Vector2.zero, 1f, Color.white, 1);
            p.Idle = p.Picture.gameObject.AddComponent<SpriteFrames>();
            p.Idle.enabled = false;
            p.KindMark = SpriteUtil.AddDiamond(t, "Void Kind", Vector2.zero, 1f, 1f, Color.white, KindOrder);
            p.Contamination = SpriteUtil.AddDiamond(t, "Contaminated", Vector2.zero, 1f, 1f, SpreadVoidColor, ContaminationOrder);
            p.Crack = SpriteUtil.AddDiamond(t, "Crack", Vector2.zero, config.TileWidth, config.cellSize, CrackColor, CrackOrder);
            p.Crack.enabled = false;
            p.Label = TextUtil.AddText(t, "Label", Vector2.zero, "", LabelFontSize, 0.07f, Color.white, LabelOrder);
            p.LabelRenderer = p.Label.GetComponent<MeshRenderer>();
            return p;
        }

        /// <summary>Makes sure there are pieces for every cell of a width x height board (the array never shrinks).</summary>
        void EnsurePieces(int width, int height)
        {
            int oldW = pieces.GetLength(0), oldH = pieces.GetLength(1);
            if (width <= oldW && height <= oldH) return;
            int newW = Mathf.Max(width, oldW), newH = Mathf.Max(height, oldH);
            var grown = new CellPieces[newW, newH];
            for (int x = 0; x < newW; x++)
                for (int y = 0; y < newH; y++)
                    grown[x, y] = x < oldW && y < oldH ? pieces[x, y] : CreatePieces(x, y);
            pieces = grown;
        }

        /// <summary>A thin line between the Wisp and the monster it strengthens (taken from the buff line pool).</summary>
        void ShowBuffLink(Vector2 from, Vector2 to)
        {
            if (buffLinksUsed == buffLinks.Count) buffLinks.Add(SpriteUtil.AddSquare(transform, "Buff Link", Vector2.zero, 1f, BuffLinkColor, BuffLinkOrder));
            var link = buffLinks[buffLinksUsed++];
            link.enabled = true;
            Vector2 d = to - from;
            link.transform.localPosition = (from + to) * 0.5f;
            link.transform.localScale = new Vector3(d.magnitude, 0.06f, 1f);
            link.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active) go.SetActive(active);
        }

        /// <summary>
        /// Redraws the board from the controller's grid (on every Changed). The pieces are reused; see CellPieces.
        /// </summary>
        void Rebuild()
        {
            var grid = controller.Grid;
            EnsurePieces(grid.Width, grid.Height);
            buffLinksUsed = 0;
            cracksShown.Clear();

            for (int x = 0; x < pieces.GetLength(0); x++)
            {
                for (int y = 0; y < pieces.GetLength(1); y++)
                {
                    var p = pieces[x, y];
                    bool shown = x < grid.Width && y < grid.Height && IsOnBoard(grid, x, y);
                    SetActive(p.Root, shown);
                    if (!shown) continue;
                    ShowCell(p, grid, x, y);
                    bool cracked = grid[x, y].Floor == CellFloor.Cracked;
                    if (p.Crack.enabled != cracked) p.Crack.enabled = cracked;
                    if (cracked) cracksShown.Add(p.Crack);
                }
            }

            for (int i = buffLinksUsed; i < buffLinks.Count; i++) buffLinks[i].enabled = false;
        }

        void ShowCell(CellPieces p, Grid grid, int x, int y)
        {
            var cell = grid[x, y];
            float tileW = config.TileWidth;
            float tileH = config.cellSize;
            Vector2 center = CellOffset(x, y);
            // Every piece is placed relative to the cell's root, which sits on the cell's center.
            p.Root.transform.localPosition = center;

            // Fired cells and cells a block covered are dead: dimmed, no label.
            bool dead = cell.Used || cell.Covered;
            Color color = cell.Type == CellType.Monster && cell.Magic ? MagicMonsterColor : ColorOf(cell.Type);
            if (dead) color *= UsedDim;
            color.a = 1f;

            // A cell a block covered gets a red frame: a red diamond with the cell's own diamond inset on top.
            // The cell that fired gets a bright frame instead, so what fired and what was only covered read apart.
            bool fired = cell.Covered && cell.Used;
            bool blocked = cell.Covered && grid.BlockCovered && !fired;
            p.Frame.enabled = fired || blocked;
            if (p.Frame.enabled)
            {
                p.Frame.color = fired ? FiredFrameColor : BlockedFrameColor;
                p.Frame.transform.localScale = new Vector3(tileW, tileH, 1f);
            }

            float inset = fired || blocked ? 1f - BlockedFrameThickness * 2f : 1f;
            // With a picture for this kind of cell, draw it (dimmed when dead) instead of the colored diamond.
            var art = TileArt.For(cell);
            if (art != null)
            {
                TileArt.Apply(p.Tile, art, tileW * inset, tileH * inset);
                p.Tile.color = dead ? DeadTint : Color.white;
            }
            else
            {
                p.Tile.sprite = SpriteUtil.Diamond;
                p.Tile.transform.localScale = new Vector3(tileW * inset, tileH * inset, 1f);
                p.Tile.color = color;
            }

            // A small picture on the colored tile: the monster, coin, heart or spikes. The cell's number moves
            // down to the lower part of the tile so it does not sit on the picture.
            Vector2 labelAt = Vector2.zero;
            float labelScale = 1f;
            p.Picture.enabled = false;
            p.Idle.enabled = false;
            // A tile further down the screen is in front: its picture is drawn over the ones behind it.
            int pictureOrder = Mathf.Clamp(1 + (grid.Width + grid.Height - 2) - (x + y), 1, 9);
            if (art == null && TileArt.TryMonster(cell, tileW, out var monsterFrames, out float monsterWidth))
            {
                // An animated monster standing on the tile: the frame's pivot is its feet. A dead one stays on its first frame.
                p.Picture.enabled = true;
                p.Picture.sortingOrder = pictureOrder;
                p.Picture.color = dead ? DeadTint : Color.white;
                p.Picture.sprite = monsterFrames[0];
                p.Picture.transform.localPosition = new Vector2(0f, -tileH * MonsterFootDrop);
                float monsterScale = monsterWidth / monsterFrames[0].bounds.size.x;
                p.Picture.transform.localScale = new Vector3(monsterScale, monsterScale, 1f);
                if (!dead && monsterFrames.Length > 1)
                {
                    p.Idle.Init(p.Picture, monsterFrames, MonsterFps, (x * 0.37f + y * 0.61f) % 1f);
                    p.Idle.enabled = true;
                }
                labelAt = new Vector2(0f, -tileH * IconLabelDrop);
                labelScale = IconLabelScale;
            }
            else if (art == null && TileArt.TryIcon(cell, tileW, tileH, out var icon, out float iconWidth, out float iconY))
            {
                p.Picture.enabled = true;
                p.Picture.sortingOrder = pictureOrder;
                p.Picture.color = dead ? DeadTint : Color.white;
                p.Picture.sprite = icon;
                p.Picture.transform.localPosition = new Vector2(0f, iconY);
                float iconScale = iconWidth / icon.bounds.size.x;
                p.Picture.transform.localScale = new Vector3(iconScale, iconScale, 1f);
                labelAt = new Vector2(0f, -tileH * IconLabelDrop);
                labelScale = IconLabelScale;
            }

            // A special void is drawn as a purple frame around its kind color, so it still reads as a void.
            p.KindMark.enabled = cell.Type == CellType.Void && !dead && cell.Kind != VoidKind.Normal;
            if (p.KindMark.enabled)
            {
                p.KindMark.color = KindColor(cell.Kind);
                p.KindMark.transform.localScale = new Vector3(tileW * VoidInnerScale, tileH * VoidInnerScale, 1f);
            }
            // A contaminated cell keeps its look, with a small marker at its top corner.
            p.Contamination.enabled = cell.Contaminated && !dead;
            if (p.Contamination.enabled)
            {
                p.Contamination.transform.localPosition = new Vector2(0f, tileH * 0.5f - tileH * ContaminationScale * 0.5f);
                p.Contamination.transform.localScale = new Vector3(tileW * ContaminationScale, tileH * ContaminationScale, 1f);
            }

            // Monster power is public. Locked cells show nothing. The odds depend on which monsters a block
            // covers together, so they are shown on the drag ghost rather than per cell.
            string text = null;
            Vector2 textAt = labelAt;
            float size = 0.07f * labelScale;
            Color textColor = Color.white;
            if (cell.Type == CellType.Monster && !dead)
            {
                // A monster a Wisp is strengthening shows its raised power in yellow.
                text = (cell.Magic ? "M" : "A") + grid.EffectivePower(x, y);
                if (grid.IsBuffed(x, y)) textColor = BuffedLabelColor;
                // The Wisp is tied to the monster it strengthens by a thin yellow line.
                if (cell.BuffAmount > 0 && cell.BuffX >= 0 && grid.IsBuffLinkAlive(x, y)) ShowBuffLink(center, CellOffset(cell.BuffX, cell.BuffY));
            }
            else if (cell.Type == CellType.Void && cell.Kind == VoidKind.Danger && !dead)
            {
                text = cell.Hp.ToString();
                textAt = Vector2.zero;
                size = 0.07f;
            }
            else if (cell.Type == CellType.Void && cell.Kind == VoidKind.Buff && !dead)
            {
                text = "+" + grid.BuffPower;
                textAt = Vector2.zero;
                size = 0.06f;
                textColor = Color.black;
            }
            // Reward cells say what they give. Locked cells show nothing.
            else if (cell.Type == CellType.Trap && !dead)
            {
                text = "-" + config.trapDamage;
            }
            else if (cell.Type == CellType.Gold && !dead)
            {
                text = "$" + config.goldCellReward;
                size = 0.06f * labelScale;
                textColor = RewardLabelColor;
            }
            else if (cell.Type == CellType.Heal && !dead)
            {
                text = "+" + config.healCellAmount + " HP";
                size = 0.045f * labelScale;
                textColor = RewardLabelColor;
            }
            // Every void cell shows, faintly, the same power: the monster it would spawn. It re-rolls after every placement.
            else if (cell.Type == CellType.Void && cell.Kind == VoidKind.Normal && !dead)
            {
                text = grid.VoidPower.ToString();
                textAt = Vector2.zero;
                size = 0.07f;
                textColor = VoidPowerColor;
            }
            ShowLabel(p, text, textAt, size, textColor);
        }

        /// <summary>Shows the cell's number, or hides it when text is null. Unchanged values are not set again (each set rebuilds the text mesh).</summary>
        static void ShowLabel(CellPieces p, string text, Vector2 at, float characterSize, Color color)
        {
            p.LabelRenderer.enabled = text != null;
            if (text == null) return;
            if (p.Label.text != text) p.Label.text = text;
            if (p.LabelFont != p.Label.font || !Mathf.Approximately(p.LabelCharacterSize, characterSize))
            {
                TextUtil.SetSize(p.Label, LabelFontSize, characterSize);
                p.LabelFont = p.Label.font;
                p.LabelCharacterSize = characterSize;
            }
            if (p.Label.color != color) p.Label.color = color;
            p.Label.transform.localPosition = at;
        }

        /// <summary>Shows the whole play screen (see HudLayout), whatever the screen aspect ratio.</summary>
        void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            fittedAspect = cam.aspect;
            HudLayout.FitCamera(cam);
        }

        public static Color ChanceColor(float chance)
        {
            if (chance >= 0.7f) return new Color(0.85f, 1f, 0.85f);
            if (chance >= 0.4f) return new Color(1f, 0.95f, 0.6f);
            return new Color(1f, 0.8f, 0.7f);
        }

        static Color KindColor(VoidKind kind)
        {
            switch (kind)
            {
                case VoidKind.Buff: return BuffVoidColor;
                case VoidKind.Spread: return SpreadVoidColor;
                case VoidKind.Danger: return DangerVoidColor;
                default: return VoidColor;
            }
        }

        static Color ColorOf(CellType type)
        {
            switch (type)
            {
                case CellType.Monster: return MonsterColor;
                case CellType.Gold: return GoldColor;
                case CellType.Heal: return HealColor;
                case CellType.Empty: return EmptyColor;
                case CellType.Trap: return TrapColor;
                default: return VoidColor;
            }
        }
    }
}
