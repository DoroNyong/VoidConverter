using UnityEngine;

namespace VoidConverter.View
{
    public static class SpriteUtil
    {
        static Sprite square;

        /// <summary>A 1x1 world-unit white square, tinted via SpriteRenderer.color.</summary>
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return square;
            }
        }

        public static SpriteRenderer AddSquare(Transform parent, string name, Vector2 localPos, float size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
