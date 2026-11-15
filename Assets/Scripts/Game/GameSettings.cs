using System;
using UnityEngine;

namespace VoidConverter.Game
{
    public enum Language { English, Korean }

    /// <summary>
    /// The player's options: music and sound volume, window mode and size, language. Kept between launches in
    /// PlayerPrefs. Views read the values and listen to <see cref="Changed"/>; the settings menu writes them.
    /// </summary>
    public static class GameSettings
    {
        const string MusicKey = "VoidConverter.MusicVolume";
        const string SfxKey = "VoidConverter.SfxVolume";
        const string FullscreenKey = "VoidConverter.Fullscreen";
        const string ResolutionKey = "VoidConverter.Resolution";
        const string LanguageKey = "VoidConverter.Language";

        /// <summary>The window sizes the menu offers. Only sizes that fit the display are used.</summary>
        public static readonly Vector2Int[] ResolutionOptions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
        };

        public const int VolumeSteps = 10;

        /// <summary>Raised after any setting changed.</summary>
        public static event Action Changed;

        static bool loaded;
        static float music = 0.6f;
        // 1 is the loudness the effects had before the setting existed.
        static float sfx = 1f;
        static bool fullscreen;
        static int resolutionIndex;
        static Language language;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            music = PlayerPrefs.GetFloat(MusicKey, music);
            sfx = PlayerPrefs.GetFloat(SfxKey, sfx);
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, 0) == 1;
            resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 0), 0, ResolutionOptions.Length - 1);
            language = (Language)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, (int)Language.English), 0, Enum.GetValues(typeof(Language)).Length - 1);
            Core.Loc.Korean = language == Language.Korean;
        }

        // The texts made by the rules (Core) are in the saved language from the first frame on.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadAtStart() => Load();

        static void Save()
        {
            PlayerPrefs.SetFloat(MusicKey, music);
            PlayerPrefs.SetFloat(SfxKey, sfx);
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
            PlayerPrefs.SetInt(LanguageKey, (int)language);
            PlayerPrefs.Save();
        }

        static float Snap(float volume) => Mathf.Round(Mathf.Clamp01(volume) * VolumeSteps) / VolumeSteps;

        public static float MusicVolume
        {
            get { Load(); return music; }
            set { Load(); music = Snap(value); Save(); Changed?.Invoke(); }
        }

        public static float SfxVolume
        {
            get { Load(); return sfx; }
            set { Load(); sfx = Snap(value); Save(); Changed?.Invoke(); }
        }

        public static bool Fullscreen
        {
            get { Load(); return fullscreen; }
            set { Load(); fullscreen = value; Save(); ApplyDisplay(); Changed?.Invoke(); }
        }

        public static int ResolutionIndex
        {
            get { Load(); return resolutionIndex; }
            set
            {
                Load();
                resolutionIndex = ((value % ResolutionOptions.Length) + ResolutionOptions.Length) % ResolutionOptions.Length;
                Save();
                ApplyDisplay();
                Changed?.Invoke();
            }
        }

        public static Language Language
        {
            get { Load(); return language; }
            set { Load(); language = value; Core.Loc.Korean = language == Language.Korean; Save(); Changed?.Invoke(); }
        }

        /// <summary>The browser decides the size of a web build, so the window rows of the menu are hidden there.</summary>
        public static bool CanChangeWindow => Application.platform != RuntimePlatform.WebGLPlayer;

        /// <summary>Applies the saved window mode and size. Called once at the start and after every change.</summary>
        public static void ApplyDisplay()
        {
            Load();
            if (!CanChangeWindow) return;
            var size = ResolutionOptions[resolutionIndex];
            Screen.SetResolution(size.x, size.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        /// <summary>At the start of the game: only touches the window if the player ever chose a window setting.</summary>
        public static void ApplySavedDisplay()
        {
            if (PlayerPrefs.HasKey(FullscreenKey) || PlayerPrefs.HasKey(ResolutionKey)) ApplyDisplay();
        }
    }
}
