using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The play screen's panels (HudLayout): the info box in the top left (the last action's result), the character
    /// picture and stats in the right-hand column, the tile odds weights and an empty box in the bottom row. Also the
    /// banner over the board when an Event stage has no events.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        static readonly Color StatsColor = Color.white;
        static readonly Color MessageColor = new Color(0.85f, 0.9f, 1f);
        static readonly Color DeadColor = new Color(1f, 0.45f, 0.45f);
        static readonly Color ClearColor = new Color(1f, 0.9f, 0.4f);
        static readonly Color BannerShade = new Color(0f, 0f, 0f, 0.65f);
        static readonly Color UpColor = new Color(0.6f, 1f, 0.6f);
        static readonly Color DownColor = new Color(1f, 0.6f, 0.55f);
        // Panels sit under every picture and text on the screen (the board's tiles start at order 0).
        const int PanelOrder = -5;
        const int TextOrder = 10;
        // The cell kinds an event can make more or less likely, in the order they are listed.
        static readonly CellType[] WeightedCells = { CellType.Monster, CellType.Gold, CellType.Heal, CellType.Trap };

        GameController controller;
        GameConfig config;
        TextMesh infoTitle;
        TextMesh message;
        TextMesh characterName;
        SpriteRenderer characterPicture;
        int shownCharacter = -1;
        TextMesh stats;
        TextMesh skill;
        TextMesh weightsTitle;
        TextMesh weights;
        GameObject banner;
        TextMesh bannerText;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;

            var info = HudLayout.Info;
            HudLayout.AddPanel(transform, "Info Panel", info, HudLayout.PanelColor, PanelOrder);
            infoTitle = HudLayout.AddLeftText(transform, "Info Title", HudLayout.TopLeft(info), "", 44, 0.045f, HudLayout.TitleColor, TextOrder);
            message = HudLayout.AddLeftText(transform, "Message", HudLayout.TopLeft(info) + new Vector2(0f, -0.55f), "", 44, 0.036f, MessageColor, TextOrder);

            var character = HudLayout.Character;
            HudLayout.AddPanel(transform, "Character Panel", character, HudLayout.PanelColor, PanelOrder);
            characterName = HudLayout.AddLeftText(transform, "Character Name", HudLayout.TopLeft(character), "", 44, 0.045f, HudLayout.TitleColor, TextOrder);
            characterPicture = SpriteUtil.AddSquare(transform, "Character Picture", new Vector2(character.center.x, character.center.y - 0.25f), 1f, Color.white, TextOrder);

            var statsBox = HudLayout.Stats;
            HudLayout.AddPanel(transform, "Stats Panel", statsBox, HudLayout.PanelColor, PanelOrder);
            stats = HudLayout.AddLeftText(transform, "Stats", HudLayout.TopLeft(statsBox), "", 44, 0.045f, StatsColor, TextOrder);
            skill = HudLayout.AddLeftText(transform, "Skill", new Vector2(statsBox.xMin + 0.25f, statsBox.yMin + 0.6f), "", 44, 0.03f, HudLayout.MutedColor, TextOrder);

            var weightsBox = HudLayout.Weights;
            HudLayout.AddPanel(transform, "Weights Panel", weightsBox, HudLayout.PanelColor, PanelOrder);
            weightsTitle = HudLayout.AddLeftText(transform, "Weights Title", HudLayout.TopLeft(weightsBox), "", 44, 0.045f, HudLayout.TitleColor, TextOrder);
            weights = HudLayout.AddLeftText(transform, "Weights", HudLayout.TopLeft(weightsBox) + new Vector2(0f, -0.55f), "", 44, 0.035f, StatsColor, TextOrder);

            // Kept empty for now (2026-11-16).
            HudLayout.AddPanel(transform, "Empty Panel", HudLayout.Empty, HudLayout.PanelColor, PanelOrder);

            // A dim shade over the board with big text, shown when an Event stage has no events.
            banner = new GameObject("Banner");
            banner.transform.SetParent(transform, false);
            Vector2 board = HudLayout.BoardCenter;
            SpriteUtil.AddDiamond(banner.transform, "Shade", board, config.GridHalfWidth * 2f, config.GridHalfHeight * 2f, BannerShade, 30);
            bannerText = TextUtil.AddText(banner.transform, "Banner Text", board, "", 64, 0.09f, ClearColor, 40);
            banner.SetActive(false);

            controller.Changed += Refresh;
            GameSettings.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
            GameSettings.Changed -= Refresh;
        }

        void Refresh()
        {
            var p = controller.Player;

            infoTitle.text = Loc.T("hud.info");
            message.text = controller.Message;

            RefreshCharacter();

            string attack = p.AttackMin == p.AttackMax ? p.AttackMin.ToString() : p.AttackMin + "-" + p.AttackMax;
            stats.text = $"{Loc.T("stat.hp")} {p.Hp}/{p.MaxHp}\n" +
                         $"{Loc.T("stat.atk")} {attack}    {Loc.T("stat.mag")} {p.Magic}\n" +
                         $"{Loc.T("stat.gold")} {p.Gold}";
            stats.color = p.IsDead ? DeadColor : StatsColor;
            skill.text = Loc.T("hud.skill");

            weightsTitle.text = Loc.T("hud.weights");
            RefreshWeights(p);

            // EventView draws the event screen; the banner only covers an Event stage with no events configured.
            // (The shop, clear and defeat screens are drawn by their own views.)
            bool showBanner = controller.Phase == RunPhase.Event && controller.CurrentEvent == null;
            banner.SetActive(showBanner);
            if (!showBanner) return;
            bannerText.text = Loc.T("hud.noEvents");
            bannerText.color = MessageColor;
        }

        /// <summary>Name and picture of the character the run is played with.</summary>
        void RefreshCharacter()
        {
            var list = config.characters;
            int index = controller.SelectedCharacter;
            if (list == null || index < 0 || index >= list.Count)
            {
                characterName.text = "";
                characterPicture.gameObject.SetActive(false);
                return;
            }

            var data = list[index];
            characterName.text = Loc.TOr("char." + data.displayName + ".name", data.displayName);
            if (index == shownCharacter) return;
            shownCharacter = index;

            // A picture named after the character (Resources/Sprites/Characters/warrior); without one, the card color.
            var sprite = Resources.Load<Sprite>("Sprites/Characters/" + data.displayName.ToLowerInvariant());
            var box = HudLayout.Character;
            float height = box.height - 0.9f;
            characterPicture.gameObject.SetActive(true);
            if (sprite != null)
            {
                characterPicture.sprite = sprite;
                characterPicture.color = Color.white;
                float scale = height / sprite.bounds.size.y;
                characterPicture.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                characterPicture.sprite = SpriteUtil.Square;
                characterPicture.color = data.color;
                characterPicture.transform.localScale = new Vector3(height, height, 1f);
            }
        }

        /// <summary>
        /// How much more or less likely each cell kind is than at the start of the run (events such as Lucky Shrine),
        /// one per line, e.g. "Gold +30%".
        /// </summary>
        void RefreshWeights(PlayerStats p)
        {
            var lines = new List<string>();
            bool anyUp = false, anyDown = false;
            foreach (var type in WeightedCells)
            {
                int percent = Mathf.RoundToInt((p.TileWeight(type) - 1f) * 100f);
                if (percent == 0) continue;
                lines.Add($"{Loc.CellName(type)} {Loc.Signed(percent)}%");
                if (percent > 0) anyUp = true; else anyDown = true;
            }

            if (lines.Count == 0)
            {
                weights.text = Loc.T("hud.weightsNone");
                weights.color = HudLayout.MutedColor;
                return;
            }
            // Two columns side by side once there are more than two lines, so four kinds fit the low box.
            weights.text = lines.Count <= 2
                ? string.Join("\n", lines)
                : string.Join("\n", Pair(lines, 0), Pair(lines, 2));
            // Mostly the same direction: tint it; mixed: plain.
            weights.color = anyUp && !anyDown ? UpColor : anyDown && !anyUp ? DownColor : StatsColor;
        }

        static string Pair(List<string> lines, int start)
        {
            if (start >= lines.Count) return "";
            return start + 1 < lines.Count ? lines[start] + "      " + lines[start + 1] : lines[start];
        }
    }
}
