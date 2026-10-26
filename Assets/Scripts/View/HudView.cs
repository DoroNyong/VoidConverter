using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>Player stats, corruption and the last placement summary, above the grid.</summary>
    public class HudView : MonoBehaviour
    {
        static readonly Color StatsColor = Color.white;
        static readonly Color VoidColor = new Color(0.8f, 0.65f, 1f);
        static readonly Color MessageColor = new Color(0.85f, 0.9f, 1f);
        static readonly Color DeadColor = new Color(1f, 0.45f, 0.45f);

        GameController controller;
        GameConfig config;
        TextMesh stats;
        TextMesh voidStats;
        TextMesh message;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;

            float step = config.cellSize + config.cellGap;
            float top = config.gridHeight * step * 0.5f;
            stats = TextUtil.AddText(transform, "Stats", new Vector2(0f, top + 1.5f), "", 64, 0.045f, StatsColor, 10);
            voidStats = TextUtil.AddText(transform, "Void Stats", new Vector2(0f, top + 1.05f), "", 48, 0.034f, VoidColor, 10);
            message = TextUtil.AddText(transform, "Message", new Vector2(0f, top + 0.5f), "", 48, 0.032f, MessageColor, 10);

            controller.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        void Refresh()
        {
            var p = controller.Player;
            stats.text = $"HP {p.Hp}/{p.MaxHp}    ATK {p.AttackMin}-{p.AttackMax}    Blocks {controller.PlacementsUsed}/{controller.MaxPlacements}";
            stats.color = p.IsDead ? DeadColor : StatsColor;

            int monsterPercent = Mathf.RoundToInt(VoidRules.MonsterChance(p) * 100f);
            voidStats.text = $"Corruption {p.Corruption}/{p.MaxCorruption}    Void monster {monsterPercent}%    Void power {controller.Grid.VoidPower}";
            message.text = controller.Message;
        }
    }
}
