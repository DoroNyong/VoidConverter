using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The event screen, drawn full screen during an Event stage: the event's title and text, and one button per
    /// choice (click to take it). After a choice is taken the buttons dim and Continue (RoundButtonView) unlocks.
    /// </summary>
    public class EventView : MonoBehaviour
    {
        static readonly Color ShadeColor = new Color(0.07f, 0.1f, 0.16f);
        static readonly Color TitleColor = new Color(1f, 0.9f, 0.5f);
        static readonly Color TextColor = new Color(0.85f, 0.9f, 1f);
        static readonly Color ChoiceColor = new Color(0.2f, 0.3f, 0.42f);
        static readonly Color ChosenColor = new Color(0.25f, 0.55f, 0.35f);
        static readonly Color DimColor = new Color(0.16f, 0.18f, 0.22f);
        static readonly Color DimTextColor = new Color(1f, 1f, 1f, 0.4f);
        static readonly Color EffectColor = new Color(0.7f, 1f, 0.75f);
        static readonly Vector2 ChoiceSize = new Vector2(7.2f, 0.8f);
        const float ChoiceGap = 0.2f;
        const float ChoiceTop = 0.4f;

        GameController controller;
        GameConfig config;
        Camera cam;
        GameObject root;
        GameObject choicesRoot;
        TextMesh title;
        TextMesh description;
        TextMesh message;
        TextMesh status;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            root = new GameObject("Event");
            root.transform.SetParent(transform, false);
            // A solid backdrop that always fills the camera, so the board, hand and HUD are hidden behind this screen.
            SpriteUtil.AddSquare(root.transform, "Backdrop", Vector2.zero, 1f, ShadeColor, 60).gameObject.AddComponent<CameraFill>();
            title = TextUtil.AddText(root.transform, "Title", new Vector2(0f, 2.4f), "", 64, 0.06f, TitleColor, 70);
            description = TextUtil.AddText(root.transform, "Description", new Vector2(0f, 1.5f), "", 48, 0.036f, TextColor, 70);
            message = TextUtil.AddText(root.transform, "Message", new Vector2(0f, -2.7f), "", 48, 0.034f, Color.white, 70);
            status = TextUtil.AddText(root.transform, "Status", new Vector2(0f, 3.2f), "", 48, 0.036f, TextColor, 70);
            choicesRoot = new GameObject("Choices");
            choicesRoot.transform.SetParent(root.transform, false);

            controller.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        static Vector2 ChoiceCenter(int index) => new Vector2(0f, ChoiceTop - index * (ChoiceSize.y + ChoiceGap));

        void Update()
        {
            if (controller.Phase != RunPhase.Event || controller.CurrentEvent == null) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            for (int i = 0; i < controller.CurrentEvent.Choices.Count; i++)
            {
                Vector2 c = ChoiceCenter(i);
                if (Mathf.Abs(world.x - c.x) <= ChoiceSize.x * 0.5f && Mathf.Abs(world.y - c.y) <= ChoiceSize.y * 0.5f)
                {
                    controller.ChooseEventOption(i);
                    return;
                }
            }
        }

        void Refresh()
        {
            var e = controller.CurrentEvent;
            bool open = controller.Phase == RunPhase.Event && e != null;
            root.SetActive(open);
            if (!open) return;

            title.text = e.Title;
            description.text = e.Description;
            message.text = controller.Message;
            var p = controller.Player;
            status.text = $"Gold {p.Gold}    HP {p.Hp}/{p.MaxHp}";

            foreach (Transform child in choicesRoot.transform) Destroy(child.gameObject);

            for (int i = 0; i < e.Choices.Count; i++)
            {
                var choice = e.Choices[i];
                bool chosen = controller.EventChoiceTaken == i;
                // Dim the other choices once one was taken, and the ones the player cannot pay for.
                bool dim = controller.EventResolved ? !chosen : !choice.IsAvailable(controller.Player);
                Vector2 c = ChoiceCenter(i);

                var button = SpriteUtil.AddSquare(choicesRoot.transform, "Choice", c, 1f, chosen ? ChosenColor : dim ? DimColor : ChoiceColor, 65);
                button.transform.localScale = new Vector3(ChoiceSize.x, ChoiceSize.y, 1f);

                var textColor = dim ? DimTextColor : Color.white;
                TextUtil.AddText(choicesRoot.transform, "Label", c + new Vector2(-1.9f, 0f), choice.Label, 48, 0.04f, textColor, 70);
                TextUtil.AddText(choicesRoot.transform, "Effect", c + new Vector2(1.9f, 0f), choice.Summary, 48, 0.04f, dim ? DimTextColor : EffectColor, 70);
            }
        }
    }
}
