using VoidConverter.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The "next stage" box in the bottom right corner (HudLayout): "Leave 2/4" while playing (enabled once the
    /// minimum number of blocks is placed), with the next stage written above it; "Continue" on the event and shop
    /// screens, "Restart" once the run is cleared or lost. Space / Enter do the same. Also the reroll button.
    /// </summary>
    public class RoundButtonView : MonoBehaviour
    {
        static readonly Color EnabledColor = new Color(0.25f, 0.55f, 0.35f);
        static readonly Color DisabledColor = new Color(0.18f, 0.24f, 0.3f);
        static readonly Color RestartColor = new Color(0.7f, 0.4f, 0.25f);
        GameController controller;
        GameConfig config;
        Camera cam;
        SpriteRenderer background;
        TextMesh label;
        // What comes next ("Next: Event (3/11)"), small, above the label.
        TextMesh nextLabel;
        // The "next stage" box in the bottom right corner (HudLayout).
        Rect area;
        // Pick-One rule: the reroll button, left of the hand.
        SpriteRenderer rerollBackground;
        TextMesh rerollLabel;
        Rect rerollArea;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            area = HudLayout.NextStage;
            // Above the full-screen event and shop backdrops, so Continue stays visible there.
            background = HudLayout.AddPanel(transform, "Button", area, EnabledColor, 90);
            label = TextUtil.AddText(transform, "Label", area.center + new Vector2(0f, -0.25f), "", 64, 0.05f, Color.white, 100);
            nextLabel = TextUtil.AddText(transform, "Next Label", new Vector2(area.center.x, area.yMax - 0.45f), "", 44, 0.04f, new Color(1f, 1f, 1f, 0.85f), 100);

            rerollArea = HudLayout.Reroll;
            rerollBackground = HudLayout.AddPanel(transform, "Reroll Button", rerollArea, EnabledColor, 90);
            rerollLabel = TextUtil.AddText(transform, "Reroll Label", rerollArea.center, "", 64, 0.045f, Color.white, 100);

            controller.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            if (area.Contains(world) && !HiddenByScreen) controller.PressMainButton();
            if (rerollArea.Contains(world) && rerollBackground.gameObject.activeSelf) controller.RerollHand();
        }

        /// <summary>"Next: Event (4/11)": the stage after this one and its place in the run.</summary>
        string NextStageLine()
        {
            int next = controller.Stage + 1;
            if (next > controller.StageCount) return Loc.T("hud.nextClear");
            return Loc.T("hud.next", Loc.T("stage." + config.stages[next - 1]), next, controller.StageCount);
        }

        // The title and the clear / defeat screens (FlowScreenView) have their own big button.
        bool HiddenByScreen => controller.OnTitle || controller.Phase == RunPhase.Cleared || controller.Phase == RunPhase.Defeated;

        void Refresh()
        {
            background.gameObject.SetActive(!HiddenByScreen);
            label.gameObject.SetActive(!HiddenByScreen);
            bool playing = controller.Phase == RunPhase.Playing;
            nextLabel.gameObject.SetActive(!HiddenByScreen && playing);
            if (playing) nextLabel.text = NextStageLine();
            label.transform.localPosition = area.center + new Vector2(0f, playing ? -0.25f : 0f);
            bool showReroll = !HiddenByScreen && controller.PickOneRule && controller.Phase == RunPhase.Playing;
            rerollBackground.gameObject.SetActive(showReroll);
            rerollLabel.gameObject.SetActive(showReroll);
            if (showReroll)
            {
                rerollLabel.text = Loc.T("btn.reroll", controller.RerollsLeft);
                rerollBackground.color = controller.RerollsLeft > 0 ? EnabledColor : DisabledColor;
            }

            if (controller.Phase == RunPhase.Event && !controller.EventResolved)
            {
                label.text = Loc.T("btn.chooseOne");
                background.color = DisabledColor;
            }
            else if (controller.Phase == RunPhase.Event || controller.Phase == RunPhase.Shop)
            {
                label.text = Loc.T("btn.continue");
                background.color = EnabledColor;
            }
            else if (controller.Phase != RunPhase.Playing)
            {
                label.text = Loc.T("btn.restart");
                background.color = RestartColor;
            }
            else if (controller.PickOneRule)
            {
                // Reaching the minimum unlocks the button: leave now, or keep placing for more (there is no maximum).
                // Until then it counts the blocks placed against the minimum, e.g. "Leave 2/4".
                bool reached = controller.IsBossStage ? controller.BossReady : controller.CanEndRound;
                string waiting = Loc.T(controller.IsBossStage ? "btn.bossCount" : "btn.leaveCount", controller.PlacementsUsed, controller.MinPlacements);
                string go = controller.IsBossStage
                    ? Loc.T(controller.IsFinalBossRound ? "btn.defeatBoss" : "btn.nextPhase")
                    : Loc.T(controller.RoundOver ? "btn.nextStage" : "btn.leave");
                label.text = (reached ? go : waiting).Replace('\n', ' ');
                background.color = reached ? EnabledColor : DisabledColor;
            }
            else if (controller.IsBossStage)
            {
                bool ready = controller.BossReady;
                string waiting = controller.ClearRule
                    ? $"Monsters\nleft {controller.MonstersLeft}"
                    : $"Hold out\n({controller.PlacementsUsed}/{controller.BossHoldTarget})";
                label.text = ready ? "Defeat\nBoss" : waiting;
                background.color = ready ? RestartColor : DisabledColor;
            }
            else if (controller.ClearRule)
            {
                // The round ends by itself once every monster is covered; there is no button to press.
                label.text = $"Monsters\nleft {controller.MonstersLeft}";
                background.color = DisabledColor;
            }
            else if (controller.CanEndRound)
            {
                label.text = "End Round";
                background.color = EnabledColor;
            }
            else
            {
                label.text = $"End Round\n({controller.PlacementsUsed}/{controller.MinPlacementsToEnd})";
                background.color = DisabledColor;
            }
        }
    }
}
