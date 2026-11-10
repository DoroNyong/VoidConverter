using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The screens at both ends of a run, drawn full screen: the title screen (Start) before the first stage and the
    /// clear / defeat screen (Play Again) after the last. Creates itself in any scene that has a GameController, so
    /// the scene file does not need to change. Space / Enter do what the big button does.
    /// </summary>
    public class FlowScreenView : MonoBehaviour
    {
        static readonly Color BackdropColor = new Color(0.07f, 0.1f, 0.16f);
        static readonly Color TitleColor = new Color(0.8f, 0.65f, 1f);
        static readonly Color ClearColor = new Color(1f, 0.9f, 0.4f);
        static readonly Color DeadColor = new Color(1f, 0.45f, 0.45f);
        static readonly Color TextColor = new Color(0.85f, 0.9f, 1f);
        static readonly Color DimTextColor = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color ButtonColor = new Color(0.25f, 0.55f, 0.35f);
        static readonly Vector2 ButtonSize = new Vector2(3.4f, 1.1f);

        // Above every other view except the screen fader.
        const int BackdropOrder = 200;
        const int ButtonOrder = 210;
        const int TextOrder = 220;

        GameController controller;
        Camera cam;
        GameObject root;
        TextMesh heading;
        TextMesh details;
        TextMesh hint;
        TextMesh buttonLabel;
        Vector2 buttonCenter;
        bool showing;
        bool wasDefeated;
        float defeatedAt;
        const float DefeatPause = 1.2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<GameController>() == null || FindFirstObjectByType<FlowScreenView>() != null) return;
            new GameObject("Flow Screens").AddComponent<FlowScreenView>();
        }

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            cam = Camera.main;

            root = new GameObject("Screen");
            root.transform.SetParent(transform, false);
            SpriteUtil.AddSquare(root.transform, "Backdrop", Vector2.zero, 1f, BackdropColor, BackdropOrder).gameObject.AddComponent<CameraFill>();

            heading = TextUtil.AddText(root.transform, "Heading", new Vector2(0f, 1.9f), "", 64, 0.12f, TitleColor, TextOrder);
            details = TextUtil.AddText(root.transform, "Details", new Vector2(0f, 0.3f), "", 64, 0.05f, TextColor, TextOrder);

            buttonCenter = new Vector2(0f, -1.9f);
            var button = SpriteUtil.AddSquare(root.transform, "Button", buttonCenter, 1f, ButtonColor, ButtonOrder);
            button.transform.localScale = new Vector3(ButtonSize.x, ButtonSize.y, 1f);
            buttonLabel = TextUtil.AddText(root.transform, "Button Label", buttonCenter, "", 64, 0.06f, Color.white, TextOrder);
            hint = TextUtil.AddText(root.transform, "Hint", buttonCenter + new Vector2(0f, -1.05f), "or press Space", 64, 0.04f, DimTextColor, TextOrder);

            controller.Changed += Refresh;
            // The run starts on the title screen.
            controller.ShowTitle();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        void Update()
        {
            // A loss shows the screen after a short pause, so the last hit can be seen first.
            var phase = controller.Phase;
            if (phase == RunPhase.Defeated && !wasDefeated) defeatedAt = Time.unscaledTime;
            wasDefeated = phase == RunPhase.Defeated;
            bool visible = controller.OnTitle || phase == RunPhase.Cleared ||
                           (phase == RunPhase.Defeated && Time.unscaledTime - defeatedAt >= DefeatPause);
            if (root.activeSelf != visible)
            {
                root.SetActive(visible);
                if (visible) Refresh();
            }
            // Buttons work once the screen is fully shown (not during a fade).
            showing = visible && (controller.OnTitle || !controller.InputLocked);

            if (!showing) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            bool inside = Mathf.Abs(world.x - buttonCenter.x) <= ButtonSize.x * 0.5f && Mathf.Abs(world.y - buttonCenter.y) <= ButtonSize.y * 0.5f;
            if (!inside) return;

            if (controller.OnTitle) controller.StartGame();
            else controller.PressMainButton();
        }

        void Refresh()
        {
            var phase = controller.Phase;
            // Visibility is decided in Update; this only refreshes the texts.
            if (!controller.OnTitle && phase != RunPhase.Cleared && phase != RunPhase.Defeated)
            {
                if (root.activeSelf) root.SetActive(false);
                return;
            }

            var p = controller.Player;
            if (controller.OnTitle)
            {
                heading.text = "VOID\nCONVERTER";
                heading.color = TitleColor;
                details.text = "Place a block.\nOne covered cell fires.";
                buttonLabel.text = "Start";
                return;
            }

            bool cleared = phase == RunPhase.Cleared;
            heading.text = cleared ? "CLEARED!" : "DEFEATED";
            heading.color = cleared ? ClearColor : DeadColor;

            var items = new List<string>();
            foreach (var item in controller.Items) items.Add(item.Name);
            string itemLine = items.Count > 0 ? "Items: " + string.Join(", ", items) : "No items";
            string progress = cleared ? "All stages done" : $"Reached stage {controller.Stage}/{controller.StageCount}";
            details.text = $"{progress}\nGold {p.Gold}    HP {p.Hp}/{p.MaxHp}    ATK {p.AttackMin}    MAG {p.Magic}\n{itemLine}";
            buttonLabel.text = "Play Again";
        }
    }
}
