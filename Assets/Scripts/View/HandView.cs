using System.Collections.Generic;
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
        // Not yellow, which is the gold cells' color.
        static readonly Color GhostValidColor = new Color(0.7f, 0.95f, 1f, 0.7f);
        static readonly Color GhostInvalidColor = new Color(1f, 0.3f, 0.3f, 0.55f);
        const int MaxBlockCells = 4;
        // Above every picture on the board (their orders run 1 to 9), below the cells' numbers (11).
        const int GhostOrder = 10;
        // Gap between the grid's right edge and the odds panel. GridView reserves the same room in the camera.
        public const float SidePanelGap = 0.45f;
        public const float SidePanelWidth = 2.8f;

        GameController controller;
        GameConfig config;
        GridView gridView;
        Camera cam;

        bool dragging;
        int hoveredSlot = -1;
        Transform ghostRoot;
        SpriteRenderer[] ghostCells;
        TextMesh ghostInfo;
        // The chance badges and the walking border over the cells that could fire (Pick-One rule).
        PreviewMarksView marks;

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
                ghostCells[i] = SpriteUtil.AddDiamond(ghostRoot, "Ghost Cell", Vector2.zero, config.TileWidth * 0.92f, config.cellSize * 0.92f, GhostValidColor, GhostOrder);
            }
            ghostInfo = TextUtil.AddText(ghostRoot, "Ghost Info", Vector2.zero, "", 64, 0.036f, Color.white, 20);
            // Left-aligned so the lines read as a list and grow rightwards from the grid edge.
            ghostInfo.anchor = TextAnchor.MiddleLeft;
            ghostInfo.alignment = TextAlignment.Left;
            marks = new GameObject("Preview Marks").AddComponent<PreviewMarksView>();
            marks.Init(config);
            HideGhost();

            controller.Changed += Rebuild;
            Rebuild();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Rebuild;
            if (ghostRoot != null) Destroy(ghostRoot.gameObject);
            if (marks != null) Destroy(marks.gameObject);
        }

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null) return;
            // The hand is idle during the event and shop screens and after the run ends.
            if (controller.Phase != RunPhase.Playing || controller.InputLocked) return;

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
                    hasSnap = false;
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
                // Released over a spot the ghost snapped to: place it there. Released far from any legal spot: put it back.
                if (TrySnap(world, out var origin)) controller.TryPlace(origin.X, origin.Y);
                hasSnap = false;
                // A drop that did not place the block puts it back; it is no longer "held".
                controller.ClearSelection();
                hoveredSlot = SlotAt(world);
                Rebuild();
            }
        }

        // How far (in world units) the cursor may be from a block's center for the block to snap to a legal spot.
        // Past it the ghost follows the cursor, red, and releasing puts the block back.
        float SnapRadius => config.StepX * 0.5f;
        // A new spot must be this much closer than the current one to take over (stops flicker between two equal spots).
        const float SnapHysteresis = 0.12f;

        bool hasSnap;
        Int2 snapOrigin;

        /// <summary>
        /// The legal spot for the held block: of every place it fits (CanPlace, so spent cells, blocked cells and any
        /// cell the board lacks are skipped), the one whose center is nearest the cursor on screen, within
        /// <see cref="SnapRadius"/>. Returns false if there is none.
        /// </summary>
        bool TrySnap(Vector3 world, out Int2 origin)
        {
            origin = new Int2(0, 0);
            var cells = controller.Hand.SelectedCells();
            if (cells == null) return false;

            var grid = controller.Grid;
            Vector2 cursor = world;
            float best = SnapRadius;
            bool found = false;
            // A block's cells span 0..3 either way, so every origin that can touch the board lies in this range.
            for (int ox = -4; ox < grid.Width; ox++)
            {
                for (int oy = -4; oy < grid.Height; oy++)
                {
                    if (!grid.CanPlace(cells, ox, oy)) continue;
                    float d = Vector2.Distance(BlockCenter(cells, ox, oy), cursor);
                    // The spot the ghost is on already gets a small head start.
                    if (hasSnap && ox == snapOrigin.X && oy == snapOrigin.Y) d -= SnapHysteresis;
                    if (d >= best) continue;
                    best = d;
                    origin = new Int2(ox, oy);
                    found = true;
                }
            }
            hasSnap = found;
            if (found) snapOrigin = origin;
            return found;
        }

        /// <summary>Screen position of the middle of the block when its origin cell is (ox, oy).</summary>
        Vector2 BlockCenter(Int2[] cells, int ox, int oy)
        {
            Vector2 sum = Vector2.zero;
            foreach (var c in cells) sum += gridView.CellToWorld(ox + c.X, oy + c.Y);
            return sum / cells.Length;
        }

        void UpdateGhost(Vector3 world)
        {
            var cells = controller.Hand.SelectedCells();
            if (cells == null)
            {
                HideGhost();
                return;
            }

            bool valid = TrySnap(world, out var origin);
            for (int i = 0; i < ghostCells.Length; i++)
            {
                bool used = i < cells.Length;
                ghostCells[i].gameObject.SetActive(used);
                if (!used) continue;
                Vector2 pos;
                if (valid)
                {
                    // Snapped: the ghost sits exactly on the cells the block would take.
                    pos = gridView.CellToWorld(origin.X + cells[i].X, origin.Y + cells[i].Y);
                }
                else
                {
                    // Nothing legal nearby: the ghost follows the cursor freely, red, so it is clear it will not land.
                    Vector2 own = config.CellOffset(cells[i].X, cells[i].Y);
                    Vector2 mid = Vector2.zero;
                    foreach (var c in cells) mid += config.CellOffset(c.X, c.Y);
                    pos = (Vector2)world + own - mid / cells.Length;
                }
                ghostCells[i].transform.position = new Vector3(pos.x, pos.y, 0f);
                ghostCells[i].color = valid ? GhostValidColor : GhostInvalidColor;
            }

            // The contest is the block's covered monsters summed against one attack roll. Covered void cells may add
            // a monster each, so the win chance weighs every combination of their outcomes.
            if (valid && controller.PickOneRule)
            {
                ShowPickOdds(cells, origin);
            }
            else if (valid)
            {
                marks.Hide();
                // The size bonus of the held block counts in the win chances below; HideGhost puts it back.
                controller.Player.BlockBonus = controller.AttackBonusFor(cells.Length);
                int fixedPower = controller.Grid.MonsterPowerUnder(cells, origin.X, origin.Y);
                var voidPowers = controller.Grid.VoidPowersUnder(cells, origin.X, origin.Y);
                float monsterChance = VoidRules.MonsterChance(controller.Player);
                float win = Odds.WinChance(controller.Player, fixedPower, voidPowers, monsterChance);

                string cellRewards = RewardLine(cells, origin);
                if (fixedPower == 0 && voidPowers.Count == 0)
                {
                    ghostInfo.text = cellRewards.Length > 0 ? "Safe\n" + cellRewards : "Safe";
                }
                else
                {
                    string text = $"Total {fixedPower}";
                    if (cellRewards.Length > 0) text += "\n" + cellRewards;
                    if (voidPowers.Count > 0)
                    {
                        // One line per possible void result: how likely it is, and the win chance if it happens.
                        text += "\n" + DescribeOutcomes(controller.Player, fixedPower, voidPowers, monsterChance);
                    }
                    text += $"\nWin {Mathf.RoundToInt(win * 100f)}%";
                    ghostInfo.text = text;
                }
                string kindLine = VoidKindLine(cells, origin);
                if (kindLine.Length > 0) ghostInfo.text += "\n" + kindLine;
                ghostInfo.color = GridView.ChanceColor(win);
                controller.Player.BlockBonus = 0;
                // Fixed spot beside the grid, so it never covers the cells the player is judging.
                float sideX = controller.Config.GridHalfWidth + SidePanelGap;
                ghostInfo.transform.position = new Vector3(sideX, 0f, 0f);
                ghostInfo.gameObject.SetActive(true);
            }
            else
            {
                marks.Hide();
                ghostInfo.gameObject.SetActive(false);
            }
        }

        /// <summary>Pick-One rule: the odds of the one cell that will fire, and what the block is worth on average.</summary>
        void ShowPickOdds(Int2[] cells, Int2 origin)
        {
            var candidates = controller.Grid.CandidatesUnder(cells, origin.X, origin.Y);
            var odds = PickOdds.Analyze(controller.Player, candidates, config.goldCellReward, config.healCellAmount, config.goldPerMonsterPower, config.trapDamage);

            // The chance of each cell that could fire goes on the cell itself, and a bright border walks over them.
            var chances = PickChance.Probabilities(candidates, controller.Player);
            var cellPositions = new List<Vector2>(candidates.Count);
            var cellChances = new List<float>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                cellPositions.Add(gridView.CellToWorld(candidates[i].X, candidates[i].Y));
                cellChances.Add(chances[i]);
            }
            marks.Show(cellPositions, cellChances);

            if (odds.Candidates == 0)
            {
                ghostInfo.text = "Nothing under\nthis block";
                ghostInfo.color = new Color(1f, 1f, 1f, 0.6f);
            }
            else
            {
                var lines = new List<string> { $"1 of {odds.Candidates} cells fires" };
                if (odds.Golds > 0) lines.Add($"Gold {Percent(odds.GoldChance)}%  (+${config.goldCellReward})");
                if (odds.Monsters > 0)
                {
                    // With one fixed attack every fight is certain: say how many of the covered monsters are beaten.
                    bool certain = controller.Player.AttackMin == controller.Player.AttackMax;
                    string outcome = certain ? $"beat {odds.MonsterWins} of {odds.Monsters}" : $"win {Percent(odds.MonsterWinChance)}%";
                    lines.Add($"Monster {Percent(odds.MonsterChance)}%  ({outcome})");
                }
                if (odds.Traps > 0) lines.Add($"Trap {Percent(odds.TrapChance)}%  (-{config.trapDamage} HP)");
                if (odds.Heals > 0) lines.Add($"Heal {Percent(odds.HealChance)}%  (+{config.healCellAmount} HP)");
                lines.Add($"Avg ${odds.ExpectedGold:0.0}   HP {(odds.ExpectedHeal - odds.ExpectedDamage):+0.0;-0.0;0.0}");
                ghostInfo.text = string.Join("\n", lines);
                // The more gold on average, the greener.
                ghostInfo.color = GridView.ChanceColor(Mathf.Clamp01(odds.ExpectedGold / 2f));
            }
            float sideX = controller.Config.GridHalfWidth + SidePanelGap;
            ghostInfo.transform.position = new Vector3(sideX, 0f, 0f);
            ghostInfo.gameObject.SetActive(true);
        }

        static int Percent(float chance) => Mathf.RoundToInt(chance * 100f);

        /// <summary>One line about the special voids under the block (they have no contest of their own).</summary>
        string VoidKindLine(Int2[] cells, Int2 origin)
        {
            var grid = controller.Grid;
            var parts = new List<string>();
            int buff = grid.CountVoidKindUnder(cells, origin.X, origin.Y, VoidKind.Buff);
            int spread = grid.CountVoidKindUnder(cells, origin.X, origin.Y, VoidKind.Spread);
            int danger = grid.CountVoidKindUnder(cells, origin.X, origin.Y, VoidKind.Danger);
            if (buff > 0) parts.Add("Buff void removed");
            if (spread > 0) parts.Add("Spread void removed");
            if (danger > 0) parts.Add($"Danger void: hit for your ATK, ${config.voidDangerRewardGold} when it breaks");
            return string.Join("\n", parts);
        }

        /// <summary>What the gold and heal cells under the block give, when it is placed (given the player survives).</summary>
        string RewardLine(Int2[] cells, Int2 origin)
        {
            int gold = controller.Grid.CountUnder(cells, origin.X, origin.Y, CellType.Gold) * config.goldCellReward;
            int heal = controller.Grid.CountUnder(cells, origin.X, origin.Y, CellType.Heal) * config.healCellAmount;
            if (gold == 0 && heal == 0) return "";
            if (gold > 0 && heal > 0) return $"Cells: ${gold}  +{heal} HP";
            return gold > 0 ? $"Cells: ${gold}" : $"Cells: +{heal} HP";
        }

        static string DescribeOutcomes(PlayerStats player, int fixedPower, List<int> voidPowers, float monsterChance)
        {
            var lines = new List<string>();
            foreach (var group in Odds.Breakdown(player, fixedPower, voidPowers, monsterChance))
            {
                string label;
                if (voidPowers.Count == 1) label = group.Monsters == 0 ? "Reward" : "Monster";
                else label = group.Monsters == 1 ? "1 monster" : $"{group.Monsters} monsters";
                lines.Add($"{label} {Mathf.RoundToInt(group.Probability * 100f)}% : win {Mathf.RoundToInt(group.WinChance * 100f)}%");
            }
            return string.Join("\n", lines);
        }

        void HideGhost()
        {
            foreach (var cell in ghostCells) cell.gameObject.SetActive(false);
            ghostInfo.gameObject.SetActive(false);
            if (marks != null) marks.Hide();
        }

        /// <summary>Screen offset of a block cell (x, y) in the hand, with tiles <paramref name="height"/> high.</summary>
        Vector2 DiamondStep(float height, float x, float y)
        {
            return new Vector2((x - y) * height * config.tileAspect * 0.5f, (x + y) * height * 0.5f);
        }

        Vector2 SlotCenter(int index)
        {
            float gridHalfHeight = config.GridHalfHeight;
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

                // Bigger blocks hit harder: say how much, under the block.
                int bonus = controller.AttackBonusFor(cells.Length);
                if (!controller.PickOneRule && (bonus > 0 || controller.Config.fixedHand))
                {
                    TextUtil.AddText(transform, "Block Bonus", center + new Vector2(0f, -config.handSlotSize * 0.5f + 0.25f),
                        $"ATK +{bonus}", 48, 0.036f, bonus > 0 ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 1f, 1f, 0.5f), 10);
                }

                int maxX = 0, maxY = 0;
                foreach (var c in cells)
                {
                    maxX = Mathf.Max(maxX, c.X);
                    maxY = Mathf.Max(maxY, c.Y);
                }
                // The block is drawn in the board's diamond projection, so it looks the way it will on the board.
                float mini = config.handMiniCellSize;
                Vector2 half = DiamondStep(mini, maxX * 0.5f, maxY * 0.5f);
                foreach (var c in cells)
                {
                    Vector2 pos = center + DiamondStep(mini, c.X, c.Y) - half;
                    SpriteUtil.AddDiamond(transform, "Block Cell", pos, mini * config.tileAspect * 0.92f, mini * 0.92f, BlockColor, 1);
                }
            }
        }
    }
}
