using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;
using Random = System.Random;

namespace VoidConverter.EditorTools
{
    /// <summary>
    /// Plays whole runs with simple bots to measure balance, so numbers can be tuned from data instead of by feel.
    /// Needs play mode (GameController initializes in Awake). Use BalanceSim.Compare from an eval or a menu.
    /// </summary>
    public static class BalanceSim
    {
        public enum Bot
        {
            // Any legal placement. A floor: this should almost never clear.
            Random,
            // Minimizes the damage it expects, and takes rewards when it can. Never fights on purpose.
            Passive,
            // Weighs expected gold and healing against expected damage and the corruption it would add.
            Greedy,
        }

        public class Result
        {
            public Bot Bot;
            public int Runs;
            public int Cleared;
            public double TotalRoundsPlayed;
            public double TotalHpLeftOnClear;
            public double TotalGold;
            public int[] DeathsByRound;
        }

        static readonly FieldInfo ConfigField =
            typeof(GameController).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>Runs every bot on a copy of the default config modified by <paramref name="tweak"/>.</summary>
        public static string Compare(Action<GameConfig> tweak, int runs, string label = "")
        {
            var sb = new StringBuilder();
            sb.Append(label.Length > 0 ? label + ": " : "");
            foreach (Bot bot in Enum.GetValues(typeof(Bot)))
            {
                var cfg = ScriptableObject.CreateInstance<GameConfig>();
                tweak?.Invoke(cfg);
                sb.Append(Format(Run(cfg, bot, runs, 1234)));
                sb.Append(" | ");
                UnityEngine.Object.DestroyImmediate(cfg);
            }
            return sb.ToString();
        }

        public static Result Run(GameConfig cfg, Bot bot, int runs, int seed)
        {
            var rng = new Random(seed);
            var result = new Result { Bot = bot, Runs = runs, DeathsByRound = new int[cfg.stages.Count + 2] };

            for (int i = 0; i < runs; i++)
            {
                var go = new GameObject("BalanceSim");
                go.SetActive(false);
                var gc = go.AddComponent<GameController>();
                ConfigField.SetValue(gc, cfg);
                go.SetActive(true);                      // Awake runs here, with the config above

                int guard = 0;
                while ((gc.Phase == RunPhase.Playing || gc.Phase == RunPhase.Event || gc.Phase == RunPhase.Shop) && guard++ < 200)
                {
                    // The bots do not buy yet: they pick an event choice and walk through the shop.
                    if (gc.Phase != RunPhase.Playing)
                    {
                        if (gc.Phase == RunPhase.Event && !gc.EventResolved) gc.ChooseEventOption(PickEventChoice(gc));
                        gc.PressMainButton();
                        continue;
                    }
                    // The boss stage ends with a click once the boss is open.
                    if (gc.BossReady)
                    {
                        gc.PressMainButton();
                        continue;
                    }
                    if (!PlaceOne(gc, cfg, bot, rng))
                    {
                        if (gc.CanEndRound) gc.PressMainButton();
                        else break;
                    }
                }

                result.TotalGold += gc.Player.Gold;
                if (gc.Phase == RunPhase.Cleared)
                {
                    result.Cleared++;
                    result.TotalRoundsPlayed += cfg.stages.Count;
                    result.TotalHpLeftOnClear += gc.Player.Hp;
                }
                else
                {
                    result.TotalRoundsPlayed += gc.Stage;
                    result.DeathsByRound[Mathf.Min(gc.Stage, cfg.stages.Count + 1)]++;
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            return result;
        }

        public static string Format(Result r)
        {
            var deaths = new StringBuilder();
            for (int round = 1; round < r.DeathsByRound.Length; round++)
            {
                if (r.DeathsByRound[round] > 0) deaths.Append($"r{round}:{r.DeathsByRound[round] * 100f / r.Runs:0}% ");
            }
            double hpOnClear = r.Cleared > 0 ? r.TotalHpLeftOnClear / r.Cleared : 0;
            return $"{r.Bot} clear={r.Cleared * 100f / r.Runs:0}% avgRound={r.TotalRoundsPlayed / r.Runs:0.0} " +
                   $"hpLeftOnClear={hpOnClear:0} gold={r.TotalGold / r.Runs:0} deaths[{deaths.ToString().Trim()}]";
        }

        // Heals when hurt, otherwise takes gold.
        static int PickEventChoice(GameController gc)
        {
            bool hurt = gc.Player.Hp < gc.Player.MaxHp;
            var choices = gc.CurrentEvent.Choices;
            for (int i = 0; i < choices.Count; i++)
            {
                if ((choices[i].Kind == EventEffectKind.Heal) == hurt) return i;
            }
            return 0;
        }

        // One placement for the bot. Returns false when nothing could be placed.
        static bool PlaceOne(GameController gc, GameConfig cfg, Bot bot, Random rng)
        {
            var grid = gc.Grid;
            var hand = gc.Hand;
            var player = gc.Player;
            float chance = VoidRules.MonsterChance(player);

            int bestSlot = -1, bestRot = 0, bestX = 0, bestY = 0;
            double bestScore = double.MinValue;
            int seen = 0;

            for (int slot = 0; slot < hand.Size; slot++)
            {
                if (hand.Slots[slot] == null) continue;
                for (int rot = 0; rot < 4; rot++)
                {
                    var cells = hand.Slots[slot].Rotated(rot);
                    for (int x = 0; x < grid.Width; x++)
                    {
                        for (int y = 0; y < grid.Height; y++)
                        {
                            if (!grid.CanPlace(cells, x, y)) continue;

                            if (bot == Bot.Random)
                            {
                                // Uniform pick over all legal placements (reservoir sampling).
                                seen++;
                                if (rng.Next(seen) != 0) continue;
                                bestSlot = slot; bestRot = rot; bestX = x; bestY = y;
                                continue;
                            }

                            double score = Score(grid, player, cfg, bot, cells, x, y, chance) + rng.NextDouble() * 1e-3;
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSlot = slot; bestRot = rot; bestX = x; bestY = y;
                            }
                        }
                    }
                }
            }

            if (bestSlot < 0) return false;
            hand.Select(bestSlot);
            hand.Rotations[bestSlot] = bestRot;
            return gc.TryPlace(bestX, bestY);
        }

        static double Score(Core.Grid grid, PlayerStats player, GameConfig cfg, Bot bot, Int2[] cells, int x, int y, float chance)
        {
            int fixedPower = grid.MonsterPowerUnder(cells, x, y);
            var voidPowers = grid.VoidPowersUnder(cells, x, y);
            int voidCells = voidPowers.Count;

            double expectedDamage = 0, expectedKillGold = 0;
            foreach (var group in Odds.Breakdown(player, fixedPower, voidPowers, chance))
            {
                int sum = fixedPower + group.Monsters * grid.VoidPower;
                if (sum <= 0) continue;
                expectedDamage += group.Probability * ExpectedDamage(player, sum);
                expectedKillGold += group.Probability * group.WinChance * sum * cfg.goldPerMonsterPower;
            }

            int goldCells = grid.CountUnder(cells, x, y, CellType.Gold);
            int healCells = grid.CountUnder(cells, x, y, CellType.Heal);
            double rewardGold = (goldCells + voidCells * (1 - chance)) * cfg.goldCellReward;
            double heal = Math.Min(player.MaxHp - player.Hp, healCells * cfg.healCellAmount);
            double corruptionCost = voidCells * (0.5 + 0.3 * player.Corruption);

            if (bot == Bot.Passive) return -10 * expectedDamage + rewardGold + heal - corruptionCost;
            return rewardGold + expectedKillGold + heal - 1.5 * expectedDamage - corruptionCost;
        }

        static double ExpectedDamage(PlayerStats player, int power)
        {
            int n = player.AttackMax - player.AttackMin + 1;
            double total = 0;
            for (int roll = player.AttackMin; roll <= player.AttackMax; roll++) total += Math.Max(0, power - roll);
            return total / n;
        }
    }
}
