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

        static Sprite ring;

        /// <summary>A 1x1 world-unit thin white ring (a circle outline), tinted via SpriteRenderer.color.</summary>
        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                {
                    const int size = 128;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                    float c = (size - 1) * 0.5f;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                            // A band near the edge, soft on both sides.
                            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                        }
                    }
                    tex.Apply();
                    ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                }
                return ring;
            }
        }

        public static SpriteRenderer AddRing(Transform parent, string name, Vector2 localPos, float size, Color color, int order)
        {
            var sr = AddSquare(parent, name, localPos, size, color, order);
            sr.sprite = Ring;
            return sr;
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
