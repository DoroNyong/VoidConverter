using UnityEngine;

namespace VoidConverter.View
{
    /// <summary>World-space text via TextMesh, so no UI canvas or font asset import is needed. ASCII only.</summary>
    public static class TextUtil
    {
        static Font font;

        static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static TextMesh AddText(Transform parent, string name, Vector2 localPos, string text,
            int fontSize, float characterSize, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var tm = go.AddComponent<TextMesh>();
            tm.font = DefaultFont;
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = characterSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = DefaultFont.material;
            renderer.sortingOrder = order;
            return tm;
        }
    }
}
