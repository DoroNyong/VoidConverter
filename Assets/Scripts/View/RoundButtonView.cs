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

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            float step = config.cellSize + config.cellGap;
            float gridHalfHeight = config.gridHeight * step * 0.5f;
            float handWidth = config.handSize * config.handSlotSize + (config.handSize - 1) * config.handSlotGap;
            // Level with the hand slots, just past the last one.
            float x = handWidth * 0.5f + 0.25f + Size.x * 0.5f;
            float y = -(gridHalfHeight + 0.6f + config.handSlotSize * 0.5f);
            center = new Vector2(x, y);

            // Above the full-screen event and shop backdrops, so Continue stays visible there.
            background = SpriteUtil.AddSquare(transform, "Button", center, 1f, EnabledColor, 90);
            background.transform.localScale = new Vector3(Size.x, Size.y, 1f);
            label = TextUtil.AddText(transform, "Label", center, "", 64, 0.04f, Color.white, 100);

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
            if (inside) controller.PressMainButton();
        }

        void Refresh()
        {
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
            else if (controller.IsBossStage)
            {
                bool ready = controller.BossReady;
                label.text = ready ? "Defeat\nBoss" : $"Hold out\n({controller.PlacementsUsed}/{controller.BossHoldTarget})";
                background.color = ready ? RestartColor : DisabledColor;
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
