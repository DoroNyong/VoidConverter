using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The screens at both ends of a run, drawn full screen: before the first stage the title, the tutorial and the
    /// character select; after the last the clear / defeat screen (back to character select). Creates itself in any
    /// scene that has a GameController, so the scene file does not need to change. Space / Enter do what the big
    /// button does.
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
        static readonly Color SideButtonColor = new Color(0.2f, 0.26f, 0.36f);
        static readonly Color CardColor = new Color(0.13f, 0.18f, 0.27f);

        // Above every other view except the screen fader.
        const int BackdropOrder = 200;
        const int ButtonOrder = 210;
        const int SwatchOrder = 215;
        const int TextOrder = 220;

        const int CharacterSlots = 2;

        class Button
        {
            public GameObject Root;
            public SpriteRenderer Background;
            public TextMesh Label;
            public Vector2 Center;
            public Vector2 Size;
            public Color Normal;
            public Action Click;

            public bool Contains(Vector3 world) =>
                Mathf.Abs(world.x - Center.x) <= Size.x * 0.5f && Mathf.Abs(world.y - Center.y) <= Size.y * 0.5f;
        }

        GameController controller;
        Camera cam;
        GameObject root;
        TextMesh heading;
        TextMesh details;
        TextMesh hint;
        bool showing;
        bool wasDefeated;
        float defeatedAt;
        const float DefeatPause = 1.2f;

        Button mainButton;
        Button sideButton;
        Button backButton;
        Button skipButton;
        Button selectBackButton;
        readonly Button[] cards = new Button[CharacterSlots];
        readonly SpriteRenderer[] swatches = new SpriteRenderer[CharacterSlots];
        readonly TextMesh[] cardNames = new TextMesh[CharacterSlots];
        readonly TextMesh[] cardTexts = new TextMesh[CharacterSlots];
        readonly List<Button> shown = new List<Button>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<GameController>() == null || FindFirstObjectByType<FlowScreenView>() != null) return;
            new GameObject("Flow Screens").AddComponent<FlowScreenView>();
        }

        Button MakeButton(string name, Vector2 center, Vector2 size, Color color, float textSize, Action click)
        {
            var b = new Button { Center = center, Size = size, Normal = color, Click = click };
            b.Root = new GameObject(name);
            b.Root.transform.SetParent(root.transform, false);
            b.Background = SpriteUtil.AddSquare(b.Root.transform, "Background", center, 1f, color, ButtonOrder);
            b.Background.transform.localScale = new Vector3(size.x, size.y, 1f);
            b.Label = TextUtil.AddText(b.Root.transform, "Label", center, "", 64, textSize, Color.white, TextOrder);
            return b;
        }

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            cam = Camera.main;
            controller.TutorialPageCount = Loc.TutorialPageCount;

            root = new GameObject("Screen");
            root.transform.SetParent(transform, false);
            SpriteUtil.AddSquare(root.transform, "Backdrop", Vector2.zero, 1f, BackdropColor, BackdropOrder).gameObject.AddComponent<CameraFill>();

            heading = TextUtil.AddText(root.transform, "Heading", new Vector2(0f, 1.9f), "", 64, 0.12f, TitleColor, TextOrder);
            details = TextUtil.AddText(root.transform, "Details", new Vector2(0f, 0.3f), "", 64, 0.05f, TextColor, TextOrder);
            hint = TextUtil.AddText(root.transform, "Hint", new Vector2(0f, -2.95f), "", 64, 0.04f, DimTextColor, TextOrder);

            mainButton = MakeButton("Main Button", new Vector2(0f, -1.9f), new Vector2(3.4f, 1.1f), ButtonColor, 0.06f, OnMainButton);
            sideButton = MakeButton("Side Button", new Vector2(0f, -3.2f), new Vector2(3.4f, 0.8f), SideButtonColor, 0.05f, OnSideButton);
            // In the tutorial the Back button sits left of Next; on character select it sits under the cards (see ShowSelectStep).
            backButton = MakeButton("Back Button", new Vector2(-3.4f, -2.2f), new Vector2(2.2f, 0.9f), SideButtonColor, 0.05f, controller.PreviousTutorialPage);
            selectBackButton = MakeButton("Select Back Button", new Vector2(0f, -2.5f), new Vector2(2.2f, 0.9f), SideButtonColor, 0.05f, controller.GoBack);
            skipButton = MakeButton("Skip Button", new Vector2(3.4f, -2.2f), new Vector2(2.2f, 0.9f), SideButtonColor, 0.05f, controller.FinishTutorial);

            for (int i = 0; i < CharacterSlots; i++)
            {
                int index = i;
                var center = new Vector2(i == 0 ? -2.5f : 2.5f, 0.15f);
                cards[i] = MakeButton("Card " + (i + 1), center, new Vector2(4.4f, 3.6f), CardColor, 0.05f, () => controller.ChooseCharacter(index));
                cards[i].Label.text = "";
                swatches[i] = SpriteUtil.AddSquare(cards[i].Root.transform, "Swatch", center + new Vector2(0f, 1.0f), 1.1f, Color.white, SwatchOrder);
                cardNames[i] = TextUtil.AddText(cards[i].Root.transform, "Name", center + new Vector2(0f, 0.15f), "", 64, 0.08f, Color.white, TextOrder);
                cardTexts[i] = TextUtil.AddText(cards[i].Root.transform, "Text", center + new Vector2(0f, -0.95f), "", 64, 0.04f, TextColor, TextOrder);
            }

            controller.Changed += Refresh;
            GameSettings.Changed += Refresh;
            // The run starts on the title screen.
            controller.ShowTitle();
        }

        void OnDestroy()
        {
            GameSettings.Changed -= Refresh;
            if (controller != null) controller.Changed -= Refresh;
        }

        void OnMainButton()
        {
            if (controller.OnTitle)
            {
                if (controller.Step == GameController.TitleStep.Title) controller.PressStart();
                else if (controller.Step == GameController.TitleStep.Tutorial) controller.NextTutorialPage();
            }
            else controller.PressMainButton();
        }

        void OnSideButton()
        {
            if (controller.OnTitle && controller.Step == GameController.TitleStep.Title) controller.OpenTutorial();
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
            showing = visible && !controller.SettingsOpen && (controller.OnTitle || !controller.InputLocked);

            if (!showing) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Button hovered = null;
            foreach (var b in shown)
            {
                if (b.Contains(world)) hovered = b;
            }
            // A light highlight under the pointer.
            foreach (var b in shown) b.Background.color = b == hovered ? Color.Lerp(b.Normal, Color.white, 0.18f) : b.Normal;

            if (hovered != null && mouse.leftButton.wasPressedThisFrame) hovered.Click?.Invoke();
        }

        void Show(params Button[] buttons)
        {
            shown.Clear();
            foreach (var b in new[] { mainButton, sideButton, backButton, selectBackButton, skipButton, cards[0], cards[1] }) b.Root.SetActive(false);
            foreach (var b in buttons)
            {
                b.Root.SetActive(true);
                b.Background.color = b.Normal;
                shown.Add(b);
            }
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

            heading.transform.localPosition = new Vector2(0f, 1.9f);
            if (controller.OnTitle)
            {
                switch (controller.Step)
                {
                    case GameController.TitleStep.Title: ShowTitleStep(); break;
                    case GameController.TitleStep.Tutorial: ShowTutorialStep(); break;
                    default: ShowSelectStep(); break;
                }
                return;
            }

            ShowEndStep(phase == RunPhase.Cleared);
        }

        void ShowTitleStep()
        {
            heading.text = "VOID\nCONVERTER";
            heading.color = TitleColor;
            details.text = Loc.T("title.tagline");
            details.transform.localPosition = new Vector2(0f, 0.1f);
            mainButton.Label.text = Loc.T("title.start");
            sideButton.Label.text = Loc.T("title.howto");
            hint.text = Loc.T("title.hint");
            hint.transform.localPosition = new Vector2(0f, -4.0f);
            Show(mainButton, sideButton);
        }

        void ShowTutorialStep()
        {
            int page = Mathf.Clamp(controller.TutorialPage, 0, Loc.TutorialPageCount - 1);
            bool last = page == Loc.TutorialPageCount - 1;
            heading.text = Loc.T("tut." + page + ".title");
            heading.color = TitleColor;
            details.text = Loc.T("tut." + page + ".body");
            details.transform.localPosition = new Vector2(0f, 0.2f);
            mainButton.Label.text = last ? Loc.T("tut.choose") : Loc.T("tut.next");
            backButton.Label.text = Loc.T("tut.back");
            skipButton.Label.text = last ? "" : Loc.T("tut.skip");
            hint.text = Loc.T("tut.hint", page + 1, Loc.TutorialPageCount);
            hint.transform.localPosition = new Vector2(0f, -3.4f);

            var buttons = new List<Button> { mainButton };
            if (page > 0) buttons.Add(backButton);
            if (!last) buttons.Add(skipButton);
            Show(buttons.ToArray());
        }

        void ShowSelectStep()
        {
            heading.text = Loc.T("select.heading");
            heading.color = TitleColor;
            heading.transform.localPosition = new Vector2(0f, 3.4f);
            details.text = "";
            hint.text = Loc.T("select.hint");
            hint.transform.localPosition = new Vector2(0f, -3.4f);
            selectBackButton.Label.text = Loc.T("select.back");

            var config = controller.Config;
            var buttons = new List<Button>();
            for (int i = 0; i < CharacterSlots; i++)
            {
                if (i >= controller.CharacterCount) continue;
                var c = config.characters[i];
                swatches[i].color = c.color;
                cardNames[i].text = $"{i + 1}. {Loc.TOr("char." + c.displayName + ".name", c.displayName)}";
                cardTexts[i].text = $"HP {c.maxHp}    {Loc.T("stat.atk")} {c.attack}    {Loc.T("stat.mag")} {c.magic}\n{Loc.TOr("char." + c.displayName + ".desc", c.description)}";
                buttons.Add(cards[i]);
            }
            // With no character configured the run still starts with the plain Player block.
            if (buttons.Count == 0) buttons.Add(mainButton);
            buttons.Add(selectBackButton);
            Show(buttons.ToArray());
            if (buttons.Contains(mainButton)) mainButton.Label.text = Loc.T("title.start");
        }

        void ShowEndStep(bool cleared)
        {
            var p = controller.Player;
            heading.text = cleared ? Loc.T("end.cleared") : Loc.T("end.defeated");
            heading.color = cleared ? ClearColor : DeadColor;

            var items = new List<string>();
            foreach (var item in controller.Items) items.Add(item.Name);
            string itemLine = items.Count > 0 ? Loc.T("end.items", string.Join(", ", items)) : Loc.T("end.noItems");
            string progress = cleared ? Loc.T("end.allDone") : Loc.T("end.reached", controller.Stage, controller.StageCount);
            details.text = progress + "\n" + Loc.T("end.stats", p.Gold, p.Hp, p.MaxHp, p.AttackMin, p.Magic) + "\n" + itemLine;
            details.transform.localPosition = new Vector2(0f, 0.3f);
            mainButton.Label.text = Loc.T("end.again");
            hint.text = Loc.T("title.hint");
            hint.transform.localPosition = new Vector2(0f, -2.95f);
            Show(mainButton);
        }
    }
}
