using UnityEngine;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// Background music, looping, at the music volume of the settings. Plays Resources/Music/bgm if a file is put
    /// there; until then a quiet placeholder pad is made in code. Creates itself in any scene with a GameController.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        // The pad is quiet on purpose: it only has to prove the volume setting works until real music exists.
        const float PadGain = 0.35f;
        const int Rate = 22050;
        const int LoopSeconds = 16;

        AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<GameController>() == null || FindFirstObjectByType<MusicPlayer>() != null) return;
            new GameObject("Music").AddComponent<MusicPlayer>();
        }

        void Start()
        {
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = Resources.Load<AudioClip>("Music/bgm") ?? MakePad();
            source.ignoreListenerPause = true;
            Apply();
            source.Play();
            GameSettings.Changed += Apply;
        }

        void OnDestroy() => GameSettings.Changed -= Apply;

        void Apply()
        {
            if (source != null) source.volume = GameSettings.MusicVolume * PadGain;
        }

        // A slow chord of sine voices. Every frequency fits a whole number of cycles into the loop, so it repeats
        // without a click; each voice swells and fades on its own cycle.
        static AudioClip MakePad()
        {
            var voices = new[]
            {
                (freq: 110f, swells: 1, phase: 0.0f),
                (freq: 164.8f, swells: 2, phase: 1.3f),
                (freq: 220f, swells: 3, phase: 2.1f),
                (freq: 261.6f, swells: 5, phase: 0.7f),
                (freq: 329.6f, swells: 4, phase: 3.4f),
            };
            int count = Rate * LoopSeconds;
            var data = new float[count];
            foreach (var v in voices)
            {
                // Round to a frequency with a whole number of cycles per loop.
                double cycles = System.Math.Round(v.freq * LoopSeconds);
                for (int i = 0; i < count; i++)
                {
                    double t = (double)i / count;
                    float swell = 0.5f + 0.5f * Mathf.Sin((float)(2.0 * System.Math.PI * v.swells * t) + v.phase);
                    data[i] += (float)System.Math.Sin(2.0 * System.Math.PI * cycles * t) * swell * 0.2f;
                }
            }
            var clip = AudioClip.Create("Placeholder Pad", count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
