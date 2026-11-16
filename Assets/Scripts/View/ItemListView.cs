using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Core;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>The items the player carries, listed in the items box of the right-hand column (HudLayout.Items).</summary>
    public class ItemListView : MonoBehaviour
    {
        static readonly Color TitleColor = new Color(1f, 0.9f, 0.5f);
        static readonly Color ItemColor = Color.white;
        static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color HintColor = new Color(1f, 1f, 1f, 0.35f);
        const float Margin = 0.3f;

        GameController controller;
        TextMesh title;
        TextMesh list;
        TextMesh hint;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();

            HudLayout.AddPanel(transform, "Items Panel", HudLayout.Items, HudLayout.PanelColor, -5);
            title = CreateText("Title", Loc.T("hud.items"), 44, 0.04f, TitleColor);
            list = CreateText("List", "", 44, 0.032f, ItemColor);
            hint = CreateText("Hint", "", 44, 0.022f, HintColor);
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

        TextMesh CreateText(string name, string text, int fontSize, float characterSize, Color color)
        {
            return HudLayout.AddLeftText(transform, name, Vector2.zero, text, fontSize, characterSize, color, 10);
        }

        /// <summary>The list sits in the items box of the right-hand column (HudLayout.Items).</summary>
        void Reposition()
        {
            var box = HudLayout.Items;
            Vector2 top = HudLayout.TopLeft(box);
            title.transform.position = top;
            list.transform.position = top + new Vector2(0f, -0.55f);
            // The debug keys go just under the box, small.
            hint.transform.position = new Vector2(box.xMin + Margin, box.yMin + 0.45f);
        }

        void Refresh()
        {
            title.text = Loc.T("hud.items");
            var items = controller.Items;
            if (items.Count == 0)
            {
                list.text = Loc.T("hud.none");
                list.color = EmptyColor;
            }
            else
            {
                list.text = Describe(items);
                list.color = ItemColor;
            }
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
                var total = new ItemDefinition(id, item.Name, item.AttackBonus * n, item.MaxHpBonus * n, item.MagicBonus * n);
                lines.Add($"{item.Name}{(n > 1 ? " x" + n : "")}  {total.Summary}");
            }
            return string.Join("\n", lines);
        }
    }
}
