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
            // Not a child of the board: Rebuild clears the board's children, and the flare outlives a rebuild.
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

        /// <summary>World position of a cell's center.</summary>
        public Vector2 CellToWorld(int x, int y)
        {
            return (Vector2)transform.position + config.CellOffset(x, y);
        }

        /// <summary>World position to fractional cell coordinates (cell centers sit on whole numbers).</summary>
        public Vector2 WorldToGrid(Vector3 world)
        {
            return config.OffsetToCell((Vector2)(world - transform.position));
        }

        static readonly Color BuffedLabelColor = new Color(1f, 0.92f, 0.3f);
        static readonly Color BuffLinkColor = new Color(1f, 0.92f, 0.3f, 0.9f);

        /// <summary>A thin line between the Wisp and the monster it strengthens.</summary>
        void AddBuffLink(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            var link = SpriteUtil.AddSquare(transform, "Buff Link", (from + to) * 0.5f, 1f, BuffLinkColor, 4);
            link.transform.localScale = new Vector3(d.magnitude, 0.06f, 1f);
            link.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            var grid = controller.Grid;
            float tileW = config.TileWidth;
            float tileH = config.cellSize;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid[x, y];
                    // Fired cells and cells a block covered are dead: dimmed, no label.
                    bool dead = cell.Used || cell.Covered;
                    Color color = cell.Type == CellType.Monster && cell.Magic ? MagicMonsterColor : ColorOf(cell.Type);
                    if (dead) color *= UsedDim;
                    color.a = 1f;
                    Vector2 center = config.CellOffset(x, y);
                    // A cell a block covered gets a red frame: a red diamond with the cell's own diamond inset on top.
                    // The cell that fired gets a bright frame instead, so what fired and what was only covered read apart.
                    bool fired = cell.Covered && cell.Used;
                    bool blocked = cell.Covered && grid.BlockCovered && !fired;
                    if (fired) SpriteUtil.AddDiamond(transform, $"Fired Frame {x},{y}", center, tileW, tileH, FiredFrameColor, -1);
                    else if (blocked) SpriteUtil.AddDiamond(transform, $"Blocked Frame {x},{y}", center, tileW, tileH, BlockedFrameColor, -1);
                    float inset = fired || blocked ? 1f - BlockedFrameThickness * 2f : 1f;
                    var cellRenderer = SpriteUtil.AddDiamond(transform, $"Cell {x},{y}", center, tileW * inset, tileH * inset, color, 0);
                    // With a picture for this kind of cell, draw it (dimmed when dead) instead of the colored diamond.
                    var art = TileArt.For(cell);
                    if (art != null)
                    {
                        TileArt.Apply(cellRenderer, art, tileW * inset, tileH * inset);
                        cellRenderer.color = dead ? new Color(UsedDim, UsedDim, UsedDim, 1f) : Color.white;
                    }

                    // A small picture on the colored tile: the monster, coin, heart or spikes. The cell's number moves
                    // down to the lower part of the tile so it does not sit on the picture.
                    Vector2 labelAt = center;
                    float labelScale = 1f;
                    if (art == null && TileArt.TryIcon(cell, tileW, tileH, out var icon, out float iconWidth, out float iconY))
                    {
                        // A tile further down the screen is in front: its picture is drawn over the ones behind it.
                        int order = Mathf.Clamp(1 + (grid.Width + grid.Height - 2) - (x + y), 1, 9);
                        var iconRenderer = SpriteUtil.AddSquare(transform, $"Icon {x},{y}", center + new Vector2(0f, iconY), 1f,
                            dead ? new Color(UsedDim, UsedDim, UsedDim, 1f) : Color.white, order);
                        Vector2 size = icon.bounds.size;
                        iconRenderer.sprite = icon;
                        iconRenderer.transform.localScale = new Vector3(iconWidth / size.x, iconWidth / size.x, 1f);
                        labelAt = center + new Vector2(0f, -tileH * IconLabelDrop);
                        labelScale = IconLabelScale;
                    }

                    if (cell.Type == CellType.Void && !dead && cell.Kind != VoidKind.Normal)
                    {
                        SpriteUtil.AddDiamond(transform, $"Void Kind {x},{y}", center, tileW * VoidInnerScale, tileH * VoidInnerScale, KindColor(cell.Kind), 1);
                    }
                    // A contaminated cell keeps its look, with a small marker at its top corner.
                    if (cell.Contaminated && !dead)
                    {
                        Vector2 top = center + new Vector2(0f, tileH * 0.5f - tileH * ContaminationScale * 0.5f);
                        SpriteUtil.AddDiamond(transform, $"Contaminated {x},{y}", top, tileW * ContaminationScale, tileH * ContaminationScale, SpreadVoidColor, 2);
                    }

                    // Monster power is public. Locked cells show nothing. The odds depend on which monsters a block
                    // covers together, so they are shown on the drag ghost rather than per cell.
                    if (cell.Type == CellType.Monster && !dead)
                    {
                        // A monster a Wisp is strengthening shows its raised power in yellow.
                        bool buffed = grid.IsBuffed(x, y);
                        TextUtil.AddText(transform, "Power", labelAt, (cell.Magic ? "M" : "A") + grid.EffectivePower(x, y), 64, 0.07f * labelScale, buffed ? BuffedLabelColor : Color.white, 11);
                        // The Wisp is tied to the monster it strengthens by a thin yellow line.
                        if (cell.BuffAmount > 0 && cell.BuffX >= 0 && grid.IsBuffLinkAlive(x, y)) AddBuffLink(center, config.CellOffset(cell.BuffX, cell.BuffY));
                    }
                    else if (cell.Type == CellType.Void && cell.Kind == VoidKind.Danger && !dead)
                    {
                        TextUtil.AddText(transform, "Danger HP", center, cell.Hp.ToString(), 64, 0.07f, Color.white, 11);
                    }
                    else if (cell.Type == CellType.Void && cell.Kind == VoidKind.Buff && !dead)
                    {
                        TextUtil.AddText(transform, "Buff Power", center, "+" + grid.BuffPower, 64, 0.06f, Color.black, 11);
                    }
                    // Reward cells say what they give. Locked cells show nothing.
                    else if (cell.Type == CellType.Trap && !dead)
                    {
                        TextUtil.AddText(transform, "Trap Damage", labelAt, "-" + config.trapDamage, 64, 0.07f * labelScale, Color.white, 11);
                    }
                    else if (cell.Type == CellType.Gold && !dead)
                    {
                        TextUtil.AddText(transform, "Gold Reward", labelAt, "$" + config.goldCellReward, 64, 0.06f * labelScale, RewardLabelColor, 11);
                    }
                    else if (cell.Type == CellType.Heal && !dead)
                    {
                        TextUtil.AddText(transform, "Heal Amount", labelAt, "+" + config.healCellAmount + " HP", 64, 0.045f * labelScale, RewardLabelColor, 11);
                    }
                    // Every void cell shows, faintly, the same power: the monster it would spawn. It re-rolls after every placement.
                    else if (cell.Type == CellType.Void && cell.Kind == VoidKind.Normal && !dead)
                    {
                        TextUtil.AddText(transform, "Void Power", center, grid.VoidPower.ToString(), 64, 0.07f, VoidPowerColor, 11);
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

            float gridHalfWidth = config.GridHalfWidth;
            float gridHalfHeight = config.GridHalfHeight;
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
