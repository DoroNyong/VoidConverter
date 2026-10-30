using System;
using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>Pure rules/data for the board. No UnityEngine dependency.</summary>
    public class Grid
    {
        public int Width { get; }
        public int Height { get; }

        readonly Cell[,] cells;

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new Cell[width, height];
        }

        public Cell this[int x, int y] => cells[x, y];

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        /// <summary>
        /// How many spacing rules had to be dropped to lay out the last generated board (see GridLayout.Layout.Relaxation).
        /// 0 is the normal case.
        /// </summary>
        public int LastRelaxation { get; private set; }

        public void Generate(GridSettings settings, Random rng)
        {
            var layout = GridLayout.Build(Width, Height, settings, rng);
            LastRelaxation = layout.Relaxation;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var type = layout.Types[x, y];
                    int power = type == CellType.Monster ? rng.Next(settings.MonsterPowerMin, settings.MonsterPowerMax + 1) : 0;
                    // Empty cells start out spent, like a cell that was already activated.
                    cells[x, y] = new Cell { Type = type, Used = type == CellType.Empty, Power = power };
                }
            }

            VoidPower = rng.Next(settings.VoidPowerMin, settings.VoidPowerMax + 1);
            BuffPower = settings.BuffPower;
            AssignVoidKinds(settings, rng);
        }

        /// <summary>How much stronger a Buff void makes each monster next to it.</summary>
        public int BuffPower { get; private set; }

        // Hands out the special kinds to random void cells; the rest stay Normal.
        void AssignVoidKinds(GridSettings settings, Random rng)
        {
            var voids = new List<Cell>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y].Type == CellType.Void) voids.Add(cells[x, y]);
                }
            }
            for (int i = voids.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (voids[i], voids[j]) = (voids[j], voids[i]);
            }

            int next = 0;
            void Give(VoidKind kind, int count)
            {
                for (int i = 0; i < count && next < voids.Count; i++)
                {
                    var cell = voids[next++];
                    cell.Kind = kind;
                    if (kind == VoidKind.Danger) cell.Hp = settings.DangerHp;
                }
            }
            Give(VoidKind.Buff, settings.BuffCount);
            Give(VoidKind.Spread, settings.SpreadCount);
            Give(VoidKind.Danger, settings.DangerCount);
        }

        /// <summary>
        /// The power of the monster any void cell would spawn right now. It is one shared value, shown faintly on
        /// every void cell, and re-rolled after every placement.
        /// </summary>
        public int VoidPower { get; private set; }

        /// <summary>
        /// When true, a block may also cover cells that were already activated (and Empty cells). Those cells do
        /// nothing, so a monster boxed in by spent cells can still be covered. When false, every covered cell must be fresh.
        /// </summary>
        public bool AllowOverlap { get; set; }

        /// <summary>How many monster cells have not been activated yet.</summary>
        public int CountUnusedMonsters()
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y].Type == CellType.Monster && !cells[x, y].Used) count++;
                }
            }
            return count;
        }

        /// <summary>
        /// A block fits when every cell is inside the grid and (unless overlapping is allowed) not already activated.
        /// </summary>
        public bool CanPlace(Int2[] blockCells, int originX, int originY)
        {
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) return false;
                var cell = cells[x, y];
                if (cell.Used && !AllowOverlap) return false;
            }
            return true;
        }

        /// <summary>
        /// Power of a monster cell right now: its own plus the bonus of every standing Buff void around it.
        /// Buff voids in <paramref name="covered"/> (cells a block is about to activate) no longer count.
        /// </summary>
        public int EffectivePower(int x, int y, HashSet<int> covered = null)
        {
            int power = cells[x, y].Power;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (!InBounds(nx, ny)) continue;
                    var n = cells[nx, ny];
                    if (n.Type != CellType.Void || n.Kind != VoidKind.Buff || n.Used) continue;
                    if (covered != null && covered.Contains(nx + ny * Width)) continue;
                    power += BuffPower;
                }
            }
            return power;
        }

        HashSet<int> CoveredSet(Int2[] blockCells, int originX, int originY)
        {
            var set = new HashSet<int>();
            foreach (var c in blockCells) set.Add(originX + c.X + (originY + c.Y) * Width);
            return set;
        }

        /// <summary>Total power of the monster cells (not yet activated) that the block would cover, buffs included.</summary>
        public int MonsterPowerUnder(Int2[] blockCells, int originX, int originY)
        {
            var covered = CoveredSet(blockCells, originX, originY);
            int total = 0;
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                var cell = cells[x, y];
                if (cell.Type == CellType.Monster && !cell.Used) total += EffectivePower(x, y, covered);
            }
            return total;
        }

        /// <summary>How many not-yet-activated void cells of the given kind the block would cover.</summary>
        public int CountVoidKindUnder(Int2[] blockCells, int originX, int originY, VoidKind kind)
        {
            int count = 0;
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                var cell = cells[x, y];
                if (cell.Type == CellType.Void && cell.Kind == kind && !cell.Used) count++;
            }
            return count;
        }

        /// <summary>How many not-yet-activated cells of the given type the block would cover.</summary>
        public int CountUnder(Int2[] blockCells, int originX, int originY, CellType type)
        {
            int count = 0;
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                var cell = cells[x, y];
                if (cell.Type == type && !cell.Used) count++;
            }
            return count;
        }

        /// <summary>The monster power of each void cell the block would cover (all equal to VoidPower), one entry per cell. Only Normal voids roll a monster.</summary>
        public List<int> VoidPowersUnder(Int2[] blockCells, int originX, int originY)
        {
            var powers = new List<int>();
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                if (cells[x, y].Type == CellType.Void && cells[x, y].Kind == VoidKind.Normal && !cells[x, y].Used) powers.Add(VoidPower);
            }
            return powers;
        }

        /// <summary>
        /// Boss stage: turns up to <paramref name="count"/> randomly chosen monster cells (not yet activated) into
        /// strong monsters with the given power.
        /// </summary>
        public void PlaceStrongMonsters(int count, int power, Random rng)
        {
            var monsters = new List<Cell>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y].Type == CellType.Monster && !cells[x, y].Used) monsters.Add(cells[x, y]);
                }
            }
            for (int i = 0; i < count && monsters.Count > 0; i++)
            {
                int pick = rng.Next(monsters.Count);
                monsters[pick].Power = power;
                monsters.RemoveAt(pick);
            }
        }

        /// <summary>Boss stage: every monster cell that is not yet activated gets stronger by this amount.</summary>
        public void GrowMonsters(int amount)
        {
            if (amount == 0) return;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = cells[x, y];
                    if (cell.Type == CellType.Monster && !cell.Used) cell.Power += amount;
                }
            }
        }

        /// <summary>
        /// Hits a Danger void for <paramref name="damage"/>. Returns true if it broke (it is then locked).
        /// </summary>
        public bool DamageDanger(int x, int y, int damage)
        {
            var cell = cells[x, y];
            if (cell.Type != CellType.Void || cell.Kind != VoidKind.Danger || cell.Used) return false;
            cell.Hp -= damage;
            if (cell.Hp > 0) return false;
            cell.Hp = 0;
            cell.Used = true;
            return true;
        }

        /// <summary>
        /// Every standing Spread void contaminates one random cell next to it (not yet activated, not contaminated,
        /// not another Spread void). Returns how many cells were contaminated.
        /// </summary>
        public int SpreadContamination(Random rng)
        {
            var sources = new List<(int x, int y)>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var c = cells[x, y];
                    if (c.Type == CellType.Void && c.Kind == VoidKind.Spread && !c.Used) sources.Add((x, y));
                }
            }

            int count = 0;
            foreach (var (sx, sy) in sources)
            {
                var targets = new List<Cell>();
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = sx + dx, ny = sy + dy;
                        if (!InBounds(nx, ny)) continue;
                        var n = cells[nx, ny];
                        if (n.Used || n.Contaminated) continue;
                        if (n.Type == CellType.Void && n.Kind == VoidKind.Spread) continue;
                        targets.Add(n);
                    }
                }
                if (targets.Count == 0) continue;
                targets[rng.Next(targets.Count)].Contaminated = true;
                count++;
            }
            return count;
        }

        /// <summary>Rolls a new shared void power from the player's current corruption.</summary>
        public void RerollVoidPower(PlayerStats player, VoidSettings settings, Random rng)
        {
            VoidPower = VoidRules.RollPower(player, settings, rng);
        }

        /// <summary>True if the shape fits somewhere in at least one of its four rotations.</summary>
        public bool CanPlaceAnywhere(BlockShape shape)
        {
            for (int r = 0; r < 4; r++)
            {
                var rotated = shape.Rotated(r);
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (CanPlace(rotated, x, y)) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Activates every covered cell and reports what was activated. Effects (combat, corruption, rewards)
        /// are applied by later systems from this result. Returns null if the block does not fit.
        /// </summary>
        public PlacementResult Place(Int2[] blockCells, int originX, int originY)
        {
            if (!CanPlace(blockCells, originX, originY)) return null;

            var covered = CoveredSet(blockCells, originX, originY);
            var result = new PlacementResult();
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                var cell = cells[x, y];
                // A spent cell under the block (overlap) does nothing.
                if (cell.Used) continue;
                int power = cell.Type == CellType.Void ? VoidPower : EffectivePower(x, y, covered);
                result.Activated.Add(new ActivatedCell
                {
                    X = x, Y = y, Type = cell.Type, Power = power,
                    Kind = cell.Kind, Contaminated = cell.Contaminated,
                });
            }
            // Lock the cells only after every power is read, so a buff covered together with its monsters is already gone.
            foreach (var c in blockCells)
            {
                var cell = cells[originX + c.X, originY + c.Y];
                if (cell.Type == CellType.Void && cell.Kind == VoidKind.Danger) cell.ActivationCount++;
                else cell.Used = true;
            }
            return result;
        }
    }

    public struct GridSettings
    {
        // Cell counts: an exact number of void cells, a minimum for each other type, and the leftover cells
        // split among monster/gold/heal by these weights.
        public int VoidCount;
        public int MonsterMin;
        // Above 0: exactly this many monster cells, gold and heal fill up to RewardMax, and the rest are Empty.
        // 0 keeps the older rule (MonsterMin plus a weighted share of the leftover cells, no Empty cells).
        public int MonsterCount;
        public int GoldMin;
        public int HealMin;
        // Gold and heal cells together never exceed this (0 = no cap). Cells over the cap become monsters.
        public int RewardMax;
        public float MonsterWeight;
        public float GoldWeight;
        public float HealWeight;
        // Spacing, both as Chebyshev distances (2 = cells may not even touch diagonally, 1 = no rule).
        // VoidMinSpacing applies between void cells, RewardMinSpacing between any two reward (gold/heal) cells.
        // MonsterMaxCluster caps the largest group of edge-connected monsters (0 = no cap).
        public int VoidMinSpacing;
        public int RewardMinSpacing;
        public int MonsterMaxCluster;
        public int MonsterPowerMin;
        public int MonsterPowerMax;
        public int VoidPowerMin;
        public int VoidPowerMax;
        // How many of the void cells are each special kind (the rest are Normal).
        public int BuffCount;
        public int SpreadCount;
        public int DangerCount;
        public int BuffPower;
        public int DangerHp;
    }

    public struct ActivatedCell
    {
        public int X;
        public int Y;
        public CellType Type;
        public int Power;
        public VoidKind Kind;
        public bool Contaminated;
    }

    public class PlacementResult
    {
        public List<ActivatedCell> Activated { get; } = new List<ActivatedCell>();
    }
}
