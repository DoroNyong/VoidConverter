using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// One on-screen button to the right of the hand: "End Round" while playing (enabled after enough blocks were
    /// placed), "Continue" on the event and shop screens, "Restart" once the run is cleared or lost. Space / Enter do the same.
    /// </summary>
    public class RoundButtonView : MonoBehaviour
    {
        static readonly Color EnabledColor = new Color(0.25f, 0.55f, 0.35f);
        static readonly Color DisabledColor = new Color(0.18f, 0.24f, 0.3f);
        static readonly Color RestartColor = new Color(0.7f, 0.4f, 0.25f);
        static readonly Vector2 Size = new Vector2(1.9f, 0.95f);

        GameController controller;
        GameConfig config;
        Camera cam;
        SpriteRenderer background;
        TextMesh label;
        Vector2 center;
        // Pick-One rule: the reroll button, to the left of the hand.
        SpriteRenderer rerollBackground;
        TextMesh rerollLabel;
        Vector2 rerollCenter;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            float gridHalfHeight = config.GridHalfHeight;
            float handWidth = config.handSize * config.handSlotSize + (config.handSize - 1) * config.handSlotGap;
            // Level with the hand slots, just past the last one.
            float x = handWidth * 0.5f + 0.25f + Size.x * 0.5f;
            float y = -(gridHalfHeight + 0.6f + config.handSlotSize * 0.5f);
            center = new Vector2(x, y);

            // Above the full-screen event and shop backdrops, so Continue stays visible there.
            background = SpriteUtil.AddSquare(transform, "Button", center, 1f, EnabledColor, 90);
            background.transform.localScale = new Vector3(Size.x, Size.y, 1f);
            label = TextUtil.AddText(transform, "Label", center, "", 64, 0.04f, Color.white, 100);

            rerollCenter = new Vector2(-x, y);
            rerollBackground = SpriteUtil.AddSquare(transform, "Reroll Button", rerollCenter, 1f, EnabledColor, 90);
            rerollBackground.transform.localScale = new Vector3(Size.x, Size.y, 1f);
            rerollLabel = TextUtil.AddText(transform, "Reroll Label", rerollCenter, "", 64, 0.04f, Color.white, 100);

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

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            bool inside = Mathf.Abs(world.x - center.x) <= Size.x * 0.5f && Mathf.Abs(world.y - center.y) <= Size.y * 0.5f;
            if (inside && !HiddenByScreen) controller.PressMainButton();
            bool onReroll = Mathf.Abs(world.x - rerollCenter.x) <= Size.x * 0.5f && Mathf.Abs(world.y - rerollCenter.y) <= Size.y * 0.5f;
            if (onReroll) controller.RerollHand();
        }

        // The title and the clear / defeat screens (FlowScreenView) have their own big button.
        bool HiddenByScreen => controller.OnTitle || controller.Phase == RunPhase.Cleared || controller.Phase == RunPhase.Defeated;

        void Refresh()
        {
            background.gameObject.SetActive(!HiddenByScreen);
            label.gameObject.SetActive(!HiddenByScreen);
            bool showReroll = !HiddenByScreen && controller.PickOneRule && controller.Phase == RunPhase.Playing;
            rerollBackground.gameObject.SetActive(showReroll);
            rerollLabel.gameObject.SetActive(showReroll);
            if (showReroll)
            {
                rerollLabel.text = $"Reroll (E)\n{controller.RerollsLeft} left";
                rerollBackground.color = controller.RerollsLeft > 0 ? EnabledColor : DisabledColor;
            }

            if (controller.Phase == RunPhase.Event && !controller.EventResolved)
            {
                label.text = "Choose\none";
                background.color = DisabledColor;
            }
            else if (controller.Phase == RunPhase.Event || controller.Phase == RunPhase.Shop)
            {
                label.text = "Continue";
                background.color = EnabledColor;
            }
            else if (controller.Phase != RunPhase.Playing)
            {
                label.text = "Restart";
                background.color = RestartColor;
            }
            else if (controller.PickOneRule)
            {
                // Reaching the goal unlocks the button: cash out now, or keep placing for more.
                bool reached = controller.IsBossStage ? controller.BossReady : controller.CanEndRound;
                string waiting = $"Place\n{controller.PlacementsUsed}/{controller.MinPlacements}";
                string go = controller.IsBossStage ? (controller.IsFinalBossRound ? "Defeat\nBoss" : "Next\nPhase") : controller.RoundOver ? "Next\nStage" : "Leave";
                label.text = reached ? go : waiting;
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
