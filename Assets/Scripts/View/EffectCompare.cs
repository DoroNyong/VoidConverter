using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// A before/after test scene for recording a play clip. The screen is split in two: the left half shows how the
    /// core moment looked in v0.1.3 (the cell just changes, no effect or sound), the right half shows v0.1.4 (the
    /// current effects). Both halves have the same six tiles (monster win physical / magic, monster loss, trap, heal,
    /// gold). Clicking a tile fires only that side's tile.
    /// Space plays all six in a row on both sides at once, M mutes. Not part of the game build: it lives in its own scene.
    /// </summary>
    public class EffectCompare : MonoBehaviour
    {
        class Tile
        {
            public SpriteRenderer Body;
            public Color Color;
            public Vector2 Center;
            public FeedbackEvent Event;
        }

        const float LeftX = -40f;
        const float RightX = 40f;
        const float TileSize = 1.4f;
        const float TileStep = 1.8f;
        const float ViewHalfHeight = 4.2f;
        const float ViewHalfWidth = 3.4f;

        static readonly Color LeftBackground = new Color(0.10f, 0.10f, 0.12f);
        static readonly Color RightBackground = new Color(0.07f, 0.12f, 0.10f);
        static readonly Color DeadColor = new Color(0.22f, 0.22f, 0.25f);

        Camera leftCam, rightCam;
        FeedbackView feedback;
        Tile[] left, right;
        float fittedAspect;
        bool muted;
        Coroutine playing;

        void Start()
        {
            var light = new GameObject("Global Light").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            leftCam = MakeCamera("Left Camera", LeftX, new Rect(0f, 0f, 0.5f, 1f), LeftBackground);
            rightCam = MakeCamera("Right Camera", RightX, new Rect(0.5f, 0f, 0.5f, 1f), RightBackground);
            rightCam.gameObject.AddComponent<AudioListener>();

            // The effects live at the world origin: they are positioned with world coordinates.
            feedback = new GameObject("Feedback (v0.1.4)").AddComponent<FeedbackView>();
            feedback.Setup(rightCam, TileSize);

            left = BuildSide(LeftX, "v0.1.3", "before: the cell only changes", new Color(0.8f, 0.8f, 0.85f));
            right = BuildSide(RightX, "v0.1.4", "after: effects and sound", new Color(0.6f, 1f, 0.7f));
            Fit();
        }

        Camera MakeCamera(string name, float x, Rect rect, Color background)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(x, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ViewHalfHeight;
            cam.rect = rect;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            return cam;
        }

        Tile[] BuildSide(float x, string version, string caption, Color titleColor)
        {
            var root = new GameObject("Side " + version).transform;
            TextUtil.AddText(root, "Title", new Vector2(x, 3.35f), version, 72, 0.1f, titleColor, 5);
            TextUtil.AddText(root, "Caption", new Vector2(x, 2.75f), caption, 40, 0.1f, new Color(1f, 1f, 1f, 0.55f), 5);
            TextUtil.AddText(root, "Hint", new Vector2(x, -3.65f), "click a tile  |  Space: play all  |  M: mute",
                32, 0.1f, new Color(1f, 1f, 1f, 0.35f), 5);

            var defs = new[]
            {
                Def("A3", "Win (A)", new Color(0.85f, 0.3f, 0.28f), FeedbackKind.Win, 3, false, 3),
                Def("M4", "Win (M)", new Color(0.3f, 0.45f, 0.9f), FeedbackKind.Win, 4, true, 4),
                Def("A5", "Lose", new Color(0.55f, 0.18f, 0.18f), FeedbackKind.Lose, 2, false, 5),
                Def("TRAP", "Trap", new Color(0.7f, 0.45f, 0.15f), FeedbackKind.Trap, 1, false, 0),
                Def("HEAL", "Heal", new Color(0.25f, 0.7f, 0.35f), FeedbackKind.Heal, 2, false, 0),
                Def("GOLD", "Gold", new Color(0.85f, 0.7f, 0.2f), FeedbackKind.Gold, 2, false, 0),
            };

            var tiles = new Tile[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                int col = i % 3, row = i / 3;
                var center = new Vector2(x + (col - 1) * TileStep, 0.9f - row * 2.3f);
                var d = defs[i];
                var body = SpriteUtil.AddSquare(root, "Tile " + d.Name, center, TileSize, d.Color, 1);
                TextUtil.AddText(root, "Tile Label", center, d.Label, 40, 0.1f, Color.white, 2);
                TextUtil.AddText(root, "Tile Name", center + new Vector2(0f, -0.95f), d.Name, 36, 0.1f,
                    new Color(1f, 1f, 1f, 0.7f), 2);
                d.Event.X = i;
                tiles[i] = new Tile { Body = body, Color = d.Color, Center = center, Event = d.Event };
            }
            return tiles;
        }

        struct TileDef
        {
            public string Label, Name;
            public Color Color;
            public FeedbackEvent Event;
        }

        static TileDef Def(string label, string name, Color color, FeedbackKind kind, int amount, bool magic, int power)
        {
            return new TileDef
            {
                Label = label,
                Name = name,
                Color = color,
                Event = new FeedbackEvent { Kind = kind, Amount = amount, Magic = magic, Power = power },
            };
        }

        // Frames the six tiles in each half, whatever the window shape.
        void Fit()
        {
            float aspect = (Screen.width * 0.5f) / Mathf.Max(1, Screen.height);
            fittedAspect = aspect;
            float size = Mathf.Max(ViewHalfHeight, ViewHalfWidth / aspect);
            leftCam.orthographicSize = size;
            rightCam.orthographicSize = size;
        }

        void Update()
        {
            if (!Mathf.Approximately((Screen.width * 0.5f) / Mathf.Max(1, Screen.height), fittedAspect)) Fit();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.mKey.wasPressedThisFrame)
                {
                    muted = !muted;
                    AudioListener.volume = muted ? 0f : 1f;
                }
                if (keyboard.spaceKey.wasPressedThisFrame && playing == null) playing = StartCoroutine(PlayAll());
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screen = mouse.position.ReadValue();
                bool onLeft = screen.x < Screen.width * 0.5f;
                var cam = onLeft ? leftCam : rightCam;
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                var tiles = onLeft ? left : right;
                for (int i = 0; i < tiles.Length; i++)
                {
                    var c = tiles[i].Center;
                    if (Mathf.Abs(world.x - c.x) <= TileSize * 0.5f && Mathf.Abs(world.y - c.y) <= TileSize * 0.5f)
                    {
                        // A click fires only the side that was clicked.
                        if (onLeft) FireLeft(i); else FireRight(i);
                        break;
                    }
                }
            }
        }

        IEnumerator PlayAll()
        {
            for (int i = 0; i < left.Length; i++)
            {
                FireLeft(i);
                FireRight(i);
                yield return new WaitForSecondsRealtime(1.6f);
            }
            playing = null;
        }

        void FireLeft(int i)
        {
            StartCoroutine(Blink(left[i]));
        }

        void FireRight(int i)
        {
            StartCoroutine(Blink(right[i]));
            feedback.Play(right[i].Event, right[i].Center);
        }

        // What v0.1.3 showed: the tile just changes (it goes dark for a moment), nothing else.
        IEnumerator Blink(Tile tile)
        {
            tile.Body.color = DeadColor;
            yield return new WaitForSecondsRealtime(0.45f);
            tile.Body.color = tile.Color;
        }
    }
}
