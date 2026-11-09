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
            var assigned = AssignMonsterSpecs(layout.Types, settings, rng);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var type = layout.Types[x, y];
                    int power = type == CellType.Monster ? rng.Next(settings.MonsterPowerMin, settings.MonsterPowerMax + 1) : 0;
                    bool magic = false;
                    int buff = 0;
                    // With monster types, a monster cell takes one (by weight) instead of a random power.
                    if (type == CellType.Monster && assigned != null)
                    {
                        var spec = assigned[x, y];
                        power = spec.Power;
                        magic = spec.Magic;
                        buff = spec.BuffAdjacent;
                    }
                    // Empty cells start out spent, like a cell that was already activated.
                    cells[x, y] = new Cell { Type = type, Used = type == CellType.Empty, Power = power, Magic = magic, BuffAmount = buff };
                }
            }
            ChooseBuffTargets(rng);

            VoidPower = rng.Next(settings.VoidPowerMin, settings.VoidPowerMax + 1);
            BuffPower = settings.BuffPower;
            AssignVoidKinds(settings, rng);
        }

        // Hands the monster kinds out evenly: each kind gets its weighted share of the monster cells (not a random roll
        // per cell), then cells are swapped until no kind forms a group of more than 2 edge-connected cells.
        // Null when there are no monster kinds (monsters then roll a random power).
        static MonsterSpec[,] AssignMonsterSpecs(CellType[,] types, GridSettings settings, Random rng)
        {
            var specs = settings.MonsterSpecs;
            if (specs == null || specs.Length == 0) return null;
            int w = types.GetLength(0), h = types.GetLength(1);
            var cellsList = new List<(int x, int y)>();
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) if (types[x, y] == CellType.Monster) cellsList.Add((x, y));
            int n = cellsList.Count;

            // Quotas by weight, the remainder drawn by weight.
            float total = 0f;
            foreach (var s in specs) total += Math.Max(0f, s.Weight);
            var quota = new int[specs.Length];
            int given = 0;
            for (int i = 0; i < specs.Length; i++)
            {
                quota[i] = total <= 0f ? n / specs.Length : (int)(n * Math.Max(0f, specs[i].Weight) / total);
                given += quota[i];
            }
            for (; given < n; given++) quota[Array.IndexOf(specs, PickSpec(specs, rng))]++;

            // A kind with a limit per board never gets more than that; the surplus goes to kinds that still have room.
            // With a minimum as well (below the maximum) the limit is drawn for each board: 1 or 2, say.
            var limit = new int[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                int max = specs[i].MaxPerBoard, min = specs[i].MinPerBoard;
                limit[i] = max <= 0 ? int.MaxValue : (min > 0 && min < max ? rng.Next(min, max + 1) : max);
            }
            int surplus = 0;
            for (int i = 0; i < specs.Length; i++)
            {
                if (quota[i] > limit[i])
                {
                    surplus += quota[i] - limit[i];
                    quota[i] = limit[i];
                }
            }
            for (; surplus > 0; surplus--)
            {
                var room = new List<MonsterSpec>();
                for (int i = 0; i < specs.Length; i++)
                {
                    if (quota[i] < limit[i]) room.Add(specs[i]);
                }
                // Every kind is full (only limited kinds are in play): the limit gives way, so no monster cell is left without a kind.
                if (room.Count == 0) room.AddRange(specs);
                quota[Array.IndexOf(specs, PickSpec(room.ToArray(), rng))]++;
            }

            var kinds = new List<int>();
            for (int i = 0; i < specs.Length; i++) for (int k = 0; k < quota[i]; k++) kinds.Add(i);
            for (int i = kinds.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
            }

            var kindAt = new int[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) kindAt[x, y] = -1;
            for (int i = 0; i < n; i++) kindAt[cellsList[i].x, cellsList[i].y] = kinds[i];

            int bad = KindClusterExcess(kindAt, w, h);
            for (int step = 0; step < 400 && bad > 0 && n > 1; step++)
            {
                var a = cellsList[rng.Next(n)];
                var b = cellsList[rng.Next(n)];
                if (kindAt[a.x, a.y] == kindAt[b.x, b.y]) continue;
                (kindAt[a.x, a.y], kindAt[b.x, b.y]) = (kindAt[b.x, b.y], kindAt[a.x, a.y]);
                int next = KindClusterExcess(kindAt, w, h);
                if (next <= bad) bad = next;
                else (kindAt[a.x, a.y], kindAt[b.x, b.y]) = (kindAt[b.x, b.y], kindAt[a.x, a.y]);
            }

            var result = new MonsterSpec[w, h];
            foreach (var c in cellsList) result[c.x, c.y] = specs[kindAt[c.x, c.y]];
            return result;
        }

        // Cells over 2 in every group of edge-connected monsters of the same kind.
        static int KindClusterExcess(int[,] kindAt, int w, int h)
        {
            int excess = 0;
            var seen = new bool[w, h];
            var stack = new Stack<(int x, int y)>();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (seen[x, y] || kindAt[x, y] < 0) continue;
                    int kind = kindAt[x, y], size = 0;
                    stack.Push((x, y));
                    seen[x, y] = true;
                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();
                        size++;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = p.x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = p.y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h || seen[nx, ny] || kindAt[nx, ny] != kind) continue;
                            seen[nx, ny] = true;
                            stack.Push((nx, ny));
                        }
                    }
                    if (size > 2) excess += size - 2;
                }
            }
            return excess;
        }

        static MonsterSpec PickSpec(MonsterSpec[] specs, Random rng)
        {
            float total = 0f;
            foreach (var s in specs) total += Math.Max(0f, s.Weight);
            if (total <= 0f) return specs[rng.Next(specs.Length)];
            float roll = (float)rng.NextDouble() * total;
            foreach (var s in specs)
            {
                roll -= Math.Max(0f, s.Weight);
                if (roll < 0f) return s;
            }
            return specs[specs.Length - 1];
        }

        // Each buffing monster (the Wisp) picks one monster next to it to strengthen, one that is not a buffer itself.
        // Cells that share an edge come first; only with none of those does it look at the cells touching its corners.
        // With no monster around it at all it buffs nobody.
        void ChooseBuffTargets(Random rng)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = cells[x, y];
                    if (cell.Type != CellType.Monster || cell.BuffAmount <= 0) continue;
                    PickBuffTarget(x, y, rng);
                }
            }
        }

        // Picks a new target for the buffing monster at (x, y) among the monsters next to it that still stand
        // (not fired, not covered). Sets the target to none (-1) if there is no such monster.
        void PickBuffTarget(int x, int y, Random rng)
        {
            var dx = new[] { 1, -1, 0, 0, 1, 1, -1, -1 };
            var dy = new[] { 0, 0, 1, -1, 1, -1, 1, -1 };
            var cell = cells[x, y];
            var options = new List<(int x, int y)>();
            for (int ring = 0; ring < 2 && options.Count == 0; ring++)
            {
                for (int d = ring * 4; d < ring * 4 + 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (!InBounds(nx, ny)) continue;
                    var n = cells[nx, ny];
                    if (n.Type == CellType.Monster && n.BuffAmount <= 0 && !n.Used && !n.Covered) options.Add((nx, ny));
                }
            }
            if (options.Count == 0)
            {
                cell.BuffX = -1;
                cell.BuffY = -1;
                return;
            }
            var pick = options[rng.Next(options.Count)];
            cell.BuffX = pick.x;
            cell.BuffY = pick.y;
        }

        // A standing buffing monster whose target was fired or covered picks a new one among its neighbours.
        // Called after every placement.
        void RetargetBuffs(Random rng)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var w = cells[x, y];
                    if (w.Type != CellType.Monster || w.BuffAmount <= 0 || w.Used || w.Covered || w.BuffX < 0) continue;
                    var target = cells[w.BuffX, w.BuffY];
                    if (!target.Used && !target.Covered) continue;
                    PickBuffTarget(x, y, rng);
                }
            }
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

        /// <summary>
        /// Pick-One rule: a cell a block already covered cannot be covered again (it is dead, fired or not). Empty cells
        /// were never covered, so they stay free.
        /// </summary>
        public bool BlockCovered { get; set; }

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
                if (BlockCovered && cell.Covered) return false;
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
            // A buffing monster (the Wisp) strengthens the one monster it picked, for as long as it stands:
            // not fired, not covered by a block (a covered cell is lost) and not under the block about to land.
            for (int wx = 0; wx < Width; wx++)
            {
                for (int wy = 0; wy < Height; wy++)
                {
                    var w = cells[wx, wy];
                    if (w.BuffAmount <= 0 || w.BuffX != x || w.BuffY != y) continue;
                    if (w.Used || w.Covered) continue;
                    if (covered != null && covered.Contains(wx + wy * Width)) continue;
                    power += w.BuffAmount;
                }
            }
            return power;
        }

        /// <summary>
        /// True while the buffing monster at (x, y) still has a monster to strengthen: it stands, and the monster it
        /// picked has not fired and has not been covered by a block (a covered monster is lost).
        /// </summary>
        public bool IsBuffLinkAlive(int x, int y)
        {
            var w = cells[x, y];
            if (w.BuffAmount <= 0 || w.BuffX < 0 || w.Used || w.Covered) return false;
            var target = cells[w.BuffX, w.BuffY];
            return !target.Used && !target.Covered;
        }

        /// <summary>True if a standing Wisp is strengthening the monster at (x, y) right now.</summary>
        public bool IsBuffed(int x, int y) => EffectivePower(x, y) > cells[x, y].Power;

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
                // The boss's strong monsters hit the attack, never the (weaker) magic.
                monsters[pick].Magic = false;
                // A strong monster is no Wisp any more.
                monsters[pick].BuffAmount = 0;
                monsters[pick].BuffX = -1;
                monsters[pick].BuffY = -1;
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

        /// <summary>The fresh (not activated, not Empty) cells a block would cover, in the order of the block's cells.</summary>
        public List<ActivatedCell> CandidatesUnder(Int2[] blockCells, int originX, int originY)
        {
            var list = new List<ActivatedCell>();
            // Buffs from cells this block covers do not count: a covered cell that does not fire is lost.
            var covered = CoveredSet(blockCells, originX, originY);
            foreach (var c in blockCells)
            {
                int x = originX + c.X;
                int y = originY + c.Y;
                if (!InBounds(x, y)) continue;
                var cell = cells[x, y];
                if (cell.Used || cell.Covered || cell.Type == CellType.Empty) continue;
                list.Add(new ActivatedCell { X = x, Y = y, Type = cell.Type, Power = EffectivePower(x, y, covered), Magic = cell.Magic, Kind = cell.Kind, Contaminated = cell.Contaminated });
            }
            return list;
        }

        /// <summary>How many cells could still fire (not activated, not Empty).</summary>
        public int CountFresh()
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!cells[x, y].Used && !cells[x, y].Covered && cells[x, y].Type != CellType.Empty) count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Pick-one rule: only one random fresh cell under the block fires (and locks); the other covered cells stay as
        /// they were. The result lists that one cell, or nothing if the block covers no fresh cell.
        /// Returns null if the block does not fit.
        /// </summary>
        public PlacementResult PlaceOne(Int2[] blockCells, int originX, int originY, Random rng, PlayerStats player = null)
        {
            if (!CanPlace(blockCells, originX, originY)) return null;

            var result = new PlacementResult();
            var candidates = CandidatesUnder(blockCells, originX, originY);
            // Every cell under the block counts as covered, whether or not it fires.
            foreach (var c in blockCells)
            {
                int cx = originX + c.X, cy = originY + c.Y;
                cells[cx, cy].Covered = true;
            }
            if (candidates.Count == 0)
            {
                RetargetBuffs(rng);
                return result;
            }

            // Equal chances unless the player's tile weights say otherwise (PickChance).
            var pick = candidates[PickChance.Pick(candidates, player, rng)];
            cells[pick.X, pick.Y].Used = true;
            result.Activated.Add(pick);
            // A Wisp whose monster was just covered or fired looks for a new one.
            RetargetBuffs(rng);
            return result;
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
        // Trap cells per board (Pick-One rule), placed by GridLayout together with the others.
        public int TrapCount;
        // Above 0: no group of edge-connected cells of the same kind (monster, trap, gold, heal) may be larger than
        // this, and traps may not touch heal cells. The layout is shuffled and repaired until it holds.
        public int MaxCluster;
        // Same for monsters only (they are the most numerous kind). 0 = use MaxCluster.
        public int MaxMonsterCluster;
        // Monster types a monster cell may take (kind, required power, weight). Null/empty = a random physical power.
        public MonsterSpec[] MonsterSpecs;
        public int GoldMin;
        public int HealMin;
        // Pick-One rule (MonsterCount above 0): gold and heal counts are drawn from GoldMin..GoldMax and HealMin..HealMax.
        public int GoldMax;
        public int HealMax;
        // Pick-One rule: trap count is drawn from TrapCount..TrapMax.
        public int TrapMax;
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

    /// <summary>One kind of monster: beaten when the player's Attack (physical) or Magic is at least Power.</summary>
    public struct MonsterSpec
    {
        public bool Magic;
        public int Power;
        public float Weight;
        // At most this many of the kind per board (0 = no limit). With MinPerBoard above 0 (and below the maximum)
        // each board draws its own limit between the two, so it varies from board to board.
        public int MaxPerBoard;
        public int MinPerBoard;
        // Attack this monster gives one neighbouring monster while it stands (0 = none).
        public int BuffAdjacent;
    }

    public struct ActivatedCell
    {
        public bool Magic;
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
