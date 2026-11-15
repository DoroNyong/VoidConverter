using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoidConverter.Data;

namespace VoidConverter.EditorTools
{
    /// <summary>
    /// Pictures under Assets/Resources/Sprites are pixel art: point filter, no compression. A picture that is wider than it is
    /// tall and whose width is a whole number of its height is a sheet of square frames (24x24, 32x32 ...) and is cut
    /// into one sprite per frame with the pivot at the bottom middle, so a creature keeps standing on the same spot
    /// while its frames change. Any other picture stays one sprite. After every import the SpriteLibrary the game reads
    /// is rebuilt.
    /// </summary>
    public class SpriteSheetImport : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Sprites";
        const string LibraryPath = "Assets/Resources/SpriteLibrary.asset";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root + "/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 16f;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            bool sheet = height > 0 && width > height && width % height == 0;
            if (!sheet)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                // Sprites/UI are 9-slice boxes (buttons, panels): the 4 pixel rim stays fixed when the box is stretched.
                if (assetPath.StartsWith(Root + "/UI/"))
                {
                    importer.spriteBorder = new Vector4(4f, 4f, 4f, 4f);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                }
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            string baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            int frames = width / height;
            var cells = new SpriteMetaData[frames];
            for (int i = 0; i < frames; i++)
            {
                cells[i] = new SpriteMetaData
                {
                    name = baseName + "_" + i,
                    rect = new Rect(i * height, 0, height, height),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                };
            }
#pragma warning disable CS0618
            importer.spritesheet = cells;
#pragma warning restore CS0618
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!Touches(imported) && !Touches(deleted) && !Touches(moved) && !Touches(movedFrom)) return;
            // After this import round, so the sprites of a freshly cut sheet exist.
            EditorApplication.delayCall += Rebuild;
        }

        static bool Touches(string[] paths)
        {
            foreach (var p in paths)
            {
                if (p.StartsWith(Root + "/")) return true;
            }
            return false;
        }

        [MenuItem("Void Converter/Rebuild Sprite Library")]
        public static void Rebuild()
        {
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibrary>(LibraryPath);
            bool created = library == null;
            if (created)
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
                library = ScriptableObject.CreateInstance<SpriteLibrary>();
            }

            library.entries = new List<SpriteLibrary.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => FrameIndex(s.name)).ToArray();
                if (sprites.Length == 0) continue;
                library.entries.Add(new SpriteLibrary.Entry { name = System.IO.Path.GetFileNameWithoutExtension(path), frames = sprites });
            }
            library.entries = library.entries.OrderBy(e => e.name).ToList();

            if (created) AssetDatabase.CreateAsset(library, LibraryPath);
            else EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        // "slime_idle_2" -> 2. A single sprite has no index.
        static int FrameIndex(string spriteName)
        {
            int cut = spriteName.LastIndexOf('_');
            return cut >= 0 && int.TryParse(spriteName.Substring(cut + 1), out int index) ? index : 0;
        }
    }
}
