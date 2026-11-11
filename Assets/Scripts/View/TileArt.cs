using System.Collections.Generic;
using UnityEngine;
using VoidConverter.Core;

namespace VoidConverter.View
{
    /// <summary>
    /// Tile pictures for the board. A picture is used when a sprite with the right name exists in
    /// Assets/Resources/Tiles; otherwise the board keeps drawing its plain colored diamond. So art can be added one
    /// tile at a time. Names: monster_physical, monster_magic, gold, heal, trap, empty, void.
    /// </summary>
    public static class TileArt
    {
        const string Folder = "Tiles/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>The picture for this cell, or null if there is none (draw the colored diamond instead).</summary>
        public static Sprite For(Cell cell)
        {
            string name;
            switch (cell.Type)
            {
                case CellType.Monster: name = cell.Magic ? "monster_magic" : "monster_physical"; break;
                case CellType.Gold: name = "gold"; break;
                case CellType.Heal: name = "heal"; break;
                case CellType.Trap: name = "trap"; break;
                case CellType.Empty: name = "empty"; break;
                default: name = "void"; break;
            }
            if (!cache.TryGetValue(name, out var sprite))
            {
                sprite = Resources.Load<Sprite>(Folder + name);
                cache[name] = sprite;
            }
            return sprite;
        }

        // Small pictures drawn on top of the colored tile (Assets/Resources/Icons). Sizes are fractions of the tile.
        // A monster stands on the tile and may reach past its top edge; the rest sit in the middle.
        struct IconStyle
        {
            public string Name;
            public float WidthOfTile;
            public float YOffsetOfTile;
        }

        static IconStyle StyleOf(Cell cell)
        {
            switch (cell.Type)
            {
                case CellType.Monster: return new IconStyle { Name = cell.Magic ? "monster_magic" : "monster_physical", WidthOfTile = 0.52f, YOffsetOfTile = 0.14f };
                case CellType.Gold: return new IconStyle { Name = "gold", WidthOfTile = 0.4f, YOffsetOfTile = 0.08f };
                case CellType.Heal: return new IconStyle { Name = "heal", WidthOfTile = 0.36f, YOffsetOfTile = 0.08f };
                case CellType.Trap: return new IconStyle { Name = "trap", WidthOfTile = 0.58f, YOffsetOfTile = 0.1f };
                default: return default;
            }
        }

        /// <summary>The icon for this cell, how wide it is in world units, and how far above the tile center it sits. False if there is none.</summary>
        public static bool TryIcon(Cell cell, float tileWidth, float tileHeight, out Sprite sprite, out float width, out float yOffset)
        {
            sprite = null;
            width = 0f;
            yOffset = 0f;
            var style = StyleOf(cell);
            if (style.Name == null) return false;

            string key = "icon:" + style.Name;
            if (!cache.TryGetValue(key, out sprite))
            {
                sprite = Resources.Load<Sprite>("Icons/" + style.Name);
                cache[key] = sprite;
            }
            if (sprite == null) return false;
            width = tileWidth * style.WidthOfTile;
            yOffset = tileHeight * style.YOffsetOfTile;
            return true;
        }

        /// <summary>Puts <paramref name="sprite"/> on the renderer, scaled so its picture fills width x height world units.</summary>
        public static void Apply(SpriteRenderer renderer, Sprite sprite, float width, float height)
        {
            renderer.sprite = sprite;
            Vector2 size = sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        }
    }
}
