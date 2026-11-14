using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidConverter.Data
{
    /// <summary>
    /// The pictures under Assets/Sprites, found by file name at run time (a sheet's frames in order, a single picture as
    /// one frame). It is built by the editor (see SpriteSheetImport) and kept at Resources/SpriteLibrary, so a picture
    /// can stay in its own folder and still be used by the game.
    /// </summary>
    public class SpriteLibrary : ScriptableObject
    {
        public const string ResourceName = "SpriteLibrary";

        [Serializable]
        public class Entry
        {
            public string name;
            public Sprite[] frames;
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>The frames of the picture with this file name (without extension), or null if there is none.</summary>
        public Sprite[] Frames(string name)
        {
            foreach (var entry in entries)
            {
                if (entry.name == name && entry.frames != null && entry.frames.Length > 0) return entry.frames;
            }
            return null;
        }
    }
}
