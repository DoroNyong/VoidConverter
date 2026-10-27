using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>The items the player carries, listed in the top-left corner of the screen.</summary>
    public class ItemListView : MonoBehaviour
    {
        static readonly Color TitleColor = new Color(1f, 0.9f, 0.5f);
        static readonly Color ItemColor = Color.white;
        static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color HintColor = new Color(1f, 1f, 1f, 0.35f);
        const float Margin = 0.3f;

        GameController controller;
        Camera cam;
        TextMesh title;
        TextMesh list;
        TextMesh hint;
        Vector3 lastCameraPosition;
        float lastSize, lastAspect;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            cam = Camera.main;

            title = CreateText("Title", "ITEMS", 64, 0.05f, TitleColor);
            list = CreateText("List", "", 48, 0.04f, ItemColor);
            hint = CreateText("Hint", "", 48, 0.03f, HintColor);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            hint.text = "Debug: [1] Power Ring  [2] Life Charm\n[Backspace] drop the last item";
#endif

            controller.Changed += Refresh;
            Refresh();
            Reposition();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        // The camera is refitted when the window is resized, so keep the list pinned to the screen corner.
        void LateUpdate()
        {
            if (cam == null) return;
            if (cam.transform.position != lastCameraPosition || !Mathf.Approximately(cam.orthographicSize, lastSize) ||
                !Mathf.Approximately(cam.aspect, lastAspect))
            {
                Reposition();
            }
        }

        TextMesh CreateText(string name, string text, int fontSize, float characterSize, Color color)
        {
            var tm = TextUtil.AddText(transform, name, Vector2.zero, text, fontSize, characterSize, color, 10);
            tm.anchor = TextAnchor.UpperLeft;
            tm.alignment = TextAlignment.Left;
            return tm;
        }

        void Reposition()
        {
            lastCameraPosition = cam.transform.position;
            lastSize = cam.orthographicSize;
            lastAspect = cam.aspect;

            // Top-left corner of the view, in world units.
            Vector3 corner = cam.ViewportToWorldPoint(new Vector3(0f, 1f, 10f));
            float x = corner.x + Margin;
            float y = corner.y - Margin;
            title.transform.position = new Vector3(x, y, 0f);
            list.transform.position = new Vector3(x, y - 0.65f, 0f);

            int lines = Mathf.Max(1, list.text.Split('\n').Length);
            hint.transform.position = new Vector3(x, y - 0.65f - lines * 0.5f - 0.4f, 0f);
        }

        void Refresh()
        {
            var items = controller.Items;
            if (items.Count == 0)
            {
                list.text = "none";
                list.color = EmptyColor;
            }
            else
            {
                list.text = Describe(items);
                list.color = ItemColor;
            }
            if (cam != null) Reposition();
        }

        /// <summary>One line per distinct item: "Power Ring x2  ATK +2".</summary>
        static string Describe(IReadOnlyList<ItemDefinition> items)
        {
            var order = new List<string>();
            var counts = new Dictionary<string, int>();
            var first = new Dictionary<string, ItemDefinition>();
            foreach (var item in items)
            {
                if (!counts.ContainsKey(item.Id))
                {
                    counts[item.Id] = 0;
                    first[item.Id] = item;
                    order.Add(item.Id);
                }
                counts[item.Id]++;
            }

            var lines = new List<string>();
            foreach (var id in order)
            {
                var item = first[id];
                int n = counts[id];
                var total = new ItemDefinition(id, item.Name, item.AttackBonus * n, item.MaxHpBonus * n);
                lines.Add($"{item.Name}{(n > 1 ? " x" + n : "")}  {total.Summary}");
            }
            return string.Join("\n", lines);
        }
    }
}
