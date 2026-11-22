using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// World-space text via TextMesh, so no UI canvas or font asset import is needed. The built-in font has no Korean,
    /// so while the language is Korean every text switches to a Korean font: Resources/Fonts/Korean (put a font file
    /// there, e.g. Noto Sans KR or Pretendard) if it exists, else an installed system font (works on Windows / Mac
    /// builds, not in a web build).
    /// </summary>
    public static class TextUtil
    {
        static readonly string[] SystemKoreanFonts = { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "NanumGothic" };

        static Font defaultFont;
        static Font koreanFont;
        static bool koreanLoaded;
        static bool subscribed;
        static readonly List<TextMesh> meshes = new List<TextMesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            defaultFont = null;
            koreanFont = null;
            koreanLoaded = false;
            pixelFont = null;
            pixelFontLoaded = false;
            subscribed = false;
            meshes.Clear();
        }

        static Font DefaultFont
        {
            get
            {
                if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return defaultFont;
            }
        }

        static Font KoreanFont
        {
            get
            {
                if (!koreanLoaded)
                {
                    koreanLoaded = true;
                    koreanFont = Resources.Load<Font>("Fonts/Korean");
                    if (koreanFont == null) koreanFont = Font.CreateDynamicFontFromOSFont(SystemKoreanFonts, 64);
                }
                return koreanFont != null ? koreanFont : DefaultFont;
            }
        }

        // Galmuri11 (Assets/Resources/Fonts) is the game's font for every language: a pixel font with Latin, Korean,
        // Japanese and Hanja. It is drawn crisp only at multiples of its 11 pixel size.
        const string PixelFontPath = "Fonts/Galmuri11";
        const int PixelFontSize = 11;
        /// <summary>
        /// The smallest text: fontSize x characterSize. The camera always shows the same 16:9 area (HudLayout), so text
        /// grows and shrinks with the window. At 1.98 a Galmuri letter is about 15 pixels tall in a 720p window and 22 in
        /// a 1080p one (the stats text, which read fine). Bigger text keeps its own size.
        /// </summary>
        public const float MinTextScale = 1.98f;
        static Font pixelFont;
        static bool pixelFontLoaded;

        static Font PixelFont
        {
            get
            {
                if (!pixelFontLoaded)
                {
                    pixelFontLoaded = true;
                    pixelFont = Resources.Load<Font>(PixelFontPath);
                    if (pixelFont != null) Font.textureRebuilt += OnFontTextureRebuilt;
                }
                return pixelFont;
            }
        }

        // The font picture is rebuilt when new letters appear; keep it unsmoothed so the pixels stay sharp.
        static void OnFontTextureRebuilt(Font font)
        {
            if (font == pixelFont && font.material != null && font.material.mainTexture != null)
                font.material.mainTexture.filterMode = FilterMode.Point;
        }

        // Without the pixel font (the file is missing), Korean needs a system font and English the built-in one.
        static Font CurrentFont => PixelFont != null ? PixelFont : GameSettings.Language == Language.Korean ? KoreanFont : DefaultFont;

        public static TextMesh AddText(Transform parent, string name, Vector2 localPos, string text,
            int fontSize, float characterSize, Color color, int order)
        {
            if (!subscribed)
            {
                subscribed = true;
                GameSettings.Changed += RefreshFonts;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var font = CurrentFont;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            SetSize(tm, fontSize, characterSize);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = order;
            meshes.Add(tm);
            return tm;
        }

        /// <summary>
        /// Sets the size of a text made by AddText, with the same rules: the pixel font is snapped to multiples of its
        /// pixel size (the difference goes to characterSize, so the world size stays as asked), and nothing is smaller
        /// than MinTextScale. For texts that are reused with another size (the pooled board labels in GridView).
        /// </summary>
        public static void SetSize(TextMesh tm, int fontSize, float characterSize)
        {
            if (pixelFont != null && tm.font == pixelFont)
            {
                int snapped = Mathf.Max(PixelFontSize, Mathf.RoundToInt(fontSize / (float)PixelFontSize) * PixelFontSize);
                characterSize *= fontSize / (float)snapped;
                fontSize = snapped;
            }
            // No text smaller than MinTextScale (2026-11-17: item lists, descriptions and hints were too small to read).
            characterSize = Mathf.Max(characterSize, MinTextScale / fontSize);
            tm.fontSize = fontSize;
            tm.characterSize = characterSize;
        }

        /// <summary>Gives every text the font of the current language (after the language setting changed).</summary>
        static void RefreshFonts()
        {
            var font = CurrentFont;
            meshes.RemoveAll(m => m == null);
            foreach (var tm in meshes)
            {
                if (tm.font == font) continue;
                tm.font = font;
                tm.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }
    }
}
