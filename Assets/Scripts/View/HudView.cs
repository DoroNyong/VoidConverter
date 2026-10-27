using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>Run status, corruption and the last action summary above the grid; a banner when the run ends.</summary>
    public class HudView : MonoBehaviour
    {
        static readonly Color StatsColor = Color.white;
        static readonly Color VoidColor = new Color(0.8f, 0.65f, 1f);
        static readonly Color MessageColor = new Color(0.85f, 0.9f, 1f);
        static readonly Color DeadColor = new Color(1f, 0.45f, 0.45f);
        static readonly Color ClearColor = new Color(1f, 0.9f, 0.4f);
        static readonly Color BannerShade = new Color(0f, 0f, 0f, 0.65f);

        GameController controller;
        GameConfig config;
        TextMesh stats;
        TextMesh voidStats;
        TextMesh message;
        GameObject banner;
        TextMesh bannerText;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;

            float step = config.cellSize + config.cellGap;
            float top = config.gridHeight * step * 0.5f;
            stats = TextUtil.AddText(transform, "Stats", new Vector2(0f, top + 1.5f), "", 64, 0.04f, StatsColor, 10);
            voidStats = TextUtil.AddText(transform, "Void Stats", new Vector2(0f, top + 1.05f), "", 48, 0.034f, VoidColor, 10);
            message = TextUtil.AddText(transform, "Message", new Vector2(0f, top + 0.5f), "", 48, 0.032f, MessageColor, 10);

            // A dim shade over the grid with big text, shown when the run is cleared or lost.
            banner = new GameObject("Banner");
            banner.transform.SetParent(transform, false);
            float gridSize = config.gridWidth * step;
            SpriteUtil.AddSquare(banner.transform, "Shade", Vector2.zero, gridSize, BannerShade, 30);
            bannerText = TextUtil.AddText(banner.transform, "Banner Text", Vector2.zero, "", 64, 0.09f, ClearColor, 40);
            banner.SetActive(false);

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
            stats.text = $"Stage {controller.Stage}/{controller.StageCount} {controller.CurrentStage}    HP {p.Hp}/{p.MaxHp}    ATK {p.AttackMin}-{p.AttackMax}" +
                         $"    Gold {p.Gold}    Blocks {controller.PlacementsUsed}/{controller.MaxPlacements}";
            stats.color = p.IsDead ? DeadColor : StatsColor;

            int monsterPercent = Mathf.RoundToInt(VoidRules.MonsterChance(p) * 100f);
            voidStats.text = $"Corruption {p.Corruption}/{p.MaxCorruption}    Void monster {monsterPercent}%    Void power {controller.Grid.VoidPower}";
            message.text = controller.Message;

            // The shop draws its own screen; the other non-playing phases use this banner.
            var phase = controller.Phase;
            bool showBanner = phase == RunPhase.Cleared || phase == RunPhase.Defeated || phase == RunPhase.Event;
            banner.SetActive(showBanner);
            if (!showBanner) return;

            if (phase == RunPhase.Event)
            {
                bannerText.text = "EVENT\n(coming soon)";
                bannerText.color = MessageColor;
            }
            else if (phase == RunPhase.Cleared)
            {
                bannerText.text = $"CLEARED!\nGold {p.Gold}";
                bannerText.color = ClearColor;
            }
            else
            {
                bannerText.text = $"DEFEATED\nStage {controller.Stage}/{controller.StageCount}\nGold {p.Gold}";
                bannerText.color = DeadColor;
            }
        }
    }
}
