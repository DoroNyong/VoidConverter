using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Data;

namespace VoidConverter.View
{
    /// <summary>
    /// What the board shows while a block is held over it (Pick-One rule): the chance of every cell that could fire,
    /// written on the cell, and a bright border that walks over those cells. The border stays longer on a likelier
    /// cell, so the odds can be read without any text. HandView feeds it.
    /// </summary>
    public class PreviewMarksView : MonoBehaviour
    {
        static readonly Color OutlineColor = new Color(1f, 1f, 1f, 1f);
        static readonly Color BadgeColor = new Color(0.04f, 0.06f, 0.1f, 0.72f);
        static readonly Color DamageTagColor = new Color(0.55f, 0.15f, 0.85f, 0.95f);
        static readonly Color DeathTagColor = new Color(0.75f, 0f, 0.05f, 0.98f);
        static readonly Color ZoneOutlineColor = new Color(0.8f, 0.4f, 1f, 1f);
        static readonly Color DangerOutlineColor = new Color(1f, 0.15f, 0.15f, 1f);
        const int OutlineOrder = 16;
        const int BadgeOrder = 25;
        const int BadgeTextOrder = 26;
        // How long the border stays on a cell: a base time plus more for a likelier cell.
        const float StepBase = 0.12f;
        const float StepPerChance = 1.2f;

        GameConfig config;
        SpriteRenderer outline;
        readonly List<Vector2> positions = new List<Vector2>();
        readonly List<float> chances = new List<float>();
        // 0 = safe, 1 = inside the boss's danger zone (big damage), 2 = this cell ends the run, 3 = it might (a fight that can be lost).
        readonly List<int> dangers = new List<int>();
        readonly List<GameObject> badges = new List<GameObject>();
        int current;
        float stepEnds;

        public void Init(GameConfig gameConfig)
        {
            config = gameConfig;
            outline = SpriteUtil.AddDiamondOutline(transform, "Walking Border", Vector2.zero, config.TileWidth, config.cellSize, OutlineColor, OutlineOrder);
            Hide();
        }

        /// <summary>Marks these cells (world positions) with their chance (0..1) to be the one that fires.</summary>
        public void Show(IReadOnlyList<Vector2> cellPositions, IReadOnlyList<float> cellChances, IReadOnlyList<int> cellDangers = null)
        {
            int Danger(IReadOnlyList<int> list, int i) => list != null ? list[i] : 0;
            bool same = cellPositions.Count == positions.Count;
            for (int i = 0; same && i < positions.Count; i++)
            {
                same = (cellPositions[i] - positions[i]).sqrMagnitude < 1e-6f && Mathf.Approximately(cellChances[i], chances[i])
                    && Danger(cellDangers, i) == dangers[i];
            }
            if (same) return;

            positions.Clear();
            chances.Clear();
            dangers.Clear();
            positions.AddRange(cellPositions);
            chances.AddRange(cellChances);
            for (int i = 0; i < positions.Count; i++) dangers.Add(Danger(cellDangers, i));

            foreach (var b in badges) Destroy(b);
            badges.Clear();
            for (int i = 0; i < positions.Count; i++) badges.Add(MakeBadge(positions[i], chances[i], dangers[i]));

            current = 0;
            stepEnds = Time.unscaledTime + StepFor(0);
            outline.enabled = positions.Count > 0;
            if (positions.Count > 0) outline.transform.position = positions[0];
        }

        public void Hide()
        {
            if (positions.Count == 0 && badges.Count == 0 && outline != null && !outline.enabled) return;
            positions.Clear();
            chances.Clear();
            dangers.Clear();
            foreach (var b in badges) Destroy(b);
            badges.Clear();
            if (outline != null) outline.enabled = false;
        }

        float StepFor(int index) => StepBase + chances[index] * StepPerChance;

        GameObject MakeBadge(Vector2 cellCenter, float chance, int danger)
        {
            var root = new GameObject("Chance Badge");
            root.transform.SetParent(transform, false);
            // On the right half of the diamond: the picture is in the middle and the cell's number is low.
            Vector2 at = cellCenter + new Vector2(config.TileWidth * 0.29f, 0f);
            var pill = SpriteUtil.AddSquare(root.transform, "Pill", Vector2.zero, 1f, BadgeColor, BadgeOrder);
            pill.transform.localScale = new Vector3(config.cellSize * 0.66f, config.cellSize * 0.27f, 1f);
            TextUtil.AddText(root.transform, "Percent", Vector2.zero, Mathf.RoundToInt(chance * 100f) + "%", 64, 0.045f, Color.white, BadgeTextOrder);
            root.transform.position = new Vector3(at.x, at.y, 0f);

            if (danger > 0)
            {
                // A red tag above the percent: this cell is in the boss's danger zone.
                var tag = new GameObject("Danger Tag");
                tag.transform.SetParent(root.transform, false);
                var back = SpriteUtil.AddSquare(tag.transform, "Pill", Vector2.zero, 1f, danger >= 2 ? DeathTagColor : DamageTagColor, BadgeOrder);
                back.transform.localScale = new Vector3(config.cellSize * 0.66f, config.cellSize * 0.27f, 1f);
                // Placeholder text: the broken-heart picture replaces it once the art exists.
                string label = danger == 2 ? "DEAD" : danger == 3 ? "DEAD?" : "DANGER";
                TextUtil.AddText(tag.transform, "Label", Vector2.zero, label, 64, 0.04f, Color.white, BadgeTextOrder);
                tag.transform.localPosition = new Vector3(0f, config.cellSize * 0.3f, 0f);
            }
            return root;
        }

        void Update()
        {
            if (positions.Count == 0) return;

            // Walk to the next cell when this one's time is up.
            if (positions.Count > 1 && Time.unscaledTime >= stepEnds)
            {
                current = (current + 1) % positions.Count;
                stepEnds = Time.unscaledTime + StepFor(current);
                outline.transform.position = positions[current];
            }
            // A gentle pulse keeps even a single cell alive.
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 8f);
            int level = dangers[current];
            var color = level >= 2 ? DangerOutlineColor : level == 1 ? ZoneOutlineColor : OutlineColor;
            color.a = pulse;
            outline.color = color;
        }
    }
}
