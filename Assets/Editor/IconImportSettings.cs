using UnityEditor;
using UnityEngine;

namespace VoidConverter.EditorTools
{
    /// <summary>
    /// Pixel art pictures (Assets/Resources/Icons and Assets/Resources/Tiles) must stay sharp: imported as sprites with
    /// point filtering and no compression. Applies when a picture is first imported or reimported.
    /// </summary>
    public class IconImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Icons/") && !assetPath.StartsWith("Assets/Resources/Tiles/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 16f;
        }
    }
}
