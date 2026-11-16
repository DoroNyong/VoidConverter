using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The settings menu: music and sound volume, fullscreen, window size, language, and a way back to the title
    /// screen. Opens from the Menu button in the top right corner or the Esc key, and covers the game while open.
    /// Creates itself in any scene that has a GameController.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color PanelColor = new Color(0.11f, 0.15f, 0.23f);
        static readonly Color HeadingColor = new Color(0.8f, 0.65f, 1f);
        static readonly Color TextColor = new Color(0.85f, 0.9f, 1f);
        static readonly Color ButtonColor = new Color(0.22f, 0.3f, 0.43f);
        static readonly Color CloseColor = new Color(0.25f, 0.55f, 0.35f);
        static readonly Color WarnColor = new Color(0.7f, 0.3f, 0.3f);

        // Above the flow screens (up to 220) and below the screen fader (1000).
        const int ShadeOrder = 400;
        const int PanelOrder = 410;
        const int ButtonOrder = 420;
        const int TextOrder = 430;
        const int MenuButtonOrder = 350;

        const float RowGap = 1.0f;
        const float LabelX = -2.4f;
        const float MinusX = 0.9f;
        const float ValueX = 2.4f;
        const float PlusX = 3.9f;

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
                Root.activeInHierarchy && Mathf.Abs(world.x - Center.x) <= Size.x * 0.5f && Mathf.Abs(world.y - Center.y) <= Size.y * 0.5f;
        }

        GameController controller;
        Camera cam;
        GameObject panel;
        Button menuButton;
        readonly List<Button> buttons = new List<Button>();
        readonly List<TextMesh> rowLabels = new List<TextMesh>();
        TextMesh heading;
        TextMesh musicValue, sfxValue, fullscreenValue, resolutionValue, languageValue;
        TextMesh fullscreenLabel, resolutionLabel, musicLabel, sfxLabel, languageLabel;
        Button fullscreenButton, resolutionMinus, resolutionPlus, toTitleButton, closeButton;
        bool confirmingTitle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<GameController>() == null || FindFirstObjectByType<SettingsView>() != null) return;
            new GameObject("Settings").AddComponent<SettingsView>();
        }

        Button MakeButton(Transform parent, string name, Vector2 center, Vector2 size, Color color, float textSize, int order, Action click)
        {
            var b = new Button { Center = center, Size = size, Normal = color, Click = click };
            b.Root = new GameObject(name);
            b.Root.transform.SetParent(parent, false);
            b.Background = SpriteUtil.AddBox(b.Root.transform, "Background", center, size.x, size.y, color, order);
            b.Label = TextUtil.AddText(b.Root.transform, "Label", center, "", 64, textSize, Color.white, order + 10);
            buttons.Add(b);
            return b;
        }

        TextMesh MakeText(string name, Vector2 pos, float size, Color color)
        {
            return TextUtil.AddText(panel.transform, name, pos, "", 64, size, color, TextOrder);
        }

        void Start()
        {
            instance = this;
            controller = FindFirstObjectByType<GameController>();
            cam = Camera.main;
            GameSettings.ApplySavedDisplay();

            // The Menu button follows the top right corner of the view.
            menuButton = MakeButton(transform, "Menu Button", Vector2.zero, new Vector2(1.5f, 0.7f), new Color(0.15f, 0.2f, 0.3f, 0.9f), 0.045f, MenuButtonOrder, Toggle);

            panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            SpriteUtil.AddSquare(panel.transform, "Shade", Vector2.zero, 1f, ShadeColor, ShadeOrder).gameObject.AddComponent<CameraFill>();
            SpriteUtil.AddBox(panel.transform, "Panel", new Vector2(0f, 0.2f), 9.6f, 8.2f, PanelColor, PanelOrder);

            heading = MakeText("Heading", new Vector2(0f, 3.7f), 0.1f, HeadingColor);

            float y = 2.4f;
            musicLabel = MakeText("Music Label", new Vector2(LabelX, y), 0.055f, TextColor);
            MakeButton(panel.transform, "Music -", new Vector2(MinusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, () => GameSettings.MusicVolume -= 1f / GameSettings.VolumeSteps).Label.text = "-";
            musicValue = MakeText("Music Value", new Vector2(ValueX, y), 0.055f, Color.white);
            MakeButton(panel.transform, "Music +", new Vector2(PlusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, () => GameSettings.MusicVolume += 1f / GameSettings.VolumeSteps).Label.text = "+";

            y -= RowGap;
            sfxLabel = MakeText("Sfx Label", new Vector2(LabelX, y), 0.055f, TextColor);
            MakeButton(panel.transform, "Sfx -", new Vector2(MinusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, () => GameSettings.SfxVolume -= 1f / GameSettings.VolumeSteps).Label.text = "-";
            sfxValue = MakeText("Sfx Value", new Vector2(ValueX, y), 0.055f, Color.white);
            MakeButton(panel.transform, "Sfx +", new Vector2(PlusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, () => GameSettings.SfxVolume += 1f / GameSettings.VolumeSteps).Label.text = "+";

            y -= RowGap;
            fullscreenLabel = MakeText("Fullscreen Label", new Vector2(LabelX, y), 0.055f, TextColor);
            fullscreenButton = MakeButton(panel.transform, "Fullscreen", new Vector2(ValueX, y), new Vector2(2.4f, 0.7f), ButtonColor, 0.05f, ButtonOrder, () => GameSettings.Fullscreen = !GameSettings.Fullscreen);

            y -= RowGap;
            resolutionLabel = MakeText("Resolution Label", new Vector2(LabelX, y), 0.055f, TextColor);
            resolutionMinus = MakeButton(panel.transform, "Resolution <", new Vector2(MinusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, () => GameSettings.ResolutionIndex--);
            resolutionMinus.Label.text = "<";
            resolutionValue = MakeText("Resolution Value", new Vector2(ValueX, y), 0.05f, Color.white);
            resolutionPlus = MakeButton(panel.transform, "Resolution >", new Vector2(PlusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, () => GameSettings.ResolutionIndex++);
            resolutionPlus.Label.text = ">";

            y -= RowGap;
            languageLabel = MakeText("Language Label", new Vector2(LabelX, y), 0.055f, TextColor);
            MakeButton(panel.transform, "Language <", new Vector2(MinusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, NextLanguage).Label.text = "<";
            languageValue = MakeText("Language Value", new Vector2(ValueX, y), 0.055f, Color.white);
            MakeButton(panel.transform, "Language >", new Vector2(PlusX, y), new Vector2(0.8f, 0.7f), ButtonColor, 0.07f, ButtonOrder, NextLanguage).Label.text = ">";

            toTitleButton = MakeButton(panel.transform, "To Title", new Vector2(-2.3f, -2.9f), new Vector2(4.2f, 0.9f), ButtonColor, 0.045f, ButtonOrder, OnToTitle);
            closeButton = MakeButton(panel.transform, "Close", new Vector2(2.6f, -2.9f), new Vector2(3.4f, 0.9f), CloseColor, 0.05f, ButtonOrder, Close);

            panel.SetActive(false);
            GameSettings.Changed += RefreshTexts;
            controller.Changed += RefreshTexts;
            RefreshTexts();
        }

        void OnDestroy()
        {
            GameSettings.Changed -= RefreshTexts;
            if (controller != null) controller.Changed -= RefreshTexts;
        }

        static void NextLanguage()
        {
            var count = Enum.GetValues(typeof(Language)).Length;
            GameSettings.Language = (Language)(((int)GameSettings.Language + 1) % count);
        }

        void Toggle()
        {
            if (controller.SettingsOpen) Close();
            else Open();
        }

        static SettingsView instance;

        /// <summary>Opens the settings panel (the title screen's Settings button).</summary>
        public static void OpenPanel()
        {
            if (instance != null && !instance.controller.SettingsOpen) instance.Open();
        }

        void Open()
        {
            confirmingTitle = false;
            panel.SetActive(true);
            controller.SetSettingsOpen(true);
            RefreshTexts();
        }

        void Close()
        {
            panel.SetActive(false);
            controller.SetSettingsOpen(false);
        }

        void OnToTitle()
        {
            // Leaving a run loses it, so the first click only asks.
            if (!confirmingTitle)
            {
                confirmingTitle = true;
                RefreshTexts();
                return;
            }
            Close();
            controller.ReturnToTitle();
        }

        void RefreshTexts()
        {
            if (panel == null) return;
            heading.text = Loc.T("set.title");
            menuButton.Label.text = Loc.T("set.menu");
            musicLabel.text = Loc.T("set.music");
            sfxLabel.text = Loc.T("set.sfx");
            fullscreenLabel.text = Loc.T("set.fullscreen");
            resolutionLabel.text = Loc.T("set.resolution");
            languageLabel.text = Loc.T("set.language");
            musicValue.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
            sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
            fullscreenButton.Label.text = GameSettings.Fullscreen ? Loc.T("set.on") : Loc.T("set.off");
            var size = GameSettings.ResolutionOptions[GameSettings.ResolutionIndex];
            resolutionValue.text = size.x + "x" + size.y;
            languageValue.text = Loc.T("set.languageName");
            closeButton.Label.text = Loc.T("set.close");

            // A web build cannot change the window; the browser decides.
            bool windowRows = GameSettings.CanChangeWindow;
            fullscreenLabel.gameObject.SetActive(windowRows);
            fullscreenButton.Root.SetActive(windowRows);
            resolutionLabel.gameObject.SetActive(windowRows);
            resolutionValue.gameObject.SetActive(windowRows);
            resolutionMinus.Root.SetActive(windowRows);
            resolutionPlus.Root.SetActive(windowRows);

            // Returning to the title makes no sense on the title screen itself.
            bool onTitleStep = controller.OnTitle && controller.Step == GameController.TitleStep.Title;
            toTitleButton.Root.SetActive(!onTitleStep);
            toTitleButton.Label.text = confirmingTitle ? Loc.T("set.confirm") : Loc.T("set.toTitle");
            toTitleButton.Normal = confirmingTitle ? WarnColor : ButtonColor;
        }

        void Update()
        {
            // The Menu button sits in the top right corner of the character box (HudLayout), whatever the window shape.
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            var box = HudLayout.Character;
            var corner = new Vector2(box.xMax - 0.95f, box.yMax - 0.55f);
            // The button is drawn around the root's position, and its hit area follows.
            menuButton.Root.transform.position = new Vector3(corner.x, corner.y, 0f);
            menuButton.Center = corner;
            // The title screen has its own Settings button.
            menuButton.Root.SetActive(!(controller.OnTitle && controller.Step == GameController.TitleStep.Title));

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                // Esc backs out of the tutorial and the character select (the controller), so only the title opens it.
                bool backsOut = controller.OnTitle && controller.Step != GameController.TitleStep.Title;
                if (controller.SettingsOpen) Close();
                else if (!backsOut) Open();
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());

            Button hovered = null;
            foreach (var b in buttons)
            {
                if (b == menuButton ? controller.Transitioning : !controller.SettingsOpen) continue;
                if (b.Contains(world)) hovered = b;
            }
            foreach (var b in buttons) b.Background.color = b == hovered ? Color.Lerp(b.Normal, Color.white, 0.18f) : b.Normal;
            if (hovered != null && mouse.leftButton.wasPressedThisFrame) hovered.Click?.Invoke();
        }
    }
}
